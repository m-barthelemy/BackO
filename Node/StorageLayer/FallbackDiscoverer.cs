using System;
using System.Collections.Generic;
using P2PBackup.Common;
using P2PBackup.Common.Volumes;


namespace Node.StorageLayer {

	public class FallbackDiscoverer:IStorageDiscoverer {

		public delegate void LogHandler(int code, Severity severity, string message);
		public event EventHandler<LogEventArgs> LogEvent;
		
		public string Name{get{ return "fallback";}}
		public string Version{get{return "0.1";}}
		public bool IsProxyingPlugin{get{return false;}}

		public bool Initialize(ProxyTaskInfo ptI){
			return true;
		}
		
		
		/// <summary>
		/// Builds a "fake" layout containing 1 fake disk, having 1 fake partition, 
		/// containing all the mounted available filesystems
		/// </summary>
		/// <returns>
		/// The storage layout.
		/// </returns>
		public StorageLayout BuildStorageLayout(){
			
			var layout = new StorageLayout();
			var disk = new Disk{Path="?"};
			var partition = new Partition{Path="?"};
			foreach(FileSystem fs in FilesystemManager.Instance().GetAllDrives())
				partition.AddChild(fs);
			disk.AddChild(partition);
			layout.Entries.Add(disk);
			return layout;
		}
		
		
		public void Dispose(){
			
		}


		public FallbackDiscoverer ()
		{
		}
	}
}

