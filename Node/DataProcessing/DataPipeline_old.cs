using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
#if DEBUG
using System.Diagnostics;
#endif	
using System.Runtime.Serialization.Formatters.Binary;
using P2PBackup.Common;
using Node.Utilities;
using Node.DataProcessing; // toremove

namespace Node.DataProcessing{
	public enum PipelineMode_{Read,Write}
	// assembles and chains DataProcessing Streams 
	// a complete "pump" or chain would be : source file --> checksummerStream --> ClientDeduplicatorStream
	//		--> CompressorStream --> EncryptorStream --> Storage node's session Stream
	internal class DataPipeline_{
		
		private Backup backup;
		private const int bufferSize = 512*1024; //512 k buffer
		private Session storageSession;
		IDataProcessorStream firstStream;
		Stream sessionStream;
		NullSinkStream finalStream;
		//FileBlockMetadata fileMD;
		ClientDeduplicatorStream cdds;
		//bool isClientDedup;
		private byte[] headerData;
		//private Utilities.PrivilegesManager privilegesManager;
		private bool cancelRequested = false;
#if DEBUG
		Stopwatch sw = new Stopwatch();
#endif			

		internal DataProcessingFlags Flags{get;set;}

		internal int HeaderLength{
			get{return headerData.Length;}
		}

		internal IDataProcessorStream Stream{get{return firstStream;}}

		internal DataPipeline_(PipelineMode mode, Session s, Backup b, DataProcessingFlags flags){
			backup = b;

			storageSession = s;
			BinaryFormatter formatter = new BinaryFormatter();
		 	BChunkHeader header = new BChunkHeader();

			header.DataFlags = flags;
		
			header.Version = Utilities.PlatForm.Instance().NodeVersion;
			//header.TaskId = taskId;
			header.TaskId = b.TaskId;
			
			// end-of-chain stream
			sessionStream = new NetworkStream(storageSession.DataSocket);

			this.Flags = flags;
#if DEBUG
			if(ConfigManager.GetValue("BENCHMARK") != null)
				sessionStream = new DummyStream();
#endif
			if(flags.HasFlag(DataProcessingFlags.CChecksum))
				finalStream = new NullSinkStream(new ChunkHasherStream(sessionStream), mode);
			else
				finalStream = new NullSinkStream(sessionStream, mode);// dummy dest stream
			//firstStream = new NullSinkStream(); // test and benchmarking
			
			// top-of-chain streams
			firstStream = finalStream;
			if(flags.HasFlag(DataProcessingFlags.CEncrypt)){
				EncryptorStream encStream = new EncryptorStream(firstStream, true, null);
				header.EncryptionMetaData = encStream.EncryptionMetadata;
				
				// TODO !! take encryptionMetadata and add it to index
				firstStream = encStream;
			}
			if(flags.HasFlag(DataProcessingFlags.CCompress)){
				//firstStream = new CompressorStream(firstStream, CompressorAlgorithm.Lzo, 1024);
				firstStream = new GZCompressorStream(firstStream, System.IO.Compression.CompressionMode.Compress);
			}
			if(flags.HasFlag(DataProcessingFlags.CDedup)){
				cdds = new ClientDeduplicatorStream(firstStream, s.ClientId);
				/*try{ // TODO ! remove cksum provider selection from here, find a more elegant solution
					firstStream = new ChecksummerStream_MHash((ClientDeduplicatorStream)cdds);
				}
				catch(Exception e){*/
					firstStream = new ChecksummerStream((ClientDeduplicatorStream)cdds);
					//firstStream = new TigerTreeHasherStream((ClientDeduplicatorStream)cdds);
				
				/*}*/
				// Pre-Initialize dedup index (if needed)
				DedupIndex.Instance().Initialize();
			}
			MemoryStream headerStream = new MemoryStream();
			formatter.Serialize(headerStream, header);
			headerData = headerStream.ToArray();
			Logger.Append(Severity.INFO, "Created data pipeline with flags "+flags.ToString());
			//privilegesManager = new Utilities.PrivilegesManager();
		}
		
