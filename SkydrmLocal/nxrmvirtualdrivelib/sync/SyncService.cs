using nxrmvirtualdrivelib.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace nxrmvirtualdrivelib.sync
{
    public class SyncService : IDisposable
    {
        //private const double M_TIMER_INTERVAL = 600000.0;
        private const double M_TIMER_INTERVAL = 60000.0;
        private readonly System.Timers.Timer m_Timer;

        private readonly string m_syncRootPath;
        private readonly IEngine m_virtualEngine;
        private readonly ILogger m_logger;
        private CancellationToken m_token;
        private CancellationTokenSource m_cts;

        private bool m_disposed = false;

        public event SyncServiceEventHandler EventHandler;

        private readonly InboundSyncing m_InboundSyncing;
        private readonly OutboundSyncing m_OutboundSyncing;

        public SyncService(string syncRootPath, IEngine engine, ILogger logger, CancellationToken token = default)
        {
            m_Timer = new System.Timers.Timer(M_TIMER_INTERVAL)
            {
                AutoReset = false
            };
            m_Timer.Elapsed += OnTimerElapsed;

            this.m_syncRootPath = syncRootPath;
            this.m_virtualEngine = engine;
            this.m_logger = logger;
            this.m_token = token;

            this.m_InboundSyncing = new InboundSyncing((VirtualEngine)engine, 1000, logger);
            this.m_OutboundSyncing = new OutboundSyncing((VirtualEngine)engine, 1000, logger);
        }

        public async Task StartAsync(CancellationToken token = default)
        {
            m_cts?.Dispose();
            m_cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            await OnHeartbeat();
        }

        public async Task StopAsync()
        {
            m_Timer.Stop();
            try
            {
                m_cts?.Cancel();
            }
            catch (ObjectDisposedException e)
            {

            }

        }

        private async void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            await OnHeartbeat();
        }

        private async Task OnHeartbeat()
        {
            if (m_token.IsCancellationRequested)
            {
                return;
            }

            try
            {
                await m_InboundSyncing.ProcessAsync(m_token);
                await m_OutboundSyncing.ProcessAsync(m_token);

                await Task.Run(() =>
                {
                    EventHandler?.Invoke(this, new SyncServiceEventArgs());
                });
            }
            catch (Exception)
            {

            }
            finally
            {
                if (!m_token.IsCancellationRequested)
                {
                    m_Timer.AutoReset = false;
                    m_Timer.Start();
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                if (disposing)
                {
                    m_Timer.Stop();
                    m_Timer.Dispose();
                    m_cts?.Dispose();
                }
                m_disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
