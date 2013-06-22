using System;
using System.Collections.Generic;
using P2PBackup.Common.Volumes;

namespace P2PBackup.Common {


	public interface IStorageDiscoverer: IDisposable, IPlugin{

		event EventHandler<LogEventArgs> LogEvent;
		
		bool Initialize(ProxyTaskInfo proxyingInfo);

		StorageLayout BuildStorageLayout();

	}


}


