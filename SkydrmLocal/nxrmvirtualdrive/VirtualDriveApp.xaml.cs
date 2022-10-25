using nxrmvirtualdrive.pipe;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using VirtualDrive;
using VirtualDrive.core;
using VirtualDrive.nas;

namespace nxrmvirtualdrive
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class VirtualDriveApp : Application
    {
        private ConcurrentDictionary<string, VirtualEngine> SyncEngines = new ConcurrentDictionary<string, VirtualEngine>();
        public string AppPath;

        public string Version
        {
            get { return "1.0.0"; }
        }

        public string SyncRootId
        {
            get
            {
                return string.Format("{0}_[DRIVE_TYPE]!{1}![REMOTE_USER]", ProviderRegister.SKYDRM_PROVIDER_PREFIX, WindowsIdentity.GetCurrent().User);
            }
        }

        public string ClientFolder
        {
            get
            {
                return Environment.GetEnvironmentVariable("USERPROFILE") + @"\SkyDRM-vdrives\{DISPLAY_NAME}";
            }
        }

        private NamedPipeServer m_NamedPipeServer = new NamedPipeServer();

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            AppPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            try
            {
                m_NamedPipeServer.Start();

                await TryConnectSyncRoots();
            }
            catch (Exception exp)
            {

            }
        }

        private async void Application_Exit(object sender, ExitEventArgs e)
        {
            m_NamedPipeServer.Stop();

            await TryDisConnectSyncRoots();
        }

        private async Task TryConnectSyncRoots()
        {
            Console.WriteLine("Try connecting sync roots...");

            var syncRootIds = await GetSkyDRMSyncRootsAsync();
            foreach (var id in syncRootIds)
            {
                await StartEngineAsync(id);
            }
        }

        private async Task TryDisConnectSyncRoots()
        {
            var engines = SyncEngines.Values;
            if (engines == null || engines.Count == 0)
            {
                return;
            }
            foreach (var e in engines)
            {
                await e.StopAsync();
            }

            SyncEngines.Clear();
        }

        public async Task StartEngineAsync(string syncRootId)
        {
            var ctx = ProviderRegister.GetProviderCtx(syncRootId);
            var splits = Regex.Split(ctx, "->");
            if (splits == null || splits.Length == 0)
            {
                return;
            }

            var serverFolder = splits[0] ?? "";
            var clientFolder = splits[1] ?? "";

            NASEngine engine = new NASEngine(clientFolder);
            var itemId = WinFileSystemItem.GetItemIdByPath(serverFolder);
            engine.Placeholders.GetRootItem().SetRemoteStorageItemId(itemId);

            if(!SyncEngines.ContainsKey(syncRootId))
            {
                SyncEngines.TryAdd(syncRootId, engine);
            }

            await engine.StartAsync();
        }

        public async Task StopEngineAsync(string displayName)
        {
            var syncRootId = await GetSyncRootIdByNameAsync(displayName);
            if (string.IsNullOrEmpty(syncRootId))
            {
                return;
            }
            if (SyncEngines.ContainsKey(syncRootId))
            {
                SyncEngines.TryRemove(syncRootId, out var engine);
                await engine.StopAsync();
            }
        }

        private async Task<List<string>> GetSkyDRMSyncRootsAsync()
        {
            List<string> rt = new List<string>();
            var syncRoots = await ProviderRegister.GetSyncRoots();
            if (syncRoots != null && syncRoots.Count != 0)
            {
                rt.AddRange(syncRoots.Values);
            }
            return rt;
        }

        private async Task<string> GetSyncRootIdByNameAsync(string displayName)
        {
            var syncRoots = await ProviderRegister.GetSyncRoots();
            if (syncRoots == null || syncRoots.Count == 0)
            {
                return null;
            }
            if (syncRoots.TryGetValue(displayName, out var id))
            {
                return id;
            }
            return null;
        }
    }
}
