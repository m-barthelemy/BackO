
	// NtfsUsnJournal.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Node.Utilities.Native;

namespace Node.DataProcessing{

    public class NtfsUsnJournal_new : IDisposable{
        #region enum(s)
        public enum UsnJournalReturnCode{
            INVALID_HANDLE_VALUE = -1,
            USN_JOURNAL_SUCCESS = 0,
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
            USN_JOURNAL_NOT_ACTIVE = 1179,
            ERROR_JOURNAL_ENTRY_DELETED = 1181,
            ERROR_INVALID_USER_BUFFER = 1784,
            USN_JOURNAL_INVALID = 17001,
            VOLUME_NOT_NTFS = 17003,
            INVALID_FILE_REFERENCE_NUMBER = 17004,
            USN_JOURNAL_ERROR = 17005
        }

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

        #endregion

        #region private member variables

        private string _driveName = null;
		private string vol; 

        private uint _volumeSerialNumber;
        private IntPtr _usnJournalRootHandle;

        private bool bNtfsVolume;

		private bool UseRawMode = false;

        #endregion

        #region properties

        private static TimeSpan _elapsedTime;
        public static TimeSpan ElapsedTime
        {
            get { return _elapsedTime; }
        }

        

        public uint VolumeSerialNumber
        {
            get { return _volumeSerialNumber; }
        }

        #endregion

        #region constructor(s)

        /// <summary>
        /// Constructor for NtfsUsnJournal class.  If no exception is thrown, _usnJournalRootHandle and
        /// _volumeSerialNumber can be assumed to be good. If an exception is thrown, the NtfsUsnJournal
        /// object is not usable.
        /// </summary>
        /// <param name="driveInfo">DriveInfo object that provides access to information about a volume</param>
        /// <remarks> 
        /// An exception thrown if the volume is not an 'NTFS' volume or
        /// if GetRootHandle() or GetVolumeSerialNumber() functions fail. 
        /// Each public method checks to see if the volume is NTFS and if the _usnJournalRootHandle is
        /// valid.  If these two conditions aren't met, then the public function will return a UsnJournalReturnCode
        /// error.
        /// </remarks>
        public NtfsUsnJournal_new(string driveName){
			//this.UseRawMode = useRawMode;
			bNtfsVolume = true;
			DateTime start = DateTime.Now;
            _driveName = driveName;

			//if(useRawMode) return;

           // if (0 == string.Compare(_driveInfo.DriveFormat, "ntfs", true))
            //{
               

                IntPtr rootHandle = IntPtr.Zero;
                UsnJournalReturnCode usnRtnCode = GetRootHandle(out rootHandle);

                if (usnRtnCode == UsnJournalReturnCode.USN_JOURNAL_SUCCESS){
                    _usnJournalRootHandle = rootHandle;
                    //usnRtnCode = GetVolumeSerialNumber(_driveName, out _volumeSerialNumber);
                    if (usnRtnCode != UsnJournalReturnCode.USN_JOURNAL_SUCCESS){
                        _elapsedTime = DateTime.Now - start;
                        throw new Win32Exception((int)usnRtnCode);
                    }
                }
                else{
                    _elapsedTime = DateTime.Now - start;
                    throw new Win32Exception((int)usnRtnCode);
                }
           /* }
            else
            {
                _elapsedTime = DateTime.Now - start;
                throw new Exception(string.Format("{0} is not an 'NTFS' volume.", _driveInfo.Name));
            }*/
            _elapsedTime = DateTime.Now - start;
        }

        #endregion

        #region public methods

        /// <summary>
        /// CreateUsnJournal() creates a usn journal on the volume. If a journal already exists this function 
        /// will adjust the MaximumSize and AllocationDelta parameters of the journal if the requested size
        /// is larger.
        /// </summary>
        /// <param name="maxSize">maximum size requested for the UsnJournal</param>
        /// <param name="allocationDelta">when space runs out, the amount of additional
        /// space to allocate</param>
        /// <param name="elapsedTime">The TimeSpan object indicating how much time this function 
        /// took</param>
        /// <returns>a UsnJournalReturnCode
        /// USN_JOURNAL_SUCCESS                 CreateUsnJournal() function succeeded. 
        /// VOLUME_NOT_NTFS                     volume is not an NTFS volume.
        /// INVALID_HANDLE_VALUE                NtfsUsnJournal object failed initialization.
        /// USN_JOURNAL_NOT_ACTIVE              usn journal is not active on volume.
        /// ERROR_ACCESS_DENIED                 accessing the usn journal requires admin rights, see remarks.
        /// ERROR_INVALID_FUNCTION              error generated by DeviceIoControl() call.
        /// ERROR_FILE_NOT_FOUND                error generated by DeviceIoControl() call.
        /// ERROR_PATH_NOT_FOUND                error generated by DeviceIoControl() call.
        /// ERROR_TOO_MANY_OPEN_FILES           error generated by DeviceIoControl() call.
        /// ERROR_INVALID_HANDLE                error generated by DeviceIoControl() call.
        /// ERROR_INVALID_DATA                  error generated by DeviceIoControl() call.
        /// ERROR_NOT_SUPPORTED                 error generated by DeviceIoControl() call.
        /// ERROR_INVALID_PARAMETER             error generated by DeviceIoControl() call.
        /// ERROR_JOURNAL_DELETE_IN_PROGRESS    usn journal delete is in progress.
        /// ERROR_INVALID_USER_BUFFER           error generated by DeviceIoControl() call.
        /// USN_JOURNAL_ERROR                   unspecified usn journal error.
        /// </returns>
        /// <remarks>
        /// If function returns ERROR_ACCESS_DENIED you need to run application as an Administrator.
        /// </remarks>
        public UsnJournalReturnCode CreateUsnJournal(ulong maxSize, ulong allocationDelta){

			if(this.UseRawMode) return UsnJournalReturnCode.ERROR_INVALID_FUNCTION;

            UsnJournalReturnCode usnRtnCode = UsnJournalReturnCode.VOLUME_NOT_NTFS;
            DateTime startTime = DateTime.Now;

            if (bNtfsVolume){
                if (_usnJournalRootHandle.ToInt32() != Win32Api.INVALID_HANDLE_VALUE){
                    usnRtnCode = UsnJournalReturnCode.USN_JOURNAL_SUCCESS;
                    UInt32 cb;

                    Win32Api.CREATE_USN_JOURNAL_DATA cujd = new Win32Api.CREATE_USN_JOURNAL_DATA();
                    cujd.MaximumSize = maxSize;
                    cujd.AllocationDelta = allocationDelta;

                    int sizeCujd = Marshal.SizeOf(cujd);
                    IntPtr cujdBuffer = Marshal.AllocHGlobal(sizeCujd);
                    Win32Api.ZeroMemory(cujdBuffer, sizeCujd);
                    Marshal.StructureToPtr(cujd, cujdBuffer, true);

                    bool fOk = Win32Api.DeviceIoControl(
                        _usnJournalRootHandle,
                        Win32Api.FSCTL_CREATE_USN_JOURNAL,
                        cujdBuffer,
                        sizeCujd,
                        IntPtr.Zero,
                        0,
                        out cb,
                        IntPtr.Zero);
                    if (!fOk){
                        usnRtnCode = ConvertWin32ErrorToUsnError((Win32Api.GetLastErrorEnum)Marshal.GetLastWin32Error());
                    }
                    Marshal.FreeHGlobal(cujdBuffer);
                }
                else{
                    usnRtnCode = UsnJournalReturnCode.INVALID_HANDLE_VALUE;
                }
            }

            _elapsedTime = DateTime.Now - startTime;
            return usnRtnCode;
        }

