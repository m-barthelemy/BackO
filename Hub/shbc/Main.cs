using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.CSharp;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Reflection;
using System.ServiceModel;
using System.Security;
using System.Security.Principal;
using P2PBackup.Common;
using Irony.Parsing;
//test
/*using System.Data;
using System.Xml;*/
/*
using System.Linq.Expressions;
using System.Linq;
using System.Linq.Expressions.Compiler;*/
// TODO !!   look as this implementaiton : https://github.com/migueldeicaza/muget/blob/master/getline.cs


namespace shbc{
	class MainClass	{
		static string[] operations = {"discover", "get", "select", "list", "add",  "update", "set", "delete", "start", "stop", "pause", "cancel" , "help", "quit", "exit", "shutdown", "who", "restore"};
		static string[] operationTargets = {"configuration", "conf", "certificates", "currentuser", "basepath", "backupsets", "backupset", "history", "hypervisors", "hypervisor", "nodes", "node", "onlinenodes", "nodegroups", "plan", "backupplan", "tasks", "task", "runningtasks", "sessions",  "storagegroups", "storagenodes", "tasksets", "taskset", "users", "logs"};
		static string[] restrictops = {"where", "order by", "limit"};

		// mapping between the names of objects to retreive and their type
		static Dictionary<string, Type> targetPairs = new Dictionary<string, Type>{
			{"nodes", typeof(P2PBackup.Common.Node)},
			{"onlinenodes", typeof(P2PBackup.Common.Node)},
			{"node",  typeof(P2PBackup.Common.Node)},
			{"task",  typeof(Task)},
			{"tasks",  typeof(Task)},
			{"runningtasks",  typeof(Task)},
			{"basepath",  typeof(BasePath)},
			{"tasksets",  typeof(BackupSet)},
			{"taskset",  typeof(BackupSet)},
			{"currentuser",  typeof(User)},
			{"users",  typeof(User)},
			{"logs",  typeof(LogEntry)},
			{"history",  typeof(Task)},
			{"plan",  typeof(Task)},
			{"configuration",  typeof(DictionaryEntry)},
			{"certificates",  typeof(NodeCertificate)},
			{"sessions",  typeof(PeerSession)},
			{"storagenodes",  typeof(P2PBackup.Common.Node)},
			{"storagegroups",  typeof(StorageGroup)},
			{"nodegroups",  typeof(NodeGroup)},
			{"hypervisors",  typeof(Hypervisor)},
			{"hypervisor",  typeof(Hypervisor)},
		};
		static string serverIP = "127.0.0.1";
		static int serverPort = 9999;
		static string userName = "";
		static string userPassword = "";
		static bool quietMode;
		static bool simpleQueryMode;
		static IRemoteOperations remote;
		static bool logged = false;
		static ConsoleColor defaultColor;
		static bool displayPropName = false;
		static bool writeHeader = true;
		static bool limitSize = true;
		static string fs=", "; // field separator
		static string fvs = "="; // field-value separator
		static string rs = Environment.NewLine; // row/record separator
		static string[] properties; // properties to display, when using "GET a,b,c,d FROM" syntax
		static string lastQuery = "";
		static int position = 0;

		// testing purposes only
		static bool ironyParse;

		public static void Main (string[] args){
			
			AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
			if(args.Length == 0) PrintHelp ();
			defaultColor = Console.ForegroundColor;
			for(int i=0; i<args.Length; i++){
				switch(args[i]){
					
				case "--ip":case "-i":
					i++;
					serverIP = args[i];
					break;
				case "--silent":case "-s":
					quietMode = true;
					break;
				
				case "--user":case "-u":
					i++;
					if(args.Length >= i)
						RemotingManager.User = args[i];
					break;
					
				case "--password":case "-p":
					i++;
					if(args.Length >= i)
						RemotingManager.Password = args[i];
					
					break;
					
				case "--field-separator":case "-fs":case "--fs":
					i++;
					if(args.Length >= i)
						fs = @args[i];
					
					break;
				
				case "--field-value-separator":case "-fvs":case "--fvs":
					i++;
					if(args.Length >= i)
						fvs = @args[i];
					
					break;
					
				case "--row-separator":case "-rs":case "--rs":case "record-separator":
					i++;
					if(args.Length >= i)
						rs = @args[i];
					
					break;
					
				case "--query":case "-q":
					simpleQueryMode = true;
					if(RemotingManager.User == "" || RemotingManager.Password == "")
						PrintHelp();
					Login();
					i++;
					string query = "";
					while (i<args.Length){
						query += args[i]+" ";
						i++;
					}
					ParseQueryOp(query, false);
					break;
				// read commands from stdin:
				case "--irony":
					ironyParse = true;
					break;
				case "-":
					Login ();
					string stdInQ = "";
					while( (stdInQ = Console.In.ReadLine()) != null)
						ParseQueryOp(stdInQ, false);
					Environment.Exit(0);
					break;
				default:
					PrintHelp();
					break;
				}
				
			}
			
			if(!simpleQueryMode){
				if (!ironyParse) {
					Login ();
					Interact ();
				} else
					InteractIrony ();
			}
			
		}
		
		private static void Login(){
			
			try{


				//var binding = new WSHttpBinding();
				/*var binding = new NetTcpBinding(SecurityMode.None, true);
				binding.Security.Mode = SecurityMode.None;
				binding.OpenTimeout = new TimeSpan(1,0,0);
				binding.SendTimeout = new TimeSpan(1,0,0);
				binding.CloseTimeout = new TimeSpan(1,0,0);
				binding.MaxBufferSize = 2000000000;
				binding.MaxReceivedMessageSize = 2000000000;
				binding.MaxBufferPoolSize = 200000000;
				//Console.WriteLine ("binding.MaxReceivedMessageSize="+binding.MaxReceivedMessageSize);

				var address = new EndpointAddress ("net.tcp://"+serverIP+":"+serverPort+"/");
				remote = new ChannelFactory<IRemoteOperations> (binding, address).CreateChannel();*/

				if(! RemotingManager.GetRemoteObject().Login(RemotingManager.User, RemotingManager.Password)){
					Error("Could not login : invalid user or password", null);
					Environment.Exit (5);	
				}
				

			}
			catch(Exception e){
				Error ("Cannot connect and login to server '"+serverIP+"' : "+e.ToString(), e);	
				Environment.Exit(2);
			}
		}


