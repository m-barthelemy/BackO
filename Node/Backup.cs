using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using P2PBackup.Common;
using P2PBackup.Common.Volumes;
using Node.Utilities;
using Node.Snapshots;
using Node.DataProcessing;
using Node.StorageLayer;

namespace Node{


	[Serializable]
	public class Backup:Task{
		
		private string indexFileName = "";
		private List<ISnapshot> backupSnapshots;
		private List<BackupRootDrive> backupRootDrives;
		private List<ISpecialObject> specialObjects;
		private StorageLayoutManager slManager;
		private List<ISnapshotProvider> snapshotProviders;

		internal Index Index{get;private set;}
		internal long RefStartDate;
		internal long RefEndDate;
		internal StorageLayout StorageLayout{get; private set;}
		public int TotalChunks{get;set;}

		//number of entries backuped (files, directories, links...)
		//public int TotalItems{get;set;}
		public int[] ItemsByType{get;set;}
		public int SubCompletion{get;set;}

		public new string CurrentAction{
			get{return "";}
			set{
				HubNotificationEvent(this.Id, 700 /*snapshotting*/, value, "");
				//HubNotificationEvent(new NodeMessage{Context = MessageContext.Task, TaskId= this.Id, Action = MessageAction.TaskMessage, Data=value});
			}
		}
		
		/// <summary>
		/// passes back messages about backup processing to user, in order to send it to Hub for tracing and statistics purpose
		/// </summary> 
		public delegate void HubNotificationHandler(long taskId, int code, string data, string additionalMessage);
		//public delegate void HubNotificationHandler(NodeMessage message);
		public event HubNotificationHandler HubNotificationEvent;//User.HubSendTaskEvent()

		private int completionBase;
		public int CompletionBase{
			get{
				if(completionBase == 0)
					completionBase = GetCompletionBase();
				return completionBase;
			}
		}
		
		/*public BackupLevel Level{
			get{return this.bs.ScheduleTimes[0].Level;}	
		}*/
		
		public string Version{
			get{ return Utilities.PlatForm.Instance().NodeVersion;}	
			set{ ;}	
		}

		public string IndexFileName{
			get {return indexFileName; }
			set {indexFileName = value; }	
		}
		
		
		public List<BackupRootDrive> RootDrives{
			get{return backupRootDrives;}
		}
		
		public delegate void UpdateGUIHandler (string command, string param);
		public delegate void BackupDoneHandler();

		internal  Backup(){

		}

		// When deserializing Task()  from hub into Backup(), constructor is not called, thus members not initialized
		// force init thanks to this method
		internal void Init(){
			ItemsByType = new int[20];
			this.snapshotProviders = new List<ISnapshotProvider>();
		}
		~Backup(){
			Logger.Append(Severity.TRIVIA, "<TRACE> Backup destroyed.");	
		}

		/// <summary>
		/// Constructor for FULL backups. 
		/// </summary>
		/// <param name="bs">
		/// A <see cref="BackupSet"/>
		/// </param>
		/// <param name="ctaskId">
		/// A <see cref="System.Int32"/>
		/// </param>
		public Backup(BackupSet bs, long ctaskId) : base(bs, TaskStartupType.Manual){

		}


