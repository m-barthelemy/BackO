#if OS_UNIX
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
//using System.Security.AccessControl;
//using System.Security.Principal;
using Mono.Unix;
using Mono.Unix.Native;
using System.Runtime.Serialization;
using Node.Utilities;
using Node.DataProcessing;
using P2PBackup.Common;

namespace Node{
	
	//public enum FileType{File,Symlink,Hardlink,Socket,Directory,CharDevice,BlockDevice,Fifo,Stream,Hidden,Unsupported}
	
	/// <summary>
	///  Represent an Unix file.
	/// </summary>
	[Serializable]
	public class UnixFile:IFSEntry, ISerializable{
		[field: NonSerialized] private UnixFileInfo ufi;
		private string fileName;	// the original file name
		private long fileSize;
		private uint chunkStartPos;
		private FileType fileKind;

		private uint ownerUser;
		private uint ownerGroup;
		private int fileAttributes;
		private int fileSpecialAttributes;
		private long lastModifiedTime;
		private long createTime;
		
		private int fd;
		public string Name{get;set;}
		// Target of a symbolic link
		public String TargetName{get;set;}
		public string OriginalFullPath{get;set;}
		
		public string SnapFullPath{
			set{fileName = value;}
			get{return fileName;}
		}
	
		public long ID{get;set;}
		public long ParentID{get;set;}
		
		public uint ChunkStartPos {
			get {return this.chunkStartPos;}
			set { this.chunkStartPos = value;}
		}
		public long FileStartPos{get; set;}
		public long FileSize{
			set{fileSize = value;}
			get{return fileSize;}
		}
		
		public long LastModifiedTime{
			set{lastModifiedTime = value;}
			get{return lastModifiedTime;}
		}
		
		public long LastAccessedTime{get;set;}
		public long LastMetadataModifiedTime{get; set;}
		
		public long CreateTime{
			set{createTime = value;}
			get{return createTime;}
		}
		
		public FileType Kind{
			get{return fileKind;}	
			set{fileKind = value;}
		}
		
		public int Attributes{
			get{return fileAttributes;}
			set{fileAttributes = value;}
		}

		public List<Tuple<string, byte[]>> ExtendedAttributes{get;set;}
		
		public int SpecialAttributes{
			get{return fileSpecialAttributes;}
			set{fileSpecialAttributes = value;}
		}
		
		public uint OwnerUser{
			set{ownerUser = value;}
			get{return ownerUser;}
		}
		
		public uint OwnerGroup{
			set{ownerGroup = value;}
			get{return ownerGroup;}
		}
		
		public uint Permissions{get;set;}

		public bool IsSparse{get;set;}

		public bool IsPartial{get;set;}

		public DataLayoutInfos ChangeStatus{get;set;}

		public FileBlockMetadata BlockMetadata{get;set;}
		

		public UnixFile(string fileName, uint chunkStartPos):this(fileName){
			this.chunkStartPos = chunkStartPos;
		}
		
		public UnixFile(string fileName){
			this.fileName = fileName;
			ufi = new UnixFileInfo(fileName);
			this.Name = ufi.Name;
			this.fileSize = ufi.Length;
			this.Permissions = (uint)ufi.FileAccessPermissions;
			fileSpecialAttributes = (int)ufi.FileSpecialAttributes;
			fileAttributes = 0;
			fileKind = GetUKind();
			ownerUser = (uint)ufi.OwnerUserId;
			ownerGroup = (uint)ufi.OwnerGroupId;
			BlockMetadata = new FileBlockMetadata();
			this.ExtendedAttributes = new List<Tuple<string, byte[]>>();
		}
		
		public IFSEntry Clone(){
				return (IFSEntry)new UnixFile(fileName, 0);
		}
		
