using nxrmvirtualdrivelib.core;
using nxrmvirtualdrivelib.ext;
using nxrmvirtualdrivelib.metadata;
using nxrmvirtualdrivelib.native;
using nxrmvirtualdrivelib.nxl;
using nxrmvirtualdrivelib.utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;
using static Vanara.PInvoke.Kernel32;

namespace nxrmvirtualdrivelib.placeholder
{
    public abstract class PlaceholderItem
    {
        /// <summary>
        /// Path of this placeholder in the user file system.
        /// </summary>
        protected readonly string LocalPath;
        protected ulong? FileId;

        public PlaceholderItem(string path)
        {
            this.LocalPath = path;
        }

        public abstract Task<(bool IsSuccess, bool? IsNew)> IsNew();

        public abstract Task<(bool IsSuccess, bool? IsMoved)> IsMoved();

        public abstract Task<bool> DeleteAsync(CancellationToken token);

        public abstract Task<bool> UpdateAsync(IFileSystemItemMetadata metadata, bool autoHydration = true);

        public static PlaceholderItem GetPlaceholderItem(string path)
        {
            FileSystemItemType itemType = FileHelper.IsDirectory(path) ? FileSystemItemType.Folder : FileSystemItemType.File;
            return GetPlaceholderItem(path, itemType);
        }

        public static bool TryGetPlaceholderItem(string path, out PlaceholderItem item)
        {
            if (WinFileSystemItem.TryGetAttributes(path, out var attributes) && attributes.HasValue)
            {
                FileSystemItemType itemType = (attributes.Value & FileAttributes.Directory) != 0 ? FileSystemItemType.Folder : FileSystemItemType.File;
                item = GetPlaceholderItem(path, itemType);
                return true;
            }
            item = null;
            return false;
        }

        public static PlaceholderItem GetPlaceholderItem(string path, FileSystemItemType itemType)
        {
            if (itemType == FileSystemItemType.File)
            {
                return new PlaceholderFile(path);
            }
            return new PlaceholderFolder(path);
        }

        public static PlaceholderItemIdentity GetPlaceHolderId(string path, bool checkMagicNum)
        {
            using (WinFileSystemItem item = WinFileSystemItem.OpenFile(path, Kernel32.FileAccess.FILE_READ_ATTRIBUTES,
                FileMode.Open, FileShare.ReadWrite | FileShare.Delete))
            {
                return GetPlacehodlerFileId(item.SafeHandle, checkMagicNum);
            }
        }

        public static bool TryGetPlaceholderId(string path, bool checkMagicNum, out PlaceholderItemIdentity id)
        {
            using (WinFileSystemItem item = WinFileSystemItem.OpenFile(path, Kernel32.FileAccess.FILE_READ_ATTRIBUTES,
                FileMode.Open, FileShare.ReadWrite | FileShare.Delete))
            {
                id = GetPlacehodlerFileId(item.SafeHandle, checkMagicNum);
                return id != null;
            }
        }

        protected ulong GetFileId()
        {
            this.FileId = this.FileId ?? GetFileId(Path.GetDirectoryName(LocalPath.TrimEnd(new char[1] { Path.DirectorySeparatorChar })));
            return FileId.Value;
        }

        private ulong? GetFileId(string path)
        {
            using (WinFileSystemItem item = WinFileSystemItem.OpenFile(path, FileMode.Open, FileShare.ReadWrite | FileShare.Delete))
            {
                var fileIdInfo = WinNative.GetFileInformationByHandleEx<FILE_ID_INFO>(item.SafeHandle, FILE_INFO_BY_HANDLE_CLASS.FileIdInfo);
                WinFileItemIdentifier wid = new WinFileItemIdentifier(fileIdInfo.FileId.Identifier, (uint)fileIdInfo.VolumeSerialNumber);
                return wid.ItemIdLow;
            }
        }

