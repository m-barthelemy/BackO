
using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Text;
using Node.Utilities;
using P2PBackup.Common;

namespace Node{

	/// <summary>
	/// Class handling communication with Hub, sessions with peer nodes, tasks processing
	/// This is the "main loop" so NO BLOCKING/LONG RUNNING stuff ALLOWED HERE
	/// </summary>
	internal partial class User: P2PBackup.Common.Node{
	
		private Socket hubSocket;
		private Socket storageSocket;
		private static SslStream hubStream;
		private static NetworkStream underlyingHubStream;
		private List<Session> sessions = new List<Session>();
		private static RSACryptoServiceProvider keyPairCrypto = null;
		private static Queue<NodeMessage> MessageQueue;
		//private static List<Backup> backupsList;
		private static Dictionary<long, BackupManager> currentJobs;
		private static bool hasCertificate;
		//private BackupManager bManager;
		private static Queue<string> pendingHubMessages; // messages that couldn't be sent due to hub failure/disconnect
		private X509Certificate2 cert ;
		private bool run = false;


		internal ManualResetEvent CertificateGeneratedEvent{get;set;}
		internal static RSACryptoServiceProvider KeyPair{get{return keyPairCrypto;}}

		internal DateTime LastReceivedAction{get; private set;}
		//public delegate void StorageSessionReceivedHandler (Session storageSession/*, int budget*/);
		//public static event StorageSessionReceivedHandler SessionReady;//BackupManager consume

		public User(){
			//backupsList = new List<Backup>();
			//PluginsDiscoverer pd = PluginsDiscoverer.Instance();
			this.LastReceivedAction = DateTime.Now;
			PluginsDiscoverer.Instance().LogEvent += HandleLogEvent;
			PluginsDiscoverer.Instance().Start ();

			currentJobs = new Dictionary<long, BackupManager>();
			pendingHubMessages = new Queue<string>();
			this.Version = Utilities.PlatForm.Instance().NodeVersion;
			this.OS = Utilities.PlatForm.Instance().OS;
			this.HostName = Environment.MachineName;
			this.Plugins = PluginsDiscoverer.Instance().Plugins.Values.ToList();
			Console.WriteLine("User() : found "+this.Plugins.Count+" plugins");
		}

		public bool Run{
			get {return run;}
		}

		private void HandleLogEvent(Object sender, LogEventArgs e){
			Logger.Append(e.Severity, e.Message);
		}

		static bool CheckHubCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors){
			if (sslPolicyErrors != SslPolicyErrors.None) {
				Logger.Append(Severity.WARNING, "Hub certificate can't be fully trusted : "+sslPolicyErrors.ToString()+@" /!\ IGNORING");
				//return false;
			}
			return true;
		}
		
		private X509Certificate LocalSelectionCallback (object sender, string targetHost, X509CertificateCollection localCertificates,
                                                        X509Certificate remoteCertificate, string[] acceptableIssuers){
            return cert;
        }
		
		public bool  ConnectToHub(bool useCertificate){
				hasCertificate = useCertificate;
				return ConnectToHub(0);
		}

