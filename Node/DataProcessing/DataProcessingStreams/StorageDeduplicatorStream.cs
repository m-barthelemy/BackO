using System;
using System.IO;

namespace Node.DataProcessing{
	
	public class StorageDeduplicatorStream: Stream{
		
		private const int minDedupBlockSize = 128*1024;
		private Stream inputStream;
		private long length;
		private long currentPos;
		private bool lastReadIsDeduped;
		private string currentChunkName;
		private int currentChunkPos;
		private long dedupedCount;

 // total number of deduped blocks since stream creation
		private LightDedupedBlock lastDedupedBlock;
		private byte[] checksumToMatch;
		
		public byte[] ChecksumToMatch {
			get {
				return this.checksumToMatch;
			}
			set {
				checksumToMatch = value;
			}
		}

		public LightDedupedBlock LastDedupedBlock {
			get {
				return this.lastDedupedBlock;
			}
		}
		
		public string CurrentChunkName {
			get {
				return this.currentChunkName;
			}
			set {
				currentChunkName = value;
			}
		}

		public int CurrentChunkPos {
			get {
				return this.currentChunkPos;
			}
			set {
				currentChunkPos = value;
			}
		}		

		public override bool CanRead{
			get{ return true;}
		}
		
		public override bool CanWrite{
			get{ 
				return false;
			}
		}
		
		public bool IsLastReadDeduped{
			get{return lastReadIsDeduped;}
		}
		
		public long DedupedCount {
			get {
				return this.dedupedCount;
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
			get{ return length;}	
		}
		
		public override void SetLength(long value){
			length = value;
		}
		
		public override void Flush(){
			
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
			if(count < minDedupBlockSize){ 
				lastReadIsDeduped = false;
				lastDedupedBlock = null;
				inputStream.Write(fromBuffer, offset, count);
				length += count;
				return;
			}
			byte[] tempData;
			if(count != fromBuffer.Length){
				tempData = new byte[count];
				for(int i=0; i<count; i++)
					tempData[i] = fromBuffer[offset+i];
			}
			else
				tempData = fromBuffer;
			/*if(DedupIndex.Instance().Contains(checksumToMatch, this.currentChunkName, this.currentChunkPos, (uint)tempData.Length, 1)){
				lastDedupedBlock = DedupIndex.Instance().LastDedupedBlock;
				//Console.WriteLine("ClientDeduplicatorStream: deduped block(size="+tempData.Length+",sum="+Convert.ToBase64String(checksumToMatch)+")");
				lastReadIsDeduped = true;
				dedupedCount++;
				return;
			}*/
			// not duplicated, let's write to underlying stream
			lastDedupedBlock = null;
			lastReadIsDeduped = false;
			inputStream.Write(tempData, 0, tempData.Length);
			length += count;
		}
		
		public override long Seek(long offset, SeekOrigin origin){
			inputStream.Seek(offset, origin);
			return offset;
		}
		
		public StorageDeduplicatorStream(Stream inputStream){
			this.inputStream = inputStream;
			this.currentPos = 0;
			this.length = 0;
			this.dedupedCount = 0;
		}
		
		
	}
}

