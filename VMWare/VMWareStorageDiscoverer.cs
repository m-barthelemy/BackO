using System;
using System.Collections.Generic;
using P2PBackup.Common;
using P2PBackup.Common.Volumes;
using P2PBackup.Common.Virtualization;
using VDDK;

namespace VMWare {

	public class VMWareDisksDiscoverer:  IStorageDiscoverer{

		public string Name{get{ return "vmware";}}
		public string Version{get{return "0.1";}}
		public bool IsProxyingPlugin{get{return true;}}

		public delegate void LogHandler(int code, Severity severity, string message);
		public event EventHandler<LogEventArgs> LogEvent;

		private VMWareHandler vmwh;
		private VDDK.VDDK vddk;
		private ProxyTaskInfo proxyInfo;
		private string snapName;

		public VMWareDisksDiscoverer(){}

		public bool Initialize(ProxyTaskInfo ptI){

			if(ptI.Node == null || ptI.Hypervisor == null) return false;
			proxyInfo = ptI;
			vmwh = new VMWareHandler();
			vmwh.LogEvent += new EventHandler<LogEventArgs>(this.LogReceivedEvent);

			vddk = new VDDK.VDDK(Environment.OSVersion.Platform == PlatformID.Unix);
			vddk.LogEvent += new EventHandler<LogEventArgs>(this.LogReceivedEvent); //new VDDK.LogHandler(this.LogReceived);
			// Force log to be suscribed to
			if(this.LogEvent == null)
				throw new Exception("Must suscribe to LogEvent");
			return true;
		}


		/// <summary>
		/// Snapshots the VM and gets its (maybe partial) StorageLayout
		/// </summary>
		/// <returns>
		/// The physical disks.
		/// </returns> retrieve a 
		public StorageLayout BuildStorageLayout(){


			LogEvent(this, new LogEventArgs(700, Severity.INFO, "Connecting to hypervisor '"+proxyInfo.Hypervisor.Name+"' ("+proxyInfo.Hypervisor.Url+")"));
			vmwh.Connect(proxyInfo.Hypervisor.Url, proxyInfo.Hypervisor.UserName, proxyInfo.Hypervisor.Password.Value);
			if(vmwh == null) throw new Exception ("IDiskDiscoverer '"+this.Name+"' hasn't been initialized");

			List<Disk> vmDks = vmwh.GetDisks(proxyInfo.Node);
			if(vmDks.Count == 0)
				return null;
			//if(vmDks.Count == 0) throw new Exception ("Storage discoverer '"+this.Name+"' didn't find any disk.");

			//Logger.Append(Severity.DEBUG, "Found "+vmDks.Count+" disks for Node #"+proxyInfo.Node.Id+" (VM "+proxyInfo.Node.InternalId+")");
			LogEvent(this, new LogEventArgs(0, Severity.DEBUG, "Found "+vmDks.Count+" disks for Node #"+proxyInfo.Node.Id+" (VM "+proxyInfo.Node.InternalId+")"));

			// Now ask to snapshot 
			snapName = vmwh.Snapshot(proxyInfo.Node, "test"/*"snap_test_"+DateTime.Now.ToString()*/);

			vddk.Connect(proxyInfo.Hypervisor.Url, 
			             proxyInfo.Hypervisor.UserName, 
			             proxyInfo.Hypervisor.Password.Value,
			             snapName,
			             vmwh.GetVmMMorefId(proxyInfo.Node),
			             "san:nbd:hotadd:nbdssl"// make transport mode configurable
			             );

			StorageLayout sl = new StorageLayout();
			List<string> diskNames = new List<string>();

			// open disk and read MBR
			foreach(Disk d in vmDks){
				d.BlockStream = new VmdkStream(vddk.OpenPhysicalDisk(d.Path));
			
				//2013-05-29 - commented out to split vmware to separate disk discov plugin
				/*d.MbrBytes = new byte[512];
				d.BlockStream.Read(d.MbrBytes, 0, 512);
				MBR mbr = new MBR(d.MbrBytes);
				d.Signature = mbr.DiskSignature;*/


				diskNames.Add(d.Path);
				d.IsComplete = true;
				if(Environment.OSVersion.Platform == PlatformID.Unix){// on Linux, get a loop Device mountpoint for the disk
					d.Type = DiskType.Loop;
					try{
						d.ProxiedPath = vddk.GetFuseHandle(d.Path);
					}
					catch(Exception e){
						LogEvent(this, new LogEventArgs(10, Severity.ERROR, "Could not attach disk '"+d.Path+"' to loopback device : "+e.ToString()));
						//Logger.Append(Severity.ERROR, "Could not attach disk '"+d.Path+"' to loopback device : "+e.ToString());
					}
				}
				sl.Entries.Add(d);
			}

			// At this point we have disk-->partitions rudimentary mapping, won't go further on a linux VM
			if(Environment.OSVersion.Platform == PlatformID.Unix) return sl;

			//on windows, we can use vddk to mount them all Attribute once/
			// the read mounted registry hive to map partition <--->drive letter
			// we don't use vixmntapi mapping because it doesn't report partitions offset (thus making impossible to match
			// the disc raw MBR discovery with mounts)
			// Also, we can open all disks at one and benefit from the VDDK(s ability
			// to mount everything (NTFS-based) incuding Windows LDM volumes (spanned, raid...)
			List<FileSystem> disksFses = vddk.MountNTDrives(diskNames);
			List<Tuple<string, uint, ulong>> registryDrives = new List<Tuple<string, uint, ulong>>();
			foreach(FileSystem mountedFS in disksFses){
				using(NTSystemRegistry nsr = new NTSystemRegistry()){
					nsr.LogEvent += LogReceivedEvent;
					if(nsr.MountSystemHive(mountedFS.MountPoint, 0)){
						List<Tuple<string, uint, ulong>> drives = nsr.GetMountPoints();
						if(drives != null && drives.Count >0)
							registryDrives.AddRange(drives);
						LogEvent(this, new LogEventArgs(0, Severity.TRIVIA, "Currently have "+registryDrives.Count+" drives from registry"));
					}
				}
			}
			if(registryDrives == null){	
					LogEvent(this, new LogEventArgs(899, Severity.ERROR, "Couldn't retrieve mount information from NT registry. Drives count: "+registryDrives.Count));
					return sl;
			}

			foreach(FileSystem mountedFS in disksFses){
				//Console.WriteLine (Environment.NewLine+"@@@  FS2 '"+mountedFS.ToString());

					 // does not have registry, or not a "mountable" fs

					foreach(Tuple<string, uint, ulong> driveInfo in registryDrives){

						Console.WriteLine ("@@@  TUPLE '"+driveInfo.Item1+", sig="+driveInfo.Item2+", offset="+driveInfo.Item3);
						if(mountedFS.OriginalMountPoint == null ||
					   		(driveInfo.Item1.ToLower() != mountedFS.OriginalMountPoint.ToLower())
						)
							continue;
						bool foundFsDisk = false;
						foreach(Disk d in sl.Entries){
							Console.WriteLine ("@@@  DISK '"+d.ToString());
							if (d.Signature != driveInfo.Item2)
								continue;
							// add new partition having the offset discovered by registry tuple
							Console.WriteLine ("@@@  PART  "+driveInfo.Item3);
							Partition partialPart = new Partition();
							partialPart.Offset = driveInfo.Item3;

							partialPart.AddChild(mountedFS);
							d.AddChild(partialPart);

							Console.WriteLine ("@@@     ADDED   new FS '"+mountedFS.MountPoint+"' to part "+partialPart.ToString());
							foundFsDisk = true;

						} // end foreach Disk

						// If we couldn't find a matching disk & part for the FS, still add it to the layout root.
						// This allows to backup FS items even if we know nothing about its backing device(s) layout.
						// thus BMR won't be possible
						/*if(!foundFsDisk)
							sl.Entries.Add(mountedFS);*/

					} //end foreach registry drive

				//}// end using
				}

			return sl;
		}

