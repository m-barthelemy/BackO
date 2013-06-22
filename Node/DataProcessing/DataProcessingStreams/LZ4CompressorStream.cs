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
	public class LZ4CompressorStream:IDataProcessorStream{
		
		private long currentPos;
		private IDataProcessorStream innerStream;
		private LZ4Stream.LZ4Stream lz;
		private int read;
#if DEBUG
		Stopwatch sw = new Stopwatch();
#endif	
		public override List<IFileBlockMetadata> BlockMetadata{get;set;}
		
		public LZ4CompressorStream (IDataProcessorStream myInnerStream){
			this.innerStream = myInnerStream;
			this.BlockMetadata = new List<IFileBlockMetadata>();
			lz = new LZ4Stream.LZ4Stream(myInnerStream, true, true, 1*1024*1024/*1MB buffer*/);
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
			/*int read = innerStream.Read(destBuffer, offset, count);
			Console.WriteLine("NullSinkStream() : read"+read+" into buffer with size "+destBuffer.Length+".  offset="+offset+", length="+count);
			length += read;
			return read;*/
			read = lz.Read(destBuffer, offset, count);
			currentPos += read;
			return read;
		}

		
		public override void Write(byte[] fromBuffer, int offset, int count){
			if(count ==0) return;
#if DEBUG
			sw.Start();
#endif
			lz.Write(fromBuffer, offset, count);
			
#if DEBUG
			sw.Stop();
			BenchmarkStats.Instance().CompressTime += sw.ElapsedMilliseconds;
			sw.Reset();
#endif
			
		}
		
		public override long Seek(long offset, SeekOrigin origin){
			return innerStream.Seek(offset, origin);
		}
		
		public override void Close(){
			Flush ();
			lz.Close();
			innerStream.Close();
		}
	}
}



