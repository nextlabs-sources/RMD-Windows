using SkydrmDesktop;
using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.featureProvider.externalDrive;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.exception;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.helper;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static SkydrmLocal.rmc.helper.NetworkStatus;
using static SkydrmLocal.rmc.ui.windows.CustomMessageBoxWindow;

namespace SkydrmLocal.rmc.common.component
{
    /// <summary>
    /// Support multiple files upload at the same time using threadPool.
    /// </summary>
    public class UploadManagerEx
    {
        private readonly SkydrmApp app = SkydrmApp.Singleton;

        // waiting upload queue
        private readonly Queue<INxlFile> waitingUploadQueue = new Queue<INxlFile>();

        // copy queue, we should use this queue if judge file whether has uploaded. 
        private readonly Queue<INxlFile> copyWaitingUploadQueue = new Queue<INxlFile>();

        // Record uploading files.
        private IList<INxlFile> uploadingList = new List<INxlFile>();

        // callback
        private OnUploadCompleteEx callback;

        // flags
        private bool IsStopUpload = false;
        private bool isNetworkAvailable;

        // Notify to change the status of file that in current working listview
        public event Action<INxlFile, EnumNxlFileStatus> NotifyChangeStatus;

        // Callback after sync when found modified file.
        public event Action<INxlFile> SyncModifiedFileCallback;

        // Defined as single instance, though Main Window may be closed, as long as "upload flag" is enabled,
        // the file still will upload, in this case, we can see file upload status through Service Manager window.
        private static UploadManagerEx instance;
        private static readonly object locker = new object();

        private UploadManagerEx()
        {
            // Regsiter network status event listener
            AvailabilityChanged += new NetworkStatusChangedHandler(OnNetworkStatusChanged);
            // init net
            isNetworkAvailable = NetworkStatus.IsAvailable;

            ConfigThreadPool();
        }

        // Config the thread maximum number, or else the default value is very big and maybe consume too many system resources. 
        // (the default maximum worker thread number is 32767 and completion port thread number is 1000 on my machine).
        // (the default minimum worker thread number is 8 and completion port thread number is 8 on my machine).
        private void ConfigThreadPool()
        {
            // Note: 1. the maximum of thread count cannot be smaller than processors count on the computer
            // 2. the maximum of thread count cannot be smaller than the corresponding minimum count of thread

            // Print log
            int logicProcessorCount = System.Environment.ProcessorCount;
            int maxWorker; int maxCP;
            ThreadPool.GetMaxThreads(out maxWorker, out maxCP);
            int minWorker; int minCP;
            ThreadPool.GetMinThreads(out minWorker, out minCP);

            app.Log.Info("logicProcessorCount:  " + logicProcessorCount + "\n" +
                         "maxWorker: " + maxWorker + "\n" +
                         "maxCP: " + maxCP + "\n" +
                         "minWorker: " + minWorker + "\n" +
                         "minCP: " + minCP);

            int count = Math.Max(logicProcessorCount, 8);
            if(!ThreadPool.SetMinThreads(count, count))
            {
                app.Log.Info("ThreadPool.SetMinThreads call failed, count is: " + count);
            }

            if (!ThreadPool.SetMaxThreads(count, count))
            {
                app.Log.Info("ThreadPool.SetMaxThreads call failed, count is: " + count);
            }
        }

        public static UploadManagerEx GetInstance()
        {
            if (instance == null)
            {
                lock (locker)
                {
                    if (instance == null)
                    {
                        instance = new UploadManagerEx();
                    }
                }
            }

            return instance;
        }

        public void SetCallback(OnUploadCompleteEx callback)
        {
            this.callback = callback;
        }

        private void OnNetworkStatusChanged(object sender, NetworkStatusChangedArgs e)
        {
            isNetworkAvailable = e.IsAvailable;
        }

        public void SetUploadKey(bool flag)
        {
            SkydrmApp.Singleton.User.StartUpload = flag;
        }

        #region For stop upload
        public void StopUpload()
        {
            SetStopUpload();
            // todo, cancel if supported.
        }

