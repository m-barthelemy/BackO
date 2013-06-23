using System;
using System.Data;
using System.IO;
using System.Net;
using System.Threading;
using Node.Utilities;
//using System.ServiceProcess;
using System.Configuration;
//using System.Configuration.Install;
using System.ComponentModel;
using System.Security.Principal;
using P2PBackup.Common;

namespace Node{
	
	//Exit/Return codes:
	// 0 : normal exit
	// 10: unexpected and unhandlable error (crash)
	[Serializable]
	public class Client  : /*System.ServiceProcess.ServiceBase ,*/ MarshalByRefObject, IClientNode{	


		private User user;
		//internal static string DefaultPubKey = @"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";
		// work under windows, not mono internal static string DefaultPubKey = @"MIIByjCCATOgAwIBAgIQMYvx8wD+t0G9DT0eT50jhzANBgkqhkiG9w0BAQUFADAbMRkwFwYDVQQDExBTaEJhY2t1cCBUZXN0IENBMB4XDTExMDYxNTE2NTc1NVoXDTM5MTIzMTE3NTk1OVowFTETMBEGA1UEAxMKbGludXgtYXgyZzCBnTANBgkqhkiG9w0BAQEFAAOBiwAwgYcCgYEArEMOt8A0dKYH6wQT7mf0PftZ2/qTUKQ+5S779gCDNUJCi6euzso63bJI/8aLvKIJYizfHS0XBLzSmV04R+WF4PJ8YzkTyQ59ordWBaAhbu9FiN35eU+yUUTNREkz+2+VkcGEWePuCsbw63Lyojnxoe8Na/UYJNTOsF+XPvvQO6cCARGjFzAVMBMGA1UdJQQMMAoGCCsGAQUFBwMBMA0GCSqGSIb3DQEBBQUAA4GBAH0kt316PCaU9NmL/ZKoDG8DXeqao7XhsccXkoYpc6RAp1Dj9HhBnmPAADDmFKb75CU4Cw2u8OaL/MNOHNA49nibnEvLq6xjAlOOoCSB44qtHiOk5mzql7xTyKPccioy6aLsSuKElf+luE+oYuEgqoTqxz0zI9raZD2iyPfbqAAn";
		// 3rd try
		internal static string DefaultPubKey = "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";

		public Client(){
			AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
			AppDomain.CurrentDomain.AssemblyResolve += ResolveLibs;

			// Useful on Windows only. We need to elevate COM privileges to get VSS working properly (system writer in particular)
			PrivilegesManager pm = new PrivilegesManager();
			pm.Grant();
			// haven't received yet configuration from hub, we set a default value
			if(ConfigManager.GetValue("Logger.Level") == null)
				ConfigManager.SetValue("Logger.Level", "TRIVIA");
			Console.WriteLine("TEMP DEBUG : Client() instanciated");
			user = new User();
		}

		/// <summary>
		/// Login to the Hub and retrieve configuration.
		/// Returns an array of 2 IPEndPoints : the Hub ip and UDP port, the node's IP and UDP port
		/// these values are used to listen for udp PNG and WAKEUP messages
		/// </summary>
		/// <param name="args">Arguments.</param>
		public IPEndPoint[] Login(string[] args){
			Console.WriteLine("Client.cs Login() : args="+string.Join(",", args));
			ConfigManager.SetValue("Hub.Port", "52561");
			ConfigManager.SetValue("Security.CertificateFile", "certificate.pfx");
			
			ParseParams(args);

			WindowsIdentity runningAccount = WindowsIdentity.GetCurrent();
			
			if(Utilities.PlatForm.IsUnixClient()){
				if(runningAccount.Name != "root"){
					Console.WriteLine("Warning : insufficient privileges. Should be run as 'root'");
					Logger.Append(Severity.WARNING, "Insufficient privileges. Should be run as 'root'");
				}
			}
			else{
				WindowsPrincipal principal = new WindowsPrincipal (runningAccount);
				if(!principal.IsInRole(WindowsBuiltInRole.Administrator) 
				   && !principal.IsInRole(WindowsBuiltInRole.SystemOperator)
				   && !principal.IsInRole(WindowsBuiltInRole.BackupOperator)){
					
					//Console.WriteLine("ERROR : insufficient privileges. Administrator/System/BackupOperator account is required.");
					Logger.Append(Severity.ERROR, "Insufficient privileges. Administrator/System/BackupOperator account is required.");
				}
			}
			if(Utilities.ConfigManager.GetValue("Hub.IP") == null || Utilities.ConfigManager.GetValue("Hub.Port") == null
			   || Utilities.ConfigManager.GetValue("Security.CertificateFile") == null){
				Console.WriteLine("Some parameters are missing."+Environment.NewLine);
				PrintHelp();
			}


			Logger.Append(Severity.INFO, "### Starting Node. Current date:"+DateTime.Now.ToString()+", Version: "+Utilities.PlatForm.Instance().NodeVersion
			      +", OS: "+Utilities.PlatForm.Instance().OS+", Runtime: "+Utilities.PlatForm.Instance().Runtime);
			Logger.Append(Severity.DEBUG, "Neither connected nor running");
			bool hasCert = false;
			if(File.Exists(ConfigManager.GetValue("Security.CertificateFile")))
				hasCert = true;
			if(user.ConnectToHub(hasCert)){
				if(!hasCert){
					user.CertificateGeneratedEvent.WaitOne();
					user.Disconnect(true, false);
					user = null;
					Login(args);
				}
			}
			else{
				throw new Exception("cannot connect to Hub : ");
			}

			while(user.Run){
				Thread.Sleep(10000);	
				if(user.Status == NodeStatus.Idle && user.LastReceivedAction.AddMinutes(2) < DateTime.Now){
					Logger.Append(Severity.INFO, "Inactive for more than 2 minutes, unloading... node status="+user.Status+", last received action was "+user.LastReceivedAction);
					Stop(true);
					var udpEndPoints = new System.Net.IPEndPoint[2];
					Console.WriteLine("will use ip '"+Utilities.ConfigManager.GetValue("Hub.IP")+"' as hub udp endpoint");
					udpEndPoints[0] = new IPEndPoint(
						IPAddress.Parse(
						Utilities.ConfigManager.GetValue("Hub.IP")), 
						int.Parse(Utilities.ConfigManager.GetValue("Hub.Port"))
					);
					udpEndPoints[1] = new IPEndPoint(IPAddress.Any, user.ListenPort );
					return udpEndPoints;
				}
			}
			return null;

		}

