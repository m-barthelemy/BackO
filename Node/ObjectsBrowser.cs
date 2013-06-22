using System;
using System.Collections.Generic;
using Node.Snapshots;

namespace Node{
	public class ObjectsBrowser{
		private ObjectsBrowser (){
		}
		/// <summary>
		/// Returns an XMl representation of special objects on the system.
		/// The only real use-case we can imagine for now is the list of NT VSS providers
		/// 	(System state, Exchange and SQLServer providers, maybe HyperV vm images)
		/// Another use case (not studied yet) would be VMWare®© storage API
		/// </summary>
		/*internal static string BuildObjectsList(){
			string xml = "";
			xml += "<objects name=\"/\" type=\"\" version=\"\">";
			ISnapshotProvider provider = SnapshotProvider.GetProvider();
			foreach(ISnapshot sn in provider.ListSpecialObjects()){
				xml += "<object name=\""+sn.Name+"\"  path=\""+sn.Path+"\" version=\""+sn.Version+"\" type=\""+sn.Type+"\">";
				foreach(ISnapshot childSn in sn.ChildComponents)
					xml += "<childObject name=\""+childSn.Name+"\"  path=\""+childSn.Path+"\" version=\""+childSn.Version+"\" type=\""+childSn.Type+"\" leaf=\"true\"/>";
				xml += "</object>";
			}
			xml += "</objects>";
			return xml;
		}*/
		
		internal static string BuildObjectsList(){
			string xml = "";
			xml += "{name:\"VSS Writers\", type:\"\", version:\"\", children:[";
			if(!Utilities.PlatForm.IsUnixClient()){
				ISnapshotProvider provider = SnapshotProvider.GetProvider("VSS");
				List<ISnapshot> spos = provider.ListSpecialObjects();
				if(spos != null){
					foreach(ISnapshot sn in spos){
						xml += "{name:\""+sn.Path+"\",  path:\""+sn.MountPoint+"\", version:\""+sn.Version+"\", type:\""+sn.Type+"\", disabled:\""+sn.Disabled+ "\", checked:false, children:[";
						foreach(ISnapshot childSn in sn.ChildComponents){
							xml += "{name:\""+childSn.Path+"\",  path:\""+childSn.MountPoint+"\", version:\""+childSn.Version+"\", type:\""+childSn.Type+"\", leaf:\"true\", disabled:\""+childSn.Disabled
								+"\", icon:\""+((childSn.Icon == null)? "" : "data:image/x-windows-bmp;base64,"+Convert.ToBase64String(childSn.Icon) )+"\"";
							if(childSn.Disabled == false)
									xml += ", checked:false";
							xml +="},";
						}
						xml += "]},";
					}
				}
			}
			xml += "]}";
			
			return xml;
		}
	}
}

