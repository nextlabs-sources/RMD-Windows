using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.model;
using SkydrmDesktop.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.search;
using SkydrmLocal.rmc.ui.utils;

namespace SkydrmDesktop.rmc.ui.windows.fileOperationWindow.operation.UpdateRecipient
{
    class UpdateRecipient : IUpdateRecipients
    {
        private readonly SkydrmApp app = SkydrmApp.Singleton;

        private NxlFileFingerPrint fingerPrint;

        private OperateFileInfo fileInfo;
        private CurrentSelectedSavePath currentSelectedSavePath = new CurrentSelectedSavePath(DataTypeConvertHelper.MY_VAULT, "/", DataTypeConvertHelper.MY_SPACE);

        public UpdateRecipient(NxlFileFingerPrint fingerP, OperateFileInfo fileInfoP)
        {
            fingerPrint = fingerP;
            fileInfo = fileInfoP;
        }

        public FileAction FileAction => FileAction.UpdateRecipients;

        public OperateFileInfo FileInfo { get => fileInfo; set => throw new NotImplementedException(); }

        public CurrentSelectedSavePath CurrentSelectedSavePath => currentSelectedSavePath;

        public NxlFileType NxlType => fingerPrint.isByAdHoc ? NxlFileType.Adhoc : NxlFileType.CentralPolicy;

        public List<FileRights> NxlRights
        {
            get
            {
                List<FileRights> fileRights = fingerPrint.rights.ToList();
                if (!string.IsNullOrWhiteSpace(fingerPrint.adhocWatermark) && !fileRights.Contains(FileRights.RIGHT_WATERMARK))
                {
                    fileRights.Add(FileRights.RIGHT_WATERMARK);
                }
                return fileRights;
            }
        }

        public WaterMarkInfo NxlAdhocWaterMark => new WaterMarkInfo() { text = fingerPrint.adhocWatermark, fontName = "", fontColor = "" };

        public Expiration NxlExpiration => fingerPrint.expiration;

        public bool IsOwner => fingerPrint.isOwner;

        public bool IsRevoked(out List<string> sharedEmail)
        {
            sharedEmail = new List<string>();
            if (fingerPrint.isFromMyVault)
            {
                string pathId = "";
                ISearchFileInMyVault searchFileInMyVault = new SearchMyVaultFileByDuid();
                var results = searchFileInMyVault.SearchInRmsFiles(fingerPrint.duid);
                if (results != null)
                {
                    pathId = results.Path_Id;
                    if (results.Is_Revoked)
                    {
                        sharedEmail = StringHelper.ConvertString2List(results.Shared_With_List);
                        return true;
                    }
                }

                //Sanity check.
                if (string.IsNullOrEmpty(pathId))
                {
                    return false;
                }
                //we will get its metadata to judge whether this file revoked or not.
                try
                {
                    var md = app.Rmsdk.User.GetMyVaultFileMetaData(fingerPrint.localPath, pathId);
                    if (md.isRevoked)
                    {
                        sharedEmail = md.recipents;
                        return true;
                    }
                }
                catch (Exception e)
                {
                    app.ShowBalloonTip(e.ToString(), false);
                }
            }
            return false;
        }

        public bool UpdateRecipients(List<string> addEmails, List<string> removeEmails, string message)
        {
            try
            {
                var nxlLocalPath = fingerPrint.localPath;

                ISearchFileInMyVault SearchFileInMyVault = new SearchMyVaultFileByDuid();
                var myVaultFile = SearchFileInMyVault.SearchInRmsFiles(fingerPrint.duid);
                if (myVaultFile != null)
                {
                    myVaultFile?.ShareFile(nxlLocalPath, addEmails.ToArray(), removeEmails.ToArray(), message);

                    // Update the file 'share with' in MainWindow
                    app.MainWin.viewModel.UpdateFileShareWith(myVaultFile.Duid);
                    return true;
                }

                ISearchFileInSharedWithMe SearchFileInSharedWithMe = new SearchSharedWithMeFileByDuid();
                var sharedWithMeFile = SearchFileInSharedWithMe.Search(fingerPrint.duid);
                if (sharedWithMeFile != null)
                {
                    sharedWithMeFile?.ShareFile(nxlLocalPath, addEmails.ToArray(), removeEmails.ToArray(), message);
                    return true;
                }

                // Can't search the file(means the file does not exist on rms, will forbid to perform share operation)
                // Fix bug 62026
                app.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("UpdateRecipiOperation_FileNotExistServer"), false,
                   FileInfo.FileName[0]);

            }
            catch (RmRestApiException e)
            {
                if (e.ErrorCode == 4001) // File has been revoked.
                {
                    app.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("UpdateRecipiOperation_ShareFailedHasRevoked"), false,
                        FileInfo.FileName[0]);
                }
                else
                {
                    app.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("UpdateRecipiOperation_ShareFailed"), false,
                        FileInfo.FileName[0]);
                }
            }
            catch (Exception msg)
            {
                // to do, will popup prompt info
                app.Log.Info("app.Session.User.UpdateRecipients is failed", msg);
                app.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("UpdateRecipiOperation_ShareFailed"), false,
                    FileInfo.FileName[0]);
            }

            return false;
        }
    }
}
