using System;
using System.Collections;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Collections.Generic;
using ServiceStack.DataAnnotations;
using ServiceStack.DesignPatterns.Model;


namespace P2PBackup.Common{
	

	public enum BackupStatus{Ok, Warning, Error, Null}

	[Flags]
	public enum DataProcessingFlags{None=0,CCompress=1,CEncrypt=2,CDedup=4,CReplicate=8,CChecksum=16,SCompress=512,SEncrypt=1024,SDedup=2048,SReplicate=4096,HybridDedup=16384}

	public enum ParallelismLevel{Absolute,Disk,FS};

	public class Parallelism{

		public ParallelismLevel Kind{get;set;}
		public uint Value{get;set;}

		public Parallelism(){}

		public Parallelism(string rawValue){
			string[] split = rawValue.ToLower().Split(new char[]{':'});
			int pValue = 0;
			if(split[0] == "a")
				this.Kind = ParallelismLevel.Absolute;
			else if(split[0] == "d")
				this.Kind = ParallelismLevel.Disk;
			else if (split[0] == "f")
				this.Kind = ParallelismLevel.FS;
			else
				throw new InvalidCastException("Cannot map raw value to ParallelismType");
			if(int.TryParse(split[1], out pValue))
			   this.Value = (uint)pValue;
			else
			   throw new InvalidCastException("Cannot cast raw value'"+split[1]+"' to parallism value(integer)");
		}

		public override string ToString () {
			return string.Format ("[{0}, {1}]", Kind, Value);
		}
	}

	[Serializable]
	[DataContract]
	[Alias("backupset")]
	[CompositeIndex(true, new string[]{"Id","Generation"})]
	public class BackupSet: Object,IEquatable<BackupSet>/*, IHasId<long>*/{ // implement iequatable to allow comparison in scheduler
		
		//private List<BasePath> basePaths;
		//private List<ScheduleTime> backupTimes;
		//private List<int> storageGroups;

		[DisplayFormatOption(Size=4)]
		[DataMember]
		public int Id {get;set;}

		[DataMember]
		[DisplayFormatOption(Display=false)]
		public int Generation{get; set;}

		[DataMember]
		[DisplayFormatOption(Display=false)]
		public bool IsTemplate{get; set;}

		[DataMember]
		[DisplayFormatOption(Size=5)]
		public bool Enabled{get; set;}

		// if >0, inherits default values from parent BackupSet (which is thus considered as a template)
		[DataMember]
		[DisplayFormatOption(Size=5)]
		public int Inherits{get;set;}

		[DisplayFormatOption(Size=20)]
		[DataMember]
		public string Name{get;set;}
			
		[DisplayFormatOption(Size=3, DisplayAs="Node")]
		[DataMember]
		[Index(false)]
		public int NodeId {get;set;}

		/// <summary>
		/// Indicated if this backupset is processed by an alternate, 'proxy' node
		/// will be mainly used if client Node is a VM(backuped by a VADP host)
		/// </summary>
		/// <value>
		/// The handled by.
		/// </value>
		[DisplayFormatOption(Size=3, DisplayAs="H.By")]
		[DataMember]
		public int HandledBy {get;set;}

		/*[DisplayFormatOption(Size=8, DisplayAs="Stor.Grp")]
		[DataMember]
		public List<int> StorageGroups{
			get{return storageGroups;}	
			set{storageGroups = value;}
		}*/

		[DisplayFormatOption(Size=8, DisplayAs="Stor.Grp")]
		[DataMember]
		public int StorageGroup{get;set;}

		[Ignore]
		[DisplayFormatOption(Size=30)]
		[DataMember]
		public List<ScheduleTime> ScheduleTimes {get;set;}

		public int ScheduleId{get;set;}	

		[DisplayFormatOption(Size=30)]
		[DataMember]
		public List<BasePath> BasePaths {get;set;}

		[DisplayFormatOption(Size=8,DisplayAs="//")]
		[DataMember]
		public Parallelism Parallelism{get;set;}

		[DisplayFormatOption(Size=25)]
		[DataMember]
		public DataProcessingFlags DataFlags{get;set;}

		[DisplayFormatOption(Display=false)]
		[DataMember]
		public string Preop {get;set;}

		[DisplayFormatOption(Display=false)]
		[DataMember]
		public string Postop {get;set;}

		[DisplayFormatOption(Size=3,DisplayAs="Copies")]
		[DataMember]
		public int Redundancy{get;set;}

		[DisplayFormatOption(Size=4,DisplayAs="Ret.")]
		[DataMember]
		public int RetentionDays {get;set;}