        private void SetStopUpload()
        {

            if (SkydrmApp.Singleton.User.StartUpload)
            {
                IsStopUpload = false;
            }
            else
            {
                IsStopUpload = true;
            }
        }
        #endregion // For stop upload


        // Remove specified file from the q, which used to remove specified local created file by user.
        public void RemoveFromQueue(INxlFile nxlFile)
        {
            if (IsExistInWaitingQueue(nxlFile))
            {
                RemoveFromWaitingQueue(nxlFile);
            }

            if (IsExistInCopyQueue(nxlFile))
            {
                RemoveFromCopyQueue(nxlFile);
            }
        }


        #region Upload using threadPool
        public void TryToUpload()
        {
            SetStopUpload();

            // check
            if (!isNetworkAvailable || IsStopUpload)
            {
                return;
            }

            // restore if needed
            if (IsCopyQueueAvailable)
            {
                TryToRestore();
            }

            // no file
            if (!IsQueueAvailable)
            {
                return;
            }

            // get file from queue
            INxlFile nxl = GetFromWaitingQueue();
            if(nxl == null)
            {
                return;
            }

            do
            {
                // For local edited file, need to check the remote corresponding file if is modified\edited or not before uploading.
                if (IsFileEdited(nxl))
                {
                    HandleFileConflictBeforeUpload(nxl);
                }
                else
                {
                    StartUpload(nxl);
                }

            } while ((nxl = GetFromWaitingQueue()) != null);
        }

        /// <summary>
        /// Upload file directyl without checking file if is modified or not.
        /// </summary>
        public void TryToUploadDirectly()
        {
            SetStopUpload();

            // check
            if (!isNetworkAvailable || IsStopUpload)
            {
                return;
            }

            // restore if needed
            if (IsCopyQueueAvailable)
            {
                TryToRestore();
            }

            // no file
            if (!IsQueueAvailable)
            {
                return;
            }

            // get file from queue
            INxlFile nxl = GetFromWaitingQueue();
            if (nxl == null)
            {
                return;
            }

            do
            {
                StartUpload(nxl);

            } while ((nxl = GetFromWaitingQueue()) != null);
        }

        /// <summary>
        /// Try to upload specify file
        /// </summary>
        /// <param name="nxlFile">The file to upload</param>
        /// <param name="isOverWrite">If is overwrite or not, its false in default.</param>
        /// <param name="callback">Upload progress callback, its null in default, means don't care progress.</param>
        public void UploadSpecifiedFile(INxlFile nxlFile, bool isOverWrite = false, IUploadProgressCallback callback = null)
        {
            if (nxlFile == null || !isNetworkAvailable)
            {
                return;
            }

            new NoThrowTask(true, () =>
            {
                InnerUpload(nxlFile, isOverWrite, callback);
            }).Do();
        }

        /// <summary>
        /// Check the file whether is modified\edited in remote before uploading.
        /// </summary>
        private void HandleFileConflictBeforeUpload(INxlFile nxl)
        {
            app.Log.Info("in HandleFileConflictBeforeUpload, file Name: " + nxl.Name);

            NxlFileConflictMgr.GetInstance().CheckFileVersion(nxl, (bool bIsModified) => {
                if (bIsModified)
                {
                    var result = Edit.Helper.ShowOverwriteDialog(nxl.Name);

                    // overwrite
                    if (CustomMessageBoxResult.Positive == result)
                    {
                        StartUpload(nxl);
                    }

                    // discard upload & update file to local.
                    else 
                    {
                        FileHelper.Delete_NoThrow(nxl.LocalPath);

                        NxlFileConflictMgr.GetInstance().SyncFileNodeFromRms(nxl, (INxlFile updatedFile) => {
                            if (updatedFile != null)
                            {
                                SyncModifiedFileCallback?.Invoke(updatedFile);
                            } else
                            {
                                app.ShowBalloonTip(string.Format(CultureStringInfo.ApplicationFindResource("Sync_ModifiedFile_download_Failed"), updatedFile.Name), false, nxl.Name);
                            }
                        });
                    }
                }
                else
                {
                    StartUpload(nxl);
                }
            });

        }

