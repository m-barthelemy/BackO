using System;
using P2PBackup.Common;

namespace Node.Utilities {

	internal class SystemLoggerFactory{
		internal static ISystemLogger GetLogger(){
			if(Utilities.PlatForm.IsUnixClient())
				return new UnixLogger();
			else
				return new NTLogger();
		}
	}

	internal interface ISystemLogger{
		void Log(Severity severity, string message);
	}

	/// <summary>
	/// Unix logger : wrapper around Syslog calls
	/// </summary>
	internal class UnixLogger : ISystemLogger{

		internal UnixLogger (){}

		public void Log(Severity severity, string message){
			Mono.Unix.Native.SyslogLevel sev = Mono.Unix.Native.SyslogLevel.LOG_INFO;
			switch(severity){
			case Severity.CRITICAL:
				sev = Mono.Unix.Native.SyslogLevel.LOG_EMERG;
				break;
			case Severity.ERROR:
				sev = Mono.Unix.Native.SyslogLevel.LOG_ERR;
				break;
			case Severity.WARNING:
				sev = Mono.Unix.Native.SyslogLevel.LOG_WARNING;
				break;
			case Severity.NOTICE:
				sev = Mono.Unix.Native.SyslogLevel.LOG_NOTICE;
				break;
			default:
				sev = Mono.Unix.Native.SyslogLevel.LOG_INFO;
				break;
			}

			Mono.Unix.Native.Syscall.syslog(Mono.Unix.Native.SyslogFacility.LOG_USER, sev, message);
		}

	}

	/// <summary>
	/// NT logger : wrapper around windows EventLog
	/// </summary>
	internal class NTLogger : ISystemLogger{

		System.Diagnostics.EventLog appLog;

		internal NTLogger(){
			appLog = new System.Diagnostics.EventLog() ;
			appLog.Source = "Node";
		}

		public void Log(Severity severity, string message){
			System.Diagnostics.EventLogEntryType sev = System.Diagnostics.EventLogEntryType.Information;
			if(severity == Severity.ERROR)
				sev = System.Diagnostics.EventLogEntryType.Error;
			else if (severity == Severity.WARNING)
				sev = System.Diagnostics.EventLogEntryType.Warning;
			appLog.WriteEntry(message, sev);
		}
	}
}

