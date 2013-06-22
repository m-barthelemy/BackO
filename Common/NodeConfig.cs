using System;
using System.Collections.Generic;

namespace P2PBackup.Common {

	/*public class StorageSpace{

	}*/

	public class NodeConfig {

		public Severity LogLevel{get;set;}
		public bool LogToSyslog{get;set;}
		public string LogFile{get;set;}
		public string StoragePath{get;set;}
		public long StorageSize{get;set;}
		public string IndexPath{get;set;}
		public string ListenIP{get;set;}
		public UInt16 ListenPort{get;set;}


		public NodeConfig (){
			this.LogLevel = Severity.INFO;
			this.ListenPort = 52562;
			this.ListenIP = "*";
		}
	}
}

