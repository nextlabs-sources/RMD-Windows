using nxrmvirtualdrivelib.core;
using nxrmvirtualdrivelib.nxl;
using nxrmvirtualdrivelib.ext;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.nas
{
    public class NASFile : NASItem, IVirtualFile
    {
        private static readonly int CHUNKSIZE = 4096;

        private string m_userFsPath;
        private bool m_isNxlFile;
        private bool m_isAnonymousNxlFile;

        public NASFile(byte[] itemId, string userFsPath, ILogger logger) : base(GetPath(itemId), logger)
        {
            this.m_userFsPath = userFsPath;
            this.m_isNxlFile = NxlFile.IsNxlFile(m_pathId);
            this.m_isAnonymousNxlFile = NxlFile.IsAnonymousNxlFile(m_pathId);
        }

        public async Task ReadAsync(Stream output, long offset, long length, CancellationToken token = default)
        {
            m_logger?.Info(string.Format("[NASFile]: ReadAsync for file {0} from offset {1} with length {2}.", m_pathId, offset, length));
            if (token.IsCancellationRequested)
            {
                return;
            }

            long fileSize = length;
            long requiredOffset = offset;
            int chunkSize = (int)Math.Min(fileSize, CHUNKSIZE);

            using (var source = File.OpenRead(m_pathId))
            {
                source.Seek(offset, SeekOrigin.Begin);
                await source.CopyToAsync(output, chunkSize, length);
            }
        }

        public async Task WriteAsync(IFileSystemItemMetadata metadata, Stream content, CancellationToken token = default)
        {
            m_logger?.Info(string.Format("[NASFile]: WriteAsync for file {0}.", m_pathId));
            if (token.IsCancellationRequested)
            {
                return;
            }
            if (content == null)
            {
                return;
            }
            FileInfo remoteInfo = new FileInfo(m_pathId);
            using (var dest = remoteInfo.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.Delete))
            {
                await content.CopyToAsync(dest);
                dest.SetLength(content.Length);
            }
            remoteInfo.Attributes = metadata.Attributes;
            remoteInfo.CreationTimeUtc = metadata.CreationTime.UtcDateTime;
            remoteInfo.LastWriteTimeUtc = metadata.LastWriteTime.UtcDateTime;
            remoteInfo.LastAccessTimeUtc = metadata.LastAccessTime.UtcDateTime;
        }

        public async Task<IFileSystemItemMetadata> GetMetadataAsync()
        {
            m_logger?.Info(string.Format("[NASFile]: GetMetadataAsync for file {0}.", m_pathId));
            FileInfo remoteInfo = new FileInfo(m_pathId);

            return await GetMetadataAsync(remoteInfo);
        }

        public async Task DeleteAsync(IDeleteFile confirmation, CancellationToken token)
        {
            m_logger.Info(string.Format("[NASFile]: DeleteAsync for file {0} confirmed by default.", m_pathId));
            if (token.IsCancellationRequested)
            {
                return;
            }

            //confirmation?.OnConfirm();
        }

        public async Task<bool> DeleteCompletionAsync(CancellationToken token)
        {
            m_logger?.Info(string.Format("[NASFile]: DeleteCompletionAsync for file {0}.", m_pathId));
            if (token.IsCancellationRequested)
            {
                return false;
            }

            try
            {
                if (string.IsNullOrEmpty(m_pathId))
                {
                    return false;
                }
                if (File.Exists(m_pathId))
                {
                    FileInfo remoteInfo = new FileInfo(m_pathId);
                    remoteInfo.Delete();
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("DeleteCompletionAsync for file {0} failed with error {1}.", m_pathId, e));
                return false;
            }
        }

        public async Task<bool> MoveToAsync(string destPath, byte[] remoteStorageFolderId, CancellationToken token = default)
        {
            m_logger?.Info(string.Format("[NASFile]: MoveToAsync for file {0} with dest path {1} confirmed by default.", m_pathId, destPath));
            if (token.IsCancellationRequested)
            {
                return false;
            }
            return true;
        }

        public async Task<bool> MoveToCompletionAsync(string destPath, byte[] remoteStorageFolderId, CancellationToken token = default)
        {
            m_logger?.Info(string.Format("[NASFile]: MoveToCompletionAsync for file {0} with dest path {1}.", m_pathId, destPath));
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
            var name = new FileInfo(destPath).Name;
            var destFullPath = Path.Combine(destFolder, name);

            File.Move(m_pathId, destFullPath);
            return true;
        }
    }
}
