//# define DEBUG
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace Node.DataProcessing{
	
	/// <summary>
	/// This class takes the previously calculated checksum and, if not in the global client dedupe list, adds it and returns
	/// the date. If checksum already exists, it enters the heavy/slow path : calculate a second checksum of data, more robust
	/// and collsion-free than the previous quick calculation. If 
	/// </summary>
	internal class ClientDeduplicatorStream:IDataProcessorStream {

		private const int minDedupBlockSize = 16*1024; // don't dedup blocks < 16k
		private IDataProcessorStream inputStream;
		private long length;
		private long currentPos;
		private uint storageNodeId;
		private DedupIndex ddb;
 // total number of deduped blocks since stream creation
		//private LightDedupedBlock lastDedupedBlock;
		private List<long> dedupedBlocks;

		long dedupedBlockId; // latest deduped data block id

		//benchmarking
#if DEBUG
		Stopwatch sw = new Stopwatch();
#endif	
		public override List<IFileBlockMetadata> BlockMetadata{get;set;}
		
		public byte[] ChecksumToMatch {get;set;}

		public string CurrentChunkName {get;set;}

		public int CurrentChunkPos {get;set;}

		public override bool CanRead{
			get{ return true;}
		}
		
		public override bool CanWrite{
			get{ return false;}
		}
		
		/*public bool IsLastReadDeduped{
			get{return lastReadIsDeduped;}
		}*/
		
		public long DedupedCount {get;set;}
		
		public override bool CanSeek{
			get{return false;}
		}
		
		public override long Position{
			get{ return currentPos;}
			set{ Seek(value, SeekOrigin.Begin);}
		}
		
		public override long Length{
			get{ return length;}	
		}
		
		public override void SetLength(long value){
			length = value;
			inputStream.SetLength(value);
		}
		
		public override void Flush(){
			inputStream.Flush();
		}
		
		public override void FlushMetadata(){
			if(dedupedBlocks.Count>0){
				inputStream.BlockMetadata.Add(new ClientDedupedBlocks(dedupedBlocks));
				this.BlockMetadata = new List<IFileBlockMetadata>();//.Clear();
				this.dedupedBlocks = new List<long>();
			}
			inputStream.FlushMetadata();
		}
		/// <summary>
		/// Reads checksummed data and sees if it already exists.
		/// If yes, returns 0 bytes
		/// If no, checksum for current data is created and the method returns full data into destBuffer.
		/// For this first reason, read can return 0 although previous streams contains data ; this only means that data 
		/// 	has been deduplicated.
		/// </summary>
		/// <param name="destBuffer">
		/// A <see cref="System.Byte[]"/>
		/// </param>
		/// <param name="offset">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="count">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Int32"/>
		/// </returns>
		public override int Read(byte[] destBuffer, int offset, int count){
			//lastChecksum = md5.ComputeHash(destBuffer);
			throw new NotImplementedException("Restore direction not implemented.");
		}
		
		
		public override void Write(byte[] fromBuffer, int offset, int count){
			if(count-offset < minDedupBlockSize){ 
				inputStream.Write(fromBuffer, offset, count);
				length += count-offset;
				return;
			}
#if DEBUG
			sw.Start();
#endif	
			if(ddb.Contains(this.ChecksumToMatch, this.CurrentChunkName, this.CurrentChunkPos, count-offset, storageNodeId, ref dedupedBlockId)){
				this.DedupedCount++;
#if DEBUG
				sw.Stop();
				BenchmarkStats.Instance().DedupTime += sw.ElapsedMilliseconds;
				sw.Reset();
#endif
			}
			// not duplicated, let's write to underlying stream
			else{
				inputStream.Write(fromBuffer, offset, count);
				length += count-offset;
			}
			dedupedBlocks.Add(dedupedBlockId);
		}
		
		public override long Seek(long offset, SeekOrigin origin){
			inputStream.Seek(offset, origin);
			return offset;
		}
		
		public ClientDeduplicatorStream(IDataProcessorStream inputStream, uint storageNode, DedupIndex ddb){
			this.inputStream = inputStream;
			this.currentPos = 0;
			this.length = 0;
			this.DedupedCount = 0;
			storageNodeId = storageNode;
			this.BlockMetadata = new List<IFileBlockMetadata>();
			dedupedBlocks = new List<long>();
			this.ddb = ddb;
			
		}
	}
}

