using System;
using System.Net;

namespace P2PBackupHub.Clustering {
	
	public enum ClusterRole{CurrentActive, CurrentPassive, OtherActive, OtherPassive}
	public class ClusterNode {
		
		public short ID{get; set;}
		public ClusterRole Role{get;set;}
		public IPAddress Address{get;set;}
		public int Port{get;set;}
		public short Weight{get; set;}
		public int CurrentLoad{get;set;}
		public ClusterNode(){
		}
	}
}

