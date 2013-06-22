#if OS_WIN
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32;
using System.ComponentModel;
using Microsoft.Win32.SafeHandles;

namespace Node.Utilities.Native{

	 [SuppressUnmanagedCodeSecurity]
	public class Win32Api{

       
        public enum GetLastErrorEnum{
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

        public enum UsnJournalDeleteFlags{
            USN_DELETE_FLAG_DELETE = 1,
            USN_DELETE_FLAG_NOTIFY = 2
        }

        public enum FILE_INFORMATION_CLASS{
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

		public enum Ioctls:uint{
			GetVolumeDiskExtents = /*&H*/0x560000,
			DiskGetDriveGeometry = 0x70000,
			GetMediaSerialNumber = 0x00074080 //0x2D0C10
		}
       

		[StructLayout(LayoutKind.Sequential)]
		public struct _FILE_ALLOCATED_RANGE_BUFFER {
  			public long FileOffset;
  			public long Length;
		}

        public const Int32 INVALID_HANDLE_VALUE = -1;

        public const UInt32 GENERIC_READ = 0x80000000;
        public const UInt32 GENERIC_WRITE = 0x40000000;
        public const UInt32 FILE_SHARE_READ = 0x00000001;
        public const UInt32 FILE_SHARE_WRITE = 0x00000002;
		public const uint FILE_SHARE_DELETE = 0x00000004;
        public const UInt32 FILE_ATTRIBUTE_DIRECTORY = 0x00000010;

        public const UInt32 CREATE_NEW = 1;
        public const UInt32 CREATE_ALWAYS = 2;
        public const UInt32 OPEN_EXISTING = 3;
        public const UInt32 OPEN_ALWAYS = 4;
        public const UInt32 TRUNCATE_EXISTING = 5;

        public const UInt32 FILE_ATTRIBUTE_NORMAL = 0x80;
        public const UInt32 FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
		public const uint FILE_FLAG_NO_BUFFERING = 0x20000000;
		public const uint FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000;


        public const UInt32 FileNameInformationClass = 9;
        public const UInt32 FILE_OPEN_FOR_BACKUP_INTENT = 0x4000;
        public const UInt32 FILE_OPEN_BY_FILE_ID = 0x2000;
		public const UInt32 FILE_OPEN_REPARSE_POINT = 0x00200000;
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

		[Flags]
        public enum UsnReasonCode:uint{
            USN_REASON_DATA_OVERWRITE = 0x00000001,
            USN_REASON_DATA_EXTEND = 0x00000002,
            USN_REASON_DATA_TRUNCATION = 0x00000004,
            USN_REASON_NAMED_DATA_OVERWRITE = 0x00000010,
            USN_REASON_NAMED_DATA_EXTEND = 0x00000020,
            USN_REASON_NAMED_DATA_TRUNCATION = 0x00000040,
            USN_REASON_FILE_CREATE = 0x00000100,
            USN_REASON_FILE_DELETE = 0x00000200,
            USN_REASON_EA_CHANGE = 0x00000400,
            USN_REASON_SECURITY_CHANGE = 0x00000800,
            USN_REASON_RENAME_OLD_NAME = 0x00001000,
            USN_REASON_RENAME_NEW_NAME = 0x00002000,
            USN_REASON_INDEXABLE_CHANGE = 0x00004000,
            USN_REASON_BASIC_INFO_CHANGE = 0x00008000,
            USN_REASON_HARD_LINK_CHANGE = 0x00010000,
            USN_REASON_COMPRESSION_CHANGE = 0x00020000,
            USN_REASON_ENCRYPTION_CHANGE = 0x00040000,
            USN_REASON_OBJECT_ID_CHANGE = 0x00080000,
            USN_REASON_REPARSE_POINT_CHANGE = 0x00100000,
            USN_REASON_STREAM_CHANGE = 0x00200000,
			USN_REASON_CLOSE = 0x80000000 //-1
        }

        /*public static Int32 GWL_EXSTYLE = -20;
        public static Int32 WS_EX_LAYERED = 0x00080000;
        public static Int32 WS_EX_TRANSPARENT = 0x00000020;*/

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

