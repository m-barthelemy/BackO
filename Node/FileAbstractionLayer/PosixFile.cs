#if OS_UNIX
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
//using System.Security.AccessControl;
//using System.Security.Principal;
using Mono.Unix;
using Mono.Unix.Native;
//using Node.PostProcess;
using Node.Utilities;
using Node.DataProcessing;
using Microsoft.Win32.SafeHandles;

namespace Node{
	
	//public enum FileType{File,Symlink,Hardlink,Socket,Directory,CharDevice,BlockDevice,Fifo,Stream,Hidden,Unsupported}
	
	/// <summary>
	///  Represent an Unix file.
	/// </summary>
	[Serializable]
	public class PosixFile:IFSEntry{

		[field: NonSerialized] private UnixFileInfo ufi;

		private uint chunkStartPos;
		private Dirent unixEntry;

		public string Name{get;set;}
		public string OriginalFullPath{get;set;}
		public string SnapFullPath{get;set;}
		public long ID{get;set;} // inode num
		public long ParentID{get;set;}
		public uint ChunkStartPos {get;set;}
		public long FileStartPos{get; set;}
		public long FileSize{get;set;}

		//public DateTime LastAccessedTime{get;set;}
		public long LastModifiedTime{get;set;}
		public long LastMetadataModifiedTime{get; set;}
		public long CreateTime{get;set;}
		public FileType Kind{get;set;}

		public String TargetName{get;set;}
		public int Attributes{get;set;}
		public int SpecialAttributes{get;set;}
		public List<Tuple<string, byte[]>> ExtendedAttributes{get;set;}
		public uint OwnerUser{get;set;}
		public uint OwnerGroup{get;set;}
			
		public uint /*FileAccessPermissions*/ Permissions{get;set;}

		/*public bool IsSparse{get;set;}
		public bool IsPartial{get;set;}*/

		public DataLayoutInfos ChangeStatus{get;set;}
		public FileBlockMetadata BlockMetadata{get;set;}


		public PosixFile(){
			BlockMetadata = new FileBlockMetadata();
		}

		public PosixFile(string fName){

			this.SnapFullPath = fName;
			//this.Name = this.SnapFullPath.Substring(this.SnapFullPath.LastIndexOf('/')+1);
			ufi = new UnixFileInfo(this.SnapFullPath);
			this.Name = ufi.Name;
			this.Kind = GetUKind();
			Console.WriteLine ("file "+this.SnapFullPath+", kind="+this.Kind.ToString());
			this.ID = ufi.Inode;
			//this.IsSparse = false;
			if(this.Kind == FileType.File) // though dirs also have sizes, useless to get it
				this.FileSize = ufi.Length;
			//this.LastAccessedTime = ufi.LastAccessTime;
			this.LastModifiedTime = Utilities.Utils.GetUtcUnixTime(ufi.LastWriteTimeUtc); //ufi.LastWriteTime.ToFileTimeUtc();
			this.LastMetadataModifiedTime = Utilities.Utils.GetUtcUnixTime(ufi.LastStatusChangeTimeUtc); //ufi.LastStatusChangeTime.ToFileTimeUtc();
			this.CreateTime = 0; // dummy value for correctness of incrementals using filecompare
			this.Permissions = (uint)ufi.FileAccessPermissions;

			this.SpecialAttributes = (int)ufi.FileSpecialAttributes;
			
			if(this.Kind == FileType.Symlink){
				UnixSymbolicLinkInfo link = new UnixSymbolicLinkInfo(this.SnapFullPath);
				if(link.HasContents)
					this.TargetName = link.GetContents().FullName;
			}
			this.OwnerUser = (uint)ufi.OwnerUserId;
			this.OwnerGroup = (uint)ufi.OwnerGroupId;
			BlockMetadata = new FileBlockMetadata();
		}
		
