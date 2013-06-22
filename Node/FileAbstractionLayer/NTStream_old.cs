using System;
using System.IO;
using System.Runtime.InteropServices;
using System.ComponentModel;
using Microsoft.Win32.SafeHandles;
using Node.DataProcessing;
using Node.Utilities.Native;

namespace Node{
	
	/// <summary>
	///  This class allows to handle Windows NT BackupRead() using Stream semantics
	/// </summary>
	public class NTStream:Stream{
		
		private long currentPos;
		private long length;
		private string fileName;
		private FileMode fileMode;
		//private IntPtr fs;
		private IntPtr m_volumeHandle;
		//private IntPtr buffer = Marshal.AllocHGlobal(1024*1024); // 1Mb buffer size
		IntPtr context;
		byte[] buffer = new byte[1024*1024];
		
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
		
		public NTStream(string fName, FileMode fileMode){
			this.fileName = fName;	
			this.currentPos = 0;
			this.length = 0;
			this.fileMode = fileMode;
			context = IntPtr.Zero;
			//fs = new FileStream(fileName, fileMode);
			//fs = CreateFile(this.fileName, GENERIC_READ, SHARE_ALLOWREAD, IntPtr.Zero, 3, FILE_FLAG_BACKUP_SEMANTICS|FILE_FLAG_SEQUENTIAL_SCAN , IntPtr.Zero);
			m_volumeHandle = Win32Api.CreateFile(
						fileName, 
						Win32Api.GENERIC_READ,
						Win32Api.FILE_SHARE_READ|Win32Api.FILE_SHARE_WRITE,
						IntPtr.Zero, 
						Win32Api.OPEN_EXISTING, 
						Win32Api.FILE_FLAG_BACKUP_SEMANTICS, // todo : | sequentialscan 
						IntPtr.Zero
			);
			//if(fs == IntPtr.Zero)
			if(m_volumeHandle == IntPtr.Zero)
				throw new Exception("Couldn't open file "+fName+": "+(new Win32Exception(Marshal.GetLastWin32Error()).Message));
		}
	
	
		unsafe public override int Read(byte[] buffer, int offset, int count){
			uint numRead = 0;
			int lastError = 0;
			bool isok = false;
			if(offset > 0)
				Seek(offset, SeekOrigin.Begin);
			
			
			//fixed (byte* pbuffer = &buffer[0]){
				IntPtr pbuffer  = Marshal.AllocHGlobal(buffer.Length);
				//Console.WriteLine("NTStream() : before backupread() "+fileName);
				isok = BackupRead(m_volumeHandle, pbuffer, (uint)buffer.Length, out numRead, false, true, ref context);
				Marshal.Copy(pbuffer, buffer, 0, (int)numRead);
				Marshal.FreeHGlobal(pbuffer);
				//Console.WriteLine("NTStream() : after backupread() "+fileName+", read "+numRead);
				/*if(numRead == 0){ // end of file, disposing 
					Console.WriteLine("NTStream() : before ending backupread");
					BackupRead(m_volumeHandle, pBuffer, (uint)buffer.Length, out numRead, true, true, ref context);
					Console.WriteLine("NTStream() : after ending backupread");
				}*/
			//}
			if(!isok){
				//Console.WriteLine("NTStream() : error backupread() ");
				//lastError = Marshal.GetLastWin32Error();
				throw new Exception("NTStream : Error calling BackupRead : "+(new Win32Exception(Marshal.GetLastWin32Error()).Message));
			}
			
			return (int)numRead;
		}
		
		unsafe public override void Write(byte[] buffer, int offset, int count){
			bool isok = false;
			uint numWritten = 0;
			int lastError = 0;
			if(offset > 0)
				Seek(offset, SeekOrigin.Begin);
			fixed (byte* pBuffer = &buffer[0]){
				//isok = BackupWrite(fs, pBuffer, (uint)buffer.Length, out numWritten, false, true, ref context);
				lastError = Marshal.GetLastWin32Error();
			}
			if(!isok){
				throw new Exception("NTStream : Error calling BackupWrite : "+(new Win32Exception(Marshal.GetLastWin32Error()).Message));
			}
			
		}
		
