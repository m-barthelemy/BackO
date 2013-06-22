using System;
using System.IO;
using System.Diagnostics;
//using System.Collections;
using System.Collections.Generic;
using Node.Snapshots;
using Node.Utilities;
using P2PBackup.Common;
using P2PBackup.Common.Volumes;

namespace Node.StorageLayer{
	
	/// <summary>
	/// Manages all volumes on the system, and their snapshottable state
	/// TODO: make singleton Instance(), build a cache on first call, and reuse it to avoid heavy operations
	/// </summary>
	public class FilesystemManager{
		
		private static  FilesystemManager _instance;
		private List<FileSystem> allDrives;
		
		private FilesystemManager (){
			allDrives = new List<FileSystem>();
#if OS_UNIX
			try{
				allDrives.AddRange(GetZfsDrives());
			}
			catch{}
			try{
				allDrives.AddRange(GetLVMDrives());
			}
			catch{}
#endif
			try{
				foreach(FileSystem sdi in GetBasicDrives()){
					try{
					if(! allDrives.Contains(sdi)) //we avoid to add a drive already detected (will be the case for lvm partitions)
					   allDrives.Add(sdi);
					}
					catch{}
				}
			}
			catch(Exception e){ //probably running on Solaris
				Logger.Append(Severity.WARNING, "Couldn't gather system drives : "+e.ToString());
			}
		}
		
		public static FilesystemManager Instance(){
			if(_instance == null)
				_instance = new FilesystemManager();
			return _instance;
		}
		
		//private  FileSystem[] systemDrivez;
		
		public static SnapshotType GetDriveSnapshotType(string driveName){
			try{
				DriveInfo udi = new DriveInfo(driveName);
				Console.WriteLine("GetDriveSnapshotType, drive "+driveName+"="+udi.DriveFormat);
				if(udi.DriveFormat == "nfs")
					return SnapshotType.NONE;
				else if(udi.DriveFormat == "zfs")
					return SnapshotType.ZFS;
				else if(udi.DriveFormat == "NTFS")
					return SnapshotType.VSS;
				else if(udi.DriveFormat == "btrfs")
					return SnapshotType.BTRFS;
				}
			catch{} // if an exception is thrown, most likely we hit a ZFS filesystem
#if OS_UNIX
			// now the harder part begins : since Mono can't (for now) report a zfs fs (doesn't appear in /etc/fstab)
			//   nor a lvm one (it's not a fs but a volume manager with snapshotting capabilities),
			//   we have to gather lvm and zfs volumes by hand.
			foreach(FileSystem sd in GetZfsDrives()){
				if(	sd.MountPoint == driveName)
					return SnapshotType.ZFS;
			}
			if(Utilities.PlatForm.Instance().OS == "Linux")
				foreach(FileSystem sd in GetLVMDrives()){
					if(	sd.MountPoint == driveName)
						return SnapshotType.LVM;
			}
#endif
			return SnapshotType.NONE;
			
		}
	

#if OS_UNIX
		/// <summary>
		///  Zfs volumes are not listed in fstab, so we need special handling to retrieve and list them
		/// </summary>
		/// <returns>
		/// A <see cref="SpecialDrive[]"/>
		/// </returns>
		internal static FileSystem[] GetZfsDrives(){
			ProcessStartInfo pi = new ProcessStartInfo("zfs", "list -H -t filesystem -o name,mountpoint");
			pi.RedirectStandardOutput = true;
			pi.RedirectStandardError = true;
			pi.UseShellExecute = false;
			List<FileSystem> zLines = new List<FileSystem>();
			try{
				using (Process p = Process.Start(pi)){
					p.WaitForExit();
					//p.StandardOutput.ReadLine(); // get rid of the header line
					string line = "";
					if(p.StandardError.ReadLine() != null) // no Zfs on this system
						return new FileSystem[0];
					while((line = p.StandardOutput.ReadLine()) != null){
						FileSystem zd = new FileSystem();
						char[] sep ={' ', '\t'};
						string[] vol =  line.Split(sep, StringSplitOptions.RemoveEmptyEntries);
						zd.MountPoint = vol[1];
						zd.Path = vol[0];
						//zd.Name = vol[0];
						zd.DriveFormat = "zfs";
						zd.SnapshotType = SnapshotType.ZFS;
						//zd.DriveType = DriveType.Fixed;
						/*long size = 0;
						long.TryParse(vol[2], out size);
						zd.TotalSize = size;*/
						zLines.Add(zd);
					}
				}
			}
			catch(Exception e){
				Logger.Append(Severity.WARNING, "Error gathering ZFS volumes: "+e.Message);	
			}
			
			// to know size and free space, no other solution than re-executing a command, for each detected volume
			// if there is a quota, it will be used as total volume size ; else we use 'available' prorperty
			// zfs get -H -p -o name,value -r quota,available,used rpool
			foreach(FileSystem sd in zLines){
				ProcessStartInfo vpi = new ProcessStartInfo("zfs", "get -H -p -o value -r quota,available,used "+sd.Path);
				vpi.RedirectStandardOutput = true;
				vpi.RedirectStandardError = true;
				vpi.UseShellExecute = false;
				using(Process vp = Process.Start(vpi)){
					vp.WaitForExit();
					if(vp.ExitCode != 0)
						break;
					long quota = 0;
					long avail = 0;
					long used = 0;
					long.TryParse(vp.StandardOutput.ReadLine(), out quota);
					long.TryParse(vp.StandardOutput.ReadLine(), out avail);
					long.TryParse(vp.StandardOutput.ReadLine(), out used);
					if(quota > 0) sd.Size = quota;
					else sd.Size = avail;
					sd.AvailableFreeSpace = sd.Size - used;
				}
			}
			return zLines.ToArray();
		}
		
