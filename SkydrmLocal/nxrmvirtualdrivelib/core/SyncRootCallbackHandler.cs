using nxrmvirtualdrivelib.impl;
using nxrmvirtualdrivelib.placeholder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;

namespace nxrmvirtualdrivelib.core
{
    public class SyncRootCallbackHandler : IDisposable
    {
        private readonly IEngine m_virtualEngine;
        private readonly string m_userSyncRootPath;
        private ILogger m_logger;

        private readonly CF_CALLBACK_REGISTRATION[] m_callbackTable = new CF_CALLBACK_REGISTRATION[14];
        private static CF_CONNECTION_KEY m_connectionKey;
        private volatile bool m_isConnected;

        private CancellationToken CancellationToken = CancellationToken.None;

        public SyncRootCallbackHandler(IEngine engine, string syncRootPath)
        {
            this.m_virtualEngine = engine;
            this.m_userSyncRootPath = syncRootPath;
            this.m_logger = engine.Logger;
        }

        public void ConnectSyncRoot(CancellationToken cancellationToken)
        {
            if (m_isConnected)
            {
                return;
            }
            this.CancellationToken = cancellationToken;
            m_callbackTable[0] = new CF_CALLBACK_REGISTRATION
            {
                Callback = OnFetchData,
                Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_FETCH_DATA,
            };
            m_callbackTable[1] = new CF_CALLBACK_REGISTRATION()
            {
                Callback = OnValidateData,
                Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_VALIDATE_DATA,
            };
            m_callbackTable[2] = new CF_CALLBACK_REGISTRATION()
            {
                Callback = OnCancelFetchData,
                Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_CANCEL_FETCH_DATA,
            };
            m_callbackTable[3] = new CF_CALLBACK_REGISTRATION()
            {
                Callback = OnFetchPlaceHolders,
                Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_FETCH_PLACEHOLDERS,
            };
            m_callbackTable[4] = new CF_CALLBACK_REGISTRATION()
            {
                Callback = OnCancelFetchPlaceHolders,
                Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_CANCEL_FETCH_PLACEHOLDERS,
            };
            m_callbackTable[5] = new CF_CALLBACK_REGISTRATION()
            {
                Callback = OnOpenCompletion,
                Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_FILE_OPEN_COMPLETION,
            };
            m_callbackTable[6] = new CF_CALLBACK_REGISTRATION()
            {
                Callback = OnCloseCompletion,
                Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_FILE_CLOSE_COMPLETION,
            };
            m_callbackTable[7] = new CF_CALLBACK_REGISTRATION()
            {
                Callback = OnDehydrate,
                Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_DEHYDRATE,
            };
            m_callbackTable[8] = new CF_CALLBACK_REGISTRATION()
            {
                Callback = OnDehydrateCompletion,
                Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_DEHYDRATE_COMPLETION,
            };
            m_callbackTable[9] = new CF_CALLBACK_REGISTRATION()
            {
                Callback = OnDelete,
                Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_DELETE,
            };
            m_callbackTable[10] = new CF_CALLBACK_REGISTRATION()
            {
                Callback = OnDeleteCompletion,
                Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_DELETE_COMPLETION,
            };
            m_callbackTable[11] = new CF_CALLBACK_REGISTRATION()
            {
                Callback = OnMoveTo,
                Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_RENAME,
            };
            m_callbackTable[12] = new CF_CALLBACK_REGISTRATION()
            {
                Callback = OnMoveToCompletion,
                Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_NOTIFY_RENAME_COMPLETION,
            };
            m_callbackTable[13] = CF_CALLBACK_REGISTRATION.CF_CALLBACK_REGISTRATION_END;

            var result = CfConnectSyncRoot(m_userSyncRootPath, m_callbackTable, IntPtr.Zero,
                CF_CONNECT_FLAGS.CF_CONNECT_FLAG_REQUIRE_PROCESS_INFO | CF_CONNECT_FLAGS.CF_CONNECT_FLAG_REQUIRE_FULL_FILE_PATH,
                out m_connectionKey);

            m_isConnected = (result == HRESULT.S_OK);
        }

