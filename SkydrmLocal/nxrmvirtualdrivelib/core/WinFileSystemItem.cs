using nxrmvirtualdrivelib.native;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;

namespace nxrmvirtualdrivelib.core
{
    public class WinFileSystemItem : IDisposable
    {
        public readonly SafeHFILE SafeHandle;
        private bool mDisposed = false;

        private WinFileSystemItem(SafeHFILE handle)
        {
            SafeHandle = handle;
        }

        public static WinFileSystemItem OpenFile(string path, FileMode fileMode, FileShare fileShare)
        {
            return OpenFile(path, Kernel32.FileAccess.FILE_READ_ATTRIBUTES, fileMode, fileShare);
        }

        public static WinFileSystemItem OpenFile(string path, Kernel32.FileAccess desiredAccess, FileMode fileMode, FileShare fileShare)
        {
            if (TryCreateFile(path, desiredAccess, fileShare, fileMode, out SafeHFILE handle))
            {
                if (handle.IsInvalid)
                {
                    return null;
                }
                return new WinFileSystemItem(handle);
            }
            return null;
        }

        public static bool TryGetAttributes(string path, out FileAttributes? attributes)
        {
            try
            {
                attributes = (FileAttributes)WinNative.GetFileAttributes(path);
                if ((int)attributes.Value != -1)
                {
                    return true;
                }
            }
            catch
            {

            }
            attributes = null;
            return false;
        }

        public static string GetFileAttributes(FileAttributes attributes)
        {
            List<FileFlagsAndAttributes> attrLists = Enumerable.ToList(Enumerable.Cast<FileFlagsAndAttributes>(Enum.GetValues(typeof(FileFlagsAndAttributes))));
            string text = "";
            foreach (var item in attrLists)
            {
                text += (((uint)attributes & (uint)item) > 0) ? item.ToString() : "";
            }
            return text;
        }

        public static byte[] GetItemIdByPath(string path)
        {
            using (WinFileSystemItem item = OpenFile(path, Kernel32.FileAccess.FILE_READ_ATTRIBUTES,
                FileMode.Open, FileShare.ReadWrite | FileShare.Delete))
            {
                return item.GetItemId();
            }
        }

        public static string GetPathByItemId(byte[] itemId)
        {
            if (!GetPathByItemIdInternal(itemId, out string path))
            {
                return null;
            }
            return path;
        }

        public static bool GetVolumeBySerialNumber(ulong serialNumber, out string volumePathName)
        {
            volumePathName = null;
            foreach (var val in DriveInfo.GetDrives())
            {
                var name = val.Name;
                if (WinNative.GetVolumeInformation(val.Name, out string volumeName,
                    out uint volumeSerialNumber, out uint maximumComponentLength,
                    out FileSystemFlags fileSystemFlags, out string fileSystemName))
                {
                    if (volumeSerialNumber == serialNumber)
                    {
                        volumePathName = name;
                        return true;
                    }
                }
            }
            return false;
        }

        public byte[] GetItemId()
        {
            var fileIdInfo = GetFileIdInfo();
            WinFileItemIdentifier fileId = new WinFileItemIdentifier(fileIdInfo.FileId.Identifier, (uint)fileIdInfo.VolumeSerialNumber);
            return fileId.MarshalAs();
        }

        public ulong GetSerialNumber()
        {
            var fileIdInfo = GetFileIdInfo();
            return fileIdInfo.VolumeSerialNumber;
        }

        public FILE_ID_INFO GetFileIdInfo()
        {
            return WinNative.GetFileInformationByHandleEx<FILE_ID_INFO>(SafeHandle, FILE_INFO_BY_HANDLE_CLASS.FileIdInfo);
        }

        internal static bool TryCreateFile(string path, Kernel32.FileAccess desiredAccess,
            FileShare fileShare, FileMode fileMode, out SafeHFILE handle)
        {
            handle = WinNative.CreateFile(path, desiredAccess,
                fileShare, null, fileMode, FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS, HFILE.NULL);
            return !handle.IsInvalid;
        }

