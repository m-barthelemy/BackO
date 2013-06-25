using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Sockets;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;
using System.Threading;
using System.Configuration;
using P2PBackupHub.Utilities;
using P2PBackup.Common;

namespace P2PBackupHub{

	/// <summary>
	/// Handles all connections from client nodes
	/// </summary>
	public class Hub {
		
		internal static NodesList NodesList;
		private static SessionsList sessionsList;
		private static Socket connection;
		private static bool running;	

		private static X509Certificate2 cert;
		private X509Certificate2 rootCert;

		/*internal static NodesList NodesList{
			get{return nodeList;}
		}*/

		internal static List<PeerSession> SessionsList{
			get{return sessionsList.ToList();}
		}

		internal static X509Certificate2 Certificate{
			get{
				if(cert == null){
					if(ConfigurationManager.AppSettings["Security.CertificateFile"] == null || !System.IO.File.Exists(ConfigurationManager.AppSettings["Security.CertificateFile"])){
						Logger.Append("HUBRN", Severity.CRITICAL, "Could not load hub SSL certificate (configuration told it would be '"+ConfigurationManager.AppSettings["Security.CertificateFile"]+"')");
						throw new Exception("FATAL : unable to load HUB certificate  ");
					}
					cert = new X509Certificate2(ConfigurationManager.AppSettings["Security.CertificateFile"], "");
					Utils.DisplayCertificateInformation(cert);
				}
				return cert;
			}
		}
		
		private X509Certificate RootCA{
			get{
				if(rootCert == null){
					if(ConfigurationManager.AppSettings["Security.CACertificate"] == null || !System.IO.File.Exists(ConfigurationManager.AppSettings["Security.CACertificate"])){
						Logger.Append("HUBRN", Severity.CRITICAL, "Could not load hub SSL CA certificate (configuration told it would be '"+ConfigurationManager.AppSettings["Security.CACertificate"]+"')");
						throw new Exception("FATAL : unable to load HUB CA certificate");
					}
					rootCert = new X509Certificate2(ConfigurationManager.AppSettings["Security.CACertificate"], "");
				}
				return rootCert;
			}
		}

		/*internal static  RSA MotherKey{
			get{
				if(motherKey == null){
					if(ConfigurationManager.AppSettings["Security.MotherCertificateFile"] == null || !System.IO.File.Exists(ConfigurationManager.AppSettings["Security.MotherCertificateFile"])){
						Logger.Append("HUBRN", Severity.CRITICAL, "Could not load hub RSA mother key (configuration told it would be '"+ConfigurationManager.AppSettings["Security.MotherCertificateFile"]+"')");
						throw new Exception("FATAL : unable to load HUB mother key");
					}
					System.IO.StreamReader sr = new System.IO.StreamReader(ConfigurationManager.AppSettings["Security.MotherCertificateFile"]);
					
					motherKey.FromXmlString(sr.ReadToEnd());
				}
				return motherKey;
			}
		}*/
		
		public Hub(){
			NodesList = new NodesList();
			sessionsList = new SessionsList();
		}

		/// <summary>
		/// Accepts a connection from a client, creates a User with this connection
		/// Calls method to place the user in listening mode
		/// Adds the user to the UserList and Online Users's listbox after verification
		/// </summary>
 		private void AcceptClient(IAsyncResult iar){
			Socket hub = (Socket)iar.AsyncState;
			try{
				Socket client = hub.EndAccept(iar);
				Logger.Append("HUBRN", Severity.DEBUG, "Connection attempt from "+client.RemoteEndPoint.ToString());

				StateObject so = new StateObject();
				so.buffer = new byte[1024];
				so.workSocket = client;
				SocketError se = new SocketError();
				client.BeginReceive(so.buffer, 0, so.buffer.Length, SocketFlags.None, out se, ClientSSLAccept, so);
			}
			catch(ObjectDisposedException)	{
				// socket closed when user logged out, do nothing
			}
			catch(Exception ex){
				Utilities.Logger.Append("HUBRN", Severity.ERROR, "Could not accept logging-in node : "+ ex.Message);
				//nodeList.RemoveUser(u);
			}
			finally{

				try{
					if(running) // don't accept new connections if shutdown requested
						hub.BeginAccept(new AsyncCallback(AcceptClient), hub);// Loop...	
				}
				catch(Exception e){
					Logger.Append("HUBRN", Severity.WARNING, "Error accepting new node connection : "+e.Message);	
				}
			}
		}