        public void DisconnectSyncRoot()
        {
            if (m_isConnected)
            {
                CfDisconnectSyncRoot(m_connectionKey);
                m_isConnected = false;
            }
        }

        /// <summary>
        /// Callback to satisfy an I/O request, or a placeholder hydration request.
        /// </summary>
        /// <param name="callbackInfo"></param>
        /// <param name="callbackParameters"></param>
        private void OnFetchData(in CF_CALLBACK_INFO callbackInfo, in CF_CALLBACK_PARAMETERS callbackParameters)
        {
            OnFetchDataAsync(callbackInfo, callbackParameters);
        }

        private async Task OnFetchDataAsync(CF_CALLBACK_INFO callbackInfo, CF_CALLBACK_PARAMETERS callbackParameters)
        {
            var userFsPath = GetUserFileSystemPath(callbackInfo);
            m_logger.Info(string.Format("[SyncRootCallbackHandler]: OnFetchDataAsync for user filesystem path {0}.", userFsPath));

            ITransferData transferData = new TransferData(m_virtualEngine, callbackInfo, callbackParameters);
            try
            {
                var placeholderId = GetPlaceHolderId(callbackInfo);
                if (placeholderId == null)
                {
                    return;
                }
                IFileSystemItem fsItem = await m_virtualEngine.GetFileSystemItemAsync(userFsPath, FileSystemItemType.File,
                    placeholderId.RemoteStorageItemId);
                if (fsItem == null)
                {
                    return;
                }

                var requiredOffset = callbackParameters.FetchData.RequiredFileOffset;
                var requiredLength = callbackParameters.FetchData.RequiredLength;
                var fileSize = callbackInfo.FileSize;
                using (var output = new FileOuputStreamEx(requiredOffset, requiredLength, transferData))
                {
                    await (fsItem as IVirtualFile).ReadAsync(output, requiredOffset, requiredLength);
                }
            }
            catch (Exception e)
            {
                m_logger.Error(string.Format("[SyncRootCallbackHandler]: OnFetchDataAsync failed with error {0}.", e));
                transferData?.ReportStatus(e.Message, 0u);
            }
        }

        /// <summary>
        /// Callback to validate placeholder data.
        /// </summary>
        /// <param name="callbackInfo"></param>
        /// <param name="callbackParameters"></param>
        private void OnValidateData(in CF_CALLBACK_INFO callbackInfo, in CF_CALLBACK_PARAMETERS callbackParameters)
        {
            try
            {
                OnValidateDataAsync(callbackInfo, callbackParameters);
            }
            catch (Exception e)
            {
                m_logger.Error(e);
            }
        }

        private async Task OnValidateDataAsync(CF_CALLBACK_INFO callbackInfo, CF_CALLBACK_PARAMETERS callbackParameters)
        {
            var userFsPath = GetUserFileSystemPath(callbackInfo);
            m_logger.Info(string.Format("[SyncRootCallbackHandler]: OnValidateDataAsync for user filesystem path {0}.", userFsPath));

            var placeholderId = GetPlaceHolderId(callbackInfo);
            if (placeholderId == null)
            {
                return;
            }
            IFileSystemItem fsItem = await m_virtualEngine.GetFileSystemItemAsync(userFsPath, FileSystemItemType.File,
                placeholderId.RemoteStorageItemId);
            if (fsItem == null)
            {
                return;
            }
            if (fsItem is IFileWindows)
            {
                var validateDataParams = callbackParameters.ValidateData;
                var offset = validateDataParams.RequiredFileOffset;
                var length = validateDataParams.RequiredLength;
                bool explicitHydration = validateDataParams.Flags == CF_CALLBACK_VALIDATE_DATA_FLAGS.CF_CALLBACK_VALIDATE_DATA_FLAG_EXPLICIT_HYDRATION;

                IValidateData validateData = new ValidateData(m_virtualEngine, callbackInfo);
                await (fsItem as IFileWindows).ValidateDataAsync(offset, length, explicitHydration, validateData);
            }
        }

