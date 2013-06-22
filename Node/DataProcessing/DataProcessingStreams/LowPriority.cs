using System;
using System.IO;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Node.DataProcessing{
	
	/// <summary>
	/// This stream doesn nothing except sleeping 1ms after each Write().
	/// It it used for backups whose priority is 'Low', to save system resources and slow down overall processing
	/// </summary>
	public class LowPriorityStream:IDataProcessorStream{
		
		private IDataProcessorStream innerStream;

		public override List<IFileBlockMetadata> BlockMetadata{get;set;}
		
		public LowPriorityStream(IDataProcessorStream innerStream){
			
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
			get{ return innerStream.Position;}
			set{ innerStream.Position = value;}
		}
		
		public override long Length{
			get{ 
				return innerStream.Length;
			}	
		}
		
		public override void SetLength(long value){
			innerStream.SetLength(value);
		}
		
		public override void Flush(){
			
		}
		
		public override void FlushMetadata(){
			
		}
		
		public override int Read(byte[] destBuffer, int offset, int count){
			return innerStream.Read(destBuffer, offset, count);
		}
		
		
		public override void Write(byte[] fromBuffer, int offset, int count){
			System.Threading.Thread.Sleep(1);
			innerStream.Write(fromBuffer, offset, count);
			
		}
		
		public override long Seek(long offset, SeekOrigin origin){
			return innerStream.Seek(offset, origin);;
		}
		
		public override void Close(){
			innerStream.Close();
		}
	}
}