		private void ClientSSLAccept(IAsyncResult ar){
			StateObject so = (StateObject)ar.AsyncState;
			Socket client = so.workSocket;
			System.Threading.Tasks.Task sslT = System.Threading.Tasks.Task.Factory.StartNew(() =>{
				NetworkStream clientStream = new NetworkStream(client);
				SslStream sslStream = new SslStream(clientStream, false, ClientCertAuthenticate);
				Logger.Append("HUBRN", Severity.TRIVIA, "SSL Connection attempt from "+client.RemoteEndPoint.ToString()+" : beginning SSL authentication");
				try{
					sslStream.AuthenticateAsServer(Certificate, true, SslProtocols.Default, false);
				}
				catch(AuthenticationException){
					Logger.Append("HUBRN", Severity.INFO, "Client node "+client.RemoteEndPoint.ToString()+" tried to connect without certificate");
				}
				Logger.Append("HUBRN", Severity.TRIVIA, "Connection attempt from "+client.RemoteEndPoint.ToString()+" : SSL authentication done.");
				Utils.DisplayCertificateInformation(sslStream.RemoteCertificate);


				PeerNode pn = AuthenticateNode(sslStream, client);
				if(pn == null) return;

				PutNodeOnline(pn);
			});

			sslT.ContinueWith(t =>{
				var aggException = t.Exception.Flatten();
				foreach(var e in aggException.InnerExceptions)
					Logger.Append("HUBRN", Severity.ERROR, "Unexpected error ("+e.Message+") : "+e.ToString());
			}, System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted);
		}

		private PeerNode AuthenticateNode(SslStream clientSslStream, Socket clientSocket){
			string nodeIP = clientSocket.RemoteEndPoint.ToString().Split(':')[0];

			// if cert is empty or come with the default/harcoded hash, this a new node.
			// Generate and send him a certificate
			if(clientSslStream.RemoteCertificate == null 
			   || clientSslStream.RemoteCertificate.GetCertHashString() == "3EE15BE077586D9CB9AEC105AE8AB0613ED6C34B"){
				//TODO : make certmanager directly return an X509Certificate2
				//TODO : store the whole cert into a 'Password' structure (need private key if client 
				// node is lost (or its certificate is lost), or to do cross-restores 
				Mono.Security.X509.PKCS12 newCert = GenerateNewClientCertificate(nodeIP);
				// new node, unknown by hub. let's add it in "pending for approval" status
				var x509cert2 = new System.Security.Cryptography.X509Certificates.X509Certificate2();
				x509cert2.Import(newCert.Certificates[0].RawData, "", 
				                 System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.PersistKeySet| System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.Exportable );
				var u = CreateNewNode(nodeIP, new NodeCertificate(x509cert2));
				u.SetSockets(clientSslStream, clientSocket);
				u.SendCertificate(newCert.GetBytes());
				u.Disconnect();
				return null;
			}

			X509Certificate2 remoteCert = new X509Certificate2(clientSslStream.RemoteCertificate);
			PeerNode node = new DAL.NodeDAO().NodeApproved(remoteCert.GetSerialNumber());
			node.IP = nodeIP;
			if(node != null){
				Logger.Append("HUBRN", Severity.TRIVIA, "Newly connected node : Id="+node.Id+", NodeName="+node.Name+",IP="+node.IP+", status="+node.Status);
				if (!node.Locked){
					node.Status = NodeStatus.Idle;
				}
				else{
					node.Status = NodeStatus.Locked; // pending for manual approval
					Logger.Append("HUBRN", Severity.NOTICE, "Newly connected node #"+node.Id+" is locked.");
				}
			}
			node.SetSockets(clientSslStream, clientSocket);
			node.SendAuthStatus();
			return node;
		}

		private PeerNode CreateNewNode(string ip, NodeCertificate cert){
			var node = new PeerNode();
			node.Name = Dns.GetHostEntry(ip).HostName;
			node.IP = ip;
			node.Locked = true;
			node.Status = NodeStatus.New;
			node = new DAL.NodeDAO().Save(node);
			cert.NodeId = node.Id;
			cert = new DAL.CertificateDAO().Save(cert);
			Logger.Append("HUBRN", Severity.INFO, "Created new node #"+node.Id+" with cert #"+cert.Id+" for client "+ip);
			return node;
		}