		internal void PrepareAll(){

			// lauch pre-backup commands/scripts
			string [] cmdOuts = ExecuteCommand(this.BackupSet.Preop);
			HubNotificationEvent(this.Id, 710, "STDOUT", cmdOuts[0]);
			HubNotificationEvent(this.Id, 710, "STDERR", cmdOuts[1]);

			// DEBUG print basepaths
			foreach(BasePath bsp in this.BackupSet.BasePaths)
				Console.WriteLine ("0##### bp path="+bsp.Path+", type="+bsp.Type);



			// Gather the FSs paths required by special objects (if there are any)
			//Dictionary<string, SPOMetadata> spoMetadatas = PrepareSpecialObjects();
			List< Tuple<string, SPOMetadata, List<string>>> spoMetadatas  = PrepareSpecialObjects();
			// Telling Special objects to tell the app they manage to put themselves into backup mode (if possble)
			// and/or freeze their IOs
			Logger.Append(Severity.INFO, "Freezing "+this.specialObjects.Count+" special objects...");
			foreach(ISpecialObject spo in this.specialObjects)
				spo.Freeze();


			SanitizePaths();


			// Now snapshot (if requested). 
			PrepareDrivesAndSnapshots();


			this.Index = new Index(this.Id, (this.Level != BackupLevel.Full));
			Index.Header = new IndexHeader{TaskId = this.Id, BackupType= this.Level};
			//Index.Header.RootDrives = backupRootDrives;
			Index.Create(backupRootDrives);//taskId, (this.Bs.ScheduleTimes[0].Level != BackupLevel.Full), this.RootDrives);
			Index.WriteHeaders();

			// Collect incremental providers metadata (for subsequent backups)
			foreach(BackupRootDrive brd in backupRootDrives){
				Dictionary<string, byte[]> provMetadata = IncrementalPluginProvider.SignalBackupBegin(this.Id, brd);
				if(provMetadata == null) continue;
				foreach(KeyValuePair<string, byte[]> kp in provMetadata){
					Logger.Append(Severity.TRIVIA, "Signaled backup to Incremental providers, got metadata from "+kp.Key);
					Index.AddProviderMetadata(kp.Key, brd, kp.Value);
				}
			}
			if(spoMetadatas != null){
				//foreach(KeyValuePair<string,SPOMetadata> spoMetadata in spoMetadatas){
				foreach(Tuple<string, SPOMetadata, List<string> > tuple in spoMetadatas){
					if(tuple.Item2 != null)
						using(MemoryStream mStr = new MemoryStream()){
							(new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter()).Serialize(mStr, tuple.Item2);
							Index.AddProviderMetadata(tuple.Item1, backupRootDrives[0], mStr.GetBuffer());
							Index.AddSpecialObject(tuple.Item1, tuple.Item3);
						}
					else
						Logger.Append(Severity.WARNING, "Could'nt save metadata from provider '"+tuple.Item1+"' : metadata is null");
				
				}
			}


		}
		