        private bool IsFileEdited(INxlFile nxl)
        {
            //return nxl != null && nxl.IsEdit &&
            //       (nxl.FileRepo == EnumFileRepo.REPO_PROJECT 
            //       || nxl.FileRepo == EnumFileRepo.REPO_WORKSPACE
            //       || nxl.FileRepo == EnumFileRepo.REPO_EXTERNAL_DRIVE);

            return nxl != null && nxl.IsEdit;
        }

        private void StartUpload(INxlFile nxl)
        {
            // Record the task into another queue.
            EnCopyQueue(nxl);

            // submit task into thread pool to execute, this don't means the file will be upload immediately, since the max thread num is setted MAX_THREAD_NUM.
            // only the threadPool thread becomes available, can execute the task, so we should do copy.
            ThreadPool.QueueUserWorkItem(new WaitCallback(Upload), nxl);

            Console.WriteLine("********The file has been submitted thread pool queue********: " + nxl.Name);
        }

        private void InnerUpload(INxlFile uploadingFile, bool isOverWrite = false, IUploadProgressCallback uploadCallback = null)
        {
            // tmp var
            EnumNxlFileStatus originalStatus = uploadingFile.FileStatus;
            bool bUploadSucceed = true;
            Exception exception = null;

            try
            {
                // disable logout & exit when uploading...
                DoMenuDisable(true);

                // update status
                uploadingFile.FileStatus = EnumNxlFileStatus.Uploading;

                // record uploading files.
                AddToUploadingList(uploadingFile);

                // Need to notify current workinig listview corresponding file status to change.
                // May the file is reproduced from db by refresh, and the original file object that added into the queue is a different object.
                NotifyChangeStatus?.Invoke(uploadingFile, EnumNxlFileStatus.Uploading);

                // invoke api                    
                if (uploadingFile is PendingUploadFile)
                {
                    var doc = uploadingFile as PendingUploadFile;
                    doc.Upload(isOverWrite, uploadCallback);
                }
                else
                {
                    // Update the edited file to rms, support Project and WorkSpace.
                    uploadingFile.UploadEditedFile();
                }
            }
            catch (SkydrmException e)
            {
                SkydrmApp.Singleton.Log.Error("Upload SkydrmException: " + uploadingFile.Name, e);
                bUploadSucceed = false;
                exception = e;
            }
            catch (Exception e)
            {
                SkydrmApp.Singleton.Log.Error("Upload Exception: " + uploadingFile.Name, e);
                bUploadSucceed = false;
                exception = e;
            }
            finally
            {
                // remove from uploading list.
                RemoveFromUploadingList(uploadingFile);

                // When handle session invalid exception, this must be placed after the operaton 'RemoveFromUploadingList',
                // or else, logout will fail (since the flag that file is uploading don't remove) when session is invalid.
                HandleUploadException(uploadingFile, exception);

                // callback
                callback?.Invoke(new UploadResult(uploadingFile, originalStatus, bUploadSucceed));

                // enable menu item
                if (MenuDisableMgr.GetSingleton().IsCanEnableMenu())
                {
                    DoMenuDisable(false);
                }

            }
        }

        private void Upload(object task)
        {
            Console.WriteLine("Sub thread id: {0}", Thread.CurrentThread.ManagedThreadId);
            INxlFile uploadingFile = task as INxlFile;

            // Stop upload
            if (IsStopUpload)
            {
                // Need re-queue the waiting queue.
                RemoveFromCopyQueue(uploadingFile);
                AddToWaitingQueue(uploadingFile);
                return;
            }

            // check
            if (task == null || !isNetworkAvailable || IsRemoved(uploadingFile))
            {
                return;
            }

            // remove from copy q to upload.
            RemoveFromCopyQueue(uploadingFile);

            InnerUpload(uploadingFile);
        }

