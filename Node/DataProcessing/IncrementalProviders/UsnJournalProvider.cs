#if OS_WIN

using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using Node.Utilities;
using Node.Utilities.Native;
using P2PBackup.Common;

namespace Node.DataProcessing{

	public class UsnJournalProvider:IIncrementalProvider{

		// first, ensure we don't read unknown record types. For now we don"t support (yet) windows 8 and server 2012
		private const short UsnRecordVersion = 1; // supported version (1 < win8, server 2008. 2 = win 8, server 2012)

		private long prevTransactionId; //last checked usn transaction
		private ulong prevJournalId;
		private long transactionId; //last checked usn transaction
		private long journalMinUsn;
		private ulong journalId;
		private long refTimeStamp; // timestamp of reference backup snapshot

		private BackupRootDrive brd;
		//private string usnDataFile;
		private NtfsUsnJournal usnJ;

		private Dictionary<int, Win32Api.UsnEntry> entries;


		public delegate void SubCompletionHandler(string path);
		public event SubCompletionHandler SubCompletionEvent;

		public short Priority{
			get{return 2;}
		}

		public string Name{
			get{
				return "UsnJournalProvider";
			}
		}

		public bool ReturnsOnlyChangedEntries{get{return true;}}

		public bool IsEnabled{get;set;}
		
		internal UsnJournalProvider(long refTaskId, BackupRootDrive rd){
			prevJournalId = 0;
			prevTransactionId = 0;
			brd = rd;
		}
		
		/// <summary>
		/// Checks if we have all the required reference information (ref backup usn metadata)
		/// and if the usn journal is available (existing and usable from reference journal & transaction ids)
		/// </summary>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		public bool CheckCapability(){
			
