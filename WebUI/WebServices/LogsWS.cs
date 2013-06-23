using System;
using System.Collections.Generic;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceHost;

using P2PBackup.Common;
using SharpBackupWeb.Utilities;


namespace Backo.Api.WebServices {
	
	[Route("/api/HubLogs/")]
	public class GetHubLogs : IReturn<List<LogEntry>>{
		public int Interval { get; set; }
	}
	
	[Authenticate]
	public class LogWS :AppServiceBase{
		IRemoteOperations remoteOperation;
		
		public LogEntry[] Get(GetHubLogs req){
			int interval = 60; // minutes from now
			if(!string.IsNullOrEmpty(base.Request.QueryString["interval"]))
				interval = int.Parse (base.Request.QueryString["interval"]);
			return remoteOperation.GetLogBuffer();
			
		}
		
		public LogWS (){
			remoteOperation = remoteOperation = RemotingManager.GetRemoteObject();
		}
	}
}

