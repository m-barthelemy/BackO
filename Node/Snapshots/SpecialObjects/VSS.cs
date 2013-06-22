using System;
using System.Collections.Generic;
using P2PBackup.Common;
using Node.Utilities;
using Alphaleonis.Win32.Vss;

namespace Node.Snapshots{

	public class VSS: ISpecialObject{
		
		public string Name{get{return "VSS";}}
		public string Version{get{return "0.1";}}
		public bool IsProxyingPlugin{get{return false;}}

		private IVssBackupComponents backup;
		public List<BasePath> BasePaths{get; private set;}
		public List<string> ExplodedComponents{get; private set;}
		public SPOMetadata Metadata{get;set;}
		public RestoreOrder RestorePosition{get; private set;}
		public event EventHandler<LogEventArgs> LogEvent;

		public VSS():this(BackupLevel.Full){}

		public VSS(BackupLevel level){
			this.RestorePosition = RestoreOrder.AfterStorage;
			this.ExplodedComponents = new List<string>();
			Metadata = new SPOMetadata();
			BasePaths = new List<BasePath>();
			IVssImplementation vss = VssUtils.LoadImplementation();
			backup = vss.CreateVssBackupComponents();
			//Logger.Append(Severity.DEBUG, "0/6 Initializing Snapshot ("+ ((BasePaths == null)? "NON-component mode" : "component mode")+")");
         	backup.InitializeForBackup(null);
			VssBackupType vssLevel = VssBackupType.Full;
			if(level == BackupLevel.Full)
					vssLevel = VssBackupType.Full;
			else if (level == BackupLevel.Refresh)
					vssLevel = VssBackupType.Incremental;
		/*	else if (level == BackupLevel.Differential)
					vssLevel = VssBackupType.Differential;*/
			else if (level == BackupLevel.TransactionLog)
					vssLevel = VssBackupType.Log;
			//if(spoPaths == null) // component-less snapshot set
			//		backup.SetBackupState(false, true, VssBackupType.Full, false);
			//else
					backup.SetBackupState(true, true, vssLevel, false);
			if (OperatingSystemInfo.IsAtLeast(OSVersionName.WindowsServer2003)){
					// The only context supported on Windows XP is VssSnapshotContext.Backup 
					backup.SetContext(VssSnapshotContext.AppRollback);
			}
			//Logger.Append(Severity.DEBUG, "1/6 Gathering writers metadata and status");
			using (IVssAsync async = backup.GatherWriterMetadata()){
                async.Wait();
                async.Dispose();
        	}
			// gather writers status before adding backup set components
			using (IVssAsync async = backup.GatherWriterStatus()){
				async.Wait();
				async.Dispose();
			}
		}
		
