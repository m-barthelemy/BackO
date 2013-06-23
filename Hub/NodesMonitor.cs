using System;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using P2PBackup.Common;
using P2PBackupHub.Utilities;

namespace P2PBackupHub {

	/// <summary>
	/// Nodes monitor send "UDP pings" to each node at regular intervals (5mn by default)
	/// to check that they are reachable in case an operation is needed
	/// </summary>
	internal class NodesMonitor {

		private static readonly NodesMonitor _instance = new P2PBackupHub.NodesMonitor();
		private CancellationTokenSource tokenSource;
		private CancellationToken token;
		private Socket pingerSock;

		private NodesMonitor() {	}

		internal static NodesMonitor Instance{get{return _instance;}}

		internal delegate void NodeOfflineHandler(Node n);
		public event NodeOfflineHandler NodeOffline;

		internal void Start(){
			tokenSource = new CancellationTokenSource();
			token = tokenSource.Token;
			pingerSock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			ListenHubWakeups();
			var workerThread = new Thread( () => Watch() );
			workerThread.Start();
		}

		internal void Stop(){
			tokenSource.Cancel();
		}

		private void Watch(){
			while(!token.IsCancellationRequested){
				foreach(PeerNode n in Hub.NodesList){
					//Exclude non-sleeping nodes from ping
					if(n.Status != NodeStatus.Idle)
						continue;
					// No repln.LastReceivedPing.AddMinutes(2) < DateTime.Nowy from more than 5 minutes : consider node as offline
					if(n.LastReceivedPing.AddMinutes(5) < DateTime.Now){
						if(NodeOffline != null)NodeOffline(n);
					}
					else if(n.LastReceivedPing.AddMinutes(2) < DateTime.Now){
						Logger.Append("WATCHER", Severity.TRIVIA, "Checking if node #"+n.Id+" is still reachable...");
						SendUdpMessage("PING "+n.Id, n);
					}
				}
				Thread.Sleep(5000); // TODO : find more elegant way to 'do nothing'

			}
			pingerSock.Dispose();
		}

		internal void WakeUp(P2PBackup.Common.Node n){
			if(n == null) return;
			Logger.Append("TASK", Severity.DEBUG, "Sending wakeup signal to node #"+n.Id);
			SendUdpMessage("WAKEUP", n);
		}

		private void SendUdpMessage(string message, Node n){
			IPEndPoint nep = new IPEndPoint(System.Net.IPAddress.Parse(n.IP), n.ListenPort);
			byte[] msg = System.Text.Encoding.ASCII.GetBytes(message);
			pingerSock.SendTo(msg, nep);
		}

		private static void ListenHubWakeups(){
			//Socket wakeupSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			IPEndPoint hubEP = new IPEndPoint(IPAddress.Any, 52561);
			//wakeupSocket.Bind(hubEP);
			var u = new UdpClient(hubEP);
			UdpState s = new UdpState{U=u, E = hubEP};
			u.BeginReceive(MessageReceived, s);
		}

		private static void MessageReceived(IAsyncResult ar){
			UdpClient u = (UdpClient)((UdpState)(ar.AsyncState)).U;
			IPEndPoint e = (IPEndPoint)((UdpState)(ar.AsyncState)).E;

			Byte[] receiveBytes = u.EndReceive(ar, ref e);
			string msg = System.Text.Encoding.ASCII.GetString(receiveBytes);
			Console.WriteLine("Received UDP message "+msg);
			if(msg.StartsWith("PING")){
				int nodeId = 0;
				if(int.TryParse(msg.Substring(5), out nodeId) && nodeId >0){
					Logger.Append("WATCHER", Severity.TRIVIA, "Received alive confirmation from node #"+nodeId);
					Hub.NodesList.GetById(nodeId).LastReceivedPing = DateTime.Now;
				}
			}
			u.BeginReceive(MessageReceived, (UdpState)ar.AsyncState);
		}
	}
}
