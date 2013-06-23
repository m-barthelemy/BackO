using System;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using P2PBackup.Common;
using P2PBackupHub.Utilities;
using P2PBackupHub.Virtualization;
using System.Configuration;
using System.Threading;
using System.Text;
using System.Security.Principal;
using System.Security.Permissions;
using System.Security.Policy;
using System.Linq;

using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace P2PBackupHub{

	/*[Serializable]*/
	/*[DataContract]*/
	[KnownType(typeof(TaskRunningStatus))]
	[KnownType(typeof(TaskStatus))]
	[KnownType(typeof(P2PBackup.Common.Task))]
	[KnownType(typeof(TaskStartupType))]
	[KnownType(typeof(TaskPriority))]
	[KnownType(typeof(TaskAction))]
	[KnownType(typeof(TaskOperation))]
	[KnownType(typeof(BackupSet))]
	[KnownType(typeof(P2PBackup.Common.Node))]
	[KnownType(typeof(LogEntry))]
	[KnownType(typeof(Severity))]
	[KnownType(typeof(NodeCertificate))]
	[KnownType(typeof(Hypervisor))]
	[ServiceBehavior(InstanceContextMode=InstanceContextMode.PerSession)]
	public class RemoteOperations:  IRemoteOperations{
		
		private bool hasSession;
		private User sessionUser;
		GenericPrincipal principal;
		IIdentity identity;
		
		public RemoteOperations (){
			//Logger.Append("HUBRN", Severity.DEBUG2, "Remoting connection initiated.");
			hasSession = false;
		}
		
		
		public bool Login(string userName, string password){
			Console.WriteLine ("RemoteOperations Login 1 : login="+userName+", pass="+password);
			//sessionUser = (new DBHandle()).AuthenticateUser(userName, password);
			sessionUser = new DAL.UserDAO().AuthenticateUser(userName, password);
			hasSession = (sessionUser != null);
			if(!hasSession) return false;
			Console.WriteLine ("RemoteOperations Login 2");
			//Thread.CurrentPrincipal = new GenericPrincipal(
            		//new GenericIdentity(sessionUser.Name), new string[] { "SuperAdmin" });
			
			identity = new GenericIdentity(sessionUser.Name);

 
            //Console.WriteLine( ((RemoteEndpointMessageProperty)OperationContext.Current.IncomingMessageProperties[RemoteEndpointMessageProperty.Name]).Address);


			//Console.WriteLine ("RemoteOperations Login 3");
			//string[] roles = null;
			//if(sessionUser.Name == "admin")
			//	roles = new string[] { "SuperAdmin" };
			var rList = from  role in sessionUser.Roles select role.Role.ToString();
			principal = new GenericPrincipal(identity, rList.ToArray());
				
			//Console.WriteLine ("RemoteOperations Login(), roles="+string.Join(",", rList.ToArray()));
			try{
				AppDomain.CurrentDomain.SetThreadPrincipal(principal);
			} // avoid throwing error when reconnecting from Web ui while Hub still has active session
			catch{}
			if(Thread.CurrentPrincipal.Identity.Name != principal.Identity.Name)
				Thread.CurrentPrincipal = principal;
			//Console.WriteLine ("RemoteOperations Login 5");
			
			
			//CheckSession();
			return hasSession; 
		}
		/*public User[] GetClients(){
			DBHandle db = new DBHandle();
			Logger.Append("HUBRN","DEBUG","RemoteOperations","Called GetClients...");
			User[] clients = (User[])db.GetClients().ToArray(typeof(User));
			Logger.Append("HUBRN","DEBUG","RemoteOperations","Called GetClients, got "+clients.Length);                                                       
			return clients;
		}*/
		
		/*private void CheckSession(){
			Console.WriteLine("current user name: "+sessionUser.Name);
			Console.WriteLine("current user principal: "+Thread.CurrentPrincipal.Identity.Name);
			//Console.WriteLine("current user principal has role SuperAdmin: "+principal.IsInRole("SuperAdmin"));
			//Console.WriteLine("current user principalhas role SuperViewer: "+principal.IsInRole("SuperViewer"));
			//Console.WriteLine("");
			Console.WriteLine("current user principal has role SuperAdmin: "+Thread.CurrentPrincipal.IsInRole("SuperAdmin"));
			Console.WriteLine("current user principal has role SuperViewer: "+Thread.CurrentPrincipal.IsInRole("SuperViewer"));
			Console.WriteLine("----------------");
			if(!hasSession) throw new Exception ("Login denied, or session expired");
		}*/

		public string WhoAmI(){
			return "thread: "+Thread.CurrentPrincipal.Identity.Name+", session:"+sessionUser.Name;
		}
		/*[PrincipalPermission(SecurityAction.Demand, Role="Viewer")]
		[PrincipalPermission(SecurityAction.Demand, Role="SuperViewer")]
		[PrincipalPermission(SecurityAction.Demand, Role="Admin")]
		[PrincipalPermission(SecurityAction.Demand, Role="SuperAdmin")]*/
		/*public ArrayList GetClients(){
			ArrayList iClientNodes = new ArrayList();
			CheckSession();
			DBHandle db = new DBHandle();                                                     
			foreach(Node n in db.GetClients())
			        iClientNodes.Add((IClientNode)n);
			return iClientNodes;
		}*/
		
		/*[PrincipalPermission(SecurityAction.Demand, Role="SuperViewer")]
		[PrincipalPermission(SecurityAction.Demand, Role="Admin")]
		[PrincipalPermission(SecurityAction.Demand, Role="SuperAdmin")]*/
		public List<P2PBackup.Common.Node> GetNodes(int? groupId){

			List<P2PBackup.Common.Node> nodes = new DAL.NodeDAO(sessionUser).GetAll(groupId);
			foreach(P2PBackup.Common.Node n in nodes){
				if(Hub.NodesList.Contains(n.Id))
					n.Status = Hub.NodesList.GetById(n.Id).Status;
			}
			return nodes;
		}

		public List<P2PBackup.Common.Node> GetNodesHavingPlugin(int? groupId, string pluginName){
			List<P2PBackup.Common.Node> nodes = new DAL.NodeDAO(sessionUser).GetAllHavingPlugin(groupId, pluginName);
			foreach(P2PBackup.Common.Node n in nodes){
				if(Hub.NodesList.Contains(n.Id))
					n.Status = NodeStatus.Idle;
			}
			return nodes;
		}

		public P2PBackup.Common.Node GetNode (uint id){
			return new DAL.NodeDAO(sessionUser).Get(id);
		}

		public P2PBackup.Common.Node UpdateNode(P2PBackup.Common.Node node){
			return new DAL.NodeDAO(sessionUser).Update(node);
		}

		[PrincipalPermission(SecurityAction.Demand, Role="SuperViewer")]
		[PrincipalPermission(SecurityAction.Demand, Role="SuperAdmin")]
		public List<NodeCertificate> GetCertificates(){
			//List<NodeCertificate> certs = new List<NodeCertificate>();

			return new DAL.CertificateDAO().GetAll();
		}
		/*public User[] GetStorageNodes(){
			DBHandle db = new DBHandle();
			Logger.Append("HUBRN","DEBUG","RemoteOperations","Called GetStorageNodes...");
			User[] clients = (User[])db.GetStorageNodes().ToArray(typeof(User));
			Logger.Append("HUBRN","DEBUG","RemoteOperations","Called GetStorageNodes, got "+clients.Length);                                                       
			return clients;
		}*/

		[PrincipalPermission(SecurityAction.Demand, Role="SuperViewer")]
		[PrincipalPermission(SecurityAction.Demand, Role="SuperAdmin")]
		public List<Hypervisor> GetHypervisors(){
			return new DAL.HypervisorDAO(sessionUser).GetAll();
		}

		[PrincipalPermission(SecurityAction.Demand, Role="SuperAdmin")]
		public Hypervisor CreateHypervisor(Hypervisor hv){
			return new DAL.HypervisorDAO().Save(hv);
		}

		[PrincipalPermission(SecurityAction.Demand, Role="SuperAdmin")]
		public Hypervisor UpdateHypervisor(Hypervisor hv){
			return new DAL.HypervisorDAO().Update(hv);
		}

		[PrincipalPermission(SecurityAction.Demand, Role="SuperAdmin")]
		public void DeleteHypervisor(Hypervisor hv){
			new DAL.HypervisorDAO().Delete(hv);
		}

		public List<P2PBackup.Common.Node> Discover (int hypervisorId){
			return Hub.DiscoverVms(hypervisorId);
		}

		[PrincipalPermission(SecurityAction.Demand, Role="SuperViewer")]
		[PrincipalPermission(SecurityAction.Demand, Role="SuperAdmin")]
		public List<PeerSession> GetSessions(){
			return Hub.SessionsList.ToList();
		}

		public List<Plugin> GetPlugins(PluginCategory category){
			return new DAL.PluginDAO(sessionUser).GetDistinctAvailable(category);
		}

		[PrincipalPermission(SecurityAction.Demand, Role="SuperViewer")]
		[PrincipalPermission(SecurityAction.Demand, Role="SuperAdmin")]
		public List<P2PBackup.Common.Node> GetStorageNodes(int? storageGroupId){
			//DBHandle db = new DBHandle(sessionUser);                                       
			return new DAL.NodeDAO(sessionUser).GetStorageNodes(storageGroupId);
		}
		
		/*public StorageGroup[] GetStorageGroups(){
			DBHandle db = new DBHandle();
			Logger.Append("HUBRN","DEBUG","RemoteOperations","Called GetStorageGroups...");
			StorageGroup[] sg = (StorageGroup[])db.GetStorageGroups().ToArray(typeof(StorageGroup));
			Logger.Append("HUBRN","DEBUG","RemoteOperations","Called GetStorageGroups, got "+sg.Length);                                                       
			return sg;
		}*/

		[PrincipalPermission(SecurityAction.Demand, Role="SuperViewer")]
		[PrincipalPermission(SecurityAction.Demand, Role="SuperAdmin")]
		public List<P2PBackup.Common.StorageGroup> GetStorageGroups(){
			//DBHandle db = new DBHandle(sessionUser);
			//return db.GetStorageGroups();
			return new DAL.StorageGroupDAO(sessionUser).GetAll();
		}
		

		public List<NodeGroup> GetNodeGroups(){
			return 	new DAL.NodeGroupDAO(sessionUser).GetAll();
		}
		
	
		[PrincipalPermission(SecurityAction.Demand, Role="SuperAdmin")]
		public void ApproveNode(uint nodeId, bool lockStatus){
			//(new DBHandle()).ApproveNode(nodeId, lockStatus);
			new DAL.NodeDAO().Approve(nodeId, lockStatus);
			if(lockStatus == false){
				PeerNode n = Hub.NodesList.GetById(nodeId);
				if(n != null) {
					n.Status = NodeStatus.Idle;
					// TODO : call putonline()
					n.SendAuthStatus();
				}
			}
			else{ // lock immediately : an online node will be disconnected
				PeerNode n = Hub.NodesList.GetById(nodeId);
				if(n != null && n.Status != NodeStatus.Offline)
					n.Disconnect();
			}
				
		}
		
		public int Ping(){
			Logger.Append("HUBRN", Severity.DEBUG, "Called Ping");
			return 1;	
		}
		
		/*public User BeginSession(string userName, string password){
			User u = (new DBHandle()).AuthenticateUser(userName, password) ;
			hasSession = (u==null);
			return 	u;
		}*/

		public User GetCurrentUser(){
			return sessionUser;
		}

		/// <summary>
		/// Gets the online clients IDs.
		/// </summary>
		/// <returns>
		/// The online clients.
		/// </returns>
		[PrincipalPermission(SecurityAction.Demand, Role="SuperViewer")]
		[PrincipalPermission(SecurityAction.Demand, Role="SuperAdmin")]
		public Dictionary<uint, NodeStatus> GetOnlineClients(){
			var cnList = new Dictionary<uint, NodeStatus>();
			foreach(PeerNode cn in Hub.NodesList)
				cnList.Add(cn.Id, cn.Status );
			return cnList;
			//return (List<Node>)Hub.NodesList;
			
		}

		[PrincipalPermission(SecurityAction.Demand, Role="SuperViewer")]
		[PrincipalPermission(SecurityAction.Demand, Role="SuperAdmin")]
		public List<P2PBackup.Common.Node> GetOnlineNodes(){
			return Hub.NodesList.Cast<P2PBackup.Common.Node>().ToList();
		}


		public List<Plugin> GetAllAvailablePlugins(){
			return new DAL.NodeDAO().GetAllInstalledStoragePlugins();
		}

		[PrincipalPermission(SecurityAction.Demand, Role="SuperViewer")]
		[PrincipalPermission(SecurityAction.Demand, Role="SuperAdmin")]
		public List<BackupSetSchedule> GetBackupPlan(int nbHours){
			Console.WriteLine("remoteoperations.GetBackupPlan() : called");
			//return 	(new DBHandle()).GetTaskSets(DateTime.Now.Subtract(new TimeSpan(1,0,0) ), DateTime.Now.Add(new TimeSpan(nbHours-1,0,0) ), true );
			return 	new DAL.BackupSetDAO(sessionUser).GetPlan(DateTime.Now, DateTime.Now.Add(new TimeSpan(nbHours,0,0) ) );
		}


		public BackupSet GetTaskSet(int bsid){
			//return (new DBHandle(sessionUser)).GetTaskSet(bsid);
			return (new DAL.BackupSetDAO(sessionUser)).GetById(bsid);
		}

		[PrincipalPermission(SecurityAction.Demand, Role="SuperAdmin")]
		public Dictionary<string, string> GetConfigurationParameters(){
			Dictionary<string, string> config = new Dictionary<string, string>();
			if(sessionUser.IsSuperAdmin())
				foreach(string key in ConfigurationManager.AppSettings.AllKeys)
					config.Add(key, ConfigurationManager.AppSettings[key]);
			return config;
		}


		public List<BackupSet> GetNodeBackupSets(uint nodeId){
			//return (new DBHandle(sessionUser)).GetNodeBackupSets(nodeId);	
			return new DAL.BackupSetDAO(sessionUser).GetNodeBackupSets(nodeId);	
		}

		public BackupSet GetBackupSet(int id){
			return new DAL.BackupSetDAO(sessionUser).GetById(id);
		}

		public List<BackupSet> GetBackupSets(int start, int limit, bool templatesOnly){
			return new DAL.BackupSetDAO(sessionUser).GetAll(start, limit, null, null,templatesOnly);
		}
		
		public List<Task> GetBackupHistory(int bsId, DateTime startDate, DateTime endDate){
			return 	(new DAL.TaskDAO(sessionUser)).GetTasksHistory(bsId, startDate, endDate);
		}

		public List<P2PBackup.Common.Task> GetTasksHistory(string[] bs, DateTime from, DateTime to, List<TaskRunningStatus> status, string sizeOperator, long size, int limit, int offset, out int totalCount){
			//return 	(new DBHandle(sessionUser)).GetTaskHistory(bs, from, to, status, sizeOperator, size, limit, offset, out totalCount);
			return 	(new DAL.TaskDAO(sessionUser)).GetTaskHistory(bs, from, to, status, sizeOperator, size, limit, offset, out totalCount);
		}

		public Task GetTaskHistory(long taskId){
			return (new DAL.TaskDAO(sessionUser)).GetTaskHistory(taskId);
		}

		[PrincipalPermission(SecurityAction.Demand, Role="SuperViewer")]
		[PrincipalPermission(SecurityAction.Demand, Role="SuperAdmin")]
		public List<P2PBackup.Common.Task> GetRunningTasks(){
			/*List<P2PBackup.Common.Task> displayableRunningTasks = new List<Task>();
			List<P2PBackup.Common.Task> allRunningTasks = TaskScheduler.Instance().GetTasks();
			List<Node> nodes = GetNodes();
			foreach(Task t in allRunningTasks){
				foreach(Node n in nodes)
					if(t.BackupSet.ClientId == n.Uid)
						displayableRunningTasks.Add(t);
			}
			return displayableRunningTasks;*/
			return TaskScheduler.Instance().Tasks;
				
		}
		
		public List<TaskLogEntry> GetArchivedTaskLogEntries(int taskId){
			return (new DAL.TaskDAO(sessionUser)).GetTaskLogEntries(taskId);
		}

		[PrincipalPermission(SecurityAction.Demand, Role="SuperViewer")]
		[PrincipalPermission(SecurityAction.Demand, Role="SuperAdmin")]
		public BrowseNode Browse(uint nodeId, string path){
			PeerNode n = Hub.NodesList.GetById(nodeId);
			return n.Browse(path);

		}

		// 'onlygroups' : returns only devices and folders
		// 'onlyleafs' : returns only files (and links) items
		// null or empty : returns everything
		[PrincipalPermission(SecurityAction.Demand, Role="SuperViewer")]
		[PrincipalPermission(SecurityAction.Demand, Role="SuperAdmin")]
		public BrowseNode BrowseIndex(uint nodeId, long taskId, string rootFS, long parentId, string filter){
			PeerNode n = Hub.NodesList.GetById(nodeId);
			return n.BrowseIndex(taskId, rootFS, parentId, filter);
			
		}
		
		// should be relevant only for NT systems (VSS providers)
		public string GetSpecialObjects(uint nodeId){
			PeerNode n = Hub.NodesList.GetById(nodeId);
			return n.GetSpecialObjects();
		}
		
		public string GetDrives(uint nodeId){
			PeerNode n = Hub.NodesList.GetById(nodeId);
			return n.GetDrives();

		}
		
		public string GetVMs(uint nodeId){
			/*foreach(PeerNode n in Hub.NodesList){
				if(n.Id != nodeId)
					continue;
				n.GetVMs();
				int i=0;
				while(n.VMS.Length ==0 && i<1000){
					System.Threading.Thread.Sleep(10);
					i++;
				}
				return n.VMS;
			}
			return null;*/
			throw new NotImplementedException("GetVMs is not implemented anymore, VMs discover has been delegated to Hub");
			
		}

		//[PrincipalPermission(SecurityAction.Demand, Role="Admin")]
		public BackupSet CreateBackupSet(BackupSet bs){
			//DBHandle db = new DBHandle();
			//db.AddBackupSet(bs);
			return new DAL.BackupSetDAO(sessionUser).Save(bs);
		}

		public BackupSet UpdateBackupSet(BackupSet bs){
			return new DAL.BackupSetDAO(sessionUser).Update(bs);
		}

		[PrincipalPermission(SecurityAction.Demand, Role="SuperAdmin")]
		public StorageGroup CreateStorageGroup(StorageGroup sg){
			return new DAL.StorageGroupDAO(sessionUser).Save(sg);
		}

		[PrincipalPermission(SecurityAction.Demand, Role="SuperAdmin")]
		public StorageGroup UpdateStorageGroup(StorageGroup sg){
			return new DAL.StorageGroupDAO(sessionUser).Update(sg);
		}

		[PrincipalPermission(SecurityAction.Demand, Role="SuperAdmin")]
		public void DeleteStorageGroup(StorageGroup sg){
			new DAL.StorageGroupDAO(sessionUser).Delete(sg);
		}

		[PrincipalPermission(SecurityAction.Demand, Role="SuperAdmin")]
		public NodeGroup CreateNodeGroup(NodeGroup ng){
			return new DAL.NodeGroupDAO(sessionUser).Save(ng);
		}

		[PrincipalPermission(SecurityAction.Demand, Role="SuperAdmin")]
		public NodeGroup UpdateNodeGroup(NodeGroup ng){
			return new DAL.NodeGroupDAO(sessionUser).Update(ng);
		}

		[PrincipalPermission(SecurityAction.Demand, Role="SuperAdmin")]
		public void DeleteNodeGroup(NodeGroup ng){
			new DAL.NodeGroupDAO(sessionUser).Delete(ng);
		}



		[PrincipalPermission(SecurityAction.Demand, Role="Admin")]
		public string ChangeTasks(List<long> tasks, TaskAction action){
			foreach(long taskId in tasks){
				if(action == TaskAction.Cancel) TaskScheduler.Instance().CancelTask(taskId, sessionUser);
				else if(action == TaskAction.Pause) TaskScheduler.Instance().PauseTask(taskId, sessionUser);
			}
			return "not implemented";	
		}

		[PrincipalPermission(SecurityAction.Demand, Role="SuperAdmin")]
		[PrincipalPermission(SecurityAction.Demand, Role="SuperViewer")]
		public LogEntry[] GetLogBuffer(){
			return Logger.GetBuffer();	
		}

		[PrincipalPermission(SecurityAction.Demand, Role="SuperAdmin")]
		public NameValueCollection GetHubConf(){
			return ConfigurationManager.AppSettings;
		}
		
		//used to manage users session time zones and formats, languages
		public CultureInfo[] GetCultures(){
			//Dictionary<string, string> cultures = new Dictionary<string, string>();
			//List<Tuple<string, string>> cultures = new List<Tuple<string, string>>();
			/*CultureInfo[] allCultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures);
			foreach (CultureInfo culture in allCultures){
				cultures.Add(new Tuple<string, string>(culture.Name, culture.NativeName));
			}
			return cultures;*/
			return CultureInfo.GetCultures(CultureTypes.SpecificCultures);
		}

		[PrincipalPermission(SecurityAction.Demand, Role="SuperAdmin")]
		public List<User> GetUsers(){
			//return (new DBHandle(sessionUser)).GetUsers();	
			return new DAL.UserDAO().GetAll();
		}

		[PrincipalPermission(SecurityAction.Demand, Role="SuperAdmin")]
		public User CreateUser(User u){
			return new DAL.UserDAO().Save(u);
		}

		/// <summary>
		/// Updates a complete user. 
		/// Reserved for SuperAdmin Usage since it can modify all fields 
		///    (including privileges/permissions)
		/// </summary>
		/// <returns>
		/// The user.
		/// </returns>
		/// <param name='u'>
		/// the user to update.
		/// </param>
		[PrincipalPermission(SecurityAction.Demand, Role="SuperAdmin")]
		public User UpdateUser(User u){
			return new DAL.UserDAO().Update(u);
		}

		[PrincipalPermission(SecurityAction.Demand, Role="SuperAdmin")]
		public void DeleteUser(User u){
			new DAL.UserDAO().Delete(u);
		}


		public Password CreatePassword(Password p){
			//if(sessionUser.IsSuperAdmin() || p.Id == sessionUser.PasswordId){
			return PasswordManager.Add(p);
			//}
		}

		public Password UpdatePassword(Password p){
			PasswordManager.Update(p);
			return p;
		}

		/// <summary>
		/// Gets stats about last 24hrs tasks
		/// </summary>
		/// <returns>
		/// The last stats.
		/// </returns>
		public List<Tuple<string, int>> GetLastTasksStats(){
			throw new NotImplementedException();
			//return (new DBHandle()).GetLastTasksStats();
		}

		public long StartTask(int bsid, BackupLevel? level){
			return TaskScheduler.Instance().StartImmediateTask(bsid, sessionUser, level);
		}

		public void StopTask(long taskId){
			TaskScheduler.Instance().CancelTask(taskId, sessionUser);
		}

		[PrincipalPermission(SecurityAction.Demand, Role="SuperAdmin")]
		public void ShutdownHub(){
			Hub.Shutdown(sessionUser, false);
		}
	}
	
	
//}
	
}
