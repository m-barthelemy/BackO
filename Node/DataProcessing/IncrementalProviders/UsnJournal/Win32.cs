// Win32Api.cs
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32;
using System.ComponentModel;
using Microsoft.Win32.SafeHandles;

namespace Node.DataProcessing{
	
	public class Win32Api{
        #region enums
        public enum GetLastErrorEnum {
            INVALID_HANDLE_VALUE = -1, 
            ERROR_SUCCESS = 0,
            ERROR_INVALID_FUNCTION = 1,
            ERROR_FILE_NOT_FOUND = 2,
            ERROR_PATH_NOT_FOUND = 3,
            ERROR_TOO_MANY_OPEN_FILES = 4,
            ERROR_ACCESS_DENIED = 5,
            ERROR_INVALID_HANDLE = 6,
            ERROR_INVALID_DATA = 13,
            ERROR_HANDLE_EOF = 38,
            ERROR_NOT_SUPPORTED = 50,
            ERROR_INVALID_PARAMETER = 87,
            ERROR_JOURNAL_DELETE_IN_PROGRESS = 1178,
            ERROR_JOURNAL_NOT_ACTIVE  = 1179,
            ERROR_JOURNAL_ENTRY_DELETED = 1181,
            ERROR_INVALID_USER_BUFFER = 1784
        }

        public enum UsnJournalDeleteFlags {
            USN_DELETE_FLAG_DELETE = 1,
            USN_DELETE_FLAG_NOTIFY = 2
        }

        public enum FILE_INFORMATION_CLASS {
            FileDirectoryInformation = 1,     // 1
            FileFullDirectoryInformation = 2,     // 2
            FileBothDirectoryInformation = 3,     // 3
            FileBasicInformation = 4,         // 4
            FileStandardInformation = 5,      // 5
            FileInternalInformation = 6,      // 6
            FileEaInformation = 7,        // 7
            FileAccessInformation = 8,        // 8
            FileNameInformation = 9,          // 9
            FileRenameInformation = 10,        // 10
            FileLinkInformation = 11,          // 11
            FileNamesInformation = 12,         // 12
            FileDispositionInformation = 13,       // 13
            FilePositionInformation = 14,      // 14
            FileFullEaInformation = 15,        // 15
            FileModeInformation = 16,     // 16
            FileAlignmentInformation = 17,     // 17
            FileAllInformation = 18,           // 18
            FileAllocationInformation = 19,    // 19
            FileEndOfFileInformation = 20,     // 20
            FileAlternateNameInformation = 21,     // 21
            FileStreamInformation = 22,        // 22
            FilePipeInformation = 23,          // 23
            FilePipeLocalInformation = 24,     // 24
            FilePipeRemoteInformation = 25,    // 25
            FileMailslotQueryInformation = 26,     // 26
            FileMailslotSetInformation = 27,       // 27
            FileCompressionInformation = 28,       // 28
            FileObjectIdInformation = 29,      // 29
            FileCompletionInformation = 30,    // 30
            FileMoveClusterInformation = 31,       // 31
            FileQuotaInformation = 32,         // 32
            FileReparsePointInformation = 33,      // 33
            FileNetworkOpenInformation = 34,       // 34
            FileAttributeTagInformation = 35,      // 35
            FileTrackingInformation = 36,      // 36
            FileIdBothDirectoryInformation = 37,   // 37
            FileIdFullDirectoryInformation = 38,   // 38
            FileValidDataLengthInformation = 39,   // 39
            FileShortNameInformation = 40,     // 40
            FileHardLinkInformation = 46    // 46    
        }

		[Flags]
        public enum EMethod : uint
        {
            Buffered = 0,
            InDirect = 1,
            OutDirect = 2,
            Neither = 3
        }

		[Flags]
        public enum EFileDevice : uint
        {
            Beep = 0x00000001,
            CDRom = 0x00000002,
            CDRomFileSytem = 0x00000003,
            Controller = 0x00000004,
            Datalink = 0x00000005,
            Dfs = 0x00000006,
            Disk = 0x00000007,
            DiskFileSystem = 0x00000008,
            FileSystem = 0x00000009,
            InPortPort = 0x0000000a,
            Keyboard = 0x0000000b,
            Mailslot = 0x0000000c,
            MidiIn = 0x0000000d,
            MidiOut = 0x0000000e,
            Mouse = 0x0000000f,
            MultiUncProvider = 0x00000010,
            NamedPipe = 0x00000011,
            Network = 0x00000012,
            NetworkBrowser = 0x00000013,
            NetworkFileSystem = 0x00000014,
            Null = 0x00000015,
            ParellelPort = 0x00000016,
            PhysicalNetcard = 0x00000017,
            Printer = 0x00000018,
            Scanner = 0x00000019,
            SerialMousePort = 0x0000001a,
            SerialPort = 0x0000001b,
            Screen = 0x0000001c,
            Sound = 0x0000001d,
            Streams = 0x0000001e,
            Tape = 0x0000001f,
            TapeFileSystem = 0x00000020,
            Transport = 0x00000021,
            Unknown = 0x00000022,
            Video = 0x00000023,
            VirtualDisk = 0x00000024,
            WaveIn = 0x00000025,
            WaveOut = 0x00000026,
            Port8042 = 0x00000027,
            NetworkRedirector = 0x00000028,
            Battery = 0x00000029,
            BusExtender = 0x0000002a,
            Modem = 0x0000002b,
            Vdm = 0x0000002c,
            MassStorage = 0x0000002d,
            Smb = 0x0000002e,
            Ks = 0x0000002f,
            Changer = 0x00000030,
            Smartcard = 0x00000031,
            Acpi = 0x00000032,
            Dvd = 0x00000033,
            FullscreenVideo = 0x00000034,
            DfsFileSystem = 0x00000035,
            DfsVolume = 0x00000036,
            Serenum = 0x00000037,
            Termsrv = 0x00000038,
            Ksec = 0x00000039
        }

		[Flags]
        public enum EFileAccess : uint
        {
            /// <summary>
            /// 
            /// </summary>
            GenericRead = 0x80000000,
            /// <summary>
            /// 
            /// </summary>
            GenericWrite = 0x40000000,
            /// <summary>
            /// 
            /// </summary>
            GenericExecute = 0x20000000,
            /// <summary>
            /// 
            /// </summary>
            GenericAll = 0x10000000
        }

        [Flags]
        public enum EFileShare : uint
        {
            /// <summary>
            /// 
            /// </summary>
            None = 0x00000000,
            /// <summary>
            /// Enables subsequent open operations on an object to request read access. 
            /// Otherwise, other processes cannot open the object if they request read access. 
            /// If this flag is not specified, but the object has been opened for read access, the function fails.
            /// </summary>
            Read = 0x00000001,
            /// <summary>
            /// Enables subsequent open operations on an object to request write access. 
            /// Otherwise, other processes cannot open the object if they request write access. 
            /// If this flag is not specified, but the object has been opened for write access, the function fails.
            /// </summary>
            Write = 0x00000002,
            /// <summary>
            /// Enables subsequent open operations on an object to request delete access. 
            /// Otherwise, other processes cannot open the object if they request delete access.
            /// If this flag is not specified, but the object has been opened for delete access, the function fails.
            /// </summary>
            Delete = 0x00000004,
            /// <summary>
            /// Combination of read and write
            /// </summary>
            ReadWrite = Read | Write,
            /// <summary>
            /// Combo flag that specifies all access
            /// </summary>
            All = None | Read | Write
        }

