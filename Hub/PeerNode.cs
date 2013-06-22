using System;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Configuration;
using System.Collections.Generic;
using System.ServiceModel;
using System.Runtime.Serialization;
using ServiceStack.DataAnnotations;
using ServiceStack.DesignPatterns.Model;
using ServiceStack.Text;
using P2PBackupHub.Utilities;
using P2PBackup.Common;

namespace P2PBackupHub{

	/// <summary>
	/// Holds all information about an online node
	/// </summary>
	[Alias("Node")] // DB table
	// Force WCF to serialize exactly like Common.node 
	[DataContract(Name = "Node", Namespace = " http://schemas.datacontract.org/")]
	public class PeerNode : P2PBackup.Common.Node, IDisposable {

		//[field: NonSerialized] private DBHandle dbhandle;	
		[field: NonSerialized] private SslStream hubStream;
		[field: NonSerialized] private bool verified = false;
		[field: NonSerialized] private ActionType action = ActionType.Default;		// 0 = default	1 = backup	2 = recovery
		

		/// <summary>
		/// Gets or sets the storage space reserved (and not yet consumed) by backup operations.
		/// </summary>
		/// <value>
		/// The reserved space.
		/// </value>
		internal long ReservedSpace{get;set;}

		public delegate void OfflineHandler(PeerNode node);
		[field: NonSerialized]		
		public event OfflineHandler OfflineEvent;

		public delegate void LogHandler (string user, bool received, string message);
		[field: NonSerialized]
		public event LogHandler LogEvent;

		public delegate void SessionHandler(PeerSession s, PeerNode fromNode);
		public event SessionHandler SessionEvent;

		//public delegate void NeedStorageHandler(int nodeId, long taskId, long sessionId, int parallelism, bool isIndex, bool isAlternateRequest);
		public delegate void NeedStorageHandler(int nodeId, PeerSession s, int parallelism, bool isIndex, bool isAlternateRequest);
		public event NeedStorageHandler NeedStorageEvent;

		internal DateTime LastReceivedPing{get; set;}

		private Dictionary<int, NodeMessage> syncMessages = new Dictionary<int, NodeMessage>() ;
		// Waits for and signals sync messages (blocking wait for reply)
		private delegate NodeMessage SyncMessageHandler(NodeMessage m);
		[field: NonSerialized]
		private event SyncMessageHandler SyncMessageReceived;

		private ManualResetEvent syncMsg = new ManualResetEvent(false);

		internal Socket Connection{get; private set;}

		internal void AskStats(long taskId){
			SendMessage(new NodeMessage{Context = MessageContext.Task, TaskId = taskId, Action = "TASKSTATS"});
		}
		
		private string GetCertCN(X509Certificate2 cert){
			return cert.SubjectName.Name;
		}
		
		internal void SetSockets(SslStream hubSSlStream, Socket connection){
			try{
				this.Connection = connection;
				hubStream = hubSSlStream;
				//MessageQueue = new Queue<string>();
				//ThreadPool.QueueUserWorkItem(ProcessMessages);
				if(LogEvent != null)
					LogEvent(this.Name, false, "VER "+this.Version);
			}
			catch(Exception ex){
				Logger.Append("HUBRN", Severity.ERROR, "Error assigning socket and stream to PeerNode : "+ex.Message); 
			}
		}

		private void Decode(NodeMessage message){
			try{
				if (message == null) return ;
				if (LogEvent != null) LogEvent(this.Name, true, message.ToString());

				switch(message.Context){
					case MessageContext.Authentication:
						HandleAuthMessage(message);
						break;

					case MessageContext.Generic: // only for authenticated nodes
						if(!verified) throw new NodeSecurityException();
						HandleGenericMessage(message);
						break;

					case MessageContext.Task: // only for authenticated nodes
						if(!verified) throw new NodeSecurityException();
						HandleTaskMessage(message);
						break;

					default:
						throw new P2PBackup.Common.ProtocolViolationException(message);
				}
				if(message.Synchroneous && SyncMessageReceived != null){
					Console.WriteLine("Decode() : raising event for received sync message");
					SyncMessageReceived(message);
				}
			}
			catch(Exception ex){ // most likely received a badly formatted message, or a message from a non authorized/authenticated node
				Logger.Append("CLIENT", Severity.WARNING, "Node #"+this.Id+" : "+ex.ToString()); 
			}
		}