        internal static bool GetPathByItemIdInternal(byte[] itemId, out string path)
        {
            path = "";
            if (!GetWinFileSystemItemByItemId(itemId, Kernel32.FileAccess.FILE_READ_ATTRIBUTES, FileShare.ReadWrite | FileShare.Delete,
                out WinFileSystemItem item))
            {
                return false;
            }
            using (item)
            {
                uint len = WinNative.GetFinalPathNameByHandle(item.SafeHandle, null, 0u, FinalPathNameOptions.FILE_NAME_NORMALIZED);
                if (len == 0)
                {
                    return false;
                }
                StringBuilder pathBuilder = new StringBuilder((int)len);
                if (WinNative.GetFinalPathNameByHandle(item.SafeHandle, pathBuilder, len, FinalPathNameOptions.FILE_NAME_NORMALIZED) == 0)
                {
                    return false;
                }
                path = pathBuilder.ToString();
            }
            return true;
        }

        internal static bool GetWinFileSystemItemByItemId(byte[] itemId, Kernel32.FileAccess desiredAccess, FileShare fileShare,
            out WinFileSystemItem item)
        {
            item = null;
            if (!WinFileItemIdentifier.TryMarshalFrom(itemId, out WinFileItemIdentifier parser) || !GetVolumeBySerialNumber(parser.VolumeSerialNum, out string volumeName))
            {
                return false;
            }
            using (SafeHFILE hVolume = WinNative.CreateFile(volumeName, Kernel32.FileAccess.GENERIC_READ, FileShare.ReadWrite | FileShare.Delete, null,
                FileMode.Open, FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero))
            {
                if (hVolume == null || hVolume.IsInvalid)
                {
                    return false;
                }
                FILE_ID_DESCRIPTOR fileId = FILE_ID_DESCRIPTOR.Default;

                FILE_ID_DESCRIPTOR.DUMMYUNIONNAME id = new FILE_ID_DESCRIPTOR.DUMMYUNIONNAME
                {
                    FileId = (long)parser.ItemIdLow
                };
                fileId.Type = FILE_ID_TYPE.FileIdType;
                fileId.Id = id;

                SafeHFILE hFile = WinNative.OpenFileById(hVolume, fileId, desiredAccess, fileShare, null,
                    FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS);
                if (hFile == null || hFile.IsInvalid)
                {
                    return false;
                }
                item = new WinFileSystemItem(hFile);
            }
            return true;
        }

        public static async Task<long> GetUSNAsyncA(string path)
        {
            (bool IsSuccess, long? USN) = await GetUSNAsync(path);
            if (IsSuccess)
            {
                return USN.Value;
            }
            return -1;
        }

        public static async Task<(bool IsSuccess, long? USN)> GetUSNAsync(string path)
        {
            using (WinFileSystemItem item = OpenFile(path, FileMode.Open, FileShare.ReadWrite | FileShare.Delete))
            {
                var hDev = item.SafeHandle;
                if (hDev.IsInvalid)
                {
                    return (true, null);
                }
                var record = await GetUSNRecordV3Async(item.SafeHandle);
                if (record.HasValue)
                {
                    return (true, record.Value.Usn);
                }
            }
            return (false, null);
        }

        private static async Task<USN_RECORD_V3?> GetUSNRecordV3Async(HFILE hDev)
        {
            USN_RECORD_V3 rt = new USN_RECORD_V3();
            WinNative.DeviceIoControl(hDev, IOControlCode.FSCTL_READ_FILE_USN_DATA, out rt);
            return rt;
        }

        public virtual void Close(bool disposing = true)
        {
            if (!mDisposed)
            {
                if (disposing)
                {
                    SafeHandle?.Close();
                    SafeHandle?.Dispose();
                }
                mDisposed = true;
            }
        }

        public void Dispose()
        {
            Close(true);
        }
    }
}
