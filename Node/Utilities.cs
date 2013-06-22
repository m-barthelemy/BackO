using System;
using System.IO;
//using System.Xml.Serialization;
using System.Collections.Specialized;
using System.Net.Sockets;
using System.Net.Security;
using System.Text;
using System.Reflection;
using System.Configuration;
using System.Runtime.InteropServices;
using P2PBackup.Common;
using System.Diagnostics;
using Alphaleonis.Win32.Security;

namespace Node.Utilities{

	//public enum Severity{CRITICAL, ERROR, WARNING, INFO, DEBUG, TRIVIA};
	
	public class Logger	{
		
		private static Logger _instance;
		private static Session callingSession;
		private static readonly object ConsoleLock = new object();
		static StreamWriter SW;
		private static bool logToConsole = false;
		private static bool logToFile = false;
		private static bool logToSyslog = false;
		//[ThreadStatic]
		internal static Severity MinSeverity;
		 
		private Logger (){
			if(ConfigManager.GetValue("Logger.LogToConsole") != null && ConfigManager.GetValue("Logger.LogToConsole").ToLower() == "true")
				logToConsole = true;
			if(!string.IsNullOrEmpty(ConfigManager.GetValue("Logger.LogFile")))
				logToFile = true;
			if(ConfigManager.GetValue("Logger.Syslog") != null && ConfigManager.GetValue("Logger.Syslog").ToLower() == "true")
				logToSyslog = true;
			if(logToFile){
				try{
					SW=File.AppendText(ConfigManager.GetValue("Logger.LogFile"));
					SW.AutoFlush = true;
				}
				catch(Exception e){
					logToFile = false;
					Console.WriteLine ("ERROR : Could not log to file "+ConfigManager.GetValue("Logger.LogFile")+" : "+e.Message);
				}
			}
			 
			Severity configuredSeverity = Severity.INFO;
			Enum.TryParse<Severity>(ConfigManager.GetValue("Logger.Level"), out configuredSeverity);
			MinSeverity = configuredSeverity;
		}
		/*internal Logger(Session s){
			callingSession = s;
			if(_instance == null)
				_instance = new Logger();
			
		}*/
		
		
		
