using System;
using System.Runtime.InteropServices;

namespace VDDK{

    public enum VixError:ulong{
        VIX_OK = 0,

        // General errors 
        VIX_E_FAIL = 1,
        VIX_E_OUT_OF_MEMORY = 2,
        VIX_E_INVALID_ARG = 3,
        VIX_E_FILE_NOT_FOUND = 4,
        VIX_E_OBJECT_IS_BUSY = 5,
        VIX_E_NOT_SUPPORTED = 6,
        VIX_E_FILE_ERROR = 7,
        VIX_E_DISK_FULL = 8,
        VIX_E_INCORRECT_FILE_TYPE = 9,
        VIX_E_CANCELLED = 10,
        VIX_E_FILE_READ_ONLY = 11,
        VIX_E_FILE_ALREADY_EXISTS = 12,
        VIX_E_FILE_ACCESS_ERROR = 13,
        VIX_E_REQUIRES_LARGE_FILES = 14,
        VIX_E_FILE_ALREADY_LOCKED = 15,
        VIX_E_NOT_SUPPORTED_ON_REMOTE_OBJECT = 20,
        VIX_E_FILE_TOO_BIG = 21,
        VIX_E_FILE_NAME_INVALID = 22,
        VIX_E_ALREADY_EXISTS = 23,
        VIX_E_BUFFER_TOOSMALL = 24,
		VIX_E_OBJECT_NOT_FOUND = 25,
        VIX_E_HOST_NOT_CONNECTED = 26,
        VIX_E_INVALID_UTF8_STRING = 27,

        VIX_E_OPERATION_ALREADY_IN_PROGRESS = 31,
        // Handle Errors 
        VIX_E_INVALID_HANDLE = 1000,
        VIX_E_NOT_SUPPORTED_ON_HANDLE_TYPE = 1001,
        VIX_E_TOO_MANY_HANDLES = 1002,

        // XML errors 
        VIX_E_NOT_FOUND = 2000,
        VIX_E_TYPE_MISMATCH = 2001,
        VIX_E_INVALID_XML = 2002,

        // VM Control Errors 
        VIX_E_TIMEOUT_WAITING_FOR_TOOLS = 3000,
        VIX_E_UNRECOGNIZED_COMMAND = 3001,
        VIX_E_OP_NOT_SUPPORTED_ON_GUEST = 3003,
        VIX_E_PROGRAM_NOT_STARTED = 3004,
        VIX_E_CANNOT_START_READ_ONLY_VM = 3005,
        VIX_E_VM_NOT_RUNNING = 3006,
        VIX_E_VM_IS_RUNNING = 3007,
        VIX_E_CANNOT_CONNECT_TO_VM = 3008,
        VIX_E_POWEROP_SCRIPTS_NOT_AVAILABLE = 3009,
        VIX_E_NO_GUEST_OS_INSTALLED = 3010,
        VIX_E_VM_INSUFFICIENT_HOST_MEMORY = 3011,
        VIX_E_SUSPEND_ERROR = 3012,
        VIX_E_VM_NOT_ENOUGH_CPUS = 3013,
        VIX_E_HOST_USER_PERMISSIONS = 3014,
        VIX_E_GUEST_USER_PERMISSIONS = 3015,
        VIX_E_TOOLS_NOT_RUNNING = 3016,
        VIX_E_GUEST_OPERATIONS_PROHIBITED = 3017,
        VIX_E_ANON_GUEST_OPERATIONS_PROHIBITED = 3018,
        VIX_E_ROOT_GUEST_OPERATIONS_PROHIBITED = 3019,
        VIX_E_MISSING_ANON_GUEST_ACCOUNT = 3023,
        VIX_E_CANNOT_AUTHENTICATE_WITH_GUEST = 3024,
        VIX_E_UNRECOGNIZED_COMMAND_IN_GUEST = 3025,
        VIX_E_CONSOLE_GUEST_OPERATIONS_PROHIBITED = 3026,
        VIX_E_MUST_BE_CONSOLE_USER = 3027,
        VIX_E_NOT_ALLOWED_DURING_VM_RECORDING = 3028,
        VIX_E_NOT_ALLOWED_DURING_VM_REPLAY = 3029,