		/// <summary>
		/// This method first looks if backup needs snapshotting drives. If so, then we do snapshot
		/// and use it to call BuildBackup method
		/// </summary>
		private void PrepareDrivesAndSnapshots(){

			SnapshotSupportedLevel snapLevel = ConvertBackupLevelToSnLevel(this.Level);
			Logger.Append(Severity.INFO, "Preparing backup for Task "+this.Id);
			HubNotificationEvent(this.Id, 701 /*snapshotting*/, "", "");

			backupRootDrives = GetBackupInvolvedRootDrives();



			Console.WriteLine ("PrepareDrivesAndSnapshots()() backupRootDrives count="+backupRootDrives.Count);
			string[] snapshotProvNames = new string[backupRootDrives.Count];
			for(int i=0; i<backupRootDrives.Count; i++){
				if(backupRootDrives[i].RequiresSnapshot)
					snapshotProvNames[i] = SnapshotProvider.GetDriveSnapshotProviderName(backupRootDrives[i].SystemDrive.MountPoint);
				else
					snapshotProvNames[i] = "NONE";
			}

			// the final list of all snapshot needed to perform the backup set
			backupSnapshots = new List<ISnapshot>();

			// We try to make all snapshot in the shortest amount of time we can, 
			// for data to be as consistent as possible
			foreach(string snapProv in snapshotProvNames.Distinct()){
				Console.WriteLine ("PrepareDrivesAndSnapshots() snapshotProvNames="+snapProv);
				List<FileSystem> snapShotMemberDrives = new List<FileSystem>();
				for(int i=0; i< backupRootDrives.Count; i++){
					if(snapshotProvNames[i] == snapProv)
						snapShotMemberDrives.Add(backupRootDrives[i].SystemDrive);
				}
				//Gather SpecialObjects (VSS writers) if any
				var spoList = from BasePath bp in this.BackupSet.BasePaths
						where bp.Type != null && bp.Type.ToLower().StartsWith("object:")//P2PBackup.Common.BasePath.PathType.OBJECT
						select bp.Path;
				
				ISnapshotProvider snapProvider = SnapshotProvider.GetProvider(snapProv);
				snapshotProviders.Add(snapProvider);
				snapProvider.LogEvent += LogReceivedEvent; 
				ISnapshot[] volSnaps;
				try{

					volSnaps =  snapProvider.CreateVolumeSnapShot(snapShotMemberDrives, spoList.ToArray(), snapLevel);
					//if(snapProvider.Metadata != null)
					//	Index.Header.ProviderMetadata.Add(new Tuple<string, Hashtable>(snapProvider.Name, snapProvider.Metadata.Metadata));
					string volList = "";
					foreach(FileSystem vol in snapShotMemberDrives) volList += vol.MountPoint+",";
					Logger.Append(Severity.INFO, "Took snapshots (type "+snapProvider.Type+") of drives "+volList);
				}
				catch(Exception e){
					// we return a fake snapshot (snapshot path is the volume itself)
					string volList = "";
					foreach(FileSystem vol in snapShotMemberDrives)
						volList += vol.MountPoint+",";
					Logger.Append(Severity.WARNING, "Unable to take snapshot of drives "+volList+", falling back to fake provider. Error was : "+e.Message+" --- "+e.StackTrace);
					HubNotificationEvent(this.Id, 805 /*cant snapshot*/, volList, e.Message);
					ISnapshotProvider nullProv = SnapshotProvider.GetProvider("NONE");
					volSnaps = nullProv.CreateVolumeSnapShot(snapShotMemberDrives, spoList.ToArray(), snapLevel);
				}
				backupSnapshots.AddRange(volSnaps);
				snapProvider.LogEvent -= LogReceivedEvent; 
			}

			//finally, add snapshot to corresponding BackupRootDrive
			Console.WriteLine ("##### dumping rootdrives : ");
			foreach(BackupRootDrive rd in backupRootDrives){
				
				foreach(ISnapshot snap in backupSnapshots){
					if(/*snap.MountPoint*/snap.Path == rd.SystemDrive.MountPoint){
						rd.Snapshot = snap;
						Console.WriteLine("matched snapshot : "+snap.Path+", snap mount path="+snap.MountPoint);
						// let's change paths and excluded paths to their snapshotted values
						foreach(BasePath bp in rd.Paths){
							//bp.Path = snap.Path + bp.Path;
							/*if(snap.Name == "/")
								bp.Path = snap.Path;
							else
								bp.Path = bp.Path.Replace(snap.Name, snap.Path);*/
							for(int i=0; i< bp.ExcludedPaths.Count; i++){
								//bp.ExcludedPaths[i] = snap.Path +  bp.ExcludedPaths[i];
								if(snap.Path == "/")
									bp.ExcludedPaths[i] = snap.MountPoint + bp.ExcludedPaths[i];
								else{
									bp.ExcludedPaths[i] = bp.ExcludedPaths[i].Replace(snap.Path, snap.MountPoint);
									//bp.ExcludedPaths[i] = bp.ExcludedPaths[i].Replace(snap.Name, "");
									//bp.ExcludedPaths[i] = Path.Combine(snap.Path, bp.ExcludedPaths[i]);
									//bp.ExcludedPaths[i] = Path.GetFullPath(bp.ExcludedPaths[i]);
								}
							}
						}
						break;
					}
				}

				Console.WriteLine("BackupRootDrive id="+rd.ID+", mount="+rd.SystemDrive.MountPoint/*+", snapshot= "+rd.snapshot.Path*/);
				foreach(BasePath p in rd.Paths) {
					Console.WriteLine("\t p="+p.Path+", excludes : ");
					foreach(string s in p.ExcludedPaths)
						Console.WriteLine("\t excluded "+s);
					Console.WriteLine("\t\t, include patterns : ");
					foreach(string s in p.IncludePolicy)
						Console.WriteLine("\t match "+s);
				}	
			}
			Console.WriteLine ("#####   #####");
			foreach(BackupRootDrive rds in backupRootDrives){
				Logger.Append(Severity.DEBUG, "Drive '"+rds.SystemDrive.OriginalMountPoint+"' was snapshotted to '"+rds.Snapshot.MountPoint+"' using provider '"+rds.Snapshot.Type+"' with timestamp "+rds.Snapshot.TimeStamp+" ("+Utils.GetLocalDateTimeFromUnixTime(rds.Snapshot.TimeStamp)+")");
			}
		}	
		
