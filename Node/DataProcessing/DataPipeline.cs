using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
#if DEBUG
using System.Diagnostics;
#endif	
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using P2PBackup.Common;
using Node.Utilities;

namespace Node.DataProcessing{

	public enum PipelineMode{Read,Write}
	/// assembles and chains DataProcessing Streams 
	/// a complete "pump" or chain would be : source file --> checksummerStream --> ClientDeduplicatorStream
	///		--> CompressorStream --> EncryptorStream --> Storage node's session Stream
	internal class DataPipeline : IDisposable{

		private int bsId; // ref to task in order to open the right dedup db
		private DedupIndex ddb;

		private const int bufferSize = 512*1024; //512 k buffer
		IDataProcessorStream firstStream;

		NullSinkStream finalStream;
		ClientDeduplicatorStream cdds;
		NullSinkStream counterStream;

#if DEBUG
		Stopwatch sw = new Stopwatch();
#endif			

		internal DataProcessingFlags Flags{get;set;}
		//internal RSACryptoServiceProvider CryptoKey{get;set;}
		internal byte[] CryptoKey{get;set;}
		internal byte[] IV{get;set;}

		internal PipelineMode Mode{get;private set;}
		internal byte[] EncryptionMetaData{get;private set;}
		internal int StorageNode{get;set;}

		internal string CurrentChunk{// sets the current data chunk name (needed for dedup index)
			set{
				if(cdds != null)
					cdds.CurrentChunkName = value;
			}
		}

		internal long FinalSize{get{ return counterStream.Length;}}

		internal Stream OutputStream{get;set;}
		//internal Stream InputStream{get;set;}

		/*internal int HeaderLength{
			get{return headerData.Length;}
		}*/

		internal IDataProcessorStream Stream{get{return firstStream;}}
		internal IDataProcessorStream FinalStream{get{return finalStream;}}


		internal DataPipeline(PipelineMode mode, DataProcessingFlags flags){
			this.Flags = flags;
			this.Mode = mode;
		}

		internal DataPipeline(PipelineMode mode, DataProcessingFlags flags, int bsId, DedupIndex ddidx):this(mode, flags){
			this.bsId = bsId;
			ddb = ddidx;
		}

		internal void Init(){
			Logger.Append(Severity.INFO, "Creating data pipeline with mode = "+this.Mode+", flags = '"+this.Flags.ToString()+"'");
#if DEBUG
			if(ConfigManager.GetValue("BENCHMARK") != null)
				this.OutputStream = new DummyStream();
#endif
			if(this.Flags.HasFlag(DataProcessingFlags.CChecksum) )
				counterStream = new NullSinkStream(new ChunkHasherStream(this.OutputStream), this.Mode);
			else
				counterStream = new NullSinkStream(this.OutputStream, this.Mode);
			finalStream = counterStream;

			
			// top-of-chain streams
			firstStream = finalStream;
			if(this.Flags.HasFlag(DataProcessingFlags.CEncrypt) || this.Flags.HasFlag(DataProcessingFlags.SEncrypt)){
				if(this.Mode == PipelineMode.Read){
					throw new NotImplementedException("PipeLine read mode with decryption not yet implemented");
				}
				else{
					Console.WriteLine ("Pipeline.init() : this.CryptoKey="+this.CryptoKey);
					EncryptorStream encStream = new EncryptorStream(firstStream, true, this.CryptoKey, this.IV);
					this.EncryptionMetaData = encStream.EncryptionMetadata;
					// TODO !! take encryptionMetadata and add it to index
					firstStream = encStream;
				}
			}
			if(this.Flags.HasFlag(DataProcessingFlags.CCompress)||this.Flags.HasFlag(DataProcessingFlags.SCompress)){
				if(this.Mode == PipelineMode.Read){
					firstStream = new LZ4Decompressor(firstStream);
					//firstStream = new GZCompressorStream(firstStream, System.IO.Compression.CompressionMode.Decompress);
				}
				else{
					//firstStream = new QuickLZCompressionStream(firstStream);
					//firstStream = new GZCompressorStream(firstStream, System.IO.Compression.CompressionMode.Compress);
					firstStream = new LZ4CompressorStream(firstStream);
				}
			}
			if(this.Flags.HasFlag(DataProcessingFlags.CDedup)|| this.Flags.HasFlag(DataProcessingFlags.SDedup)){
				cdds = new ClientDeduplicatorStream(firstStream, this.StorageNode, ddb/*DedupIndex.Instance(0, true)*/);
				/*try{ // TODO ! remove cksum provider selection from here, find a more elegant solution
					firstStream = new ChecksummerStream_MHash((ClientDeduplicatorStream)cdds);
				}
				catch(Exception e){*/
					firstStream = new ChecksummerStream((ClientDeduplicatorStream)cdds);
					//firstStream = new TigerTreeHasherStream((ClientDeduplicatorStream)cdds);
			}
		}

		internal void Reset(){
			finalStream.SetLength(0);
			counterStream.SetLength(0);
		}

		public  void Dispose(){
			//calling close() on first stream will recursively call close() on all nested streams
			if(firstStream != null)
				firstStream.Close();

		}

	}
}

