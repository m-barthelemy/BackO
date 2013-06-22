using System;
using System.Collections.Generic;
using P2PBackup.Common;
using P2PBackup.Common.Volumes;
using System.Runtime.Serialization.Formatters.Binary;

namespace Node.Snapshots{

	/// <summary>
	/// This specialobject handle the client node's Volume layout.
	/// Saving this information will allow Bare Metal recovery (we will know how to recreate disks & fs layouts)
	/// </summary>
	public class StorageLayoutSPO: ISpecialObject{

		public string Name{get{return "Storage Layout";}}
		public string Version{get{return "0.1";}}
		public bool IsProxyingPlugin{get{return false;}}

		public List<BasePath> BasePaths{get;private set;}
		public List<string> ExplodedComponents{get; private set;}
		public RestoreOrder RestorePosition{get; private set;}
		public event EventHandler<LogEventArgs> LogEvent;

		private StorageLayout layout;

		public StorageLayoutSPO(){}

		public StorageLayoutSPO(P2PBackup.Common.Volumes.StorageLayout layout){
			this.RestorePosition = RestoreOrder.BeforeStorage;
			this.layout = layout;
			this.BasePaths = new List<BasePath>(); // unused , but initialize it anyway
			this.ExplodedComponents = new List<string>();
		}

		public void SetItems(List<string> spoPaths){

				System.IO.MemoryStream ms = new System.IO.MemoryStream();
				BinaryFormatter bf = new BinaryFormatter();
				bf.Serialize(ms, layout);
				ms.Flush ();
				Console.WriteLine ("VolumeLayoutSPO serialized storagelayout called ");
				//serialize vmci to Metadata!!!
				this.Metadata = new SPOMetadata();
				this.Metadata.Metadata.Add("", ms.ToArray());
		}

		public SPOMetadata Metadata{get;set;}
		
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

