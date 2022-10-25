using nxrmvirtualdrivelib.core;
using nxrmvirtualdrivelib.placeholder;
using nxrmvirtualdrivelib.threadpool;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Vanara.PInvoke.CldApi;

namespace nxrmvirtualdrivelib.sync
{
    public class OutboundSyncing
    {
        private readonly VirtualEngine m_virtualEngine;
        private int m_initialCount;
        private ILogger m_logger;

        public OutboundSyncing(VirtualEngine engine, int initialCount, ILogger logger)
        {
            this.m_virtualEngine = engine;
            this.m_initialCount = initialCount;
            this.m_logger = logger;
        }

        public async Task ProcessAsync(CancellationToken cancellationToken)
        {
            using (CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(m_virtualEngine.CancellationTokenSource.Token, cancellationToken))
            {
                var token = cts.Token;
                if (token.IsCancellationRequested)
                {
                    return;
                }
                long total = 0;

                Stopwatch stopwatch = Stopwatch.StartNew();
                total += await ProcessCreatesAsync(token);
                total += await ProcessUpdatesAsync(token);
                stopwatch.Stop();

                if (total > 0)
                {
                    m_logger.Info(string.Format("[OutboundSyncing]: ProcessAsync finished with total {0} items, consumed {1}.", total, stopwatch.Elapsed.ToString()));
                }
            }
        }

        private async Task<long> ProcessCreatesAsync(CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return 0;
            }
            long num = 0;

            Stopwatch stopWatch = Stopwatch.StartNew();
            try
            {
                var creates = await GetCreatesAsync(token);

                await ThreadPoolExecutor.Execute(creates, async delegate (string path)
                {
                    num += await m_virtualEngine.GetRemoteStub(path).CreateAsync(token);
                }, delegate (string path, Exception e)
                {
                    m_logger.Error(string.Format("Failed to CreateRemoteAsync for path {0} with error {1}.", path, e));
                }, m_initialCount, token);

                stopWatch.Stop();
                if (num > 0)
                {
                    m_logger.Info(string.Format("[OutboundSyncing]: ProcessCreateAsync processed with num {0}, target {1}, consumes {2}", num, creates.Count(), stopWatch.Elapsed.ToString()));
                }
            }
            catch (Exception e)
            {
                m_logger.Error(string.Format("[OutboundSyncing]: ProcessCreateAsync failed with error {0}.", e));
            }
            return num;
        }

        private async Task<IEnumerable<string>> GetCreatesAsync(CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return null;
            }

            List<string> data = new List<string>();
            await ThreadPoolExecutor.Execute(Directory.EnumerateFileSystemEntries(m_virtualEngine.SyncRootPath,
                "*", SearchOption.AllDirectories),
                async delegate (string path)
                {
                    if (PlaceholderItem.TryGetPlaceholderItem(path, out var item))
                    {
                        (bool IsSuccess, bool? IsNew) = await item.IsNew();
                        if (IsSuccess && IsNew.Value)
                        {
                            data.Add(path);
                        }
                    }
                }, delegate (string path, Exception e)
                {
                    m_logger.Error(string.Format("Failed to get create item {0} with error {1}", path, e));
                }, m_initialCount, token);
            return data;
        }

        private async Task<long> ProcessUpdatesAsync(CancellationToken token)
        {
            long num = 0;
            if (token.IsCancellationRequested)
            {
                return num;
            }

            Stopwatch stopWatch = Stopwatch.StartNew();
            try
            {
                var updates = await GetUpdatesAsync(token);
                await ThreadPoolExecutor.Execute(updates, async delegate (UpdateOperation operation)
                {
                    num += await m_virtualEngine.GetRemoteStub(operation.UserFsPath).UpdateAsync(token);
                }, delegate (UpdateOperation operation, Exception e)
                {
                    m_logger.Error(string.Format("Failed to ProcessUpdateAsync for item {0} with error {1}.", operation.UserFsPath, e));
                }, m_initialCount, token);

                stopWatch.Stop();
                if (num > 0)
                {
                    m_logger.Info(string.Format("[OutboundSyncing]: ProcessUpdatesAsync processed with num {0}, target {1}, consumes {2}.", num, updates.Count(), stopWatch.Elapsed.ToString()));
                }
            }
            catch (Exception e)
            {
                m_logger.Error(string.Format("[OutboundSyncing]: ProcessUpdatesAsync failed with error {0}.", e));
            }
            return num;
        }

        private async Task<IEnumerable<UpdateOperation>> GetUpdatesAsync(CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return Enumerable.Empty<UpdateOperation>();
            }
            ConcurrentDictionary<string, UpdateOperation> data = new ConcurrentDictionary<string, UpdateOperation>();
            await ThreadPoolExecutor.Execute(Directory.EnumerateFileSystemEntries(m_virtualEngine.SyncRootPath, "*", SearchOption.AllDirectories),
                async delegate (string path)
                {
                    if (WinFileSystemItem.TryGetAttributes(path, out var attributes) && attributes.HasValue && ((attributes.Value & FileAttributes.Offline) == 0))
                    {
                        if (PlaceholderItem.TryGetPlaceholderState(path, out var state) && state.HasValue && ((state.Value & CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_IN_SYNC) == 0))
                        {
                            if (PlaceholderItem.TryGetPlaceholderItem(path, out var item))
                            {
                                (bool IsSuccess, bool? IsNew) newResult = await item.IsNew();
                                if (newResult.IsSuccess && !newResult.IsNew.Value)
                                {
                                    (bool IsSuccess, bool? IsMoved) moveResult = await item.IsMoved();
                                    if (moveResult.IsSuccess && !moveResult.IsMoved.Value)
                                    {
                                        (bool IsSuccess, long? USN) usnResult = await WinFileSystemItem.GetUSNAsync(path);
                                        if (usnResult.IsSuccess && item.TryGetRemoteStorageItemId(out var remoteItemId))
                                        {
                                            UpdateOperation operation = new UpdateOperation
                                            {
                                                UserFsPath = path,
                                                ItemType = item.GetItemType(),
                                                RemoteItemId = remoteItemId,
                                                USN = usnResult.USN.Value
                                            };
                                            data.TryAdd(path, operation);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }, delegate (string path, Exception e)
                {
                    m_logger.Error(string.Format("Failed to GetUpdatesAsync for item {0} with error {1}.", path, e));
                }, m_initialCount, token);
            return data.Select((KeyValuePair<string, UpdateOperation> pair) => pair.Value);
        }
    }
}