        /// <summary>
        /// Callback to cancel an ongoing placeholder hydration.
        /// </summary>
        /// <param name="callbackInfo"></param>
        /// <param name="callbackParameters"></param>
        private void OnCancelFetchData(in CF_CALLBACK_INFO callbackInfo, in CF_CALLBACK_PARAMETERS callbackParameters)
        {
            OnCancelFetchDataAsync(callbackInfo, callbackParameters);
        }

        private async Task OnCancelFetchDataAsync(CF_CALLBACK_INFO callbackInfo, CF_CALLBACK_PARAMETERS callbackParameters)
        {
            var userFsPath = GetUserFileSystemPath(callbackInfo);
            m_logger.Info(string.Format("[SyncRootCallbackHandler]: OnCancelFetchDataAsync for user filesystem path {0}.", userFsPath));
        }

        /// <summary>
        /// Callback to request information about the contents of placeholder files.
        /// </summary>
        /// <param name="callbackInfo"></param>
        /// <param name="callbackParameters"></param>
        private void OnFetchPlaceHolders(in CF_CALLBACK_INFO callbackInfo, in CF_CALLBACK_PARAMETERS callbackParameters)
        {
            OnFetchPlaceHoldersAsync(callbackInfo, callbackParameters);
        }

        private async Task OnFetchPlaceHoldersAsync(CF_CALLBACK_INFO callbackInfo, CF_CALLBACK_PARAMETERS callbackParameters)
        {
            var userFsPath = GetUserFileSystemPath(callbackInfo);
            m_logger.Info(string.Format("[SyncRootCallbackHandler]: OnFetchPlaceHoldersAsync for path {0}.", userFsPath));

            IListFile listFile = new ListFile(m_virtualEngine, callbackInfo, userFsPath);
            try
            {
                var placeholderId = GetPlaceHolderId(callbackInfo);
                if (placeholderId == null)
                {
                    return;
                }
                IFileSystemItem fsItem = await m_virtualEngine.GetFileSystemItemAsync(userFsPath, FileSystemItemType.Folder,
                    placeholderId.RemoteStorageItemId);
                if (fsItem == null)
                {
                    return;
                }

                string pattern = callbackParameters.FetchPlaceholders.Pattern;
                var children = await (fsItem as IVirtualFolder).GetChildrenAsync(pattern, CancellationToken);

                await listFile.ListAsync(children, children == null ? 0 : children.LongLength, !m_virtualEngine.IsRPMDir(userFsPath));
            }
            catch (Exception e)
            {
                m_logger.Error(e);
                //result?.ReportStatus(e.Message, 0u);
            }
        }

        private string GetUserFileSystemPath(CF_CALLBACK_INFO callbackInfo)
        {
            return GetPath(callbackInfo.VolumeDosName, callbackInfo.NormalizedPath);
        }

        private PlaceholderItemIdentity GetPlaceHolderId(CF_CALLBACK_INFO callbackInfo)
        {
            byte[] fileIdentityData = GetFileIdentityData(callbackInfo);
            if (fileIdentityData != null)
            {
                return PlaceholderItemIdentity.MarshalFrom(fileIdentityData, true);
            }
            return new PlaceholderItemIdentity(FileSystemItemType.Folder, null, null, true);
        }

        private byte[] GetFileIdentityData(CF_CALLBACK_INFO callbackInfo)
        {
            var len = callbackInfo.FileIdentityLength;
            var sourceFileIdPtr = callbackInfo.FileIdentity;

            byte[] dest = null;
            if (sourceFileIdPtr != IntPtr.Zero && callbackInfo.FileIdentityLength != 0)
            {
                dest = new byte[len];
                Marshal.Copy(sourceFileIdPtr, dest, 0, dest.Length);
            }
            return dest;
        }

