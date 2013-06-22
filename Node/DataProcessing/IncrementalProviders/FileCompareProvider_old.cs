using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using Node.Utilities;
using P2PBackup.Common;

namespace Node.DataProcessing{
	
	/// <summary>
	/// This is ta portable, generic (and, in most situations, slowest) incremental provider.
	/// it uses the reference task index to detect renamed and removed items,
	/// the reference backup start time to detect modified items,
	/// the reference backup Max item ID to detect new files, or reference index if new files got an ID lower than reference maxId
	///  Because of its general slowness and a possible lack of accuracy in some (rare) corner-cases,
	///  this provider has the lowest priority.
	/// Its main advantage is that it is the most platform and filesystem independant provider.
	/// </summary>
	public class FileCompareProvider_old:IIncrementalProvider{
		private long refBackupTimeStart;
		private long refBackupTimeEnd;
		private long backupTimeStart;
		IFileProvider prov;
		//private IEnumerator<IFile> refTaskEnumerator;
		private int depth;
		private long refMaxId;
		private long refTaskId;
		private long taskId;
		
		private BackupRootDrive rootDrive;
		private Index refIndex;
		
		// reference index items enumerator
		//System.Collections.IEnumerable idxProv;
		// current FS/snap items enumerator
		System.Collections.IEnumerable fsProv;
		IEnumerator<IFSEntry> idxEnumerator;
		
		public short Priority{
			get{return 1;}
		}

		public string Name{
			get{
				return "FileCompareProvider";
			}
		}

		public bool IsEnabled{get;set;}
		
		internal FileCompareProvider_old(long task, BackupRootDrive rd){
			this.rootDrive = rd;
			this.taskId = task;
			prov = ItemProvider.GetProvider();
			//Logger.Append(Severity.INFO, "Gathering changed items since "+refBackupStart.ToString());
			//itemsToCompare = new List<IFile>();
			depth = 0;
			// get the full/synth index of reference backup, to check for renames/deletions/creations
			
			
			
			/*Index refIndex = new Index();
			refIndex.Open(refTaskId);
			refMaxId = refIndex.GetMaxId(rd.systemDrive.MountPoint);
			refTaskEnumerator = refIndex.GetItemsEnumerator(rd.systemDrive.MountPoint).GetEnumerator();*/
		}
		
