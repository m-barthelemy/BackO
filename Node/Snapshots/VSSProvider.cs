using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Alphaleonis.Win32.Vss;
using Alphaleonis.Win32.Security;
using Node.Misc;
using Node.Utilities;
using P2PBackup.Common;
using P2PBackup.Common.Volumes;

namespace Node.Snapshots{

	public class VSSProvider:ISnapshotProvider, IDisposable{
		
		private SnapshotCapabilities[] caps;
		private SnapshotSupportedLevel[] levels;
		private /*static*/ IVssBackupComponents backup;
		//Hashtable metadata;
		public SPOMetadata Metadata{get;set;}
		public string Name{get{return "VSS";}}

		public delegate void LogHandler(int code, Severity severity, string message);
		public event EventHandler<LogEventArgs> LogEvent;

		public VSSProvider(){
			
		}
		
		public List<ISnapshot> ListSnapshottable(string path){
			return new List<ISnapshot>();	
		}
		
		public List<ISnapshot> ListSpecialObjects(){
			List<ISnapshot> alreadyExisting = new List<ISnapshot>();
			IVssImplementation vssImplementation = VssUtils.LoadImplementation();
			using (IVssBackupComponents backup = vssImplementation.CreateVssBackupComponents()){
				backup.InitializeForBackup(null);
				using (IVssAsync async = backup.GatherWriterMetadata()){
	                async.Wait();
	                async.Dispose();
            	}

	            if (OperatingSystemInfo.IsAtLeast(OSVersionName.WindowsServer2003)){
					// The only context supported on Windows XP is VssSnapshotContext.All 
					//backup.SetContext(VssSnapshotContext.Backup);
					backup.SetContext(VssSnapshotContext.AppRollback|VssSnapshotContext.All);
				
				}
				//backup.SetBackupState(false,  true, VssBackupType.Full, false);
				//Guid snapID = backup.StartSnapshotSet();
				//backup.AddToSnapshotSet(@"C:\");
	            foreach (IVssExamineWriterMetadata writer in backup.WriterMetadata){
					VSSSnapshot s = new VSSSnapshot();
					s.Path = writer.WriterName;
					s.Type = (writer.Source == VssSourceType.TransactedDB || writer.Source == VssSourceType.NonTransactedDB)? "VSSDB" : "VSSWriter";
					s.Version = writer.Version.Major+"."+writer.Version.Minor+"."+writer.Version.MajorRevision+"."+writer.Version.MinorRevision+writer.Version.Revision;
					s.MountPoint = writer.WriterName;
					foreach(IVssWMComponent cmp in writer.Components){
						VSSSnapshot childS = new VSSSnapshot();
						childS.Path = cmp.ComponentName;
						childS.Icon = cmp.GetIcon();
						childS.Type = (cmp.Type == VssComponentType.Database)? "VSSDB" : "VSSFileGroup";
						if(!cmp.Selectable) childS.Type = "spoDisabled";
						childS.MountPoint = cmp.LogicalPath;
						childS.Disabled = true;
						if (OperatingSystemInfo.IsAtLeast(OSVersionName.WindowsServer2003)){
							foreach(VssWMDependency dep in cmp.Dependencies){
								Logger.Append(Severity.INFO, "TODO: vss component "+cmp.ComponentName+" has dependancy on "+dep.ComponentName);	
							}
						}
						if(cmp.Selectable)
							childS.Disabled = false;
						s.AddChildComponent(childS);
					}
					alreadyExisting.Add(s);
					
				}			
				//backup.QueryProviders
				foreach (IVssWriterComponents wc in   backup.WriterComponents){
					
					foreach(IVssComponent c in wc.Components){
						VSSSnapshot s = new VSSSnapshot();
						s.Path = c.ComponentName;
						s.Type = c.ComponentType.ToString();
						alreadyExisting.Add(s);
					}
				}
			}
			return alreadyExisting;
		}
		
		/*public VSSSnapshot Create(SnapshotSupportedLevel level){
			
			
			return new VSSSnapshot();
		}*/
		
		public VSSSnapshot CreateWriterSnapshot(List<string> writerPaths){
			return new VSSSnapshot();
		}
		
		internal ISnapshot[] CreateVolumeSnapShot(List<FileSystem> volumeNames, SnapshotSupportedLevel level){
			return CreateVolumeSnapShot(volumeNames, null, level);	
		}
		
