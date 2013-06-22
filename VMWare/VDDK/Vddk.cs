using System;
using System.Runtime.InteropServices;
using System.Text;
//using VDDKWrappers;
using System.IO;
using System.Configuration;
using System.Collections.Generic;
using P2PBackup.Common;
using P2PBackup.Common.Volumes;

namespace VDDK{
	/* VDDK will work once the VDDK has been downloaded from VMware site
	 * and installed. On Windows, the VDK binaries' path (like C:\Program Files\VMware\VMware Virtual Disk Development Kit\bin)
	 * must be added to  the Path environment variable.
	 * 
	 * 
	 * */
	public class VDDK {
		//private static readonly VDDK _instance = new VDDK();
		static VixDiskLibConnectParams connParams;
		static IntPtr connPtr;
		string libdir;
		string libdirMnt;
		string configfile;
		VixDiskLibGenericLogFunc logI;
		VixDiskLibGenericLogFunc logW;
		VixDiskLibGenericLogFunc logC;
		VixDiskLibGenericLogFunc mntlogI;
		VixDiskLibGenericLogFunc mntlogW;
		VixDiskLibGenericLogFunc mntlogC;
		private Dictionary<string, IntPtr> openDisks; // <diskName, diskPtr>
		private Dictionary<string, IntPtr> diskSets; // on nt : <null, disksetptr>  on linux: <diskname,disksetptr>


		public delegate void LogHandler(int code, Severity severity, string message);
		public event EventHandler<LogEventArgs> LogEvent;

		static bool isUnixProxy;
		//IntPtr[] diskHandles;
		IntPtr[] volHandles;
		IntPtr diskSetPtr; 
		uint openFlag;

		public VDDK(bool runningOnUnix){
			isUnixProxy = runningOnUnix;
			logI = new VixDiskLibGenericLogFunc(LogI);
			logW = new VixDiskLibGenericLogFunc(LogW);
			logC = new VixDiskLibGenericLogFunc(LogC);

			mntlogI = new VixDiskLibGenericLogFunc(LogI);
			mntlogW = new VixDiskLibGenericLogFunc(LogW);
			mntlogC = new VixDiskLibGenericLogFunc(LogC);
			openDisks = new Dictionary<string, IntPtr>();
			diskSets = new Dictionary<string, IntPtr>();

			openFlag = 0;
			/*if(isUnixProxy)*/ openFlag = (uint)VixDiskLib.VIXDISKLIB_FLAG_OPEN_READ_ONLY;


		}


