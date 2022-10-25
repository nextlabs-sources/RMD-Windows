using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;
using static Vanara.PInvoke.Kernel32;

namespace nxrmvirtualdrivelib.native
{
    public class WinNative
    {
        #region Kernel32 API
        public static SafeHFILE CreateFile(string lpFileName, Kernel32.FileAccess dwDesiredAccess, FileShare dwShareMode,
            SECURITY_ATTRIBUTES lpSecurityAttributes, FileMode dwCreationDisposition, FileFlagsAndAttributes dwFlagsAndAttributes, HFILE hTemplateFile)
        {
            return Kernel32.CreateFile(lpFileName, dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
        }

        public static bool GetFileInformationByHandleEx(HFILE hFile, FILE_INFO_BY_HANDLE_CLASS fileInformationClass,
            SafeAllocatedMemoryHandle lpFileInformation, uint dwBufferSize)
        {
            return Kernel32.GetFileInformationByHandleEx(hFile, fileInformationClass, lpFileInformation, dwBufferSize);
        }

        public static T GetFileInformationByHandleEx<T>(HFILE hFile, FILE_INFO_BY_HANDLE_CLASS fileInformationClass) where T : struct
        {
            return Kernel32.GetFileInformationByHandleEx<T>(hFile, fileInformationClass);
        }

        public static FileFlagsAndAttributes GetFileAttributes(string lpFileName)
        {
            return Kernel32.GetFileAttributes(lpFileName);
        }

        public static bool GetVolumeInformation(string rootPathName, out string volumeName, out uint volumeSerialNumber, out uint maximumComponentLength, out FileSystemFlags fileSystemFlags, out string fileSystemName)
        {
            return Kernel32.GetVolumeInformation(rootPathName, out volumeName, out volumeSerialNumber, out maximumComponentLength, out fileSystemFlags, out fileSystemName);
        }

        public static uint GetFinalPathNameByHandle(HFILE hFile, StringBuilder lpszFilePath, uint cchFilePath, FinalPathNameOptions dwFlags)
        {
            return Kernel32.GetFinalPathNameByHandle(hFile, lpszFilePath, cchFilePath, dwFlags);
        }

        public static SafeHFILE OpenFileById(HFILE hVolumeHint, in FILE_ID_DESCRIPTOR lpFileId, Kernel32.FileAccess dwDesiredAccess, FileShare dwShareMode, SECURITY_ATTRIBUTES lpSecurityAttributes, FileFlagsAndAttributes dwFlagsAndAttributes)
        {
            return Kernel32.OpenFileById(hVolumeHint, lpFileId, dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwFlagsAndAttributes);
        }

        public static Task<TOut?> DeviceIoControlAsync<TOut>(HFILE hDev, uint ioControlCode) where TOut : struct
        {
            return Kernel32.DeviceIoControlAsync<TOut>(hDev, ioControlCode);
        }

        public static bool DeviceIoControl<TOut>(HFILE hDev, uint ioControlCode, out TOut outVal) where TOut : struct
        {
            return Kernel32.DeviceIoControl(hDev, ioControlCode, out outVal);
        }

        public static Win32Error GetLastError()
        {
            return Kernel32.GetLastError();
        }
        #endregion

        #region Cloud filter API 
        public static HRESULT ConnectSyncRoot(string syncRootPath, CF_CALLBACK_REGISTRATION[] callbackTable, IntPtr callbackContext,
            CF_CONNECT_FLAGS connectFlags, out CF_CONNECTION_KEY connectionKey)
        {
            return CfConnectSyncRoot(syncRootPath, callbackTable, callbackContext, connectFlags, out connectionKey);
        }

        public static HRESULT DisConnectSyncRoot(CF_CONNECTION_KEY connectionKey)
        {
            return CfDisconnectSyncRoot(connectionKey);
        }

        public static HRESULT Execute(CF_OPERATION_INFO opInfo, ref CF_OPERATION_PARAMETERS opParams)
        {
            return CfExecute(opInfo, ref opParams);
        }

        public static HRESULT CreatePlaceholders(string baseDirectoryPath, CF_PLACEHOLDER_CREATE_INFO[] placeholderArray, uint placeholderCount, CF_CREATE_FLAGS createFlags, out uint entriesProcessed)
        {
            return CfCreatePlaceholders(baseDirectoryPath, placeholderArray, placeholderCount, createFlags, out entriesProcessed);
        }

        public static HRESULT ConvertToPlaceholder(HFILE fileHandle, IntPtr fileIdentity, uint fileIdentityLength, CF_CONVERT_FLAGS convertFlags, out long convertUsn, IntPtr overlapped)
        {
            return CfConvertToPlaceholder(fileHandle, fileIdentity, fileIdentityLength, convertFlags, out convertUsn, overlapped);
        }

        public static CF_PLACEHOLDER_STATE GetPlaceholderState(IntPtr infoBuffer, FILE_INFO_BY_HANDLE_CLASS fileInformationClass)
        {
            return CfGetPlaceholderStateFromFileInfo(infoBuffer, fileInformationClass);
        }

        public static HRESULT GetPlaceholderInfo(HFILE hFile, CF_PLACEHOLDER_INFO_CLASS infoClass, IntPtr infoBuffer, uint infoBufferLength, out uint returnedLength)
        {
            return CfGetPlaceholderInfo(hFile, infoClass, infoBuffer, infoBufferLength, out returnedLength);
        }

        public static T GetPlaceholderInfo<T>(HFILE hFile) where T : struct
        {
            return CfGetPlaceholderInfo<T>(hFile);
        }

        public static HRESULT UpdatePlaceholder(HFILE hFile, in CF_FS_METADATA metadata,
            IntPtr fileIdentity, uint fileIdentityLength,
            CF_FILE_RANGE[] dehydrateRangeArray, uint dehydrateRangeCount,
            CF_UPDATE_FLAGS updateFlags, ref long updateUsn, IntPtr overlapped)
        {
            return CfUpdatePlaceholder(hFile, metadata, fileIdentity, fileIdentityLength,
                dehydrateRangeArray, dehydrateRangeCount, updateFlags, ref updateUsn, overlapped);
        }

        public static HRESULT HydratePlaceholder(HFILE fileHandle, long startingOffset = 0, long length = -1, CF_HYDRATE_FLAGS hydrateFlags = CF_HYDRATE_FLAGS.CF_HYDRATE_FLAG_NONE, IntPtr overlapped = default)
        {
            return CfHydratePlaceholder(fileHandle, startingOffset, length, hydrateFlags, overlapped);
        }

        public static HRESULT ReportProviderProgess(CF_CONNECTION_KEY connectionKey, CF_TRANSFER_KEY transferKey,
            long providerProgressTotal, long providerProgressCompleted)
        {
            return CfReportProviderProgress(connectionKey, transferKey, providerProgressTotal, providerProgressCompleted);
        }

        public static CF_PLACEHOLDER_STATE GetPlaceholderStateFromFileInfo(IntPtr infoBuffer, FILE_INFO_BY_HANDLE_CLASS infoClass)
        {
            return CfGetPlaceholderStateFromFileInfo(infoBuffer, infoClass);
        }

        public static HRESULT SetInSyncState(HFILE fileHandle, CF_IN_SYNC_STATE inSyncState, CF_SET_IN_SYNC_FLAGS inSyncFlags, ref long inSyncUsn)
        {
            return CfSetInSyncState(fileHandle, inSyncState, inSyncFlags, ref inSyncUsn);
        }

        #endregion

        public static SafeFileHandle CreateFile(string lpFileName, DesiredAccess desiredAccess,
            FileShare shareMode, SecurityAttributes securityAttributes, FileMode createtionDisposition,
            FlagsAndAttributes flagsAndAttributes, [Optional] IntPtr templateFile)
        {
            return CreateFile(lpFileName, (uint)desiredAccess,
                (uint)shareMode, securityAttributes, (uint)createtionDisposition,
                (uint)flagsAndAttributes, templateFile);
        }

        /*
         HANDLE CreateFileA(
  [in]           LPCSTR                lpFileName,
  [in]           DWORD                 dwDesiredAccess,
  [in]           DWORD                 dwShareMode,
  [in, optional] LPSECURITY_ATTRIBUTES lpSecurityAttributes,
  [in]           DWORD                 dwCreationDisposition,
  [in]           DWORD                 dwFlagsAndAttributes,
  [in, optional] HANDLE                hTemplateFile
);
             */
        ///// <summary>
        ///// 
        ///// </summary>
        ///// <returns></returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, EntryPoint = "CreateFile", SetLastError = true)]
        private static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess,
            uint dwShareMode, SecurityAttributes lpSecurityAttributes, uint dwCreationDisposition,
            uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [StructLayout(LayoutKind.Sequential)]
        public class SecurityAttributes
        {
            public int Size = Marshal.SizeOf(typeof(SecurityAttributes));
            public IntPtr LpSecurityDescriptor;
            [MarshalAs(UnmanagedType.Bool)]
            public bool BInheritHandle;
        }

        [Flags]
        public enum DesiredAccess : uint
        {
            FILE_LIST_DIRECTORY = 1u,
            FILE_READ_DATA = 1u,
            FILE_ADD_FILE = 2u,
            FILE_WRITE_DATA = 2u,
            FILE_ADD_SUBDIRECTORY = 4u,
            FILE_APPEND_DATA = 4u,
            FILE_READ_EA = 8u,
            FILE_WRITE_EA = 0x10u,
            FILE_TRAVERSE = 0x20u,
            FILE_DELETE_CHILD = 0x40u,
            FILE_READ_ATTRIBUTES = 0x80,
            FILE_WRITE_ATTRIBUTES = 0x100,
        }

        [Flags]
        public enum FlagsAndAttributes : uint
        {
            FILE_ATTRIBUTE_READONLY = 1u,
            FILE_ATTRIBUTE_HIDDEN = 2u,
            FILE_ATTRIBUTE_SYSTEM = 4u,
            FILE_ATTRIBUTE_DIRECTORY = 0x10u,
            FILE_ATTRIBUTE_ARCHIVE = 0x20u,
            FILE_ATTRIBUTE_DEVICE = 0x40u,
            FILE_ATTRIBUTE_NORMAL = 0x80u,
            FILE_ATTRIBUTE_TEMPORARY = 0x100u,
            FILE_ATTRIBUTE_SPARSE_FILE = 0x200u,
            FILE_ATTRIBUTE_REPARSE_POINT = 0x400u,
            FILE_ATTRIBUTE_COMPRESSED = 0x800u,
            FILE_ATTRIBUTE_OFFLINE = 0x1000u,
            FILE_ATTRIBUTE_NOT_CONTENT_INDEXED = 0x2000u,
            FILE_ATTRIBUTE_ENCRYPTED = 0x4000u,
            FILE_ATTRIBUTE_INTEGRITY_STREAM = 0x8000u,
            FILE_ATTRIBUTE_VIRTUAL = 0x10000u,
            FILE_ATTRIBUTE_NO_SCRUB_DATA = 0x20000u,
            FILE_ATTRIBUTE_RECALL_ON_OPEN = 0x40000u,
            FILE_ATTRIBUTE_PINNED = 0x80000u,
            FILE_ATTRIBUTE_UNPINNED = 0x100000u,
            FILE_FLAG_POSIX_SEMANTICS = 0x1000000u,
            FILE_FLAG_RANDOM_ACCESS = 0x10000000u,
            FILE_FLAG_OPEN_REPARSE_POINT = 0x200000u,
            FILE_FLAG_BACKUP_SEMANTICS = 0x2000000u,
            FILE_FLAG_NO_BUFFERING = 0x20000000u,
            FILE_FLAG_DELETE_ON_CLOSE = 0x4000000u,
            FILE_FLAG_OVERLAPPED = 0x40000000u,
            FILE_FLAG_SESSION_AWARE = 0x800000u,
            FILE_FLAG_SEQUENTIAL_SCAN = 0x8000000u,
            FILE_FLAG_WRITE_THROUGH = 0x80000000u
        }
    }
}
