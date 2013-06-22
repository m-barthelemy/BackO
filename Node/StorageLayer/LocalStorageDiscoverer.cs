using System;
using System.Collections.Generic;
using P2PBackup.Common;
using P2PBackup.Common.Volumes;

namespace Node.StorageLayer {



	public class LocalStorageDiscoverer:IStorageDiscoverer{

		public delegate void LogHandler(int code, Severity severity, string message);
		public event EventHandler<LogEventArgs> LogEvent;

		public string Name{get{ return "local";}}
		public string Version{get{return "0.1";}}
		public bool IsProxyingPlugin{get{return false;}}

		public bool Initialize(ProxyTaskInfo ptI){

			return true;
		}

		/*public string[] GetPhysicalDisksPaths(){

			return default (string[]);
		}*/

		public StorageLayout BuildStorageLayout(){
			if(Utilities.PlatForm.IsUnixClient())
				return (new LinuxStorageDiscoverer()).BuildStorageLayout();
			else if(Utilities.PlatForm.Instance().OS.StartsWith("NT"))
				return (new NTStorageDiscoverer()).BuildStorageLayout();

			return null;
		}


		public void Dispose(){

		}


	}
}