		public bool Connect(string url, string username, string password, string snapshotName, string virtualMachineMoRef, string wantedTransports){

			libdir = ConfigurationManager.AppSettings ["VDDK.LibDir"];
			configfile = ConfigurationManager.AppSettings["VDDK.ConfigFile"];
			if(string.IsNullOrEmpty(libdir)){
				if(isUnixProxy){
					libdir = "/usr/lib/vmware-vix-disklib/lib64";
					//libdirMnt = "/usr/lib/vmware-vix-disklib/";
				}
				else{
					if(IntPtr.Size == 8) // running on 64bits OS
						libdir = @"C:\Program Files (x86)\VMware\VMware Virtual Disk Development Kit";
					else
						libdir = @"C:\Program Files\VMware\VMware Virtual Disk Development Kit\bin";
					libdirMnt = libdir;
				}
			}
			if(string.IsNullOrEmpty(configfile))
				configfile = "./vddk.conf";
			LogEvent(this, new LogEventArgs(0, Severity.DEBUG, "Libdir="+libdir+", configfile="+configfile+", ptrSize="+IntPtr.Size));
			/*if(!isUnixProxy && IntPtr.Size == 8)
				SetDllDirectory(libdir+@"\vddk64");*/
			//VixError  error = VixDiskLib.InitEx(1, 2, logI, logW, logC, libdir, configfile);
			VixError  error = VixDiskLib.InitEx(1, 2, null, null, null, libdir, configfile);
			LogEvent(this, new LogEventArgs(0, Severity.DEBUG, "VixDiskLib_InitEx returned "+error.ToString()));

			// TODO!!!!! Ease vddk install : on unix, ln -s /usr/lib/libvixMntapi /node.exe_bin_dir
			// else this crap of vddk is unable to find it, as it doesn't seem
			// to handle the most basic unix notions about shared libraries

			//error = VixMntApi.Init(1,0, mntlogI, mntlogW, mntlogC, libdir, configfile);
			error = VixMntApi.Init(1,0, null, null, null, libdir, configfile);
			LogEvent(this, new LogEventArgs(0, Severity.DEBUG, "VixMntapi_Init returned "+error.ToString()));

			connParams = new VixDiskLibConnectParams();
			connParams.CredType = (uint)VixDiskLibCredType.VIXDISKLIB_CRED_UID;
			connParams.VixCredentials = new VixDiskLibCreds();
			connParams.VixCredentials.Uid = new VixDiskLibUidPasswdCreds();

			connParams.ServerName = url.Substring(0, url.LastIndexOf("/")).Replace("https://","").Replace("http://","").Replace ("/sdk", "");
			connParams.VixCredentials.Uid.UserName = username;
			connParams.VixCredentials.Uid.Password = password;
			connParams.Port = 902;
			connParams.VmxSpec = "moref="+virtualMachineMoRef; //"moref=2";
			//char[] backupName = "backup_task";
			//IntPtr identity = (IntPtr)Marshal.StringToHGlobalAnsi("backupTask9999999999999999999999999999999999999999");
			string taskName = "backup_task";
			error = VixDiskLib.PrepareForAccess(ref connParams, taskName); // crash when passing string under win64
			LogEvent(this, new LogEventArgs(0, Severity.DEBUG, "VixDiskLib_PrepareForAccess : "+error.ToString()));

			connPtr = Marshal.AllocHGlobal(8); // was 8
			error =  VixDiskLib.ConnectEx(ref connParams, (openFlag>0), snapshotName, wantedTransports, out connPtr);
			LogEvent(this, new LogEventArgs(0, Severity.DEBUG, "VixDiskLib_ConnectEx : "+error.ToString()));
			if(error != VixError.VIX_OK)
				throw new Exception(error.ToString());

			return true;
		}

		 

		/// <summary>
		/// For (Linux) VMs backuped from a Linux proxy, provides a Fuse device to be scanned and mounted
		/// </summary>
		/// <returns>
		/// The loop handle as a string representing the complete path to the Fuse VDDK mount
		/// </returns>
		/// <param name='diskName'>
		/// Disk name (VMware file path including datastore)
		/// </param>
		public string GetFuseHandle(string diskName){

			if(LogEvent != null) LogEvent(this, new LogEventArgs(700, Severity.INFO, "Mounting "+diskName+" as Fuse device 0%"));
			diskSetPtr = Marshal.AllocHGlobal(8);
			VixError error = VixError.VIX_OK;
			error = VixMntApi.OpenDisks(connPtr, new string[]{diskName}, 1, openFlag, ref diskSetPtr);
			if(LogEvent != null) LogEvent(this, new LogEventArgs(0, Severity.DEBUG, "VixMntApi_OpenDisks : "+error.ToString()+", intPtr null : "+(diskSetPtr == IntPtr.Zero)));
			if(error != VixError.VIX_OK){
				CleanupAndDisconnect();
				throw new Exception(error.ToString());
			}
			diskSets.Add(diskName, diskSetPtr);

			IntPtr  diskSetInfoPtr =  Marshal.AllocHGlobal(Marshal.SizeOf(typeof(VixDiskSetInfo)));
			error = VixMntApi.GetDiskSetInfo(diskSetPtr, ref diskSetInfoPtr);
			if(LogEvent != null) LogEvent(this, new LogEventArgs(0, Severity.DEBUG, "VixMntApi_GetDiskSetInfo : "+error.ToString()+", intPtr null : "+(diskSetPtr == IntPtr.Zero)));
			if(error != VixError.VIX_OK){
				CleanupAndDisconnect();
				throw new Exception(error.ToString());
			}

			VixDiskSetInfo diskSetInfo = (VixDiskSetInfo)Marshal.PtrToStructure(diskSetInfoPtr, typeof(VixDiskSetInfo));
			return diskSetInfo.MountPath;
		}

