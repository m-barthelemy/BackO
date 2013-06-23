using System;
using System.Collections.Generic;
using P2PBackup.Common;

namespace P2PBackupHub {
	
	internal class NodesList : SynchronizedKeyedCollection<uint, PeerNode> {
		
		internal NodesList() : base(){
		}
		
		protected override uint GetKeyForItem(PeerNode item) {
			return item.Id;
		}

		internal PeerNode GetById(uint id){
			if(this.Contains(id))
				return this[id];
			else
				return null;
		}

		internal new  bool Remove(PeerNode n){
			lock(base.SyncRoot){
				return base.Remove(n);
			}
		}
		/*internal bool RemoveById(int nodeId){
			this.R
		}*/
	}
}

