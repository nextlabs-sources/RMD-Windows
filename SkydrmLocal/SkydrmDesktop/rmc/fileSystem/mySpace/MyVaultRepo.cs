using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui.utils;
using SkydrmLocal.rmc.helper;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.fileSystem.myvault;
using System.ComponentModel;
using SkydrmLocal.rmc.featureProvider.common;
using SkydrmLocal.rmc.common.component;
using SkydrmLocal.rmc.featureProvider.MyVault;
using System.Threading;
using SkydrmLocal.rmc.Edit;
using SkydrmLocal.rmc.ui;
using SkydrmDesktop;
using SkydrmDesktop.Resources.languages;
using SkydrmLocal.rmc.exception;
using SkydrmDesktop.rmc.fileSystem;
using SkydrmDesktop.rmc.common.helper;
using SkydrmLocal.rmc.featureProvider.SharedWithMe;
using SkydrmDesktop.rmc.fileSystem.utils;
using SkydrmLocal.rmc.common.helper;

namespace SkydrmLocal.rmc.fileSystem.myvault
{
    public class MyVaultRepo : AbstractFileRepo
    {
        // For all myVault files
        private IList<INxlFile> FilePool { get; set; }

        // Flag that is loading data, Binding UI to display
        public bool IsLoading { get; private set; }

        // Sync worker
        private BackgroundWorker sycnDataWorker = new BackgroundWorker();
        private OnRefreshComplete syncResult;


        public MyVaultRepo()
        {
            sycnDataWorker.WorkerReportsProgress = false;
            sycnDataWorker.WorkerSupportsCancellation = true;
            sycnDataWorker.DoWork += SyncData_Handler;
            sycnDataWorker.RunWorkerCompleted += SyncDataCompleted_Handler;
        }

        public override string RepoDisplayName { get => FileSysConstant.MYVAULT; set => new NotImplementedException(); }
        public override string RepoType => FileSysConstant.MYVAULT;
        public override string RepoId { get => "0"; }

        public override IList<INxlFile> GetFilePool()
        {
            return FilePool;
        }

        public IList<INxlFile> GetAllData()
        {
            SkydrmApp.Singleton.Log.Info("Get MyVault all files from DB.");

            IList<INxlFile> results = new List<INxlFile>();
            InnerGetFilesFromDB(results);

            // store file object pool
            FilePool = results;

            return results;
        }

        /// <summary>
        /// Inner impl to get files from DB, including local added file and remote file nodes.
        /// </summary>
        /// <param name="pathId"></param>
        /// <returns></returns>
        private void InnerGetFilesFromDB(IList<INxlFile> results)
        {
            try
            {
                foreach (var one in GetPendingFilesFromDB())
                {
                    results.Add(one);
                }

                foreach (var one in SkydrmApp.Singleton.MyVault.List())
                {
                    var doc = new MyVaultRmsDoc(one);
                    results.Add(doc);

                    // Submit the file that downloading failed 
                    // (may caused by crash, killed when downloading) into task queue. -- restore download
                    if (doc.FileStatus == EnumNxlFileStatus.Downloading)
                    {
                        DownloadManager.GetSingleton().SubmitToTaskQueue(doc);
                    }
                }
            }
            catch (Exception e)
            {
                SkydrmApp.Singleton.Log.Error("Invoke MyVault GetAllData failed." + e.Message, e);
            }

        }

        /// <summary>
        /// Get myvault local files from db
        /// </summary>
        private IList<INxlFile> GetPendingFilesFromDB()
        {
            IList<INxlFile> ret = new List<INxlFile>();
            try
            {
                foreach (var one in SkydrmApp.Singleton.MyVault.ListLocalAdded())
                {
                    var doc = new PendingUploadFile(one);
                    ret.Add(doc);
                }
            }
            catch (Exception e)
            {
                SkydrmApp.Singleton.Log.Error("Invoke MyVault ListLocalFile failed." + e, e);
            }
            return ret;
        }