        private void HandleUploadException(INxlFile uploadFile, Exception e)
        {
            if (uploadFile == null || e == null)
            {
                return;
            }

            if( e is SkydrmException)
            {
                if (e is RmRestApiException)
                {
                    var ex = e as RmRestApiException;

                    // Handle session expiration, force user to logout.
                    if (!SkydrmApp.Singleton.IsPopUpSessionExpirateDlg && ex.ErrorCode == 401)
                    {
                        SkydrmApp.Singleton.Dispatcher.Invoke((Action)delegate
                        {
                            GeneralHandler.HandleSessionExpiration();
                        });
                    }

                    //if(ex.ErrorCode == 4005)
                    //{
                    //    app.ShowBalloonTip(ex.Message, false, uploadFile.Name);
                    //}

                    //
                    // In featureProvider level, upload file will handle RmRestApiException and notify message, so block the code below.
                    //
                    //// Selected dest project or folder may has been deleted.
                    //if(ex.ErrorCode == 404)
                    //{
                    //    string msg = string.Format(CultureStringInfo.ApplicationFindResource("Common_Upload_Not_Found_DestFolder"), uploadFile.Name);
                    //    app.ShowBalloonTip(msg, false, uploadFile.Name);
                    //}

                    //// Handle storage exceeded exception.
                    //if (ex.ErrorCode == 6001 || ex.ErrorCode == 6002) 
                    //{
                    //    app.ShowBalloonTip(ex.Message, false, uploadFile.Name);
                    //}

                    app.Log.Error("----- error code: " + ex.ErrorCode + "--- message: " + ex.Message);
                    
                }

                return;
            }

            // Fix bug 52246, Notify to delete file that in current working listview, when the file was deleted in rmSdk folder. 
            if (!FileHelper.Exist(uploadFile.LocalPath))
            {
                SkydrmApp.Singleton.Log.Error("The file is not exist :" + e.Message, e);
                SkydrmApp.Singleton.Dispatcher.Invoke((Action)delegate
                {
                    // prompt info
                    string msg = CultureStringInfo.ApplicationFindResource("Common_Upload_File_Failed");
                    GeneralHandler.HandleUploadFailed(new SkydrmException(CultureStringInfo.ApplicationFindResource("Common_File_Not_Exist")), msg);
                });

            }
        }

        // Need judge the file wether has been removed by user.
        private bool IsRemoved(INxlFile file)
        {
            return !IsExistInCopyQueue(file);
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

        #endregion // Upload using threadPool


        #region Waiting queue operation
        public void AddToWaitingQueue(INxlFile nxlFile)
        {
            // Here using lock in order to handle with multiple thread operation case.
            lock (this)
            {
                // For uploading file, shouldn't add the queue again.
                if (FileIfIsUploading(nxlFile))
                {
                    return;
                }

                if (IsExistInWaitingQueue(nxlFile))
                {
                    // remove first
                    RemoveFromWaitingQueue(nxlFile);
                }

                // add
                waitingUploadQueue.Enqueue(nxlFile);
            }
        }

        public void SubmitToWaitingQueue(IList<INxlFile> nxlSet)
        {
            if (nxlSet == null || nxlSet.Count == 0)
            {
                return;
            }
            foreach (var one in nxlSet)
            {
                // Here using lock in order to handle with multiple thread operation case.
                lock (this)
                {
                    // For uploading file, shouldn't add the queue again.
                    if (FileIfIsUploading(one))
                    {
                        return;
                    }

                    waitingUploadQueue.Enqueue(one);
                }
            }

        }

        public void ClearWaitingQueue()
        {
            lock (this)
            {
                waitingUploadQueue.Clear();
            }
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

        // Judge the specified file if exist in copy queue.
        private bool IsExistInWaitingQueue(INxlFile nxlFile)
        {
            bool ret = false;

            lock (this)
            {
                INxlFile[] array = waitingUploadQueue.ToArray();
                foreach (INxlFile one in array)
                {
                    if (one.Equals(nxlFile))
                    {
                        ret = true;
                        break;
                    }
                }
            }

            return ret;
        }

        // Remove specified file from the q, which used to remove specified local created file by user.
        private void RemoveFromWaitingQueue(INxlFile nxlFile)
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
                    foreach (var one in l)
                    {
                        if (!one.Equals(nxlFile))
                        {
                            waitingUploadQueue.Enqueue(one);
                        }
                    }
                }
            }
        }

