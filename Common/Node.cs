using System;
using System.Collections.Generic;
//using System.Runtime.Serialization.Formatters;
using System.ServiceModel;
using System.Runtime.Serialization;
using ServiceStack.DataAnnotations;
using ServiceStack.DesignPatterns.Model;

namespace P2PBackup.Common{
	
	public enum NodeStatus{Offline, Idle, Online, Backuping, Storing,  Restoring, Duplicating, Removing, Error, Locked, New, Rejected}
	public enum ActionType{Default, Backup, Restore}

	public enum KindEnum{Physical, Virtual}

	[Serializable]
	[KnownType(typeof(NodeStatus))]
	[KnownType(typeof(NodeConfig))]
	// Force WCF to serialize exactly like PeerNode 
	[DataContract(Name = "Node", Namespace = " http://schemas.datacontract.org/")]
	public class Node : IEquatable<P2PBackup.Common.Node>{

		private NodeCertificate certificate;

		private long storageSize; // have to define custom setter here to adjust available when storage changes

		[DataMember]
		[DisplayFormatOption(Size=3)]
		[PrimaryKey]
		public uint Id {get;set;}
			
		[DataMember]
		[DisplayFormatOption(Size=20)]
		public string Name{get;set;}

		[DataMember]
		[DisplayFormatOption(Display=false)]
		public string Description{get;set;}
			
		[DataMember]
		[DisplayFormatOption(Size=15)]
		public string HostName{get;set;}

		[DataMember]
		[DisplayFormatOption(Size=6)]
		public KindEnum Kind{get;set;}

		[Ignore]
		[DisplayFormatOption(Display=false)]
		public NodeCertificate Certificate{
			get{
				return certificate;
			}
			set{
				certificate = value;
			}
		}

		// not exported but serialized to json
		[DataMember]
		[Ignore]
		[DisplayFormatOption(Display=false)]
		public string PublicKey{get;set;}

		[DataMember]
		[DisplayFormatOption(Size=7)]
		public string OS{get;set;}
			
		[DataMember]
		[DisplayFormatOption(Size=15)]
		public string IP{get;set;}

		[DataMember]
		[DisplayFormatOption(Size=15)]
		[Ignore]
		public string ListenIp{get;set;}

		[DataMember]
		[DisplayFormatOption(Size=5,DisplayAs="Port")]
		[Ignore] // don't save to Db since it's just a 'proxy' to configuration.listenport
		public UInt16 ListenPort{
			get{
				if(this.Configuration != null)
					return this.Configuration.ListenPort;
				else return 0;
			}
			set{}
		}
			

		[DataMember]
		[DisplayFormatOption(Size=8,FormatAs=DisplayFormat.Size)]
		public long Quota{get;set;}

		[DataMember]
		[DisplayFormatOption(Size=8,DisplayAs="Used",FormatAs=DisplayFormat.Size)]
		public long UsedQuota{get;set;}

		[DataMember]
		[DisplayFormatOption(Size=8,DisplayAs="Stor.Size",FormatAs=DisplayFormat.Size)]
		public long StorageSize{
			get{return storageSize;}
			set{
				storageSize = value;
				//Available += value;
			}
		}

		[DataMember]
		[DisplayFormatOption(Size=8,FormatAs=DisplayFormat.Size)]
		public long StorageUsed{get;set;}

		[DataMember]
		[DisplayFormatOption(Size=2,DisplayAs="Stor.Prio")]
		public int StoragePriority{get;set;}

		[Ignore] // don't save this transient value
		[DataMember]
		[DisplayFormatOption(Size=3,DisplayAs="Stor.Load")]
		public float CurrentLoad{get;set;}

		[DataMember]
		[DisplayFormatOption(Size=3,DisplayAs="Stor.Group")]
		public int StorageGroup{get;set;}

		[DataMember]
		[DisplayFormatOption(Size=3)]
		public int Group{get;set;}
			
			
		[DataMember]
		[DisplayFormatOption(Size=5)]
		public bool Locked{get;set;}
	
		[Ignore]
		[DataMember]
		[DisplayFormatOption(Size=10)]
		public NodeStatus Status{get;set;}
			
		[DataMember]
		[DisplayFormatOption(Size=6)]
		public string Version{get;set;}

		[Ignore]
		[DataMember]
		[DisplayFormatOption(Display=false)]
		public bool IsUnixClient{get;set;}
		
		[DataMember]
		[DisplayFormatOption(Display=false)]
		public string InternalId {get;set;}
		
		[DataMember]
		[DisplayFormatOption(Size=19,FormatAs=DisplayFormat.Time)]
		public DateTime LastConnection{get;set;}

		[DataMember]
		[DisplayFormatOption(Size=19,FormatAs=DisplayFormat.Time)]
		public DateTime CreationDate{get;set;}

		/*[DataMember]
		[Ignore] // don't save to DB
		[DisplayFormatOption(Display=false)]
		public DateTime LastReceivedAction{get; private set;}*/

		[DataMember]
		[DisplayFormatOption(Display=false)]
		public int Hypervisor {get;set;}

		[DataMember]
		[DisplayFormatOption(Display=false)]
		public NodeConfig Configuration{get;set;}

		[DataMember]
		[Ignore] // don't save into Node table
		[DisplayFormatOption(Display=false)]
		public List<Plugin> Plugins{get;set;}

		/// <summary>
		/// Gets or sets the storage space reserved (and not yet consumed) by backup operations.
		/// </summary>
		/// <value>
		/// The reserved space.
		/// </value>
		[Ignore] // transient value
		internal long ReservedSpace{get;set;}

		[Ignore]
		[DisplayFormatOption(Display=false)]
		public	bool HasAgent{get{return this.Version != null;}}

		public Node (){
			this.Locked = true;
			this.Configuration = new NodeConfig();
			this.Group = 0; //-2; // -2 is a hack to mean 'no group'
			//this.Plugins = new List<Plugin>();
		}


		public bool Equals(P2PBackup.Common.Node other){
		      if (this.Id == other.Id)
		         return true;
		      else
		         return false;
		}


		public override string ToString(){
			return ""+this.Id+" ("+this.Name+")";
		}
	}
}
