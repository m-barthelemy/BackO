//# define DEBUG
using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Security.Cryptography;
using Crimson.Security.Cryptography;

namespace Node.DataProcessing{
	
	/// <summary>
	/// This class provides an MD5 checksum for each block of data read.
	///  !!!!! This implementation makes native calls to the mhash library, this it is a dependance that must be installed.
	/// </summary>
	public class ChecksummerStream_MHash:/*Stream,*/ IDataProcessorStream{
		
		private long length;
		private long currentPos;
		private byte[] lastChecksum;
		private ClientDeduplicatorStream inputStream;
		private MD5 md5;
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
			inputStream.SetLength(length);
		}
		
		public override void Flush(){
			inputStream.Flush();
		}
		
		public override void FlushMetadata(){
			inputStream.BlockMetadata.AddRange(this.BlockMetadata);
			this.BlockMetadata.Clear();
			inputStream.FlushMetadata();
		}
		
		public ChecksummerStream_MHash(ClientDeduplicatorStream inputStream){
			this.inputStream = inputStream;
			this.currentPos = 0;
			this.length = 0;
			md5 = MD5Native.Create(); 
			this.BlockMetadata = new List<IFileBlockMetadata>();

			
		}
	
		public override int Read(byte[] destBuffer, int offset, int count){
			//lastChecksum = md5.ComputeHash(destBuffer);
			throw new NotImplementedException("restore direction not implemented for checksumming");
		}
		private string ByteArrayToString(byte [] toConvert){
			StringBuilder sb = new StringBuilder(toConvert.Length);
			for (int i = 0; i < toConvert.Length - 1; i++){
				sb.Append(toConvert[i].ToString("X"));
			}
			return sb.ToString();
		}
		
		
		public override void Write(byte[] fromBuffer, int offset, int count){
#if DEBUG
			sw.Start();
#endif
			lastChecksum = md5.ComputeHash(fromBuffer, offset, count);
#if DEBUG
			sw.Stop();
			BenchmarkStats.Instance().ChecksumTime += sw.ElapsedMilliseconds;
			sw.Reset();
#endif
			//Console.WriteLine("ChecksummerStream:md5="+Convert.ToBase64String(lastChecksum));
			inputStream.ChecksumToMatch = lastChecksum; 
			inputStream.Write(fromBuffer, offset, count);
			length += count;
		}
		
		public override long Seek(long offset, SeekOrigin origin){
			inputStream.Seek(offset, origin);
			return offset;
		}
		
		
	}
}

