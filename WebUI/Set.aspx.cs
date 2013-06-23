
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.UI;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using P2PBackup.Common;
//using P2PBackupHub;
using SharpBackupWeb.Utilities;

namespace SharpBackupWeb{


	public partial class Set : System.Web.UI.Page{
		
		
		protected  void  Page_Load (object sender, EventArgs args){
			StreamReader reader = new StreamReader(Request.InputStream);
			string rawPostData = reader.ReadLine();
			Console.WriteLine(rawPostData); // for debug only
			
			if(Request.QueryString["w"] == "AddBackupSet")
				Response.Write(ParseAndAddBS(rawPostData));
			else if (Request.QueryString["w"] == "ConfigureNodes"){
				if(Request.QueryString["nodeId"] != null)
					Response.Write(ParseAndProcessConf(int.Parse(Request.QueryString["nodeId"]), rawPostData));
			}
			else if (Request.QueryString["w"] == "ApproveNodes"){
				string approve = Request.QueryString["approve"];
				Response.Write(ApproveNodes(rawPostData, approve));
			}
			else if (Request.QueryString["w"] == "ChangeTasks"){
				string action = Request.QueryString["action"];
				//Console.WriteLine ("action="+action);
				IRemoteOperations rOp = RemotingManager.GetRemoteObject();
				Response.Write( rOp.ChangeTasks(rawPostData.Split(new char[] {','}, 
					StringSplitOptions.RemoveEmptyEntries).Select(n => long.Parse(n)).ToList<long>(), 
					(TaskAction)Enum.Parse(typeof(TaskAction), action)));
			}
		}
		
		private string ApproveNodes(string nodesList, string approve){
			IRemoteOperations rOp = RemotingManager.GetRemoteObject();
			string outMsg = String.Empty;
			bool doApprove = false;
			try{
				doApprove = bool.Parse(approve);
			}
			catch(Exception boolE){
				outMsg += "Could not parse parameter as bool type :"+boolE.Message;
				//Console.WriteLine("ApproveNodes : invalid status : "+boolE.Message);
				return outMsg;
			}
			foreach(string nid in nodesList.Split(',')){
				try{
					if(nid != null && nid != String.Empty)
						rOp.ApproveNode(int.Parse(nid), doApprove);
				}
				catch(Exception e){
					outMsg	+= ""+e.Message+"---"+e.StackTrace+",";
				}
			}
			return outMsg;
		}
		
		private string ParseAndAddBS(string rawData){
			ArrayList clientIds;
			ArrayList paths;
			if(rawData.Substring(0,2) == "[]")
				return "You must select at least 1 node which will process this backup set";
			int endOfClients = rawData.IndexOf("]],");
			clientIds = GetClientIds(rawData.Substring(2,endOfClients-2));
			if(clientIds.Count == 0)
				return "You must select at least one client to backup";
			if(rawData.Substring(endOfClients, 3) == "[]")
				return "You must select at least one path to backup";
			int endOfPaths = rawData.IndexOf('{');
			paths = GetPaths(rawData.Substring(endOfClients, endOfPaths - endOfClients));
			int endOfFilesSelection = rawData.IndexOf('}');
			string jGroupBk = rawData.Substring(endOfPaths+1, endOfFilesSelection - endOfPaths -1);
			string rootPath = GetValueFromJson("basePath", jGroupBk);
			if(paths.Count == 0 || rootPath == null)
				return "You must select at least one path to backup";
			string includePolicy = GetValueFromJson("includePolicy", jGroupBk);
			string excludePolicy = GetValueFromJson("excludePolicy", jGroupBk);
			bool compress = (GetValueFromJson("compress", jGroupBk) == "on")?true:false;
			bool encrypt = (GetValueFromJson("encrypt", jGroupBk) == "on")?true:false;
			int endOfOps = rawData.IndexOf('}',endOfFilesSelection - endOfPaths );
			string jGroupOps = rawData.Substring(endOfFilesSelection - endOfPaths, endOfOps);
			string preops = GetValueFromJson("preops", jGroupOps);
			string postops = GetValueFromJson("postops", jGroupOps);
			Console.WriteLine("rootPath="+rootPath+",includepolicy="+includePolicy+"excludepolicy="+excludePolicy+",compress="+compress);
			// getting per-day backup scheduling
			string rawSchedulingInfo = rawData.Substring(endOfOps);
			
			List<ScheduleTime> bTimes = new List<ScheduleTime>();
			List<BasePath> bPaths = new List<BasePath>();
			for(int i=0; i<7; i++){
				string day = ((DayOfWeek)i).ToString();	
				if(GetValueFromJson(day+"Do", rawSchedulingInfo) == "on"){
					if(GetValueFromJson(day+"Hour", rawSchedulingInfo).Split(':').Length == 2
						  && GetValueFromJson(day+"Type", rawSchedulingInfo) != null){
							BackupLevel bType = (BackupLevel)Enum.Parse(typeof(BackupLevel), GetValueFromJson(day+"Type", rawSchedulingInfo));
							ScheduleTime bt = new ScheduleTime(bType, (DayOfWeek)i, GetValueFromJson(day+"Hour", rawSchedulingInfo), string.Empty);
							bTimes.Add(bt);
					}
				}
			}
			if(rootPath != null){
				BasePath bp =new BasePath();
				bp.Path = rootPath;
				bp.IncludePolicy.Add(includePolicy);
				bp.ExcludePolicy.Add(excludePolicy);
				bPaths.Add(bp);
			}
			foreach(string thePath in paths){
				BasePath bp = new BasePath();
				bp.Path = thePath;
				bp.IncludePolicy.Add(includePolicy);
				bp.ExcludePolicy.Add(excludePolicy);
				bPaths.Add(bp);
			}
			IRemoteOperations rOp = RemotingManager.GetRemoteObject();
			string createResult = null;
			foreach (int client in clientIds){
				BackupSet newBs = new BackupSet();
				newBs.NodeId = client;
				newBs.ScheduleTimes = bTimes;
				newBs.BasePaths = bPaths;
				//newBs.Compress = compress;
				//newBs.Encrypt = encrypt;
				newBs.Preop = preops;
				newBs.Postop = postops;
				createResult += ","+rOp.CreateBackupSet(newBs);
			}
			if ( createResult == null)
				return "New BackupSet successfully created on Hub.";
			else
				return createResult;
		}
		
