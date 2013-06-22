using System;
using System.IO;
using System.Collections.Generic;
using P2PBackup.Common;
using Node.Utilities;
using Node.DataProcessing;

namespace Node{
	/// <summary>
	/// A backup's processing is split into BackupRootDriveHandler, one for each device involved in backup.
	/// This allow backuping using snapshots, and backup parallelism (processing multiple drives a tonce) if required.
	///   Having a per-device based parallelism helps maximizing throughput.
	/// </summary>
	internal class BackupRootDriveHandler	: IDisposable{
		
		private  IEnumerator<IFSEntry> itemIterator = null;
		//private IEnumerable<IFile> itemIterator = null;
		private BasePath currentPath;
		private string snapshottedPath;
		private long maxChunkSize;
		private long maxPackSize;
		private int maxChunkFiles;
		// max item ID (inode/NTFS object id), to allow easier new files detection during incr/diff
		internal long MaxItemID{get;private set;}
		internal long TaskId{get;private set;}
		private int nbItems;
		private int depth;
		private int completionBase;
		private int chunkOrder = 0;
		private int subCompletionNb = 1;
		private long refStartDate, refEndDate;
		private BackupLevel backupType;
		private BackupRootDrive backupRootDrive;
		private IFileProvider prov;
		internal delegate void SubCompletionHandler(string path);
		internal event SubCompletionHandler SubCompletionEvent;
		internal delegate void LogHandler(int code, Severity severity, string message);
		internal event EventHandler<LogEventArgs> LogEvent;


		private System.Collections.IEnumerable fsprov;
		private SearchPattern includeSp;
		private SearchPattern excludeSp;
		private long referenceTaskid;
		IFSEnumeratorProvider fsEnumerator;

		internal int SubCompletionNb{
			get{return subCompletionNb;}
		}


		~BackupRootDriveHandler(){
			Logger.Append(Severity.TRIVIA, "<TRACE> BackupRootDriveHandler destroyed.");
		}
		
		internal BackupRootDriveHandler(BackupRootDrive brd, long taskId, long maxChkSize, long maxPack, int maxChunkFiles, P2PBackup.Common.BackupLevel bType, long refStartDate, long refEndDate, long referenceTaskid/*, long refMaxId*/){
			backupRootDrive = brd;
			maxChunkSize = maxChkSize;
			maxPackSize = maxPack;
			this.maxChunkFiles = maxChunkFiles;
			nbItems = 0;
			this.backupType = bType;
			this.depth = 0;
			this.subCompletionNb = 0;
			this.completionBase = 0;
			this.refStartDate = refStartDate;
			this.refEndDate = refEndDate;
			this.referenceTaskid = referenceTaskid;
			this.MaxItemID = 0;//refMaxId;
			this.TaskId = taskId;
			//if(path.Type ==  BasePath.PathType.FS)
				//itemIterator = GetFilesToBackup().GetEnumerator();
			prov = ItemProvider.GetProvider();
			fsEnumerator = FSEnumeratorProvider.GetFSEnumeratorProvider();

		}

