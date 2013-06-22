using System;
using System.Diagnostics;
using System.IO;
using LZ4Stream.Streams;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Node.DataProcessing{
	
	/// <summary>
	/// Thin wrapper around GZipStream
	/// 
	/// </summary>
	public class LZ4Decompressor:IDataProcessorStream{
		
		private long currentPos;
		private IDataProcessorStream innerStream;
		private LZ4Stream.LZ4Stream lz;
		private int read;
#if DEBUG
		Stopwatch sw = new Stopwatch();
#endif	
		public override List<IFileBlockMetadata> BlockMetadata{get;set;}
		
		public LZ4Decompressor (IDataProcessorStream myInnerStream){
			this.innerStream = myInnerStream;
			this.BlockMetadata = new List<IFileBlockMetadata>();
			lz = new LZ4Stream.LZ4Stream(myInnerStream, false, true, 1*1024*1024/*1MB buffer*/);
		}
		
		public override bool CanRead{
			get{ return lz.CanRead;}
		}
		
		public override bool CanWrite{
			get{ return lz.CanWrite;}
		}
		
		public override bool CanSeek{
			get{
				return lz.CanSeek;
			}
		}
		
		public override long Position{
			get{ return currentPos;}
			set{ currentPos = value; Seek(value, SeekOrigin.Begin);}
		}
		
		public override long Length{
			get{ return innerStream.Length;	/*return length;*/}	
		}
		
		public override void SetLength(long value){
			innerStream.SetLength(value);
		}
		
		public override void Flush(){
			innerStream.Flush();
		}
		
		public override void FlushMetadata(){
			innerStream.BlockMetadata.AddRange(this.BlockMetadata);
			this.BlockMetadata.Clear();
			innerStream.FlushMetadata();
		}
		
		public override int Read(byte[] destBuffer, int offset, int count){
			Console.WriteLine ("LZ4Decompressor : asked to read() "+count+" from offset "+offset+" from innerstream "+innerStream.GetType());
			//if(innerStream.Length ==0) return 0; // if called with empty files, LZ4 throws error when not finding its 'magic number' header
			/*int read = innerStream.Read(destBuffer, offset, count);
			Console.WriteLine("NullSinkStream() : read"+read+" into buffer with size "+destBuffer.Length+".  offset="+offset+", length="+count);
			length += read;
			return read;*/
			read = lz.Read(destBuffer, offset, count);
			currentPos += read;
			return read;
		}
		
		
		public override void Write(byte[] fromBuffer, int offset, int count){

			
		}
		
		public override long Seek(long offset, SeekOrigin origin){
			return innerStream.Seek(offset, origin);
		}
		
		public override void Close(){
			lz.Close();
			innerStream.Close();
		}
	}
}



