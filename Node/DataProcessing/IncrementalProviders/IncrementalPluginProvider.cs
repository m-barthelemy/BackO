using System;
using System.Collections.Generic;
using Node.Utilities;
using P2PBackup.Common;

namespace Node.DataProcessing{

	internal class IncrementalPluginProvider{

		//private static FileCompareProvider fcp;
		//private static UsnJournalProvider usnp;
		//private static BtrfsProvider btrfsp;
		private static List <KeyValuePair<string, IIncrementalProvider>> selectablePlugins = new List <KeyValuePair<string, IIncrementalProvider>>();

		/*internal static IIncrementalProvider GetProviderByName(string name){
			return selectablePlugins[name].Value;
		}*/
		
		internal static IIncrementalProvider GetProviderByPriority(BackupRootDrive rd, Dictionary<string, byte[]> initMetadata){
			if(initMetadata == null || initMetadata.Count ==0)
				return null;

			IIncrementalProvider iip = null; //new FileCompareProvider(referenceBackupStart, referenceBackupEnd, refTaskId);
			//iip = from plugins where plugins.ContainsKey(rd.systemDrive.MountPoint) and p
			int maxPrio = 0;
			foreach(KeyValuePair<string, IIncrementalProvider> prov in selectablePlugins){
				if(prov.Key == rd.SystemDrive.OriginalMountPoint && prov.Value.Priority > maxPrio){
					Logger.Append(Severity.DEBUG, "Testing incr provider '"+prov.Value.Name+"' for drive '"+rd.SystemDrive.OriginalMountPoint+"'");
					foreach(KeyValuePair<string, byte[]> kp in initMetadata)
						Logger.Append (Severity.TRIVIA, "Got reference metadata for provider "+kp.Key);
					if(initMetadata.ContainsKey(prov.Value.Name))
						prov.Value.SetReferenceMetadata(initMetadata[prov.Value.Name]);
					if(prov.Value.CheckCapability()){
						iip = prov.Value;
						maxPrio = prov.Value.Priority;
						Logger.Append (Severity.INFO, "Incremental provider "+prov.Value.Name+" with priority "+prov.Value.Priority+" is selectable for this task.");
					}
					else 
						Logger.Append (Severity.INFO, "Incremental provider "+prov.Value.Name+" with priority "+prov.Value.Priority+" is NOT selectable for this task.");
				}
			}
			/*if(Utilities.PlatForm.IsUnixClient()){
				FileCompareProvider fcp = new FileCompareProvider(referenceBackupStart, referenceBackupEnd, refTaskId, rd);
				if(fcp.CheckCapability()) iip = fcp;
			}
			else{
				if(usnp == null) throw new Eception("Provider USNJournalProvider has not been initialized, call PrepareProvidersForBackup first");
				else {
				//UsnJournalProvider usn = new UsnJournalProvider(bType, rd, referenceBackupStart, referenceBackupEnd);
				//UsnJournalProvider usn = new UsnJournalProvider(rd, referenceBackupStart, referenceBackupEnd);
					if(usnp.CheckCapability()) iip = usn;

				}
			}*/
			if(iip != null){
				Logger.Append(Severity.INFO, "Incremental/Differential provider "+iip.GetType().ToString()+", priority "+iip.Priority+" choosen.");

			}
			return iip;
		}

		// returns true if incremental plugin only returns changed items, false if it returns a complete list
		// of items (including unchanged ones)
		//TODO : rewrite this crappy impl
		internal static bool IsPartialTypePlugin(string pluginName){
			if(pluginName == "UsnJournalProvider")
				return (new UsnJournalProvider(0, null)).ReturnsOnlyChangedEntries;
			else
				return (new FileComparer(0, null)).ReturnsOnlyChangedEntries;
		}


		/// <summary>
		/// Signals all providers that a backup is going to occur.
		/// Used and unused providers can then gather their metadata, that we will store into index for later use.
		/// For example, Usnjournal provider will give us USN journal ID ans position (last usn record number) allowing to perform 
		/// subsequent incr backup based on these reference IDs.
		/// </summary>
		/// <returns>
		/// a list of providername-metadata pairs (name-drivename-metadata)
		/// </returns>
		/// <param name='path'>
		/// Path.
		/// </param>
		internal static Dictionary<string, byte[]> SignalBackupBegin(long taskId, BackupRootDrive rd){
			Dictionary<string, byte[]> provMetadataPairs = new Dictionary<string, byte[]>();

			try{
#if OS_WIN
				if(!Utilities.PlatForm.IsUnixClient()){
					UsnJournalProvider usnp = new UsnJournalProvider(taskId, rd);
					usnp.SignalBackup();
					//if(usnp.CheckCapability()){
						provMetadataPairs.Add(usnp.Name, usnp.GetMetadata());
						selectablePlugins.Add(new KeyValuePair<string, IIncrementalProvider>(rd.SystemDrive.OriginalMountPoint, usnp));
					//}
					//else // re-set it to null, ,as we won't use it
					//usnp = null;
				}
#endif
#if OS_UNIX
				/*else{

				}*/
				//FileCompareProvider fcp = new FileCompareProvider(taskId, rd);
				FileComparer fcp = new FileComparer(taskId, rd);
				fcp.SignalBackup();
				//if(fcp.CheckCapability()){
					provMetadataPairs.Add(fcp.Name, fcp.GetMetadata());
					selectablePlugins.Add(new KeyValuePair<string, IIncrementalProvider>(rd.SystemDrive.OriginalMountPoint, fcp));
				//}
#endif
			}
			catch(Exception e){
				Logger.Append (Severity.TRIVIA, e.ToString());
			}// Yes. In case of exception the provider is simply not added, and that's an expected behavior (as of now)

			return provMetadataPairs;


		}

		private void LoadProviders(){
			selectablePlugins = new List <KeyValuePair<string, IIncrementalProvider>>();


		}
		/// <summary>
		/// signals all available and relevant providers that a backup is going to occur.
		/// This allows them to SessionType theire NullReferenceException point:
		/// For example, the USN provider will check the current rootdrive USN journal and Sequence IDs ; 
		/// we will store these information into index as provider metadata, and these will be the reference IDs 
		/// for further incremental backups using USN.
		/// </summary>
		/// <param name='rd'>
		/// Rd.
		/// </param> 
		/*internal Dictionary<string, byte[]> PrepareProvidersForBackup(BackupRootDrive rd){

		}*/


	}
}

