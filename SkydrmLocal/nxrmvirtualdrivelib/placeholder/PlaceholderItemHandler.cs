using nxrmvirtualdrivelib.core;
using nxrmvirtualdrivelib.filter;
using nxrmvirtualdrivelib.impl;
using nxrmvirtualdrivelib.metadata;
using nxrmvirtualdrivelib.threadpool;
using nxrmvirtualdrivelib.utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Vanara.PInvoke.CldApi;

namespace nxrmvirtualdrivelib.placeholder
{
    public class PlaceholderItemHandler : IRemoteServiceStub
    {
        private static ConcurrentDictionary<string, PlaceholderItemIdentity> m_AnonymousNxlDeletedItems = new ConcurrentDictionary<string, PlaceholderItemIdentity>();
        private string m_path;
        private IFileSystemItem m_item;
        private PlaceholderItemIdentity m_Id;

        private readonly VirtualEngine m_engine;
        private readonly ILogger m_logger;

        public PlaceholderItemHandler(string path, VirtualEngine engine)
        {
            this.m_path = path;
            this.m_engine = engine;
            this.m_logger = engine.Logger;
        }

        public PlaceholderItemHandler(string path, PlaceholderItemIdentity id, VirtualEngine engine) : this(path, engine)
        {
            this.m_Id = id;
        }

        public async Task<IFileSystemItem> GetFileSystemItemAsync()
        {
            if (m_item == null)
            {
                if (m_Id == null)
                {
                    m_Id = PlaceholderItem.GetPlaceHolderId(m_path, true);
                }
                m_item = await m_engine.GetFileSystemItemAsync(m_path, m_Id.ItemType, m_Id?.RemoteStorageItemId);
            }
            return m_item;
        }

        private bool TryGetFileSystemItemType(out FileSystemItemType? itemType)
        {
            if (m_Id != null)
            {
                itemType = m_Id.ItemType;
                return true;
            }
            if (m_item != null)
            {
                itemType = (m_item is IVirtualFile) ? FileSystemItemType.File : FileSystemItemType.Folder;
                return true;
            }
            if (PlaceholderItem.TryGetPlaceholderId(m_path, true, out m_Id))
            {
                itemType = m_Id.ItemType;
                return true;
            }
            itemType = null;
            return false;
        }

        private bool TryGetAttributesAndType(out FileAttributes? attributes, out FileSystemItemType? itemType)
        {
            if (WinFileSystemItem.TryGetAttributes(m_path, out attributes) && attributes.HasValue)
            {
                itemType = (((uint?)attributes & 0x10) != 0) ? FileSystemItemType.Folder : FileSystemItemType.File;

                return true;
            }
            attributes = null;
            itemType = null;
            return false;
        }

        public async Task OnOpenCompletionAsync(CancellationToken token = default)
        {

        }

        public async Task OnCloseCompletionAsync(CancellationToken token = default)
        {

        }

        public async Task<long> CreateAsync(CancellationToken token = default)
        {
            long num = 0;
            if (token.IsCancellationRequested)
            {
                return num;
            }
            if (m_engine.Placeholders.TryGetItem(m_path, out var item))
            {
                (bool IsSuccess, bool? IsNew) = await item.IsNew();
                if (IsSuccess)
                {
                    num += await CreateInternalAsync(token);
                }
            }
            return num;
        }

