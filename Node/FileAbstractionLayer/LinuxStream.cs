#if OS_UNIX
using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Mono.Unix;
using Mono.Unix.Native;
using Node.Utilities;
using P2PBackup.Common;

namespace Node{
	
	/// <summary>
	///  This class allows to use Linux Sycall.open and use fadvise(), using Stream semantics
	/// fadvise() is used to instruct the OS to not pollute its cache with the data we read, otherwise
	/// backups would have a bigger impact on other running applications
	/// </summary>
	public class LinuxStream:Stream{
		
		private long currentPos;
		private long length;
		private string fileName;
		private FileMode fileMode;
		private int fd;
		//private IntPtr buffer = Marshal.AllocHGlobal(1024*1024); // 1Mb buffer size
		//byte[] buffer = new byte[1024*1024]; // 1Mb read/write buffer
		//IntPtr  buffer;
		IntPtr buffer;
		// sparse files constant
		private const int SEEK_HOLE=4;
		private const int SEEK_DATA=3;
		private bool canHandleSparse; // ability to handle sparse regions in file (linux >= 3.1), or their presence
		
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
			get{ return currentPos;}
			set{ Seek(value, SeekOrigin.Begin);}
		}
		
		public override long Length{
			get{ return length;}	
		}
		
		public override void SetLength(long value){
			
		}
		
		public override void Flush(){
			
		}
		
		public LinuxStream(string fileName, FileMode fileMode){
			this.fileName = fileName;	
			this.currentPos = 0;
			this.length = 0;
			this.fileMode = fileMode;
			buffer  = Marshal.AllocHGlobal(/*destBuffer.Length*/512*1024);

			if(fileMode == FileMode.Open) // read file for backup
				fd = Syscall.open(fileName, OpenFlags.O_RDONLY);
			if(fileMode == FileMode.CreateNew) //create chunk to be stored
				fd = Syscall.open(fileName, OpenFlags.O_CREAT|OpenFlags.O_WRONLY, FilePermissions.S_IRWXU);
			if(fd < 0){
				Logger.Append(Severity.INFO, "Unable to open file '"+fileName+"': "+Stdlib.GetLastError()+" (Errno "+fd+")");
				throw new IOException("Unable to open file ");
			}
			canHandleSparse = false;
			// try to detect sparse ranges ("holes" in file) if filesystem supports it
			if(fileMode == FileMode.Open){
				long sparsereturn = lseek(fd, 0, SEEK_HOLE);
				long datareturn = lseek(fd, 0, SEEK_DATA);
				//if(sparsereturn < 
				//Console.WriteLine ("file "+fileName+", size="+(new FileInfo(fileName)).Length+", seek_hole="+sparsereturn+", seek_data="+datareturn);
			}
			//catch(Exception e){
			//	Logger.Append(Severity.INFO, "Platform does not seem to support sparse file detection : "+e.Message);
			//}
		}
	
		unsafe public override int Read(byte[] destBuffer, int offset, int count){
			long numRead = 0;
			ulong numBytesToRead = (ulong)count;
			if(offset > 0)
				Seek(offset, SeekOrigin.Current);
			//IntPtr buffer  = Marshal.AllocHGlobal(destBuffer.Length);

			numRead = Syscall.read(fd, buffer, numBytesToRead);
			Marshal.Copy(buffer, destBuffer, 0, (int)numRead);
			int ret = Syscall.posix_fadvise (fd, offset, offset+numRead, PosixFadviseAdvice.POSIX_FADV_SEQUENTIAL); // doubles readahead
			if(ret != 0)
				Logger.Append(Severity.DEBUG, "Could not call Fadvise(POSIX_FADV_SEQUENTIAL) to optimize file access for "+fileName+". Return code was "+ret);
			ret = Syscall.posix_fadvise (fd, offset, offset+numRead, PosixFadviseAdvice.POSIX_FADV_NOREUSE); // avoids to pollute OS cache
			if(ret != 0)
				Logger.Append(Severity.DEBUG, "Could not call Fadvise(POSIX_FADV_NOREUSE) to optimize file access for "+fileName+". Return code was "+ret);
			//if(numRead != count-offset)
			//	throw new Exception("LinuxStream : Error calling Read : expected to read "+(count-offset)+", but got "+numRead);

			return (int)numRead;
		}
		
		public override void Write(byte[] fromBuffer, int offset, int count){
			bool isok = true;
			long numWritten = 0;
			//int lastError = 0;
			if(offset > 0)
				Seek(offset, SeekOrigin.Begin);
			//fixed (byte* pBuffer = &buffer[0]){
				//isok = BackupWrite(fs.SafeFileHandle, pBuffer, (uint)buffer.Length, out numWritten, false, true, ref context);
				//lastError = Marshal.GetLastWin32Error();
				IntPtr buffer  = Marshal.AllocHGlobal(count);
				Marshal.Copy(fromBuffer, offset, buffer, count);
				//while(numWritten < count - offset){
				numWritten = Syscall.write(fd, buffer, (ulong)count);
				//}
				Syscall.fdatasync(fd);
				int ret = Syscall.posix_fadvise (fd, offset, offset+count, PosixFadviseAdvice.POSIX_FADV_NOREUSE); // avoids to pollute OS cache
				if(ret < 0)
					Logger.Append(Severity.DEBUG, "Could not call Fadvise(POSIX_FADV_NOREUSE) to optimize file access for "+fileName+". Return code was "+ret);
				if(numWritten != count)
					isok = false;
				Marshal.FreeHGlobal(buffer);
			//}
			if(!isok){
				throw new Exception("LinuxStream : Error calling Write : expected to write "+(count-offset)+", error: "+Syscall.GetLastError().ToString());
			}
			
		}
		
		public override long Seek(long offset, SeekOrigin origin){
			SeekFlags seekFlags = SeekFlags.SEEK_SET;
			if(origin == SeekOrigin.Current)
			    seekFlags = SeekFlags.SEEK_CUR;
			else if (origin == SeekOrigin.End)
				seekFlags = SeekFlags.SEEK_END;
				
			long seeked = Syscall.lseek(fd, offset, seekFlags);// == offset)
			//if(seeked == offset)
				currentPos = offset;
			//else
			//	throw new Exception("LinuxStream : unable to seek to position "+offset+" (got "+seeked+"): "+ Mono.Unix.Native.Stdlib.strerror (Mono.Unix.Native.Stdlib.GetLastError ()));
			return seeked;
		}
		
		public override void Close(){
			/*Syscall.fdatasync(fd); // ensure the data is flushed to disk*/
			// if stream represents a stored chunk, chattr+i it.
			Marshal.FreeHGlobal(buffer);
			Mono.Unix.Native.Syscall.close(fd);
		}
		
		[DllImport ("libc")]
	 	private static extern long lseek(int fdesc, long offset, int whence);
	
		}
}
#endif 