		internal void SetCurrentPath(BasePath bp){
			//foreach(BasePath path in this.backupRootDrive.paths){
			var pathSnapshot = backupRootDrive.Snapshot;
			//Console.WriteLine("SetCurrentPath() : basepath="+bp.Path+",pathSnapshot.Name="+pathSnapshot.Name+", pathSnapshot.Path="+pathSnapshot.Path);
			//currentPath = bp.Path;
			currentPath = bp;

			// handle unix special case of '/'
			if(pathSnapshot.Path == "/" /*&& (this.backupRootDrive.SystemDrive.MountPoint == this.backupRootDrive.SystemDrive.OriginalMountPoint)*/ )
				snapshottedPath = pathSnapshot.MountPoint + bp.Path;
			else{
				if(!pathSnapshot.MountPoint.EndsWith(""+Path.DirectorySeparatorChar))
					pathSnapshot.MountPoint += Path.DirectorySeparatorChar;
				snapshottedPath = bp.Path.Replace(pathSnapshot.Path, pathSnapshot.MountPoint/*+Path.DirectorySeparatorChar*/);
			}
			Logger.Append(Severity.TRIVIA, "Handler for FS "+backupRootDrive.Snapshot.MountPoint+" : Path "+currentPath.Path+" replaced by "+snapshottedPath+" in "+bp.Path);
			fsprov = FSEnumeratorProvider.GetFSEnumeratorProvider().GetFSEnumerator(snapshottedPath);

			if(itemIterator == null) // initialize on first Basepath. For next Basepaths, Calling SetCurrentPath() is enough
				itemIterator = GetFilesToBackup().GetEnumerator();


			//Console.WriteLine("SetCurrentPath() : got enumerator for "+snapshottedPath);
			includeSp = new SearchPattern(bp.IncludePolicy, true);
			excludeSp = new SearchPattern(bp.ExcludePolicy, false);
			//Console.WriteLine("SetCurrentPath() : got search pattern"+Environment.NewLine);
		}
		

		internal IEnumerable<BChunk> GetNextChunk(){
			chunkOrder++;
			BChunk chunk = new BChunk(this.TaskId);
			chunk.Order = chunkOrder;
			chunk.RootDriveId = this.backupRootDrive.ID;
			uint filePosInChunk = 0;
			long currentSize = 0;

			while(itemIterator.MoveNext()){
			//foreach(IFSEntry ent in GetFilesToBackup()){

				IFSEntry ent = itemIterator.Current;
				if(ent == null) continue;


				// 1st case : we can add more files to the chunk
				if(ent.FileSize < maxChunkSize){
					//try{
						filePosInChunk += (uint)ent.FileSize;
						//IFSEntry f = ent;
						ent.ChunkStartPos = filePosInChunk;
						ent.FileStartPos = 0;
						chunk.Add(ent);
						currentSize += ent.FileSize;
						//Console.WriteLine("GetNextChunk() : added new file to chunk - "+itemIterator.Current.FileName);
					//}
					//catch(Exception e){
					//	Logger.Append(Severity.ERROR, "Could not add file "+itemIterator.Current.SnapFullPath+" : "+e.Message);
					//}
				}
				//2nd case : a file is too big to fit into one chunk, split it
				else{ 
					if(chunk.Items.Count >0) yield return chunk;
					/*chunk = new BChunk(currentPath.Path, snapshottedPath, this.TaskId);
					chunk.Order = chunkOrder;
					chunk.RootDriveId = this.backupRootDrive.ID;
					filePosInChunk = 0;
					currentSize = 0;*/
					foreach(BChunk bigFileChunk in GetBigFileChunks(itemIterator.Current, filePosInChunk))
						yield return bigFileChunk;
					chunkOrder++;
					chunk = new BChunk(this.TaskId);
					chunk.Order = chunkOrder;
					chunk.RootDriveId = this.backupRootDrive.ID;
					filePosInChunk = 0;
					currentSize = 0;
				}
				// 3rd case : if a chunk reaches its max packSize, we create another one
				if(currentSize > maxChunkSize || chunk.Items.Count > 0 && currentSize > maxPackSize 
					   	/*|| currentSize == 0 && chunk.Files.Count ==0 */
				   		||  chunk.Items.Count > maxChunkFiles){
					
					//Console.WriteLine("GetNextChunk() : chunk reached max files or max size:currentsize="+currentSize+"/"+maxChunkSize
					//          +",chunkfilescount="+chunk.Files.Count+"/"+maxChunkFiles);
					yield return chunk;
					chunkOrder++;
					chunk = new BChunk(this.TaskId);
					chunk.Order = chunkOrder;
					chunk.RootDriveId = this.backupRootDrive.ID;
					filePosInChunk = 0;
					currentSize = 0;
				}
			}
			//4th case : // done processing file list but chunk not complete
			Logger.Append (Severity.TRIVIA, "GetNextChunk() : Done gathering files inside '"+snapshottedPath+"' without reaching chunk max size. "+ chunk.Items.Count+" files, "+currentSize/1024+"k");
			//if(currentSize > 0){ 
				yield return chunk;
			//itemIterator = GetFilesToBackup().GetEnumerator();
			//yield break;
			//}
			
		}
		