		/* Irony Tests */


		private static void Flatten(ParseTreeNode node, List<ParseTreeNode> nodes)
		{
			nodes.Add(node);

			foreach (ParseTreeNode child in node.ChildNodes)
			{
				Flatten(child, nodes);
			}
		}

		private static string IronyParse(string query){
			Console.WriteLine ("Parsing : "+query);
			LanguageData language = new LanguageData(new BackOGrammar());
			Parser parser = new Parser(language);
			ParseTree parseTree = parser.Parse(query);
			ParseTreeNode root = parseTree.Root;
			//return root != null;
			List<ParseTreeNode> nodes = new List<ParseTreeNode>();
			// Flatten the nodes for easier processing with LINQ
			Flatten(root, nodes);

			Console.WriteLine ("parsed : "+root != null+", root="+root.ToString());
			Console.WriteLine("Flattened tree : "+string.Join(" # ", nodes));
			ParseTreeNode actionKind = root.ChildNodes[0];
			switch(actionKind.Term.Name){
			case "selectStmt":
				Console.WriteLine ("request type : SELECT, has "+actionKind.ChildNodes.Count+" child");

				break;
			}
			return "parsed : "+(root != null)+", root="+root.ToString();
		}


		private static string ParseQueryOp(string query, bool autoComplete){


			if(query.Length == 0) return null;
			int opSeparatorPos = query.IndexOf(" ");
			if(opSeparatorPos <1) opSeparatorPos = query.Length;
			string opr = null;
			//try{
				opr = query.ToLower().Substring(0, opSeparatorPos);
			
			//}catch{return string.Empty;} 
			// protect from "get" not followed by instruction separator (space):
			//if(query.IndexOf(" ") <1) return string.Empty;
			switch(opr){
				case "discover":
					int hvId = int.Parse (query.Substring(opSeparatorPos+1/*, 1*/));
					Console.WriteLine ("Discovering VMs on hypervisor "+hvId);
					WriteObjects(RemotingManager.GetRemoteObject().Discover(hvId), targetPairs["nodes"], null , "");
					break;
				case "get":case "select":
					return ProcessGetRequest(query.Substring(opSeparatorPos+1), autoComplete);
					break;
				case "start": case "pause": case "stop": case "cancel":
					ProcessTaskRequest(opr, query.Substring(opSeparatorPos+1));
					break;
				case "update":
					return ProcessUpdateRequest(query.Substring(opSeparatorPos+1), autoComplete);
					break;
				case "exit": case "quit":
					Environment.Exit(0);
					break;
				case "help":
					PrintInteractiveHelp();
					break;
				case "whoami":case"who":
					Console.WriteLine(RemotingManager.GetRemoteObject().WhoAmI());
					break;
				case "restore":
					ProcessRestoreRequest(query.Substring(opSeparatorPos+1));
					break;
				case "shutdown": 
					ShutdownHub();
					break;
				case "echo": // may be used in scripts
					Console.WriteLine (query.Substring(opSeparatorPos+1));
					break;
				default:
					if(!autoComplete)
						goto error;
					else{
						
						foreach(string op in operations)
							if(op.IndexOf(opr) == 0)
								return op.Substring(op.IndexOf(opr)+opr.Length)+" ";
					}
					break;
			}
			return string.Empty;
			error:
				lastQuery = query;
				Error("unknown command '"+query.ToLower().Substring(0, opSeparatorPos)+"'. syntax : [get|add|update|start|pause|cancel|restore|help|exit]", null);
			
			return string.Empty;
		}
		
		private static string ProcessUpdateRequest(string query, bool autoComplete){
			if(!autoComplete) lastQuery = query;
			string item = "";
			int opSeparatorPos;
			//try{
				if(query == "" || query == null) return string.Empty;
				
				// search if query has the form "get xx,yy,zz FROM"
				/*int fromIndex = query.ToLower().IndexOf(" from ");
				if(fromIndex >=0){
					properties = query.Substring(0, fromIndex).Split(new char[]{','});
					query = query.Substring(fromIndex+6);
					//opSeparatorPos = query.Substring(fromIndex+6).IndexOf(" ");
				}*/
				opSeparatorPos = query.IndexOf(" ");
				string whereClause = null;
				string obClause = null;
				if(opSeparatorPos >0){
					item = query.ToLower().Substring(0, opSeparatorPos+1);
					//whereClause = query.Substring(opSeparatorPos);
					if(query.IndexOf(" where ") >=0){
						whereClause = query.Substring(query.IndexOf(" where "));
						//string propToComplete = 
						
						// now see it user reclaims properties completion
						if(autoComplete){
							Type curType = null;
							targetPairs.TryGetValue(item, out curType);
							//Console.WriteLine("type="+curType+", item='"+item+"'");
							if(curType != null){
								
								//Type t = Type.GetType("LogEntry");
								
								//Console.WriteLine("type type="+t.FullName);
								PropertyInfo[] rawProps = (curType.GetProperties(BindingFlags.Public | BindingFlags.Instance));
								foreach(PropertyInfo pi in rawProps){
									//Console.Write ("#"+pi.Name+", wh='"+"'#");
									if(pi.Name.ToLower().IndexOf(whereClause.Substring(7))==0)
										return pi.Name.Substring(whereClause.Substring(7).Length);
								}
							}
							
						}
							
					}
					if (autoComplete){ // 'where' not written, see if reclaimed (user typed 'w' for ex)
						string restrictComplBase = query.Substring(query.IndexOf(item)+item.Length+1);
						//Console.WriteLine ("restrict compl for '"+restrictComplBase +"'");
						foreach(string restrictop in restrictops)
							if(restrictop.IndexOf(restrictComplBase) >=0)
								return restrictop.Substring(restrictop.IndexOf(restrictComplBase)+restrictComplBase.Length)+" ";
					}
				}
				else
					item = query.ToLower();
				string queryAfterItem = query.Substring (query.IndexOf(item)+item.Length);
				//Console.WriteLine("queryAfterItem="+queryAfterItem);
				switch(item){
				case "node ":
					uint nodeId = 0;
					int actionSeparatorPos = queryAfterItem.IndexOf(" ");
					try{
						nodeId = uint.Parse(queryAfterItem.Substring(0, actionSeparatorPos));
					}
					catch(Exception e){
						Error ("Error. Syntax : UPDATE NODE #nodeid [SET <property>=<value>	] [lock|unlock]", e);
					}
					string action = queryAfterItem.Substring(actionSeparatorPos+1);
					switch(action){
						case "unlock":
							RemotingManager.GetRemoteObject().ApproveNode(nodeId, false);
							break;
						case "lock":
							RemotingManager.GetRemoteObject().ApproveNode(nodeId, true);
							break;
						case "set":
							Error ("lioulioulioulioute", null);
							break;
						default:
							Error("Valid 'node' actions are SET, LOCK, UNLOCK", null);	
							break;
					}
					break;
				case "tasks ":case "jobs":

					break;
				default:
					if(!autoComplete)
						Error("unknown object '"+item+"'. available objects: "+string.Join(",", operationTargets), null);
					else{
						foreach(string op in operationTargets)
							if(op.IndexOf(item) == 0)
								return op.Substring(op.IndexOf(item)+item.Length)+" ";
					}
					break;
				}
					
			return "";
		}
		