		public PosixFile(Dirent entry){
			unixEntry = entry; // used for clone()
			this.SnapFullPath = entry.d_name;
			this.Name = this.SnapFullPath.Substring(this.SnapFullPath.LastIndexOf('/')+1);
			ID = (long)entry.d_ino;
			this.Kind = GetKind(entry);
			//this.IsSparse = false;
			// try to get ID version (inode generation number), for easier delete/move/renames detection
			// EOPNOTSUP = 95

			Stat entryStat;
			if(this.Kind != FileType.Symlink){
				//Syscall.fstat(fd, out entryStat);
				Syscall.stat(this.SnapFullPath, out entryStat);
				string[] xattrs = new string[16];
				Syscall.listxattr(this.SnapFullPath, out xattrs);
				GetXattrs(xattrs);
			}
			else{
				Syscall.lstat(this.SnapFullPath, out entryStat);
				System.Text.StringBuilder sb = new System.Text.StringBuilder(2048);
				Syscall.readlink(this.SnapFullPath, sb);
				this.TargetName = sb.ToString();
				//Console.WriteLine ("symlink pointing to "+targetName);
			}
			//this.LastAccessedTime = Utils.GetDateTimeFromUnixTime(entryStat.st_atime);
			this.LastMetadataModifiedTime = entryStat.st_ctime;
			this.LastModifiedTime = entryStat.st_mtime;
			this.CreateTime = 0; //DateTime.MinValue.ToFileTimeUtc();
			this.Permissions = (uint)entryStat.st_mode;
			if(this.Kind == FileType.File) // though dirs also have sizes, useless to get it
				this.FileSize = entryStat.st_size; 
			this.OwnerUser = entryStat.st_uid;
			this.OwnerGroup = entryStat.st_gid;
			this.SpecialAttributes = 0;// TODO!!!
			BlockMetadata = new FileBlockMetadata();
			//Syscall.close(fd);
		}
		
		public IFSEntry Clone(){
			PosixFile newF = new PosixFile(unixEntry);
			newF.ParentID = this.ID;
			newF.ChangeStatus = this.ChangeStatus; // propagate changestatus flags for correct incremental processing
			return newF;

		}
		
		public Stream OpenStream(FileMode fileMode){
			if(Utilities.ConfigManager.GetValue("OPTIMIZED_FILE_ACCESS") == "true")
				return new LinuxStream(this.SnapFullPath, fileMode);
			else return new FileStream(this.SnapFullPath, fileMode);
		}
		
		/*public void CloseStream(){
			if(fd > 0)
				Syscall.close(fd);
		}*/
		

		
		private int GetUAttributes(UnixFileInfo fi){
			return (int)fi.FileSpecialAttributes;
		}
		
		private void SetUAttributes(){
			
		}
		
