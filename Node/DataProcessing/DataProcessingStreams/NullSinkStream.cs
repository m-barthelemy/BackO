using System;
using System.IO;
using System.Collections.Generic;
using System.Net.Sockets;
#if DEBUG
using System.Diagnostics;
#endif	

namespace Node.DataProcessing{
	
	/// <summary>
	/// This class is to be used as the final pipeline stream, just above the data receiver stream (storage node networkstream)
	/// 
	/// </summary>
	public class NullSinkStream:IDataProcessorStream{
		
		private long length;
		private long currentPos;
		private Stream innerStream;
		private PipelineMode operationMode;
		//byte[] buffer = new byte[512*1024]; // store data until it reached 512k
		private int bufferPos = 0;
#if DEBUG
		Stopwatch sw = new Stopwatch();
#endif			
		public override List<IFileBlockMetadata> BlockMetadata{get;set;}
		
		public NullSinkStream(Stream myInnerStream, PipelineMode mode){
			operationMode = mode;

			this.innerStream = myInnerStream;
			length = 0;
			this.BlockMetadata = new List<IFileBlockMetadata>();
		}
		
		public override bool CanRead{
			get{ return true;}
		}
		
		public override bool CanWrite{
			get{ return true;}
		}
		
		public override bool CanSeek{
			get{return true;}
		}
		
		public override long Position{
			get{ return currentPos;}
			set{ currentPos = value; Seek(value, SeekOrigin.Begin);}
		}
		
		public override long Length{
			get{ 
				/*if(operationMode == PipelineMode.Write)
					return length;
				else
					return innerStream.Length;*/
				return length;
			}	
		}
		
		public override void SetLength(long value){
			length = value;
		}
		
		public override void Flush(){
			//RealWrite ();
			innerStream.Flush();
			//Console.WriteLine ("nullsinkstream : flush requested");
			
		}
		
		public override void FlushMetadata(){
			//this.BlockMetadata = new List<IFileBlockMetadata>();
			//Console.WriteLine ("NullsinkStream has "+this.BlockMetadata.Count+" block metadata entries");
		}
	
		public override int Read(byte[] destBuffer, int offset, int count){
			Console.WriteLine ("NullSinkStream : asked to read() "+count+" from offset "+offset+" from innerstream "+innerStream.GetType());
			int read = innerStream.Read(destBuffer, offset, count);
			//Console.WriteLine("NullSinkStream() : read"+read+" into buffer with size "+destBuffer.Length+".  offset="+offset+", length="+count);
			length += read;
			currentPos += read;
			return read;
		}
		
		
		public override void Write(byte[] fromBuffer, int offset, int count){
			//Console.WriteLine("nullsink : write "+(count-offset));
#if DEBUG
			sw.Start();
#endif			
			/*
			if(count >0){
				//Console.WriteLine("Write() : begin "+DateTime.Now);
				if( (count - offset) > (512*1024 - bufferPos)){
					RealWrite();
				}
				Array.Copy(fromBuffer, offset, buffer, bufferPos, count - offset);
				bufferPos += (count - offset);
				//innerStream.Write(fromBuffer, offset, count);
				
			}
			length += count-offset;*/
			innerStream.Write(fromBuffer, offset, count);
			length += (count - offset);
#if DEBUG
			sw.Stop();
			BenchmarkStats.Instance().SendTime += sw.ElapsedMilliseconds;
			sw.Reset();
#endif		
			
			
		}
					
		/*private void RealWrite(){
			innerStream.Write(buffer, 0, bufferPos);
			//Console.WriteLine("Written "+bufferPos);
			bufferPos = 0;
		}*/
		
		public override long Seek(long offset, SeekOrigin origin){
			currentPos = innerStream.Seek(offset, origin);
			return currentPos;
		}
		
		public override void Close(){
			if(operationMode == PipelineMode.Write && bufferPos > 0)
				Flush();
			innerStream.Close();
		}
	}
}

