using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.database;
using Viewer.utils;

namespace Viewer.share
{
    public class UpdateRecipients : IShare
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

        public UpdateRecipients(RunWorkerCompletedEventHandler RunWorkerCompleted)
        {
            mBackground.WorkerReportsProgress = true;
            mBackground.WorkerSupportsCancellation = true;
            mBackground.DoWork += DoWorkEventHandler;
            mBackground.RunWorkerCompleted += RunWorkerCompleted;
        }

        public void Share(NxlFileFingerPrint nxlFileFingerPrint, List<string> sharedEmailLists, string comment)
        {
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
                    MyVaultMetaData md = mViewerInstance.Session.User.GetMyVaultFileMetaData(mNxlFileFingerPrint.localPath, myVaultFile.RmsPathId);        
                    if (md.isShared)
                    {
                        mViewerInstance.Session.User.UpdateRecipients(mNxlFileFingerPrint.localPath, mSharedEmailLists, delEmails);
                    }
                    else
                    {
                        mViewerInstance.Session.User.MyVaultShareFile(mNxlFileFingerPrint.localPath,
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
                    Exception ex = new Exception(CultureStringInfo.Unknown_File);
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
