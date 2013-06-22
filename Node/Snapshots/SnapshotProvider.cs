using System;
using Node.Utilities;
using P2PBackup.Common;
using Node.StorageLayer;

namespace Node.Snapshots{

	public class SnapshotProvider{

		private SnapshotProvider (){
			
			
		}
			
		public static string GetDriveSnapshotProviderName(string driveName){
			//if(Utilities.PlatForm.IsUnixClient())
				return FilesystemManager.GetDriveSnapshotType(driveName).ToString();
			//else
			//	return "VSS"; //new VSSProvider();
		}


		
		// obtain the right provider at backup time, according to what has been stored in database for the backupset
		internal static ISnapshotProvider GetProvider(string providerName){
			switch(providerName){
				case "VSS": // Windows VSS shapshotting system
					return (ISnapshotProvider)new VSSProvider();
					//break;
				case "LVM": // Linux LVM2 snapshotting system (layer below traditional FS)
					return (ISnapshotProvider)new LVMProvider();
					//break;
				case "NONE":case "": // return the fake provider (returning a snapshot with the path to the original drive)
					return (ISnapshotProvider)new NullProvider();
					//break;
				case "ZFS": // Solaris/OpenSolaris/FreeBSD ZFS filesystem with snapshotting capabilities
					return (ISnapshotProvider)new ZfsProvider();
					//break;
				case "BTRFS": // Linux filesystem with snapshotting capabilities
					return (ISnapshotProvider)new BtrfsProvider();
					//break;
				default:
					Logger.Append(Severity.WARNING, "Asking for a non-existant snapshot provider ("+providerName+"). Ignoring, will do backup without snapshot.");
					return (ISnapshotProvider)new NullProvider();
					//break;
				
			}
			
			
		}

		internal static ISnapshotProvider GetProvider(ISnapshot sn){
			Console.WriteLine(">>>>>>>>>>>>>>>>>>>>> snapshot type to string='"+ sn.GetType().ToString());
			switch( sn.GetType().ToString().Substring(sn.GetType().ToString().LastIndexOf('.')+1) ){

			case "VSSSnapshot":
				return (ISnapshotProvider)new VSSProvider();

			default:
				return (ISnapshotProvider)new NullProvider();
			}
		}
	}
}

