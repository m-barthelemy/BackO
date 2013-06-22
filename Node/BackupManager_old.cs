//# define DEBUG
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
//using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
//using System.Security.Cryptography.X509Certificates;
using Node.DataProcessing;
using Node.Utilities;
using P2PBackup.Common;

namespace Node{
	
	/// <summary>
	/// This class is responsible for getting backup chunks generated, and process them (transfer, maintain index),
	///  	following a Producer(Chunk.Build()) - Consumer(USer.SendPut+wait for transfer success) model
	/// It will process chunks building and transferring following the provided level of paralleism.
	/// Paralleism will directly impact memory usage:
	/// 	max used memory <=> parallelism*maxchunksize
	/// </summary>
	public class BackupManager_	{

		private Backup backup;
		private static List<BChunk> processingChunks; // manages chunks being processed 
		//private static ConcurrentBag<BChunk> processingChunks;
		
		private BlockingCollection<BChunk> chunkBuilderFeed;
		private BlockingCollection<BChunk> indexerChunksFeed; // chunks done and ready to be indexed
		private BChunk indexChunk;
		private int doneBdhProducers;
		private int bdhProducersCount;
		public delegate void BackupDoneHandler(long taskId);
		public event BackupDoneHandler BackupDoneEvent;
		internal CancellationTokenSource cancellationTokenSource;
		IEnumerator<BackupRootDrive> brdEnumerator;
		bool areBrdDone ;
	
		/// <summary>
		/// Initializes a new instance of the <see cref="Node.BackupManager"/> class.
		/// Must be passed an initialized backup (with snapshots done ect) as parameter.
		/// </summary>
		/// <param name='b'>
		/// Backup intance previously created and prepared
		/// </param>
		/// <param name='taskId'>
		/// Task identifier.
		/// </param>
		public BackupManager_(Backup b, long taskId){
			backup = b;
			chunkBuilderFeed = new BlockingCollection<BChunk>(new ConcurrentQueue<BChunk>(), (int)b.Parallelism.Value);
			indexerChunksFeed = new BlockingCollection<BChunk>(new ConcurrentQueue<BChunk>(), (int)b.Parallelism.Value+1);
			processingChunks = new List<BChunk>();
			cancellationTokenSource = new CancellationTokenSource();
			//processingChunks = new ConcurrentBag<BChunk>();
			//index = new BackupIndex(b);
			//index.WriteHeaders();
			doneBdhProducers = 0;
			bdhProducersCount = 0;
		}
		
		~BackupManager_(){
			Logger.Append(Severity.DEBUG2, "<TRACE> BackupManager destroyed.");	
		}
		
		internal void Run(){
			areBrdDone = false;
			User.SendPut(backup.TaskId, -1, (int)backup.Parallelism.Value);
			brdEnumerator = backup.RootDrives.GetEnumerator();
			User.StorageSessionReceivedEvent += new User.StorageSessionReceivedHandler(this.SessionReceived);
			for(int i=0; i<backup.Parallelism.Value; i++){
				if(!brdEnumerator.MoveNext()){ 
					areBrdDone = true;
					break;
				}
				BackupRootDrive brd = (BackupRootDrive)brdEnumerator.Current;
				bdhProducersCount++;
				var producer = System.Threading.Tasks.Task.Factory.StartNew(()=>{
					Produce(brd);
				}, TaskCreationOptions.LongRunning); // WRONG : split paths first
				producer.ContinueWith(o => UnexpectedError(producer), TaskContinuationOptions.OnlyOnFaulted);
			}
			// Start indexer task
			var indexer = System.Threading.Tasks.Task.Factory.StartNew(()=>{
					DoIndex();
			}, cancellationTokenSource.Token, TaskCreationOptions.None, TaskScheduler.Default);
			
			indexer.ContinueWith(o => UnexpectedError(indexer), TaskContinuationOptions.OnlyOnFaulted);
			indexer.ContinueWith(o => ProcessIndex(), TaskContinuationOptions.OnlyOnRanToCompletion /*| TaskContinuationOptions.NotOnFaulted| TaskContinuationOptions.NotOnCanceled*/);
			/*System.Threading.Tasks.Task.Factory.ContinueWhenAll(producers,  z=>{
					
			});*/
		}
		