        private static bool TryGetFileId(string path, out ulong? fileId)
        {
            using (WinFileSystemItem item = WinFileSystemItem.OpenFile(path, FileMode.Open, FileShare.ReadWrite | FileShare.Delete))
            {
                var fileIdInfo = WinNative.GetFileInformationByHandleEx<FILE_ID_INFO>(item.SafeHandle, FILE_INFO_BY_HANDLE_CLASS.FileIdInfo);
                WinFileItemIdentifier wid = new WinFileItemIdentifier(fileIdInfo.FileId.Identifier, (uint)fileIdInfo.VolumeSerialNumber);
                fileId = wid.ItemIdLow;
                return true;
            }
        }

        public byte[] GetRemoteStorageItemId()
        {
            var id = GetPlaceHolderId(LocalPath, true);
            if (id != null)
            {
                return id.RemoteStorageItemId;
            }
            return null;
        }

        public bool TryGetRemoteStorageItemId(out byte[] remoteStorageItemId)
        {
            remoteStorageItemId = null;
            if (TryGetPlaceholderId(LocalPath, true, out var id))
            {
                remoteStorageItemId = id.RemoteStorageItemId;
                return true;
            }
            return false;
        }

        public void SetRemoteStorageItemId(byte[] remoteStorageItemId)
        {
            using (WinFileSystemItem item = WinFileSystemItem.OpenFile(LocalPath,
                Kernel32.FileAccess.FILE_READ_ATTRIBUTES | Kernel32.FileAccess.FILE_WRITE_ATTRIBUTES,
                FileMode.Open, FileShare.ReadWrite | FileShare.Delete))
            {
                var phId = GetPlacehodlerFileId(item.SafeHandle, true);
                if (phId == null)
                {
                    string directoryName = Path.GetDirectoryName(LocalPath.TrimEnd(new char[1] { Path.DirectorySeparatorChar }));
                    FileSystemItemType itemType = (!(this is PlaceholderFile)) ? FileSystemItemType.Folder : FileSystemItemType.File;
                    phId = new PlaceholderItemIdentity(itemType, remoteStorageItemId, null, true, GetFileId(), directoryName);
                }
                phId.RemoteStorageItemId = remoteStorageItemId;
                UpdatePlaceholder(item.SafeHandle, phId, null);
            }
        }

        public PlaceholderItemIdentity GetPlaceholderId()
        {
            if (TryGetPlaceholderId(LocalPath, true, out var id))
            {
                return id;
            }
            return null;
        }

        private static bool UpdatePlaceholder(SafeHFILE hFile, PlaceholderItemIdentity phId, bool? markInSync = null)
        {
            CF_UPDATE_FLAGS updateFlags = CF_UPDATE_FLAGS.CF_UPDATE_FLAG_NONE;
            if (markInSync.HasValue && markInSync.Value)
            {
                updateFlags |= CF_UPDATE_FLAGS.CF_UPDATE_FLAG_MARK_IN_SYNC;
            }
            if (markInSync.HasValue && !markInSync.Value)
            {
                updateFlags |= CF_UPDATE_FLAGS.CF_UPDATE_FLAG_CLEAR_IN_SYNC;
            }
            var itemIdData = phId.MarshalAs();
            IntPtr fileIdPtr = IntPtr.Zero;

            try
            {
                var fileIdLen = itemIdData.Length;
                fileIdPtr = Marshal.AllocHGlobal(fileIdLen);
                Marshal.Copy(itemIdData, 0, fileIdPtr, fileIdLen);

                CF_FS_METADATA metadata = new CF_FS_METADATA();
                long updateUsn = 0L;
                var result = WinNative.UpdatePlaceholder(hFile, metadata, fileIdPtr, (uint)fileIdLen, null,
                    0u, updateFlags, ref updateUsn, IntPtr.Zero);

                return result == HRESULT.S_OK;
            }
            finally
            {
                if (fileIdPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(fileIdPtr);
                }
            }
        }