        /// <summary>
        /// DeleteUsnJournal() deletes a usn journal on the volume. If no usn journal exists, this
        /// function simply returns success.
        /// </summary>
        /// <param name="journalState">USN_JOURNAL_DATA object for this volume</param>
        /// <param name="elapsedTime">The TimeSpan object indicating how much time this function 
        /// took</param>
        /// <returns>a UsnJournalReturnCode
        /// USN_JOURNAL_SUCCESS                 DeleteUsnJournal() function succeeded. 
        /// VOLUME_NOT_NTFS                     volume is not an NTFS volume.
        /// INVALID_HANDLE_VALUE                NtfsUsnJournal object failed initialization.
        /// USN_JOURNAL_NOT_ACTIVE              usn journal is not active on volume.
        /// ERROR_ACCESS_DENIED                 accessing the usn journal requires admin rights, see remarks.
        /// ERROR_INVALID_FUNCTION              error generated by DeviceIoControl() call.
        /// ERROR_FILE_NOT_FOUND                error generated by DeviceIoControl() call.
        /// ERROR_PATH_NOT_FOUND                error generated by DeviceIoControl() call.
        /// ERROR_TOO_MANY_OPEN_FILES           error generated by DeviceIoControl() call.
        /// ERROR_INVALID_HANDLE                error generated by DeviceIoControl() call.
        /// ERROR_INVALID_DATA                  error generated by DeviceIoControl() call.
        /// ERROR_NOT_SUPPORTED                 error generated by DeviceIoControl() call.
        /// ERROR_INVALID_PARAMETER             error generated by DeviceIoControl() call.
        /// ERROR_JOURNAL_DELETE_IN_PROGRESS    usn journal delete is in progress.
        /// ERROR_INVALID_USER_BUFFER           error generated by DeviceIoControl() call.
        /// USN_JOURNAL_ERROR                   unspecified usn journal error.
        /// </returns>
        /// <remarks>
        /// If function returns ERROR_ACCESS_DENIED you need to run application as an Administrator.
        /// </remarks>
        /*public UsnJournalReturnCode DeleteUsnJournal(Win32Api.USN_JOURNAL_DATA journalState){

			if(this.UseRawMode) return UsnJournalReturnCode.ERROR_INVALID_FUNCTION;

            UsnJournalReturnCode usnRtnCode = UsnJournalReturnCode.VOLUME_NOT_NTFS;
            DateTime startTime = DateTime.Now;

            if (bNtfsVolume){
                if (_usnJournalRootHandle.ToInt32() != Win32Api.INVALID_HANDLE_VALUE){
                    usnRtnCode = UsnJournalReturnCode.USN_JOURNAL_SUCCESS;
                    UInt32 cb;

                    Win32Api.DELETE_USN_JOURNAL_DATA dujd = new Win32Api.DELETE_USN_JOURNAL_DATA();
                    dujd.UsnJournalID = journalState.UsnJournalID;
                    dujd.DeleteFlags = (UInt32)Win32Api.UsnJournalDeleteFlags.USN_DELETE_FLAG_DELETE;

                    int sizeDujd = Marshal.SizeOf(dujd);
                    IntPtr dujdBuffer = Marshal.AllocHGlobal(sizeDujd);
                    Win32Api.ZeroMemory(dujdBuffer, sizeDujd);
                    Marshal.StructureToPtr(dujd, dujdBuffer, true);

                    bool fOk = Win32Api.DeviceIoControl(
                        _usnJournalRootHandle,
                        Win32Api.FSCTL_DELETE_USN_JOURNAL,
                        dujdBuffer,
                        sizeDujd,
                        IntPtr.Zero,
                        0,
                        out cb,
                        IntPtr.Zero);

                    if (!fOk){
                        usnRtnCode = ConvertWin32ErrorToUsnError((Win32Api.GetLastErrorEnum)Marshal.GetLastWin32Error());
                    }
                    Marshal.FreeHGlobal(dujdBuffer);
                }
                else{
                    usnRtnCode = UsnJournalReturnCode.INVALID_HANDLE_VALUE;
                }
            }

            _elapsedTime = DateTime.Now - startTime;
            return usnRtnCode;
        }*/