		/// <summary>
		///  LVM volumes cannot be detected by mono/.Net, so we need special handling to retrieve and list them
		/// </summary>
		/// <returns>
		/// A <see cref="SpecialDrive[]"/>
		/// </returns>
		internal static FileSystem[] GetLVMDrives(){
			
			ProcessStartInfo pi = new ProcessStartInfo("lvm", "lvs --noheadings --units b --nosuffix");
			pi.RedirectStandardOutput = true;
			pi.RedirectStandardError = true;
			pi.UseShellExecute = false;
			List<FileSystem> lvmLines = new List<FileSystem>();
			try{
				using(Process p = Process.Start(pi)){
					p.WaitForExit();
					string line = "";
					//if(p.StandardError.ReadLine() != null) // no LVM on this system
					//	return new SpecialDrive[0];
					while((line = p.StandardOutput.ReadLine()) != null){
						FileSystem lvmd = new FileSystem();
						char[] sep ={' ', '\t'};
						string[] vol =  line.Split(sep, StringSplitOptions.RemoveEmptyEntries);
						//zd.Name = vol[1];
						long size = 0;
						lvmd.Path = "/dev/"+vol[1]+"/"+vol[0];	
						long.TryParse(vol[3], out size);
						lvmd.Size = size;
						//lvmd.DriveType = DriveType.Fixed;
						lvmd.SnapshotType = SnapshotType.LVM;
						// Now, try to guess mountpoint. As LVM is a layer completly independant from physical disks and mounts, this require some magic
						try{
							StreamReader sr = new StreamReader("/proc/mounts");
							string mountLine = String.Empty;
							while((mountLine = sr.ReadLine()) != null){
								char[] sepM ={' ', '\t'};
								string[] mountInfo =  mountLine.Split(sepM, StringSplitOptions.RemoveEmptyEntries);
								if(mountInfo[0].Equals("/dev/mapper/"+vol[1]+"-"+vol[0])){
									lvmd.MountPoint = mountInfo[1];
									lvmd.DriveFormat = mountInfo[2];
								}
							}
						}
						catch{} // probably /proc/mounts doesn't exists, because we're not on Linux
						//Console.WriteLine("LVM Drive : name="+lvmd.Name+", volumelabel="+lvmd.VolumeLabel+", size="+lvmd.TotalSize);
						// reject not mounted volumes
						if(lvmd.Path != null && lvmd.MountPoint!= null)
							lvmLines.Add(lvmd);
					}
				}
			}
			catch(Exception e){
					Logger.Append(Severity.WARNING, "Error gathering LVM volumes: "+e.Message);	
			}
			
			return lvmLines.ToArray();
			
		}
#endif 		
		internal static FileSystem[] GetBasicDrives(){
			if(Utilities.PlatForm.IsUnixClient())
				return UDrives.GetDrives();
			else
				return WDrives.GetDrives();
		}
		
