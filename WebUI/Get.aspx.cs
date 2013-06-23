
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.Script;
using SharpBackupWeb.Utilities;

//using System.Runtime.Serialization.Json;
/*using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;*/
//using P2PBackupHub.Remoting;

//using P2PBackupHub;
using P2PBackup.Common;
using SharpBackupWeb.Utilities;


namespace SharpBackupWeb{


	public partial class Get : System.Web.UI.Page{
		//private TcpChannel channel;
		IRemoteOperations remoteOperation;
		private  void  Page_Load (object sender, EventArgs args){
			
		
			//remoteOperation = Utilities.GetRemoteObject();
			remoteOperation = RemotingManager.GetRemoteObject();// (IRemoteOperations)Session["remote"];
			
			
			if(Request.QueryString["w"] == "Clients"){
					//ArrayList onLineUsers = (ArrayList)remoteOperation.GetOnlineClients();
					//ArrayList users = (ArrayList)remoteOperation.GetClients();
					bool showOnlineOnly = false;
					if(Request.QueryString["online"] != null && Request.QueryString["online"].ToLower() == "true")
						showOnlineOnly = true;
					Response.Write(BuildClients(showOnlineOnly));
			}
			/*else if(Request.QueryString["w"] == "Clients2"){
					Response.Write(BuildClients2(true));
			}*/
			else if (Request.QueryString["w"] == "StorageNodes"){
					List<P2PBackup.Common.Node> users = remoteOperation.GetStorageNodes();
					Dictionary<int, NodeStatus> onLineUsers = remoteOperation.GetOnlineClients();
				
					bool groupsOnly = false;
					if(Request["groupsOnly"] != null && Request["groupsOnly"].ToLower() == "true")
						groupsOnly = true;
					Response.Write(BuildStorages(users, onLineUsers, groupsOnly));
			}
			else if(Request.QueryString["w"] == "BackupPlan"){
				List<P2PBackup.Common.Node> users = remoteOperation.GetNodes();
				Dictionary<int, NodeStatus> onLineUsers = remoteOperation.GetOnlineClients();
				List<P2PBackup.Common.BackupSet> bPlans = remoteOperation.GetBackupPlan((Request["interval"] == null)?12:int.Parse (Request["interval"]));
				Response.Write(BuildBP(bPlans, users, onLineUsers));
			
			}
			else if(Request.QueryString["w"] == "Backupsets"){
				Console.WriteLine("requested backupsets for node "+Request["nodeId"]);
				if(Request["nodeId"] != null){
						Response.Write(BuildBackupSet(int.Parse(Request["nodeId"])));
				}
			}
			else if(Request.QueryString["w"] == "Tasks"){
				Response.Write(BuildTasks());
			}
			else if(Request.QueryString["w"] == "TaskLogEntries"){
				if(Request["trackingId"] != null){
					Response.Write(BuildTaskLog(int.Parse(Request["trackingId"])));
				}
			}
			else if(Request.QueryString["w"] == "Browse"){
				int nodeId = int.Parse(Request["curNode"]);
				string path = Request.QueryString["path"];
				if(path == null || path == String.Empty)
					path = HttpUtility.UrlDecode(Request.QueryString["node"]);
				Response.ClearHeaders();
				Response.ContentEncoding = Encoding.UTF8;
				Response.ContentType = "text/xml;charset=utf-8";
				Response.Write(remoteOperation.Browse(nodeId, path));
			}
			else if(Request.QueryString["w"] == "SpecialObjects"){
				int nodeId = int.Parse(Request["nodeId"]);
				Response.ClearHeaders();
				Response.ContentEncoding = Encoding.UTF8;
				Response.ContentType = "text/xml;charset=utf-8";
				Response.Write(remoteOperation.GetSpecialObjects(nodeId));
			}
			else if(Request.QueryString["w"] == "Drives"){
				int nodeId = int.Parse(Request["nodeId"]);
				Response.ClearHeaders();
				Response.ContentEncoding = Encoding.UTF8;
				Response.ContentType = "text/xml;charset=utf-8";
				Response.Write(remoteOperation.GetDrives(nodeId));
			}
			else if(Request.QueryString["w"] == "VM"){
				int nodeId = int.Parse(Request["nodeId"]);
				Response.Write(remoteOperation.GetVMs(nodeId));
			}
			else if(Request.QueryString["w"] == "HubLogs"){
				LogEntry[] log = remoteOperation.GetLogBuffer();
				int start = 0;
				int limit = 20;
				if(Request["start"] != null)
					start = int.Parse(Request["start"]);
				if(Request["limit"] != null)
					limit = int.Parse(Request["limit"]);
				Response.Write(BuildLog(log, start, limit));
			}
			else if(Request.QueryString["w"] == "NodeConf"){
				int nodeId = int.Parse(Request["nodeId"]);
				Response.Write(BuildNodeConf(nodeId));
			}
			else if(Request.QueryString["w"] == "BackupHistory"){
				if(Request["bsId"] != null && Request["bsId"] != String.Empty && Request["startDate"] != null && Request["endDate"] != null){
					int bsId = int.Parse(Request["bsId"]);
					int limit = 50;
					int offset = 0;
					
					DateTime start = DateTime.ParseExact(Request["startDate"], "yyyy-mm-dd", CultureInfo.InvariantCulture);
					DateTime end = DateTime.ParseExact(Request["endDate"], "yyyy-mm-dd", CultureInfo.InvariantCulture);
					Response.Write(BuildBackupHistory(bsId, start, end));
				}
			}
			else if(Request.QueryString["w"] == "Users"){
				Response.Write(BuildUsers());
			}
			else if(Request.QueryString["w"] == "NodeGroups"){
				Response.Write(BuildNodeGroups());
			}
			else if(Request.QueryString["w"] == "Cultures"){
				Response.Write(BuildCultures());
			}
		}
		
