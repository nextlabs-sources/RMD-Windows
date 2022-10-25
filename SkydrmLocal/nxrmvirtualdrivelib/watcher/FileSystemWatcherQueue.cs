using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.watcher
{
    public class FileSystemWatcherQueue : IDisposable
    {
        private FileSystemWatcher m_fsWatcher;
        private readonly BlockingCollection<Tuple<WatcherChangeTypes, object, object>> m_blockingQueue;

        private CancellationTokenSource m_cts;
        private Task m_watcherTask;

        private bool m_disposed;

        private FileSystemEventHandler m_created;
        private FileSystemEventHandler m_deleted;
        private FileSystemEventHandler m_changed;
        private RenamedEventHandler m_renamed;
        private ErrorEventHandler m_error;

        public event FileSystemEventHandler Created
        {
            add
            {
                m_created = (FileSystemEventHandler)Delegate.Combine(m_created, value);
            }
            remove
            {
                m_created = (FileSystemEventHandler)Delegate.Remove(m_created, value);
            }
        }

        public event FileSystemEventHandler Deleted
        {
            add
            {
                m_deleted = (FileSystemEventHandler)Delegate.Combine(m_deleted, value);
            }
            remove
            {
                m_deleted = (FileSystemEventHandler)Delegate.Remove(m_deleted, value);
            }
        }

        public event FileSystemEventHandler Changed
        {
            add
            {
                m_changed = (FileSystemEventHandler)Delegate.Combine(m_changed, value);
            }
            remove
            {
                m_changed = (FileSystemEventHandler)Delegate.Remove(m_changed, value);
            }
        }

        public event RenamedEventHandler Renamed
        {
            add
            {
                m_renamed = (RenamedEventHandler)Delegate.Combine(m_renamed, value);
            }
            remove
            {
                m_renamed = (RenamedEventHandler)Delegate.Remove(m_renamed, value);
            }
        }

        public event ErrorEventHandler Error
        {
            add
            {
                m_error = (ErrorEventHandler)Delegate.Combine(m_error, value);
            }
            remove
            {
                m_error = (ErrorEventHandler)Delegate.Remove(m_error, value);
            }
        }

        public string Path
        {
            get => m_fsWatcher.Path;
            set
            {
                m_fsWatcher.Path = value;
            }
        }

        public bool IncludeSubdirectories
        {
            get => m_fsWatcher.IncludeSubdirectories;
            set
            {
                m_fsWatcher.IncludeSubdirectories = value;
            }
        }

        public NotifyFilters NotifyFilters
        {
            get => m_fsWatcher.NotifyFilter;
            set
            {
                m_fsWatcher.NotifyFilter = value;
            }
        }

        public bool EnableRaisingEvents
        {
            get => m_fsWatcher.EnableRaisingEvents;
            set
            {
                m_fsWatcher.EnableRaisingEvents = value;
            }
        }

        public FileSystemWatcherQueue()
        {
            m_blockingQueue = new BlockingCollection<Tuple<WatcherChangeTypes, object, object>>();
            m_fsWatcher = InitFSWatcher();
            m_cts = new CancellationTokenSource();
            m_watcherTask = RunWatcherTask(m_cts.Token);
        }

        public void FileWatcherEventHandler(WatcherChangeTypes types, object sender, object eventArgs)
        {
            m_blockingQueue.Add(new Tuple<WatcherChangeTypes, object, object>(types, sender, eventArgs));
        }

        private FileSystemWatcher InitFSWatcher()
        {
            FileSystemWatcher watcher = new FileSystemWatcher
            {
                InternalBufferSize = 65536
            };
            watcher.Created += delegate (object sender, FileSystemEventArgs e)
            {
                FileWatcherEventHandler(WatcherChangeTypes.Created, sender, e);
            };
            watcher.Deleted += delegate (object sender, FileSystemEventArgs e)
            {
                FileWatcherEventHandler(WatcherChangeTypes.Deleted, sender, e);
            };
            watcher.Renamed += delegate (object sender, RenamedEventArgs e)
            {
                FileWatcherEventHandler(WatcherChangeTypes.Renamed, sender, e);
            };
            watcher.Changed += delegate (object sender, FileSystemEventArgs e)
            {
                FileWatcherEventHandler(WatcherChangeTypes.Changed, sender, e);
            };
            watcher.Error += delegate (object sender, ErrorEventArgs e)
            {
                m_error?.Invoke(sender, e);
            };
            return watcher;
        }

        public Task RunWatcherTask(CancellationToken token)
        {
            return Task.Run(delegate
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        var item = m_blockingQueue.Take();
                        token.ThrowIfCancellationRequested();

                        var types = item.Item1;
                        var sender = item.Item2;
                        var eventArgs = item.Item3;

                        ProcessEvents(types, sender, eventArgs);
                    }
                }
                catch (OperationCanceledException)
                {

                }
            }, token);
        }

        private void ProcessEvents(WatcherChangeTypes types, object sender, object eventArgs)
        {
            switch (types)
            {
                case WatcherChangeTypes.Created:
                    m_created?.Invoke(sender, eventArgs as FileSystemEventArgs);
                    break;
                case WatcherChangeTypes.Deleted:
                    m_deleted?.Invoke(sender, eventArgs as FileSystemEventArgs);
                    break;
                case WatcherChangeTypes.Changed:
                    m_changed?.Invoke(sender, eventArgs as FileSystemEventArgs);
                    break;
                case WatcherChangeTypes.Renamed:
                    m_renamed?.Invoke(sender, eventArgs as RenamedEventArgs);
                    break;
            }
        }

        public void Dispose()
        {
            if (!m_disposed)
            {
                m_fsWatcher.Dispose();
                m_cts.Cancel();
                m_watcherTask.Wait();

                m_disposed = true;
            }
        }
    }
}
