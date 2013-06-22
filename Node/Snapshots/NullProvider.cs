using System;
using System.Collections;
using System.Collections.Generic;
using Node.Utilities;
using P2PBackup.Common;
using P2PBackup.Common.Volumes;

namespace Node.Snapshots{

	public class NullProvider:ISnapshotProvider{

		public string Name{get{return "Null";}}
		public SPOMetadata Metadata{get;set;}
		public event EventHandler<LogEventArgs> LogEvent;

		public List<ISnapshot> ListSnapshottable(string path){
			return new List<ISnapshot>();	
		}
		
		public List<ISnapshot> ListSpecialObjects(){
			return null;
		}
		
		public NullProvider (){
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
				Logger.Append(Severity.DEBUG, "1/1 create fake snapshot for volume "+volume.MountPoint);
				Snapshot sn = new Snapshot();
				sn.Path = volume.MountPoint; //volume.Path;
				sn.MountPoint = volume.MountPoint;
				sn.Id = Guid.Empty;
				sn.TimeStamp = Utilities.Utils.GetUtcUnixTime(DateTime.UtcNow);
				snapshots.Add(sn);
			}
			Logger.Append(Severity.INFO, "Generated fake snapshots for "+volumes.Count+" volumes.");
			return (ISnapshot[])snapshots.ToArray(typeof(ISnapshot));
		}
		
		public void Delete(ISnapshot sn){
			
		}
		
		public bool IsVolumeSnapshottable(string volumeName){
			return true;
		}
		
		public SnapshotType Type{
			get{return SnapshotType.NONE;}
		}
		
		public SnapshotCapabilities Capabilities{
			get{return SnapshotCapabilities.Volume;}
		}
		
		public SnapshotSupportedLevel Levels{
			get{return SnapshotSupportedLevel.Full;}
		}

		public void Dispose(){

		}
	}
}