		private FileType GetUKind(){
			
			if (ufi.FileType == Mono.Unix.FileTypes.Directory)
				return FileType.Directory;
			else if(ufi.FileType == Mono.Unix.FileTypes.SymbolicLink)
				return FileType.Symlink;
			else if(ufi.FileType == Mono.Unix.FileTypes.RegularFile)	
				return FileType.File;
			
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
		
		private FileType GetKind(Dirent e){
			
			switch((DirentType)e.d_type){
			case DirentType.DT_DIR:
					return FileType.Directory;
			case DirentType.DT_LNK:
					return FileType.Symlink;
			case DirentType.DT_REG:
				return FileType.File;
			case DirentType.DT_BLK:
				return FileType.BlockDevice;
			case DirentType.DT_CHR:
				return FileType.CharDevice;
			default:
				return FileType.Unsupported;
				
			}
			
			//return FileType.Unsupported;                
		}
		
		private void GetXattrs(string[] attrsList){
			List<Tuple<string, byte[]>> attrPairs = new List<Tuple<string, byte[]>>();
			foreach(string attr in attrsList){
				byte[] attrValue = new byte[2048]; // hard limit attribute size to 2k
				if(this.Kind != FileType.Symlink)
					Syscall.getxattr(this.SnapFullPath, attr, out attrValue);
				else
					Syscall.lgetxattr(this.SnapFullPath, attr, out attrValue);
					attrPairs.Add(new Tuple<string, byte[]>(attr, attrValue));
				Console.WriteLine ("Item '"+this.SnapFullPath+"' has xattribute '"+attr+"'");
			}
			this.ExtendedAttributes = attrPairs;

		}
	
		private enum DirentType : byte{
		    DT_UNKNOWN = 0,
		    DT_FIFO = 1,
		    DT_CHR = 2,
		    DT_DIR = 4,
		    DT_BLK = 6,
		    DT_REG = 8,
		    DT_LNK = 10,
		    DT_SOCK = 12,
		    DT_WHT = 14
	  	};


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
		
		

		
		public void GetObjectData(SerializationInfo info, StreamingContext context){
		 
		    //info.AddValue("attrs", this.Attributes);
			// crtime is almost never implemented on *nix Oses and filesystems. Forget it for now.
		    //info.AddValue("crtime", this.CreateTime);

		    //info.AddValue("ctime", this.LastMetadataModifiedTime);
			//info.AddValue("mtime", this.LastModifiedTime);
			/*info.AddValue("k", this.Kind, typeof(Node.FileType));
			info.AddValue("s", this.FileSize);*/
			//info.AddValue("csp", this.ChunkStartPos);
			//info.AddValue("fsp", this.FileStartPos);


			/*info.AddValue("og", this.OwnerGroup);
			info.AddValue("ou", this.OwnerUser);
			info.AddValue("perm", (uint)this.Permissions);
			info.AddValue("sattr", this.SpecialAttributes);*/
			if(this.ExtendedAttributes != null && this.ExtendedAttributes.Count >0)
				info.AddValue("xattr", this.ExtendedAttributes);
			if(this.BlockMetadata.BlockMetadata != null && this.BlockMetadata.BlockMetadata.Count >0)
				info.AddValue("data", this.BlockMetadata.BlockMetadata, typeof(FileBlockMetadata));
			if(this.TargetName != null)
				info.AddValue("tgt", this.TargetName, typeof(string));
			
		}
		
		protected PosixFile(SerializationInfo info,StreamingContext context){
		
			//this.Attributes = info.GetInt32("attrs");
			//Console.WriteLine ("PosixFile GetObjectData() : got attrs");
			//this.CreateTime = info.GetInt64("crtime");
			//this.LastMetadataModifiedTime = info.GetInt64("ctime");
			//Console.WriteLine ("PosixFile GetObjectData() : got crtime & ctime");
			//this.LastModifiedTime = info.GetInt64("mtime");
			//Console.WriteLine ("PosixFile GetObjectData() : got mtime");
			/*this.Kind = (Node.FileType)info.GetValue("k", typeof(Node.FileType));
			//Console.WriteLine ("PosixFile GetObjectData() : got k");
			this.FileSize = info.GetInt64("s");*/
			//Console.WriteLine ("PosixFile GetObjectData() : got s");
			//this.ChunkStartPos = info.GetUInt32("csp");
			//Console.WriteLine ("PosixFile GetObjectData() : got csp");
			//this.FileStartPos = info.GetInt64("fsp");
			//Console.WriteLine ("PosixFile GetObjectData() : got fsp");
			//this.OwnerGroup = info.GetUInt32("og");
			//Console.WriteLine ("PosixFile GetObjectData() : got og");
			//this.OwnerUser = info.GetUInt32("ou");
			//Console.WriteLine ("PosixFile GetObjectData() : got ou");
			//this.Permissions = (Mono.Unix.FileAccessPermissions)info.GetValue("perm", typeof(Mono.Unix.FileAccessPermissions));
			//Console.WriteLine ("PosixFile GetObjectData() : got perm");
			//this.SpecialAttributes = info.GetInt32("sattr");
			//Console.WriteLine ("PosixFile GetObjectData() : getting xattrs..");
			try{
				this.ExtendedAttributes = (List<Tuple<string, byte[]>>)info.GetValue("xattr", typeof(List<Tuple<string, byte[]>>));
			}catch{}
			//Console.WriteLine ("PosixFile GetObjectData() : getting blockmetadata (excepted dedupinfo)..");
			try{
				this.BlockMetadata = new FileBlockMetadata();
				this.BlockMetadata.BlockMetadata = (List<IFileBlockMetadata>)info.GetValue("data", typeof(List<IFileBlockMetadata>));
			}catch{

			}
			try{
				this.TargetName = (string)info.GetValue("tgt", typeof(string));
			}catch{}
		}

		public override string ToString () {
			return string.Format("[PosixEntry: Id={0}, pId={1}, Name={2}, Size={3}, Change={4}]", this.ID, this.ParentID, this.Name, this.FileSize, this.ChangeStatus);
		}
	}
} 
#endif