        private string GetPath(string volume, string childPath)
        {
            return Path.GetFullPath(Path.Combine(volume.TrimStart(new char[1] { Path.DirectorySeparatorChar }), childPath));
        }

        /// <summary>
        /// Callback to cancel a request for the contents of placeholder files.
        /// </summary>
        /// <param name="callbackInfo"></param>
        /// <param name="callbackParameters"></param>
        private void OnCancelFetchPlaceHolders(in CF_CALLBACK_INFO callbackInfo, in CF_CALLBACK_PARAMETERS callbackParameters)
        {
            var usrFsPath = GetUserFileSystemPath(callbackInfo);
            m_logger.Info(string.Format("[SyncRootCallbackHandler]: OnCancelFetchPlaceHolders for path {0}.", usrFsPath));
        }

        /// <summary>
        /// Callback to inform the sync provider that a placeholder under one of its sync
        /// roots has been successfully opened for read/write/delete access
        /// </summary>
        /// <param name="callbackInfo"></param>
        /// <param name="callbackParameters"></param>
        private void OnOpenCompletion(in CF_CALLBACK_INFO callbackInfo, in CF_CALLBACK_PARAMETERS callbackParameters)
        {
            try
            {
                OnOpenCompletionAsync(callbackInfo, callbackParameters);
            }
            catch (Exception e)
            {
                m_logger.Error(e);
            }
        }

        private async Task OnOpenCompletionAsync(CF_CALLBACK_INFO callbackInfo, CF_CALLBACK_PARAMETERS callbackParameters)
        {
            var userFsPath = GetUserFileSystemPath(callbackInfo);
            m_logger.Info(string.Format("[SyncRootCallbackHandler]: OnOpenCompletionAsync for path {0}.", userFsPath));


            var placeholderId = GetPlaceHolderId(callbackInfo);
            if (placeholderId == null)
            {
                return;
            }
            PlaceholderItemHandler itemHandler = new PlaceholderItemHandler(userFsPath, placeholderId, (VirtualEngine)m_virtualEngine);
            IFileSystemItem fsItem = await itemHandler.GetFileSystemItemAsync();
            if (fsItem == null)
            {
                return;
            }
            if (fsItem is IFileWindows)
            {
                await (fsItem as IFileWindows).OpenCompletionAsync(CancellationToken);
            }

            await itemHandler.OnOpenCompletionAsync(CancellationToken);
        }

        /// <summary>
        /// Callback to inform the sync provider that a placeholder under one of its sync
        /// roots that has been previously opened for read/write/delete access is now closed.
        /// </summary>
        /// <param name="callbackInfo"></param>
        /// <param name="callbackParameters"></param>
        private void OnCloseCompletion(in CF_CALLBACK_INFO callbackInfo, in CF_CALLBACK_PARAMETERS callbackParameters)
        {
            try
            {
                OnCloseCompletionAsync(callbackInfo, callbackParameters);
            }
            catch (Exception e)
            {
                m_logger.Error(e);
            }
        }

        private async Task OnCloseCompletionAsync(CF_CALLBACK_INFO callbackInfo, CF_CALLBACK_PARAMETERS callbackParameters)
        {
            var userFsPath = GetUserFileSystemPath(callbackInfo);
            m_logger.Info(string.Format("[SyncRootCallbackHandler]: OnCloseCompletionAsync for path {0}.", userFsPath));

            var placeholderId = GetPlaceHolderId(callbackInfo);
            if (placeholderId == null)
            {
                return;
            }
            PlaceholderItemHandler itemHandler = new PlaceholderItemHandler(userFsPath, placeholderId, (VirtualEngine)m_virtualEngine);
            IFileSystemItem fsItem = await itemHandler.GetFileSystemItemAsync();
            if (fsItem == null)
            {
                return;
            }
            if (fsItem is IFileWindows)
            {
                await (fsItem as IFileWindows).CloseCompletionAsync(CancellationToken);
            }

            if (fsItem == null || (callbackParameters.CloseCompletion.Flags
                & CF_CALLBACK_CLOSE_COMPLETION_FLAGS.CF_CALLBACK_CLOSE_COMPLETION_FLAG_DELETED) != 0)
            {
                return;
            }

            await itemHandler.OnCloseCompletionAsync(CancellationToken);
        }

