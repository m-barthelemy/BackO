using System;
using System.Collections.Generic;

namespace P2PBackup.Common.Volumes {

	/// <summary>
	/// Sub file system mean to represent sub-entities that some filesystems support.
	/// Examples would be Btrfs subvolumes (independantly snapshottable and moutable (though already mounted under their parent vol)
	/// or Zfs filesystems under a pool (though having separate mountpoints)
	/// </summary>
	public class SubFileSystem:IDiskElement {

		public string Path{get;set;}
		public string Id{get;set;}
		public List<IDiskElement> Parents{get;set;}
		//public List<IDiskElement> Children{get;private set;}
		public System.Collections.ObjectModel.ReadOnlyCollection<IDiskElement> Children{get{return children.AsReadOnly();}}
		public ulong Offset{get;set;}
		public long Size{get;set;}
		public bool IsComplete{get;set;}

		public string Format {get;set;}
		public string SnapshotType{get;set;}
		public string MountPoint{get;set;}

		private List<IDiskElement> children;

		public SubFileSystem(){
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

