using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Globalization;
using System.ServiceModel;
//using P2PBackupHub;

namespace  P2PBackup.Common{
	
	
	//[ServiceBehavior(IncludeExceptionDetailInFaults=true)]
	[ServiceContract(SessionMode = SessionMode.Required)]
	[ServiceKnownType(typeof(NodeGroup))]
	[ServiceKnownType(typeof(StorageGroup))]
	[ServiceKnownType(typeof(P2PBackup.Common.Node))]
	[ServiceKnownType(typeof(BackupSet))]
	[ServiceKnownType(typeof(P2PBackup.Common.Task))]
	[ServiceKnownType(typeof(TaskRunningStatus))]
	[ServiceKnownType(typeof(TaskStatus))]
	[ServiceKnownType(typeof(TaskStartupType))]
	[ServiceKnownType(typeof(TaskPriority))]
	[ServiceKnownType(typeof(TaskAction))]
	[ServiceKnownType(typeof(TaskOperation))]
	[ServiceKnownType(typeof(Severity))]
	[ServiceKnownType(typeof(NodeCertificate))]
	[ServiceKnownType(typeof(PeerSession))]
	[ServiceKnownType(typeof(DataProcessingFlags))]
	[ServiceKnownType(typeof(RoleEnum))]
	[ServiceKnownType(typeof(Hypervisor))]
	[ServiceKnownType(typeof(Password))]
	[ServiceKnownType(typeof(BrowseNode))]
	public interface  IRemoteOperations{
		
		//[OperationContract]
		//ArrayList GetClients();
		
		/*[OperationContract]
		List<P2PBackup.Common.Node> GetNodes();*/

		[OperationContract]
		List<P2PBackup.Common.Node> GetNodes(int? groupId);

		[OperationContract]
		List<P2PBackup.Common.Node> GetNodesHavingPlugin(int? groupId, string pluginName);

		[OperationContract]
		Node GetNode(uint id);

		[OperationContract]
		P2PBackup.Common.Node UpdateNode(P2PBackup.Common.Node node);

		/*[OperationContract]
		Node UpdateNodeParent(Node node);*/

		[OperationContract]
		List<P2PBackup.Common.Node> GetStorageNodes(int? storageGroupId);
		
		[OperationContract]
		List<P2PBackup.Common.StorageGroup> GetStorageGroups();
		
		[OperationContract]
		List<NodeCertificate> GetCertificates();

		[OperationContract]
		List<PeerSession> GetSessions();

		[OperationContract]
		List<Plugin> GetPlugins(PluginCategory category);

		[OperationContract]
		List<NodeGroup> GetNodeGroups();
		
		[OperationContract]
		void ApproveNode(uint nodeId, bool lockStatus);
		
		[OperationContract]
		int Ping();
		
		[OperationContract]
		User GetCurrentUser();
		
		[OperationContract(IsInitiating = true)]
		bool Login(string userName, string userPassword); 

		[OperationContract]
		[Obsolete]
		Dictionary<uint, NodeStatus> GetOnlineClients();

		[OperationContract]
		List<Node> GetOnlineNodes();

		[OperationContract]
		List<Plugin> GetAllAvailablePlugins();

		[OperationContract]
		List<BackupSetSchedule> GetBackupPlan(int interval);

		[OperationContract]
		BackupSet GetTaskSet(int bsid);

		[OperationContract]
		Dictionary<string, string> GetConfigurationParameters();
		
		[OperationContract]
		List<P2PBackup.Common.Task> GetRunningTasks();
		
		[OperationContract]
		List<P2PBackup.Common.Task> GetTasksHistory(string[] bs, DateTime from, DateTime to, List<TaskRunningStatus> status, string sizeOperator, long size, int limit, int offset, out int totalCount);

		[OperationContract]
		Task GetTaskHistory(long taskId);

		[OperationContract]
		List<TaskLogEntry> GetArchivedTaskLogEntries(int taskId);
			
		[OperationContract]
		List<BackupSet> GetNodeBackupSets(uint nodeId);

		[OperationContract]
		BackupSet GetBackupSet(int id);

		[OperationContract]
		List<BackupSet> GetBackupSets(int start, int limit, bool templatesOnly);

		[OperationContract]
		List<Task> GetBackupHistory(int bsId, DateTime startDate, DateTime endDate);
		
		[OperationContract]
		BrowseNode Browse(uint nodeId, string path);

		[OperationContract]
		BrowseNode BrowseIndex(uint nodeId, long taskId, string rootFS, long parentId, string filter);

		[OperationContract]
		string GetSpecialObjects(uint nodeId);
			
		[OperationContract]
		string GetDrives(uint nodeId);
		
		[OperationContract]
		string GetVMs(uint nodeId);

		[OperationContract]
		List<Hypervisor> GetHypervisors();

		[OperationContract]
		Hypervisor CreateHypervisor(Hypervisor hv);

		[OperationContract]
		Hypervisor UpdateHypervisor(Hypervisor hv);

		[OperationContract]
		void DeleteHypervisor(Hypervisor hv);

		[OperationContract]
		List<Node> Discover (int hypervisorId);

		[OperationContract]
		BackupSet CreateBackupSet(BackupSet bs);

		[OperationContract]
		BackupSet UpdateBackupSet(BackupSet bs);

		[OperationContract]
		StorageGroup CreateStorageGroup(StorageGroup sg);

		[OperationContract]
		StorageGroup UpdateStorageGroup(StorageGroup sg);

		[OperationContract]
		void DeleteStorageGroup(StorageGroup sg);

		[OperationContract]
		NodeGroup CreateNodeGroup(NodeGroup ng);

		[OperationContract]
		NodeGroup UpdateNodeGroup(NodeGroup ng);

		[OperationContract]
		void DeleteNodeGroup(NodeGroup ng);

		[OperationContract]
		LogEntry[] GetLogBuffer();

		//used to manage users session time zones and formats, languages
		[OperationContract]
		CultureInfo[] GetCultures();
		
		[OperationContract]
		List<User> GetUsers();

		[OperationContract]
		User CreateUser(User u);

		[OperationContract]
		User UpdateUser(User u);

		[OperationContract]
		void DeleteUser(User u);

		[OperationContract]
		Password CreatePassword(Password p);

		[OperationContract]
		Password UpdatePassword(Password p);

		[OperationContract]
		long StartTask(int bsid, BackupLevel? level);
		
		[OperationContract]
		void StopTask(long taskId);
		
		[OperationContract]
		string ChangeTasks(List<long> tasks, TaskAction action);
		
		[OperationContract]
		string WhoAmI();
		
		[OperationContract]
		void ShutdownHub();
	}
//}
	
}
