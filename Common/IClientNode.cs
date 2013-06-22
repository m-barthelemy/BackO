using System;
using System.Net;

namespace P2PBackup.Common{

	// Represents a client node instances started as a separated appdomain.
	// Avoids direct dependancy on Node library
	public interface IClientNode {
		
		IPEndPoint[] Login(string[] args);
		
		void Stop(bool goingIdle);

		//event EventHandler OnGoingIdle;
	}
}

