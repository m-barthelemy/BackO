using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Node.DataProcessing.Utils;

namespace Node.DataProcessing{
	
	public enum CompressorAlgorithm{Lzo,Bzip2}
	
	public class LzoCompressorStream:IDataProcessorStream{
		// don't compress very small blocks as there is really little gain to expect
		private const int minBlockSize = 1024*512; 
		private long length;
		private long currentPos;
		//private int compressorBufferSize; // to save into index, to allow decompression...
		private byte[] tempBuffer;
		private int internalOffset;
		private int compressedSize;
#if DEBUG
		Stopwatch sw = new Stopwatch();
#endif			
		public override List<IFileBlockMetadata> BlockMetadata{get;set;}
		
		public int CompressedSize {
			get {
				return this.compressedSize;
			}
		}

		private IDataProcessorStream inputStream;
		// gatherBuffer will accumulate data until it reaches blocksize
		private byte[] gatherBuffer;
		private int currentGatherPos;
		/*public int CompressorBufferSize{
			get{return compressorBufferSize;}
			
		}*/

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
			set{ 
				Seek(value, SeekOrigin.Begin);
				currentPos = value;
			}
		}
		
		public override long Length{
			get{ return length;}	
		}
		
		public override void SetLength(long value){
			length = value;
			inputStream.SetLength(length);
		}
		
		public override void Flush(){
			//resize array to real data size
			// TODO !! allow quicklz.compress to take offset and length, to avoid resizes...
			Array.Resize(ref gatherBuffer, currentGatherPos);
			DoCompressAndWrite();
			inputStream.Flush();
			gatherBuffer = new byte[minBlockSize];
			currentGatherPos = 0;
		}
		
		public override void FlushMetadata(){
			inputStream.BlockMetadata.AddRange(this.BlockMetadata);
			this.BlockMetadata.Clear();
			inputStream.FlushMetadata();
		}
		
		public override int Read(byte[] destBuffer, int offset, int count){
			//lastChecksum = md5.ComputeHash(destBuffer);
			//if(count == 0) return 0;
			tempBuffer = new byte[count];
			//internalOffset = offset;
			/*while(internalOffset < compressorBufferSize || (read = inputStream.Read(tempBuffer, offset+internalOffset, count-read)) > 0){
				internalOffset += read;
			}*/
			int read = inputStream.Read(tempBuffer, offset, count);
			//if(read < minBlockSize){
				
			//}
			//else
				destBuffer = QuickLZ.Decompress(tempBuffer);
			//QuickLZ.
			//if(read == 0) return 0;
			//Console.WriteLine("CompressorStream : read "+read+", compressed to "+destBuffer.Length);
			return QuickLZ.sizeDecompressed(tempBuffer);
		}
		