		private static string ProcessGetRequest(string query, bool autoComplete){
			if(!autoComplete) lastQuery = query;
			string item = "";
			int opSeparatorPos;
			try{
				if(query == "" || query == null) return string.Empty;
				
				// search if query has the form "get xx,yy,zz FROM"
				int fromIndex = query.ToLower().IndexOf(" from ");
				if(fromIndex >=0){
					properties = query.Substring(0, fromIndex).Split(new char[]{','});
					query = query.Substring(fromIndex+6);
					//opSeparatorPos = query.Substring(fromIndex+6).IndexOf(" ");
				}
				opSeparatorPos = query.IndexOf(" ");
				string whereClause = null;
				string obClause = null;
				if(opSeparatorPos >0){
					item = query.ToLower().Substring(0, opSeparatorPos);
					//whereClause = query.Substring(opSeparatorPos);
					if(query.IndexOf(" where ") >=0){
						whereClause = query.Substring(query.IndexOf(" where "));
						//string propToComplete = 
						
						// now see it user reclaims properties completion
						if(autoComplete){
							Type curType = null;
							targetPairs.TryGetValue(item, out curType);
							//Console.WriteLine("type="+curType+", item='"+item+"'");
							if(curType != null){
								
								//Type t = Type.GetType("LogEntry");
								
								//Console.WriteLine("type type="+t.FullName);
								PropertyInfo[] rawProps = (curType.GetProperties(BindingFlags.Public | BindingFlags.Instance));
								foreach(PropertyInfo pi in rawProps){
									//Console.Write ("#"+pi.Name+", wh='"+"'#");
									if(pi.Name.ToLower().IndexOf(whereClause.Substring(7))==0)
										return pi.Name.Substring(whereClause.Substring(7).Length);
								}
							}
							
						}
							
					}
					if(query.ToLower().IndexOf(" order by ") >=0){
						obClause = query.Substring(query.ToLower().IndexOf(" order by ")+10);
					}
					else if (autoComplete){ // 'where' not written, see if reclaimed (user typed 'w' for ex)
						string restrictComplBase = query.Substring(query.IndexOf(item)+item.Length+1);
						//Console.WriteLine ("restrict compl for '"+restrictComplBase +"'");
						foreach(string restrictop in restrictops)
							if(restrictop.IndexOf(restrictComplBase) >=0)
								return restrictop.Substring(restrictop.IndexOf(restrictComplBase)+restrictComplBase.Length)+" ";
					}
				}
				else
					item = query.ToLower();
				switch(item){
				case "backupplan": case "plan":
					//if(RemotingManager.GetRemoteObject().GetBackupPlan(120) == null) return string.Empty;
					WriteObjects(RemotingManager.GetRemoteObject().GetBackupPlan(120), targetPairs[item], whereClause, obClause);
					break;
				case "tasks":case "jobs":
					//if(RemotingManager.GetRemoteObject().GetRunningTasks() == null) return string.Empty;
					WriteObjects(RemotingManager.GetRemoteObject().GetRunningTasks (), targetPairs[item], whereClause, obClause);
					/*foreach(Task t in RemotingManager.GetRemoteObject().GetRunningTasks()){
						Console.Write ("id"+fvs+t.TrackingId+fs+"node"+fvs+t.UserId+fs+"operation"+fvs+t.Operation+fs+"name"+fvs+t.BackupSet.Name+fs+"type"+fvs+t.Type+fs+"status"+fvs+t.RunningStatus+fs+"%"+fvs+t.CompletionPercent+fs+"current_action"+fvs+t.CurrentAction+rs);
					}*/
					break;
				case "tasksets":
					//if(RemotingManager.GetRemoteObject().GetTaskSets() == null) return string.Empty;
					WriteObjects(RemotingManager.GetRemoteObject().GetBackupSets(0, Int32.MaxValue, false), targetPairs[item], whereClause, obClause);
					/*foreach(Task t in RemotingManager.GetRemoteObject().GetRunningTasks()){
						Console.Write ("id"+fvs+t.TrackingId+fs+"node"+fvs+t.UserId+fs+"operation"+fvs+t.Operation+fs+"name"+fvs+t.BackupSet.Name+fs+"type"+fvs+t.Type+fs+"status"+fvs+t.RunningStatus+fs+"%"+fvs+t.CompletionPercent+fs+"current_action"+fvs+t.CurrentAction+rs);
					}*/
					break;
				case "hypervisors":
					//if(RemotingManager.GetRemoteObject().GetTaskSets() == null) return string.Empty;
					WriteObjects(RemotingManager.GetRemoteObject().GetHypervisors(), targetPairs[item], whereClause, obClause);
					/*foreach(Task t in RemotingManager.GetRemoteObject().GetRunningTasks()){
						Console.Write ("id"+fvs+t.TrackingId+fs+"node"+fvs+t.UserId+fs+"operation"+fvs+t.Operation+fs+"name"+fvs+t.BackupSet.Name+fs+"type"+fvs+t.Type+fs+"status"+fvs+t.RunningStatus+fs+"%"+fvs+t.CompletionPercent+fs+"current_action"+fvs+t.CurrentAction+rs);
					}*/
					break;
				case "taskset":
					fs=Environment.NewLine;
					fvs=" : ";
					displayPropName = true;
					writeHeader = false;
					limitSize = false;
					int tid = 0;
					int.TryParse(query.ToLower().Substring(opSeparatorPos).Trim(), out tid);
					/*foreach(BackupSet bs in RemotingManager.GetRemoteObject().GetTaskSets()){
						if(bs.Id != tid) continue;
						WriteObject (bs, GetPropertiesAndFormatting(properties, targetPairs[item]));
					}*/
					WriteObject (RemotingManager.GetRemoteObject().GetTaskSet(tid), GetPropertiesAndFormatting(properties, targetPairs[item]));
					displayPropName = false;
					writeHeader = true;
					limitSize = true;
					break;
				case "basepath":
					fs=Environment.NewLine;
					fvs=" : ";
					displayPropName = true;
					writeHeader = false;
					limitSize = false;
					//int tid = 0;
					int.TryParse(query.ToLower().Substring(opSeparatorPos).Trim(), out tid);
					foreach(BasePath bp in RemotingManager.GetRemoteObject().GetTaskSet(tid).BasePaths)
						WriteObject (bp, GetPropertiesAndFormatting(properties, targetPairs[item]));
					displayPropName = false;
					writeHeader = true;
					limitSize = true;
					break;
				case "history":// ./System.ServiceModel/Test/System.ServiceModel.Channels/BinaryMessageEncodingBindingElementTest.cs : max size=65536
					if(whereClause == null){
						int totalCount = 0;
						WriteObjects(RemotingManager.GetRemoteObject().GetTasksHistory(null, DateTime.Now.Subtract(new TimeSpan(12,0,0)), DateTime.Now, null, null, 0, 1000, 0, out totalCount), targetPairs[item], whereClause, obClause);     
					}
					break;
				case "nodes":case "clients":
					//if(RemotingManager.GetRemoteObject().GetNodes() == null) return string.Empty;
					/*foreach(P2PBackup.Common.Node n in RemotingManager.GetRemoteObject().GetNodes()){
						Console.Write ("id"+fvs+n.Uid+fs+"name"+fvs+n.NodeName+fs+"version"+fvs+n.Version+fs+"listen_ip"+fvs+n.ListenIp+":"+n.ListenPort+rs);
					}*/
					Dictionary<uint, NodeStatus> onlineIds = RemotingManager.GetRemoteObject().GetOnlineClients();
					List<P2PBackup.Common.Node> nodes = new List<P2PBackup.Common.Node>();
					foreach(P2PBackup.Common.Node n in RemotingManager.GetRemoteObject().GetNodes(null)){
						if(onlineIds.ContainsKey(n.Id)){
							n.Status = onlineIds[n.Id];
						   
						}
						nodes.Add(n);
					}
					WriteObjects(nodes, targetPairs[item], whereClause, obClause);
					break;
				case "node":
					fs=Environment.NewLine;
					fvs=" : ";
					displayPropName = true;
					writeHeader = false;
					limitSize = false;
					uint nodeId = 0;
					uint.TryParse(query.ToLower().Substring(opSeparatorPos).Trim(), out nodeId);
					//foreach(Node n in RemotingManager.GetRemoteObject().GetNodes()){
					//	if(n.Uid != nodeId) continue;
						WriteObject (RemotingManager.GetRemoteObject().GetNode(nodeId), GetPropertiesAndFormatting(properties, targetPairs[item]));
					//}
					Console.WriteLine (Environment.NewLine+"Configuration:");
					displayPropName = false;
					//WriteObject(RemotingManager.GetRemoteObject().GetNode(nodeId).Configuration, typeof(NodeConfig), whereClause, obClause);
					WriteObject(RemotingManager.GetRemoteObject().GetNode(nodeId).Configuration,null);
					writeHeader = true;
					limitSize = true;
					break;
				case "onlinenodes":
					Dictionary<uint, NodeStatus> online = RemotingManager.GetRemoteObject().GetOnlineClients();
					List<P2PBackup.Common.Node> onlineNodes = new List<P2PBackup.Common.Node>();
					foreach(P2PBackup.Common.Node n in RemotingManager.GetRemoteObject().GetNodes(null))
						if(online.ContainsKey(n.Id)){
							n.Status = online[n.Id];
						   onlineNodes.Add(n);
						}
					WriteObjects(onlineNodes, targetPairs[item], whereClause, obClause);
					break;
				case "certificates":
					WriteObjects(RemotingManager.GetRemoteObject().GetCertificates(), targetPairs[item], whereClause, obClause);
					break;
				case "sessions":
					WriteObjects(RemotingManager.GetRemoteObject().GetSessions(), targetPairs[item], whereClause, obClause);
					break;
				case "logs":case "log":
					//if(RemotingManager.GetRemoteObject().GetLogBuffer() == null) return string.Empty;
					/*foreach(string log in RemotingManager.GetRemoteObject().GetLogBuffer()){
						if(log != null)
						Console.WriteLine(log);
					}*/
					WriteObjects(RemotingManager.GetRemoteObject().GetLogBuffer().ToList<LogEntry>(), targetPairs[item], whereClause, obClause);
					break;
				case "users":
					if(RemotingManager.GetRemoteObject().GetUsers() == null) return string.Empty;
					/*foreach(User u in RemotingManager.GetRemoteObject().GetUsers()){
						Console.WriteLine("id"+fvs+u.Id+fs+"name"+fvs+u.Name+fs+"last_login"+fvs+u.LastLoginDate+fs+"mail"+fvs+u.Email);
					}*/
					WriteObjects (RemotingManager.GetRemoteObject().GetUsers(), targetPairs[item], whereClause, obClause);
					break;
				case "task":case"job":
					fs=Environment.NewLine;
					fvs=" : ";
					displayPropName = true;
					writeHeader = false;
					limitSize = false;
					int taskid = 0;
					int.TryParse(query.ToLower().Substring(opSeparatorPos).Trim().Replace ("#",""), out taskid);
					Task wantedTask = null;
					foreach(Task t in RemotingManager.GetRemoteObject().GetRunningTasks()){
						if(t.Id != taskid) continue;
						if(properties == null || (properties.Length == 1 && properties[0] == "*")){
							PropertyInfo[] rawProps = (t.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance));
							properties = new string[rawProps.Length];
							int i=0;
							foreach(PropertyInfo pi in rawProps){
									properties[i] = pi.Name;
									i++;
							}
						}
						wantedTask = t;


					}
					// if not found in in running tasks, search archived ones
					if(wantedTask == null)
						wantedTask = RemotingManager.GetRemoteObject().GetTaskHistory(taskid);
					if(wantedTask == null){
						Error ("Couldn't find a task with Id #"+taskid, null);
						displayPropName = false;
						writeHeader = true;
						limitSize = true;
						return "";
					}
					//Console.WriteLine ("id"+fvs+t.Id+fs+"node"+fvs+t.UserId+fs+"operation"+fvs+t.Operation+fs+"name"+fvs+t.BackupSet.Name+fs+"type"+fvs+t.Type+fs+"status"+fvs+t.RunStatus+fs+"%"+fvs+t.Percent+fs+"current_action"+fvs+t.CurrentAction+fs+"original_size"+fvs+t.OriginalSize+fs+"final_size"+fvs+t.FinalSize);
					WriteObject (wantedTask, GetPropertiesAndFormatting(properties, targetPairs[item]));
					Console.Write (Environment.NewLine+"Log entries:"+Environment.NewLine);
					if(wantedTask.LogEntries != null)
						foreach (TaskLogEntry entry in wantedTask.LogEntries){
							Console.WriteLine (entry.Date+", "+entry.Code+", "+Messages.GetMessage(entry.Code, entry.Message1, entry.Message2));	
						}
					displayPropName = false;
					writeHeader = true;
					limitSize = true;
					break;
				case "nodegroups": 
					if(RemotingManager.GetRemoteObject().GetNodeGroups() == null) return string.Empty;
					WriteObjects(RemotingManager.GetRemoteObject().GetNodeGroups(), targetPairs[item], whereClause, obClause);
					break;
				case "storagenodes":
					if(RemotingManager.GetRemoteObject().GetStorageNodes(null) == null) return string.Empty;
					WriteObjects(RemotingManager.GetRemoteObject().GetStorageNodes(null), targetPairs[item], whereClause, obClause);
					break;
				case "storagegroups":
					if(RemotingManager.GetRemoteObject().GetStorageGroups() == null) return string.Empty;
					WriteObjects (RemotingManager.GetRemoteObject().GetStorageGroups(), targetPairs[item], whereClause, obClause);
					break;
				case "conf": case "configuration":
					//if(RemotingManager.GetRemoteObject().GetConfigurationParameters() == null) return string.Empty;
					WriteObjects(RemotingManager.GetRemoteObject().GetConfigurationParameters(), targetPairs[item], whereClause, obClause);
					break;
				case "currentuser": case "whoami":
					/*var tempU = new List<User>();
					tempU.Add(RemotingManager.GetRemoteObject().GetCurrentUser());
					WriteObjects(tempU, whereClause, obClause);*/
					fs=Environment.NewLine;
					fvs=" : ";
					displayPropName = true;
					writeHeader = false;
					limitSize = false;
					WriteObject (RemotingManager.GetRemoteObject().GetCurrentUser(), GetPropertiesAndFormatting(properties, targetPairs[item]));
					displayPropName = false;
					writeHeader = true;
					limitSize = true;
					break;
				case "*":
					if(autoComplete)
					return " FROM ";
					break;
				default:
					if(!autoComplete)
						Error("unknown object '"+item+"'. available objects: "+string.Join(",", operationTargets), null);
					else{
						foreach(string op in operationTargets)
							if(op.IndexOf(item) == 0)
								return op.Substring(op.IndexOf(item)+item.Length)+" ";
					}
					break;
				}
			}
			catch(System.ServiceModel.Security.SecurityAccessDeniedException e){
				Error("The account you used to authenticate has been denied the right to perform operation '"+item+"' ", e);
			}
			return string.Empty;
		}
		