		// splits a big file ( size > maxchunkfile) into multiple chunks
		private List<BChunk> GetBigFileChunks(IFSEntry bigFile, long filePosInChunk){

			long pos = 0;
			long remaining = bigFile.FileSize;
			List<BChunk> chunks = new List<BChunk>();
			while(remaining > 0){
				chunkOrder++;
				IFSEntry f = bigFile.Clone();
				BChunk chunk = new BChunk(this.TaskId);
				chunk.Order = chunkOrder;
				chunk.RootDriveId = this.backupRootDrive.ID;
				f.ChunkStartPos = 0;
				chunk.Add(f);
				chunks.Add(chunk);
				f.FileStartPos = pos;
				if(remaining > maxChunkSize)
					pos += maxChunkSize;
				else
					pos += remaining;
				remaining = bigFile.FileSize - pos;
				
				//if(remaining <0)
				Logger.Append (Severity.TRIVIA, "GetNextChunk() : splitted file "+f.SnapFullPath+" (size "+f.FileSize+") , remaining="+remaining+" to chunk - "+chunk.Name+" starting @ offset "+f.FileStartPos);
				
			}
			return chunks;
		}



		internal IEnumerable<IFSEntry> GetFilesToBackup(/*string currentPath, int depth*/){

			IEnumerable<Node.IFSEntry> enumerable = null;
			if(backupType == BackupLevel.Full || backupType == BackupLevel.Default)
				enumerable = GetFull(true);
			else if(backupType == BackupLevel.Refresh ){
				//try{
					enumerable = GetIncremental();
				//}
				//catch{ //Unable to find a suitable IncrementalPluginProvider BtrfsProvider, defaulting to full
				//	enumerable = GetFull(true);
				//}
			}
			/*else if(backupType == BackupLevel.SyntheticFull)
				enumerable = GetIncremental(true);*/

			foreach(IFSEntry entry in enumerable){
				//Console.WriteLine (" * ** * *  * * * * ** * * entry 1 : "+entry.Name);
				//Console.WriteLine (" * ** * *  * * * * ** * * entry 2: match="+sp.Matches(entry.Name));
				if( (entry.Kind != FileType.Directory && entry.Kind != FileType.MountPoint 
					&& entry.Kind != FileType.Hardlink)
				    && !includeSp.Matches(entry.Name))

				{
					Console.WriteLine (" * **BRDH GetFilesToBackup *  * * entry DOES NOT MATCH include rule");
					continue;
				}

				// check if entry hasn't to be excluded
				if(excludeSp.Matches(entry.Name)) {
					Console.WriteLine (" * **BRDH GetFilesToBackup *  * * entry DOES MATCH exclude rule");
					continue;
				}
				if(entry.ChangeStatus != DataLayoutInfos.Deleted)
					entry.OriginalFullPath = entry.SnapFullPath.Replace(backupRootDrive.Snapshot.MountPoint, backupRootDrive.Snapshot.Path);
				yield return entry;
				nbItems++;
			}
			//Console.WriteLine (" * ** * *  * BRDH GetFilesToBackup DONE END MEHOD");
		}