		static public void Append(Severity severity, string message){
			//if(_instance == null)
			//	_instance = new Logger();
			//SW.WriteLine(operation+";"+name+";"+transfer+";"+ message + ";"+Codes.GetDescription(message));
			if(_instance == null)
				_instance = new Logger();
			string caller = "";
			if((int)MinSeverity >= (int)Severity.DEBUG){
				StackTrace stackTrace = new StackTrace();
				caller = stackTrace.GetFrame(1).GetMethod().ReflectedType.Name+"."+ stackTrace.GetFrame(1).GetMethod().Name;
				if(string.IsNullOrEmpty(caller))
					caller = stackTrace.GetFrame(2).GetMethod().ReflectedType.Name+"."+ stackTrace.GetFrame(2).GetMethod().Name;
			}
			if((int)severity <= (int)MinSeverity){
				string logLine = DateTime.Now.ToString("hh:mm:ss") +"; "+ severity.ToString ().PadRight(8)+"; "+caller+"; "+message;
				if(logToConsole){ // TODO : will we keep colourized output or just end up writing logline?
					lock(ConsoleLock){
						Console.Write(DateTime.Now.ToString("hh:mm:ss") +"; ");
						switch(severity){
						case Severity.CRITICAL:
							Console.ForegroundColor = ConsoleColor.Red;
							break;
						case Severity.ERROR:
							Console.ForegroundColor = ConsoleColor.DarkRed;
							break;
						case Severity.WARNING:
							Console.ForegroundColor = ConsoleColor.Yellow;
							break;
						case Severity.NOTICE:
							Console.ForegroundColor = ConsoleColor.Cyan;
							break;
						case Severity.INFO:
							Console.ForegroundColor = ConsoleColor.DarkGreen;
							break;
						default: 
							Console.ForegroundColor = ConsoleColor.White;
							break;
						}
						Console.Write (severity.ToString().PadRight(8));
						Console.ForegroundColor = ConsoleColor.Gray;
						Console.Write ("; "+caller+"; "+message+Environment.NewLine);
					}
				}
				if(logToFile)
					try{
						SW.WriteLine(logLine);
					}
					catch(Exception _e){
						Console.WriteLine("Logger : unable to write to '"+ConfigurationManager.AppSettings["Logger.Logfile"]+"' : "+_e.Message);
						/*if(logToSyslog){
							Mono.Unix.Native.Syscall.syslog(Mono.Unix.Native.SyslogLevel.LOG_ALERT, "Could not write to log file : "+_e.Message);
						}*/
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
	
	internal class SessionLogger{
		
		private Session callingSession;
		
		internal SessionLogger(Session s){
			callingSession = s;
		}
		
		internal void Log(Severity severity,  string message){
			if((int)severity <= (int)Logger.MinSeverity/*(int)Enum.Parse(typeof(Severity), ConfigManager.GetValue("Logger.Level"))*/){
				//string caller = "";
				/*if((int)severity >= (int)Severity.DEBUG){
					StackTrace stackTrace = new StackTrace();
					Type callerType = stackTrace.GetFrame(1).GetMethod().ReflectedType;
					if(callerType is SessionLogger)
						caller = stackTrace.GetFrame(2).GetMethod().ReflectedType.Name+"."+ stackTrace.GetFrame(2).GetMethod().Name;
					else
						caller = stackTrace.GetFrame(1).GetMethod().ReflectedType.Name+"."+ stackTrace.GetFrame(1).GetMethod().Name;
				}*/
				//string logLine = null;
			
				if(callingSession == null)
					message = "#<NULL SESSION># ;"+message;
				else
					message = callingSession.Id+": "+callingSession.FromNode.Id+" --> #"+callingSession.ToNode.Id+" ("+callingSession.ToNode.IP+":"+callingSession.ToNode.ListenPort+");"+message;
					
				Logger.Append(severity, message);
			}
		}
	}
	
	public class PlatForm{
		public string  NodeVersion;
		private static PlatForm _instance;
		private string oS;
		private string runtime;
		
		public string OS {
			get {return this.oS;}
		}
		
		public string Runtime{
			get{ return runtime;}	
		}
		
		private PlatForm(){
			oS = GetOS();
			Type monoRuntimeType;
			MethodInfo getDisplayNameMethod;
			if ((monoRuntimeType = typeof(object).Assembly.GetType("Mono.Runtime")) != null && (getDisplayNameMethod = monoRuntimeType.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.ExactBinding, null, Type.EmptyTypes, null)) != null)
      			runtime = "Mono "+(string)getDisplayNameMethod.Invoke(null, null);
			else
				runtime = ".Net "+Environment.Version.ToString();
			//Version myVersion = Assembly.GetExecutingAssembly().GetName().Version;
			//NodeVersion = myVersion.Major+"."+myVersion.Minor+"."+myVersion.Revision;

			Assembly asm = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(asm.Location);
			NodeVersion = fvi.FileVersion;//+"-"+fvi.FilePrivatePart; //.ProductMajorPart+"."+fvi.ProductMinorPart+"."+fvi.ProductBuildPart;
			//myVersion.
		}
		
		public static PlatForm Instance(){
			if(_instance == null)
				_instance = new PlatForm();
			return _instance;
		}

		internal static bool IsUnixClient(){
			int p = (int) Environment.OSVersion.Platform;
			
            if ((p == 4) || (p == 6) || (p == 128)) {
            	return true;
            } else {
            	return false;
            }
		}
		
		private  string GetOS(){
			if(IsUnixClient()){
				ProcessStartInfo pi = new ProcessStartInfo("uname", "-s");
				pi.RedirectStandardOutput = true;
				pi.UseShellExecute = false;
				Process p = Process.Start(pi);
				p.WaitForExit();
				return p.StandardOutput.ReadToEnd().Replace(Environment.NewLine, String.Empty);
			}
			else
				return "NT"+Environment.OSVersion.Version.Major+"."+Environment.OSVersion.Version.Minor;
		}
	}
	
	public class Codes{
		
		private Codes(){}
		
		//static public string ClientVersion = "0.7";
		static public string GetDescription(string code){
			string desc;
			switch(code){
				//Successes
				case "201":
					desc = "Authentication client-hub succedeed.";
					break;
				case "202":
					desc = "Ok to begin backup.";
					break;
				case "204":
					desc = "Verification of peer client succedeed.";
					break;
				case "208":
					desc = "Successfully added new storage space.";
					break;
				case "209":
					desc = "Received public key.";
					break;
				case "211":
					desc = "Ok to send file.";
					break;
				case "213":
					desc = "Storage node accepted authentication.";
					break;
				case "215":
					desc = "New backup unit  successfully processed..";
					break;
				// Errors
				case "403":
					desc = "Not enough storage space for this backup.";
					break;
				case "406":
					desc = "No storage space available. All storage nodes are full, or no storage node (appart from client) is online.";
					break;
				case "505":
					desc = "Backup unit doesn't exist on storage node.";
					break;
				case "506":
					desc = "Wrong key received from client, authentication refused.";
					break;
				// Infos
				case "ACS":
					desc = "Added space from newly connected storage node.";
					break;
				case "SSS":
					desc = "Received order from hub to share storage space";
					break;
				default :
					desc = "<No description available for this code>";
					break;
				
				
			}
			return desc;
		}
	}
	
	/*
	
	internal class PathBrowser:IPathBrowser{
		internal string Browse(string path){
			Logger.Append(Severity.DEBUG, "User.Browse", "starting browsing "+path);
			string dXml ="";
			dXml += "<root d=\"/"+null+"\">";
			try{
				if(Utilities.PlatForm.IsUnixClient()){
					UnixDriveInfo[] drives = UnixDriveInfo.GetDrives();	
					string[] pdi = Directory.GetDirectories(path);
					
					foreach(string d in pdi){
						bool found = false;
						foreach(UnixDriveInfo udi in drives){
							if(udi.Name == Path.Combine(path,d)){
								found = true;
								if(udi.DriveType == UnixDriveType.Fixed)
									dXml += "<d n=\""+d.Substring(d.LastIndexOf('/')+1)+"\" type=\"v\" label=\""+udi.VolumeLabel+"\" size=\""+udi.TotalSize+"\" avail=\""+udi.AvailableFreeSpace+"\" fs=\""+udi.DriveFormat+"\"></d>";
								else if(udi.DriveType == UnixDriveType.Network)
									dXml += "<d n=\""+d.Substring(d.LastIndexOf('/')+1)+"\" type=\"n\" size=\""+udi.TotalSize+"\" avail=\""+udi.AvailableFreeSpace+"\" fs=\""+udi.DriveFormat+"\"></d>";
								else if(udi.DriveType == UnixDriveType.NoRootDirectory)
									dXml += "<d n=\""+path+"/"+d.Substring(d.LastIndexOf('/')+1)+"\" type=\"d\"></d>";
								else if(udi.DriveType == UnixDriveType.Unknown)
									dXml += "<d n=\""+d.Substring(d.LastIndexOf('/')+1)+"\" type=\"v\" label=\""+udi.VolumeLabel+"\" size=\""+udi.TotalSize+"\" avail=\""+udi.AvailableFreeSpace+"\" fs=\""+udi.DriveFormat+"\"></d>";
							}
						}
						if(found == false)
							dXml += "<d n=\""+d.Substring(d.LastIndexOf('/')+1)+"\"></d>";
					}
				}
				else{		// windows			
					Console.WriteLine("not running on unix - reduced features set for now.");
					string[] pdi = Directory.GetDirectories(path);
					foreach(string d in pdi)
						dXml += "<d n=\""+d.Substring(d.LastIndexOf('/')+1)+"\"></d>";
				}
				
			}
			catch(Exception e){
				Logger.Append(Severity.WARNING, "Utilities.Browse", "Couldn't brose directory "+path+", error :"+e.Message );	
			}
			finally{
				dXml += "</root>";
				Logger.Append(Severity.TRIVIA, "Utilities.Browse", "Asked to browse "+path+", sending "+dXml);
			}
			return dXml;
		}
	}
	*/
	internal class Storage{
	
		internal static bool MakeStorageDirs(){
			string storagePath = ConfigManager.GetValue("Storage.StoragePath");
			if(storagePath == null || storagePath == String.Empty){
				Logger.Append(Severity.INFO, "Storage directory is not configured, this node will not be a storage node");
				return false;	
			}	
			int createNeeded = 0;
			int depth = 0;
			try{
				MakeStorageDirs(ref storagePath, ref depth, ref createNeeded);
			}
			catch(Exception e){
				Logger.Append(Severity.CRITICAL, "Error while trying to create storage subdirectories inside "+storagePath+" : "+e.Message);
				return false;
			}
			if(createNeeded == 0) Logger.Append(Severity.DEBUG, "Storage subdirectories already exist");
			else Logger.Append(Severity.INFO, "Created "+createNeeded+" storage directories");
			return true;
		}
		
		private static void MakeStorageDirs(ref string storagePath, ref int depth, ref int createNeeded){
			string[] storageSubdirList = { "A", "B", "C", "D", "E", "F", "0", "1",
										   "2", "3", "4", "5", "6", "7", "8", "9" };
			if(depth == 3){
				return;
			}
			//try{
				foreach (string c in storageSubdirList){
					string currentPath = Path.Combine(storagePath, c);
					if(!Directory.Exists(currentPath)){
				   		Directory.CreateDirectory(currentPath);
						createNeeded ++;
					}
					depth++;
					MakeStorageDirs(ref currentPath, ref depth, ref createNeeded);
					depth--;
				}
			//}
			//catch(Exception e){
					
			//	return false;
			//}
			//return true;
		}
		
		internal static string GetStoragePath(string chunkName){
			return 	ConfigManager.GetValue("Storage.StoragePath") + Path.DirectorySeparatorChar + chunkName.Substring(0, 1)
				+ Path.DirectorySeparatorChar + chunkName.Substring(2, 1)
				+ Path.DirectorySeparatorChar + chunkName;
		}
		
	}
	
	internal static class ConfigManager{

		private static NameValueCollection config = new NameValueCollection();
		//private static string configFilePath;

		
		internal static void BuildConfFromHub(NodeConfig conf){
			//System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

			/*string[] pairs = rawConf.Split('|');
			foreach (string pair in pairs){
				string[] keyValue = pair.Split('=');
				if(keyValue.Length == 2)
					SetValue(keyValue[0], keyValue[1]);
				//ConfigurationManager.AppSettings.Set(keyValue[0], keyValue[1]);
				//config.AppSettings.Settings[keyValue[0]] = keyValue[1];
			}*/
			SetValue("Storage.IndexPath", conf.IndexPath);
			SetValue("Storage.StoragePath", conf.StoragePath);
			SetValue("Storage.StorageSize", conf.StorageSize.ToString());
			SetValue("Storage.ListenIP", conf.ListenIP);
			SetValue("Storage.ListenPort", conf.ListenPort.ToString ());
			SetValue("Logger.Level", conf.LogLevel.ToString());
			SetValue("Logger.LogFile", conf.LogFile);
			SetValue("Logger.LogToSyslog", conf.LogToSyslog.ToString ());

			Logger.Append(Severity.INFO, "Received configuration.");
			if(Environment.GetEnvironmentVariable("NO_FILEOPTIMIZATION") != null ){
				Logger.Append(Severity.INFO, " ** NO_FILEOPTIMIZATION : Asked to use non-optimized file access");
				config.Add("OPTIMIZED_FILE_ACCESS", "false");
			}
			else
				config.Add("OPTIMIZED_FILE_ACCESS", "true");
			if(Environment.GetEnvironmentVariable("BENCHMARK") != null ){
				config.Add("BENCHMARK", "true");
				Logger.Append(Severity.INFO, " ** BENCHMARK : Asked to start in benchmark (no real data transferring) mode");
			}
			if(Environment.GetEnvironmentVariable("STDSOCKBUF") != null ){
				config.Add("STDSOCKBUF", "true");
				Logger.Append(Severity.INFO, " ** STDSOCKBUF : Asked to process tasks using default network buffer sizes");
			}
			
			//Save();
		}
		
		/*internal static void Save(){
			StreamWriter fileWriter = new StreamWriter(Path.GetDirectoryName("./")+"shbc.conf", true);
			try	{
				XmlSerializer serializer =  new XmlSerializer(typeof(System.Collections.Specialized.NameValueCollection));
			
				using (fileWriter){
					serializer.Serialize(fileWriter.BaseStream, config);
				}
			}
			catch (Exception ex){
				Logger.Append(Severity.ERROR, "ConfigManager.Save", ex.Message);
			}
		}*/
			
		/*internal static void Load(string filePath){
				configFilePath = filePath;
		}*/
		
		/// <summary>
		/// Gets a configuration value, be it a command-line passed value, a config file read value, or a hub received value.
		/// </summary>
		/// <returns>
		/// The value.
		/// </returns>
		/// <param name='key'>
		/// Key.
		/// </param>
		internal static string GetValue(string key){
			// we first search the config items received from hub or command-line to avoid security vulenrabilities 
			// (eg. someone overrides a hub-managed parameter in the node's config file)
			return 	config[key] == null? ConfigurationManager.AppSettings[key]:config[key];
		}
		
		internal static void SetValue(string key, string value){
			if(config[key] == null)
				config.Add(key, value);
			else
				config[key] = value;
		}
	}
	
	// StateObject is the structure used to keep state between async receives (communication with hub)
	internal class StateObject {
		// sslstream
		public SslStream stream;
		// Client socket.
		public Socket workSocket = null;
		// Size of receive buffer.
		public const int BufferSize = 8192;
		    // Receive buffer.
		public byte[] buffer = new byte[BufferSize];
	}
	
	public class Utils{
		
		public static long GetLocalUnixTime(){
			TimeSpan span = (DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Local).ToLocalTime());
			return (long)span.TotalSeconds;
		}	

		public static long GetUtcUnixTime(){
			TimeSpan span = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
			return (long)span.TotalSeconds;
		}	
		
		public static DateTime GetLocalDateTimeFromUnixTime(long utcUnixTime){
			try{
			return (new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).AddSeconds(utcUnixTime).ToLocalTime();
			}
			catch{
				return DateTime.MaxValue;	
			}
		}
		
		public static long GetUtcUnixTime(DateTime dt){
			TimeSpan span = (dt.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
			return (long)span.TotalSeconds;
		}
		
		public static void SetProcInfo(string info){
			if(Utilities.PlatForm.Instance().OS == "Linux")
				try{
					prctl (15/* PR_SET_NAME */, info,IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
				}catch{}
				
			else if (Utilities.PlatForm.Instance().OS == "FreeBSD" || Utilities.PlatForm.Instance().OS == "SunOS")
				try{
					setproctitle (Encoding.ASCII.GetBytes ("%s\0"), Encoding.ASCII.GetBytes (info + "\0"));
				}catch{}
		}
		
		[DllImport ("libc")] // Linux
		private static extern int prctl (int option, string/*byte []*/ arg2, IntPtr arg3, IntPtr arg4, IntPtr arg5);
		[DllImport ("libc")] // BSD
		private static extern void setproctitle (byte [] fmt, byte [] str_arg);
	}
	
	internal class PrivilegesManager{
		PrivilegeEnabler priv; 
		
		internal void Grant(){
			if(!Utilities.PlatForm.IsUnixClient()){
				priv = new Alphaleonis.Win32.Security.PrivilegeEnabler(Privilege.Backup, Privilege.Restore, Privilege.ManageVolume);
				//see if this proves stable and reliable on all NT platforms
				int sec = Node.Misc.NativeMethods.CoInitializeSecurity(IntPtr.Zero, -1, IntPtr.Zero, IntPtr.Zero, Node.Misc.RpcAuthenticationLevels.None,
      				Node.Misc.RpcImpersonationLevels.Impersonate, IntPtr.Zero, Node.Misc.EoAuthenticationCapabilities.None, IntPtr.Zero);	
				Logger.Append( Severity.DEBUG, "CoInitializeSecurity() returned "+sec);
			}
		}
		
		internal void Revoke(){
			if(priv != null){
				priv.Dispose();
			}
		}
	}
	
	
}

