using System;
using System.Collections.Generic;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceHost;

using P2PBackup.Common;
using SharpBackupWeb.Utilities;


namespace Backo.Api.WebServices {
	
	[Route("/api/Tasks/Running")]
	public class GetRunningTasks : IReturn<List<Task>>{
	}

	[Route("/api/Tasks/{Id}/Log")]
	public class GetTaskLog : IReturn<List<LogEntry>>{
		public int Id { get; set; }
	}

	[Route("/api/Tasks/QueryHistory/")]
	public class QueryHistory : IReturn<List<Task>>{
	}

	[Authenticate]
	public class TasksWS :AppServiceBase{

		public List<Task> Get(GetRunningTasks req){
			return RemotingManager.GetRemoteObject().GetRunningTasks();
		}

		public List<TaskLogEntry> Get (GetTaskLog req){
			foreach(P2PBackup.Common.Task t in RemotingManager.GetRemoteObject().GetRunningTasks()){
				if(t.Id == req.Id)
					return t.LogEntries;
			}
			// wanted task is not running, let's search through archived ones
			return RemotingManager.GetRemoteObject().GetArchivedTaskLogEntries(req.Id);
		}

		public class HistoryResult{
			public int TotalCount{get;set;}
			public List<Task> Items{get;set;}
		}

		public HistoryResult Get (QueryHistory q){

			char[] delimiterChars = {','};
			DateTime from = DateTime.Parse(Request.QueryString["from"]);
			DateTime to = DateTime.Parse(Request.QueryString["to"]);
			//string[] statusesString = Request.QueryString["statuses"].Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
			TaskRunningStatus[] status = Array.ConvertAll(Request.QueryString["statuses"].Split(','), s=>(TaskRunningStatus)Enum.Parse(typeof(TaskRunningStatus), s));
			List<TaskRunningStatus> statuses = new List<TaskRunningStatus>(status);
			//Console.WriteLine(HttpUtility.UrlDecode(Request.QueryString["bs"]));
			
			string[] bs = base.Request.QueryString["bs"].Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
			string sizeOperator = Request.QueryString["sizeOperator"];
			long size = long.Parse(Request.QueryString["size"]);
			//tasksList = (List<P2PBackup.Common.Task>)remoteOperation.GetTasksHistory(bs, from, to);
			int offset=0;
			int limit=20;
			int.TryParse(base.Request.QueryString["start"], out offset);
			int.TryParse(base.Request.QueryString["limit"], out limit);
			var hr = new HistoryResult();
			int totalCount = 0;
			hr.Items = (List<P2PBackup.Common.Task>)RemotingManager.GetRemoteObject().GetTasksHistory(bs, from, to, statuses, sizeOperator, size, limit, offset, out totalCount);
			hr.TotalCount = totalCount;
			return hr;
			//RemotingManager.GetRemoteObject().GetTasksHistory
		}

		public TasksWS (){
		}
	}
}

