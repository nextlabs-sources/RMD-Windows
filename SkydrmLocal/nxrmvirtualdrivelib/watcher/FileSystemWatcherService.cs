using nxrmvirtualdrivelib.core;
using nxrmvirtualdrivelib.placeholder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.watcher
{
    public class FileSystemWatcherService : IDisposable
    {
        private readonly VirtualEngine m_virtualEngine;
        private ILogger m_logger;

        internal readonly FileSystemWatcherQueue m_fswQueue = new FileSystemWatcherQueue();
        private CancellationToken m_token = default;

        public FileSystemWatcherService(string path, VirtualEngine engine, ILogger logger)
        {
            this.m_virtualEngine = engine;
            this.m_logger = logger;

            m_fswQueue.IncludeSubdirectories = true;
            m_fswQueue.Path = path;
            m_fswQueue.NotifyFilters = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Attributes;

            m_fswQueue.Created += OnCreated;
            m_fswQueue.Deleted += OnDeleted;
            m_fswQueue.Changed += OnChanged;
            m_fswQueue.Renamed += OnRenamed;
            m_fswQueue.Error += OnError;
        }

        public async Task StartAsync(CancellationToken token)
        {
            m_token = token;
            m_fswQueue.EnableRaisingEvents = true;
        }

        public async Task StopAsync()
        {
            m_fswQueue.EnableRaisingEvents = false;
        }

        public void OnEvent(WatcherChangeTypes types, string path)
        {
            m_fswQueue.FileWatcherEventHandler(types, this, new FileSystemEventArgs(types, Directory.GetParent(path).FullName, Path.GetFileName(path)));
        }

        private async void OnCreated(object sender, FileSystemEventArgs e)
        {
            m_logger.Info(string.Format("[FileSystemWatcherService]: OnCreated for path {0}.", e.FullPath));
            await OnCreated(e.ChangeType, e.FullPath);
        }

        private async Task OnCreated(WatcherChangeTypes type, string path)
        {
            if (m_token.IsCancellationRequested)
            {
                return;
            }
            try
            {
                if (PlaceholderItem.TryCheckPlaceholder(path, out bool? flag) && !flag.Value)
                {
                    await m_virtualEngine.GetRemoteStub(path).CreateAsync();
                }
            }
            catch (Exception e)
            {
                m_logger.Error(e);
            }
        }

        private async void OnDeleted(object sender, FileSystemEventArgs e)
        {
            m_logger.Info(string.Format("[FileSystemWatcherService]: OnDeleted for path {0}.", e.FullPath));
        }

        private async void OnChanged(object sender, FileSystemEventArgs e)
        {
            m_logger.Info(string.Format("[FileSystemWatcherService]: OnChanged for path {0}.", e.FullPath));
        }

        private async void OnRenamed(object sender, RenamedEventArgs e)
        {
            m_logger.Info(string.Format("[FileSystemWatcherService]: OnRenamed for path {0}, with new dest path {1}.", e.OldFullPath, e.FullPath));
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            m_logger.Error(e.GetException());
        }

        public void Dispose()
        {
            m_fswQueue?.Dispose();
        }
    }
}
