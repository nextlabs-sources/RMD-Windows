using nxrmvirtualdrivelib.core;
using nxrmvirtualdrivelib.native;
using nxrmvirtualdrivelib.ext;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;

namespace nxrmvirtualdrivelib.placeholder
{
    public class PlaceholderFile : PlaceholderItem
    {
        public PlaceholderFile(string path) : base(path)
        {
        }

        public bool TryHydrationRequired(out bool? hydrationRequired)
        {
            if (WinFileSystemItem.TryGetAttributes(LocalPath, out var attributes))
            {
                uint num = (uint)attributes.Value;
                if (((num & (uint)FileFlagsAndAttributes.FILE_ATTRIBUTE_DIRECTORY) == 0) && ((num & (uint)FileFlagsAndAttributes.FILE_ATTRIBUTE_PINNED) != 0) && ((num & (uint)FileFlagsAndAttributes.FILE_ATTRIBUTE_OFFLINE) != 0))
                {
                    hydrationRequired = true;
                    return true;
                }
                hydrationRequired = false;
                return true;
            }
            hydrationRequired = null;
            return false;
        }

        public bool TryDehydrationRequired(out bool? dehydrationRequired)
        {
            if (WinFileSystemItem.TryGetAttributes(LocalPath, out var attributes))
            {
                int num = (int)attributes.Value;
                if (((uint)num & 0x10u) != 0 || (num & 0x100000) == 0 || ((uint)num & 0x1000u) != 0)
                {
                    dehydrationRequired = false;
                    return true;
                }
                if (TryCheckPlaceholder(LocalPath, out bool? flag))
                {
                    dehydrationRequired = flag.Value;
                    return true;
                }
            }
            dehydrationRequired = null;
            return false;
        }

        /// <summary>
        /// The process of downloading the file content from the remote storage to the user file system is called hydration.
        /// When files are initially created in the user file system they do not have any content on disk.
        /// Event though it got correct file size, they are dehydrated.
        /// 
        /// Such files are marked with an offline attribute and have a cloud icon in the Windows File Manager.
        /// </summary>
        public bool Hydrate()
        {
            FileInfo info = new FileInfo(LocalPath);
            if (!info.Exists)
            {
                return false;
            }
            return Hydrate(LocalPath, 0, info.Length);
        }

        public bool Hydrate(long startingOffset = 0, long length = -1)
        {
            FileInfo info = new FileInfo(LocalPath);
            if (!info.Exists)
            {
                return false;
            }
            return Hydrate(LocalPath, startingOffset, length);
        }

        public bool DeHydrate(long startingOffset, long length)
        {
            FileInfo info = new FileInfo(LocalPath);
            if (!info.Exists)
            {
                return false;
            }
            using (WinFileSystemItem item = WinFileSystemItem.OpenFile(LocalPath, Kernel32.FileAccess.FILE_WRITE_DATA, FileMode.Open, FileShare.Delete))
            {
                return UpdatePlaceholder(item.SafeHandle, startingOffset, length);
            }
        }

        public static bool Hydrate(string path, long startingOffset = 0, long length = -1)
        {
            FileInfo info = new FileInfo(path);
            if (!info.Exists)
            {
                return false;
            }
            using (WinFileSystemItem item = WinFileSystemItem.OpenFile(path, Kernel32.FileAccess.FILE_READ_DATA,
                FileMode.Open, FileShare.ReadWrite | FileShare.Delete))
            {
                WinNative.HydratePlaceholder(item.SafeHandle, startingOffset, length,
                    CF_HYDRATE_FLAGS.CF_HYDRATE_FLAG_NONE, IntPtr.Zero).CheckResult();
                return true;
            }
        }

        public override async Task<bool> DeleteAsync(CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return false;
            }
            if (File.Exists(LocalPath))
            {
                File.Delete(LocalPath);
                return true;
            }
            return false;
        }

        public override async Task<bool> UpdateAsync(IFileSystemItemMetadata metadata, bool autoHydration = true)
        {
            if (metadata == null)
            {
                return false;
            }
            using (WinFileSystemItem item = WinFileSystemItem.OpenFile(LocalPath,
                Kernel32.FileAccess.FILE_WRITE_DATA, FileMode.Open, FileShare.Delete))
            {
                IntPtr fileIdPtr = IntPtr.Zero;
                try
                {
                    FileFlagsAndAttributes attributes = (FileFlagsAndAttributes)File.GetAttributes(LocalPath);
                    FileFlagsAndAttributes pinAttr = attributes & FileFlagsAndAttributes.FILE_ATTRIBUTE_PINNED;
                    FileFlagsAndAttributes unPinAttr = attributes & FileFlagsAndAttributes.FILE_ATTRIBUTE_UNPINNED;

                    metadata.Name = Path.GetFileName(LocalPath);

                    CF_FS_METADATA fsMd = ConvertMetadata(metadata);


                    byte[] fileIdentityData = GetItemId(metadata, true, FileId);
                    fileIdPtr = fileIdentityData.AllocHGlobal();
                    uint fileIdLen = (uint)fileIdentityData.Length;
                    long usn = 0L;

                    if ((attributes & FileFlagsAndAttributes.FILE_ATTRIBUTE_PINNED) != 0)
                    {
                        WinNative.UpdatePlaceholder(item.SafeHandle, fsMd, fileIdPtr, fileIdLen, null,
                            0, CF_UPDATE_FLAGS.CF_UPDATE_FLAG_VERIFY_IN_SYNC, ref usn, IntPtr.Zero).CheckResult();
                    }

                    CF_UPDATE_FLAGS updateFlags = CF_UPDATE_FLAGS.CF_UPDATE_FLAG_VERIFY_IN_SYNC;
                    if (autoHydration)
                    {
                        updateFlags |= CF_UPDATE_FLAGS.CF_UPDATE_FLAG_DEHYDRATE;
                    }
                    fsMd.BasicInfo.FileAttributes |= pinAttr | unPinAttr;
                    WinNative.UpdatePlaceholder(item.SafeHandle, fsMd, fileIdPtr, fileIdLen, null,
                        0, updateFlags, ref usn, IntPtr.Zero).CheckResult();

                    if (autoHydration && (pinAttr != 0 || ((attributes & FileFlagsAndAttributes.FILE_ATTRIBUTE_OFFLINE) == 0
                        && (attributes & FileFlagsAndAttributes.FILE_ATTRIBUTE_UNPINNED) == 0)))
                    {
                        WinNative.HydratePlaceholder(item.SafeHandle, 0, fsMd.FileSize,
                            CF_HYDRATE_FLAGS.CF_HYDRATE_FLAG_NONE, IntPtr.Zero).CheckResult();
                    }
                }
                finally
                {
                    fileIdPtr.FreeHGlobal();
                }
            }
            return true;
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
    }
}
