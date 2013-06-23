using System;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Node.Utilities;
using P2PBackup.Common;

namespace Node{

	/// <summary>
	/// A session is created when the first request for a transfer (send or receive) between 2 nodes is received. 
	/// The session stays open until a task ends (backup or restore done) to allow reuse.
	/// </summary>
	internal class Session : PeerSession{
		private Socket clientSocket;
		private SslStream controlStream;
		private Socket dataSocket;

		private X509Certificate2 cert;
		//private RSACryptoServiceProvider myKeyPairCrypto;

		/*volatile*/ bool controlSocketVerified = false;
		/*volatile*/ bool dataSocketVerified = false;

		internal delegate void RemoveSessionHandler (Session s);
		internal event RemoveSessionHandler SessionRemoved;

		public delegate void TransfertDoneHandler (bool sent, long taskId, string chunkName, uint destination, int finalSize);
		public event TransfertDoneHandler TransfertDoneEvent;

		public delegate void FileReceivedHandler (bool received, BChunk bFReceived);
		public event FileReceivedHandler FileReceivedEvent;//User.

		public delegate void UpdateStorageHandler (PeerSession session);
		public event UpdateStorageHandler UpdateStorageEvent;//User.

		//public delegate void MessageHandler (long taskid, int code, string data);
		internal ManualResetEvent AuthenticatedEvent;

		private SessionLogger logger;
		private long currentChunkSize;
		private CancellationTokenSource cancellationTokenSource;
		private bool disconnecting = false;
		private DataProcessing.DataPipeline pipeline; // data processing pipeline

		// used as symmetric encryption key (if tasks requires pipeline to encrypt data)
		// IV will be the task ID.
		internal byte[] GuidKey{get;private set;}
		internal byte[] CryptoKey{get;private set;} // used if data encryption is required

		internal Socket ClientSocket{
			get{ return clientSocket;} 
			set { clientSocket = value; }
		}
		
		internal Socket DataSocket{
			get{ return dataSocket;} 
			set { dataSocket = value; }
		}
		
		internal SessionLogger LoggerInstance{
			get{return logger;}
		}

		internal new int Budget{get;set;}

		public Session(PeerSession s, X509Certificate2 nodeCert){
			logger = new SessionLogger(this);
			this.Id = s.Id;
			this.Kind = s.Kind;
			this.Budget =  s.Budget;
			this.FromNode = s.FromNode;
			this.ToNode = s.ToNode;
			this.TaskId = s.TaskId;
			this.GuidKey = System.Guid.NewGuid().ToByteArray();
			this.Secret = s.Secret;
			//myKeyPairCrypto = csp;
			cert = nodeCert;
			AuthenticatedEvent = new ManualResetEvent(false);

			if(this.Kind == SessionType.Store){
				this.Budget = 0; // bug?? do we have to initialize to 0 when receiving/storing?
				// client-side flags are not relevant here since the client data processing is not initialized by the Session.
				if(s.Flags.HasFlag(DataProcessingFlags.CChecksum)) s.Flags ^= DataProcessingFlags.CChecksum;
				if(s.Flags.HasFlag(DataProcessingFlags.CCompress)) s.Flags ^= DataProcessingFlags.CCompress;
				if(s.Flags.HasFlag(DataProcessingFlags.CDedup)) s.Flags ^= DataProcessingFlags.CDedup;
				if(s.Flags.HasFlag(DataProcessingFlags.CEncrypt)) s.Flags ^= DataProcessingFlags.CEncrypt;
				this.Flags = s.Flags ;
				pipeline = new Node.DataProcessing.DataPipeline(Node.DataProcessing.PipelineMode.Read, this.Flags);
				logger.Log(Severity.DEBUG, "Creating storage session ("+this.Kind.ToString()+") with client node #"+this.FromNode.Id+" ("+this.FromNode.IP+":<UNAVAILABLE>)");
			}
			else if(this.Kind == SessionType.Backup){
				this.CryptoKey = System.Guid.NewGuid().ToByteArray();
				logger.Log(Severity.DEBUG, "Creating client session #"+this.Id+" with storage node #"+this.ToNode.Id+" ("+this.ToNode.IP+":"+this.ToNode.ListenPort+")");
			}
		}
		
