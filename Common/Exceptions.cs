using System;

namespace P2PBackup.Common {
	
	public class OverQuotaException:Exception {
		public new string Message{get;private set;}
		
		
		public OverQuotaException (long used, long quota){
			this.Message = "Node usage ("+used+") is overquota ("+quota+")";
		}
	}
	
	public class UnreachableNodeException:Exception {

		public UnreachableNodeException (){}

		public UnreachableNodeException (string message):base(message){}
	}

	public class NodeSecurityException : Exception{

		public NodeSecurityException():base(){}

		public NodeSecurityException(string message):base(message){}
	}

	public class ProtocolViolationException:Exception{
		

		public ProtocolViolationException(){}
		
		public ProtocolViolationException(string message):base(message){}

		public ProtocolViolationException(NodeMessage message):base("Received Unknown or bad Message : "+message.ToString()){}
	}

	/// <summary>
	/// Permisssion exception.
	/// Raised when a remote method is called on an item for which the logged user has insufficient rights.
	/// Mostly throwed by dbhandle
	/// </summary>
	public class PermisssionException:Exception{
		
		public PermisssionException(string message):base(message){
			
		}
	}

}