		/// <summary>
		/// Mount disks as loop devices, and return a maybet partial) StorageLayout
		/// Try to put the boot disk as first element, else inGuestMountPoints won't work,
		///  Ref : http://communities.vmware.com/thread/223740?start=0&tstart=0
		/// </summary>
		/// <param name='vmdks'>
		/// The Vmdks complete paths, under the form: "[Datastore_name] folder/disk_name.vmdk"
		/// </param>
		/// <param name='snapshotName'>
		/// The Snapshot MoRef.
		/// </param>
		/// <param name='virtualMachineMoRef'>
		/// Virtual machine MoRef.
		/// </param>
		/// <param name='wantedTransports'>
		/// transports mode by preference order, separated by semicolon, ex: san:hotadd:nbd:nbdssl
		/// Note that san is unavailable under a Linux node when the VM is running, 
		/// and hotadd availability is subject to special licensing.
		/// </param>
		public List<FileSystem> MountNTDrives(List<string> vmdks){
			if(isUnixProxy)
				if(LogEvent != null) LogEvent(this, new LogEventArgs(0, Severity.WARNING, "We don't support using VDDK mount ops on Linux!"));
			List<FileSystem> disksFses = new List<FileSystem>();
			// try to put the boot disk as first element, else inGuestMountPoints won't work,
			// ref : http://communities.vmware.com/thread/223740?start=0&tstart=0

			if(LogEvent != null) LogEvent(this, new LogEventArgs(700, Severity.INFO, "Opening and mounting "+vmdks.Count+" disk(s) 0%"));
			IntPtr diskSetPtr = Marshal.AllocHGlobal(8);

			VixError error = VixError.VIX_OK;
			IntPtr[] diskHandles = new IntPtr[openDisks.Count];
			openDisks.Values.CopyTo(diskHandles, 0);
			/*int diskPos = 0;
			foreach(IntPtr diskPtr in openDisks.Values){
				diskHandles[diskPos] = diskPtr;
				diskPos++;
			}*/
			error = VixMntApi.OpenDiskSet(diskHandles, (uint)openDisks.Count, openFlag/*(uint)VixDiskLib.VIXDISKLIB_FLAG_OPEN_READ_ONLY*/, out diskSetPtr); 
			diskSets.Add("",diskSetPtr);
			LogEvent(this, new LogEventArgs(0, Severity.DEBUG, "VixMntapi_OpenDiskSet: "+error.ToString()+", intPtr null : "+(diskSetPtr == IntPtr.Zero)));
			if(error != VixError.VIX_OK){
				Console.ReadLine();
				CleanupAndDisconnect();
				throw new Exception(error.ToString());
			}
			if(LogEvent != null) LogEvent(this, new LogEventArgs(700, Severity.INFO, "Opening and mounting "+vmdks.Count+" disk(s) 10%"));
			/*
			IntPtr  diskSetInfoPtr =  Marshal.AllocHGlobal(Marshal.SizeOf(typeof(VixDiskSetInfo)));
			error = VixMntApi.GetDiskSetInfo(diskSetPtr, ref diskSetInfoPtr);
			if(LogEvent != null) LogEvent(this, new LogEventArgs(0, Severity.DEBUG, "VixMntApi_GetDiskSetInfo : "+error.ToString()+", intPtr null : "+(diskSetPtr == IntPtr.Zero)));
			if(error != VixError.VIX_OK){
				CleanupAndDisconnect();
				throw new Exception(error.ToString());
			}
			VixDiskSetInfo diskSetInfo = (VixDiskSetInfo)Marshal.PtrToStructure(diskSetInfoPtr, typeof(VixDiskSetInfo));
			*/
			if(LogEvent != null) LogEvent(this, new LogEventArgs(700, Severity.INFO, "Opening and mounting "+vmdks.Count+" disk(s) 20%"));



			// we don't call GetOsInfo as 
			//1- only works on Win
			//2- involves very slooow operations such as mounting the volumes, scanning for registry, mounting registry, dismounting...
			/*IntPtr vixOsInfoPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(VixOsInfo)));
			error = VixMntApi.GetOsInfo(diskSetPtr, ref vixOsInfoPtr);
			LogEvent(this, new LogEventArgs(0, Severity.DEBUG, "GetOsInfo : "+error.ToString()));
			if(error == VixError.VIX_OK){
				VixOsInfo osInfo = (VixOsInfo)Marshal.PtrToStructure(vixOsInfoPtr, typeof(VixOsInfo));
				LogEvent(this, new LogEventArgs(0, Severity.DEBUG, "OS info : family="+osInfo.Family+ ", Version = "+osInfo.MajorVersion+"."+osInfo.MinorVersion));
			}*/

			int nbVol = 16;
			IntPtr[] volumeHandles = new IntPtr[nbVol];
			int arraySize = volumeHandles.Length;
			IntPtr buffer = Marshal.AllocHGlobal(Marshal.SizeOf(arraySize) * arraySize);
			Marshal.Copy(volumeHandles, 0, buffer, arraySize);
			long longNbVol = 0;
			error = VixMntApi.GetVolumeHandles(diskSetPtr, ref longNbVol, ref buffer); 
			if(LogEvent != null) LogEvent(this, new LogEventArgs(0, Severity.DEBUG, "VixMntapi_GetVolumeHandles : "+error.ToString()+", intPtr null : "+(connPtr == IntPtr.Zero)));
			//if(error != VixError.VIX_OK) return;
			if(LogEvent != null) LogEvent(this, new LogEventArgs(0, Severity.INFO, "VixMntapi_GetVolumeHandles : Got "+longNbVol+" Volumes inside disk"));
			if(LogEvent != null) LogEvent(this, new LogEventArgs(700, Severity.INFO, "Opening and mounting "+vmdks.Count+" disk(s) 30%"));

			volHandles = new IntPtr[(int)longNbVol];
			for(int i=0; i< (int)longNbVol; i++)
				volHandles[i] = IntPtr.Zero;
			Marshal.Copy(buffer, volHandles, 0, (int)longNbVol);
			int percentDone = 30;
			int percentGap = (int)Math.Round((decimal)1/volHandles.Length*70, 0);
			foreach(IntPtr volHandle in volHandles){
				if( volHandle != IntPtr.Zero){

					error = VixMntApi.MountVolume(volHandle, (openFlag>0));
					if(LogEvent != null) LogEvent(this, new LogEventArgs(0, Severity.DEBUG, "VixMntapi_MountVolume : "+error.ToString()+", intPtr null : "+(connPtr == IntPtr.Zero)));

					IntPtr volMountInfoPtr =  Marshal.AllocHGlobal(Marshal.SizeOf(typeof(VixVolumeInfo)));
					error = VixMntApi.GetVolumeInfo(volHandle, ref volMountInfoPtr);
					if(LogEvent != null) LogEvent(this, new LogEventArgs(0, Severity.DEBUG, "GetVolumeInfo : "+error.ToString()));
					//Partition part = new Partition();
					if(error == VixError.VIX_OK){
						VixVolumeInfo volInfo = (VixVolumeInfo)Marshal.PtrToStructure(volMountInfoPtr, typeof(VixVolumeInfo));
						Console.WriteLine ("\t Mounted : "+volInfo.IsMounted);
						Console.WriteLine ("\t Symlink : "+volInfo.SymbolicLink);
						//part.Path = volInfo.SymbolicLink;
						//part.Schema = PartitionSchemes.Classical;
						FileSystem fs = new FileSystem();
						fs.DriveFormat = "ntfs";
						//fs.Path = volInfo.SymbolicLink;
						fs.MountPoint = volInfo.SymbolicLink;
						//part.Children.Add(fs);
						Console.WriteLine ("\t Type : "+volInfo.Type.ToString());
						Console.WriteLine ("\t NB Mountpoints : "+volInfo.NumGuestMountPoints);
						for (int i = 0; i < volInfo.NumGuestMountPoints; i++){
							IntPtr p = new IntPtr(volInfo.InGuestMountPoints.ToInt32() + sizeof(uint)*i);
							string s = Marshal.PtrToStringAnsi(p);
							Console.WriteLine ("\t Mounted as "+s);
							fs.OriginalMountPoint =  s;

						}
						disksFses.Add(fs);
					}

				}
				percentDone += percentGap;
				if(LogEvent != null) LogEvent(this, new LogEventArgs(700, Severity.INFO, "Opening and mounting "+vmdks.Count+" disk(s) "+percentDone+"%"));
			}
			if(LogEvent != null) LogEvent(this, new LogEventArgs(700, Severity.INFO, "Opening and mounting "+vmdks.Count+" disk(s) 100%"));
			return disksFses;
		}


