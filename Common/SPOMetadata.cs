using System;
using System.Collections.Generic;

namespace P2PBackup.Common{

	[Serializable]
	public class SPOMetadata{
		//public Hashtable Metadata{get;set;}
		public Dictionary<string, object> Metadata{get;set;}
		
		public SPOMetadata(){
			//Metadata = new Hashtable(); //<object>();
			Metadata = new Dictionary<string, object>();
		}
	}
}

