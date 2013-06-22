using System;
using System.Linq;
using System.Collections.Generic;
using P2PBackup.Common;

//  create table specialobjects(nodeid integer, spopath varchar, password integer);

namespace Node.Snapshots{
	public class SPOProvider{
		/*public SPOProvider (){
		}*/
		
		public static ISpecialObject GetByCategory(string objectName, Backup backup, P2PBackup.Common.BackupLevel level, ProxyTaskInfo pti){
			/*switch(objectName){
			case "VSS":
				return new VSS(level);
			case "VMWare VM configuration":
				return new VMWareVmConfig(pti);
			case "StorageLayout":
				return new StorageLayoutSPO(backup.StorageLayout);

			default:
				throw new Exception("No Special object matching category '"+objectName+"'");*/
				
			return (ISpecialObject)Activator.CreateInstance(PluginsDiscoverer.Instance().Plugins[objectName].RawType);	
		}
		
		public static List<string> ListAvailableProviders(){
			/*List<string> providers = new List<string>();
			if(!Utilities.PlatForm.IsUnixClient())
				providers.Add("VSS");
			providers.Add("VMWARE");
			providers.Add ("VMWare VM configuration");
			providers.Add("MYSQL");
			providers.Add("LIBVIRT");
			return providers;*/
			//var plugNames = from Plugin p in PluginsDiscoverer.Instance().Plugins.Values where p.Category == PluginCategory.ISpecialObject select p.Name;
			return (from Plugin p 
			        in PluginsDiscoverer.Instance().GetPlugins<ISpecialObject>() 
			        select p.Name).ToList();
		}
	}
}