		/*void HandleLogEvent (object sender, LogEventArgs e)
		{
			//nsr.LogEvent += HandleLogEvent;
			LogEvent(sender, e);
		}*/


		/// <summary>
		/// Given a complete vmdk path (with datastore and snapshot (-000x), returns the disk path
		/// as 'vmfolder/vmdisk.vmdk'
		/// </summary>
		/// <returns>
		/// The disk path.
		/// </returns>
		/// <param name='rawPath'>
		/// Raw path.
		/// </param>
		public string GetDiskPath(string rawPath){
			Console.WriteLine ("  ********** GetDiskPath () 1  rawpath= :"+rawPath);
			// rawpaths won't be handlable if something went wrng with mounting, so return original value
			if(rawPath.LastIndexOf("] ") <=0) return rawPath;

			string path = rawPath = rawPath.Substring(rawPath.LastIndexOf("] ")+2);
			if(path.LastIndexOf("-") >0)// sometimes vSphere returns a snap path instead of the disk path (vSphere 5?)
				path = path.Substring(0, path.LastIndexOf("-"))+".vmdk";
			Console.WriteLine ("  ********** GetDiskPath () 3 :"+path);
			return path;
		}

		private void LogReceivedEvent(object sender, LogEventArgs args){
			if(LogEvent != null)
				LogEvent(sender, args);
		}

		public void Dispose(){
			if(LogEvent != null)
				LogEvent(this, new LogEventArgs(0, Severity.DEBUG, "VMWareStorageDiscoveer : Disposing ressources.."));
			if(vmwh == null) return;
			try{
				vddk.CloseNtDrivesAndLoops();
				//vddk.CloseDisk(d.Path);
				vddk.CloseDisks();
				vddk.CleanupAndDisconnect();
				vmwh.DeleteSnapshot(proxyInfo.Node, snapName);
				vmwh.Dispose();
			}
			catch{}
		}

	}
}