		public ISnapshot[] CreateVolumeSnapShot(List<FileSystem> volumes, string[] spoPaths, SnapshotSupportedLevel level){
			using(new Alphaleonis.Win32.Security.PrivilegeEnabler(Privilege.Backup, Privilege.Restore)){
			//PrivilegesManager pm = new PrivilegesManager();
			//pm.Grant();
			VssBackupType vssLevel = VssBackupType.Full;
			if(level == SnapshotSupportedLevel.Full)
					vssLevel = VssBackupType.Full;
			else if (level == SnapshotSupportedLevel.Incremental)
					vssLevel = VssBackupType.Incremental;
			else if (level == SnapshotSupportedLevel.Differential)
					vssLevel = VssBackupType.Differential;
			else if (level == SnapshotSupportedLevel.TransactionLog)
					vssLevel = VssBackupType.Log;
			ArrayList snapshots = new ArrayList();
			Metadata = new SPOMetadata();
			bool snapshotSuccedeed = false;
			try{
				IVssImplementation vss = VssUtils.LoadImplementation();
				backup = vss.CreateVssBackupComponents();
				Logger.Append(Severity.DEBUG, "0/6 Initializing Snapshot ("+ ((spoPaths == null)? "NON-component mode" : "component mode")+", level "+level+")");
	         	backup.InitializeForBackup(null);
				if(spoPaths == null) // component-less snapshot set
						backup.SetBackupState(false, true, VssBackupType.Full, false);
				else
						backup.SetBackupState(true, true, vssLevel, (vssLevel == VssBackupType.Full)?false:true );
				if (OperatingSystemInfo.IsAtLeast(OSVersionName.WindowsServer2003)){
						// The only context supported on Windows XP is VssSnapshotContext.Backup 
						backup.SetContext(VssSnapshotContext.AppRollback/*|VssSnapshotContext.All*/);
				}
				Logger.Append(Severity.DEBUG, "1/6 Gathering writers metadata and status");
				using (IVssAsync async = backup.GatherWriterMetadata()){
	                async.Wait();
	                async.Dispose();
            	}
				// gather writers status before adding backup set components
				using (IVssAsync async = backup.GatherWriterStatus()){
					async.Wait();
					async.Dispose();
				}
				Logger.Append(Severity.DEBUG, "2/6 Adding writers and components");
				// Now we add the components (vss writers, writer paths/params) of this snapshot set
				if(spoPaths != null){
					foreach (IVssExamineWriterMetadata writer in backup.WriterMetadata){
						foreach(string spo in spoPaths){
							//Logger.Append (Severity.TRIVIA, "Searching writer and/or component matching "+spo);
							int index = spo.IndexOf(writer.WriterName);
							if(index <0 && spo != "*")
								continue;
							bool found = false;
							Logger.Append (Severity.TRIVIA, "Found matching writer "+writer.WriterName+", instance name="+writer.InstanceName);
							// First we check that the writer's status is OK, else we don't add it to avoid failure of complete snapshot if it's not
							bool writerOk = false;
							foreach (VssWriterStatusInfo status in backup.WriterStatus){
								if(status.Name == writer.WriterName){
									Logger.Append(Severity.TRIVIA, "Checking required writer "+status.Name
										+", status="+status.State.ToString()+", error state="+status.Failure.ToString());
									if(status.State == VssWriterState.Stable && status.Failure == VssError.Success) // if we get there it means that we are ready to add the wanted component to VSS set 				
										writerOk = true;
									else{
										Logger.Append(Severity.ERROR, "Cannot add writer "+status.Name+" to snapshot set,"
											+" status="+status.State.ToString()+". Backup data  managed by this writer may not be consistent" );
											if(LogEvent!=null) LogEvent(this, new LogEventArgs(820, Severity.WARNING, status.Name+", Status="+status.State.ToString()+", Failure="+status.Failure.ToString()));
									}
								}	
							}
							if(!writerOk){
								if(OperatingSystemInfo.IsAtLeast(OSVersionName.WindowsServer2003))
									backup.DisableWriterClasses(new Guid[]{writer.WriterId});
								continue;
							}
							bool addAllComponents = false;
							if(spo.Length == index+writer.WriterName.Length || spo == "*" || spo == ""+writer.WriterName+@"\*")
									addAllComponents = true;
							foreach (IVssWMComponent component in writer.Components){
								found = false;
								//Console.WriteLine("createvolsnapshot : current component is :"+component.LogicalPath+@"\"+component.ComponentName);
								if((!addAllComponents) && spo.IndexOf(component.LogicalPath+@"\"+component.ComponentName) < 0)
									continue;
								//Logger.Append (Severity.TRIVIA, "Asked to recursively select all '"+writer.WriterName+"' writer's components");
								if(OperatingSystemInfo.IsAtLeast(OSVersionName.WindowsServer2003)){
									foreach(VssWMDependency dep in  component.Dependencies){
										Logger.Append (Severity.TRIVIA, "Component "+component.ComponentName+" depends on component "+dep.ComponentName);
									}
								}
								if(component.Selectable)
									backup.AddComponent(writer.InstanceId, writer.WriterId, component.Type, component.LogicalPath, component.ComponentName);
								Logger.Append (Severity.INFO, "Added writer '"+writer.WriterName+"' component "+component.ComponentName);
								found = true;
								
								// Second we need to find every drive containing files necessary for writer's backup
								// and add them to drives list, in case they weren't explicitely selected as part of backuppaths
								List<VssWMFileDescription> componentFiles = new List<VssWMFileDescription>();
								componentFiles.AddRange(component.Files);
								componentFiles.AddRange(component.DatabaseFiles);
								componentFiles.AddRange(component.DatabaseLogFiles);
								foreach (VssWMFileDescription file in componentFiles){
									if(string.IsNullOrEmpty(file.Path)) continue;
									//Console.WriteLine ("component file path="+file.Path+", alt backuplocation="+file.AlternateLocation
									//		+", backuptypemask="+file.BackupTypeMask.ToString()+", spec="+file.FileSpecification+", recursive="+file.IsRecursive);
									// TODO : Reuse GetInvolvedDrives (put it into VolumeManager class)
	                                string drive = file.Path.Substring(0, 3).ToUpper();
	                                if (drive.Contains(":") && drive.Contains("\\")){
										var searchedVol = from FileSystem vol in volumes
											where vol.MountPoint.Contains(drive)
											select vol;
	                                    //if(!volumes.Contains(drive)){
										if(searchedVol == null){
											Logger.Append(Severity.INFO, "Select VSS component "+component.LogicalPath
											              +@"\"+component.ComponentName+" requires snapshotting of drive "+drive+", adding it to the list.");
										   	volumes.Add(searchedVol.First());
										}
	                                    break;
	                                }
		                        }
								
								//Logger.Append(Severity.TRIVIA, "Added writer/component "+spo);
	                       		//break;
		                    }	
							//metadata.Metadata.Add(writer.SaveAsXml());
							Metadata.Metadata.Add(writer.WriterName, writer.SaveAsXml());
							if(found == false)
								Logger.Append(Severity.WARNING, "Could not find VSS component "+spo+" which was part of backup paths");
						}
					}
				}
				Logger.Append(Severity.DEBUG, "3/6 Preparing Snapshot ");
				//backup.SetBackupState(false,  true, VssBackupType.Full, false);
				Guid snapID = backup.StartSnapshotSet();
				//Guid volID = new Guid();
				foreach(FileSystem volume in volumes){
					VSSSnapshot snapshot = new VSSSnapshot();
						snapshot.Type = this.Name;
					Logger.Append(Severity.DEBUG, "Preparing Snapshot of "+volume.MountPoint);
					if(volume.MountPoint != null && backup.IsVolumeSupported(volume.MountPoint) ){
						snapshot.Id = backup.AddToSnapshotSet(volume.MountPoint);
						
						snapshot.Path = volume.MountPoint;
						
					}
					else{ // return the fake provider to get at least a degraded backup, better than nothing
						Logger.Append(Severity.WARNING, "Volume '"+volume.MountPoint+"' is not snapshottable (or null). Backup will be done without snapshot, risks of data inconsistancy.");
						ISnapshotProvider fakeSnapProvider = SnapshotProvider.GetProvider("NONE");
						List<FileSystem> fakeList = new List<FileSystem>();
						fakeList.Add(volume);
						snapshot = (VSSSnapshot)fakeSnapProvider.CreateVolumeSnapShot(fakeList, null, SnapshotSupportedLevel.Full)[0];
						
					}
					if(snapshot.Id == System.Guid.Empty)
							Logger.Append(Severity.ERROR, "Unable to add drive "+volume.MountPoint+" to snapshot set (null guid)");
					else
							Logger.Append(Severity.TRIVIA, "Drive "+volume.MountPoint+" will be snapshotted to "+snapshot.Id);
					snapshots.Add(snapshot);
				}
				Logger.Append(Severity.DEBUG, "4/6 Calling Prepare...");
				using (IVssAsync async = backup.PrepareForBackup()){
					async.Wait();
					async.Dispose();
				}
				Logger.Append(Severity.DEBUG, "5/6 Snapshotting volumes");
				using (IVssAsync async = backup.DoSnapshotSet()){
					async.Wait();
					async.Dispose();
				}
				//if(OperatingSystemInfo.IsAtLeast(OSVersionName.WindowsServer2003))
					foreach(IVssExamineWriterMetadata w in backup.WriterMetadata){
						foreach(IVssWMComponent comp in w.Components)
							try{
								backup.SetBackupSucceeded(w.InstanceId, w.WriterId, comp.Type, comp.LogicalPath, comp.ComponentName, true);
								Logger.Append (Severity.TRIVIA, "Component "+comp.ComponentName+" has been notified about backup success.");
							}
							catch(Exception){
								//Logger.Append (Severity.WARNING, "Could not notify component "+comp.ComponentName+" about backup completion : "+se.Message);	
							}
					}
				//Node.Misc.VSSObjectHandle.StoreObject(backup);
				try{
					//on XP backupcomplete consider that we have done with the snapshot and releases it.	
					//if(OperatingSystemInfo.IsAtLeast(OSVersionName.WindowsServer2003)){
						backup.BackupComplete();
					
						Metadata.Metadata.Add("_bcd_", backup.SaveAsXml());
					//}
					
				}catch(Exception bce){
						Logger.Append(Severity.WARNING, "Error calling VSS BackupComplete() : "+bce.Message);
				}
					
				using (IVssAsync async = backup.GatherWriterStatus()){
					async.Wait();
					async.Dispose();
				}
				
				Logger.Append(Severity.DEBUG, "6/6 Successfully shapshotted volume(s) ");
				
				foreach(ISnapshot sn in snapshots){
					if(sn.Id == Guid.Empty)
						continue;
					sn.MountPoint = backup.GetSnapshotProperties(sn.Id).SnapshotDeviceObject;
					sn.TimeStamp = Utilities.Utils.GetUtcUnixTime(backup.GetSnapshotProperties(sn.Id).CreationTimestamp.ToUniversalTime());

						/*
					DirectoryInfo snapMountPoint = Directory.CreateDirectory( Path.Combine(Utilities.ConfigManager.GetValue("Backups.TempFolder"), "snapshot_"+sn.Id));
					Logger.Append(Severity.DEBUG, "Mounting shapshotted volume '"+sn.Name+"' to '"+snapMountPoint.FullName+"'");
					backup.ExposeSnapshot(sn.Id, null, VssVolumeSnapshotAttributes.ExposedLocally, snapMountPoint.FullName);
					//sn.Path = snapMountPoint.FullName+Path.DirectorySeparatorChar;
					//sn.Path = @"\\?\Volume{"+sn.Id+"}"+Path.DirectorySeparatorChar;*/
				}
			}
			catch(Exception e){
				try{
					//backup.BackupComplete();
					backup.AbortBackup();
				}
				catch(Exception ae){
						Logger.Append(Severity.ERROR, "Error trying to cancel VSS snapshot set : "+ae.Message);
				}
				
				// TODO !! report snapshoty failure to hub task
				Logger.Append(Severity.ERROR, "Error creating snapshot :'"+e.Message+" ---- "+e.StackTrace+"'. Backup will continue without snapshot. Backup of VSS components will fail. !TODO! report that to hub");
				backup.Dispose();
				throw new Exception(e.Message);
			}
			finally{
				// TODO !!! reactivate dispose
				//backup.Dispose();
				
				//pm.Revoke();
			}
			
			return (ISnapshot[])snapshots.ToArray(typeof(ISnapshot));
			}
		}
		
		public void Delete(ISnapshot sn){
			Logger.Append(Severity.DEBUG, "Deleting snapshot "+sn.Path+" (id "+sn.Id+")");
			if(!OperatingSystemInfo.IsAtLeast(OSVersionName.WindowsServer2003)) 
				// Deleting is unnecessary on XP since snaps are non-persistent
				// and automatically released on Disposing VSS objects.
				return;


			try{
				IVssImplementation vssi = VssUtils.LoadImplementation();
				using(IVssBackupComponents  oVSS = vssi.CreateVssBackupComponents()){
		            oVSS.InitializeForBackup(null);
					oVSS.SetContext(VssSnapshotContext.All);
					oVSS.DeleteSnapshot(sn.Id, true);
				}

				Logger.Append(Severity.INFO, "Deleted snapshot "+sn.Path+" (id "+sn.Id+")");
			}
			catch(Exception vsse){
				Logger.Append(Severity.WARNING, "Unable to delete snapshot "+sn.Path+" (id "+sn.Id+"): "+vsse.Message);
				//backup.Dispose();
				throw vsse;
			}
			
		}
		
		public bool IsVolumeSnapshottable(string volumeName){
			IVssImplementation vssImplementation = VssUtils.LoadImplementation();
			using (IVssBackupComponents backup = vssImplementation.CreateVssBackupComponents()){
				backup.InitializeForBackup(null);
				using (IVssAsync async = backup.GatherWriterMetadata()){
	                async.Wait();
	                async.Dispose();
	        	}
				return backup.IsVolumeSupported(volumeName);
			}
		}
		
		public SnapshotType Type{
			get{return SnapshotType.VSS;}
		}
		
		public SnapshotCapabilities Capabilities{
			get{return SnapshotCapabilities.Volume;}
		}
		
		public SnapshotSupportedLevel Levels{
			get{return SnapshotSupportedLevel.Full|SnapshotSupportedLevel.Incremental;}
		}

		public void Dispose(){
			if(backup != null)
				backup.Dispose();
		}
	}
}