		private void ContinueProducing(){
			doneBdhProducers++;
			if(brdEnumerator.MoveNext()){ 
				bdhProducersCount++;
				BackupRootDrive brdn = (BackupRootDrive)brdEnumerator.Current;
				var continuedProducer = System.Threading.Tasks.Task.Factory.StartNew(() =>{
					Produce(brdn);
				}, TaskCreationOptions.LongRunning);
			}
			else
				areBrdDone = true;
			if(areBrdDone && (doneBdhProducers >= bdhProducersCount)){
				if(!cancellationTokenSource.IsCancellationRequested)
					Logger.Append(Severity.INFO, "Done gathering items to backup.");
				chunkBuilderFeed.CompleteAdding();
				return;
			}
		}
		
		
		/// <summary>
		/// One 'Produce' task generates chunks for one BackupRootDrive (ie 1 mountpoint).
		/// </summary>
		/// <param name='bdr'>
		/// the BackupRootDrive to scan for items
		/// </param>
		private void Produce(BackupRootDrive bdr){
			Logger.Append(Severity.INFO, "Collecting items to backup for drive "+bdr.SystemDrive.MountPoint);
			BackupRootDriveHandler bdh = new BackupRootDriveHandler(bdr, this.backup.TaskId, backup.MaxChunkSize, backup.MaxChunkSize, backup.MaxChunkFiles, backup.Level, backup.RefStartDate, backup.RefEndDate, backup.RefTaskId);
			bdh.LogEvent += LogReceived;
			bdh.SubCompletionEvent += new BackupRootDriveHandler.SubCompletionHandler(IncrementSubCompletion);

			//IEnumerator<BChunk> chunkEnumerator = bdh.GetNextChunk().GetEnumerator();//backup.GetNextChunk().GetEnumerator();

			foreach(P2PBackup.Common.BasePath baseP in bdr.Paths){
				bdh.SetCurrentPath(baseP);
				IEnumerator<BChunk> chunkEnumerator = bdh.GetNextChunk().GetEnumerator();
				while(chunkEnumerator.MoveNext() && !cancellationTokenSource.IsCancellationRequested){
					BChunk chunk = chunkEnumerator.Current;
					try{
						chunkBuilderFeed.Add(chunk, cancellationTokenSource.Token);
					}
					catch(OperationCanceledException){
						Logger.Append(Severity.DEBUG2, "Producer has been manually cancelled on purpose, stopping...");
						return;
					}
					catch(Exception e){
						Logger.Append(Severity.ERROR, "###################### Produce()	: add refused : "+e.Message+" ---- "+e.StackTrace);
						return;
					}
					// stats
					foreach(IFSEntry item in chunk.Files)
						backup.ItemsByType[(int)item.Kind]++;
					Logger.Append(Severity.DEBUG, "Added chunk "+chunk.Name+" containing "+chunk.Files.Count+" items ");
				}
			}
			bdh.SubCompletionEvent -= new BackupRootDriveHandler.SubCompletionHandler(IncrementSubCompletion);
			bdh.LogEvent -= LogReceived;
			Logger.Append(Severity.INFO, "Producer has done collecting items to backup for drive "+bdr.SystemDrive.MountPoint);
			if(!cancellationTokenSource.IsCancellationRequested)
				ContinueProducing();
			else 
				bdh.Dispose();
		}
		