		private void PutNodeOnline(PeerNode pn){
			if(NodesList.Contains(pn.Id) && NodesList.GetById(pn.Id).Status != NodeStatus.Idle){
				Logger.Append("HUBRN", Severity.WARNING, "Node #"+pn.Id+" tried to connect but appears to already be online, rejecting.");
				pn.Status = NodeStatus.Rejected;
				pn.SendAuthStatus();
				pn.Disconnect();
				pn.Dispose();
			}
			else{
				if(NodesList.Contains(pn.Id))
					NodesList.Remove(pn.Id);
				NodesList.Add(pn);

				if(pn.Status == NodeStatus.Idle){
					pn.LogEvent += new P2PBackupHub.PeerNode.LogHandler(LogEvent);
					pn.NeedStorageEvent += ChooseStorage;
					pn.SessionEvent += HandleSessionEvent;
				}
				pn.StartListening();
				pn.OfflineEvent += ClearNode;
				pn.Status = NodeStatus.Online;
				Logger.Append("HUBRN", Severity.INFO, "Node #"+pn.Id+" is online (total : "+NodesList.Count+" online nodes)");
			}
		}

		private /*byte[]*/Mono.Security.X509.PKCS12 GenerateNewClientCertificate(string clientIP){
			Logger.Append("CLIENT", Severity.INFO, "Generating PKCS certificate for new node "+clientIP);
			CertificateManager cm = new CertificateManager();
			string clientHostName = System.Net.Dns.GetHostEntry(clientIP).HostName;
			Logger.Append("CLIENT", Severity.DEBUG, "Resolved new client to host '"+clientHostName+"'");
			return cm.GenerateCertificate(false, false, clientHostName, null);
		}

		void HandleSessionEvent(PeerSession s, PeerNode fromNode){
			// check that the received Session doesn't claim to own something it doesn"t
			Task curTask = TaskScheduler.Instance().GetTask(s.TaskId);
			PeerSession curSess = sessionsList.GetById(s.Id);
			if(curTask == null || curSess == null)
				throw new NodeSecurityException("Node #"+fromNode.Id+" claims it handles a task (#"+s.TaskId+") or a session (#"+s.Id+") which doesn't exist");
			if(   (s.Kind == SessionType.Backup && (curTask.NodeId != fromNode.Id /*|| curTask.BackupSet.HandledBy != fromNode.Id */) )
			   || (s.Kind == SessionType.Store && curSess.ToNode.Id != fromNode.Id)
			  )
				throw new NodeSecurityException("Node #"+fromNode.Id+" claims it handles a task (#"+s.TaskId+") it doesn't own !");

			Logger.Append("HUBRN", Severity.TRIVIA, "Task #"+s.TaskId+" : session #"+s.Id +" between node #"+s.FromNode.Id+" and node #"+s.ToNode.Id+" ended.");

			curSess.SetUsage(s);
			if(curSess.IsStorageUsageConfirmed()){
				Logger.Append("HUBRN", Severity.TRIVIA, "Task #"+s.TaskId+" : session #"+s.Id +"  : Storage space usage has been double-confirmed");
				PeerNode n = NodesList[curSess.ToNode.Id];
				lock(n){// Release reserved space and set really consumed space
					n.ReservedSpace -= curSess.Budget*curTask.BackupSet.MaxChunkSize;
					n.StorageUsed += curSess.RealHandledData;
				}
			}
		}