		public void Stop(bool goingIdle){
			user.Disconnect(false, goingIdle);
		}


	
		/*[STAThread]
		static void Main(string[] args){
			System.ServiceProcess.ServiceBase[] ServicesToRun; 

			ServicesToRun = new System.ServiceProcess.ServiceBase[] { new Client() }; 
			System.ServiceProcess.ServiceBase.Run(ServicesToRun); 
			
		
		}*/


		
		/*static void Main(string[] args){

		//protected override void OnStart(string[] args){

			// default values...
			ConfigManager.SetValue("Hub.Port", "52561");
			ConfigManager.SetValue("Security.CertificateFile", "certificate.pfx");

			int param = 0;
			while(param < args.Length){
				string currentParam = args[param];
				switch (currentParam.ToLower()){
					case "-h": case "--help":
						PrintHelp();
						return;
					case "--hubip": case "-i":
						ConfigManager.SetValue("Hub.IP", args[param+1]);
						break;
					case "--hubport": case "-p":
						ConfigManager.SetValue("Hub.Port", args[param +1]);
						break;
					case "--cert": case "--certificate": case "-c":
						ConfigManager.SetValue("Security.CertificateFile", args[param +1]);
						break;
					case "--logfile": case "--log": case "-l":
						ConfigManager.SetValue("Logger.LogFile", args[param +1]);
						break;
					case "--debug": case "-d":
						ConfigManager.SetValue("Logger.Level", "TRIVIA");
						ConfigManager.SetValue("Logger.LogToConsole", "true");
						break;
					
					case "--install":
						SaveConfig();
						//new Installer1(); 
						break;

					// ""hidden"" or advanced options:
					case "--dump-ddb":
						Node.Utilities.ConfigManager.SetValue("Storage.IndexPath", args[param +1]);
						Node.DataProcessing.DedupIndex di = Node.DataProcessing.DedupIndex.Instance(0, false);
						long countItems = di.DebugDump();
						Console.WriteLine("total  : "+countItems+" items.");
						Environment.Exit(0);
						break;

					default : 
						Console.WriteLine("Unrecognized option : "+currentParam+Environment.NewLine);
						PrintHelp();
						return;
				}
				param = param +2;
			}

			WindowsIdentity runningAccount = WindowsIdentity.GetCurrent();
			
			if(Utilities.PlatForm.IsUnixClient()){
				if(runningAccount.Name != "root"){
					Console.WriteLine("Warning : insufficient privileges. Should be run as 'root'");
					Logger.Append(Severity.WARNING, "Insufficient privileges. Should be run as 'root'");
				}
			}
			else{
				WindowsPrincipal principal = new WindowsPrincipal (runningAccount);
	   			if(!principal.IsInRole(WindowsBuiltInRole.Administrator) 
					|| !principal.IsInRole(WindowsBuiltInRole.SystemOperator)
					|| !principal.IsInRole(WindowsBuiltInRole.BackupOperator)){
				   
					//Console.WriteLine("ERROR : insufficient privileges. Administrator/System/BackupOperator account is required.");
					Logger.Append(Severity.ERROR, "Insufficient privileges. Administrator/System/BackupOperator account is required.");
					//return ;
				}
			}
			if(Utilities.ConfigManager.GetValue("Hub.IP") == null || Utilities.ConfigManager.GetValue("Hub.Port") == null
				|| Utilities.ConfigManager.GetValue("Security.CertificateFile") == null){
				Console.WriteLine("Some parameters are missing."+Environment.NewLine);
			   PrintHelp();
			}
			else{
				try{
					Client c = new Client();
					c.Login(args);
				}
				catch(Exception e){
					Console.WriteLine ("Unable to start client : "+e.ToString());	
					if(e.InnerException != null)
						Console.WriteLine (""+e.InnerException.ToString());
				}
			}
		}*/