        // VM Errors 
        VIX_E_VM_NOT_FOUND = 4000,
        VIX_E_NOT_SUPPORTED_FOR_VM_VERSION = 4001,
        VIX_E_CANNOT_READ_VM_CONFIG = 4002,
        VIX_E_TEMPLATE_VM = 4003,
        VIX_E_VM_ALREADY_LOADED = 4004,
        VIX_E_VM_ALREADY_UP_TO_DATE = 4006,

        // Property Errors 
        VIX_E_UNRECOGNIZED_PROPERTY = 6000,
        VIX_E_INVALID_PROPERTY_VALUE = 6001,
        VIX_E_READ_ONLY_PROPERTY = 6002,
        VIX_E_MISSING_REQUIRED_PROPERTY = 6003,

        // Completion Errors 
        VIX_E_BAD_VM_INDEX = 8000,

        // Snapshot errors 
        VIX_E_SNAPSHOT_INVAL = 13000,
        VIX_E_SNAPSHOT_DUMPER = 13001,
        VIX_E_SNAPSHOT_DISKLIB = 13002,
        VIX_E_SNAPSHOT_NOTFOUND = 13003,
        VIX_E_SNAPSHOT_EXISTS = 13004,
        VIX_E_SNAPSHOT_VERSION = 13005,
        VIX_E_SNAPSHOT_NOPERM = 13006,
        VIX_E_SNAPSHOT_CONFIG = 13007,
        VIX_E_SNAPSHOT_NOCHANGE = 13008,
        VIX_E_SNAPSHOT_CHECKPOINT = 13009,
        VIX_E_SNAPSHOT_LOCKED = 13010,
        VIX_E_SNAPSHOT_INCONSISTENT = 13011,
        VIX_E_SNAPSHOT_NAMETOOLONG = 13012,
        VIX_E_SNAPSHOT_VIXFILE = 13013,
        VIX_E_SNAPSHOT_DISKLOCKED = 13014,
        VIX_E_SNAPSHOT_DUPLICATEDDISK = 13015,
        VIX_E_SNAPSHOT_INDEPENDENTDISK = 13016,
        VIX_E_SNAPSHOT_NONUNIQUE_NAME = 13017,

        // Host Errors 
        VIX_E_HOST_DISK_INVALID_VALUE = 14003,
        VIX_E_HOST_DISK_SECTORSIZE = 14004,
        VIX_E_HOST_FILE_ERROR_EOF = 14005,
        VIX_E_HOST_NETBLKDEV_HANDSHAKE = 14006,
        VIX_E_HOST_SOCKET_CREATION_ERROR = 14007,
        VIX_E_HOST_SERVER_NOT_FOUND = 14008,
        VIX_E_HOST_NETWORK_CONN_REFUSED = 14009,
        VIX_E_HOST_TCP_SOCKET_ERROR = 14010,
        VIX_E_HOST_TCP_CONN_LOST = 14011,
        VIX_E_HOST_NBD_HASHFILE_VOLUME = 14012,
        VIX_E_HOST_NBD_HASHFILE_INIT = 14013,

        // Disklib errors 
        VIX_E_DISK_INVAL = 16000,
        VIX_E_DISK_NOINIT = 16001,
        VIX_E_DISK_NOIO = 16002,
        VIX_E_DISK_PARTIALCHAIN = 16003,
        VIX_E_DISK_NEEDSREPAIR = 16006,
        VIX_E_DISK_OUTOFRANGE = 16007,
        VIX_E_DISK_CID_MISMATCH = 16008,
        VIX_E_DISK_CANTSHRINK = 16009,
        VIX_E_DISK_PARTMISMATCH = 16010,
        VIX_E_DISK_UNSUPPORTEDDISKVERSION = 16011,
        VIX_E_DISK_OPENPARENT = 16012,
        VIX_E_DISK_NOTSUPPORTED = 16013,
        VIX_E_DISK_NEEDKEY = 16014,
        VIX_E_DISK_NOKEYOVERRIDE = 16015,
        VIX_E_DISK_NOTENCRYPTED = 16016,
        VIX_E_DISK_NOKEY = 16017,
        VIX_E_DISK_INVALIDPARTITIONTABLE = 16018,
        VIX_E_DISK_NOTNORMAL = 16019,
        VIX_E_DISK_NOTENCDESC = 16020,
        VIX_E_DISK_NEEDVMFS = 16022,
        VIX_E_DISK_RAWTOOBIG = 16024,
        VIX_E_DISK_TOOMANYOPENFILES = 16027,
        VIX_E_DISK_TOOMANYREDO = 16028,
        VIX_E_DISK_RAWTOOSMALL = 16029,
        VIX_E_DISK_INVALIDCHAIN = 16030,
        VIX_E_DISK_KEY_NOTFOUND = 16052,
        VIX_E_DISK_SUBSYSTEM_INIT_FAIL = 16053,
        VIX_E_DISK_INVALID_CONNECTION = 16054,
        VIX_E_DISK_NOLICENSE = 16064,

