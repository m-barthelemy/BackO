using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
//using System.Security.AccessControl;
//using System.Security.Principal;
using Mono.Unix;
using Mono.Unix.Native;
//using Node.PostProcess;
using Node.Utilities;
using Node.DataProcessing;
using P2PBackup.Common;

namespace Node{
	
	//public enum FileType{File,Symlink,Hardlink,Socket,Directory,CharDevice,BlockDevice,Fifo,Stream,Hidden,Unsupported}
	
	/// <summary>
	///  Represent an Unix file.
	/// </summary>
	[Obsolete]
	[Serializable]
	public class FreeBSDFile/*:IFile*/{
		[field: NonSerialized] private UnixFileInfo ufi;
		private string fileName;	// the original file name
		private long fileSize;
		private long chunkStartPos;
		private FileType fileKind;
		// Unix permissions
		private FileAccessPermissions  unixPermissions;
		// TODO: windows permissions
		
		private long ownerUser;
		private long ownerGroup;
		private int fileAttributes;
		private int fileSpecialAttributes;
		private DateTime lastModifiedTime;
		private DateTime createTime;
		
		private int fd;
		
		public string FileName{
			set{fileName = value;}
			get{return fileName;}
		}
		/*public string OriginalFileName{get; set;}*/
		//public string PathTemp{
		//	set{pathTemp = value;}
		//	get{return pathTemp;}
		//}
		public long ChunkStartPos {
			get {return this.chunkStartPos;}
			set { this.chunkStartPos = value;}
		}
		public long FileStartPos{get; set;}
		
		public long FileSize{
			set{fileSize = value;}
			get{return fileSize;}
		}
		
		public DateTime LastModifiedTime{
			set{lastModifiedTime = value;}
			get{return lastModifiedTime;}
		}
		
		public DateTime LastMetadataModifiedTime{get; set;}
		
		public DateTime CreateTime{
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
		
		public int SpecialAttributes{
			get{return fileSpecialAttributes;}
			set{fileSpecialAttributes = value;}
		}
		
		public long OwnerUser{
			set{ownerUser = value;}
			get{return ownerUser;}
		}
		
		public long OwnerGroup{
			set{ownerGroup = value;}
			get{return ownerGroup;}
		}
		
		public FileAccessPermissions Permissions{
			set{ unixPermissions = value;}
			get{return unixPermissions;}
		}
		
		public FileBlockMetadata BlockMetadata{get;set;}
		
		/*public BFile(){
		
		}*/

		public FreeBSDFile(string fN, long chunkStartPos){
			fileName = fN;
			ufi = new UnixFileInfo(fN);
			this.chunkStartPos = chunkStartPos;
			this.fileSize = ufi.Length;
			unixPermissions = ufi.FileAccessPermissions;
			fileSpecialAttributes = (int)ufi.FileSpecialAttributes;
			fileAttributes = 0;
			fileKind = GetUKind();
			ownerUser = ufi.OwnerUserId;
			ownerGroup = ufi.OwnerGroupId;
			BlockMetadata = new FileBlockMetadata();
		}
		
		public FreeBSDFile(string fileName){
			this.fileName = fileName;
			//ufi = new UnixFileInfo(fileName);
			BlockMetadata = new FileBlockMetadata();
		}
		
		
		public Stream OpenStream(FileMode fileMode){
			fd = Syscall.open(fileName, OpenFlags.O_RDONLY);
			if((int)fd < 0){
				Logger.Append(Severity.INFO, "Unable to open file with optimized parameters. Falling back to regular open. (Return code was "+fd+")");
				return (new UnixFile(fileName)).OpenStream(fileMode);
			}
			else{ 
				//int ret = shb_fcntl(fd, 15 /*F_READAHEAD*/, 512);
				int ret = shb_fcntl(fd, 16 /*F_READAHEAD*/, 512);
				if(ret != 0)
					Logger.Append(Severity.DEBUG, "Could not set F_READAHEAD to optimize file access for "+ufi.FullName+". Return code was "+ret);
				UnixStream us = new UnixStream(fd, false);
				return (Stream)us;
			}
		}
		
		// no way to set F_READAHEAD or F_RDHEAD with mono.
		[DllImport ("MonoPosixHelper", SetLastError=true, EntryPoint="Mono_Posix_Syscall_fcntl_arg")]
        private static extern int shb_fcntl (int fd, int cmd, long arg);
		
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
		/*	
			// get the directory name
				string directoryName = Path.GetDirectoryName(originalFileName);
				// create the target directory, if necessary
				Directory.CreateDirectory(directoryName);
				//DirectoryInfo di = Directory.CreateDirectory(directoryName);
			
			
			//fsOutput.WriteAllBytes(fsKey.ReadAllBytes());
			File.Copy(tempRestorePath+Path.DirectorySeparatorChar+substituteFileName+".raw", originalFileName, true);
			Logger.Append("DEBUG","BFile.RawRestore", "Restored file "+originalFileName);
			File.SetLastWriteTime(originalFileName, this.LastModifiedTime);
			// Final step : restore permissions, ownership, special attributes..
			if (this.UPermissions.ToString() != string.Empty){
				UnixFileInfo ufi = new UnixFileInfo(originalFileName);
				ufi.SetOwner(this.OwnerUser, this.OwnerGroup);
				SetUPermissions(ufi);
				if(this.Attributes > 0)
					SetUAttributes(ufi);
			}
			Logger.Append("DEBUG","BFile.RawRestore", "Successfully restored permissions and attributes.");
			*/
		}
		
		
		private void SetUPermissions(){
			ufi.FileAccessPermissions = (FileAccessPermissions)this.unixPermissions;
		}
		
		private void SetUAttributes(UnixFileInfo ufi){
			ufi.FileSpecialAttributes = (Mono.Unix.FileSpecialAttributes)this.Attributes;
		}
		
	}
} 
