using System;

namespace P2PBackup.Common {

	/// <summary>
	/// Event context : Generic to simply log it into Node log, Task if it's worth relaying it to Hub and insert into task log entries.
	/// </summary>
	public enum EventContext{Generic, Task}

	public class LogEventArgs : EventArgs{

		public LogEventArgs(int code, Severity severity, string message)  {
			Code = code;
			Severity = severity;
			Message = message;
			Context = EventContext.Generic;
		}

	    public LogEventArgs(int code, Severity severity, string message, EventContext context)  {
			Code = code;
			Severity = severity;
			Message = message;
			Context = context;
	    }

		public LogEventArgs(){
			this.Severity = Severity.DEBUG;
		}

		public EventContext Context{get;set;}
	    public int Code { get; private set; }
		public Severity Severity{get; set;}
		public string Message{get;set;}
	}



}