        /// <summary>
        /// Now myVault don't have folder, will get all files from DB. inculding rms files and addLocaled files 
        /// </summary>
        /// <returns></returns>
        public override IList<INxlFile> GetWorkingFolderFilesFromDB()
        {
            IList<INxlFile> results = new List<INxlFile>();

            InnerGetFilesFromDB(results);

            return results;
        }


        #region Sync file from rms
        public override void SyncFiles(OnRefreshComplete callback, string itemFlag = null)
        {
            this.syncResult = callback;
            if (!sycnDataWorker.IsBusy)
            {
                IsLoading = true;
                sycnDataWorker.RunWorkerAsync();
            }
        }

        private void SyncData_Handler(object sender, DoWorkEventArgs args)
        {
            bool bSucess = true;
            IList<INxlFile> ret = new List<INxlFile>();
            FilePool.Clear();

            try
            {
                foreach (var one in SkydrmApp.Singleton.MyVault.Sync())
                {
                    MyVaultRmsDoc doc = new MyVaultRmsDoc(one);
                    ret.Add(doc);
                    FilePool.Add(doc);
                }
            }
            catch (Exception e)
            {
                bSucess = false;
                SkydrmApp.Singleton.Log.Error("Invoke MyVault SyncFile failed."+ e.ToString());

                // Handler session expiration
                GeneralHandler.TryHandleSessionExpiration(e);
            }

            args.Result = bSucess;
        }

        private void SyncDataCompleted_Handler(object sender, RunWorkerCompletedEventArgs args)
        {
            IsLoading = false;

            bool bResult = (bool)args.Result;
            this.syncResult?.Invoke(bResult, FilePool, "/");
        }

        /// <summary>
        /// Sync the specify file from rms to check if it is modified or not, apply for overwrite.
        /// </summary>
        public override void SyncDestFile(INxlFile selectedFile, OnSyncDestComplete callback, bool bNeedFindParent = false)
        {
            AsyncHelper.RunAsync(()=>
            {
                bool bSucess = true;
                IList<INxlFile> ret = new List<INxlFile>();
                FilePool.Clear();

                try
                {
                    foreach (var one in SkydrmApp.Singleton.MyVault.Sync())
                    {
                        MyVaultRmsDoc doc = new MyVaultRmsDoc(one);
                        ret.Add(doc);
                        FilePool.Add(doc);
                    }
                }
                catch (Exception e)
                {
                    bSucess = false;
                    SkydrmApp.Singleton.Log.Error("Invoke MyVault SyncDestFile failed." + e.ToString());

                    // Handler session expiration
                    GeneralHandler.TryHandleSessionExpiration(e);
                }

                return bSucess;
            }, 

            (rt) => 
            {
                // find the specify updated node
                INxlFile updatedNode = null;
                if (rt)
                {
                    foreach (var one in FilePool)
                    {
                        // Can't compare by INxlFile.Equal, since the duid is different for overwrite of the same name file.
                        if (selectedFile != null && selectedFile.Name == one.Name)
                        {
                            updatedNode = one;
                            break;
                        }
                    }
                }

                callback?.Invoke(rt, updatedNode);
            });
        }

        #endregion // Sync file from rms.


        #region // Download
        public override void DownloadFile(INxlFile nxlFile, bool isViewOnly, OnDownloadComplete callback, bool isDownloadPartial = false, bool isOnlineView = false)
        {
            Console.WriteLine("||||||| DownloadFile, isWownloadPartial is : ----- " + isDownloadPartial.ToString());

            DownloadManager.GetSingleton()
             .SubmitToTaskQueue(nxlFile)
             .TryDownload(callback, isViewOnly, isDownloadPartial, isOnlineView);
        }

