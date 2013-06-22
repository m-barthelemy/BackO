using System;
using P2PBackupHub.Utilities;
using P2PBackupHub.Notifiers;
using P2PBackup.Common;

namespace P2PBackupHub {
	public class NotifierFactory {
		private NotifierFactory() {
		}
		
		internal static INotifier GetNotifier(string name){
			switch(name){
			
			case "email":
				return new MailNotifier();
			case "archiver":
				return new Archiver();
			/*case "clientshell":
				return new ClientShell();
				break;*/
				
			default:
				Logger.Append("NOTIFIER", Severity.WARNING, "Could not get notifier '"+name);
				return null;
			}
		}
	}
}

