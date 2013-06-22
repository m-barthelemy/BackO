using System;
using System.Collections.Generic;
using Node.Utilities;
using P2PBackup.Common;
using Node.StorageLayer;
using ServiceStack.Text;

namespace Node{

	internal partial class User:P2PBackup.Common.Node	{
	

		/// <summary>
		/// Routes all messages that arrive from the hub, according to their Context
		/// </summary>
		/// <param name="message">arrived message</param>
		private void Decode(NodeMessage message){
			Logger.Append(Severity.TRIVIA, "Received raw message '"+message.ToJson()+"'");
			switch(message.Context){
				case MessageContext.Generic:
					HandleGenericRequest(message);
					break;
				case MessageContext.Authentication:
				HandleAuthMessage(message);
					break;
				case MessageContext.Task:
					HandleTaskMessage(message);
					break;
			}
		}

		private void HandleGenericRequest(NodeMessage message){
			try{
				//string[] decoded;
				string[] decoded2 = new string[0];
				/*if(message.Data != null)
					decoded = message.Data.Split(new char[]{' '});*/	
				if(message.Data2 != null)
					decoded2 = message.Data2.Split(new char[]{' '});	
				switch(message.Action){

					case "AVAILABLEQUOTA":
						this.StorageUsed = long.Parse(message.Data);
						break;

					case "BROWSE": // hub asks to browse a FS path
						HubWrite(new NodeMessage{
							Context = MessageContext.Generic,
							Synchroneous = true,
							Id = message.Id,
							Action = "BROWSE",
							Data = PathBrowser.GetPathBrowser().Browse(message.Data).ToJson<BrowseNode>()
						});
						break;

					case "BROWSESPECIALOBJECTS":
						HubWrite(new NodeMessage{
							Context = MessageContext.Generic,
							Id = message.Id,
							Action = "BROWSESPECIALOBJECTS",
							Synchroneous = true,
							Data = ObjectsBrowser.BuildObjectsList()//.ToJson<BrowseNode>()
						});
						break;

					case "BROWSEINDEX":
						HubWrite(new NodeMessage{
							Context = MessageContext.Generic,
							TaskId = message.TaskId,
							Id = message.Id,
							Action = "BROWSEINDEX",
							Synchroneous = true,
							Data = IndexBrowser.Browse(message.TaskId, message.Data, long.Parse(decoded2[0]), decoded2[1]).ToJson<BrowseNode>()
						});
						break;

					case "CONFIGURATION": // Hub sends requested configuration upon successful authentication
						this.Configuration = message.Data.FromJson<NodeConfig>();
						Utilities.ConfigManager.BuildConfFromHub(Configuration);
						this.ApplyConf();
						// We check for storage sub-directories, and create them if needed
						if(Utilities.Storage.MakeStorageDirs())
							StartStorageListener();
						break;

					case "STORE"://Receive and store chunks
						StartStoreSession(message.Data.FromJson<PeerSession>());
						HubWrite(new NodeMessage{ // send confirmation, since "STORE" messages a re synchronous
							Context = MessageContext.Generic,
							TaskId = message.TaskId,
							Id = message.Id,
							Action = "STORE",
							Synchroneous = true
						});
						break;

					case "DELETECHUNK": // delete 1 stored chunk (housekeeping operation)
						ReceiveDelete(message.Data.FromJson<PeerSession>(), message.Data2);
						break;
				
					/*case "VMS":
						Virtualization.VirtualMachinesManager vmm = new Node.Virtualization.VirtualMachinesManager();
						
						HubWrite("VMS "+vmm.BuildVmsJson());
						break;*/

					default : 
						throw new ProtocolViolationException(message);
				}
			}
			catch(Exception ex){
				Logger.Append(Severity.ERROR, "message '"+message.ToString()+"' :"+ex.Message+" ---- "+ex.StackTrace);
			}
		}

		private void HandleAuthMessage(NodeMessage message){
			//string[] decoded = message.Data.Split(new char[]{' '});	
			switch(message.Action){

				case "CERTIFICATE": // Hub sends the requested SSL certificate
					Logger.Append(Severity.INFO, "Received SSL certificate, saving...");
					SaveCert(Convert.FromBase64String(message.Data));
					break;

				case "AUTHENTICATION":
					NodeStatus fromHub = NodeStatus.Error;
					fromHub = (NodeStatus)Enum.Parse(typeof(NodeStatus), message.Data);
					switch(fromHub){
						case NodeStatus.Idle: //Verification client-hub ok
							HubWrite (new NodeMessage{
								Context = MessageContext.Authentication, 
								Action = "CLIENTINFO",
								Data = this.ToJson<P2PBackup.Common.Node>()
								//Data = Utilities.PlatForm.Instance().NodeVersion+" "+Utilities.PlatForm.Instance().OS+" "+Environment.MachineName
							});
							AskConfig(); // Now we ask to download our configuration.
							break;

						case NodeStatus.New://Node is new to hub, must be manually approved
							Logger.Append(Severity.INFO, "First connection, waiting for approval from hub (401)");
							break;

						case NodeStatus.Locked:// Pending for approval
							Logger.Append(Severity.INFO, "Received Waiting for Hub approval (502), could not operate right now.");
							break;
						case NodeStatus.Rejected: //Hub refuses this connection, mostly because a node with same certificate is already online
							Logger.Append(Severity.INFO, "Received reject order, exiting...");
							Disconnect(false, false);
							break;
						default:
							throw new ProtocolViolationException(message);
					}
					break;

				default:
					throw new ProtocolViolationException(message);
			}
		}

		private void HandleTaskMessage(NodeMessage message){

			string[] decoded = message.Data.Split(new char[]{' '});	
			switch(message.Action){

				case "STARTTASK": // order from hub to start backup
					Backup b = message.Data.FromJson<Backup>();
					b.Init();
					RunBackup(b);
					break;

				case "ASKSTORAGE":
					StartBackupSession(message.Data.FromJson<PeerSession>(), bool.Parse (message.Data2));
					break;

				/*case "INDEXSTORAGE":
					StartBackupSession(message.Data.FromJson<PeerSession>(), true);
					break;*/

				case "TASKSTATS": // Hub requests statistics about running task. 
						//Send an custom extract of the required task.
						// We chose not to send the complete serialized Task() object, but this may change in the future.
						string stats = GetBackupStats(message.TaskId);
						message.Data = stats;
						HubWrite(message);
						break;

				case "EXPIREBACKUP":
					if(currentJobs.Count >1){
						HubWrite("TSK "+decoded[1]+" 800");
						Logger.Append(Severity.INFO, "Maximum number of jobs reached, refusing to process task "+decoded[1]);
					}
					else
						DeleteBackup(long.Parse(decoded[0]), long.Parse(decoded[1]), decoded[2], decoded[3]);
					break;

				case "CANCELTASK"://Share is smaller than needed space, or no storage space available
					Logger.Append(Severity.INFO, "Received order to cancel task #"+message.TaskId+" Reason : "+message.Data+". Stopping operations...");
					CancelTask(message.TaskId);
					//CleanBackup(message.TaskId);
					break;

				default:
					throw new ProtocolViolationException(message);
			}
		}

	}
}