        // Remoting Errors. 
        VIX_E_CANNOT_CONNECT_TO_HOST = 18000,
        VIX_E_NOT_FOR_REMOTE_HOST = 18001,

        // Guest Errors
        VIX_E_NOT_A_FILE = 20001,
        VIX_E_NOT_A_DIRECTORY = 20002,
        VIX_E_NO_SUCH_PROCESS = 20003,
        VIX_E_FILE_NAME_TOO_LONG = 20004,

		// Guest VMWare tools errors
		VIX_E_TOOLS_INSTALL_NO_IMAGE = 21000,
		VIX_E_TOOLS_INSTALL_IMAGE_INACCESIBLE = 21001,
		VIX_E_TOOLS_INSTALL_NO_DEVICE = 21002,
		VIX_E_TOOLS_INSTALL_DEVICE_NOT_CONNECTED = 21003,
		VIX_E_TOOLS_INSTALL_CANCELLED = 21004,
		VIX_E_TOOLS_INSTALL_INIT_FAILED = 21005,
		VIX_E_TOOLS_INSTALL_AUTO_NOT_SUPPORTED = 21006,
		VIX_E_TOOLS_INSTALL_GUEST_NOT_READY = 21007,
		VIX_E_TOOLS_INSTALL_SIG_CHECK_FAILED = 21008,
		VIX_E_TOOLS_INSTALL_ERROR = 21009,
		VIX_E_TOOLS_INSTALL_ALREADY_UP_TO_DATE = 21010,
		VIX_E_TOOLS_INSTALL_IN_PROGRESS = 21011,

		VIX_E_WRAPPER_WORKSTATION_NOT_INSTALLED = 22001,
		VIX_E_WRAPPER_VERSION_NOT_FOUND = 22002,
		VIX_E_WRAPPER_SERVICEPROVIDER_NOT_FOUND = 22003,
		VIX_E_WRAPPER_PLAYER_NOT_INSTALLED = 22004,
		VIX_E_WRAPPER_RUNTIME_NOT_INSTALLED = 22005,
		VIX_E_WRAPPER_MULTIPLE_SERVICEPROVIDERS = 22006,

