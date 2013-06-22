using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Runtime.InteropServices;
using Node.Utilities;

namespace Node{
	
	//public enum FileType{File,Symlink,Hardlink,Socket,Directory,CharDevice,BlockDevice,Fifo,Stream,Hidden,Unsupported}
	
	/// <summary>
	///  Represent an Unix file.
	/// </summary>
	[Serializable]
	public class NTFile:IFile{
		
		[field: NonSerialized]private FileInfo fi;
		private string fileName;	// the original file name
		private long fileSize;
		private long chunkStartPos;
		private FileType fileKind;
		
		// TODO: windows permissions
		
		private long ownerUser;
		private long ownerGroup;
		private int fileAttributes;
		private int fileSpecialAttributes;
		private int filePermissions;
		private DateTime lastModifiedTime;
		private DateTime createTime;
		
		private IntPtr fd;
		
		public string FileName{
			set{fileName = value;}
			get{return fileName;}
		}

		//public string PathTemp{
		//	set{pathTemp = value;}
		//	get{return pathTemp;}
		//}
		public long ChunkStartPos {
			get {return this.chunkStartPos;}
			set { this.ChunkStartPos = value;}
		}

		public long FileSize{
			set{fileSize = value;}
			get{return fileSize;}
		}
		
		public DateTime LastModifiedTime{
			set{lastModifiedTime = value;}
			get{return lastModifiedTime;}
		}
		
		public DateTime CreateTime{
			set{createTime = value;}
			get{return createTime;}
		}
		
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
		
		public long OwnerUser{
			set{ownerUser = value;}
			get{return ownerUser;}
		}
		
		public long OwnerGroup{
			set{ownerGroup = value;}
			get{return ownerGroup;}
		}
		
	
		
		// TODO : Windows permissions/ACLs
	
		
		/*public BFile(){
		
		}*/

		public NTFile(string fN, long chunkStartPos){
			fileName = fN;
			fi = new FileInfo(fN);
			this.chunkStartPos = chunkStartPos;
			this.fileSize = fi.Length;
			filePermissions = 0; // irrelevant under windows 
			fileAttributes = (int)fi.Attributes;
			fileSpecialAttributes = 0;
			fileKind = GetKind();
			ownerUser = 0; //fi.GetAccessControl().
			ownerGroup = 0; //fi.OwnerGroupId;
		}
		
		// TODO : get rid of FileInfo inside constructor
		public NTFile(string fileName){
			fileName = fileName;
			//fi = new FileInfo(fileName);
		}
		
		/*public WFile(FileInfo fi, long chunkStartPos){
			fileName = fi.FullName;
			fileSize = fi.Length;
			this.chunkStartPos = chunkStartPos;
			
				fileAttributes = GetWAttributes();
				fileKind = GetWKind();
				ownerUser = GetWOwner();
				//ownerGroup = GetWGroup();
			lastModifiedTime = fi.LastWriteTime;
			createTime = fi.CreationTime;
			
		}*/
		
		
		private int GetWAttributes(){
			return (int)File.GetAttributes(this.fileName);
		}
		
		
		
		private void SetUAttributes(){
			
		}
		
		public Stream OpenStream(){
			//return new FileStream(fi.FullName, FileMode.Open, FileAccess.Read);
			//new FileStream(
			fd = UnsafeOpen();
			if ((int)fd == -1){
				Logger.Append(Severity.INFO, "WFile.OpenStream", "Unable to open file with optimized parameters. Falling back to regular open");
                return new FileStream(fileName, FileMode.Open, FileAccess.Read);
            }
			else
				return (Stream)new FileStream(fd, FileAccess.Read);
		}
		
		public void CloseStream(){
			
		}
		
		private long GetWOwner(){
			// <TODO> !!!
			Type obTypeToGet = Type.GetType("System.Security.Principal.NTAccount");
			//return File.GetAccessControl(originalFileName).GetOwner(obTypeToGet).ToString
			  FileInfo fileInfo = new FileInfo (@"C:\Contacts.txt");
            FileSecurity fileSecurity = fileInfo.GetAccessControl();
            IdentityReference identityReference = fileSecurity.GetOwner(typeof(NTAccount));
            
			return 0;
		}
		
		private FileType GetKind(){
			if(fi.Attributes == FileAttributes.Offline)	
				return FileType.Unsupported;
			else if (fi.Attributes == FileAttributes.Directory)
				return FileType.Directory;
			else if(fi.Attributes == FileAttributes.Device)
				return FileType.BlockDevice;
			else if(fi.Attributes == (FileAttributes.ReparsePoint|FileAttributes.Directory)) // This is a mount point!! (NTFS Junction)
				return FileType.Unsupported; 
			else if(fi.Attributes == FileAttributes.ReparsePoint)
				return FileType.Symlink;
			else	
				return FileType.File;
			// <TODO> DLLimport to detect hardlinks and softlinks and know where they point to, on NTFS !! important
			// 
		}
		
		[DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetFileInformationByHandle(IntPtr hFile, out BY_HANDLE_FILE_INFORMATION lpFileInformation);

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BY_HANDLE_FILE_INFORMATION{
            public uint FileAttributes;
            public FILETIME CreationTime;
            public FILETIME LastAccessTime;
            public FILETIME LastWriteTime;
            public uint VolumeSerialNumber;
            public uint FileSizeHigh;
            public uint FileSizeLow;
            public uint NumberOfLinks;
            public uint FileIndexHigh;
            public uint FileIndexLow;
        }
		
		// Windows'isms not accessible from .NET
		const uint GENERIC_READ = 0x80000000;
		const uint FILE_FLAG_NO_BUFFERING = 0x20000000;
      	const uint OPEN_EXISTING = 3;
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct FILETIME {
            public uint DateTimeLow;
            public uint DateTimeHigh;
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
            handle = CreateFile(this.FileName,  GENERIC_READ, 0, 0, OPEN_EXISTING,  FILE_FLAG_NO_BUFFERING,0);
			// <TODO> : see if FILE_FLAG_SEQUENTIAL_SCAN can also help and is not mutually exclusive with FILE_FLAG_NO_BUFFERING
            return handle;
      	}
		private string GetPointingTo(){
		
			return "";
		}
		
		/*private FileType GetWKind(){
			return FileType.File;
		}*/
		
		
		
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
		
		
		
		private void SetAttributes(){
			fi.Attributes = (FileAttributes)this.Attributes;
		}
		
	}
} 
