using SkydrmDesktop;
using SkydrmDesktop.Resources.languages;
using SkydrmLocal.rmc.exception;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.fileSystem;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.fileSystem.myvault;
using SkydrmLocal.rmc.helper;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui;
using SkydrmLocal.rmc.ui.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static SkydrmDesktop.rmc.fileSystem.workspace.WorkSpaceRepo;
using static SkydrmLocal.rmc.fileSystem.project.ProjectRepo;
using static SkydrmLocal.rmc.helper.NetworkStatus;

namespace SkydrmLocal.rmc.common.component
{
    class DownloadManager
    {
        private bool isNetworkAvailable;   

        private static DownloadManager singleInstance;
        private static readonly object locker = new object();

        // download task queue
        private readonly Queue<INxlFile> taskQueue = new Queue<INxlFile>();

        /// <summary>
        /// Record the files that is downloading, should forbid the downloading file to add into download queue again.
        /// When user switch treeview item during downloading, then switch back into original item again(imaging file is still downloading),
        /// will refresh and re-get data from db and produce another different object and then add into download queue again(since file status is "downloading", in order to restore download).
        /// ---- fix bug 52838.
        /// </summary>
        private readonly IList<INxlFile> downloadingList = new List<INxlFile>();

        // Flag that indicates if is executing submit task into thread pool
        private bool IsExecutingSubmit { get; set; }

        private DownloadManager()
        {
            // Regsiter network status event listener
            AvailabilityChanged += new NetworkStatusChangedHandler(OnNetworkStatusChanged);
            // init net
            isNetworkAvailable = NetworkStatus.IsAvailable;

            //// Get the machine logic processor count, which as the max thread count.
            //int logicProcessorCount = System.Environment.ProcessorCount;

            //// config thread max count -- must be not less than the logic processor count, or else, set failed.
            //if (!ThreadPool.SetMaxThreads(logicProcessorCount, logicProcessorCount))
            //{
            //    Console.WriteLine("Set max thread count failed");
            //};

            IsExecutingSubmit = false;
        }
        
        public static DownloadManager GetSingleton()
        {
            if (singleInstance == null)
            {
                lock (locker)
                {
                    if (singleInstance == null)
                    {
                        singleInstance = new DownloadManager();
                    }
                }
            }

            return singleInstance;
        }

        private void OnNetworkStatusChanged(object sender, NetworkStatusChangedArgs e)
        {
            isNetworkAvailable = e.IsAvailable;
        }

        public void TryDownload(OnDownloadComplete callback, bool isViewOnly,bool isDownloadPartial, bool isOnlineView)
        {
            // Do not have condition
            if (!IsQueueAvailable || !isNetworkAvailable)
            {
                callback(false);
                return;
            }

            // Flag that indicates if is executing submit task into thread pool
            if (IsExecutingSubmit)
            {
                callback(false);
                return;
            }

            INxlFile nxl = GetFromQueue();
            if (nxl != null)
            {
                IsExecutingSubmit = true;

                do
                {
                    DownloadConfig config = new DownloadConfig(nxl, callback, isViewOnly, isDownloadPartial, isOnlineView);

                    Console.WriteLine("==ndstest=== File name: =======" + nxl.Name + "=== Submit to QueueUserWorkItem === ");

                    // submit task into thread pool to execute.
                    ThreadPool.QueueUserWorkItem(new WaitCallback(Download), config);  

                } while ((nxl = GetFromQueue()) != null);

            }

            IsExecutingSubmit = false;

            Console.WriteLine("==ndstest=== submit task complete! ======= ");
        }


        /// <summary>
        ///  Used to restore last downloading -- App exit or killed when file is downloading.
        /// </summary>
        public void TryRestoreDownload(OnDownloadCompleteEx callback)
        {
            if (!IsQueueAvailable || !isNetworkAvailable)
            {
                return;
            }

            // Flag that indicates if is executing submit task into thread pool
            if (IsExecutingSubmit)
            {
                return;
            }


            INxlFile nxl = GetFromQueue();
            if (nxl != null)
            {
                IsExecutingSubmit = true;

                do
                {
                    DownloadConfig config = new DownloadConfig(nxl, callback);

                    // submit task into thread pool to execute.
                    ThreadPool.QueueUserWorkItem(new WaitCallback(RestoreDownload), config);

                } while ((nxl = GetFromQueue()) != null);

            }

            IsExecutingSubmit = false;
        }


        private void Download(object task)
        {
            DownloadConfig config = (DownloadConfig)task;

            // download
            INxlFile nxl = config.File;
            bool result = InnerDownload(nxl, config.IsDownloadPartial,config.IsForViewOnly, config.IsOnlineView);

            config.Callback?.Invoke(result);
        }

        private void RestoreDownload(object task)
        {
            DownloadConfig config = (DownloadConfig)task;

            // download
            INxlFile nxl = config.File;
            bool result = InnerDownload(nxl);

            config.CallbackEx.Invoke(result, nxl);
        }