		private static void ProcessTaskRequest(string action, string parameters){
			string[] taskParams = parameters.Split(new char[]{' '});
			long bsId = 0;
			long.TryParse(taskParams[0], out bsId);
			switch (action.ToLower()){
				case "start":
					bool hasLevel = false;
					if(bsId >=0){
						long taskId = 0;
						try{
							BackupLevel level = BackupLevel.Default;
							if(taskParams.Length >1 && taskParams[1].ToLower() == "level"){

								hasLevel = Enum.TryParse(taskParams[2], true, out level);
								Console.WriteLine ("custom level '"+taskParams[2]+"', haslevel="+hasLevel+", parsed level="+level);
								if(level != BackupLevel.Default)
									taskId = RemotingManager.GetRemoteObject().StartTask((int)bsId, level);
								else
									Error ("Unable to start Taskset , invalid parameters '"+taskParams[2]+"'", null);
							}
							else
								taskId = RemotingManager.GetRemoteObject().StartTask((int)bsId, null);

							
						}
						catch(Exception e){
							Error ("Unable to start Taskset "+bsId+" : "+e.ToString(), e);
						}
						if(taskId >0)
							Console.Write ("Successfully started as task #"+taskId+(hasLevel?" with custom level.":""));
						else if(taskId == -1)
							Error("Error : Taskset "+bsId+" is already running", null);
					}
					break;
				case "stop":
					if(bsId >0){
						try{
							RemotingManager.GetRemoteObject().StopTask(bsId);
							Console.WriteLine ("Asked to cancel task #"+bsId);
						}
						catch(Exception e){
							Error ("Unable to cancel task #"+bsId+", check that task id is valid ("+e.Message+")", e);
						}
					}
					break;
				
					
			}
		}

