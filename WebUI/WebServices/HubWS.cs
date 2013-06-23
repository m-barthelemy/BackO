using System;
using System.Collections.Generic;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceHost;

using P2PBackup.Common;
using SharpBackupWeb.Utilities;


namespace Backo.Api.WebServices {
	
	[Route("/api/Hub/Logs/")]
	public class GetHubLogs : IReturn<List<LogEntry>>{
		//public int Interval { get; set; }
	}

	[Route("/api/Hub/Configuration/")]
	public class GetHubConf : IReturn<List<LogEntry>>{
		//public int Interval { get; set; }
	}
	
	[Authenticate]
	public class HubWS :AppServiceBase{

		public LogEntry[] Get(GetHubLogs req){
			int interval = 60; // minutes from now
			if(!string.IsNullOrEmpty(base.Request.QueryString["interval"]))
				interval = int.Parse (base.Request.QueryString["interval"]);
			return RemotingManager.GetRemoteObject().GetLogBuffer();
			
		}

		public Dictionary<string, string> Get (GetHubConf req){
			return RemotingManager.GetRemoteObject().GetConfigurationParameters();
			//return default(Dictionary<string, string>);
		}
		public HubWS (){
		}
	}
}

