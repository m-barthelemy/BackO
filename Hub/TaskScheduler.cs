#define DEBUG
using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using P2PBackup.Common;
using P2PBackupHub.Utilities;
using System.Configuration;
using ServiceStack.Text;

namespace P2PBackupHub{

	// The scheduler manage tasks lifecycle and maintains a list of running ones.
	// Every minute we poll the database to retrieve the next tasks to start
	public class TaskScheduler{

		public delegate void TaskEventHandler(Task t, PeerNode n);
		public event TaskEventHandler TaskEvent;
		public delegate void NodeWakeUpNeededHandler(Node n);
		public event NodeWakeUpNeededHandler NodeWakeUpNeeded;

		private static TaskScheduler _instance;
		private static CancellationTokenSource tokenSource;
		private static int maxLateness;
		private static bool isFirstStart;
		private static TasksList TasksQueue;
		private static Thread workerThread;

		private TaskScheduler(){
			isFirstStart = true;
			TasksQueue = new TasksList();
			maxLateness = int.Parse(ConfigurationManager.AppSettings["BackupSet.RetryTime"]);
			tokenSource = new CancellationTokenSource();
			workerThread = new Thread( () => Schedule(tokenSource.Token) );
        	workerThread.Start();
			int availThreads,null1 = 0;
			int maxThreads,null2 = 0;
			ThreadPool.GetAvailableThreads(out availThreads, out null1);
			ThreadPool.GetMaxThreads(out maxThreads, out null2);
			Logger.Append("HUBRN", Severity.INFO, "Started scheduler, thread pool "+availThreads+"/"+maxThreads)	;
		}
		
		public static void Start(){
			if(_instance == null)
				_instance = new TaskScheduler();
		}
		
		public static void Stop(){
			tokenSource.Cancel();
			Logger.Append("HUBRN", Severity.INFO, "Stopping scheduler...");
			workerThread.Join();
		}
		
		internal static TaskScheduler Instance(){
			return _instance;
		}
		
		internal  List<Task> Tasks{
			get{return TasksQueue.ToList();}	
		}
		

