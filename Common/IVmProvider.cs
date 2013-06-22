using System;
using System.Collections.Generic;
using P2PBackup.Common;
using P2PBackup.Common.Volumes;

namespace P2PBackup.Common.Virtualization {
	public interface IVmProvider : IDisposable{

		event EventHandler<LogEventArgs> LogEvent;

		string Name{get;}

		bool Connect(string url, string userName, string password);

		List<Node> GetVMs();

		List<Disk> GetDisks(Node vm);



	}


}

