using System;
using System.Collections.Generic;
using System.Diagnostics;
using Node.Utilities;
using Node.Snapshots;
using Node.Virtualization;
using P2PBackup.Common;
using P2PBackup.Common.Volumes;
using ServiceStack.Text;

namespace Node.StorageLayer{

	public interface IPathBrowser{
		//string Browse(string path);	
		BrowseNode Browse(string path);	
		string BrowseSnapshottable(string path);
		string GetDrives();
		string[] GetDrivesNames();
	}


}

namespace Node.StorageLayer{
	public class PathBrowser{
		internal static IPathBrowser GetPathBrowser(){
			if(Utilities.PlatForm.IsUnixClient())
				return new UPathBrowser();
			else
				return new WPathBrowser();
		}
	}
}

namespace Node.StorageLayer{
	using System.IO;
	using System.Collections.Generic;

	public class WPathBrowser:IPathBrowser{
		
		public string[] GetDrivesNames(){
			List<string> dList = new List<string>();
			DriveInfo[] drives = DriveInfo.GetDrives();
			foreach(DriveInfo wdi in drives)
				dList.Add(wdi.Name);	
			return dList.ToArray();
		}
		
		public string GetDrives(){
			Logger.Append(Severity.DEBUG,  "starting browsing drives");
			string dXml ="";
			dXml += "<root d=\""+null/*this.UserName*/+"\">";
			try{
				DriveInfo[] drives =  DriveInfo.GetDrives();
				foreach(DriveInfo wdi in drives){
					if(wdi.DriveType == DriveType.Fixed){
						dXml += "<d n=\""+wdi.Name+"\" type=\"v\" label=\""+wdi.VolumeLabel+"\" size=\""+wdi.TotalSize+"\" avail=\""+wdi.AvailableFreeSpace+"\" fs=\""+wdi.DriveFormat+"\"";
						string prov = SnapshotProvider.GetDriveSnapshotProviderName(wdi.Name);
						//if(prov.IsVolumeSnapshottable(wdi.Name))
							dXml += " snap=\""+prov+"\"";
						dXml += "></d>";
					}
					else if(wdi.DriveType == DriveType.Network)
						dXml += "<d n=\""+wdi.Name+"\" type=\"n\" size=\""+wdi.TotalSize+"\" avail=\""+wdi.AvailableFreeSpace+"\" fs=\""+wdi.DriveFormat+"\"></d>";
					else if(wdi.DriveType == DriveType.NoRootDirectory)
						dXml += "<d n=\""+wdi.Name+"\" type=\"d\"></d>";
					else if(wdi.DriveType == DriveType.Unknown)
						dXml += "<d n=\""+wdi.Name+"\" type=\"v\" label=\""+wdi.VolumeLabel+"\" size=\""+wdi.TotalSize+"\" avail=\""+wdi.AvailableFreeSpace+"\" fs=\""+wdi.DriveFormat+"\"></d>";
				}
			}
			catch(Exception e){
				Logger.Append(Severity.WARNING, "Couldn't browse drives, error :"+e.Message );	
			}
			finally{
				dXml += "</root>";
				Logger.Append(Severity.TRIVIA, "Asked to browse drives, sending "+dXml);
			}
			return dXml;
		}

