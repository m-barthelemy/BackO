using System;
using System.Runtime.InteropServices;

namespace VDDK{

	/**
	 * OS Family types - Currently, only Windows OS is recognized.
	 */
	public enum VixOsFamily {
	  	VIXMNTAPI_NO_OS            =  0,
	   	VIXMNTAPI_WINDOWS          =  1,
	   	VIXMNTAPI_OTHER            =  255
	} 

	/// <summary>
	/// Information about the default OS installed on the disks. Windows only.
	/// </summary>
	/*[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]*/
	public struct VixOsInfo{
	   public VixOsFamily Family;        // OS Family
	   public uint MajorVersion;       // On Windows, 4=NT, 5=2000 and above
	   public uint MinorVersion;       // On Windows, 0=2000, 1=XP, 2=2003
	   public bool  OsIs64Bit;           // True if the OS is 64-bit
	   public string Vendor;              // e.g. Microsoft, RedHat, etc ...
	   public string Edition;             // e.g. Desktop, Enterprise, etc ...
	   public string OsFolder;            // Location where the default OS is installed
	} 


	/// <summary>
	/// Partition type of the volume
	/// </summary>
	public enum VixVolumeType {
	   VIXMNTAPI_UNKNOWN_VOLTYPE  = 0,
	   VIXMNTAPI_BASIC_PARTITION  = 1,
	   VIXMNTAPI_GPT_PARTITION    = 2,
	   VIXMNTAPI_DYNAMIC_VOLUME   = 3,
	   VIXMNTAPI_LVM_VOLUME       = 4
	}


	/// <summary>
	/// Volume/Partition information.
	/// </summary>
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public struct VixVolumeInfo{
		public VixVolumeType    Type;            // Type of the volume
		public bool             IsMounted;       // True if the volume is mounted on the proxy.
		[MarshalAs(UnmanagedType.LPStr)] 
		public string           SymbolicLink;    // Path to the volume mount point, NULL if the volume is not mounted on the proxy.
		public uint  NumGuestMountPoints;       	// Number of mount points for the volume in the guest, 0 if the volume is not mounted on the proxy
		public IntPtr InGuestMountPoints;
	     /* [MarshalAs(UnmanagedType.SafeArray)]   */                              
	  /* public string[] InGuestMountPoints; */       // Mount points for the volume in the guest
	} 


	/**
	 * Diskset information.
	 */
	public struct VixDiskSetInfo{
	   public uint OpenFlags;
	   public string MountPath;
	} 

	/*public struct VixDiskSetHandleStruct VixDiskSetHandle;
	public struct VixVolumeHandleStruct VixVolumeHandle;*/

	public struct VixVolumeHandle{
				/*[MarshalAs(UnmanagedType.I1)]*/
				public ulong dummy;
	}




	public class VixMntApi {

		public const uint VIXMNTAPI_MAJOR_VERSION = 1;
		public const uint VIXMNTAPI_MINOR_VERSION = 0;


		/**
		 * Initializes the VixDiskMount library.
		 * @param majorVersion [in] API major version.
		 * @param minorVersion [in] API minor version.
		 * @param log [in] Callback function to write log messages.
		 * @param warn [in] Callback function to write warning messages.
		 * @param panic [in] Callback function to report fatal errors.
		 * @param libDir [in] Installation directory for library files - can be NULL.
		 * @param configFile [in] Path name of the configuration file containing :
		 *                tmpDirectory = <path to tempdir>
		 * @return VIX_OK if success, suitable VIX error code otherwise.
		 */
		[DllImport("vixMntapi", EntryPoint="VixMntapi_Init", SetLastError=true)]
		public static extern VixError Init(
						uint majorVersion,
		               uint minorVersion,
		               VixDiskLibGenericLogFunc log,
		               VixDiskLibGenericLogFunc warn,
		               VixDiskLibGenericLogFunc panic,
		               string libDir,
		               string configFile);