		[Flags]
        public enum EIOControlCode : uint
        {
            // STORAGE
            StorageBase = EFileDevice.MassStorage,
            StorageCheckVerify = (StorageBase << 16) | (0x0200 << 2) | EMethod.Buffered | (EFileAccess.GenericRead << 14),
            StorageCheckVerify2 = (StorageBase << 16) | (0x0200 << 2) | EMethod.Buffered | (0 << 14), // FileAccess.Any
            StorageMediaRemoval = (StorageBase << 16) | (0x0201 << 2) | EMethod.Buffered | (EFileAccess.GenericRead << 14),
            StorageEjectMedia = (StorageBase << 16) | (0x0202 << 2) | EMethod.Buffered | (EFileAccess.GenericRead << 14),
            StorageLoadMedia = (StorageBase << 16) | (0x0203 << 2) | EMethod.Buffered | (EFileAccess.GenericRead << 14),
            StorageLoadMedia2 = (StorageBase << 16) | (0x0203 << 2) | EMethod.Buffered | (0 << 14),
            StorageReserve = (StorageBase << 16) | (0x0204 << 2) | EMethod.Buffered | (EFileAccess.GenericRead << 14),
            StorageRelease = (StorageBase << 16) | (0x0205 << 2) | EMethod.Buffered | (EFileAccess.GenericRead << 14),
            StorageFindNewDevices = (StorageBase << 16) | (0x0206 << 2) | EMethod.Buffered | (EFileAccess.GenericRead << 14),
            StorageEjectionControl = (StorageBase << 16) | (0x0250 << 2) | EMethod.Buffered | (0 << 14),
            StorageMcnControl = (StorageBase << 16) | (0x0251 << 2) | EMethod.Buffered | (0 << 14),
            StorageGetMediaTypes = (StorageBase << 16) | (0x0300 << 2) | EMethod.Buffered | (0 << 14),
            StorageGetMediaTypesEx = (StorageBase << 16) | (0x0301 << 2) | EMethod.Buffered | (0 << 14),
            StorageResetBus = (StorageBase << 16) | (0x0400 << 2) | EMethod.Buffered | (EFileAccess.GenericRead << 14),
            StorageResetDevice = (StorageBase << 16) | (0x0401 << 2) | EMethod.Buffered | (EFileAccess.GenericRead << 14),
            StorageGetDeviceNumber = (StorageBase << 16) | (0x0420 << 2) | EMethod.Buffered | (0 << 14),
            StoragePredictFailure = (StorageBase << 16) | (0x0440 << 2) | EMethod.Buffered | (0 << 14),
            StorageObsoleteResetBus = (StorageBase << 16) | (0x0400 << 2) | EMethod.Buffered | ((EFileAccess.GenericRead | EFileAccess.GenericWrite) << 14),
            StorageObsoleteResetDevice = (StorageBase << 16) | (0x0401 << 2) | EMethod.Buffered | ((EFileAccess.GenericRead | EFileAccess.GenericWrite) << 14),
            // DISK
            DiskBase = EFileDevice.Disk,
            DiskGetDriveGeometry = (DiskBase << 16) | (0x0000 << 2) | EMethod.Buffered | (0 << 14),
            DiskGetPartitionInfo = (DiskBase << 16) | (0x0001 << 2) | EMethod.Buffered | (EFileAccess.GenericRead << 14),
            DiskSetPartitionInfo = (DiskBase << 16) | (0x0002 << 2) | EMethod.Buffered | ((EFileAccess.GenericRead | EFileAccess.GenericWrite) << 14),
            DiskGetDriveLayout = (DiskBase << 16) | (0x0003 << 2) | EMethod.Buffered | (EFileAccess.GenericRead << 14),
            DiskSetDriveLayout = (DiskBase << 16) | (0x0004 << 2) | EMethod.Buffered | ((EFileAccess.GenericRead | EFileAccess.GenericWrite) << 14),
            DiskVerify = (DiskBase << 16) | (0x0005 << 2) | EMethod.Buffered | (0 << 14),
            DiskFormatTracks = (DiskBase << 16) | (0x0006 << 2) | EMethod.Buffered | ((EFileAccess.GenericRead | EFileAccess.GenericWrite) << 14),
            DiskReassignBlocks = (DiskBase << 16) | (0x0007 << 2) | EMethod.Buffered | ((EFileAccess.GenericRead | EFileAccess.GenericWrite) << 14),
            DiskPerformance = (DiskBase << 16) | (0x0008 << 2) | EMethod.Buffered | (0 << 14),
            DiskIsWritable = (DiskBase << 16) | (0x0009 << 2) | EMethod.Buffered | (0 << 14),
            DiskLogging = (DiskBase << 16) | (0x000a << 2) | EMethod.Buffered | (0 << 14),
            DiskFormatTracksEx = (DiskBase << 16) | (0x000b << 2) | EMethod.Buffered | ((EFileAccess.GenericRead | EFileAccess.GenericWrite) << 14),
            DiskHistogramStructure = (DiskBase << 16) | (0x000c << 2) | EMethod.Buffered | (0 << 14),
            DiskHistogramData = (DiskBase << 16) | (0x000d << 2) | EMethod.Buffered | (0 << 14),
            DiskHistogramReset = (DiskBase << 16) | (0x000e << 2) | EMethod.Buffered | (0 << 14),
            DiskRequestStructure = (DiskBase << 16) | (0x000f << 2) | EMethod.Buffered | (0 << 14),
            DiskRequestData = (DiskBase << 16) | (0x0010 << 2) | EMethod.Buffered | (0 << 14),
            DiskControllerNumber = (DiskBase << 16) | (0x0011 << 2) | EMethod.Buffered | (0 << 14),
            DiskSmartGetVersion = (DiskBase << 16) | (0x0020 << 2) | EMethod.Buffered | (EFileAccess.GenericRead << 14),
            DiskSmartSendDriveCommand = (DiskBase << 16) | (0x0021 << 2) | EMethod.Buffered | ((EFileAccess.GenericRead | EFileAccess.GenericWrite) << 14),
            DiskSmartRcvDriveData = (DiskBase << 16) | (0x0022 << 2) | EMethod.Buffered | ((EFileAccess.GenericRead | EFileAccess.GenericWrite) << 14),
            DiskUpdateDriveSize = (DiskBase << 16) | (0x0032 << 2) | EMethod.Buffered | ((EFileAccess.GenericRead | EFileAccess.GenericWrite) << 14),
            DiskGrowPartition = (DiskBase << 16) | (0x0034 << 2) | EMethod.Buffered | ((EFileAccess.GenericRead | EFileAccess.GenericWrite) << 14),
            DiskGetCacheInformation = (DiskBase << 16) | (0x0035 << 2) | EMethod.Buffered | (EFileAccess.GenericRead << 14),
            DiskSetCacheInformation = (DiskBase << 16) | (0x0036 << 2) | EMethod.Buffered | ((EFileAccess.GenericRead | EFileAccess.GenericWrite) << 14),
            DiskDeleteDriveLayout = (DiskBase << 16) | (0x0040 << 2) | EMethod.Buffered | ((EFileAccess.GenericRead | EFileAccess.GenericWrite) << 14),
            DiskFormatDrive = (DiskBase << 16) | (0x00f3 << 2) | EMethod.Buffered | ((EFileAccess.GenericRead | EFileAccess.GenericWrite) << 14),
            DiskSenseDevice = (DiskBase << 16) | (0x00f8 << 2) | EMethod.Buffered | (0 << 14),
            DiskCheckVerify = (DiskBase << 16) | (0x0200 << 2) | EMethod.Buffered | (EFileAccess.GenericRead << 14),
            DiskMediaRemoval = (DiskBase << 16) | (0x0201 << 2) | EMethod.Buffered | (EFileAccess.GenericRead << 14),
            DiskEjectMedia = (DiskBase << 16) | (0x0202 << 2) | EMethod.Buffered | (EFileAccess.GenericRead << 14),
            DiskLoadMedia = (DiskBase << 16) | (0x0203 << 2) | EMethod.Buffered | (EFileAccess.GenericRead << 14),
            DiskReserve = (DiskBase << 16) | (0x0204 << 2) | EMethod.Buffered | (EFileAccess.GenericRead << 14),
            DiskRelease = (DiskBase << 16) | (0x0205 << 2) | EMethod.Buffered | (EFileAccess.GenericRead << 14),
            DiskFindNewDevices = (DiskBase << 16) | (0x0206 << 2) | EMethod.Buffered | (EFileAccess.GenericRead << 14),
            DiskGetMediaTypes = (DiskBase << 16) | (0x0300 << 2) | EMethod.Buffered | (0 << 14),
            // CHANGER
            ChangerBase = EFileDevice.Changer,
            ChangerGetParameters = (ChangerBase << 16) | (0x0000 << 2) | EMethod.Buffered | (EFileAccess.GenericRead << 14),
            ChangerGetStatus = (ChangerBase << 16) | (0x0001 << 2) | EMethod.Buffered | (EFileAccess.GenericRead << 14),
            ChangerGetProductData = (ChangerBase << 16) | (0x0002 << 2) | EMethod.Buffered | (EFileAccess.GenericRead << 14),
            ChangerSetAccess = (ChangerBase << 16) | (0x0004 << 2) | EMethod.Buffered | ((EFileAccess.GenericRead | EFileAccess.GenericWrite) << 14),
            ChangerGetElementStatus = (ChangerBase << 16) | (0x0005 << 2) | EMethod.Buffered | ((EFileAccess.GenericRead | EFileAccess.GenericWrite) << 14),
            ChangerInitializeElementStatus = (ChangerBase << 16) | (0x0006 << 2) | EMethod.Buffered | (EFileAccess.GenericRead << 14),
            ChangerSetPosition = (ChangerBase << 16) | (0x0007 << 2) | EMethod.Buffered | (EFileAccess.GenericRead << 14),
            ChangerExchangeMedium = (ChangerBase << 16) | (0x0008 << 2) | EMethod.Buffered | (EFileAccess.GenericRead << 14),
            ChangerMoveMedium = (ChangerBase << 16) | (0x0009 << 2) | EMethod.Buffered | (EFileAccess.GenericRead << 14),
            ChangerReinitializeTarget = (ChangerBase << 16) | (0x000A << 2) | EMethod.Buffered | (EFileAccess.GenericRead << 14),
            ChangerQueryVolumeTags = (ChangerBase << 16) | (0x000B << 2) | EMethod.Buffered | ((EFileAccess.GenericRead | EFileAccess.GenericWrite) << 14),
            // FILESYSTEM
            FsctlRequestOplockLevel1 = (EFileDevice.FileSystem << 16) | (0 << 2) | EMethod.Buffered | (0 << 14),
            FsctlRequestOplockLevel2 = (EFileDevice.FileSystem << 16) | (1 << 2) | EMethod.Buffered | (0 << 14),
            FsctlRequestBatchOplock = (EFileDevice.FileSystem << 16) | (2 << 2) | EMethod.Buffered | (0 << 14),
            FsctlOplockBreakAcknowledge = (EFileDevice.FileSystem << 16) | (3 << 2) | EMethod.Buffered | (0 << 14),
            FsctlOpBatchAckClosePending = (EFileDevice.FileSystem << 16) | (4 << 2) | EMethod.Buffered | (0 << 14),
            FsctlOplockBreakNotify = (EFileDevice.FileSystem << 16) | (5 << 2) | EMethod.Buffered | (0 << 14),
            FsctlLockVolume = (EFileDevice.FileSystem << 16) | (6 << 2) | EMethod.Buffered | (0 << 14),
            FsctlUnlockVolume = (EFileDevice.FileSystem << 16) | (7 << 2) | EMethod.Buffered | (0 << 14),
            FsctlDismountVolume = (EFileDevice.FileSystem << 16) | (8 << 2) | EMethod.Buffered | (0 << 14),
            FsctlIsVolumeMounted = (EFileDevice.FileSystem << 16) | (10 << 2) | EMethod.Buffered | (0 << 14),
            FsctlIsPathnameValid = (EFileDevice.FileSystem << 16) | (11 << 2) | EMethod.Buffered | (0 << 14),
            FsctlMarkVolumeDirty = (EFileDevice.FileSystem << 16) | (12 << 2) | EMethod.Buffered | (0 << 14),
            FsctlQueryRetrievalPointers = (EFileDevice.FileSystem << 16) | (14 << 2) | EMethod.Neither | (0 << 14),
            FsctlGetCompression = (EFileDevice.FileSystem << 16) | (15 << 2) | EMethod.Buffered | (0 << 14),
            FsctlSetCompression = (EFileDevice.FileSystem << 16) | (16 << 2) | EMethod.Buffered | ((EFileAccess.GenericRead | EFileAccess.GenericWrite) << 14),
            FsctlMarkAsSystemHive = (EFileDevice.FileSystem << 16) | (19 << 2) | EMethod.Neither | (0 << 14),
            FsctlOplockBreakAckNo2 = (EFileDevice.FileSystem << 16) | (20 << 2) | EMethod.Buffered | (0 << 14),
            FsctlInvalidateVolumes = (EFileDevice.FileSystem << 16) | (21 << 2) | EMethod.Buffered | (0 << 14),
            FsctlQueryFatBpb = (EFileDevice.FileSystem << 16) | (22 << 2) | EMethod.Buffered | (0 << 14),
            FsctlRequestFilterOplock = (EFileDevice.FileSystem << 16) | (23 << 2) | EMethod.Buffered | (0 << 14),
            FsctlFileSystemGetStatistics = (EFileDevice.FileSystem << 16) | (24 << 2) | EMethod.Buffered | (0 << 14),
            FsctlGetNtfsVolumeData = (EFileDevice.FileSystem << 16) | (25 << 2) | EMethod.Buffered | (0 << 14),
            FsctlGetNtfsFileRecord = (EFileDevice.FileSystem << 16) | (26 << 2) | EMethod.Buffered | (0 << 14),
            FsctlGetVolumeBitmap = (EFileDevice.FileSystem << 16) | (27 << 2) | EMethod.Neither | (0 << 14),
            FsctlGetRetrievalPointers = (EFileDevice.FileSystem << 16) | (28 << 2) | EMethod.Neither | (0 << 14),
            FsctlMoveFile = (EFileDevice.FileSystem << 16) | (29 << 2) | EMethod.Buffered | (0 << 14),
            FsctlIsVolumeDirty = (EFileDevice.FileSystem << 16) | (30 << 2) | EMethod.Buffered | (0 << 14),
            FsctlGetHfsInformation = (EFileDevice.FileSystem << 16) | (31 << 2) | EMethod.Buffered | (0 << 14),
            FsctlAllowExtendedDasdIo = (EFileDevice.FileSystem << 16) | (32 << 2) | EMethod.Neither | (0 << 14),
            FsctlReadPropertyData = (EFileDevice.FileSystem << 16) | (33 << 2) | EMethod.Neither | (0 << 14),
            FsctlWritePropertyData = (EFileDevice.FileSystem << 16) | (34 << 2) | EMethod.Neither | (0 << 14),
            FsctlFindFilesBySid = (EFileDevice.FileSystem << 16) | (35 << 2) | EMethod.Neither | (0 << 14),
            FsctlDumpPropertyData = (EFileDevice.FileSystem << 16) | (37 << 2) | EMethod.Neither | (0 << 14),
            FsctlSetObjectId = (EFileDevice.FileSystem << 16) | (38 << 2) | EMethod.Buffered | (0 << 14),
            FsctlGetObjectId = (EFileDevice.FileSystem << 16) | (39 << 2) | EMethod.Buffered | (0 << 14),
            FsctlDeleteObjectId = (EFileDevice.FileSystem << 16) | (40 << 2) | EMethod.Buffered | (0 << 14),
            FsctlSetReparsePoint = (EFileDevice.FileSystem << 16) | (41 << 2) | EMethod.Buffered | (0 << 14),
            FsctlGetReparsePoint = (EFileDevice.FileSystem << 16) | (42 << 2) | EMethod.Buffered | (0 << 14),
            FsctlDeleteReparsePoint = (EFileDevice.FileSystem << 16) | (43 << 2) | EMethod.Buffered | (0 << 14),
            FsctlEnumUsnData = (EFileDevice.FileSystem << 16) | (44 << 2) | EMethod.Neither | (0 << 14),
            FsctlSecurityIdCheck = (EFileDevice.FileSystem << 16) | (45 << 2) | EMethod.Neither | (EFileAccess.GenericRead << 14),
            FsctlReadUsnJournal = (EFileDevice.FileSystem << 16) | (46 << 2) | EMethod.Neither | (0 << 14),
            FsctlSetObjectIdExtended = (EFileDevice.FileSystem << 16) | (47 << 2) | EMethod.Buffered | (0 << 14),
            FsctlCreateOrGetObjectId = (EFileDevice.FileSystem << 16) | (48 << 2) | EMethod.Buffered | (0 << 14),
            FsctlSetSparse = (EFileDevice.FileSystem << 16) | (49 << 2) | EMethod.Buffered | (0 << 14),
            FsctlSetZeroData = (EFileDevice.FileSystem << 16) | (50 << 2) | EMethod.Buffered | (EFileAccess.GenericWrite << 14),
            FsctlQueryAllocatedRanges = (EFileDevice.FileSystem << 16) | (51 << 2) | EMethod.Neither | (EFileAccess.GenericRead << 14),
            FsctlEnableUpgrade = (EFileDevice.FileSystem << 16) | (52 << 2) | EMethod.Buffered | (EFileAccess.GenericWrite << 14),
            FsctlSetEncryption = (EFileDevice.FileSystem << 16) | (53 << 2) | EMethod.Neither | (0 << 14),
            FsctlEncryptionFsctlIo = (EFileDevice.FileSystem << 16) | (54 << 2) | EMethod.Neither | (0 << 14),
            FsctlWriteRawEncrypted = (EFileDevice.FileSystem << 16) | (55 << 2) | EMethod.Neither | (0 << 14),
            FsctlReadRawEncrypted = (EFileDevice.FileSystem << 16) | (56 << 2) | EMethod.Neither | (0 << 14),
            FsctlCreateUsnJournal = (EFileDevice.FileSystem << 16) | (57 << 2) | EMethod.Neither | (0 << 14),
            FsctlReadFileUsnData = (EFileDevice.FileSystem << 16) | (58 << 2) | EMethod.Neither | (0 << 14),
            FsctlWriteUsnCloseRecord = (EFileDevice.FileSystem << 16) | (59 << 2) | EMethod.Neither | (0 << 14),
            FsctlExtendVolume = (EFileDevice.FileSystem << 16) | (60 << 2) | EMethod.Buffered | (0 << 14),
            FsctlQueryUsnJournal = (EFileDevice.FileSystem << 16) | (61 << 2) | EMethod.Buffered | (0 << 14),
            FsctlDeleteUsnJournal = (EFileDevice.FileSystem << 16) | (62 << 2) | EMethod.Buffered | (0 << 14),
            FsctlMarkHandle = (EFileDevice.FileSystem << 16) | (63 << 2) | EMethod.Buffered | (0 << 14),
            FsctlSisCopyFile = (EFileDevice.FileSystem << 16) | (64 << 2) | EMethod.Buffered | (0 << 14),
            FsctlSisLinkFiles = (EFileDevice.FileSystem << 16) | (65 << 2) | EMethod.Buffered | ((EFileAccess.GenericRead | EFileAccess.GenericWrite) << 14),
            FsctlHsmMsg = (EFileDevice.FileSystem << 16) | (66 << 2) | EMethod.Buffered | ((EFileAccess.GenericRead | EFileAccess.GenericWrite) << 14),
            FsctlNssControl = (EFileDevice.FileSystem << 16) | (67 << 2) | EMethod.Buffered | (EFileAccess.GenericWrite << 14),
            FsctlHsmData = (EFileDevice.FileSystem << 16) | (68 << 2) | EMethod.Neither | ((EFileAccess.GenericRead | EFileAccess.GenericWrite) << 14),
            FsctlRecallFile = (EFileDevice.FileSystem << 16) | (69 << 2) | EMethod.Neither | (0 << 14),
            FsctlNssRcontrol = (EFileDevice.FileSystem << 16) | (70 << 2) | EMethod.Buffered | (EFileAccess.GenericRead << 14),
            // VIDEO
            VideoQuerySupportedBrightness = (EFileDevice.Video << 16) | (0x0125 << 2) | EMethod.Buffered | (0 << 14),
            VideoQueryDisplayBrightness = (EFileDevice.Video << 16) | (0x0126 << 2) | EMethod.Buffered | (0 << 14),
            VideoSetDisplayBrightness = (EFileDevice.Video << 16) | (0x0127 << 2) | EMethod.Buffered | (0 << 14)
        }
        #endregion