	 	private bool ClientCertAuthenticate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors){
				
			// case where client cert is null : node has lost its cert or, 
			// more probably, this a new client node
			if(certificate == null){
				Logger.Append("HUBRN", Severity.TRIVIA, "Client sent empty/null certificate!");
				return true; // accept anyway, as this could be a new node requesting a certificate.
			}
			try{
				var certificates = new X509Certificate2Collection();
				certificates.Add(new X509Certificate2(this.RootCA));
				var ckChain = new X509Chain();
				ckChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
				ckChain.ChainPolicy.ExtraStore.AddRange(certificates);
				ckChain.ChainPolicy.VerificationFlags = (X509VerificationFlags.NoFlag); // .AllowUnknownCertificateAuthority | X509VerificationFlags.IgnoreWrongUsage);
				bool isValidCertificate = ckChain.Build(new X509Certificate2(certificate));

				if(isValidCertificate) return true;

				bool requireMatchingCA = true;
				bool.TryParse(ConfigurationManager.AppSettings["Security.RequireCAMatchingHubCA"], out requireMatchingCA);
				bool requireNotExpiredCert = true;
				bool.TryParse(ConfigurationManager.AppSettings["Security.RequireCertNotExpired"], out requireNotExpiredCert);
				string node = certificate.Subject;
				string advancedErrors = "";
				foreach(X509ChainStatus chainStatus in ckChain.ChainStatus){
					if(chainStatus.Status == X509ChainStatusFlags.CtlNotTimeValid){
						bool retStatus = true;
						Severity sev = Severity.WARNING;
						if(requireNotExpiredCert){
							retStatus = false;
							sev = Severity.ERROR;
						}
						Logger.Append("HUBRN", sev, "Certificate for node '"+node+"' has expired (expiration date: '"+certificate.GetExpirationDateString()+"' or has invalid dates"+( (sev == Severity.ERROR)?", rejecting node.":""));
						return retStatus;
					}
					else if(chainStatus.Status == X509ChainStatusFlags.PartialChain){
						bool retStatus = true;
						Severity sev = Severity.WARNING;
						if(requireMatchingCA){
							retStatus =  false;
							sev = Severity.ERROR;
						}
						Logger.Append("HUBRN", sev, "Certificate for node '"+node+"' has not been generated using Hub root CA"+( (sev == Severity.ERROR)?", rejecting node.":""));	
						return retStatus;
					}
					// we tolerate UntrustedRoot errors, provided the client certificcate has been signed by the Hub CA.
					else if (chainStatus.Status != X509ChainStatusFlags.NoError && chainStatus.Status != X509ChainStatusFlags.UntrustedRoot)
						advancedErrors += chainStatus.StatusInformation+", ";
				}
				if(advancedErrors != ""){ // other types of errors : should not happen, so reject client.
					Logger.Append("HUBRN", Severity.WARNING, "Certificate for node '"+node+"' is invalid, reason(s): "+advancedErrors);	
					return false;
				}
			}
			catch(Exception e){
				Logger.Append("HUBRN", Severity.WARNING, "error trying to validate client cert : "+e.Message+" ---- "+e.StackTrace);
				return false;
			}
            		return true;
        	}
		

		internal void Run(){
			if(running)
				throw new Exception("Already running");
			int maxClientNodes = 300;
			int.TryParse(ConfigurationManager.AppSettings["Nodes.MaxConnectedClients"], out maxClientNodes);
			Utilities.Logger.Append("START", Severity.INFO, "######## Starting Hub as '"+Thread.CurrentPrincipal.Identity.Name+"'. Date: "+DateTime.Now.ToString()+", Version: "+Utilities.PlatForm.Instance().NodeVersion
	      			+", OS: "+Utilities.PlatForm.Instance().OS+", Runtime: "+Utilities.PlatForm.Instance().Runtime
			        +", DB Provider: "+ConfigurationManager.AppSettings["Storage.DBHandle.Provider"]);
			running = true;
			Socket hub = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
			connection = hub;
			
			string listenIp = ConfigurationManager.AppSettings["Nodes.ListenIp"];
			int listenPort = 52561;
			int.TryParse(ConfigurationManager.AppSettings["Nodes.ListenPort"], out listenPort);
			IPAddress listenAddress = (listenIp == "*") ? IPAddress.Any : IPAddress.Parse(listenIp);
			IPEndPoint src = new IPEndPoint(listenAddress, listenPort);
			
			hub.Bind(src);
			hub.Listen(100);
			hub.BeginAccept(new AsyncCallback(AcceptClient),hub);

			Utilities.Logger.Append("START", Severity.INFO,"Starting Scheduler...");
			TaskScheduler.Start();
			TaskScheduler.Instance().TaskEvent += HandleTaskEvent;
			TaskScheduler.Instance().NodeWakeUpNeeded += WakeUpNode;
			TasksMonitor.Start();
			Utilities.Logger.Append("START", Severity.INFO,"Starting nodes watcher...");
			NodesMonitor.Instance.Start();
			NodesMonitor.Instance.NodeOffline += HandleOfflineNode;
			Utilities.Logger.Append("START", Severity.INFO, "Starting Remoting Server...");
			Remoting.RemotingServer.Instance.Start();
			Utilities.Logger.Append("START", Severity.INFO,"Hub started.");
		}


