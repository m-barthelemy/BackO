using System;
using System.Runtime.Serialization;

namespace P2PBackup.Common{
	public enum Severity{CRITICAL, ERROR, WARNING, NOTICE, INFO, DEBUG, TRIVIA};
	
	[KnownType(typeof(Severity))]
	[DataContract]
	public class LogEntry{
		
		[DataMember]
		[DisplayFormatOption(Size=20)]
		public DateTime Date{get;set;}

		[DataMember]
		[DisplayFormatOption(Size=8)]
		public Severity Severity{get; set;}

		[DataMember]
		[DisplayFormatOption(Size=10)]
		public string Origin{get;set;}

		[DataMember]
		[DisplayFormatOption(Size=100)]
		public string Message{get;set;}
		
		public LogEntry(DateTime date, Severity severity, string origin, string message){
			this.Date = date;
			this.Severity = severity;
			this.Origin = origin;
			this.Message = message;
		}
		
		public LogEntry(){}
	
	}
	
}