		// syntax: restore item1, item2 from node X [to node Y] [replace existing|into /dest/path]
		private static void ProcessRestoreRequest(string parameters){
			int endOfItems = GetWordPos(parameters, "from node", true);
			string[] itemsToRestore = parameters.Substring(0, endOfItems).Split(new char[]{','});
			Console.WriteLine (itemsToRestore.Length+" items to restore");
			parameters = parameters.Substring(endOfItems + " from node ".Length);
			uint fromNodeId = uint.Parse(parameters.Substring(0, parameters.IndexOf(" ")));
			Console.WriteLine ("From Node : "+fromNodeId);
			BackupSet restoreSet = new BackupSet();
			restoreSet.Operation = TaskOperation.Restore;
			List<BackupSet> nodeBSes = RemotingManager.GetRemoteObject().GetNodeBackupSets(fromNodeId);
			Console.WriteLine (" Backuped root items for this node :");
			/*var basePathsByLength = from bs in nodeBSes select bs.BasePaths orderby bs.Path descending;
			foreach(BackupSet bs in nodeBSes)
					Console.WriteLine (string.Join("\t"+Environment.NewLine, bs.BasePaths));
			*/

		}

		private static int GetWordPos(string query, string word, bool caseInsensitive){

			return query.LastIndexOf(" "+word+" ", caseInsensitive?StringComparison.InvariantCultureIgnoreCase:StringComparison.InvariantCulture);
		}

