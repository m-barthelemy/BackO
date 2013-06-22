using System;
using System.Collections.Generic;

namespace Node.Snapshots{
	[Serializable]
	public class Snapshot:ISnapshot{
		
	

		private List<ISnapshot> childComponents;
		
		public List<ISnapshot> ChildComponents {
			get {return this.childComponents;}
		}

		public string MountPoint {get; set;}

		public string Type{get; set;}

		public string Version{get; set;}
			
		public string Path{get; set;}
		
		public Guid Id {get;set;}
		
		public bool Disabled {get; set;}

		public long TimeStamp{get; set;}

		public byte[] Icon{get;set;}


		public Snapshot(){

			childComponents = new List<ISnapshot>();
		}
		
		public void AddChildComponent(ISnapshot cmp){
			childComponents.Add(cmp);	
		}
		
	}
}

