//# define DEBUG
using System;
using System.IO;
using System.Text;
#if DEBUG
using System.Diagnostics;
#endif	
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Node.DataProcessing{
	
	/// <summary>
	/// This class computes an MD5 checksum for each block of data read
	/// </summary>
	internal class ChecksummerStream: IDataProcessorStream{

		private const int minBlockSize = 16*1024; // don't checksum blocks < 16k
		private long length;
		private long currentPos;
		private byte[] lastChecksum = new byte[20];
		private ClientDeduplicatorStream outputStream;
		private MD5 hasher;
#if DEBUG
		Stopwatch sw = new Stopwatch();
#endif	
		
		public byte[] LastChecksum {
			get {
				return this.lastChecksum;
			}
		}
		
		public override List<IFileBlockMetadata> BlockMetadata{get;set;}
			
		
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
			outputStream.SetLength(length);
		}
		
		public override void Flush(){
			outputStream.Flush();
		}
		
		public override void FlushMetadata(){
			outputStream.BlockMetadata.AddRange(this.BlockMetadata);
			this.BlockMetadata.Clear();
			outputStream.FlushMetadata();
		}
		
		public ChecksummerStream(ClientDeduplicatorStream inputStream){
			this.outputStream = inputStream;
			this.currentPos = 0;
			this.length = 0;
			hasher = MD5CryptoServiceProvider.Create();
			this.BlockMetadata = new List<IFileBlockMetadata>();

			
		}
	
		public override int Read(byte[] destBuffer, int offset, int count){
			//lastChecksum = md5.ComputeHash(destBuffer);
			throw new NotImplementedException("restore direction not implemented for checksumming");
		}

		/*private string ByteArrayToString(byte [] toConvert){
			StringBuilder sb = new StringBuilder(toConvert.Length);
			for (int i = 0; i < toConvert.Length - 1; i++){
				sb.Append(toConvert[i].ToString("X"));
			}
			return sb.ToString();
		}*/
		
		
		public override void Write(byte[] fromBuffer, int offset, int count){
			if( (count - offset) >= minBlockSize){

#if DEBUG
				sw.Start();
#endif
				Array.Copy(hasher.ComputeHash(fromBuffer, offset, count), lastChecksum, 16);
				Array.Copy (BitConverter.GetBytes(count-offset), 0, lastChecksum, 16, 4);
#if DEBUG
				sw.Stop();
				BenchmarkStats.Instance().ChecksumTime += sw.ElapsedMilliseconds;
				sw.Reset();
#endif
				outputStream.ChecksumToMatch = lastChecksum; 
			}
			outputStream.Write(fromBuffer, offset, count);
			length += count;
		}
		
		public override long Seek(long offset, SeekOrigin origin){
			outputStream.Seek(offset, origin);
			return offset;
		}
		
		
	}
}