        protected static bool UpdatePlaceholder(SafeHFILE hFile, long startingOffset, long length)
        {
            CF_FILE_RANGE range = new CF_FILE_RANGE
            {
                StartingOffset = startingOffset,
                Length = length
            };

            CF_UPDATE_FLAGS updateFlags = CF_UPDATE_FLAGS.CF_UPDATE_FLAG_DEHYDRATE;

            CF_FS_METADATA metadata = new CF_FS_METADATA();
            long updateUsn = 0L;
            WinNative.UpdatePlaceholder(hFile, metadata, IntPtr.Zero, 0u, new CF_FILE_RANGE[1] { range },
                0u, updateFlags, ref updateUsn, IntPtr.Zero).CheckResult();

            return true;
        }

        private static PlaceholderItemIdentity GetPlacehodlerFileId(SafeHFILE hFile, bool checkMagicNum)
        {
            var phId = GetPlaceholderFileId(hFile);
            if (phId != null && phId.Length != 0)
            {
                return PlaceholderItemIdentity.MarshalFrom(phId, checkMagicNum);
            }
            return null;
        }

        private static byte[] GetPlaceholderFileId(SafeHFILE hFile)
        {
            return GetPlaceholderBasicInfo(hFile).FileIdentity;
        }

        public static CF_PLACEHOLDER_BASIC_INFO GetPlaceholderBasicInfo(SafeHFILE hFile)
        {
            return WinNative.GetPlaceholderInfo<CF_PLACEHOLDER_BASIC_INFO>(hFile);
        }

        public static CF_PLACEHOLDER_STANDARD_INFO GetPlaceholderStandardInfo(SafeHFILE hFile)
        {
            return WinNative.GetPlaceholderInfo<CF_PLACEHOLDER_STANDARD_INFO>(hFile);
        }

        public PlaceholderItem GetItemByType(string path, FileSystemItemType itemType)
        {
            if (itemType == FileSystemItemType.File)
            {
                return new PlaceholderFile(path);
            }
            return new PlaceholderFolder(path);
        }

        public FileSystemItemType GetItemType()
        {
            return this is PlaceholderFile ? FileSystemItemType.File : FileSystemItemType.Folder;
        }

        public static (CF_PLACEHOLDER_CREATE_INFO phCreateInfo, GCHandle pinnedFileIdentity, byte[] fileIdentityData) Convert(bool checkFile,
            ulong? fileId, IFileSystemItemMetadata metadata, bool allowConflict = true)
        {
            byte[] itemId = GetItemId(metadata, checkFile, fileId);
            GCHandle itemGCHandle = GCHandle.Alloc(itemId, GCHandleType.Pinned);

            HRESULT result = new HRESULT();
            var phInfo = CreatePlaceholder(metadata, itemGCHandle.AddrOfPinnedObject(), (uint)itemId.Length, ref result, allowConflict);
            if (result != HRESULT.S_OK)
            {
                itemGCHandle.Free();
            }
            return (phInfo, itemGCHandle, itemId);
        }

        protected static byte[] GetItemId(IFileSystemItemMetadata metadata, bool checkFile, ulong? fileId)
        {
            FileSystemItemType itemType = ((metadata.Attributes & FileAttributes.Directory) != 0) ? FileSystemItemType.Folder : FileSystemItemType.File;
            PlaceholderItemIdentity id = null;
            if (itemType == FileSystemItemType.File)
            {
                FileMetadata fileMetadata = (FileMetadata)metadata;
                id = new PlaceholderItemIdentity(itemType, metadata.RemoteStorageItemId, null, checkFile, fileId, metadata.Name,
                    fileMetadata.IsNxlFile, fileMetadata.IsAnonymousNxlFile, fileMetadata.IsConfict);
            }
            else
            {
                id = new PlaceholderItemIdentity(itemType, metadata.RemoteStorageItemId, null, checkFile, fileId, metadata.Name);
            }
            return id.MarshalAs();
        }

        private static CF_PLACEHOLDER_CREATE_INFO CreatePlaceholder(IFileSystemItemMetadata metadata, IntPtr fileIdPtr,
            uint fileIdLen, ref HRESULT result, bool allowConflict = true)
        {
            CF_FS_METADATA metaData = ConvertMetadata(metadata);

            CF_PLACEHOLDER_CREATE_INFO info = new CF_PLACEHOLDER_CREATE_INFO()
            {
                RelativeFileName = GetFileName(metadata, allowConflict),
                FsMetadata = metaData,
                FileIdentity = fileIdPtr,
                FileIdentityLength = fileIdLen,
                Flags = CF_PLACEHOLDER_CREATE_FLAGS.CF_PLACEHOLDER_CREATE_FLAG_MARK_IN_SYNC,
                Result = result,
            };
            return info;
        }

