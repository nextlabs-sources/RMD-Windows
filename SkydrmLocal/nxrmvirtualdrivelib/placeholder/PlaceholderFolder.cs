using nxrmvirtualdrivelib.core;
using nxrmvirtualdrivelib.filter;
using nxrmvirtualdrivelib.ext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Vanara.PInvoke.CldApi;
using System.IO;
using nxrmvirtualdrivelib.metadata;
using nxrmvirtualdrivelib.nxl;
using System.Collections.Concurrent;
using System.Threading;
using Vanara.PInvoke;
using nxrmvirtualdrivelib.native;

namespace nxrmvirtualdrivelib.placeholder
{
    public class PlaceholderFolder : PlaceholderItem
    {
        public PlaceholderFolder(string path) : base(path)
        {
        }

        public async Task<Dictionary<ulong, IFileSystemItemMetadata>> GetChildrenRecursivelyAsync(bool allowConflict = true)
        {
            var path = LocalPath;
            if (!Directory.Exists(path))
            {
                return null;
            }
            var placeholders = new DirectoryInfo(path).EnumerateFileSystemInfos("*", SearchOption.AllDirectories);
            if (placeholders == null || placeholders.Count() == 0)
            {
                return null;
            }
            //<remoteStorageItemId, IFileSystemItemMetadata>
            Dictionary<ulong, IFileSystemItemMetadata> rt = new Dictionary<ulong, IFileSystemItemMetadata>();
            foreach (var info in placeholders)
            {
                IFileSystemItemMetadata metadata = null;
                PlaceholderItem item = Placeholders.GetItem(info.FullName);
                var placeholderId = item.GetPlaceholderId();
                var isNxlFile = placeholderId != null ? placeholderId.IsNxlFile : false;
                var isAnonymousNxlFile = placeholderId != null ? placeholderId.IsAnonymousNxlFile : false;

                if (info is FileInfo)
                {
                    metadata = new FileMetadata
                    {
                        Length = ((FileInfo)info).Length,
                        LocalPath = info.FullName,
                        PathId = GetPathIdWithoutName(info.FullName, path) + placeholderId.FileName,
                        IsNxlFile = isNxlFile,
                        IsAnonymousNxlFile = (bool)placeholderId?.IsAnonymousNxlFile,
                        Name = (isNxlFile & !allowConflict) ? NxlFile.AppendNxlExt(info.Name, isAnonymousNxlFile) : info.Name,
                    };
                }
                else
                {
                    metadata = new FolderMetadata
                    {
                        LocalPath = info.FullName,
                        PathId = info.FullName.Substring(path.Length),
                        Name = info.Name,
                    };
                }
                metadata.RemoteStorageItemId = item.GetRemoteStorageItemId();
                metadata.Attributes = info.Attributes;
                metadata.CreationTime = info.CreationTime;
                metadata.LastWriteTime = info.LastWriteTime;
                metadata.LastAccessTime = info.LastAccessTime;
                metadata.ChangeTime = info.LastWriteTime;

                if (metadata.RemoteStorageItemId == null)
                {
                    continue;
                }
                ulong fileId = BitConverter.ToUInt64(metadata.RemoteStorageItemId, 0);

                if (rt.ContainsKey(fileId))
                {
                    fileId += (ulong)DateTime.UtcNow.Ticks;
                    rt.Add(fileId, metadata);
                }
                else
                {
                    rt.Add(fileId, metadata);
                }
            }
            return rt;
        }