		private void ParseParams(string[] args){
			int param = 0;
			while(param < args.Length){
				string currentParam = args[param];
				switch (currentParam.ToLower()){
				case "-h": case "--help":
					PrintHelp();
					return;
				case "--hubip": case "-i":
					ConfigManager.SetValue("Hub.IP", args[param+1]);
					break;
				case "--hubport": case "-p":
					ConfigManager.SetValue("Hub.Port", args[param +1]);
					break;
				case "--cert": case "--certificate": case "-c":
					ConfigManager.SetValue("Security.CertificateFile", args[param +1]);
					break;
				case "--logfile": case "--log": case "-l":
					ConfigManager.SetValue("Logger.LogFile", args[param +1]);
					break;
				case "--debug": case "-d":
					ConfigManager.SetValue("Logger.Level", "TRIVIA");
					ConfigManager.SetValue("Logger.LogToConsole", "true");
					break;
					
				case "--install":
					SaveConfig();
					//new Installer1(); 
					break;
					
					// ""hidden"" or advanced options:
				case "--dump-ddb":
					Node.Utilities.ConfigManager.SetValue("Storage.IndexPath", args[param +1]);
					Node.DataProcessing.DedupIndex di = Node.DataProcessing.DedupIndex.Instance(0, false);
					long countItems = di.DebugDump();
					Console.WriteLine("total  : "+countItems+" items.");
					Environment.Exit(0);
					break;
					
				default : 
					Console.WriteLine("Unrecognized option : "+currentParam+Environment.NewLine);
					PrintHelp();
					return;
				}
				param = param +2;
			}
		}

		/// <summary>
		/// Traps any unhandled exception. We try to report it to Hub, then save it through logger, and then shutdown
		/// </summary>
		/// <param name='sender'>
		/// Sender.
		/// </param>
		/// <param name='e'>
		/// E.
		/// </param>
		private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e){
			try{
				Logger.Append(Severity.CRITICAL, "Unhandlable error : "+e.ExceptionObject);	
				User.SendEmergency(e.ExceptionObject.ToString());
			}catch{}
    		Console.WriteLine("Unhandlable error : "+e.ExceptionObject);
			Environment.Exit(10);
		}

		//Called only when the CLR doesn't find an assembly using sandard paths
		private static System.Reflection.Assembly ResolveLibs(object sender, ResolveEventArgs args){

			Logger.Append(Severity.TRIVIA, "Trying to resolve required dependency '"+args.Name+"'");
			System.Reflection.Assembly wantedAssembly = null;
			System.Reflection.Assembly objExecutingAssemblies;

			objExecutingAssemblies = System.Reflection.Assembly.GetExecutingAssembly();
			System.Reflection.AssemblyName[] arrReferencedAssmbNames = objExecutingAssemblies.GetReferencedAssemblies();
			string binsPath = Path.GetDirectoryName(
				System.Reflection.Assembly.GetExecutingAssembly().Location).Replace("lib"/*+Path.DirectorySeparatorChar+"Debug"*/, "bin");
			string libsPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

			// first, try exact match
			/*foreach(System.Reflection.AssemblyName strAssmbName in arrReferencedAssmbNames){
				//Console.WriteLine("Matching  "+args.Name.Substring(0, args.Name.IndexOf(","))+" with compile-ref "+strAssmbName.FullName.Substring(0, strAssmbName.FullName.IndexOf(",")));
				//Console.WriteLine("Matching  "+args.Name+" with compile-ref "+strAssmbName.FullName);
				if(strAssmbName.FullName.Substring(0, strAssmbName.FullName.IndexOf(",")) == args.Name.Substring(0, args.Name.IndexOf(","))){
					strTempAssmbPath= libsPath+Path.DirectorySeparatorChar+args.Name.Substring(0, args.Name.IndexOf(","))+".dll";
					break;
				}
			}
			if(strTempAssmbPath == ""){
				Logger.Append(Severity.NOTICE, "Could not found wanted assembly '"+args.Name+"', will ignore.");
				return null;
			}
			try{
				wantedAssembly = System.Reflection.Assembly.LoadFrom(strTempAssmbPath);	
				Logger.Append(Severity.INFO, "Loaded library "+args.Name);
			}
			catch(Exception e){
				Logger.Append(Severity.WARNING, "Could not load library '"+args.Name+"' from path '"+strTempAssmbPath+"' : "+e.Message);
			}
			return wantedAssembly;*/
			string argsName = args.Name;
			if(argsName.IndexOf(",") >0)
				argsName = argsName.Substring(0, argsName.IndexOf(","));

			string wantedLibAssemblyPath = System.IO.Path.Combine(libsPath, argsName+".dll");
			string wantedBinAssemblyPath = System.IO.Path.Combine(binsPath, argsName+".exe");
			Console.WriteLine("probable lib assembly path : "+wantedLibAssemblyPath);	
			Console.WriteLine("probable bin assembly path : "+wantedBinAssemblyPath);	
			if(File.Exists(wantedLibAssemblyPath))
				return System.Reflection.Assembly.LoadFile(wantedLibAssemblyPath);	
			else if(File.Exists(wantedBinAssemblyPath))
				return System.Reflection.Assembly.LoadFile(wantedBinAssemblyPath);	
			else{
				Logger.Append(Severity.NOTICE, "Could not find library '"+args.Name+"' inside '"+binsPath+"' or '"+binsPath+"' paths");
				return null;

			}
		}