        private static string GetFileName(IFileSystemItemMetadata metadata, bool allowConflict = true)
        {
            if (metadata == null)
            {
                return "";
            }
            if (metadata is FileMetadata && !allowConflict)
            {
                FileMetadata fileMetadata = (FileMetadata)metadata;
                if (fileMetadata.IsConfict)
                {
                    return fileMetadata.ConfictName;
                }
                var isAnonymousNxlFile = fileMetadata.IsAnonymousNxlFile;
                if (isAnonymousNxlFile)
                {
                    return NxlFile.AppendNxlExt(fileMetadata.Name);
                }
            }
            return metadata.Name;
        }

        protected static CF_FS_METADATA ConvertMetadata(IFileSystemItemMetadata metadata)
        {
            ValidateFileSystemMeatadata(metadata);

            FILE_BASIC_INFO basicInfo = new FILE_BASIC_INFO
            {
                CreationTime = FromDateTime(metadata.CreationTime),
                LastWriteTime = FromDateTime(metadata.LastWriteTime),
                LastAccessTime = FromDateTime(metadata.LastAccessTime),
                ChangeTime = FromDateTime(metadata.ChangeTime),
                FileAttributes = (FileFlagsAndAttributes)metadata.Attributes
            };

            CF_FS_METADATA metaData = new CF_FS_METADATA
            {
                BasicInfo = basicInfo,
                FileSize = metadata.Length
            };
            if ((metadata.Attributes & FileAttributes.Directory) != 0)
            {
                basicInfo.FileAttributes |= FileFlagsAndAttributes.FILE_ATTRIBUTE_DIRECTORY;
            }
            else
            {
                basicInfo.FileAttributes &= ~FileFlagsAndAttributes.FILE_ATTRIBUTE_DIRECTORY;
            }
            return metaData;
        }

        private static void ValidateFileSystemMeatadata(IFileSystemItemMetadata metadata)
        {
            if (metadata == null)
            {
                throw new ArgumentNullException("Invalid metadata.");
            }
        }

        private static System.Runtime.InteropServices.ComTypes.FILETIME FromDateTime(DateTimeOffset time)
        {
            long ticks = time.ToFileTime();
            return new System.Runtime.InteropServices.ComTypes.FILETIME
            {
                dwHighDateTime = (int)(ticks >> 32),
                dwLowDateTime = (int)(ticks & 0xFFFFFFFFu)
            };
        }

        public static bool TryCheckPlaceholder(string path, out bool? isPlaceholder)
        {
            SafeHFILE safeHandle = null;
            try
            {
                if (WinFileSystemItem.TryCreateFile(path, Kernel32.FileAccess.FILE_READ_ATTRIBUTES, FileShare.ReadWrite, FileMode.Open, out safeHandle) &&
                    TryCheckPlaceholder(safeHandle, out isPlaceholder))
                {
                    return true;
                }
            }
            finally
            {
                safeHandle?.Close();
                safeHandle?.Dispose();
            }
            isPlaceholder = null;
            return false;
        }

        public static bool TryCheckPlaceholder(SafeHFILE fHandle, out bool? isPlaceholder)
        {
            if (GetPlaceholderState(fHandle, out var state))
            {
                isPlaceholder = ((uint)state & 1u) != 0;
                return true;
            }
            isPlaceholder = null;
            return false;
        }

