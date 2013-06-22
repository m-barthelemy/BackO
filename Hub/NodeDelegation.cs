using System;
using System.Collections.Generic;

namespace P2PBackupHub {
	public enum NodePermission{P2PBackup,Restore,CreateTaskset,ModifyTaskset,DeleteTaskset};
	// permissions that a client can own
	// -start backup (instead of waiting for hub to tell when to backup)
	// -create backupsets
	// -allow live backup(max. every x minutes)
	// -quota
	public class NodeDelegation {

		public string UserName{get;set;}
		public List<NodePermission> Permissions{get;set;}
		public NodeDelegation() {
			Permissions = new List<NodePermission>();
		}
	}
}

