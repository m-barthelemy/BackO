#if OS_WIN
using System;
using System.IO;
using System.Security.Cryptography;
//using System.Text;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Node.Utilities;
using Node.Utilities.Native;
using Node.DataProcessing;
using Microsoft.Win32.SafeHandles;
//using System.ComponentModel;
using Alphaleonis.Win32.Filesystem;
using P2PBackup.Common;

namespace Node{
	

	/// <summary>
	///  Represents a Windows NT Backup IFile.
	/// NT has a very special (but useful) way to access files for backup : since NTFS allow multiple data streams per file,
	/// and userspace/.Net has no easy access to streams (except the "default" one), we have to use the BackupRead 
	/// (BackupWrite for restore) API call. BackupRead allows us to retrieve a complete "dump" of an NTFS file, including security
	/// permissions and data streams. (Note : alternate streams are heavily used under Windows, so it is mandatory to backup them
	/// to allow a consistent restore).
	/// </summary>
	/// 

	// ALERT /!\  We already KNOW that the code here is, at best, inefficient, and at worst, ugly. Maybe even both.
	// Maybe we will reimplement everything here : a NT fs enumerator that reads MFT, and a custom NTBackupFile which doesn't
	// use AlphaLeonis FileSystemEntryInfo. Or extend it to implement all bits we need in order to avoid
	// the very dirty things we do here (FileSystemEntryInfo for some information, PLUS GetFileInformationByHandle to get ID,
	// PLUS FindFirstStream... 


	// TODO : tuning, use GetHandleInfos() filetime | use own enumeration routine instead of depending on AlphaFS
	[Serializable]
	public class NTBackupFile:IFSEntry{
		
		[field: NonSerialized]private string fileName;	// the snapshotted file full path
	
		[field: NonSerialized] private FileSystemEntryInfo fseInfo;
		//private IntPtr fd;
		
		public string Name{get;set;}
		public string OriginalFullPath{get;set;}
		
		public string SnapFullPath{
			set{fileName = value;}
			get{return fileName;}
		}
		
		public long ID{get;set;} // TODO : update AlphaFS to retrieve ntfs ID (but difficult since alphafs uses native structures not containing id)
		public long ParentID{get;set;}
		public uint ChunkStartPos {get;set;}
		public long FileStartPos{get; set;}
		public long FileSize{get;set;}
		public long LastModifiedTime{get;set;}
		public long LastMetadataModifiedTime{get; set;}
		public long CreateTime{get;set;}
		public FileType Kind{get;set;}
		public int Attributes{get;set;}
		public List<Tuple<string, byte[]>> ExtendedAttributes{get;set;}
		public int SpecialAttributes{get;set;}
		public uint Permissions{get;set;}
		public FileSecurity WSecurity{get;set;}
		public uint OwnerUser{get;set;}
		public uint OwnerGroup{get;set;}
		public DataLayoutInfos ChangeStatus{get;set;}
		// Target of a symbolic link/mountpoint/reparsepoint
		public String TargetName{get;set;}
		public FileBlockMetadata BlockMetadata{get;set;}
		

