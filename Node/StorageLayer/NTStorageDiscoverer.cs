#if OS_WIN
namespace Node.StorageLayer {
	using System;
	using System.IO;
	using System.Collections.Generic;
	using System.Runtime.InteropServices;
	using P2PBackup.Common;
	using P2PBackup.Common.Volumes;
	using P2PBackup.Common.Virtualization;
	using Node.Utilities;
	using Node.Utilities.Native;

	public class NTStorageDiscoverer:IStorageDiscoverer{

		/*
		 * format : call FormatEx() from fmifs.dll
		 * create parts  : IOCTL_SET_PARTITION_INFORMATION_EX, IOCTL_DISK_SET_DRIVE_LAYOUT_EX
		 * */

		public string Name{get{ return "NT";}}
		public string Version{get{return "0.1";}}
		public bool IsProxyingPlugin{get{return false;}}

		public delegate void LogHandler(int code, Severity severity, string message);
		public event EventHandler<LogEventArgs> LogEvent;

		private List<IntPtr> openHandles;

		public bool Initialize(ProxyTaskInfo ptI){
			openHandles = new List<IntPtr>();
			return true;
		}
		 


		/// <summary>
		/// Snapshots the VM and gets its (maybe partial) StorageLayout
		/// </summary>
		/// <returns>
		/// The physical disks.
		/// </returns> retrieve a 
		public StorageLayout BuildStorageLayout(){

			StorageLayout sl = new StorageLayout();
			// http://jo0ls-dotnet-stuff.blogspot.fr/2008/12/howto-get-physical-drive-string.html

			foreach(DriveInfo di in DriveInfo.GetDrives()){
				if(di.DriveType != DriveType.Fixed /*&& di.DriveType != DriveType.Network*/)
					continue;
				string devicePath = @"\\.\"+di.RootDirectory.ToString().TrimEnd(new char[]{'\\'});
				Console.WriteLine ("Mounted dev path="+devicePath);

			
				IntPtr handle = Win32Api.CreateFile(devicePath, Win32Api.GENERIC_READ|Win32Api.GENERIC_WRITE, 
				    Win32Api.FILE_SHARE_READ|Win32Api.FILE_SHARE_WRITE, IntPtr.Zero, 
					Win32Api.OPEN_EXISTING,  0/*Win32Api.FILE_FLAG_BACKUP_SEMANTICS | (uint)Alphaleonis.Win32.Filesystem.FileSystemRights.SystemSecurity*/, IntPtr.Zero);
				openHandles.Add(handle);

				// Then query underlying partition(s)
				Win32Api.DiskExtents extents = new Win32Api.DiskExtents();

				int size = 0;
				bool ok = Win32Api.DeviceIoControl(handle, (uint)Win32Api.Ioctls.GetVolumeDiskExtents, IntPtr.Zero, 
				                                  0, ref extents, Marshal.SizeOf(extents), out size, IntPtr.Zero);
				//Console.WriteLine ("DeviceIoControl : "+(new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error())).Message);
				if(!ok){
					Console.WriteLine ("DeviceIoControl failed : "+(new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error())).Message);
					int blobSize  = Marshal.SizeOf(typeof(Win32Api.DiskExtents)) +  (extents.numberOfExtents - 1) * Marshal.SizeOf(typeof(Win32Api.DiskExtent));
					IntPtr pBlob= Marshal.AllocHGlobal(blobSize);
					uint dataSize = 0;
					ok = Win32Api.DeviceIoControl(handle, (uint)Win32Api.Ioctls.GetVolumeDiskExtents, IntPtr.Zero, 0, pBlob, blobSize, out dataSize, IntPtr.Zero);
          			if(ok){
						IntPtr pNext = new IntPtr(pBlob.ToInt32() + IntPtr.Size/*4*/); // is this always ok on 64 bit OSes? ToInt64?
						for(int i = 0; i< extents.numberOfExtents; i++){
			              // DiskExtent diskExtentN = DirectCast(Marshal.PtrToStructure(pNext, GetType(DiskExtent)), DiskExtent)
							Win32Api.DiskExtent diskExtentN = (Win32Api.DiskExtent)Marshal.PtrToStructure(pNext, typeof(Win32Api.DiskExtent));
			              // physicalDrives.Add("\\.\PhysicalDrive" & diskExtentN.DiskNumber.ToString)
							Console.WriteLine ("found multiple backing part disk=: "+diskExtentN.DiskNumber+", offset="+diskExtentN.StartingOffset+", length="+diskExtentN.ExtentLength);

							pNext = new IntPtr(pNext.ToInt32() + Marshal.SizeOf(typeof(Win32Api.DiskExtent)));
						}
					
					}
					else
						Console.WriteLine ("DeviceIoControl for multiple backing extents failed : "+(new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error())).Message);

				}
				else
					Console.WriteLine ("found part disk=: "+extents.first.DiskNumber+", offset="+extents.first.StartingOffset+", length="+extents.first.ExtentLength);