        #region constants
        public const Int32 INVALID_HANDLE_VALUE = -1;

        public const UInt32 GENERIC_READ = 0x80000000;
        public const UInt32 GENERIC_WRITE = 0x40000000;
        public const UInt32 FILE_SHARE_READ = 0x00000001;
        public const UInt32 FILE_SHARE_WRITE = 0x00000002;
        public const UInt32 FILE_ATTRIBUTE_DIRECTORY = 0x00000010;

        public const UInt32 CREATE_NEW = 1;
        public const UInt32 CREATE_ALWAYS = 2;
        public const UInt32 OPEN_EXISTING = 3;
        public const UInt32 OPEN_ALWAYS = 4;
        public const UInt32 TRUNCATE_EXISTING = 5;

        public const UInt32 FILE_ATTRIBUTE_NORMAL = 0x80;
        public const UInt32 FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
        public const UInt32 FileNameInformationClass = 9;
        public const UInt32 FILE_OPEN_FOR_BACKUP_INTENT = 0x4000;
        public const UInt32 FILE_OPEN_BY_FILE_ID = 0x2000;
        public const UInt32 FILE_OPEN = 0x1;
        public const UInt32 OBJ_CASE_INSENSITIVE = 0x40;
        //public const OBJ_KERNEL_HANDLE = 0x200;