		public NTBackupFile(string fullName){

			if(fullName == null) throw new Exception("NTBackupFileXP(byname): NULL name");
			//Console.WriteLine ("NTBackupFile(byname): raw name="+fullName);

			Alphaleonis.Win32.Filesystem.FileInfo fsi = new Alphaleonis.Win32.Filesystem.FileInfo(fullName);
			fsi.Refresh();

			fseInfo = fsi.SystemInfo;
			fseInfo.FullPath = fullName;
			this.Name = fsi.Name;
			fileName = fullName;
			GetHandleInfos(); // gets ID and sparse attribute.
			//this.fileSize = fileI.Length;
			this.FileStartPos = 0;
			this.ChunkStartPos = 0;
			this.Kind = GetKind(fseInfo);

			// GetSize is more precise (though not yet 100%) but slower. As we will generally work on snapshot, 
			// don't be so obsessed with getting real size and reporting sizes changes during backup on NT.
			//GetSize (); 
			this.FileSize = fseInfo.FileSize;

			if(this.Kind == FileType.Symlink)
				this.TargetName = fsi.SystemInfo.VirtualFullPath;

			if(this.Kind != FileType.Directory || this.Kind != FileType.Unsupported)
				this.LastModifiedTime = Utilities.Utils.GetUtcUnixTime(fsi.LastWriteTime);// fsi.LastWriteTime.ToFileTimeUtc();
			else
				this.LastModifiedTime = 0; //fsi.LastWriteTime.ToFileTimeUtc(); //DateTime.MaxValue.ToFileTimeUtc();
			this.LastMetadataModifiedTime = 0; // dummy value for correctness of incrementals using filecompare
			this.CreateTime = Utilities.Utils.GetUtcUnixTime(fsi.CreationTime);//fsi.CreationTime.ToFileTimeUtc();
			//this.ID = Utilities.Utils.GetUnixTime(fsi.CreationTime);
			if(fsi.Attributes.HasFlag(Alphaleonis.Win32.Filesystem.FileAttributes.SparseFile))
				this.ChangeStatus |= DataLayoutInfos.SparseFile;
			if(fseInfo.IsMountPoint || fseInfo.IsReparsePoint || fseInfo.IsSymbolicLink){
				this.TargetName = fseInfo.VirtualFullPath;
				Console.WriteLine("** Item "+fileName+" is a "+this.Kind);
				Console.WriteLine ("reparsepoint tag(s)="+fsi.SystemInfo.ReparsePointTag.ToString());
			}
			if(this.Kind == FileType.Unsupported)
				Console.WriteLine("unsupported file "+fileName+" with attributes "+fseInfo.Attributes.ToString());
			this.Attributes = (int)fsi.Attributes;
			//wSecurity =  GetSecurity(); // unneeded as we save using BackupRead(), which includes security info
			//ownerUser = wSecurity.GetOwner(typeof(NTAccount)).;
			BlockMetadata = new FileBlockMetadata();
		}
		

		
		public NTBackupFile(Alphaleonis.Win32.Filesystem.FileSystemEntryInfo fsi){

			fileName = fsi.FullPath;
			this.Name = fsi.FileName;
			fseInfo = fsi;
			this.Kind = GetKind(fsi);

			/*this.FileSize = 0;
			try{
				this.FileSize += GetSize ();
			}
			catch(Exception e){
				Logger.Append(Severity.ERROR, "Unable to get size of item "+SnapFullPath+" "+e.Message);
					throw(e);
			}*/
			this.FileSize = fsi.FileSize;

			if(this.Kind == FileType.Symlink)
				this.TargetName = fsi.VirtualFullPath;
			if(this.Kind != FileType.Directory || this.Kind != FileType.Unsupported)
				this.LastModifiedTime = Utilities.Utils.GetUtcUnixTime(fsi.LastModified); //fsi.LastModified.ToFileTimeUtc();
			else{
				this.LastModifiedTime = DateTime.MaxValue.ToFileTimeUtc();
			}

			this.LastMetadataModifiedTime = 0; // dummy value for correctness of incrementals using filecompare
			this.CreateTime = Utilities.Utils.GetUtcUnixTime(fsi.Created); //fsi.Created.ToFileTimeUtc();

			GetHandleInfos();

			if(fseInfo.IsMountPoint || fseInfo.IsReparsePoint || fseInfo.IsSymbolicLink){
				//this.TargetName = fseInfo.VirtualFullPath;
				Console.WriteLine("** Item "+fileName+" is a "+this.Kind+", target="+TargetName);
				//Console.WriteLine ("reparsepoint tag(s)="+fsi.ReparsePointTag.ToString());
			}
			if(this.Kind == FileType.Unsupported)
				Console.WriteLine("unsupported file "+fileName+" with attributes "+fseInfo.Attributes.ToString());
			this.Attributes = (int)fsi.Attributes;
			//wSecurity =  GetSecurity(); // unneeded as we save using BackupRead(), which includes security info
			//ownerUser = wSecurity.GetOwner(typeof(NTAccount)).;
			BlockMetadata = new FileBlockMetadata();
		}

		internal void GetHandleInfos(){
			IntPtr fsiHandle = UnsafeOpen();
			Win32Api.BY_HANDLE_FILE_INFORMATION handleInfo;
			if(Win32Api.GetFileInformationByHandle(fsiHandle, out handleInfo)){
				this.ID = handleInfo.FileIndex.FileIndexHigh;
				this.FileSize = handleInfo.FileSizeLow;
				//this.IsSparse = (handleInfo.FileAttributes &  512 /*FILE_ATTRIBUTE_SPARSE_FILE */);
			}
			else {
				this.ID = 0;
				throw new Exception("unable to get entry ID for '"+fileName+"' : "+(new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error())).Message);
			}
			// get target for reparse points
			if(fseInfo.IsMountPoint || fseInfo.IsReparsePoint || fseInfo.IsSymbolicLink){
				Console.WriteLine("Get target name of reparsepoint...");
				/*System.Text.StringBuilder sb = new System.Text.StringBuilder(4096);
				int retcode = Win32Api.GetFinalPathNameByHandle(fsiHandle, sb, sb.Capacity, 2);
				this.TargetName = sb.ToString();*/
				this.TargetName = GetReparseTarget(fsiHandle);
				Console.WriteLine("Got target '"+this.TargetName);

			}
			CloseHandle(fsiHandle);	
		}