		private SnapshotSupportedLevel  ConvertBackupLevelToSnLevel(BackupLevel backupLevel){
			if(backupLevel == BackupLevel.Full /*|| backupLevel == BackupLevel.SyntheticFull*/)
				return SnapshotSupportedLevel.Full;
			/*else if (backupLevel == BackupLevel.Differential)
				return SnapshotSupportedLevel.Differential;*/
			else if (backupLevel == BackupLevel.Refresh)
				return SnapshotSupportedLevel.Incremental;
			else
				return SnapshotSupportedLevel.Full;
		}
		
		/// <summary>
		/// Group basepaths by SPO type, call each SPO to inject relevant basepaths and
		/// expand/traduce it as additional basepaths
		/// </summary>
		private List< Tuple<string, SPOMetadata, List<string> >> PrepareSpecialObjects(){

			List<string> spoProviders = SPOProvider.ListAvailableProviders();
			//Dictionary<string, SPOMetadata> spoMetadatas = new Dictionary<string, SPOMetadata>();
			List< Tuple<string, SPOMetadata, List<string> >> spoMetadatas = new List<Tuple<string, SPOMetadata, List<string>>>();
			this.specialObjects = new List<ISpecialObject>();
			Console.WriteLine ("PrepareSpecialObjects() : 1");
			foreach(string provName in spoProviders){
				if(string.IsNullOrEmpty(provName)) continue;
				var provItems = (from BasePath b in this.BackupSet.BasePaths 
						where b!= null && !(string.IsNullOrEmpty(b.Type)) && b.Type.ToLower().StartsWith("object:") //BasePath.PathType.OBJECT 
				        && b.Path != null && b.Type.ToLower().Substring(b.Type.IndexOf(":")+1) == provName.ToLower()
						select b.Path).ToList<string>();
				Console.WriteLine ("PrepareSpecialObjects() : 2");
				if(provItems == null || provItems.Count == 0) continue;
				Console.WriteLine ("PrepareSpecialObjects() : 3 : "+string.Join(",", provItems));
				ISpecialObject spo = SPOProvider.GetByCategory(provName, this, this.Level, this.BackupSet.ProxyingInfo);

				Console.WriteLine ("PrepareSpecialObjects() : 4");
				spo.SetItems(provItems);
				specialObjects.Add(spo);
				//spoMetadatas.Add(spo.Name, spo.Metadata);
				spoMetadatas.Add(new Tuple<string, SPOMetadata, List<string>>(spo.Name, spo.Metadata, spo.ExplodedComponents));
				this.BackupSet.BasePaths.AddRange(spo.BasePaths);
				/*foreach(BasePath bp in spo.BasePaths){
					Console.WriteLine ("*** basepath="+bp.Path+", includepol="+bp.IncludePolicy+", recursive="+bp.Recursive);	
				}*/

			}
			//int beforeFatorize = bs.BasePaths.Count;
			/*Console.WriteLine ("bs count1="+beforeFatorize);
			FactorizePaths();
			FactorizePaths();*/
			return spoMetadatas;
		}
		
		/// <summary>
		/// Factorizes all BasePaths by merging them when possible.
		/// </summary>
		private void FactorizePaths(StringComparison comp){
			//Console.WriteLine ("bs null? : "+(bs == null));
			//Console.WriteLine ("bs basep null?: "+(bs.BasePaths == null));
			int beforeFatorize = this.BackupSet.BasePaths.Count;
			Console.WriteLine ("bs count2 before factorize="+beforeFatorize);
			this.BackupSet.BasePaths = (	from BasePath b in this.BackupSet.BasePaths 
			                	where b!=null && b.Path!=null
			               		// orderby b.Path.Length descending 
			               		// thenby by.Path
			                	select b
			                )
							.OrderBy/*Descending*/(path => path.Path.Length)
							.ThenBy(path=>path.Path)
							.ToList();//bs.BasePaths.OrderByDescending(path => path.Path).ToList();
		
			/*for(int i = bs.BasePaths.Count-1; i >= 1; i--){
				if(bs.BasePaths[i].Type != bs.BasePaths[i-1].Type)
					continue;
				if(string.Equals(bs.BasePaths[i].Path, bs.BasePaths[i-1].Path, comp)){
					bs.BasePaths[i-1] = MergeRules(bs.BasePaths[i-1], bs.BasePaths[i]);
					bs.BasePaths[i-1].Path = ExpandFilesystemPath(bs.BasePaths[i-1].Path);
					bs.BasePaths.RemoveAt(i);
				}
				else if(bs.BasePaths[i].Path.IndexOf(bs.BasePaths[i-1].Path, comp) == 0 
						&&  bs.BasePaths[i-1].Recursive
						//&& (bs.BasePaths[i-1].IncludePolicy == null ||  bs.BasePaths[i-1].IncludePolicy.Count == 0)
						&& !bs.BasePaths[i-1].ExcludedPaths.Contains(bs.BasePaths[i].Path)){ // todo : case insensitive on nt
					bs.BasePaths[i-1] = MergeRules(bs.BasePaths[i-1], bs.BasePaths[i]);
					bs.BasePaths.RemoveAt(i);
				}
				else // just expand the path
					bs.BasePaths[i].Path = ExpandFilesystemPath(bs.BasePaths[i].Path);
			}*/
			for(int i = this.BackupSet.BasePaths.Count-1; i >= 0; i--){
				foreach(BasePath baseP in this.BackupSet.BasePaths)
				if(baseP.CanSwallow(this.BackupSet.BasePaths[i], comp)){
					this.BackupSet.BasePaths.RemoveAt(i);
						//i--;
						break;
					}
			}
			Logger.Append(Severity.TRIVIA, "Factorized and reduced paths from "+beforeFatorize+" to "+this.BackupSet.BasePaths.Count);
		}
		
