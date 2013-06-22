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
		private static string[] startArgs;
		private static bool idle = false;

		public static void Main (string[] args) {
			startArgs = args;
			AppDomain.CurrentDomain.AssemblyResolve += ResolveLibs;
			AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += ResolveLibs;

			WakeUp();
			while(true){
				System.Threading.Thread.Sleep (2000);
			}
		}

		private static void ListenHubWakeups(IPEndPoint hubEP){
			//Socket wakeupSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			//IPEndPoint hubEP = new IPEndPoint(IPAddress.Any, 52566);
			//wakeupSocket.Bind(hubEP);
			u = new UdpClient(hubEP);
			UdpState s = new UdpState{U=u, E = hubEP};
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
				u.Send(receivedBytes, receivedBytes.Length, e);
				Console.WriteLine("Received PING, replying");
			}
			u.BeginReceive(MessageReceived, (UdpState)ar.AsyncState);
		}


		private static void WakeUp(){
			idle = false;
			string libsPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location).Replace("bin", "lib");
			AppDomainSetup domainSetup = new AppDomainSetup { PrivateBinPath = libsPath, ApplicationBase = libsPath };
			var nodeDomain = AppDomain.CreateDomain("Node", null, domainSetup);

			IClientNode c = (IClientNode)nodeDomain.CreateInstanceAndUnwrap("Node", "Node.Client");
			Console.CancelKeyPress += delegate {
				Console.WriteLine ("Exiting...");
				try{
					c.Stop(false);
					u.Close();
				}
				catch(Exception e){
					Console.WriteLine ("Could not properly stop Node : "+e.Message);
				}
				AppDomain.Unload(nodeDomain);
			};
			Console.WriteLine ("Node created");
			//c.OnGoingIdle += HandleClientIdle;
			//c.OnGoingIdle += (object sender, EventArgs e) => AppDomain.Unload(nodeDomain);
			IPEndPoint[] hubEP = c.Login(startArgs);
			ListenHubWakeups(hubEP[0]);
			// If we get there, the node has decided to go idle. 
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

		/*private static void HandleClientIdle(object sender, EventArgs e){
			Console.WriteLine("unloading node..."+((nodeDomain == null)? " node already null": ""));
			if(nodeDomain != null)
				AppDomain.Unload(nodeDomain);
		}*/

		//Called only when the CLR doesn't find an assembly using sandard paths
		private static System.Reflection.Assembly ResolveLibs_old(object sender, ResolveEventArgs args){

			Console.WriteLine("Trying to resolve required dependency '"+args.Name+"'");
			System.Reflection.Assembly wantedAssembly = null;
			System.Reflection.Assembly objExecutingAssemblies;
			string strTempAssmbPath="";
			
			objExecutingAssemblies = System.Reflection.Assembly.GetExecutingAssembly();
			System.Reflection.AssemblyName[] arrReferencedAssmbNames = objExecutingAssemblies.GetReferencedAssemblies();
			string libsPath = Path.GetDirectoryName(
				System.Reflection.Assembly.GetExecutingAssembly().Location).Replace("bin"/*+Path.DirectorySeparatorChar+"Debug"*/, "lib");
			
			// first, try exact match
			foreach(System.Reflection.AssemblyName strAssmbName in arrReferencedAssmbNames){
				//Console.WriteLine("Matching  "+args.Name.Substring(0, args.Name.IndexOf(","))+" with compile-ref "+strAssmbName.FullName.Substring(0, strAssmbName.FullName.IndexOf(",")));
				Console.WriteLine("Matching  "+args.Name+" with compile-ref "+strAssmbName.FullName);
				string curAssembly = strAssmbName.FullName;
				if(curAssembly.IndexOf(",") >0)
					curAssembly = curAssembly.Substring(0, curAssembly.IndexOf(","));

				string argsName = args.Name;
				if(argsName.IndexOf(",") >0)
					argsName = argsName.Substring(0, argsName.IndexOf(","));

				if(curAssembly == argsName){
					strTempAssmbPath= libsPath+Path.DirectorySeparatorChar+args.Name.Substring(0, args.Name.IndexOf(","))+".dll";
					break;
				}
			}
			if(strTempAssmbPath == ""){
				//Logger.Append(Severity.NOTICE, "Could not found wanted assembly '"+args.Name+"', will ignore.");
				return null;
			}
			//try{
				wantedAssembly = System.Reflection.Assembly.LoadFile(strTempAssmbPath);	
			Console.WriteLine("Required dependency '"+args.Name+"' found.");
				//Logger.Append(Severity.INFO, "Loaded library "+args.Name);
			//}
			/*catch(Exception e){
				//Logger.Append(Severity.WARNING, "Could not load library '"+args.Name+"' from path '"+strTempAssmbPath+"' : "+e.Message);
			}*/
			return wantedAssembly;			
		}



		/*private static void SendUdpMessage(string message, IPEndPoint endpoint){

			Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			string wakeup =  message;
			byte[] msg = System.Text.Encoding.ASCII.GetBytes(wakeup);
			sock.SendTo(msg, endpoint);
		}*/
		
	}

}