        /// <summary>
        /// GetNtfsVolumeFolders() reads the Master File Table to find all of the folders on a volume 
        /// and returns them in a SortedList<UInt64, Win32Api.UsnEntry> folders out parameter.
        /// </summary>
        /// <param name="folders">A SortedList<string, UInt64> list where string is
        /// the filename and UInt64 is the parent folder's file reference number
        /// </param>
        /// <param name="elapsedTime">A TimeSpan object that on return holds the elapsed time
        /// </param>
        /// <returns>
        /// USN_JOURNAL_SUCCESS                 GetNtfsVolumeFolders() function succeeded. 
        /// VOLUME_NOT_NTFS                     volume is not an NTFS volume.
        /// INVALID_HANDLE_VALUE                NtfsUsnJournal object failed initialization.
        /// USN_JOURNAL_NOT_ACTIVE              usn journal is not active on volume.
        /// ERROR_ACCESS_DENIED                 accessing the usn journal requires admin rights, see remarks.
        /// ERROR_INVALID_FUNCTION              error generated by DeviceIoControl() call.
        /// ERROR_FILE_NOT_FOUND                error generated by DeviceIoControl() call.
        /// ERROR_PATH_NOT_FOUND                error generated by DeviceIoControl() call.
        /// ERROR_TOO_MANY_OPEN_FILES           error generated by DeviceIoControl() call.
        /// ERROR_INVALID_HANDLE                error generated by DeviceIoControl() call.
        /// ERROR_INVALID_DATA                  error generated by DeviceIoControl() call.
        /// ERROR_NOT_SUPPORTED                 error generated by DeviceIoControl() call.
        /// ERROR_INVALID_PARAMETER             error generated by DeviceIoControl() call.
        /// ERROR_JOURNAL_DELETE_IN_PROGRESS    usn journal delete is in progress.
        /// ERROR_INVALID_USER_BUFFER           error generated by DeviceIoControl() call.
        /// USN_JOURNAL_ERROR                   unspecified usn journal error.
        /// </returns>
        /// <remarks>
        /// If function returns ERROR_ACCESS_DENIED you need to run application as an Administrator.
        /// </remarks>
        public UsnJournalReturnCode GetNtfsVolumeFolders(out List<Win32Api.UsnEntry> folders){

			folders = new List<Win32Api.UsnEntry>();
			if(this.UseRawMode) return UsnJournalReturnCode.ERROR_INVALID_FUNCTION;

            DateTime startTime = DateTime.Now;
            
            UsnJournalReturnCode usnRtnCode = UsnJournalReturnCode.VOLUME_NOT_NTFS;

            if (bNtfsVolume){
                if (_usnJournalRootHandle.ToInt32() != Win32Api.INVALID_HANDLE_VALUE){
                    usnRtnCode = UsnJournalReturnCode.USN_JOURNAL_SUCCESS;

                    Win32Api.USN_JOURNAL_DATA usnState = new Win32Api.USN_JOURNAL_DATA();
                    usnRtnCode = QueryUsnJournal(ref usnState);

                    if (usnRtnCode == UsnJournalReturnCode.USN_JOURNAL_SUCCESS){
                        //
                        // set up MFT_ENUM_DATA structure
                        //
                        Win32Api.MFT_ENUM_DATA med;
                        med.StartFileReferenceNumber = 0;
                        med.LowUsn = 0;
                        med.HighUsn = usnState.NextUsn;
                        Int32 sizeMftEnumData = Marshal.SizeOf(med);
                        IntPtr medBuffer = Marshal.AllocHGlobal(sizeMftEnumData);
                        Win32Api.ZeroMemory(medBuffer, sizeMftEnumData);
                        Marshal.StructureToPtr(med, medBuffer, true);

                        //
                        // set up the data buffer which receives the USN_RECORD data
                        //
                        int pDataSize = sizeof(UInt64) + 10000;
                        IntPtr pData = Marshal.AllocHGlobal(pDataSize);
                        Win32Api.ZeroMemory(pData, pDataSize);
                        uint outBytesReturned = 0;
                        Win32Api.UsnEntry usnEntry = null;

                        //
                        // Gather up volume's directories
                        //
                        while (false != Win32Api.DeviceIoControl(
                            _usnJournalRootHandle,
                            Win32Api.FSCTL_ENUM_USN_DATA,
                            medBuffer,
                            sizeMftEnumData,
                            pData,
                            pDataSize,
                            out outBytesReturned,
                            IntPtr.Zero))
                        {
                            IntPtr pUsnRecord = new IntPtr(pData.ToInt32() + sizeof(Int64));
                            while (outBytesReturned > 60)
                            {
                                usnEntry = new Win32Api.UsnEntry(pUsnRecord);
                                //
                                // check for directory entries
                                //
                                if (usnEntry.IsFolder)
                                {
                                    folders.Add(usnEntry);
                                }
                                pUsnRecord = new IntPtr(pUsnRecord.ToInt32() + usnEntry.RecordLength);
                                outBytesReturned -= usnEntry.RecordLength;
                            }
                            Marshal.WriteInt64(medBuffer, Marshal.ReadInt64(pData, 0));
                        }

                        Marshal.FreeHGlobal(pData);
                        usnRtnCode = ConvertWin32ErrorToUsnError((Win32Api.GetLastErrorEnum)Marshal.GetLastWin32Error());
                        if (usnRtnCode == UsnJournalReturnCode.ERROR_HANDLE_EOF)
                        {
                            usnRtnCode = UsnJournalReturnCode.USN_JOURNAL_SUCCESS;
                        }
                    }
                }
                else
                {
                    usnRtnCode = UsnJournalReturnCode.INVALID_HANDLE_VALUE;
                }
            }
            folders.Sort();
            _elapsedTime = DateTime.Now - startTime;
            return usnRtnCode;
        }

        public UsnJournalReturnCode  GetFilesMatchingFilter(string filter, out List<Win32Api.UsnEntry> files){
            DateTime startTime = DateTime.Now;
            filter = filter.ToLower();
            files = new List<Win32Api.UsnEntry>();
            string[] fileTypes = filter.Split(' ', ',', ';');
            UsnJournalReturnCode usnRtnCode = UsnJournalReturnCode.VOLUME_NOT_NTFS;

            if (bNtfsVolume){
                if (_usnJournalRootHandle.ToInt32() != Win32Api.INVALID_HANDLE_VALUE){
                    usnRtnCode = UsnJournalReturnCode.USN_JOURNAL_SUCCESS;

                    Win32Api.USN_JOURNAL_DATA usnState = new Win32Api.USN_JOURNAL_DATA();
                    usnRtnCode = QueryUsnJournal(ref usnState);

                    if (usnRtnCode == UsnJournalReturnCode.USN_JOURNAL_SUCCESS)
                    {
                        //
                        // set up MFT_ENUM_DATA structure
                        //
                        Win32Api.MFT_ENUM_DATA med;
                        med.StartFileReferenceNumber = 0;
                        med.LowUsn = 0;
                        med.HighUsn = usnState.NextUsn;
                        Int32 sizeMftEnumData = Marshal.SizeOf(med);
                        IntPtr medBuffer = Marshal.AllocHGlobal(sizeMftEnumData);
                        Win32Api.ZeroMemory(medBuffer, sizeMftEnumData);
                        Marshal.StructureToPtr(med, medBuffer, true);

                        //
                        // set up the data buffer which receives the USN_RECORD data
                        //
                        int pDataSize = sizeof(UInt64) + 10000;
                        IntPtr pData = Marshal.AllocHGlobal(pDataSize);
                        Win32Api.ZeroMemory(pData, pDataSize);
                        uint outBytesReturned = 0;
                        Win32Api.UsnEntry usnEntry = null;

                        //
                        // Gather up volume's directories
                        //
                        while (false != Win32Api.DeviceIoControl(
                            _usnJournalRootHandle,
                            Win32Api.FSCTL_ENUM_USN_DATA,
                            medBuffer,
                            sizeMftEnumData,
                            pData,
                            pDataSize,
                            out outBytesReturned,
                            IntPtr.Zero))
                        {
                            IntPtr pUsnRecord = new IntPtr(pData.ToInt32() + sizeof(Int64));
                            while (outBytesReturned > 60)
                            {
                                usnEntry = new Win32Api.UsnEntry(pUsnRecord);
                                //
                                // check for directory entries
                                //
                                if (usnEntry.IsFile)
                                {
                                    string extension = Path.GetExtension(usnEntry.Name).ToLower();
                                    if (0 == string.Compare(filter, "*"))
                                    {
                                        files.Add(usnEntry);
                                    }
                                    else if(!string.IsNullOrEmpty(extension))
                                    {
                                        foreach (string fileType in fileTypes)
                                        {
                                            if (extension.Contains(fileType))
                                            {
                                                files.Add(usnEntry);
                                            }
                                        }
                                    }
                                }
                                pUsnRecord = new IntPtr(pUsnRecord.ToInt32() + usnEntry.RecordLength);
                                outBytesReturned -= usnEntry.RecordLength;
                            }
                            Marshal.WriteInt64(medBuffer, Marshal.ReadInt64(pData, 0));
                        }

                        Marshal.FreeHGlobal(pData);
                        usnRtnCode = ConvertWin32ErrorToUsnError((Win32Api.GetLastErrorEnum)Marshal.GetLastWin32Error());
                        if (usnRtnCode == UsnJournalReturnCode.ERROR_HANDLE_EOF)
                        {
                            usnRtnCode = UsnJournalReturnCode.USN_JOURNAL_SUCCESS;
                        }
                    }
                }
                else
                {
                    usnRtnCode = UsnJournalReturnCode.INVALID_HANDLE_VALUE;
                }
            }
            files.Sort();
            _elapsedTime = DateTime.Now - startTime;
            return usnRtnCode;
        }