		/// <summary>
		/// Logs all messages between user and hub
		/// </summary>
		/// <param name="name">sending/receiving user</param>
		/// <param name="received">direction of the transfer - received or sent</param>
		/// <param name="message">the message received or sent</param>
		private void LogEvent(string name, bool received, string message){
			string transfer = "rcvd";
			if(!received)
				transfer = "sent";
			String code = message.Substring(0,3);
			if(received) Utilities.Logger.Append(transfer /* operation*/,  Severity.DEBUG, message +" "+ Codes.GetDescription(code));
		}

		/// <summary>
		/// Adds or remove a transfer session. Also updates the storage node's load accordingly
		/// </summary>
		/// <param name='s'>
		/// S : the PeerSession
		/// </param>
		/// <param name='added'>
		/// If set to <c>true</c>,the session has to be added, else it has to be removed
		/// </param>
		private static void AddRemoveSession(PeerSession s, bool added){
			Console.WriteLine("AddRemoveSession1("+added+") : Node #"+s.ToNode.Id+" load="+s.ToNode.CurrentLoad);
			if(added){
				sessionsList.Add(s);
				NodesList[s.ToNode.Id].CurrentLoad += 1/(s.ToNode.StoragePriority);
			}
			else{
				sessionsList.Remove(s);
				NodesList[s.ToNode.Id].CurrentLoad -= 1/(s.ToNode.StoragePriority);
			}
			Console.WriteLine("AddRemoveSession2("+added+") : Node #"+s.ToNode.Id+" load="+NodesList[s.ToNode.Id].CurrentLoad);
		}

		private void AddRemoveSession(long sid, bool added){
			AddRemoveSession(sessionsList.GetById(sid), added);
		}

		/// <summary>
		/// Gets destinations for storing data chunks. Destinations count can be 1 to R (redundancy level) or 1 to p (p=paralleism)
		/// 
		/// </summary>
		/// <param name="bsId">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="chunkName">
		/// A <see cref="System.String"/>
		/// </param>
		/// <param name="size">
		/// A <see cref="System.Int64"/>
		/// </param>
		/// <param name="isIndex">
		/// A <see cref="System.Boolean"/>. If set to true, this tells the client to send a confirmation when chunk has effectively 
		/// been transfered and stored : this way  we can track index location(s) into database.
		/// </param>
		//private void  ChooseStorage(int nodeId, long taskId, long sessionId, int parallelism, bool isIndex, bool isAlternateRequest){
		private void  ChooseStorage(uint nodeId, PeerSession s, int parallelism, bool isIndex, bool isAlternateRequest){
			Task currentTask = TaskScheduler.Instance().GetTask(s.TaskId);
			try{
				if(s.Id >0 && ! isAlternateRequest) {
					RenewStorageSession(currentTask, s.Id, 20);
					return;
				}
			}
			catch(Exception e){
				Logger.Append("HUBRN", Severity.WARNING, "Could not renew storage session #"+s.Id+", will try to obtain a new one. Error : "+e.Message);
				isAlternateRequest = true;
			}

			PeerNode askingNode = NodesList[nodeId];
			Console.WriteLine("choosedestinations : askingNode  = #"+askingNode.Id);
			List<PeerNode> dests = new List<PeerNode>();
			List<P2PBackup.Common.Node> excludedDests = new List<P2PBackup.Common.Node>();
			excludedDests.Add(askingNode);
			int budget = currentTask.StorageBudget;
			if(budget == 0) budget = 20; // default value if task has never run before.
			
			if(isIndex){ // only return a session with a budget of 1
				Console.WriteLine("ChooseDestinations() : requested index storage");
				budget = 1;
				parallelism = 1;
			}
			if(isAlternateRequest){ // request for new destination after failure with an already existing session
				currentTask.AddLogEntry(new TaskLogEntry{TaskId = s.TaskId, Code = 601, Message1 = ""+s.Id });
				AddRemoveSession(s.Id, false);
				excludedDests.AddRange(currentTask.StorageNodes);
				parallelism = 1;
			}
			if(dests.Count == 0) // if not asked to renew already existing session (thus existing destination)
				dests = CalculateChunkDestinations(s.TaskId, parallelism, budget, excludedDests);
			if(dests.Count > 0){
				// for each storage node, budget = budget/nbnodes+1
				int perNodeBudget = budget/dests.Count+1;
				CreateStorageSessions(askingNode, dests, currentTask, perNodeBudget, currentTask.BackupSet.DataFlags, isIndex);
			}
			else{
				Utilities.Logger.Append("HUBRN", Severity.WARNING, "Task #"+s.TaskId+" : No storage space available for request of client #"+askingNode.Id+" ("+askingNode.Name+"), <TODO> handle that and report" );	
				TaskScheduler.AddTaskLogEntry(s.TaskId, new TaskLogEntry{TaskId = s.TaskId, Code = 806});
				TaskScheduler.Instance().SetTaskStatus(s.TaskId, TaskStatus.Error);
				TaskScheduler.Instance().SetTaskRunningStatus(s.TaskId, TaskRunningStatus.Cancelled);
				askingNode.ManageTask(currentTask, TaskAction.Cancel);
			}
		}