		long parentId=-1;
		internal IEnumerable<IFSEntry> GetFull(bool rootCall){

			if(snapshottedPath == null)
				throw new ArgumentNullException("Current path is null, ensure SetCurrentPath() has been called");
			bool exclude = false;

			// before enumerating sub-entries, add root path itself to the backup
			if(rootCall){
				IFSEntry basePathEntry = null;
				try{
					Console.WriteLine ("brdh GetFilesToBackup() 1: rootCall entry. snapshottedPath="+snapshottedPath);
					basePathEntry =  prov.GetItemByPath(snapshottedPath);
					parentId = basePathEntry.ID;
					Console.WriteLine ("brdh GetFilesToBackup() 2: rootCall entry ID="+basePathEntry.ID);
				}
				catch(FileNotFoundException fnf){
					Logger.Append (Severity.WARNING, "Basepath '"+snapshottedPath+"' doesn't exist ("+fnf.Message+")");
					if(LogEvent != null) LogEvent(this, new LogEventArgs(911, Severity.WARNING, snapshottedPath));

				}
				catch(Exception e){
					Logger.Append (Severity.WARNING, "Basepath '"+snapshottedPath+"' couldn't be opened ("+e.Message+")");
					if(LogEvent != null) LogEvent(this, new LogEventArgs(911, Severity.WARNING, snapshottedPath));
					/*Logger.Append (Severity.ERROR, "Cannot open basepath '"+snapshottedPath+"' : "+e.Message);
					throw;*/
				}
				if(basePathEntry != null) yield return basePathEntry;
			}
			//Console.WriteLine ("brdh GetFilesToBackup() 3: before GetFSEnumerator ");
			fsprov = fsEnumerator.GetFSEnumerator(snapshottedPath);
			//Console.WriteLine ("brdh GetFilesToBackup() 4: after GetFSEnumerator");
			foreach(var backupItem in fsprov){
				IFSEntry entry = null;
				try{
					entry = prov.GetItem(backupItem);
					//if(!rootCall)
					entry.ParentID = parentId;// WRONG!
					if(entry == null) continue;
				}
				catch(Exception e){// permission errors, deleted file...
					try{
						Logger.Append(Severity.WARNING, "Could not add element '"+entry.SnapFullPath+"' to backup : "+e.Message);
						if(LogEvent != null) LogEvent(this, new LogEventArgs(912, Severity.WARNING, entry.SnapFullPath));
					}
					catch(Exception){
						Logger.Append(Severity.WARNING, "Could not add element (with unknown name) in folder "+snapshottedPath+" to backup : "+e.Message);
						if(LogEvent != null) LogEvent(this, new LogEventArgs(912, Severity.WARNING, "<UNKNOWN>"));
					}
					continue;
				}
				if(entry.Kind == FileType.Directory){
					//parentId = entry.ID;
					//if(depth <2) Console.WriteLine ("GetFull() : entering "+entry.SnapshottedFullPath);
					for(int i = currentPath.ExcludedPaths.Count-1; i>=0; i--){
						//Console.WriteLine ("GetFull() : checking if excluded path '"+currentPath.ExcludedPaths[i]+"' matches...");
						if(entry.SnapFullPath.IndexOf(currentPath.ExcludedPaths[i]) == 0){
							Logger.Append (Severity.INFO, "Ignoring path "+entry.SnapFullPath);
							currentPath.ExcludedPaths.RemoveAt(i);
							exclude = true;
							//yield return entry;
							break;
						}
					}
					depth++;
					if(depth == 2){
						//if(SubCompletionEvent != null) SubCompletionEvent(entry.SnapFullPath.Replace(snapshottedPath, currentPath.Path));
						if(SubCompletionEvent != null) SubCompletionEvent(currentPath.Path);
						//entry.FileName.Replace(snapshottedPath, currentPath
					}
					snapshottedPath = entry.SnapFullPath;
					if(!exclude)
						foreach(IFSEntry e in GetFull(false)) {
							//e.ParentID = parentId;
							e.ParentID =  entry.ID;
							yield return e;	
						}
					depth--;
				}
				//entry.ParentID = parentId;
				yield return entry;
				exclude = false;
			}
			yield break;
		}
		
