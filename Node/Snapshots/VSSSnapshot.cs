using System;
using System.Collections.Generic;


namespace Node.Snapshots{
	[Serializable]
	public class VSSSnapshot:ISnapshot{
		
		private string name;
		private Guid id;
		private string type;
		private string path;
		private string version;
		private bool disabled;
		private List<ISnapshot> childComponents;
		
		public List<ISnapshot> ChildComponents {
			get {return this.childComponents;}
		}

		public string MountPoint {
			get {return this.path;}
			set {path = value;}
		}

		public string Type {
			get {return "VSS";}
			set {type = value;}
		}

		public string Version {
			get {return this.version;}
			set {version = value;}
		}
		
		public bool Disabled {
			get {return this.disabled;}
			set {disabled = value;}
		}

		public string Path{
			get{return name;}
			set{name = value;}
		}
		
		public Guid Id {
			get {return this.id;}
			set {id = value;}
		}

		public long TimeStamp{get; set;}

		public byte[] Icon{get;set;}
		
		public VSSSnapshot(){
			name = "";
			//type = "VSS";
			path = "";
			version = "";
			childComponents = new List<ISnapshot>();
		}
		
		public void AddChildComponent(ISnapshot cmp){
			childComponents.Add(cmp);	
		}
		
		//public string[] GetRequiredDrivesForSnapshot(){
		//	
		//}
		
	}
}