        /// <summary>
        /// Given a file reference number GetPathFromFrn() calculates the full path in the out parameter 'path'.
        /// </summary>
        /// <param name="frn">A 64-bit file reference number</param>
        /// <returns>
        /// USN_JOURNAL_SUCCESS                 GetPathFromFrn() function succeeded. 
        /// VOLUME_NOT_NTFS                     volume is not an NTFS volume.
        /// INVALID_HANDLE_VALUE                NtfsUsnJournal object failed initialization.
        /// ERROR_ACCESS_DENIED                 accessing the usn journal requires admin rights, see remarks.
        /// INVALID_FILE_REFERENCE_NUMBER       file reference number not found in Master File Table.
        /// ERROR_INVALID_FUNCTION              error generated by NtCreateFile() or NtQueryInformationFile() call.
        /// ERROR_FILE_NOT_FOUND                error generated by NtCreateFile() or NtQueryInformationFile() call.
        /// ERROR_PATH_NOT_FOUND                error generated by NtCreateFile() or NtQueryInformationFile() call.
        /// ERROR_TOO_MANY_OPEN_FILES           error generated by NtCreateFile() or NtQueryInformationFile() call.
        /// ERROR_INVALID_HANDLE                error generated by NtCreateFile() or NtQueryInformationFile() call.
        /// ERROR_INVALID_DATA                  error generated by NtCreateFile() or NtQueryInformationFile() call.
        /// ERROR_NOT_SUPPORTED                 error generated by NtCreateFile() or NtQueryInformationFile() call.
        /// ERROR_INVALID_PARAMETER             error generated by NtCreateFile() or NtQueryInformationFile() call.
        /// ERROR_INVALID_USER_BUFFER           error generated by NtCreateFile() or NtQueryInformationFile() call.
        /// USN_JOURNAL_ERROR                   unspecified usn journal error.
        /// </returns>
        /// <remarks>
        /// If function returns ERROR_ACCESS_DENIED you need to run application as an Administrator.
        /// </remarks>

        public UsnJournalReturnCode GetPathFromFileReference(UInt64 frn, out string path){
            DateTime startTime = DateTime.Now;
            path = "Unavailable";
            UsnJournalReturnCode usnRtnCode = UsnJournalReturnCode.VOLUME_NOT_NTFS;

            if (bNtfsVolume){
                if (_usnJournalRootHandle.ToInt32() == Win32Api.INVALID_HANDLE_VALUE)
					return UsnJournalReturnCode.ERROR_INVALID_HANDLE;
                if (frn == 0)
					return UsnJournalReturnCode.ERROR_INVALID_PARAMETER;

                usnRtnCode = UsnJournalReturnCode.USN_JOURNAL_SUCCESS;

                long allocSize = 0;
                Win32Api.UNICODE_STRING unicodeString;
                Win32Api.OBJECT_ATTRIBUTES objAttributes = new Win32Api.OBJECT_ATTRIBUTES();
                Win32Api.IO_STATUS_BLOCK ioStatusBlock = new Win32Api.IO_STATUS_BLOCK();
                IntPtr hFile = IntPtr.Zero;

                IntPtr buffer = Marshal.AllocHGlobal(4096);
                IntPtr refPtr = Marshal.AllocHGlobal(8);
                IntPtr objAttIntPtr = Marshal.AllocHGlobal(Marshal.SizeOf(objAttributes));

                //
                // pointer >> fileid
                //
                Marshal.WriteInt64(refPtr, (long)frn); 
				// mbarthelemy
				/*int frnId = (int)frn;
				Marshal.WriteInt64(refPtr, (long)frnId);*/

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
                objAttributes.RootDirectory = _usnJournalRootHandle;
                objAttributes.Attributes = (int)Win32Api.OBJ_CASE_INSENSITIVE;

                int fOk = Win32Api.NtCreateFile(
                    ref hFile, 
                    FileAccess.Read, 
                    ref objAttributes, 
                    ref ioStatusBlock, 
                    ref allocSize, 
                    0,
                     Win32Api.FILE_SHARE_READ|Win32Api.FILE_SHARE_WRITE,/*FileShare.ReadWrite,*/
                    Win32Api.FILE_OPEN,
                    Win32Api.FILE_OPEN_BY_FILE_ID | Win32Api.FILE_OPEN_FOR_BACKUP_INTENT, 
                    IntPtr.Zero, 0);

				Console.WriteLine("GetPathFromFileReference("+frn+") CreateFile(), fok="+fOk+", _usnJournalRootHandle="+_usnJournalRootHandle.ToInt32()+", error="+(new Win32Exception(Marshal.GetLastWin32Error()).Message));
                if (Marshal.GetLastWin32Error()/*fOk*/ == 0){
                    fOk = Win32Api.NtQueryInformationFile(
                        hFile, 
                        ref ioStatusBlock, 
                        buffer, 
                        4096, 
                        Win32Api.FILE_INFORMATION_CLASS.FileNameInformation);
					Console.WriteLine("GetPathFromFileReference NtQueryInformationFile(), fOk="+fOk+",error="+(new Win32Exception(Marshal.GetLastWin32Error()).Message));
                    if (Marshal.GetLastWin32Error()/*fOk*/ == 0){
                        // first 4 bytes are the name length
                        int nameLength = Marshal.ReadInt32(buffer, 0);
						Console.WriteLine("GetPathFromFileReference name length will be "+nameLength);
                        // next bytes are the name
                        
						//path = Marshal.PtrToStringUni(new IntPtr(buffer.ToInt32() + 4), nameLength / 2);
                    }
                }
                Win32Api.CloseHandle(hFile);
                Marshal.FreeHGlobal(buffer);
                Marshal.FreeHGlobal(objAttIntPtr);
                Marshal.FreeHGlobal(refPtr);
            }
            _elapsedTime = DateTime.Now - startTime;
            return usnRtnCode;
        }

