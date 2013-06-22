using System;
using System.IO;


namespace VDDK {

	public class VmdkStream :Stream{

		long currentPos;
		long length;
		IntPtr diskPtr;

		public VmdkStream (IntPtr diskPtr ){
			if(diskPtr == IntPtr.Zero)
				throw new Exception("VMDK disk handle is null");
			this.diskPtr = diskPtr;

		}

		public override bool CanRead{
			get{ return true;}
		}
		
		public override bool CanWrite{
			get{ 
				return true;
			}
		}
		
		public override bool CanSeek{
			get{return false;}
		}
		
		public override long Position{
			get{ return currentPos;}
			set{ currentPos = value; Seek(value, SeekOrigin.Begin);}
		}
		
		public override long Length{
			get{ 
				return length;
			}	
		}
		
		public override void SetLength(long value){
			length = value;
		}
		
		public override void Flush(){
			
		}
		

	
		public override int Read(byte[] destBuffer, int offset, int count){
			if(offset % 512 != 0 || count % 512 != 0)
				throw new Exception("Offset and Count must be multiples of 512");
			if(destBuffer.Length < count-offset)
				throw new Exception("Buffer too small! must be >="+(count-offset)+", but is "+destBuffer.Length);
			VixError error = VixDiskLib.Read(diskPtr, (ulong)offset/512, (ulong)count/512, destBuffer);

			if(error == VixError.VIX_OK)
				return count;
			else if(error == VixError.VIX_E_DISK_OUTOFRANGE)
				return 0;
			else
				throw new Exception("Error reading VM disk : "+error.ToString());
		}
		
		
		public override void Write(byte[] fromBuffer, int offset, int count){
			//Console.WriteLine("nullsink : write "+(count-offset));
			
			length += count-offset;
			
		}
		
		public override long Seek(long offset, SeekOrigin origin){
			
			return currentPos;
		}
		
		public override void Close(){
			
		}


	}
}

