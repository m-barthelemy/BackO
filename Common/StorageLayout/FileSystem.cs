using System;
using System.Collections.Generic;

namespace P2PBackup.Common.Volumes {
	//public enum LogicalVolumeType{Unknown=0,LVM=1,NT=3,Btrfs=4,Zfs=5,MD=6}

	public class FileSystem:IDiskElement {

		public string Path{get;set;}
		public string Id{get;set;}
		public List<IDiskElement> Parents{get;set;}
		//public List<IDiskElement> Children{get;private set;}
		public System.Collections.ObjectModel.ReadOnlyCollection<IDiskElement> Children{get{return children.AsReadOnly();}}
		public ulong Offset{get;set;}
		public long Size{get;set;}
		public bool IsComplete{get;set;}

		public string DriveFormat {get;set;}
		public SnapshotType SnapshotType{get;set;}
		public string MountPoint{get;set;}
		// if task is handled by a "proxy" node, set original mountpoint
		public string OriginalMountPoint{get;set;}
		public string Label{get;set;}
		public long AvailableFreeSpace{get;set;}

		private List<IDiskElement> children;

		public FileSystem (){
			this.Parents = new List<IDiskElement>();
			this.children = new List<IDiskElement>();
		}

		public void AddChild(IDiskElement elt){
			this.children.Add(elt);
			elt.Parents.Add(this);
		}

		public List<Disk> GetParentPhysicalDisks(){
			List<Disk> physDisks = new List<Disk>();
			foreach(IDiskElement ide in this.Parents){
				List<IDiskElement> curElts = new List<IDiskElement>();
				curElts.Add(ide);
				bool more = true;
				while(more){
					foreach(IDiskElement curElt in curElts){
						if(curElt is Disk){
							physDisks.Add((Disk)curElt);
							more = false;
						}
						else{
							curElts = curElt.Parents; 
						}
					}

				}
			}
			return physDisks;

		}

		public List<Partition> GetParentPhysicalPartitions(){
			List<Partition> physPartitions = new List<Partition>();
			foreach(IDiskElement ide in this.Parents){
				List<IDiskElement> curElts = new List<IDiskElement>();
				curElts.Add(ide);
				bool more = true;
				while(more){
					foreach(IDiskElement curElt in curElts){
						if(curElt is Partition){
							physPartitions.Add((Partition)curElt);
							more = false;
						}
						else{
							curElts = curElt.Parents; 
						}
					}

				}
			}
			return physPartitions;

		}

		public bool Equals(IDiskElement other){
			if(this.GetType() != other.GetType())
				return false;
	        if (this.Path != other.Path) 
	            return false;
			if(this.Id != other.Id)
				return false;
	        return true;
    	}

		public override string ToString () {
			return string.Format ("[FileSystem: Path={0}, Id={1}, Parents={2}, Children={3}, Offset={4}, Size={5}, IsComplete={6}, DriveFormat={7}, SnapshotType={8}, MountPoint={9}, OriginalMountPoint={10}, AvailableFreeSpace={11}, Label={12}]", Path, Id, "", Children, Offset, Size, IsComplete, DriveFormat, SnapshotType, MountPoint, OriginalMountPoint, AvailableFreeSpace, Label);
		}
	}
}