		private void HandleGenericMessage(NodeMessage message){
			char[] separator = {' '};
			string[] decoded = message.Data.Split(separator);
			try{
			switch(message.Action){
				
				case "BROWSE": case "BROWSEINDX":// response to browse FS path or index  request. Not handled here since it's synchronous 
					Logger.Append("HUBRN", Severity.TRIVIA, "browse node result : got "+message.Data);
					break;
				
				case "BROWSESPECIALOBJECTS": // lists special backupable application objects
					break;

				case "BROWSEDRIVES": // response to mounted filesystems request
					Logger.Append("HUBRN", Severity.TRIVIA, "getdrives  result : got "+message.Data);
					break;
				case "CONFIGURATION": // request for configuration
					if(verified) SendNodeConfiguration();
					break;
				case "EMERGENCY": // unhandled error
					string errorMsg = "";
					for(int i=1; i<decoded.Length; i++) errorMsg += decoded[i]+" ";
					Logger.Append("HUBRN", Severity.ERROR, "Node #"+this.Id+" has crashed due to the followinf unrecoverable error : "+errorMsg);
					break;
				case "STORE": // Storage node confirms it has started the storage session 
					//and is now waiting for client node to connect.
					// do nothing.
					break;
				case "IDLE": // Node informs it is going into 'idle' state. 

					break;
				case "EXP": // expire
					if(verified && decoded.Length >=3){
						Task task = TaskScheduler.Instance().GetTask(long.Parse(decoded[1]));
						// verify if cleaning task exists (for security reasons)
						if(task == null){
							Logger.Append("CLEAN", Severity.ERROR, "Suspect clean request from node "+this.Id);
							return;
						}
						if(decoded[3] == "DEL" && decoded.Length ==6){ // request to delete stored chunk
							//dbhandle.DeleteTask(long.Parse(decoded[2]));
							Logger.Append("CLEAN", Severity.DEBUG, "Task "+decoded[2]+" asks to delete chunk "+decoded[4]+" from node "+decoded[5]);

							int[] storageNodes = Array.ConvertAll(((string)decoded[5]).Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries), s=>int.Parse(s));
							foreach(int nodeId in storageNodes){
								PeerNode storageNode = Hub.NodesList.GetById(nodeId);
								if(storageNode != null){
									//ReceiveDelete(int nodeId, long taskId, string cIp, string cN, string cK, string chunkName)
									storageNode.SendMessage("DEL "+this.Id+" "+decoded[2]+" "+this.IP + " " + this.Name + " " + this.PublicKey+" "+decoded[4]);
								}
							}
						}
						else if(int.Parse(decoded[3]) == 810){ // unable to find or read backup index
							//dbhandle.DeleteTask(long.Parse(decoded[2]));
							Logger.Append("CLEAN", Severity.WARNING, "Deleting damaged task "+decoded[2]);
							TaskScheduler.AddTaskLogEntry(message.TaskId, new TaskLogEntry{TaskId = long.Parse(decoded[0]), Code = int.Parse(decoded[1]), Message1 = decoded[2], Message2 = decoded[3]});
							
						}
						else if(int.Parse(decoded[3]) == 710){ // cleaning done
							new DAL.TaskDAO().UpdateStatus(this.Id, long.Parse(decoded[2]), TaskRunningStatus.Expired);
							//dbhandle.UpdateTaskStatus(long.Parse(decoded[2]), TaskRunningStatus.Expired);
							//dbhandle.DeleteTask(long.Parse(decoded[2]));
							Logger.Append("CLEAN", Severity.INFO, "Task "+decoded[2]+" cleaned.");
						}
						
						else
							throw new P2PBackup.Common.ProtocolViolationException("Incorrect number of parameters for message "+message);
						task.Percent += 1/ task.TotalItems *100;
					}
					
					break;
				case "LFI":
					if(decoded.Length == 2 && verified)	GetLastFullBackupInformation(int.Parse(decoded[1]));
					break;
				
				
				/*case "REC":
					if((decoded.Length == 2) && (verified))
						GetSource(decoded[1]);
					break;*/
				case "RIX":
					if((decoded.Length == 2) && (verified))
						GetIndexSource(decoded[1]);
					break;
				
				case "TSK": //"TSK "+taskId+" "+code+" "+data+" "+additionalMessage
					break;
				case "UNKNOWN": // unknown task
					if(decoded.Length == 2 && verified){
						TaskScheduler.Instance().SetTaskStatus(long.Parse(decoded[1]), TaskStatus.Error);
						TaskScheduler.Instance().SetTaskRunningStatus(long.Parse(decoded[1]), TaskRunningStatus.Cancelled);
						Logger.Append("HUBRN", Severity.ERROR, "Task "+decoded[1]+" is unknown to node #"+this.Id+", node was probably restarted.");
					}
					break;
				
				case "VMS": // send hosted VMs
					/*if(decoded.Length >1 && verified){
						string xml = "";
						for(int i=1; i<decoded.Length; i++) xml += decoded[i]+" ";
						Logger.Append("HUBRN", Severity.TRIVIA, "getvms  result : got "+xml);
						vms = xml;
					}
					break;*/
				case "701":
					Logger.Append("HUBRN", Severity.WARNING, "Backupset "+decoded[1]+", path \""+decoded[2]+"\" does not exist");
					//dbhandle.AddBackupSetError(int.Parse(decoded[1]), type.Trim(), decoded[2]);
					//TaskScheduler.Instance().GetTask(long.Parse(decoded[1])).AddLogEntry(701, decoded[2], null);
					Logger.Append("HUBRN", Severity.ERROR ,"TODO!!! move this message to Task messages, not node messages");
					break;
				case "702":
					Logger.Append("HUBRN", Severity.WARNING, "Backupset "+decoded[1]+", path \""+decoded[2]+"\" acces denied");
					Logger.Append("HUBRN", Severity.ERROR ,"TODO!!! move this message to Task messages, not node messages");
					//dbhandle.AddBackupSetError(int.Parse(decoded[1]), type.Trim(), decoded[2]);
					break;
					
				case "800":
					Logger.Append("HUBRN", Severity.WARNING, "Backupset "+decoded[1]+" cannot be processed by client : too many jobs. |TODO| maintain job on queue");
					break;
				default : 
					throw new P2PBackup.Common.ProtocolViolationException(message);
				}
			}
			catch(Exception ex){
				Logger.Append("CLIENT", Severity.WARNING, "Node #"+this.Id+" : "+ex.ToString()); 
			}
		}