        /// <summary>
        /// GetUsnJournalState() gets the current state of the USN Journal if it is active.
        /// </summary>
        /// <param name="usnJournalState">
        /// Reference to usn journal data object filled with the current USN Journal state.
        /// </param>
        /// <param name="elapsedTime">The elapsed time for the GetUsnJournalState() function call.</param>
        /// <returns>
        /// USN_JOURNAL_SUCCESS                 GetUsnJournalState() function succeeded. 
        /// VOLUME_NOT_NTFS                     volume is not an NTFS volume.
        /// INVALID_HANDLE_VALUE                NtfsUsnJournal object failed initialization.
        /// USN_JOURNAL_NOT_ACTIVE              usn journal is not active on volume.
        /// ERROR_ACCESS_DENIED                 accessing the usn journal requires admin rights, see remarks.
        /// ERROR_INVALID_FUNCTION              error generated by DeviceIoControl() call.
        /// ERROR_FILE_NOT_FOUND                error generated by DeviceIoControl() call.
        /// ERROR_PATH_NOT_FOUND                error generated by DeviceIoControl() call.
        /// ERROR_TOO_MANY_OPEN_FILES           error generated by DeviceIoControl() call.
        /// ERROR_INVALID_HANDLE                error generated by DeviceIoControl() call.
        /// ERROR_INVALID_DATA                  error generated by DeviceIoControl() call.
        /// ERROR_NOT_SUPPORTED                 error generated by DeviceIoControl() call.
        /// ERROR_INVALID_PARAMETER             error generated by DeviceIoControl() call.
        /// ERROR_JOURNAL_DELETE_IN_PROGRESS    usn journal delete is in progress.
        /// ERROR_INVALID_USER_BUFFER           error generated by DeviceIoControl() call.
        /// USN_JOURNAL_ERROR                   unspecified usn journal error.
        /// </returns>
        /// <remarks>
        /// If function returns ERROR_ACCESS_DENIED you need to run application as an Administrator.
        /// </remarks>
        public UsnJournalReturnCode  GetUsnJournalState(ref Win32Api.USN_JOURNAL_DATA usnJournalState){

            UsnJournalReturnCode usnRtnCode = UsnJournalReturnCode.VOLUME_NOT_NTFS;
            DateTime startTime = DateTime.Now;

            if(bNtfsVolume){
                if(_usnJournalRootHandle.ToInt32() != Win32Api.INVALID_HANDLE_VALUE){
                    usnRtnCode = QueryUsnJournal(ref usnJournalState);
                }
                else{
                    usnRtnCode = UsnJournalReturnCode.INVALID_HANDLE_VALUE;
                }
            }

            _elapsedTime = DateTime.Now - startTime;
            return usnRtnCode;
        }

        /// <summary>
        /// Given a previous state, GetUsnJournalEntries() determines if the USN Journal is active and
        /// no USN Journal entries have been lost (i.e. USN Journal is valid), then
        /// it loads a SortedList<UInt64, Win32Api.UsnEntry> list and returns it as the out parameter 'usnEntries'.
        /// If GetUsnJournalChanges returns anything but USN_JOURNAL_SUCCESS, the usnEntries list will 
        /// be empty.
        /// </summary>
        /// <param name="previousUsnState">The USN Journal state the last time volume 
        /// changes were requested.</param>
        /// <param name="newFiles">List of the filenames of all new files.</param>
        /// <param name="changedFiles">List of the filenames of all changed files.</param>
        /// <param name="newFolders">List of the names of all new folders.</param>
        /// <param name="changedFolders">List of the names of all changed folders.</param>
        /// <param name="deletedFiles">List of the names of all deleted files</param>
        /// <param name="deletedFolders">List of the names of all deleted folders</param>
        /// <param name="currentState">Current state of the USN Journal</param>
        /// <returns>
        /// USN_JOURNAL_SUCCESS                 GetUsnJournalChanges() function succeeded. 
        /// VOLUME_NOT_NTFS                     volume is not an NTFS volume.
        /// INVALID_HANDLE_VALUE                NtfsUsnJournal object failed initialization.
        /// USN_JOURNAL_NOT_ACTIVE              usn journal is not active on volume.
        /// ERROR_ACCESS_DENIED                 accessing the usn journal requires admin rights, see remarks.
        /// ERROR_INVALID_FUNCTION              error generated by DeviceIoControl() call.
        /// ERROR_FILE_NOT_FOUND                error generated by DeviceIoControl() call.
        /// ERROR_PATH_NOT_FOUND                error generated by DeviceIoControl() call.
        /// ERROR_TOO_MANY_OPEN_FILES           error generated by DeviceIoControl() call.
        /// ERROR_INVALID_HANDLE                error generated by DeviceIoControl() call.
        /// ERROR_INVALID_DATA                  error generated by DeviceIoControl() call.
        /// ERROR_NOT_SUPPORTED                 error generated by DeviceIoControl() call.
        /// ERROR_INVALID_PARAMETER             error generated by DeviceIoControl() call.
        /// ERROR_JOURNAL_DELETE_IN_PROGRESS    usn journal delete is in progress.
        /// ERROR_INVALID_USER_BUFFER           error generated by DeviceIoControl() call.
        /// USN_JOURNAL_ERROR                   unspecified usn journal error.
        /// </returns>
        /// <remarks>
        /// If function returns ERROR_ACCESS_DENIED you need to run application as an Administrator.
        /// </remarks>
        public UsnJournalReturnCode  GetUsnJournalEntries(Win32Api.USN_JOURNAL_DATA previousUsnState,
            UInt32 reasonMask, 
            out List<Win32Api.UsnEntry> usnEntries, 
            out Win32Api.USN_JOURNAL_DATA newUsnState)
        {
			if(this.UseRawMode) return GetUsnJournalEntries_Raw(previousUsnState, reasonMask, out usnEntries, out newUsnState);

            DateTime startTime = DateTime.Now;
            usnEntries = new List<Win32Api.UsnEntry>();
            newUsnState = new Win32Api.USN_JOURNAL_DATA();
            UsnJournalReturnCode usnRtnCode = UsnJournalReturnCode.VOLUME_NOT_NTFS;

            if (bNtfsVolume){
                if (_usnJournalRootHandle.ToInt32() != Win32Api.INVALID_HANDLE_VALUE){
                    //
                    // get current usn journal state
                    //
                    usnRtnCode = QueryUsnJournal(ref newUsnState);
                    if (usnRtnCode == UsnJournalReturnCode.USN_JOURNAL_SUCCESS){
                        bool bReadMore = true;
                        //
                        // sequentially process the usn journal looking for image file entries
                        //
                        int pbDataSize = sizeof(UInt64) * 0x4000;
                        IntPtr pbData = Marshal.AllocHGlobal(pbDataSize);
                        Win32Api.ZeroMemory(pbData, pbDataSize);
                        uint outBytesReturned = 0;

                        Win32Api.READ_USN_JOURNAL_DATA rujd = new Win32Api.READ_USN_JOURNAL_DATA();
                        rujd.StartUsn = previousUsnState.NextUsn;
                        rujd.ReasonMask = reasonMask;
                        rujd.ReturnOnlyOnClose = 0;
                        rujd.Timeout = 0;
                        rujd.bytesToWaitFor = 0;
                        rujd.UsnJournalId = (ulong)previousUsnState.UsnJournalID;
                        int sizeRujd = Marshal.SizeOf(rujd);

                        IntPtr rujdBuffer = Marshal.AllocHGlobal(sizeRujd);
                        Win32Api.ZeroMemory(rujdBuffer, sizeRujd);
                        Marshal.StructureToPtr(rujd, rujdBuffer, true);

                        Win32Api.UsnEntry usnEntry = null;

                        //
                        // read usn journal entries
                        //
                        while (bReadMore){
                            bool bRtn = Win32Api.DeviceIoControl(_usnJournalRootHandle, Win32Api.FSCTL_READ_USN_JOURNAL,
                                rujdBuffer, sizeRujd, pbData, pbDataSize, out outBytesReturned, IntPtr.Zero);
                            if (bRtn){
                                IntPtr pUsnRecord = new IntPtr(pbData.ToInt32() + sizeof(UInt64));
                                while (outBytesReturned > 60){   // while there are at least one entry in the usn journal
                                    usnEntry = new Win32Api.UsnEntry(pUsnRecord);
                                    if (usnEntry.USN >= newUsnState.NextUsn){
                                        bReadMore = false;
                                        break;
                                    }
                                    usnEntries.Add(usnEntry);

                                    pUsnRecord = new IntPtr(pUsnRecord.ToInt32() + usnEntry.RecordLength);
                                    outBytesReturned -= usnEntry.RecordLength;

                                }   // end while (outBytesReturned > 60) - closing bracket

                            }   // if (bRtn)- closing bracket
                            else{
                                Win32Api.GetLastErrorEnum lastWin32Error = (Win32Api.GetLastErrorEnum)Marshal.GetLastWin32Error();
                                if (lastWin32Error == Win32Api.GetLastErrorEnum.ERROR_HANDLE_EOF){
                                    usnRtnCode = UsnJournalReturnCode.USN_JOURNAL_SUCCESS;
                                }
                                else{
                                    usnRtnCode = ConvertWin32ErrorToUsnError(lastWin32Error);
                                }
                                break;
                            }

                            Int64 nextUsn = Marshal.ReadInt64(pbData, 0);
                            if (nextUsn >= newUsnState.NextUsn)
                                break;

                            Marshal.WriteInt64(rujdBuffer, nextUsn);

                        }   // end while (bReadMore) - closing bracket

                        Marshal.FreeHGlobal(rujdBuffer);
                        Marshal.FreeHGlobal(pbData);

                    }   // if (usnRtnCode == UsnJournalReturnCode.USN_JOURNAL_SUCCESS) - closing bracket

                }   // if (_usnJournalRootHandle.ToInt32() != Win32Api.INVALID_HANDLE_VALUE)
                else
                    usnRtnCode = UsnJournalReturnCode.INVALID_HANDLE_VALUE;
            }   // if (bNtfsVolume) - closing bracket

            _elapsedTime = DateTime.Now - startTime;
            return usnRtnCode;
        }   // GetUsnJournalChanges() - closing bracket