		private string ParseAndProcessConf(int nodeId, string rawPostData){
			try{
				/*string logLevel = Request.Params["Logger.Level"];
				string logFile = Request.Params["Logger.LogFile"];
				bool syslog = bool.Parse(Request.Params["Logger.Syslog"]);
				string backupTempFolder = Request.Params["Backup.TempFolder"];
				string backupIndexFolder = Request.Params["Storage.IndexFolder"];
				
				bool isStorageNode = bool.Parse(Request.Params["istorageNode"]);
				int storageGroup = int.Parse(Request.Params["Storage.StorageGroup"]);
				IPAddress listenIP = IPAddress.Parse(Request.Params["Storage.ListenIP"]);
				int listenPort = int.Parse(Request.Params["Storage.ListenPort"]);
				string storageDir = Request.Params["Storage.Directory"];
				
				*/
				Hashtable nodeConfig = new Hashtable();
				foreach(string key in Request.Params){
					nodeConfig.Add(key, Request.Params[key]);
					Console.WriteLine(key+"="+Request.Params[key]);
				}
				IRemoteOperations rOp = RemotingManager.GetRemoteObject();
				//rOp.SetNodeConfig(nodeId, null, logLevel, syslog, logFile, listenIP.ToString(), listenPort, backupTempFolder, backupIndexFolder, shareSize);
				// We don't do params validation at this stage. On hub side, the method savenodeconfig will ignore every key that doesn't exist in nodes config template (db id=0)
				
				rOp.SetNodeConfig(nodeId, nodeConfig);
				long? storageSize = long.Parse(Request.Params["shareSize"]);
				long? quota = long.Parse(Request.Params["quota"]);
				int? storageGroup = int.Parse(Request.Params["storageGroup"]);
				int? nodeGroup = int.Parse(Request.Params["nodeGroup"]);
				rOp.UpdateNodeGeneralConf(nodeId, storageSize, quota, storageGroup, nodeGroup);
				return "OK";
					
			}
			catch(Exception e){
				Response.StatusCode = 500;
				return (e.Message+":"+e.TargetSite);
			}
			return "ok";
		}
		
		
		private string GetValueFromJson(string key, string jsonKVP){
			
			jsonKVP = jsonKVP.Replace("\"","").Replace("{","").Replace("}","");
			Console.WriteLine("GetValueFromJson, json="+jsonKVP);
			string[] kvp = jsonKVP.Split(',');
			Regex keyValuePair = new Regex("("+key+")"+Regex.Escape(":")+@"(?<val>.*)");
			Console.WriteLine(keyValuePair.ToString());
			foreach (string kv in kvp){
				Console.WriteLine("GetValueFromJson, searching for "+key+" in "+kv);
				Match m = keyValuePair.Match(kv);
				if(m.Success){
					Console.WriteLine("GetValueFromJson, success, got  "+m.Groups["val"].Value);
					return m.Groups["val"].Value;

				}
			}
			return null;
		}
		
		private ArrayList GetPaths(string rawPaths){
			ArrayList pathsList = new ArrayList();
			rawPaths = rawPaths.Replace("path","");
			rawPaths = rawPaths.Replace("[","");
			rawPaths = rawPaths.Replace("]","");
			rawPaths = rawPaths.Replace("\"","");
			Console.WriteLine("GetPaths, raw ="+rawPaths);
			string[] paths = rawPaths.Split(',');
			foreach (string path in paths)
				if(path != null && path != string.Empty)
					pathsList.Add(path);
			Console.WriteLine("GetPaths, got "+pathsList.Count);

			return pathsList;
			
		}
		
		private ArrayList GetClientIds(string rawClients){
			ArrayList ids = new ArrayList();
			// enlever guillemets, 'true', crochets
			rawClients = rawClients.Replace("true","");
			rawClients = rawClients.Replace("[","");
			rawClients = rawClients.Replace("]","");
			rawClients = rawClients.Replace("\"","");
			Console.WriteLine("GetClientIds:  "+rawClients);
			string[] clients = rawClients.Split(',');
			foreach(string client in clients){
				int clientId = 0;
				int.TryParse(client, out clientId);	
				if(clientId >0)
					ids.Add(clientId);
			}
			Console.WriteLine("GetClientIds, got "+ids.Count);
			return ids;
		}
	}
}