		/*private BasePath MergeRules(BasePath first, BasePath second){
			first.ExcludedPaths.AddRange(second.ExcludedPaths);
			//first.ExcludePolicy.AddRange(second.ExcludePolicy);
			//first.IncludePolicy.AddRange(second.IncludePolicy);
			//if(first.Recursive || second.Recursive)
			//	first.Recursive = true;
			return first;
		}*/
					
		private void FactorizeBasePathRules(){
			
		}
		
		/// <summary>
		/// Replaces environment variables (if found) by their values
		/// </summary>
		/// <param name='path'>
		/// Path.
		/// </param>
		private string ExpandFilesystemPath(string path){
			//string[] pathItems = path.Split(new char[]{Path.DirectorySeparatorChar}, StringSplitOptions.RemoveEmptyEntries);
			//foreach(string item in pathItems){
				return Environment.ExpandEnvironmentVariables(path);
			//}
			
		}
		/// <summary>
		/// Gets the completion base as number of depth-1 subdirectories.
		/// Used to calculate backup completion %
		/// </summary>
		/// <returns>
		/// The completion base.
		/// </returns>
		private int GetCompletionBase(){
			int cBase = 0;
			if(backupRootDrives == null) return 0; // storage layout not yet initialized
			try{
				foreach(BackupRootDrive brd in backupRootDrives){
					foreach(BasePath bp in brd.Paths){
						foreach(string f in Directory.EnumerateDirectories(bp.Path, "*", SearchOption.TopDirectoryOnly))
							cBase++;
						}
						
				}
			}catch{} // will throw exception on proxied backups 
			return cBase+1;
		}
		
		
		public void Terminate(bool isSuccessful){
			Logger.Append(Severity.DEBUG, "Calling Terminate");
			//if( isSuccessful && bs.BackupTimes[0].Type == BackupType.Full)
			//		IncrementalProvider.SignalFullBackup();	
			// delete snapshots

			try{
				// Execute post-backup commands
				string[] postCmdOut = ExecuteCommand(this.BackupSet.Postop);
				HubNotificationEvent(this.Id, 711, "STDOUT", postCmdOut[0]);
				HubNotificationEvent(this.Id, 711, "STDERR", postCmdOut[1]);
				Logger.Append(Severity.DEBUG, "Disposing storage layout builders...");
				if(slManager != null)
					slManager.Dispose();
				foreach(ISnapshotProvider prov in snapshotProviders)
					if(prov != null)
						prov.Dispose();
				snapshotProviders = null;
			}
			catch(Exception e){
				Logger.Append(Severity.ERROR, "Error cleaning task resources : "+e.ToString ());
			}
			if(this.BackupSet.SnapshotRetention == 0)
				foreach(BackupRootDrive rootDrive in this.backupRootDrives){
					try{
						//string prov = SnapshotProvider.GetDriveSnapshotProviderName(rootDrive.SystemDrive.MountPoint);
						SnapshotProvider.GetProvider(rootDrive.Snapshot.Type).Delete(rootDrive.Snapshot);
						//SnapshotProvider.GetProvider(prov)

					}
					catch(Exception e){
						AddHubNotificationEvent(906, rootDrive.Snapshot.Path, e.Message);	
					}
				}
			Logger.Append(Severity.DEBUG, "Terminated");
		}