		private string BuildTasks(){
			List<P2PBackup.Common.Task> tasksList = new List<P2PBackup.Common.Task>();
			int totalCount = 0;
			if(Request.QueryString["bs"] == null)
				tasksList = (List<P2PBackup.Common.Task>)remoteOperation.GetRunningTasks();
			else{
				char[] delimiterChars = {','};
				DateTime from = DateTime.Parse(Request.QueryString["from"]);
				DateTime to = DateTime.Parse(Request.QueryString["to"]);
				//string[] statusesString = Request.QueryString["statuses"].Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
				TaskRunningStatus[] status = Array.ConvertAll(Request.QueryString["statuses"].Split(','), s=>(TaskRunningStatus)Enum.Parse(typeof(TaskRunningStatus), s));
				List<TaskRunningStatus> statuses = new List<TaskRunningStatus>(status);
				//Console.WriteLine(HttpUtility.UrlDecode(Request.QueryString["bs"]));
				
				string[] bs = HttpUtility.UrlDecode(Request.QueryString["bs"]).Split(delimiterChars, StringSplitOptions.RemoveEmptyEntries);
				string sizeOperator = Request.QueryString["sizeOperator"];
				long size = long.Parse(Request.QueryString["size"]);
				//tasksList = (List<P2PBackup.Common.Task>)remoteOperation.GetTasksHistory(bs, from, to);
				int offset=0;
				int limit=20;
				int.TryParse(Request["start"], out offset);
				int.TryParse(Request["limit"], out limit);
				
				tasksList = (List<P2PBackup.Common.Task>)remoteOperation.GetTasksHistory(bs, from, to, statuses, sizeOperator, size, limit, offset, out totalCount);
				
			}
			StringBuilder jsb = new StringBuilder();
			foreach(P2PBackup.Common.Task t in tasksList){
				jsb.AppendLine("{");
				jsb.Append("id:"+t.Id+",");
				jsb.Append("type:'"+t.Type+"',");
				jsb.Append("userid:"+t.UserId+","); // user that started the task, if Manual
				jsb.Append("priority:'"+t.Priority+"',");
				jsb.Append("operation:'"+t.Operation+"',");
				jsb.Append("runningStatus:'"+t.RunStatus+"',");
				jsb.Append("status:'"+t.Status+"',");
				jsb.Append("startDate:'"+t.StartDate+"',");
				jsb.Append("endDate:'"+t.EndDate+"',");
				jsb.Append("originalSize:'"+t.OriginalSize+"',");
				jsb.Append("finalSize:'"+t.FinalSize+"',");
				jsb.Append("totalItems:"+t.TotalItems+",");
				jsb.Append("percent:'"+t.Percent+"',");
				int elapsed;
				if(t.RunStatus == TaskRunningStatus.PendingStart || t.RunStatus == TaskRunningStatus.Started)
					elapsed = (int)DateTime.Now.Subtract(t.StartDate).TotalSeconds; //ToString(@"hh\:mm\:ss");
				else if(t.EndDate > t.StartDate)
					elapsed = (int)t.EndDate.Subtract(t.StartDate).TotalSeconds; //.ToString(@"hh\:mm\:ss");
				else
					elapsed = (int)DateTime.Now.Subtract(t.StartDate).TotalSeconds;//.ToString(@"hh\:mm\:ss");
				jsb.Append("elapsedTime:"+elapsed+", ");
				if(t.Operation == TaskOperation.Backup){
					jsb.Append("bsId:'"+t.BackupSet.Id+"',");
					jsb.Append("clientId:'"+t.BackupSet.NodeId+"',");
					jsb.Append("bsName:'"+t.BackupSet.Name+"',");
					jsb.Append("bsType:'"+t.BackupSet.ScheduleTimes[0].Level+"',");
					//jsb.Append("compress:'"+t.BackupSet.Compress+"',");
					//jsb.Append("encrypt:'"+t.BackupSet.Encrypt+"',");
					//jsb.Append("clientdedup:'"+t.BackupSet.ClientDedup+"',");
					jsb.Append("flags:'"+t.BackupSet.DataFlags+"',");
					jsb.Append("parallelism:"+t.BackupSet.Parallelism+",");
				}
				jsb.Append("currentAction:'"+t.CurrentAction.Replace(@"\", @"\\")+"',");
				jsb.Append("children:[");
				/*foreach(P2PBackup.Common.Node node in t.StorageNodes){
					jsb.AppendLine(BuildNode(node)+", ");
				}*/ //20120705
				jsb.Append("]");
				jsb.AppendLine("},");
				
			}
			jsb.Insert(0, "{totalCount:"+/*totalCount*/tasksList.Count+", items:[");
			//jsb.Insert(0, "{totalCount:1000, items:[");
			//jsb.Insert(0, "[");
			jsb.AppendLine("]}");
			return jsb.ToString();
		}
		
		private string BuildTaskLog(int taskId){
			List<P2PBackup.Common.Task> tasksList = (List<P2PBackup.Common.Task>)remoteOperation.GetRunningTasks();
			StringBuilder jsb = new StringBuilder();
			bool found = false;
			foreach(P2PBackup.Common.Task t in tasksList){
				if(t.Id != taskId)
					continue;
				found = true;
				foreach(Tuple<DateTime, int, string, string> logEntry in t.LogEntries){
					jsb.AppendLine("{");
					jsb.Append("date:'"+logEntry.Item1.ToString()+"',");
					jsb.Append("code:"+logEntry.Item2+",");
					//jsb.Append("message:\""+logEntry.Item3.Replace("'",@"\'")+"\",");
					// quick and (very) dirty way to sanitize strings to be json-compliant
					jsb.Append("message1:\""+logEntry.Item3.Replace("\"","'").Replace(@"\", @"\\")+"\",");
					jsb.Append("message2:\""+logEntry.Item4.Replace("\"","'").Replace(@"\", @"\\")+"\",");
					jsb.AppendLine("},");
				}
			}
			if(!found){ // asked task is not running, let's search through archived ones
				foreach(Tuple<DateTime, int, string, string> logEntry in remoteOperation.GetArchivedTaskLogEntries(taskId)){
					// that's ugly duplicate code. FIXME
					jsb.AppendLine("{");
					jsb.Append("date:'"+logEntry.Item1.ToString()+"',");
					jsb.Append("code:'"+logEntry.Item2+"',");
					jsb.Append("message1:\""+logEntry.Item3.Replace("\"","'").Replace(@"\", @"\\")+"\",");
					jsb.Append("message2:\""+logEntry.Item4.Replace("\"","'").Replace(@"\", @"\\")+"\",");
					jsb.AppendLine("},");
				}
			}
			jsb.Insert(0, "[");
			jsb.AppendLine("]");
			return jsb.ToString();
		}
		
		private string BuildBackupSet(int nodeId){
			//Node n = remoteOperation.GetNode(nodeId);
			//List<BackupSet> bsList = n.GetBackupSets();
			List<BackupSet> bsList = remoteOperation.GetNodeBackupSets(nodeId);
			StringBuilder jsb = new StringBuilder();
			
			int count = 0;
			foreach(BackupSet bs in bsList){
					jsb.AppendLine("{");
					jsb.Append("id:'"+bs.Id+"',");
					jsb.Append("name:'"+bs.Name+"',");
					jsb.Append("checked:false,");
					jsb.Append("iconCls:'icon-bs',");
					jsb.Append("path:' ',");
					jsb.Append("children:[");
					int count2 = 0;
					foreach(BasePath bp in bs.BasePaths){
						jsb.AppendLine("{");
						jsb.Append("id:'bs"+bs.Id+"p"+count2 +"',");
						jsb.Append("path:'"+ bp.Path+ "',");
						jsb.Append("includerule:'"+ bp.IncludePolicy +"',");
						jsb.Append("excluderule:'"+ bp.ExcludePolicy +"',");
						jsb.Append("checked:false,");
						jsb.Append("leaf:true,");
						jsb.Append("iconCls:'icon-bp',");
						jsb.Append("},");
						count2++;
					}
					if(count2 >0)
						jsb.Remove(jsb.Length-1, 1);
					jsb.Append("]");
					jsb.AppendLine("},");
					count++;
			}
			//jsb.Insert(0,"{'count':'"+bsList.Count+"', 'backupsets':[");
			jsb.Insert(0, "[");
			jsb.AppendLine("");
			if(count >0)
				jsb.Remove(jsb.Length-1, 1);
			//jsb.AppendLine("]}");
			jsb.AppendLine("]");
			return jsb.ToString();
		}
		
		private string BuildBackupHistory(int bsId, DateTime start, DateTime end){
			//DateTime.Parse(Request["startDate"])
			Hashtable history = remoteOperation.GetBackupHistory(bsId, start, end);
			Console.WriteLine("history : got "+history.Count);
			StringBuilder jsb = new StringBuilder();
			jsb.Append("{\"success\":true,\"message\":\"Loaded data\",\"data\":[");
			// Extensible extjs calendar is definitely crappy about the (only) date format it accepts.
			foreach(DictionaryEntry de in history){
				jsb.Append("{");
				jsb.Append("\"id\":"+de.Key+", ");
				jsb.Append("\"cid\":1, ");
				jsb.Append("\"start\":'"+((DateTime)de.Value).ToString("yyyy-MM-ddTHH:mm:ss") +"', ");
				jsb.Append("\"end\":'"+((DateTime)de.Value).AddMinutes(1).ToString("yyyy-MM-ddTHH:mm:ss")+"', ");
				
				//jsb.Append("\"start\":\""+((DateTime)de.Value).ToString("yyyy-M-ddThh:mm:ss") +"\", ");
				//jsb.Append("\"end\":\""+((DateTime)de.Value).AddHours(1).ToString("yyyy-M-ddThh:mm:ss")+"\", ");
				//jsb.Append("\"start\":\""+((DateTime)de.Value).ToString("yyyy/M/dd hh:mm:ss") +"\", ");
				//jsb.Append("\"end\":\""+((DateTime)de.Value).AddHours(1).ToString("yyyy/M/dd hh:mm:ss")+"\", ");
				jsb.Append("\"title\":\"\", ");
				jsb.Append("\"note\":\"\"");
				jsb.Append("},");
			}
			jsb.Append("]}");
			
			return jsb.ToString();
		}
		
		private string BuildNodeConf(int nodeId){
			StringBuilder jsb = new StringBuilder();
			jsb.Append("{");
			Dictionary<string, string> conf = remoteOperation.GetNodeConf(nodeId);
			foreach(KeyValuePair<string,string> de in conf){
				jsb.Append(de.Key.ToString().Replace(".","_")+":'"+de.Value.ToString().Replace(@"\",@"\\")+"', ");
			}
			jsb.Remove(jsb.Length-1, 1);
			jsb.Append("}");
			//jsb.Insert(0,"{'count':'1', 'records':[");
			//jsb.Append("]}");
			return jsb.ToString();
		}
		
		private string BuildLog(LogEntry[] log, int start, int limit){
			StringBuilder jsb = new StringBuilder();
			int count = 0;
			for(int i=start; i<start+limit;i++){
				if(log[i] != null){
					//string[] logEntry = escaped.Split(';');
				
					jsb.Append("{");
					// date, origin, subsystem,message
					jsb.Append("date:'"+log[i].Date+"',");
					jsb.Append("origin:'"+log[i].Origin+"',");
					jsb.Append("subsystem:'',");
					jsb.Append("severity:'"+log[i].Severity+"',");
					jsb.Append("message:'"+log[i].Message.Replace("'","\"")+"'");
					jsb.Append("},");
					count ++;
				}
				
			}
			//jsb.Insert(0,"{count:"+log.Length+", records:[");
			jsb.Insert(0,"[");
			if(count >0)
				jsb.Remove(jsb.Length-1, 1);
			//jsb.Append("]}");
			jsb.Append("]");
			return jsb.ToString();
			
		}
		
		private string BuildNodeGroups(){
			List<NodeGroup> nodeGroups = remoteOperation.GetNodeGroups();
			StringBuilder jsb = new StringBuilder();
			jsb.AppendLine("[");
			foreach(NodeGroup ng in nodeGroups)			
				jsb.AppendLine("{name:'"+ng.Name+"', id:"+ng.Id+", description:'"+ng.Description+"'},");
			jsb.Append(@"]");
			return jsb.ToString();
		}
		
		/*private string BuildClients2(bool onlineOnly){
			List<int, NodeStatus> onLineNodes = remoteOperation.GetOnlineClients();
			System.Web.Script.Serialization.JavaScriptSerializer jsonFormatter = new System.Web.Script.Serialization.JavaScriptSerializer();
			
			return jsonFormatter.Serialize(onLineNodes);
		}*/
		
		/*private string BuildNode(Node n){
			System.Web.Script.Serialization.JavaScriptSerializer jsonFormatter = new System.Web.Script.Serialization.JavaScriptSerializer();
			
			return jsonFormatter.Serialize(n);
		}*/
		
		private string BuildClients(bool showOnlineOnly){
			
			StringBuilder jsb = new StringBuilder("[");
			List<NodeGroup> nodeGroups = remoteOperation.GetNodeGroups();
			List<P2PBackup.Common.Node> nodesL = new List<P2PBackup.Common.Node>();
			Dictionary<int, NodeStatus> onLineNodes = remoteOperation.GetOnlineClients();
			foreach(NodeGroup ng in nodeGroups){			
			jsb.AppendLine("{userName:'"+ng.Name+"', id:'g"+ng.Id+"', description:'"+ng.Description+"', ip:'', available:'', version:'', "
				+"uiProvider:'col', cls:'master-task', iconCls:'task-folder', children:");
			jsb.Append(@"[");
			int i=0;
			
			/*if(showOnlineOnly)
				nodesL = onLineNodes;
			else*/
				nodesL = remoteOperation.GetNodes();
			
			foreach (P2PBackup.Common.Node u in nodesL){
				if(showOnlineOnly && !onLineNodes.ContainsKey(u.Uid)) continue;
				if(u.Status != NodeStatus.Locked && u.Group == ng.Id){//registered nodes only
					jsb.AppendLine("{");
					//jsb.Append("id:'"+u.UserName+"',\n");
					//jsb.Append("checked:false,\n");
					jsb.AppendLine("userName:'"+u.NodeName+"',\n");
					jsb.Append("id:'"+u.Uid+"',\n");
					jsb.Append("ip:'"+u.IP+"',\n");
					jsb.Append("share:"+u.StorageSize+",\n");
					jsb.Append("available:"+u.Available+",\n");
					jsb.Append("group:"+u.Group+",\n");
					jsb.Append("storagegroup:"+u.StorageGroup+",\n");
					//jsb.Append("percent:'"+Math.Round((double)100 - u.Available/u.ShareSize*100,1)+"%', \n");
					jsb.Append("version:'"+u.Version+"',\n");
					jsb.Append("os:'"+u.OS+"',\n");
					jsb.Append("delegations:'"+String.Empty+"',\n");
					jsb.Append("backupsets:'"+u.NbBackupSets+"',\n");
					if(u.Quota == -1)
						jsb.Append("quota:'None',\n");
					else
						jsb.Append("quota:"+u.Quota+",\n");
					if(u.Quota == -1)
						jsb.Append("usedquota:'"+"<img class=\"ssi\" src=\"/images/sq_di.png\"/>', \n");
					else
						jsb.Append("usedquota:'"+u.UsedQuota+"',\n");
						//jsb.Append("usedquota:'"+FormatPercent(u.Quota, u.UsedQuota)+"%',\n");
					// specific to display
					jsb.Append("lastconnection:'"+u.LastConnection+"',\n");
					jsb.Append("status:'"+u.Status+"',\n");
					jsb.Append("certificateStatus:'sec-OK',\n");		
					jsb.Append("uiProvider:'col',\n");
					
					string status = "off";
					//if(u.Locked == true)
					//	status="lock";
					//else{
					/*foreach (int onlineId in onLineNodes){*/
						if (onLineNodes.ContainsKey(u.Uid)){
							//jsb.Append("certCN:'"+u.CertCN+"',\n");
							status = "on";
							//break;	
						}
					/*}*/
					//}
					jsb.Append("iconCls:'task-"+status+"',\n");
					jsb.Append("checked:false,\n");
					if(Request.QueryString["leaf"] == null && Request.QueryString["leaf"] != "false")
						jsb.Append("leaf:true,\n");
					else{
						jsb.Append("leaf:false,\n");
						jsb.Append("children:[{}],\n");
					}
					jsb.AppendLine("},");
					i++;
				}
			}
			
			if(i>0)	jsb.Remove(jsb.Length-2,1);
			jsb.Append("]");
			jsb.Append("},");
			} // end foreach nodegroup
			if(!showOnlineOnly){
				jsb.AppendLine(@"{userName:'Waiting for authorization', id:'locked', ip:'', available:'', version:'', uiProvider:'col', 
					cls:'master-task', iconCls:'task-folder', children:");
				jsb.Append(@"[");
				int i=0;
				foreach (P2PBackup.Common.Node u in nodesL){
					if(u.Status == NodeStatus.Locked){//registered nodes only
						jsb.Append("{");
						jsb.Append("userName:'"+u.NodeName+"',\n");
						jsb.Append("ip:'"+u.IP+"',\n");
						jsb.Append("id:'"+u.Uid+"',\n");
						jsb.Append("share:"+u.StorageSize+",\n");
						jsb.Append("available:"+u.Available+",\n");
						jsb.Append("group:"+u.Group+",\n");
						jsb.Append("storagegroup:"+u.StorageGroup+",\n");
						//jsb.Append("percent:'"+Math.Round((double)100 - u.Available/u.ShareSize*100,1)+"%', \n");
						jsb.Append("version:'"+u.Version+"',\n");
						jsb.Append("os:'"+u.OS+"',\n");
						jsb.Append("delegations:'"+String.Empty+"',\n");
						jsb.Append("backupsets:'"+u.NbBackupSets+"',\n");
						jsb.Append("lastconnection:'"+u.LastConnection+"',\n");
						jsb.Append("status:'"+u.Status+"',\n");
						if(u.Quota == -1)
							jsb.Append("quota:'None',\n");
						else
							jsb.Append("quota:'"+Math.Round((decimal)u.Quota/1024/1024/1024,1)+"G',\n");
						if(u.Quota == -1)
							jsb.Append("usedquota:'"+"<img class=\"ssi\" src=\"/images/sq_di.png\"/>', \n");
						else
							jsb.Append("usedquota:'"+FormatPercent(u.Quota, u.UsedQuota)+"%',\n");
						jsb.Append("certificateStatus:'sec-ERROR',\n");
						// specific to display
						jsb.Append("uiProvider:'col',\n");
						jsb.Append("leaf:true,\n");
						string status = "off";
						if(u.Status == NodeStatus.Locked)
							status="lock";
						else{
							//foreach (int onlineId in onLineNodes){
								if (onLineNodes.ContainsKey(u.Uid)){
									status = "on";
									//jsb.Append("certCN:'"+u.CertCN+"',\n");
									//break;
								}
							//}
						}
						jsb.Append("iconCls:'task-"+status+"',\n");
						jsb.Append("checked:false,\n");
						if(Request.QueryString["leaf"] == null && Request.QueryString["leaf"] != "false")
							jsb.Append("leaf:true,\n");
						else{
							jsb.Append("leaf:false,\n");
							jsb.Append("children:[{}],\n");
						}
						jsb.AppendLine("},");
					}
				}
			
				if(i>0)	jsb.Remove(jsb.Length-2,1);
			
				jsb.Append("]");
				jsb.Append("},");
			}
			
			// nodes without group
			jsb.AppendLine(@"{userName:'Nodes without group', id:'gU', ip:'', available:'', version:'', uiProvider:'col', 
					cls:'master-task', iconCls:'task-folder', children:");
			jsb.Append(@"[");
			//i=0;
			foreach (P2PBackup.Common.Node u in nodesL){
				if(u.Group == 0 && u.Status != NodeStatus.Locked){//registered nodes only
					jsb.Append("{");
					jsb.Append("userName:'"+u.NodeName+"',\n");
					jsb.Append("ip:'"+u.IP+"',\n");
					jsb.Append("id:'"+u.Uid+"',\n");
					jsb.Append("share:"+u.StorageSize+",\n");
					jsb.Append("available:"+u.Available+",\n");
					//jsb.Append("group:"+u.Group+",\n");
					jsb.Append("storagegroup:"+u.StorageGroup+",\n");
					//jsb.Append("percent:'"+Math.Round((double)100 - u.Available/u.ShareSize*100,1)+"%', \n");
					jsb.Append("version:'"+u.Version+"',\n");
					jsb.Append("os:'"+u.OS+"',\n");
					jsb.Append("delegations:'"+String.Empty+"',\n");
					jsb.Append("backupsets:'"+u.NbBackupSets+"',\n");
					jsb.Append("lastconnection:'"+u.LastConnection+"',\n");
					jsb.Append("status:'"+u.Status+"',\n");
					if(u.Quota == -1)
						jsb.Append("quota:'None',\n");
					else
						jsb.Append("quota:'"+Math.Round((decimal)u.Quota/1024/1024/1024,1)+"G',\n");
					if(u.Quota == -1)
						jsb.Append("usedquota:'"+"<img class=\"ssi\" src=\"/images/sq_di.png\"/>', \n");
					else
						jsb.Append("usedquota:'"+FormatPercent(u.Quota, u.UsedQuota)+"%',\n");
					jsb.Append("certificateStatus:'sec-OK',\n");
					// specific to display
					jsb.Append("uiProvider:'col',\n");
					jsb.Append("leaf:true,\n");
					string status = "off";
					if(u.Status == NodeStatus.Locked)
						status="lock";
					else{
						//foreach (int onlineId in onLineNodes){
							if (onLineNodes.ContainsKey(u.Uid)){
								status = "on";
								//jsb.Append("certCN:'"+u.CertCN+"',\n");
								//break;
							}
						//}
					}
					jsb.Append("iconCls:'task-"+status+"',\n");
					jsb.Append("checked:false,\n");
					if(Request.QueryString["leaf"] == null && Request.QueryString["leaf"] != "false")
						jsb.Append("leaf:true,\n");
					else{
						jsb.Append("leaf:false,\n");
						jsb.Append("children:[{}],\n");
					}
					jsb.AppendLine("},");
				}
			}
			//if(i>0)	jsb.Remove(jsb.Length-2,1);
		
			jsb.Append("]");
			jsb.Append("}");
			jsb.Append("]");
			
			return(jsb.ToString());
		}
		
		private string BuildNode(P2PBackup.Common.Node u){
			
			StringBuilder jsb = new StringBuilder();
			jsb.AppendLine("{userName:'"+u.NodeName+"',\n");
			jsb.Append("id:'"+u.Uid+"',\n");
			jsb.Append("ip:'"+u.IP+"',\n");
			jsb.Append("share:"+u.StorageSize+",\n");
			jsb.Append("available:"+u.Available+",\n");
			jsb.Append("group:"+u.Group+",\n");
			jsb.Append("storagegroup:"+u.StorageGroup+",\n");
			//jsb.Append("percent:'"+Math.Round((double)100 - u.Available/u.ShareSize*100,1)+"%', \n");
			jsb.Append("version:'"+u.Version+"',\n");
			jsb.Append("os:'"+u.OS+"',\n");
			jsb.Append("delegations:'"+String.Empty+"',\n");
			jsb.Append("backupsets:'"+u.NbBackupSets+"',\n");
			jsb.Append("lastconnection:'"+u.LastConnection+"',\n");
			if(u.Quota == -1)
				jsb.Append("quota:'None',\n");
			else
				jsb.Append("quota:"+u.Quota+",\n");
			if(u.Quota == -1)
				jsb.Append("usedquota:'"+"<img class=\"ssi\" src=\"/images/sq_di.png\"/>', \n");
			else
				jsb.Append("usedquota:'"+ u.UsedQuota+"',\n");
				//jsb.Append("usedquota:'"+FormatPercent(u.Quota, u.UsedQuota)+"%',\n");
			// specific to display
				
			jsb.Append("certificateStatus:'sec-OK',\n");		
			jsb.Append("uiProvider:'col',\n}");
			//Console.WriteLine ("task sn : "+jsb.ToString());
			return jsb.ToString();
		}
		
		private string FormatPercent(double total, double used){
			double percent = Math.Round(used/total*100,0);
			/*string formated = "";
			if(percent > 90)
				formated = "<img class=\"ssi\" src=\"/images/sq_re.gif\"/>";
			else if(percent > 80)
				formated = "<img class=\"ssi\" src=\"/images/sq_ye.gif\"/>";
			else if(percent >=0)
				formated = "<img class=\"ssi\" src=\"/images/sq_gr.gif\"/>";
			else 
				formated = "<img class=\"ssi\" src=\"/images/sq_di.png\"/>";*/
			//return formated+" "+percent;
			return percent.ToString();
			
		}
		
		private string BuildStorages(List<P2PBackup.Common.Node> nodes, Dictionary<int, NodeStatus> onLineNodes, bool groupsOnly){
			List<P2PBackup.Common.StorageGroup> sgs = remoteOperation.GetStorageGroups();
			StringBuilder jsb = new StringBuilder("[");
			string jsHeader = "";
			foreach(StorageGroup sg in sgs){
				int nb=0;
				StringBuilder nsb = new StringBuilder();
				nsb.Append(@"[");
				double sgTotalSpace = 0;
				double sgAvailableSpace = 0;
				
				foreach (P2PBackup.Common.Node u in nodes){
					decimal share = 0;
					decimal available = 0;
					if(u.StorageGroup == sg.Id){
						if(!groupsOnly){
							nsb.Append("{");
							nsb.Append("userName:'"+u.NodeName+"',\n");
							nsb.Append("ip:'"+u.IP+"',\n");
							nsb.Append("id:'n"+u.Uid+"',\n");
							//share = Math.Round((decimal)u.ShareSize/1024/1024/1024,1);
							//nsb.Append("share:"+share+",\n");
							nsb.Append("share:"+u.StorageSize+",\n");
							
							//available = Math.Round((decimal)u.Available/1024/1024/1024,1);
							//nsb.Append("available:"+available+",\n");
							nsb.Append("available:"+u.Available+",\n");
							nsb.Append("percent:'"+Math.Round((double)100 - (double)u.Available/u.StorageSize*100,0)+"', \n");
							nsb.Append("shareroot:'"+u.ShareRoot+"', \n");
							nsb.Append("version:'"+u.Version+"',\n");
							nsb.Append("os:'"+u.OS+"',\n");
							// specific to display
							nsb.Append("uiProvider:'col',\n");
							nsb.Append("leaf:true,\n");
							string status = "off";
							if(u.Status == NodeStatus.Locked)
								status="lock"; 
							
							else{
								//foreach (int onlineId in onLineNodes){
								//	if (onlineId == u.Uid){
								if(onLineNodes.ContainsKey(u.Uid)){
										status = "on";
										//nsb.Append("certCN:'"+u.CertCN+"',\n");
										//break;
								}
								//}
							}
							nsb.Append("iconCls:'task-"+status+"'\n");
							nsb.Append("},\n");
							nb++;

						}
						sgTotalSpace += (double)u.StorageSize;
						sgAvailableSpace += (double)u.Available;
					}// end if !groupsOnly
					
					
				} 
				double sgUsedPercent =0;
				if(sgTotalSpace >0)
					sgUsedPercent = Math.Round(100 - sgAvailableSpace/sgTotalSpace*100,0);
				if(nb > 0)
					nsb.Remove(nsb.Length-1,1);
				jsHeader = "\n"+@"{id:'"+sg.Id+"', userName:'"+sg.Name+"', ip:'"+nb+" nodes', share:"+sgTotalSpace+", available:"+sgAvailableSpace
					+", percent:'"+FormatPercent(sgTotalSpace, sgTotalSpace-sgAvailableSpace)+"', uiProvider:'col', cls:'master-task', checked:false, "
					+"iconCls:'task-sg', priority:'"+sg.Priority+"', capabilities:'"+sg.Capabilities+"', children:";
				nsb.Append("]");
				nsb.Append("},\n");
				nsb.Insert(0, jsHeader);
				jsb.Insert(1, nsb.ToString());
					
				
			}
			jsb.Remove(jsb.Length-1,1);
			jsb.Append("]");
			
			
			return(jsb.ToString());
		}
		
		private string BuildBP(List<P2PBackup.Common.BackupSet> bSets, List<P2PBackup.Common.Node> clients, Dictionary<int, NodeStatus> onLineNodes){
			StringBuilder jsb = new StringBuilder();
			//jsb.Append("{backupSets:[");
			jsb.Append("[");
			foreach (BackupSet bs in bSets){
				jsb.Append("{");	
				foreach(P2PBackup.Common.Node n in clients){
					if(n.Uid != bs.NodeId)
						continue;
					string status = "off";
					/*foreach (int onlineId in onLineNodes.Keys){
						if (onlineId == n.Uid)
							status = "on";
					}*/
					if(onLineNodes.ContainsKey(n.Uid)) status = "on";
					jsb.Append("'client':'"+n.NodeName+":"+status+"',");
						
				}
				
				foreach(ScheduleTime bt in bs.ScheduleTimes){
					string pathShort = "";
					//Console.WriteLine ("bt.Day="+bt.Day);
					if(string.IsNullOrEmpty(bs.Name)){
						foreach(BasePath bp in bs.BasePaths)
							pathShort += bp.Path+", ";
						pathShort = pathShort.Remove(pathShort.Length -2 );
					}
					else
						pathShort = bs.Name;
					string roundedBegin = RoundTime(bt.Begin);
					int beg=int.Parse (roundedBegin.Substring(0, roundedBegin.IndexOf(":")));
					int end = -1;
					//int begMin=int.Parse (RoundTime(bt.Begin).Substring(0, RoundTime(bt.Begin).LastIndexOf(":")));
					if(beg==0){
						jsb.Append("'1_"+roundedBegin+"':'b:"+pathShort+"',");
					}
					else{
						jsb.Append("'0_"+roundedBegin+"':'b:"+pathShort+"',");
					}
					if(bt.End != null && bt.End.Length >2){
						int.TryParse (bt.End.Substring(0, bt.End.IndexOf(":")), out end);
						int duration = 0;
						if(end > beg)
							duration = end-beg;
						else
							duration = 24-(beg-end);
						
						jsb.Append("'d':"+duration+",");
						
					}
					else{
						jsb.Append("'d':0,");
					}
					jsb.Append("'type':'"+bt.Level+"', ");
					jsb.Append ("TaskSet:[{");
					jsb.Append("id:'"+bs.Id+"', ");
					jsb.Append("name:'"+bs.Name+"', ");
					jsb.Append("operation:'"+bs.Operation+"', ");
					jsb.Append("level:'"+bt.Level+"', ");
					//jsb.Append("compress:'"+bs.Compress+"', ");
					//jsb.Append("encrypt:'"+bs.Encrypt+"', ");
					//jsb.Append("clientdedup:'"+bs.ClientDedup+"', ");
					jsb.Append("parallelism:'"+bs.Parallelism+"', ");
					jsb.Append("begin:'"+bt.Day+" "+bt.Begin+"', ");
					jsb.Append("end:'"+bt.End+"', ");
					jsb.Append ("BasePath:[");
					foreach(BasePath bsp in bs.BasePaths)
						jsb.Append ("{path:'"+bsp.Path.Replace("\\", @"\\")+"'},");
					jsb.Append ("],");
					jsb.Append ("},],");
				}
				jsb.Append("},");
			}
			if(bSets.Count >0)
				jsb.Remove(jsb.Length-1,1);
			jsb.Append("]");
			return jsb.ToString();
		}
		
		private string BuildUsers(){
			List<User> users = remoteOperation.GetUsers();
			StringBuilder jsb = new StringBuilder();
			jsb.AppendLine("[");
			foreach(User u in users){
				jsb.AppendLine("{");
				jsb.Append("id:"+u.Id+", ");
				jsb.Append("name:'"+u.Name+"', ");
				jsb.Append("email:'"+u.Email+"', ");
				jsb.Append("culture:'"+u.Culture+"', ");
				jsb.Append("language:'"+u.Culture/*.NativeName*/+"', ");
				jsb.Append("enabled:"+u.IsEnabled.ToString().ToLower()+", ");
				jsb.Append("lastlogin:'"+u.LastLoginDate+"', ");
				jsb.Append("checked:false");
				jsb.AppendLine("},");
			}
			jsb.AppendLine("]");
			return jsb.ToString();
		}
		
		private string BuildCultures(){
			Hashtable cultures = remoteOperation.GetCultures();
			StringBuilder jsb = new StringBuilder();
			jsb.AppendLine("[");
			foreach(DictionaryEntry de in cultures){
				jsb.AppendLine("{code:'"+de.Key.ToString()+"', name:'"+de.Value+"'}, ");
			}
			jsb.AppendLine("]");
			return jsb.ToString();
		}
		
		private string RoundTime(string time){
			Console.WriteLine("roundtime, time:'"+time+"'");
			string[] timeA = time.Split(':');
			Console.WriteLine("roundtimer:"+timeA[0]+":"+timeA[1]);
			int minutes = int.Parse(timeA[1]);
			if(minutes < 16)
				timeA[1] = "00";
			else if (minutes < 46)
				timeA[1] = "30";
			else{
				timeA[1] = "00";
				Console.WriteLine ("timeA[0]="+timeA[0]);
				int hours = int.Parse(timeA[0]);
				if(hours < 24)
					timeA[0] = (hours+1).ToString();
				else
					timeA[0] = "0";
				if(timeA[0] == "24") timeA[0] ="0";
			}
			return timeA[0]+":"+timeA[1];
			              
		}
		// THis is also REALLY CRAP: we need to have backuptimes (begin and end) as timespans, not strings... <TODO!!!>
		private string AddHalfHour(string time){
			Console.WriteLine("addhalfhour, time:"+time);
			string[] timeA = time.Split(':');
			Console.WriteLine("addhalfhour:"+timeA[0]+":"+timeA[1]);
			int hours = int.Parse(timeA[0]);
			int minutes = int.Parse(timeA[1]);
			minutes = minutes +30;
			if(minutes > 59){
				minutes = 60-minutes;
				hours = hours +1;
				if(hours > 24)
					hours = 24-hours;
			}
			return hours+":"+minutes;
				
		}
	}
}

