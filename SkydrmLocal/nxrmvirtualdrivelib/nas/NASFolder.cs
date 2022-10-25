using nxrmvirtualdrivelib.core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.nas
{
    public class NASFolder : NASItem, IVirtualFolder
    {
        public NASFolder(byte[] itemId, ILogger logger) : base(GetPath(itemId), logger)
        {

        }

        public async Task<Dictionary<ulong, IFileSystemItemMetadata>> GetChildrenRecursivelyAsync()
        {
            m_logger?.Info(string.Format("[NASFolder]: GetChildrenRecursivelyAsync for path {0}.", m_pathId));
            var children = await GetChildrenRecursivelyAsync("*");
            if (children == null || children.Length == 0)
            {
                return null;
            }
            Dictionary<ulong, IFileSystemItemMetadata> rt = new Dictionary<ulong, IFileSystemItemMetadata>();
            foreach (var item in children)
            {
                var itemId = item.RemoteStorageItemId;
                if (itemId == null)
                {
                    continue;
                }
                ulong fileId = BitConverter.ToUInt64(itemId, 0);
                rt.Add(fileId, item);
            }
            return rt;
        }

        public async Task<IFileSystemItemMetadata[]> GetChildrenRecursivelyAsync(string pattern, CancellationToken token = default)
        {
            m_logger?.Info(string.Format("[NASFolder]: GetChildrenRecursivelyAsync for path {0}.", m_pathId));
            if (token.IsCancellationRequested)
            {
                return null;
            }
            if (!Directory.Exists(m_pathId))
            {
                return null;
            }

            var remoteChildren = new DirectoryInfo(m_pathId).EnumerateFileSystemInfos(pattern, SearchOption.AllDirectories);
            if (remoteChildren == null || remoteChildren.Count() == 0)
            {
                return null;
            }
            List<IFileSystemItemMetadata> rt = new List<IFileSystemItemMetadata>();
            foreach (var item in remoteChildren)
            {
                IFileSystemItemMetadata metadata = await GetMetadataAsync(item);
                metadata.PathId = item.FullName.Substring(m_pathId.Length);
                rt.Add(metadata);
            }

            return rt.ToArray();
        }

        public async Task<IFileSystemItemMetadata[]> GetChildrenAsync(string pattern, CancellationToken token = default)
        {
            m_logger?.Info(string.Format("[NASFolder]: GetChildrenAsync for path {0}.", m_pathId));
            if (token.IsCancellationRequested)
            {
                return null;
            }
            if (!Directory.Exists(m_pathId))
            {
                return null;
            }
            var remoteChildren = new DirectoryInfo(m_pathId).EnumerateFileSystemInfos(pattern);
            if (remoteChildren == null || remoteChildren.Count() == 0)
            {
                return null;
            }
            List<IFileSystemItemMetadata> rt = new List<IFileSystemItemMetadata>();
            foreach (var item in remoteChildren)
            {
                rt.Add(await GetMetadataAsync(item));
            }

            return rt.ToArray();
        }

        public async Task<IFileSystemItemMetadata> GetMetadataAsync()
        {
            m_logger?.Info(string.Format("[NASFolder]: GetMetadataAsync for path {0}.", m_pathId));
            DirectoryInfo info = new DirectoryInfo(m_pathId);
            return await GetMetadataAsync(info);
        }

        public async Task<byte[]> CreateFolderAsync(IFolderMetadata metadata, CancellationToken token = default)
        {
            string fullPath = Path.Combine(m_pathId, metadata.Name);
            m_logger?.Info(string.Format("[NASFolder]: CreateFolderAsync for path {0}.", fullPath));

            DirectoryInfo remoteInfo = new DirectoryInfo(fullPath);
            remoteInfo.Create();

            remoteInfo.Attributes = metadata.Attributes;
            remoteInfo.CreationTimeUtc = metadata.CreationTime.UtcDateTime;
            remoteInfo.LastWriteTimeUtc = metadata.LastWriteTime.UtcDateTime;
            remoteInfo.LastAccessTimeUtc = metadata.LastAccessTime.UtcDateTime;

            return GetItemId(fullPath);
        }

        public async Task<byte[]> CreateFileAsync(IFileMetadata metadata, Stream content, CancellationToken token = default)
        {
            if (metadata == null || content == null)
            {
                return null;
            }

            // Full path of file to be created.
            string fullPath = Path.Combine(m_pathId, metadata.Name);
            m_logger?.Info(string.Format("[NASFolder]: CreateFileAsync for file {0}.", fullPath));

            FileInfo remoteInfo = new FileInfo(fullPath);

            using (var dest = remoteInfo.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete))
            {
                await content.CopyToAsync(dest);
                dest.SetLength(content.Length);
            }

            // Update metadata of new created file.
            remoteInfo.Attributes = metadata.Attributes;
            remoteInfo.CreationTimeUtc = metadata.CreationTime.UtcDateTime;
            remoteInfo.LastWriteTimeUtc = metadata.LastWriteTime.UtcDateTime;
            remoteInfo.LastAccessTimeUtc = metadata.LastAccessTime.UtcDateTime;

            return GetItemId(fullPath);
        }

        public async Task DeleteAsync(IDeleteFile confirmation, CancellationToken token = default)
        {
            m_logger?.Info(string.Format("[NASFolder]: DeleteAsync for file {0}, confirmed by default.", m_pathId));
            if (token.IsCancellationRequested)
            {
                return;
            }
            //confirmation?.OnConfirm();
        }

        public async Task<bool> DeleteCompletionAsync(CancellationToken token = default)
        {
            m_logger?.Info(string.Format("[NASFolder]: DeleteCompletionAsync for file {0}.", m_pathId));
            if (token.IsCancellationRequested)
            {
                return false;
            }
            if (string.IsNullOrEmpty(m_pathId))
            {
                return false;
            }
            try
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(m_pathId);
                directoryInfo.Delete(true);

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("[NASFolder]: DeleteCompletionAsync for folder {0} failed with error {1}.", m_pathId, e));
                return false;
            }
        }

        public async Task<bool> MoveToAsync(string destPath, byte[] remoteStorageFolderId, CancellationToken token = default)
        {
            m_logger?.Info(string.Format("[NASFolder]: MoveToAsync for file {0}, confirmed by default.", m_pathId));
            if (token.IsCancellationRequested)
            {
                return false;
            }
            return true;
        }

        public async Task<bool> MoveToCompletionAsync(string destPath, byte[] remoteStorageFolderId, CancellationToken token = default)
        {
            m_logger?.Info(string.Format("[NASFolder]: MoveToCompletionAsync for file {0}.", m_pathId));
            if (token.IsCancellationRequested)
            {
                return false;
            }
            if (remoteStorageFolderId == null)
            {
                return false;
            }
            var destFolder = WinFileSystemItem.GetPathByItemId(remoteStorageFolderId);
            if (destFolder == null || destFolder.Length == 0)
            {
                return false;
            }
            if (!Directory.Exists(destFolder))
            {
                return false;
            }
            var name = new DirectoryInfo(destPath).Name;
            var destFullPath = Path.Combine(destFolder, name);

            Directory.Move(m_pathId, destFullPath);
            return true;
        }

        public async Task WriteAsync(IFolderMetadata metadata, CancellationToken token = default)
        {
            m_logger?.Info(string.Format("[NASFolder]: WriteAsync for file {0}.", m_pathId));
            DirectoryInfo remoteInfo = new DirectoryInfo(m_pathId)
            {
                Attributes = metadata.Attributes,
                CreationTimeUtc = metadata.CreationTime.UtcDateTime,
                LastWriteTimeUtc = metadata.LastWriteTime.UtcDateTime,
                LastAccessTimeUtc = metadata.LastAccessTime.UtcDateTime
            };
        }
    }
}