		private string GetReparseTarget(IntPtr handle){
			Int32 outBufferSize = Marshal.SizeOf(typeof(Win32Api.REPARSE_GUID_DATA_BUFFER));
            IntPtr outBuffer = Marshal.AllocHGlobal(outBufferSize);
 			string NonInterpretedPathPrefix = "\\??\\"; 
			string targetDir = null;          
            // Read the reparse point data:
            Int32 bytesReturned;
            bool readOK = Win32Api.DeviceIoControl(handle,
           		Win32Api.FSCTL_GET_REPARSE_POINT,
                IntPtr.Zero,
                0,
                outBuffer,
                outBufferSize,
                out bytesReturned,
                IntPtr.Zero);
            if(readOK){
				// Get the target directory from the reparse point data:
            	Win32Api.REPARSE_GUID_DATA_BUFFER rgdBuffer = (Win32Api.REPARSE_GUID_DATA_BUFFER)Marshal.PtrToStructure(outBuffer, typeof(Win32Api.REPARSE_GUID_DATA_BUFFER));
				targetDir = System.Text.Encoding.Unicode.GetString(rgdBuffer.PathBuffer, rgdBuffer.SubstituteNameOffset, rgdBuffer.SubstituteNameLength);
                if (targetDir.StartsWith(NonInterpretedPathPrefix))
					targetDir = targetDir.Substring(NonInterpretedPathPrefix.Length);
			}
                       
            // Free the buffer for the reparse point data:
            Marshal.FreeHGlobal(outBuffer);
			return targetDir;
		}

		public NTBackupFile(){
			BlockMetadata = new FileBlockMetadata();
		}

		public virtual IFSEntry Clone(){
			NTBackupFile newF = new NTBackupFile(fseInfo);
			newF.ParentID = this.ID;
			newF.ChangeStatus = this.ChangeStatus; // propagate changestatus flags for correct incremental processing
			return (IFSEntry)newF;	
		}

		public Stream OpenStream(System.IO.FileMode fileMode){
			return (Stream)new NTStream(fileName, fileMode);
			//return (Stream) new NTStream_old(fileName, fileMode);
		}
		
		public void CloseStream(){
			
		}
		
		/*private FileSecurity GetSecurity(){
			// <TODO> !!!
			Type obTypeToGet = Type.GetType("System.Security.Principal.NTAccount");
			//return File.GetAccessControl(originalFileName).GetOwner(obTypeToGet).ToString
			  System.IO.FileInfo fileInfo = new System.IO.FileInfo (fileName);
			
            return fileInfo.GetAccessControl();
			//Alphaleonis.Win32.Filesystem.FileInfo ff = new Alphaleonis.Win32.Filesystem.FileInfo(fileName);
			//Alphaleonis.Win32.Filesystem.FileSystemRights
           
			//return 0;
		}*/
		
		internal FileType GetKind(Alphaleonis.Win32.Filesystem.FileSystemEntryInfo fseInfo){

			if(fseInfo.IsReparsePoint)
				return FileType.MountPoint;
			else if(fseInfo.IsSymbolicLink)
				return FileType.Symlink;
			else if(fseInfo.Attributes == Alphaleonis.Win32.Filesystem.FileAttributes.Offline)	
				return FileType.Unsupported;
			else if(fseInfo.Attributes.HasFlag(Alphaleonis.Win32.Filesystem.FileAttributes.Device))
				return FileType.BlockDevice;
			else if(fseInfo.IsDirectory)
				return FileType.Directory;
			else if(fseInfo.IsFile)
				return FileType.File;
			else
				Console.WriteLine ("unsupported file type for "+fileName+", attributes : "+fseInfo.Attributes.ToString());
			return FileType.Unsupported;

		
		}