		/**
		 * Cleans up VixDiskMount library.
		 */
		[DllImport("vixMntapi", EntryPoint="VixMntapi_Exit", SetLastError=true)]
		public static extern void Exit();

		/**
		 * Opens the set of disks for mounting. All the disks for a dynamic volume or
		 * LDM volume must be opened together.
		 * @param diskHandles [in] Array of handles to open disks.
		 * @param numberOfDisks [in] Number of disk handles in the array.
		 * @param openMode [in, optional] Mode to open the diskset - Can be
		 *             VIXDISKLIB_FLAG_OPEN_READ_ONLY
		 * @param handle [out] Disk set handle filled in.
		 * @return VIX_OK if success, suitable VIX error code otherwise.
		 * Supported only on Windows.
		 */
		// WINDOWS ONLY !!!
		[DllImport("vixMntapi", EntryPoint="VixMntapi_OpenDiskSet", SetLastError=true)]
		public static extern VixError OpenDiskSet(IntPtr[] diskHandles,
		                      uint numberOfDisks,
		                      uint openMode,
		                      out IntPtr vixDiskSetHandle);

		/**
		 * Opens the set of disks for mounting.
		 * @param connection [in] VixDiskLibConnection to use for opening the disks.
		 *          VixDiskLib_Open with the specified flags will be called on each
		 *          disk to open.
		 * @param diskNames [in] Array of names of disks to open.
		 * @param numberOfDisks [in] Number of disk handles in the array.
		 *                           Must be 1 for Linux.
		 * @param flags [in] Flags to open the disk.
		 * @param handle [out] Disk set handle filled in.
		 * @return VIX_OK if success, suitable VIX error code otherwise.
		 */
		[DllImport("vixMntapi", EntryPoint="VixMntapi_OpenDisks", CharSet=CharSet.Ansi, SetLastError=true)]
		public static extern VixError OpenDisks(IntPtr VixDiskLibConnection,
		                    string[] diskNames,
		                    uint numberOfDisks,
		                    uint openFlags,
		                    ref IntPtr vixDiskSetHandle);

		/// <summary>
		/// Retrieves the diskSet information.
		/// </summary>
		/// <returns>
		/// ptr to a DiskSetInfo structure
		/// </returns>
		/// <param name='VixDiskSetHandle'>
		/// DiskSet handle, from OpenDisks() or OpenDiskSet()
		/// </param>
		/// <param name='diskSetInfo'>
		/// Ptr to DiskSetInfo
		/// </param>
		[DllImport("vixMntapi", EntryPoint="VixMntapi_GetDiskSetInfo", SetLastError=true)]
		public static extern VixError GetDiskSetInfo(IntPtr VixDiskSetHandle,
		                         ref IntPtr diskSetInfo);


		/// <summary>
		/// Frees the disk set info.
		/// </summary>
		/// <param name='diskSetInfo'>
		/// Disksetinfo Ptr to the structure to be freed.
		/// </param>
		[DllImport("vixMntapi", EntryPoint="VixMntapi_FreeDiskSetInfo", SetLastError=true)]
		public static extern void FreeDiskSetInfo(ref VixDiskSetInfo diskSetInfo);


		/**
		 * Closes the disk set.
		 * @param diskSet [in] Handle to an open disk set.
		 * @return VIX_OK if success, suitable VIX error code otherwise.
		 */
		[DllImport("vixMntapi", EntryPoint="VixMntapi_CloseDiskSet", SetLastError=true)]
		public static extern VixError CloseDiskSet(IntPtr vixDiskSetHandle);

