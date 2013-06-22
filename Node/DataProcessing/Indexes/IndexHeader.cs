using System;
using System.Collections;
using System.Collections.Generic;
using P2PBackup.Common;
using Node.Snapshots;

namespace Node.DataProcessing{
	[Serializable]
	internal class IndexHeader{
		internal IndexHeader (){
			this.BsId = 0;
			this.BackupType = BackupLevel.Full;
			this.MaxChunkFiles = 0;
			this.MaxPackSize = 0;
			this.MaxChunkSize = 0;
			this.StartDate = DateTime.Now;
			this.Version = String.Empty;
			//this.ProviderMetadata = new List<Tuple<string, Hashtable>>();
			
		}
		
		internal string Version{get; set;}
		internal long TaskId{get;set;}
		internal int BsId{get;set;}
		internal BackupLevel BackupType{get;set;}
		internal long MaxChunkSize{get;set;}
		internal long MaxPackSize{get;set;}
		internal long MaxChunkFiles{get;set;}
		internal DateTime StartDate{get;set;}
		//internal BackupRootDrive[] RootDrives{get;set;}
		//internal List<SPOMetadata> SPOMetadata{get;set;}
		// metadata for providers such as USN journal data
		//internal List<Tuple<string, Hashtable>> ProviderMetadata{get;set;}// drivename/path, metadata
		
	}
}

