using System;
using System.IO;
using System.Configuration;
using P2PBackup.Common;
using System.Reflection;
using System.ServiceModel;
using System.Diagnostics;

namespace SharpBackupWeb.Utilities{

	public class RemotingManager{
		
		static IRemoteOperations remote;
		static NetTcpBinding binding;
		static ChannelFactory<IRemoteOperations> cf;

		private RemotingManager ()	{
			binding = new NetTcpBinding(SecurityMode.None, true);
			binding.Security.Mode = SecurityMode.None;
			binding.OpenTimeout = new TimeSpan(1,0,0);
			binding.SendTimeout = new TimeSpan(1,0,0);
			binding.CloseTimeout = new TimeSpan(1,0,0);
			binding.ReceiveTimeout = new TimeSpan(1,0,0);
			binding.MaxBufferSize = 10000000;
			binding.MaxReceivedMessageSize = 10000000;
			binding.MaxBufferPoolSize = 10000000;
			binding.MaxConnections = 100;
		}
		
		internal static IRemoteOperations GetRemoteObject(){
			string hubIP = ConfigurationManager.AppSettings["Hub.IP"];
			string hubPort = ConfigurationManager.AppSettings["Hub.Port"];
			if(binding == null)
				new RemotingManager();
			var address = new EndpointAddress ("net.tcp://"+hubIP+":"+hubPort);
			cf = new ChannelFactory<IRemoteOperations> (binding, address);
			cf.Faulted += OnChannelFault;
			remote = cf.CreateChannel ();

			return remote;
		}

		private static void OnChannelFault(object sender, EventArgs e){
			Logger.Append(Severity.DEBUG, "Channel has entered faulted state : "+e.ToString());
			cf.Abort();
		}
	}
	
	public class Logger	{
		
		private static Logger _instance;
		static StreamWriter SW;
		private static bool logToConsole = false;
		private static bool logToFile = false;
		private static bool logToSyslog = false;
		internal static Severity configSev{get;private set;}
		 
		private Logger (){
			if(ConfigurationManager.AppSettings["Logger.LogToConsole"] != null)
				logToConsole = true;
			if(!string.IsNullOrEmpty(ConfigurationManager.AppSettings["Logger.LogFile"]))
				logToFile = true;
			if(ConfigurationManager.AppSettings["Logger.Syslog"] != null && ConfigurationManager.AppSettings["Logger.Syslog"].ToLower() == "true")
				logToSyslog = true;
			if(logToFile){
				try{
					SW=File.AppendText(ConfigurationManager.AppSettings["Logger.LogFile"]);
					SW.AutoFlush = true;
				}
				catch(Exception e){
					logToFile = false;
					Console.WriteLine ("ERROR : Could not log to file "+ConfigurationManager.AppSettings["Logger.LogFile"]+" : "+e.Message);
				}
			}
			configSev = Severity.INFO;
			try{
				configSev = (Severity)Enum.Parse(typeof(Severity), ConfigurationManager.AppSettings["Logger.Level"]);
			
			}
			catch{}
		}

		
		static public void Append(Severity severity, string message){
			//if(_instance == null)
			//	_instance = new Logger();
			//SW.WriteLine(operation+";"+name+";"+transfer+";"+ message + ";"+Codes.GetDescription(message));
			if(_instance == null)
				_instance = new Logger();
			string caller = "";
			if((int)configSev >= (int)Severity.DEBUG){
				StackTrace stackTrace = new StackTrace();
				caller = stackTrace.GetFrame(1).GetMethod().ReflectedType.Name+"."+ stackTrace.GetFrame(1).GetMethod().Name;
				if(string.IsNullOrEmpty(caller))
					caller = stackTrace.GetFrame(2).GetMethod().ReflectedType.Name+"."+ stackTrace.GetFrame(2).GetMethod().Name;
			}
			if((int)severity <= (int)configSev){
				string logLine = DateTime.Now.ToString("hh:mm:ss") +";"+severity+";"+caller+";"+message;
				if(logToConsole)
					Console.WriteLine(logLine);
				if(logToFile)
					try{
						SW.WriteLine(logLine);
					}
					catch(Exception _e){
						Console.WriteLine("Logger : unable to write to '"+ConfigurationManager.AppSettings["Logger.Logfile"]+"' : "+_e.Message);
					}
			}
		}
		
		static public void Reload(){
			Append (Severity.INFO, "Asked to restart Logger...");
			if(SW != null)
				SW.Close();
			_instance = null;	
		}
	}
}

