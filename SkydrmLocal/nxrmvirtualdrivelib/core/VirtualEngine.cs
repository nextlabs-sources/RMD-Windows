using nxrmvirtualdrivelib.filter;
using nxrmvirtualdrivelib.logger;
using nxrmvirtualdrivelib.nxl;
using nxrmvirtualdrivelib.placeholder;
using nxrmvirtualdrivelib.stub;
using nxrmvirtualdrivelib.sync;
using nxrmvirtualdrivelib.watcher;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.core
{
    public abstract class VirtualEngine : IEngine
    {
        public readonly int InitialCount = 1000;
        public Placeholders Placeholders;
        public string SyncRootPath;

        private readonly SyncRootCallbackHandler SyncRootCallbackHandler;
        private SyncService SyncService;
        internal FileSystemWatcherService m_fswService;
        protected NxlFileHandler m_nxlFileHandler;

        internal readonly IItemHandler ItemHandler;
        internal CancellationTokenPool CancellationTokenPool;
        public CancellationTokenSource CancellationTokenSource;
        protected ILogger m_logger;

        private readonly MsOfficeFilter m_offinceFilter = new MsOfficeFilter();

        private bool m_enableConsoleLogger = false;

        private List<string> m_RPMDirs = new List<string>();

        public VirtualEngine(string syncRootPath)
        {
            if (m_enableConsoleLogger)
            {
                this.m_logger = new ConsoleLogger().CreateLogger("VirtualDrive");
            }
            else
            {
                this.m_logger = new Log4NetLogger().CreateLogger("VirtualDrive");
            }

            this.SyncRootCallbackHandler = new SyncRootCallbackHandler(this, syncRootPath);
            this.SyncService = new SyncService(syncRootPath, this, m_logger);
            this.m_fswService = new FileSystemWatcherService(syncRootPath, this, m_logger);

            this.m_nxlFileHandler = new NxlFileHandler();

            this.Placeholders = new Placeholders(syncRootPath);
            this.SyncRootPath = syncRootPath;
            this.ItemHandler = new WinFileSystemItemHandler(syncRootPath, true, InitialCount);
            this.CancellationTokenPool = new CancellationTokenPool(int.MaxValue);

            this.SyncService.EventHandler += OnHeartbeat;
        }

        public ILogger Logger { get => m_logger; set { m_logger = value; } }

        public virtual async Task<bool> FilterAsync(string pathName, SourceFrom sourceFrom, SourceAction sourceAction, FileSystemItemType itemType, string newPath = null)
        {
            bool filtered = await m_offinceFilter.FilterAsync(pathName, sourceFrom, sourceAction, itemType, newPath);
            m_logger.Info(string.Format("[VirtualEngine]: FilterAsync with path {0}, item type {1}, filtered: {2}, SourceFrom {3}, SourceAction {4}, newPath {5}",
                pathName, itemType, filtered, sourceFrom, sourceAction, newPath));
            return filtered;
        }

        protected async void OnHeartbeat(object sender, SyncServiceEventArgs e)
        {
            try
            {
                m_RPMDirs.Clear();
                m_RPMDirs.AddRange(m_nxlFileHandler.GetRPMDirs());

                await OnHeartbeat();
            }
            catch (Exception ex)
            {
                m_logger.Error(ex);
            }
        }

        protected abstract Task OnHeartbeat();

        public bool IsThisDrivePath(string path)
        {
            if (path == null || path.Length == 0)
            {
                return false;
            }
            if (path.StartsWith(@"\\?\"))
            {
                path = path.Remove(0, 4);
            }
            return path.StartsWith(SyncRootPath.TrimEnd(new char[1] { Path.DirectorySeparatorChar }), StringComparison.InvariantCultureIgnoreCase);
        }

        public bool IsThisDriveAsRPMDir()
        {
            return IsRPMDir(SyncRootPath, true);
        }

        public bool IsRPMDir(string path)
        {
            return IsRPMDir(path, true);
        }

        public bool IsRPMDir(string dir, bool allowCache = false)
        {
            if (string.IsNullOrEmpty(dir))
            {
                return false;
            }

            List<string> rpmdirs = null;
            if (allowCache)
            {
                rpmdirs = m_RPMDirs;
            }
            else
            {
                rpmdirs = m_nxlFileHandler.GetRPMDirs();
            }

            if (rpmdirs == null || rpmdirs.Count == 0)
            {
                return false;
            }
            foreach (var item in rpmdirs)
            {
                if (item.Equals(dir, StringComparison.InvariantCultureIgnoreCase)
                    || item.StartsWith(dir)
                    || dir.StartsWith(item))
                {
                    return true;
                }
            }
            return false;
        }

        public byte[] GetRemoteStorageRootItemId()
        {
            return Placeholders?.GetRootItem()?.GetRemoteStorageItemId();
        }

        public abstract Task<IFileSystemItem> GetFileSystemItemAsync(string userFileSystemPath, FileSystemItemType itemType, byte[] itemId);

        public IVirtualEngineStub GetClientStub(string userFSPath)
        {
            return new ClientEngineStub(userFSPath, this, m_logger, CancellationTokenSource.Token);
        }

        public IRemoteServiceStub GetRemoteStub(string path)
        {
            return new PlaceholderItemHandler(path, this);
        }

        public async Task StartAsync(bool processChanges = true, CancellationToken token = default)
        {
            m_RPMDirs.Clear();
            m_RPMDirs.AddRange(m_nxlFileHandler.GetRPMDirs());
            try
            {
                CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

                CancellationToken cancelToken = CancellationTokenSource.Token;
                //Connect to sync root.
                SyncRootCallbackHandler.ConnectSyncRoot(cancelToken);

                //Start file system watcher service.
                await m_fswService.StartAsync(token);

                if (processChanges)
                {
                    Task.Run(() =>
                    {
                        SyncService.StartAsync(token);
                    });
                }
            }
            catch (Exception e)
            {
                m_logger.Error(e);
            }
        }

        public async Task StopAsync()
        {
            try
            {
                CancellationTokenSource?.Cancel();
            }
            catch (ObjectDisposedException)
            {

            }

            try
            {
                //Stop file system watcher service.
                await m_fswService.StopAsync();
            }
            catch (Exception e)
            {
                m_logger.Error(e);
            }

            try
            {
                await SyncService.StopAsync();
            }
            catch (Exception e)
            {
                m_logger.Error(e);
            }

            try
            {
                //Disconnect with sync root.
                SyncRootCallbackHandler.Dispose();
            }
            catch (Exception e)
            {
                m_logger.Error(e);
            }

            //try
            //{
            //    if (m_nxlFileHandler.IsRPMDir(SyncRootPath))
            //    {
            //        m_nxlFileHandler.RemoveRPMDir(SyncRootPath, out var errMsg);
            //        if (errMsg != null)
            //        {
            //            m_logger.Error(errMsg);
            //        }
            //    }
            //    if (Directory.Exists(SyncRootPath))
            //    {
            //        Directory.Delete(SyncRootPath, true);
            //    }
            //}
            //catch (Exception e)
            //{
            //    m_logger.Error(e);
            //}

            try
            {
                m_nxlFileHandler?.Dispose();
            }
            catch (Exception e)
            {
                m_logger.Error(e);
            }

            m_RPMDirs.Clear();
        }

        #region Logger

        public virtual void LogInfo(object message)
        {
            Logger.Info(message);
        }

        public virtual void LogDebug(object message)
        {
            Logger.Debug(message);
        }

        public virtual void LogDebug(object message, Exception exception)
        {
            Logger.Debug(message, exception);
        }

        public virtual void LogError(object message)
        {
            Logger.Error(message);
        }

        public virtual void LogError(object message, Exception exception)
        {
            Logger.Error(message, exception);
        }

        public virtual void LogFatal(object message)
        {
            Logger.Fatal(message);
        }

        public virtual void LogFatal(object message, Exception exception)
        {
            Logger.Fatal(message, exception);
        }


        #endregion
    }
}