		public Stream OpenStream(FileMode fileMode){
			if(Utilities.ConfigManager.GetValue("NO_FILEOPTIMIZATION") != null)
				return new FileStream(fileName, FileMode.Open);
			if(fileMode == FileMode.Open)
				fd = Syscall.open(fileName, OpenFlags.O_RDONLY);
			else if(fileMode == FileMode.CreateNew)
				fd = Syscall.open(fileName, OpenFlags.O_CREAT);
			if((int)fd < 0){
				Logger.Append(Severity.INFO, "Unable to open file with optimized parameters. Falling back to regular open. (Return code was "+fd+")");
				return new FileStream(fileName, FileMode.Open);
			}
			else{ 
				UnixStream us = new Mono.Unix.UnixStream(fd);
				return (Stream)us;
			}
		}
		
		public void CloseStream(){
			if(fd > 0)
				Syscall.close(fd);
		}
		
		private int GetWAttributes(){
			return (int)File.GetAttributes(this.fileName);
		}
		
		private int GetUAttributes(UnixFileInfo fi){
			return (int)fi.FileSpecialAttributes;
		}
		
		private void SetUAttributes(){
			
		}
		
		private FileType GetUKind(){
			if(ufi.FileType == Mono.Unix.FileTypes.RegularFile)	
				return FileType.File;
			else if (ufi.FileType == Mono.Unix.FileTypes.Directory)
				return FileType.Directory;
			else if(ufi.FileType == Mono.Unix.FileTypes.SymbolicLink)
				return FileType.Symlink;
			else if (ufi.FileType == Mono.Unix.FileTypes.Socket)
				return FileType.Socket;
			else if(ufi.FileType == Mono.Unix.FileTypes.Fifo)
				return FileType.Fifo;
			else if(ufi.FileType == Mono.Unix.FileTypes.BlockDevice)
				return FileType.BlockDevice;
			else if(ufi.FileType == Mono.Unix.FileTypes.CharacterDevice)
					return FileType.CharDevice;
			// not supported in mono.unix : hardlinks and doors(solaris)		
			else 
				return FileType.Unsupported;                
		}
		
	
		public void RawRestore(){
		
		}

		
		private void SetUAttributes(UnixFileInfo ufi){
			ufi.FileSpecialAttributes = (Mono.Unix.FileSpecialAttributes)this.Attributes;
		}
		
		
		public void GetObjectData(SerializationInfo info, StreamingContext context){
		  /*  info.AddValue("id", this.ID);
			info.AddValue("pid", this.ParentID);*/
		    info.AddValue("attrs", this.Attributes);
		   // info.AddValue("crtime", this.CreateTime);
		    info.AddValue("ctime", this.LastMetadataModifiedTime);
			info.AddValue("mtime", this.LastModifiedTime);
			/*info.AddValue("k", this.Kind);
			info.AddValue("s", this.FileSize);*/
			info.AddValue("csp", this.ChunkStartPos);
			info.AddValue("fsp", this.FileStartPos);
			info.AddValue("og", this.OwnerGroup);
			info.AddValue("ou", this.OwnerUser);
			info.AddValue("perm", this.Permissions);
			info.AddValue("sattr", this.SpecialAttributes);
			info.AddValue("data", this.BlockMetadata);
			
		}
		
		protected UnixFile(SerializationInfo info,StreamingContext context){
		/*	this.ID = info.GetInt64("id");
			this.ParentID = info.GetInt64("pid");*/
			/*this.Attributes = info.GetInt32("attrs");
			//this.CreateTime = info.GetInt64("crtime");
			this.LastMetadataModifiedTime = info.GetInt64("ctime");
			this.LastModifiedTime = info.GetInt64("mtime");*/
			/*this.Kind = (Node.FileType)info.GetValue("k", typeof(Node.FileType));
			this.FileSize = info.GetInt64("s");*/
			/*this.ChunkStartPos = info.GetUInt32("csp");
			this.FileStartPos = info.GetInt64("fsp");
			this.OwnerGroup = info.GetUInt32("og");
			this.OwnerUser = info.GetUInt32("ou");
			this.Permissions = (Mono.Unix.FileAccessPermissions)info.GetValue("perm", typeof(Node.FileType));
			this.SpecialAttributes = info.GetInt32("sattr");*/
			this.BlockMetadata = (FileBlockMetadata)info.GetValue("data", typeof(FileBlockMetadata));
		}
	}
} 
#endif