using System;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using P2PBackup.Common;

namespace NodeD {

	/// <summary>
	/// Main class. Waits for an UDP 'wakeup' message and instanciates the client node in a dedicated appdomain.
	/// When client nodes signals it has finished its work, go back to 'idle' state by unloading the appdomain.
	/// </summary>
	class MainClass : MarshalByRefObject {

		private static UdpClient u;
		private static IClientNode c;
		private static string[] startArgs;
		private static bool idle = false;
		private static int hubPort;
		private static System.Threading.ManualResetEvent stopEvent;

		public static void Main (string[] args) {
			startArgs = args;
			stopEvent = new System.Threading.ManualResetEvent (false);
			AppDomain.CurrentDomain.AssemblyResolve += ResolveLibs;
			AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += ResolveLibs;
			Console.CancelKeyPress += Shutdown;
			WakeUp();

			//Keep main thread/loop running forever (until ctrl-c or stop signal)
			stopEvent.WaitOne ();
			/*while(true){
				System.Threading.Thread.Sleep (2000);
			}*/
		}

		private static void ListenHubWakeups(IPEndPoint ep){
			IPEndPoint hubEP = new IPEndPoint(IPAddress.Any, ep.Port);
			if(u != null) // check if not already listening (if active -> sleeping -> active -> sleeping again)
				return;
			u = new UdpClient(hubEP);
			UdpState s = new UdpState{U=u, E = ep};
			u.BeginReceive(MessageReceived, s);
		}

		private static void MessageReceived(IAsyncResult ar){
			UdpClient u = (UdpClient)((UdpState)(ar.AsyncState)).U;
			IPEndPoint e = (IPEndPoint)((UdpState)(ar.AsyncState)).E;
			byte[] receivedBytes = u.EndReceive(ar, ref e);
			string msg = Encoding.ASCII.GetString(receivedBytes);

			if(msg == "WAKEUP" && idle)
				WakeUp();
			else if(msg.StartsWith("PING")){
				//SendUdpMessage(msg, new System.Net.IPEndPoint(e.Address, 52566));
				u.Send(receivedBytes, receivedBytes.Length, e.Address.ToString(), hubPort);
				Console.WriteLine("Received PING, replying to "+e.ToString());
			}
			u.BeginReceive(MessageReceived, (UdpState)ar.AsyncState);
		}


		private static void WakeUp(){
			idle = false;
			string libsPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location).Replace("bin", "lib");
			AppDomainSetup domainSetup = new AppDomainSetup { PrivateBinPath = libsPath, ApplicationBase = libsPath };
			var nodeDomain = AppDomain.CreateDomain("Node", null, domainSetup);
			/*IClientNode*/ c = (IClientNode)nodeDomain.CreateInstanceAndUnwrap("Node", "Node.Client");
			//c.OnGoingIdle += (object sender, EventArgs e) => AppDomain.Unload(nodeDomain);
			IPEndPoint[] hubEP = c.Login(startArgs);

			// If we get there, the node has decided to go idle. 
			ListenHubWakeups(hubEP[1]);
			hubPort = hubEP[0].Port;
			idle = true;
			Console.WriteLine("unloading node..."+((nodeDomain == null)? " node already null": ""));
			if(nodeDomain != null)
				AppDomain.Unload(nodeDomain);
		}

		private static System.Reflection.Assembly ResolveLibs(object sender, ResolveEventArgs args){
			Console.WriteLine("Trying to resolve required dependency '"+args.Name+"'");
			string binsPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
			string libsPath = Path.GetDirectoryName(
				System.Reflection.Assembly.GetExecutingAssembly().Location).Replace("bin", "lib");
			string argsName = args.Name;
			if(argsName.IndexOf(",") >0)
				argsName = argsName.Substring(0, argsName.IndexOf(","));

			string wantedLibAssemblyPath = System.IO.Path.Combine(libsPath, argsName+".dll");
			string wantedBinAssemblyPath = System.IO.Path.Combine(binsPath, argsName+".exe");
			Console.WriteLine("(main)probable assembly path : "+wantedLibAssemblyPath);	
			if(File.Exists(wantedLibAssemblyPath))
				return System.Reflection.Assembly.LoadFile(wantedLibAssemblyPath);	
			else if(File.Exists(wantedBinAssemblyPath))
				return System.Reflection.Assembly.LoadFile(wantedBinAssemblyPath);	
			else
				return null;
		}

		private static void Shutdown(object sender, ConsoleCancelEventArgs ccea){
			Console.WriteLine ("Exiting...");
			try{
				c.Stop(false);
				u.Close();
				stopEvent.Set();
			}
			catch{}
		}

		/*private static void SendUdpMessage(string message, IPEndPoint endpoint){
			Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			string wakeup =  message;
			byte[] msg = System.Text.Encoding.ASCII.GetBytes(wakeup);
			sock.SendTo(msg, endpoint);
		}*/
	}
}