		private bool IsDriveExcluded(BasePath bp, FileSystem sd){
			foreach(string exclude in bp.ExcludedPaths){
				//Console.WriteLine ("Searching if "+sd.MountPoint+" is excluded from rule "+exclude);
				if(sd.MountPoint.IndexOf(exclude) == 0){
					//Console.WriteLine ("(yes)");
					return true;
				}
			}
			return false;
		}

		private void SanitizePaths(){
			foreach(BasePath bp in this.BackupSet.BasePaths){
				if(bp.Type == null || bp.Type.ToLower() == "fs" || bp.Type.ToLower() == "fs:")
					bp.Type = "FS";
			}
		}


		private List<BackupRootDrive> GetBackupInvolvedRootDrives(){

			// DEBUG print basepaths
			/*foreach(BasePath bsp in bs.BasePaths)
				Console.WriteLine ("2##### bp path="+bsp.Path+", type="+bsp.Type);
			Console.ReadLine();*/
				                
			Int16 rootDriveId = 0;
			char[] pathSeparators = new char[]{Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, '/', '\\'};


			// gather all paths to backup and send them to the storagelayout (allows it to build a partial layout including only the layout necessary for this task)
			var taskPathsToBackup = (from bp in this.BackupSet.BasePaths 
			                        where bp != null && !string.IsNullOrEmpty(bp.Type) && bp.Type.ToLower().StartsWith("fs") 
			                    	select bp.Path).ToList();

			slManager = new StorageLayoutManager(taskPathsToBackup);
			slManager.LogEvent += this.LogReceivedEvent;
			//List<FileSystem> allFSes = new List<FileSystem>();
			//string storageP = bs.BasePaths[0].
			//Logger.Append (Severity.TRIVIA, "Building storage layout using provider '"+storageProvs[0]+"'...");

			this.StorageLayout = slManager.BuildStorageLayout(this.BackupSet.StorageLayoutProvider, this.BackupSet.ProxyingInfo);
			//sl.GetAllFileSystems(sl.Entries, ref allFSes);

			Logger.Append(Severity.DEBUG, "Got "+this.StorageLayout.GetAllFileSystems(null).Count()+" raw FSes");

			// we sort drives by reverse mountpoint length : on unix this allows us to find, for example,
			// '/home' before '/' when asked to backup '/home/user'
			var fsMountsByNameLength = from sd in this.StorageLayout.GetAllFileSystems(null) 
											where (! string.IsNullOrEmpty(sd.MountPoint))
											orderby sd.MountPoint.Length descending 
											select sd;

			Logger.Append(Severity.DEBUG, "Got "+fsMountsByNameLength.Count()+" FSes");
			List<BackupRootDrive> drives = new List<BackupRootDrive>();
			// We first expand the basepaths defined by backupset configuration
			//  in order to manage nested mountpoints (eg : backups tells to save /usr, but this path contains /usr/local 
			//  which is a mountpoint to another drive



			for(int i= this.BackupSet.BasePaths.Count-1; i>=0; i--){

				if(this.BackupSet.BasePaths[i].Type != null && this.BackupSet.BasePaths[i].Type.ToLower().StartsWith("object:"))//BasePath.PathType.OBJECT)
					continue;
				foreach(FileSystem filesystem in fsMountsByNameLength){
					if(filesystem.DriveFormat == "proc" || filesystem.DriveFormat == "sysfs" || filesystem.DriveFormat == "debugfs"
					   		|| filesystem.DriveFormat == "devpts" || filesystem.DriveFormat == "procfs"){
							Logger.Append (Severity.TRIVIA, "GetBackupInvolvedRootDrives() : excluded non-backupable fs  "+filesystem.MountPoint);
						Logger.Append(Severity.INFO, "Excluded fs "+filesystem.MountPoint+" from "+this.BackupSet.BasePaths[i].Path+" (non-backupable fs)");
						this.BackupSet.BasePaths[i].ExcludedPaths.Add(filesystem.MountPoint);
							continue;
					}
					//Console.WriteLine ("basepath :i="+i+", lulute 1");
					//if(IsDriveExcluded(bs.BasePaths[i], filesystem)) continue;

					if(string.IsNullOrEmpty(filesystem.OriginalMountPoint)){
						Logger.Append(Severity.NOTICE, "Proxied Filesystem '"+filesystem.MountPoint+"' has unknown original mountpoint, will be backuped with current mountpoint as root"); 
						filesystem.OriginalMountPoint = filesystem.MountPoint;
					}
					//Console.WriteLine ("basepath :i="+i+", lulute 2");
					if(this.BackupSet.BasePaths[i].Path == "*" 
					   || filesystem.OriginalMountPoint.IndexOf(this.BackupSet.BasePaths[i].Path) == 0 && this.BackupSet.BasePaths[i].Path != filesystem.Path ){
						BasePath bp = new BasePath();
						bp.Path = filesystem.MountPoint;
						bp.Type = "FS"; //BasePath.PathType.FS;
						// inherit include/exclude rules
						bp.IncludePolicy = this.BackupSet.BasePaths[i].IncludePolicy;
						bp.ExcludePolicy = this.BackupSet.BasePaths[i].ExcludePolicy;
						this.BackupSet.BasePaths.Add(bp);
						Logger.Append (Severity.TRIVIA, "Expanded config path "+this.BackupSet.BasePaths[i].Path+" to "+bp.Path);


					}
					//Console.WriteLine ("basepath :i="+i+", lulute 3");
				}
				// remove original wildcard basepaths, now they have been expanded
				if(this.BackupSet.BasePaths[i].Path == "*" )
					this.BackupSet.BasePaths.RemoveAt(i);
			}

			StringComparison sComp;
			if(!Utilities.PlatForm.IsUnixClient())
				sComp = StringComparison.InvariantCultureIgnoreCase;
			else
				sComp = StringComparison.InvariantCulture;

			FactorizePaths(sComp);
			FactorizePaths(sComp);
			//debug print raw basepaths
			foreach(BasePath basep in this.BackupSet.BasePaths)
				Console.WriteLine ("¤¤¤¤¤¤¤  raw basepath : "+basep.ToString());



			// SYSTEM EXCLUDES HERE!!!
			// get  system-wide exclusions rules, expand and them apply them.
			// we re-generate the BasePaths list for the last time.
			List<string> systemExcludes = PathExcluderFactory.GetPathsExcluder().GetPathsToExclude(/*this.HandledBy>0*/);
			for(int i=0; i< systemExcludes.Count; i++){
				systemExcludes[i] = ExpandFilesystemPath(systemExcludes[i]);
				Console.WriteLine ("sys excl : ~~~~~~~~~~~~~"+systemExcludes[i]);
			}
			// order paths by length desc : avoids adding an exclude rule 'c:\mydatadir\2' on 'c:\' instead of 'c:\mydatadir\'
			// since DESC length ordering will return' c:\mydatadir' before 'c:\'
			//var basePathsByLength = bs.BasePaths.OrderByDescending( path=> path.Path.Length);
			foreach(BasePath sortedBp in this.BackupSet.BasePaths.OrderByDescending( path=> path.Path.Length)){
				for(int j = systemExcludes.Count-1; j>=0; j--){
					// TODO!! on windows and only windows, use StringComparison.InvariantCultureIgnoreCase
					if(systemExcludes[j].IndexOf(sortedBp.Path, sComp) == 0){
						sortedBp.ExcludedPaths.Add(systemExcludes[j]);
						systemExcludes.RemoveAt(j);
					}

				}
			}


			foreach(BasePath path in this.BackupSet.BasePaths){

				// search if filesystem matches wanted backup path
				foreach(FileSystem fsMount in fsMountsByNameLength){
					//Console.WriteLine ("basepath.Path="+path.Path+", current fs path="+fsMount.Path+", mntpt="+fsMount.MountPoint+", origmntpt="+fsMount.OriginalMountPoint);
					// if drive is explicitely deselected (excluded), don't add it to list
					//if(IsDriveExcluded(path, sysDrive))
					//		continue;

					// special case of "*" has been treated before and splitted into 1 BasePath per filesystem, so ignore it
					if(	path.Path == "*") {
						Console.WriteLine ("WOW WOW WOW met '*' entry, should have been expanded before!!!!  "+path.Path+", type="+path.Type);
						//Console.ReadLine();

						continue;
					}


					if(	/*path.Path == "*"
						||*/ path.Path.IndexOf(fsMount.MountPoint, sComp) == 0 
					   	|| (path.Path.EndsWith(Path.PathSeparator+"*") 
					    		&& path.Path.Substring(0, path.Path.LastIndexOfAny(pathSeparators)) == 
					 			fsMount.Path.Substring(0, fsMount.Path.LastIndexOfAny(pathSeparators)))
					   ){
						//Console.WriteLine ("basepath.Path="+path.Path+" MATCHES");
						// 1/2 exclude weird/pseudo fs
						if(fsMount.DriveFormat == "proc" || fsMount.DriveFormat == "sysfs" || fsMount.DriveFormat == "debugfs"
					   		|| fsMount.DriveFormat == "devpts" || fsMount.DriveFormat == "procfs"){
							Console.WriteLine ("GetBackupInvolvedRootDrives() : excluded mountpoint  "+path.Path+" (non-backupable fs)");
							continue;
						}
						
						/*if(udi.DriveFormat */
						
						// first pass : add basepaths as defined in backupset configuration
						// if drive doesn't exist yet in rootdrives, add it. Else, add path to existing rootdrive
						bool found = false;
						foreach(BackupRootDrive rd in drives){
							//Console.WriteLine (" @@@@ GetBInvoldedRd :cur rd="+rd.SystemDrive.OriginalMountPoint+", fsmountpath="+fsMount.Path);
							if(rd.SystemDrive.OriginalMountPoint == fsMount.OriginalMountPoint){
								rd.Paths.Add(path);
								found = true;
							}
						}
						if(found == false/* && !IsDriveExcluded(path, sysDrive)*/){
							Console.WriteLine (" @@@@ GetBInvoldedRd : added new rootdrives to list : "+fsMount.OriginalMountPoint+" for path "+path.Path);
							BackupRootDrive rootDrive = new BackupRootDrive();
							rootDrive.SystemDrive = fsMount;
							rootDrive.Paths.Add(path);
							rootDrive.ID = rootDriveId;
							drives.Add(rootDrive);
							rootDriveId++;
						}
						//Console.WriteLine("match : path "+path.Path+", drive "+sysDrive.MountPoint);
						break; //avoid continuing scanning  until '/' (would be false positive)
					}
				}
			}
			return drives;
		}
				

