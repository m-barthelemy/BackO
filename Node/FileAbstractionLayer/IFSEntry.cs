using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Node.DataProcessing;

namespace Node{

	public enum FileType{
		File=0,
		Directory=1,
		Symlink=2,
		Hardlink=3,
		Socket=4,
		CharDevice=5,
		BlockDevice=6,
		Fifo=7,
		Stream=8,
		Hidden=9,
		MountPoint=10, // includes NTFS ReparsePoints
		Unsupported=15
	}

	// During incr/diff backups, for synthetic indexing, allow fast querying of items that need a special treatment:
	// Metadata only : keep ref index's item chunk, but use cur index item's metadata
	// DeletedItem : Delete from cur index
	// Partial : merge ref index metadata with cur index metadata (especially DataRegion)
	[Flags]
	public enum DataLayoutInfos{HasChanges=0, NoChange=1, RenameOnly=2, MetadaOnly=4, Deleted=8, SparseFile=16, PartialRangesFile=32, New = 64, Invalid=1024}

	public interface IFSEntry: ISerializable{
		/// <summary>
		/// complete file name at the moment of backuping (may be a snapshot path)
		/// </summary>
		string SnapFullPath{get; set;}
		string OriginalFullPath{get;set;}
		long ID{get;set;} // unique file id (inode nb on *nix)
		long ParentID{get;set;}
		// position inside chunk, if chunk packs multiple files
		uint ChunkStartPos {get; set;}
		// position inside file, if too big to fit in 1 chunk
		long FileStartPos{get; set;}
		long FileSize{get; set;}
		
		string Name{get;set;}

		/// <summary>
		/// If entry is a link, complete path to its target.
		/// </summary>
		string TargetName{get;set;} 

		long LastModifiedTime{get; set;}
		
		long LastMetadataModifiedTime{get; set;}
			
		long CreateTime{get; set;}
		
		FileType Kind{get; set;}
			
		int Attributes{get; set;}
			
		int SpecialAttributes{get; set;}

		List<Tuple<string, byte[]>> ExtendedAttributes{get;set;}

		uint OwnerUser{get; set;}

		uint OwnerGroup{get; set;}

		uint Permissions{get; set;}
		
		/*bool IsSparse{get;set;}

		bool IsPartial{get;set;}*/

		DataLayoutInfos ChangeStatus{get;set;}

		FileBlockMetadata BlockMetadata{get;set;}
		
		// open the file and return an optimized (for this OS) streamreader.
		Stream OpenStream(FileMode fileMode);
		
		IFSEntry Clone();
		

	}
} 

