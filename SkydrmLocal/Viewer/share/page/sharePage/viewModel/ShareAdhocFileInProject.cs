using Newtonsoft.Json;
using SkydrmLocal.rmc.drive;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.utils;
using static Viewer.utils.IPC.NamedPipe;
using static Viewer.utils.IPC.NamedPipe.NamedPipeClient;

namespace Viewer.share
{
    public class ShareAdhocFileInProject : IShare
    {
        private ViewerApp mViewerInstance = (ViewerApp)System.Windows.Application.Current;
        private NxlFileFingerPrint mNxlFileFingerPrint;
        private BackgroundWorker mBackground = new BackgroundWorker();
        private List<string> mSharedEmailLists = new List<string>();
        private string mComment;

        public bool IsBusy
        {
            get { return mBackground.IsBusy; }
        }

        public ShareAdhocFileInProject(RunWorkerCompletedEventHandler RunWorkerCompleted)
        {
            mBackground.WorkerReportsProgress = true;
            mBackground.WorkerSupportsCancellation = true;
            mBackground.DoWork += DoWorkEventHandler;
            mBackground.RunWorkerCompleted += RunWorkerCompleted;
        }

        public void Share(NxlFileFingerPrint nxlFileFingerPrint, List<string> sharedEmailLists,string comment)
        {
            this.mNxlFileFingerPrint = nxlFileFingerPrint;
            this.mSharedEmailLists = sharedEmailLists;
            this.mComment = comment;
            mBackground.RunWorkerAsync();
        }

        private void DoWorkEventHandler(object sender, DoWorkEventArgs e)
        {
            string decryptFilePath = string.Empty;

            ShareResult shareResult = new ShareResult();

            try
            {
                decryptFilePath = RightsManagementService.GenerateDecryptFilePath(
                                 mViewerInstance.Appconfig.RPM_FolderPath,
                                 mNxlFileFingerPrint.localPath,
                                 false
                                 );


                RightsManagementService.DecryptNXLFile(mViewerInstance.User,
                                                   mViewerInstance.Log,
                                                   mNxlFileFingerPrint.localPath,
                                                   decryptFilePath
                                                   );

                UserSelectTags selectTags = new UserSelectTags();

                List<FileRights> rights = new List<FileRights>(mNxlFileFingerPrint.rights);

                WaterMarkInfo waterMark = new WaterMarkInfo();
                // ---- Note: below is test value, avoid crash in wrappersdk.
                waterMark.text = "";
                waterMark.fontColor = "";
                waterMark.fontSize = 10;
                waterMark.fontName = "";
                waterMark.text = mNxlFileFingerPrint.adhocWatermark;

                string nxlFilePath = ProtectAchocFileToMyVault(decryptFilePath, rights, waterMark, mNxlFileFingerPrint.expiration, selectTags);

                MessageNotify.NotifyMsg(Path.GetFileName(nxlFilePath), CultureStringInfo.Protected_Successfully, EnumMsgNotifyType.LogMsg, MsgNotifyOperation.PROTECT, EnumMsgNotifyResult.Succeed, EnumMsgNotifyIcon.Online);

                string SharedEmails = string.Empty;

                for (int i = 0; i < mSharedEmailLists.Count; i++)
                {
                    SharedEmails += mSharedEmailLists[i];
                    if (i != mSharedEmailLists.Count - 1)
                    {
                        SharedEmails += ",";
                    }
                }

                mViewerInstance.Session.User.UploadMyVaultFile(nxlFilePath, decryptFilePath, SharedEmails, mComment);

                shareResult.Code = 0;
                shareResult.Result = Path.GetFileName(nxlFilePath);
                e.Result = shareResult;

                MessageNotify.NotifyMsg(Path.GetFileName(nxlFilePath), CultureStringInfo.Uploaded_Successfully, EnumMsgNotifyType.LogMsg, MsgNotifyOperation.UPLOAD, EnumMsgNotifyResult.Succeed, EnumMsgNotifyIcon.Online);

                Bundle<string> bundle = new Bundle<string>()
                {
                    Intent = Intent.LeaveCopy,
                    obj = nxlFilePath
                };
                string json = JsonConvert.SerializeObject(bundle);
                NamedPipeClient.Start(json);
            }
            catch (Exception ex)
            {
                shareResult.Code = 1;
                shareResult.Exception = ex;
                e.Result = shareResult;
            }
            try
            {
                if (!string.IsNullOrEmpty(decryptFilePath) && File.Exists(decryptFilePath))
                {
                    RightsManagementService.RPMDeleteDirectory(mViewerInstance.Session, mViewerInstance.Log, mViewerInstance.Appconfig.RPM_FolderPath, Path.GetDirectoryName(decryptFilePath));
                }
            }
            catch (Exception eex)
            {

            }
        }

        public string ProtectAchocFileToMyVault(string path, List<FileRights> rights, WaterMarkInfo waterMark, Expiration expiration, UserSelectTags tags)
        {
            string result = string.Empty;
            try
            {
                // tell api to convert to nxl by protect
                mViewerInstance.Log.Info("try to protect file to myVault: " + path);
                result = mViewerInstance.Session.User.ProtectFile(path, rights, waterMark, expiration, tags);
            }
            catch (Exception e)
            {
                mViewerInstance.Log.Error("Error occured when tring to protect file in myault" + e.Message, e);
                throw e;
            }

            return result;
        }
    }
}
