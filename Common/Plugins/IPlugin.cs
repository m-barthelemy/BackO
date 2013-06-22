using System;

namespace P2PBackup.Common {

	public interface IPlugin {

		string Name{get;}
		string Version{get;}
		bool IsProxyingPlugin{get;}

	}
}