        private async Task<long> CreateInternalAsync(CancellationToken token)
        {
            long num = 0;
            if (token.IsCancellationRequested)
            {
                return num;
            }

            bool flag = TryGetAttributesAndType(out var attributes, out var itemType);
            if (flag)
            {
                flag = !await m_engine.FilterAsync(m_path, SourceFrom.CLIENT, SourceAction.CREATE, itemType.Value);
            }
            if (flag)
            {
                var fileSystemInfo = FileHelper.GetFileInfo(m_path);
                if (fileSystemInfo == null)
                {
                    return num;
                }

                FileStream fileStream = null;
                try
                {
                    try
                    {
                        if (fileSystemInfo is FileInfo)
                        {
                            fileStream = ((FileInfo)fileSystemInfo).Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                        }
                    }
                    catch (Exception)
                    {

                    }
                    fileSystemInfo.Refresh();
                    IFileSystemItemMetadata metadata = FileHelper.Convert(fileSystemInfo);

                    var dirName = Path.GetDirectoryName(m_path);
                    if (!PlaceholderItem.TryGetPlaceholderId(dirName, true, out var itemId))
                    {
                        return num;
                    }

                    var item = await m_engine.GetFileSystemItemAsync(m_path, FileSystemItemType.Folder, itemId?.RemoteStorageItemId);
                    if (item == null)
                    {
                        return num;
                    }
                    byte[] remoteStorageId = null;
                    if (metadata is FileMetadata)
                    {
                        bool markInSync = fileStream != null;
                        FileMetadata fileMetadata = (FileMetadata)metadata;
                        if (m_AnonymousNxlDeletedItems.TryRemove(m_path, out var id))
                        {
                            fileMetadata.IsNxlFile = id.IsAnonymousNxlFile;
                            fileMetadata.IsAnonymousNxlFile = id.IsAnonymousNxlFile;
                            fileMetadata.Name = id.FileName;
                        }
                        //Create remote file.
                        remoteStorageId = await ((IVirtualFolder)item).CreateFileAsync(fileMetadata, fileStream, token);
                        //Convert target file to placeholder.
                        bool success = await PlaceholderItem.ConvertToPlaceholder(m_path, fileMetadata.Name, FileSystemItemType.File, remoteStorageId, null, markInSync, false,
                            fileMetadata.IsNxlFile, fileMetadata.IsAnonymousNxlFile, token);
                        if (success)
                        {
                            num++;
                            m_logger.Info(string.Format("File {0} is converted to a placeholder.", m_path));
                        }
                    }
                    else
                    {
                        remoteStorageId = await ((IVirtualFolder)item).CreateFolderAsync((IFolderMetadata)metadata, token);
                        bool success = await PlaceholderItem.ConvertToPlaceholder(m_path, metadata.Name, FileSystemItemType.File, remoteStorageId, null, true, false, false, false, token);
                        if (success)
                        {
                            m_logger.Info(string.Format("Folder {0} is converted to a placeholder.", m_path));

                            await ThreadPoolExecutor.Execute(Directory.GetFileSystemEntries(m_path), async delegate (string path)
                            {
                                num += await m_engine.GetRemoteStub(path).CreateAsync();
                            }, delegate (string path, Exception e)
                            {
                                m_logger.Error(string.Format("Failed to CreateAsync for path {0}, with error {1}", path, e));
                            }, m_engine.InitialCount, token);
                        }
                    }
                }
                finally
                {
                    fileStream?.Close();
                }
            }
            return num;
        }

        public async Task<bool> DeleteAsync(IDeleteFile confirmation, CancellationToken token = default)
        {
            if (token.IsCancellationRequested)
            {
                return false;
            }

            bool result = true;
            bool flag = TryGetFileSystemItemType(out FileSystemItemType? itemType);
            if (flag)
            {
                flag = !await m_engine.FilterAsync(m_path, SourceFrom.REMOTE, SourceAction.DELETE, itemType.Value);
            }
            if (flag)
            {
                using (m_engine.CancellationTokenPool.AddOne(token))
                {
                    IFileSystemItem fsItem = await GetFileSystemItemAsync();
                    await fsItem.DeleteAsync(confirmation);
                    result = ((confirmation != null) ? (confirmation as DeleteFile) : null)?.Confirmed ?? true;
                }
            }
            return result;
        }

        public async Task<bool> DeleteCompletionAsync(ulong? fileId, ulong? fileIdData, string name, CancellationToken token = default)
        {
            if (token.IsCancellationRequested)
            {
                return false;
            }

            IFileSystemItem item = await GetFileSystemItemAsync();
            return await DeleteCompletionAsync(m_engine, m_path, item, fileId, fileIdData, name, token); ;
        }

