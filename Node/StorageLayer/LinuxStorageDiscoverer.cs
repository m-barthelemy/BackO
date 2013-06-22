#if OS_UNIX
namespace Node.StorageLayer {
	using System;
	using System.IO;
	using Mono.Posix;
	using System.Collections.Generic;
	using P2PBackup.Common;
	using P2PBackup.Common.Volumes;
	using P2PBackup.Common.Virtualization;
	using Node.Utilities;

	internal class LinuxStorageDiscoverer:IStorageDiscoverer{

		public string Name{get{ return "linux";}}
		public string Version{get{return "0.1";}}
		public bool IsProxyingPlugin{get{return false;}}

		public delegate void LogHandler(int code, Severity severity, string message);
		public event EventHandler<LogEventArgs> LogEvent;

		private bool IsLayoutLoop{
			get{
				return (loopDevices!= null && loopDevices.Count > 0);
			}
		}

		private string[] fileSystemFormats = new string[]{"ext4", "ext3", "ext2", "xfs", "btrfs"};
		private string sysfsRoot = "/sys/class/scsi_disk";
		private List<string> loopDevices;
		private string tempMountsPath;
		private LinuxLoopDeviceHelper lldh = new LinuxLoopDeviceHelper();
		private StorageLayout layout = new StorageLayout();

		public bool Initialize(ProxyTaskInfo ptI){
			if(!Directory.Exists(sysfsRoot))
				return false;
			return true;
		}

		// loop-devices based discovery
		public bool Initialize(List<string> loopDevices, string tempMountsPath){
			if(loopDevices == null || loopDevices.Count == 0)
				return false;
			this.loopDevices = loopDevices;
			this.tempMountsPath = tempMountsPath;
			sysfsRoot = "/sys/devices/virtual/block";
			return Initialize(null);
		}



		/// <summary>
		/// Snapshots the VM and gets its (maybe partial) StorageLayout
		/// </summary>
		/// <returns>
		/// The physical disks.
		/// </returns> retrieve a 
		public StorageLayout BuildStorageLayout(){

			List<FileSystem> fsList = new List<FileSystem>();
			if(this.loopDevices == null) // local layout
				fsList = GetMountedFilesystems();
			/*else
				fsList = MountLoopFilesystems();*/

			Mono.Unix.UnixDirectoryInfo ud = new Mono.Unix.UnixDirectoryInfo(sysfsRoot);

			foreach(Mono.Unix.Native.Dirent scsiDevice in ud.GetEntries()){
				if(this.IsLayoutLoop && !loopDevices.Contains(scsiDevice.d_name)) continue;

				Console.WriteLine ("device subsystem name : "+scsiDevice.d_name);
				Disk disk = new Disk();
				string path = "";
				if(this.IsLayoutLoop)
					path = sysfsRoot+"/"+scsiDevice;
				else
					path = sysfsRoot+"/"+scsiDevice+"/device/block";

				if(!Directory.Exists(path))
					disk.Enabled = false;
				else
					disk.Enabled = true;
				string diskName = "";
				if(this.IsLayoutLoop)
					diskName = scsiDevice.d_name;
				else{
					Mono.Unix.UnixDirectoryInfo scsiPath = new Mono.Unix.UnixDirectoryInfo(path);
					diskName = scsiPath.GetEntries()[0].d_name;
					path += "/"+diskName;
				}
				disk.Path = "/dev/"+diskName;
				disk.Size = long.Parse(File.ReadAllText(path+"/size"));
				//disk.Enabled =  File.ReadAllText(path+"/removable").StartsWith("0");
				if(!File.ReadAllText(path+"/removable").StartsWith("0")) // exclude removable devices (external drives, usb keys...)
					continue;
				disk.Enabled = true;
				disk.SectorSize = uint.Parse(File.ReadAllText(path+"/queue/hw_sector_size"));
				if(this.IsLayoutLoop)
					disk.Type = DiskType.Loop;
				try{
					disk.BlockStream = new FileStream(disk.Path, System.IO.FileMode.Open);
				}
				catch(Exception e){
					Console.WriteLine ("ERROR : "+e.Message);
				}
				//MBR mbr = new MBR(disk.MbrBytes);

				// gather what MBR sees
				//disk.Children.AddRange(StorageLayoutManager.GetPartitionsFromMBR(disk));
				// now compare with what is inside sysfs and complete information
				List<Partition> sysfsParts = GetSysfsPartitions(path, diskName);

				foreach(Partition sysfsP in sysfsParts){
					//Console.WriteLine ("@@@@@@@@@@@@@@@@ GetSysfsPartitions SYS current="+sysfsP.Offset+"  ("+sysfsP.Path+")");
					//foreach(Partition p in disk.Children){
						//Console.WriteLine ("@@@@@@@@@@@@@@@@ GetSysfsPartitions MBR current="+p.Offset);
						//if (p.Offset != sysfsP.Offset) continue;
						//Console.WriteLine ("@@@@@@@@@@@@@@@@ GetSysfsPartitions MATCHED MBR partition "+sysfsP.Path);
						//p.Path = sysfsP.Path;
						//p.Size = sysfsP.Size;

					foreach(FileSystem f in fsList){
							
						string fsDevName = GetPartitionFromPathOrUuid(f.Path);
						//Console.WriteLine ("@@@@@@@@@@@@@@@@ GetSysfsPartitions FS current="+f.Path+", mntpt="+f.MountPoint+", mapped="+fsDevName);
						if(fsDevName == sysfsP.Path){
							sysfsP.AddChild(f);
						//Console.WriteLine ("@@@@@@@@@@@@@@@@ GetSysfsPartitions FS MATCHED"+Environment.NewLine);
						}
					}
					//break;
					disk.AddChild(sysfsP);
					//}
				}

				layout.Entries.Add(disk);
			}// end foreach

			// if using loop devices, try to detect fses and mount only now that we have a complete view and access to disks and parts.
			// doing it so late allows to detect things like LVM volumes spread on multiple partitions or disks
			if(fsList.Count == 0){
				//1- try to mount the loop partitions
				foreach(Partition p in layout.GetAllPartitions(null)){
					// quick, hackish and dirty way to avoid mounting an 'extended' partition,
					// which sould result in an endless loop while burning 100% cPU in zombie state. Yup. Kernel bug?
					if(p.Size == 2) continue;

					FileSystem mountedFS = MountLoopFilesystem(p);
					if(mountedFS != null)
						p.AddChild(mountedFS);
				}
				// 2- try to access and read VM /etc/fstab, from there guess the original loop FS mountpoint
				Dictionary<string,string> originalMountpoints = TryGuessOriginalMountPoint(layout.GetAllFileSystems(null));
				foreach(FileSystem fs in layout.GetAllFileSystems(null)){
					foreach(KeyValuePair<string, string> mp in originalMountpoints){

						string fsBlockDevice = GetPartitionFromPathOrUuid(mp.Key);
						//Console.WriteLine ("ORIGINAL LAYOUT FS: mountpoint key="+mp.Key+", value="+mp.Value+", fromuuid="+fsBlockDevice+", fs.path="+fs.Path+", fs.mountpoint="+fs.MountPoint);
						if(fsBlockDevice == fs.Path){
							fs.OriginalMountPoint = mp.Value;
							if(LogEvent != null) LogEvent(this, new LogEventArgs(0, Severity.DEBUG, "Filesystem at mountpoint '"+fs.MountPoint+"' was originally mounted as '"+fs.OriginalMountPoint+"'"));
							break;;
						}
					}
				}
			}
			
			return layout;
		}