		/// <summary>
		/// Calculates the chunk destination(s) upon receiving client node request for storage space.
		/// The algorithm is the following
		/// -take all online storage nodes
		/// -restrict to those eligible from task conf (members of required storagegroup(s)
		/// -restrict to those having enough space to store (task chunk's max size)*task budget
		/// -order by reverse  current load (the less loaded node(s) will be elected)
		/// - if needed, restrict to those not present in exclusion list (unreachable nodes)
		/// <b>Note: </b> Several requests to this method may be made before nodes CurrentLoad is updated (when real transfer session starts),
		/// which means we might endup selecting some nodes too many times.
		/// This is an expected behavior, since CurrentLoad has to be considered more as a hint than as an 
		/// absolute, always-right, basis for storage destinations calculation.
		/// </summary>
		/// <returns>
		/// The chunk destinations.
		/// </returns>
		/// <param name='taskId'>
		/// Task identifier.
		/// </param>
		/// <param name='parallelism'>
		/// Parallelism.
		/// </param>
		/// <param name='budget'>
		/// Budget.
		/// </param>
		/// <param name='nodesToExclude'>
		/// Nodes to exclude (because signaled as unreachable by client node).
		/// </param>
		private List<PeerNode> CalculateChunkDestinations(long taskId, int parallelism, int budget, List<P2PBackup.Common.Node> nodesToExclude){
			
			List<PeerNode> destinations = new List<PeerNode>();
			// budget for each storage node will be budget/parallelism
			int perNodeBudget = (budget/parallelism)+1;
			BackupSet bs = TaskScheduler.Instance().GetTask(taskId).BackupSet;

			// All the magic to select a storage node happends here. How cool Linq is!
			// Query itself should be self-explainatory
			var destinationRawList = (from PeerNode node in NodesList
	                                     where  node.StorageGroup == bs.StorageGroup && node.StoragePriority > 0
	                                     && (node.StorageSize - node.StorageUsed - node.ReservedSpace) > bs.MaxChunkSize*perNodeBudget
			                     && !nodesToExclude.Contains(node)
	                                     orderby node.CurrentLoad descending
	                                     select node).Take(parallelism).ToList<PeerNode>();

			if(destinationRawList.Count == 0){
				Logger.Append("HUBRN", Severity.ERROR, "Could not choose any storage node from group "+bs.StorageGroup+" for task #"+taskId+" with parallelism="+parallelism+" and budget="+budget+" ("+budget*bs.MaxChunkSize/1024/1024+"MB required)");
			}
			//if we can't find enough storage nodes, we will use fewer nodes but require more storage space from each one
			if(0 < destinationRawList.Count && destinationRawList.Count < parallelism){
				Logger.Append("HUBRN", Severity.WARNING, "Not enough storage nodes to satisfy parallelism ("+parallelism+") of task #"+taskId);
				CalculateChunkDestinations(taskId, destinationRawList.Count, budget, null);
			}
			for(int i=0; i< Math.Min(parallelism, destinationRawList.Count); i++){
				//destinationRawList[i].ReservedSpace += bs.MaxChunkSize*perNodeBudget;
				destinations.Add(destinationRawList[i]);
			}
			return destinations;
		}