		/// <summary>
		/// Connects the user to the hub and creates Reader and Writer for communication.
		/// </summary>
		/// <param name="ip">Hub IP</param>
		/// <param name="port">Hub Port</param>
		/// 
		private bool ConnectToHub(short tries){
			this.Status = NodeStatus.Idle;
			Console.WriteLine ("ConnectToHub() hascert="+hasCertificate);
			Logger.Append(Severity.INFO, "Connecting to Hub (attempt "+tries+")...");
			tries ++;
			try{
				hubSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

				hubSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, (int)1);
				hubSocket.Connect(ConfigManager.GetValue("Hub.IP"), int.Parse (ConfigManager.GetValue("Hub.Port")));
				underlyingHubStream = new NetworkStream(hubSocket);


				X509CertificateCollection certs = new X509CertificateCollection();
				//HubWrite(new NodeMessage{Context = MessageContext.Authentication, Action = "HELLO"});
				HubNoSslWrite("lilutelilutelilutelilutelilutelilutelilutelilutelilutelilutelilutelilutelilutelilutelilute"); // Why the hell do we need to send initial dummy data????
				if(!hasCertificate){ //we should immediately receive a certificate from hub
					cert= new X509Certificate2(Convert.FromBase64String(Client.DefaultPubKey), "");
					CertificateGeneratedEvent = new ManualResetEvent(false);
				}
				else{ 
					cert = new X509Certificate2(ConfigManager.GetValue("Security.CertificateFile"), ""/*, X509KeyStorageFlags.Exportable*/);
				}
				certs.Add(cert);
				Console.WriteLine ("creating ssl stream...");
				if(hubStream != null)
					hubStream.Close();
				hubStream = new SslStream(underlyingHubStream, false, new RemoteCertificateValidationCallback(CheckHubCertificate), LocalSelectionCallback );
				hubStream.WriteTimeout = 30000; // Timeout after 30s : hub is probably disconnected.
				Console.WriteLine ("creating ssl stream2...");

				//certs.Add(rootCert); // TODO : see if we really need to add root cert
				Console.WriteLine ("creating ssl stream3...");
				hubStream.AuthenticateAsClient("hub"/*ConfigManager.GetValue("Hub.IP")*/, certs, SslProtocols.Default, false);
				Console.WriteLine ("creating ssl stream4...");
				Logger.Append(Severity.TRIVIA, "SSL authentication done");
			}
			catch(SocketException e){
				Logger.Append(Severity.ERROR, "Can't connect to hub ("+tries+" attempts)... will retry indefinitely every 30s (error : "+e.Message+")");
				Thread.Sleep(30000);
				ConnectToHub(tries);
			}
			catch(ObjectDisposedException ode){
				// if the user, for some reason, has been logged out and the user is logging in again
				// create a new socket aund call ConnectToHub again
				Console.WriteLine("WARN: User.ConnectToHub : re-connecting because of : "+ode.Message);
				hubSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				ConnectToHub(tries);
			}
			catch(Exception ex){ // probably SSL error
				Logger.Append(Severity.ERROR, ex.Message+" : "+ex.StackTrace); 
				Logger.Append(Severity.ERROR, ex.InnerException.Message+" : "+ex.InnerException.StackTrace); 
				return false;
			}
			StartListening ();
			return true;
		}

		private static void HubWrite(NodeMessage message){
			HubWrite (message.ToJson<NodeMessage>());
		}

		private  static void HubWrite(string message){
			byte[] byteMsg = Encoding.UTF8.GetBytes(message);
			int msgSize = byteMsg.Length;
			byte[] header = BitConverter.GetBytes(msgSize); // header always has 'int' size (4 bytes)
			lock(hubStream){
				try{
					hubStream.Write(header);
					hubStream.Write(byteMsg);
					hubStream.Flush();
				}
				catch(Exception e){
					Logger.Append(Severity.ERROR, "Can't contact Hub. ("+e.Message+") : "+e.ToString());
					lock(pendingHubMessages){
						pendingHubMessages.Enqueue(message);
					}
				}
			}
			Logger.Append(Severity.TRIVIA, "Sent message to hub : "+message); 
		}

		private  static void HubNoSslWrite(string message){
			byte[] byteMsg = Encoding.UTF8.GetBytes(message);
			lock(underlyingHubStream){
				try{
					underlyingHubStream.Write(byteMsg, 0, byteMsg.Length);
					underlyingHubStream.Flush();
				}
				catch(Exception e){
					Logger.Append(Severity.ERROR, "Can't contact Hub. ("+e.Message+")");
				}
			}
			Logger.Append(Severity.TRIVIA, "Sent message to hub : "+message); 
		}
		
		/// <summary>
		/// When a critical, unrecoverable error occurs, try to inform Hub.
		/// </summary>
		/// <param name='data'>
		/// Data.
		/// </param>
		internal static void SendEmergency(string data){
			HubWrite(new NodeMessage{
				Context = MessageContext.Generic,
				Action = "EMERGENCY",
				Data = data
			});
		}
		
		private void HeaderReceived (IAsyncResult ar){
			if(!run) return;
			StateObject so = (StateObject) ar.AsyncState;
			try{
				int read = so.Stream.EndRead(ar);
				if(read == 0){
					Logger.Append(Severity.INFO, "Hub has disconnected.");
					Disconnect(true, false);
				}
			}
			catch(ArgumentOutOfRangeException){
				Logger.Append(Severity.INFO, "Hub has disconnected.");
				Disconnect(true, false);
			}
			catch(Exception e){
				Logger.Append(Severity.ERROR, "HeaderReceived error : "+e.Message);
				Disconnect(true, false);
			}
			int msg_length = BitConverter.ToInt32(so.Buffer, 0);
			//Logger.Append(Severity.TRIVIA, "Received message header, msg size will be "+msg_length);
			try{
				so.Stream.BeginRead(so.Buffer, 0, msg_length, new AsyncCallback(MessageReceived), so);
			}
			catch(Exception e){
				Logger.Append(Severity.ERROR, "HeaderReceived(2) error : "+e.Message);
				Disconnect(true, false);
			}
		}
		
		private void MessageReceived(IAsyncResult ar){
			this.LastReceivedAction = DateTime.Now;
			if(!run) return;
			try{
				StateObject so = (StateObject) ar.AsyncState;
				so.Stream = hubStream;
				int read = so.Stream.EndRead(ar);
				NodeMessage message = Encoding.UTF8.GetString(so.Buffer, 0, read).FromJson<NodeMessage>();
				Decode(message);
				so.Stream.BeginRead(so.Buffer, 0, 4, new AsyncCallback(HeaderReceived), so);
			}
			catch(Exception ioe){
				Logger.Append(Severity.ERROR, "Error reading data " + ioe.Message+". "+ioe.ToString());
				Disconnect(true, false);
			}
		}	
		
		/// <summary>
		/// Sets the client in listening mode.
		/// </summary>
		private void StartListening(){
			Logger.Append (Severity.DEBUG, "Starting to listen to Hub");
			try{
				run = true;
				StateObject state = new StateObject();
				state.Stream = hubStream;
				byte[] header = new byte[4];
				state.Stream.BeginRead(state.Buffer, 0, header.Length, this.HeaderReceived, state);
				Logger.Append (Severity.TRIVIA, "Startlistening : done");
			}
			catch(Exception ex)	{
				//run = false;
				Logger.Append(Severity.ERROR, ex.Message+" ---- "+ex.StackTrace);
			}
		}

		/// <summary>
		/// Breaks the listening mode and disconnects the client from the hub.
		/// </summary>
		public void Disconnect(bool reconnect, bool goingIdle){
			try{
				try{
					if(goingIdle)
						this.SayGoodBye();
					hubStream.Close();
					underlyingHubStream.Close();
					hubStream = null;
					underlyingHubStream = null;
				}
				catch(Exception e){
					Logger.Append (Severity.DEBUG, "Error while disconnecting from Hub : "+e.Message);
				}

				Thread.Sleep(1000);
				Logger.Append(Severity.INFO, "Closed connection to hub (will reconnect : "+reconnect+")");

				if(!reconnect)
					run = false;
				else
					ConnectToHub(hasCertificate);
					//Environment.Exit(0);

			}
			catch(Exception ex){
				Logger.Append(Severity.ERROR, ex.Message); 
			}
		}

	
		public void AskConfig(){
			HubWrite (new NodeMessage{Context = MessageContext.Generic, Action = "CONFIGURATION"});
		}
		
		private void RunBackup(Backup b){
			this.Status = NodeStatus.Backuping;
			if(currentJobs.Count > 0){ 
				// Refuse to process 2 simultaneous tasks on win XP since it can only handle 1 VSS snap at any given time.
				if(Utilities.PlatForm.Instance().OS == "NT5.1"){
					HubWrite (new NodeMessage{Context = MessageContext.Task, TaskId = b.Id, Data="800"});
					Logger.Append(Severity.WARNING, "A task is already running on Win-XP node, refusing to process other.");
					return;
				}
				foreach(BackupManager runningJob in currentJobs.Values)
					if(runningJob.Backup.BackupSet.Id == b.BackupSetId){
					HubWrite (new NodeMessage{Context = MessageContext.Task, TaskId = runningJob.Backup.Id, Data="800"});
					Logger.Append(Severity.WARNING, "Taskset #"+runningJob.Backup.BackupSetId+" (task id #"+runningJob.Backup.Id+") "+runningJob.Backup.ToString()+" is already running, refusing to process it twice...");
						return;
				}
			}

			//backupsList.Add(b);
			Logger.Append(Severity.INFO, "Starting task +"+b.Id+", operation Backup, type "+b.Level.ToString()+".");
			b.HubNotificationEvent += this.HubSendTaskEvent;

			try{
				System.Threading.Tasks.Task prepareTask = new System.Threading.Tasks.Task(
					()=>{
					b.PrepareAll();
					BackupManager bManager = new BackupManager(b);
					currentJobs.Add(b.Id, bManager);
					bManager.StorageNeeded += AskStorage;
					bManager.BackupDoneEvent += this.BackupDone;
					bManager.Run();
				}, System.Threading.Tasks.TaskCreationOptions.LongRunning);
				
				prepareTask.Start(TaskScheduler.Default);
				
				// handle exceptions...
				prepareTask.ContinueWith(
					o=>{UnexpectedError(prepareTask, b.Id);},
					System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted
				);
			}
			catch(Exception e){
				Logger.Append(Severity.ERROR, "<--> Interrupting task "+b.Id+" after unexpected error : "+e.ToString());
				HubWrite("TSK "+b.Id+" 899 "+e.Message);
				BackupDone(b);
			}
		}

		private void UnexpectedError(System.Threading.Tasks.Task task, long taskId){

			var aggException = task.Exception.Flatten();
         	foreach(var exception in aggException.InnerExceptions){
				Logger.Append(Severity.CRITICAL, "Unexpected error while processing backup task "+taskId+" : "+exception.Message+" ---- Stacktrace : "+exception.StackTrace);
				HubWrite("TSK "+taskId+" 899 "+exception.Message);
			}
			task.Dispose();
			//Backup cur = GetCurrentBackup(taskId);
			//cur.Terminate(false);
			currentJobs[taskId].Backup.Terminate(false);
			BackupDone(currentJobs[taskId].Backup);
			// rethrow to allow continuations to NOT be processed when they are NotOnFaulted
			throw new Exception("Propagating unexpected exception..."); 
		}

		private void BackupDone(Backup b){
			lock(currentJobs){
				currentJobs[b.Id].Backup.HubNotificationEvent -= new Backup.HubNotificationHandler(this.HubSendTaskEvent);
				//backupsList.RemoveAt(i);
				SendDoneTask(b);
				if(currentJobs[b.Id] != null){// can be null if BackupDone is called after an exception occured in backup.Prepare
					currentJobs[b.Id].StorageNeeded -= AskStorage;
					currentJobs[b.Id].BackupDoneEvent -= BackupDone;
				}
				currentJobs[b.Id] = null;
				currentJobs.Remove(b.Id);
			}
			Utils.SetProcInfo("Node (Sleeping)");
		}

		/// <summary>
		/// Request a destination for data chunks.
		/// Hub replies with Session object.
		/// </summary>
		private static void AskStorage(PeerSession s, int parallelism, bool isIndex, bool alternateRequest){
			string action = "ASKSTORAGE";
			if(isIndex)
				action = "INDEXSTORAGE";
			HubWrite(new NodeMessage{
				Context = MessageContext.Task, 
				Action 	= action, 
				TaskId 	= s.TaskId, 
				Data 	= s.ToJson<PeerSession>(),
				Data2 	= string.Format("{0} {1}", parallelism, alternateRequest)
			});
		}


		/// <summary>
		/// Tells the hub that backup is done.
		/// </summary>
		internal static void SendDoneTask(Backup b){	
			Logger.Append(Severity.INFO, "Task #"+b.Id+" done, notifying Hub.");
			HubWrite(new NodeMessage{
				Context = MessageContext.Task,
				TaskId = b.Id,
				Action = "TASKDONE",
				Data = b.ToJson<P2PBackup.Common.Task>()
			});
		}

		private void CancelTask(long taskId){
			lock(currentJobs){
				if(!currentJobs.ContainsKey(taskId)){
					Logger.Append(Severity.ERROR, "Received order to cancel a task (#"+taskId+") that doesn't exist");
					return;
				}
				currentJobs[taskId].Backup.AddHubNotificationEvent(790, "", "");
				Logger.Append(Severity.INFO, "Task "+taskId+" stopping...");
				currentJobs[taskId].Cancel();
				currentJobs[taskId].Backup.Terminate(false);
				currentJobs[taskId].StorageNeeded -= AskStorage;
				currentJobs[taskId].BackupDoneEvent -= BackupDone;
				currentJobs.Remove(taskId);
			}
		}

		// restore a backup in specified destination directory, if we wnat an destination diferent than orignal path
		public void PrepareRecovery(string backupName, string restorePath){
			PrepareRecovery(backupName);
		}
		

		/// <summary>
		/// Does the preparations for a recovery.
		/// </summary>
		/// <param name="backupName">the name of the backup, in this case the same as the name for indexfile</param>
		public void PrepareRecovery(string backupName){
			Directory.CreateDirectory("Temp");
			Console.WriteLine("DEBUG : User.PrepareRecovery : backupName="+backupName);
			//backup = fileHand.GetFromIndexFile(backupName);
			//long total = Int64.Parse(backup.GetBackupSize());
			SendRecoveryToHub();
		}
		
		
		public static void GetLastFullIndexInfo(int taskId){
			HubWrite("LFI "+taskId);
		}
		
		public static void SendRecoverIndexToHub(string indexName){
			HubWrite("RIX " + indexName); // Define RIX (recover index)
		}

		/// <summary>
		/// Prepare to go into Idle state by advising Hub
		/// </summary>
		private void SayGoodBye(){
			HubWrite(new NodeMessage{Context = MessageContext.Generic, Action="IDLE"});
		}

		/// <summary>
		/// Tells the hub that client/user wants to fetch file from other client.
		/// When all files are fetched, they are decrypted and temporary folder is deleted.
		/// </summary>
		private void SendRecoveryToHub(){
			/*bool allFilesFetched = true;
			foreach (BFile bf in backup){
				if(bf.Fetched == false){
						HubWrite("REC " + bf.ClientName);
					allFilesFetched = false;;
					break;
				}
			}
			if (allFilesFetched == true){
				long total = Int64.Parse(backup.GetBackupSize());
				backup.GetFileToDecrypt(keyPairCrypto);
				try{
					Directory.Delete("Temp", true);
				}
				catch (Exception ex){
					MessageBox.Show(ex.Message, "User.SendRecoveryToHub");
				}
				backup = null;
			}*/
		}

		/// <summary>
		/// Send used space Hub, in order to update storagegroup available space
		/// </summary>
		public void SendSessionUpdate(PeerSession s){
			this.StorageSize -= s.RealHandledData;
			HubWrite (new NodeMessage{
				Context = MessageContext.Task,
				TaskId = s.TaskId,
				Action = "SESSION",
				Data = s.ToJson<PeerSession>()
				//Data2 = sessionId+""
			});
		}

		// TODO! Save certificate in 0700 mode (only readable by owner)
		private bool SaveCert(byte[] certificate){
			try{
				//System.Security.AccessControl.FileSecurity certSec = new System.Security.AccessControl.FileSecurity();
				//System.Security.AccessControl.FileSystemAccessRule ar = new System.Security.AccessControl.FileSystemAccessRule(null,System.Security.AccessControl.FileSystemRights.
				//certSec.SetAccessRule();
				using(System.IO.FileStream certStream = new System.IO.FileStream(ConfigManager.GetValue("Security.CertificateFile"), System.IO.FileMode.Create)){
					certStream.Write(certificate, 0, certificate.Length);
				}
				CertificateGeneratedEvent.Set();
			}
			catch(Exception e){
				Logger.Append(Severity.ERROR, "Could not save received certificate to '"+ConfigManager.GetValue("Security.CertificateFile")+"' : "+e.Message);
				return false;
			}
			return true;
		}
		

		/// <summary>
		///Get stats (upon hub request) about a running task.
		/// </summary>
		/// <param name="taskId">
		/// A <see cref="System.Int64"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/> : originalsize finalsize nbitems completionpercent
		/// </returns>
		private string GetBackupStats(long taskId){
			//var rawTask = (from Backup b in backupsList where b.Id == taskId select b);
			lock(currentJobs){
				if(currentJobs.ContainsKey(taskId)){
					Backup backup = currentJobs[taskId].Backup;
					return backup.OriginalSize+" "+backup.FinalSize+" "+backup.TotalItems+" "+backup.CompletionPercent;
				}
				else
					return string.Empty;
			}
		}
		
		/// <summary>
		/// Takes a bytearray and creates a string.
		/// </summary>
		/// <param name="toConvert">byte[] to convert</param>
		/// <returns>string representation of byte[]</returns>
		private string ByteArrayToString(byte [] toConvert){
			StringBuilder sb = new StringBuilder(toConvert.Length);
			for (int i = 0; i < toConvert.Length - 1; i++){
				sb.Append(toConvert[i].ToString("X"));
			}
			return sb.ToString();
		}
		

		private void StartStorageListener(){ 
				storageSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				string listenIp = ConfigManager.GetValue("Storage.ListenIP");
				int listenPort = 0;//this.ListenPort;
				int.TryParse(ConfigManager.GetValue("Storage.ListenPort"), out listenPort);
				if(listenIp == null || listenPort == 0){
					Logger.Append(Severity.INFO, "No storage listener configuration found.");
					return;
				}
				IPAddress listenAddress;
				if (listenIp == "*")
					listenAddress = IPAddress.Any;
				else
					listenAddress = IPAddress.Parse(listenIp);
				IPEndPoint src = new IPEndPoint(listenAddress, listenPort);
				if(!storageSocket.IsBound){
					storageSocket.Bind(src);
					storageSocket.Listen(1);
					Logger.Append(Severity.INFO, "Listening on ip "+listenIp+", port "+listenPort);
				}
				else
					Logger.Append(Severity.WARNING, "I am already listening on port "+listenPort);
		}

		/// <summary>
		/// Starts listening for and accepts a connection from other client.
		/// </summary>
		private void StartListeningForClient( Session peerSession){
				storageSocket.BeginAccept(new AsyncCallback(EndAcceptControlSocket), peerSession);
		}
		
		private void EndAcceptControlSocket(IAsyncResult result){
			try{
					if(!result.AsyncWaitHandle.WaitOne(30000, true)){
					Console.WriteLine ("       / / / / / / /client didn't connect in time");
					throw new Exception("Client  didn't connect in time");
				}
				Session peerSession = (Session)result.AsyncState;
				Socket clientSession = storageSocket.EndAccept(result);

				try{
					clientSession.ReceiveBufferSize = 0; 
					clientSession.NoDelay = true;
				}
				catch{} //doesn't work at least on freebsd

				// Wait for and accept data channel/socket
				Socket dataSession;
				IAsyncResult result2 = storageSocket.BeginAccept(null, storageSocket);
				if(!result2.AsyncWaitHandle.WaitOne(30000, true)){
					throw new Exception("Client data socket didn't connect in time");
				}
				Socket temp2 = (Socket)result2.AsyncState;
				dataSession = temp2.EndAccept(result2);
				Console.WriteLine ("after endaccept");

				try{
					dataSession.ReceiveBufferSize = 512*1024;
					storageSocket.NoDelay = false;
				}
				catch{}
				/*if( ((IPEndPoint)dataSession.RemoteEndPoint).Address.ToString() != clientIp){
					Logger.Append(Severity.WARNING, "Received data connection attempt from bad IP (expected "+clientIp+", got "+((IPEndPoint)clientSession.RemoteEndPoint).Address.ToString()+")");	
					//dataSession.Close();
					//clientSession.Close();
					//return;
				}*/
				peerSession.ClientSocket = clientSession;
				peerSession.DataSocket = dataSession;
				peerSession.StartListening(true);
				Logger.Append(Severity.DEBUG, "Listening for "+clientSession.RemoteEndPoint.ToString());
			}
			catch(ObjectDisposedException){
				// socket closed when user logged out, do nothing
				Logger.Append(Severity.DEBUG, "Storage listener socket was closed.");
			}
			catch(Exception ex){
				Logger.Append(Severity.ERROR, "Error establishing listener for storage node : "+ex.Message+"---"+ex.StackTrace);
			}
		}

		/// <summary>
		/// Removes session from session list
		/// </summary>
		/// <param name="s">session to remove</param>
		/// <param name="sessionType">sessionType to remove</param>
		private void RemoveSession(Session s){
			try{
				Console.WriteLine ("RemoveSession() : active sessions="+sessions.Count); 
				lock(sessions){
					for(int i = sessions.Count-1; i>=0; i--){
						if(sessions[i] == s && sessions[i].Kind == s.Kind){
							
							if(s.Kind == SessionType.Backup){
								sessions[i].SessionRemoved -= new Node.Session.RemoveSessionHandler(this.RemoveSession);
								sessions[i].FileReceivedEvent -= new Node.Session.FileReceivedHandler(this.FileReceived);
								HubSendTaskEvent(s.TaskId, 699, ""+s.Id, "");
								//HubSendTaskEvent(new NodeMessage{Context = MessageContext.Task, TaskId = (long)s.TaskId, Action = MessageAction.TaskMessage, Data = "699"});
							}
							sessions.RemoveAt(i);
						}
					}
				}
				Logger.Append(Severity.INFO, "Removed session #"+s.Id+" from list, "+sessions.Count+" remaining");
				s = null;
				// go back to idle/sleeping state if no more session
				if(sessions.Count == 0)
					this.Status = NodeStatus.Idle;
			}
			catch(Exception _e){
				Logger.Append(Severity.ERROR, "Could not remove session : "+_e.Message+" ---- "+_e.StackTrace);	
			}
		}
		
		/// <summary>
		///	Sends information, errors, statistics to Hub about a running task 
		/// </summary>
		/// <param name="code">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="data">
		/// A <see cref="System.String"/>
		/// </param>
		private void HubSendTaskEvent(long taskId, int code, string data, string additionalMessage){
			HubWrite (new NodeMessage{
					Context = MessageContext.Task, 
					TaskId = taskId, 
					Action = "TASK",
					Data = string.Format("{0} {1} {2}", code, data, additionalMessage) 
			}); 
		}
		

		/// <summary>
		/// If file is recovery-file is received SendRecoveryToHub is called
		/// </summary>
		/// <param name="received">true if file has been received</param>
		/// <param name="bFReceived">the file that has been received</param>
		private void FileReceived(bool received, BChunk chunk){
			/*if (received == true){
				foreach (BChunk bc in backup.Chunks){
					if(bc.Name == chunk.Name){
						bc.Fetched = true;
						UpdateGUIEvent("prbRecovery", bc.Size.ToString());
						SendRecoveryToHub();
					}	
				}
			}*/
		}

		private void StartBackupSession(PeerSession s, bool isIndex){
			Logger.Append(Severity.DEBUG, "Received permission to store "+s.Budget+" chunks to node #"+s.ToNode.Id+":"+s.ToNode.IP+" "+s.ToNode.ListenPort);
			try{
				Session backupSess = GetStorageSession(s);
				if(!isIndex)
					currentJobs[s.TaskId].SessionReceived(backupSess);
				else
					currentJobs[s.TaskId].SendIndex(backupSess);

			}
			catch(Exception e){ // could not get session with storage node, ask alternate destination to hub
				Logger.Append(Severity.WARNING, "Could not connect to storage node #"+s.ToNode.Id+ " ("+e.Message+"---"+e.StackTrace+"), asking ALTernate destination to hub");
				//AskStorage(s.TaskId, s.Id, 1, true);
				AskStorage(s, 1, false, true);
			}
		}

		/*private Session GetCleanSession(int taskId, int nodeId, string nodeIp, int port, string cN, string cK){
			Session cleanSession = null;
			foreach(Session sess in sessionsConnect)
				if(sess.Type == SessionType.Clean && sess.ClientId == nodeId){
					Logger.Append(Severity.DEBUG, "Already have an open cleaning session with storage node "+cN+" ("+nodeIp+":"+port+"), reusing it. ");
					cleanSession = sess;
				}
			if(cleanSession == null){
				cleanSession = new Session(SessionType.Backup, nodeId, nodeIp, port, cN, cK, this.keyPairCrypto);
				//with threadpool, ALT doesnt work (no exceptions propagation between threads)
				ThreadPool.QueueUserWorkItem(cleanSession.ConnectToStorageNode);
				//storageSession.ConnectToStorageNode(null);
				cleanSession.RemoveSessionEvent += new Node.Session.RemoveSessionHandler(this.RemoveSession);
				//storageSession.FileSentEvent += new Node.Session.FileSentHandler(this.ChunkSent); //, bsId, chunkName, nodeId);
				cleanSession.FileReceivedEvent += new Node.Session.FileReceivedHandler(this.FileReceived);
				sessionsConnect.Add(cleanSession);
				cleanSession.AuthenticatedAndReadyEvent.WaitOne();
			}	
			return cleanSession;
		}*/

		private Session GetStorageSession(PeerSession s){
			Session storageSession = null;
			foreach(Session sess in sessions)
				if(sess.Kind == SessionType.Backup && sess.Id == s.Id){
					Logger.Append(Severity.DEBUG, "Reusing open session #"+s.Id+" with storage node #"+s.ToNode.Id+" ("+s.ToNode.IP+":"+s.ToNode.ListenPort+"), reusing it. ");
					storageSession = sess;
					storageSession.RenewBudget(s.Budget);
				}
			if(storageSession == null){
				storageSession = new Session(s, cert);
			
				storageSession.Connect();
				storageSession.SessionRemoved += new Node.Session.RemoveSessionHandler(this.RemoveSession);
				//storageSession.FileSentEvent += new Node.Session.FileSentHandler(this.ChunkSent); //, bsId, chunkName, nodeId);
				storageSession.FileReceivedEvent += new Node.Session.FileReceivedHandler(this.FileReceived);
				if(storageSession.AuthenticatedEvent.WaitOne(new TimeSpan(0, 0, 30)) )
					sessions.Add(storageSession);
				else 
					throw new TimeoutException("Didn't receive handshake confirmation in time for session with peer #"+s.ToNode.Id); 
			}	
			//if(SessionReady != null) SessionReady(storageSession);
			return storageSession;
		}

		
		/// <summary>
		/// To be called when a task is done (good or error).
		/// It close sessions with storage peers.
		/// </summary>
		/// <param name="taskId">
		/// A <see cref="System.Int32"/>
		/// </param>
		internal void TerminateTask(int taskId){
			for(int i = sessions.Count-1; i==0; i--){
				sessions[i].SessionRemoved -= new Node.Session.RemoveSessionHandler(this.RemoveSession);
				sessions[i].FileReceivedEvent -= new Node.Session.FileReceivedHandler(this.FileReceived);
				sessions[i].Disconnect();
				sessions[i] = null;
				sessions.RemoveAt(i);
			}
			if(this.sessions.Count == 0)
				this.Status = NodeStatus.Idle;

		}
		

		/// <summary>
		/// Creates a session for receiving a backup
		/// </summary>
		/// <param name="cIp">client1 IP</param>
		private void StartStoreSession(PeerSession s){
			this.Status = NodeStatus.Storing;
			foreach(Session listeningSess in sessions/*Listen*/){
				if(listeningSess.Kind == SessionType.Store && listeningSess.FromNode.Id == s.FromNode.Id 
				   && listeningSess.Id == s.Id){
					Logger.Append(Severity.DEBUG, "Reusing existing session "+s.Id+" with node #"+s.FromNode.Id+", "+s.FromNode.Name+" ("+s.FromNode.IP+":"+")");
					listeningSess.RenewBudget(s.Budget);
					return;
				}
			}
			Logger.Append(Severity.DEBUG, "Creating new session to receive data from node #"+s.FromNode.Id+", "+s.FromNode.Name+" ("+s.FromNode.IP+":"+")");
			Session client1 = new Session(s, cert);
			client1.SessionRemoved += new Node.Session.RemoveSessionHandler(this.RemoveSession);
			client1.FileReceivedEvent += new Node.Session.FileReceivedHandler(this.FileReceived);
			client1.UpdateStorageEvent += this.SendSessionUpdate;
			client1.RenewBudget(s.Budget);
			sessions.Add(client1);
			try{
				StartListeningForClient(client1);
			}
			catch(Exception e){
				Logger.Append(Severity.WARNING, "Could not accept session with client node #"+s.FromNode.Id+" : "+e.Message);
				RemoveSession(client1);
			}
		}

		private void ReceiveDelete(PeerSession s, string chunkName){
			bool alreadyExistingSession = false;
			foreach(Session listeningSess in sessions){
				if(listeningSess.Kind == SessionType.CleanData && listeningSess.Id == s.Id ){
					alreadyExistingSession = true;
					Logger.Append(Severity.DEBUG, "Reusing existing cleaning session #"+s.Id+" with node "+s.FromNode.Id+" ("+s.FromNode.IP+":"+")");
				}
			}	
			if(!alreadyExistingSession){
				Logger.Append(Severity.DEBUG, "Creating new session to delete data from node "+s.FromNode.Id+" ("+s.FromNode.IP+":"+")");
				Session client1 = new Session(s, cert);
				client1.SessionRemoved += this.RemoveSession;
				client1.UpdateStorageEvent += this.SendSessionUpdate;
				sessions.Add(client1);
				StartListeningForClient(client1);
			}
		}
		/// <summary>
		/// Creates a session for receiving a recovery
		/// </summary>
		/// <param name="cIp">client2 IP</param>
		/// <param name="cN">client2 IP</param>
		/// <param name="cK">client2 public key</param>
		/*private void GetRecovery(string cIp, int port, string cN, string cK){
			foreach(BChunk bf in backup.Chunks){
				if(bf.ClientName ==  cN && bf.Fetched == false){
					Session client2 = new Session(2, this, cIp, port, cN, cK, this.keyPairCrypto,bf);
					client2.RemoveSessionEvent += new Node.Session.RemoveSessionHandler(this.RemoveSession);
					client2.FileSentEvent += new Node.Session.FileSentHandler(this.FileSent);
					client2.FileReceivedEvent += new Node.Session.FileReceivedHandler(this.FileReceived);
					sessionsConnect.Add(client2);
					ConnectToClient(client2);
					break;
				}
			}	
		}*/

		/// <summary>
		/// Creates a session for sending a recovery
		/// </summary>
		/// <param name="cIp">client1 IP</param>
		/// <param name="cN">client1 IP</param>
		/// <param name="cK">client1 public key</param>
		private void SendRecovery(int NodeId, string cIp, string cN, string cK){
			/*Session client1 = new Session(4, nodeId, cIp, cN, cK, this.keyPairCrypto,this.SharePath);
			client1.RemoveSessionEvent += new Node.Session.RemoveSessionHandler(this.RemoveSession);
			client1.FileSentEvent += new Node.Session.FileSentHandler(this.ChunkSent);
			client1.FileReceivedEvent += new Node.Session.FileReceivedHandler(this.FileReceived);
			sessionsListen.Add(client1);
			StartListeningForClient(cN, client1);*/
		}

		private void ApplyConf(){
			Logger.Reload();
		}
		
		private void DeleteBackup(long cleanTaskId, long taskId, string indexName, string indexSum){

			Console.WriteLine("!!TODO!! User.DeleteBackup() : reimplement");
			/*
			P2PBackup.Common.Task cleanTask = new P2PBackup.Common.Task();
			cleanTask.Id = cleanTaskId;
			cleanTask.Operation = TaskOperation.HouseKeeping;
			Node.DataProcessing.Index bi = new Node.DataProcessing.Index(taskId, false);
			try{

				bi.Open();
			}
			catch(Exception e){
				Logger.Append(Severity.ERROR, "Could not start Housekeeping task "+cleanTaskId+" for backup task "+taskId+": "+e.Message);
				HubWrite("EXP "+cleanTaskId+" "+ taskId+" "+810);
				// TODO : remove task from list
				return;
			}
			Console.WriteLine ("DeleteBackup : TODO!! pass bsid to dedup instance");
			Node.DataProcessing.DedupIndex dedupDB = Node.DataProcessing.DedupIndex.Instance(-1);
			//dedupDB.Initialize(-1);//TODO : pass reference Task when deleting backup, to open correct dedup db
			//BChunk chunk;
			int deletable=0;
			int nonDeletable = 0;
			int dereferenceable = 0;*/

			// TODO! port to new sql index implementation
			/*while((chunk = bi.ReadChunk()) != null){
				//Console.WriteLine ("read chunk "+chunk.Name+", storage node="+String.Join(",", chunk.StorageDestinations));
				// 1/2 : find if this chunk is referenced by other backups (in dedup db). If not, can be deleted
				int refs = dedupDB.ChunkReferences(chunk.Name);
				
				if(refs ==0){
					deletable++;
					HubWrite("EXP "+cleanTaskId+" "+ taskId+" DEL "+chunk.Name+" "+ string.Join(",", chunk.StorageDestinations));
				}
				else
					nonDeletable++;
				// 2/2 : for this chunk's content, decrement refcounts (if backup ued dedup)
				foreach(IFSEntry f in chunk.Files){
					if(f.BlockMetadata == null)continue;
					foreach(Node.DataProcessing.IFileBlockMetadata metadata in f.BlockMetadata.BlockMetadata){
						if(metadata is Node.DataProcessing.ClientDedupedBlock){
							//Console.WriteLine(f.SnapshottedFullPath+": "+((Node.DataProcessing.ClientDedupedBlock)metadata).Checksum.ToString());
							dereferenceable++;
							dedupDB.DeReference(((Node.DataProcessing.ClientDedupedBlock)metadata).Checksum);
						}
					}
					
				}*/
				
			}

			/*
			// TODO : 2nd pass deleting chunks not referenced anymore in dedup list
			// TODO : 3rd pass compacting chunks having more than 50% unused/expired data
			dedupDB.Persist();
			dedupDB = null;
			bi.Terminate();
			HubWrite("EXP "+cleanTaskId+" "+ taskId+" "+710);
			Logger.Append (Severity.INFO, "Expired task "+taskId);
			Console.WriteLine ("task "+taskId+" had "+deletable+" deletable chunks, "+nonDeletable+" referenced chunks, "+dereferenceable+" deduped blocks dereferences");
			// TODO : also delete index/dedupDB chunk
			
		}
		*/

	}
}