		public unsafe UsnJournalReturnCode  GetUsnJournalEntries_Raw(Win32Api.USN_JOURNAL_DATA previousUsnState,
            UInt32 reasonMask, out List<Win32Api.UsnEntry> usnEntries, out Win32Api.USN_JOURNAL_DATA newUsnState){

			usnEntries = new List<Win32Api.UsnEntry>();
			newUsnState = new Win32Api.USN_JOURNAL_DATA();
			if (_usnJournalRootHandle.ToInt32() == Win32Api.INVALID_HANDLE_VALUE)
				return UsnJournalReturnCode.INVALID_HANDLE_VALUE;
			QueryUsnJournal(ref newUsnState);
			string rawJournal= vol/*_driveName*/ + @"\$Extend\$UsnJrnl:$J";

			Console.WriteLine ("GetUsnJournalEntries_Raw() : opened journal entries stream ($J) "+rawJournal+", error="+(new Win32Exception(Marshal.GetLastWin32Error()).Message));

			int read = 0;
			byte[] usnraw = new byte[4];

			using(FileStream rawJStream = Alphaleonis.Win32.Filesystem.File.OpenRead(rawJournal)){

				long pos = previousUsnState.NextUsn/*+287834112*/;
				int recordSize = 0;
				byte[] usnrecord;
				long seeked = 0;
				Console.WriteLine ("   #### pre-read="+read+", initial pos="+pos);
				while(pos < newUsnState.NextUsn){
					seeked = rawJStream.Seek(pos, SeekOrigin.Begin);
					Console.WriteLine ("   #### seek() wanted="+pos+", got="+seeked);
					read = rawJStream.Read(usnraw, 0, usnraw.Length);
					recordSize = BitConverter.ToInt32(usnraw, 0);
					Console.WriteLine ("   #### record size="+recordSize);
					if(recordSize == 0){
						Console.WriteLine ("   #### record size is ZERO");
						pos = pos+4;
						continue;
					}
					else if(recordSize <60){
						Console.WriteLine("found too small record, jumping until there is data");
						pos = pos+(int)recordSize;
						while(usnraw[pos] == 0x00){
							Console.WriteLine("found 0; jumping...");
							pos++;
						}
						continue;
					}
					try{
						usnrecord = new byte[recordSize/*usnraw.Length-pos*/];
						rawJStream.Read(usnrecord, 4, recordSize-4);

						Array.Copy(usnraw, 0, usnrecord, 0, 4);
						fixed (byte* p = usnrecord){
						    IntPtr ptr = (IntPtr)p;
							Win32Api.UsnEntry entry = new Win32Api.UsnEntry(ptr);
							Console.WriteLine ("   #### entry-1: "+entry.ToString());
							ptr = IntPtr.Zero;
							usnEntries.Add(entry);
						}

					}
					catch(Exception e){
						throw new Exception("Unparsable USN entry, pos="+pos+", size="+recordSize);
					}
					if(pos == 0)
						pos = previousUsnState.NextUsn;
					else
						pos += (int)recordSize;
						
				}
			}// end using
			return UsnJournalReturnCode.USN_JOURNAL_SUCCESS;
		}


        /// <summary>
        /// tests to see if the USN Journal is active on the volume.
        /// </summary>
        /// <returns>true if USN Journal is active
        /// false if no USN Journal on volume</returns>
        public bool IsUsnJournalActive(){
            DateTime start = DateTime.Now;
            bool bRtnCode = false;
			Console.WriteLine ("IsUsnJournalActive() : _usnJournalRootHandle.ToInt32()="+_usnJournalRootHandle.ToInt32());
            if (bNtfsVolume){
                if (_usnJournalRootHandle.ToInt32() != Win32Api.INVALID_HANDLE_VALUE){
                    Win32Api.USN_JOURNAL_DATA usnJournalCurrentState = new Win32Api.USN_JOURNAL_DATA();
					Console.WriteLine ("IsUsnJournalActive() : querying QueryUsnJournal()");
                    UsnJournalReturnCode usnError = QueryUsnJournal(ref usnJournalCurrentState);
                    if (usnError == UsnJournalReturnCode.USN_JOURNAL_SUCCESS){
                        bRtnCode = true;
                    }
                }
            }
            _elapsedTime = DateTime.Now - start;
            return bRtnCode;
        }

