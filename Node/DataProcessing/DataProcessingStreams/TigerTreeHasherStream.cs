
/* Based on the work of Gil Schmidt, please read below.
 * http://www.codeproject.com/Articles/9336/ThexCS-TTH-tiger-tree-hash-maker-in-C
 * 
 * Tiger Tree Hash - by Gil Schmidt.
 * 
 *  - this code was writtin based on:
 *    "Tree Hash EXchange format (THEX)" 
 *    http://www.open-content.net/specs/draft-jchapweske-thex-02.html
 * 
 *  - the tiger hash class was converted from visual basic code called TigerNet:
 *    http://www.hotpixel.net/software.html
 * 
 *  - Base32 class was taken from:
 *    http://msdn.microsoft.com/msdnmag/issues/04/07/CustomPreferences/default.aspx
 *    didn't want to waste my time on writing a Base32 class.
 * 
 *  - fixed code for 0 byte file (thanks Flow84).
 * 
 *  if you use this code please add my name and email to the references!
 *  [ contact me at Gil_Smdt@hotmali.com ]
 * 
 */


using System;
using System.Text;
using System.IO;
using System.Collections;
//# define DEBUG
using System.Diagnostics;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Node.DataProcessing{
	
	/// <summary>
	/// This class provides an MD5 checksum for each block of data read
	/// </summary>
	internal class TigerTreeHasherStream:/*Stream,*/ IDataProcessorStream{
		
		private long length;
		private long currentPos;
		private byte[] lastChecksum;
		private byte[] previousChecksum;
		private ClientDeduplicatorStream inputStream;
		private HashAlgorithm hashAlgo;
		int        Block_Size = 1024*512;
        const   int        ZERO_BYTE_FILE = 0;
		private int        Leaf_Count;
		private List<HashHolder>  LeafCollection;
		private byte[] data;
		int dataPos = 0;
		private FileStream FilePtr; // toremove
		private bool havePair = false; //when true, we can hash 2 leaves

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
			if(!havePair)
				LeafCollection.Add(new HashHolder(hashAlgo.ComputeHash(data, 0, dataPos) ));
		}
		
		private byte[] GetRootHash(){
			ArrayList InternalCollection = new ArrayList();

			do{
				InternalCollection = new ArrayList(LeafCollection);
				LeafCollection.Clear();

				while (InternalCollection.Count > 1){
					//load next two leafs.
					byte[] HashA = ((HashHolder) InternalCollection[0]).HashValue;
					byte[] HashB = ((HashHolder) InternalCollection[1]).HashValue;
					
					//add their combined hash.
					LeafCollection.Add(new HashHolder(IH(HashA,HashB)));

					//remove the used leafs.
					InternalCollection.RemoveAt(0);
					InternalCollection.RemoveAt(0);
				}

				//if this leaf can't combine add him at the end.
				if (InternalCollection.Count > 0)
					LeafCollection.Add((HashHolder)InternalCollection[0]);
			} while (LeafCollection.Count > 1);

			return  LeafCollection[0].HashValue;
		}
		
		public override void FlushMetadata(){
			inputStream.BlockMetadata.AddRange(this.BlockMetadata);
			this.BlockMetadata.Clear();
			inputStream.FlushMetadata();
		}
		
		public TigerTreeHasherStream(ClientDeduplicatorStream inputStream, HashAlgorithm hashAlgorithm, int maxBlockSize){
			this.inputStream = inputStream;
			this.currentPos = 0;
			this.length = 0;
			Block_Size = maxBlockSize;
			hashAlgo = hashAlgorithm;
			hashAlgo.Initialize();
			LeafCollection = new List<HashHolder>();
			this.BlockMetadata = new List<IFileBlockMetadata>();
			data = new byte[Block_Size*2+2]; // holds 2 data nodes + leaf marks
			
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
			data[dataPos] = 0x00;
			Array.Copy(fromBuffer, offset, data, dataPos+1, count);
			previousChecksum = lastChecksum;
			lastChecksum = hashAlgo.ComputeHash(data, dataPos, count - offset+1);
			if(!havePair){
				dataPos = count - offset + 1;
				havePair = true;
			}
			else{
				LeafCollection.Add(
					new HashHolder(
						IH(
							previousChecksum, lastChecksum
						)
					)
				);
				
				havePair = false;
				dataPos = 0;
			}
			
			
			
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
		

		
		private struct HashHolder{
			public byte[] HashValue;

			public HashHolder(byte[] HashValue){
				this.HashValue = HashValue;
			}
		}
		
		
		
		/*public byte[] GetTTH(string Filename) 
		{
			
			HashHolder Result;
			try
			{
				FilePtr = new FileStream(Filename,FileMode.Open,FileAccess.Read,FileShare.ReadWrite);
			
				//if the file is 0 byte long.
                if (FilePtr.Length == ZERO_BYTE_FILE)
                {
                    Tiger TG = new Tiger();

                    Result.HashValue = TG.ComputeHash(new byte[] { 0 });
                }
                //if there's only one block in file use SmallFile().
                else if (FilePtr.Length <= Block_Size)
                    Result.HashValue = SmallFile();

                else
                {
                    //get how many leafs are in file.
                    Leaf_Count = (int)FilePtr.Length / Block_Size;

                    if (FilePtr.Length % Block_Size > 0)
                        Leaf_Count++;

                    //load blocks of data and get tiger hash for each one.
                    LoadLeafHash();

                    //get root hash from blocks hash.
                    Result = GetRootHash();
                }

                FilePtr.Close();

				return Result.HashValue;
			}
			catch (Exception e)
			{
				System.Diagnostics.Debug.WriteLine("error while trying to get TTH for file: " + 
					Filename + ". (" + e.Message + ")");

                if (FilePtr != null)
                    FilePtr.Close();

				return null;
			}
		}*/

		private byte[] SmallFile()
		{
			Tiger TG = new Tiger();
			byte[] Block = new byte[Block_Size];

			int BlockSize = FilePtr.Read(Block,0,1024);

			//gets hash for a single block file.
			return LH(ByteExtract(Block,BlockSize));
		}

		private void LoadLeafHash()
		{
			//LeafCollection = new ArrayList();

			for (int i = 0; i < (int) Leaf_Count / 2; i++)
			{
				byte[] BlockA = new byte[Block_Size],BlockB = new byte[Block_Size];
				
				FilePtr.Read(BlockA,0,1024);
				int DataSize = FilePtr.Read(BlockB,0,1024);

				//check if the block isn't big enough.
				if (DataSize < Block_Size)
					BlockB = ByteExtract(BlockB,DataSize);

				BlockA = LH(BlockA);
				BlockB = LH(BlockB);

				//add combined leaf hash.
				LeafCollection.Add(new HashHolder(IH(BlockA,BlockB)));
			}

			//leaf without a pair.
			if (Leaf_Count % 2 != 0)
			{
				byte[] Block = new byte[Block_Size];
				int DataSize = FilePtr.Read(Block,0,1024);

				if (DataSize < 1024)
					Block = ByteExtract(Block,DataSize);

				LeafCollection.Add(new HashHolder(LH(Block)));
			}
		}

		

		private byte[] IH(byte[] LeafA,byte[] LeafB) //internal hash.
		{ 
			byte[] Data = new byte[LeafA.Length + LeafB.Length + 1];

			Data[0] = 0x01; //internal hash mark.

			//combines two leafs.
			LeafA.CopyTo(Data,1);
			LeafB.CopyTo(Data,LeafA.Length + 1);

			//gets tiger hash value for combined leaf hash.
			Tiger TG = new Tiger();
			TG.Initialize();
			return TG.ComputeHash(Data);			
		}

		private byte[] LH(byte[] Raw_Data) //leaf hash.
		{ 
			byte[] Data = new byte[Raw_Data.Length + 1];

			Data[0] = 0x00; //leaf hash mark.
			Raw_Data.CopyTo(Data,1);

			//gets tiger hash value for leafs blocks.
			Tiger TG = new Tiger();
			TG.Initialize();
			return TG.ComputeHash(Data);
		}

		private byte[] ByteExtract(byte[] Raw_Data,int Data_Length) //copy 
		{
			//return Data_Length bytes from Raw_Data.
			byte[] Data = new byte[Data_Length];

			for (int i = 0; i < Data_Length; i++)
				Data[i] = Raw_Data[i];

			return Data;
		}
	}
}
