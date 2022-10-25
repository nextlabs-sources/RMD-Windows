using nxrmvirtualdrivelib.core;
using nxrmvirtualdrivelib.threadpool;
using nxrmvirtualdrivelib.utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.nas
{
    public class NASEngine : VirtualEngine
    {
        public NASEngine(string syncRootPath) : base(syncRootPath)
        {

        }

        public override async Task<IFileSystemItem> GetFileSystemItemAsync(string userFsPath,
            FileSystemItemType itemType, byte[] itemId)
        {
            if (itemType == FileSystemItemType.File)
            {
                return new NASFile(itemId, userFsPath, m_logger);
            }
            return new NASFolder(itemId, m_logger);
        }

        protected override async Task OnHeartbeat()
        {
            var remotes = await GetRemoteChildrenRecursivelyAsync();
            var locals = await GetLocalChildrenRecursivelyAsync();

            var results = await FileHelper.FilterRemoteAsync(remotes, locals);

            var creates = results.creates;
            var deletes = results.deletes;
            var updates = results.updates;
            var moves = results.moves;

            try
            {
                //Process deletes.
                await ProcessDeletesAsync(deletes);
            }
            catch (Exception e)
            {
                m_logger.Error(e);
            }

            try
            {
                //Process creates.
                await ProcessCreatesAsync(creates);
            }
            catch (Exception e)
            {
                m_logger.Error(e);
            }

            try
            {
                //Process moves.
                await ProcessMovesAsync(moves);
            }
            catch (Exception e)
            {
                m_logger.Error(e);
            }

            try
            {
                await ProcessUpdateAsync(updates);
            }
            catch (Exception e)
            {
                m_logger.Error(e);
            }
        }

        private async Task ProcessCreatesAsync(IFileSystemItemMetadata[] data)
        {
            if (data == null || data.Length == 0)
            {
                return;
            }
            Logger.Info(string.Format("[NASEngine]:ProcessCreatesAsync with size {0}", data.Length));

            Dictionary<string, List<IFileSystemItemMetadata>> children = new Dictionary<string, List<IFileSystemItemMetadata>>();
            foreach (var f in data)
            {
                var fullPath = SyncRootPath + f.PathId;
                var parent = Path.GetDirectoryName(fullPath);

                if (children.Keys.Contains(parent))
                {
                    var values = children[parent];
                    if (values == null)
                    {
                        List<IFileSystemItemMetadata> items = new List<IFileSystemItemMetadata>
                        {
                            f
                        };
                        children.Add(parent, items);
                    }
                    else
                    {
                        values.Add(f);
                    }
                }
                else
                {
                    List<IFileSystemItemMetadata> items = new List<IFileSystemItemMetadata>
                    {
                        f
                    };
                    children.Add(parent, items);
                }
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            ulong num = 0;
            await ThreadPoolExecutor.Execute(children.Keys, async delegate (string path)
            {
                num += await GetClientStub(path).CreateAsync(children[path].ToArray(), !IsRPMDir(path));
            }, delegate (string path, Exception e)
            {
                Logger.Error(string.Format("Failed to CreateAsync for file {0} with error {1}", path, e));
            });
            stopwatch.Stop();
            if (num > 0)
            {
                Logger.Info(string.Format("[NASEngine]: ProcessCreatesAsync finished with size {0}, consumes {1}", num, stopwatch.Elapsed.ToString()));
            }
        }

        private async Task ProcessDeletesAsync(IFileSystemItemMetadata[] data)
        {
            if (data == null || data.Length == 0)
            {
                return;
            }

            List<string> deletes = new List<string>();
            foreach (var f in data)
            {
                var fullPath = SyncRootPath + f.PathId;
                deletes.Add(fullPath);
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            ulong num = 0;
            await ThreadPoolExecutor.Execute(deletes, async delegate (string path)
            {
                if (await GetClientStub(path).DeleteAsync())
                {
                    num++;
                }
            }, delegate (string path, Exception e)
            {
                Logger.Error(string.Format("Failed to DeleteAsync for file {0} with error {1}", path, e));
            });
            stopwatch.Stop();
            if (num > 0)
            {
                Logger.Info(string.Format("[NASEngine]: ProcessDeletesAsync finished with size {0}, consumes {1}", num, stopwatch.Elapsed.ToString()));
            }
        }

        private async Task ProcessMovesAsync(Dictionary<string, string> data)
        {
            if (data == null || data.Count == 0)
            {
                return;
            }

            Dictionary<string, string> moves = new Dictionary<string, string>();
            foreach (var key in data.Keys)
            {
                var fullSrcPath = SyncRootPath + key;
                var fullDestPath = SyncRootPath + data[key];
                moves.Add(fullSrcPath, fullDestPath);
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            ulong num = 0;
            await ThreadPoolExecutor.Execute(moves.Keys, async delegate (string path)
            {
                if (await GetClientStub(path).MoveToAsync(moves[path]))
                {
                    num++;
                }
            }, delegate (string path, Exception e)
            {
                Logger.Error(string.Format("Failed to MoveToAsync for file {0} with error {1}", path, e));
            });
            stopwatch.Stop();
            if (num > 0)
            {
                Logger.Info(string.Format("[NASEngine]: ProcessMovesAsync finished with size {0}, consumes {1}", num, stopwatch.Elapsed.ToString()));
            }
        }

        private async Task ProcessUpdateAsync(IFileSystemItemMetadata[] data)
        {
            if (data == null || data.Length == 0)
            {
                return;
            }

            Dictionary<string, IFileSystemItemMetadata> updates = new Dictionary<string, IFileSystemItemMetadata>();
            foreach (var f in data)
            {
                string fullPath = SyncRootPath + f.PathId;
                if (!updates.ContainsKey(fullPath))
                {
                    updates.Add(fullPath, f);
                }
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            ulong num = 0;
            await ThreadPoolExecutor.Execute(updates.Keys, async delegate (string path)
            {
                if (await GetClientStub(path).UpdateAsync(updates[path]))
                {
                    num++;
                }
            }, delegate (string path, Exception e)
            {
                Logger.Error(string.Format("Failed to UpdateAsync for file {0} with error {1}", path, e.Message));
            });
            stopwatch.Stop();

            if (num > 0)
            {
                Logger.Info(string.Format("[NASEngine]: ProcessUpdateAsync finished with size {0}, consumes {1}", num, stopwatch.Elapsed.ToString()));
            }
        }

        private async Task<Dictionary<ulong, IFileSystemItemMetadata>> GetRemoteChildrenRecursivelyAsync()
        {
            var itemId = GetRemoteStorageRootItemId();
            if (itemId == null)
            {
                return null;
            }
            var rootNasFolder = new NASFolder(itemId, m_logger);
            return await rootNasFolder.GetChildrenRecursivelyAsync();
        }

        private async Task<Dictionary<ulong, IFileSystemItemMetadata>> GetLocalChildrenRecursivelyAsync()
        {
            var phRootFolder = Placeholders.GetRootItem();
            return await phRootFolder.GetChildrenRecursivelyAsync(!IsThisDriveAsRPMDir());
        }
    }
}
