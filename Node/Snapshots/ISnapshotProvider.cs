using System;
using System.Collections;
using System.Collections.Generic;
using P2PBackup.Common;
using P2PBackup.Common.Volumes;

namespace Node.Snapshots{
	
	[Flags]
	public enum SnapshotCapabilities{Volume, Directory, File}
	[Flags]
	public enum SnapshotSupportedLevel{Full, Differential, Incremental, TransactionLog}

	internal interface ISnapshotProvider:IDisposable{

		event EventHandler<LogEventArgs> LogEvent;

		List<ISnapshot> ListSnapshottable(string path);
		
		List<ISnapshot> ListSpecialObjects();
		
		SPOMetadata Metadata{get;set;}
		
		string Name{get;}
		//ISnapshotProvider Get();
		
		//bool Create(SnapshotSupportedLevel level);
		
		SnapshotType Type{get;}
		
		SnapshotCapabilities Capabilities{get;}
		
		SnapshotSupportedLevel Levels{get;}
		
		bool IsVolumeSnapshottable(string volumeName);
		
		ISnapshot[] CreateVolumeSnapShot(List<FileSystem> volumes, string[] specialObjects, SnapshotSupportedLevel level);
		
		void Delete(ISnapshot snapshot);
	}
}

