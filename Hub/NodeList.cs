using System;
using System.Collections;
using P2PBackupHub.Utilities;
using P2PBackup.Common;

/*namespace P2PBackupHub{

	/// <summary>
	/// Holds all online users
	/// </summary>
	[Serializable]
	public class NodeList:ArrayList	{

		private static readonly object _locker = new object();

		public NodeList(){
	
		}

		internal void AddNode(PeerNode node){
			node.RemoveUserEvent += new P2PBackupHub.PeerNode.RemoveUserHandler(RemoveNode);
			//node.CalculateDestinationEvent += new P2PBackupHub.Node.CalculateDestinationHandler(CalculateDestination);
			//node.GetClientEvent += new P2PBackupHub.Node.GetClientHandler(GetClient);
			lock(_locker){
				if(GetNode(node.Id) != null) // remove any potentially un-logged existing instance for the same node
					RemoveNode(GetNode(node.Id));
				this.Add(node);	
			}
		}

		/// <summary>
		/// Removes a user from the UserList, the user is no longer online
		/// </summary>
		/// <param name="user">the user to remove</param>
		internal void RemoveNode(PeerNode node){
			try{
				lock(_locker)
					this.Remove(node);
				node.RemoveUserEvent -= new P2PBackupHub.PeerNode.RemoveUserHandler(RemoveNode);
			}
			catch(Exception e){
				Logger.Append("HUBRN", Severity.WARNING, "Could not remove node from online list :"+e.Message);
			}
		}

		public PeerNode GetNode(int id){
			foreach(PeerNode n in this){
				Console.WriteLine("NodeList.getNode("+id+") : current node = "+n.Id);
				if(n.Id == id)
					return n;
			}
			return null;
		}
		
		public bool IsOnline(int nodeId){
			foreach(PeerNode n in this)
				if(n.Id == nodeId)
					return true;
			return false;
		}
	}
}*/
