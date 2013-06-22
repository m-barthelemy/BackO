using System;
using System.IO;
using System.Threading;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using P2PBackup.Common;
using Node.Utilities;
#if DEBUG
using System.Diagnostics;
#endif	

namespace Node.DataProcessing{

	// to replace DataPipeline Process() method. DataPipeline must be reduced to only a mean of building a serie/pipeline of data procvessing streams.
	internal class ChunkProcessor	{

		DataPipeline pipeline;
		Session storageSession;
		Backup backup;
		Stream sessionDataStream;
		private byte[] headerData;
		private bool cancelRequested = false;
		private CancellationToken token; 

		internal int HeaderLength{
			get{return headerData.Length;}
		}
#if DEBUG
		Stopwatch sw = new Stopwatch();
#endif	
		internal ChunkProcessor(Session session, DataPipeline p, Backup b, CancellationToken t){

			backup = b;
			pipeline = p;
			storageSession = session;
			token = t;
			BinaryFormatter formatter = new BinaryFormatter();
		 	BChunkHeader header = new BChunkHeader();
			header.DataFlags = pipeline.Flags;
			header.Version = Utilities.PlatForm.Instance().NodeVersion;
			header.TaskId = session.TaskId;
			header.OwnerNode = b.NodeId;
			MemoryStream headerStream = new MemoryStream();
			formatter.Serialize(headerStream, header);
			headerData = headerStream.ToArray();

			// end-of-chain stream
			sessionDataStream = new NetworkStream(storageSession.DataSocket);
			p.OutputStream = sessionDataStream;
			p.Init();
		}