        // CTL_CODE( DeviceType, Function, Method, Access ) (((DeviceType) << 16) | ((Access) << 14) | ((Function) << 2) | (Method))
        private const UInt32 FILE_DEVICE_FILE_SYSTEM = 0x00000009;
        private const UInt32 METHOD_NEITHER = 3;
        private const UInt32 METHOD_BUFFERED = 0;
        private const UInt32 FILE_ANY_ACCESS = 0;
        private const UInt32 FILE_SPECIAL_ACCESS = 0;
        private const UInt32 FILE_READ_ACCESS = 1;
        private const UInt32 FILE_WRITE_ACCESS = 2;

        public const UInt32 USN_REASON_DATA_OVERWRITE = 0x00000001;
        public const UInt32 USN_REASON_DATA_EXTEND = 0x00000002;
        public const UInt32 USN_REASON_DATA_TRUNCATION = 0x00000004;
        public const UInt32 USN_REASON_NAMED_DATA_OVERWRITE = 0x00000010;
        public const UInt32 USN_REASON_NAMED_DATA_EXTEND = 0x00000020;
        public const UInt32 USN_REASON_NAMED_DATA_TRUNCATION = 0x00000040;
        public const UInt32 USN_REASON_FILE_CREATE = 0x00000100;
        public const UInt32 USN_REASON_FILE_DELETE = 0x00000200;
        public const UInt32 USN_REASON_EA_CHANGE = 0x00000400;
        public const UInt32 USN_REASON_SECURITY_CHANGE = 0x00000800;
        public const UInt32 USN_REASON_RENAME_OLD_NAME = 0x00001000;
        public const UInt32 USN_REASON_RENAME_NEW_NAME = 0x00002000;
        public const UInt32 USN_REASON_INDEXABLE_CHANGE = 0x00004000;
        public const UInt32 USN_REASON_BASIC_INFO_CHANGE = 0x00008000;
        public const UInt32 USN_REASON_HARD_LINK_CHANGE = 0x00010000;
        public const UInt32 USN_REASON_COMPRESSION_CHANGE = 0x00020000;
        public const UInt32 USN_REASON_ENCRYPTION_CHANGE = 0x00040000;
        public const UInt32 USN_REASON_OBJECT_ID_CHANGE = 0x00080000;
        public const UInt32 USN_REASON_REPARSE_POINT_CHANGE = 0x00100000;
        public const UInt32 USN_REASON_STREAM_CHANGE = 0x00200000;
        public const UInt32 USN_REASON_CLOSE = 0x80000000;

        public static Int32 GWL_EXSTYLE = -20;
        public static Int32 WS_EX_LAYERED = 0x00080000;
        public static Int32 WS_EX_TRANSPARENT = 0x00000020;

        public const UInt32 FSCTL_GET_OBJECT_ID = 0x9009c;

        // FSCTL_ENUM_USN_DATA = CTL_CODE(FILE_DEVICE_FILE_SYSTEM, 44,  METHOD_NEITHER, FILE_ANY_ACCESS)
        public const UInt32 FSCTL_ENUM_USN_DATA = (FILE_DEVICE_FILE_SYSTEM << 16) | (FILE_ANY_ACCESS << 14) | (44 << 2) | METHOD_NEITHER;

        // FSCTL_READ_USN_JOURNAL = CTL_CODE(FILE_DEVICE_FILE_SYSTEM, 46,  METHOD_NEITHER, FILE_ANY_ACCESS)
        public const UInt32 FSCTL_READ_USN_JOURNAL = (FILE_DEVICE_FILE_SYSTEM << 16) | (FILE_ANY_ACCESS << 14) | (46 << 2) | METHOD_NEITHER;

        //  FSCTL_CREATE_USN_JOURNAL        CTL_CODE(FILE_DEVICE_FILE_SYSTEM, 57,  METHOD_NEITHER, FILE_ANY_ACCESS)
        public const UInt32 FSCTL_CREATE_USN_JOURNAL = (FILE_DEVICE_FILE_SYSTEM << 16) | (FILE_ANY_ACCESS << 14) | (57 << 2) | METHOD_NEITHER;

        //  FSCTL_QUERY_USN_JOURNAL         CTL_CODE(FILE_DEVICE_FILE_SYSTEM, 61, METHOD_BUFFERED, FILE_ANY_ACCESS)
        public const UInt32 FSCTL_QUERY_USN_JOURNAL = (FILE_DEVICE_FILE_SYSTEM << 16) | (FILE_ANY_ACCESS << 14) | (61 << 2) | METHOD_BUFFERED;

        // FSCTL_DELETE_USN_JOURNAL        CTL_CODE(FILE_DEVICE_FILE_SYSTEM, 62, METHOD_BUFFERED, FILE_ANY_ACCESS)
        public const UInt32 FSCTL_DELETE_USN_JOURNAL = (FILE_DEVICE_FILE_SYSTEM << 16) | (FILE_ANY_ACCESS << 14) | (62 << 2) | METHOD_BUFFERED;

        #endregion

        #region dll imports

        /// <summary>
        /// Creates the file specified by 'lpFileName' with desired access, share mode, security attributes,
        /// creation disposition, flags and attributes.
        /// </summary>
        /// <param name="lpFileName">Fully qualified path to a file</param>
        /// <param name="dwDesiredAccess">Requested access (write, read, read/write, none)</param>
        /// <param name="dwShareMode">Share mode (read, write, read/write, delete, all, none)</param>
        /// <param name="lpSecurityAttributes">IntPtr to a 'SECURITY_ATTRIBUTES' structure</param>
        /// <param name="dwCreationDisposition">Action to take on file or device specified by 'lpFileName' (CREATE_NEW,
        /// CREATE_ALWAYS, OPEN_ALWAYS, OPEN_EXISTING, TRUNCATE_EXISTING)</param>
        /// <param name="dwFlagsAndAttributes">File or device attributes and flags (typically FILE_ATTRIBUTE_NORMAL)</param>
        /// <param name="hTemplateFile">IntPtr to a valid handle to a template file with 'GENERIC_READ' access right</param>
        /// <returns>IntPtr handle to the 'lpFileName' file or device or 'INVALID_HANDLE_VALUE'</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr 
            CreateFile(string lpFileName, 
            uint dwDesiredAccess,
			uint dwShareMode, 
            IntPtr lpSecurityAttributes, 
            uint dwCreationDisposition,
			uint dwFlagsAndAttributes, 
            IntPtr hTemplateFile);

        /// <summary>
        /// Closes the file specified by the IntPtr 'hObject'.
        /// </summary>
        /// <param name="hObject">IntPtr handle to a file</param>
        /// <returns>'true' if successful, otherwise 'false'</returns>
		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool 
            CloseHandle(
            IntPtr hObject);

