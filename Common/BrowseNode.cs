using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using ServiceStack.DataAnnotations;
using ServiceStack.DesignPatterns.Model;

namespace P2PBackup.Common {

	[Serializable]
	[DataContract]
	public class BrowseNode{
		
		[DataMember]
		public List<BrowseItem> Children{get;set;}
		
		public BrowseNode(){
			this.Children = new List<BrowseItem>();
		}
		
		/*public override string ToString () {
			return string.Format ("[BrowseNode: Name={0}, Type={1}, Label={2}, FS={3}, Snap={4}, Size={5}, Avail={6}, Children={7}]", Name, Type, Label, FS, Snap, Size, Avail, Children);
		}*/
		
	}

	[Serializable]
	[DataContract]
	public class BrowseItem{

		[DataMember]
		public long Id{get;set;}
		[DataMember]
		public string Name{get;set;}
		[DataMember]
		public /*System.IO.DriveType*/ string Type{get;set;}
		[DataMember]
		public string Label{get;set;}
		[DataMember]
		public string FS{get;set;}
		[DataMember]
		public string Snap{get;set;}
		[DataMember]
		public long Size{get;set;}
		[DataMember]
		public long Avail{get;set;}
		//[DataMember]
		//public List<BrowseNode> Children{get;set;}

		public BrowseItem(){
			//this.Children = new List<BrowseNode>();
		}

		public override string ToString () {
			return string.Format ("[BrowseNode: Name={0}, Type={1}, Label={2}, FS={3}, Snap={4}, Size={5}, Avail={6}]", Name, Type, Label, FS, Snap, Size, Avail);
		}

	}
}