		public BrowseNode Browse(string path){
			Logger.Append(Severity.DEBUG, "starting browsing '"+path+"'");
			BrowseNode n = new BrowseNode();
			try{
				FileSystem[] drives =  FilesystemManager.Instance().GetAllDrives();
				string[] pdi;
				//handle windows specificity : multiple roots (drives)
				if(path.Trim() == "/"){
					pdi = new string[drives.Length];
					for(int i=0; i< pdi.Length; i++)
						pdi[i] = drives[i].MountPoint;
				}
				else{
					if(path.Substring(0,1) == "/")
						path = path.Substring(1);
					pdi = Directory.GetDirectories(path);
					
				}
				foreach(string d in pdi){
					bool found = false;
					foreach(FileSystem wdi in drives){
						if(wdi.MountPoint == Path.Combine(path,d)){
							found = true;
							BrowseItem child = new BrowseItem{Name=d.Substring(d.LastIndexOf('/')+1), Label=wdi.Path, Size=wdi.Size, Avail=wdi.AvailableFreeSpace, FS=wdi.DriveFormat, Type="fs"};
							ISnapshotProvider prov = SnapshotProvider.GetProvider(wdi.MountPoint);
							if(prov.IsVolumeSnapshottable(wdi.MountPoint))
								child.Snap = prov.ToString();
							//	dXml += " snap=\""+prov+"\"";
							//dXml += "></d>";
							n.Children.Add(child);
							//}
							/*else if(wdi.DriveType == DriveType.Network)
								dXml += "<d n=\""+d.Substring(d.LastIndexOf('/')+1)+"\" type=\"n\" size=\""+wdi.Size+"\" avail=\""+wdi.AvailableFreeSpace+"\" fs=\""+wdi.DriveFormat+"\"></d>";
							else if(wdi.DriveType == DriveType.NoRootDirectory)
								dXml += "<d n=\""+path+"/"+d.Substring(d.LastIndexOf('/')+1)+"\" type=\"d\"></d>";
							else if(wdi.DriveType == DriveType.Unknown)
								dXml += "<d n=\""+d.Substring(d.LastIndexOf('/')+1)+"\" type=\"v\" label=\""+wdi.BlockDevice+"\" size=\""+wdi.Size+"\" avail=\""+wdi.AvailableFreeSpace+"\" fs=\""+wdi.DriveFormat+"\"></d>";
							*/
						}
					}
					if(found == false)
						n.Children.Add(new BrowseItem{Name = d.Substring(d.LastIndexOf('\\')+1)});
				}
			}
			catch(Exception e){
				Logger.Append(Severity.WARNING, "Couldn't brose directory "+path+", error :"+e.Message );	
			}
			finally{
				Logger.Append(Severity.TRIVIA, "Asked to browse '"+path+"', sending "+n.ToJson<BrowseNode>());
			}
			return n;
		}

		/*public string Browse(string path){
			Logger.Append(Severity.DEBUG, "starting browsing '"+path+"'");
			string dXml ="";
			dXml += "<root d=\""+null+"\">";
			try{
				FileSystem[] drives =  FilesystemManager.Instance().GetAllDrives();
				string[] pdi;
				//handle windows specificity : multiple roots (drives)
				if(path.Trim() == "/"){
					pdi = new string[drives.Length];
					for(int i=0; i< pdi.Length; i++)
						pdi[i] = drives[i].MountPoint;
				}
				else{
					if(path.Substring(0,1) == "/")
						path = path.Substring(1);
					pdi = Directory.GetDirectories(path);
					
				}
				foreach(string d in pdi){
					bool found = false;
					foreach(FileSystem wdi in drives){
						if(wdi.MountPoint == Path.Combine(path,d)){
							found = true;
							//if(wdi.DriveType == DriveType.Fixed){
								dXml += "<d n=\""+d.Substring(d.LastIndexOf('/')+1)+"\" type=\"v\" label=\""+wdi.Path+"\" size=\""+wdi.Size+"\" avail=\""+wdi.AvailableFreeSpace+"\" fs=\""+wdi.DriveFormat+"\"";
								ISnapshotProvider prov = SnapshotProvider.GetProvider(wdi.MountPoint);
								if(prov.IsVolumeSnapshottable(wdi.MountPoint))
									dXml += " snap=\""+prov+"\"";
								dXml += "></d>";
								
							//}
							//else if(wdi.DriveType == DriveType.Network)
							//	dXml += "<d n=\""+d.Substring(d.LastIndexOf('/')+1)+"\" type=\"n\" size=\""+wdi.Size+"\" avail=\""+wdi.AvailableFreeSpace+"\" fs=\""+wdi.DriveFormat+"\"></d>";
							//else if(wdi.DriveType == DriveType.NoRootDirectory)
							//	dXml += "<d n=\""+path+"/"+d.Substring(d.LastIndexOf('/')+1)+"\" type=\"d\"></d>";
							//else if(wdi.DriveType == DriveType.Unknown)
							//	dXml += "<d n=\""+d.Substring(d.LastIndexOf('/')+1)+"\" type=\"v\" label=\""+wdi.BlockDevice+"\" size=\""+wdi.Size+"\" avail=\""+wdi.AvailableFreeSpace+"\" fs=\""+wdi.DriveFormat+"\"></d>";

						}
					}
					if(found == false)
							dXml += "<d n=\""+d.Substring(d.LastIndexOf('\\')+1)+"\"></d>";
				}
			}
			catch(Exception e){
				Logger.Append(Severity.WARNING, "Couldn't brose directory "+path+", error :"+e.Message );	
			}
			finally{
				dXml += "</root>";
				Logger.Append(Severity.TRIVIA, "Asked to browse '"+path+"', sending "+dXml);
			}
			return dXml;
		}*/
		
		public string BrowseSnapshottable(string path){
			//ISnapshotProvider snapProvider = SnapshotProvider.GetProvider();
			//snapProvider.Get();
			
			return "";
		}
	}
	
	
}

