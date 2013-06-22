// <ORIGINALCODE>
// This class contains original code from The Mono project
// </ORIGINALCODE>
// Authors:
//  Matthew S. Ford (Matthew.S.Ford@Rose-Hulman.Edu)
//  Sebastien Pouliot (sebastien@ximian.com)
//
// Copyright 2001 by Matthew S. Ford.
// Portions (C) 2002 Motus Technologies Inc. (http://www.motus.com)
// Copyright (C) 2004-2006 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace Node.DataProcessing{
	public class ChunkHasherStream:Stream/*:IDataProcessorStream*/{
		
		private long length;
		private long currentPos;
		private byte[] lastChecksum;
		private Stream outputStream;
		private MD5 md5;
		
		
		public byte[] LastChecksum {
			get {
				return this.lastChecksum;
			}
		}
		
		/*public override List<IFileBlockMetadata> BlockMetadata{get;set;}*/

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
			set{ currentPos = value; Seek(value, SeekOrigin.Begin);}
		}
		
		public override long Length{
			get{ return length;}	
		}
		
		public override void SetLength(long value){
			length = value;
			
		}
		
		public override void Flush(){
			outputStream.Flush();
		}
		
		/*public override void FlushMetadata(){
			outputStream.BlockMetadata.AddRange(this.BlockMetadata);
			this.BlockMetadata.Clear();
			outputStream.FlushMetadata();
		}*/
		
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
			lastChecksum = md5.ComputeHash(fromBuffer, offset, count);
			//Console.WriteLine("ChecksummerStream:md5="+Convert.ToBase64String(lastChecksum));
			//outputStream.ChecksumToMatch = lastChecksum; 
			outputStream.Write(fromBuffer, offset, count);
			//length += count;
		}
		
		public override long Seek(long offset, SeekOrigin origin){
			outputStream.Seek(offset, origin);
			return offset;
		}
		public ChunkHasherStream(Stream outputStream){
			this.outputStream = outputStream;
			this.currentPos = 0;
			this.length = 0;
			md5 = MD5.Create(); 
			
		}
	}
}