        /// <summary>
        /// Fills the 'BY_HANDLE_FILE_INFORMATION' structure for the file specified by 'hFile'.
        /// </summary>
        /// <param name="hFile">Fully qualified name of a file</param>
        /// <param name="lpFileInformation">Out BY_HANDLE_FILE_INFORMATION argument</param>
        /// <returns>'true' if successful, otherwise 'false'</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool 
            GetFileInformationByHandle(
            IntPtr hFile,
            out BY_HANDLE_FILE_INFORMATION lpFileInformation);
        
        /// <summary>
        /// Deletes the file specified by 'fileName'.
        /// </summary>
        /// <param name="fileName">Fully qualified path to the file to delete</param>
        /// <returns>'true' if successful, otherwise 'false'</returns>
		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DeleteFile(
            string fileName);

        /// <summary>
        /// Read data from the file specified by 'hFile'.
        /// </summary>
        /// <param name="hFile">IntPtr handle to the file to read</param>
        /// <param name="lpBuffer">IntPtr to a buffer of bytes to receive the bytes read from 'hFile'</param>
        /// <param name="nNumberOfBytesToRead">Number of bytes to read from 'hFile'</param>
        /// <param name="lpNumberOfBytesRead">Number of bytes read from 'hFile'</param>
        /// <param name="lpOverlapped">IntPtr to an 'OVERLAPPED' structure</param>
        /// <returns>'true' if successful, otherwise 'false'</returns>
		[DllImport("kernel32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ReadFile(
            IntPtr hFile, 
            IntPtr lpBuffer,
			uint nNumberOfBytesToRead, 
            out uint lpNumberOfBytesRead, 
            IntPtr lpOverlapped);

        /// <summary>
        /// Writes the 
        /// </summary>
        /// <param name="hFile">IntPtr handle to the file to write</param>
        /// <param name="bytes">IntPtr to a buffer of bytes to write to 'hFile'</param>
        /// <param name="nNumberOfBytesToWrite">Number of bytes in 'lpBuffer' to write to 'hFile'</param>
        /// <param name="lpNumberOfBytesWritten">Number of bytes written to 'hFile'</param>
        /// <param name="overlapped">IntPtr to an 'OVERLAPPED' structure</param>
        /// <returns>'true' if successful, otherwise 'false'</returns>
		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool WriteFile(
            IntPtr hFile, 
            IntPtr bytes,
			uint nNumberOfBytesToWrite, 
            out uint lpNumberOfBytesWritten,
			int overlapped);

        /// <summary>
        /// Writes the data in 'lpBuffer' to the file specified by 'hFile'.
        /// </summary>
        /// <param name="hFile">IntPtr handle to file to write</param>
        /// <param name="lpBuffer">Buffer of bytes to write to file 'hFile'</param>
        /// <param name="nNumberOfBytesToWrite">Number of bytes in 'lpBuffer' to write to 'hFile'</param>
        /// <param name="lpNumberOfBytesWritten">Number of bytes written to 'hFile'</param>
        /// <param name="overlapped">IntPtr to an 'OVERLAPPED' structure</param>
        /// <returns>'true' if successful, otherwise 'false'</returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WriteFile(
            IntPtr hFile,
            byte[] lpBuffer,
            uint nNumberOfBytesToWrite,
            out uint lpNumberOfBytesWritten,
            int overlapped);

		/*
        /// <summary>
        /// Sends the 'dwIoControlCode' to the device specified by 'hDevice'.
        /// </summary>
        /// <param name="hDevice">IntPtr handle to the device to receive 'dwIoControlCode'</param>
        /// <param name="dwIoControlCode">Device IO Control Code to send</param>
        /// <param name="lpInBuffer">Input buffer if required</param>
        /// <param name="nInBufferSize">Size of input buffer</param>
        /// <param name="lpOutBuffer">Output buffer if required</param>
        /// <param name="nOutBufferSize">Size of output buffer</param>
        /// <param name="lpBytesReturned">Number of bytes returned in output buffer</param>
        /// <param name="lpOverlapped">IntPtr to an 'OVERLAPPED' structure</param>
        /// <returns>'true' if successful, otherwise 'false'</returns>
        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeviceIoControl(
            IntPtr hDevice,
            UInt32 dwIoControlCode,
            IntPtr lpInBuffer, 
            Int32 nInBufferSize,
            out USN_JOURNAL_DATA lpOutBuffer, 
            Int32 nOutBufferSize,
            out uint lpBytesReturned, 
            IntPtr lpOverlapped);

        /// <summary>
        /// Sends the control code 'dwIoControlCode' to the device driver specified by 'hDevice'.
        /// </summary>
        /// <param name="hDevice">IntPtr handle to the device to receive 'dwIoControlCode</param>
        /// <param name="dwIoControlCode">Device IO Control Code to send</param>
        /// <param name="lpInBuffer">Input buffer if required</param>
        /// <param name="nInBufferSize">Size of input buffer </param>
        /// <param name="lpOutBuffer">Output buffer if required</param>
        /// <param name="nOutBufferSize">Size of output buffer</param>
        /// <param name="lpBytesReturned">Number of bytes returned</param>
        /// <param name="lpOverlapped">Pointer to an 'OVERLAPPED' struture</param>
        /// <returns></returns>
        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeviceIoControl(
            IntPtr hDevice,
            UInt32 dwIoControlCode,
            ref IntPtr lpInBuffer, 
            Int32 nInBufferSize,
            IntPtr lpOutBuffer, 
            Int32 nOutBufferSize,
            out uint lpBytesReturned, 
            IntPtr lpOverlapped);
*/

		 /// <summary>
        /// Sends the dwIoControlCode to the device specified by hDevice.
        /// </summary>
        /// <param name="hDevice">Safe handle to the device </param>
        /// <param name="dwIoControlCode">Device IO Control Code to send</param>
        /// <param name="lpInBuffer">Input buffer if required</param>
        /// <param name="nInBufferSize">Size of input buffer</param>
        /// <param name="lpOutBuffer">Output buffer if required</param>
        /// <param name="nOutBufferSize">Size of output buffer</param>
        /// <param name="lpBytesReturned">Number of bytes returned in output buffer</param>
        /// <param name="lpOverlapped">IntPtr to an 'OVERLAPPED' structure</param>
        /// <returns>'true' if successful, otherwise 'false'</returns>
       /* [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool DeviceIoControl(
            SafeFileHandle hDevice,
            EIOControlCode IoControlCode,
            [In] READ_USN_JOURNAL_DATA InBuffer,
            uint nInBufferSize,
            [In] IntPtr OutBuffer,
            uint nOutBufferSize,
            ref uint pBytesReturned,
            [In] IntPtr overlapped //[In] ref System.Threading.NativeOverlapped Overlapped
        );*/

        /// <summary>
        /// Sends the dwIoControlCode to the device specified by hDevice.
        /// </summary>
        /// <param name="hDevice">Safe handle to the device </param>
        /// <param name="dwIoControlCode">Device IO Control Code to send</param>
        /// <param name="lpInBuffer">Input buffer if required</param>
        /// <param name="nInBufferSize">Size of input buffer</param>
        /// <param name="lpOutBuffer">Output buffer if required</param>
        /// <param name="nOutBufferSize">Size of output buffer</param>
        /// <param name="lpBytesReturned">Number of bytes returned in output buffer</param>
        /// <param name="lpOverlapped">IntPtr to an 'OVERLAPPED' structure</param>
        /// <returns>'true' if successful, otherwise 'false'</returns>
        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool DeviceIoControl(
            SafeFileHandle hDevice,
            EIOControlCode IoControlCode,
            [In] ref MFT_ENUM_DATA InBuffer,
            uint nInBufferSize,
            [In] IntPtr OutBuffer,
            uint nOutBufferSize,
            ref uint pBytesReturned,
            [In] IntPtr overlapped //[In] ref System.Threading.NativeOverlapped Overlapped
        );

        /// <summary>
        /// Sends the dwIoControlCode to the device specified by hDevice.
        /// </summary>
        /// <param name="hDevice">Safe handle to the device </param>
        /// <param name="dwIoControlCode">Device IO Control Code to send</param>
        /// <param name="lpInBuffer">Input buffer if required</param>
        /// <param name="nInBufferSize">Size of input buffer</param>
        /// <param name="lpOutBuffer">Output buffer if required</param>
        /// <param name="nOutBufferSize">Size of output buffer</param>
        /// <param name="lpBytesReturned">Number of bytes returned in output buffer</param>
        /// <param name="lpOverlapped">IntPtr to an 'OVERLAPPED' structure</param>
        /// <returns>'true' if successful, otherwise 'false'</returns>
        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool DeviceIoControl(
            SafeFileHandle hDevice,
            EIOControlCode IoControlCode,
            [MarshalAs(UnmanagedType.AsAny)]
            [In] object InBuffer,
            uint nInBufferSize,
            [Out] out USN_JOURNAL_DATA OutBuffer,
            uint nOutBufferSize,
            ref uint pBytesReturned,
            [In] IntPtr overlapped //[In] ref System.Threading.NativeOverlapped Overlapped
        );