		//windows only
		public void CloseNtDrivesAndLoops(){
			VixError error = VixError.VIX_OK;
			if(volHandles != null){
				foreach(IntPtr volHandle in volHandles){
					if(volHandle != IntPtr.Zero){
						error = VixMntApi.VixMntapi_DismountVolume(volHandle, true);
						if(LogEvent != null) LogEvent(this, new LogEventArgs(0, Severity.DEBUG, "VixMntapi_DismountVolume : "+error.ToString()+", intPtr null : "+(connPtr == IntPtr.Zero)));
					}
				}
			}
			CloseLoops ();
			//if(diskSetPtr != IntPtr.Zero)
			//	error = VixMntApi.CloseDiskSet(diskSetPtr);
			//VixMntApi.Exit();
			//VixDiskLib.Exit();


		}

		private void CloseLoops(){
			VixError error = VixError.VIX_OK;
			foreach(KeyValuePair<string, IntPtr> kp in diskSets){
				error = VixMntApi.CloseDiskSet(kp.Value);
				if(LogEvent != null) LogEvent(this, new LogEventArgs(0, Severity.DEBUG, "VixMntapi_CloseDiskSet ("+kp.Key+") : "+error.ToString()+", intPtr null : "+(connPtr == IntPtr.Zero)));
				System.Threading.Thread.Sleep(500);
			}

		}

