using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.DataAnnotations;
using ServiceStack.DesignPatterns.Model;

namespace P2PBackup.Common{
	
	[DataContract]public enum TaskOperation{
		[EnumMember]Backup, 
		[EnumMember]Restore, 
		[EnumMember]Check, 
		[EnumMember]HouseKeeping};
	/// <summary>
	///  User-requested actions on queued tasks
	/// </summary>
	[DataContract]public enum TaskAction{
		[EnumMember]Start, 
		[EnumMember]Pause, 
		[EnumMember]Restart, 
		[EnumMember]Cancel,
		[EnumMember]Expire}; 
	/// <summary>
	/// Task type : scheduled by hub or manually (user) requested
	/// </summary>
	[DataContract]
	public enum TaskStartupType{
		[EnumMember]Scheduled,
		[EnumMember]Manual};
	
	/// <summary>
	/// Task running status once it is in scheduler queue
	/// </summary>
	[DataContract(Name = "RunStatus")]public enum TaskRunningStatus{
		[EnumMember]Unknown, 
		[EnumMember]PendingStart,
		[EnumMember]Started,
		[EnumMember]PreProcessing,
		[EnumMember]WaitingForStorage,
		[EnumMember]WindowExceeded,
		[EnumMember]Paused, 
		[EnumMember]PostProcessing, 
		[EnumMember]Stopped, 
		[EnumMember]Error, 
		[EnumMember]Cancelling, 
		[EnumMember]Cancelled, 
		[EnumMember]Done, 
		[EnumMember]Expiring, 
		[EnumMember]Expired}
	
	[DataContract]public enum TaskStatus{
		[EnumMember]Ok,
		[EnumMember]Warning,
		[EnumMember]Error,
		[EnumMember]Null}
	
	/// <summary>
	/// Task priority.
	/// High is the default and will maximize system resources usage to complete as quickly as possible.
	/// Low will make small pauses (eg. after each file write() operation in case of a backup tasdk) to be more fair 
	/// with the system	and other running applications
	/// </summary>
	[DataContract]public enum TaskPriority{
		[EnumMember]High, 
		[EnumMember]Low}
	
	/*[Serializable]*/
	[DataContract]
	[KnownType(typeof(TaskRunningStatus))]
	[KnownType(typeof(TaskStatus))]
	[KnownType(typeof(TaskStartupType))]
	[KnownType(typeof(TaskAction))]
	[KnownType(typeof(TaskOperation))]
	[KnownType(typeof(TaskPriority))]
	[KnownType(typeof(Node))]
	[KnownType(typeof(BackupSet))]
	public class Task : /*BackupSet,*/ IEquatable<Task>{
		

		private BackupSet backupSet;

		[DataMember(Order = 0)] 
		[DisplayFormatOption(Size=6)]
		public long Id {get;set;}

		[DataMember]
		[DisplayFormatOption(Size=8)]
		public TaskOperation Operation{get;set;}
		
		[DataMember]
		[DisplayFormatOption(Size=9)]
		public TaskStartupType Type{get;set;}

		[DataMember]
		[DisplayFormatOption(Size=6)]
		public BackupLevel Level{get;set;} 

		[DataMember]
		[DisplayFormatOption(Size=3, DisplayAs="Node")]
		[Index(false)]
		public uint NodeId{get;set;}

		[DataMember]
		[DisplayFormatOption(Size=4)]
		[Ignore] // don't persist to DB
		public BackupSet BackupSet {
			get{return backupSet;}
			set{
				backupSet = value;
				if(backupSet != null)
					this.BackupSetId = backupSet.Id;
			}
		}

		[DataMember]
		[DisplayFormatOption(Size=7)]
		public int BackupSetId{get;set;}

		[DataMember]
		[DisplayFormatOption(Size=10)]
		public TaskRunningStatus RunStatus {get;set;}
			
		[DataMember]
		[DisplayFormatOption(Size=3, DisplayAs="%")]
		public int Percent{get; set;}
		
		[DataMember]
		[DisplayFormatOption(Size=8)]
		public TaskStatus Status {get;set;}
			
		[DataMember]
		[DisplayFormatOption(Size=7,DisplayAs="Items")]
		public int TotalItems{get;set;}

		[DataMember]
		[DisplayFormatOption(Size=8,DisplayAs="Size",FormatAs=DisplayFormat.Size)]
		public long OriginalSize {get;set;}

		[DataMember]
		[DisplayFormatOption(Size=8,FormatAs=DisplayFormat.Size,DisplayAs="F.Size")]
		public long FinalSize {get;set;}

		[DataMember]	
		[DisplayFormatOption(Display=false)]
		[Ignore] // don't save to database
		public int StorageBudget{get;set;}
		

		[DisplayFormatOption(Display=false)]
		[Ignore] // don't save to database
		public  List<P2PBackup.Common.Node> StorageNodes{get; protected set;}
			