		// TODO! Check if we can benefit from using GetFileInformationByHandleEx() to only retrieve FileID
		// check if there is any performance benefit. Only for > Vista & 2008

	
		/// <summary>
		/// Optimizes file reading with FILE_FLAG_NO_BUFFERING (don't pollute OS cache)
		/// </summary>
		/// <returns>
		/// A <see cref="IntPtr"/>
		/// </returns>
		internal IntPtr UnsafeOpen(){
			IntPtr handle = IntPtr.Zero;
            // open the existing file for reading   .
			// Note : we use the BACKUP_SEMANTICS, else retrieving file id fails on directories (http://msdn.microsoft.com/en-us/library/windows/desktop/aa365258(v=vs.85).aspx)
            handle = Win32Api.CreateFile(this.SnapFullPath,  0/*GENERIC_READ*/, 
				Win32Api.FILE_SHARE_DELETE | Win32Api.FILE_SHARE_READ| Win32Api.FILE_SHARE_WRITE,
				IntPtr.Zero, 
			    Win32Api.OPEN_EXISTING,  
				Win32Api.FILE_FLAG_NO_BUFFERING |Win32Api.FILE_FLAG_BACKUP_SEMANTICS|Win32Api.FILE_FLAG_OPEN_REPARSE_POINT, 
				IntPtr.Zero
			);
			int returnCode = Marshal.GetLastWin32Error();
			if(returnCode == 2 || returnCode == 3){
				Console.WriteLine ("###############NTBACKUPFILE() : file not found '"+this.Name+"' with fullpath:"+this.SnapFullPath);
				throw new FileNotFoundException(this.SnapFullPath);
			}
			// <TODO> : see if FILE_FLAG_SEQUENTIAL_SCAN can also help and is not mutually exclusive with FILE_FLAG_NO_BUFFERING
            return handle;
      	}
		
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool CloseHandle(IntPtr handle);
		

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
			//fi.Attributes = (Alphaleonis.Win32.Filesystem.FileAttributes)this.Attributes;
			//fseInfo.Attributes = (Alphaleonis.Win32.Filesystem.FileAttributes)this.Attributes;
		}
		
		//private const int ERROR_HANDLE_EOF = 38;
	    private enum StreamInfoLevels { FindStreamInfoStandard = 0 }
	
	    [DllImport("kernel32.dll", ExactSpelling=true, CharSet=CharSet.Auto, SetLastError=true)]
	    private static extern SafeFindHandle FindFirstStreamW(
	        string lpFileName, StreamInfoLevels InfoLevel,
	        [In, Out, MarshalAs(UnmanagedType.LPStruct)] 
	        WIN32_FIND_STREAM_DATA lpFindStreamData, uint dwFlags);
	
	    [DllImport("kernel32.dll", ExactSpelling=true, CharSet=CharSet.Auto, SetLastError=true)]
	    [return: MarshalAs(UnmanagedType.Bool)]
	    private static extern bool FindNextStreamW(SafeFindHandle hndFindFile,  [In, Out, MarshalAs(UnmanagedType.LPStruct)] 
	        WIN32_FIND_STREAM_DATA lpFindStreamData);
			
	    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
	    private class WIN32_FIND_STREAM_DATA{
	        public long StreamSize;
	        [MarshalAs(UnmanagedType.ByValTStr, SizeConst=296)]
	        public string cStreamName;
	    }
	

		private enum StreamType{
			  Data = 1, ExternalData = 2, SecurityData = 3, 
			  AlternateData = 4, Link = 5, PropertyData = 6, 
			  ObjectID = 7, ReparseData = 8, SparseDock = 9
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
			Win32Api.BY_HANDLE_FILE_INFORMATION handleInfo;
			if(Win32Api.GetFileInformationByHandle(fsiHandle, out handleInfo))
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
			/*using (System.IO.FileStream fs = Alphaleonis.Win32.Filesystem.File.Open(fileName, 
				Alphaleonis.Win32.Filesystem.FileMode.Open, Alphaleonis.Win32.Filesystem.FileAccess.Read, 
			    Alphaleonis.Win32.Filesystem.FileShare.ReadWrite)){*/
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


		/*

	    internal virtual long GetSize(){
			//Console.WriteLine ("GetStreams : file "+fileName);
			long size = 0;
	        WIN32_FIND_STREAM_DATA findStreamData = new WIN32_FIND_STREAM_DATA();
	        Node.SafeFindHandle handle = FindFirstStreamW(fileName, StreamInfoLevels.FindStreamInfoStandard, findStreamData, 0);
	        if (handle.IsInvalid) return 0; //throw new Exception("Invalid handle for item "+fileName);
			else
				size += findStreamData.StreamSize;
	        try{
				int lastError = 0;
				while(lastError != ERROR_HANDLE_EOF){
					FindNextStreamW(handle, findStreamData);
					size += findStreamData.StreamSize;
					lastError = Marshal.GetLastWin32Error();
				}
	          
	            if (lastError != ERROR_HANDLE_EOF) 
	                //throw new Exception(lastError.ToString());
					Logger.Append(Severity.WARNING, "Could not get file size (normal if running on XP):"+lastError.ToString());
	        }
	        finally { 
				
				//CloseHandle(handle.DangerousGetHandle());
				handle.Dispose(); 
			}
			return size;
	    }*/


		public void GetObjectData(SerializationInfo info, StreamingContext context){
		   

			if(this.TargetName != null)
				info.AddValue("tgt", this.TargetName);

			if(this.ChangeStatus == DataLayoutInfos.Deleted) return; // worthless (and leading to exception) to try to process anything if item has been deleted
			if(this.BlockMetadata.BlockMetadata != null && this.BlockMetadata.BlockMetadata.Count >0)
				info.AddValue("data", this.BlockMetadata.BlockMetadata, typeof(FileBlockMetadata));
		}

		protected NTBackupFile(SerializationInfo info,StreamingContext context){
			/*this.ID = info.GetInt64("id");
			//Console.WriteLine ("PosixFile GetObjectData() : got id");
			this.ParentID = info.GetInt64("pid");*/
			//Console.WriteLine ("PosixFile GetObjectData() : got pid");
			//this.Attributes = info.GetInt32("attrs");
			//Console.WriteLine ("PosixFile GetObjectData() : got attrs");
			//this.CreateTime = info.GetInt64("crtime");
			//this.LastMetadataModifiedTime = info.GetInt64("ctime");
			//Console.WriteLine ("PosixFile GetObjectData() : got crtime & ctime");
			//this.LastModifiedTime = info.GetInt64("mtime");
			//Console.WriteLine ("PosixFile GetObjectData() : got mtime");
			//this.Kind = (Node.FileType)info.GetValue("k", typeof(Node.FileType));
			//Console.WriteLine ("PosixFile GetObjectData() : got k");
			//this.FileSize = info.GetInt64("s");
			//Console.WriteLine ("PosixFile GetObjectData() : got s");
			//this.ChunkStartPos = info.GetUInt32("csp");
			//Console.WriteLine ("PosixFile GetObjectData() : got csp");
			//this.FileStartPos = info.GetInt64("fsp");
			//Console.WriteLine ("PosixFile GetObjectData() : got fsp");
			//this.OwnerGroup = info.GetUInt32("og");
			//Console.WriteLine ("PosixFile GetObjectData() : got og");
			//this.OwnerUser = info.GetUInt32("ou");
			//Console.WriteLine ("PosixFile GetObjectData() : got ou");
			//this.Permissions = info.GetInt32("perm");
			//Console.WriteLine ("PosixFile GetObjectData() : got perm");
			//this.SpecialAttributes = info.GetInt32("sattr");
			try{
				this.TargetName = info.GetString("tgt");
			}catch{}// target is null, nothing wrong with that if not a symlink/reparsepoint
			Console.WriteLine ("PosixFile GetObjectData() : getting blockmetadata (excepted dedupinfo)..");
			try{
				this.BlockMetadata = new FileBlockMetadata();
				this.BlockMetadata.BlockMetadata = (List<IFileBlockMetadata>)info.GetValue("data", typeof(List<IFileBlockMetadata>));
			}catch{

			} // data is null, nothing wrong with that
		}

		public override string ToString () {
			return string.Format("[PosixEntry: Id={0}, pId={1}, Name={2}, Size={3}]", this.ID, this.ParentID, this.Name, this.FileSize);
		}
	}
		

	internal sealed class SafeFindHandle : SafeHandleZeroOrMinusOneIsInvalid{
	    private SafeFindHandle() : base(true) { }
	
	    protected override bool ReleaseHandle() {
	        return FindClose(this.handle);
	    }
	
	    [DllImport("kernel32.dll")]
	    [return: MarshalAs(UnmanagedType.Bool)]
	    private static extern bool FindClose(IntPtr handle);
	}

} 

#endif