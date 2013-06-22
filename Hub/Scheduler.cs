/*using System;
using System.Linq;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using P2PBackup.Common;
using P2PBackupHub.Utilities;
using System.Configuration;

namespace P2PBackupHub{
	
	// The scheduler runs an infinite thread that launchs backupsSets when it's time to.
	// It maintains a list of running tasks (backups).
	// every minute we poll the database to retrieve the next backups to start
	public class Scheduler{
		
		private static Scheduler _instance;
		private static bool run;
		private static int maxLateness;
		private static bool isFirstStart;
		private  static List<BackupSet> BackupsQueue;
		
		private Scheduler(){
			Thread workerThread = new Thread(Schedule);
			run = true;
			isFirstStart = true;
			BackupsQueue = new List<BackupSet>();
			maxLateness = int.Parse(ConfigurationManager.AppSettings["BackupSet.RetryTime"]);
        		workerThread.Start();
			Logger.Append("HUBRN", "Scheduler.Schedule", Severity.INFO, "Started scheduler");
		}
		
		public static void Start(){
			if(_instance == null)
				_instance = new Scheduler();
		}
		
		public static void Stop(){
			run = false;
		}
		
		internal static Scheduler Instance(){
			return _instance;
		}
		
		private void Schedule(){
			while(run){
				if(isFirstStart){
					Thread.Sleep(20000);
					isFirstStart = false;
				}
				//if(BackupsQueue.Count >= 0){
					Logger.Append("HUBRN","Scheduler.Schedule",Severity.DEBUG,"Polling database for next BackupSets to run.");
					DBHandle dbHandle = new DBHandle();
					ArrayList nextBS = dbHandle.GetBackupSets(DateTime.Now, DateTime.Now.AddMinutes(1));
					int added=0;
					foreach(BackupSet bsn in nextBS){
						if(!BackupsQueue.Contains(bsn)){ //iequatable implementation
							BackupsQueue.Add(bsn);
							added++;
						}
					}
					Logger.Append("HUBRN", "Scheduler.Schedule", Severity.DEBUG, "Added "+nextBS.Count+" Backup Sets to queue. Total queue size : "+BackupsQueue.Count);
					
				//}
				for(int i = BackupsQueue.Count - 1; i >= 0; i--){
					BackupSet bs = BackupsQueue[i];
					
					//Console.WriteLine("schedule : parsed date begin="+bs.BackupTimes[0].Begin+":00");
				//REACTIVER!!!	//if(TimeSpan.Parse(bs.BackupTimes[0].Begin+":00") <= new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, 0)){
						Logger.Append("HUBRN", "Scheduler.Schedule", Severity.INFO, "Starting backup Set "+bs.Id+" for client #"+bs.ClientId);
						if(bs.Status == BackupStatus.Done){
							BackupsQueue.RemoveAt(i);
							Logger.Append("HUBRN", "Scheduler.Schedule", Severity.DEBUG, "Backupset "+bs.Id+" done, removing from queue.");
						}	
						if (bs.Status != BackupStatus.Backuping){
							//BackupsQueue.RemoveAt(i);
							dbHandle.AddBackupTracking(bs.Id, bs.BackupTimes[0].Type);
							if(SendBackupStart(bs))
								bs.Status = BackupStatus.Backuping;
							else{
								TimeSpan lateness = new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, 0).Subtract(TimeSpan.Parse(bs.BackupTimes[0].Begin+":00"));
								if(lateness > new TimeSpan(0, maxLateness,0) ){
									Logger.Append("HUBRN","Scheduler",Severity.ERROR,"Could not start BackupSet "+bs.Id+" for client #"+bs.ClientId+", retry time expired. (lateness :"+lateness.Minutes+" minutes)");	
									BackupsQueue.RemoveAt(i);
								}
								else 
									Logger.Append("HUBRN", "Scheduler", Severity.WARNING, "Could not start BackupSet for client "+bs.ClientId+", will retry during "+new TimeSpan(0, maxLateness, 0).Subtract(lateness).Minutes+" mn");	
							}
						}
						
					//}
					
				}
				Thread.Sleep(60000); 
			}
		}
		
		private bool SendBackupStart(BackupSet bs){
			bool done = false;
			//<TODO> : replace foreach by for to avoid "List has Changed" crash when users disconnects before we send him the BCK signal
			foreach (Node n in (ArrayList)Hub.NodesList){
				//Console.WriteLine("node uid="+n.Uid+", bs.clientid="+bs.ClientId);
				if(n.Uid == bs.ClientId){
					try{
						n.SendMessage("BKS "+bs.Id+" "+ConfigurationManager.AppSettings["Backup.MaxChunkSize"]+" "+ConfigurationManager.AppSettings["Backup.MaxChunkSize"]+" "+bs.DumpToXml());
						//n.Status = NodeStatus.Backuping;
						done = true;
					}
					catch (Exception e){
						done = false;
						Logger.Append("HUBRN","Scheduler.SendBackupStart",Severity.ERROR,""+e.Message);
						//n.Status = NodeStatus.Error;
					}
				}
			}
			return done;
		}
		
		internal static void SetBackupStatus(int bsId, BackupStatus status){
			for(int i = BackupsQueue.Count - 1; i >= 0; i--){
				if(BackupsQueue[i].Id == bsId)
					BackupsQueue[i].Status = status;
			}
		}
		
		internal  BackupSet GetRunningBs(int bsId){
			var bs = from runningBs in BackupsQueue 
					where runningBs.Id == bsId 
					select runningBs;
			//Console.WriteLine("getrunningbs : got "+bs.Count());
			return (bs.ToList())[0] as BackupSet;
		}
	}
}

 */