		private void HandleAuthMessage(NodeMessage message){
			char[] separator = {' '};
			string[] decoded = message.Data.Split(separator);
			switch(message.Action){

				/*case  "MAKECERTIFICATE": // new node asks for certificate
					Logger.Append("CLIENT", Severity.DEBUG, "Node asked for certificate");
					CreateAndSendCertificate();
					break;*/

				case "CLIENTINFO": // send version and OS
					
					P2PBackup.Common.Node remoteInfo = message.Data.FromJson<P2PBackup.Common.Node>();

					this.Version = remoteInfo.Version;
					this.OS = remoteInfo.OS;
					this.HostName = remoteInfo.HostName;
					this.Plugins = remoteInfo.Plugins;
					this.LastConnection = DateTime.Now;
					new DAL.NodeDAO().UpdatePartial(this);
					
					break;

				default:
					throw new P2PBackup.Common.ProtocolViolationException(message);
			}
		}

		private void HandleTaskMessage(NodeMessage message){
			//TODO! security : verify that task is really assigned to this node
			string[] decoded = message.Data.Split(new char[]{' '});
			string[] decoded2 = string.IsNullOrEmpty(message.Data2)? null : message.Data2.Split(new char[]{' '});
			switch(message.Action){
				case  "TASK":
					long taskId = message.TaskId;
					int code = int.Parse(decoded[0]);
					string data = String.Empty;
					string msg = String.Empty;
					if (decoded.Length > 3){
						data = decoded[3];
						for(int i=4; i<decoded.Length; i++) msg += decoded[i]+" ";
					}
					if(code <700){
						if(code == 699){ // session ended
							//TaskScheduler.Instance().RemoveTaskSession(taskId, int.Parse(data));
							//if (SessionChanged != null) SessionChanged(false, SessionType.Backup, short.Parse(data), this, null, taskId, 0);
						}
					}
					if(code == 700){ // messages updating CurrentAction, not archived
						TaskScheduler.Instance().SetTaskCurrentActivity(taskId, code, msg);
						return;
					}
					else if(code <800) // INFO class messages
						TaskScheduler.Instance().SetTaskCurrentActivity(taskId, code, data);
					else if(code <900){// WARNING class messages
						TaskScheduler.Instance().SetTaskStatus(taskId, TaskStatus.Warning);
					}
					else{ // ERROR messages
						TaskScheduler.Instance().SetTaskCurrentActivity(taskId, code, data);
						TaskScheduler.Instance().SetTaskStatus(taskId, TaskStatus.Error);
					}
					TaskScheduler.AddTaskLogEntry(taskId, new TaskLogEntry{TaskId = taskId, Code = code, Message1 = data});
					/*break;
						default:
							Logger.Append("DECODE_TASK", Severity.ERROR, "Unknown TaskContext message : "+message.Data);
							break;
					}*/
					break;
				case  "TASKSTATS":// "DBU"
					TaskScheduler.Instance().UpdateTaskStats(message.TaskId, long.Parse(decoded[0]), long.Parse(decoded[1]), long.Parse(decoded[2]), int.Parse(decoded[3]));
					break;

				case "ASKSTORAGE": // client node asks where to store data chunks
					PeerSession s = message.Data.FromJson<PeerSession>();
					if(s.Id >0) // Nodes consumed previous session budget, update storage space
						SessionEvent(s, this);
					//NeedStorageEvent(this.Id, message.TaskId, sessionId,  int.Parse(decoded[1]), false, bool.Parse(decoded[2]));
					if(NeedStorageEvent != null) NeedStorageEvent(this.Id, s, int.Parse(decoded2[0]), false, bool.Parse(decoded2[1]));
					break;

				case "SESSION":
					if(SessionEvent != null) SessionEvent(message.Data.FromJson<PeerSession>(), this);
					break;

				case "INDEXSTORAGE": // client node asks where to store chunks
					PeerSession ps = message.Data.FromJson<PeerSession>();
					if(NeedStorageEvent != null) NeedStorageEvent(this.Id, ps, 1, true, false);// TODO : instead of false parse from message to know if index request is an alternate one
					break;

				case "TASKDONE":
					SendAvailableSpace();
					TaskScheduler.Instance().UpdateTerminatedTask(message.Data.FromJson<Task>());
					break;
				default:
					throw new P2PBackup.Common.ProtocolViolationException(message);
			}
		}