		~Session(){ // for debug purpose
			//Logger.Append(Severity.TRIVIA," <TRACE> Session #"+this.Id+" destroyed");	
		}
		
		/// <summary>
		/// Connects to client2
		/// </summary>
		public void Connect(){
			Logger.Append(Severity.DEBUG, "Trying to start connection to node "+this.ToNode.IP+", port "+this.ToNode.ListenPort);
			IPAddress addr = IPAddress.Parse(this.ToNode.IP);
			IPEndPoint dest = new IPEndPoint(addr, this.ToNode.ListenPort);

			// 1- Opening control socket
			clientSocket = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
		
			IAsyncResult result = clientSocket.BeginConnect(dest, null, null);
			result.AsyncWaitHandle.WaitOne(10000, true);//not connected after 10s means storage node too slow of unavailable
			if(!clientSocket.Connected)
				throw new Exception("Could not connect message socket");


			// 2- Opening data socket
			dataSocket = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);

			/*if(ConfigManager.GetValue("STDSOCKBUF") == null){
				dataSocket.SendBufferSize = 512*1024; //512k
			}*/
			/*else{
				try{
					dataSocket.NoDelay = false;
					clientSocket.SendBufferSize = 0;
					clientSocket.NoDelay = true;
				}
				catch{} // NoDelay doesn't seem to be supported on FreeBSD
			}*/
			dataSocket.SendTimeout = 120000;
			result = dataSocket.BeginConnect(dest, null, null);
			result.AsyncWaitHandle.WaitOne(10000, true);
			if(!dataSocket.Connected)
				throw new Exception("Could not connect data socket");
			StartListening(false);
			SendDigitalSignature();
			logger.Log(Severity.INFO, "Opened 1 session with storage node #"+this.ToNode.Id);
		}

		/// <summary>
		/// Sets the streams to the socket, creates a new thread that listens
		/// </summary>
		public void StartListening(bool isServer){
				clientSocket.ReceiveTimeout = -1;
				try{
					clientSocket.ReceiveBufferSize = 0;
					clientSocket.NoDelay = true;
				}catch{}

				// listen for messages on the control socket
				StateObject state = new StateObject();
        		state.WorkSocket = clientSocket;
				NetworkStream peerStream = new NetworkStream(clientSocket);
				state.Stream = new SslStream(peerStream, false, new RemoteCertificateValidationCallback(AcceptCertificate), null);
				controlStream = state.Stream;
				//state.Stream.AuthenticateAsServer(cert);
				if(isServer)
					state.Stream.AuthenticateAsServer(cert);	
				else
					state.Stream.AuthenticateAsClient(this.ToNode.HostName);

				state.Stream.BeginRead(state.Buffer, 0, 4, this.HeaderReceived, state);
				//clientSocket.BeginReceive( state.Buffer, 0, 4, 0, this.HeaderReceived, state);

				// also listen for (authentication) on data socket
				StateObject dataState = new StateObject();
				dataState.WorkSocket = dataSocket;
				dataState.IsDataSocket = true;
				dataSocket.BeginReceive( dataState.Buffer, 0, 4, 0, this.HeaderReceived, dataState);
				logger.Log(Severity.DEBUG, "Listening for messages from "+clientSocket.RemoteEndPoint.ToString());
		}