		public void SetItems(List<string> spoPaths){
			foreach (IVssExamineWriterMetadata writer in backup.WriterMetadata){
							
				foreach(string spo in spoPaths){
					try{
					Logger.Append (Severity.TRIVIA, "Searching writer and/or component matching "+spo+", current writer="+writer.InstanceName);
					int index = spo.IndexOf(writer.WriterName);
					if(index <0 && spo != "*")
						continue;
					bool found = false;
					
					// First we check that the writer's status is OK, else we don't add it to avoid failure of complete snapshot if it's not
					bool writerOk = false;
					foreach (VssWriterStatusInfo status in backup.WriterStatus){
						if(status.Name == writer.WriterName){
							if(status.State == VssWriterState.Stable){
								// if we get there it means that we are ready to add the wanted component to VSS set 				
								writerOk = true;
								Metadata.Metadata.Add(writer.WriterName, writer.SaveAsXml());
							}
							else{
								Logger.Append(Severity.ERROR, "***Cannot add writer "+status.Name+" to snapshot set,"
									+" status="+status.State.ToString()+". Backup data  managed by this writer may not be consistent. Restore will be hazardous." );
								if(OperatingSystemInfo.IsAtLeast(OSVersionName.WindowsServer2003))
									backup.DisableWriterClasses(new Guid[]{writer.WriterId});
							}
						}	
					}
					
					bool addAllComponents = false;
					if(spo.Length == index+writer.WriterName.Length || spo == "*" || spo == ""+writer.WriterName+@"\*")
							addAllComponents = true;
					// exclude items indicated by writer
					foreach (VssWMFileDescription file in writer.ExcludeFiles){
						BasePath bp = new BasePath();
						bp.Type = "FS";
						bp.Path = file.Path;
						bp.ExcludePolicy.Add(file.FileSpecification);
						//bp.ExcludedPaths = writer.E
						bp.Recursive = file.IsRecursive;
						BasePaths.Add(bp);
					}
					foreach (IVssWMComponent component in writer.Components){
						found = false;
						Console.WriteLine("***createvolsnapshot : current component is :"+component.LogicalPath+@"\"+component.ComponentName);
						if((!addAllComponents) && spo.IndexOf(component.LogicalPath+@"\"+component.ComponentName) < 0)
							continue;
						Logger.Append (Severity.TRIVIA, "***Asked to recursively select all '"+writer.WriterName+"' writer's components");
						if(OperatingSystemInfo.IsAtLeast(OSVersionName.WindowsServer2003)){
							foreach(VssWMDependency dep in  component.Dependencies){
								Logger.Append (Severity.TRIVIA, "***Component "+component.ComponentName+" depends on component "+dep.ComponentName+" TODO TODO TODO add it automatically");
							}
						}
						if(component.Selectable)
							backup.AddComponent(writer.InstanceId, writer.WriterId, component.Type, component.LogicalPath, component.ComponentName);
						//Logger.Append (Severity.INFO, "***Added writer '"+writer.WriterName+"' component "+component.ComponentName);
							this.ExplodedComponents.Add(writer.WriterName+@"\"+component.LogicalPath+@"\"+component.ComponentName);
						found = true;
						
						// Second we need to find every drive containing files necessary for writer's backup
						// and add them to drives list, in case they weren't explicitely selected as part of backuppaths
						List<VssWMFileDescription> componentFiles = new List<VssWMFileDescription>();
						componentFiles.AddRange(component.Files);
						componentFiles.AddRange(component.DatabaseFiles);
						componentFiles.AddRange(component.DatabaseLogFiles);
						foreach (VssWMFileDescription file in componentFiles){
							if (string.IsNullOrEmpty(file.Path)) continue;
							//Console.WriteLine ("***component file path="+file.Path+", alt backuplocation="+file.AlternateLocation
							//	+", backuptypemask="+file.BackupTypeMask.ToString()+", spec="+file.FileSpecification+", recursive="+file.IsRecursive);
							BasePath bp = new BasePath();
							bp.Path = file.Path;
							bp.Type = "FS"; //BasePath.PathType.FS;
							bp.IncludePolicy.Add(file.FileSpecification);
							bp.Recursive = file.IsRecursive;
							BasePaths.Add(bp);
                           
                        }
						
						//backup.SetBackupSucceeded(writer.InstanceId, writer.WriterId, component.Type, component.LogicalPath, component.ComponentName, false);
						//Logger.Append(Severity.TRIVIA, "Added writer/component "+spo);
                   		//break;
                    }	
					//metadata.Metadata.Add(writer.SaveAsXml());
					// Retrieve Backup Components Document
					//Metadata.Metadata.Add("bcd", backup.SaveAsXml());
					
					if(found == false)
						Logger.Append(Severity.WARNING, "Could not find VSS component "+spo+" which was part of backup paths");
					}
					catch(Exception e){
						Console.WriteLine (" *** SetItems() : error "+e.Message);
					}
					
				}
				
			}
			//backup.BackupComplete();
			backup.FreeWriterStatus();
			backup.FreeWriterMetadata();
			backup.AbortBackup();
			backup.Dispose();
		}
		

		// the way VSS works make it impossible to freeze objects independantly from snapshot
		// (and still have a consistent snapshot)
		public void Freeze(){
			
		}
		
		public void Resume(){
			
		}

		public void PrepareRestore(List<string> spoPaths){

		}

		public void Restore(){

		}
	}
}