		/// <summary>
		/// Start receiving messages for this client node
		/// </summary>
		internal void StartListening(){
			try{
				StateObject state = new StateObject();
				state.stream = hubStream;
				byte[] header = new byte[4];
				hubStream.BeginRead(state.buffer, 0, header.Length, this.HeaderReceived, state);
			}
			catch(Exception ex){
				Logger.Append("HUBRN", Severity.WARNING, ex.Message); 
			}
		}
		
		private void HeaderReceived(IAsyncResult ar){
			StateObject so = (StateObject) ar.AsyncState;
			hubStream = so.stream;
			try{
				int read = hubStream.EndRead(ar);
				if(read == 0){
					Logger.Append("HUBRN", Severity.DEBUG, "Node #"+this.Id+" ("+this.Name+") has disconnected");
					if(OfflineEvent != null) OfflineEvent(this);
					return;
				}
			}
			catch{
				if(OfflineEvent != null) OfflineEvent(this);
				return;
			}
			int msg_length = BitConverter.ToInt32(so.buffer, 0);
			if(msg_length > so.buffer.Length)
				Logger.Append("rcvd", Severity.ERROR, "Received message is too large, size="+msg_length+", but max buffer size="+so.buffer.Length);
			hubStream.BeginRead(so.buffer, 0, msg_length, new AsyncCallback(MessageReceived), so);
		}
		
