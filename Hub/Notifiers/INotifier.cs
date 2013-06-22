using System;
using P2PBackup.Common;
using P2PBackupHub.Notifiers;

namespace P2PBackupHub {

	public interface INotifier {
		
		void Fire(Task t);
	}
}

