using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using Node.Utilities;
using P2PBackup.Common;
using P2PBackup.Common.Volumes;

namespace Node.Snapshots{

	public class BtrfsProvider:ISnapshotProvider{


		public string Name{get{return "BTRFS";}}
		public SPOMetadata Metadata{get;set;}
		public event EventHandler<LogEventArgs> LogEvent;

		public List<ISnapshot> ListSnapshottable(string path){
			return new List<ISnapshot>();	
		}
		
		public List<ISnapshot> ListSpecialObjects(){
			return null;
		}
		
		public BtrfsProvider (){
		}
		
		public Snapshot Create(SnapshotSupportedLevel level){
			
			
			return new Snapshot();
		}
		
		public Snapshot CreateWriterSnapshot(List<string> writerPaths){
			return new Snapshot();
		}
		
		public ISnapshot[] CreateVolumeSnapShot(List<FileSystem> volumes, string[] specialObjects, SnapshotSupportedLevel level){
			ArrayList snapshots = new ArrayList();
			foreach(FileSystem volume in volumes){
				Snapshot sn = new Snapshot();
				sn.Type = this.Name;
				//volumeName = volumeName.Substring(1);
				Logger.Append(Severity.DEBUG, "1/2 Snapshotting Btrfs volume "+volume.MountPoint);
				string snapshotName = "backup_"+DateTime.Now.ToString("yyyyMMdd-hh:mm");
				ProcessStartInfo pi = new ProcessStartInfo("btrfs", "subvolume snapshot "+volume.MountPoint+"  "+volume.MountPoint+"/"+volume.MountPoint.Substring(0, volume.MountPoint.LastIndexOf('/'))+"@"+snapshotName);
				pi.RedirectStandardOutput = true;
				pi.RedirectStandardError = true;
				pi.UseShellExecute = false;
				sn.TimeStamp = Utilities.Utils.GetUtcUnixTime(DateTime.UtcNow);
				using(Process p = Process.Start(pi)){
					p.WaitForExit();
					string stdOut = p.StandardOutput.ReadToEnd();
					string stdErr = p.StandardError.ReadToEnd();
					if(stdErr != String.Empty) // something went wrong, on success the command returns nothing
						throw new Exception("Unable to snapshot Btrfs volume '"+volume.MountPoint+"'. Error : '"+stdErr+"' (std output was: '"+stdOut+"')");
					else
						Logger.Append(Severity.DEBUG, "2/2 Successfully snapshotted "+volume.MountPoint+" to "+snapshotName);
				}
				sn.Path = volume.MountPoint;
				sn.Id = Guid.Empty;
				//sn.Path = volume.MountPoint+"/"+volume.MountPoint.Substring(0, volume.MountPoint.LastIndexOf('/'))+"@"+snapshotName;
				//sn.MountPoint = System.IO.Path.Combine(volume.MountPoint, "@"+snapshotName);
				sn.MountPoint = volume.MountPoint+"/"+"@"+snapshotName;
				snapshots.Add(sn);
			}
			return (ISnapshot[])snapshots.ToArray(typeof(ISnapshot));
		}
		
		public bool IsVolumeSnapshottable(string volumeName){
			return true;
		}
		
		public void Delete(ISnapshot sn){
			Logger.Append(Severity.DEBUG, "Delete() Deleting snapshot, command is: 'btrfs subvolume delete "+sn.MountPoint+"'");
			ProcessStartInfo pi = new ProcessStartInfo("btrfs", "subvolume delete "+sn.MountPoint);
			pi.RedirectStandardOutput = true;
			pi.RedirectStandardError = true;
			pi.UseShellExecute = false;
			using(Process p = Process.Start(pi)){
				p.WaitForExit();
				//string stdOut = p.StandardOutput.ReadToEnd();
				string stdErr = p.StandardError.ReadToEnd();
				if(stdErr != String.Empty) // something went wrong, on success the command returns nothing
					throw new Exception("Unable to delete Btrfs snapshot "+sn.MountPoint+". Error : "+stdErr);
				else
					Logger.Append(Severity.INFO, "Deleted snapshot "+sn.MountPoint);
			}
		}
		
		public SnapshotType Type{
			get{return SnapshotType.BTRFS;}
		}
		
		public SnapshotCapabilities Capabilities{
			get{return SnapshotCapabilities.Volume;}
		}
		
		public SnapshotSupportedLevel Levels{
			get{return SnapshotSupportedLevel.Full|SnapshotSupportedLevel.Incremental;}
		}

		public void Dispose(){

		}
	}
}

