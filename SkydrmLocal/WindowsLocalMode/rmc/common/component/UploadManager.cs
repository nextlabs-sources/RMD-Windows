using SkydrmLocal.rmc.common.component;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.fileSystem.myvault;
using SkydrmLocal.rmc.fileSystem.project;
using SkydrmLocal.rmc.ui;
using SkydrmLocal.rmc.ui.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static SkydrmLocal.rmc.helper.NetworkStatus;

namespace SkydrmLocal.rmc.helper
{
    public class UploadManager
    {
        private SkydrmLocalApp App = (SkydrmLocalApp)SkydrmLocalApp.Current;

        // waiting uploaf queue
        private readonly Queue<INxlFile> waitingUploadQueue = new Queue<INxlFile>();

        // Upload thread
        private Thread upLoadThread;
        private OnUploadComplete callback;

        // record now is uploading file.
        private INxlFile uploadingFile;
        public INxlFile UploadingFile
        {
            get { return uploadingFile; }
            set { uploadingFile = value; }
        }

        private bool IsStopUpload = false;
        private bool isNetworkAvailable;

        // Notify to change the status of file that in current working listview
        public event Action<INxlFile, EnumNxlFileStatus> NotifyChangeStatus;

        // Defined as single instance, though Main Window may be closed, as long as "upload flag" is enabled,
        // the file still will upload, in this case, we can see file upload status through Service Manager window.
        private static UploadManager instance;

        private UploadManager()
        {
            // Regsiter network status event listener
            AvailabilityChanged += new NetworkStatusChangedHandler(OnNetworkStatusChanged);
            // init net
            isNetworkAvailable = NetworkStatus.IsAvailable;
        }

        public static UploadManager GetInstance()
        {
            if (instance == null)
            {
                instance = new UploadManager();
            }

            return instance;
        }

        public void setCallback(OnUploadComplete callback)
        {
            this.callback = callback;
        }

        private void OnNetworkStatusChanged(object sender, NetworkStatusChangedArgs e)
        {
            isNetworkAvailable = e.IsAvailable;
        }

        public void SetUploadKey(bool flag)
        {
            //App.Config.ModifyPreference("Upload", flag);
            App.User.StartUpload = flag;
        }

        public bool IsUploadThreadAlive()
        {
            return upLoadThread != null && upLoadThread.IsAlive;
        }

        public void ExcuteUpload()
        {
            SetStopUpload();

            // Upload thread is created and alive
            if (IsUploadThreadAlive())
            {
                return;
            }

            // create one thread to execute upload.
            upLoadThread = new Thread(new ThreadStart(UpLoadWorkingThread)) { Name = "UplaodMgr", IsBackground = true };
            upLoadThread.Start();
        }

        private void UpLoadWorkingThread()
        {

            while (!IsStopUpload && isNetworkAvailable && !App.IsRequestLogout)
            {
                // upload flag.
                bool bUploadSucceed = true;

                // Get file from queue and upload it.
                uploadingFile = GetFromQueue();

                // queue is empty.
                if (uploadingFile == null)
                {
                    // terminate the thread.
                    // Actually, the thread can't be terminated immediately(Because our code is calling native dll code)
                    App.Log.Warn("empty of the uploading file queue, abort uplaoding_thread");
                    upLoadThread.Abort();
                }

                try
                {

                    // disable logout & exit when uploading...
                    App.Dispatcher.Invoke(new Action(() => {
                        MenuDisableMgr.GetSingleton().MenuDisableNotify(CultureStringInfo.MenuItem_Logout, true);
                        MenuDisableMgr.GetSingleton().MenuDisableNotify(CultureStringInfo.MenuItem_Exit, true);
                    }));


                    // update status
                    uploadingFile.FileStatus = EnumNxlFileStatus.Uploading;

                    // Need to notify current workinig listview corresponding file status to change.
                    // May the file is reproduced from db by refresh, and the original file object that added into the queue is a 
                    // different object.
                    NotifyChangeStatus?.Invoke(uploadingFile, EnumNxlFileStatus.Uploading);

                    // invoke api                    
                    if (uploadingFile is PendingUploadFile)
                    {
                        var doc = uploadingFile as PendingUploadFile;
                        doc.Upload();
                    }

                }
                catch (Exception e)
                {
                    bUploadSucceed = false;
                    Console.WriteLine(e.ToString());
                    
                    // prompt info
                    // App.ShowBalloonTip(CultureStringInfo.Common_Upload_File_Failed);
                }
                finally
                {
                    callback?.Invoke(bUploadSucceed, uploadingFile);

                    // enable
                    if (!IsUploading() && !DownloadManager.GetSingleton().IsDownloading())
                    {
                        App.Dispatcher.Invoke(new Action(() => {
                            MenuDisableMgr.GetSingleton().MenuDisableNotify(CultureStringInfo.MenuItem_Logout, false);
                            MenuDisableMgr.GetSingleton().MenuDisableNotify(CultureStringInfo.MenuItem_Exit, false);
                        }));
                    }

                }

            }

        }

