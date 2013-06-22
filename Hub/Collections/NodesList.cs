using System;
using System.Collections.Generic;
using P2PBackup.Common;

namespace P2PBackupHub {
	
	internal class NodesList : SynchronizedKeyedCollection<int, PeerNode> {
		
		internal NodesList() : base(){
		}
		
		protected override int GetKeyForItem(PeerNode item) {
			return item.Id;
		}

		internal PeerNode GetById(int id){
			if(this.Contains(id))
				return this[id];
			else
				return null;
		}


	}
}

