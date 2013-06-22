using System;
using ServiceStack.DataAnnotations;
using ServiceStack.DesignPatterns.Model;

namespace P2PBackup.Common {

	public class TaskLogEntry {

		[Index(false)]
		public long TaskId{get;set;}

		public int Code{get;set;}

		public DateTime Date{get;set;}

		// context/plugin name
		public string Subsystem{get;set;}

		// additional data/information
		public string Message1{get;set;}
		public string Message2{get;set;}

		public TaskLogEntry (long taskId, DateTime when, int code, string message1, string message2){
		}

		public TaskLogEntry(long taskId){
			this.TaskId = taskId;
			this.Date = DateTime.Now;
		}

		public TaskLogEntry(){
			this.Date = DateTime.Now;
		}
	}
}

