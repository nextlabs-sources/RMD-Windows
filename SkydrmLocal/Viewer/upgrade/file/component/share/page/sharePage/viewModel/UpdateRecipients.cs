using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Viewer.upgrade.application;
using Viewer.upgrade.database;
using Viewer.upgrade.utils;

namespace Viewer.upgrade.file.component.share.page.sharePage.viewModel
{
    public class UpdateRecipients : IShare
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

        public UpdateRecipients(RunWorkerCompletedEventHandler RunWorkerCompleted)
        {
            mBackground.WorkerReportsProgress = true;
            mBackground.WorkerSupportsCancellation = true;
            mBackground.DoWork += DoWorkEventHandler;
            mBackground.RunWorkerCompleted += RunWorkerCompleted;
        }

        public void Share(NxlFileFingerPrint nxlFileFingerPrint, List<string> sharedEmailLists, string comment)
        {
            this.mViewerInstance = (ViewerApp)ViewerApp.Current;
            this.mNxlFileFingerPrint = nxlFileFingerPrint;
            this.mSharedEmailLists = sharedEmailLists;
            this.mComment = comment;
            mBackground.RunWorkerAsync();
        }

        private void DoWorkEventHandler(object sender, DoWorkEventArgs e)
        {
            try
            {
                List<string> delEmails = new List<string>();

                MyVaultFile myVaultFile = mViewerInstance.FunctionProvider.QueryMyVaultFileByDuid(mNxlFileFingerPrint.duid);
                if (null != myVaultFile)
                {
                    MyVaultMetaData md = mViewerInstance.SdkSession.User.GetMyVaultFileMetaData(mNxlFileFingerPrint.localPath, myVaultFile.RmsPathId);        
                    if (md.isShared)
                    {
                        mViewerInstance.SdkSession.User.UpdateRecipients(mNxlFileFingerPrint.localPath, mSharedEmailLists, delEmails);
                    }
                    else
                    {
                        mViewerInstance.SdkSession.User.MyVaultShareFile(mNxlFileFingerPrint.localPath,
                            mSharedEmailLists.ToArray(),
                            myVaultFile.RmsRepoId,
                            mNxlFileFingerPrint.name,
                            myVaultFile.RmsPathId,
                            myVaultFile.RmsDisplayPath,
                            mComment);
                    }
                }
                else
                {
                    Exception ex = new Exception(mViewerInstance.FindResource("Unknown_File").ToString());
                    e.Result = ex;
                }
            }
            catch (Exception ex)
            {
                e.Result = ex;
            }
        }
    }
}
