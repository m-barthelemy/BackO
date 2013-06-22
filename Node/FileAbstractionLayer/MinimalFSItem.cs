using System;
using System.Collections.Generic;
using System.IO;
using Node.Utilities;
using Node.DataProcessing;
using System.Runtime.Serialization;

namespace Node {

	/// <summary>
	/// Minimal/cross-platform Filesystem entry.
	/// Used when all specialized FSItems fail, or when backuping special data (index, dedup db)
	/// </summary>
	[Serializable]
	public class MinimalFsItem : IFSEntry{

		public string SnapFullPath{get; set;}

		public string OriginalFullPath{get;set;}

		public long ID{get;set;} // unique file id (inode nb on *nix)

		public long ParentID{get;set;}

		// position inside chunk, if chunk packs multiple files
		public uint ChunkStartPos {get; set;}

		// position inside file, if too big to fit in 1 chunk
		public long FileStartPos{get; set;}

		public long FileSize{get; set;}
		
		public string Name{get;set;}

		// Target of a link
		public String TargetName{get;set;}
			
		public long LastModifiedTime{get; set;}
		
		public long LastMetadataModifiedTime{get; set;}
			
		public long CreateTime{get; set;}
		
		public FileType Kind{get; set;}
			
		public int Attributes{get; set;}
			
		public List<Tuple<string, byte[]>> ExtendedAttributes{get;set;}

		public int SpecialAttributes{get; set;}
		
		public uint OwnerUser{get; set;}
		
		public uint OwnerGroup{get; set;}

		public uint Permissions{get;set;}
		
		public bool IsSparse{get;set;}

		public bool IsPartial{get;set;}

		public DataLayoutInfos ChangeStatus{get;set;}

		public FileBlockMetadata BlockMetadata{get;set;}

		[field: NonSerialized]private string fileName;

		// open the file and return a (hopefully OS optimized)  Stream.
		public Stream OpenStream(FileMode fileMode){
			return new FileStream(fileName, FileMode.Open);
		}
		
		public IFSEntry Clone(){
			return (IFSEntry) new MinimalFsItem(this.fileName);
		}

		public MinimalFsItem (string name){
			this.fileName = name;
			FileInfo fi = new FileInfo(fileName);
			this.FileSize = fi.Length;
			this.Name = fi.Name;
			this.ID = 0;
			this.ParentID = 0;
			this.LastMetadataModifiedTime = 0;
			this.LastModifiedTime = fi.LastWriteTimeUtc.ToFileTimeUtc();
			this.CreateTime = fi.CreationTimeUtc.ToFileTimeUtc();
			//this.Kind = GetKind(fi);
			this.Kind = FileType.File;
			BlockMetadata = new FileBlockMetadata();

		}

		/*private FileType GetKind(FileInfo fi){
			if(fi.
		}*/

		public void GetObjectData(SerializationInfo info, StreamingContext context){
		   /* info.AddValue("id", this.ID);
			info.AddValue("pid", this.ParentID);*/
		    info.AddValue("attrs", this.Attributes);
		    info.AddValue("crtime", this.CreateTime);
		    info.AddValue("ctime", this.LastMetadataModifiedTime);
			info.AddValue("mtime", this.LastModifiedTime);
			/*info.AddValue("k", this.Kind);
			info.AddValue("s", this.FileSize);*/
			info.AddValue("csp", this.ChunkStartPos);
			info.AddValue("fsp", this.FileStartPos);
			info.AddValue("og", this.OwnerGroup);
			info.AddValue("ou", this.OwnerUser);
			//info.AddValue("perm", this.Permissions);
			info.AddValue("sattr", this.SpecialAttributes);
			info.AddValue("data", this.BlockMetadata);
			
		}
		
		protected MinimalFsItem(SerializationInfo info,StreamingContext context){
			/*this.ID = info.GetInt64("id");
			this.ParentID = info.GetInt64("pid");*/
			this.Attributes = info.GetInt32("attrs");
			this.CreateTime = info.GetInt64("crtime");
			this.LastMetadataModifiedTime = info.GetInt64("ctime");
			this.LastModifiedTime = info.GetInt64("mtime");
			/*this.Kind = (Node.FileType)info.GetValue("k", typeof(Node.FileType));
			this.FileSize = info.GetInt64("s");*/
			this.ChunkStartPos = info.GetUInt32("csp");
			this.FileStartPos = info.GetInt64("fsp");
			this.OwnerGroup = info.GetUInt32("og");
			this.OwnerUser = info.GetUInt32("ou");
			//this.Permissions = (Mono.Unix.FileAccessPermissions)info.GetValue("perm", typeof(Node.FileType));
			this.SpecialAttributes = info.GetInt32("sattr");
			this.BlockMetadata = (FileBlockMetadata)info.GetValue("data", typeof(FileBlockMetadata));
		}
	}
}