        private static string GetPathIdWithoutName(string fullPath, string rootPath)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                return "";
            }
            if (string.IsNullOrEmpty(rootPath))
            {
                return "";
            }
            var dir = Path.GetDirectoryName(fullPath);
            if (!dir.EndsWith(@"\"))
            {
                dir += @"\";
            }
            return dir.Substring(rootPath.Length);
        }

        public static async Task<(CF_PLACEHOLDER_CREATE_INFO[], GCHandle[], byte[][])> Convert(bool checkFile, ulong? fileId,
            IFileSystemItemMetadata[] children, string userFsPath, IFileFilter filter, bool allowConflict = true)
        {
            if (children == null || children.Length == 0)
            {
                return (null, null, null);
            }
            List<CF_PLACEHOLDER_CREATE_INFO> phCreateInfoList = new List<CF_PLACEHOLDER_CREATE_INFO>();
            List<GCHandle> pinnedFileIndentityLists = new List<GCHandle>();
            List<byte[]> fileIdentityDatalList = new List<byte[]>();

            ConcurrentDictionary<string, IFileSystemItemMetadata> parents = new ConcurrentDictionary<string, IFileSystemItemMetadata>();
            foreach (var item in children)
            {
                string fullPath = Path.Combine(userFsPath, item.Name);
                FileSystemItemType itemType = (((uint)item.Attributes & 0x10) != 0) ? FileSystemItemType.Folder : FileSystemItemType.File;
                if (await filter.FilterAsync(fullPath, SourceFrom.REMOTE, SourceAction.CREATE, itemType))
                {
                    continue;
                }
                if (item is FileMetadata metadata && !allowConflict)
                {
                    if (IsConflict(parents, item))
                    {
                        metadata.IsConfict = true;
                    }
                }

                (CF_PLACEHOLDER_CREATE_INFO phCreatInfo, GCHandle pinnedFileIdentity, byte[] fileIdentityData) = Convert(checkFile, fileId, item, allowConflict);
                phCreateInfoList.Add(phCreatInfo);
                pinnedFileIndentityLists.Add(pinnedFileIdentity);
                fileIdentityDatalList.Add(fileIdentityData);
            }
            return (phCreateInfoList.ToArray(), pinnedFileIndentityLists.ToArray(), fileIdentityDatalList.ToArray());
        }

        /// <summary>
        /// In RPM mode, the normal version and the nxl version of a file
        /// must not be existing at the same time under the same dir. 
        /// e.g.[a.docx.nxl & a.docx]
        /// 1. RPM will generate a normal file(a.docx[nxl file without .nxl extension]) for a nxl file(a.doc.nxl).
        /// 2. If try to convert the normal file(a.docx) to a placeholder, an "placehodler with the same name error will be throwned by cloud filter api."
        /// 3. In this case, we will support this condition by mark one of them as conflicted, and will using the conflict name to generate a placeholder file.
        /// </summary>
        /// <param name="parents"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        private static bool IsConflict(ConcurrentDictionary<string, IFileSystemItemMetadata> parents, IFileSystemItemMetadata metadata)
        {
            var name = metadata.Name;
            var tmp = name;
            if (name.EndsWith(NxlFile.NXL_EXT))
            {
                tmp = name.Remove(name.IndexOf(NxlFile.NXL_EXT), NxlFile.NXL_EXT.Length);
            }
            else
            {
                tmp = NxlFile.AppendNxlExt(name);
            }
            if (parents.ContainsKey(name) || parents.ContainsKey(tmp))
            {
                return true;
            }
            else
            {
                parents.TryAdd(name, metadata);
                parents.TryAdd(tmp, metadata);
            }
            return false;
        }

        public static void FreeGCHandle(GCHandle[] gcHandle)
        {
            if (gcHandle == null || gcHandle.Length == 0)
            {
                return;
            }
            foreach (var handle in gcHandle)
            {
                if (handle.IsAllocated)
                {
                    try
                    {
                        handle.Free();
                    }
                    catch (Exception)
                    {

                    }
                }
            }
        }

        public async Task<uint> CreateAsync(IFileSystemItemMetadata[] children, IFileFilter fileFilter)
        {
            CF_PLACEHOLDER_CREATE_INFO[] placeholderArray = null;
            GCHandle[] pinnedFileIds = null;
            byte[][] fileIdDatas = null;

            try
            {
                (placeholderArray, pinnedFileIds, fileIdDatas) = await Convert(true, GetFileId(), children.ToArray(),
                    LocalPath, fileFilter);

                if (placeholderArray == null || placeholderArray.Length == 0)
                {
                    return 0u;
                }

                CreatePlaceholders(placeholderArray, (uint)placeholderArray.Length,
                    CF_CREATE_FLAGS.CF_CREATE_FLAG_STOP_ON_ERROR, out uint entriesProcessed);

                return entriesProcessed;
            }
            finally
            {
                pinnedFileIds.Free();
            }
        }

        public override async Task<bool> DeleteAsync(CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return false;
            }
            if (Directory.Exists(LocalPath))
            {
                Directory.Delete(LocalPath);
                return true;
            }
            return false;
        }

        public async override Task<(bool IsSuccess, bool? IsNew)> IsNew()
        {
            if (TryCheckPlaceholder(LocalPath, out bool? flag) && flag.HasValue)
            {
                if (flag.Value)
                {
                    return (true, false);
                }
                return (true, true);
            }
            return (false, null);
        }

        public override async Task<(bool IsSuccess, bool? IsMoved)> IsMoved()
        {
            return (true, false);
        }

        public override async Task<bool> UpdateAsync(IFileSystemItemMetadata metadata, bool autoHydration = true)
        {
            if (metadata == null)
            {
                return false;
            }
            using (WinFileSystemItem item = WinFileSystemItem.OpenFile(LocalPath, Kernel32.FileAccess.FILE_WRITE_DATA,
                FileMode.Open, FileShare.Read | FileShare.Delete))
            {
                IntPtr fileIdPtr = IntPtr.Zero;
                try
                {
                    FileFlagsAndAttributes attributes = (FileFlagsAndAttributes)File.GetAttributes(LocalPath);
                    FileFlagsAndAttributes pinAttr = attributes & FileFlagsAndAttributes.FILE_ATTRIBUTE_PINNED;
                    FileFlagsAndAttributes unPinAttr = attributes & FileFlagsAndAttributes.FILE_ATTRIBUTE_UNPINNED;

                    metadata.Name = Path.GetFileName(LocalPath);

                    CF_FS_METADATA fsMd = ConvertMetadata(metadata);
                    fsMd.BasicInfo.FileAttributes |= pinAttr | unPinAttr;

                    byte[] fileIdentityData = GetItemId(metadata, true, FileId);
                    fileIdPtr = fileIdentityData.AllocHGlobal();
                    uint fileIdLen = (uint)fileIdentityData.Length;
                    long usn = 0L;

                    WinNative.UpdatePlaceholder(item.SafeHandle, fsMd,
                        fileIdPtr, fileIdLen,
                        null, 0,
                        CF_UPDATE_FLAGS.CF_UPDATE_FLAG_VERIFY_IN_SYNC, ref usn, IntPtr.Zero).CheckResult();
                }
                finally
                {
                    fileIdPtr.FreeHGlobal();
                }
            }

            return true;
        }

        private void CreatePlaceholders(CF_PLACEHOLDER_CREATE_INFO[] placeholderArray, uint placeholderCount,
            CF_CREATE_FLAGS createFlags, out uint entriesProcessed)
        {
            WinNative.CreatePlaceholders(LocalPath, placeholderArray, placeholderCount, createFlags, out entriesProcessed)
                .CheckResult();
        }

    }
}