        /// <summary>
        /// Callback to inform the sync provider that a placeholder under one of its sync
        /// roots is about to be dehydrated.
        /// </summary>
        /// <param name="callbackInfo"></param>
        /// <param name="callbackParameters"></param>
        private void OnDehydrate(in CF_CALLBACK_INFO callbackInfo, in CF_CALLBACK_PARAMETERS callbackParameters)
        {
            var userFsPath = GetUserFileSystemPath(callbackInfo);
            m_logger.Info(string.Format("[SyncRootCallbackHandler]: OnDehydrate for path {0}.", userFsPath));
        }

        /// <summary>
        /// Callback to inform the sync provider that a placeholder under one of its sync
        /// roots has been successfully dehydrated.
        /// </summary>
        /// <param name="callbackInfo"></param>
        /// <param name="callbackParameters"></param>
        private void OnDehydrateCompletion(in CF_CALLBACK_INFO callbackInfo, in CF_CALLBACK_PARAMETERS callbackParameters)
        {
            var userFsPath = GetUserFileSystemPath(callbackInfo);
            m_logger.Info(string.Format("[SyncRootCallbackHandler]: OnDehydrateCompletion for path {0}.", userFsPath));
        }

        /// <summary>
        /// Callback to inform the sync provider that a placeholder under one of its sync
        /// roots is about to be deleted.
        /// </summary>
        /// <param name="callbackInfo"></param>
        /// <param name="callbackParameters"></param>
        private void OnDelete(in CF_CALLBACK_INFO callbackInfo, in CF_CALLBACK_PARAMETERS callbackParameters)
        {
            OnDeleteAsync(callbackInfo, callbackParameters);
        }

        private async Task OnDeleteAsync(CF_CALLBACK_INFO callbackInfo, CF_CALLBACK_PARAMETERS callbackParameters)
        {
            IDeleteFile deleteFile = new DeleteFile(m_virtualEngine, callbackInfo);

            try
            {
                var phId = GetPlaceHolderId(callbackInfo);

                var usrFsPath = GetUserFileSystemPath(callbackInfo);
                m_logger.Info(string.Format("[SyncRootCallbackHandler]: OnDeleteAsync for file {0}.", usrFsPath));
                PlaceholderItemHandler handler = new PlaceholderItemHandler(usrFsPath, phId, (VirtualEngine)m_virtualEngine);

                await handler.DeleteAsync(deleteFile);
                deleteFile?.OnConfirm();
            }
            catch (Exception e)
            {
                m_logger.Error(e);
                deleteFile?.OnCancel();
            }
        }

        /// <summary>
        /// Callback to inform the sync provider that a placeholder under one of its sync
        /// roots has been successfully deleted.
        /// </summary>
        /// <param name="callbackInfo"></param>
        /// <param name="callbackParameters"></param>
        private void OnDeleteCompletion(in CF_CALLBACK_INFO callbackInfo, in CF_CALLBACK_PARAMETERS callbackParameters)
        {
            OnDeleteCompletionAsync(callbackInfo, callbackParameters);
        }

        private async Task OnDeleteCompletionAsync(CF_CALLBACK_INFO callbackInfo, CF_CALLBACK_PARAMETERS callbackParameters)
        {
            try
            {
                var phId = GetPlaceHolderId(callbackInfo);
                var usrFsPath = GetUserFileSystemPath(callbackInfo);
                PlaceholderItemHandler handler = new PlaceholderItemHandler(usrFsPath, phId, (VirtualEngine)m_virtualEngine);
                m_logger.Info(string.Format("[SyncRootCallbackHandler]: OnDeleteCompletionAsync for file {0}.", usrFsPath));
                await handler.DeleteCompletionAsync((ulong?)callbackInfo.FileId, phId.ParentFileId, phId.FileName);
            }
            catch (Exception e)
            {
                m_logger.Error(e);
            }
        }

