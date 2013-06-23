using System;
using System.Collections;
using System.Security.Principal;

// to remove once ported from remoting to WCF
//using System.Runtime.Remoting;
//using System.Runtime.Remoting.Channels;
//using System.Runtime.Remoting.Channels.Tcp;

using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Description;

using P2PBackupHub.Utilities;
using P2PBackup.Common;

namespace P2PBackupHub.Remoting{

	public class RemotingServer{
		
		private static RemotingServer _instance = new RemotingServer();
		private ServiceHost host;

		private RemotingServer (){
		}

		public static RemotingServer Instance{get{return _instance;}}

		public void Start(){
			/*AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.UnauthenticatedPrincipal);
			//TcpChannel channel = new TcpChannel(9999);
			IDictionary dict = new Hashtable();
			dict["port"] = 9999;
			dict["impersonate"] = false;
			//dict["secure"] = true;
			//dict["tokenImpersonationLevel"] = TokenImpersonationLevel.Anonymous;
			//TcpServerChannel channel = new TcpServerChannel(dict, null, new AuthorizationModule());
			TcpServerChannel channel = new TcpServerChannel(dict, null);
			//AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.UnauthenticatedPrincipal);
			ChannelServices.RegisterChannel(channel, false);
			RemotingConfiguration.RegisterWellKnownServiceType(typeof(RemoteOperations), "IRemoteOperations", WellKnownObjectMode.Singleton);
			System.Runtime.Remoting.Lifetime.LifetimeServices.LeaseTime = new TimeSpan(0,5,0);			
			//RemotingConfiguration.RegisterWellKnownServiceType(typeof(INode), "Node", WellKnownObjectMode.SingleCall);
			//System.Runtime.Remoting.Channels.Tcp.TcpServerChannel grut = new System.Runtime.Remoting.Channels.Tcp.TcpServerChannel(

			*/

			var binding = new NetTcpBinding(SecurityMode.None, true);
			/*var binding = new NetTcpBinding();
			binding.Security = new NetTcpSecurity();
			binding.Security.Mode = SecurityMode.Transport;
			binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Certificate;*/



			//binding.ReliableSession = new ReliableSession();
			//binding.ReliableSession.Enabled = true;

			// doesn't work under mono 3.x !!!,???
			//binding.ReliableSession.InactivityTimeout = TimeSpan.FromHours(1);

			binding.Security.Mode = SecurityMode.None; // else it won't work under Mono (2.10.x)
			binding.OpenTimeout = new TimeSpan(1,0,0);
			binding.SendTimeout = new TimeSpan(1,0,0);
			binding.ReceiveTimeout = new TimeSpan(1,0,0);
			binding.CloseTimeout = new TimeSpan(1,0,0);
			binding.ListenBacklog = 50;
			binding.MaxReceivedMessageSize = 100000000;
			binding.MaxBufferPoolSize = 10000000;

			binding.MaxBufferSize = 100000000;

			//binding.TransferMode = TransferMode.Streamed;
			/*binding.ReaderQuotas.MaxDepth = 64;
	                binding.ReaderQuotas.MaxStringContentLength = 2147483647;
	                binding.ReaderQuotas.MaxArrayLength = 2147483647;
	                binding.ReaderQuotas.MaxBytesPerRead = 16384;
	                binding.ReaderQuotas.MaxNameTableCharCount = 2147483647;*/
		    	var address = new Uri ("net.tcp://localhost:9999");
			//var address = new Uri ("http://localhost:9999");
		    	host = new ServiceHost (typeof(RemoteOperations));
			host.OpenTimeout = new TimeSpan(1,0,0);
			host.CloseTimeout = new TimeSpan(1,0,0);
		    	host.AddServiceEndpoint (typeof(IRemoteOperations), binding, address);
		
		    	/*ServiceThrottlingBehavior behavior = new ServiceThrottlingBehavior (){
		        	MaxConcurrentCalls = 30,
		        	MaxConcurrentSessions = 10,
		        	MaxConcurrentInstances = 10,
		    	};
			*/
			foreach(IServiceBehavior bh in host.Description.Behaviors){
				if(bh is ServiceDebugBehavior)
					((ServiceDebugBehavior)bh).IncludeExceptionDetailInFaults = true;
			}
			/*ServiceDebugBehavior debugBehavior = new ServiceDebugBehavior();
			debugBehavior.IncludeExceptionDetailInFaults = true;
		   	//host.Description.Behaviors.Add(behavior);
			host.Description.Behaviors.Add(debugBehavior);*/
			host.Open();
			host.Faulted += HandleError;
			host.UnknownMessageReceived += HandleUnknownMsg;

		}

		internal void Stop(){
			if(host != null)
				try{
				host.Close();
				}
				catch{}
		}
		private void HandleError(Object sender, EventArgs e){
			Logger.Append("RAPI", Severity.ERROR, "Remoting API service error : "+e.ToString());
		}

		private void HandleUnknownMsg(Object sender, EventArgs e){
			Logger.Append("RAPI", Severity.ERROR, "Remoting API service received unknown message : "+e.ToString());
		}
	}
	
	/*class AuthorizationModule : IAuthorizeRemotingConnection{
            public bool IsConnectingEndPointAuthorized(System.Net.EndPoint endPoint){
            	Logger.Append("HUBRN", Severity.INFO, "Remoting connection initiated, from " + endPoint);
            	return true;
            }

            public bool IsConnectingIdentityAuthorized(IIdentity identity){
			
            	Console.WriteLine(" ########### Connecting identity: " + identity.Name);
                return true;
            }
        }*/
}