        /// <summary>
        /// Sets the number of bytes specified by 'size' of the memory associated with the argument 'ptr' 
        /// to zero.
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="size"></param>
        [DllImport("kernel32.dll")]
		public static extern void ZeroMemory(IntPtr ptr, int size);

        /// <summary>
        /// Retrieves the cursor's position, in screen coordinates.
        /// </summary>
        /// <param name="pt">Pointer to a POINT structure that receives the screen coordinates of the cursor</param>
        /// <returns>Returns nonzero if successful or zero otherwise. To get extended error information, call GetLastError.</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(out POINT pt);

        /// <summary>
        /// retrieves information about the specified window. The function also retrieves the 32-bit (long) 
        /// value at the specified offset into the extra window memory.
        /// </summary>
        /// <param name="hWnd">Handle to the window and, indirectly, the class to which the window belongs</param>
        /// <param name="nIndex">the zero-based offset to the value to be retrieved</param>
        /// <returns>If the function succeeds, the return value is the requested 32-bit value.
        /// If the function fails, the return value is zero. To get extended error information, call GetLastError
        ///</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern Int32 GetWindowLong(IntPtr hWnd, Int32 nIndex);

        /// <summary>
        /// changes an attribute of the specified window. The function also sets the 32-bit (long) value at 
        /// the specified offset into the extra window memory
        /// </summary>
        /// <param name="hWnd">Handle to the window and, indirectly, the class to which the window belongs</param>
        /// <param name="nIndex">the zero-based offset to the value to be set</param>
        /// <param name="newVal">the replacement value</param>
        /// <returns>If the function succeeds, the return value is the previous value of the specified 32-bit 
        /// integer. If the function fails, the return value is zero. To get extended error information, call GetLastError.
        /// </returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern Int32 SetWindowLong(IntPtr hWnd, Int32 nIndex, Int32 newVal);

        /// <summary>
        /// Creates a new file or directory, or opens an existing file, device, directory, or volume
        /// </summary>
        /// <param name="handle">A pointer to a variable that receives the file handle if the call is successful (out)</param>
        /// <param name="access">ACCESS_MASK value that expresses the type of access that the caller requires to the file or directory (in)</param>
        /// <param name="objectAttributes">A pointer to a structure already initialized with InitializeObjectAttributes (in)</param>
        /// <param name="ioStatus">A pointer to a variable that receives the final completion status and information about the requested operation (out)</param>
        /// <param name="allocSize">The initial allocation size in bytes for the file (in)(optional)</param>
        /// <param name="fileAttributes">file attributes (in)</param>
        /// <param name="share">type of share access that the caller would like to use in the file (in)</param>
        /// <param name="createDisposition">what to do, depending on whether the file already exists (in)</param>
        /// <param name="createOptions">options to be applied when creating or opening the file (in)</param>
        /// <param name="eaBuffer">Pointer to an EA buffer used to pass extended attributes (in)</param>
        /// <param name="eaLength">Length of the EA buffer</param>
        /// <returns>either STATUS_SUCCESS or an appropriate error status. If it returns an error status, the caller can find more information about the cause of the failure by checking the IoStatusBlock</returns>
        [DllImport("ntdll.dll", ExactSpelling = true, SetLastError = true)]
        public static extern int NtCreateFile(
            ref IntPtr handle, 
            FileAccess access, 
            ref OBJECT_ATTRIBUTES objectAttributes, 
            ref IO_STATUS_BLOCK ioStatus, 
            ref long allocSize, 
            uint fileAttributes, 
            FileShare share, 
            uint createDisposition, 
            uint createOptions, 
            IntPtr eaBuffer, 
            uint eaLength);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileHandle"></param>
        /// <param name="IoStatusBlock"></param>
        /// <param name="pInfoBlock"></param>
        /// <param name="length"></param>
        /// <param name="fileInformation"></param>
        /// <returns></returns>
        [DllImport("ntdll.dll", ExactSpelling = true, SetLastError = true)]
        public static extern int NtQueryInformationFile(
            IntPtr fileHandle, 
            ref IO_STATUS_BLOCK IoStatusBlock, 
            IntPtr pInfoBlock, 
            uint length, 
            FILE_INFORMATION_CLASS fileInformation);

        #endregion

        #region structures

        /// <summary>
        /// By Handle File Information structure, contains File Attributes(32bits), Creation Time(FILETIME),
        /// Last Access Time(FILETIME), Last Write Time(FILETIME), Volume Serial Number(32bits),
        /// File Size High(32bits), File Size Low(32bits), Number of Links(32bits), File Index High(32bits),
        /// File Index Low(32bits).
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BY_HANDLE_FILE_INFORMATION
        {
            public uint FileAttributes;
            public FILETIME CreationTime;
            public FILETIME LastAccessTime;
            public FILETIME LastWriteTime;
            public uint VolumeSerialNumber;
            public uint FileSizeHigh;
            public uint FileSizeLow;
            public uint NumberOfLinks;
            public uint FileIndexHigh;
            public uint FileIndexLow;
        }

        /// <summary>
        /// USN Journal Data structure, contains USN Journal ID(64bits), First USN(64bits), Next USN(64bits),
        /// Lowest Valid USN(64bits), Max USN(64bits), Maximum Size(64bits) and Allocation Delta(64bits).
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct USN_JOURNAL_DATA
        {
            public UInt64 UsnJournalID;
            public Int64 FirstUsn;
            public Int64 NextUsn;
            public Int64 LowestValidUsn;
            public Int64 MaxUsn;
            public UInt64 MaximumSize;
            public UInt64 AllocationDelta;
        }

        /// <summary>
        /// MFT Enum Data structure, contains Start File Reference Number(64bits), Low USN(64bits),
        /// High USN(64bits).
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct MFT_ENUM_DATA
        {
            public UInt64 StartFileReferenceNumber;
            public Int64 LowUsn;
            public Int64 HighUsn;
        }

        /// <summary>
        /// Create USN Journal Data structure, contains Maximum Size(64bits) and Allocation Delta(64(bits).
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CREATE_USN_JOURNAL_DATA
        {
            public UInt64 MaximumSize;
            public UInt64 AllocationDelta;
        }

        /// <summary>
        /// Create USN Journal Data structure, contains Maximum Size(64bits) and Allocation Delta(64(bits).
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DELETE_USN_JOURNAL_DATA
        {
            public UInt64 UsnJournalID;
            public UInt32 DeleteFlags;
            public UInt32 Reserved;
        }

        /// <summary>
        /// Contains the USN Record Length(32bits), USN(64bits), File Reference Number(64bits), 
        /// Parent File Reference Number(64bits), Reason Code(32bits), File Attributes(32bits),
        /// File Name Length(32bits), the File Name Offset(32bits) and the File Name.
        /// </summary>
        public class UsnEntry : IComparable<UsnEntry>
        {
            private const int FR_OFFSET = 8;
            private const int PFR_OFFSET = 16;
            private const int USN_OFFSET = 24;
            private const int REASON_OFFSET = 40;
            public const int FA_OFFSET = 52;
            private const int FNL_OFFSET = 56;
            private const int FN_OFFSET = 58;

            private UInt32 _recordLength;
            public UInt32 RecordLength
            {
                get { return _recordLength; }
            }

            private Int64 _usn;
            public Int64 USN
            {
                get { return _usn; }
            }

            private UInt64 _frn;
            public UInt64 FileReferenceNumber
            {
                get { return _frn; }
            }

            private UInt64 _pfrn;
            public UInt64 ParentFileReferenceNumber
            {
                get { return _pfrn; }
            }

            private UInt32 _reason;
            public UInt32 Reason
            {
                get { return _reason; }
            }

            private string _name;
            public string Name
            {
                get
                {
                    return _name;
                }
            }

            private string _oldName;
            public string OldName
            {
                get 
                {
                    if (0 != (_fileAttributes & USN_REASON_RENAME_OLD_NAME))
                    {
                        return _oldName;
                    }
                    else
                    {
                        return null;
                    }
                }
                set { _oldName = value; }
            }

            private UInt32 _fileAttributes;
            public bool IsFolder
            {
                get
                {
                    bool bRtn = false;
                    if (0 != (_fileAttributes & Win32Api.FILE_ATTRIBUTE_DIRECTORY))
                    {
                        bRtn = true;
                    }
                    return bRtn;
                }
            }

            public bool IsFile
            {
                get
                {
                    bool bRtn = false;
                    if (0 == (_fileAttributes & Win32Api.FILE_ATTRIBUTE_DIRECTORY))
                    {
                        bRtn = true;
                    }
                    return bRtn;
                }
            }
			
			public uint Attributes{
				get{ return _fileAttributes;}
			}
                