		private void MessageReceived(IAsyncResult ar){
			try{
				StateObject so = (StateObject) ar.AsyncState;
				hubStream = so.stream;
				int read = hubStream.EndRead(ar);
				if(read == 0){
					Logger.Append("HUBRN", Severity.DEBUG, "Node #"+this.Id+" ("+this.Name+") has disconnected");
					if(OfflineEvent != null) OfflineEvent(this);
					return;
				}
				NodeMessage message = Encoding.UTF8.GetString(so.buffer, 0, read).FromJson<NodeMessage>();
				Decode(message);
				hubStream.BeginRead(so.buffer, 0, 4, new AsyncCallback(HeaderReceived), so);
			}
			catch(Exception ioe){
				Logger.Append(this.Name,  Severity.ERROR, "Error reading data ("+ioe.Message+"), Disconnecting session. " + ioe.ToString());
				if(OfflineEvent != null) OfflineEvent(this);
			}
		}	
		
		/// <summary>
		/// Tells the client node if is authenticated and approved.
		/// If so (NodeStatus.Idle), we set the node as 'verified' (allowed to make any request)
		/// and client can then request its configuration.
		/// </summary>
		internal void SendAuthStatus(/*NodeStatus authStatus*/){
			if(this.Status == NodeStatus.Idle)
				verified = true; // Authorizes the full range of operations between the client and the hub.
			SendMessage(new NodeMessage{Context = MessageContext.Authentication, Action="AUTHENTICATION", Data=""+this.Status});
		}


		private void GetLastFullBackupInformation(int bsId){
			SendMessage("LFI_"+bsId+"_NOT_IMPLEMENTED_" /*dbhandle.GetLastFullBackupInformation(bsId)*/);
		}

		private void SendMessage(string message){
			byte[] byteMsg = Encoding.UTF8.GetBytes(message);
			int msgSize = byteMsg.Length;
			byte[] header = BitConverter.GetBytes(msgSize); // header always has int size (4 bytes)
			try{
				lock(hubStream){
					hubStream.Write(header);
					hubStream.Write(byteMsg);
					hubStream.Flush();
				}
				Logger.Append("HUBRN", Severity.TRIVIA, "Sent message to node #"+this.Id+" ("+this.Name+") : "+message);
			}
			catch(Exception ex){
				Disconnect();	
				Logger.Append("HUBRN", Severity.ERROR, "Unable to send message to node #"+this.Id+" ("+this.Name+") : "+ex.Message);
			}
		}

