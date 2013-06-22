using System;
using P2PBackup.Common;
using Node.DataProcessing;

namespace Node {

	internal class IndexBrowser {

		public IndexBrowser(){
		}

		internal static BrowseNode Browse(long taskId, string fileSystem, long parentItemId, string filter){
			Index i = new Index(taskId, false);
			i.Open();
			var bn = new BrowseNode();
			if(parentItemId == 0 && fileSystem == ""){
				foreach(string fs in i.GetRootDrives()){
					bn.Children.Add (new BrowseItem{
						Name = fs,
						Type = "fs"
					});
				}
			}
			else{
				foreach(IFSEntry entry in i.BrowseChildren(fileSystem, parentItemId, filter)){
					bn.Children.Add (new BrowseItem{
						Name = entry.Name,
						Id = entry.ID,
						Label = ""+entry.ParentID, // temporary, to debug parenthoot
						Type = ""+(int)entry.Kind,
						Size = entry.FileSize

					});
				}
			}
			i.Terminate();
			return bn;
		}
	}
}

