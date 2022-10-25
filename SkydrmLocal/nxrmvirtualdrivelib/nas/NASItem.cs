using nxrmvirtualdrivelib.core;
using nxrmvirtualdrivelib.metadata;
using nxrmvirtualdrivelib.nxl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.nas
{
    public abstract class NASItem
    {
        protected ILogger m_logger;
        protected string m_pathId;

        public NASItem(string pathId, ILogger logger)
        {
            this.m_pathId = pathId;
            this.m_logger = logger;
        }

        public static async Task<IFileSystemItemMetadata> GetMetadataAsync(FileSystemInfo info)
        {
            if (info == null)
            {
                return null;
            }
            IFileSystemItemMetadata metadata;
            if (info is FileInfo)
            {
                var isNxlFile = NxlFile.IsNxlFile(info.FullName);
                var ext = Path.GetExtension(info.FullName);
                var isAnonymousNxlFile = isNxlFile && !ext.Equals(NxlFile.NXL_EXT);

                metadata = new FileMetadata
                {
                    Length = ((FileInfo)info).Length,
                    IsNxlFile = isNxlFile,
                    IsAnonymousNxlFile = isAnonymousNxlFile
                };
            }
            else
            {
                metadata = new FolderMetadata();
            }

            metadata.RemoteStorageItemId = GetItemId(info.FullName);
            metadata.Name = info.Name;
            metadata.Attributes = info.Attributes;
            metadata.CreationTime = info.CreationTime;
            metadata.LastWriteTime = info.LastWriteTime;
            metadata.LastAccessTime = info.LastAccessTime;
            metadata.ChangeTime = info.LastWriteTime;

            return metadata;
        }

        protected static byte[] GetItemId(string fullPath)
        {
            return WinFileSystemItem.GetItemIdByPath(fullPath);
        }

        protected static string GetPath(byte[] itemId)
        {
            return WinFileSystemItem.GetPathByItemId(itemId);
        }
    }
}