         //public const UInt32 FSCTL_QUERY_ALLOCATED_RANGES = (FILE_DEVICE_FILE_SYSTEM << 16) | (FILE_ANY_ACCESS << 14) | (51 << 2) | METHOD_NEITHER;
		public const UInt32 FSCTL_QUERY_ALLOCATED_RANGES =  (FILE_DEVICE_FILE_SYSTEM << 16) | (51 << 2) | METHOD_NEITHER | ((int)System.IO.FileAccess.Read << 14);
        
		public const UInt32 FSCTL_GET_REPARSE_POINT = 0x900A8;

		[StructLayout(LayoutKind.Sequential)]
        public struct REPARSE_GUID_DATA_BUFFER{
            public UInt32 ReparseTag;
            public UInt16 ReparseDataLength;
            public UInt16 Reserved;
            public UInt16 SubstituteNameOffset;
            public UInt16 SubstituteNameLength;
            public UInt16 PrintNameOffset;
            public UInt16 PrintNameLength;
 
            /// <summary>
            /// Contains the SubstituteName and the PrintName.
            /// The SubstituteName is the path of the target directory.
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x3FF0)]
            public byte[] PathBuffer;
        }

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
		public static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// Fills the 'BY_HANDLE_FILE_INFORMATION' structure for the file specified by 'hFile'.
        /// </summary>
        /// <param name="hFile">Fully qualified name of a file</param>
        /// <param name="lpFileInformation">Out BY_HANDLE_FILE_INFORMATION argument</param>
        /// <returns>'true' if successful, otherwise 'false'</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetFileInformationByHandle(
            IntPtr hFile,
            out BY_HANDLE_FILE_INFORMATION lpFileInformation);
        