		public unsafe override void Write(byte[] fromBuffer, int offset, int count){
			//Console.WriteLine ("called Write(), offset="+offset+", count="+count+", gatherpos="+currentGatherPos);
#if DEBUG
			sw.Start();
#endif
			// we do calculations in order to pack data by minblocksize sized arrays
			// this increases performance by calling compress once for potentially multiple small/deduped blocks
			// additionallt we get better compression ratios when compressing longer blocks
			int realDataToCopy = count-offset;
			if(realDataToCopy + currentGatherPos == minBlockSize){ // perfect case, let's directly compress
				//Console.WriteLine("data : perfect");
				gatherBuffer = fromBuffer;
				DoCompressAndWrite();	
				currentGatherPos = 0;
				return;
			}			
			if(realDataToCopy <= minBlockSize){
				//try{
				if(realDataToCopy + currentGatherPos > minBlockSize){
					/*
					 * Array sourceArray,	int sourceIndex,	Array destinationArray,		int destinationIndex,	int length
					*/
					//Console.WriteLine("data : split ("+realDataToCopy+"). gather buffer size="+currentGatherPos+", remzining space="+(minBlockSize - currentGatherPos));
					Array.Copy(fromBuffer, offset, gatherBuffer, currentGatherPos, minBlockSize - currentGatherPos);
					DoCompressAndWrite();
					
					int remainingData = realDataToCopy - (minBlockSize - currentGatherPos);
					//Console.WriteLine("remaining="+remainingData);
					currentGatherPos = 0;
					//Console.WriteLine("copying "+remainingData+" bytes to gatherbuffer
					Array.Copy(fromBuffer, realDataToCopy - (remainingData)/*+1*/, gatherBuffer, 0, remainingData/*+1*/);
					currentGatherPos = remainingData;
					//Console.WriteLine("data : added ("+remainingData+"). gather buffer size="+currentGatherPos);
				}
				else{
					try{
					Array.Copy(fromBuffer, offset, gatherBuffer, currentGatherPos, realDataToCopy);
					currentGatherPos += realDataToCopy/*+1*/;
					}
					catch(Exception){
						Console.WriteLine(" @@@@@@@@@@ orig buffer="+fromBuffer.Length+", offset="+offset+", current buf size="+currentGatherPos+", realdatatocopy="+realDataToCopy);	
					}
					//Console.WriteLine("data : insufficient ("+realDataToCopy+"). gather buffer size="+currentGatherPos);
				}
				/*}
				catch(Exception e){
					Console.WriteLine("error "+e.Message+"-----"+e.StackTrace);
					throw;
				}*/
					
			}
			else{ //count > minBlockSize, don't handle this case as we are supposed to coordinate between provided data and ProcessorStreams blocksize
				Console.WriteLine("data : too much ("+realDataToCopy+"/"+minBlockSize+")");
				throw new NotSupportedException("Cannot compress data block bigger than "+minBlockSize+", but got a "+count+" length data block.");
				
			}
#if DEBUG
			sw.Stop();
			BenchmarkStats.Instance().CompressTime += sw.ElapsedMilliseconds;
			sw.Reset();
#endif
		}
		
		private void DoCompressAndWrite(){

			byte[] compressedData = QuickLZ.Compress(gatherBuffer, 3);
			length += compressedData.Length;
			//Console.WriteLine ("send, gather buffer="+gatherBuffer.Length+", gatherpos="+currentGatherPos+", compressed block="+compressedData.Length);
			inputStream.Write(compressedData, 0, compressedData.Length);
			//Console.WriteLine ("sent.");
			//currentGatherPos = 0;
		}
		
		/*public override void Write(byte[] fromBuffer, int offset, int count){
			//throw new NotSupportedException("ChecksummerStream doesn't support writing");
			if(count < minBlockSize){ // don't compress, write data as-is
				inputStream.Write(fromBuffer, offset, count);
				compressedSize = count;
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
			
			//TODO : microoptimization : don"t copy buffer if size < blocksize ; make compress() handle offset and count parameters instead
			byte[] compressedData = QuickLZ.Compress(tempData, 3);
			
			//Console.WriteLine("CompressorStream : got "+count+", compressed to "+compressedData.Length);
			compressedSize = compressedData.Length;
			length += compressedSize;
#if DEBUG
				sw.Stop();
				BenchmarkStats.Instance().CompressTime += sw.ElapsedMilliseconds;
				sw.Reset();
#endif
			inputStream.Write(compressedData, 0, compressedSize);
			
		}*/
		
		public override long Seek(long offset, SeekOrigin origin){
			Console.WriteLine ("called seek()");
			inputStream.Seek(offset, origin);
			return offset;
		}
		
		public LzoCompressorStream (IDataProcessorStream inputStream, CompressorAlgorithm algorithm, int bufferSize){
			this.inputStream = inputStream;
			this.currentPos = 0;
			this.length = 0;
			//this.compressorBufferSize = bufferSize; //1k buffer ; compression won't work well below 1k
			//tempBuffer = new byte[compressorBufferSize];
			internalOffset = 0;
			compressedSize = 0;
			gatherBuffer = new byte[minBlockSize];
			currentGatherPos = 0;
			this.BlockMetadata = new List<IFileBlockMetadata>();
		}
		
		public override void Close(){
			inputStream.Close();
		}
	}
}

