using System;
using System.Collections.Generic;

namespace P2PBackup.Common.Volumes {
	public enum LogicalVolumeType{Unknown=0,LVM=1,NT=3,Btrfs=4,Zfs=5,MD=6}

	public class LogicalVolume:IDiskElement {

		public string Path{get;set;}
		public string Id{get;set;}
		public List<IDiskElement> Parents{get;set;}
		//public List<IDiskElement> Children{get;private set;}
		public System.Collections.ObjectModel.ReadOnlyCollection<IDiskElement> Children{get{return children.AsReadOnly();}}
		public ulong Offset{get;set;}
		public long Size{get;set;}
		public bool IsComplete{get;set;}

		public LogicalVolumeType Type{get;set;}
		public SnapshotType SnapshotType{get;set;}

		private List<IDiskElement> children;

		public LogicalVolume(){
			this.Parents = new List<IDiskElement>();
			this.children = new List<IDiskElement>();
		}

		public void AddChild(IDiskElement elt){
			this.children.Add(elt);
			elt.Parents.Add(this);
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
	}
}