		static bool AcceptCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors){
			return true;
		}

		private void HeaderReceived (IAsyncResult ar){
			StateObject so = (StateObject) ar.AsyncState;
			int read = 0;
			try{	
				if(so.IsDataSocket)
					read = so.WorkSocket.EndReceive(ar);
				else
					read =  so.Stream.EndRead(ar);// so.WorkSocket.EndReceive(ar);
			}
			catch(Exception){
				//logger.Log(Severity.ERROR, "Error waiting for data from peer : "+e.ToString());
				Disconnect();	
			}
			if(read == 0 && ! disconnecting){
				logger.Log(Severity.ERROR, "Peer node  has disconnected. (read 0)");
				Disconnect();
				return;
			}
			
			//logger.Log(Severity.TRIVIA, "Received message header, msg size will be "+msg_length);
			try{
				int msg_length = BitConverter.ToInt32(so.Buffer, 0);
				if(!disconnecting){
					//so.WorkSocket.BeginReceive( so.Buffer, 0, msg_length/*so.buffer.Length*/, 0, this.MessageReceived, so);
					if(so.IsDataSocket)
						so.WorkSocket.BeginReceive( so.Buffer, 0, msg_length/*so.buffer.Length*/, 0, this.MessageReceived, so);
					else
						so.Stream.BeginRead(so.Buffer, 0, msg_length, this.MessageReceived, so);
				}
			}
			catch(Exception e){
				logger.Log(Severity.ERROR, "Peer node has disconnected ("+e.Message+")");
			}
		}
		
		private void MessageReceived(IAsyncResult ar){
			try{
				StateObject so = (StateObject) ar.AsyncState;
				int read = 0;
				if(so.IsDataSocket){
					read = so.WorkSocket.EndReceive(ar);
				}
				else{
					read = so.Stream.EndRead(ar);// so.WorkSocket.EndReceive(ar);
				}
				Decode(Encoding.UTF8.GetString(so.Buffer, 0, read));
				if(!disconnecting  && !so.IsDataSocket)
					so.Stream.BeginRead(so.Buffer, 0, 4, this.HeaderReceived, so);
					//so.WorkSocket.BeginReceive( so.Buffer, 0, 4, 0, this.HeaderReceived, so);
			}
			catch(Exception ioe){
				logger.Log(Severity.ERROR, "Error reading data, Disconnecting session. (" + ioe.Message+")"+" ---- "+ioe.StackTrace);
				Disconnect();
			}
		}	
		
		/// <summary>
		/// Decodes the incoming messages
		/// </summary>
		/// <param name="message">the incoming message</param>
		private void Decode(string message){
			message = message.Replace(System.Environment.NewLine, "");
			string[] decoded = message.Split(' ');
			string msgExcerpt = null;
			if(message.Length > 45)
				msgExcerpt = message.Substring(0,45);
			else
				msgExcerpt = message;
			logger.Log(Severity.TRIVIA, "Received message '"+message+"'");
			string type = decoded[0].Trim();
			switch(type){
				case "CRC"://Checksum
					if(decoded.Length == 2){
					}
					break;
				case "END":
					VerifyAuth();
					//UpdateStorageEvent(this.TaskId, storedDataSize, this.Id);
					UpdateStorageEvent(this);
					Disconnect();
					break;
				case "ERR":
					VerifyAuth();
					//UpdateStorageEvent(this.TaskId, storedDataSize, this.Id);
					UpdateStorageEvent(this);
					Disconnect();
					break;
				case "CHD": // error while storing chunk
					VerifyAuth();
					if(decoded.Length == 3){
						currentChunkSize = long.Parse(decoded[2]);
						cancellationTokenSource.Cancel();
						Thread.MemoryBarrier();
					}
					else
						ProtocolViolationException("CHD operation requires 2 parameters");
					break;
				case "DS1": case "DS3"://Digital Signature from client node, on both control and data sockets
					if(decoded.Length == 2){
						if(type == "DS1")
							this.controlSocketVerified = CheckDigitalSignature(type, decoded[1]);
						else if(type == "DS3")
							this.dataSocketVerified = CheckDigitalSignature(type, decoded[1]);
						if(this.controlSocketVerified /*&& !this.dataSocketVerified*/){
							// Use Original string as cryptokey if encryption is done at the storagenode level
							this.CryptoKey = Convert.FromBase64String(this.Secret);
							logger.Log(Severity.TRIVIA, "Successfully checked peer signature on control socket. controlSocketVerified="+controlSocketVerified+",dataSocketVerified="+dataSocketVerified);
							
						}
						else if(this.dataSocketVerified /*&& !this.controlSocketVerified*/){
							logger.Log(Severity.TRIVIA, "Successfully checked peer signature on data socket. dataSocketVerified="+dataSocketVerified+",controlSocketVerified="+controlSocketVerified);
						}
						/*else{
							logger.Log(Severity.ERROR, "Failed to check peer signature");
							SendMessageToNode("506");
						}*/
						if(this.controlSocketVerified && this.dataSocketVerified){
							EndInit();
							SendDigitalSignature();
						}
					}
					else
						ProtocolViolationException("DS1 operation expects 3 parameters, but got "+decoded.Length+". Raw message :"+message);
					
					break;
				case "DS2"://Digital Signature from storage node 
					if(decoded.Length == 2){
						logger.Log(Severity.DEBUG, "Received DS2, digital signature from storage node");
						this.controlSocketVerified = CheckDigitalSignature(type, decoded[1]);
						if(controlSocketVerified){ 
							logger.Log(Severity.INFO, "Storage node verification Ok, connection and authentication successful");
							AuthenticatedEvent.Set();
						}
						else{
						logger.Log(Severity.ERROR, "Failed to check signature from peer data socket, check keys.");
						}	
					}
					else
						ProtocolViolationException("DS{0,1,2} operation expects 2 parameters, but got "+decoded.Length+". Raw message :"+message);
					break;

				case "FIL":
					VerifyAuth();
					if(decoded.Length == 3){ 
						string currentChunkName = decoded[1];
						int currentChunkSize = int.Parse(decoded[2]);
						long finalChunkSize = 0;
						cancellationTokenSource = new CancellationTokenSource();
						System.Threading.Tasks.Task receiveT = System.Threading.Tasks.Task.Factory.StartNew(() =>{
							 finalChunkSize = ReceiveChunk(currentChunkName, currentChunkSize, cancellationTokenSource.Token); 
						}, cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
						receiveT.ContinueWith( t => {
							var aggException = t.Exception.Flatten();
							foreach(var exception in aggException.InnerExceptions){
								this.logger.Log(Severity.ERROR, "Unexpected error while Receiving data : "+exception.ToString());
								//backup.AddHubNotificationEvent(999, exception.Message, "");
							}
							
						}, TaskContinuationOptions.OnlyOnFaulted);
						receiveT.ContinueWith( t => {
							SendMessageToNode("205 "+currentChunkName+" "+finalChunkSize);
						}, TaskContinuationOptions.NotOnFaulted);

					//
					}
					break;
				case "GET":
					VerifyAuth();
					if(decoded.Length == 2){
						if(this.Kind == SessionType.RecoverData){
							/*if(File.Exists(this.sharePath + Path.DirectorySeparatorChar + decoded[1])){
								CheckForFileSize(decoded[1]);
							}
							else{
								SendMessageToNode("507");
								Console.WriteLine("INFO : Session.Decode : Asked file not found "+decoded[1]);
							}*/
						}
					}
					break;
				case "205": //File received
					VerifyAuth();
					if(decoded.Length == 3 /*&& RemoveSessionEvent != null*/){
						if(this.Kind == SessionType.Backup ){
							this.RealHandledData += int.Parse(decoded[2]);
							if(TransfertDoneEvent != null)TransfertDoneEvent(true, 0, decoded[1], this.ToNode.Id, int.Parse(decoded[2]));
						}
						if(this.Kind == SessionType.RecoverData){
							//Disconnect();
							//RemoveSessionEvent(this,sessionType);
						}
					}
					else
						ProtocolViolationException("205 operation expects 2 parameter and removesessionevent, but got "+decoded.Length+". Raw message :"+message);
					break;
				case "406"://No storage space for backup requested by me
					logger.Log(Severity.ERROR, "No storage space available for backup.");
					break;
				case "506"://Client sent unrecognized key.
					logger.Log(Severity.DEBUG, "RECEIVED 506 (wrong key)!!!!!!!!!!!!!!!!!!!!!!");
					break;
				case "507" :
					logger.Log(Severity.ERROR, "received 507  <TODO> request other destination having another copy of this file");
					break;
				case "OOS":
					logger.Log(Severity.ERROR, "Received OOS (Peer claims to be out of space)");
					break;
				case "RSD" : // RSD, ReSenD missing part of chunk
					logger.Log(Severity.WARNING, "received RSD (resend petition) for chunk "+decoded[1]);
					break;
				default : 
					logger.Log(Severity.WARNING, "received message with unknown type : "+msgExcerpt+"...");
					//throw new Exception(); // DEBUG hack : on weird message, choose to crash
					break;
			}
		}

		/// <summary>
		/// Closes the socket and streams for this session
		/// if the connected client has disconnected
		/// Sets the bool attributes to false
		/// </summary>
		internal  void Disconnect(){
			if(disconnecting) return;
			disconnecting = true;
			//Thread.MemoryBarrier();
			logger.Log(Severity.DEBUG, "Ending '"+this.Kind.ToString()+"' session #"+this.Id+" with peer...");
			try{
				controlStream.Close();
				clientSocket.Shutdown(SocketShutdown.Both);
				clientSocket.Close();
				dataSocket.Shutdown(SocketShutdown.Both);
				dataSocket.Close();
			}
			catch(Exception e){
				logger.Log(Severity.INFO, "Socket was probably already closed by sudden client disconnection : "+e.Message);
			}
			finally{
				if(SessionRemoved != null)	SessionRemoved(this);
				logger.Log(Severity.DEBUG, "Session #"+this.Id+"  ended.");
				//MessageEvent(taskId, 799, clientIp);
			}
		}

		private void EndInit(){
			if(this.Kind != SessionType.Store)
				return;
			if(!(dataSocketVerified && controlSocketVerified))
				return;
			logger.Log(Severity.TRIVIA, "Finishing storage session initialization.");
			// TODO: IMPORTANT  find a much better place to put all this
			pipeline.CryptoKey = this.CryptoKey;
			byte[] iv = new byte[16];
			Array.Copy (System.BitConverter.GetBytes(this.TaskId), iv, 8);
			Array.Copy (System.BitConverter.GetBytes(this.TaskId), 0, iv, 8, 8);
			pipeline.IV = iv; //new byte[]{Conver
			pipeline.Init();
		}

		public void SendDigitalSignature(){
			string peerPubKey = "";
			if(this.Kind == SessionType.Backup || this.Kind == SessionType.Recover)
				peerPubKey = this.ToNode.PublicKey;
			else if(this.Kind == SessionType.Store || this.Kind == SessionType.RecoverData)
				peerPubKey = this.FromNode.PublicKey;

			byte[] digSig = CreateDigitalSignature(peerPubKey, this.Secret);
			Logger.Append(Severity.DEBUG, "Created and sent digital signature to peer");
			if(this.Kind == SessionType.Backup || this.Kind == SessionType.Recover){ // double auth on control and data sockets
				SendMessageToNode("DS1 " + Convert.ToBase64String(digSig));
				Thread.Sleep(200); // temp hack to mitigate out-of-order DS1/DS3
				SendMessageToNode("DS3 " + Convert.ToBase64String(digSig), true);
			}
			else if(this.Kind == SessionType.Store){
				SendMessageToNode("DS2" + " " + Convert.ToBase64String(digSig) );
			}
			
			//Console.WriteLine("DEBUG: 2-Session.SendDigitalSignature : sent digital signature");
		}


		/// <summary>
		/// Sends filname and filesize to client2
		/// </summary>
		/// <param name="fileName">filename</param>
		/// <param name="fileSize">filesize</param>
		internal void AnnounceChunkBeginTransfer(string fileName, long fileSize){
			SendMessageToNode("FIL " + fileName + " " + fileSize);
		}
		
		internal void AnnounceChunkEndTransfer(string fileName, long fileSize){
			SendMessageToNode("CHD " + fileName + " " + fileSize);
			Budget--;
		}

		/// <summary>
		/// Tells the other client that a recoveryfile should be fetched
		/// </summary>
		/// <param name="fileName"></param>
		private void GetFile(string fileName){
			SendMessageToNode("GET " + fileName);
		}
		
		/// <summary>
		/// Renews the budget.To be called when hub asks us to receive more chunks than previously budgeted
		/// </summary>
		/// <param name='additionalBudget'>
		/// Additional budget.
		/// </param>
		new internal void RenewBudget(int additionalBudget){
			Budget += additionalBudget;	
			logger.Log(Severity.INFO, "Renewed budget to "+Budget);
		}

		/// <summary>
		/// Receives and stores the chunk.
		/// </summary>
		/// <returns>
		/// The final data size after processing
		/// </returns>
		/// <param name='chunkName'>
		/// Chunk name.
		/// </param>
		/// <param name='headerSize'>
		/// Header size.
		/// </param>
		/// <param name='token'>
		/// Token.
		/// </param>
		private long ReceiveChunk(string chunkName, int headerSize, CancellationToken token){
			if(Budget == 0){
				logger.Log(Severity.ERROR, "Can't receive chunk, authorized budget is 0 (zero)");
				SendMessageToNode("OOB "+chunkName); //out-of-budget
				Disconnect();
				return 0;
			}
			currentChunkSize = 0;
			string storePath = Utilities.Storage.GetStoragePath(chunkName);
			DateTime start = DateTime.Now;
			long rdbyte = 0;
			int received = 0;

			try	{
				//dataSocket.ReceiveTimeout = 30000;
				logger.Log(Severity.DEBUG, "1/2 Receiving chunk from client node, header size "+headerSize+", destination "+ storePath);
				using(Stream file = PlatformStreamFactory.Instance().GetPlatformStream(true, storePath, FileMode.CreateNew)){
					pipeline.OutputStream = file;
					pipeline.Init();
					pipeline.Reset();
					byte[] buffer = new byte[512*1024]; //512k
					long bytesToReceive = long.MaxValue;
					while(rdbyte < bytesToReceive){
						if(dataSocket.Available == 0 && !token.IsCancellationRequested){
							//Console.WriteLine ("ReceiveChunk() : cancellationTokenSource.IsCancellationRequested : "+/*cancellationTokenSource.IsCancellationRequested*/token.IsCancellationRequested);
							Thread.Sleep(10);
							continue;
						}
						if(/*cancellationTokenSource.IsCancellationRequested*/token.IsCancellationRequested){// transfert done
							bytesToReceive = currentChunkSize;
							//Console.WriteLine("received cancellation request, final size="+bytesToReceive+", received until now="+rdbyte);
							if(rdbyte >= bytesToReceive)
								break;
						}
						if(dataSocket.Available<=0) continue;
						received = dataSocket.Receive(buffer, buffer.Length, SocketFlags.None);
						rdbyte += received;
						pipeline.Stream.Write(buffer,0,received) ;
						//Console.WriteLine ("@4/4after pipeline Write()");
					}
					pipeline.Stream.Flush();
					logger.Log(Severity.TRIVIA, "Received "+rdbyte+"/"+bytesToReceive+", final size after processing="+/*pipeline.Stream.Length*/pipeline.FinalSize);

				}// end using. Output file is automatically closed.
				// TODO : on *Nix, set sticky bit to prevent accidntal deletion of the chunk (it's backuped data!!)

				double transferTime = (DateTime.Now - start).TotalMilliseconds;
				if(rdbyte == headerSize || rdbyte == 0) // don't store empty chunks
					DeleteChunk(chunkName, false);
				this.RealHandledData += pipeline.FinalSize;

				Budget--;
				logger.Log(Severity.DEBUG, "2/2 Successfully received chunk "+chunkName+", size="+Math.Round((double)rdbyte/1024)+"KB,"
					+"speed="+Math.Round((double)(rdbyte/1024)/(transferTime/1000),1)+" KB/s), remaining budget="+Budget);
				if(Budget == 0 && UpdateStorageEvent != null) UpdateStorageEvent(this);


				/*if(sessionType == 2 && FileReceivedEvent != null){
					FileReceivedEvent(true,bf);
				}*/
			}
			catch(Exception e){
				Logger.Append(Severity.ERROR, "Error while receiving data chunk : "+e.ToString());
				if(this.Kind == SessionType.Store)
					DeleteChunk(chunkName, false);
				SendMessageToNode("ERR "+chunkName);
			}
			return pipeline.FinalSize;
		}	

		/// <summary>
		/// Deletes a chunk.
		/// </summary>
		/// <returns>
		/// The deleted chunk size (== recovered space)
		/// </returns>
		/// <param name='chunkName'>
		/// Chunk name.
		/// </param>
		/// <param name='confirm'>
		/// if true, confirm to peer node that chunk has been deleted
		/// </param>
		private long DeleteChunk(string chunkName, bool confirm){
			string chunkFullPath = Utilities.Storage.GetStoragePath(chunkName);
			try{
				long size = (new FileInfo(chunkFullPath)).Length;
				File.Delete(chunkFullPath);
				if(confirm)
					SendMessageToNode("206 "+chunkName+" "+size);
				return size;
			}
			catch(Exception e){
				this.LoggerInstance.Log(Severity.ERROR, "Could not delete chunk "+chunkName+" : "+e.Message);
				if(confirm)
					SendMessageToNode("806 "+chunkName);//error : cant delete chunk
			}
			return 0;
		}
		
		internal void SendDisconnect(){
			SendMessageToNode("END");
			Disconnect();
		}

		private void SendMessageToNode(string message){
			SendMessageToNode(message, false);
		}

		private byte[] CreateDigitalSignature(string peerPubKey, string clearSecret){
			RSACryptoServiceProvider peerCrypto = new RSACryptoServiceProvider();
			peerCrypto.FromXmlString(peerPubKey);
			return peerCrypto.Encrypt(Convert.FromBase64String(clearSecret), false);//sign with other peer's public key
		}

		internal bool CheckDigitalSignature(string type, string peerNodeSignature){
			RSACryptoServiceProvider myKeyPairCrypto;
			GetCrypto(out myKeyPairCrypto);
			Logger.Append (Severity.DEBUG, "Auth step '"+type+"' : Checking peer encrypted value ("+peerNodeSignature+") against our private key");
			byte[] decryptedSecret = myKeyPairCrypto.Decrypt(Convert.FromBase64String(peerNodeSignature), false);
			Console.WriteLine ("decrypted value="+Convert.ToBase64String(decryptedSecret)+", original secret="+this.Secret);
			bool clientVerification = (Convert.ToBase64String(decryptedSecret) == this.Secret);
			return clientVerification;
		}

		// provides option to also send message to data socket (for digital signature check)
		private void SendMessageToNode(string message, bool sendToDataSocket){
			if(disconnecting) return; // avoid racy situations
			byte[] byteMsg = Encoding.UTF8.GetBytes(message);
			int msgSize = byteMsg.Length;
			byte[] header = BitConverter.GetBytes(msgSize); // header always has 'int' size (4 bytes)
			try{
				if(sendToDataSocket){
					dataSocket.Send(header);
					dataSocket.Send(byteMsg);
				}
				else{
					/*clientSocket.Send(header);
					clientSocket.Send(byteMsg);*/
					controlStream.Write(header);
					controlStream.Write(byteMsg);
				}
				logger.Log(Severity.TRIVIA, "Sent "+message);
			}
			catch(Exception e){
				logger.Log(Severity.ERROR, "Can't send message '"+message+"' to peer node : "+e.Message);
				throw(e);
			}
		}
		
		private void ProtocolViolationException(string msg){
			Console.WriteLine("ERROR : Session.Decode : protocol violation exception : "+msg);
		}

		private void GetCrypto(out RSACryptoServiceProvider keyPair){
			keyPair = null;
			if(!File.Exists(ConfigManager.GetValue("Security.CertificateFile"))) return;
			X509Certificate2 cert = new X509Certificate2(ConfigManager.GetValue("Security.CertificateFile"), "", X509KeyStorageFlags.Exportable);
			keyPair =  (RSACryptoServiceProvider)cert.PrivateKey;
		}

		/// <summary>
		/// Determines whether this instance has been authenticated using 3-peers keys exchange.
		/// If not, it throws a NodeSecurity sexception to prevent the current session from going further and protect node's security.
		/// </summary>
		/// <returns>
		/// <c>true</c> if this instance is authenticated; otherwise, <c>false</c>.
		/// </returns>
		private void VerifyAuth(){
			if( (this.Kind == SessionType.Backup || this.Kind == SessionType.Recover)&& controlSocketVerified)
				return ;
			else if( (this.Kind == SessionType.Store || this.Kind == SessionType.Recover) && controlSocketVerified && dataSocketVerified)
				return ;
			else
				throw new NodeSecurityException("Session is not authenticated and verified for the requested operation");
		}
	}

}
