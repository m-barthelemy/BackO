using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using ServiceStack.OrmLite;
using P2PBackup.Common;

namespace P2PBackupHub.DAL {


	public class StorageGroupDAO {

		IDbConnection dbc;
		private User sessionUser;

		public StorageGroupDAO() {
		}

		public StorageGroupDAO(User currentUser) {
			sessionUser = currentUser;
		}

		public List<StorageGroup> GetAll(){
			using(dbc = DAL.Instance.GetDb()){
				return dbc.Select<StorageGroup>();
			}
		}

		public StorageGroup Save(StorageGroup sg){
			using(dbc = DAL.Instance.GetDb()){
				sg.Id = IdManager.GetId();
				dbc.Insert(sg);
				return sg;
			}
		}

		public StorageGroup Update(StorageGroup sg){
			using(dbc = DAL.Instance.GetDb()){
				dbc.Update(sg);
				return sg;
			}
		}

		/// <summary>
		/// Delete the specified StorageGroup.
		/// Also update any member node to point to 'no group' (id = -1) 
		/// </summary>
		/// <param name="sg">Sg.</param>
		public void Delete(StorageGroup sg){
			if(sg.Id <= 0) // prevent deleting 'default' and 'no group' groups
				throw new ArgumentOutOfRangeException("Cannot delete a Group with Id < 0");

			var nodeDao = new NodeDAO(sessionUser);
			var memberNodes = nodeDao.GetStorageNodes(sg.Id);
			foreach( Node n in memberNodes){
				n.StorageGroup = -1;
				nodeDao.Update(n);
			}

			using(dbc = DAL.Instance.GetDb()){
				dbc.Delete<StorageGroup>(sg);
			}
		}

	}
}

