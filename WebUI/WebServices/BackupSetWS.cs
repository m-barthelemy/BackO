using System;
using System.Linq;
using System.Collections.Generic;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceHost;
using P2PBackup.Common;
using SharpBackupWeb.Utilities;


namespace Backo.Api.WebServices {

	[Route("/api/BackupSets/{NodeId}/")]
	public class BSForNode{
		public uint NodeId{get;set;}
	}

	[Route("/api/BackupSets/Templates/")]
	public class BSTemplates{
		//public int NodeId{get;set;}
	}

	[Route("/api/BackupSet/{Id}/{Action}/")]
	public class BSAction{
		public long Id{get;set;}
		public string Action{get;set;}
	}

	/*[Route("/api/BackupSet/{Id}/", "GET")]
	public class GetBs{
		public int Id{get;set;}
	}*/

	[Authenticate]
	public class BackupSetWS : AppServiceBase{
		
		
		public List<BackupSet> Get(BSForNode req){
			return RemotingManager.GetRemoteObject().GetNodeBackupSets(req.NodeId);
		}

		public List<BackupSet> Get (BSTemplates req){
			return RemotingManager.GetRemoteObject().GetBackupSets(0, Int32.MaxValue, true);
		}

		public long Get(BSAction action){
			if(action.Action == "Start")
				return RemotingManager.GetRemoteObject().StartTask((int)action.Id, null);
			else if(action.Action == "Stop")
				RemotingManager.GetRemoteObject().StopTask(action.Id);
			return 0;
		}

		/*public BackupSet Get(GetBs req){
			return RemotingManager.GetRemoteObject().GetBackupSet(req.Id);
		}*/

		public BackupSet Get(BackupSet req){
			return RemotingManager.GetRemoteObject().GetBackupSet(req.Id);
		}

		public BackupSet Put(BackupSet req){
			return RemotingManager.GetRemoteObject().UpdateBackupSet(req);
		}

		public BackupSet Post(BackupSet bs){
			return RemotingManager.GetRemoteObject().CreateBackupSet(bs);
		}
		public BackupSetWS(){}


	}
	
	
}