		private void Schedule(CancellationToken cancelToken){
			while(!cancelToken.IsCancellationRequested){
				if(isFirstStart){
					int interruptedTasks = new DAL.TaskDAO().UpdateInterrupted();
					Logger.Append("HUBRN", Severity.INFO, "Updated "+interruptedTasks+" previously interrupted task(s).");
					Logger.Append("HUBRN", Severity.INFO, "Waiting 20s before starting to schedule tasks...");
					Thread.Sleep(20000);
					isFirstStart = false;
				}
				Logger.Append("HUBRN", Severity.DEBUG, "Polling database for next BackupSets to run.");
				var nextBS = new List<BackupSet> ();
				try{
					nextBS = new DAL.BackupSetDAO().GetNextToSchedule(1);
				}
				catch(Exception e){
					Logger.Append("HUBRN", Severity.CRITICAL, "Cannot retrieve next tasks to schedule from DB : "+e.ToString());
				}
				foreach(BackupSet bsn in nextBS){
					CreateTask(bsn, TaskStartupType.Scheduled, null, null);
				}
				Logger.Append("HUBRN", Severity.DEBUG, "Added "+nextBS.Count+" Backup Sets to queue. Total queue size : "+TasksQueue.Count);


				for(int i = TasksQueue.Count - 1; i >= 0; i--){
					Task task = TasksQueue.GetByIndex(i);
					//REACTIVER!!!	//if(TimeSpan.Parse(bs.BackupTimes[0].Begin+":00") <= new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, 0)){
						
					if(task.RunStatus == TaskRunningStatus.Done || task.RunStatus == TaskRunningStatus.Cancelled || task.RunStatus == TaskRunningStatus.Error){
						Logger.Append("HUBRN", Severity.INFO, "Task "+task.Id+", Backupset "+task.BackupSet.ToString()+" ended with status "+task.RunStatus+", removing from queue.");
						task.EndDate = DateTime.Now;
						new DAL.TaskDAO().Complete(task);
						TasksQueue.RemoveAt(i); // TODO : remove this line, instead keep DEBUG one
						if(TaskEvent != null) TaskEvent(task, null);
#if DEBUG
#else
						TasksQueue.RemoveAt(i);
#endif
					}
					else if(task.RunStatus == TaskRunningStatus.PendingStart){
						task.Status = TaskStatus.Ok;
						try{
							StartTask(task);
							task.RunStatus = TaskRunningStatus.Started;
							// wait 0.1 second before starting another task
							Thread.Sleep(100);
						}
						catch(OverQuotaException oqe){
							Logger.Append("HUBRN", Severity.ERROR, "Could not start task #"+task.Id+" on client node #"+task.BackupSet.NodeId
						              +" : "+oqe.Message);
								task.Status = TaskStatus.Error;
								task.RunStatus = TaskRunningStatus.Error;
								task.EndDate = DateTime.Now;
								task.CurrentAction = oqe.Message;
							task.AddLogEntry(new TaskLogEntry(task.Id){Code=830, Message1="", Message2=""});
						}
						catch(UnreachableNodeException){
							Logger.Append("HUBRN", Severity.ERROR, "Could not send task #"+task.Id+" to node #"+task.BackupSet.NodeId+": Node is offline");	
							TimeSpan lateness = DateTime.Now.Subtract(task.StartDate);
							if(lateness >= new TimeSpan(0, maxLateness,0) ){
								Logger.Append("HUBRN", Severity.ERROR, "Could not start task "+task.Id+" for client #"+task.BackupSet.NodeId+", retry time expired. (lateness :"+lateness.Minutes+" minutes)");	
								task.Status = TaskStatus.Error;
								task.RunStatus = TaskRunningStatus.Error;
								task.EndDate = DateTime.Now;
								task.CurrentAction = "Retry time expired";
								task.AddLogEntry(new TaskLogEntry(task.Id){Code=901, Message1="", Message2=""});
							}
							else {
								int remainingRetry = new TimeSpan(0, maxLateness, 0).Subtract(lateness).Minutes;
								Logger.Append("HUBRN", Severity.WARNING, "Could not start task "+task.Id+" for client #"+task.BackupSet.NodeId+", will retry during "+remainingRetry+" mn");	
								task.Status = TaskStatus.Warning;
								//TaskPublisher.Instance().Notify(task);
								task.AddLogEntry(new TaskLogEntry(task.Id){Code=901, Message1=remainingRetry.ToString(), Message2=""});
								task.CurrentAction = "Could not start operation, will retry during "+remainingRetry+" mn";
							}
						}
						
					}
				} // end for
				Utils.SetProcInfo("Hub ("+TasksQueue.Count+" tasks)");
				for(int i=0; i<60;i++)
					if(!cancelToken.IsCancellationRequested) Thread.Sleep(1000); 
			}
			Logger.Append("HUBRN", Severity.INFO, "Scheduler stopped.");
		}
		
		private bool TasksQueueExists(BackupSet bs){
			var list = from Task t in TasksQueue 
				where t.BackupSet.Id == bs.Id 
				&& t.RunStatus != TaskRunningStatus.Cancelling
				&& t.RunStatus != TaskRunningStatus.Cancelled 
				&& t.RunStatus != TaskRunningStatus.Done
				select t;
			if(list.Count() == 0) return false;
			else return true;
		}
		