		private void RenewStorageSession(Task task, long sessionId, int budget){

			PeerNode n = null;
			PeerSession existingSession =  sessionsList.GetById(sessionId);
			if(existingSession != null){
				n = NodesList[existingSession.ToNodeId];
				existingSession.RenewBudget(budget);
				if( (n.StorageSize - n.StorageUsed - n.ReservedSpace) > task.BackupSet.MaxChunkSize*budget){
					CreateStorageSession(existingSession, task, false);
				}
				else
					throw new Exception("Cannot Renew session #"+sessionId);
			}
		}

		private void CreateStorageSessions(PeerNode askingNode, List<PeerNode> targetNodes, Task currentTask, int budget, DataProcessingFlags flags, bool isIndexStorage){
			foreach(PeerNode chunkDestNode in targetNodes){
				PeerSession targetSess = null;
				try{
					int sessId = sessionsList.Count+1;
					targetSess = new PeerSession{
						FromNode = askingNode,
						ToNode = chunkDestNode,
						Id = sessId, //sessionId,
						Flags = flags, //currentTask.BackupSet.DataFlags,
						TaskId = currentTask.Id,
						Kind = SessionType.Backup,
						Secret = currentTask.EncryptionKey
					};
					targetSess.RenewBudget(budget);
					CreateStorageSession(targetSess, currentTask, isIndexStorage);

					//if (SessionChanged != null && existingSession == null) SessionChanged(true, SessionType.Backup, targetSess.Id, this, chunkDestNode, currentTask.Id, budget);
					// 3 - we add the storage node(s) to task
					currentTask.AddStorageNode(chunkDestNode);
				}
				catch(IOException ioe){
					// change back destination's available space
					//chunkDestNode.Available = chunkDestNode.Available + currentTask.BackupSet.MaxChunkSize*budget;
					Utilities.Logger.Append("HUBRN", Severity.ERROR, "dest "+chunkDestNode.Name+" not available ("+ioe.Message+"), looking for an alternate one");
					// try another node, recursive call
					//ChooseStorage(askingNode.Id, s, 1, false, true);
					ChooseStorage(askingNode.Id, new PeerSession{TaskId = currentTask.Id, Id = -1}, 1, false, true);
				}
				catch(Exception ex){
					Utilities.Logger.Append("HUBRN", Severity.ERROR, "dest "+chunkDestNode.Name+" : "+ex.Message);
				}
			}
		}

		private void CreateStorageSession(PeerSession s, Task currentTask, bool isIndexSession){
			// --OBSOLETE COMMENT - REMOVE --  First, generate a random key sent to the two nodes.
			// Each node will then send this key to the other.
			// Then received peer key is compared and checked against the one originally sent by server.
			// Doing so helps avoiding (some) nodes identity usurpation

			// 1 - we tell storage node to accept transfer from client, if shared key is verified
			NodesList.GetById(s.ToNode.Id).SendSession(s, SessionType.Store, false);

			// 2 - we tell client node where to put chunk
			NodesList.GetById(s.FromNode.Id).SendSession(s, SessionType.Backup, isIndexSession);

			NodesList.GetById(s.ToNode.Id).ReservedSpace += currentTask.BackupSet.MaxChunkSize*s.Budget;
			if(sessionsList.GetById(s.Id) == null)
				AddRemoveSession(s, true);
			else
				sessionsList[s.Id].RenewBudget(s.Budget);
		}

		private static void HandleTaskEvent(Task t, PeerNode n){
			if(t.RunStatus == TaskRunningStatus.Cancelled || t.RunStatus == TaskRunningStatus.Done){
				// save nodes used space
				foreach(P2PBackup.Common.Node curN in t.StorageNodes)
					new DAL.NodeDAO().UpdateStorageSpace(curN);
				// then, remove sessions
				var sessToRemove = new List<PeerSession>();
				foreach(PeerSession s in sessionsList){
					if(s.TaskId == t.Id){
						sessToRemove.Add(s);
					}
				}
				foreach(PeerSession s in sessToRemove)
					AddRemoveSession(s, false);
			}
		}

