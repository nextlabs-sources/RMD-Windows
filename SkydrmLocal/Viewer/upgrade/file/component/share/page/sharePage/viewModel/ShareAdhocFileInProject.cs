using Newtonsoft.Json;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.upgrade.application;
using Viewer.upgrade.communication.message;
using Viewer.upgrade.communication.namedPipe.client;
using Viewer.upgrade.file.utils;
using Viewer.upgrade.utils;
using static Viewer.upgrade.communication.namedPipe.client.NamedPipeClient;

namespace Viewer.upgrade.file.component.share.page.sharePage.viewModel
{
    public class ShareAdhocFileInProject : IShare
    {
        private ViewerApp mViewerInstance;
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
            mViewerInstance = (ViewerApp)ViewerApp.Current;
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

                decryptFilePath = NxlFileUtils.Decrypt(mNxlFileFingerPrint.localPath,"",true);

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

                MessageNotify.NotifyMsg(mViewerInstance.SdkSession, Path.GetFileName(nxlFilePath), mViewerInstance.FindResource("Protected_Successfully").ToString(), EnumMsgNotifyType.LogMsg, MsgNotifyOperation.PROTECT, EnumMsgNotifyResult.Succeed, EnumMsgNotifyIcon.Online);

                string SharedEmails = string.Empty;

                for (int i = 0; i < mSharedEmailLists.Count; i++)
                {
                    SharedEmails += mSharedEmailLists[i];
                    if (i != mSharedEmailLists.Count - 1)
                    {
                        SharedEmails += ",";
                    }
                }

                mViewerInstance.SdkSession.User.UploadMyVaultFile(nxlFilePath, Path.GetFileName(decryptFilePath), SharedEmails, mComment);

                shareResult.Code = 0;
                shareResult.Result = Path.GetFileName(nxlFilePath);
                e.Result = shareResult;

                MessageNotify.NotifyMsg(mViewerInstance.SdkSession, Path.GetFileName(nxlFilePath), mViewerInstance.FindResource("Uploaded_Successfully").ToString(), EnumMsgNotifyType.LogMsg, MsgNotifyOperation.UPLOAD, EnumMsgNotifyResult.Succeed, EnumMsgNotifyIcon.Online);

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
                    RPMDeleteDirectory(Path.GetDirectoryName(decryptFilePath));
                }
            }
            catch (Exception eex)
            {

            }
        }


        private void RPMDeleteDirectory(string directoryPath)
        {
            try
            {
                string[] allFilePath = Directory.GetFiles(directoryPath);

                foreach (string filePath in allFilePath)
                {
                    mViewerInstance.SdkSession.RPM_DeleteFile(filePath);
                }

                string[] allSubdirectory = Directory.GetDirectories(directoryPath);

                foreach (string subDirectoryPath in allSubdirectory)
                {
                    RPMDeleteDirectory(subDirectoryPath);
                }

                mViewerInstance.SdkSession.RPM_DeleteFolder(directoryPath);
            }
            catch (Exception ex)
            {
                mViewerInstance.Log.Error(ex.Message);
            }
        }

        public string ProtectAchocFileToMyVault(string path, List<FileRights> rights, WaterMarkInfo waterMark, Expiration expiration, UserSelectTags tags)
        {
            string result = string.Empty;
            try
            {
                // tell api to convert to nxl by protect
                mViewerInstance.Log.Info("try to protect file to myVault: " + path);
                result = mViewerInstance.SdkSession.User.ProtectFile(path, rights, waterMark, expiration, tags);
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