        public async Task<bool> MoveToAsync(string destPath, byte[] remoteStorageFolderId, ulong? fileId, ulong? placehodlerId,
            string fileName, CancellationToken token = default)
        {
            if (token.IsCancellationRequested)
            {
                return false;
            }

            string sourcePath = m_path;
            bool flag = false;
            if (TryGetFileSystemItemType(out FileSystemItemType? itemType) && itemType.HasValue)
            {
                flag = true;
            }
            if (flag)
            {
                flag = !await m_engine.FilterAsync(sourcePath, SourceFrom.CLIENT, SourceAction.MOVE, itemType.Value, destPath);
            }
            if (flag)
            {
                var fileSystemItem = await GetFileSystemItemAsync();
                if (fileSystemItem == null)
                {
                    return false;
                }
                string directoryName = Path.GetDirectoryName(destPath);
                if (m_engine.IsThisDrivePath(directoryName))
                {
                    return await fileSystemItem.MoveToAsync(destPath, remoteStorageFolderId, token);
                }
                else
                {
                    // TBD
                    //If file not this this dirve, should delete first then create a new one.
                }
            }
            return false;
        }

        public async Task<bool> MoveToCompletionAsync(string destPath, byte[] remoteStorageFolderId, CancellationToken token = default)
        {
            if (token.IsCancellationRequested)
            {
                return false;
            }

            string sourcePath = m_path;
            bool flag = false;
            if (TryGetFileSystemItemType(out FileSystemItemType? itemType) && itemType.HasValue)
            {
                flag = true;
            }
            if (flag)
            {
                flag = !await m_engine.FilterAsync(sourcePath, SourceFrom.CLIENT, SourceAction.MOVE_COMPLETION, itemType.Value, destPath);
            }
            if (flag)
            {
                var fileSystemItem = await GetFileSystemItemAsync();
                if (fileSystemItem == null)
                {
                    return false;
                }
                string directoryName = Path.GetDirectoryName(destPath);
                if (m_engine.IsThisDrivePath(directoryName))
                {
                    return await fileSystemItem.MoveToCompletionAsync(destPath, remoteStorageFolderId, token);
                }
                else
                {
                    // TBD
                    //If file not this this dirve, should delete first then create a new one.
                }
            }

            return false;
        }

        public async Task<bool> DeleteCompletionAsync(VirtualEngine engine, string userFsPath, IFileSystemItem item,
            ulong? fileId, ulong? fileIdData, string name,
            CancellationToken token = default)
        {
            if (token.IsCancellationRequested)
            {
                return false;
            }

            bool result = true;
            bool flag = TryGetFileSystemItemType(out FileSystemItemType? itemType);
            if (flag)
            {
                flag = !await m_engine.FilterAsync(m_path, SourceFrom.REMOTE, SourceAction.DELETE_COMPLETION, itemType.Value);
            }
            if (flag)
            {
                result = await item.DeleteCompletionAsync(token);
                if (result)
                {
                    if (m_Id != null && (m_Id.IsAnonymousNxlFile || m_Id.IsConflict))
                    {
                        m_AnonymousNxlDeletedItems.TryAdd(m_path, m_Id);
                    }
                }
            }
            return result;
        }

        public async Task<(long FoundHydrate, long FoundDehydrate, long ProcessedHydrated, long ProcessedDehydrated)> ProcessHydrationAsync(CancellationToken token)
        {
            long foundHydrate = 0;
            long foundDehydrate = 0;
            long processedHydrated = 0;
            long processedDehydrated = 0;

            PlaceholderFile placeholderFile = new PlaceholderFile(m_path);
            if (placeholderFile.TryHydrationRequired(out var hydrationRequired) && hydrationRequired.HasValue)
            {
                if (hydrationRequired.Value)
                {
                    foundHydrate++;

                    bool flag = placeholderFile.Hydrate(0, -1);
                    if (flag)
                    {
                        processedHydrated++;
                    }
                }
            }
            else if (placeholderFile.TryDehydrationRequired(out var deHydrationRequired) && deHydrationRequired.HasValue)
            {
                if (deHydrationRequired.Value)
                {
                    foundDehydrate++;

                    bool flag = placeholderFile.DeHydrate(0, -1);
                    if (flag)
                    {
                        processedDehydrated++;
                    }
                }
            }
            return (foundHydrate, foundDehydrate, processedHydrated, processedDehydrated);
        }