		private static void PrintHelp(){
			Console.WriteLine("Node version "+Utilities.PlatForm.Instance().NodeVersion);	
			Console.WriteLine("Usage : Node.exe --hubip <ip_address> --hubport <port> --cert </path/to/certificate.pfx> [--debug]");
			Console.WriteLine(" --hubip, -i : \t\tHub hostname or IP address. If using clustering, specify multiple values separated by ';'");
			Console.WriteLine(" --hubport, -p : \tHub(s) port.");
			Console.WriteLine(" --cert, -c : \t\tCertificate file (complete path)");
			Console.WriteLine(" [--log, -l ]: \t\tSend log messages to specified file");
			Console.WriteLine(" [--debug, -d ]: \tTurn on debug/trace messages in logfile and console.");
			Console.WriteLine(" --install : \t\tSave configuration values (-i, -p, -c) to avoid entering them each time Node is started.");
		}
		
		/// <summary>
		/// Updates/Saves the config added or modified using the --install option.
		/// </summary>
		private static void SaveConfig(){
			System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			config.AppSettings.Settings.Remove("Hub.Port");
			config.AppSettings.Settings.Add("Hub.Port", ConfigManager.GetValue("Hub.Port"));
			config.AppSettings.Settings.Remove("Security.CertificateFile");
			config.AppSettings.Settings.Add("Security.CertificateFile", ConfigManager.GetValue("Security.CertificateFile"));
			config.AppSettings.Settings.Remove("Logger.LogFile");
			config.AppSettings.Settings.Add("Logger.LogFile", ConfigManager.GetValue("Logger.LogFile"));
			config.AppSettings.Settings.Remove("Hub.IP");
			config.AppSettings.Settings.Add("Hub.IP", ConfigManager.GetValue("Hub.IP"));
			config.Save(System.Configuration.ConfigurationSaveMode.Modified);
			ConfigurationManager.RefreshSection("appSettings");
			Console.WriteLine("Configuration saved.");
		}
	}
	
	/*[RunInstaller(true)] 
	public  class Installer1 : Installer{
        private ServiceInstaller serviceInstaller;
        private ServiceProcessInstaller processInstaller;

        public Installer1():base() {
            //InitializeComponent();
            processInstaller = new ServiceProcessInstaller();
            serviceInstaller = new ServiceInstaller();

            // Service will run under system account
            processInstaller.Account = ServiceAccount.LocalSystem;
            // Service will have Start Type of Manual
            serviceInstaller.StartType = ServiceStartMode.Manual;
            serviceInstaller.ServiceName = "SharpBackup";
			serviceInstaller.Description = "SharpBackup Client and Storage node. (v"+Utilities.PlatForm.Instance().NodeVersion+")";
			
			InstallContext c = new InstallContext();
			Console.WriteLine ("assembly location : "+this.GetType().Assembly.Location);
			processInstaller.Context.Parameters.Add("assemblypath",this.GetType().Assembly.Location);
			//c.Parameters.Add ("hubip", "192.168.0.10");
			//c.Parameters.Add ("hubport", "52561");
			//c.Parameters.Add ("cert", @"c:\users\administrator\Node\bin\debug\certiticate.pfx");
			serviceInstaller.Context = c;
            Installers.Add(serviceInstaller);
            Installers.Add(processInstaller);
			System.Collections.Specialized.ListDictionary state = new System.Collections.Specialized.ListDictionary(); 
			serviceInstaller.Install(state); 
            
        }
	}*/
}
