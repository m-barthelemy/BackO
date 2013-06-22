using System;
namespace P2PBackup.Common{
	
	[Serializable]
	public class NodeGroup{

		[DisplayFormatOption(Size=3)]
		public int Id{get;set;}
			
		[DisplayFormatOption(Size=25)]
		public string Name{get;set;}
			
		[DisplayFormatOption(Size=50)]
		public string Description{get;set;}
			
		public NodeGroup (){
			 //
		}
	}
}

