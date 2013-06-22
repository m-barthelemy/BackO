using System;
using System.Collections.Generic;

namespace Node.Snapshots{
	
	public interface ISnapshot{

		Guid Id{get;set;}

		// Represents the original path (as present in Backupset configuration), matching root of BasePath.Path
		string Path{get;set;}

		// The 'real' path to access the snapshot
		string MountPoint{get;set;}
		
		string Type{get;set;}
		
		string Version{get;set;}

		long TimeStamp{get; set;}

		byte[] Icon{get;set;}
		
		bool Disabled{get; set;}
		List<ISnapshot> ChildComponents{get;}
		
		void AddChildComponent(ISnapshot cmp);
		
		//void Delete(); 
	}
}

