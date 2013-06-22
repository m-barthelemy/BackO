using System;
using System.Runtime.Serialization;
using ServiceStack.DataAnnotations;

namespace P2PBackup.Common {

	public enum PluginCategory{IStorageDiscoverer, ISpecialObject}

	[DataContract]
	[KnownType(typeof(PluginCategory))]
	public class Plugin :IPlugin{

		[DataMember]
		[AutoIncrement]
		[Index(false)]
		public int Id{get;set;}

		[DataMember]
		[Index(false)]
		[References(typeof(Node))]
		public int NodeId{get;set;}

		[DataMember]
		public string Name{get;set;}

		[DataMember]
		public string Version{get;set;}

		[DataMember]
		public bool IsProxyingPlugin{get;set;}

		[DataMember]
		public PluginCategory Category{get;set;}

		[DataMember]
		public bool Enabled{get;set;}

		[Ignore]
		public Type RawType{get;set;}

		public Plugin(){
		}
	}
}