		public void CleanupAndDisconnect(){
			VixError error = VixDiskLib.EndAccess(ref connParams, "backup_task");
			LogEvent(this, new LogEventArgs(0, Severity.DEBUG, "VixDiskLib_EndAccess : "+error.ToString()));

			uint nbCleanedUp = 0;
			uint nbRemaining = 0;
			error = VixDiskLib.Cleanup(ref connParams, ref nbCleanedUp, ref nbRemaining);
			LogEvent(this, new LogEventArgs(0, Severity.DEBUG, "VixDiskLib_Cleanup : "+error.ToString()+", cleaned up "+nbCleanedUp+", remaining="+nbRemaining));

			error = VixDiskLib.Disconnect(connPtr);
			LogEvent(this, new LogEventArgs(0, Severity.DEBUG, "VixDiskLib_Disconnect : "+error.ToString()+", intPtr null : "+(connPtr == IntPtr.Zero)));
			VixMntApi.Exit();
			VixDiskLib.Exit();

		}
	

		public /*byte[]*/IntPtr OpenPhysicalDisk(string diskName){

			VixError error = VixError.VIX_OK;

			IntPtr diskPtr = Marshal.AllocHGlobal(8); //IntPtr.Zero;
			error = VixDiskLib.Open(connPtr, 
				diskName, 
			    openFlag, out diskPtr);

			LogEvent(this, new LogEventArgs(0, Severity.TRIVIA, "VixDiskLib_Open : "+error.ToString()+", intPtr null : "+(connPtr == IntPtr.Zero)));
			if(error != VixError.VIX_OK){
				LogEvent(this, new LogEventArgs(0, Severity.ERROR, "Could not open remote disk '"+diskName+"': "+error.ToString()));
				//Console.WriteLine (" ### ERROR : "+VixDiskLib.GetErrorText((ulong)error, null));
				//CleanupAndDisconnect();
				return IntPtr.Zero;
			}
			openDisks.Add(diskName, diskPtr);
			IntPtr transport = VixDiskLib.GetTransportMode(diskPtr);
			LogEvent(this, new LogEventArgs(0, Severity.INFO,  ("Transport mode for '"+diskName+"':"+Marshal.PtrToStringAnsi(transport))));

			/*uint nbSectToRead = 2;
			byte[] dataBuf = new byte[512*nbSectToRead];
			error = VixDiskLib.Read(diskPtr, 0, nbSectToRead, dataBuf);
			LogEvent(this, new LogEventArgs(0, Severity.DEBUG, "VixDiskLib_Read : "+error.ToString()));

			return dataBuf;*/


			return diskPtr;



			uint length = 129;
			IntPtr buf = IntPtr.Zero;
			//error = VixDiskLib.VixDiskLib_GetMetadataKeys(diskPtr, ref buf, 1, ref length);
			//Console.WriteLine ("  ##### Result 5 (VixDiskLib_GetMetadataKeys) : "+error.ToString()+", keys length="+length);
			//char[] realBuf = new char[length];
			uint dummy = 0;
			IntPtr buf2 =  Marshal.AllocHGlobal((int)length);
			//string keys = Marshal.PtrToStringAuto(keysBuf);
			error = VixDiskLib.GetMetadataKeys(diskPtr, ref buf2, length*8, ref dummy);
			Console.WriteLine ("  ##### Result 5 (VixDiskLib_GetMetadataKeys) : "+error.ToString()+", keys length="+length);
			Console.WriteLine ("lulute");
			Console.WriteLine ("Keys="+Marshal.PtrToStringAnsi(buf2));
			//char[] test = new char[length*2]
			//Marshal.PtrToStringAuto
			/*Console.WriteLine ("---- dumping metadata keys : -------");
			for(int i=0; i<length; i++)
				Console.Write*/

			/*FileStream vmdkfs  = new FileStream("test-backup-vmdk.vmdk", FileMode.CreateNew);
			ulong readSectors = 0;
			while (readSectors < 2097152){

				ulong nbSectToRead = 1000;
				if((2097152-readSectors) < 1000)
					nbSectToRead = 2097152-readSectors;
				byte[] dataBuf = new byte[512*nbSectToRead];
				error = VixDiskLib.VixDiskLib_Read(diskPtr, readSectors, nbSectToRead, dataBuf);
				vmdkfs.Write(dataBuf, 0, (int)nbSectToRead*512);
				readSectors += 1000;
			}*/



			//VixDiskLib.VixDiskLib_Disconnect(connPtr);
			return IntPtr.Zero;
		}