		/*private IEnumerable<IFile> GetFull_old(){
			foreach(string f in Directory.EnumerateFileSystemEntries(snapshottedPath, "*", SearchOption.TopDirectoryOnly)){
				IFile entry = null;
				//Console.WriteLine("GetFull() : "+f);
				try{
					entry = FileProvider.GetFile(f);
				}
				catch(Exception e){// permission errors, deleted file...
					Logger.Append(Severity.WARNING, "Could not add element '"+f+"' to backup : "+e.Message);
					Logger.Append(Severity.INFO, "TODO : report exception to hub for task logentry");
				}
				if(entry.Kind == FileType.Directory){
					depth++;
					snapshottedPath = f;
					foreach(IFile e in GetFull()) //GetFilesToBackup())
						yield return e;
					
					if(depth == 1){
						if(SubCompletionEvent != null) SubCompletionEvent(entry.FileName.Replace(snapshottedPath, currentPath));
						//entry.FileName.Replace(snapshottedPath, currentPath
					}
					depth--;
					//else
					//	Console.WriteLine ("##### depth="+depth);
						//subCompletionNb++;
					
				}
				
					//Console.WriteLine("GetFilesToBackup() : "+f);
				yield return entry;
			}
		}*/
		
		private IEnumerable<Node.IFSEntry> GetIncremental(){
			// open reference index
			//IndexManager im = new IndexManager();
			Index refIdx = new Index(referenceTaskid, false);
			refIdx.Open();
			// set previous chunk max id in order to not overlap ids
			chunkOrder = refIdx.GetMaxChunkId();
			Dictionary<string, byte[]> dict = refIdx.GetProviderMetadata(this.backupRootDrive.SystemDrive.OriginalMountPoint);
			IIncrementalProvider incrProv = null;
			/*if(isFullRefresh){
				incrProv = new FileCompareProvider(this.TaskId, this.backupRootDrive);
			}
			else{*/
				// get the most efficient (higher priority) incrementalprovider for the current drive, passing it the reference backup's metadata	
				incrProv = IncrementalPluginProvider.GetProviderByPriority(this.backupRootDrive, dict);
			//}
			this.backupRootDrive.IncrementalPlugin = incrProv;
			if(incrProv != null)
				Logger.Append(Severity.INFO, "Chose Incremental/Differential provider : "+incrProv.GetType().ToString()+", priority: "+incrProv.Priority);
			else{ // no reference data for any provider, unable to perform incr, default to full
				Logger.Append(Severity.ERROR, "Unable to select any of the Incremental providers for FS "+this.backupRootDrive.SystemDrive.MountPoint);
				//yield return GetFull();
				throw new Exception("Unable to select any of the Incr providers for FS "+this.backupRootDrive.SystemDrive.MountPoint);
			}
			refIdx.Terminate();
			bool exclude = false;

			foreach(IFSEntry entry in incrProv.GetNextEntry(currentPath, snapshottedPath)){

				// don't try to match include/exclude rules if file was deleted
				if(entry.ChangeStatus == DataLayoutInfos.Deleted){
					yield return entry;
					continue;
				}
				//Console.WriteLine (DateTime.Now.TimeOfDay+"  @@@ GetIncremental() : got entry "+entry.Name);
				//if(entry.Kind == FileType.Directory){
					if(entry.SnapFullPath == null){
						Logger.Append(Severity.NOTICE, "Got null entry for '"+entry.Name+"' from provider '"+entry.GetType()+"' while collecting in incremental mode, this is unexpected");
						continue;
					}
					// TODO : report completionpercent if possible


				exclude = false;
					//if(depth <2) Console.WriteLine ("GetFull() : entering "+entry.SnapshottedFullPath);
					for(int i = currentPath.ExcludedPaths.Count-1; i>=0; i--){
						if(entry.SnapFullPath.IndexOf(currentPath.ExcludedPaths[i]) == 0){
							Logger.Append (Severity.DEBUG, "Exclusion rule '"+currentPath.ExcludedPaths[i]+"' : ignoring entry "+entry.SnapFullPath);
							//currentPath.ExcludedPaths.RemoveAt(i);
							exclude = true;
							//yield return entry;
							break;
						}
					}
				//}
				if(!exclude && entry != null){
					Console.WriteLine ("  /////// return incr entry : "+entry.ToString());
					yield return entry;
				}
			}
		}
		
		public void Dispose(){
			Logger.Append (Severity.DEBUG, "Disposing BackupRootdrive handler...");
			fsEnumerator.Dispose();

		}
	}


}