namespace Node.StorageLayer{
	using Mono.Posix;
	using Mono.Unix;
	using System.IO;
	using System.Collections.Generic;

	public class UPathBrowser:IPathBrowser{
		
		public string[] GetDrivesNames(){
			List<string> dList = new List<string>();
			try{ 
				foreach(FileSystem sd in FilesystemManager.Instance().GetAllDrives())
					dList.Add(sd.MountPoint);
			}
			catch(Exception e){
				Logger.Append(Severity.ERROR, "Could not get system drives list : "+e.Message);
			}
			
			return dList.ToArray();
		}
		
		public string GetDrives(){
			Logger.Append(Severity.DEBUG, "starting browsing drives");
			string dXml ="";
			dXml += "<root d=\""+null/*this.UserName*/+"\">";
			try{
				FileSystem[] drives = FilesystemManager.Instance().GetAllDrives();
				foreach(FileSystem wdi in drives){
					//if(wdi.DriveType == DriveType.Fixed){
						dXml += "<d n=\""+wdi.MountPoint+"\" type=\"v\" label=\""+wdi.Path+"\" size=\""+wdi.Size+"\" avail=\""+wdi.AvailableFreeSpace+"\" fs=\""+wdi.DriveFormat+"\" snap=\""+wdi.SnapshotType+"\"></d>";
						/*ISnapshotProvider prov = SnapshotProvider.GetProvider(wdi.Name);
						if(prov.IsVolumeSnapshottable(wdi.Name))
							dXml += " snap=\""+prov.Type.ToString()+"\"";
						dXml += "></d>";*/
						
					//}
					/*else if(wdi.DriveType == DriveType.Network)
						dXml += "<d n=\""+wdi.MountPoint+"\" type=\"n\" size=\""+wdi.Size+"\" label=\""+wdi.BlockDevice+"\" avail=\""+wdi.AvailableFreeSpace+"\" fs=\""+wdi.DriveFormat+"\"></d>";
					else if(wdi.DriveType == DriveType.NoRootDirectory)
						dXml += "<d n=\""+wdi.MountPoint+"\" type=\"d\"></d>";
					else if(wdi.DriveType == DriveType.Unknown)
						dXml += "<d n=\""+wdi.MountPoint+"\" type=\"v\" label=\""+wdi.BlockDevice+"\" size=\""+wdi.Size+"\" avail=\""+wdi.AvailableFreeSpace+"\" fs=\""+wdi.DriveFormat+"\" snap=\""+wdi.SnapshotType+"\"></d>";
					*/
				}
			}
			catch(Exception e){
				Logger.Append(Severity.WARNING, "Couldn't browse drives, error :"+e.Message+"--"+e.StackTrace );	
			}
			/*try{
				SpecialDrive[] ZFSDrives = VolumeManager.GetZfsDrives(); 
				foreach(SpecialDrive wdi in ZFSDrives){
					dXml += "<d n=\""+wdi.Name+"\" type=\"v\" label=\""+wdi.VolumeLabel+"\" size=\""+wdi.TotalSize+"\"  fs=\"ZFS\" snap=\"ZFS\"";
					dXml += "></d>";
				}
			}
			catch(Exception e){
				Logger.Append(Severity.WARNING, "UPathBrowser.GetDrives", "Couldn't browse ZFS drives, error :"+e.Message+"--"+e.StackTrace );	
			}*/
			dXml += "</root>";
			Logger.Append(Severity.TRIVIA, "Asked to browse drives, sending "+dXml);
			return dXml;
		}

