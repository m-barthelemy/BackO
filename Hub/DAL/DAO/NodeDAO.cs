using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using ServiceStack.OrmLite;
using P2PBackup.Common;
//using P2PBackup.DAL;

namespace P2PBackupHub.DAL {

	public class NodeDAO {

		IDbConnection dbc;
		private User sessionUser;

		public NodeDAO() {
		}

		public NodeDAO(User currentUser) {
			sessionUser = currentUser;
		}

		public P2PBackup.Common.Node Get(uint id){
			using(dbc = DAL.Instance.GetDb()){
				Node n =  dbc.GetById<P2PBackup.Common.Node>( id);
				n.Plugins = new PluginDAO(sessionUser).GetForNode(id);
				return n;
			}
		}

		private PeerNode GetPeerNode(uint id){
			using(dbc = DAL.Instance.GetDb()){
				return dbc.GetById<PeerNode>(id);
			}
		}

		public List<P2PBackup.Common.Node> GetAll(int? groupId){
			using(dbc = DAL.Instance.GetDb()){
				if(groupId.HasValue){
					//Console.WriteLine("nodeDAO : requested nodes having group ="+groupId.Value);
					return dbc.Select<P2PBackup.Common.Node>( n => n.Group == groupId.Value);
				}
				else
					return dbc.Select<P2PBackup.Common.Node>();
			}
		}

		public List<P2PBackup.Common.Node> GetAllHavingPlugin(int? groupId, string pluginName){
			using(dbc = DAL.Instance.GetDb()){
				if(groupId.HasValue){
					var jn = new JoinSqlBuilder<Node, Plugin>();
					jn = jn.Join<Node, Plugin>(x => x.Id, y => y.NodeId)
						.Where<Node>(x => x.Group == groupId.Value)
						.And<Plugin>(p => p.Name == pluginName);
					// temp hack waiting for issue #256 to be solved
					jn = jn.SelectAll<Node>();
					Console.WriteLine("GetAllHavingPlugin : sql="+jn.ToSql());
					return dbc.Query<Node>(jn.ToSql());
				}
				else
					return dbc.Select<P2PBackup.Common.Node>();
			}
		}

		public P2PBackup.Common.Node GetByInternalId(string internalId){
			using(dbc = DAL.Instance.GetDb()){
				var result =  dbc.Select<P2PBackup.Common.Node>(n => n.InternalId == internalId);
				if(result.Count >0)
					return result[0];
				else
					return null;
			}
		}

		public List<Plugin> GetAllInstalledStoragePlugins(){
			using(dbc = DAL.Instance.GetDb()){
				return dbc.Query<Plugin>("SELECT \"Plugins\" FROM \"Node\"");
			}
		}

		public PeerNode Save(PeerNode n){
			using(dbc = DAL.Instance.GetDb()){
				n.Id = (uint)IdManager.GetId();
				n.CreationDate = DateTime.Now;
				//n.Generation = 0;
				dbc.Insert(n);
				//return (long)dbc.GetLastInsertId();
				//return person.Id;
			}
			return n;
		}
		
		public P2PBackup.Common.Node Update(P2PBackup.Common.Node newN){
			// prevent from updating available space
			// and prevent updating storage space if new value < available
			P2PBackup.Common.Node dbNode = Get(newN.Id);
			P2PBackup.Common.Node liveNode = Hub.NodesList.GetById(newN.Id);

			// If storage space size was chaged, ensure it was not set to less than currently used space
			if(liveNode != null){
				lock(liveNode){
					if(newN.StorageSize < liveNode.StorageUsed)
						throw new ArgumentException("Cannot set node storage size to a value being less than currently used storage value.");
					else
						liveNode.StorageSize = newN.StorageSize;
				}
			}
			else{
				if(newN.StorageSize < dbNode.StorageUsed)
					throw new ArgumentException("Cannot set node storage size to a value being less than currently used storage value.");
			}
			newN.StorageUsed = dbNode.StorageUsed;
			using(dbc = DAL.Instance.GetDb())
				dbc.Update(newN);
			return newN;
		}

		/// <summary>
		/// When a node connects, update some information : IP, LastConnection, OS and Version.
		/// </summary>
		public void UpdatePartial(P2PBackup.Common.Node n){
			using(dbc = DAL.Instance.GetDb()){
				dbc.Update<P2PBackup.Common.Node>(new 
					{ OS = n.OS, 
					Version = n.Version, 
					LastConnection = DateTime.Now, 
					IP = n.IP, 
					HostName = n.HostName ,
					//Plugins = n.Plugins
				}, p => p.Id == n.Id);
			}
			// update plugins. TODO : maybe implement a Repository pattern instead of cross-DAO calls
			new PluginDAO().AddOrUpdateForNode(n.Id, n.Plugins);

		}

		/// <summary>
		/// Changes a node's group or parent
		/// </summary>
		/// <param name="n">The node with its updated group information</param>
		/*public Node UpdateParent(Node n){
			using(dbc = DAL.Instance.GetDb()){
				dbc.Update<P2PBackup.Common.Node>(new {Group = n.Group}, p => p.Id == n.Id);
			}
			return n;
		}*/

		public void UpdateStorageSpace(P2PBackup.Common.Node n){
			using(dbc = DAL.Instance.GetDb())
				dbc.Update<P2PBackup.Common.Node>(
					new  { Id = n.Id, Available = n.StorageUsed }, p => p.Id == n.Id
				);
		}

		internal PeerNode NodeApproved(byte[] certSerial){
			NodeCertificate nc = new CertificateDAO().GetBySerial(certSerial);
			if(nc == null) return null;
			PeerNode n =  GetPeerNode(nc.NodeId);
			n.Certificate = nc;
			n.PublicKey = nc.PublicKey;
			return n;
		}

		internal List<P2PBackup.Common.Node> GetStorageNodes(int? storageGroupId){
			using(dbc = DAL.Instance.GetDb()){
				if(storageGroupId.HasValue)
					return dbc.Select<P2PBackup.Common.Node>(n => n.StorageGroup == storageGroupId.Value);
				else
					return dbc.Select<P2PBackup.Common.Node>(n => n.StorageGroup >0);
			}
		}

		internal void Approve(uint nodeId, bool locked){
			using(dbc = DAL.Instance.GetDb())
				dbc.Update<P2PBackup.Common.Node>(new { Locked = locked  }, p => p.Id == nodeId);
				//dbc.Update(new P2PBackup.Common.Node{ Id = nodeId, Locked = locked});
				//dbc.Update(new { Id = n.Id, OS = n.OS, Version = n.Version, LastConnection = DateTime.Now, IP = n.IP }, p => p.Id == n.Uid);
				/*dbc.Update<P2PBackup.Common.Node>(
					new  P2PBackup.Common.Node { Id = nodeId, Locked = false }
				);*/
		}
	}
}