		private bool StartTask(Task task){
			bool done = false;
			if(task.Operation == TaskOperation.HouseKeeping)
				return StartHouseKeeping(task);

			PeerNode taskTargetNode = GetHandlingNode(task);

			// temp : for debugging udp wakeup
			/*if(taskTargetNode == null){
				NodesMonitor.Instance.WakeUp(new DAL.NodeDAO().Get(task.NodeId));
				Thread.Sleep(5000);
				taskTargetNode = GetHandlingNode(task.Id);
			}*/

			if(taskTargetNode == null){
				throw new UnreachableNodeException("Node #"+taskTargetNode+" is offline or unreachable");
			}
			else if(taskTargetNode.Quota > 0 && taskTargetNode.UsedQuota >= taskTargetNode.Quota){
				throw new OverQuotaException(taskTargetNode.UsedQuota, taskTargetNode.Quota);
			}
			else if(taskTargetNode.Status == NodeStatus.Idle){
				Logger.Append("HUBRN", Severity.INFO, "Node #"+taskTargetNode.Id+" is idle, telling him to wakeup and prepare for task #"+task.Id);
				NodeWakeUpNeeded(taskTargetNode);
				return false;
			}
			Logger.Append("HUBRN", Severity.INFO, "Starting Task "+task.Id+" : type "+task.Type +" ( level "+task.Level/* .BackupSet.ScheduleTimes[0].Level*/+"), backup Set "+task.BackupSet.Id+" for client #"+task.BackupSet.NodeId+" (handled by node #"+task.BackupSet.HandledBy+")");
			//Console.WriteLine("TaskScheduler : handledby = "+task.BackupSet.HandledBy+", proxying info is null : "+(task.BackupSet.ProxyingInfo == null));
			try{
				BackupLevel referenceLevel = BackupLevel.Default;
				if(task.Level == BackupLevel.Differential)
					referenceLevel = BackupLevel.Full;

				P2PBackup.Common.Task referenceTask = new DAL.TaskDAO().GetLastReferenceTask(task.BackupSet.Id, referenceLevel);
				if(referenceTask != null){
					task.StorageBudget = (int)((referenceTask.OriginalSize/task.BackupSet.MaxChunkSize)+2);
					Console.WriteLine(" ____ ref task="+referenceTask.Id+", oSize="+referenceTask.OriginalSize/1024/1024+"MB, maxchunksize="+task.BackupSet.MaxChunkSize/1024/1024+"MB, %%="+referenceTask.OriginalSize/task.BackupSet.MaxChunkSize+", calculated budget="+task.StorageBudget);
					task.ParentTask = referenceTask;
				}

				if(task.Level != BackupLevel.Full){
					
					if(referenceTask == null || referenceTask.Id <= 0){ // no ref backup found, doing full
						Logger.Append("HUBRN", Severity.INFO, "No reference backup found for task "+task.Id+", performing FULL backup.");
						task.Level = BackupLevel.Full;
					}
					else{
						task.ParentTrackingId = referenceTask.Id;
						Logger.Append("HUBRN", Severity.INFO, "Task "+task.Id+" is "+task.Level+"."
						              +" Using reference task "+referenceTask.Id+" ("+referenceTask.StartDate+" - "+referenceTask.EndDate +")");
					}
				}
				taskTargetNode.ManageTask(task, TaskAction.Start);
				task.RunStatus = TaskRunningStatus.Started;
				//n.Status = NodeStatus.Backuping;
				done = true;
			}
			catch (Exception e){
				done = false;
				Logger.Append("HUBRN", Severity.ERROR, "Could not send task "+task.Id+" to node #"+taskTargetNode.Id+" : "+e.ToString()/*+"---Stacktrace:"+e.StackTrace+" inner msg:"+e.InnerException.Message*/);
				//n.Status = NodeStatus.Error;
			}
			return done;
		}

		internal long StartImmediateTask(int bsId, User u, BackupLevel? level){
			if(tokenSource.Token.IsCancellationRequested) return 0;
			BackupSet bs = new DAL.BackupSetDAO().GetById(bsId);
			if(bs != null && bs.Id >=0){
				Task manualTask = CreateTask(bs, TaskStartupType.Manual, u, level);
				StartTask(manualTask);
				return manualTask.Id;
			}
			else
				throw new Exception("Invalid Taskset id");
		}
		
		private PeerNode GetHandlingNode(Task t){
			if(t.BackupSet.HandledBy >0)
				return Hub.NodesList.GetById(t.BackupSet.HandledBy);
			else 
				return Hub.NodesList.GetById(t.NodeId);
		}

		internal void SetTaskRunningStatus(long taskId, TaskRunningStatus status){
			TasksQueue[taskId].RunStatus = status;
		}
		
		internal void SetTaskCurrentActivity(long taskId, int code, string activity){
			TasksQueue[taskId].CurrentAction = activity;
		}
		