		private static void WriteObjects<T>(IEnumerable<T> list, Type t, string whereClause, string orderByClause){
			Console.WriteLine("orderByClause='"+orderByClause+"'");
			if(list == null) return;
			
			///var listQ = list.Cast().ToList();
			//Type eltType = listQ[0].GetType();
			if(orderByClause != null){
				
				list = list.OrderBy(orderByClause);
			}	
			string whereProp = null;
			string expression = null;
			string op = null;
			string refValue = null;
			if(whereClause != null && whereClause != string.Empty){
				int wherepos = whereClause.ToLower().IndexOf(" where ");
				int operatorpos = whereClause.IndexOfAny(new char[]{'=','<','>','!'});
				//int operatorpos = whereClause.Substring (wherepos+7).Split (new string[]{"<",">","=","!","LIKE"}, StringSplitOptions.None);
				int opEndPos = 1;
				try{ // protect from [tabs] on 'WHERE notfoundProp'
					opEndPos = whereClause.Substring(operatorpos).IndexOfAny(new char[]{'=','<','>','!'/*, ' '*/});
				}catch{return;}
				if(opEndPos <=0) opEndPos = 1; // x>y type
				else opEndPos++;
				op = whereClause.Substring(operatorpos, opEndPos);
				//whereProp = whereClause.Substring(wherepos+7, whereClause.Length - (operatorpos+wherepos+7));
				string withoutWhere = whereClause.Substring(wherepos+7);
				whereProp = withoutWhere.Substring(0, /*withoutWhere.Length -*/ withoutWhere.IndexOfAny(new char[]{'=','<','>','!'/*, ' '*/}) );
				
				expression = whereClause.Substring(wherepos+7);
				refValue = whereClause.Substring(operatorpos+opEndPos);
				//Console.WriteLine("expr="+expression+", operator='"+op+"', whereProp="+whereProp);
			}
			Dictionary<string, int> propAndSize = new Dictionary<string, int>();
			// display header if needed
			//if(!displayPropName){
				fs = "\t";
				//IEnumerator headerEnum = list.GetEnumerator();
				//if(!headerEnum.MoveNext()) return;;
				//var firstObj = headerEnum.Current;
				if(properties == null || (properties.Length == 1 && properties[0] == "*")){
						PropertyInfo[] rawProps = (/*firstObj.GetType().*/t.GetProperties(BindingFlags.Public | BindingFlags.Instance));
						properties = new string[rawProps.Length];
						int i=0;
						foreach(PropertyInfo pi in rawProps){
								properties[i] = pi.Name;
								i++;
						}
				}

			//}
				propAndSize = GetPropertiesAndFormatting(properties, t);
			if(!displayPropName){

			}
				foreach(dynamic n in list){
					try{
						if(n == null) continue;
					}
					catch{} // struct types
					//if( ((n is Dictionary<string, string>)) || n == null) continue;
					if(whereClause != null){
						try{
							string wherevalue = (string)n.GetType().GetProperty(whereProp, BindingFlags.IgnoreCase|BindingFlags.Public|BindingFlags.Instance).GetValue(n, null).ToString();
							if(! Compare(op, wherevalue, refValue)) continue;
						}
						catch(Exception e){
							Error ("Property '"+whereProp+"' used in WHERE clause doesn't exist", e);	
						}
					}
				
					
					
					WriteObject(n, propAndSize);
					Console.Write(rs);
				}	
			properties = null;
		}