		private void SessionReceived(long taskId, Session session/*, int budget*/){
			//ThreadPerTaskScheduler
			//System.Threading.Tasks.TaskFactory consumersFactory = new System.Threading.Tasks.TaskFactory(new System.Threading.Tasks.Schedulers.ThreadPerTaskScheduler());
			if(chunkBuilderFeed.IsCompleted) return;
			System.Threading.Tasks.Task consumeTask = System.Threading.Tasks.Task.Factory.StartNew(() =>{
			//Task consumeTask = consumersFactory.StartNew(() =>{
					Consume(session);
			}, TaskCreationOptions.LongRunning);
			consumeTask.ContinueWith(o=>AfterTask(consumeTask, session), TaskContinuationOptions.OnlyOnRanToCompletion
			                         	| TaskContinuationOptions.NotOnFaulted | TaskContinuationOptions.NotOnCanceled);
			consumeTask.ContinueWith(o=>UnexpectedError(consumeTask), TaskContinuationOptions.OnlyOnFaulted);
			//consumeTask.Dispose();
		}
		
		
		private void Consume(Session s/*, int budget*/){

			// Filter client-side processing flags
			DataProcessingFlags clientFlags = DataProcessingFlags.None;
			foreach (DataProcessingFlags flag in Enum.GetValues(typeof(DataProcessingFlags))){
				if((int)flag < 512 && backup.DataFlags.HasFlag(flag))
					clientFlags |= flag;
			}
			
			DataPipeline pipeline = new DataPipeline(PipelineMode.Write, clientFlags, this.backup.Bs.Id);
			if(backup.DataFlags.HasFlag(DataProcessingFlags.CDedup))
			   pipeline.StorageNode = s.ClientId;
			if(backup.DataFlags.HasFlag(DataProcessingFlags.CEncrypt)){
				//X509Certificate2 cert = new X509Certificate2(ConfigManager.GetValue("Security.CertificateFile"), "");
			   //pipeline.CryptoKey = (RSACryptoServiceProvider)cert.PublicKey.Key;
				pipeline.CryptoKey = s.CryptoKey;

				byte[] iv = new byte[16];
				Array.Copy (System.BitConverter.GetBytes(backup.TaskId), iv, 8);
				Array.Copy (System.BitConverter.GetBytes(backup.TaskId), 0, iv, 8, 8);
				pipeline.IV = iv; //new byte[]{Convert.ToByte(backup.TaskId)};
			}
			//pipeline.Init();
			ChunkProcessor cp = new ChunkProcessor(s, pipeline, backup);
			s.TransfertDoneEvent += new Session.TransfertDoneHandler(ManageChunkSent);
			
			// We transfer chunks until reaching budget or there is no more chunks to send (backup done, or severe error)
			while((!chunkBuilderFeed.IsCompleted)  && (s.Budget >0)){
				if(cancellationTokenSource.IsCancellationRequested){
					s.LoggerInstance.Log(Severity.INFO, "Received cancellation request for task #"+backup.TaskId+", stop processing...");
					s.TransfertDoneEvent -= new Session.TransfertDoneHandler(ManageChunkSent);
					s.SendDisconnect();
					s.Disconnect();
					return;
				}
				BChunk chunk = null;
				try{
					lock(processingChunks){
						chunk = chunkBuilderFeed.Take(cancellationTokenSource.Token);
						s.LoggerInstance.Log(Severity.DEBUG2, "Processing chunk "+chunk.Name);
						processingChunks.Add(chunk);
					}
					cp.Process(chunk, backup.MaxChunkSize);

					/*backup.OriginalSize += chunk.OriginalSize;
					backup.FinalSize += chunk.Size;
					backup.TotalChunks ++;
					backup.TotalItems += chunk.Files.Count;*/
					//if(chunk.Size > pipeline.HeaderLength)// an empty chunk doesn't count
					//		budget--;
					
					/// TODO replace waitone with a cancellationtoken-aware impl : http://msdn.microsoft.com/en-us/library/ee191552.aspx
					//if (chunk.SentEvent.WaitOne()){ // (60000, false)){
					chunk.SentEvent.Wait(cancellationTokenSource.Token);	
					s.LoggerInstance.Log(Severity.DEBUG2, "Processed  chunk "+chunk.Name+", remaining budget="+s.Budget);
						chunk.SentEvent.Dispose();
					/*}
					else{ // timeout waiting for storage node confirmation
						Logger.Append(Severity.WARNING, "Timeout waiting for storage node #"+s.ClientId+" ("+s.ClientIp+") confirmation, chunk "+chunk.Name);
						// TODO : but if have an error with one chunk, it's likely we will have one with next chunks too.
						//		close session now instead of continuing???
						try{
							chunkBuilderFeed.Add(chunk, cancellationTokenSource.Token);
						}
						catch(InvalidOperationException){
							Logger.Append(Severity.ERROR, "Timeout waiting for storage node #"+s.ClientId+" : A session error occured, unable to use a new session to process chunk (queue is closed)");	
							backup.AddHubNotificationEvent(811, chunk.Name, "Timeout waiting for storage node #"+s.ClientId+" : A session error occured, unable to use a new session to process chunk (queue is closed)");	
						}
					}*/
				}
				catch(System.Net.Sockets.SocketException e){
					// error sending to storage node. Re-add chunk to list and ask another storage session to hub.
					Console.WriteLine("############## Produce()	: TAKE refused for chunk "+chunk.Name+": "+e.Message+" ---- "+e.StackTrace);
					backup.AddHubNotificationEvent(811, chunk.Name, e.Message);
					if(chunk == null) return;
					RemoveChunk(chunk);
					//s.Disconnect();
					try{
						User.AskAlternateDestination(backup.TaskId, s.ClientId);
						chunkBuilderFeed.Add(chunk, cancellationTokenSource.Token);
					}
					catch(InvalidOperationException ioe){
						Logger.Append(Severity.ERROR, "A session error occured, unable to use a new session to process chunk (queue is closed) : "+ioe.Message);	
						backup.AddHubNotificationEvent(811, chunk.Name, "A session error occured, unable to use a new session to process chunk (queue is closed)");	
					}
					//throw new Exception("Something went wrong with this consumer");
				}
				catch(OperationCanceledException){
					Logger.Append(Severity.DEBUG2, "Consumer task has been manually cancelled on purpose, stopping...");
					s.TransfertDoneEvent -= new Session.TransfertDoneHandler(ManageChunkSent);
					return;
				}
				/*Logger.Append(Severity.INFO, "DataProcessorStreams statistics : checksum="+BenchmarkStats.Instance().ChecksumTime
				              +"ms, dedup="+BenchmarkStats.Instance().DedupTime
				              +"ms, compress="+BenchmarkStats.Instance().CompressTime
							  +"ms, send="+BenchmarkStats.Instance().SendTime+"ms.");*/
			}
			s.TransfertDoneEvent -= new Session.TransfertDoneHandler(ManageChunkSent);
			Logger.Append(Severity.DEBUG, "Session with node #"+s.ClientId+": processed and transferred all data chunks, unused budget="+s.Budget);
		}
		