		private static void HandleOfflineNode(PeerNode n){
			Logger.Append("WATCHER", Severity.INFO, "Node #"+n.Id+" is offline (didn't reply for more than 5mn)"); 
			try{
				NodesList[n.Id].Dispose();
				Console.WriteLine("node disposed()");
			}
			catch{}
			try{
				//nodeList.Remove(
				Console.WriteLine("Node remove : "+NodesList.Remove(n));
			}
			catch{}
		}

		private static void WakeUpNode(Node n){
			//Node n = new DAL.NodeDAO().Get(nodeId);
			NodesMonitor.Instance.WakeUp(n);
		}

		internal static List<P2PBackup.Common.Node> DiscoverVms(int hypervisorId){
			List<P2PBackup.Common.Node> newNodes = new List<P2PBackup.Common.Node>();
			using (P2PBackupHub.Virtualization.HypervisorManager hvm = new P2PBackupHub.Virtualization.HypervisorManager()){
				Hypervisor hv = new DAL.HypervisorDAO().GetById(hypervisorId);
				hvm.Id = hv.Id;
				hvm.Kind = hv.Kind;
				hvm.Url = hv.Url;
				hvm.UserName = hv.UserName;
				hvm.Password = hv.Password;
				hv.LastDiscover = DateTime.Now;
				new DAL.HypervisorDAO().Update(hv);
				Console.WriteLine("DiscoverVms()  0");
				List<P2PBackup.Common.Node> discoveredNodes = hvm.Discover();
				Console.WriteLine("DiscoverVms()  1");
				Logger.Append("HUBRN", Severity.DEBUG, "Discovered "+discoveredNodes.Count+" VMs");

				foreach(P2PBackup.Common.Node newN in  discoveredNodes){
					Console.WriteLine("DiscoverVms()  3, cur newn="+newN.Name);
					try{
						P2PBackup.Common.Node existingNode = new DAL.NodeDAO().GetByInternalId(newN.InternalId);
						Console.WriteLine("DiscoverVms()  3.1");
						if(existingNode == null){
							Logger.Append("HUBRN", Severity.DEBUG, "The VM "+newN.Name+" has been added to the client nodes.");
							newN.Hypervisor = hypervisorId;
							existingNode = new DAL.NodeDAO().Save((PeerNode)newN);
							if(existingNode != null){
								existingNode.Status = NodeStatus.New;
								newNodes.Add(existingNode);
							}
						}
					}
					catch(Exception e){
						Logger.Append("HUBRN", Severity.ERROR, "Could not add new discovered node : "+e.ToString());
					}
				}
			}
			return newNodes;
		}

		/// <summary>
		/// Logs out all online users and stops the hub
		/// </summary>
		internal static void Shutdown(User user, bool cancelTasks){
			PeerNode u;
			try{
				Utilities.Logger.Append("STOP", Severity.INFO, "Hub shutdown requested by '"+user.Name+"', stopping...");
				Utilities.Logger.Append("STOP", Severity.INFO, "Informing client nodes of shutdown...");
				if(!running)
					return;
				Remoting.RemotingServer.Instance.Stop();
				running = false;
				Thread.MemoryBarrier();
				TaskScheduler.Instance().TaskEvent -= HandleTaskEvent;
				TaskScheduler.Stop();
				TasksMonitor.Stop();
				for (int i = 0; i < NodesList.Count; i++){
					u = (PeerNode)NodesList[i];
					u.Disconnect();
					i = i - 1; 
				}
				if(connection != null){
					connection.Disconnect(true);
					connection.Dispose();
				}
				Utilities.Logger.Append("STOP", Severity.INFO, "Hub stopped.");
				P2PBackupHub.IdManager.Instance.Stop();
				Environment.Exit(0);
			}
			catch(Exception ex){
				Utilities.Logger.Append("STOP", Severity.ERROR, "Could not stop Hub : "+ex.Message);
				Environment.Exit(1);
			}
		}

		private void ClearNode(PeerNode n){
			Utilities.Logger.Append("HUBRN", Severity.INFO, "Node #"+n.Id +" has disconnected.");
			//Console.WriteLine("nodelist remove : "+nodeList.Remove(n));
			n.Disconnect();
			n.Status = NodeStatus.Idle;
			n.LastReceivedPing = DateTime.Now;
		}
	}
}
