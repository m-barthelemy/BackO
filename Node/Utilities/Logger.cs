using System;
using System.IO;
using System.Configuration;
using System.Diagnostics;
using P2PBackup.Common;
using System.Reflection;

namespace Node.Utilities {

	public class Logger	{
		
		private static Logger _instance;
		private static Session callingSession;
		private static readonly object ConsoleLock = new object();
		static StreamWriter SW;
		static ISystemLogger sysLogger;
		private static bool logToConsole = false;
		private static bool logToFile = false;
		private static bool logToSyslog = false;
		internal static Severity MinSeverity;
		
		private Logger (){
			if(ConfigManager.GetValue("Logger.LogToConsole") != null && ConfigManager.GetValue("Logger.LogToConsole").ToLower() == "true")
				logToConsole = true;
			if(!string.IsNullOrEmpty(ConfigManager.GetValue("Logger.LogFile")))
				logToFile = true;
			if(ConfigManager.GetValue("Logger.LogToSyslog") != null && ConfigManager.GetValue("Logger.LogToSyslog").ToLower() == "true")
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
			if(logToSyslog)
				sysLogger = SystemLoggerFactory.GetLogger();
			
			Severity configuredSeverity = Severity.INFO;
			Enum.TryParse<Severity>(ConfigManager.GetValue("Logger.Level"), out configuredSeverity);
			MinSeverity = configuredSeverity;
		}
		
		
		static public void Append(Severity severity, string message){
			
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
				if(logToFile){
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
				if(logToSyslog){
					sysLogger.Log(severity, message);
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
					message = callingSession.Id+": #"+callingSession.FromNode.Id+" --> #"+callingSession.ToNode.Id+" ("+callingSession.ToNode.IP+":"+callingSession.ToNode.ListenPort+");"+message;
				
				Logger.Append(severity, message);
			}
		}
	}
}