		// MntApi Specific codes
		VIX_E_MNTAPI_MOUNTPT_NOT_FOUND = 24000,
		VIX_E_MNTAPI_MOUNTPT_IN_USE = 24001,
		VIX_E_MNTAPI_DISK_NOT_FOUND = 24002,
		VIX_E_MNTAPI_DISK_NOT_MOUNTED = 24003,
		VIX_E_MNTAPI_DISK_IS_MOUNTED = 24004,
		VIX_E_MNTAPI_DISK_NOT_SAFE = 24005,
		VIX_E_MNTAPI_DISK_CANT_OPEN = 24006,
		VIX_E_MNTAPI_CANT_READ_PARTS = 24007,
		VIX_E_MNTAPI_UMOUNT_APP_NOT_FOUND = 24008,
		VIX_E_MNTAPI_UMOUNT = 24009,
		VIX_E_MNTAPI_NO_MOUNTABLE_PARTITONS = 24010,
		VIX_E_MNTAPI_PARTITION_RANGE = 24011,
		VIX_E_MNTAPI_PERM = 24012,
		VIX_E_MNTAPI_DICT = 24013,
		VIX_E_MNTAPI_DICT_LOCKED = 24014,
		VIX_E_MNTAPI_OPEN_HANDLES = 24015,
		VIX_E_MNTAPI_CANT_MAKE_VAR_DIR = 24016,
		VIX_E_MNTAPI_NO_ROOT = 24017,
		VIX_E_MNTAPI_LOOP_FAILED = 24018,
		VIX_E_MNTAPI_DAEMON = 24019,
		VIX_E_MNTAPI_INTERNAL = 24020,
		VIX_E_MNTAPI_SYSTEM = 24021,
		VIX_E_MNTAPI_NO_CONNECTION_DETAILS = 24022,
		VIX_E_MNTAPI_INCOMPATIBLE_VERSION = 24300,
		VIX_E_MNTAPI_OS_ERROR = 24301,
		VIX_E_MNTAPI_DRIVE_LETTER_IN_USE = 24302,
		VIX_E_MNTAPI_DRIVE_LETTER_ALREADY_ASSIGNED = 24303,
		VIX_E_MNTAPI_VOLUME_NOT_MOUNTED = 24304,
		VIX_E_MNTAPI_VOLUME_ALREADY_MOUNTED = 24305,
		VIX_E_MNTAPI_FORMAT_FAILURE = 24306,
		VIX_E_MNTAPI_NO_DRIVER = 24307,
		VIX_E_MNTAPI_ALREADY_OPENED = 24308,
		VIX_E_MNTAPI_ITEM_NOT_FOUND = 24309,
		VIX_E_MNTAPI_UNSUPPROTED_BOOT_LOADER = 24310,
		VIX_E_MNTAPI_UNSUPPROTED_OS = 24311,
		VIX_E_MNTAPI_CODECONVERSION = 24312,
		VIX_E_MNTAPI_REGWRITE_ERROR = 24313,
		VIX_E_MNTAPI_UNSUPPORTED_FT_VOLUME = 24314,
		VIX_E_MNTAPI_PARTITION_NOT_FOUND = 24315,
		VIX_E_MNTAPI_PUTFILE_ERROR = 24316,
		VIX_E_MNTAPI_GETFILE_ERROR = 24317,
		VIX_E_MNTAPI_REG_NOT_OPENED = 24318,
		VIX_E_MNTAPI_REGDELKEY_ERROR = 24319,
		VIX_E_MNTAPI_CREATE_PARTITIONTABLE_ERROR = 24320,
		VIX_E_MNTAPI_OPEN_FAILURE = 24321,
		VIX_E_MNTAPI_VOLUME_NOT_WRITABLE = 24322,

