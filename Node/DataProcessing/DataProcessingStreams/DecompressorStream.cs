using System;
using System.IO;
using Node.DataProcessing.Utils;

namespace Node.DataProcessing{
	
	
	public class DecompressorStream:Stream{
		
		private long length;
		private long currentPos;
		private Stream inputStream;
		
		

		public override bool CanRead{
			get{ return true;}
		}
		
		public override bool CanWrite{
			get{ 
				return false;
			}
		}
		
		public override bool CanSeek{
			get{return false;}
		}
		
		public override long Position{
			get{ return currentPos;}
			set{ Seek(value, SeekOrigin.Begin);}
		}
		
		public override long Length{
			get{ return Length;}	
		}
		
		public override void SetLength(long value){
			length = value;
		}
		
		public override void Flush(){
			
		}
		
	
		public override int Read(byte[] destBuffer, int offset, int count){
			//lastChecksum = md5.ComputeHash(destBuffer);
			byte[] tempBuffer = new byte[destBuffer.Length];
			int read = inputStream.Read(tempBuffer, offset, count);
			if(read == 0) return 0;
			
			return read;
		}
		
		
		
		public override void Write(byte[] fromBuffer, int offset, int count){
			throw new NotSupportedException("ChecksummerStream doesn't support writing");
			
		}
		
		public override long Seek(long offset, SeekOrigin origin){
			
			return offset;
		}
		
		public DecompressorStream (Stream inputStream, CompressorAlgorithm algorithm){
			this.inputStream = inputStream;
			this.currentPos = 0;
			this.length = 0;
		}
	}
}

