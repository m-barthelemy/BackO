using System;
using Node.Utilities;
using P2PBackup.Common;
using P2PBackup.Common.Volumes;

namespace Node.StorageLayer  {

	public class StorageLayoutFactory {

		private StorageLayoutFactory(){
		}

		internal static IStorageDiscoverer Create(string providerName){
			if(string.IsNullOrEmpty(providerName)) providerName = "local";
			return (IStorageDiscoverer)Activator.CreateInstance(
				PluginsDiscoverer.Instance().Plugins[providerName].RawType
			);
			/*switch (providerName.ToLower()){
				//case "vmware":
				//	return new VMWareDisksDiscoverer();
				case "local":case "fs":case null:
					if(Utilities.PlatForm.Instance().OS.ToLower() == "linux")
						return new LinuxStorageDiscoverer();
					else if(Utilities.PlatForm.Instance().OS.ToLower().StartsWith("nt"))
						return new NTStorageDiscoverer();
					else return new FallbackDiscoverer();
				default:
					Logger.Append(Severity.ERROR, "Cannot select storage  type'"+providerName+"' (doesn't exist or plugin not loaded)");
					return null;

			}*/
		}
	}
}