		private static Dictionary<string, int> GetPropertiesAndFormatting(string[] props, Type t){
			if(props == null || (props.Length == 1 && props[0] == "*")){
					PropertyInfo[] allProps = (t.GetProperties(BindingFlags.Public | BindingFlags.Instance));
					props = new string[allProps.Length];
					int j=0;
					foreach(PropertyInfo pi in allProps){
							props[j] = pi.Name;
							j++;
					}
			}
			Dictionary<string, int> propsAndSize = new Dictionary<string, int>();

			PropertyInfo[] rawProps = (t.GetProperties(BindingFlags.Public | BindingFlags.Instance));
			int i = 0;
			if(writeHeader)
				Console.WriteLine();
			foreach(string wantedProp in props){
				foreach(PropertyInfo pi in rawProps){
					if(pi.Name.ToLower() != wantedProp.ToLower()) continue;
					Object[] displayAttrs = pi.GetCustomAttributes(false);
					int size=50;
					bool display = true;
					string displayName = pi.Name;
					foreach(Object o in displayAttrs){
						if(o is DisplayFormatOption){
							size =  ((DisplayFormatOption)o).Size ;
							display = ((DisplayFormatOption)o).Display;
							displayName = ((DisplayFormatOption)o).DisplayAs;
							if(displayName == null) displayName = pi.Name;
						}
					}
					if(size < displayName.Length) size = displayName.Length+1;
						//properties[i] = pi.Name;
						//Console.Write (pi.Name.PadRight(size));
						//i++;
					if(displayPropName || display){
						propsAndSize.Add(pi.Name, size);
						//Console.WriteLine(pi.Name+","+size);
					}
					if(writeHeader && display)
						Console.Write (displayName.PadRight(size+1));
				}
			}

			if(writeHeader){
				Console.Write(Environment.NewLine);
				for(int j=0; j<Console.WindowWidth-4; j++)
					Console.Write("-");
				Console.Write(Environment.NewLine);
			}
			return propsAndSize;
		}

		private static void WriteObject(Object n, Dictionary<string, int> properties){
			if(properties == null) properties = GetPropertiesAndFormatting(null, n.GetType());
			int propTableDisplaySize = 20;
			/*if(properties == null || (properties.Length == 1 && properties[0] == "*")){
					PropertyInfo[] rawProps = (n.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance));
					properties = new string[rawProps.Length];
					int i = 0;
					foreach(PropertyInfo pi in rawProps){
						
						properties[i] = pi.Name;
						i++;
					}
			}*/
			foreach(string prop in properties.Keys){
					//string fProp = prop.Substring(0,1).ToUpper()+prop.Substring(1).ToLower();
					try{
						// for nested objects, split object and property
						string objectName = null;
						string propName = null;
						if(prop.IndexOf ('.')>0){
							objectName = prop.Split(new char[]{'.'})[0];
							propName = prop.Split(new char[]{'.'})[1];
						}	
						else
							propName = prop;
						PropertyInfo propType = n.GetType().GetProperty(propName, BindingFlags.IgnoreCase|BindingFlags.Public|BindingFlags.Instance);


						if(propType == null) continue;

						if(displayPropName)
							Console.Write ((prop+fvs).PadRight(20));
						string val = "";
						if(propType.PropertyType.IsGenericType){
							if(propType.GetValue (n, null) == null){
								Console.Write (fs);
								continue;
							}
							val += "[";
							IList item = (IList)propType.GetValue(n, null);
		                    //if (item != null){
		                        foreach (object o in item)
		                            val += o.ToString()+",";
		                    //}
							val +="]";
						}
						else if (propType.GetValue(n, null) != null && propType.GetValue(n, null).GetType() == typeof( Password)){
							string passValue = ((Password)propType.GetValue(n, null)).Value;
							if(passValue != null)
								foreach(char c in passValue)
									val += "*";
							else
							val = "<NULL>";
						}	
						else{
							if(propType != null && propType.GetValue(n, null) != null) 
								val = propType.GetValue(n, null).ToString ();
						}
						//check if value has to be formatted before being displayed
						Object[] displayAttrs = propType.GetCustomAttributes(false);
		
						foreach(Object o in displayAttrs){
							if(o is DisplayFormatOption){
								DisplayFormat format = ((DisplayFormatOption)o).FormatAs;
								if(format == DisplayFormat.Size){
									val = FormatSize(val);
								}
								else if(format == DisplayFormat.Time)
									val = DateTime.Parse(val).ToString("MM/dd hh:mm");
							}
						}
						if(limitSize && val.Length > properties[prop])
							val = val.Substring(0, properties[prop]-2)+"..";
						Console.Write ( val.PadRight((int)properties[prop]+1));
						
					}
					catch(Exception e){ // property not found (incorrect syntax)
						//Console.Write(fs.PadRight(20));
						//Console.Write (prop+fvs+"<NOTFOUND>"/*+e.Message+" ---- "+e.StackTrace*/);
					Console.WriteLine(e.ToString());
					}
					if(displayPropName)
						Console.Write(fs);
				}
		}
		
