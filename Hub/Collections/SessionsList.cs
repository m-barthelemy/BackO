using System;
using System.Collections.Generic;
using P2PBackup.Common;

namespace P2PBackupHub {
	
	internal class SessionsList : SynchronizedKeyedCollection<long, PeerSession> {
		
		internal SessionsList() : base(){
		}
		
		protected override long GetKeyForItem(PeerSession item) {
			return item.Id;
		}

		internal PeerSession GetById(long id){
			if(this.Contains(id))
				return this[id];
			else
				return null;
			 
		}
	}
}