        private bool InnerDownload(INxlFile nxl, bool isDownloadPartial = false,bool isForViewOnly=false, bool isOnlineView = false)
        {

            // disable logout & exit when downloading...
            DoMenuDisable(true);

            bool bSuccess = true;
            SkydrmException skydrmException = null;

            try
            {
                // record downloading files
                AddToDownloadingList(nxl);

                // Change status, used to ui icon quik binding.
                if (!isDownloadPartial && !isOnlineView)
                {
                    nxl.FileStatus = EnumNxlFileStatus.Downloading;
                }

                Console.WriteLine("|||||||| begin to download......");

                if (isDownloadPartial)
                {
                    // download partial file for get rights.
                    nxl.DownloadPartial();
                }
                else
                {
                    nxl.DownloadFile(isForViewOnly);
                }
            }
            catch(SkydrmException e)
            {
                SkydrmApp.Singleton.Log.Error(e);
                bSuccess = false;
                skydrmException = e;
            }
            catch (Exception e)
            {
                SkydrmApp.Singleton.Log.Error(e);
                bSuccess = false;
            }
            finally
            {
                // remove from downloading list
                RemoveFromDownloadingList(nxl);

                // Handle session invalid, this must be placed after the operaton 'RemoveFromDownloadingList',
                // or else, logout will fail (since the flag that file is downloading don't remove) when session is invalid.
                HandleSkydrmExcep(skydrmException);

                // enable.
                if (MenuDisableMgr.GetSingleton().IsCanEnableMenu())
                {
                    DoMenuDisable(false);
                }
            }

            return bSuccess;
        }

        private void HandleSkydrmExcep(SkydrmException e)
        {
            if (e != null && e is RmRestApiException)
            {
                var ex = e as RmRestApiException;
                // handle session expiration, logout
                if (!SkydrmApp.Singleton.IsPopUpSessionExpirateDlg && ex.ErrorCode == 401)
                {
                    SkydrmApp.Singleton.Dispatcher.Invoke((Action)delegate
                    {
                        GeneralHandler.HandleSessionExpiration();
                    });
                }
                else if (ex.ErrorCode == 403)
                {
                   // SkydrmApp.Singleton.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Exception_Sdk_Rest_403_AccessForbidden"), false);
                }
            }
        }

        private void DoMenuDisable(bool isDisable)
        {
            if (isDisable)
            {
                SkydrmApp.Singleton.Dispatcher.Invoke(new Action(() => {
                    MenuDisableMgr.GetSingleton().MenuDisableNotify("LOGOUT", true);
                    MenuDisableMgr.GetSingleton().MenuDisableNotify("EXIT", true);
                }));
            }
            else
            {
                SkydrmApp.Singleton.Dispatcher.Invoke(new Action(() => {
                    MenuDisableMgr.GetSingleton().MenuDisableNotify("LOGOUT", false);
                    MenuDisableMgr.GetSingleton().MenuDisableNotify("EXIT", false);
                }));
            }
        }

        #region Downloading list operation
        private void AddToDownloadingList(INxlFile nxlFile)
        {
            lock(this)
            {
                downloadingList.Add(nxlFile);
            }
        }

        private void RemoveFromDownloadingList(INxlFile nxlfile)
        {
            lock(this)
            {
                downloadingList.Remove(nxlfile);
            }
        }

        // Judge if there is download task is executing.
        public bool IsDownloading()
        {
            lock (this)
            {
                return downloadingList.Count > 0;
            }
        }

        // Judge the specified file whether is downloading.
        public bool FileIfIsDownloading(INxlFile nxlFile)
        {
            lock (this)
            {
                foreach (var one in downloadingList)
                {
                    if (one.Equals(nxlFile) && one.FileStatus == EnumNxlFileStatus.Downloading)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        #endregion // Downloading list operation

        #region queue operation

        public DownloadManager SubmitToTaskQueue(INxlFile nxlFile)
        {
            lock (this)
            {
                if (!FileIsExistTaskQueue(nxlFile) && !FileIfIsDownloading(nxlFile))
                {
                    Console.WriteLine("||||||| SubmitToTaskQueue, file name:  " + nxlFile.Name);

                    taskQueue.Enqueue(nxlFile);
                }
            }

            return this;
        }

        // Judge the specified file if has already existed in download task queue.
        private bool FileIsExistTaskQueue(INxlFile nxl)
        {
            bool ret = false;

            foreach(INxlFile one in taskQueue)
            {
                if (one.Equals(nxl))
                {
                    ret = true;
                    break;
                }
            }

            return ret;
        }

        public INxlFile GetFromQueue()
        {
            INxlFile ret = null;

            lock (this)
            {
                if (IsQueueAvailable)
                {
                    ret = taskQueue.Dequeue();
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
                    return taskQueue.Count > 0;
                }
            }
        }

        public void ClearQueue()
        {
            lock (this)
            {
                taskQueue.Clear();
            }
        }

        #endregion // queue operation

        class DownloadConfig
        {
            public INxlFile File { get; }
            public OnDownloadComplete Callback { get; }

            // used to resore.
            public OnDownloadCompleteEx CallbackEx { get; }

            public bool IsDownloadPartial { get; }

            public bool IsForViewOnly { get; }

            public bool IsOnlineView { get; }

            public DownloadConfig(INxlFile file, OnDownloadComplete callback,bool isForView, bool isDownloadPartial, bool isOnlineView)
            {
                this.File = file;
                this.Callback = callback;
                this.IsDownloadPartial = isDownloadPartial;
                this.IsForViewOnly = isForView;
                this.IsOnlineView = isOnlineView;
            }

            public DownloadConfig(INxlFile file, OnDownloadCompleteEx callback)
            {
                this.File = file;
                this.CallbackEx = callback;
            }

        }

    }
}