		private void AfterTask(System.Threading.Tasks.Task task, Session s){
			if( (((!chunkBuilderFeed.IsCompleted) || chunkBuilderFeed.Count > 0 ) && !cancellationTokenSource.IsCancellationRequested)){
				Logger.Append(Severity.DEBUG, "Asking for another storage session because of expired budget with storage node #"+s.ClientId);
				User.SendPut(backup.TaskId, s.Id, 1);
				return;
			}
			
			//unregister storage session for normal chunks
			Logger.Append(Severity.DEBUG, "Data backup done, ready to process index.");
			User.StorageSessionReceivedEvent -= new User.StorageSessionReceivedHandler(this.SessionReceived);
			s.TransfertDoneEvent -= new Session.TransfertDoneHandler(ManageChunkSent);
			// gracefully disconnect from storage node
			s.SendDisconnect();
			task.Dispose();
			
			//20120729
			// if processing chunks is not empty, active sessions finishing transferrring remains, so do nothind
			/*if(processingChunks.Count > 0 && !cancellationTokenSource.IsCancellationRequested){
				string remainingC = "";
				foreach(BChunk c in processingChunks)
					remainingC += " "+c.Name;
				Logger.Append(Severity.INFO, processingChunks.Count+" active chunks ("+remainingC+")transfers remaining, not processing index yet...");
				return;
			}*/
			// stop indexer task
			
		}

