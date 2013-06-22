using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using Node.Utilities;
using P2PBackup.Common;

namespace Node.DataProcessing{
	
	/// <summary>
	/// This is a portable, generic (and, in most situations, slowest) incremental provider.
	/// it uses the reference task index (as an array of [fileId,modificationtime] to detect renamed and removed items,
	/// the reference backup start time to detect modified items by metadata modification time,
	/// the reference backup Max item ID to detect new files, or reference index if new files got an ID lower than reference maxId
	///  	Because of its general slowness (needs to traverse the whole FS entries) and memory usage (array of reference backup entries)
	///  this provider has the lowest priority.
	/// 	A 3rd drawback would be its vulnerability to modificationtime alteration 
	/// (a user can modify a file, then set its mtime back to what it was before modifying the file).
	/// 	To allow to mitigate this potential issue, we offer the option to 'not trust mtime'. If set, this option
	/// will backup again a file whose metadata modification time has changed. Metadata mod time is not alterable from users,
	/// so we gain increased reliability with the (maybe bug) penalty of backuping files that have not seen their data changed at
	/// all but only a real metadata chage (permissions, ownership...). We let the responsability to choose between mtime nd ctime to the user
	///
	/// The main advantage of this provider is that it is the most platform and filesystem independant.
	/// </summary>
	public class FileComparer:IIncrementalProvider{

		public delegate void SubCompletionHandler(string path);
		public event SubCompletionHandler SubCompletionEvent;
		
		public short Priority{get{return 1;}}
		
		public string Name{get{return "FileComparer";}}
		
		public bool ReturnsOnlyChangedEntries{get{return true;}}
		
		public bool IsEnabled{get;set;}
		
		internal FileComparer(long task, BackupRootDrive rd){
			this.rootDrive = rd;
			this.taskId = task;
			prov = ItemProvider.GetProvider();
		}

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
		private Dictionary<long, Pair<long, bool>> refEntries; // holds references index <fileId,modificationTime,foundOnFsScan> entries

		/// <summary>
		/// Checks if we have information from the reference backup task
		/// </summary>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		public bool CheckCapability(){
			if(refBackupTimeStart == 0 || refTaskId <=0/*|| refBackupTimeEnd == long.MaxValue || refMaxId == 0 */|| refTaskId == 0){
				Logger.Append(Severity.DEBUG, "FileCompare incr provider : some parameters are missing, provider is thus unusable");
				return false;
			}
			Logger.Append(Severity.DEBUG, "FileComparer incr provider : opening reference index "+refTaskId+"...");
			//IndexManager.IsIndexPresentAndValid(refTaskId, 
			refIndex = new Index(refTaskId, false);
			refIndex.Open();
			refMaxId = refIndex.GetMaxId(this.rootDrive.SystemDrive.OriginalMountPoint);
			Logger.Append(Severity.TRIVIA, "Incremental ref max item id="+refMaxId);
			// load ref backup entries (file/inode id, last mod time)
			refEntries = refIndex.GetItemsForRefresh(this.rootDrive.SystemDrive.OriginalMountPoint, false);
			refIndex.Terminate();
			return true;
		}
		
		/// <summary>
		/// Gets the next incremental/diff entry to backup. 
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
			
			//To efficiently reuse existing code, we instanciate
			// a "phantom" BackupRootDriveHandler an use its "GetFull()" FS items provider as an items source to check
			// for modifications.
			// Each entry already existing in the ref backup index is marked as 'found' (Tuple.Item2 = true)
			// to allow to detect not found items (deleted) after collecting curent FS items.
			phantomBrd = new BackupRootDriveHandler(rootDrive, this.taskId, 0, 0, 0, BackupLevel.Full, 0, 0, 0);
			phantomBrd.SetCurrentPath(path);
			phantomBrd.SubCompletionEvent += new BackupRootDriveHandler.SubCompletionHandler(BubbleUpSubCompletion);

			foreach(IFSEntry entry in phantomBrd.GetFull(true)){
				// New File (inode/id > last refMaxId known inode/id) or doesn't exist in ref backup
				if(entry.ID > refMaxId || !refEntries.ContainsKey(entry.ID) ){ //todo : also check CreationTime?
					Console.WriteLine("Incremental GetFilesToBackup() added NEW entry : "+entry.SnapFullPath);
					entry.ChangeStatus = DataLayoutInfos.New;
					yield return entry;	
					continue;
				}
				else if(entry.LastModifiedTime > refEntries[entry.ID].Item1){
					entry.ChangeStatus = DataLayoutInfos.HasChanges;
					refEntries[entry.ID].Item2 = true;
					yield return entry;	
					continue;
				}	
				else if(entry.LastMetadataModifiedTime > refBackupTimeStart){
					entry.ChangeStatus = DataLayoutInfos.MetadaOnly;
					refEntries[entry.ID].Item2 = true;
					yield return entry;	
					continue;
				}	
				else
					refEntries[entry.ID].Item2 = true;
			} // // end foreach phantomBrdh

			// last step : returd deleted entries
			foreach(var item in refEntries){
				if(!item.Value.Item2){
					IFSEntry deleted =  prov.GetEmptyItem();
					deleted.ID = item.Key;
					deleted.ChangeStatus = DataLayoutInfos.Deleted;
					yield return deleted;
				}
			}
			refEntries = null;
			yield break;
		}
		
		private void BubbleUpSubCompletion(string newPath){
			if(SubCompletionEvent != null)
				SubCompletionEvent(newPath);
		}
		
		public void SignalBackup(){
			backupTimeStart = Utilities.Utils.GetUtcUnixTime(DateTime.UtcNow);			
		}
		
		public byte[] GetMetadata(){
			using(MemoryStream metaWriter = new MemoryStream()){
				BinaryFormatter formatter = new BinaryFormatter();
				formatter.Serialize(metaWriter, backupTimeStart);
				formatter.Serialize(metaWriter, this.taskId);
				metaWriter.Flush();
				return metaWriter.ToArray();
			}
		}
		
		public void SetReferenceMetadata(byte[] metadata){
			Logger.Append (Severity.TRIVIA, "Setting reference metadata...");
			if(metadata == null){
				Logger.Append (Severity.ERROR, "Reference metadata is null");
				throw new NullReferenceException("provided FileComparer metadata is null");
			}
			using(MemoryStream metadataReader = new MemoryStream(metadata, false)){
				BinaryFormatter formatter = new BinaryFormatter();
				this.refBackupTimeStart = (long)formatter.Deserialize(metadataReader);
				this.refTaskId = (long)formatter.Deserialize(metadataReader);
				//refBackupTimeEnd = (long)formatter.Deserialize(metadataReader);
			}
			
			// convert ref start date to filetime
			DateTime refUtc = Utilities.Utils.GetLocalDateTimeFromUnixTime(refBackupTimeStart);
			Logger.Append(Severity.DEBUG, "Reference backup start : "+refBackupTimeStart+" (local time : "+refUtc.ToLocalTime().ToString()+", utc filetime:"+refUtc.ToFileTimeUtc()+"), end : "+refBackupTimeEnd);
			this.refBackupTimeStart = refUtc.ToFileTimeUtc();
		}
	}
}

