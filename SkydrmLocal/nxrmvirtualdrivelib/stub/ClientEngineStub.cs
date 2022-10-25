using nxrmvirtualdrivelib.core;
using nxrmvirtualdrivelib.filter;
using nxrmvirtualdrivelib.metadata;
using nxrmvirtualdrivelib.nxl;
using nxrmvirtualdrivelib.placeholder;
using nxrmvirtualdrivelib.utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.stub
{
    public class ClientEngineStub : IVirtualEngineStub
    {
        private readonly string m_userFSPath;
        private readonly IFileFilter m_fileFilter;

        private CancellationToken m_cancelToken;
        private ILogger m_logger;

        public ClientEngineStub(string userFSPath, IFileFilter fileFilter, ILogger logger, CancellationToken token = default)
        {
            this.m_userFSPath = userFSPath;
            this.m_fileFilter = fileFilter;
            this.m_cancelToken = token;
            this.m_logger = logger;
        }

        public async Task<uint> CreateAsync(IFileSystemItemMetadata[] items, bool allowConflict = true)
        {
            m_logger.Info(string.Format("[ClientEngineStub]: CreateAsync started with items {0}.", items == null ? 0 : items.Length));
            Stopwatch stopwatch = Stopwatch.StartNew();

            if (m_cancelToken.IsCancellationRequested)
            {
                m_logger.Info("[ClientEngineStub]: CreateAsync canceled.");
                return 0u;
            }
            if (items == null || items.Length == 0)
            {
                return 0u;
            }
            string parent = m_userFSPath;
            bool exists = Directory.Exists(parent);
            bool offine = new DirectoryInfo(parent).Attributes.HasFlag(FileAttributes.Offline);
            if (!exists || offine)
            {
                if (!exists)
                {
                    m_logger.Error(string.Format("[ClientEngineStub]: CreateAsync for file under parent {0} failed, file does not exists.", parent));
                }
                if (offine)
                {
                    m_logger.Info(string.Format("[ClientEngineStub]: CreateAsync for file under parent {0} failed, file is in offline status, try with on-demand loading.", parent));
                }
                return 0u;
            }

            uint processed = 0;
            try
            {
                List<IFileSystemItemMetadata> children = new List<IFileSystemItemMetadata>();
                foreach (var f in items)
                {
                    string fullPath = Path.Combine(parent, f.Name);
                    FileSystemItemType itemType = (f is FileMetadata) ? FileSystemItemType.File : FileSystemItemType.Folder;

                    if (await m_fileFilter?.FilterAsync(fullPath, SourceFrom.REMOTE, SourceAction.CREATE, itemType))
                    {
                        continue;
                    }

                    //Should rename file if conflicted.
                    //nxl file may conflict in RPM-mode.
                    if (f is FileMetadata metadata && !allowConflict)
                    {
                        if (IsConflict(parent, f.Name))
                        {
                            metadata.IsConfict = true;
                        }
                    }

                    children.Add(f);
                }

                PlaceholderFolder phFolder = new PlaceholderFolder(parent);

                processed = await phFolder.CreateAsync(children.ToArray(), m_fileFilter);
                return processed;
            }
            finally
            {
                stopwatch.Stop();
                if (processed > 0)
                {
                    m_logger.Info(string.Format("[ClientEngineStub]: CreateAsync finished with items {0} processed, consumes time {1}.", processed, stopwatch.Elapsed.ToString()));
                }
            }
        }

        /// <summary>
        /// In RPM mode, the normal version and the nxl version of a file
        /// must not be existing at the same time under the same dir. 
        /// e.g.[a.docx.nxl & a.docx]
        /// 1. RPM will generate a normal file(a.docx[nxl file without .nxl extension]) for a nxl file(a.doc.nxl).
        /// 2. If try to convert the normal file(a.docx) to a placeholder, an "placehodler with the same name error will be throwned by cloud filter api."
        /// 3. In this case, we will support this condition by mark one of them as conflicted, and will using the conflict name to generate a placeholder file.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private bool IsConflict(string parent, string fileName)
        {
            if (string.IsNullOrEmpty(parent))
            {
                return false;
            }
            if (!Directory.Exists(parent))
            {
                return false;
            }
            var children = Directory.EnumerateFiles(parent);
            if (children == null || children.Count() == 0)
            {
                return false;
            }
            foreach (var f in children)
            {
                FileInfo info = new FileInfo(f);
                if (!info.Exists)
                {
                    continue;
                }
                var name = Path.GetFileName(f);
                var tmp = name;
                if (name.EndsWith(NxlFile.NXL_EXT))
                {
                    tmp = name.Remove(name.IndexOf(NxlFile.NXL_EXT), NxlFile.NXL_EXT.Length);
                }
                else
                {
                    tmp = NxlFile.AppendNxlExt(name);
                }
                if (tmp.Equals(fileName, StringComparison.InvariantCultureIgnoreCase)
                    || name.Equals(fileName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public async Task<bool> DeleteAsync()
        {
            m_logger.Info(string.Format("[ClientEngineStub]: DeleteAsync for file {0}.", m_userFSPath));
            if (m_cancelToken.IsCancellationRequested)
            {
                return false;
            }
            if (FileHelper.TryGetItemType(m_userFSPath, out var itemType) && itemType.HasValue)
            {
                if (await m_fileFilter?.FilterAsync(m_userFSPath, SourceFrom.REMOTE, SourceAction.DELETE, itemType.Value))
                {
                    return false;
                }

                var item = Placeholders.GetItem(m_userFSPath);
                return await item.DeleteAsync(m_cancelToken);
            }
            return false;
        }

        public async Task<bool> UpdateAsync(IFileSystemItemMetadata metadata, bool autoHydration = true)
        {
            m_logger.Info(string.Format("[ClientEngineStub]: UpdateAsync for file {0}.", m_userFSPath));
            if (m_cancelToken.IsCancellationRequested)
            {
                return false;
            }
            if (FileHelper.TryGetItemType(m_userFSPath, out var itemType) && itemType.HasValue)
            {
                if (await m_fileFilter?.FilterAsync(m_userFSPath, SourceFrom.REMOTE, SourceAction.UPDATE, itemType.Value))
                {
                    return false;
                }

                var item = Placeholders.GetItem(m_userFSPath);
                return await item.UpdateAsync(metadata, autoHydration);
            }
            return false;
        }

        public async Task<bool> MoveToAsync(string moveToPath)
        {
            m_logger.Info(string.Format("[ClientEngineStub]: MoveToAsync for file {0} with new path {1}.", m_userFSPath, moveToPath));
            if (m_cancelToken.IsCancellationRequested)
            {
                return false;
            }
            string sourcePath = m_userFSPath.TrimEnd(new char[1] { Path.DirectorySeparatorChar });
            string destPath = moveToPath.TrimEnd(new char[1] { Path.DirectorySeparatorChar });

            if (WinFileSystemItem.TryGetAttributes(sourcePath, out var attributes) && attributes.HasValue)
            {
                string destParent = Path.GetDirectoryName(destPath);
                if (!Directory.Exists(destParent) || new DirectoryInfo(destParent).Attributes.HasFlag(FileAttributes.Offline))
                {
                    return false;
                }

                FileSystemItemType itemType = FileHelper.IsDirectory(attributes.Value) ? FileSystemItemType.Folder : FileSystemItemType.File;
                if (await m_fileFilter?.FilterAsync(sourcePath, SourceFrom.REMOTE, SourceAction.MOVE, itemType))
                {
                    return false;
                }

                Directory.Move(sourcePath, destPath);
                return true;
            }
            return false;
        }

    }
}