		/// <summary>
		/// Gets the sysfs partitions.
		/// </summary>
		/// <returns>
		/// The sysfs partitions.
		/// </returns>
		/// <param name='diskPath'>
		/// Disk path. (eg /sys/devices/virtual/block/loop0)
		/// </param>
		/// <param name='diskName'>
		/// Disk name. (eg loop0)
		/// </param>
		internal List<Partition> GetSysfsPartitions(string diskPath, string diskName){

			List<Partition> parts = new List<Partition>();

			Mono.Unix.UnixDirectoryInfo diskDir = new Mono.Unix.UnixDirectoryInfo(diskPath);
			foreach(Mono.Unix.Native.Dirent part in diskDir.GetEntries(diskName+".*")){
				Partition p = new Partition();
				p.Path = "/dev/"+part.d_name;
				p.Offset = ulong.Parse(File.ReadAllText(diskPath+"/"+part.d_name+"/start"));
				p.Size = long.Parse(File.ReadAllText(diskPath+"/"+part.d_name+"/size"));
				parts.Add(p);
			}
			return parts;
		}

		private List<FileSystem> GetMountedFilesystems(){
			List<FileSystem> fses = new List<FileSystem>();
			fses.AddRange(FilesystemManager.Instance().GetAllDrives());
			return fses;
		}

		private FileSystem MountLoopFilesystem(Partition partition){
				
			string mountPoint = PrepareMountPoint(partition.Path);
			foreach(string fsFormat in fileSystemFormats){
				Logger.Append(Severity.DEBUG, "Trying to mount '"+partition.Path+"' to '"+mountPoint+"' as "+fsFormat);
				bool mountOk = false;
				try{
					mountOk = lldh.Mount(partition.Path, mountPoint, fsFormat, LinuxLoopDeviceHelper.MountFlags.MS_RDONLY, "norecovery");
				}
				catch(Exception e){
					Logger.Append (Severity.DEBUG, "Could not mount using fs '"+fsFormat+"'");
				}
				if(mountOk){
					Logger.Append(Severity.DEBUG, "Mounted '"+partition.Path+"' to '"+mountPoint+"' as "+fsFormat);

					FileSystem fs = new FileSystem();
					fs.Path = partition.Path;
					fs.DriveFormat = fsFormat;
					fs.MountPoint = mountPoint;
					System.Threading.Thread.Sleep(200);

					return fs;
				}
			}
			return null;
		}

