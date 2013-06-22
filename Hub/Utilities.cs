using System;
using System.IO;
using System.Configuration;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Runtime.InteropServices;
using P2PBackup.Common;

namespace P2PBackupHub.Utilities{
	
	
	public class Logger	{

		static StreamWriter SW;
		static Ring logRing;
		private static Logger _instance;
		private static bool logToFile;
		private static bool logToSyslog;
		private static bool logToConsole;
		private static Severity defaultSeverity;
		
		private Logger (){
				logRing = new Ring(200);
				logToConsole = false;
				logToSyslog = false;
				logToFile = false;
				defaultSeverity = Severity.INFO;
				if(ConfigurationManager.AppSettings["Logger.File"] != null)
					logToFile = bool.TryParse(ConfigurationManager.AppSettings["Logger.File"], out logToFile);
				if(ConfigurationManager.AppSettings["Logger.Syslog"] != null && ConfigurationManager.AppSettings["Logger.Syslog"].ToLower() == "true")
					logToSyslog = true;
				if(ConfigurationManager.AppSettings["Logger.Console"] != null && ConfigurationManager.AppSettings["Logger.Console"].ToLower() == "true")
					logToConsole = true;
				if(ConfigurationManager.AppSettings["Logger.Level"] != null)
					Enum.TryParse<Severity>((string)ConfigurationManager.AppSettings["Logger.Level"], out defaultSeverity);
					//defaultSeverity =  (int)Enum.Parse(typeof(Severity), ConfigurationManager.AppSettings["Logger.Level"]);
				if(logToFile){
					try{
						SW=File.AppendText(ConfigurationManager.AppSettings["Logger.Logfile"]);
						SW.AutoFlush = true;
					}
					catch(Exception e){
					Console.WriteLine("Could not open configured log file ("+ConfigurationManager.AppSettings["Logger.Logfile"]+") for writing : "+e.Message);
						logToFile = false;
					}
				}
				
		}

		
		static public void Append(string operation, Severity severity, string message){
			
			if(_instance == null)
				_instance = new Logger();
			
			if(severity > defaultSeverity)
				return;
			string caller = "";
			if((int)severity >= (int)Severity.DEBUG){
				StackTrace stackTrace = new StackTrace();
				caller = stackTrace.GetFrame(1).GetMethod().ReflectedType.Name+"."+ stackTrace.GetFrame(1).GetMethod().Name;
			}
			string logLine = DateTime.Now.ToString("MM/dd hh:mm:ss")+"; "+severity+";\t"+operation+"; "+caller+";  "+ message;
			if((int)severity < (int)Severity.DEBUG)
				logRing.Put(new LogEntry(DateTime.Now, severity, caller, message));
			if(logToFile)
				try{
					SW.WriteLine(logLine);
				}
				catch(Exception _e){
					Console.WriteLine("Logger : unable to write to '"+ConfigurationManager.AppSettings["Logger.Logfile"]+"' : "+_e.Message);
					if(logToSyslog){
						Mono.Unix.Native.Syscall.syslog(Mono.Unix.Native.SyslogLevel.LOG_ALERT, "Could not write to log file : "+_e.Message);
					}
				}
			if(logToConsole)
				Console.WriteLine(logLine);
			if(logToSyslog){
				Mono.Unix.Native.SyslogLevel s;
				switch(severity){
				
				case Severity.ERROR:	
					s = Mono.Unix.Native.SyslogLevel.LOG_ERR;
					break;
				case Severity.DEBUG:
					s = Mono.Unix.Native.SyslogLevel.LOG_DEBUG;
					break;
				case Severity.WARNING:
					s = Mono.Unix.Native.SyslogLevel.LOG_WARNING;
					break;
				case  Severity.INFO:
					s = Mono.Unix.Native.SyslogLevel.LOG_INFO;
					break;
				default:
					s = Mono.Unix.Native.SyslogLevel.LOG_NOTICE;
					break;
				}
				string sysLogLine = operation+";"+caller+";"+ message;
				Mono.Unix.Native.Syscall.syslog(s, sysLogLine);
			}
		}
		
		static public LogEntry[] GetBuffer(){
			return logRing.Get();	
		}
	}
	
	public class Ring{
		private LogEntry[] content;
		private int begining;
		private int size;
		public Ring(int size){
			this.size = size;
			content = new LogEntry[size];
			begining = 0;
		}
		
		public void Put(LogEntry entry){
			if(begining == this.size - 1)
				begining = 0;
			content[begining] = entry;
			begining++;
		}
		