             /// <summary>
            /// USN Record Constructor
            /// </summary>
            /// <param name="p">Buffer pointer to first byte of the USN Record</param>
            public UsnEntry(IntPtr ptrToUsnRecord)
            {
                _recordLength = (UInt32)Marshal.ReadInt32(ptrToUsnRecord);
                _frn = (UInt64)Marshal.ReadInt64(ptrToUsnRecord, FR_OFFSET);
                _pfrn = (UInt64)Marshal.ReadInt64(ptrToUsnRecord, PFR_OFFSET);
                _usn = (Int64)Marshal.ReadInt64(ptrToUsnRecord, USN_OFFSET);
                _reason = (UInt32)Marshal.ReadInt32(ptrToUsnRecord, REASON_OFFSET);
                _fileAttributes = (UInt32)Marshal.ReadInt32(ptrToUsnRecord, FA_OFFSET);
                short fileNameLength = Marshal.ReadInt16(ptrToUsnRecord, FNL_OFFSET);
                short fileNameOffset = Marshal.ReadInt16(ptrToUsnRecord, FN_OFFSET);
                _name = Marshal.PtrToStringUni(new IntPtr(ptrToUsnRecord.ToInt32() + fileNameOffset), fileNameLength / sizeof(char));
            }



            #region IComparable<UsnEntry> Members

            public int CompareTo(UsnEntry other)
            {
                return string.Compare(this.Name, other.Name, true);
            }

            #endregion
        }

        /// <summary>
        /// Contains the Start USN(64bits), Reason Mask(32bits), Return Only on Close flag(32bits),
        /// Time Out(64bits), Bytes To Wait For(64bits), and USN Journal ID(64bits).
        /// </summary>
        /// <remarks> possible reason bits are from Win32Api
        /// USN_REASON_DATA_OVERWRITE
        /// USN_REASON_DATA_EXTEND
        /// USN_REASON_DATA_TRUNCATION
        /// USN_REASON_NAMED_DATA_OVERWRITE
        /// USN_REASON_NAMED_DATA_EXTEND
        /// USN_REASON_NAMED_DATA_TRUNCATION
        /// USN_REASON_FILE_CREATE
        /// USN_REASON_FILE_DELETE
        /// USN_REASON_EA_CHANGE
        /// USN_REASON_SECURITY_CHANGE
        /// USN_REASON_RENAME_OLD_NAME
        /// USN_REASON_RENAME_NEW_NAME
        /// USN_REASON_INDEXABLE_CHANGE
        /// USN_REASON_BASIC_INFO_CHANGE
        /// USN_REASON_HARD_LINK_CHANGE
        /// USN_REASON_COMPRESSION_CHANGE
        /// USN_REASON_ENCRYPTION_CHANGE
        /// USN_REASON_OBJECT_ID_CHANGE
        /// USN_REASON_REPARSE_POINT_CHANGE
        /// USN_REASON_STREAM_CHANGE
        /// USN_REASON_CLOSE
        /// </remarks>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct READ_USN_JOURNAL_DATA
        {
            public Int64 StartUsn;
            public UInt32 ReasonMask;
            public UInt32 ReturnOnlyOnClose;
            public UInt64 Timeout;
            public UInt64 bytesToWaitFor;
            public UInt64 UsnJournalId;
        }