        public async Task<long> UpdateAsync(CancellationToken token = default)
        {
            long num = 0;
            if (token.IsCancellationRequested)
            {
                return num;
            }
            if (m_engine.Placeholders.TryGetItem(m_path, out var item))
            {
                (bool IsSuccess, bool? IsNew) = await item.IsNew();
                if (IsSuccess && !IsNew.Value)
                {
                    num = await UpdateInternalAsync(token);
                }
            }
            return num;
        }

        private async Task<long> UpdateInternalAsync(CancellationToken token = default)
        {
            long num = 0;
            if (token.IsCancellationRequested)
            {
                return num;
            }

            bool flag = TryGetFileSystemItemType(out var itemType);
            if (flag)
            {
                flag = !await m_engine.FilterAsync(m_path, SourceFrom.CLIENT, SourceAction.UPDATE, itemType.Value);
            }
            if (flag && PlaceholderItem.TryGetPlaceholderState(m_path, out var state)
                && (state.Value & CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_IN_SYNC) == 0)
            {
                //if ((state.Value & CF_PLACEHOLDER_STATE.CF_PLACEHOLDER_STATE_PLACEHOLDER) == 0)
                //{

                //}

                long usn = await WinFileSystemItem.GetUSNAsyncA(m_path);
                if (m_engine.Placeholders.TryGetItem(m_path, out var item))
                {
                    (bool IsSuccess, bool? IsMoved) = await item.IsMoved();
                    if (IsSuccess && !IsMoved.Value)
                    {
                        //Lock
                        //Update
                        num += await UpdateInternalAsync(usn, token);
                        //Unlock
                    }
                }
            }

            return num;
        }

        private async Task<long> UpdateInternalAsync(long usn, CancellationToken token = default)
        {
            long num = 0;
            if (token.IsCancellationRequested)
            {
                return num;
            }
            var fileInfo = FileHelper.GetFileInfo(m_path);
            FileStream fileStream = null;
            try
            {
                using (WinFileSystemItem item = WinFileSystemItem.OpenFile(m_path, FileMode.Open, FileShare.ReadWrite | FileShare.Delete))
                {
                    var hFile = item.SafeHandle;
                    if (hFile.IsInvalid)
                    {
                        return num;
                    }
                    if (fileInfo is FileInfo)
                    {
                        if (PlaceholderItem.GetPlaceholderStandardInfo(hFile).ModifiedDataSize > 0)
                        {
                            try
                            {
                                fileStream = ((FileInfo)fileInfo).Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                            }
                            catch (IOException)
                            {
                                return num;
                            }
                        }
                    }
                    fileInfo.Refresh();

                    IFileSystemItemMetadata metadata = FileHelper.Convert(fileInfo);
                    IFileSystemItem fileSystemItem = await GetFileSystemItemAsync();
                    if (fileSystemItem is IVirtualFile)
                    {
                        await ((IVirtualFile)fileSystemItem).WriteAsync(metadata, fileStream, token);
                    }
                    else
                    {
                        await ((IVirtualFolder)fileSystemItem).WriteAsync((IFolderMetadata)metadata, token);
                    }

                    (bool IsSuccess, long? USN) = await WinFileSystemItem.GetUSNAsync(m_path);
                    if (IsSuccess)
                    {
                        if (usn == USN.Value)
                        {
                            if (PlaceholderItem.SetInSyncState(hFile, true))
                            {
                                num++;
                            }
                        }
                    }
                }
            }
            finally
            {
                fileStream?.Close();
            }
            return num;
        }
    }
}
