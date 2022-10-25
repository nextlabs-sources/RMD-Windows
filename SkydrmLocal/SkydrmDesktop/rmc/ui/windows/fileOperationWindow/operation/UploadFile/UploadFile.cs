using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.featureProvider.MessageNotify;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.model;
using SkydrmDesktop.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.fileSystem;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui.utils;

namespace SkydrmDesktop.rmc.ui.windows.fileOperationWindow.operation
{
    class UploadFile : IUpload
    {
        private readonly SkydrmApp app = SkydrmApp.Singleton;

        private OperateFileInfo fileInfo;
        private CurrentSelectedSavePath currentSelectedSavePath;
        public UploadFile(OperateFileInfo fileInfo, CurrentSelectedSavePath selectedSavePath)
        {
            this.fileInfo = fileInfo;
            this.currentSelectedSavePath = selectedSavePath;
        }

        public FileAction FileAction => FileAction.UploadFile;

        public OperateFileInfo FileInfo { get => fileInfo; set => throw new NotImplementedException(); }

        public CurrentSelectedSavePath CurrentSelectedSavePath { get => currentSelectedSavePath; }

        public List<INxlFile> ProtectFile(List<FileRights> rights, string waterMarkTxt, Expiration expiration)
        {
            // Reset user-selected actions
            app.User.ApplyAllSelectedOption = false;
            app.User.SelectedOption = 0;

            List<INxlFile> createdNxlFiles = new List<INxlFile>();

            // init WarterMarkInfo
            WaterMarkInfo waterMarkInfo = new WaterMarkInfo()
            {
                fontColor = "",
                fontName = "",
                text = "",
                fontSize = 0,
                repeat = 0,
                rotation = 0,
                transparency = 0
            };

            if (rights.Contains(FileRights.RIGHT_WATERMARK))
            {
                waterMarkInfo.text = waterMarkTxt;
            }

            // protect files
            List<string> nxlFileName = new List<string>();
            Dictionary<string,string> failedFileName = new Dictionary<string,string>();
            INxlFile doc = null;
            for (int i = 0; i < FileInfo.FilePath.Length; i++)
            {
                doc = ProtectFileHelper.MyVaultAddLocalFile(FileInfo.FilePath[i], rights, waterMarkInfo, expiration, out string msg);

                if (doc != null)
                {
                    nxlFileName.Add(doc.Name);
                    createdNxlFiles.Add(doc);
                }
                else
                {
                    if (app.User.SelectedOption != 3)
                    {
                        failedFileName.Add(FileInfo.FileName[i], msg);
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

        public List<INxlFile> AddUploadFile()
        {
            // Reset user-selected actions
            app.User.ApplyAllSelectedOption = false;
            app.User.SelectedOption = 0;

            List<INxlFile> createdNxlFiles = new List<INxlFile>();

            List<string> nxlFileName = new List<string>();
            Dictionary<string, string> failedFileName = new Dictionary<string, string>();
            INxlFile doc = null;
            for (int i = 0; i < FileInfo.FilePath.Length; i++)
            {
                doc = MyDriveAddLocalFile(FileInfo.FilePath[i], CurrentSelectedSavePath.DestPathId, out string msg);

                if (doc != null)
                {
                    nxlFileName.Add(doc.Name);
                    createdNxlFiles.Add(doc);
                }
                else
                {
                    if (app.User.SelectedOption != 3)
                    {
                        failedFileName.Add(FileInfo.FileName[i], msg);
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
        private INxlFile MyDriveAddLocalFile(string filePath, string parentFolder, out string msg)
        {
            msg = string.Empty;
            try
            {
                var localFile = app.MyDrive.AddLocalAdded(parentFolder, filePath);

                PendingUploadFile doc = new PendingUploadFile(localFile);

                return doc;
            }
            catch (Exception e)
            {
                msg = e.Message;
                // Notify serviceManger
                app.MessageNotify.NotifyMsg(Alphaleonis.Win32.Filesystem.Path.GetFileName(filePath),
                    e.Message,
                    EnumMsgNotifyType.LogMsg,
                    MsgNotifyOperation.PROTECT,
                    EnumMsgNotifyResult.Failed,
                    EnumMsgNotifyIcon.Unknown);
                return null;
            }
        }

    }
}
