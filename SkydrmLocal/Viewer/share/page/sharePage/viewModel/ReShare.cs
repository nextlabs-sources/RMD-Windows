using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.database;
using Viewer.utils;

namespace Viewer.share
{
    public class ReShare : IShare
    {
        private ViewerApp mViewerInstance = (ViewerApp)System.Windows.Application.Current;
        private NxlFileFingerPrint mNxlFileFingerPrint;
        private BackgroundWorker mBackground = new BackgroundWorker();
        private List<string> mSharedEmailLists = new List<string>();
        private string mComment;

        public ReShare(RunWorkerCompletedEventHandler RunWorkerCompleted)
        {
            mBackground.WorkerReportsProgress = true;
            mBackground.WorkerSupportsCancellation = true;
            mBackground.DoWork += DoWorkEventHandler;
            mBackground.RunWorkerCompleted += RunWorkerCompleted;
        }

        public bool IsBusy
        {
            get { return mBackground.IsBusy; }
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
                SharedWithMeFile sharedWithMeFile = mViewerInstance.FunctionProvider.QuerySharedWithMeFileByDuid(mNxlFileFingerPrint.duid);
                if (null != sharedWithMeFile)
                {
                    string tid = sharedWithMeFile.Transaction_id;
                    string tcode = sharedWithMeFile.Transaction_code;
                    mViewerInstance.Session.User.SharedWithMeReshareFile(tid, tcode, mSharedEmailLists.ToArray());
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
