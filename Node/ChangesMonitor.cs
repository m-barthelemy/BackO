using System;
using System.IO;
using System.Collections.Generic;
using Node.Utilities;

namespace Node{
	
	/// <summary>
	/// Permanently monitors for files changes in paths to be backuped.
	/// This allows very fast differential and incremental backups
	/// and continuous, on-change backups.
	/// If the service is interrupted, list is invalid and a traditionnal diff/incr 
	/// (file-to-file comparison) will have to be done.
	/// </summary> 
	// 
	internal class ChangesMonitor{
		private FileSystemWatcher watcher;
		
		internal ChangesMonitor(){
			watcher = new FileSystemWatcher();
			watcher.InternalBufferSize = 16384; //16k
			watcher.IncludeSubdirectories = true;
		
		}
	}
}