		/// <summary>
		/// Executes the command. Used for backup tasks pre- ans post- commands
		/// </summary>
		/// <returns>
		/// The command outputs, as : [0] = stdout, [1] = stderr
		/// </returns>
		/// <param name='cmd'>
		/// The command(s) to be executed
		/// </param>
		private string[] ExecuteCommand(string cmd){
			string[] outputs = new string[2]; // stdout and stderr
			if(cmd == null || cmd == string.Empty) return outputs;
			ProcessStartInfo pi;
			if(Utilities.PlatForm.IsUnixClient())
				pi = new ProcessStartInfo("/bin/sh", "-c \""+@cmd+"\"");
			else
				pi = new ProcessStartInfo("cmd.exe", "/c \""+@cmd+"\"");
			pi.RedirectStandardOutput = true;
			pi.RedirectStandardError = true;
			pi.UseShellExecute = false;
			Process p = Process.Start(pi);
			pi.CreateNoWindow = true;
			//p.ExitCode;
			outputs[0] = p.StandardOutput.ReadToEnd();
			outputs[1] = p.StandardError.ReadToEnd();
			p.WaitForExit();
			return outputs;
		}
		
		public int CompletionPercent{
			get{
				if(CompletionBase == 0) return 0;
				return (int)(Math.Round((double)SubCompletion/CompletionBase,2)*100);
			}
		}
		
		internal void AddHubNotificationEvent(int code, string message, string additionalData){
			HubNotificationEvent(this.Id, code, message, additionalData);	
		}

		private void LogReceivedEvent(object sender, LogEventArgs args){
			Logger.Append(args.Severity, args.Message);
			if(args.Code >0){
				HubNotificationEvent(this.Id, args.Code, "", args.Message);
			}
			
		}

		/*public void Dispose(){
			slManager.Dispose();
		}*/

	}
}
