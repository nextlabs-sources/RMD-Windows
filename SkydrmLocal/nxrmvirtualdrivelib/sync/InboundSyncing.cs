using nxrmvirtualdrivelib.core;
using nxrmvirtualdrivelib.placeholder;
using nxrmvirtualdrivelib.threadpool;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.sync
{
    public class InboundSyncing
    {
        private readonly VirtualEngine m_virtualEngine;
        private readonly int m_initialCount;
        private readonly ILogger m_logger;

        public InboundSyncing(VirtualEngine engine, int initialCount, ILogger logger)
        {
            this.m_virtualEngine = engine;
            this.m_initialCount = initialCount;
            this.m_logger = logger;
        }

        public async Task ProcessAsync(CancellationToken cancellationToken)
        {
            using (CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(m_virtualEngine.CancellationTokenSource.Token, cancellationToken))
            {
                CancellationToken token = cts.Token;
                if (token.IsCancellationRequested)
                {
                    return;
                }

                long num = 0;

                Stopwatch stopWatch = Stopwatch.StartNew();
                num += await ProcessHydrationAsync(token);
                stopWatch.Stop();

                if (num > 0)
                {
                    m_logger.Info(string.Format("[InboundSyncing]: ProcessAsync process items {0}, consumes {1}", num, stopWatch.Elapsed.ToString()));
                }
            }
        }

        private async Task<long> ProcessHydrationAsync(CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return 0;
            }

            long foundHydrate = 0;
            long foundDehydrate = 0;
            long processedHydrated = 0;
            long processedDehydrated = 0;

            Stopwatch stopWatch = Stopwatch.StartNew();
            await ThreadPoolExecutor.Execute(Directory.EnumerateFileSystemEntries(m_virtualEngine.SyncRootPath, "*", SearchOption.AllDirectories),
                async delegate (string path)
                {
                    PlaceholderItemHandler handler = new PlaceholderItemHandler(path, m_virtualEngine);
                    var (FoundHydrate, FoundDehydrate, ProcessedHydrated, ProcessedDehydrated) = await handler.ProcessHydrationAsync(token);
                    foundHydrate += FoundDehydrate;
                    foundDehydrate += FoundDehydrate;
                    processedHydrated += ProcessedHydrated;
                    processedDehydrated += ProcessedDehydrated;

                }, delegate (string path, Exception e)
                {
                    m_logger.Error(string.Format("Failed to ProcessHydrationAsync for file {0} with error {1}.", path, e));
                }, m_initialCount, token);
            stopWatch.Stop();

            if (foundHydrate > 0 || foundDehydrate > 0)
            {
                m_logger.Info(string.Format("[InboundSyncing]: ProcessHydrationAsync foundHydrate {0}, foundDehydrate {1}, consumes {2}", foundHydrate, foundDehydrate, stopWatch.Elapsed.ToString()));
            }
            return processedHydrated + processedDehydrated;
        }

    }
}
