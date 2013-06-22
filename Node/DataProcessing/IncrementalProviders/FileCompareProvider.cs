using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using Node.Utilities;
using P2PBackup.Common;

namespace Node.DataProcessing{
	
	/// <summary>
	/// This is a portable, generic (and, in most situations, slowest) incremental provider.
	/// it uses the reference task index to detect renamed and removed items,
	/// the reference backup start time to detect modified items,
	/// the reference backup Max item ID to detect new files, or reference index if new files got an ID lower than reference maxId
	///  Because of its general slowness and a possible lack of accuracy in some (rare) corner-cases,
	///  this provider has the lowest priority.
	/// Its main advantage is that it is the most platform and filesystem independant provider.
	/// </summary>
	public class FileCompareProvider:IIncrementalProvider{
		private long refBackupTimeStart;
		private long refBackupTimeEnd;
		private long backupTimeStart;
		private long refMaxId;
		private long refTaskId;
		private long taskId;
		
		private BackupRootDrive rootDrive;
		private Index refIndex;
		private BackupRootDriveHandler phantomBrd;
		
		private IFileProvider prov;
		private System.Collections.IEnumerable fsProv;
		private IEnumerator<IFSEntry> idxEnumerator;

		// items IDs when current fs id does not match ref index enumerator id
		private List<long> idsToWatch;
		private bool isFullRefreshBackup = false;

		public delegate void SubCompletionHandler(string path);
		public event SubCompletionHandler SubCompletionEvent;

		public short Priority{
			get{return 1;}
		}

		public string Name{
			get{
				return "FileCompareProvider";
			}
		}

		public bool IsEnabled{get;set;}
		
		internal FileCompareProvider(long task, BackupRootDrive rd){
			this.rootDrive = rd;
			this.taskId = task;
			prov = ItemProvider.GetProvider();
			//Logger.Append(Severity.INFO, "Gathering changed items since "+refBackupStart.ToString());
			//depth = 0;
			/*Index refIndex = new Index();
			refIndex.Open(refTaskId);
			refMaxId = refIndex.GetMaxId(rd.systemDrive.MountPoint);
			refTaskEnumerator = refIndex.GetItemsEnumerator(rd.systemDrive.MountPoint).GetEnumerator();*/
		}

		internal FileCompareProvider(long task, BackupRootDrive rd, bool isFullRefreshBackup)
			:this(task, rd){
			this.isFullRefreshBackup = isFullRefreshBackup;
		}
		/// <summary>
		/// Checks if we have information from the reference backup task
		/// </summary>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		public bool CheckCapability(){
			if(refBackupTimeStart == 0 /*|| refBackupTimeEnd == long.MaxValue || refMaxId == 0 */|| refTaskId == 0){
				Logger.Append(Severity.DEBUG, "FileCompare incr provider : some parameters are missing, provider is thus unusable");
				return false;
			}
			refIndex = new Index(refTaskId, false);
			refIndex.Open();
			refMaxId = refIndex.GetMaxId(this.rootDrive.SystemDrive.OriginalMountPoint);
			Logger.Append(Severity.DEBUG2, "Incremental ref max item id="+refMaxId);
			//idxProv = refIndex.GetItemsEnumerator(rootDrive.systemDrive.MountPoint);
			idxEnumerator = refIndex.GetBaseItemsEnumerator(rootDrive.SystemDrive.OriginalMountPoint, 0).GetEnumerator();
			return true;
		}
		
