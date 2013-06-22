#if OS_WIN
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32;
//using Node.Utilities;
using P2PBackup.Common;

namespace VMWare {

	public class NTSystemRegistry:IDisposable {

		public delegate void LogHandler(int code, Severity severity, string message);
		public event EventHandler<LogEventArgs> LogEvent;
		//public event EventHandler<LogEventArgs> LogEvent;

		public const int TOKEN_ADJUST_PRIVILEGES = 0x00000020;
		public const int TOKEN_QUERY = 0x00000008;
		public const int SE_PRIVILEGE_ENABLED = 0x00000002;
		public const string SE_RESTORE_NAME = "SeRestorePrivilege";
		public const string SE_BACKUP_NAME = "SeBackupPrivilege";
		public const uint HKEY_USERS = 0x80000003;
		public string shortname;
		bool unloaded = false;

		int nodeId;
		string hiveCopyFile;
		string hiveMountPath;

		public NTSystemRegistry ()
		{
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct LUID{
			public uint LowPart;
			public int HighPart;
		} 
		[StructLayout(LayoutKind.Sequential)]
		public struct TOKEN_PRIVILEGES{
			public LUID Luid;
			public int Attributes;
			public int PrivilegeCount;
		}

		/*public struct TOKEN_PRIVILEGES {
      public UInt32 PrivilegeCount;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
      public LUID_AND_ATTRIBUTES[] Privileges;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LUID_AND_ATTRIBUTES {
      public LUID Luid;
      public UInt32 Attributes;
    }*/

		[DllImport("advapi32.dll", CharSet=CharSet.Auto)]
		public static extern int OpenProcessToken(int ProcessHandle, int DesiredAccess, 
		ref int tokenhandle);

		[DllImport("kernel32.dll", CharSet=CharSet.Auto)]
		public static extern int GetCurrentProcess();

		[DllImport("advapi32.dll", CharSet=CharSet.Auto)]
		public static extern int LookupPrivilegeValue(string lpsystemname, string lpname, 
			[MarshalAs(UnmanagedType.Struct)] ref LUID lpLuid);

		[DllImport("advapi32.dll", CharSet=CharSet.Auto)]
		public static extern int AdjustTokenPrivileges(int tokenhandle, int disableprivs, 
			[MarshalAs(UnmanagedType.Struct)]ref TOKEN_PRIVILEGES Newstate, int bufferlength, 
			int PreivousState, int Returnlength);

		[DllImport("advapi32.dll", CharSet=CharSet.Auto, SetLastError=true)]
		public static extern int RegLoadKey(uint hKey,string lpSubKey, string lpFile);

		[DllImport("advapi32.dll", CharSet=CharSet.Auto, SetLastError=true)]
		public static extern int RegUnLoadKey(uint hKey, string lpSubKey);



		internal bool MountSystemHive(string systemHivePath, int nodeId){
			this.nodeId = nodeId;
			Random r = new Random();
			int instanceId = r.Next(10000);
			hiveMountPath = "VDDK_Node_"+nodeId+"_"+instanceId;
			hiveCopyFile = "SystemHive_Node_"+nodeId+"_"+instanceId;

			int retval=0;
			int lastError = 0;

			//Utilities.PrivilegesManager pm = new Utilities.PrivilegesManager ();
			//pm.Grant();

			// Loading System hive
			string fullHivePath=systemHivePath+@"Windows\system32\config\SYSTEM";

			try{
				Alphaleonis.Win32.Filesystem.File.Copy(fullHivePath, hiveCopyFile);
			}
			catch(System.IO.DirectoryNotFoundException){
				//Console.WriteLine ("alphaleonis copy error : "+e.ToString());
				fullHivePath=systemHivePath+@"WINNT\system32\config\SYSTEM";
				try{
					Alphaleonis.Win32.Filesystem.File.Copy(fullHivePath, hiveCopyFile);

				}
				catch(Exception e){
					//Logger.Append(Severity.WARNING, "Error copying registry hive from node "+nodeId+ " :"+e.Message);
					//VMWareDisksDiscoverer.LogEvent(this, NewsStyleUriParser LogEventArgs{Context = EventContext.Task, Severity = Severity.WARNING, Message
					LogEvent(this, new LogEventArgs(0, Severity.WARNING, "Error copying registry hive from node "+nodeId+ " :"+e.Message));
					return false;
				}
			}
			catch(Exception e){
				LogEvent(this, new LogEventArgs(0, Severity.ERROR, "Error mounting registry hive from node "+nodeId+ " :"+e.Message, EventContext.Task));
			}
			retval = RegLoadKey(HKEY_USERS, hiveMountPath, hiveCopyFile);
			lastError = Marshal.GetLastWin32Error();
			if(lastError != 0){
				string errorMessage = new System.ComponentModel.Win32Exception(lastError).Message;
				LogEvent(this, new LogEventArgs(0, Severity.WARNING, "Error mounting registry hive from node "+nodeId+ " :"+errorMessage));
				//Console.WriteLine ("MountSystemHive(),RegLoadKey hivePath='"+copiedHivePath+"', RetVal : "+retval+", err = "+ errorMessage);
				return false;
			}
			LogEvent(this, new LogEventArgs(0, Severity.DEBUG, "Mounted registry hive from node "+nodeId+ " to "+hiveMountPath));
			return true;

		}

		/// <summary>
		/// Gets the mount points.
		/// </summary>
		/// <returns>
		/// The mount points as tuples (driveLetter, diskId, PartitionOffset)
		/// </returns>
		internal List<Tuple<string, uint, ulong>> GetMountPoints(){
			//Registry.GetValue(@"HKEY_USERS\VDDK_Node_"+nodeId+@"\MountedDevices");
			RegistryKey baseKey = Registry.Users.OpenSubKey(hiveMountPath+@"\MountedDevices");
			if(baseKey == null) return null;
			List<Tuple<string, uint, ulong>> devices = new List<Tuple<string, uint, ulong>>();
			string baseVolumeKey = @"\??Volume";
			string baseMountpointKey = @"\DosDevices\";
			foreach( string keyName in baseKey.GetValueNames()){

				//Console.WriteLine ("   *    ****** NTSystemRegistry.GetMountPoints() : "+keyName);
				if(keyName.StartsWith(baseMountpointKey)){
					byte[] keyValue = (byte[])baseKey.GetValue(keyName);

					uint diskId = BitConverter.ToUInt32(keyValue, 0);
					ulong offset = BitConverter.ToUInt64(keyValue, 4);
					//Console.WriteLine ("GetMountPoints : drive="+keyName.Replace(baseMountpointKey, "")+", disk="+diskId+", offset="+offset);
					Tuple<string, uint, ulong> device =
						new Tuple<string, uint, ulong>(keyName.Replace(baseMountpointKey, ""), diskId, offset/512);

					devices.Add(device);
				}



			}
			baseKey.Close();
			return devices;
			/*Both of the above Values will contain the same data - a 12 byte binary entry. 
				The first four bytes contain the disk signature of the disk containing the partition, 
				the other eight bytes represent the partition offset.
					Using the example f6 b2 f6 b2 00 7e 00 00 00 00 00 00 - 
					the disk signature corresponds to the binary value f6 b2 f6 b2 
					and the partition offset is 00 7e 00 00 00 00 00 00 (which in this case equals sector 63).*/

			//HKCU\software\microsoft\windows\currentversion\explorer\mountpoints2\CPC\Volume\{id}
			// --->Data

		}

		internal RegistryKey GetGey(string path){
			RegistryKey baseKey = Registry.Users.OpenSubKey(hiveMountPath+@"\"+path);
			return baseKey;
		}


		public void Dispose(){
			try{
				LogEvent(this, new LogEventArgs(0, Severity.DEBUG, "Unmounting registry hive "+hiveMountPath));
				RegUnLoadKey(HKEY_USERS, hiveMountPath);
				System.IO.File.Delete(hiveCopyFile);
			}
			catch{}

		}

		/*[StructLayout(LayoutKind.Sequential)]
		struct PackedDiskInfo{
			int DiskId;
			long PartitionOffset;
		}*/
	}
}

#endif