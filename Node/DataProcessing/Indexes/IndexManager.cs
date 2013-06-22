using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using Node.Utilities;
using P2PBackup.Common;

namespace Node.DataProcessing{
	
	internal class IndexManager{
		Index taskIndex ;
		//private BChunk taskChunk;
		
		internal IndexManager (){
		}
		
		/*internal void CreateSyntheticFullIndex(long referenceTask, long task){
			Index refIndex = null;
			Index mergeIndex = null;
			try{
			refIndex = new Index();
			mergeIndex = new Index();
			mergeIndex.Create(task, true);
			refIndex.Open(referenceTask);
			Logger.Append(Severity.DEBUG, "Opened reference index "+referenceTask+"...");
			taskIndex.Open(task);
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
			foreach(IFile refItem in refIndex.GetItemsEnumerator()){
					//Console.WriteLine("CreateSFI() : reading chunk "+refChunk.Name+", "+refChunk.Files.Count+" items");
					//for(int i = refChunk.Files.Count-1; i >=0; i--){
					//foreach(IFile item in refChunk.Files){
						//Console.WriteLine("CreateSFI() : chunk "+refChunk.Name+", item "+item.OriginalFullPath+", type="+item.Kind);
					
					IFile searchedForItem = taskIndex.SearchItem(refItem);
					if(searchedForItem != null){
						Console.WriteLine ("CreateSFI() : item "+refItem.ID+", "+refItem.OriginalFullPath+ " is new");
					}
						
			}
					//if(refChunk.Files.Count > 0)
					//	mergeIndex.AddChunk(refChunk);
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
				
			
		}*/
		
		/// <summary>
		/// Creates a 'synthetic full' index given a reference (full/synth full) task and a new incr/diff task
		/// </summary>
		/// <param name='referenceTask'>
		/// The Reference task.
		/// </param>
		/// <param name='task'>
		/// The new incr/diff Task.
		/// </param>
		/// /// <param name='rootdrives'>
		/// The list of the rootdrives of the current backup
		/// </param>
		/// <returns>
		/// The synthetic index full path
		/// </returns>

		internal string CreateSyntheticFullIndex(long referenceTask, long task, List<BackupRootDrive> rootDrives){
			Logger.Append(Severity.TRIVIA, "CreateSyntheticFullIndex: initializing...");
			Index mergeIndex = null;
			Index refIndex = null; //new Index();
			try{
				mergeIndex = new Index(task, false);
				
				refIndex = new Index(referenceTask, false);
				refIndex.Open();
				//taskIndex.Open(task);
				taskIndex = new Index(task, true);
				taskIndex.Open();
				Logger.Append(Severity.DEBUG, "Opened reference index "+referenceTask+"...");
				if(refIndex.Header.TaskId != referenceTask){
					Logger.Append(Severity.ERROR, "Reference index doesn't handle expected task (wanted "+referenceTask+", got "+refIndex.Header.TaskId);
					return null;
				}
				// synthetic index will have the header of the just-ended task
				mergeIndex.Header = taskIndex.Header;
				//mergeIndex.Header.RootDrives = taskIndex.Header.RootDrives;

				mergeIndex.Create(/*task, false, */rootDrives);
				mergeIndex.WriteHeaders();
				// ask to perform the merge magic
				mergeIndex.MergeIndexes(referenceTask, task);
				mergeIndex.Terminate();
				refIndex.Terminate();
				taskIndex.Terminate();
				Logger.Append (Severity.TRIVIA, "Memory usage (before collect) : "+GC.GetTotalMemory(false)/1024);
				GC.Collect();
				Logger.Append (Severity.TRIVIA, "Memory usage (after collect) : "+GC.GetTotalMemory(false)/1024);
			}
			catch(Exception e){
				Logger.Append(Severity.ERROR, "Error creating synthetic full index : "+e.Message+" ---- "+e.StackTrace);
			}
			return mergeIndex.FullName;
		}
		
		//List<BChunk> actualTaskChunks;
		/*private IFile SearchItemInActualIndex(IFile entry){
			if(actualTaskChunks == null) actualTaskChunks = taskIndex.ReadAllChunks();
			foreach(BChunk taskChunk in actualTaskChunks){
				//if(entry.OriginalFullPath.IndexOf(taskChunk.RootDriveName) <0) continue;
				//Console.WriteLine ("SearchItemInActualIndex() : searching "+entry.OriginalFullPath+" in chunk "+taskChunk.Name+", chunk path="+taskChunk.RootDriveName);
				foreach(IFile chunkEntry in taskChunk.Files){
					//Console.WriteLine (entry.OriginalFullPath+" == "+chunkEntry.OriginalFullPath+" ??");
					if(	chunkEntry.OriginalFullPath == entry.OriginalFullPath)
						return chunkEntry;
				}
				
			}
			return null;
		}*/
		
		


		/// <summary>
		/// Checks if the index is locally available, and if its checksum matches.
		/// </summary>
		/// <returns>
		/// <c>true</c> if these 2 conditions are met. 
		/// </returns>
		/// <param name='taskId'>
		/// If set to <c>true</c> task identifier.
		/// </param>
		/// <param name='checksum'>
		/// If set to <c>true</c> checksum.
		/// </param>
		/// <param name='isPartial'>
		/// If set to <c>true</c> is partial.
		/// </param>
		internal static bool IsIndexPresentAndValid(long taskId, string checksum, bool isPartial){
			Index wantedIndex;
			//try{
				wantedIndex = new Index(taskId, isPartial);

				//wantedIndex.Terminate();
			if(!wantedIndex.Exists()){
				Logger.Append (Severity.INFO, "The requested index for task '"+taskId+"' is not available locally");
				return false;
			}
			//using(FileStream cksumFS = new FileStream(wantedIndex.FullName, FileMode.Open, FileAccess.Read)){
					//string sumHash = BitConverter.ToString(SHA1.Create().ComputeHash(cksumFS));
					if( CheckSumIndex(taskId, isPartial) != checksum){
						Logger.Append (Severity.WARNING, "The requested index '"+taskId+"' local copy has been altered or corrupted.");
					return false;
					}
			//}
			Logger.Append (Severity.INFO, "The requested index for task '"+taskId+"' has been successfully checked against alteration and corruption.");
			return true;
		}

		internal static string CheckSumIndex(long taskId, bool isPartial){
			Index wantedIndex;
			try{
				wantedIndex = new Index(taskId, isPartial);
				Logger.Append(Severity.DEBUG, "About to checksum index "+wantedIndex.Name);
			}
			catch{
				Logger.Append (Severity.INFO, "The requested index for task '"+taskId+"' is not available locally");
				return null;
			}
			using(FileStream cksumFS = new FileStream(wantedIndex.FullName, FileMode.Open, FileAccess.Read)){
				return BitConverter.ToString(SHA1.Create().ComputeHash(cksumFS));
			}
		}
		/*internal static void DeleteOldIndexes(int retentionDays, bool synthetic){
			try{
				string indexFolder = Utilities.ConfigManager.GetValue("Backups.IndexFolder");

			}
			catch(Exception e){

			}
		}*/


		
	}
}