		/**
		 * Retrieves handles to the volumes in the disk set.
		 * @param diskSet [in] Handle to an open disk set.
		 * @param numberOfVolumes [out] Number of volume handles .
		 * @param volumeHandles [out] Array of volume handles.
		 * @return VIX_OK if success, suitable VIX error code otherwise.
		 */
		/*[DllImport("vixMntapi", SetLastError=true)]
		public static extern VixError VixMntapi_GetVolumeHandles(IntPtr vixDiskSetHandle,
		                           uint numberOfVolumes,
		                           IntPtr volumeHandles);*/
		[DllImport("vixMntapi", EntryPoint="VixMntapi_GetVolumeHandles", ExactSpelling=true, SetLastError=true)]
		public static extern VixError GetVolumeHandles(IntPtr vixDiskSetHandle,
		                           ref long numberOfVolumes,
		                           ref IntPtr volumeHandles);

		/**
		 * Frees memory allocated in VixMntapi_GetVolumes.
		 * @param volumeHandles [in] Volume handle to be freed.
		 */
		[DllImport("vixMntapi",  EntryPoint="VixMntapi_FreeVolumeHandles", SetLastError=true)]
		public static extern void FreeVolumeHandles(IntPtr volumeHandles);

		/**
		 * Retrieves information about the default operating system in the disk set.
		 * @param diskSet [in] Handle to an open disk set.
		 * @param info [out] OS information filled up.
		 * @return VIX_OK if success, suitable VIX error code otherwise.
		 */
		[DllImport("vixMntapi", EntryPoint="VixMntapi_GetOsInfo", SetLastError=true)]
		public static extern VixError GetOsInfo(IntPtr vixDiskSetHandle,
		                    ref IntPtr vixOsInfo);

		/**
		 * Frees memory allocated in VixMntapi_GetOperatingSystemInfo.
		 * @param info [in] OS info to be freed.
		 */
		[DllImport("vixMntapi",  EntryPoint="VixMntapi_FreeOsInfo", SetLastError=true)]
		public static extern void FreeOsInfo(VixOsInfo info);

		/**
		 * Mounts the volume. After mounting the volume, use VixMntapi_GetVolumeInfo
		 * to obtain the path to the mounted volume.
		 * @param volumeHandle [in] Handle to a volume.
		 * @param readOnly [in] Whether to mount the volume in read-only mode.
		 * @return VIX_OK if success, suitable VIX error code otherwise.
		 */
		[DllImport("vixMntapi",  EntryPoint="VixMntapi_MountVolume", SetLastError=true, CharSet=CharSet.Ansi)]
		public static extern VixError MountVolume(IntPtr volumeHandle,
				         	[MarshalAs(UnmanagedType.I1)]
		                      bool readOnly);

		/**
		 * Unmounts the volume.
		 * @param volumeHandle [in] Handle to a volume.
		 * @param force [in] Force unmount even if files are open on the volume.
		 * @return VIX_OK if success, suitable VIX error code otherwise.
		 */
		[DllImport("vixMntapi", EntryPoint="VixMntapi_DismountVolume", SetLastError=true)]
		public static extern VixError VixMntapi_DismountVolume(IntPtr volumeHandle,
		                         bool force);

		/**
		 * Retrieves information about a volume. Some of the volume information is
		 * only available if the volume is mounted. Hence, this must be called after
		 * calling VixMntapi_MountVolume.
		 * @param volumeHandle [in] Handle to a volume.
		 * @param info [out] Volume information filled up.
		 * @return VIX_OK if success, suitable VIX error code otherwise.
		 */
		[DllImport("vixMntapi", EntryPoint="VixMntapi_GetVolumeInfo", SetLastError=true)]
		public static extern VixError GetVolumeInfo(IntPtr volumeHandle,
		                        ref IntPtr vixVolumeInfo);

		/**
		 * Frees memory allocated in VixMntapi_GetVolumeInfo.
		 * @param info [in] Volume info to be freed.
		 */
		[DllImport("vixMntapi", EntryPoint="VixMntapi_FreeVolumeInfo",SetLastError=true)]
		public static extern void FreeVolumeInfo(VixVolumeInfo info);




		public VixMntApi ()
		{
		}
	}
}