        /// <summary>
        /// tests to see if there is a USN Journal on this volume and if there is 
        /// determines whether any journal entries have been lost.
        /// </summary>
        /// <returns>true if the USN Journal is active and if the JournalId's are the same 
        /// and if all the usn journal entries expected by the previous state are available 
        /// from the current state.
        /// false if not</returns>
        public bool IsUsnJournalValid(Win32Api.USN_JOURNAL_DATA usnJournalPreviousState){
            DateTime start = DateTime.Now;
            bool bRtnCode = false;

            if (bNtfsVolume){
                if (_usnJournalRootHandle.ToInt32() != Win32Api.INVALID_HANDLE_VALUE){
                    Win32Api.USN_JOURNAL_DATA usnJournalState = new Win32Api.USN_JOURNAL_DATA();
                    UsnJournalReturnCode usnError = QueryUsnJournal(ref usnJournalState);

                    if (usnError == UsnJournalReturnCode.USN_JOURNAL_SUCCESS){
                        if (usnJournalPreviousState.UsnJournalID == usnJournalState.UsnJournalID){
                            if (usnJournalPreviousState.NextUsn >= usnJournalState.NextUsn){
                                bRtnCode = true;
                            }
                        }
                    }
                }
            }
            _elapsedTime = DateTime.Now - start;
            return bRtnCode;
        }

        #endregion

        #region private member functions
        /// <summary>
        /// Converts a Win32 Error to a UsnJournalReturnCode
        /// </summary>
        /// <param name="Win32LastError">The 'last' Win32 error.</param>
        /// <returns>
        /// INVALID_HANDLE_VALUE                error generated by Win32 Api calls.
        /// USN_JOURNAL_SUCCESS                 usn journal function succeeded. 
        /// ERROR_INVALID_FUNCTION              error generated by Win32 Api calls.
        /// ERROR_FILE_NOT_FOUND                error generated by Win32 Api calls.
        /// ERROR_PATH_NOT_FOUND                error generated by Win32 Api calls.
        /// ERROR_TOO_MANY_OPEN_FILES           error generated by Win32 Api calls.
        /// ERROR_ACCESS_DENIED                 accessing the usn journal requires admin rights.
        /// ERROR_INVALID_HANDLE                error generated by Win32 Api calls.
        /// ERROR_INVALID_DATA                  error generated by Win32 Api calls.
        /// ERROR_HANDLE_EOF                    error generated by Win32 Api calls.
        /// ERROR_NOT_SUPPORTED                 error generated by Win32 Api calls.
        /// ERROR_INVALID_PARAMETER             error generated by Win32 Api calls.
        /// ERROR_JOURNAL_DELETE_IN_PROGRESS    usn journal delete is in progress.
        /// ERROR_JOURNAL_ENTRY_DELETED         usn journal entry lost, no longer available.
        /// ERROR_INVALID_USER_BUFFER           error generated by Win32 Api calls.
        /// USN_JOURNAL_INVALID                 usn journal is invalid, id's don't match or required entries lost.
        /// USN_JOURNAL_NOT_ACTIVE              usn journal is not active on volume.
        /// VOLUME_NOT_NTFS                     volume is not an NTFS volume.
        /// INVALID_FILE_REFERENCE_NUMBER       bad file reference number - see remarks.
        /// USN_JOURNAL_ERROR                   unspecified usn journal error.
        /// </returns>
        private UsnJournalReturnCode ConvertWin32ErrorToUsnError(Win32Api.GetLastErrorEnum Win32LastError){
           UsnJournalReturnCode usnRtnCode;

           switch (Win32LastError){
                case Win32Api.GetLastErrorEnum.ERROR_JOURNAL_NOT_ACTIVE:
                    usnRtnCode = UsnJournalReturnCode.USN_JOURNAL_NOT_ACTIVE;
                    break;
                case Win32Api.GetLastErrorEnum.ERROR_SUCCESS:
                    usnRtnCode = UsnJournalReturnCode.USN_JOURNAL_SUCCESS;
                    break;
               case Win32Api.GetLastErrorEnum.ERROR_HANDLE_EOF:
                    usnRtnCode = UsnJournalReturnCode.ERROR_HANDLE_EOF;
                    break;
                default:
					Console.WriteLine("usn journal error:"+Win32LastError.ToString());
                    usnRtnCode = UsnJournalReturnCode.USN_JOURNAL_ERROR;
                    break;
            }

           return usnRtnCode;
        }

        /// <summary>
        /// Gets a Volume Serial Number for the volume represented by driveInfo.
        /// </summary>
        /// <param name="driveInfo">DriveInfo object representing the volume in question.</param>
        /// <param name="volumeSerialNumber">out parameter to hold the volume serial number.</param>
        /// <returns></returns>
        private UsnJournalReturnCode GetVolumeSerialNumber(string driveName, out uint volumeSerialNumber){
            //Console.WriteLine("GetVolumeSerialNumber() function entered for drive '{0}'", driveInfo.Name);

            volumeSerialNumber = 0;
            UsnJournalReturnCode usnRtnCode = UsnJournalReturnCode.USN_JOURNAL_SUCCESS;
            //string pathRoot = string.Concat("\\\\.\\", driveName);

            IntPtr hRoot = Win32Api.CreateFile(/*pathRoot*/vol,
                0,
                Win32Api.FILE_SHARE_READ | Win32Api.FILE_SHARE_WRITE,
                IntPtr.Zero,
                Win32Api.OPEN_EXISTING,
                Win32Api.FILE_FLAG_BACKUP_SEMANTICS,
                IntPtr.Zero);

            if (hRoot.ToInt32() != Win32Api.INVALID_HANDLE_VALUE){
                Win32Api.BY_HANDLE_FILE_INFORMATION fi = new Win32Api.BY_HANDLE_FILE_INFORMATION();
                bool bRtn = Win32Api.GetFileInformationByHandle(hRoot, out fi);

                if (bRtn){
                    UInt64 fileIndexHigh = (UInt64)fi.FileIndex.FileIndexHigh;
                    UInt64 indexRoot = (fileIndexHigh << 32) | fi.FileIndex.FileIndexLow;
                    volumeSerialNumber = fi.VolumeSerialNumber;
                }
                else{
                    usnRtnCode = (UsnJournalReturnCode)Marshal.GetLastWin32Error();
                }

                Win32Api.CloseHandle(hRoot);
            }
            else{
                usnRtnCode = (UsnJournalReturnCode)Marshal.GetLastWin32Error();
            }

            return usnRtnCode;
        }

