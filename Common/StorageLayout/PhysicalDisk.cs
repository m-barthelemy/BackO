using System;
using System.IO;
using System.Collections.Generic;

namespace P2PBackup.Common.Volumes {

	// classical : physical disk, 
	// hwraid : volume appearing as physical to the os, but in fact a HW raid array. unused
	// Loop : disk appearing as loop file (vmware/libvrirt snapshot...)
	public enum DiskType{Classical=0,HwRaid=1,Loop=3}
	public enum LayoutType{MBR=0,GPT=1,None=2}

	public class Disk:IDiskElement, IBlockDevice {

		public string Path{get;set;}
		public string Id{get;set;}
		public List<IDiskElement> Parents{get;set;}
		public System.Collections.ObjectModel.ReadOnlyCollection<IDiskElement> Children{get{return children.AsReadOnly();}}
		public ulong Offset{get;set;}
		public long Size{get;set;}
		public DiskType Type{get;set;}
		public bool IsComplete{get;set;}

		/// /// <summary>
		/// If this disk is reachable for backup 
		/// (mainly used for vmware snapshots : enabled=false if disk is independant)
		/// </summary>
		/// <value>
		/// <c>true</c> if enabled; otherwise, <c>false</c>.
		/// </value>
		public string ProxiedPath{get;set;}
		public bool Enabled{get;set;}
		public DriveType DriveType {get;set;}
		public SnapshotType SnapshotType{get;set;}
		public uint Signature{get;set;}
		public uint SectorSize{get;set;}


		public ushort MbrSignature{get;set;}
		public byte[] MbrCode{get;set;}
		public byte[] MbrBytes{get;set;}
		//public bool TreatAsLoop{get;set;}
		public Stream BlockStream{get;set;}

		private List<IDiskElement> children;

		public Disk (){
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

		/*public override string ToString () {
			return string.Format ("[Disk: Path={0}, Id={1}, Parents={2}, Children={3}, Offset={4}, Size={5}, Type={6}, IsComplete={7}, ProxiedPath={8}, Enabled={9}, DriveType={10}, SnapshotType={11}, Signature={12}, MbrSignature={13}, MbrCode={14}, MbrBytes={15}, TreatAsLoop={16}]", Path, Id, Parents, Children.Count, Offset, Size, Type, IsComplete, ProxiedPath, Enabled, DriveType, SnapshotType, Signature, MbrSignature, MbrCode, MbrBytes, TreatAsLoop);
		}*/
		public override string ToString () {
			return string.Format ("[Disk: Path={0}, Id={1}, Parents={2}, Children={3}, Offset={4}, Size={5}, Type={6}, IsComplete={7}, ProxiedPath={8}, Enabled={9}, DriveType={10}, SnapshotType={11}, Signature={12}, SectorSize={13}, MbrSignature={14}, MbrCode={15}, MbrBytes={16}]", Path, Id, "", Children.Count, Offset, Size, Type, IsComplete, ProxiedPath, Enabled, DriveType, SnapshotType, Signature, SectorSize, MbrSignature, MbrCode, MbrBytes);
		}
	}
}