        public static bool TryGetPlaceholderState(string path, out CF_PLACEHOLDER_STATE? state)
        {
            state = null;
            using (WinFileSystemItem item = WinFileSystemItem.OpenFile(path, FileMode.Open, FileShare.ReadWrite))
            {
                if (GetPlaceholderState(item.SafeHandle, out state))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool GetPlaceholderState(SafeHFILE hFile, out CF_PLACEHOLDER_STATE? state)
        {
            IntPtr infoPtr = IntPtr.Zero;
            try
            {
                var info = WinNative.GetFileInformationByHandleEx<FILE_ATTRIBUTE_TAG_INFO>(hFile,
                    FILE_INFO_BY_HANDLE_CLASS.FileAttributeTagInfo);

                infoPtr = ExtensionClass.AllocHGlobal<FILE_ATTRIBUTE_TAG_INFO>(info);
                state = WinNative.GetPlaceholderState(infoPtr, FILE_INFO_BY_HANDLE_CLASS.FileAttributeTagInfo);
                if (((uint?)state & 0xFFFFFFFFu) != 0)
                {
                    state = null;
                    return false;
                }
            }
            catch
            {
                state = null;
                return false;
            }
            finally
            {
                infoPtr.FreeHGlobal();
            }

            return true;
        }

        public static bool SetInSyncState(SafeHFILE hFile, bool inSync)
        {
            CF_IN_SYNC_STATE state = inSync ? CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_IN_SYNC
                : CF_IN_SYNC_STATE.CF_IN_SYNC_STATE_NOT_IN_SYNC;
            long usn = 0;
            var result = WinNative.SetInSyncState(hFile, state, CF_SET_IN_SYNC_FLAGS.CF_SET_IN_SYNC_FLAG_NONE, ref usn);
            return result == HRESULT.S_OK;
        }

        public static async Task<bool> ConvertToPlaceholder(string path, string fileName, FileSystemItemType itemType,
            byte[] remoteStorageItemId, byte[] reservedItemId = null,
            bool markInSync = true, bool dehydrate = false,
             bool isNxlFile = false, bool isAnonymousNxlFile = false,
            CancellationToken token = default)
        {
            if (token.IsCancellationRequested)
            {
                return false;
            }
            Kernel32.FileAccess fileAccess = dehydrate ? Kernel32.FileAccess.FILE_WRITE_DATA : Kernel32.FileAccess.FILE_WRITE_ATTRIBUTES;
            FileShare fileShare = dehydrate ? FileShare.None : (FileShare.ReadWrite | FileShare.Delete);
            //string fileName = Path.GetFileName(path);
            path = path.TrimEnd(new char[1] { Path.DirectorySeparatorChar });
            if (TryGetFileId(Path.GetDirectoryName(path), out var fileId) && fileId.HasValue)
            {
                PlaceholderItemIdentity id = new PlaceholderItemIdentity(itemType, remoteStorageItemId, reservedItemId, true, fileId, fileName, isNxlFile, isAnonymousNxlFile);

                using (WinFileSystemItem item = WinFileSystemItem.OpenFile(path, fileAccess, FileMode.Open, fileShare))
                {
                    if (item.SafeHandle.IsInvalid)
                    {
                        return false;
                    }
                    return await ConvertToPlaceholder(item.SafeHandle, id.MarshalAs(), markInSync, dehydrate);
                }
            }
            return false;
        }

        private static async Task<bool> ConvertToPlaceholder(SafeHFILE hFile, byte[] fileIdData, bool markInSync = true, bool dehydrate = false)
        {
            IntPtr fileIdPtr = IntPtr.Zero;
            try
            {
                CF_CONVERT_FLAGS flags = CF_CONVERT_FLAGS.CF_CONVERT_FLAG_NONE;
                if (markInSync)
                {
                    flags |= CF_CONVERT_FLAGS.CF_CONVERT_FLAG_MARK_IN_SYNC;
                }
                if (dehydrate)
                {
                    flags |= CF_CONVERT_FLAGS.CF_CONVERT_FLAG_DEHYDRATE;
                }

                fileIdPtr = fileIdData.AllocHGlobal();
                var len = fileIdData.Length;

                WinNative.ConvertToPlaceholder(hFile, fileIdPtr, (uint)len, flags, out long usn, IntPtr.Zero).CheckResult();

                return true;
            }
            finally
            {
                fileIdPtr.FreeHGlobal();
            }
        }
    }
}