		[DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetFileSizeEx(
     		IntPtr hFile,
  		  	out long lpFileSize);

        /// <summary>
        /// Deletes the file specified by 'fileName'.
        /// </summary>
        /// <param name="fileName">Fully qualified path to the file to delete</param>
        /// <returns>'true' if successful, otherwise 'false'</returns>
		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DeleteFile(string fileName);

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
            /*IntPtr lpBuffer,*/
			out byte[] buffer,
			uint nNumberOfBytesToRead, 
            out uint lpNumberOfBytesRead, 
            IntPtr lpOverlapped);

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
            IntPtr lpInBuffer, 
            Int32 nInBufferSize,
            IntPtr lpOutBuffer, 
            Int32 nOutBufferSize, 
            out uint lpBytesReturned, 
            IntPtr lpOverlapped);


		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeviceIoControl(
            IntPtr hDevice,
            UInt32 dwIoControlCode,
            IntPtr lpInBuffer, 
            Int32 nInBufferSize,
            ref DiskExtents lpOutBuffer, 
            Int32 nOutBufferSize,
            out int lpBytesReturned, 
            IntPtr lpOverlapped);

     
		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeviceIoControl(
            IntPtr hDevice,
            UInt32 dwIoControlCode,
            IntPtr lpInBuffer, 
            Int32 nInBufferSize,
            ref DISK_GEOMETRY lpOutBuffer, 
            Int32 nOutBufferSize,
            out int lpBytesReturned, 
            IntPtr lpOverlapped);

		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeviceIoControl(
            IntPtr hDevice,
            UInt32 dwIoControlCode,
            IntPtr lpInBuffer, 
            Int32 nInBufferSize,
            ref MEDIA_SERIAL_NUMBER_DATA lpOutBuffer, 
            Int32 nOutBufferSize,
            out int lpBytesReturned, 
            IntPtr lpOverlapped);


		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeviceIoControl(
            IntPtr hDevice,
            UInt32 dwIoControlCode,
            IntPtr lpInBuffer, 
            Int32 nInBufferSize,
           	 IntPtr OutBuffer,
            Int32 nOutBufferSize,
            out int lpBytesReturned, 
            IntPtr lpOverlapped);

		 /*[DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool DeviceIoControl(
            SafeFileHandle hDevice,
            EIoControlCode dwIoControlCode,
            IntPtr InBuffer,
            int nInBufferSize,
            IntPtr OutBuffer,
            int nOutBufferSize,
            ref int pBytesReturned,
            [In] ref NativeOverlapped lpOverlapped
        );*/

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
       // [DllImport("ntdll.dll", ExactSpelling = true, SetLastError = true)]
		 [DllImport("ntdll.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint NtCreateFile(
            ref IntPtr handle, 
            uint/*FileAccess*/ access, 
            ref OBJECT_ATTRIBUTES objectAttributes, 
            ref IO_STATUS_BLOCK ioStatus, 
            ref long allocSize, 
            uint fileAttributes, 
            uint share, 
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
        public static extern uint NtQueryInformationFile(
            IntPtr fileHandle, 
            ref IO_STATUS_BLOCK IoStatusBlock, 
            IntPtr pInfoBlock, 
            uint length, 
            FILE_INFORMATION_CLASS fileInformation);

      	

        /// <summary>
        /// By Handle File Information structure, contains File Attributes(32bits), Creation Time(FILETIME),
        /// Last Access Time(FILETIME), Last Write Time(FILETIME), Volume Serial Number(32bits),
        /// File Size High(32bits), File Size Low(32bits), Number of Links(32bits), File Index High(32bits),
        /// File Index Low(32bits).
        /// </summary>
       /* [StructLayout(LayoutKind.Sequential, Pack = 1)]
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
        }*/


		[StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BY_HANDLE_FILE_INFORMATION{
            public uint FileAttributes;
            public FILETIME CreationTime;
            public FILETIME LastAccessTime;
            public FILETIME LastWriteTime;
            public uint VolumeSerialNumber;
            public uint FileSizeHigh;
            public uint FileSizeLow;
            public uint NumberOfLinks;
            /*public uint FileIndexHigh;
            public uint FileIndexLow;*/
			public FileID FileIndex;
        }

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct FILETIME {
            public uint DateTimeLow;
            public uint DateTimeHigh;
        }
		
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct FileID {
            public uint FileIndexLow;
            public uint FileIndexHigh;
        }

		[StructLayout(LayoutKind.Sequential)]
		public struct DISK_GEOMETRY{
			public long Cylinders;
			public int MediaType;
			public int TracksPerCylinder;
			public int SectorsPerTrack;
			public int BytesPerSector;
			public long DiskSize{
				get{
					return Cylinders * (long)TracksPerCylinder * (long)SectorsPerTrack * (long)BytesPerSector;
				}
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct MEDIA_SERIAL_NUMBER_DATA{
		  public ulong SerialNumberLength;
		  public ulong Result;
		  public ulong Reserved;
		  public byte[] SerialNumberData;
		} 


        /// <summary>
        /// USN Journal Data structure, contains USN Journal ID(64bits), First USN(64bits), Next USN(64bits),
        /// Lowest Valid USN(64bits), Max USN(64bits), Maximum Size(64bits) and Allocation Delta(64bits).
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct USN_JOURNAL_DATA{
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
        public struct MFT_ENUM_DATA{
            public UInt64 StartFileReferenceNumber;
            public Int64 LowUsn;
            public Int64 HighUsn;
        }

        /// <summary>
        /// Create USN Journal Data structure, contains Maximum Size(64bits) and Allocation Delta(64(bits).
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CREATE_USN_JOURNAL_DATA{
            public UInt64 MaximumSize;
            public UInt64 AllocationDelta;
        }

        /// <summary>
        /// Create USN Journal Data structure, contains Maximum Size(64bits) and Allocation Delta(64(bits).
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DELETE_USN_JOURNAL_DATA{
            public UInt64 UsnJournalID;
            public UInt32 DeleteFlags;
            public UInt32 Reserved;
        }

		///Represents a disk extent (partition)
		[StructLayout(LayoutKind.Sequential)]
   		public struct DiskExtent{
			public int DiskNumber;
			public long StartingOffset;
			public long ExtentLength;
		}

 
   		[StructLayout(LayoutKind.Sequential)]
   		public struct DiskExtents{
			public int numberOfExtents;
			public DiskExtent first ;
		}

        /// <summary>
        /// Contains the USN Record Length(32bits), USN(64bits), File Reference Number(64bits), 
        /// Parent File Reference Number(64bits), Reason Code(32bits), File Attributes(32bits),
        /// File Name Length(32bits), the File Name Offset(32bits) and the File Name.
        /// </summary>
        public class UsnEntry : IComparable<UsnEntry> {
            private const int FR_OFFSET = 8;
            private const int PFR_OFFSET = 16;
            private const int USN_OFFSET = 24;
			private const int TIMESTP_OFFSET = 32;
            private const int REASON_OFFSET = 40;
            public const int FA_OFFSET = 52;
            private const int FNL_OFFSET = 56;
            private const int FN_OFFSET = 58;

			public UInt32 RecordLength{get; private set;}
			public Int64 USN{get; private set;}
			public UInt64 FileReferenceNumber{get; private set;}
			public UInt64 ParentFileReferenceNumber{get; private set;}
			public long TimeStamp{get; private set;}
			public UInt32 Reason{get; set;}
			public string Name{get; private set;}
			public string OldName{get; private set;}
               /* get {
                    if (0 != (_fileAttributes & USN_REASON_RENAME_OLD_NAME)){
                        return _oldName;
                    }
                    else{
                        return null;
                    }
                }
                set { _oldName = value; }
            }*/

            private UInt32 _fileAttributes;
            public bool IsFolder{
                get{
                    bool bRtn = false;
                    if (0 != (_fileAttributes & Win32Api.FILE_ATTRIBUTE_DIRECTORY)){
                        bRtn = true;
                    }
                    return bRtn;
                }
            }

            public bool IsFile{
                get{
                    bool bRtn = false;
                    if (0 == (_fileAttributes & Win32Api.FILE_ATTRIBUTE_DIRECTORY)){
                        bRtn = true;
                    }
                    return bRtn;
                }
            }
                
             /// <summary>
            /// USN Record Constructor
            /// </summary>
            /// <param name="p">Buffer pointer to first byte of the USN Record</param>
            public UsnEntry(IntPtr ptrToUsnRecord){
                RecordLength = (UInt32)Marshal.ReadInt32(ptrToUsnRecord);
                this.FileReferenceNumber = (UInt64)Marshal.ReadInt64(ptrToUsnRecord, FR_OFFSET);
                this.ParentFileReferenceNumber = (UInt64)Marshal.ReadInt64(ptrToUsnRecord, PFR_OFFSET);
                this.USN = (Int64)Marshal.ReadInt64(ptrToUsnRecord, USN_OFFSET);
				long fileTime =  ((long) (UInt32)Marshal.ReadInt32(ptrToUsnRecord, TIMESTP_OFFSET+4) << 32) | ((uint) (UInt32)Marshal.ReadInt32(ptrToUsnRecord, TIMESTP_OFFSET));
				this.TimeStamp = (fileTime - 116444736000000000) / 10000000;
				//DateTime.FromFileTime(
				//TimeStamp.DateTimeLow = (UInt32)Marshal.ReadInt32(ptrToUsnRecord, TIMESTP_OFFSET);
				//TimeStamp.DateTimeHigh = (UInt32)Marshal.ReadInt32(ptrToUsnRecord, TIMESTP_OFFSET+4);
                this.Reason = (UInt32)Marshal.ReadInt32(ptrToUsnRecord, REASON_OFFSET);
                _fileAttributes = (UInt32)Marshal.ReadInt32(ptrToUsnRecord, FA_OFFSET);
                short fileNameLength = Marshal.ReadInt16(ptrToUsnRecord, FNL_OFFSET);
                short fileNameOffset = Marshal.ReadInt16(ptrToUsnRecord, FN_OFFSET);
				//ComputedRecordLength = (int)(60+fileNameLength);
                this.Name = Marshal.PtrToStringUni(new IntPtr(ptrToUsnRecord.ToInt32() + fileNameOffset), fileNameLength / sizeof(char));
            }

			public UsnEntry(byte[] recordData){
                this.RecordLength = BitConverter.ToUInt32(recordData,0);
                this.FileReferenceNumber = (UInt64)BitConverter.ToInt64(recordData, FR_OFFSET);
                this.ParentFileReferenceNumber = (UInt64)BitConverter.ToInt64(recordData, PFR_OFFSET);
                this.USN = (Int64)BitConverter.ToUInt64(recordData, USN_OFFSET);
                this.Reason = (UInt32)BitConverter.ToInt32(recordData, REASON_OFFSET);
                _fileAttributes = (UInt32)BitConverter.ToInt32(recordData, FA_OFFSET);
                short fileNameLength = BitConverter.ToInt16(recordData, FNL_OFFSET);
                short fileNameOffset = BitConverter.ToInt16(recordData, FN_OFFSET);
                //_name = Marshal.PtrToStringUni(new IntPtr(recordData.ToInt32() + fileNameOffset), fileNameLength / sizeof(char));
				//ComputedRecordLength = (int)(60+fileNameLength);
				//_name = BitConverter.ToString(recordData, fileNameOffset, fileNameLength);
				Console.WriteLine ("UsnEntry() : filenameoffset="+fileNameOffset+", length="+fileNameLength);
				this.Name =System.Text.Encoding.Unicode.GetString(recordData, 64/*fileNameOffset*/, fileNameLength);
				//_name = System.Text.Encoding.Default.GetString(recordData, fileNameOffset, fileNameLength / sizeof(char));
            }


            public int CompareTo(UsnEntry other){
                return string.Compare(this.Name, other.Name, true);
            }

            public override string ToString(){
				return string.Format ("[UsnRec: RecLength={0}, USN={1}, FRN={2}, pFRN={3},Name={6}, TimeStamp={4}, Reason={5}, OldName={7}, IsFolder={8}, IsFile={9}]", RecordLength, USN, (int)FileReferenceNumber, (int)ParentFileReferenceNumber, Utilities.Utils.GetLocalDateTimeFromUnixTime(TimeStamp), (UsnReasonCode)Reason, Name, OldName, IsFolder, IsFile);
			}
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
        public struct READ_USN_JOURNAL_DATA{
            public Int64 StartUsn;
            public UInt32 ReasonMask;
            public UInt32 ReturnOnlyOnClose;
            public UInt64 Timeout;
            public UInt64 bytesToWaitFor;
            public UInt64 UsnJournalId;

        }

       /* [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct POINT{
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }*/

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct IO_STATUS_BLOCK{
            public uint status;
            //public ulong information;
			public IntPtr information;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 0/*,CharSet=CharSet.Unicode*/)]
        public struct OBJECT_ATTRIBUTES{
            public Int32 Length;
            public IntPtr RootDirectory;
            public IntPtr ObjectName;
            //public Int32 Attributes;
			public uint Attributes;
            public IntPtr/*Int32*/ SecurityDescriptor;
            public IntPtr/*Int32*/ SecurityQualityOfService;
			//public Int32 SecurityDescriptor;
            //public Int32 SecurityQualityOfService;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        public struct UNICODE_STRING{
            public Int16 Length;
            public Int16 MaximumLength;
            public IntPtr Buffer;
        }
	/*	[StructLayout(LayoutKind.Sequential)]
public struct UNICODE_STRING : IDisposable
{
   public ushort Length;
   public ushort MaximumLength;
   private IntPtr buffer;

   public UNICODE_STRING(string s)
   {
     Length = (ushort)(s.Length * 2);
     MaximumLength = (ushort)(Length + 2);
     buffer = Marshal.StringToHGlobalUni(s);
   }

   public void Dispose()
   {
     Marshal.FreeHGlobal(buffer);
     buffer = IntPtr.Zero;
   }

   public override string ToString()
   {
     return Marshal.PtrToStringUni(buffer);
   }
}*/

     

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
        public static void WriteAlternateStream(string currentfile, string text){
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
                else{
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
		public static string ReadAlternateStream(string currentfile){
			string AltStreamDesc = currentfile + ":Description";
			string returnstring = ReadAlternateStreamEx(AltStreamDesc);
			return returnstring;
		}

        /// <summary>
        /// Reads the stream represented by 'currentFile'.
        /// </summary>
        /// <param name="currentfile">Fully qualified path including stream</param>
        /// <returns>Value of the alternate stream as a string</returns>
		public static string ReadAlternateStreamEx(string currentfile){
			string returnstring = string.Empty;
            IntPtr hFile = IntPtr.Zero;
            IntPtr buffer = IntPtr.Zero;
            try{
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

      	[DllImport("kernel32.dll")]
		public static extern void ZeroMemory(IntPtr ptr, int size);

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool GetVolumeNameForVolumeMountPoint(
            string volumeName,
            StringBuilder uniqueVolumeName,
            int uniqueNameBufferCapacity);

		[DllImport("kernel32.dll", EntryPoint = "GetFinalPathNameByHandleW", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern int GetFinalPathNameByHandle(IntPtr handle, [In, Out] StringBuilder path, int bufLen, int flags);
       
    }
}

#endif