		/* [StructLayout(LayoutKind.Sequential)]
        public struct MFT_ENUM_DATA
        {
            public ulong StartFileReferenceNumber;
            public long LowUsn;
            public long HighUsn;
        }*/

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct IO_STATUS_BLOCK
        {
            public uint status;
            public ulong information;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct OBJECT_ATTRIBUTES
        {
            public Int32 Length;
            public IntPtr RootDirectory;
            public IntPtr ObjectName;
            public Int32 Attributes;
            public Int32 SecurityDescriptor;
            public Int32 SecurityQualityOfService;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct UNICODE_STRING
        {
            public Int16 Length;
            public Int16 MaximumLength;
            public IntPtr Buffer;
        }

        #endregion

        #region functions

        /*
        public static string GetPathFromFileReference(IntPtr rootIntPtr, UInt64 frn)
        {
            string name = string.Empty;

            long allocSize = 0;
            UNICODE_STRING unicodeString;
            OBJECT_ATTRIBUTES objAttributes = new OBJECT_ATTRIBUTES();
            IO_STATUS_BLOCK ioStatusBlock = new IO_STATUS_BLOCK();
            IntPtr hFile;

            IntPtr buffer = Marshal.AllocHGlobal(4096);
            IntPtr refPtr = Marshal.AllocHGlobal(8);
            IntPtr objAttIntPtr = Marshal.AllocHGlobal(Marshal.SizeOf(objAttributes));

            //
            // pointer >> fileid
            //
            Marshal.WriteInt64(refPtr, (long)frn);

            unicodeString.Length = 8;
            unicodeString.MaximumLength = 8;
            unicodeString.Buffer = refPtr;
            //
            // copy unicode structure to pointer
            //
            Marshal.StructureToPtr(unicodeString, objAttIntPtr, true);

            //
            //  InitializeObjectAttributes 
            //
            objAttributes.Length = Marshal.SizeOf(objAttributes);
            objAttributes.ObjectName = objAttIntPtr;
            objAttributes.RootDirectory = rootIntPtr;
            objAttributes.Attributes = (int)OBJ_CASE_INSENSITIVE;

            int fOk = NtCreateFile(out hFile, 0, ref objAttributes, ref ioStatusBlock, ref allocSize, 0,
                FileShare.ReadWrite,
                FILE_OPEN, FILE_OPEN_BY_FILE_ID | FILE_OPEN_FOR_BACKUP_INTENT, IntPtr.Zero, 0);
            if (fOk.ToInt32() == 0)
            {
                fOk = NtQueryInformationFile(hFile, ref ioStatusBlock, buffer, 4096, FILE_INFORMATION_CLASS.FileNameInformation);
                if (fOk.ToInt32() == 0)
                {
                    //
                    // first 4 bytes are the name length
                    //
                    int nameLength = Marshal.ReadInt32(buffer, 0);
                    //
                    // next bytes are the name
                    //
                    name = Marshal.PtrToStringUni(new IntPtr(buffer.ToInt32() + 4), nameLength / 2);
                }
            }
            hFile.Close();
            Marshal.FreeHGlobal(buffer);
            Marshal.FreeHGlobal(objAttIntPtr);
            Marshal.FreeHGlobal(refPtr);
            return name;
        }
        */

        /// <summary>
        /// Writes the data in 'text' to the alternate stream ':Description' of the file 'currentFile.
        /// </summary>
        /// <param name="currentfile">Fully qualified path to a file</param>
        /// <param name="text">Data to write to the ':Description' stream</param>
        public static void WriteAlternateStream(string currentfile, string text)
		{
			string AltStreamDesc = currentfile + ":Description";
            IntPtr txtBuffer = IntPtr.Zero;
            IntPtr hFile = IntPtr.Zero;
			DeleteFile(AltStreamDesc);
            string descText = text.TrimEnd(' ');

            try
            {
                hFile = CreateFile(AltStreamDesc, GENERIC_WRITE, 0, IntPtr.Zero,
                                       CREATE_ALWAYS, 0, IntPtr.Zero);
                if (-1 != hFile.ToInt32())
                {
                    txtBuffer = Marshal.StringToHGlobalUni(descText);
                    uint nBytes, count;
                    nBytes = (uint)descText.Length;
                    bool bRtn = WriteFile(hFile, txtBuffer, sizeof(char) * nBytes, out count, 0);
                    if (!bRtn)
                    {
                        if ((sizeof(char) * nBytes) != count)
                        {
                            throw new Exception(string.Format("Bytes written {0} should be {1} for file {2}.",
                                count, sizeof(char) * nBytes, AltStreamDesc));
                        }
                        else
                        {
                            throw new Exception("WriteFile() returned false");
                        }
                    }
                }
                else
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            catch (Exception exception)
            {
                string msg = string.Format("Exception caught in WriteAlternateStream()\n  '{0}'\n  for file '{1}'.", 
                    exception.Message, AltStreamDesc);
                Console.WriteLine(msg);
            }
            finally
            {
                CloseHandle(hFile);
                hFile = IntPtr.Zero;
                Marshal.FreeHGlobal(txtBuffer);
                GC.Collect();
            }
		}

        /// <summary>
        /// Adds the ':Description' alternate stream name to the argument 'currentFile'.
        /// </summary>
        /// <param name="currentfile">The file whose alternate stream is to be read</param>
        /// <returns>A string value representing the value of the alternate stream</returns>
		public static string ReadAlternateStream(string currentfile)
		{
			string AltStreamDesc = currentfile + ":Description";
			string returnstring = ReadAlternateStreamEx(AltStreamDesc);
			return returnstring;
		}

        /// <summary>
        /// Reads the stream represented by 'currentFile'.
        /// </summary>
        /// <param name="currentfile">Fully qualified path including stream</param>
        /// <returns>Value of the alternate stream as a string</returns>
		public static string ReadAlternateStreamEx(string currentfile)
		{
			string returnstring = string.Empty;
            IntPtr hFile = IntPtr.Zero;
            IntPtr buffer = IntPtr.Zero;
            try
            {
                hFile = CreateFile(currentfile, GENERIC_READ, 0, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
                if (-1 != hFile.ToInt32())
                {
                    buffer = Marshal.AllocHGlobal(1000 * sizeof(char));
                    ZeroMemory(buffer, 1000 * sizeof(char));
                    uint nBytes;
                    bool bRtn = ReadFile(hFile, buffer, 1000 * sizeof(char), out nBytes, IntPtr.Zero);
                    if (bRtn)
                    {
                        if (nBytes > 0)
                        {
                            returnstring = Marshal.PtrToStringAuto(buffer);
                            //byte[] byteBuffer = new byte[nBytes];
                            //for (int i = 0; i < nBytes; i++)
                            //{
                            //    byteBuffer[i] = Marshal.ReadByte(buffer, i);
                            //}
                            //returnstring = Encoding.Unicode.GetString(byteBuffer, 0, (int)nBytes);
                        }
                        else
                        {
                            throw new Exception("ReadFile() returned true but read zero bytes");
                        }
                    }
                    else
                    {
                        if (nBytes <= 0)
                        {
                            throw new Exception("ReadFile() read zero bytes.");
                        }
                        else
                        {
                            throw new Exception("ReadFile() returned false");
                        }
                    }
                }
                else
                {
                    Exception excptn = new Win32Exception(Marshal.GetLastWin32Error());
                    if (!excptn.Message.Contains("cannot find the file"))
                    {
                        throw excptn;
                    }
                }
            }
            catch (Exception exception)
            {
                string msg = string.Format("Exception caught in ReadAlternateStream(), '{0}'\n  for file '{1}'.", 
                    exception.Message, currentfile);
                Console.WriteLine(msg);
                Console.WriteLine(exception.Message);
            }
            finally
            {
                CloseHandle(hFile);
                hFile = IntPtr.Zero;
                if (buffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(buffer);
                }
                GC.Collect();
            }
			return returnstring;
		}

        /// <summary>
        /// Read the encrypted alternate stream specified by 'currentFile'.
        /// </summary>
        /// <param name="currentfile">Fully qualified path to encrypted alternate stream</param>
        /// <returns>The un-encrypted value of the alternate stream as a string</returns>
		public static string ReadAlternateStreamEncrypted(string currentfile)
		{
			string returnstring = string.Empty;
            IntPtr buffer = IntPtr.Zero;
            IntPtr hFile = IntPtr.Zero;
            try
            {
                hFile = CreateFile(currentfile, GENERIC_READ, 0, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
                if (-1 != hFile.ToInt32())
                {
                    buffer = Marshal.AllocHGlobal(1000 * sizeof(char));
                    ZeroMemory(buffer, 1000 * sizeof(char));
                    uint nBytes;
                    bool bRtn = ReadFile(hFile, buffer, 1000 * sizeof(char), out nBytes, IntPtr.Zero);
                    if (0 != nBytes)
                    {
                        returnstring = DecryptLicenseString(buffer, nBytes);
                    }
                }
                else
                {
                    Exception excptn = new Win32Exception(Marshal.GetLastWin32Error());
                    if (!excptn.Message.Contains("cannot find the file"))
                    {
                        throw excptn;
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Exception caught in ReadAlternateStreamEncrypted()");
                Console.WriteLine(exception.Message);
            }
            finally
            {
                CloseHandle(hFile);
                hFile = IntPtr.Zero;
                if (buffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(buffer);
                }
                GC.Collect();
            }
			return returnstring;
		}

        /// <summary>
        /// Writes the value of 'LicenseString' as an encrypted stream to the file:stream specified
        /// by 'currentFile'.
        /// </summary>
        /// <param name="currentFile">Fully qualified path to the alternate stream</param>
        /// <param name="LicenseString">The string value to encrypt and write to the alternate stream</param>
		public static void WriteAlternateStreamEncrypted(string currentFile, string LicenseString)
		{
			RC2CryptoServiceProvider rc2 = null;
			CryptoStream cs = null;
			MemoryStream ms = null;
			uint count = 0;
            IntPtr buffer = IntPtr.Zero;
            IntPtr hFile = IntPtr.Zero;
			try {
				Encoding enc = Encoding.Unicode;

				byte[] ba = enc.GetBytes(LicenseString);
				ms = new MemoryStream();

				rc2 = new RC2CryptoServiceProvider();
				rc2.Key = GetBytesFromHexString("7a6823a42a3a3ae27057c647db812d0");
				rc2.IV = GetBytesFromHexString("827d961224d99b2d");

				cs = new CryptoStream(ms, rc2.CreateEncryptor(), CryptoStreamMode.Write);
				cs.Write(ba, 0, ba.Length);
				cs.FlushFinalBlock();

                buffer = Marshal.AllocHGlobal(1000 * sizeof(char));
				ZeroMemory(buffer, 1000 * sizeof(char));
				uint nBytes = (uint)ms.Length;
				Marshal.Copy(ms.GetBuffer(), 0, buffer, (int)nBytes);

				DeleteFile(currentFile);
				hFile = CreateFile(currentFile, GENERIC_WRITE, 0, IntPtr.Zero,
									   CREATE_ALWAYS, 0, IntPtr.Zero);
				if (-1 != hFile.ToInt32()) 
                {
					bool bRtn = WriteFile(hFile, buffer, nBytes, out count, 0);
				} 
                else 
                {
                    Exception excptn = new Win32Exception(Marshal.GetLastWin32Error());
                    if (!excptn.Message.Contains("cannot find the file"))
                    {
                        throw excptn;
                    }
                }
			} 
            catch (Exception exception) 
            {
                Console.WriteLine("WriteAlternateStreamEncrypted()");
                Console.WriteLine(exception.Message);
			} 
            finally 
            {
				CloseHandle(hFile);
				hFile = IntPtr.Zero;
                if (cs != null)
                {
                    cs.Close();
                    cs.Dispose();
                }
                
				rc2 = null;
                if (ms != null)
                {
                    ms.Close();
                    ms.Dispose();
                }
                if (buffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(buffer);
                }
			}
		}

        /// <summary>
        /// Encrypt the string 'LicenseString' argument and return as a MemoryStream.
        /// </summary>
        /// <param name="LicenseString">The string value to encrypt</param>
        /// <returns>A MemoryStream which contains the encrypted value of 'LicenseString'</returns>
        private static MemoryStream  EncryptLicenseString(string LicenseString)
		{
			Encoding enc = Encoding.Unicode;

			byte[] ba = enc.GetBytes(LicenseString);
			MemoryStream ms = new MemoryStream();

			RC2CryptoServiceProvider rc2 = new RC2CryptoServiceProvider();
			rc2.Key = GetBytesFromHexString("7a6823a42a3a3ae27057c647db812d0");
			rc2.IV = GetBytesFromHexString("827d961224d99b2d");

			CryptoStream cs = new CryptoStream(ms, rc2.CreateEncryptor(), CryptoStreamMode.Write);
			cs.Write(ba, 0, ba.Length);

			cs.Close();
			cs.Dispose();
			rc2 = null;
			return ms;
		}

        /// <summary>
        /// Given an IntPtr to a bufer and the number of bytes, decrypt the buffer and return an 
        /// unencrypted text string.
        /// </summary>
        /// <param name="buffer">An IntPtr to the 'buffer' containing the encrypted string</param>
        /// <param name="nBytes">The number of bytes in 'buffer' to decrypt</param>
        /// <returns></returns>
		private static string DecryptLicenseString(IntPtr buffer, uint nBytes)
		{
			byte[] ba = new byte[nBytes];
			for( int i=0; i<nBytes; i++) {
				ba[i] = Marshal.ReadByte(buffer, i);
			}
			MemoryStream ms = new MemoryStream(ba);

			RC2CryptoServiceProvider rc2 = new RC2CryptoServiceProvider();
			rc2.Key = GetBytesFromHexString("7a6823a42a3a3ae27057c647db812d0");
			rc2.IV = GetBytesFromHexString("827d961224d99b2d");

			CryptoStream cs = new CryptoStream(ms, rc2.CreateDecryptor(), CryptoStreamMode.Read);
			string licenseString = string.Empty;
			byte[] ba1 = new byte[4096];
			int irtn = cs.Read(ba1, 0, 4096);
			Encoding enc = Encoding.Unicode;
			licenseString = enc.GetString(ba1, 0, irtn);

			cs.Close();
			cs.Dispose();
			ms.Close();
			ms.Dispose();
			rc2 = null;
			return licenseString;
		}

        /// <summary>
        /// Gets the byte array generated from the value of 'hexString'.
        /// </summary>
        /// <param name="hexString">Hexadecimal string</param>
        /// <returns>Array of bytes generated from 'hexString'.</returns>
		public static byte[] GetBytesFromHexString(string hexString)
		{
			int numHexChars = hexString.Length / 2;
			byte[] ba = new byte[numHexChars];
			int j = 0;
			for (int i = 0; i < ba.Length; i++) {
				string hex = new string(new char[] { hexString[j], hexString[j + 1] });
				ba[i] = byte.Parse(hex, System.Globalization.NumberStyles.HexNumber);
				j = j + 2;
			}
			return ba;
        }

        #endregion
    }
}
