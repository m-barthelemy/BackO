using System;
using System.Collections.Generic;
using Node.Utilities;
using P2PBackup.Common;

namespace Node.DataProcessing{
	internal class IndexManager_old{
		BackupIndex taskIndex = new BackupIndex();
		//private BChunk taskChunk;
		
		internal IndexManager_old (){
		}
		
		internal void CreateSyntheticFullIndex(long referenceTask, long task){
			BackupIndex refIndex = null;
			BackupIndex mergeIndex = null;
			try{
			//Console.WriteLine ("CreateSyntheticFullIndex(): 1");
			refIndex = new BackupIndex();
			//Console.WriteLine ("CreateSyntheticFullIndex(): 2");
			mergeIndex = new BackupIndex(task);
			//Console.WriteLine ("CreateSyntheticFullIndex(): 3");
			refIndex.OpenByTaskId(referenceTask);
			Logger.Append(Severity.DEBUG, "Opened reference index "+referenceTask+"...");
			//Console.WriteLine ("CreateSyntheticFullIndex(): 4");
			taskIndex.OpenByTaskId(task);
			Console.WriteLine ("CreateSyntheticFullIndex() : opened indexes");
			if(refIndex.Header.TaskId != referenceTask){
				Logger.Append(Severity.ERROR, "Reference index doesn't handle expected task (wanted "+referenceTask+", got "+refIndex.Header.TaskId);
				return;
			}
			// synthetic index will have the header of the just-ended task
			mergeIndex.Header = taskIndex.Header;
			mergeIndex.WriteHeaders();
			BChunk refChunk;
			//taskChunk = taskIndex.ReadChunk();
			// walk the reference (synthetic) index and merge changes from the new backup index.
			while( (refChunk = refIndex.ReadChunk()) != null){
					Console.WriteLine("CreateSFI() : reading chunk "+refChunk.Name+", "+refChunk.Files.Count+" items");
					for(int i = refChunk.Files.Count-1; i >=0; i--){
					//foreach(IFile item in refChunk.Files){
						//Console.WriteLine("CreateSFI() : chunk "+refChunk.Name+", item "+item.OriginalFullPath+", type="+item.Kind);
						IFSEntry newEntry = SearchItemInActualIndex(refChunk.Files[i]);
						if(newEntry != null){
							Console.WriteLine("CreateSFI() : found updated entry "+newEntry.OriginalFullPath);
							refChunk.Files.RemoveAt(i);
						}
					}
					if(refChunk.Files.Count > 0)
						mergeIndex.AddChunk(refChunk);
			}
			foreach(BChunk newChunk in actualTaskChunks)
					mergeIndex.AddChunk(newChunk);
			
			}
			catch(Exception e){
				Logger.Append(Severity.ERROR, "Error creating synthetic full index : "+e.Message+" ---- "+e.StackTrace);
			}
			finally{
				try{
					refIndex.Terminate();
					taskIndex.Terminate();
					mergeIndex.Terminate();	
				}
				catch(Exception e){
					// harmless for backup and indexes consistency, but will leave open files descriptors.
					// However this case should not happen in real-life
					Logger.Append(Severity.ERROR, "Error closing indexes : "+e.Message);	
				}
			}
				
			
		}
		
		List<BChunk> actualTaskChunks;
		private IFSEntry SearchItemInActualIndex(IFSEntry entry){
			if(actualTaskChunks == null) actualTaskChunks = taskIndex.ReadAllChunks();
			foreach(BChunk taskChunk in actualTaskChunks){
				//if(entry.OriginalFullPath.IndexOf(taskChunk.RootDriveName) <0) continue;
				//Console.WriteLine ("SearchItemInActualIndex() : searching "+entry.OriginalFullPath+" in chunk "+taskChunk.Name+", chunk path="+taskChunk.RootDriveName);
				foreach(IFSEntry chunkEntry in taskChunk.Files){
					//Console.WriteLine (entry.OriginalFullPath+" == "+chunkEntry.OriginalFullPath+" ??");
					if(	chunkEntry.OriginalFullPath == entry.OriginalFullPath)
						return chunkEntry;
				}
				
			}
			return null;
		}
		
		
		internal BackupIndex GetFullIndex(long refTaskId){
			BackupIndex bi = null;
			try{
				bi.OpenByName("s"+refTaskId+".idx");
			}
			catch(System.IO.FileNotFoundException){ // synthetic not found, ref backup might be a full, let's try
				try{
					bi.OpenByTaskId(refTaskId);
					if(bi.Header.BackupType == P2PBackup.Common.BackupType.Full)
						return bi;
					else{ // ref task was not a full and no synthetic full, re-generate a synthetic
						Logger.Append(Severity.WARNING, "Could not find reference task synthetic index, will try to re-create...");
						
						
					}	
						
				}
				catch(System.IO.FileNotFoundException){//INDEX not found. ask to hub and retrieve from storage node
					Logger.Append(Severity.WARNING, "Could not find synthetic nor partial indexes, will try to retrieve them");
				}
				
				
			}
			return null;
		}
		
	}
}

