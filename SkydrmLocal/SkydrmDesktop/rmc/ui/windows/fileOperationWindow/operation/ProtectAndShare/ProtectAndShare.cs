using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.model;
using SkydrmDesktop.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui.utils;

namespace SkydrmDesktop.rmc.ui.windows.fileOperationWindow.operation
{
    class ProtectAndShare : IProtectAndShare
    {
        private readonly SkydrmApp app = SkydrmApp.Singleton;

        private OperateFileInfo fileInfo;
        private CurrentSelectedSavePath currentSelectedSavePath = new CurrentSelectedSavePath(DataTypeConvertHelper.MY_VAULT, "/", "SkyDRM://" + DataTypeConvertHelper.MY_SPACE);

        public ProtectAndShare(OperateFileInfo info)
        {
            this.fileInfo = info;
        }

        public FileAction FileAction => FileAction.Share;

        public OperateFileInfo FileInfo { get => fileInfo; set => fileInfo = value; }

        public CurrentSelectedSavePath CurrentSelectedSavePath { get => currentSelectedSavePath; }

        public List<INxlFile> ProtectAndShareFile(List<FileRights> rights, WaterMarkInfo waterMark, Expiration expiration, List<string> emails, string message)
        {
            // Reset user-selected actions
            app.User.ApplyAllSelectedOption = false;
            app.User.SelectedOption = 0;

            List<INxlFile> createdNxlFiles = new List<INxlFile>();

            if (!rights.Contains(FileRights.RIGHT_WATERMARK))
            {
                waterMark.text = "";
            }

            // protect files
            List<string> nxlFileName = new List<string>();
            Dictionary<string, string> failedFileName = new Dictionary<string, string>();
            INxlFile doc = null;

            for (int i = 0; i < FileInfo.FilePath.Length; i++)
            {
                doc = MyVaultProtectAndShareFile(FileInfo.FilePath[i], rights, waterMark,
                    expiration, emails, message, out string errorMsg);
                if (doc != null)
                {
                    nxlFileName.Add(doc.Name);
                    createdNxlFiles.Add(doc);
                }
                else
                {
                    if (app.User.SelectedOption != 3)
                    {
                        failedFileName.Add(FileInfo.FileName[i], errorMsg);
                    }
                }
            }

            // update fileName to NxlFileName
            if (nxlFileName.Count > 0)
            {
                FileInfo.FileName = nxlFileName.ToArray();
            }

            // update failed fileName
            FileInfo.FailedFileName = failedFileName;

            return createdNxlFiles;
        }
        private INxlFile MyVaultProtectAndShareFile(string filePath, List<FileRights> fileRights, 
            WaterMarkInfo waterMark, Expiration expiration, List<string>emails, string message, out string errorMsg)
        {
            errorMsg = string.Empty;
            try
            {
                UserSelectTags userSelectTags = new UserSelectTags();
                var localFile = app.MyVault.AddLocalAdded(filePath, fileRights,
                                                waterMark, expiration, userSelectTags, emails, message);

                PendingUploadFile doc = new PendingUploadFile(localFile);

                return doc;
            }
            catch (Exception e)
            {
                errorMsg = e.Message;
                // Notify serviceManger
                app.MessageNotify.NotifyMsg(Alphaleonis.Win32.Filesystem.Path.GetFileName(filePath),
                    e.Message,
                    featureProvider.MessageNotify.EnumMsgNotifyType.LogMsg,
                    featureProvider.MessageNotify.MsgNotifyOperation.PROTECT,
                    featureProvider.MessageNotify.EnumMsgNotifyResult.Failed,
                    featureProvider.MessageNotify.EnumMsgNotifyIcon.Unknown);
                return null;
            }
        }

    }
}