		// for loop devices backups, try to get the etc/fstab to get original node's mountpoints
		private Dictionary<string, string> TryGuessOriginalMountPoint(List<FileSystem> fsToParse){
			//  <partition id or path, originalmountpoint> tuples
			Dictionary<string, string> mountpoints = new Dictionary<string, string>();
			foreach(FileSystem f in fsToParse){
			//Mono.Unix.UnixFileInfo ufi = new Mono.Unix.UnixFileInfo(
				string fstabPath = f.MountPoint+"/etc/fstab";
				if(File.Exists(fstabPath)){
					foreach(string line in File.ReadLines(fstabPath)){
						string cleanLine = line.TrimStart(new char[]{' '});
						if(cleanLine.StartsWith("#")) continue;
						string[] values = cleanLine.Split(new char[]{' '});
						if(string.IsNullOrEmpty(values[1])) continue; // don't add things such as /proc, floppy drive...
						mountpoints.Add(values[0].Replace("UUID=",""), values[1]); 
						if(LogEvent != null)  LogEvent(this, new LogEventArgs(0, Severity.TRIVIA, "FSTAB parsing : added dev="+values[0].Replace("UUID=","")+",mnt="+ values[1]));
					}
				}
			}
			return mountpoints;
		}

		// create temp directories to mount loop devices partitions
		private string PrepareMountPoint(string partPath){
			string mountDir = tempMountsPath+"/"+partPath.Replace("/","_");
			if(!Directory.Exists(mountDir))
				Directory.CreateDirectory(mountDir);
			return mountDir;
		}

		/// <summary>
		/// returns the partition name (sdX format) from any by-xxxx value (by-path, by-uuid...)
		/// </summary>
		/// <param name='partitionName'>
		/// Partition name.
		/// </param>/
		private string GetPartitionFromPathOrUuid(string partitionName){
			string[] disksBy = new string[]{"by-uuid", "by-path", "by-label", "by-id"};
			foreach(string byType in disksBy){
				Mono.Unix.UnixDirectoryInfo byUuidParts = new Mono.Unix.UnixDirectoryInfo("/dev/disk/"+byType);
				foreach(Mono.Unix.Native.Dirent d in byUuidParts.GetEntries()){
					//Console.WriteLine ("______GetPartitionFromPathOrUuid looking cur entry="+d.d_name+", type="+d.d_type);
					if(d.d_type == 10/*Mono.Unix.FileTypes.SymbolicLink*/){
						string partOnlyName = "";
						if(partitionName.LastIndexOf("/") >=0)
							partOnlyName = partitionName.Substring(partitionName.LastIndexOf("/")+1);
						else if(partitionName.IndexOf("UUID=") ==0)
							partOnlyName = partitionName.Replace("UUID=","");
						else
							partOnlyName = partitionName;
						//Console.WriteLine ("______GetPartitionFromPathOrUuid :d_name="+d.d_name+", partonlyname="+partOnlyName);
						if(d.d_name == partOnlyName){
							Mono.Unix.UnixSymbolicLinkInfo devLink = new Mono.Unix.UnixSymbolicLinkInfo(byUuidParts+"/"+d.d_name);
							//Console.WriteLine ("______GetPartitionFromPathOrUuid : "+partitionName+" --> /dev/"+devLink.ContentsPath.Substring(devLink.ContentsPath.LastIndexOf('/')+1));
							return "/dev/"+devLink.ContentsPath.Substring(devLink.ContentsPath.LastIndexOf('/')+1);
						}
					}
				}
			}
			// partition was probably referenced by its devics (/dev/sdXX) path, nothing to do
			return partitionName;
		}
		/*private void LogReceived(int code, Severity severity, string message){
			if(this.LogEvent != null) LogEvent(this, new LogEventArgs(code, severity, message));
		}*/

		private void LogReceivedEvent(object sender, LogEventArgs args){
			Logger.Append(args.Severity, args.Message);
		}

		public void Dispose(){
			if(LogEvent != null) LogEvent(this, new LogEventArgs(0, Severity.TRIVIA, "Disposing LinuxStorageDiscoverer ressources...")); 
			if(loopDevices != null && loopDevices.Count > 0){
				LinuxLoopDeviceHelper lldh = new LinuxLoopDeviceHelper();
				foreach(FileSystem fs in this.layout.GetAllFileSystems(null)){
					if(!lldh.Umount(fs.MountPoint)){
						if(LogEvent != null) LogEvent(this, new LogEventArgs(0, Severity.NOTICE, "Couldn't umount FS '"+fs.MountPoint+"'"));
					}
					else
						if(LogEvent != null) LogEvent(this, new LogEventArgs(0, Severity.TRIVIA, "Unmounted '"+fs.MountPoint+"'"));

				}
				/*foreach(IDiskElement ide in this.layout.Entries)
					try{
						lldh.RemoveLoop(ide.Path);
					}
					catch(Exception e){
						LogEvent(this, new LogEventArgs(0, Severity.INFO, "Couldn't remore loop device '"+ide.Path+"' : "+e.Message));

					}*/
			}
		}

	}
}

#endif