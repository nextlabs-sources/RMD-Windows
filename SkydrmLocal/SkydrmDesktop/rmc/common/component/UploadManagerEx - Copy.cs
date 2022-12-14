using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.exception;
using SkydrmLocal.rmc.fileSystem;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.fileSystem.project;
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
using static SkydrmLocal.rmc.fileSystem.project.ProjectRepo;
using static SkydrmLocal.rmc.helper.NetworkStatus;
using static SkydrmLocal.rmc.ui.windows.CustomMessageBoxWindow;

namespace SkydrmLocal.rmc.common.component
{
    /// <summary>
    /// Support multiple files upload at the same time using threadPool.
    /// </summary>
    public class UploadManagerEx
    {
        private readonly SkydrmLocalApp app = SkydrmLocalApp.Singleton;
        private static readonly object locker = new object();

        // Max submitted uploading task number
        private static readonly int MAX_TASK_NUM = 5;

        // Used to notify waiting thread by signal, status including: signaled & non-signaled.
        private AutoResetEvent resetEvent = new AutoResetEvent(false);
        private bool bIsWaiting = false;
        private NoThrowTask uploadTask;

        // Waiting upload queue
        private readonly Queue<INxlFile> waitingUploadQueue = new Queue<INxlFile>();
        // Use this queue to record the files that has been submited into thread pool.
        private readonly Queue<INxlFile> taskSubmitQueue = new Queue<INxlFile>();

        // Record uploading files.
        private IList<INxlFile> uploadingList = new List<INxlFile>();
        private IList<INxlFile> recordTaskList = new List<INxlFile>();

        // Upload switch flag
        private bool IsStopUpload = false;
        // Network status
        private bool isNetworkAvailable;

        // Notify to change the status of file that in current working listview
        public event Action<INxlFile, EnumNxlFileStatus> NotifyChangeStatus;
        // Callback after sync when found modified file.
        public event Action<INxlFile> SyncModifiedFileCallback;
        // Upload complete callback
        private OnUploadCompleteEx callback;

        // Defined as single instance, though Main Window may be closed, as long as "upload flag" is enabled,
        // the file still will upload, in this case, we can see file upload status through Service Manager window.
        private static UploadManagerEx instance;

        // Project repository, which support edited file uploading.
        private static ProjectRepo projectRepo;

