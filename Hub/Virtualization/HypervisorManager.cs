using System;
using System.Collections.Generic;
using P2PBackup.Common;
using P2PBackup.Common.Virtualization;
using P2PBackupHub.Utilities;


namespace P2PBackupHub.Virtualization {

	// almost-stub IVmProvider factory.
	public class HypervisorManager:Hypervisor, IDisposable {


		private IVmProvider provider;

		public HypervisorManager(){

		}

		public List<P2PBackup.Common.Node> Discover(){
			if(this.Kind == null)
				throw new NullReferenceException("VM provider type not set.");
			switch(this.Kind){
			case "vmware":
				provider = new VMWare.VMWareHandler();
				break;
			case "libvirt":
				provider = new LibvirtHandler();
				break;
			default:
				Logger.Append("HUBRN", Severity.ERROR, "Unknown Hypervisor type '"+this.Kind+"'");
				return default(List<P2PBackup.Common.Node>);
				//break;
			}
			provider.LogEvent +=new EventHandler<LogEventArgs>(this.LogReceivedEvent);
			provider.Connect(Url, UserName, Password.Value);

			List<P2PBackup.Common.Node> vms = provider.GetVMs();
			/*foreach(P2PBackup.Common.Node vm in vms){
				List<SystemDrive> vmDrives = provider.GetDisks(vm);
				vm.
			}*/

			return vms;
		}

		private void LogReceivedEvent(object sender, LogEventArgs args){
			Logger.Append(provider.Name, args.Severity, args.Message);
		}

		public void Dispose(){

			provider.Dispose();
			provider.LogEvent -= LogReceivedEvent;
		}
	}
}