		private NodeMessage SendMessage(NodeMessage message){
			//Prevent sending a message unrelated to authentication 
			//if node is not authenticated and approved
			if(message.Context != MessageContext.Authentication && !this.verified){
				Logger.Append("HUBRN", Severity.ERROR, "Node #"+this.Id+" : cannot send message with context '"+message.Context+"' while node is not authenticated");
				throw new NodeSecurityException("Cannot send message with context '"+message.Context+"' while node is not authenticated");
			}
			if(message.Id >0){// existing sync message, we're called from the event handler
				syncMessages[message.Id] = message;
				this.SyncMessageReceived -= SendMessage;
				syncMsg.Set();
				return null;
			}
			else if(message.Synchroneous){
				message.Id = new Random().Next(100000);
				syncMessages.Add(message.Id, null);
				this.SyncMessageReceived += SendMessage;
			}
			byte[] byteMsg = Encoding.UTF8.GetBytes(message.ToJson<NodeMessage>());
			int msgSize = byteMsg.Length;
			byte[] header = BitConverter.GetBytes(msgSize); // header always has int size (4 bytes)
			try{
				lock(hubStream){
					hubStream.Write(header);
					hubStream.Write(byteMsg);
					hubStream.Flush();
				}
				Logger.Append("HUBRN", Severity.TRIVIA, "Sent message to node #"+this.Id+" ("+this.Name+") : "+message.ToJson<NodeMessage>());
				if(message.Synchroneous){
					if(!syncMsg.WaitOne(30000))
						throw new TimeoutException("Timed out waiting for synchronous message response from Node #"+this.Id+". Message was : "+message.ToString());
					syncMsg.Reset();
					if(syncMessages.ContainsKey(message.Id)){
						NodeMessage m = syncMessages[message.Id];
						lock(syncMessages){
							syncMessages.Remove(message.Id);
						}
						return m;
					}
				}
			}
			catch(Exception ex){
				Disconnect();	
				Logger.Append("HUBRN", Severity.ERROR, "Unable to send message to node #"+this.Id+" ("+this.Name+") : "+ex.Message);
			}
			return null;
		}

		internal BrowseNode Browse(string baseDir){
			var b = SendMessage(new NodeMessage{
				Context = MessageContext.Generic,
				Action = "BROWSE",
				Data = baseDir,
				Synchroneous = true
			});
			return b.Data.FromJson<BrowseNode>();
		}
		
		internal string GetSpecialObjects(){
			var spo = SendMessage(new NodeMessage{
				Context = MessageContext.Generic,
				Action = "BROWSESPECIALOBJECTS",
				Synchroneous = true
			});
			return spo.Data;
		}
		
		internal string GetDrives(){
			var d = SendMessage(new NodeMessage{
				Context = MessageContext.Generic,
				Action = "BROWSEDRIVES",
				Synchroneous = true
			});
			return d.Data;
		}

		internal BrowseNode BrowseIndex(long taskId, string fs, long parentId, string filter){
			var b = SendMessage(new NodeMessage{
				Context = MessageContext.Generic,
				Action = "BROWSEINDEX",
				TaskId = taskId,
				Data = fs,
				Data2 = parentId+" "+filter,
				Synchroneous = true
			});
			return b.Data.FromJson<BrowseNode>();
		}

		internal void GetVMs(){
			SendMessage("VMS");	
		}

		internal void ManageTask(Task task, TaskAction action){
			var protocolAction = "";
			switch(action){
				case TaskAction.Cancel:
					protocolAction = "CANCELTASK";
					break;
				case TaskAction.Pause:
					protocolAction = "PAUSETASK";
					break;
				case TaskAction.Start:
					protocolAction = "STARTTASK";
					break;
				case TaskAction.Restart:
					protocolAction = "RESUMETASK";
					break;
				case TaskAction.Expire:
					protocolAction = "EXPIRETASK";
					break;
				default:
					break;
			}
			var taskM = new NodeMessage{
				Context = MessageContext.Task,
				Action = protocolAction,
				TaskId = task.Id,
				Data = task.ToJson<Task>()
			};
			SendMessage(taskM);
		}

		/// <summary>
		/// Closes the socket and streams for this user
		/// if the connected user has disconnected
		/// Sets the bool attributes to false
		/// </summary>
		public void Disconnect(){
			verified = false;
			if(this.Connection == null)
				return;
			try{
				this.hubStream.Close();
				this.Connection.Shutdown(SocketShutdown.Both);
				this.Connection.Close();
				this.hubStream.Dispose();
				this.Connection.Dispose();
			}
			catch(Exception e){
				Utilities.Logger.Append("", Severity.INFO, "user :"+this.Name+" : The socket was already shutdown by sudden client disconnection ("+e.Message+")");	
			}
		}