		internal void Process(BChunk chunk, long maxChunkSize){

			IDataProcessorStream pipelineStream = pipeline.Stream;
			//pipeline.Stream.SetLength(0);
			pipeline.Reset();
			pipeline.CurrentChunk = chunk.Name;
			try{
				storageSession.AnnounceChunkBeginTransfer(chunk.Name, headerData.Length);
				sessionDataStream.Write(headerData, 0, headerData.Length);
			}
			catch(Exception ioe){
				storageSession.LoggerInstance.Log(Severity.ERROR, "Network I/O error : "+ioe.Message+" ---- "+ioe.StackTrace);
				backup.AddHubNotificationEvent(904, "", ioe.Message);	
				if(ioe.InnerException != null)
					throw(ioe.InnerException);
			}
			chunk.Size = headerData.Length;
			DateTime startChunkBuild = DateTime.Now;
			Stream fs = null;
			byte[] content = new byte[1024*512]; // read 512k at once
			long offset, remaining; // to know chunk final size (after pipeling processing streams)
			int read, itemReallyRead, partialRead;

			foreach(IFSEntry file in chunk.Items){
				if(token.IsCancellationRequested){
					Logger.Append(Severity.TRIVIA, "Received cancel order, exiting");
					return;
				}
				if(file.FileSize == 0)
					continue; 

				// TODO!! is that correct?? now that DataLayoutInfos is a flag
				if(file.ChangeStatus == DataLayoutInfos.NoChange || file.ChangeStatus == DataLayoutInfos.Deleted) // no data change
					continue;

				//Console.WriteLine ("\tProcessing/sending item "+file.Name+", starting at pos "+file.FileStartPos);
				//offset = file.FileStartPos; // if a file is split into multiple chunks, start reading at required filepart pos
			    remaining = file.FileSize;
				read = 0;
				itemReallyRead = 0;
				partialRead = 0;
				try{	
					fs = file.OpenStream(FileMode.Open);
					long seeked = fs.Seek(file.FileStartPos, SeekOrigin.Begin);
					if(seeked != file.FileStartPos){
						file.ChangeStatus = DataLayoutInfos.Invalid;
						storageSession.LoggerInstance.Log(Severity.ERROR, "Unable to seek to required position ( reached "+seeked+" instead of "+file.FileStartPos+") in file "+file.SnapFullPath);
						backup.AddHubNotificationEvent(912, file.SnapFullPath, "Seek error : wanted to go to"+file.FileStartPos+" but went to "+seeked);
					}
				}
				catch(Exception e){
					file.ChangeStatus = DataLayoutInfos.Invalid;
					storageSession.LoggerInstance.Log (Severity.ERROR, "Unable to open file "+file.SnapFullPath+": "+e.Message);
					backup.AddHubNotificationEvent(912, file.SnapFullPath, e.Message);
					try{
						fs.Close ();
					}catch{}
					continue;
				}
				try{
					//Console.WriteLine ("reading item '"+file.Name+"'");
					while( (read = fs.Read(content, partialRead, content.Length-partialRead)) > 0 && itemReallyRead <= maxChunkSize && !cancelRequested){
#if DEBUG
						//sw.Start();
#endif
						//read = fs.Read(content, partialRead, content.Length-partialRead);
#if DEBUG
						//sw.Stop();
						//BenchmarkStats.Instance().ReadTime += sw.ElapsedMilliseconds;
						//Console.WriteLine ("\t\tread "+read+" in "+sw.ElapsedMilliseconds+"ms");
						//sw.Reset();
#endif

						// if file has to be splitted, take care to read no more than maxchunksize
						if(itemReallyRead+read > maxChunkSize){
								read = (int)maxChunkSize - itemReallyRead;
							if(read == 0) break;
						}

						remaining -= read;
						partialRead += read;

						itemReallyRead += read;
						if(partialRead == content.Length || remaining == 0){
							pipelineStream.Write(content, 0, partialRead);
							partialRead = 0;
						}
						if(token.IsCancellationRequested){
							Logger.Append(Severity.TRIVIA, "Received cancel order while processing  '"+file.SnapFullPath+"', giving up");
							return;
						}
					}
					//Console.WriteLine ("\tDone reading item '"+file.Name+"', estimated size="+file.FileSize+", really read="+itemReallyRead);

					// now we correct FileSize with REAL size (which includes Alternate Streams on NT)
					// TODO 2: if total file size is < than expected, file has changed too.
					if(itemReallyRead > file.FileSize && Utilities.PlatForm.IsUnixClient()) {
						Logger.Append(Severity.WARNING, "Item '"+file.SnapFullPath+"' : size has changed during backup : expected "+file.FileSize+", got "+itemReallyRead);
						backup.AddHubNotificationEvent(903, file.SnapFullPath, itemReallyRead.ToString());	
					}
					file.FileSize = itemReallyRead;

					// Now that file has been processed, gather its metadata from processing streams, and add it to index 
					//Console.WriteLine ("Process(1/3) : about to call pipelineStream.FlushMetadata()");
					pipelineStream.FlushMetadata();

					foreach(IFileBlockMetadata mtd in pipeline.FinalStream.BlockMetadata){
						if(mtd is ClientDedupedBlocks){
							file.BlockMetadata.DedupedBlocks = ((ClientDedupedBlocks)mtd).Ids;
						}
						else
							file.BlockMetadata.BlockMetadata.Add(mtd);
					}

					pipeline.FinalStream.BlockMetadata = new System.Collections.Generic.List<IFileBlockMetadata>();
					chunk.OriginalSize += itemReallyRead;
				}
				catch(Exception ioe){
					if(ioe.InnerException is SocketException){
						storageSession.LoggerInstance.Log(Severity.ERROR, "I/O error, could not process file "+file.SnapFullPath+" of chunk "+chunk.Name+": "+ioe.Message/*+"---"+ioe.StackTrace*/);
						backup.AddHubNotificationEvent(904, file.SnapFullPath, ioe.Message);	
						throw(ioe.InnerException);
					}
					else{
						storageSession.LoggerInstance.Log(Severity.ERROR, "Could not process file "+file.SnapFullPath+" of chunk "+chunk.Name+": "+ioe.Message+"---"+ioe.StackTrace);
						backup.AddHubNotificationEvent(912, file.SnapFullPath, ioe.Message);	
					}
				}
				finally{
					fs.Close();
				}
		  	}	// end foreach ifile

			DateTime endChunkBuild = DateTime.Now;
			TimeSpan duration = endChunkBuild - startChunkBuild;
			pipeline.Stream.Flush();
#if DEBUG
			if(ConfigManager.GetValue("BENCHMARK") != null)
				storageSession.AnnounceChunkEndTransfer(chunk.Name, 0);
			else
				storageSession.AnnounceChunkEndTransfer(chunk.Name, pipeline.FinalSize + headerData.Length );
#else
			storageSession.AnnounceChunkEndTransfer(chunk.Name, chunk.Size);
#endif
			storageSession.LoggerInstance.Log(Severity.DEBUG, "Processed and transferred "+chunk.Name+", original size="+chunk.OriginalSize/1024+"k, final size="+chunk.Size/1024+"k, "+chunk.Items.Count+" files in "+duration.Seconds+"."+duration.Milliseconds+" s, "+Math.Round((chunk.OriginalSize/1024)/duration.TotalSeconds,0)+"Kb/s");
		}
		
		internal void Cancel(){
			cancelRequested = true;	
		}
	}
}

