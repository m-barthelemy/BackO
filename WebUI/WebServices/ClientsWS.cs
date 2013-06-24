using System;
using System.Linq;
using System.Collections.Generic;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceHost;
using P2PBackup.Common;
using SharpBackupWeb.Utilities;

namespace Backo.Api.WebServices {


	[Route("/api/Nodes/")]
	public class GetAllNodes : IReturn<List<P2PBackup.Common.Node>>{}

	[Route("/api/Nodes/Online")]
	public class GetOnlineNodes : IReturn<List<P2PBackup.Common.Node>>{}

	[Route("/api/Nodes/Plugins")]
	public class GetInstalledPlugins : IReturn<List<P2PBackup.Common.Plugin>>{}

	[Route("/api/Nodes/Plugin/{PluginName}/")]
	public class GetNodesHavingPlugin : IReturn<List<P2PBackup.Common.Node>>{
		public string PluginName{get;set;}
	}

	/*[Route("/api/Node/{Id}")]
	public class GetById : IReturn<P2PBackup.Common.Node>{
		public int Id{get;set;}
	}*/


	[Route("/api/StorageNodes/")]
	public class AllStorageNodes : IReturn<List<P2PBackup.Common.Node>>{
	}

	[Route("/api/Node/{Id}/Configuration/")]
	public class GetNodeConf{
		public uint Id{get;set;}
	}

	[Route("/api/Node/{Id}/Lock/{Locked}", "PUT POST")]
	public class NodeApproval{
		public uint Id{get;set;}
		public bool Locked{get;set;}
	}

	[Route("/api/Node/{NodeId}/Browse", "GET")]
	public class NodeBrowse{
		public uint NodeId{get;set;}
	}

	//[Route("/api/Node/{NodeId}/BrowseIndex/{TaskId}/{FS}/{ItemId}/", "GET")]
	[Route("/api/Node/{NodeId}/BrowseIndex/{TaskId}/", "GET")]
	public class NodeIdxBrowse{
		public uint NodeId{get;set;}
		public long TaskId{get;set;}
		/*public string FS{get;set;}
		public long ItemId{get;set;}*/
	}

	[Route("/api/Node/{NodeId}/Drives", "GET")]
	public class NodeDrives{
		public uint NodeId{get;set;}
	}

	[Route("/api/Node/{NodeId}/SpecialObjects", "GET")]
	public class NodeSpecialObjects{
		public uint NodeId{get;set;}
	}

	/*public class FacadeNode: P2PBackup.Common.Node{
		public bool leaf{get{return true;}}

	}

	public class FacadeNodeGroup: P2PBackup.Common.NodeGroup{
		public bool leaf{get{return false;}}
		
	}*/

	[Authenticate]
	public class ClientsWS :AppServiceBase{


		public Object Get(GetAllNodes dummy){
			if(string.IsNullOrEmpty(base.Request.QueryString["node"]) 
			   || base.Request.QueryString["node"] == "root"
			   || base.Request.QueryString["node"] == "NaN"){
			   //|| base.Request.QueryString["node"] == "0"){

				Logger.Append(Severity.TRIVIA, "Received request to get Node groups . Querying remote server...");
				List<P2PBackup.Common.NodeGroup> baseGroups = RemotingManager.GetRemoteObject().GetNodeGroups();
				var groups = new List<P2PBackup.Common.Node>();
				foreach(NodeGroup g in baseGroups){
					groups.Add(new P2PBackup.Common.Node{Id = (uint)g.Id,
						Name = g.Name,
						Description = g.Description,
						Group = -1 // trick to distinguish groups from leafs
						/*Description = g.Description*/});
				}
				// add dummy group for nodes without group
				groups.Add(new P2PBackup.Common.Node{Id = 0,
					Name = "Nodes without group",
					Group = -1 // trick to distinguish groups from leafs
					});
				return groups;
			}
			else{
				return RemotingManager.GetRemoteObject().GetNodes(int.Parse(base.Request.QueryString["node"]));
			}

		}

