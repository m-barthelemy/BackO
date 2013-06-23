using System;
using System.Collections.Generic;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceHost;
using System.Runtime.Serialization;
using P2PBackup.Common;
using SharpBackupWeb.Utilities;

namespace Backo.Api.WebServices {
	
	
	
	/*[Route("/api/Hypervisors/StartDiscovery/{Id}")]
	public class Discovery{
		public int Id{get;set;}
	}*/
	
	[Authenticate]
	public class PasswordsWS :AppServiceBase{
		
		
		/*public List<P2PBackup.Common.Hypervisor> Get(Hypervisor req){
			return RemotingManager.GetRemoteObject().GetHypervisors();
		}*/
		
		public Password Post(Password p){
			return RemotingManager.GetRemoteObject().CreatePassword(p);
		}
		
		public Password Put(Password p){
			return RemotingManager.GetRemoteObject().UpdatePassword(p);
		}
		
		/*public void Delete(Password p){
			RemotingManager.GetRemoteObject().DeleteHypervisor(hv);
		}*/
		
		/*public List<P2PBackup.Common.Node> Get(Discovery req){
			return RemotingManager.GetRemoteObject().Discover(req.Id);
		}*/
		
		

	}
}

