#if OS_WIN
/*using System;
using System.Security.Cryptography;
//using System.Text;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Node.Utilities;
using Node.DataProcessing;
using Microsoft.Win32.SafeHandles;
using Alphaleonis.Win32.Filesystem;
using System.IO;
using P2PBackup.Common;

namespace Node{
	
	//public enum FileType{File,Symlink,Hardlink,Socket,Directory,CharDevice,BlockDevice,Fifo,Stream,Hidden,Unsupported}
	
	/// <summary>
	///  Represents a Windows NT Backup IFile.
	/// NT has a very special (but useful) way to access files for backup : since NTFS allow multiple data streams per file,
	/// and userspace/.Net has no easy access to streams (except the "default" one), we have to use the BackupRead 
	/// (BackupWrite for restore) API call. BackupRead allows us to retrieve a complete "dump" of an NTFS file, including security
	/// permissions and data streams. (Note : alternate streams are heavily used under Windows, so it is mandatory to backup them
	/// to allow a consistent restore).
	/// </summary>
	[Serializable]
	public class NTBackupFileXP:IFSEntry{
		

		[field: NonSerialized]private System.IO.FileInfo fi;
		[field: NonSerialized]private string fileName;	// the snapshotted file/item fullpath
		//[field: NonSerialized]
		private string originalFullPath;
		private long fileSize;
		private uint chunkStartPos;
		private FileType fileKind;
		
		// TODO: windows permissions
		
		private uint ownerUser;
		private uint ownerGroup;
		private int fileAttributes;
		private int fileSpecialAttributes;
		private int filePermissions;
		[field: NonSerialized]private FileSecurity wSecurity;
		private long lastModifiedTime;
		//private DateTime createTime;
		[field: NonSerialized] private FileSystemEntryInfo fseInfo;
		//private IntPtr fd;
		public string OriginalFullPath{
			get{return originalFullPath;}
			set{originalFullPath = value;}
		}
		
		public string SnapFullPath{
			set{fileName = value;}
			get{return fileName;}
		}
		
		public  long ID{get;set;} // TODO : update AlphaFS to retrieve ntfs ID (but difficult since alphafs uses native structures not containing id)
		public long ParentID{get;set;}
		
		public  string Name{get; set;}
		
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
		//public DateTime LastAccessedTime{get;set;}
		public long LastMetadataModifiedTime{get; set;}
		
		public long CreateTime{get;set;}
			
		public FileType Kind{
			get{return fileKind;}	
			set{fileKind = value;}
		}
		
		public int Attributes{
			get{return fileAttributes	;}
			set{fileAttributes = value;}
		}
		
		public int SpecialAttributes{
			get{return fileSpecialAttributes	;}
			set{fileSpecialAttributes = value;}
		}
		
		public int Permissions{
			get{return filePermissions	;}
			set{filePermissions = value;}
		}
		
		 public FileSecurity WSecurity{
			get{return wSecurity;}
			set{wSecurity = value;}
		}
		
		public uint OwnerUser{
			set{ownerUser = value;}
			get{return ownerUser;}
		}
		
		public uint OwnerGroup{
			set{ownerGroup = value;}
			get{return ownerGroup;}
		}
		

		public DataLayoutInfos ChangeStatus{get;set;}

		// Target of a symbolic link/mountpoint/reparsepoint
		public String TargetName{get;set;}
		
		public FileBlockMetadata BlockMetadata{get;set;}
		


		public NTBackupFileXP(){}
		
		public NTBackupFileXP(string fullName){
			//Console.WriteLine ("NTBackupFileXP (byname) raw path="+fullName);
			if(fullName == null) throw new Exception("NTBackupFileXP(byname): NULL name");
			//Console.WriteLine ("NTBackupFile(byname): raw name="+fullName);
			//System.IO.FileInfo fsi = new System.IO.FileInfo(fullName);
			Alphaleonis.Win32.Filesystem.FileInfo fsi = new Alphaleonis.Win32.Filesystem.FileInfo(fullName);
			fsi.Refresh();
			fileName = fullName;
			Init (fsi.SystemInfo);
		}
		
		public NTBackupFileXP(Alphaleonis.Win32.Filesystem.FileSystemEntryInfo fsi){
			fileName = fsi.FullPath;
			Init(fsi);
		}

		private void Init(Alphaleonis.Win32.Filesystem.FileSystemEntryInfo fsi){

			this.Name = fsi.FileName;
			fseInfo = fsi;
			this.fileKind = GetKind(fsi);
			if(this.fileKind == FileType.File || this.fileKind == FileType.Directory){
				try{
					this.fileSize = GetSize();
				}
				catch(Exception e){
					Logger.Append(Severity.ERROR, "Unable to get size of item "+SnapFullPath+" "+e.Message);
					throw(e);
				}
			}
			if(this.fileKind != FileType.Directory || this.fileKind != FileType.Unsupported)
				this.lastModifiedTime = fsi.LastModified.ToFileTimeUtc();
			else
				this.lastModifiedTime = DateTime.MaxValue.ToFileTimeUtc();
			this.LastMetadataModifiedTime = 0; //DateTime.MinValue.ToFileTimeUtc(); // dummy value for correctness of incrementals using filecompare
			this.CreateTime = fsi.Created.ToFileTimeUtc();
			long id = 0;
			GetEntryId(ref id);
			this.ID =  id;

			if(fsi.Attributes.HasFlag(Alphaleonis.Win32.Filesystem.FileAttributes.SparseFile))
				this.ChangeStatus |= DataLayoutInfos.SparseFile;
			if(fseInfo.IsMountPoint || fseInfo.IsReparsePoint || fseInfo.IsSymbolicLink){
				this.TargetName = fseInfo.VirtualFullPath;
				Console.WriteLine("** Item "+fileName+" is a "+this.fileKind);
			}
			if(this.fileKind == FileType.Unsupported)
				Console.WriteLine("unsupported file "+fileName+" with attributes "+fi.Attributes.ToString());
			this.Attributes = (int)fsi.Attributes;
			//wSecurity =  GetSecurity(); // unneeded as we save using BackupRead(), which includes security info
			//ownerUser = wSecurity.GetOwner(typeof(NTAccount)).;
			BlockMetadata = new FileBlockMetadata();
		}

		public  IFSEntry Clone(){
			return (IFSEntry)new NTBackupFileXP(fseInfo);	
		}
		
		public Stream OpenStream(System.IO.FileMode fileMode){
			return (Stream)new NTStream(fileName, fileMode);
			//return (Stream) new NTStream_old(fileName, fileMode);
		}
		
		internal FileType GetKind(Alphaleonis.Win32.Filesystem.FileSystemEntryInfo fseInfo){
			try{
			
			if(fseInfo.IsReparsePoint)
				return FileType.MountPoint;
			else if(fseInfo.IsSymbolicLink)
				return FileType.Symlink;
			else if(fseInfo.IsDirectory)
				return FileType.Directory;
			else if(fseInfo.Attributes == Alphaleonis.Win32.Filesystem.FileAttributes.Offline)	
				return FileType.Unsupported;
			else if(fseInfo.Attributes.HasFlag(Alphaleonis.Win32.Filesystem.FileAttributes.Device))
				return FileType.BlockDevice;
			else if(fseInfo.IsFile)
				return FileType.File;
			else
				Console.WriteLine ("unsupported file type for "+fileName+", attributes : "+fseInfo.Attributes.ToString());
				return FileType.Unsupported;
			}
			catch(Exception e){ // for debug only!!! 
				Console.WriteLine ("Could not get file kind "+e.Message+" ---- "+e.StackTrace);	
			}
			return FileType.Unsupported;

			// <TODO> DLLimport to detect hardlinks and softlinks and know where they point to, on NTFS !! important
			// 
		}
		

		
		private enum StreamType{
			  Data = 1, ExternalData = 2, SecurityData = 3, 
			  AlternateData = 4, Link = 5, PropertyData = 6, 
			  ObjectID = 7, ReparseData = 8, SparseDock = 9
		}

		private struct StreamInfo{
			 private readonly string Name;
			  private readonly StreamType Type;
			  private readonly long Size;
			  private StreamInfo(string name, StreamType type, long size){
			    Name = name;
			    Type = type;
			    Size = size;
			  }
		}
		[DllImport("kernel32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool BackupRead(
		    SafeFileHandle hFile, IntPtr lpBuffer,
		    uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead,
		    [MarshalAs(UnmanagedType.Bool)] bool bAbort,
		    [MarshalAs(UnmanagedType.Bool)] bool bProcessSecurity,
		    ref IntPtr lpContext);
		
		[DllImport("kernel32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool BackupSeek(SafeFileHandle hFile,
		    uint dwLowBytesToSeek, uint dwHighBytesToSeek,
		    out uint lpdwLowByteSeeked, out uint lpdwHighByteSeeked,
		    ref IntPtr lpContext);
		
		[StructLayout(LayoutKind.Sequential, Pack=4)]
		private struct Win32StreamID{
			  public StreamType dwStreamId;
			  public int dwStreamAttributes;
			  public long Size;
			  public int dwStreamNameSize;
			  // WCHAR cStreamName[1]; 
		}
		
		internal void GetEntryId(ref long ID){
			IntPtr fsiHandle = UnsafeOpen();
			BY_HANDLE_FILE_INFORMATION handleInfo;
			if(GetFileInformationByHandle(fsiHandle, out handleInfo))
				ID = handleInfo.FileIndex.FileIndexHigh;
			else
				throw new Exception("unable to get entry ID for '"+fileName+"'");
			if(!CloseHandle(fsiHandle))
				Console.WriteLine ("####### unable to close handle for item "+fileName);

		}

		// see if http://www.declaresub.com/wiki/index.php/Alternate_Data_Streams (QueryInformationFile) performs better and is more accurate
		internal  long GetSize(){
			long size=0;
			const int bufferSize = 4096;
			using (System.IO.FileStream fs = Alphaleonis.Win32.Filesystem.File.OpenBackupRead(fileName)){

		      IntPtr context = IntPtr.Zero;
		      IntPtr buffer = Marshal.AllocHGlobal(bufferSize);
		      try{
		        while(true){
			          uint numRead;
			          if(!BackupRead(fs.SafeFileHandle, buffer, (uint)Marshal.SizeOf(typeof(Win32StreamID)), out numRead, false, true, ref context)) 
								Logger.Append(Severity.WARNING, "Error getting size information for item "+fileName)	;
			
			          if(numRead > 0){
				          	Win32StreamID streamID = (Win32StreamID)Marshal.PtrToStructure(buffer, typeof(Win32StreamID));
				            // string name = null;
				            if (streamID.dwStreamNameSize > 0){
				            	if(!BackupRead(fs.SafeFileHandle, buffer, (uint)Math.Min(bufferSize, streamID.dwStreamNameSize), out numRead, false, true, ref context))         
				                       Logger.Append(Severity.WARNING, "Error  getting size information for item "+fileName);
				             // name = Marshal.PtrToStringUni(buffer, (int)numRead / 2);
				            }
				            size += streamID.Size;
				            if (streamID.Size > 0){
				            	uint lo, hi;
				            	BackupSeek(fs.SafeFileHandle, uint.MaxValue, int.MaxValue, out lo, out hi, ref context);
								size += lo+hi;
				            }
			          }
			          else break;
		        }
		      }
		      finally{
		        Marshal.FreeHGlobal(buffer);
		        uint numRead;
		        if (!BackupRead(fs.SafeFileHandle, IntPtr.Zero, 0, out numRead, true, false, ref context)) 
					Logger.Append(Severity.ERROR, "Error closing handle after getting size information for item "+fileName);
		      }
			  
			}// end using
			return size;
    	}

		
		[DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetFileInformationByHandle(IntPtr hFile, out BY_HANDLE_FILE_INFORMATION lpFileInformation);



		[StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct BY_HANDLE_FILE_INFORMATION{
            public uint FileAttributes;
            public FILETIME CreationTime;
            public FILETIME LastAccessTime;
            public FILETIME LastWriteTime;
            public uint VolumeSerialNumber;
            public uint FileSizeHigh;
            public uint FileSizeLow;
            public uint NumberOfLinks;

			public FileID FileIndex;
        }
		
		// Windows'isms not accessible from .NET
		const uint GENERIC_READ = 0x80000000;
		const uint FILE_SHARE_READ = 0x00000001;
		const uint FILE_FLAG_NO_BUFFERING = 0x20000000;
      	const uint OPEN_EXISTING = 3;
		const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
		//const uint FILE_ID_INFO = 0x12;
		
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct FILETIME {
            public uint DateTimeLow;
            public uint DateTimeHigh;
        }
		
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct FileID {
            public uint FileIndexLow;
            public uint FileIndexHigh;
        }
		
		[DllImport("kernel32", SetLastError=true)]
      	static extern unsafe IntPtr CreateFile(
            string FileName,                    // file name
            uint DesiredAccess,                 // access mode
            uint ShareMode,                     // share mode
            uint SecurityAttributes,            // Security Attributes
            uint CreationDisposition,           // how to create
            uint FlagsAndAttributes,            // file attributes
            int hTemplateFile                   // handle to template file
            );
		
		/// <summary>
		/// Optimizes file reading with FILE_FLAG_NO_BUFFERING (don't pollute OS cache)
		/// </summary>
		/// <returns>
		/// A <see cref="IntPtr"/>
		/// </returns>
		private IntPtr UnsafeOpen(){
			IntPtr handle = IntPtr.Zero;
            // open the existing file for reading    
			// Note : we use the BACKUP_SEMANTICS, else retrieving file id fails on directories (http://msdn.microsoft.com/en-us/library/windows/desktop/aa365258(v=vs.85).aspx)
            handle = CreateFile(this.SnapFullPath,  FILE_SHARE_READ, 0, 0, OPEN_EXISTING,  FILE_FLAG_NO_BUFFERING | FILE_FLAG_BACKUP_SEMANTICS,0);
			// <TODO> : see if FILE_FLAG_SEQUENTIAL_SCAN can also help and is not mutually exclusive with FILE_FLAG_NO_BUFFERING
            return handle;
      	}
		
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool CloseHandle(IntPtr handle);
		
		
		public void GetObjectData(SerializationInfo info, StreamingContext context){
		   // info.AddValue("id", this.ID);
			//info.AddValue("pid", this.ParentID);
		   // info.AddValue("attrs", this.Attributes);
		   // info.AddValue("crtime", this.CreateTime);
		    //info.AddValue("ctime", this.LastMetadataModifiedTime);
			//info.AddValue("mtime", this.LastModifiedTime);
			//info.AddValue("k", this.Kind);
			//info.AddValue("s", this.FileSize);
			//info.AddValue("csp", this.ChunkStartPos);
			//info.AddValue("fsp", this.FileStartPos);
			//info.AddValue("og", this.OwnerGroup);
			//info.AddValue("ou", this.OwnerUser);
			//info.AddValue("perm", this.Permissions);
			//info.AddValue("sattr", this.SpecialAttributes);
			info.AddValue("data", this.BlockMetadata);
		}

		protected NTBackupFileXP(SerializationInfo info,StreamingContext context){
			//this.ID = info.GetInt64("id");
			//Console.WriteLine ("PosixFile GetObjectData() : got id");
			//this.ParentID = info.GetInt64("pid");
			//Console.WriteLine ("PosixFile GetObjectData() : got pid");
			//Console.WriteLine ("About to start deserialization");
			//this.Attributes = info.GetInt32("attrs");
			//Console.WriteLine ("NTBackupFileXP GetObjectData() : got attrs");
			//this.CreateTime = info.GetInt64("crtime");
			//this.LastMetadataModifiedTime = info.GetInt64("ctime");
			//Console.WriteLine ("NTBackupFileXP GetObjectData() : got crtime & ctime");
			//this.LastModifiedTime = info.GetInt64("mtime");
			//Console.WriteLine ("NTBackupFileXP GetObjectData() : got mtime");
			//this.Kind = (Node.FileType)info.GetValue("k", typeof(Node.FileType));
			//Console.WriteLine ("NTBackupFileXP GetObjectData() : got k");
			//this.FileSize = info.GetInt64("s");
			//Console.WriteLine ("NTBackupFileXP GetObjectData() : got s");
			//this.ChunkStartPos = info.GetUInt32("csp");
			//Console.WriteLine ("NTBackupFileXP GetObjectData() : got csp");
			//this.FileStartPos = info.GetInt64("fsp");
			//Console.WriteLine ("NTBackupFileXP GetObjectData() : got fsp");
			//this.OwnerGroup = info.GetUInt32("og");
			//Console.WriteLine ("NTBackupFileXP GetObjectData() : got og");
			//this.OwnerUser = info.GetUInt32("ou");
			//Console.WriteLine ("NTBackupFileXP GetObjectData() : got ou");
			//this.Permissions = info.GetInt32("perm");
			//Console.WriteLine ("NTBackupFileXP GetObjectData() : got perm");
			//this.SpecialAttributes = info.GetInt32("sattr");
			//Console.WriteLine ("NTBackupFileXP GetObjectData() : got sattr");
			this.BlockMetadata = (FileBlockMetadata)info.GetValue("data", typeof(FileBlockMetadata));
			//Console.WriteLine ("NTBackupFileXP GetObjectData() : got data");
		}
	}
	
	

} 
*/
#endif