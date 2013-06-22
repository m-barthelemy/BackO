using System;
//using ServiceStack.Text;

namespace P2PBackup.Common {

	public enum MessageContext{Generic=1, Task=2, Authentication=3}


	public class NodeMessage {

		public int Id{get;set;}
		public MessageContext Context{get;set;}
		public long TaskId{get;set;}
		//public Message ContextMessage{get;set;}
		public string Action{get;set;}
		public string Data{get;set;}
		public string Data2{get;set;}
		public bool Synchroneous{get;set;}

		public NodeMessage (){
			this.Context = MessageContext.Generic;
			this.Synchroneous = false;
			this.Data = string.Empty;
		}

		public override string ToString () {
			return string.Format ("[NodeMessage: Id={0}, Context={1}, TaskId={2}, Action={3}, Data={4}, Data2={5}, Synchroneous={6}]", Id, Context, TaskId, Action, Data, Data2, Synchroneous);
		}
	}

	/*public interface IMessageAction{

	}

	public interface Message{
		IMessageAction Action{get;set;}
	}*/

	/*public enum GenericAction {
		Browse, // Browse Node filesystems
		BrowseSpecialObjects, // browse node special objects (VSS writers, applications plugins)
		StartTask,
		CancelTask
	}*/

	/*public class GenericMessage : Message{
		public GenericAction Action{get;set;}
	}*/


	/*public enum AuthAction {
		AskCertificate, // Client asks for a new certificate
		AskAuthentication, // Client asks to authenticate
		AskConfiguration, // Client asks its configuration
		SendClientInfo // Node sends its version, OS, hostname..
	}*/

	/*public class AuthenticationMessage : Message{
		public AuthAction Action{get;set;}
	}*/


	/*public enum TaskActionMsg {
		
		AskStorageDestination, // Client asks destination to store backup data
		AskStorageReceive, // Hub asks storagenode to receive data from peer
		AskTaskStats,
		ExpireBackup,
		DeleteChunk, // Hub asks storage node to delete a chunk (backup expiring, housekeeping)
		Generic // generic task processing message
	}*/

	/*public class TaskMessage : Message{
		public TaskActionMsg Action{get;set;}
	}*/





}