		internal void Process(BChunk chunk, long maxChunkSize){
			finalStream.SetLength(0);
			//privilegesManager.Grant();
			try{
				storageSession.AnnounceChunkBeginTransfer(chunk.Name, this.HeaderLength);
				sessionStream.Write(headerData, 0, headerData.Length);
			}
			catch(Exception ioe){
					
				storageSession.LoggerInstance.Log(Severity.ERROR, "network I/O error : "+ioe.Message/*+"---"+ioe.StackTrace*/);
				backup.AddHubNotificationEvent(904, "", ioe.Message);	
				if(ioe.InnerException != null)
					throw(ioe.InnerException);
			}
			chunk.Size = headerData.Length;
			DateTime startChunkBuild = DateTime.Now;
			Stream fs = null;
			byte[] content = new byte[1024*512]; // read 512k at once
			long sent = 0; // to know chunk final size (after pipeling processing streams)
			foreach(IFile file in chunk.Files){
				if(file == null || file.Kind != FileType.File)
					continue;
				// if at this stage IFile already has metadata, it does not need to be processed:
				//  we are running a diff/incr, and file has only got metadata change, or has been renamed/moved/deleted
				/*if(file.BlockMetadata.BlockMetadata.Count >0){
					if(!(file.BlockMetadata.BlockMetadata[0] is ClientDedupedBlock)){
					Console.WriteLine ("Process() : detected early metadata, file "+file.FileName+" has been renamed/deleted/metadataonlychanged");
					continue;
					}
				}*/
				long offset=file.FileStartPos; // if a file is split into multiple chunks, start reading at required filepart pos
			    long remaining = file.FileSize;
				int read = 0;
				int reallyRead = 0;
				try{	
					fs = file.OpenStream(FileMode.Open);
					long seeked = fs.Seek(offset, SeekOrigin.Begin);
					//Console.WriteLine ("Process() : seeked "+seeked+"/"+offset);
					if(seeked != offset){
						storageSession.LoggerInstance.Log(Severity.ERROR, "Unable to seek to required position ( reached "+seeked+" instead of "+offset+") in file "+file.SnapFullPath);
						backup.AddHubNotificationEvent(912, file.SnapFullPath, "Seek error");
					}
					// we read from snapshot but we need to set original file path:
					// TODO !!! change back filename to original path (remove snapshotteds path)
					//file.FileName = file.FileName.Replace(snapPath, bPath);
				}
				catch(Exception e){

					storageSession.LoggerInstance.Log (Severity.ERROR, "Unable to open file "+file.SnapFullPath+": "+e.Message);
					backup.AddHubNotificationEvent(912, file.SnapFullPath, e.Message);
					try{
						fs.Close ();
					}catch{}
					//chunk.RemoveItem(file); // we don't want a failed item to be part of the backup index
					continue;
				}
				try{
					while( (read = fs.Read(content, 0, content.Length)) > 0 && reallyRead <= maxChunkSize && !cancelRequested){
						// if file has to be splitted, take care to read no more than maxchunksize
						if(reallyRead+read > maxChunkSize){
								read = (int)maxChunkSize - reallyRead;
							if(read == 0) break;
						}
						remaining -= read;
				        offset += read;
						reallyRead += read;
						//try{
							firstStream.Write(content, 0, read);
						//}
						/*catch(Exception e){
							storageSession.LoggerInstance.Log (Severity.ERROR, "Could not write to pipeline streams : "+e.Message+" --- \n"+e.StackTrace);
							if(e.InnerException != null)
								storageSession.LoggerInstance.Log (Severity.ERROR, "Could not write to pipeline streams, inner stack trace : "+e.InnerException.Message+" ---- \n"+e.InnerException.StackTrace);
							throw(e);
						}*/
						sent += read;
						//fs.Seek(offset, SeekOrigin.Begin);
					}
					// now we correct FileSize with REAL size (which includes Alternate Streams on NT)
					// TODO report to hub
					// TODO 2: if total file size is < than expected, file has changed too.
					if(offset > file.FileSize /*&& Utilities.PlatForm.IsUnixClient()*/) {
						Logger.Append(Severity.WARNING, "TODO:report File '"+file.SnapFullPath+"' has changed during backup : expected size "+file.FileSize+", got "+offset);
						backup.AddHubNotificationEvent(903, file.SnapFullPath, "");	
					}
					//Console.WriteLine("Built : file="+file.FileName+", size="+file.FileSize+", read="+offset+"\n");
					file.FileSize = offset;
					firstStream.FlushMetadata();
					file.BlockMetadata.BlockMetadata.AddRange(finalStream.BlockMetadata);
					//Console.WriteLine ("file "+file.FileName+" has "+file.BlockMetadata.BlockMetadata.Count+" metadata blocks");
					finalStream.BlockMetadata.Clear();
					fs.Close();
					
					chunk.OriginalSize += reallyRead;
				}
				catch(Exception ioe){
					fs.Close();
					if(ioe.InnerException is SocketException){
						storageSession.LoggerInstance.Log(Severity.ERROR, "I/O error, could not process file "+file.SnapFullPath+" of chunk "+chunk.Name+": "+ioe.Message/*+"---"+ioe.StackTrace*/);
						backup.AddHubNotificationEvent(904, file.SnapFullPath, ioe.Message);	
						throw(ioe.InnerException);
					}
					else{
						storageSession.LoggerInstance.Log(Severity.ERROR, "Could not process file "+file.SnapFullPath+" of chunk "+chunk.Name+": "+ioe.Message+"---"+ioe.StackTrace);
						backup.AddHubNotificationEvent(912, file.SnapFullPath, ioe.Message);	
						continue;	
					}
				}
		  	}	// end foreach ifile
			
			firstStream.Flush();
			finalStream.Flush();
			DateTime endChunkBuild = DateTime.Now;
			TimeSpan duration = endChunkBuild - startChunkBuild;
			chunk.Size += finalStream.Length;
			//chunk.Size += sessionStream.Length;
#if DEBUG
			if(ConfigManager.GetValue("BENCHMARK") != null)
				storageSession.AnnounceChunkEndTransfer(chunk.Name, 0);
			else
				storageSession.AnnounceChunkEndTransfer(chunk.Name, chunk.Size);
#else
			storageSession.AnnounceChunkEndTransfer(chunk.Name, chunk.Size);
#endif
			//privilegesManager.Revoke();
			storageSession.LoggerInstance.Log(Severity.DEBUG, "Processed and transferred "+chunk.Name+", original size="+chunk.OriginalSize/1024+"k, final size="+chunk.Size/1024+"k, "+chunk.Files.Count+" files in "+duration.Seconds+"."+duration.Milliseconds+" s, "+Math.Round((chunk.OriginalSize/1024)/duration.TotalSeconds,0)+"Kb/s");
		}
		
		internal void Cancel(){
			cancelRequested = true;	
		}
	}
}