			if(Utilities.PlatForm.IsUnixClient())
				return false;
			try{

				if(prevJournalId == 0 || prevTransactionId == 0 /*|| brd.SystemDrive.DriveFormat.ToLower() != "ntfs"*/){
						Logger.Append(Severity.DEBUG, "No reference USN journal found. Either this is the first backup, either SetMetadata() has not been called yet, or drive is not NTFS/ReFS");
						return false;
				}

				using(NtfsUsnJournal jtest = new NtfsUsnJournal(brd/*.Snapshot.MountPoint*/)){
					Console.WriteLine ("CheckCapability() : instanciated usn j");
					if(!jtest.IsUsnJournalActive()){
						Logger.Append(Severity.INFO, "the drive "+brd.SystemDrive.OriginalMountPoint+" doesn't have an USN tracking journal."
						              +" It can be manually created using the command 'fsutil usn createjournal m=400000 a=100 "+ brd.SystemDrive.OriginalMountPoint+"'");
					}
						
				
					if (journalId != prevJournalId /*|| transactionId < prevTransactionId*/){ // journal has been reset, can't rely on it
						Logger.Append(Severity.WARNING, "Drive "+brd.SystemDrive.OriginalMountPoint+" USN Journal has been reset since last time we checked it. Can't be used :"
						              +"transaction id "+transactionId+"/"+prevTransactionId+", journal "+journalId+"/"+prevJournalId);
						/*Console.WriteLine ("********************************");
						Console.WriteLine ("***MAKE IT RETURN FALSE!!!      *");
						Console.WriteLine ("********************************");*/
						return false;
					}
					if(prevTransactionId < journalMinUsn){// some space has been freeed inside the journal, we cannot read back to wanted transaction 
						Logger.Append(Severity.WARNING, "Drive "+brd.SystemDrive.MountPoint+" Wanted start USN ("+prevTransactionId+")is too old, has been recycled by the journal (min usable USN is "+journalMinUsn+"). Unable to use USN. If this occurs frequently, consider growing the USN journal max size.");
						return false;
					}
				}
				return true;
			}
			catch(Exception e){
				Logger.Append(Severity.WARNING, "Drive '"+brd.SystemDrive.MountPoint+" (snap "+brd.Snapshot.MountPoint+")' : can't use UsnJournal incremental/differential provider : "+e.ToString()
				              +". Journal may not be accessed, not available for this kind of filesystem, or doesn't exist. You can create one using 'fsutil usn createjournal' command");
				return false;
			}
		}
		

		
		public IEnumerable<IFSEntry> GetNextEntry(BasePath path, string snapshottedPath){
			IFileProvider itemProv = ItemProvider.GetProvider();
			try{
				if(entries == null)
					entries = GetUsnRecordsDictionary();

				Logger.Append(Severity.TRIVIA, "Got "+entries.Count+" (total for whole drive)  changed entries since reference backup");
			}
			catch(Exception e){
				Logger.Append(Severity.ERROR, "Cannot query drive USN journal : "+e.Message);
				throw(e);
			}
			// now that we got a list with only the last usn cumulated change, analyze it
			foreach(KeyValuePair<int, Win32Api.UsnEntry> entry in entries){
				string ePath="";
				Console.WriteLine (" USN entry: "+entry.ToString());
				usnJ.GetPathFromFileReference(entry.Value.FileReferenceNumber/*(ulong)entry.Key*/, out ePath);
				ePath = brd.Snapshot.MountPoint/*Path*/.TrimEnd('\\')+ePath;
				//if(ePath.ToLower().Contains("copy") ||ePath.ToLower().Contains("docs") )
					Console.WriteLine ("   ///// fullpath="+ePath);


				IFSEntry changedItem;

				/*if(((NtfsUsnJournal.UsnReasonCode)entry.Value.Reason).HasFlag(NtfsUsnJournal.UsnReasonCode.USN_REASON_FILE_CREATE))
					Console.WriteLine ("***  item "+ePath+" CREATED");
				else */
				if( (((Win32Api.UsnReasonCode)entry.Value.Reason).HasFlag ( Win32Api.UsnReasonCode.USN_REASON_FILE_DELETE )) ){
					/*if( (((NtfsUsnJournal.UsnReasonCode)entry.Value.Reason).HasFlag ( NtfsUsnJournal.UsnReasonCode.USN_REASON_FILE_CREATE )) ){
						Console.WriteLine ("***  item "+entry.Value.Name+" CREATED+DELETED");
						// TODO : handle deleted + created items (ID reused), if NTFS does so
						continue;
					}*/
				    Console.WriteLine ("***DELETEF###  item "+entry.Value.Name+" DELETED");
					changedItem = new NTBackupFile();
					changedItem.ID = entry.Key;

					changedItem.Name = entry.Value.Name;
					//changedItem.BlockMetadata.BlockMetadata.Add(new DeletedItem());
					changedItem.ChangeStatus = DataLayoutInfos.Deleted;


					yield return changedItem;
					continue;
				}
				// item not corresponding to current wanted path.
				if(ePath.IndexOf(snapshottedPath) != 0) 
					continue;
				try{
					changedItem = itemProv.GetItemByPath(ePath);
					changedItem.ParentID = (int)entry.Value.ParentFileReferenceNumber;


					/*else/* if( (((NtfsUsnJournal.UsnReasonCode)entry.Value.Reason).HasFlag ( NtfsUsnJournal.UsnReasonCode.USN_REASON_DATA_EXTEND ))
					        || (((NtfsUsnJournal.UsnReasonCode)entry.Value.Reason).HasFlag ( NtfsUsnJournal.UsnReasonCode.USN_REASON_DATA_OVERWRITE )) 
					        || (((NtfsUsnJournal.UsnReasonCode)entry.Value.Reason).HasFlag ( NtfsUsnJournal.UsnReasonCode.USN_REASON_DATA_TRUNCATION )) 
					        || (((NtfsUsnJournal.UsnReasonCode)entry.Value.Reason).HasFlag ( NtfsUsnJournal.UsnReasonCode.USN_REASON_ENCRYPTION_CHANGE )) 
					        )
						{
						Console.WriteLine ("***  item "+ePath+" DATA MODIFIED");
						changedItem.BlockMetadata.BlockMetadata.Add(new Mod());
					}*/
					/*else */if( (((Win32Api.UsnReasonCode)entry.Value.Reason).HasFlag ( Win32Api.UsnReasonCode.USN_REASON_RENAME_NEW_NAME )
					          || (((Win32Api.UsnReasonCode)entry.Value.Reason).HasFlag ( Win32Api.UsnReasonCode.USN_REASON_RENAME_OLD_NAME ) )
					          ))
					{
					    Console.WriteLine ("***RENAMED###  item "+ePath+" RENAMED ");
						//RenamedOrMovedItem rmi = new RenamedOrMovedItem();
						//rmi.OldName = entry.Value.OldName;
						//changedItem.BlockMetadata.BlockMetadata.Add(rmi);

						// check if there is data changes , additionally to rename:
						Win32Api.UsnReasonCode newR = ((Win32Api.UsnReasonCode)entry.Value.Reason);
						newR &= ~Win32Api.UsnReasonCode.USN_REASON_RENAME_NEW_NAME;
						newR &= ~Win32Api.UsnReasonCode.USN_REASON_RENAME_OLD_NAME;
						newR &= ~Win32Api.UsnReasonCode.USN_REASON_CLOSE;

						if((uint)newR == 0x00000000)
							changedItem.ChangeStatus = DataLayoutInfos.RenameOnly;
							//changedItem.BlockMetadata.BlockMetadata.Add(new UnchangedDataItem());
						 
					}
					/*else if( (((NtfsUsnJournal.UsnReasonCode)entry.Value.Reason).HasFlag ( NtfsUsnJournal.UsnReasonCode.U )) )
					    Console.WriteLine ("***  item "+ePath+" MOVED");*/
				}
				catch(Exception e){//don't add item, but report it, put backup task in Warning state
					Logger.Append (Severity.ERROR, "Could not add item '"+ePath+"', TODO : REPORT REPORT!!. "+e.Message);
					continue;
				}
				//Console.WriteLine ("  == usn entry path="+ePath+", reason="+((NtfsUsnJournal.UsnReasonCode)entry.Value.Reason).ToString()+" ("+entry.Value.Reason+"), ID="+ (int)(entry.Value.FileReferenceNumber)+", pid="+(int)entry.Value.ParentFileReferenceNumber);
				yield return changedItem;
			}

			//}
		}
		
