using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Node.DataProcessing{
	
	/// <summary>
	/// Thin wrapper around GZipStream
	/// 
	/// </summary>
	public class GZCompressorStream:IDataProcessorStream{
		
		private long length;
		private long currentPos;
		private IDataProcessorStream innerStream;
		private GZipStream gz;
		private int read;
#if DEBUG
		Stopwatch sw = new Stopwatch();
#endif	
		public override List<IFileBlockMetadata> BlockMetadata{get;set;}
		
		public GZCompressorStream (IDataProcessorStream myInnerStream, CompressionMode mode){
			this.innerStream = myInnerStream;
			length = 0;
			this.BlockMetadata = new List<IFileBlockMetadata>();
			gz = new GZipStream(myInnerStream, mode, true);
			
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
			get{
				return gz.CanSeek;
			}
		}
		
		public override long Position{
			get{ return currentPos;}
			set{ currentPos = value; Seek(value, SeekOrigin.Begin);}
		}
		
		public override long Length{
			get{ 
				return innerStream.Length;
				//return length;
			}	
		}
		
		public override void SetLength(long value){
			length = value;
		}
		
		public override void Flush(){
			//gz.Flush(); // TODO : BUGGY!!! we should really call flush here.
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
			read = gz.Read(destBuffer, offset, count);
			currentPos += read;
			return read;
		}
		
		
		
		public override void Write(byte[] fromBuffer, int offset, int count){
			if(count <1) return;
			//	innerStream.Write(fromBuffer, offset, count);
#if DEBUG
			sw.Start();
#endif
			gz.Write(fromBuffer, offset, count);
			
			//length += count-offset;
#if DEBUG
			sw.Stop();
			BenchmarkStats.Instance().CompressTime += sw.ElapsedMilliseconds;
			sw.Reset();
#endif
			
		}
		
		public override long Seek(long offset, SeekOrigin origin){
			
			return offset;
		}
		
		public override void Close(){
			gz.Close();
			innerStream.Close();
		}
	}
}

