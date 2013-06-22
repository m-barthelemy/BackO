using System;
using System.Configuration;
using System.Reflection;
using System.ServiceModel;
using P2PBackup.Common;

public class RemotingManager{
	
	static IRemoteOperations remote;
	static NetTcpBinding binding;
	static ChannelFactory<IRemoteOperations> cf;
	static string serverIP = "127.0.0.1";
	static int serverPort = 9999;
	internal static string User = "admin";
	internal static string Password = "";

	private RemotingManager ()	{

		binding = new NetTcpBinding(SecurityMode.None, true);
		binding.Security.Mode = SecurityMode.None;
		binding.OpenTimeout = new TimeSpan(1,0,0);
		binding.SendTimeout = new TimeSpan(1,0,0);
		binding.CloseTimeout = new TimeSpan(1,0,0);
		binding.ReceiveTimeout = new TimeSpan(1,0,0);
		binding.MaxBufferSize = 10000000;
		binding.MaxReceivedMessageSize = 10000000;
		binding.MaxBufferPoolSize = 10000000;
		binding.MaxConnections = 100;
	}
	
	internal static IRemoteOperations GetRemoteObject(){
		if(binding == null)
			new RemotingManager();
		var address = new EndpointAddress ("net.tcp://"+serverIP+":"+serverPort);
		cf = new ChannelFactory<IRemoteOperations> (binding, address);
		cf.Faulted += OnChannelFault;
		remote = cf.CreateChannel ();
		
		return remote;
	}


	private static void OnChannelFault(object sender, EventArgs e){
		cf.Abort();
	}
}
