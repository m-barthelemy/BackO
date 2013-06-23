using System;
using System.Collections.Generic;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceHost;

using P2PBackup.Common;
using SharpBackupWeb.Utilities;


namespace Backo.Api.WebServices {

	[Route("/api/NodeGroups/", "GET")]
	public class NG : IReturn<List<NodeGroup>>{
		//public int Interval { get; set; }
	}

	[Authenticate]
	public class NodeGroupWS :AppServiceBase{

		public List<NodeGroup> Get(NG req){
			return RemotingManager.GetRemoteObject().GetNodeGroups();
		}

		public NodeGroup Post(NodeGroup ng){
			return RemotingManager.GetRemoteObject().CreateNodeGroup(ng);
		}

		public NodeGroup Put(NodeGroup ng){
			return RemotingManager.GetRemoteObject().UpdateNodeGroup(ng);
		}

		public void Delete(NodeGroup ng){
			RemotingManager.GetRemoteObject().DeleteNodeGroup(ng);
		}

		public NodeGroupWS (){

		}
	}
}