        // Discard
        private void Download_Handler(object sender, DoWorkEventArgs args)
        {
            DownloadConfig para = (DownloadConfig)args.Argument;

            MyVaultRmsDoc doc = para.File as MyVaultRmsDoc;
            IMyVaultFile file = doc.MyVaultFile;

            bool ret = true;
            try
            {
                file.Download();

                doc.LocalPath = file.Nxl_Local_Path;
            }
            catch (Exception e)
            {
                ret = false;
                SkydrmApp.Singleton.Log.Error(e.ToString(),e);
            }
            finally
            {
                // un-register
                para.BackgroundWorker.DoWork -= Download_Handler;

                para.IsSuccess = ret;
                args.Result = para;
            }

        }

        private void DownloadCompleted_Handler(object sender, RunWorkerCompletedEventArgs args)
        {
            DownloadConfig para = (DownloadConfig)args.Result;

            para.BackgroundWorker.RunWorkerCompleted -= DownloadCompleted_Handler;

            para.Callback?.Invoke(para.IsSuccess);
        }

        #endregion // End for download.


        public override void MarkOffline(INxlFile nxlFile, OnDownloadComplete callback)
        {
            // todo, need to check file if has been in localddd
            DownloadFile(nxlFile, false, callback);
        }

        public override IList<INxlFile> GetOfflines()
        {
            IList<INxlFile> rt = new List<INxlFile>();
            foreach (var i in SkydrmApp.Singleton.MyVault.GetOfflines())
            {
                if(i is MyVaultFile)
                {
                    var of = new MyVaultRmsDoc((MyVaultFile)i);
                    rt.Add(of);
                } else if(i is SharedWithMeFile)
                {
                    var of = new SharedWithDoc((SharedWithMeFile)i);
                    rt.Add(of);
                }               
            }
            return rt;
        }

        public override IList<INxlFile> GetPendingUploads()
        {
            IList<INxlFile> rt = new List<INxlFile>();
            foreach (var i in SkydrmApp.Singleton.MyVault.GetPendingUploads())
            {
                var pf = new PendingUploadFile(i);

                rt.Add(pf);
            }
            return rt;
        }