        private UploadManagerEx()
        {
            // Regsiter network status event listener
            AvailabilityChanged += new NetworkStatusChangedHandler(OnNetworkStatusChanged);
            // init net
            isNetworkAvailable = NetworkStatus.IsAvailable;

            uploadTask = new NoThrowTask(true, UploadTask);

            //fix bug ipc can not communication, if current computer only have 1 cpu ; logicProcessorCount == 1;

            //Get the machine logic processor count, which as the max thread count.
            //int logicProcessorCount = System.Environment.ProcessorCount;

            // config thread max count -- must be not less than the logic processor count, or else, set failed.
            //if(!ThreadPool.SetMaxThreads(logicProcessorCount, logicProcessorCount))
            //{
            //    Console.WriteLine("Set max thread count failed");
            //};
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

        public UploadManagerEx SetFileRepo(ProjectRepo fr)
        {
            projectRepo = fr;
            return GetInstance();
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
            SkydrmLocalApp.Singleton.User.StartUpload = flag;
        }

        #region For stop upload
        public void StopUpload()
        {
            SetStopUpload();
            // todo, cancel if supported.
        }

        private void SetStopUpload()
        {

            if (SkydrmLocalApp.Singleton.User.StartUpload)
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

            if (IsExistInTaskSubmitQueue(nxlFile))
            {
                RemoveFromTaskSubmitQueue(nxlFile);
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
            if (IsTaskSubmitQueueAvailable)
            {
                TryToRestore();
            }

            // no file
            if (!IsQueueAvailable)
            {
                return;
            }

            uploadTask.Do();
        }

        private void DoUpload(INxlFile nxl)
        {
            app.Dispatcher.Invoke(() => {

                app.Log.Info("================> ---Submit ----, file name: " + nxl.Name);

                // For project edited file, need to check the remote corresponding file if is modified or not before uploading.
                if (nxl.FileRepo == EnumFileRepo.REPO_PROJECT && nxl.IsEdit)
                {
                    HandleFileConflictBeforeUpload(nxl);
                }
                else
                {
                    StartUpload(nxl);
                }

            });
        }

        private void UploadTask()
        {
            // get file from queue
            INxlFile nxl = GetFromWaitingQueue();
            if (nxl == null)
            {
                return;
            }

            do
            {
                if (!IsExceedMaxSubmitTask())
                {
                    DoUpload(nxl);
                }
                else
                {
                    app.Log.Info("================> ---Begin ---waitOne(), file name: " + nxl.Name);

                    // block here and waiting next waik
                    bIsWaiting = true;
                    resetEvent.WaitOne();

                    app.Log.Info("================> ---End ---- waitOne(), file name: " + nxl.Name);

                    DoUpload(nxl);
                }

            } while ((nxl = GetFromWaitingQueue()) != null);
        }

        /// <summary>
        /// Upload file directyl without checking file if is modified or not, mainly used for upload edited file.
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
            if (IsTaskSubmitQueueAvailable)
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
        public void UploadSpecifiedFile(INxlFile nxlFile)
        {
            if (nxlFile == null || !isNetworkAvailable)
            {
                return;
            }

            new NoThrowTask(true, () =>
            {
                InnerUpload(nxlFile);
            }).Do();
        }


        /// <summary>
        /// Handle the project file conflict issue before uploading.
        /// </summary>
        private void HandleFileConflictBeforeUpload(INxlFile nxl)
        {
            app.Log.Info("in HandleFileConflictBeforeUpload, file Name: " + nxl.Name);

            NxlFileConflictMgr.GetInstance().SetFileRepo(projectRepo).CheckFileVersion(nxl, (bool bIsModified) => {
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
                                app.ShowBalloonTip(string.Format(CultureStringInfo.Sync_ModifiedFile_download_Failed, updatedFile.Name));
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

        private void StartUpload(INxlFile nxl)
        {
            // Record the submitted task into another queue.
            EnTaskSubmitQueue(nxl);

            AddtoRecordTask(nxl);

            // submit task into thread pool to execute, this don't means the file will be upload immediately, since the max thread num is setted MAX_THREAD_NUM.
            // only the threadPool thread becomes available, can execute the task, so we use TaskSubmitQueue to record.
            ThreadPool.QueueUserWorkItem(new WaitCallback(Upload), nxl);

            Console.WriteLine("********The file has been submitted thread pool queue********: " + nxl.Name);
        }

        private void InnerUpload(INxlFile uploadingFile)
        {
            // tmp var
            EnumNxlFileStatus originalStatus = uploadingFile.FileStatus;
            bool bUploadSucceed = true;

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
                    doc.Upload();
                }
                else if (uploadingFile is ProjectRmsDoc) // Edited project file need upload to rms again.
                {
                    // Upload project edited file.
                    var doc = uploadingFile as ProjectRmsDoc;
                    doc.Raw.UploadEditedFile();
                }

            }
            catch (SkydrmException e)
            {
                SkydrmLocalApp.Singleton.Log.Error("Upload SkydrmException: " + e);
                bUploadSucceed = false;

                HandleUploadException(uploadingFile, e);
            }
            catch (Exception e)
            {
                SkydrmLocalApp.Singleton.Log.Error("Upload Exception: " + e);
                bUploadSucceed = false;

                HandleUploadException(uploadingFile, e);
            }
            finally
            {
                // remove from uploading list.
                RemoveFromUploadingList(uploadingFile);

                RemoveRecordTask(uploadingFile);
                // set event to siganled, which enable waiting thread to continue to process.
                if (bIsWaiting)
                {
                    app.Log.Info("================> Begin Set(), file name: " + uploadingFile.Name);

                    resetEvent.Set();
                    bIsWaiting = false;
                }

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
                RemoveFromTaskSubmitQueue(uploadingFile);
                AddToWaitingQueue(uploadingFile);
                return;
            }

            // check
            if (task == null || !isNetworkAvailable || IsRemoved(uploadingFile))
            {
                return;
            }

            // remove from submitted queue to upload.
            RemoveFromTaskSubmitQueue(uploadingFile);

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
                    if (!SkydrmLocalApp.Singleton.IsPopUpSessionExpirateDlg && ex.ErrorCode == 401)
                    {
                        SkydrmLocalApp.Singleton.Dispatcher.Invoke((Action)delegate
                        {
                            GeneralHandler.HandleSessionExpiration();
                        });
                    }

                    // Selected dest project or folder may has been deleted.
                    if(ex.ErrorCode == 404)
                    {
                        string msg = string.Format(CultureStringInfo.Common_Upload_Not_Found_DestFolder, uploadFile.Name);
                        app.ShowBalloonTip(msg, 2000);
                    }

                    // Handle storage exceeded exception.
                    if (ex.ErrorCode == 6001 || ex.ErrorCode == 6002) 
                    {
                        app.ShowBalloonTip(ex.Message);
                    }

                    app.Log.Error("----- error code: " + ex.ErrorCode + "--- message: " + ex.Message);
                    
                }

                return;
            }

            // Fix bug 52246, Notify to delete file that in current working listview, when the file was deleted in rmSdk folder. 
            if (!FileHelper.Exist(uploadFile.LocalPath))
            {
                SkydrmLocalApp.Singleton.Log.Error("The file is not exist :" + e.Message, e);
                SkydrmLocalApp.Singleton.Dispatcher.Invoke((Action)delegate
                {
                    // prompt info
                    string msg = CultureStringInfo.Common_Upload_File_Failed;
                    GeneralHandler.HandleUploadFailed(new SkydrmException(CultureStringInfo.Common_File_Not_Exist), msg);
                });

            }
        }

        // Need judge the file wether has been removed by user.
        private bool IsRemoved(INxlFile file)
        {
            return !IsExistInTaskSubmitQueue(file);
        }

        private void DoMenuDisable(bool isDisable)
        {
            if (isDisable)
            {
                SkydrmLocalApp.Singleton.Dispatcher.Invoke(new Action(() => {
                    MenuDisableMgr.GetSingleton().MenuDisableNotify(CultureStringInfo.MenuItem_Logout, true);
                    MenuDisableMgr.GetSingleton().MenuDisableNotify(CultureStringInfo.MenuItem_Exit, true);
                }));
            }
            else
            {
                SkydrmLocalApp.Singleton.Dispatcher.Invoke(new Action(() => {
                    MenuDisableMgr.GetSingleton().MenuDisableNotify(CultureStringInfo.MenuItem_Logout, false);
                    MenuDisableMgr.GetSingleton().MenuDisableNotify(CultureStringInfo.MenuItem_Exit, false);
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

                // May the edited file has enqueue but not upload yet, then user edit it again and again enqueue.
                if (nxlFile.IsEdit)
                {
                    if (IsExistInWaitingQueue(nxlFile))
                    {
                        // remove first
                        RemoveFromWaitingQueue(nxlFile);
                    }

                    // add
                    waitingUploadQueue.Enqueue(nxlFile);
                }
                else
                {
                    waitingUploadQueue.Enqueue(nxlFile);
                }
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
                    if (one.Name == nxlFile.Name)
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
                        if (one.Name != nxlFile.Name)
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
            while (IsTaskSubmitQueueAvailable)
            {
                AddToWaitingQueue(DeTaskSubmitQueue());
            }
        }
        #endregion // Waiting queue operation



        #region task submit queue operation
        private void EnTaskSubmitQueue(INxlFile nxlFile)
        {
            lock (this)
            {
                if (!IsExistInTaskSubmitQueue(nxlFile))
                {
                    taskSubmitQueue.Enqueue(nxlFile);
                }
            }
        }

        private INxlFile DeTaskSubmitQueue()
        {
            INxlFile ret = null;
            lock (this)
            {
                if (IsTaskSubmitQueueAvailable)
                {
                    ret = taskSubmitQueue.Dequeue();
                }
            }

            return ret;
        }

        private bool IsExceedMaxSubmitTask()
        {
            lock (this)
            {
                return recordTaskList.Count >= MAX_TASK_NUM;
            }
        }
        private void AddtoRecordTask(INxlFile nxl)
        {
            lock (this)
            {
                recordTaskList.Add(nxl);
            }
        }

        private void RemoveRecordTask(INxlFile nxl)
        {
            lock (this)
            {
                recordTaskList.Remove(nxl);
            }
        }

        // Remove specified file from the q, which used to remove specified local created file by user.
        private void RemoveFromTaskSubmitQueue(INxlFile nxlFile)
        {
            if (IsTaskSubmitQueueAvailable)
            {
                lock (this)
                {
                    // save all
                    List<INxlFile> l = new List<INxlFile>(taskSubmitQueue.ToArray());

                    // clear q
                    taskSubmitQueue.Clear();

                    // en-queue again excluding specified delete one
                    foreach (var one in l)
                    {
                        if (one.Name != nxlFile.Name)
                        {
                            taskSubmitQueue.Enqueue(one);
                        }
                    }
                }
            }
        }

        public bool IsTaskSubmitQueueAvailable
        {
            get
            {
                lock (this)
                {
                    return taskSubmitQueue.Count > 0;
                }
            }
        }

        // Judge the specified file if exist in task submit queue.
        private bool IsExistInTaskSubmitQueue(INxlFile nxlFile)
        {
            bool ret = false;

            lock (this)
            {
                INxlFile[] array = taskSubmitQueue.ToArray();
                foreach (INxlFile one in array)
                {
                    if (one.Name == nxlFile.Name)
                    {
                        ret = true;
                        break;
                    }
                }
            }

            return ret;
        }
        #endregion //  task submit operation



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
                    if (one.Name == nxlFile.Name && (one.FileStatus == EnumNxlFileStatus.Uploading || one.IsEdit))
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
