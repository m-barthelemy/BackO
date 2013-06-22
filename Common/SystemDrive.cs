/*using System;
using System.IO;
//using Node.Snapshots;

namespace P2PBackup.Common{
	public enum SnapshotType{VSS, LVM, ZFS, BTRFS, VADP, NONE}
	/// <summary>
	///  Try to mimic DriveInfo / UnixDriveInfo, since there is no way to cast between them, and also to handle lvm/zfs
	/// </summary>
	//public enum DriveType{Fixed, NoRootDirectory, Network, Unknown
	[Serializable]
	public class SystemDrive:IEquatable<SystemDrive>{
		private string name;
		private string mountPoint;
		private string volumeLabel;
		private long size;
		private long availableFreeSpace;
		private string driveFormat;
		private SnapshotType snapshotType;
		private DriveType driveType;
		
		public DriveType DriveType {
			get {return this.driveType;}
			set {driveType = value;}
		}

		public string DriveFormat {
			get {return this.driveFormat;}
			set {driveFormat = value;}
		}

		public SnapshotType SnapshotType {
			get {return this.snapshotType;}
			set {snapshotType = value;}
		}

		public string MountPoint {
			get {return this.mountPoint;}
			set {mountPoint = value;}
		}

		public long Size {
			get {return this.size;}
			set {size = value;}
		}
		
		public long AvailableFreeSpace {
			get {return this.availableFreeSpace;}
			set {availableFreeSpace = value;}
		}

		public string BlockDevice {
			get {return this.volumeLabel;}
			set {volumeLabel = value;}
		}

		
		public string Name{
			get{return name;}
			set{name = value;}
		}

		public bool Enabled{get;set;}

		public bool Equals(SystemDrive other){
	        if (this.MountPoint == other.MountPoint){   
	            return true;
	        }
	        return false;
    	}
	}
	
}

*/