		/// <summary>
		/// Gets the next incremental/diff entry to backup. To efficiently reuse existing code, we instanciate
		/// a "phantom" BackupRootDriveHandler an use its "GetFull()" FS items provider as an items source to check
		/// for modifications.
		/// </summary>
		/// <returns>
		/// and IFSEntry
		/// </returns>
		/// <param name='path'>
		/// Path.
		/// </param>
		/// <param name='snapshottedPath'>
		/// Snapshotted path.
		/// </param>
		public IEnumerable<IFSEntry> GetNextEntry(BasePath path, string snapshottedPath){

			idsToWatch = new List<long>();
			fsProv = FSEnumeratorProvider.GetFSEnumeratorProvider().GetFSEnumerator(snapshottedPath);
			phantomBrd = new BackupRootDriveHandler(rootDrive, this.taskId, 0, 0, 0, BackupLevel.Full, 0, 0, 0);
			phantomBrd.SetCurrentPath(path);
			phantomBrd.SubCompletionEvent += new BackupRootDriveHandler.SubCompletionHandler(BubbleUpSubCompletion);

			bool moveRefEnumerator = true;
			IFSEntry refEntry = null;
			IFSEntry realRefEntry = null;

			foreach(IFSEntry entry in phantomBrd.GetFull(true)){

				Console.WriteLine ("PhantomBrd current entry : "+entry.Name);

				// New File (inode/id > last refMaxId known inode/id)
				if(entry.ID > refMaxId /*|| entry.CreateTime > refBackupTimeStart*/){ 
					Console.WriteLine("Incremental GetFilesToBackup() added NEW entry : "+entry.SnapFullPath);
					yield return entry;	
					continue;
				}

				try{ //TOREMOVE
				if(moveRefEnumerator || refEntry == null){
					if(idxEnumerator.MoveNext()){ // try to position to the same entry, previously backuped
						/*if(refEntry != null && refEntry.ID == idxEnumerator.Current.ID){
							while(idxEnumerator.Current.ID == refEntry.ID) // in case we meet BigFiles (multiple same id entries), loop.
							if(!idxEnumerator.MoveNext())
								break;
						}*/
						//else
						refEntry = idxEnumerator.Current;
						Console.WriteLine ("Ref entry : "+refEntry.Name);
					}
					else
						Console.WriteLine("CANNOT MoveNext() -- last entry is "+refEntry.Name);
					
					//firstMove = false;
				}
				else{
					moveRefEnumerator = true;
						Console.WriteLine("moveRef = false");
				}
				}
				catch(Exception e){ // TOREMOVE
					Console.WriteLine ("Incremental GetFilesToBackup()   ERROR : "+e.ToString());
				}

				//Console.WriteLine("Incremental GetFilesToBackup() ------- Refentry id="+refEntry.ID+", name="+refEntry.Name+", curid="+entry.ID+", curname="+entry.Name);
				//Console.WriteLine ("RefEntry: "+refEntry.ToString()+", curEntry: "+entry.ToString());
				//if(refEntry.ID == entry.ID)
				//	Console.WriteLine("Incremental GetFilesToBackup() entry "+entry.SnapFullPath+" __MATCHED__ in ref backup");


				if(refEntry.ID != entry.ID){
					Console.WriteLine("Incremental cur entry "+entry.ToString()+" DOESN'T_MATCH ref = "+refEntry.ToString());
					long refItemPos = 0;

					// new file potentially reusing an inode/ID number < ref max ID
					if(entry.CreateTime > refBackupTimeStart || entry.LastModifiedTime > refBackupTimeStart){
						moveRefEnumerator = false;
						yield return entry;
						continue;
					}

					//other cases : File RenamedOrMovedItem RenamedOrMovedItem deleted
					realRefEntry = refIndex.SearchItem(entry, rootDrive.SystemDrive.MountPoint, out refItemPos);
					if(realRefEntry == null){
						moveRefEnumerator = false;
						yield return entry;
						continue;
					}
					// check if entry has been moved from outside to the current dir
					else{
						refEntry = realRefEntry;
						if(refEntry.ParentID != entry.ParentID){ // moved
							moveRefEnumerator = false;
							//entry.ChangeStatus = 
							//continue;
						}

					}

					/*// Check if entry is a newly created file (ctime > last backup start time) but with an ''old'' mtime
					if(refIndex.SearchItem(entry, rootDrive.SystemDrive.MountPoint, out searchRowid) == null
						&& entry.LastMetadataModifiedTime > refBackupTimeStart){
						Console.WriteLine("Found (new?) entry with reused ID : "+entry.ToString());

					}*/

					
					


					// else check if refEntry has been deleted
					/*else{ 
						Console.WriteLine("Incremental GetFilesToBackup() entry "+refEntry.ToString()+"  __DELETED__");
						refEntry.ChangeStatus = DataLayoutInfos.Deleted;
						yield return refEntry;
						continue;

					}*/


					/*if(idsToWatch.Contains(entry.ID)){ // we found it! is was simply moved
						Logger.Append (Severity.DEBUG2, "Found wanted entry "+entry.ID+",  "+entry.SnapFullPath);
						for(int i=idsToWatch.Count-1; i==0; i--)
							if(idsToWatch[i] == entry.ID)
								idsToWatch.RemoveAt(i);
					}*/
					//Console.WriteLine("Incremental GetFilesToBackup() entry "+entry.SnapFullPath+"("+entry.ID+") DOES NOT MATCH, got "+refEntry.Name+" ("+refEntry.ID+")");
					/*long searchRowid = 0;
					if((refEntry = refIndex.SearchItem(entry, rootDrive.SystemDrive.MountPoint, out searchRowid)) != null){
						long fsToIndexOffset = searchRowid - refIndex.RowId;
						Console.WriteLine("Incremental GetFilesToBackup() Found ref entry "+entry.SnapFullPath+" at __OFFSET__="+(searchRowid - refIndex.RowId));
						if(  fsToIndexOffset < 100){

							for (int j = (int)fsToIndexOffset; j >0; j--){
								if(idxEnumerator.MoveNext())
									idsToWatch.Add(idxEnumerator.Current.ID);
							}
						}
						//if current FS entry and ref entry mismatch, but current FS entry exists in ref index , 
						// we put this ref entry on the 'to watch' list :
						// It may indicate that ref has been deleted, or, if we meet this 'watched' id during backup, that
						// the item has been moved
						else{
							//idsToWatch.Add(refEntry.ID);
							idxEnumerator.Dispose();
							idxEnumerator = refIndex.GetBaseItemsEnumerator(rootDrive.SystemDrive.OriginalMountPoint, refIndex.RowId).GetEnumerator();
							idxEnumerator.MoveNext();
							Console.WriteLine ("moved ref index enumerator to new root "+idxEnumerator.Current.ID+", name="+idxEnumerator.Current.Name);
						}
					}*/

					/*else{ // new entry reusing "old" inode number/ID 
						Console.WriteLine("\t NOT found matching ID for name="+entry.Name);	
						yield return entry;
						// this new entry could explain the offset we get between current fs and ref index
						moveRefEnumerator = false;
						continue;
					}*/
				} 
						//refEntry = srch;
						
				// Entry already existed under the same id. if it also has the same name, continue checks.
				// else, it may have been (1)renamed, or (2)deleted + inode reused for new file.
				// (1) it it safe to assume it as only renamed if lastmetadata has changed but data hasn't.
				// (2) if oldname!=newname, and lastwritetime has change, we cannot decide what happened. Consider
				// it as a new entry, for safety.
				//Console.WriteLine("\t Search found matching ID, ref name="+srch.Name);	
				/*if(srch.Name != entry.Name){
					Console.WriteLine("Incremental GetFilesToBackup() added RENAMED entry : "+entry.SnapFullPath);
					entry.BlockMetadata.BlockMetadata.Add(new RenamedOrMovedItem());
					yield return entry;
					continue;
				}*/
				// Existing entry with modified data since ref backup
				// lastmod < ref lastmod : rename(?). lastmod < ref lastmod : entry data  modified
				if(entry.LastModifiedTime != /*refBackupTimeStart*/ refEntry.LastModifiedTime
				   || entry.LastMetadataModifiedTime != refEntry.LastMetadataModifiedTime){
						Console.WriteLine("Incremental GetFilesToBackup() added MODIFIED (LastModifiedTime) entry : "+entry.SnapFullPath+", entry.LastModifiedTime="+entry.LastModifiedTime+",refEntry.LastModifiedTime="+refEntry.LastModifiedTime);
						entry.ChangeStatus = DataLayoutInfos.HasChanges;
						yield return entry;
						
						continue;
				}
				/*else if(entry.LastMetadataModifiedTime >  refEntry.LastMetadataModifiedTime){
					entry.ChangeStatus = DataLayoutInfos.MetadaOnly;
					// moved entry
					//if(srch.ParentID != entry.ParentID || srch.Name != entry.Name){
						//entry.BlockMetadata.BlockMetadata.Add(new RenamedOrMovedItem());
						entry.ChangeStatus = DataLayoutInfos.MetadaOnly; // .RenameOnly;
					//}
					//else
					//	entry.BlockMetadata.BlockMetadata.Add(new UnchangedDataItem());
					Console.WriteLine("Incremental GetFilesToBackup() added METADATACHANGE (LastMetadataModifiedTime) entry : "+entry.SnapFullPath);
					yield return entry;
					continue;
				}*/
				// if we get there, entry hasn't changed.
				//Console.WriteLine("Incremental GetFilesToBackup UNCHANGED "+entry.Name);
					
			//	}

				// If we get there, FS entry didn't change since last backup.
				// But if we are asked to perform a full refresh, return it anyway, with appropriate ChangeFlag
				if(this.isFullRefreshBackup){
					entry.ChangeStatus = DataLayoutInfos.NoChange;
					yield return entry;
				}
			} // // end foreach phantomBrdh
			Console.WriteLine ("FileCompareProvider GetNextEntry : end foreach");

			// Fs has been enumerated. Now check if some ref index IDs remain unfound (== present inside idsToWatch)
			// If so, either they have been deleted, or they have been moved out of scope (out of FS backup root directory)
			// Anyways, we tag them as 'deleted', as, from the backup's root folder point of view, they are not part of
			// the backup anymore.
			if(idsToWatch.Count>0)
				Logger.Append(Severity.DEBUG, idsToWatch.Count+" entries seem to have been deleted.");
			foreach(long id in idsToWatch){
				long useless = 0;
				IFSEntry deleted = refIndex.SearchItem(id, rootDrive.SystemDrive.MountPoint, out useless);
				if(deleted != null){
					Console.WriteLine ("\t entry "+id+" has been deleted, was "+deleted.Name);
					deleted.ChangeStatus = DataLayoutInfos.Deleted;
					yield return deleted;
				}
				else
					Console.WriteLine ("\t entry "+id+" has NOT BEEN DELETED - ERROR!!!");
			}
			yield break;
		}