		/// <summary>
		/// Sends the remaining available quota to the node
		/// </summary>
		private void SendAvailableSpace(){
			action = ActionType.Default;
			SendMessage(new NodeMessage{Action = "AVAILABLEQUOTA",Data = this.StorageUsed+""});
		}
		
		internal void SendNodeConfiguration(){
			SendMessage(new NodeMessage{Action = "CONFIGURATION", Data = this.Configuration.ToJson()});
		}

		internal void SendCertificate(byte[] cert){
			SendMessage(new NodeMessage{
				Context = MessageContext.Authentication, 
				Action="CERTIFICATE", 
				Data = Convert.ToBase64String(cert)
			});
		}

		private void GetIndexSource(string indexName){
			action = ActionType.Restore;
			Console.WriteLine("ClientNode.GetIndexSource() : not implemented");
			/*List<int> sourceNodeId = dbhandle.GetIndexSources(indexName);
			foreach(int sid in sourceNodeId){
				if(Hub.NodesList.IsOnline(sid)){
					ClientNode sourceNode = Hub.NodesList.GetNode(sid);
					SendMessage("LET" + " " + this.IP + " " + this.NodeName + " " + this.PubKey);
					if(LoggEvent != null)	LoggEvent(sourceNode.NodeName, false, "LET" + " " + this.IP + " " + this.NodeName + " " + this.PubKey);
				}
			}*/
		}
		
		/// <summary>
		/// Gets the user storing the file at recovery time
		/// </summary>
		/// <param name="nodeName">username for storing user</param>
		/*private void GetSource(string nodeName){
			PeerNode source = null;
			action = ActionType.Restore;

			if (GetClientEvent != null)
				source = GetClientEvent(nodeName);
			if (source != null){
				SendMessage("LET" + " " + this.IP + " " + this.Name + " " + this.PubKey);
				if(LogEvent != null) LogEvent(source.Name, false, "LET" + " " + this.IP + " " + this.Name + " " + this.PubKey);
			}
			else{
				SendMessage("405");
				if(LogEvent != null) LogEvent(nodeName, false, "405");
			}
		}*/


		internal void SendSession(PeerSession session, SessionType type, bool isIndexSession){
			string action = string.Empty;
			MessageContext ctx = MessageContext.Generic;
			session.Kind = type;
			bool synchronous = false;
			if(type == SessionType.Backup){
				ctx = MessageContext.Task;
				action = "ASKSTORAGE";
			}
			else if(type == SessionType.Store){
			        action = "STORE";
				// wait for confirmation that the receive/store session has been started on storage node.
				// We avoid client node to connect to a storage node that is not yet ready.
				synchronous = true;
			}
			SendMessage(new NodeMessage{
				Context = ctx,
				TaskId = session.TaskId,
				Action = action,
				Data = session.ToJson<PeerSession>(),
				Data2 = isIndexSession.ToString(),
				Synchroneous = synchronous
			});
		}

		/// <summary>
		/// Sends information about the storing client to the requesting client
		/// at recovery time
		/// </summary>
		/// <param name="source">the user storing the file</param>
		private void SendSource(PeerNode source){
			SendMessage("SRC " + source.IP + " " +source.ListenPort + " " + source.Name + " " + source.PublicKey);
			if(LogEvent != null)	LogEvent(this.Name, false, "SRC " + source.IP + " " +source.ListenPort + " " + source.Name + " " + source.PublicKey);
		}

		public void Dispose(){
			OfflineEvent = null;
			LogEvent = null;
			SessionEvent = null;
			NeedStorageEvent = null;
			SyncMessageReceived = null ;
			syncMsg.Dispose();
			hubStream.Dispose();
			Connection.Dispose();
		}
	}
}