		public override long Seek(long offset, SeekOrigin origin){
			if(offset == 0) return 0;
			//Console.WriteLine("NTStream() : before BackupSeek() "+fileName);
			var offsetNumber = new Number{Base=(int)offset};
			uint lowOffset = 0, highOffset = 0;
			int preRead = Read(new byte[1], 0, 1);
			Console.WriteLine("NTStream() : item '"+fileName+"' before BackupSeek(), asked to seek "+offset+", read "+preRead);
			BackupSeek(m_volumeHandle, offsetNumber.Low, offsetNumber.High, out lowOffset, out highOffset, ref context);
			Console.WriteLine ("Seeked : low="+lowOffset+", high="+highOffset);
			return (long)lowOffset;
		}
		
		public override void Close(){
			Win32Api.CloseHandle(m_volumeHandle);
			
		}
		
		[StructLayout(LayoutKind.Explicit)]
        struct Number{

            [FieldOffset(0)]
            public int Base;

            [FieldOffset(0)]
            public uint Low;

            [FieldOffset(2)]
            public uint High;
        }
		
		// P/Invoke calls to Kernel
		/*private const int BUFFER_SIZE = 4096;
		private const uint GENERIC_READ = 0x80000000;
		private const uint GENERIC_WRITE = 0x40000000;
		private const uint OPEN_EXISTING = 3;
		private const uint CREATE_ALWAYS = 2;
		private const uint SHARE_ALLOWREAD =  0x00000001;
		private const uint WRITE_DAC = 0x00040000;
		private const uint WRITE_OWNER = 0x00080000;
		private const uint ACCESS_SYSTEM_SECURITY = 0x01000000;
		private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
		private const uint FILE_FLAG_SEQUENTIAL_SCAN = 0x08000000;
		private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern SafeFileHandle CreateFile(string path,
											uint desiredAccess,
											uint shareMode,
											IntPtr securityAttributes,
											uint creationDisposition,
											uint flagsAndAttributes,
											IntPtr templateHandle);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool CloseHandle(IntPtr handle);*/

		
		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		unsafe private static extern bool BackupRead(
									    IntPtr handle, 
		                                //SafeFileHandle handle,
		                                //byte* buffer,
		                                IntPtr buffer,
									    uint bytesToRead, 
		                                out uint bytesRead,
									    [MarshalAs(UnmanagedType.Bool)] bool abort,
									    [MarshalAs(UnmanagedType.Bool)] bool processSecurity,
									    ref IntPtr context);
		
		
		[DllImport("kernel32.dll", SetLastError = true)]
		unsafe private static extern bool BackupSeek(
		                                IntPtr handle,
		                                //SafeFileHandle handle,             
										uint dwLowBytesToSeek,
										uint dwHighBytesToSeek,
										out uint lpdwLowByteSeeked,
										out uint lpdwHighByteSeeked,
										ref IntPtr context);
		
		[DllImport("kernel32.dll", SetLastError = true)]
		unsafe private static extern bool BackupWrite(IntPtr handle,
											byte* buffer,
											uint bytesToWrite,
											out uint bytesWritten,
											[MarshalAs(UnmanagedType.Bool)] bool abort,
											[MarshalAs(UnmanagedType.Bool)] bool processSecurity,
											ref IntPtr context);
		
		
		public enum StreamType{
			Data = 1, ExternalData = 2, SecurityData = 3, 
			AlternateData = 4, Link = 5, PropertyData = 6, 
			ObjectID = 7, ReparseData = 8, SparseDock = 9
		}
		
		[StructLayout(LayoutKind.Sequential, Size=20)]
		public struct Win32StreamID	{
			  public StreamType dwStreamId;
			  public int dwStreamAttributes;
			  public long Size;
			  public int dwStreamNameSize;
		  	  //WCHAR cStreamName[1]; 
		}
	}
}

