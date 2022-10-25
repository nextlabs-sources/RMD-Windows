using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.Provider;

namespace nxrmvirtualdrivelib.register
{
    public class ProviderRegister
    {
        public static string SKYDRM_PROVIDER_PREFIX = "SkyDRMStorageProvider";

        public static async Task RegisterAsync(RegisterConfig config)
        {
            if (config == null)
            {
                return;
            }
            var id = config.Id;
            var serverPath = config.ServerPath;
            var clientPath = config.ClientPath;
            var displayName = config.DisplayName;
            var iconResource = config.IconResource;
            var version = config.Version;
            var recycleBinUri = config.RecyleBin;
            var syncRootContext = config.SyncRootContext;

            if (id == null || id.Length == 0)
            {
                return;
            }
            if (IsRegistered(id, displayName))
            {
                return;
            }
            if (serverPath == null || serverPath.Length == 0)
            {
                return;
            }
            if (clientPath == null || clientPath.Length == 0)
            {
                return;
            }
            StorageProviderSyncRootInfo storageInfo = new StorageProviderSyncRootInfo
            {
                Path = await StorageFolder.GetFolderFromPathAsync(clientPath),
                Id = id,
                DisplayNameResource = displayName,
                IconResource = iconResource,
                Version = version,
                RecycleBinUri = recycleBinUri
            };

            storageInfo.Context = CryptographicBuffer.ConvertStringToBinary(
            syncRootContext, BinaryStringEncoding.Utf8);

            storageInfo.HydrationPolicy = StorageProviderHydrationPolicy.Full;
            storageInfo.HydrationPolicyModifier = StorageProviderHydrationPolicyModifier.None;
            storageInfo.PopulationPolicy = StorageProviderPopulationPolicy.Full;
            storageInfo.InSyncPolicy = StorageProviderInSyncPolicy.FileCreationTime
                | StorageProviderInSyncPolicy.DirectoryCreationTime
                | StorageProviderInSyncPolicy.FileLastWriteTime;

            //var customStates = storageInfo.StorageProviderItemPropertyDefinitions;
            //AddCustomState(customStates, "CustomStateName1", 1);
            //AddCustomState(customStates, "CustomStateName2", 2);
            //AddCustomState(customStates, "CustomStateName3", 3);

            StorageProviderSyncRootManager.Register(storageInfo);
        }

        public static async Task<bool> UnRegisterByNameAsync(string displayName)
        {
            var syncRoots = await GetSyncRoots();
            if (syncRoots == null || syncRoots.Count == 0)
            {
                return false;
            }
            if (syncRoots.ContainsKey(displayName))
            {
                return await UnRegisterByIdAsync(syncRoots[displayName]);
            }
            return false;
        }

        public static async Task<bool> UnRegisterByIdAsync(string syncRootId)
        {
            if (string.IsNullOrEmpty(syncRootId))
            {
                return false;
            }
            if (!IsRegistered(syncRootId))
            {
                return false;
            }
            StorageProviderSyncRootManager.Unregister(syncRootId);
            return true;
        }

        public static bool IsRegistered(string syncRootId, string displayName = "")
        {
            if (syncRootId == null || syncRootId.Length == 0)
            {
                return false;
            }
            var syncRoots = StorageProviderSyncRootManager.GetCurrentSyncRoots();
            if (syncRoots == null || syncRoots.Count == 0)
            {
                return false;
            }
            foreach (var item in syncRoots)
            {
                if (item == null)
                {
                    continue;
                }
                if (string.Equals(syncRootId, item.Id, StringComparison.InvariantCultureIgnoreCase)
                    || string.Equals(displayName, item.DisplayNameResource, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public static async Task<Dictionary<string, string>> GetSyncRoots()
        {
            var syncRoots = StorageProviderSyncRootManager.GetCurrentSyncRoots();

            Dictionary<string, string> rt = new Dictionary<string, string>();
            foreach (var item in syncRoots)
            {
                if (item.Id.StartsWith(SKYDRM_PROVIDER_PREFIX)
                    || item.Id.StartsWith("TestStorageProvider"))
                {
                    rt.Add(item.DisplayNameResource, item.Id);
                }
            }
            return rt;
        }

        public static string GetProviderCtx(string syncRootId)
        {
            if (syncRootId == null || syncRootId.Length == 0)
            {
                return "";
            }
            var syncRoots = StorageProviderSyncRootManager.GetCurrentSyncRoots();
            if (syncRoots == null || syncRoots.Count == 0)
            {
                return "";
            }
            StorageProviderSyncRootInfo target = null;
            foreach (var item in syncRoots)
            {
                if (item == null)
                {
                    continue;
                }
                if (string.Equals(syncRootId, item.Id))
                {
                    target = item;
                    break;
                }
            }
            if (target != null)
            {
                var buffer = target.Context;
                if (buffer == null)
                {
                    return "";
                }
                CryptographicBuffer.CopyToByteArray(target.Context, out byte[] value);
                return Encoding.Default.GetString(value);
            }
            return "";
        }

        private static void AddCustomState(IList<StorageProviderItemPropertyDefinition> customeStates, string displayNameResource, int id)
        {
            StorageProviderItemPropertyDefinition definition = new StorageProviderItemPropertyDefinition()
            {
                DisplayNameResource = displayNameResource,
                Id = id
            };
            customeStates.Add(definition);
        }
    }
}