        public override IList<INxlFile> GetSharedByMeFiles()
        {
            IList<INxlFile> ret = new List<INxlFile>();
            try
            {
                IMyVaultFile[] files = SkydrmApp.Singleton.MyVault.List();
                foreach(var one in files)
                {
                    if(one.Is_Shared && !one.Is_Revoked) // If file has been revoked, won't display it.
                    {
                        ret.Add(new MyVaultRmsDoc(one));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return ret;
        }

        public override IList<INxlFile> GetSharedWithMeFiles()
        {
            IList<INxlFile> ret = new List<INxlFile>();
            try
            {
                ISharedWithMeFile[] files = SkydrmApp.Singleton.SharedWithMe.List();
                foreach(var one in files)
                {
                    ret.Add(new SharedWithDoc(one));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return ret;
        }

        public override void SyncSharedWithMeFiles(OnSyncComplete callback)
        {
            // Async worker
            Func<object> asyncTask = new Func<object>(() => {

                bool bSuc = true;
                List<INxlFile> ret = new List<INxlFile>();
                try
                {
                    ISharedWithMeFile[] files = SkydrmApp.Singleton.SharedWithMe.Sync();
                    foreach(var one in files)
                    {
                        ret.Add(new SharedWithDoc(one));
                    }
                }
                catch (Exception e)
                {
                    bSuc = false;
                    SkydrmApp.Singleton.Log.Error("Invoke myVault SyncSharedWithMeFiles failed.");
                }

                return new RetValue(bSuc, ret);
            });

            // Async callback
            Action<object> cb = new Action<object>((rt) => {
                RetValue rtValue = (RetValue)rt;
                callback?.Invoke(rtValue.IsSuc, rtValue.results);
            });

            // Invoke
            AsyncHelper.RunAsync(asyncTask, cb);
        }

        public override void SyncSharedByMeFiles(OnSyncComplete callback)
        {
            // Async worker
            Func<object> asyncTask = new Func<object>(() => {

                bool bSuc = true;
                List<INxlFile> ret = new List<INxlFile>();
                try
                {
                    var files = SkydrmApp.Singleton.MyVault.Sync();
                    foreach(var one in files)
                    {
                        if (one.Is_Shared)
                        {
                         ret.Add(new MyVaultRmsDoc(one));    
                        }
                    }
                }
                catch (Exception e)
                {
                    bSuc = false;
                    SkydrmApp.Singleton.Log.Error("Invoke myvault SyncSharedByMeFiles failed.");
                }

                return new RetValue(bSuc, ret);
            });

            // Async callback
            Action<object> cb = new Action<object>((rt) => {
                RetValue rtValue = (RetValue)rt;
                callback?.Invoke(rtValue.IsSuc, rtValue.results);
            });

            // Invoke
            AsyncHelper.RunAsync(asyncTask, cb);
        }

        // This will be invoke from RootViewModel#Refresh#AsyncRefresh() when user access myVault 
        // by clicking mySpace viewFiles.
        public override void SyncFilesRecursively(string pathId, IList<INxlFile> results)
        {
            try
            {
                IMyVaultFile[] files = SkydrmApp.Singleton.MyVault.Sync();
                foreach (var f in files)
                {
                    results.Add(new MyVaultRmsDoc(f));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                SkydrmApp.Singleton.Log.Error("Invoke SyncFilesRecursively failed.");
                throw;
            }
        }

        public override void SyncParentNodeFile(INxlFile nxl, ref IList<INxlFile> results)
        {
            try
            {
                IMyVaultFile[] files = SkydrmApp.Singleton.MyVault.Sync();
                foreach (var f in files)
                {
                    results.Add(new MyVaultRmsDoc(f));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                SkydrmApp.Singleton.Log.Error("Invoke SyncParentNodeFile failed.");
                throw;
            }
        }

        // Special design callback and ui-notifying, for show shared-emails and allow UI to bind it.
        public void NotifySharedRecipentsChanged(INxlFile nxlFile, string[] newEmails)
        {
            // sanity check
            if (nxlFile == null)
            {
                return;
            }
            if (nxlFile.FileRepo != EnumFileRepo.REPO_MYVAULT)
            {
                return;
            }
            if (newEmails.Length == 0)
            {
                return;
            }
            if (nxlFile is MyVaultRmsDoc)
            {
                var doc = (MyVaultRmsDoc)nxlFile;
                doc.ChangeSharedWithList(newEmails);
                return;
            }
            if (nxlFile is PendingUploadFile)
            {
                var doc = (PendingUploadFile)nxlFile;
                doc.ChangeSharedWithList(newEmails);
                return;
            }

        }

        public void NotifySharedRecipentsChanged(INxlFile nxlFile)
        {
            // sanity check
            if (nxlFile == null)
            {
                return;
            }
            if (nxlFile.FileRepo != EnumFileRepo.REPO_MYVAULT)
            {
                return;
            }
 
            if (nxlFile is MyVaultRmsDoc)
            {
                var doc = (MyVaultRmsDoc)nxlFile;
                var metData = doc.GetMetaData();
                string[] newEmails = metData.recipents.ToArray();
                doc.ChangeSharedWithList(newEmails);
                return;
            }
        
        }

        public override bool CheckFileExists(string pathId)
        {
            return SkydrmApp.Singleton.MyVault.CheckFileExists(pathId);
        }

        private sealed class RetValue
        {
            public bool IsSuc { get; }
            public List<INxlFile> results { get; }

            public RetValue(bool isSuc, List<INxlFile> rt)
            {
                this.IsSuc = isSuc;
                this.results = rt;
            }
        }

        private class DownloadConfig
        {
            public INxlFile File { get; set; }

            public bool IsSuccess { get; set; }
            public OnDownloadComplete Callback { get; set; }
            public BackgroundWorker BackgroundWorker { get; set; }

            public DownloadConfig(INxlFile nxlFile, OnDownloadComplete callback, BackgroundWorker worker, bool isSuccess)
            {
                // paras
                Callback = callback;

                File = nxlFile;
                BackgroundWorker = worker;
                IsSuccess = isSuccess;
            }

        }

        private class GetLocalsConfig
        {
            public bool IsSucess { get; set; }

            public BackgroundWorker listWorker { get; set; }

            public OnGetLocalsComplete callback { get; set; }

            public IList<INxlFile> results { get; set; }

            public GetLocalsConfig(OnGetLocalsComplete callback, bool isSuccess)
            {
                this.callback = callback;
                this.IsSucess = isSuccess;
            }
        }

    }


    public sealed class MyVaultRmsDoc: NxlDoc
    {
        public IMyVaultFile MyVaultFile { get; set; }

        public MyVaultRmsDoc(IMyVaultFile myVaultFile)
        {
            this.MyVaultFile = myVaultFile;

            this.Name = myVaultFile.Nxl_Name;
            this.Size = myVaultFile.FileSize;
            // In order to sort, we'll get timesStap format, then will convert dateTime again when display in ui.
            this.DateModified = DateTimeHelper.DateTimeToTimestamp(myVaultFile.Last_Modified_Time.ToLocalTime()).ToString();
            this.RawDateModified = myVaultFile.Last_Modified_Time;
            this.Location = (myVaultFile.Is_Offline || myVaultFile.Status == EnumNxlFileStatus.AvailableOffline) ? EnumFileLocation.Local : EnumFileLocation.Online;
            this.SharedWith = StringHelper.ConvertString2List(myVaultFile.Shared_With_List); 

            this.LocalPath = myVaultFile.Nxl_Local_Path;
            this.DisplayPath = myVaultFile.Display_Path;
            this.PathId = myVaultFile.Path_Id;

            this.IsMarkedOffline = myVaultFile.Is_Offline;
            this.FileStatus = myVaultFile.Status;
            this.FileRepo = EnumFileRepo.REPO_MYVAULT;
            this.IsCreatedLocal = false;
            this.FileId = "";

            // bind partial local path.
            this.PartialLocalPath = myVaultFile.Partial_Local_Path;
            this.Duid = myVaultFile.Duid;
            this.IsEdit = myVaultFile.Is_Edit;

            this.SourcePath = "SkyDRM://" + FileSysConstant.MYVAULT + "/" + Name; ;
        }

        public override EnumNxlFileStatus FileStatus
        {
            get
            {
                return MyVaultFile.Status;
            }

            set
            {
                MyVaultFile.Status = value;
                if (value == EnumNxlFileStatus.Online)
                {
                    //this.LocalPath = ""; 
                }
                NotifyPropertyChanged("FileStatus");
            }
        }

        public override bool IsEdit
        {
            get
            {
                return MyVaultFile.Is_Edit;
            }

            set
            {
                MyVaultFile.Is_Edit = value;
                NotifyPropertyChanged("IsEdit");
            }
        }

        public override bool IsMarkedFileRemoteModified
        {
            get
            {
                return MyVaultFile.Is_Dirty;
            }

            set
            {
                MyVaultFile.Is_Dirty = value;
                NotifyPropertyChanged("IsMarkedFileRemoteModified");
            }
        }

        public override IFileInfo FileInfo => MyVaultFile.FileInfo;

        public void ChangeSharedWithList(string[] emails)
        {
            // update into db
            MyVaultFile?.ChangeSharedWithList(emails);

            // Tell UI to notify
            this.SharedWith = StringHelper.ConvertString2List(MyVaultFile.Shared_With_List);
            NotifyPropertyChanged("SharedWith");
        }

        public MyVaultMetaData GetMetaData()
        {
            if (MyVaultFile == null)
            {
                throw new Exception("Argument MyVaultFile founded null when invoke GetMyVaultMetaData.");
            }
            return MyVaultFile.GetMetaData();
        }

        // Delete the local file, now looks not used.
        public override void Remove()
        {
            MyVaultFile?.Remove();
        }

        public override bool UnMark()
        {
            try
            {
                MyVaultFile?.Remove();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }
            return true;
        }

        public override void DownloadFile(bool isViewOnly = false)
        {
            MyVaultFile?.Download(isViewOnly);
            // Update localPath after download
            LocalPath = MyVaultFile?.Nxl_Local_Path;
        }

        public override void DownloadPartial()
        {
            //MyVaultFile?.DownloadPartial();
            MyVaultFile?.GetNxlHeader();
            PartialLocalPath = MyVaultFile?.Partial_Local_Path;
        }

        public override void Export(string destFolder)
        {
            MyVaultFile?.Export(destFolder);
        }

    }

    public sealed class SharedWithDoc : NxlDoc
    {
        public ISharedWithMeFile Raw { get; set; }

        public SharedWithDoc(ISharedWithMeFile raw)
        {
            this.Raw = raw;

            this.Name = raw.Name;
            this.Size = raw.FileSize;
            // In order to sort, we'll get timesStap format, then will convert dateTime again when display in ui.
            this.DateModified = DateTimeHelper.DateTimeToTimestamp(raw.SharedDate.ToLocalTime()).ToString();
            this.RawDateModified = raw.SharedDate; // Display "Shared date"
            this.SharedBy = raw.SharedBy;

            this.Location = (raw.Status == EnumNxlFileStatus.AvailableOffline || raw.IsOffline) ?
                            EnumFileLocation.Local : EnumFileLocation.Online;

            this.LocalPath = raw.LocalDiskPath;
            this.DisplayPath = "/" + raw.Name;
            this.PathId = DisplayPath;

            this.FileStatus = raw.Status;
            this.FileRepo = EnumFileRepo.REPO_SHARED_WITH_ME;
            this.IsCreatedLocal = false;
            this.FileId = "";

            // bind partial local path.
            this.PartialLocalPath = raw.PartialLocalPath;

            this.IsMarkedOffline = raw.IsOffline;
            this.IsEdit = raw.IsEdit;

            this.SourcePath = "SkyDRM://" + FileSysConstant.SHAREDWITHME + "/" + Name; 
        }

        public override EnumNxlFileStatus FileStatus
        {
            get
            {
                return Raw.Status;
            }

            set
            {
                Raw.Status = value;
                if (value == EnumNxlFileStatus.Online)
                {
                    //this.LocalPath = "";
                }
                NotifyPropertyChanged("FileStatus");
            }
        }

        public override bool IsEdit
        {
            get
            {
                return Raw.IsEdit;
            }

            set
            {
                Raw.IsEdit = value;
                NotifyPropertyChanged("IsEdit");
            }
        }

        public override IFileInfo FileInfo => Raw.FileInfo;

        public string TransactionId
        {
            get
            {
                return Raw.TransactionId;
            }
        }

        public override bool UnMark()
        {
            try
            {
                Remove();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return false;
            }

            return true;
        }

        public override void Remove()
        {
            Raw?.Remove();
        }

        public override void DownloadFile(bool isViewOnly = false)
        {
            // fix bug 57706 - sometimes mark file offline will fail.
            // It shall pass "bForViewer=true" to mark the file offline for "SharedWithMe".
            Raw?.Download(true);
            // Update
            LocalPath = Raw?.LocalDiskPath;
        }

        public override void DownloadPartial()
        {
            Raw?.DownloadPartial();
            PartialLocalPath = Raw?.PartialLocalPath;
        }

        public override void Export(string destFolder)
        {
            Raw?.Export(destFolder);
        }

    }

}
