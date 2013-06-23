using System;
using System.Collections.Generic;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceHost;

using P2PBackup.Common;
using SharpBackupWeb.Utilities;


namespace Backo.Api.WebServices {
	
	[Route("/api/StorageGroups/", "GET")]
	public class SG : IReturn<List<StorageGroup>>{
		//public int Interval { get; set; }
	}

	[Authenticate]
	public class StorageGroupWS :AppServiceBase{

		public List<StorageGroup> Get(SG req){
			return RemotingManager.GetRemoteObject().GetStorageGroups();
		}

		public StorageGroup Post(StorageGroup sg){
			return RemotingManager.GetRemoteObject().CreateStorageGroup(sg);
		}

		public StorageGroup Put(StorageGroup sg){
			return RemotingManager.GetRemoteObject().UpdateStorageGroup(sg);
		}

		public void Delete(StorageGroup sg){
			RemotingManager.GetRemoteObject().DeleteStorageGroup(sg);
		}

		public StorageGroupWS (){

		}
	}
}

