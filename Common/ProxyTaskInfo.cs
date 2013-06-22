using System;
//using System.Xml.Serialization;
namespace P2PBackup.Common {

	[Serializable]
	public class ProxyTaskInfo {

		public Hypervisor Hypervisor{get;set;}
		public Node Node{get;set;}


	}
}

