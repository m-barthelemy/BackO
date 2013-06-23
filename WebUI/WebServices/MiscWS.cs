using System;
using System.Linq;
using System.Collections.Generic;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceHost;

using P2PBackup.Common;
using SharpBackupWeb.Utilities;

namespace Backo.Api.WebServices {

	[Route("/api/Misc/Cultures")]
	public class GetCultures : IReturn<List<Tuple<string, string>>>{
		//public int Interval { get; set; }
	}

	[Route("/api/Misc/Plugins/{Category}/")]
	public class GetAvailablePluginsNames : IReturn<List<Plugin>>{
		public PluginCategory Category{get;set;}
	}

	[Authenticate]
	public class MiscWS :AppServiceBase{


		public Object Get(GetCultures req){
			return "[{Key:'fr-FR', Value:'Français'}, {Key:'en-US', Value:'English (US)'}, {Key:'es-ES', Value:'Espanol (Espana)'}]";

		}

		/*public List<KeyValuePair<string,string>> Get(GetAvailablePluginsNames req){
			List<KeyValuePair<string,string>> pluginNames = new List<KeyValuePair<string,string>>();
			foreach (string plugin in  RemotingManager.GetRemoteObject().GetPlugins(req.Category))
				pluginNames.Add(new KeyValuePair<string, string>("Name", plugin));
			return pluginNames;
		}*/

		public List<Plugin> Get(GetAvailablePluginsNames req){
			/*List<Plugin> pluginNames = new List<Plugin>();
			foreach (string plugin in  RemotingManager.GetRemoteObject().GetPlugins(req.Category))
				pluginNames.Add(new KeyValuePair<string, string>("Name", plugin));
			return pluginNames;*/
			return RemotingManager.GetRemoteObject().GetPlugins(req.Category);
		}

		public MiscWS (){
		}
	}
}