		public LogEntry[] Get(){
			int index = 0;
			LogEntry[] values = new LogEntry[this.size];
			lock(content){
				int j=0;
				for(int i = 0; i<this.size; i++){
					if(begining + i > this.size - 1)
						index = this.size -i;
					else
						index = begining+i;
					if(content[index] != null){
						values[j] = content[index];
						j++;
					}
				}
			}
			return values;
		}
		
	}
	
	public class Codes{
		
		private Codes(){}
		
		static public string GetDescription(string code){
			string desc;
			switch(code){
				case "201":
					desc = "Authentication client-hub succedeed.";
					break;
				case "202":
					desc = "Ok to begin backup.";
					break;
				case "208":
					desc = "Successfully added new storage space.";
					break;
				case "209":
					desc = "Received public key.";
					break;
				case "215":
					desc = "New backup unit  successfully processed..";
					break;
				case "403":
					desc = "Not enough storage space for this backup.";
					break;
				case "406":
					desc = "No storage space available. All storage nodes are full, or no storage node (appart from this client) is online.";
					break;
				case "502":
					desc = "Node pending for approval. Hub administrator must manually trust this node.";
					break;
				case "505":
					desc = "Backup unit doesn't exist on storage node.";
					break;
				case "800":
					desc = "Node too busy to start new Backupset.";
					break;
				case "ACC":
					desc = "Accepted new client connection.";
					break;
				case "PIX":
					desc = "Client asks destination to store index.";
					break;
				case "PUT":
					desc = "Client asks destination to store chunk.";
					break;
				case "ACS":
					desc = "Sent storage space to newly connected storage node.";
					break;
				case "DFI":
					desc= "Chunk successfully stored on destination.";
					break;
				case "DBU":
					desc = "Successfully processed Backupset.";
					break;
				case "DST":
					desc = "Told client on wich storage node to put chunk.";
					break;
				case "RSP":
					desc = "Responded with hashed string.";
					break;
				default :
					desc = "<No description available for this code>";
					break;
				
			}
			return desc;
		}
	}
	
	
	
	// StateObject is the structure used to keep state between async receives (communication with client nodes)
	public class StateObject {
		// sslstream
		public SslStream stream;
		// Client socket.
		public Socket workSocket = null;
		// Size of receive buffer.
		public const int BufferSize = 1024*32; //512k max. TODO: after index browse pagination, put value back to 32k
		    // Receive buffer.
		public byte[] buffer = new byte[BufferSize];
	}
	
	internal class Utils{
		// returns the total seconds elapsed since unix time(1970-01-01)
		internal static long GetUnixTimeFromDateTime(DateTime dt){
			TimeSpan span = (dt - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).ToLocalTime());
			return (long)span.TotalSeconds;
		}
		
		internal static DateTime GetDateTimeFromUnixTime(long unixTime){
			return (new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).AddSeconds((double)unixTime).ToLocalTime();
		}
		
		internal static TimeSpan GetUptime(){
			return DateTime.Now - Process.GetCurrentProcess().StartTime;
		}
		
		internal static void SetProcInfo(string info){
			if(Utilities.PlatForm.Instance().OS == "Linux")
				try{
					prctl (15/* PR_SET_NAME */, Encoding.ASCII.GetBytes (info + "\0"), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
					//Console.WriteLine("prctl : "+ret);
				}catch{}
				
			else if (Utilities.PlatForm.Instance().OS == "FreeBSD" || Utilities.PlatForm.Instance().OS == "SunOS")
				try{
					setproctitle (Encoding.ASCII.GetBytes ("%s\0"), Encoding.ASCII.GetBytes (info + "\0"));
				}catch{}
		}
		
		internal static bool ConfigFileExists(){
        		return System.IO.File.Exists(System.Reflection.Assembly.GetEntryAssembly().Location + ".config");
    		}

		internal static void DisplayCertificateInformation(X509Certificate cert){ 
			if (cert != null){
				Logger.Append("CLIENT", Severity.DEBUG, string.Format("Cert was issued by {0} to {1} and is valid from {2} until {3}.",
				              cert.Issuer,
                                              cert.Subject,
                                              cert.GetEffectiveDateString(),
                                              cert.GetExpirationDateString()
                                              ));
			} 
			else{
				Logger.Append("CLIENT", Severity.DEBUG, "Certificate is null.");
			}
		}

		[DllImport ("libc")] // Linux
		private static extern int prctl (int option, byte[] arg2, IntPtr arg3, IntPtr arg4, IntPtr arg5);
		[DllImport ("libc")] // BSD
		private static extern void setproctitle (byte[] fmt, byte[] str_arg);
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
			NodeVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
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
				return "NT"+Environment.OSVersion.Version.Major;
		}
	}
}