        // Judge if there is uploading task is executing.
        public bool IsUploading()
        {
            return uploadingFile != null;
        }

        public void StopUpload()
        {
            SetStopUpload();
            // todo, cancel
        }

        private void SetStopUpload()
        {
            // read upload flag key from register.
            //App.Config.GetPreferences();

            if (App.User.StartUpload)
            {
                IsStopUpload = false;
            }
            else
            {
                IsStopUpload = true;
            }
        }


        #region queue operation
        public void AddToQueue(INxlFile nxlFile)
        {
            // Here using lock in order to handle with multiple thread operation case.
            lock (this)
            {
                // For uploading file, shouldn't add the queue again.
                if (FileIsUploading(nxlFile))
                {
                    return;
                }

                waitingUploadQueue.Enqueue(nxlFile);
            }
        }

        // Remove specified file from the q, which used to remove specified local created file by user.
        public void RemoveFromQueue(INxlFile nxlFile)
        {
            if (IsQueueAvailable)
            {
                lock (this)
                {
                    // save all
                    List<INxlFile> l = new List<INxlFile>(waitingUploadQueue.ToArray());

                    // clear q
                    waitingUploadQueue.Clear();

                    // en-queue again excluding specified delete one
                    foreach(var one in l)
                    {
                        if (one.Name != nxlFile.Name)
                        {
                            waitingUploadQueue.Enqueue(one);
                        }
                    }
                }
            }
        }

        // Check the file if is uploading, 
        public bool FileIsUploading(INxlFile nxlFile)
        {
            return uploadingFile != null
                && uploadingFile.FileStatus == EnumNxlFileStatus.Uploading
                && nxlFile.Name == uploadingFile.Name;
        }

        public void AddToQueue(IList<INxlFile> nxlSet)
        {
            if (nxlSet == null || nxlSet.Count == 0)
            {
                return;
            }
            foreach(var one in nxlSet)
            {
                // Here using lock in order to handle with multiple thread operation case.
                lock (this)
                {
                    // For uploading file, shouldn't add the queue again.
                    if (FileIsUploading(one))
                    {
                        return;
                    }

                    waitingUploadQueue.Enqueue(one);
                }
            }

        }

        public void ClearQueue()
        {
            lock (this)
            {
                waitingUploadQueue.Clear();
            }
        }

        public INxlFile GetFromQueue()
        {
            INxlFile ret = null;

            lock (this)
            {
                if (IsQueueAvailable)
                {
                    App.Log.Info("**********Get NxlFile successfully from waiting queue.**********");

                    while (true)
                    {
                        ret = waitingUploadQueue.Dequeue();

                        if (ret.FileStatus == EnumNxlFileStatus.WaitingUpload || ret.FileStatus == EnumNxlFileStatus.Uploading)
                        {
                            break;
                        }
                        else // may be deleted by user.
                        {
                            if (IsQueueAvailable)
                            {
                                continue;
                            }
                            else
                            {
                                ret = null;
                                break;
                            }
                        }
                    }

                }
            }

            return ret;
        }

        /// <summary>
        /// Get the specified file from the queue without remove it.
        /// </summary>
        public bool GetFileFromQueue(string name, out INxlFile localFile)
        {
            bool ret = false;
            localFile = null;

            INxlFile[] array = waitingUploadQueue.ToArray();
            foreach (INxlFile one in array)
            {
                if (one.Name == name)
                {
                    localFile = one;
                    ret = true;
                    break;
                }
            }

            return ret;
        }

        public bool IsQueueAvailable
        {
            get
            {
                lock (this)
                {
                    return waitingUploadQueue.Count > 0;
                }
            }
        }
        #endregion // queue operation

    }
}
