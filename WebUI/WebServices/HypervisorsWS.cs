using System;
using System.Collections.Generic;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceHost;
using System.Runtime.Serialization;
using P2PBackup.Common;
using SharpBackupWeb.Utilities;

namespace Backo.Api.WebServices {



	[Route("/api/Hypervisors/StartDiscovery/{Id}")]
	public class Discovery{
		public int Id{get;set;}
	}
	
	[Authenticate]
	public class HypervisorsWS :AppServiceBase{
		

		public List<P2PBackup.Common.Hypervisor> Get(Hypervisor req){
			return RemotingManager.GetRemoteObject().GetHypervisors();
		}
		
		public P2PBackup.Common.Hypervisor Post(Hypervisor hv){
			return RemotingManager.GetRemoteObject().CreateHypervisor(hv);
		}
		
		public P2PBackup.Common.Hypervisor Put(Hypervisor hv){
			return RemotingManager.GetRemoteObject().UpdateHypervisor(hv);
		}

		public void Delete(Hypervisor hv){
			RemotingManager.GetRemoteObject().DeleteHypervisor(hv);
		}

		public List<P2PBackup.Common.Node> Get(Discovery req){
			return RemotingManager.GetRemoteObject().Discover(req.Id);
		}


		public HypervisorsWS (){
			
		}
	}
}