		/// <summary>
		/// Always return true as this is the default/fallback implementation
		/// </summary>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		public bool CheckCapability(){
			if(refBackupTimeStart == 0 /*|| refBackupTimeEnd == long.MaxValue || refMaxId == 0 */|| refTaskId == 0){
				Logger.Append(Severity.DEBUG, "FileCompare incr provider : some parameters are missing, provider is thus unusable");
				return false;
			}
			refIndex = (new IndexManager()).GetFullIndex(refTaskId, false);
			refMaxId = refIndex.GetMaxId(this.rootDrive.systemDrive.MountPoint);
			Logger.Append(Severity.DEBUG2, "Incremental ref max item id="+refMaxId);
			//idxProv = refIndex.GetItemsEnumerator(rootDrive.systemDrive.MountPoint);
			idxEnumerator = refIndex.GetItemsEnumerator(rootDrive.systemDrive.MountPoint).GetEnumerator();
			return true;
		}
		
		
		public IEnumerable<IFSEntry> GetNextEntry(BasePath path, string snapshottedPath){
			//Console.WriteLine("Incremental GetFilesToBackup() 1");
			fsProv = FSEnumeratorProvider.GetFSEnumeratorProvider().GetFSEnumerator(snapshottedPath);

			//Console.WriteLine("Incremental GetFilesToBackup() 2");
			bool exclude = false;
			foreach(var item in fsProv){
				//Console.WriteLine("Incremental GetFilesToBackup() 3");
				IFSEntry entry = null;
				IFSEntry refEntry = null;
				try{
					entry = prov.GetItem(item);
					
					//Console.WriteLine ("FileCompareProvider.GetNextEntry() : "+entry.SnapshottedFullPath);
				}
				catch(Exception e){// permission errors, deleted file...
					Logger.Append(Severity.ERROR, "Could not add element '"+entry.SnapFullPath+"' to backup : "+e.Message);
					Logger.Append(Severity.INFO, "TODO : report exception to hub for task logentry");
					continue;
				}
				//Console.WriteLine("Incremental GetFilesToBackup() 4");

				if(entry.ID > refMaxId || entry.CreateTime > refBackupTimeStart){ // new File (inode/id > last refMaxId known inode/id)
					Console.WriteLine("Incremental GetFilesToBackup() added NEW entry : "+entry.SnapFullPath);
					yield return entry;	
					continue;/// @@@@@@@@@@@@@@@ TO REMOVE to allow "if(entry.Kind == FileType.Directory){" to execute????
				}
				try{
				/*else */if(idxEnumerator.MoveNext()) // try to position to the same entry, previously backuped
						refEntry = idxEnumerator.Current;
				//Console.WriteLine("Incremental GetFilesToBackup() 5");
				if(refEntry.ID == entry.ID)
					Console.WriteLine("Incremental GetFilesToBackup() entry "+entry.SnapFullPath+" MATCHED in ref backup");
				else{
					Console.WriteLine("Incremental GetFilesToBackup() entry "+entry.SnapFullPath+"("+entry.ID+") DOES NOT MATCH, got "+refEntry.Name+" ("+refEntry.ID+")");
					IFSEntry srch;
					if((srch = refIndex.SearchItem(entry, rootDrive.systemDrive.MountPoint)) != null){
						Console.WriteLine("\t found matching ID, ref name="+srch.Name);	
					}
					else
						Console.WriteLine("\t NOT found matching ID, ref name="+entry.Name);	
				}
				}
				catch(Exception e){
					Console.WriteLine (" ***  ERROR : "+e.ToString());
				}
				
				// entry modified (or created using a used-and-freed id)	
				if(entry.LastModifiedTime >= refBackupTimeStart){
					//if(entry.Kind == FileType.Directory)
						//Console.WriteLine("root path for "+entry.FileName+"="+Directory.GetDirectoryRoot(entry.FileName)+", rooted="+Path.IsPathRooted(entry.FileName));
						Console.WriteLine("Incremental GetFilesToBackup() added MODIFIED (LastModifiedTime) entry : "+entry.SnapFullPath);
					
						yield return entry;
				}
				else if(entry.LastMetadataModifiedTime >= refBackupTimeStart){
					// We will do our best to try to find if it's a renamed file
					// on *nix, a file is renamed if its inode stays the same, but the ctime and filename changes and the mtime does nots
					// if mtime and ctime change and inode is an already previously existing number, a file might have
					//  been deleted and another created, taking the freed inode number.
					// This makes impossible for us to detect files renamed AND modifed, or modified AND renamed
					// on NTFS a file has a unique ID, but it is hard to get from direftory.enumerate (does not return the right structure)
					
					// add the file to the "to be checked" list
					Console.WriteLine ("Incremental GetFilesToBackup() : item "+entry.SnapFullPath+" has undergone METADATA (LastMetadataModifiedTime)  change");
					//itemsToCompare.Add(entry);
				}
				// check for new files with metadata dates in the past (such as packages installations...)
				
				//else if(entry.ID >
				
				if(entry.Kind == FileType.Directory){
					//if(depth <2) 
					//Console.WriteLine ("FileCompareProvider.GetNextEntry() : entering "+entry.SnapshottedFullPath);
					for(int i = path.ExcludedPaths.Count-1; i>=0; i--){
						if(entry.SnapFullPath.IndexOf(path.ExcludedPaths[i]) == 0){
							Logger.Append (Severity.INFO, "Ignoring path "+entry.SnapFullPath);
							path.ExcludedPaths.RemoveAt(i);
							exclude = true;
							//yield return entry;
							break;
						}
					}
					depth++;
					
					if(depth == 1){
						//if(SubCompletionEvent != null) SubCompletionEvent(entry.SnapshottedFullPath.Replace(snapshottedPath, currentPath.Path));
						//entry.FileName.Replace(snapshottedPath, currentPath
					}
					//snapshottedPath = entry.SnapshottedFullPath;
					if(!exclude){
						// recurse using found directory as basepath
						snapshottedPath = entry.SnapFullPath;
						yield return entry; // return top-dir before entering and browse it
						foreach(IFSEntry e in GetNextEntry(path, snapshottedPath)) 
							yield return e;	
					}
					depth--;
				}
				//yield return entry;
				exclude = false;
			//}
			}
			
			//Logger.Append(Severity.DEBUG, "Done scanning file system, performing index compare...");
			//IndexCompare();
		}
		
		/*private IFile SearchItemInIndex(IFile entry){
			if(refChunk == null) refChunk = refIndex.ReadAllChunks();
			foreach(BChunk taskChunk in refChunk){
				if(entry.OriginalFullPath.IndexOf(taskChunk.RootDriveName) <0) continue;
				//Console.WriteLine ("SearchItemInActualIndex() : searching "+entry.OriginalFullPath+" in chunk "+taskChunk.Name+", chunk path="+taskChunk.RootDriveName);
				foreach(IFile chunkEntry in taskChunk.Files){
					//Console.WriteLine (entry.OriginalFullPath+" == "+chunkEntry.OriginalFullPath+" ??");
					if(	chunkEntry.OriginalFullPath == entry.OriginalFullPath)
						return chunkEntry;
				}
				
			}
			return null;
		}*/
				
		public IEnumerable<IFSEntry> IndexCompare(){
			Logger.Append(Severity.DEBUG, "Searching modifications by index comparison...");
			yield break;
		}
		
		public void SignalBackup(){
			backupTimeStart = Utilities.Utils.GetUnixTime(DateTime.UtcNow);			
			
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
			Logger.Append(Severity.DEBUG, "Reference backup start : "+refBackupTimeStart+", end : "+refBackupTimeEnd);
		}
	}
}

