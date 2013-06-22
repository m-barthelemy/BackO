using System;
//using System.Xml;
//using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters;

namespace P2PBackup.Common{
	
	[Serializable]
	public class TaskNotification{
		
		public TaskRunningStatus Status{get;set;}
		public string Notifier{get;set;}
		
		public TaskNotification (TaskRunningStatus status, string notifier){
			this.Status = status;
			this.Notifier = notifier;
		}
		
		private TaskNotification(){
		
		}

		public override string ToString () {
			return string.Format ("[Status={0}, Notifier={1}]", Status, Notifier);
		}
	}
}