		public Object Get(AllStorageNodes dummy){
			IRemoteOperations remoteOperation = RemotingManager.GetRemoteObject();
			if(string.IsNullOrEmpty(base.Request.QueryString["node"]) 
			   || base.Request.QueryString["node"] == "root"
			   || base.Request.QueryString["node"] == "NaN"
			   || base.Request.QueryString["node"] == "0"){

				List<P2PBackup.Common.StorageGroup> baseGroups = RemotingManager.GetRemoteObject().GetStorageGroups();
				var groups = new List<P2PBackup.Common.Node>();
				foreach(StorageGroup g in baseGroups){
					Console.WriteLine("Get(GetAllStorageNodes dummy) : adding 1 storagegroups as node to SG list");
					groups.Add(new P2PBackup.Common.Node{Id = (uint)g.Id,
						Name = g.Name,
						Group = -1, // trick to distinguish groups from leafs
						Description = g.Description});
					Console.WriteLine("Get(GetAllStorageNodes dummy) : ADDED 1 storagegroups as node to SG list");
				}
				return groups;
			}
			else{
				return RemotingManager.GetRemoteObject().GetStorageNodes(int.Parse(base.Request.QueryString["node"]));
			}
			
		}

		/// <summary>
		/// Returns all nodes currently online as a flat list (no groups)
		/// </summary>
		/// <param name='req'>
		/// Req.
		/// </param>
		public List<P2PBackup.Common.Node> Get(GetOnlineNodes req){
			return RemotingManager.GetRemoteObject().GetOnlineNodes();
		}

		public List<P2PBackup.Common.Node> Get(GetNodesHavingPlugin req){
			if(string.IsNullOrEmpty(base.Request.QueryString["node"]) 
			   || base.Request.QueryString["node"] == "root"
			   || base.Request.QueryString["node"] == "NaN"//){
			   || base.Request.QueryString["node"] == "0"){

				Logger.Append(Severity.TRIVIA, "Received request to get Node groups . Querying remote server...");
				List<P2PBackup.Common.NodeGroup> baseGroups = RemotingManager.GetRemoteObject().GetNodeGroups();
				var groups = new List<P2PBackup.Common.Node>();
				foreach(NodeGroup g in baseGroups){
					groups.Add(new P2PBackup.Common.Node{Id = (uint)g.Id,
						Name = g.Name,
						Description = g.Description,
						Group = -1 // trick to distinguish groups from leafs
							/*Description = g.Description*/});
				}
				// add dummy group for nodes without group
				groups.Add(new P2PBackup.Common.Node{Id = 0,
					Name = "Nodes without group",
					Group = -1 // trick to distinguish groups from leafs
				});
				return groups;
			}
			else{
				return RemotingManager.GetRemoteObject().GetNodesHavingPlugin(int.Parse(base.Request.QueryString["node"]), req.PluginName);
			}
		}

		public P2PBackup.Common.Node Get(P2PBackup.Common.Node req){
			return RemotingManager.GetRemoteObject().GetNode(req.Id);
		}

		public BrowseNode Get(NodeBrowse req){
			string path = base.Request.QueryString["path"];
			return RemotingManager.GetRemoteObject().Browse(req.NodeId, path);
		}

		public BrowseNode Get(NodeIdxBrowse req){
			string path = base.Request.QueryString["path"];
			string filter = base.Request.QueryString["filter"];
			long parentId = 0;
			long.TryParse(base.Request.QueryString["parentId"], out parentId);
			return RemotingManager.GetRemoteObject().BrowseIndex(req.NodeId, req.TaskId, path, parentId, filter);
		}

		public string Get(NodeDrives req){
			//base.Response.ContentType = "text/xml";
			return RemotingManager.GetRemoteObject().GetDrives(req.NodeId);
		}

		public string Get(NodeSpecialObjects req){
			//base.Response.ContentType = "text/xml";
			return RemotingManager.GetRemoteObject().GetSpecialObjects(req.NodeId);
		}

		public List<Plugin> Get(GetInstalledPlugins req){
			return RemotingManager.GetRemoteObject().GetAllAvailablePlugins();
		}

		/// <summary>
		/// For now POST is used to update the WHOLE node 
		/// </summary>
		/// <param name="req">Req.</param>
		public P2PBackup.Common.Node Post(P2PBackup.Common.Node req){
			return RemotingManager.GetRemoteObject().UpdateNode((P2PBackup.Common.Node)req);
		}

		public P2PBackup.Common.Node Put(P2PBackup.Common.Node req){
			return RemotingManager.GetRemoteObject().UpdateNode((P2PBackup.Common.Node)req);
		}
		/// <summary>
		/// For now PUT is used to update ONLY the node's group.
		/// </summary>
		/// <param name="req">Req.</param>
		/*public P2PBackup.Common.Node Put(P2PBackup.Common.Node req){
			return RemotingManager.GetRemoteObject().UpdateNodeParent((P2PBackup.Common.Node)req);
		}*/

		public void Post(NodeApproval req){
			RemotingManager.GetRemoteObject().ApproveNode(req.Id, req.Locked);
		}

		public ClientsWS (){
		}
	}
}