				Disk disk = new Disk();
				disk.Path = @"\\.\PhysicalDrive"+extents.first.DiskNumber.ToString();//+disk.Id;

				IntPtr physDiskHandle = Win32Api.CreateFile(disk.Path, Win32Api.GENERIC_READ|Win32Api.GENERIC_WRITE, 
				    Win32Api.FILE_SHARE_READ|Win32Api.FILE_SHARE_WRITE, IntPtr.Zero, 
					Win32Api.OPEN_EXISTING,  0/*Win32Api.FILE_FLAG_BACKUP_SEMANTICS | (uint)Alphaleonis.Win32.Filesystem.FileSystemRights.SystemSecurity*/, IntPtr.Zero);
				//Console.WriteLine ("CreateFile : "+(new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error())).Message);
				openHandles.Add(physDiskHandle);
				disk.BlockStream = new FileStream(physDiskHandle, FileAccess.Read);
				disk.Id = (string)extents.first.DiskNumber.ToString();

				// now get the disk geometry to obtain physical sector size
				Win32Api.DISK_GEOMETRY diskGeometry = new Win32Api.DISK_GEOMETRY();
				ok = Win32Api.DeviceIoControl(physDiskHandle, (uint)Win32Api.Ioctls.DiskGetDriveGeometry, IntPtr.Zero, 
				                                  0, ref diskGeometry, Marshal.SizeOf(diskGeometry), out size, IntPtr.Zero);

				Win32Api.MEDIA_SERIAL_NUMBER_DATA diskSerial = new Win32Api.MEDIA_SERIAL_NUMBER_DATA();
				ok = Win32Api.DeviceIoControl(physDiskHandle, (uint)Win32Api.Ioctls.GetMediaSerialNumber, IntPtr.Zero, 
				                                  0, ref diskSerial, Marshal.SizeOf(diskSerial), out size, IntPtr.Zero);
				Console.WriteLine ("DeviceIoControl : "+(new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error())).Message);

				disk.SectorSize = (uint)diskGeometry.BytesPerSector;
				if(diskSerial.SerialNumberData != null)
					disk.Id = System.Text.Encoding.Default.GetString(diskSerial.SerialNumberData);

				disk.Size = diskGeometry.DiskSize;
				if(!sl.Entries.Contains(disk))
					sl.Entries.Add(disk);

				Partition p = new Partition();
				p.Offset = (ulong)extents.first.StartingOffset/disk.SectorSize;
				p.Size = extents.first.ExtentLength;

				FileSystem fs = new FileSystem();
				fs.Path = devicePath;
				fs.DriveFormat = fs.DriveFormat;
				fs.MountPoint = di.RootDirectory.ToString();
				fs.OriginalMountPoint = di.RootDirectory.ToString();
				fs.AvailableFreeSpace = di.AvailableFreeSpace;
				fs.Size = di.TotalSize;
				p.AddChild(fs);
				Console.WriteLine ("created new FS, mnt="+fs.MountPoint);
				foreach(IDiskElement de in sl.Entries){
					if(de is Disk && de.Id == disk.Id){
						Console.WriteLine ("adding new part to layout");
						de.AddChild(p);
					}
				}






			}


			return sl;
		}

		/*private void LogReceived(int code, Severity severity, string message){
			if(this.LogEvent != null) LogEvent(this, new LogEventArgs(code, severity, message));
		}*/

		private void LogReceivedEvent(object sender, LogEventArgs args){
			Logger.Append(args.Severity, args.Message);
		}

		public void Dispose(){
			Logger.Append(Severity.DEBUG, "Disposing 'NT' storage discoverer resources...");
			foreach(IntPtr theHandle in openHandles)
				Win32Api.CloseHandle(theHandle);
		}


	}
}

#endif