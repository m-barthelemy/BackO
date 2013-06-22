using System;
using System.IO;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Node.DataProcessing{
	
	/// <summary>
	/// This class has no real usage except for testing and benchmarking data processing performance, 
	/// 	without the potential network and/or IP stack bottlenecks
	/// 
	/// </summary>
	public class DummyStream:IDataProcessorStream{
		
		private long length;
		private long currentPos;
		
		
		public override List<IFileBlockMetadata> BlockMetadata{get;set;}
		
		public DummyStream(){
			
			length = 0;
			this.BlockMetadata = new List<IFileBlockMetadata>();
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
			get{return true;}
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
		
		public override void FlushMetadata(){
			
		}
	
		public override int Read(byte[] destBuffer, int offset, int count){
			return count;
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