        /// <summary>
        /// Callback to inform the sync provider that a placeholder under one of its sync
        /// roots is about to be renamed or moved.
        /// </summary>
        /// <param name="callbackInfo"></param>
        /// <param name="callbackParameters"></param>
        private void OnMoveTo(in CF_CALLBACK_INFO callbackInfo, in CF_CALLBACK_PARAMETERS callbackParameters)
        {
            OnMoveToAsync(callbackInfo, callbackParameters);
        }

        private async Task OnMoveToAsync(CF_CALLBACK_INFO callbackInfo, CF_CALLBACK_PARAMETERS callbackParameters)
        {
            var sourcePath = GetUserFileSystemPath(callbackInfo);
            var destPath = GetPath(callbackInfo.VolumeDosName, callbackParameters.Rename.TargetPath);
            var destDirectoryName = Path.GetDirectoryName(destPath.TrimEnd(new char[] { Path.DirectorySeparatorChar }));

            byte[] destFolderRemoteStorageId = null;
            var placeholderId = GetPlaceHolderId(callbackInfo);
            if (placeholderId == null)
            {
                return;
            }
            PlaceholderItemHandler itemHandler = new PlaceholderItemHandler(sourcePath, placeholderId, (VirtualEngine)m_virtualEngine);
            if (m_virtualEngine.IsThisDrivePath(destDirectoryName))
            {
                PlaceholderFolder folder = new PlaceholderFolder(destDirectoryName);
                destFolderRemoteStorageId = folder.GetRemoteStorageItemId();
            }

            IMoveToFile moveToFile = new MoveToFile(m_virtualEngine, callbackInfo);

            var sourceFileId = (ulong)callbackInfo.FileId;

            bool moveTo = await itemHandler.MoveToAsync(destPath, destFolderRemoteStorageId, sourceFileId, placeholderId.ParentFileId,
                placeholderId.FileName);

            if (moveTo)
            {
                moveToFile?.OnConfirm();
            }
            else
            {
                moveToFile?.OnCancel();
            }
        }

        /// <summary>
        /// Callback to inform the sync provider that a placeholder under one of its sync
        /// roots has been successfully renamed or moved.
        /// </summary>
        /// <param name="callbackInfo"></param>
        /// <param name="callbackParameters"></param>
        private void OnMoveToCompletion(in CF_CALLBACK_INFO callbackInfo, in CF_CALLBACK_PARAMETERS callbackParameters)
        {
            OnMoveToCompletionAsync(callbackInfo, callbackParameters);
        }

        private async Task OnMoveToCompletionAsync(CF_CALLBACK_INFO callbackInfo, CF_CALLBACK_PARAMETERS callbackParameters)
        {
            var sourcePath = GetPath(callbackInfo.VolumeDosName, callbackParameters.Rename.TargetPath);
            var destPath = GetUserFileSystemPath(callbackInfo);
            var destDirectoryName = Path.GetDirectoryName(destPath.TrimEnd(new char[] { Path.DirectorySeparatorChar }));

            byte[] destFolderRemoteStorageId = null;
            var placeholderId = GetPlaceHolderId(callbackInfo);
            if (placeholderId == null)
            {
                return;
            }
            if (m_virtualEngine.IsThisDrivePath(destDirectoryName))
            {
                PlaceholderFolder folder = new PlaceholderFolder(destDirectoryName);
                destFolderRemoteStorageId = folder.GetRemoteStorageItemId();
            }

            PlaceholderItemHandler itemHandler = new PlaceholderItemHandler(sourcePath, placeholderId, (VirtualEngine)m_virtualEngine);
            await itemHandler.MoveToCompletionAsync(destPath, destFolderRemoteStorageId);
        }

        public void Dispose()
        {
            DisconnectSyncRoot();

            GC.SuppressFinalize(this);
        }
    }
}