		private Dictionary<int, Win32Api.UsnEntry> GetUsnRecordsDictionary(){
			PrivilegesManager pm = new PrivilegesManager();
			pm.Grant();
			Dictionary<int, Win32Api.UsnEntry> uEntries = new Dictionary<int, Win32Api.UsnEntry>();
			using(usnJ = new NtfsUsnJournal(/*brd.SystemDrive.MountPoint*/brd/*.Snapshot.MountPoint*/)){
				Logger.Append(Severity.DEBUG, "Reading USN journal "+journalId+" for '"+brd.SystemDrive.MountPoint
				    +"' from seq "+prevTransactionId+" to seq "+transactionId
				    +" (changed entries from "+Utilities.Utils.GetLocalDateTimeFromUnixTime(refTimeStamp).ToString()
					+" to "+Utilities.Utils.GetLocalDateTimeFromUnixTime(brd.Snapshot.TimeStamp).ToLocalTime().ToString()+")");
				Win32Api.USN_JOURNAL_DATA stateJd = new Win32Api.USN_JOURNAL_DATA();
				stateJd.UsnJournalID = journalId;
				stateJd.NextUsn = prevTransactionId;
				Win32Api.USN_JOURNAL_DATA newState = new Win32Api.USN_JOURNAL_DATA(); // unused, as we maintain our own state
				List<Win32Api.UsnEntry> changedUsnEntries = new List<Win32Api.UsnEntry>();
				usnJ.GetUsnJournalState(ref newState);
				NtfsUsnJournal.UsnJournalReturnCode retCode =  usnJ.GetUsnJournalEntries(stateJd, refTimeStamp, 0xFFFFFFFF, out changedUsnEntries, out newState);

				if(retCode != NtfsUsnJournal.UsnJournalReturnCode.USN_JOURNAL_SUCCESS)
					throw new Exception(retCode.ToString());

				int entryId = 0;
				foreach(Win32Api.UsnEntry ue in changedUsnEntries){
					if(ue != null && ue.Reason > 0){
						entryId = (int)(ue.FileReferenceNumber);

						//if(ue.Name.StartsWith("grut"))
						//Console.WriteLine ("|--------| USN seq="+ue.USN+", item "+entryId+" ("+ue.Name+") "+((NtfsUsnJournal.UsnReasonCode)ue.Reason).ToString());

						if(!uEntries.ContainsKey(entryId))
							uEntries[entryId] = ue;
						else{ // cumulate reason flags
							// ignore created+deleted (temporary or short-lived (between 2 backups) items
							if( 
							   ((Win32Api.UsnReasonCode)ue.Reason).HasFlag(Win32Api.UsnReasonCode.USN_REASON_FILE_DELETE)
								&& ((Win32Api.UsnReasonCode)uEntries[entryId].Reason).HasFlag(Win32Api.UsnReasonCode.USN_REASON_FILE_CREATE)
							   )
							{
								Console.WriteLine ("***  item "+ue.Name+" CREATED+DELETED");
								continue;
							}

							// file ID reused (file delete + new create) : totally replace previous entry
							else if(
							   ((Win32Api.UsnReasonCode)ue.Reason).HasFlag(Win32Api.UsnReasonCode.USN_REASON_FILE_CREATE)
								&& ((Win32Api.UsnReasonCode)uEntries[entryId].Reason).HasFlag(Win32Api.UsnReasonCode.USN_REASON_FILE_DELETE)
							   )
							{
								uEntries[entryId] = ue;
							}

							// cumulate flags
							else if(! ((Win32Api.UsnReasonCode)uEntries[entryId].Reason).HasFlag( ((Win32Api.UsnReasonCode)ue.Reason))){
								
								Win32Api.UsnReasonCode newReason = ((Win32Api.UsnReasonCode)uEntries[entryId].Reason) | ((Win32Api.UsnReasonCode)ue.Reason);
								uEntries[entryId] = ue;
								uEntries[entryId].Reason = (uint)newReason;
							}
							// only keep the last rename operation
							/*if(((NtfsUsnJournal.UsnReasonCode)ue.Reason).HasFlag(NtfsUsnJournal.UsnReasonCode.USN_REASON_RENAME_NEW_NAME) ){
								Console.WriteLine ("***  item "+ue.Name+" RENAMED (reasons="+((NtfsUsnJournal.UsnReasonCode)ue.Reason).ToString());
								NtfsUsnJournal.UsnReasonCode newReason = ((NtfsUsnJournal.UsnReasonCode)entries[entryId].Reason) ;
								if(!((NtfsUsnJournal.UsnReasonCode)entries[entryId].Reason).HasFlag(NtfsUsnJournal.UsnReasonCode.USN_REASON_RENAME_NEW_NAME) )
									newReason |=  NtfsUsnJournal.UsnReasonCode.USN_REASON_RENAME_NEW_NAME;
								entries[entryId] = ue;
								entries[entryId].Reason = (uint)newReason;
							}*/
						}
					}
				}
				Logger.Append(Severity.TRIVIA, "Done reading USN journal "+journalId+" for '"+brd.SystemDrive.MountPoint);
			}//end using 
			return uEntries;
		}