		public static bool Compare<T>(string op, T x, T y) where T:IComparable{
		 	switch(op) {
				case "==" : case "=": {
					if(x is String)
						return (x.ToString().ToLower() == y.ToString().ToLower());
					else
						return x.CompareTo(y)==0;
				}
				case "!=" : case "<>" : return x.CompareTo(y)!=0;
				case ">"  : return x.CompareTo(y)>0;
				case ">=" : return x.CompareTo(y)>=0;
				case "<"  : return x.CompareTo(y)<0;
				case "<=" : return x.CompareTo(y)<=0;
				case "%" : return x.ToString().IndexOf(y.ToString())>=0;
				default : throw new Exception("unknown operator '"+op+"'");
			 }
		}
		
		
		private static void Interact(){
			bool escapeRequired = false;
			Console.WriteLine ("\t Interactive mode. Press [tab] to get autocompletion. Type 'help' to get help.");
			Console.WriteLine ();
			string query = "";
			Console.Write ("# ");
			while(!escapeRequired){
					
				ConsoleKeyInfo cki;
				while((cki = Console.ReadKey(true)).Key != ConsoleKey.Tab){
					if(cki.Key == ConsoleKey.Enter || cki.Key == ConsoleKey.Execute){
						//Console.WriteLine("# "+query);
						Console.Write(Environment.NewLine);
						ParseQueryOp(query, false);
						query = "";
						Console.WriteLine();
						Console.Write ("# ");	
						position = 0;
					}
					else if(cki.Key == ConsoleKey.Backspace){
						if(Console.CursorLeft < 3) continue;
						//if(position == query.Length){
						Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
						Console.Write(" ");
						Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);	
						//}
						//query = query.Substring(0, query.Length -1);
						if(position >0){
							query = query.Substring (0, position-1)+query.Substring (position);
						}
					}
					else if(cki.Key == ConsoleKey.UpArrow){
						Console.SetCursorPosition(1, Console.CursorTop);
						Console.Write(lastQuery);
					}
					else if(cki.Key == ConsoleKey.LeftArrow){
						Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
						if(position >0)
							position --;
					}
					else if(cki.Key == ConsoleKey.RightArrow){
						Console.SetCursorPosition(Console.CursorLeft + 1, Console.CursorTop);
						if(position < query.Length)
							position++;
					}
					else if(cki.Key != ConsoleKey.Tab){
						if(position == 0 || position == query.Length)
							query += cki.KeyChar;
						else{
							query = query.Substring(0, position-1)+cki.KeyChar+query.Substring (position);
						}
						Console.Write(cki.KeyChar);
						position++;
					}
					
				}
				string compl = ParseQueryOp(query, true);
				query += compl;
				Console.Write(compl);
			}
			
		}
		private static void InteractIrony(){
			bool escapeRequired = false;
			Console.WriteLine ("\t Interactive mode. Press [tab] to get autocompletion. Type 'help' to get help.");
			Console.WriteLine ();
			string query = "";
			Console.Write ("# ");
			while(!escapeRequired){

				ConsoleKeyInfo cki;
				while((cki = Console.ReadKey(true)).Key != ConsoleKey.Tab){
					if(cki.Key == ConsoleKey.Enter || cki.Key == ConsoleKey.Execute){
						//Console.WriteLine("# "+query);
						Console.Write(Environment.NewLine);
						Console.WriteLine(IronyParse (query));
						query = "";
						Console.WriteLine();
						Console.Write ("# ");	
						position = 0;
					}
					else if(cki.Key == ConsoleKey.Backspace){
						if(Console.CursorLeft < 3) continue;
						//if(position == query.Length){
						Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
						Console.Write(" ");
						Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);	
						//}
						//query = query.Substring(0, query.Length -1);
						if(position >0){
							query = query.Substring (0, position-1)+query.Substring (position);
						}
					}
					else if(cki.Key == ConsoleKey.UpArrow){
						Console.SetCursorPosition(1, Console.CursorTop);
						Console.Write(lastQuery);
					}
					else if(cki.Key == ConsoleKey.LeftArrow){
						Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
						if(position >0)
							position --;
					}
					else if(cki.Key == ConsoleKey.RightArrow){
						Console.SetCursorPosition(Console.CursorLeft + 1, Console.CursorTop);
						if(position < query.Length)
							position++;
					}
					else if(cki.Key != ConsoleKey.Tab){
						if(position == 0 || position == query.Length)
							query += cki.KeyChar;
						else{
							query = query.Substring(0, position-1)+cki.KeyChar+query.Substring (position);
						}
						Console.Write(cki.KeyChar);
						position++;
					}

				}
				string compl = ParseQueryOp(query, true);
				query += compl;
				Console.Write(compl);
			}

		}
		
		private static string GetCompletion(string partialQ){
			
				return "asks";
		}
		
		private static void Error(string msg, Exception e){

			Console.ForegroundColor = ConsoleColor.DarkRed;
			Console.WriteLine (msg);
			Console.ForegroundColor = defaultColor;	
		}
		
		private static void PrintHelp(){
			string help = "Usage : "+AppDomain.CurrentDomain.FriendlyName+" -u <username> -p <password> [-i <server ip>] "
				+"[-fs <field separator>] [-rs <record/row separator>] [-fvs <field-value separator>] -q <query>";
			Console.WriteLine (help);
			//Environment.Exit(0);
		}
		
		private static void PrintInteractiveHelp(){
			string help = "Usage : "+Environment.NewLine
				+"get|select "+string.Join("|", operationTargets)+" [where property =|>|<|!=|>=|<=  <value>"; 
			Console.WriteLine (help);
		}
		
		private static void ShutdownHub(){
			Console.WriteLine ("You asked to shutdown hub. This will suspend any task that were running. Are you sure you want to shutdown Hub?(yes/no)");
			string confirm = Console.ReadLine();
			if(confirm.ToLower() == "yes")
				RemotingManager.GetRemoteObject().ShutdownHub();
			else
				Console.WriteLine ("Operation cancelled, will NOT shutdown.");
			
			
		}
		
		
		static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e){
    			Console.WriteLine("Error : "+((Exception)e.ExceptionObject).ToString());
				Environment.Exit(10);
		}

		// returns a bytes size under an human-readable format (MB/GB/TB)
		private static string FormatSize(string byteSize){
			double size = 0;
			double.TryParse(byteSize, out size);
			if(size > (double)1024*1024*1024*1024)
				return Math.Round(size /((double)1024*1024*1024*1024), 1)+" TB";
			else if(size > (double)1024*1024*1024)
				return Math.Round(size /((double)1024*1024*1024), 1)+" GB";
			else if(size > (double)1024*1024)
				return Math.Round(size /(1024*1024), 1)+" MB";
			else if(size > (double)1024)
				return Math.Round(size /(1024), 1)+" KB";
			else return size.ToString();
		}
	}
}