        private UsnJournalReturnCode GetRootHandle(out IntPtr rootHandle){
            //
            // private functions don't need to check for an NTFS volume or
            // a valid _usnJournalRootHandle handle
            //
            UsnJournalReturnCode usnRtnCode = UsnJournalReturnCode.USN_JOURNAL_SUCCESS; 
            rootHandle = IntPtr.Zero;
			//string vol;

			if(!_driveName.StartsWith("\\\\")){
            	vol = string.Concat("\\\\.\\", _driveName.TrimEnd('\\'));
				this.UseRawMode = false;
				//this.UseRawMode = true; // debug to check raw parsing code!
			}
			else{ // vmware mountpoint or VSS snapshot (cases where usn journal is not accessible from regular API)
				//vol = _driveName.Replace ("\\\\.","\\Device"); //+@"$Extend\$UsnJrnl";
				this.UseRawMode = true;
				vol = _driveName.TrimEnd(new char[]{'\\'});
			}

            rootHandle = Win32Api.CreateFile(vol,
                 Win32Api.GENERIC_READ | Win32Api.GENERIC_WRITE,
                 Win32Api.FILE_SHARE_READ | Win32Api.FILE_SHARE_WRITE,
                 IntPtr.Zero,
                 Win32Api.OPEN_EXISTING,
                 0,
                 IntPtr.Zero);
			Console.WriteLine ("NtfsUsnJournal GetRootHandle() : vol="+vol+",  createfile error = "+(new Win32Exception(Marshal.GetLastWin32Error())).Message);
            if (rootHandle.ToInt32() == Win32Api.INVALID_HANDLE_VALUE)
                usnRtnCode = (UsnJournalReturnCode)Marshal.GetLastWin32Error();

            return usnRtnCode;
        }

        /// <summary>
        /// This function queries the usn journal on the volume. 
        /// </summary>
        /// <param name="usnJournalState">the USN_JOURNAL_DATA object that is associated with this volume</param>
        /// <returns></returns>
        private UsnJournalReturnCode QueryUsnJournal(ref Win32Api.USN_JOURNAL_DATA usnJournalState){
            //
            // private functions don't need to check for an NTFS volume or
            // a valid _usnJournalRootHandle handle
            //
            
			if(this.UseRawMode) return Raw_QueryUsnJournal(ref usnJournalState);


			UsnJournalReturnCode usnReturnCode = UsnJournalReturnCode.USN_JOURNAL_SUCCESS;
            int sizeUsnJournalState = Marshal.SizeOf(usnJournalState);
            UInt32 cb;

            bool fOk = Win32Api.DeviceIoControl(
                _usnJournalRootHandle,
                Win32Api.FSCTL_QUERY_USN_JOURNAL,
                IntPtr.Zero,
                0,
                out usnJournalState,
                sizeUsnJournalState,
                out cb,
                IntPtr.Zero);

            if (!fOk)
               // int lastWin32Error = Marshal.GetLastWin32Error();
                usnReturnCode = ConvertWin32ErrorToUsnError((Win32Api.GetLastErrorEnum)Marshal.GetLastWin32Error());

            return usnReturnCode;
        }

		// Parse raw $UsnJrnl:$J
		private UsnJournalReturnCode Raw_QueryUsnJournal(ref Win32Api.USN_JOURNAL_DATA usnJournalState){
            //
            // private functions don't need to check for an NTFS volume or
            // a valid _usnJournalRootHandle handle
            //
			Win32Api.USN_JOURNAL_DATA jData = new Win32Api.USN_JOURNAL_DATA();
            UsnJournalReturnCode usnReturnCode = UsnJournalReturnCode.USN_JOURNAL_SUCCESS;
            int sizeUsnJournalState = Marshal.SizeOf(usnJournalState);
			string maxHandle = vol + @"\$Extend\$UsnJrnl:$Max";
			string rawJournal= vol + @"\$Extend\$UsnJrnl:$J";
			/*IntPtr rawUsnHandle = Win32Api.CreateFile(vol,
                 Win32Api.GENERIC_READ | Win32Api.GENERIC_WRITE,
                 Win32Api.FILE_SHARE_READ | Win32Api.FILE_SHARE_WRITE,
                 IntPtr.Zero,
                 Win32Api.OPEN_EXISTING,
                 0,
                 IntPtr.Zero);*/

			Console.WriteLine ("Raw_QueryUsnJournal() : maxhandle="+maxHandle+", rawj="+rawJournal);


			IntPtr journalDataHandle = Win32Api.CreateFile(rawJournal, 
			 	0, 
			  	Win32Api.FILE_SHARE_READ| Win32Api.FILE_SHARE_WRITE,
				IntPtr.Zero, Win32Api.OPEN_EXISTING,  
				0, IntPtr.Zero);


			// if invalid handle, no usn on this drive
			usnReturnCode = ConvertWin32ErrorToUsnError((Win32Api.GetLastErrorEnum)Marshal.GetLastWin32Error());
			if(usnReturnCode != UsnJournalReturnCode.USN_JOURNAL_SUCCESS)
				return usnReturnCode;


			Win32Api.BY_HANDLE_FILE_INFORMATION fileInfo = new Win32Api.BY_HANDLE_FILE_INFORMATION();
			Win32Api.GetFileInformationByHandle(journalDataHandle, out fileInfo);

			//Console.WriteLine ("usn log sizeH="+fileInfo.FileSizeHigh+", sizeL="+fileInfo.FileSizeLow);
			jData.NextUsn = fileInfo.FileSizeLow;
			Win32Api.CloseHandle(journalDataHandle);
		

			//NTStream maxStream = new NTStream(maxHandle, FileMode.Open);
			FileStream maxStream = Alphaleonis.Win32.Filesystem.File.OpenRead(maxHandle);
			byte[] maxData = new byte[32];
			maxStream.Read(maxData, 0, 32);
			//Console.WriteLine ("             @@ raw_queryusnjournal jid="+(int)BitConverter.ToUInt64(maxData, 16));
			jData.MaximumSize = BitConverter.ToUInt64(maxData, 0);
			jData.AllocationDelta = BitConverter.ToUInt64(maxData, 8);
			jData.UsnJournalID = BitConverter.ToUInt64(maxData, 16);
			jData.LowestValidUsn = BitConverter.ToInt64(maxData, 24);
			maxStream.Close();

			//Console.WriteLine ("usnj : maxsize="+jData.MaximumSize+", allocdelta="+jData.AllocationDelta+", id="+jData.UsnJournalID+", lowest="+jData.LowestValidUsn);
			usnJournalState = jData;
            return usnReturnCode;
        }


        #endregion

        #region IDisposable Members

        public void Dispose(){
            Win32Api.CloseHandle(_usnJournalRootHandle);
        }

        #endregion
    }
}