        private INxlFile GetFromWaitingQueue()
        {
            INxlFile ret = null;

            lock (this)
            {
                if (IsQueueAvailable)
                {

                    while (true)
                    {
                        ret = waitingUploadQueue.Dequeue();

                        if (ret.FileStatus == EnumNxlFileStatus.WaitingUpload 
                            || ret.FileStatus == EnumNxlFileStatus.Uploading 
                            || ret.IsEdit)
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

        // Try to restore waiting upload files from copy queue.
        // For the case that net is offline during uploading, then online again, then the copyQueue may still have waiting upload files.
        private void TryToRestore()
        {
            while (IsCopyQueueAvailable)
            {
                AddToWaitingQueue(DeCopyQueue());
            }
        }
        #endregion // Waiting queue operation



        #region Copy queue operation
        private void EnCopyQueue(INxlFile nxlFile)
        {
            lock (this)
            {
                if (!IsExistInCopyQueue(nxlFile))
                {
                    copyWaitingUploadQueue.Enqueue(nxlFile);
                }
            }
        }

        private INxlFile DeCopyQueue()
        {
            INxlFile ret = null;
            lock (this)
            {
                if (IsCopyQueueAvailable)
                {
                    ret = copyWaitingUploadQueue.Dequeue();
                }
            }

            return ret;
        }

        // Remove specified file from the q, which used to remove specified local created file by user.
        private void RemoveFromCopyQueue(INxlFile nxlFile)
        {
            if (IsCopyQueueAvailable)
            {
                lock (this)
                {
                    // save all
                    List<INxlFile> l = new List<INxlFile>(copyWaitingUploadQueue.ToArray());

                    // clear q
                    copyWaitingUploadQueue.Clear();

                    // en-queue again excluding specified delete one
                    foreach (var one in l)
                    {
                        if (!one.Equals(nxlFile))
                        {
                            copyWaitingUploadQueue.Enqueue(one);
                        }
                    }
                }
            }
        }

        public bool IsCopyQueueAvailable
        {
            get
            {
                lock (this)
                {
                    return copyWaitingUploadQueue.Count > 0;
                }
            }
        }

        // Judge the specified file if exist in copy queue.
        private bool IsExistInCopyQueue(INxlFile nxlFile)
        {
            bool ret = false;

            lock (this)
            {
                INxlFile[] array = copyWaitingUploadQueue.ToArray();
                foreach (INxlFile one in array)
                {
                    if (one.Equals(nxlFile))
                    {
                        ret = true;
                        break;
                    }
                }
            }

            return ret;
        }
        #endregion // Copy queue operation



        #region Uploading list operation
        private void AddToUploadingList(INxlFile nxlFile)
        {
            lock (this)
            {
                uploadingList.Add(nxlFile);
            }
        }

        private void RemoveFromUploadingList(INxlFile nxlFile)
        {
            lock (this)
            {
                uploadingList.Remove(nxlFile);
            }
        }

        // Judge if there is uploading task is executing.
        public bool IsExecuteUploading()
        {
            lock (this)
            {
                return uploadingList.Count > 0;
            }
        }

        // Judge the specified file whether is uploading.
        public bool FileIfIsUploading(INxlFile nxlFile)
        {
            lock (this)
            {
                foreach(var one in uploadingList)
                {
                    if ((nxlFile is PendingUploadFile) && one.Equals(nxlFile) &&
                        (one.FileStatus == EnumNxlFileStatus.Uploading || one.IsEdit))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
        #endregion // Uploading list operation

    }

    public class UploadResult
    {
        public INxlFile UploadingFile { get; }
        public bool bUploadSuccess { get; }

        //  Record the original status
        public EnumNxlFileStatus Status { get; }

        public UploadResult(INxlFile nxlFile, EnumNxlFileStatus s, bool bSuccess)
        {
            this.UploadingFile = nxlFile;
            this.Status = s;
            this.bUploadSuccess = bSuccess;
        }
    }
}