		private void BubbleUpSubCompletion(string newPath){
			if(SubCompletionEvent != null)
				SubCompletionEvent(newPath);
		}

		public void SignalBackup(){
			backupTimeStart = Utilities.Utils.GetUtcUnixTime(DateTime.UtcNow);			
			
		}

		public byte[] Metadata{
			get{ // nothing to do here
				using(MemoryStream metaWriter = new MemoryStream()){
					BinaryFormatter formatter = new BinaryFormatter();
					formatter.Serialize(metaWriter, backupTimeStart);
					formatter.Serialize(metaWriter, this.taskId);
					//formatter.Serialize(metaWriter, backupT);
					metaWriter.Flush();
					return metaWriter.ToArray();
				}
			}
		}

		public void SetReferenceMetadata(byte[] metadata){
			Logger.Append (Severity.DEBUG2, "Setting reference metadata...");
			if(metadata == null){
				Logger.Append (Severity.ERROR, "Reference metadata is null");
				throw new NullReferenceException("provided FileCompare metadata is null");
			}
			using(MemoryStream metadataReader = new MemoryStream(metadata, false)){
					BinaryFormatter formatter = new BinaryFormatter();
					this.refBackupTimeStart = (long)formatter.Deserialize(metadataReader);
					this.refTaskId = (long)formatter.Deserialize(metadataReader);
					//refBackupTimeEnd = (long)formatter.Deserialize(metadataReader);
			}

			// convert ref start date to filetime
			DateTime refUtc = Utilities.Utils.GetLocalDateTimeFromUnixTime(refBackupTimeStart);
			Logger.Append(Severity.DEBUG, "Reference backup start : "+refBackupTimeStart+" (local time : "+refUtc.ToLocalTime().ToString()+"), end : "+refBackupTimeEnd);
			this.refBackupTimeStart = refUtc.ToFileTimeUtc();
		}
	}
}