		/// <summary>
		/// We try to return all drives (windows drives, unix mounted volumes) that we can found
		/// The tricky part is that DriveInfo.GetDrives is not enough since it cannot detect neither lvm nor zfs volumes,
		///  so we have to discover manually
		/// </summary>
		/// <returns>
		/// A <see cref="SpecialDrive[]"/>
		/// </returns>
		internal  FileSystem[] GetAllDrives(){
			//if(systemDrivez != null)
			//	return systemDrivez;
			
			return allDrives.ToArray();
		}
	}
	
}

	namespace Node.StorageLayer{
		using System.IO;
		using System.Collections.Generic;
		class WDrives{
			internal static FileSystem[] GetDrives(){
				List<FileSystem> allDrives = new List<FileSystem>();
				DriveInfo[] drives = DriveInfo.GetDrives();
				foreach(DriveInfo wdi in drives){
					try{
					FileSystem sd = new FileSystem();
					sd.MountPoint = wdi.Name;
					sd.OriginalMountPoint = wdi.Name;
					sd.Size = wdi.TotalSize;
					sd.AvailableFreeSpace = wdi.AvailableFreeSpace;
					sd.Path = wdi.VolumeLabel;
					sd.DriveFormat = wdi.DriveFormat;
					sd.Label = wdi.VolumeLabel;
					//sd.DriveType = wdi.DriveType;
					try{
						sd.SnapshotType = FilesystemManager.GetDriveSnapshotType(sd.MountPoint);
						allDrives.Add(sd);
					}
					catch{}
					
					}
					catch{} // dirve not ready windows error
				}
				return allDrives.ToArray();
			}
		}
	}
#if OS_UNIX		
	namespace Node.StorageLayer{
		using Mono.Unix;
		using System.Collections;
		class UDrives{
			internal static FileSystem[] GetDrives(){
				List<FileSystem> allDrives = new List<FileSystem>();
				UnixDriveInfo[] drives = UnixDriveInfo.GetDrives();
				foreach(UnixDriveInfo udi in drives){
					// We exclude some irrelevant (pseudo)filesystems
					/*if(udi.DriveFormat == "proc" || udi.DriveFormat == "sysfs" || udi.DriveFormat == "debugfs"
				   		|| udi.DriveFormat == "devpts" || udi.DriveFormat == "procfs")
						continue;*/
					FileSystem sd = new FileSystem();
					sd.MountPoint = udi.Name;
					sd.OriginalMountPoint = udi.Name;
					//sd.Name = udi.VolumeLabel;
					sd.Size = udi.TotalSize;
					sd.AvailableFreeSpace = udi.AvailableFreeSpace;
					sd.Path = udi.VolumeLabel;
					//sd.Path = udi.Name;
				
					sd.DriveFormat = udi.DriveFormat;
					//sd.DriveType = (DriveType)udi.DriveType;
					try{
						sd.SnapshotType = FilesystemManager.GetDriveSnapshotType(sd.MountPoint);
					}
					catch{}
					allDrives.Add(sd);
					
				}
				return allDrives.ToArray();
			}
			
		}
	}
#endif