		private void UnexpectedError(System.Threading.Tasks.Task task){
			var aggException = task.Exception.Flatten();
         	foreach(var exception in aggException.InnerExceptions){
				Logger.Append(Severity.CRITICAL, "Unexpected error while processing backup task "+this.backup.TaskId+" : "+exception.ToString());
				backup.AddHubNotificationEvent(999, exception.Message, "");
			}
			cancellationTokenSource.Cancel();
			//theSession.Disconnect();
			task.Dispose();
			//backup.Terminate(false);

			// rethrow to allow continuations to NOT be processed when they are NotOnFaulted
			throw new Exception("Propagating unexpected exception..."); 
			
		}

		private void ManageChunkSent(bool sent, long taskId, string chunkName, int destinationNode, int finalSize){
			if(!sent){
				Logger.Append(Severity.ERROR, "Chunk "+chunkName+" not sent - TODO : handle that");
				backup.AddHubNotificationEvent(999, "Chunk "+chunkName+" not sent - TODO : handle that", "");
				return;
			}
			lock(processingChunks){
				for(int i = processingChunks.Count-1; i>=0; i--){
					if(processingChunks[i].Name == chunkName){
						processingChunks[i].AddDestination(destinationNode);
						processingChunks[i].Size = finalSize;
						processingChunks[i].SentEvent.Set();
						//Console.WriteLine ("sent event set for chunk "+chunkName);
						backup.OriginalSize += processingChunks[i].OriginalSize;
						backup.FinalSize += processingChunks[i].Size;
						backup.TotalChunks ++;
						backup.TotalItems += processingChunks[i].Files.Count;
						
						indexerChunksFeed.Add(processingChunks[i], cancellationTokenSource.Token);
						processingChunks.RemoveAt(i);
						
						Logger.Append (Severity.DEBUG2, "Sent and indexed chunk "+chunkName);
					}
				}
				// to remove?
				//Thread.MemoryBarrier();
				if(chunkBuilderFeed.IsCompleted && processingChunks.Count == 0 /*&& indexerChunksFeed.Count ==0*/){

					Logger.Append (Severity.INFO, "/// done backuping and indexing data");
					indexerChunksFeed.CompleteAdding();
				}
				else {
					Logger.Append (Severity.INFO, " //// NOT done backuping and indexing data : processing count="+processingChunks.Count
					               +",indexerChunksFeed.Count="+indexerChunksFeed.Count+",chunkBuilderFeed.IsCompleted="+chunkBuilderFeed.IsCompleted);
				}
			}


		}
		
		private void DoIndex(){
			while( !indexerChunksFeed.IsCompleted || !cancellationTokenSource.IsCancellationRequested){
				try{
					BChunk toBeIndexed = indexerChunksFeed.Take(cancellationTokenSource.Token);
					backup.Index.AddChunk(toBeIndexed);
					Logger.Append(Severity.DEBUG2, "Added chunk "+toBeIndexed.Name+" to index");
				}
				catch(Exception e){
					if(e is OperationCanceledException)
						Logger.Append(Severity.DEBUG2, "Indexer has been manually cancelled on purpose, stopping...");
					else if(e is InvalidOperationException)
						Logger.Append(Severity.DEBUG2, "Indexer : no more chunks to index");
					else{
						Console.WriteLine ("////// unexpected DoIndex exception : "+e.ToString());
						throw;
					}
					return;
				}
			}
		}
		