		VIX_E_NET_HTTP_UNSUPPORTED_PROTOCOL = 30001,
		VIX_E_NET_HTTP_URL_MALFORMAT = 30003,
		VIX_E_NET_HTTP_COULDNT_RESOLVE_PROXY = 30005,
		VIX_E_NET_HTTP_COULDNT_RESOLVE_HOST = 30006,
		VIX_E_NET_HTTP_COULDNT_CONNECT = 30007,
		VIX_E_NET_HTTP_HTTP_RETURNED_ERROR = 30022,
		VIX_E_NET_HTTP_OPERATION_TIMEDOUT = 30028,
		VIX_E_NET_HTTP_SSL_CONNECT_ERROR = 30035,
		VIX_E_NET_HTTP_TOO_MANY_REDIRECTS = 30047,
		VIX_E_NET_HTTP_TRANSFER = 30200,
		VIX_E_NET_HTTP_SSL_SECURITY = 30201,
		VIX_E_NET_HTTP_GENERIC = 30202
        
	}

   
    public enum VixDiskLibCredType:uint{
        [MarshalAsAttribute(UnmanagedType.U4)] VIXDISKLIB_CRED_UID = 1 , // use user/password style authentication
        [MarshalAsAttribute(UnmanagedType.U4)] VIXDISKLIB_CRED_SESSIONID = 2,  // http session id
        [MarshalAsAttribute(UnmanagedType.U4)] VIXDISKLIB_CRED_TICKETID = 3 , // vim ticket id
        [MarshalAsAttribute(UnmanagedType.U4)] VIXDISKLIB_CRED_UNKNOWN = 256
	}

    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]//, CharSet=CharSet.Auto)]
    public struct VixDiskLibConnectParams{
			[MarshalAs(UnmanagedType.LPStr)]
			public string VmxSpec;//'"MyVm/MyVm.vmx?dcPath=Path/to/MyDatacenter&dsName=storage1"
			//[MarshalAsAttribute(UnmanagedType.LPStr)]
			public string ServerName;
			[MarshalAsAttribute(UnmanagedType.U4)]	
			public uint CredType;                                         //As VixDiskLibCredType 'todo'
			public VixDiskLibCreds VixCredentials;// = new VixDiskLibCreds();
			public uint Port;
	}

	public struct VixDiskLibConnectParams51{
			[MarshalAs(UnmanagedType.LPStr)]
			public string VmxSpec;//'"MyVm/MyVm.vmx?dcPath=Path/to/MyDatacenter&dsName=storage1"
			//[MarshalAsAttribute(UnmanagedType.LPStr)]
			public string ServerName;
			[MarshalAsAttribute(UnmanagedType.U4)]	
			public uint CredType;                                         //As VixDiskLibCredType 'todo'
			public VixDiskLibCreds VixCredentials;// = new VixDiskLibCreds();
			public uint Port;
	}

  

  


    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
    public struct VixDiskLibCreds{
		public VixDiskLibUidPasswdCreds Uid;
		public IntPtr ticketId;
	}

	[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
    public struct VixDiskLibCreds51{
		public VixDiskLibUidPasswdCreds Uid;
		public IntPtr ticketId;
	}



    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
    public struct VixDiskLibUidPasswdCreds{
			[MarshalAsAttribute(UnmanagedType.LPStr)] public string UserName;
			[MarshalAsAttribute(UnmanagedType.LPStr)] public string Password;
    }

    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
    public struct VixDiskLibSessionIdCreds{
			[MarshalAsAttribute(UnmanagedType.LPStr)] public string cookie ;
			[MarshalAsAttribute(UnmanagedType.LPStr)] public string username; 
			[MarshalAsAttribute(UnmanagedType.LPStr)] public string key ;
    }

    

    //Disk info
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
    public class VixDiskLibInfo{
		public VixDiskLibGeometry BiosGeo;    // BIOS geometry for booting and partitioning
		public VixDiskLibGeometry PhysGeo;   // physical geometry
		public UInt64 Capacity;             // total capacity in sectors
		public VixDiskLibAdapterType AdapterType;  // adapter type
		public Int16 NumLinks;               // number of links (i.e. base disk + redo logs)
		IntPtr ParentFileNameHint;     // parent file for a redo log                 '<<<todo: string or ptr?
    }

    //Disk types
    public enum VixDiskLibDiskType{
        VIXDISKLIB_DISK_MONOLITHIC_SPARSE = 1,    // monolithic file, sparse
        VIXDISKLIB_DISK_MONOLITHIC_FLAT = 2,    // monolithic file,  all space pre-allocated
        VIXDISKLIB_DISK_SPLIT_SPARSE = 3,    // disk split into 2GB extents, sparse
        VIXDISKLIB_DISK_SPLIT_FLAT = 4,    // disk split into 2GB extents, pre-allocated
        VIXDISKLIB_DISK_VMFS_FLAT = 5,    // ESX 3.0 and above flat disks
        VIXDISKLIB_DISK_STREAM_OPTIMIZED = 6,    // compressed monolithic sparse
        VIXDISKLIB_DISK_UNKNOWN = 256  // unknown type
	}

    //Disk adapter types
    public enum VixDiskLibAdapterType{
        VIXDISKLIB_ADAPTER_IDE = 1,
        VIXDISKLIB_ADAPTER_SCSI_BUSLOGIC = 2,
        VIXDISKLIB_ADAPTER_SCSI_LSILOGIC = 3,
        VIXDISKLIB_ADAPTER_UNKNOWN = 256
	}

    //Geometry
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
    public struct VixDiskLibGeometry{
		public uint Cylinders;
		public uint Heads;
		public uint Sectors;
    }

    //Disk Create
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
    public struct VixDiskLibCreateParams{
			public VixDiskLibDiskType DiskType;
			public VixDiskLibAdapterType AdapterType ;
			public UInt16 HwVersion ;
			public UInt32 Capacity ;
    }

	/*[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
	public struct LogArgs{
		public uint count;
     	public IntPtr items;
	}*/

	[UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = true)]
	public delegate void VixDiskLibGenericLogFunc(
			//[MarshalAs(UnmanagedType.LPStr)] 
			//string Message,
			IntPtr Message,
			IntPtr args // C++ style va_args

		);
			

	public class VixDiskLib {
    
		public int  VIXDISKLIB_VERSION_MAJOR  = 1;
		public int  VIXDISKLIB_VERSION_MINOR = 0;
		public int VIXDISKLIB_SECTOR_SIZE = 512;

	    //Disk Open Flags
	    //VIXDISKLIB_FLAG_OPEN_UNBUFFERED -> (1 << 0)
		public static int VIXDISKLIB_FLAG_OPEN_UNBUFFERED  = (1) << (0);
	    //'VIXDISKLIB_FLAG_OPEN_SINGLE_LINK -> (1 << 1)
		public static int VIXDISKLIB_FLAG_OPEN_SINGLE_LINK  = (1) << (1);
		public static int VIXDISKLIB_FLAG_OPEN_READ_ONLY = (1) << (2);
	    //*****Virtual hardware version
		public static int  VIXDISKLIB_HWVERSION_WORKSTATION_4 = 3; // VMware Workstation 4.x and GSX Server 3.x
		public static int  VIXDISKLIB_HWVERSION_WORKSTATION_5 = 4; // VMware Workstation 5.x and Server 1.x
		public static int  VIXDISKLIB_HWVERSION_ESX30 = VIXDISKLIB_HWVERSION_WORKSTATION_5; // VMware ESX Server 3.0
		public static int  VIXDISKLIB_HWVERSION_WORKSTATION_6 = 6; // VMware Workstation 6.x
		public static int  VIXDISKLIB_HWVERSION_CURRENT = VIXDISKLIB_HWVERSION_WORKSTATION_6; //Defines the state of the art hardware version.  Be careful using this as it will change from time to time.

	    //*****Delegates
		public delegate bool VixDiskLibProgressFunc(IntPtr progressData, int percentCompleted);

	    //*****Clone Progress Callback Func
	    public int CloneProgressFunc(IntPtr param0, int percentCompleted){
				return percentCompleted;
			}

	    //*****Log struct
	   	//[StructLayout(LayoutKind., CharSet=CharSet.Ansi)]
	    /*public struct LogEntry{
			[MarshalAs(UnmanagedType.LPStr)]
			public string Message;
			[MarshalAs(UnmanagedType.LPStr)]
			public string Va_list;
	    }*/

	    //*****Function Definitions
	    [DllImport("vixDiskLib", SetLastError=true)]
		public static extern VixError VixDiskLib_Attach();
	        //n/a
	    
	    [DllImport("vixDiskLib", SetLastError=true)]
		public static extern VixError VixDiskLib_Clone(IntPtr dstConnection, string dstPath, IntPtr srcConnection, string srcPath, ref VixDiskLibCreateParams vixCreateParams, VixDiskLibProgressFunc progressFunc, IntPtr progressCallbackData, bool overWrite);
	    

	    [DllImport("vixDiskLib", EntryPoint="VixDiskLib_Close", SetLastError=true)]
		public static extern VixError Close(IntPtr diskHandle);

	    [DllImport("vixDiskLib.dll", SetLastError=true, ExactSpelling = true, EntryPoint = "VixDiskLib_Connect")]
		public static extern VixError Connect(ref VixDiskLibConnectParams connectParams, out IntPtr connection);

		[DllImport("vixDiskLib.dll", ExactSpelling=true, EntryPoint="VixDiskLib_Cleanup")]
		public static extern VixError Cleanup(ref VixDiskLibConnectParams connectParams, ref uint numCleanedUp, ref uint numRemaining);

		/// <summary>
		/// Connects to an existing snapshots
		/// </summary>
		/// <returns>
		/// The error code.
		/// </returns>
		/// <param name='connectParams'>
		/// connection parameters structure
		/// </param>
		/// <param name='readOnly'>
		/// Read only. (faster)
		/// </param>
		/// <param name='snapshotMoRef'>
		/// Snapshot mo reference.
		/// </param>
		/// <param name='transportModes'>
		/// Transport modes to use, by preference order. Exemple : "file:san:hotadd:nbd"
		/// </param>
		/// <param name='connection'>
		/// The snapshot connection pointer, ready to be used if the return code is VIX_OK
		/// </param>
		[DllImport("vixDiskLib.dll",CharSet=CharSet.Ansi, SetLastError=true, ExactSpelling = true, EntryPoint = "VixDiskLib_ConnectEx")]
		public static extern VixError ConnectEx(
					 ref VixDiskLibConnectParams connectParams,
		             [MarshalAsAttribute(UnmanagedType.I1)]
                     bool readOnly,
                     string snapshotMoRef,
                     string transportModes,
                     out IntPtr connection);

		[DllImport("vixDiskLib", EntryPoint="VixDiskLib_PrepareForAccess"/*, CharSet=CharSet.Ansi charset-ansi doesn't work!!!*/, SetLastError=true)]
		public static extern VixError PrepareForAccess(
			ref VixDiskLibConnectParams connectParams, 
			//[MarshalAs(UnmanagedType.LPStr)] 
			[In] string identity); // doesn't work on win64
			//IntPtr identity);

		[DllImport("vixDiskLib", EntryPoint="VixDiskLib_EndAccess", SetLastError=true)]
		public static extern VixError EndAccess(ref VixDiskLibConnectParams connectParams, string identity);


	    [DllImport("vixDiskLib.dll", EntryPoint="VixDiskLib_Create", SetLastError=true)]
	    public static extern VixError Create(IntPtr connection, 
		        [InAttribute(), MarshalAsAttribute(UnmanagedType.LPStr)] string path, ref VixDiskLibCreateParams createParams, VixDiskLibProgressFunc progressFunc, IntPtr progressCallbackData);

	    [DllImport("vixDiskLib", EntryPoint="VixDiskLib_CreateChild", SetLastError=true)]
		public static extern VixError CreateChild(IntPtr diskHandle, 
		        [InAttribute(), MarshalAsAttribute(UnmanagedType.LPStr)] string childPath, VixDiskLibDiskType diskType, VixDiskLibProgressFunc progressFunc, IntPtr progressCallbackData);

	    [DllImport("vixDiskLib", EntryPoint="VixDiskLib_Defragment", SetLastError=true)]
		public static extern VixError Defragment(IntPtr diskHandle, VixDiskLibProgressFunc progressFunc, IntPtr progressCallbackData);

	    [DllImport("vixDiskLib", EntryPoint="VixDiskLib_Disconnect", SetLastError=true)]
		public static extern VixError Disconnect(IntPtr connection);

	    [DllImport("vixDiskLib", EntryPoint="VixDiskLib_Exit", SetLastError=true)]
		public static extern void Exit();

	    [DllImport("vixDiskLib", EntryPoint="VixDiskLib_FreeErrorText", SetLastError=true)]
		public static extern void FreeErrorText(IntPtr vixErrorMsgPtr);

	    [DllImport("vixDiskLib", EntryPoint="VixDiskLib_FreeInfo", SetLastError=true)]
		public static extern void FreeInfo(IntPtr info);

		/// <summary>
		/// Gets the error message for the provided error code.
		/// </summary>
		/// <returns>
		/// Error message
		/// </returns>
		/// <param name='vixErrorCode'>
		/// (ulong)Vix error code.
		/// </param>
		/// <param name='locale'>
		/// Not supported, must be null.
		/// </param>
	    [DllImport("vixDiskLib", EntryPoint="VixDiskLib_GetErrorText", CharSet=CharSet.Auto, SetLastError=true)]
		[return : MarshalAs(UnmanagedType.LPStr)]
		public static extern string GetErrorText(UInt64 vixErrorCode, string locale);

	    [DllImport("vixDiskLib", EntryPoint="VixDiskLib_GetInfo", SetLastError=true)]
		public static extern VixError GetInfo(IntPtr diskHandle, ref IntPtr diskInfo);

	    [DllImport("vixDiskLib", EntryPoint="VixDiskLib_GetMetadataKeys", SetLastError=true)]
		public static extern VixError GetMetadataKeys(IntPtr diskHandle, 
						/*[MarshalAsAttribute(UnmanagedType.LPArray)]*/
		              ref IntPtr keysBuffer, 
		              uint bufLen, 
		              ref uint requiredLen);


		[DllImport("vixDiskLib", EntryPoint="VixDiskLib_GetTransportMode", /*CharSet = CharSet.Ansi,*/ ExactSpelling = true, SetLastError=true)]
		public static extern IntPtr GetTransportMode(IntPtr diskHandle);

	    [DllImport("vixDiskLib.dll", EntryPoint="VixDiskLib_Grow", SetLastError=true)]
		public static extern VixError Grow();

	    [DllImport("vixDiskLib", EntryPoint="VixDiskLib_InitEx", CallingConvention=CallingConvention.StdCall, SetLastError=true)]
		public static extern VixError InitEx(uint majorVersion, uint minorVersion, VixDiskLibGenericLogFunc logInfo, VixDiskLibGenericLogFunc logWarn, VixDiskLibGenericLogFunc logPanic, string libDir, string configFile);
	        //'Logs: C:\Documents and Settings\user\Local Settings\Temp\vmware-user, /tmp/vmware-user

	   /* [DllImport("libvixDiskLib",  ExactSpelling = true, EntryPoint = "VixDiskLib_Open", SetLastError=true)]
		public static extern VixError VixDiskLib_Open(IntPtr connection, */
			/*[InAttribute(), MarshalAsAttribute(UnmanagedType.LPStr)] */

	    [DllImport("vixDiskLib.dll", CharSet=CharSet.Ansi, ExactSpelling=true, EntryPoint="VixDiskLib_Open", SetLastError=true)]
		public static extern VixError Open([In][Out]IntPtr connection, 
			[InAttribute(), MarshalAs(UnmanagedType.LPStr)] 
		    System.String path,
		    uint flags, out IntPtr diskHandle);

	    [DllImport("vixDiskLib", EntryPoint="VixDiskLib_Read", SetLastError=true)]
		public static extern VixError Read(IntPtr diskHandle, UInt64 startSector, UInt64 numSectors , byte[] readBuffer);

	    [DllImport("vixDiskLib", EntryPoint="VixDiskLib_ReadMetadata", CharSet=CharSet.Ansi, ExactSpelling=true, SetLastError=true)]
		public static extern VixError ReadMetadata(IntPtr diskHandle, string key, IntPtr buf, uint bufLen, ref uint requiredLen);

	    [DllImport("vixDiskLib", SetLastError=true)]
		public static extern VixError VixDiskLib_Rename();

	    [DllImport("vixDiskLib", SetLastError=true)]
		public static extern VixError VixDiskLib_Shrink();

	    [DllImport("libvixDiskLib", SetLastError=true)]
		public static extern VixError VixDiskLib_SpaceNeededForClone(); 

	    [DllImport("libvixDiskLib", SetLastError=true)]
		public static extern VixError VixDiskLib_Unlink();

	    [DllImport("libvixDiskLib", SetLastError=true)]
		public static extern VixError VixDiskLib_Write(IntPtr diskHandler, UInt64 startSector, UInt64 numSectors, byte[] writeBuffer);

	    [DllImport("libvixDiskLib", SetLastError=true)]
		public static extern VixError VixDiskLib_WriteMetadata();

	    [DllImport("libvixDiskLib", SetLastError=true)]
		public static extern VixError VixDiskLib_Rename([InAttribute(), MarshalAsAttribute(UnmanagedType.LPStr)] string srcFileName, [InAttribute(), MarshalAsAttribute(UnmanagedType.LPStr)] string dstFileName) ;

	    [DllImport("libvixDiskLib", SetLastError=true)]
		public static extern VixError VixDiskLib_SpaceNeededForClone(IntPtr diskHandle, VixDiskLibDiskType diskType, ref UInt64 spaceNeeded);

	    [DllImport("libvixDiskLib", SetLastError=true)]
		public static extern VixError VixDiskLib_Unlink(IntPtr connection, [InAttribute(), MarshalAsAttribute(UnmanagedType.LPStr)] string path) ;


		private VixDiskLib (){
		}
	}
}

