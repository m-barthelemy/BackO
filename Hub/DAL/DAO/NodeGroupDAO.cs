using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using ServiceStack.OrmLite;
using P2PBackup.Common;

namespace P2PBackupHub.DAL {

	public class NodeGroupDAO {

		IDbConnection dbc;
		private User sessionUser;

		public NodeGroupDAO() {
		}

		public NodeGroupDAO(User currentUser) {
			sessionUser = currentUser;
		}

		public List<NodeGroup> GetAll(){
			using(dbc = DAL.Instance.GetDb()){
				return dbc.Select<NodeGroup>();
			}
		}

		public NodeGroup Save(NodeGroup ng){
			ng.Id = IdManager.GetId();
			using(dbc = DAL.Instance.GetDb()){
				dbc.Insert<NodeGroup>(ng);
			}
			return ng;
		}

		public NodeGroup Update(NodeGroup ng){
			using(dbc = DAL.Instance.GetDb()){
				dbc.Update(ng);
				return ng;
			}
		}

		/// <summary>
		/// Delete the specified NodeGroup.
		/// Also update any member node to point to 'no group' (id = -1) 
		/// </summary>
		/// <param name="ng">Ng.</param>
		public void Delete(NodeGroup ng){
			if(ng.Id <= 0) // prevent deleting 'default' and 'no group' groups
				throw new ArgumentOutOfRangeException("Cannot delete a Group with Id < 0");
			var nodeDao = new NodeDAO(sessionUser);
			var memberNodes = nodeDao.GetAll(ng.Id);
			foreach( Node n in memberNodes){
				n.Group = -1;
				nodeDao.Update(n);
			}

			using(dbc = DAL.Instance.GetDb()){
				dbc.Delete<NodeGroup>(ng);
			}
		}

	}
}

