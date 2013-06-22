using System;
using System.Collections.Generic;
using P2PBackup.Common;

namespace P2PBackupHub {

	internal class TasksList : SynchronizedKeyedCollection<long, Task> {

		internal TasksList() : base(){
		}

		protected override long GetKeyForItem(Task item) {
			return item.Id;
		}

		internal Task GetByIndex(int index){
			return base.Items[index];
		}
	}
}

