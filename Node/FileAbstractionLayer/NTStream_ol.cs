#if OS_WIN
using System;
using System.IO;
//using System.Runtime.InteropServices;
using System.ComponentModel;
//using Microsoft.Win32.SafeHandles;
	
using Node.DataProcessing;
using Alphaleonis.Win32.Filesystem;
using Alphaleonis.Win32.Security;

namespace Node{
	
	/// <summary>
	///  This class allows to handle Windows NT BackupRead() using Stream semantics
	/// </summary>
	public class NTStream_:Stream{
		
		//private long currentPos;
		//private long length;
		private string fileName;
		private System.IO.FileMode fileMode;
		private BackupFileStream bs;
		//private PrivilegeEnabler priv;
		//private IntPtr fs;
		//private SafeFileHandle m_volumeHandle;
		//private IntPtr buffer = Marshal.AllocHGlobal(1024*1024); // 1Mb buffer size
		//IntPtr context;
		//byte[] buffer = new byte[1024*1024];
		//private PrivilegeEnabler privilege;
		public override bool CanRead{
			get{ return true;}
		}
		
		public override bool CanWrite{
			get{ 
				return true;
			}
		}
		
		public override bool CanSeek{
			get{return true;}
		}
		
		public override long Position{
			get{ return bs.Position;}
			set{ bs.Position = value;}
		}
		
		public override long Length{
			get{ return bs.Length;}	
		}
		
		public override void SetLength(long value){
			bs.SetLength(value);
		}
		
		public override void Flush(){
			bs.Flush();
		}
		
	
		public NTStream_(string fName, System.IO.FileMode mode){
			fileName = fName;
			if(mode == System.IO.FileMode.Open){
				bs = new BackupFileStream(fName, 
				                          Alphaleonis.Win32.Filesystem.FileMode.Open, 
										 /* FileSystemRights.ReadPermissions|FileSystemRights.SystemSecurity, */
				                          FileSystemRights.ReadPermissions,
				                          Alphaleonis.Win32.Filesystem.FileShare.ReadWrite, 
				                          Alphaleonis.Win32.Filesystem.FileOptions.SequentialScan|
				                          	Alphaleonis.Win32.Filesystem.FileOptions.BackupSemantics
				                     
				                          	/*|Alphaleonis.Win32.Filesystem.FileOptions.NoBuffering
				                          	|Alphaleonis.Win32.Filesystem.FileOptions.BackupSemantics*/

				);
			}
			else if(mode == System.IO.FileMode.OpenOrCreate){
				bs = new BackupFileStream(fName, 
				                          Alphaleonis.Win32.Filesystem.FileMode.OpenOrCreate,/*also use TRUNCATE??*/ 
										  FileSystemRights.AppendData|FileSystemRights.CreateFiles
				                          	|FileSystemRights.Write|FileSystemRights.SystemSecurity, 
				                          Alphaleonis.Win32.Filesystem.FileShare.Read, 
				                          Alphaleonis.Win32.Filesystem.FileOptions.SequentialScan
				);
			}
			//bs = new BackupFileStream(fName, Alphaleonis.Win32.Filesystem.FileMode.Open);
			//Console.WriteLine ("bstream length="+bs.Length);
		}
				
		public override int Read(byte[] buffer, int offset, int count){
			//Console.WriteLine ("begin read");
			int read = bs.Read(buffer, offset, count, true);
			//Console.WriteLine ("NTStream : "+fileName+" read ("+read+")\n");
			return read; //bs.Read(buffer, offset, count, true);
			
		}
		
		public override void Write(byte[] buffer, int offset, int count){
			bs.Write(buffer, offset, count);
		}
		
		public override long Seek(long offset, SeekOrigin origin){
			//return bs.Seek(offset, origin);
			if(offset == 0) return 0;

			long skipped = 0;
			int loop=0;
			while(skipped < offset && loop <10){
				Read(new byte[20], 0, 20);
				skipped += bs.Skip(offset-skipped);
				Console.WriteLine ("NTSTREAM skipped : "+skipped+"/"+offset);
				loop++;
			}
			return skipped; //bs.Skip(offset);
		}
		
		public override void Close(){
			bs.Close();
			bs.Dispose();
			//priv.Dispose();
		}
		
		
	}
}

#endif