		private void ProcessIndex(){
			//if(cancellationTokenSource.IsCancellationRequested) return;
				
			//indexProcessing = true;
			if(backup.DataFlags.HasFlag(DataProcessingFlags.CDedup)
			   /*	&& !cancellationTokenSource.IsCancellationRequested*/){ 
				// save dedup and process index even if task is cancelled (for cleaning purposes)
					try{
						DedupIndex.Instance().Persist();
					}
					catch(Exception _e){
						Logger.Append(Severity.ERROR, "Could not save deduplication indexes DB, backup data is therefore invalid. TODO: Report!!! : "+_e.Message+" ---- "+_e.StackTrace);
						backup.AddHubNotificationEvent(809, DedupIndex.Instance().IndexDBName,_e.Message);
					}
			}
			// now we have to send backup index and dedup index
			backup.Index.Terminate();
			//backup.Index = null;
			indexChunk = BuildIndexChunk();
		}
		
		
		private BChunk BuildIndexChunk(){
			backup.AddHubNotificationEvent(704, "","");
			BChunk iChunk = new BChunk(/*backup.TaskId, */backup.Index.FullName, backup.Index.FullName, backup.TaskId); //string name, int bsid, string bPath, string snapPath)
			try{
				//iChunk.Add(FileProvider.GetFile(index.FullName));

				iChunk.Add(new MinimalFsItem(backup.Index.FullName));
				if(backup.DataFlags.HasFlag(DataProcessingFlags.CDedup)) // backup the deduplication database
					iChunk.Add(ItemProvider.GetProvider().GetItemByPath(DedupIndex.Instance().IndexDBName));
				/*string sumHash;
				using(FileStream cksumFS = new FileStream(backup.Index.FullName, FileMode.Open, FileAccess.Read)){
					sumHash = BitConverter.ToString(SHA1.Create().ComputeHash(cksumFS));
					iChunk.Sum = sumHash;
				}*/
				iChunk.Sum = IndexManager.CheckSumIndex(backup.TaskId, (backup.Level != BackupLevel.Full));

				// register for session received, to process index transfer
				User.StorageSessionReceivedEvent += new User.StorageSessionReceivedHandler(this.SendIndex);
				User.AskIndexDest(backup.TaskId, backup.Index.Name, iChunk.Sum);
				Logger.Append(Severity.DEBUG, "Asked index destination to hub");
				return iChunk;
			}
			catch(Exception e){
				Logger.Append(Severity.ERROR, "Couldn't checksum index and/or ask destination to hub: "+e.Message+"---"+e.StackTrace);
				backup.AddHubNotificationEvent(808, e.Message,"");
			}
			return null;
		}
		