		[DisplayFormatOption(Size=4,DisplayAs="Snap Ret.")]
		[DataMember]
		public int SnapshotRetention{get;set;}

		[DisplayFormatOption(Size=8)]
		[DataMember]
		public TaskOperation Operation{get;set;}

		[DisplayFormatOption(Display=false)]
		[DataMember]
		public string StorageLayoutProvider{get;set;}

		/// <summary>
		/// For a proxied backup or a backup that needs help from hypervisor,
		/// define a tuple with clientInternalId (for hyperisor to find it) and hypervisor information (url, username, password)
		/// </summary>
		/// <value>
		/// The proxy info.
		/// </value>
		[DisplayFormatOption(Size=7,Display=false)]
		[DataMember]
		public ProxyTaskInfo ProxyingInfo{get;set;}

		[DisplayFormatOption(Display=false)]
		[DataMember]
		public List<TaskNotification> Notifications{get;set;}


		/* advanced options */
		[DisplayFormatOption(Size=7,Display=false,FormatAs=DisplayFormat.Size,DisplayAs="PSize")]
		[DataMember]
		public long MaxPackSize {get;set;}
		
		[DisplayFormatOption(Size=7,Display=false,FormatAs=DisplayFormat.Size,DisplayAs="CSize")]
		[DataMember]
		public long MaxChunkSize {get;set;}
		
		[DisplayFormatOption(Size=7,Display=false,DisplayAs="CFiles")]
		[DataMember]
		public int MaxChunkFiles {get;set;}
		
		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="P2PBackup.Common.BackupSet"/> use a dedicated deduplication
		/// index. Set to true if defining multiple backupsets on a single node with a lot of data to backup and limited available RAM :
		/// only the dedup info regarding the current bs will be loaded in memory.
		/// </summary>
		[DisplayFormatOption(Display=false)]
		[DataMember]
		public bool UseDedicatedDdb{get;set;}
		
		/// <summary>
		/// Gets or sets a value indicating whether we should trust 
		/// mtime date for incrementals. Mtime usually gets updated when a file's content changes, but can be 
		/// changed from userland. If set to 'true', incrementals/refresh backups using FileComparer will 
		/// use ctime (metadata change). Ctime is more reliable (cannot be altered) but can lead to backup files 
		/// with unchanged content (only metadata)
		/// </summary>
		/// <value>
		/// <c>true</c> if dont trust mtime; otherwise, <c>false</c>.
		/// </value>
		[DisplayFormatOption(Display=false)]
		[DataMember]
		public bool DontTrustMtime{get;set;}


		public BackupSet(){
			this.DataFlags = DataProcessingFlags.None;
			this.BasePaths = new List<BasePath>();
			this.ScheduleTimes = new List<ScheduleTime>();
			this.Notifications = new List<TaskNotification>();
			this.Redundancy = 1;
			this.RetentionDays = 7; // default to 1 week retention
			this.Parallelism = new P2PBackup.Common.Parallelism{Kind = ParallelismLevel.Disk, Value=2};
			this.MaxChunkFiles = 2000;
			this.MaxChunkSize = 100*1024*1024; // 100MB
			this.MaxPackSize = 100*1024*1024; // 100MB
			this.StorageLayoutProvider = "local";
			//this.ProxyingInfo = null;
		}

		public bool Equals(BackupSet other){
	        if (this.Id == other.Id)  
	            return true;
	        else
	            return false;
    	}
		
		public override string ToString () {
			return string.Format ("[BackupSet: Id={0}, Generation={1}, IsTemplate={2}, Enabled={3}, Inherits={4}, Name={5}, NodeId={6}, HandledBy={7}, StorageGroup={8}, ScheduleTimes={9}, ScheduleId={10}, BasePaths={11}, Parallelism={12}, DataFlags={13}, Preop={14}, Postop={15}, Redundancy={16}, RetentionDays={17}, SnapshotRetention={18}, Operation={19}, MaxPackSize={20}, MaxChunkSize={21}, MaxChunkFiles={22}, ProxyingInfo={23}, Notifications={24}]", Id, Generation, IsTemplate, Enabled, Inherits, Name, NodeId, HandledBy, StorageGroup, ScheduleTimes, ScheduleId, BasePaths, Parallelism, DataFlags, Preop, Postop, Redundancy, RetentionDays, SnapshotRetention, Operation, MaxPackSize, MaxChunkSize, MaxChunkFiles, ProxyingInfo, Notifications);
		}
	

	}
}