		internal void SetTaskStatus(long taskId, TaskStatus status){
			TasksQueue[taskId].Status = status;
		}
		
		internal void CancelTask(long taskId, User u){
			Logger.Append("HUBRN", Severity.INFO, "Received cancel request for task "+taskId);
			Task task = GetTask(taskId);
			if(task.RunStatus <= TaskRunningStatus.PendingStart){
				SetTaskRunningStatus(taskId, TaskRunningStatus.Cancelled); 
				return;
			}
			PeerNode taskTargetNode = GetHandlingNode(task);
			Logger.Append("HUBRN", Severity.INFO, "Asking  to node #"+task.BackupSet.NodeId+" to cancel task "+task.Id);
			if(taskTargetNode != null){
				taskTargetNode.ManageTask(task, TaskAction.Cancel);
				taskTargetNode.Status = NodeStatus.Idle;
			}
			else
				Logger.Append("HUBRN", Severity.WARNING, "Could not send cancel message to node #"+task.BackupSet.NodeId+": node is offline");
			SetTaskRunningStatus(taskId, TaskRunningStatus.Cancelling); 

		}
		
		internal void PauseTask(long taskId, User u){
			Task task = GetTask(taskId);
			PeerNode taskTargetNode = GetHandlingNode(task);
			if(taskTargetNode != null){
				Logger.Append("HUBRN", Severity.INFO, "Asking  to node #"+task.BackupSet.NodeId+" to cancel task "+task.Id);
				taskTargetNode.ManageTask(task, TaskAction.Pause);
				SetTaskRunningStatus(taskId, TaskRunningStatus.Paused); 
			}
		}
		
		internal void UpdateTaskStats(long taskId, long originalSize, long finalSize, long nbItems, int completionpercent){
			TasksQueue[taskId].OriginalSize = originalSize;
			TasksQueue[taskId].FinalSize = finalSize;
			TasksQueue[taskId].TotalItems = (int)nbItems;
			TasksQueue[taskId].Percent = completionpercent;
		}

		// To be called when backup is done/error/cancelled
		internal void UpdateTerminatedTask(Task t){
			TasksQueue[t.Id].OriginalSize = t.OriginalSize;
			TasksQueue[t.Id].FinalSize = t.FinalSize;
			TasksQueue[t.Id].TotalItems = t.TotalItems;
			TasksQueue[t.Id].IndexName = t.IndexName;
			TasksQueue[t.Id].IndexSum = t.IndexSum;
			TasksQueue[t.Id].SyntheticIndexSum = t.SyntheticIndexSum;
			TasksQueue[t.Id].DdbSum = t.DdbSum;
			TasksQueue[t.Id].EndDate = DateTime.Now;
			TasksQueue[t.Id].IndexStorageNodes = t.IndexStorageNodes;
			//TasksQueue[t.Id].Percent = t.Percent;
			if(TasksQueue[t.Id].Status == TaskStatus.Error)
				TasksQueue[t.Id].RunStatus = TaskRunningStatus.Error;
			else if(TasksQueue[t.Id].RunStatus != TaskRunningStatus.Cancelling)
				TasksQueue[t.Id].RunStatus = TaskRunningStatus.Done;
			else
				TasksQueue[t.Id].RunStatus = TaskRunningStatus.Cancelled;
		}


		internal static void AddTaskLogEntry(long taskId, TaskLogEntry tle){
			TasksQueue[taskId].AddLogEntry(tle);
		}
		
		/*internal void RemoveTaskSession(long taskId, int nodeId){
			lock(TasksQueue){
				for(int i = TasksQueue.Count - 1; i >= 0; i--){
					if(TasksQueue[i].Id == taskId)
						foreach(P2PBackup.Common.Node n in TasksQueue[i].StorageNodes)
							if(n.Uid == nodeId)
								TasksQueue[i].RemoveStorageNode(n);
				}
			}	
		}*/
		
		internal Task GetTask(long taskId){
			if(TasksQueue.Contains(taskId))
				return TasksQueue[taskId];
			else
				return null;
		}
		