		public BrowseNode Browse(string path){
			var n = new BrowseNode();
			try{
				FileSystem[] drives;
				try{ // Solaris throws exception here
					drives = FilesystemManager.Instance().GetAllDrives();
				}
				catch(Exception se){
					Logger.Append(Severity.NOTICE, "Error getting drives : "+se.Message);
					drives = null;
				}
				string[] pdi = Directory.GetDirectories(path);
				foreach(string d in pdi){
					bool found = false;
					if(drives != null){
						foreach(FileSystem udi in drives){
							if(udi.MountPoint == Path.Combine(path,d)){
								found = true;
								//if(udi.DriveType == DriveType.Fixed)

								var b = new BrowseItem{Name=d.Substring(d.LastIndexOf('/')+1), Type="fs", Label=udi.Path, Size=udi.Size, Avail=udi.AvailableFreeSpace, FS=udi.DriveFormat, Snap=udi.SnapshotType.ToString()};
								n.Children.Add(b);
								//else if(udi.DriveType == DriveType.Network)
								//	dXml += "<d n=\""+d.Substring(d.LastIndexOf('/')+1)+"\" fn=\""+d+"\" type=\"n\" size=\""+udi.Size+"\" avail=\""+udi.AvailableFreeSpace+"\" fs=\""+udi.DriveFormat+"\"></d>";
								//else if(udi.DriveType == DriveType.NoRootDirectory)
								//	dXml += "<d n=\""+path+"/"+d.Substring(d.LastIndexOf('/')+1)+"\" fn=\""+d+"\" type=\"d\"></d>";
								//else if(udi.DriveType == DriveType.Unknown)
								//	dXml += "<d n=\""+d.Substring(d.LastIndexOf('/')+1)+"\" fn=\""+d+"\" type=\"v\" label=\""+udi.BlockDevice+"\" size=\""+udi.Size+"\" avail=\""+udi.AvailableFreeSpace+"\" fs=\""+udi.DriveFormat+"\" snap=\""+udi.SnapshotType+"\"></d>";
								
							}
						}
					}
					if(found == false || drives == null) // drives can be null at least under SunOS
						n.Children.Add(new BrowseItem{Name=d.Substring(d.LastIndexOf('/')+1)});
				}
			}
			catch(Exception e){
				Logger.Append(Severity.WARNING, "Couldn't browse directory '"+path+"', error :"+e.Message+"---"+e.StackTrace);	
			}
			finally{
				Logger.Append(Severity.TRIVIA, "Asked to browse "+path+", sending "+n.ToJson<BrowseNode>());
			}
			return n;
		}

		/*public string Browse(string path){
			path = path.Trim();
			
			string dXml ="";
			dXml += "<root d=\"/"+null+"\">";
			try{
				FileSystem[] drives;
				//try{ // Solaris throws exception here
					drives = FilesystemManager.Instance().GetAllDrives();
				//}
				//catch{drives = null;}
				//Logger.Append(Severity.DEBUG, "User.Browse", @"starting browsing '"+path.Substring(0, path.LastIndexOf('/')+1)+"', search pattern='"+path.Substring(path.LastIndexOf('/')+1)+"*");
				//string[] pdi = Directory.GetDirectories(path.Substring(0, path.LastIndexOf('/')+1), path.Substring(path.LastIndexOf('/')+1)+"*");
				string[] pdi = Directory.GetDirectories(path);
				foreach(string d in pdi){
					bool found = false;
					if(drives != null){
						foreach(FileSystem udi in drives){
							if(udi.MountPoint == Path.Combine(path,d)){
								found = true;
								//if(udi.DriveType == DriveType.Fixed)
									dXml += "<d n=\""+d.Substring(d.LastIndexOf('/')+1)+"\" fn=\""+d+"\" type=\"v\" label=\""+udi.Path+"\" size=\""+udi.Size+"\" avail=\""+udi.AvailableFreeSpace+"\" fs=\""+udi.DriveFormat+"\" snap=\""+udi.SnapshotType+"\"></d>";
								//else if(udi.DriveType == DriveType.Network)
								//	dXml += "<d n=\""+d.Substring(d.LastIndexOf('/')+1)+"\" fn=\""+d+"\" type=\"n\" size=\""+udi.Size+"\" avail=\""+udi.AvailableFreeSpace+"\" fs=\""+udi.DriveFormat+"\"></d>";
								//else if(udi.DriveType == DriveType.NoRootDirectory)
								//	dXml += "<d n=\""+path+"/"+d.Substring(d.LastIndexOf('/')+1)+"\" fn=\""+d+"\" type=\"d\"></d>";
								//else if(udi.DriveType == DriveType.Unknown)
								//	dXml += "<d n=\""+d.Substring(d.LastIndexOf('/')+1)+"\" fn=\""+d+"\" type=\"v\" label=\""+udi.BlockDevice+"\" size=\""+udi.Size+"\" avail=\""+udi.AvailableFreeSpace+"\" fs=\""+udi.DriveFormat+"\" snap=\""+udi.SnapshotType+"\"></d>";
									
							}
						}
					}
					if(found == false || drives == null) // drives can be null at least under SunOS
						dXml += "<d n=\""+d.Substring(d.LastIndexOf('/')+1)+"\" fn=\""+d+"\"></d>";
				}
			}
			catch(Exception e){
				Logger.Append(Severity.WARNING, "Couldn't browse directory '"+path+"', error :"+e.Message+"---"+e.StackTrace);	
			}
			finally{
				dXml += "</root>";
				Logger.Append(Severity.TRIVIA, "Asked to browse "+path+", sending "+dXml);
			}
			return dXml;
		}*/
		
		public string BrowseSnapshottable(string path){
			return "<snap d=\"\"></snap>";	
		}
		
		
		
	}
	
	
}