		private void SendIndex(long taskId, Session s/*, int budget*/){
			System.Threading.Tasks.Task consumeIndexTask = System.Threading.Tasks.Task.Factory.StartNew(() =>{
					DataPipeline pipeline = new DataPipeline(PipelineMode.Write, DataProcessingFlags.CCompress|DataProcessingFlags.CChecksum);
					//pipeline.Init();
					ChunkProcessor cp = new ChunkProcessor(s, pipeline, backup);
					cp.Process(indexChunk, backup.MaxChunkSize *10);
					indexChunk.Size = pipeline.Stream.Length;
					indexChunk.AddDestination(s.ClientId);
			}, TaskCreationOptions.LongRunning);

			consumeIndexTask.ContinueWith(o=>{
				Logger.Append(Severity.INFO, "Processed and sent backup index");
				backup.AddHubNotificationEvent(705, Math.Round((double)indexChunk.Size/1024/1024, 1).ToString(), "");

				string synthIndexSum = indexChunk.Sum; // for Fulls
				if(backup.Bs.ScheduleTimes[0].Level != P2PBackup.Common.BackupLevel.Full){
					IndexManager idxManager = new IndexManager();
					Logger.Append(Severity.INFO, "Building synthetic full index...");
					idxManager.CreateSyntheticFullIndex(backup.RefTaskId, taskId, backup.RootDrives);
					backup.AddHubNotificationEvent(707, "","");
					synthIndexSum = IndexManager.CheckSumIndex(taskId, false); // for synthetic backups
				}

				User.SendDoneBackup(taskId, backup.OriginalSize, backup.FinalSize, backup.TotalItems, indexChunk.Name, indexChunk.Sum, synthIndexSum, indexChunk.StorageDestinations, 100);
				Logger.Append(Severity.INFO, "Task "+taskId+" has finished. "+backup.TotalItems+" items, "+backup.TotalChunks+" chunks. Original data size="+Math.Round((double)backup.OriginalSize/1024/1024,1)+"MB, final="+Math.Round((double)backup.FinalSize/1024/1024,1)+"MB");
				string statsByKind = "Task "+taskId+" processed: ";
				for(int i=0; i<10; i++)
				 	statsByKind += backup.ItemsByType[i]+" "+((FileType)i).ToString()+", ";
				Logger.Append(Severity.INFO, statsByKind);
#if DEBUG
				Logger.Append(Severity.INFO, "DataProcessorStreams statistics : checksum="+BenchmarkStats.Instance().ChecksumTime
				              +"ms, dedup="+BenchmarkStats.Instance().DedupTime
				              +"ms, compress="+BenchmarkStats.Instance().CompressTime
							  +"ms, send="+BenchmarkStats.Instance().SendTime+"ms.");
				Logger.Append(Severity.INFO, "Dedup statistics : lookups="+BenchmarkStats.Instance().DedupLookups
				              +", hotfound="+BenchmarkStats.Instance().DedupHotFound
				              +", coldfound="+BenchmarkStats.Instance().DedupColdFound
							  +", add="+BenchmarkStats.Instance().DedupAdd+".");
#endif
				User.StorageSessionReceivedEvent -= new User.StorageSessionReceivedHandler(this.SendIndex);

				//Console.WriteLine("IndexSessionReceived() : backup typre="+backup. .BackupTimes[0].Type);

				backup.AddHubNotificationEvent(706, "","");
				backup.Terminate(true);

				BackupDoneEvent(taskId);
					
			}, TaskContinuationOptions.OnlyOnRanToCompletion|TaskContinuationOptions.ExecuteSynchronously
				|TaskContinuationOptions.NotOnFaulted|TaskContinuationOptions.NotOnCanceled);
			
			//consumeTask.Dispose();
		}
		
		
		private void IncrementSubCompletion(string newPath){
			backup.SubCompletion++;	
			backup.CurrentAction = newPath;
		}

		private void LogReceived(object sender, LogEventArgs args){
			backup.AddHubNotificationEvent(args.Code, args.Message, "");

		}

		private void RemoveChunk(BChunk chunkToRemove){
			lock(processingChunks){
				for(int i = processingChunks.Count-1; i>=0; i--)
					if(processingChunks[i].Name == chunkToRemove.Name)
						processingChunks.RemoveAt(i);
			}	
		}
	}

}

//using System.Collections.Generic; 
//using System.Linq; 
 /*
namespace System.Threading.Tasks.Schedulers 
{ 
    /// <summary>Provides a task scheduler that dedicates a thread per task.</summary> 
    public class ThreadPerTaskScheduler : TaskScheduler 
    { 
        /// <summary>Gets the tasks currently scheduled to this scheduler.</summary> 
        /// <remarks>This will always return an empty enumerable, as tasks are launched as soon as they're queued.</remarks> 
        protected override IEnumerable<Task> GetScheduledTasks() { return Enumerable.Empty<Task>(); } 
 
        /// <summary>Starts a new thread to process the provided task.</summary> 
        /// <param name="task">The task to be executed.</param> 
        protected override void QueueTask(Task task) 
        { 
            new Thread(() => TryExecuteTask(task)) {  }.Start(); 
        } 
 
        /// <summary>Runs the provided task on the current thread.</summary> 
        /// <param name="task">The task to be executed.</param> 
        /// <param name="taskWasPreviouslyQueued">Ignored.</param> 
        /// <returns>Whether the task could be executed on the current thread.</returns> 
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) 
        { 
			Console.WriteLine ("ThreadPerTaskScheduler: TryExecuteTaskInline");
            return TryExecuteTask(task); 
        } 
    } 
} */