		public void SignalBackup(){ // to be called when full is performed
			//Console.WriteLine ("brd snapshot type="+brd.Snapshot.);
			using(usnJ = new NtfsUsnJournal(brd/*.Snapshot.MountPoint*/)){
				Win32Api.USN_JOURNAL_DATA journal = new Win32Api.USN_JOURNAL_DATA();
				usnJ.GetUsnJournalState(ref journal);
				Logger.Append(Severity.DEBUG, "Current USN journal for '"+brd.Snapshot.MountPoint+"' " +journal.UsnJournalID+", USN no: " + journal.NextUsn+", maxSize="+journal.MaximumSize/1024+"k, FirstEntry="+journal.FirstUsn);
				journalId = (ulong)journal.UsnJournalID;
				transactionId = journal.NextUsn;
				journalMinUsn = journal.FirstUsn;
			}
		}

		public byte[] GetMetadata(){
				using(MemoryStream usnWriter = new MemoryStream()){
					BinaryFormatter formatter = new BinaryFormatter();
					formatter.Serialize(usnWriter, journalId);
					formatter.Serialize(usnWriter, transactionId);
					formatter.Serialize(usnWriter, brd.Snapshot.TimeStamp);
					usnWriter.Flush();
					return usnWriter.ToArray();
				}
		}

		public void SetReferenceMetadata(byte[] metadata){
			if(metadata == null) throw new NullReferenceException("provided USN metadata is null");
			using(MemoryStream usnReader = new MemoryStream(metadata, false)){
				BinaryFormatter formatter = new BinaryFormatter();
				prevJournalId = (ulong)formatter.Deserialize(usnReader);
				prevTransactionId = (long)formatter.Deserialize(usnReader);
				refTimeStamp = Utilities.Utils.GetUtcUnixTime(Utilities.Utils.GetLocalDateTimeFromUnixTime((long)formatter.Deserialize(usnReader)).ToLocalTime());

			}
			Logger.Append(Severity.DEBUG, "Reference Journal ID : "+prevJournalId+", USN : "+prevTransactionId);
		}

	}
}

#endif