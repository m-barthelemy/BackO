using System;
using System.IO;
using System.Collections.Generic;

namespace P2PBackup.Common.Volumes {

	public enum PartitionSchemes{Classical=0,GPT=1,Primary=3,Extended=4}

	public enum PartitionTypes:uint{ 
		Empty=0x00,
		FAT12_CHS=0x01,
		FAT16_16_32MB_CHS=0x04,
		Microsoft_Extended=0x05,
		FAT16_32MB_CHS=0x06,
		NTFS=0x07,
		FAT32_CHS=0x0b,
		FAT32_LBA=0x0c,
		FAT16_32MB_2GB_LBA=0x0e,
		Microsoft_Extended_LBA=0x0f,
		Hidden_FAT12_CHS=0x11,
		Hidden_FAT16_16_32MB_CHS=0x14,
		Hidden_FAT16_32MB_2GB_CHS=0x16,
		AST_SmartSleepPartition=0x18,
		Hidden_FAT32_CHS=0x1b,
		Hidden_FAT32_LBA=0x1c,
		Hidden_FAT1632MB_2GBLBA=0x1e,
		/*PQservice*/Hidden_NTFS_WinRE=0x27,
		Plan9partition=0x39,
		PartitionMagic_Recoverypartition=0x3c,
		Microsoft_MBR_DynamicDisk=0x42,
		GoBackpartition=0x44,
		Novell=0x51,
		CP_M=0x52,
		UnixSystemV=0x63,
		PC_ARMOURprotectedpartition=0x64,
		Solarisx86orLinuxSwap=0x82,
		Linux=0x83,
		Hibernation=0x84,
		LinuxExtended=0x85,
		NTFS_VolumeSet=0x86,
		NTFS_VolumeSet_2=0x87,
		BSD_OS=0x9f,
		Hibernation_1=0xa0,
		Hibernation_2=0xa1,
		FreeBSD=0xa5,
		OpenBSD=0xa6,
		MacOSX=0xa8,
		NetBSD=0xa9,
		MacOSX_Boot=0xab,
		MacOSX_HFS=0xaf,
		BSDI=0xb7,
		BSDISwap=0xb8,
		BootWizardhidden=0xbb,
		Solaris8_bootpartition=0xbe,
		CP_M_86=0xd8,
		DellPowerEdgeServerutilities_FATfs=0xde,
		DGUX_virtualdiskmanagerpartition=0xdf,
		BeOSBFS=0xeb,
		EFI_GPT_Disk=0xee,
		EFI_System_Partition=0xef,
		VMWare_FileSystem=0xfb,
		VMWare_Swap=0xfc,
	}

	public class Partition:IDiskElement, IBlockDevice {

		public string Path{get;set;}
		public string Id{get;set;}
		public List<IDiskElement> Parents{get;set;}
		//public List<IDiskElement> Children{get;private set;}
		public System.Collections.ObjectModel.ReadOnlyCollection<IDiskElement> Children{get{return children.AsReadOnly();}}
		public ulong Offset{get;set;}
		public long Size{get;set;}
		public bool IsComplete{get;set;}

		public PartitionSchemes Schema{get;set;}
		public PartitionTypes Type{get;set;}
		public bool Bootable{get;set;}
		public string OriginalPath{get;set;}

		private List<IDiskElement> children;

		public Partition ()	{
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

			if(this.Offset != other.Offset)
				return false;
	       
			/*if(this.Id != other.Id)
				return false;*/
	        return true;
    	}

		public Stream BlockStream{get;set;}


		public override string ToString () {
			return string.Format ("[Partition: Path={0}, Id={1}, Parents={2}, Children={3}, Offset={4}, Size={5}, IsComplete={6}, Schema={7}, Type={8}, Bootable={9}]", Path, Id, "", Children.Count, Offset, Size, IsComplete, Schema, Type, Bootable);
		}

	}
}