		//private List<P2PBackup.Common.Node> storageNodes;
		
		[DataMember]
		[DisplayFormatOption(Display=false)]
		public List<uint> IndexStorageNodes {get;set;}


		/*[DataMember]
		[DisplayFormatOption(Display=false)]
		public string DdbName {get;set;}
		
		[DataMember]
		[DisplayFormatOption(Display=false)]
		public long DdbSize {get;set;}*/

		// Deduplication db information, if relevant
		[DataMember]
		[DisplayFormatOption(Display=false)]
		public string DdbSum {get;set;}

		// Backup index information (mandatory on hub side, since a backup without registered index is invalid)
		[DataMember]
		[DisplayFormatOption(Display=false)]
		public string IndexName {get;set;}
			
		[DataMember]
		[DisplayFormatOption(Display=false)]
		public long IndexSize {get;set;}
			
		[DataMember]
		[DisplayFormatOption(Display=false)]
		public string IndexSum {get;set;}

		[DataMember]
		[DisplayFormatOption(Display=false)]
		public string SyntheticIndexSum {get;set;}

	
		[Ignore] // don't persist to DB
		[DisplayFormatOption(Display=false)]
		public string EncryptionKey {get;set;}

		[DataMember]
		[DisplayFormatOption(Display=false)]
		public int EncryptionKeyId {get;set;}


		[DataMember]
		[DisplayFormatOption(Display=false)]
		public long ParentTrackingId {get;set;}
			
		[Ignore] // don't persist to DB
		[DisplayFormatOption(Display=false)]
		public Task ParentTask {get;set;}

		[DataMember]
		[DisplayFormatOption(Size=12,FormatAs=DisplayFormat.Time)]
		[Index(false)]
		public DateTime StartDate {get;set;}

		[DataMember]
		[DisplayFormatOption(Size=12,FormatAs=DisplayFormat.Time)]
		[Index(false)]
		public DateTime EndDate {get;set;}
			
		[Ignore] // don't persist to Task table, use dedicated table instead
		[DisplayFormatOption(Display=false)]
		public List<TaskLogEntry> LogEntries{get;set;}

		// For non-scheduled operations, returns the user who started them.
		[DataMember]
		[DisplayFormatOption(Display=false)]
		public int UserId {get;set;}
			
		[DataMember]
		[DisplayFormatOption(Display=false)]
		public TaskPriority Priority{get;set;}

		[DataMember]
		[DisplayFormatOption(Size=50)]
		public String CurrentAction{get;set;}
			
		//date of the last stats update received for the task
		[DataMember]
		[DisplayFormatOption(Display=false)]
		public long LastUpdated{get;set;}

		/// <summary>
		/// Creates a task to be run
		/// </summary>
		/// <param name="bs">
		/// A <see cref="BackupSet"/>
		/// </param>
		/// <param name="operation">
		/// A <see cref="TaskOperation"/>
		/// </param>
		/// <param name="type">
		/// A <see cref="TaskType"/>
		/// </param>
		public Task(BackupSet bs, /*TaskOperation operation,*/ TaskStartupType type): this(){
			this.BackupSet = bs;
			this.BackupSetId = bs.Id;
			this.Operation = bs.Operation;
			this.Type = type;
			this.LogEntries = new List<TaskLogEntry>(); //new List<Tuple<DateTime, int, string, string>>();
			this.Percent = 0;
			this.IndexSize = 0;
			this.IndexName = String.Empty;
			this.IndexSum = String.Empty;
			this.Priority = TaskPriority.High;
			this.Status = TaskStatus.Ok;
			this.OriginalSize = 0;
			this.FinalSize = 0;
			this.TotalItems = 0;
			StorageBudget = 0;
		}
		
		/// <summary>
		/// Constructor for serialization only
		/// </summary>
		//[Obsolete("For Serialization Only", true)]
		public Task(){
			this.Id = -1;
			this.StorageNodes = new List<P2PBackup.Common.Node>();
			this.Status = TaskStatus.Ok;
			this.IndexStorageNodes = new List<uint>();
			this.LogEntries = new List<TaskLogEntry>(); //new List<Tuple<DateTime, int, string, string>>();
		}
		
		public void AddLogEntry(TaskLogEntry tle) {
			lock(this.LogEntries){
				this.LogEntries.Add(tle);
			}
		}

		public void AddStorageNode(P2PBackup.Common.Node node){
			if(!this.StorageNodes.Contains(node)){
				this.StorageNodes.Add(node);
			}
		}
		
		public void RemoveStorageNode(P2PBackup.Common.Node node){
			lock(StorageNodes)
				for(int i = StorageNodes.Count; i==0; i--)
					if(StorageNodes[i].Id == node.Id)
						StorageNodes.RemoveAt(i);
		}

		public bool Equals(Task other){
	        if (this.Id == other.Id)   
	            return true;
	        else
	            return false;
    	}

	}
}

