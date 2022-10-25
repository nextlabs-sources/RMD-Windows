using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Viewer.upgrade.file.basic.utils
{
      public class EditWatcher
    {
        private RegistryWatcher mInternalWatcher = new RegistryWatcher();
        private string mLocalPath;
        private FileInfo mSFileInfo;
        private long mStartFileSize;
        private DateTime mStartFileLstModifedTime;
        private Session mSession;

        public EditWatcher(Session session, string localPath)
        {
            mLocalPath = localPath;
            mSFileInfo = new FileInfo(localPath);
            mStartFileSize = mSFileInfo.Length;
            mStartFileLstModifedTime = mSFileInfo.LastWriteTime;
            mSession = session;
        }

        public void MonitorEditAction(Application application, string rpmPath, Action<bool> OnEditCompleteCallback)
        {
            mInternalWatcher.StartMonitorRegValueDeleted(mSession, rpmPath,
                (bool done) =>
                {
                    if (done)
                    {
                        if (IsFileUnModified())
                        {
                                // Edit for view only.
                                // Send edit finish callback with nomodify params which run on UI thread.
                                application.Dispatcher.BeginInvoke(OnEditCompleteCallback, false);
                        }
                        else
                        {
                                // Edit finished.
                                // Send edit finish callback with modify params which run on UI thread.
                                application.Dispatcher.BeginInvoke(OnEditCompleteCallback, true);
                        }
                    }
                });
        }

        private bool IsFileUnModified()
        {
            string path = mLocalPath;
            //Get start fileinfo recordings.
            long sSize = mStartFileSize;
            DateTime sLstModified = mStartFileLstModifedTime;
            Console.WriteLine("-------------Recording START FileInfo with status writeTime:{0} & size:{1} ", sLstModified, sSize);

            //Re-retrieve file info after edit finished.
            FileInfo eFileInfo = new FileInfo(path);
            long eSize = eFileInfo.Length;
            DateTime eLstModified = eFileInfo.LastWriteTime;
            Console.WriteLine("-------------Recording END FileInfo with status writeTime:{0} & size:{1} ", eLstModified, eSize);

            return DateTime.Equals(sLstModified, eLstModified) && sSize == eSize;
        }

        internal class RegistryWatcher
        {
            public void StartMonitorRegValueDeleted(Session session, string rpmPath, Action<bool> onMonitorDone)
            {
                // pass rmpPath to sdk to start monitor
                Thread thread = new Thread(
                    () =>
                    {
                        session.SDWL_RPM_MonitorRegValueDeleted(rpmPath,
                        (string deletedValueinReg) =>
                        {

                            Console.WriteLine("callback occured" + deletedValueinReg);
                            if (deletedValueinReg.Equals(rpmPath))
                            {
                                Console.WriteLine("the value you monitored has been deleted" + deletedValueinReg);
                                if (null != onMonitorDone)
                                {
                                    onMonitorDone?.Invoke(true);
                                }
                            }

                        });
                    }
                    );
                thread.IsBackground = true;
                thread.Start();
            }
        }
    }
}
