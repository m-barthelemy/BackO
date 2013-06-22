using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using Node.Utilities;
using P2PBackup.Common;
using P2PBackup.Common.Volumes;

namespace Node.Snapshots{

	public class ZfsProvider:ISnapshotProvider{

		public string Name{get{return "ZFS";}}
		public SPOMetadata Metadata{get;set;}
		public event EventHandler<LogEventArgs> LogEvent;

		public List<ISnapshot> ListSnapshottable(string path){
			return new List<ISnapshot>();	
		}
		
		public List<ISnapshot> ListSpecialObjects(){
			return null;
		}
		
		public ZfsProvider (){
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
				Logger.Append(Severity.DEBUG, "1/2 Snapshotting volume "+volume.MountPoint);
				string snapshotName = "backup_DONOTREMOVE_"+DateTime.Now.ToString("yyyyMMdd-hh:mm");
				ProcessStartInfo pi = new ProcessStartInfo("zfs", "snapshot "+volume.MountPoint+"@"+snapshotName);
				pi.RedirectStandardOutput = true;
				pi.RedirectStandardError = true;
				pi.UseShellExecute = false;
				sn.TimeStamp = Utilities.Utils.GetUtcUnixTime(DateTime.UtcNow);
				Process p = Process.Start(pi);
				p.WaitForExit();
				string stdOut = p.StandardOutput.ReadToEnd();
				string stdErr = p.StandardError.ReadToEnd();
				if(stdOut != String.Empty || stdErr != String.Empty) // something went wrong, on success the command returns nothing
					throw new Exception("Unable to snapshot ZFS volume '"+volume.MountPoint+"'. Error : '"+stdErr+"' (std output was: '"+stdOut+"')");
				else
					Logger.Append(Severity.DEBUG, "2/2 Successfully snapshotted "+volume.MountPoint+" to "+snapshotName);
				sn.Path = volume.MountPoint;
				sn.Id = Guid.Empty;
				sn.MountPoint = volume.MountPoint+"/.zfs/snapshot/"+snapshotName;
				snapshots.Add(sn);
			}
			return (ISnapshot[])snapshots.ToArray(typeof(ISnapshot));
		}
		
		public bool IsVolumeSnapshottable(string volumeName){
			return true;
		}
		
		public void Delete(ISnapshot sn){
		}
		
		public SnapshotType Type{
			get{return SnapshotType.ZFS;}
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