		/// <summary>
		/// Te be used when a node connects. If it is complete re-connection (with re-login), 
		/// clean previously running tasks.
		/// </summary>
		/// <param name='nodeId'>
		/// Node identifier.
		/// </param>
		internal void Clean(PeerNode node){
			lock(TasksQueue){
				for(int i = TasksQueue.Count - 1; i >= 0; i--){
					if(TasksQueue.GetByIndex(i).UserId == node.Id){
						TasksQueue.GetByIndex(i).AddLogEntry(new TaskLogEntry(TasksQueue[i].Id){Code=808});
						TasksQueue.GetByIndex(i).RunStatus = TaskRunningStatus.Cancelled;

					}
				}
			}	
		}
		
		private bool StartHouseKeeping(Task task){
			var cleanThread = System.Threading.Tasks.Task.Factory.StartNew(() =>{
				List<P2PBackup.Common.Task> expiredBackups = new DAL.TaskDAO().GetExpiredBackups();
				task.OriginalSize = expiredBackups.Sum(o=> o.FinalSize);
				task.TotalItems = expiredBackups.Count;
				Logger.Append("HUBRN", Severity.INFO, "Started cleaning "+expiredBackups.Count+" expired backups");
				//int done = 0;
				try{
				foreach(P2PBackup.Common.Task nodeTask in expiredBackups){
					if(nodeTask == null) continue;
						nodeTask.RunStatus = TaskRunningStatus.Expiring;
					new DAL.TaskDAO().Update(nodeTask);
						PeerNode node = Hub.NodesList.GetById(nodeTask.BackupSet.NodeId);
					if(node != null){
						Logger.Append("HUBRN", Severity.INFO, "Asking node #"+node.Id+" ("+node.Name+") to expire task "+nodeTask.Id);
						//node.SendMessage("EXP "+task.Id+" "+nodeTask.Id+" "+nodeTask.IndexName+" "+nodeTask.IndexSum);
						node.ManageTask(nodeTask, TaskAction.Expire);
					}
					else
						Logger.Append("HUBRN", Severity.WARNING, "Can't expire task "+nodeTask.Id+" of node #"+nodeTask.BackupSet.NodeId+", node is offline");
					//done++;
					
				}
				}
				catch(Exception e){
					Console.WriteLine("StartHouseKeeping() : "+e.Message+" ---- "+e.StackTrace);	
				}
				
			}, System.Threading.Tasks.TaskCreationOptions.LongRunning);
			/*cleanThread.ContinueWith(o=>{
				UpdateTask(task.Id, task.OriginalSize, task.FinalSize, "", "", new List<int>(), 100);
			}, System.Threading.Tasks.TaskContinuationOptions.OnlyOnRanToCompletion);*/
			return true;
		}

		private Task CreateTask(BackupSet bs, TaskStartupType taskType, User u, BackupLevel? overridenLevel){
			if(TasksQueueExists(bs))
				return null;
			Task newBackup = new Task(bs, taskType);
			newBackup.Operation = bs.Operation;
			newBackup.NodeId = bs.NodeId;
			newBackup.CurrentAction = "Initializing";
			newBackup.RunStatus = TaskRunningStatus.PendingStart;
			newBackup.StartDate = DateTime.Now;

			if(overridenLevel.HasValue)
				newBackup.Level = overridenLevel.Value;
			else if(bs.ScheduleTimes.Count == 1 )
				newBackup.Level = bs.ScheduleTimes[0].Level;
			else
				newBackup.Level = BackupLevel.Refresh;

			if(u != null){
				newBackup.UserId = u.Id;
				Logger.Append("HUBRN", Severity.DEBUG, "User "+u.Id+" ("+u.Name+") started new task for backupset "+bs.Id+" with level "+newBackup.Level+" (client #"+bs.NodeId+")");
			}
			// set an encryption key
			newBackup.EncryptionKey = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
			newBackup = new DAL.TaskDAO().Save(newBackup);
			TasksQueue.Add((Task)newBackup);
			Logger.Append("RUN", Severity.TRIVIA, "Created new task for scheduled backupset "+bs.ToString());
			TaskPublisher.Instance().Notify(newBackup);
			return newBackup;
		}


	}
	
}
	 
