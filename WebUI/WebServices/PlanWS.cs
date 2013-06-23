using System;
using System.Collections.Generic;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceHost;

using P2PBackup.Common;
using SharpBackupWeb.Utilities;


namespace Backo.Api.WebServices {

	[Route("/api/Plan/")]
	public class GetPlan : IReturn<List<BackupSet>>{
		//public int Interval { get; set; }
	}

	[Authenticate]
	public class PlanWS :AppServiceBase{

		public List<BackupSetSchedule> Get(GetPlan req){
			int interval = 4; // hours from now
			if(!string.IsNullOrEmpty(base.Request.QueryString["interval"]))
				interval = int.Parse (base.Request.QueryString["interval"]);
			return RemotingManager.GetRemoteObject().GetBackupPlan(interval);

		}

		public PlanWS (){
		}
	}
}

