using System;
using System.Collections.Generic;
using P2PBackup.Common;
using P2PBackup.Common.Volumes;
using Node.Snapshots;

namespace Node.DataProcessing{
	
	/// <summary>
	/// Before being executed, a Backup is split into several rootdrives (one per FS/mountpoint).
	/// Doing such separation allows :
	/// -backup parallelism : we can process multiple drives in parallel to maximize throughput (if by luck mountpoints 
	/// 	really are on different storage media)
	/// -per-drive personnalized handling of snapshots and incremental providers.
	/// </summary>
	[Serializable]
	public class BackupRootDrive{
		public ISnapshot Snapshot{get;set;}

		public FileSystem SystemDrive{get;set;}
		public List<BasePath> Paths{get;set;}
		public Int16 ID{get;set;}
		// if this rootdrive is being treated (scanned for items to be backuped)
		public bool Treating{get;set;}
		internal IIncrementalProvider IncrementalPlugin{get; set;}

		public BackupRootDrive(){
			Paths = new List<BasePath>();
			this.IncrementalPlugin = null;
		}
		
		public bool RequiresSnapshot{
			get{
				foreach(BasePath bp in Paths){
					if(bp.SnapshotType != "NONE")
						return true;
				}
				return false;
			}
		}
		
	}
}