		public void CloseDisk(string diskName){

			VixError error = VixDiskLib.Close(openDisks[diskName]);
			LogEvent(this, new LogEventArgs(0, Severity.DEBUG, "VixDiskLib_Close : "+error.ToString()+" (disk '"+diskName+"'"));
		}

		public void CloseDisks(){
			foreach(KeyValuePair<string, IntPtr> kp in openDisks){
				VixError error = VixDiskLib.Close(kp.Value);
				LogEvent(this, new LogEventArgs(0, Severity.DEBUG, "VixDiskLib_Close : "+error.ToString()+" (disk '"+kp.Key+"'"));
		
			}
			openDisks.Clear();
		}

		public static void Main_old(string[] args){

			Console.WriteLine ("Init 1");
			//Native.VixDiskLibGenericLogFunc infoNull = new Native.VixDiskLibGenericLogFunc();
			//Native.VixDiskLibGenericLogFunc warnNull = new Native.VixDiskLibGenericLogFunc();
			//Native.VixDiskLibGenericLogFunc critNul = new Native.VixDiskLibGenericLogFunc();
			/*VixDiskLibGenericLogFunc logI = new VixDiskLib.VixDiskLibGenericLogFunc(LogI);
			VixDiskLibGenericLogFunc logW = new VixDiskLib.VixDiskLibGenericLogFunc(LogW);
			VixDiskLibGenericLogFunc logC = new VixDiskLib.VixDiskLibGenericLogFunc(LogC);*/
			VixDiskLib.InitEx(1, 2, null, null, null, @"/usr/lib/vmware", @"/home/matt/dev/SharpBackup/VDDK-Wrappers/vddk.conf");

			VixDiskLibConnectParams connParams = new VixDiskLibConnectParams();
			connParams.CredType = (uint)VixDiskLibCredType.VIXDISKLIB_CRED_UID;
			connParams.VixCredentials = new VixDiskLibCreds();
			connParams.VixCredentials.Uid = new VixDiskLibUidPasswdCreds();


			string diskUrl = @"WH0719v2/WH0719v2.vmx?dcPath=Datacenter&dsName=SAN_MSA2012FC_L19 (BackUp-Monitoring ENG)";
			byte[] utf8Bytes = UTF8Encoding.UTF8.GetBytes(diskUrl);
			Encoding ANSI = Encoding.GetEncoding(1252);
			byte[] ansiBytes = Encoding.Convert(Encoding.UTF8, ANSI, utf8Bytes);
			String ansiString = ANSI.GetString(ansiBytes);


			//connParams.ServerName = "172.29.9.50";
			//connParams.VixCredentials.Uid.UserName = "admin";
			//connParams.VixCredentials.Uid.Password = "PSn1+Elx";
			//connParams.VmxSpec = diskUrl;
			//connParams.VmxSpec =  ansiString;


			connParams.ServerName = "192.168.0.27";
			connParams.VixCredentials.Uid.UserName = "root";
			connParams.VixCredentials.Uid.Password = "totototo";
			//connParams.VmxSpec = "vm1/vm1.vmx?dcPath=&dsName=[local_1]";

			connParams.Port = 902;
			IntPtr connPtr;
			connPtr =  IntPtr.Zero;
			VixError error =  VixDiskLib.Connect(ref connParams, out connPtr);
			//VixError error =  VixDiskLib.VixDiskLib_ConnectEx(ref connParams, true, "", "nbd:file:san:hotadd:nbdssl", out connPtr);
			//string transport = VixDiskLib.GetTransportMode(connPtr);
			//Console.WriteLine ("transport :"+transport);
			Console.WriteLine ("  ##### Result 3 (Connect) : "+error.ToString()+", intPtr null : "+(connPtr == IntPtr.Zero));

			IntPtr diskPtr = IntPtr.Zero;

			//win
			/*error = VixDiskLib.VixDiskLib_Open(connPtr, 
				@"[SAN_MSA2012FC_L19 (BackUp-Monitoring ENG)] WH0719v2/WH0719v2.vmdk", 
			    (uint)VixDiskLib.VIXDISKLIB_FLAG_OPEN_READ_ONLY, out diskPtr);*/


			//ux
			error = VixDiskLib.Open(connPtr, 
				@"[local_1] vm1/vm1.vmdk", 
			    (uint)VixDiskLib.VIXDISKLIB_FLAG_OPEN_READ_ONLY, out diskPtr);





			/*error = Native.VixDiskLib_Open(connPtr, 
				"lute.vmdk", 
			    (uint)Native.VIXDISKLIB_FLAG_OPEN_READ_ONLY, out diskPtr);
			Console.WriteLine ("Result 4 : "+error.ToString()+", disk inptr null : "+(diskPtr == IntPtr.Zero));*/
			/*Console.WriteLine (infoNull.Fmt+" , "+infoNull.Va_list);
			Console.WriteLine (warnNull.Fmt+" , "+warnNull.Va_list);
			Console.WriteLine (critNul.Fmt+" , "+critNul.Va_list);*/

			Console.WriteLine ("  ##### Result 4 (Open) : "+error.ToString()+", intPtr null : "+(connPtr == IntPtr.Zero));



			/// vmdk copy !works!
			/*FileStream vmdkfs  = new FileStream("test-backup-vldk.vmdk", FileMode.CreateNew);
			ulong readSectors = 0;
			while (readSectors < 2097152){

				ulong nbSectToRead = 1000;
				if((2097152-readSectors) < 1000)
					nbSectToRead = 2097152-readSectors;
				byte[] dataBuf = new byte[512*nbSectToRead];
				error = VixDiskLib.VixDiskLib_Read(diskPtr, readSectors, nbSectToRead, dataBuf);
				vmdkfs.Write(dataBuf, 0, (int)nbSectToRead*512);
				readSectors += 1000;
			}*/
			//Console.WriteLine ("Result 4 (Read) : "+error.ToString()+", inptr null : "+(connPtr == IntPtr.Zero));


			IntPtr diskInfoPtr = IntPtr.Zero;
			error = VixDiskLib.GetInfo(diskPtr, ref diskInfoPtr);
			VixDiskLibInfo di = new VixDiskLibInfo();
			Marshal.PtrToStructure(diskInfoPtr, di);
			//di = (VixDiskLibInfo)diskInfoPtr;

			Console.WriteLine ("*****"+di.BiosGeo.Cylinders+" CYL, "+di.BiosGeo.Sectors+" sec, bios cyl="+di.PhysGeo.Cylinders+", bios sec="+di.PhysGeo.Sectors+", nb sect="+di.Capacity);
			Console.WriteLine("physical disk size = "+di.Capacity*di.PhysGeo.Cylinders/1024/1024+" MB");
			       //VixDiskLib.VixDiskLib_ReadMetadata(
			Console.WriteLine ("Result  (VixDiskLib_GetInfo) : "+error.ToString()+", inptr null : "+(connPtr == IntPtr.Zero));


			error = VixMntApi.Init(1,0, null, null, null,
			    @"C:\Program Files\VMware\VMware Virtual Disk Development Kit", 
				@"E:\QHMP5573\Data\p\shb2\Vddk-wrappers\VDDK-Wrappers\bin\Debug\");

			Console.WriteLine ("  ##### Result 5 (VixMntapi_Init) : "+error.ToString()+", intPtr null : "+(connPtr == IntPtr.Zero));
			string[] diskNames = new string[1];
			diskNames[0] = "test"; //"WH0719v2.vmdk"; //transport; 
			uint numVolumes = 1;
			IntPtr diskSetHandle = IntPtr.Zero;
			//VixMntApi.
			//error = VixMntApi.VixMntapi_OpenDisks(connPtr, diskNames, numVolumes, 0, out diskSetHandle);
			error = VixMntApi.MountVolume(diskPtr, true);
			Console.WriteLine ("  ##### Result 6 (VixMntapi_MountVolume) : "+error.ToString()+", intPtr null : "+(connPtr == IntPtr.Zero));
			if(error == VixError.VIX_E_NOT_SUPPORTED) // disk is not readable : not formatted?
				Console.WriteLine ("  ##### Disk not mountable : "+error.ToString());




			VixDiskLib.Disconnect(connPtr);

		}

		private  void LogM(Severity severity, IntPtr messagePtr, IntPtr args){
			string message = Marshal.PtrToStringAnsi(messagePtr);
			int size = message.Length;
			// Uneble for now to find a way to retrieve va_args under mono.
			if(!isUnixProxy){
				IntPtr[] array = new IntPtr[size];
				if(args!= null && args != IntPtr.Zero && array.Length > 0){
					Marshal.Copy(args, array, 0, size);

					for (int i = 0 ; i< size; i++){
						int placeHolderPos = message.IndexOf("%");
						if(placeHolderPos <0 || array[i] == IntPtr.Zero) continue;
						string placeHolder = message.Substring(placeHolderPos, 2);
						int phLength = 2;
						string currentArg = "-";
						//try{
						switch(placeHolder){
						case "%s":
							//currentArg = Marshal.PtrToStringAnsi(array[i]);
							currentArg = Marshal.PtrToStringAnsi(array[i]);
							break;
						case "%d":
							currentArg = array[i].ToString();
							break;
						case "%#":
							// don't know how to read that, ignore
							currentArg = "<U:"+placeHolderPos+">";
							break;
						case "%I":
							if(message.Substring(placeHolderPos, 5) == "%I64u")
								currentArg = ((ulong)array[i]).ToString();
							else if(message.Substring(placeHolderPos, 5) == "%I64d")
								currentArg = ((long)array[i]).ToString();
							else
								currentArg = "<U:"+message.Substring(placeHolderPos, 4)+">"; 
							phLength = 4;
							break;
						case "%u":
							currentArg = ((uint)array[i]).ToString();
							break;
						case "%x":
							currentArg = Marshal.PtrToStringAnsi(array[i]);
							break;
						default:
							//currentArg = array[i].ToString();
							currentArg = "<U:"+placeHolder+">";
							break;
						}
						//}
						//catch(Exception e){// mono won't like va_args, so unlikely to be able to parse them

						//}
						if(message != null && placeHolderPos >=0 && currentArg != null)
							message = message.Substring(0, placeHolderPos)
								+currentArg.ToString()
								+message.Substring(placeHolderPos+phLength);

					}
				}
		  	}
			//Console.WriteLine (severity.ToString()+" <VDDK> : "+message.Replace(Environment.NewLine, string.Empty));
			if (LogEvent != null) LogEvent(this, new LogEventArgs(0, severity, message));

		}

		private  void LogI(IntPtr message, IntPtr args){
			LogM(Severity.DEBUG, message, args);
		}

		private  void LogW(IntPtr message, IntPtr args){
			LogM(Severity.INFO, message, args);
		}

		private  void LogC(IntPtr message, IntPtr args){
			LogM(Severity.ERROR, message, args);
		}

		[DllImport("kernel32.dll", CharSet=CharSet.Auto)]
		private static extern void SetDllDirectory(string lpPathName);


		/*private static void mntLogI(string message, IntPtr args){
			LogM(Severity.INFO, message, args);
		}

		private static void mntLogW(string message, IntPtr args){
			LogM(Severity.WARNING, message, args);
		}

		private static void mntLogC(string message, IntPtr args){
			LogM(Severity.ERROR, message,  args);
		}*/

	}
}

