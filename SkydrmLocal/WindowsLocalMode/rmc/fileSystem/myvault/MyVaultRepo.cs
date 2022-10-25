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

namespace SkydrmLocal.rmc.fileSystem.myvault
{
    public class MyVaultRepo : IFileRepo
    {
        private static readonly string MYVAULT = CultureStringInfo.MainWin__TreeView_MyVault;

        // For all myVault files
        private IList<INxlFile> filePool = new List<INxlFile>();
        public IList<INxlFile> FilePool { get => filePool; }
        //Flag that is loading data, Binding UI to display
        public bool IsLoading { get; set; }

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

        #region List files from local
        public IList<INxlFile> GetLocalFiles()
        {
            SkydrmLocalApp.Singleton.Log.Info("Get myvault local files.");

            IList<INxlFile> ret = new List<INxlFile>();
            filePool.Clear();

            try
            {
                foreach (var one in List())
                {
                    ret.Add(one);
                    filePool.Add(one);
                    // Submit the file that downloading failed 
                    // (may caused by crash, killed when downloading) into task queue. -- restore download
                    if (one.FileStatus == EnumNxlFileStatus.Downloading)
                    {
                        DownloadManager.GetSingleton().SubmitToTaskQueue(one);
                    }
                }

                foreach (var one in ListLocal())
                {
                    ret.Add(one);
                    filePool.Add(one);
                }
            }
            catch (Exception e)
            {
                SkydrmLocalApp.Singleton.Log.Error("Invoke MyVault GetAllData failed."+e.Message,e);
            }
            return ret;
        }

        private bool IsExecutingListLocals = false;

        // Get local files async way.
        public void GetLocalFiles(OnGetLocalsComplete result)
        {
            if (IsExecutingListLocals)
            {
                return;
            }

            GetLocalsConfig config = new GetLocalsConfig(result, true);
            ThreadPool.QueueUserWorkItem(new WaitCallback(GetLocalFilesWorker), config);
        }

        private void GetLocalFilesWorker(object task)
        {
            IsExecutingListLocals = true;
            GetLocalsConfig config = task as GetLocalsConfig;

            IList<INxlFile> ret = new List<INxlFile>();
            try
            {
                IList<INxlFile> lists = List();
                IList<INxlFile> listLocals = ListLocal();
                filePool.Clear();

                foreach (var one in lists)
                {
                    ret.Add(one);
                    filePool.Add(one);

                    // Submit the file that downloading failed (may caused by crash, killed when downloading) into task queue. -- restore download
                    if (one.FileStatus == EnumNxlFileStatus.Downloading)
                    {
                        DownloadManager.GetSingleton().SubmitToTaskQueue(one);
                    }
                }

                foreach (var one in listLocals)
                {
                    ret.Add(one);
                    filePool.Add(one);
                }
            }
            catch (Exception e)
            {
                config.IsSucess = false;
                SkydrmLocalApp.Singleton.Log.Error("Invoke MyVault GetAllData failed."+e.Message,e);
            }
            finally
            {
                IsExecutingListLocals = false;
                config?.callback.Invoke(config.IsSucess, ret);
            }

        }

        /// <summary>
        /// Get myvault remote files from db
        /// </summary>
        public IList<INxlFile> List()
        {
            var ret = new List<INxlFile>();
            try
            {
                foreach (var one in SkydrmLocalApp.Singleton.MyVault.List())
                {
                    var doc = new MyVaultRmsDoc(one);
                    ret.Add(doc);
                }
            }
            catch (Exception e)
            {
                SkydrmLocalApp.Singleton.Log.Error("Invoke MyVault ListFile failed."+e,e);
            }
            return ret;
        }

        /// <summary>
        /// Get myvault local files from db
        /// </summary>
        public IList<INxlFile> ListLocal()
        {
            IList<INxlFile> ret = new List<INxlFile>();
            try
            {
                foreach (var one in SkydrmLocalApp.Singleton.MyVault.ListLocalAdded())
                {
                    var doc = new PendingUploadFile(one);
                    ret.Add(doc);
                }
            }
            catch (Exception e)
            {
                SkydrmLocalApp.Singleton.Log.Error("Invoke MyVault ListLocalFile failed."+e,e);
            }
            return ret;
        }
        #endregion // List files from local.


        #region Sync file from rms
        public void SyncFiles(OnRefreshComplete callback, string itemFlag = null)
        {
            this.syncResult = callback;
            if (!sycnDataWorker.IsBusy)
            {
                sycnDataWorker.RunWorkerAsync();
            }
        }

        private void SyncData_Handler(object sender, DoWorkEventArgs args)
        {
            bool bSucess = true;
            IList<INxlFile> ret = new List<INxlFile>();
            filePool.Clear();

            try
            {
                foreach (var one in SkydrmLocalApp.Singleton.MyVault.Sync())
                {
                    MyVaultRmsDoc doc = new MyVaultRmsDoc(one);
                    ret.Add(doc);
                    filePool.Add(doc);
                }
            }
            catch (Exception e)
            {
                bSucess = false;
                SkydrmLocalApp.Singleton.Log.Error("Invoke MyVault SyncFile failed." + e.ToString());
            }

            args.Result = bSucess;
        }

        private void SyncDataCompleted_Handler(object sender, RunWorkerCompletedEventArgs args)
        {
            bool bResult = (bool)args.Result;
            this.syncResult?.Invoke(bResult, filePool, null);
        }

        public void SyncDestFile(INxlFile selectedFile, OnSyncDestComplete result, bool bNeedFindParent = false)
        {
            throw new NotImplementedException();
        }

        #endregion // Sync file from rms.


        #region // Download
        public void DownloadFile(INxlFile nxlFile, bool isViewOnly, OnDownloadComplete callback, bool isDownloadPartial = false, bool isOnlineView = false)
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
                SkydrmLocalApp.Singleton.Log.Error(e.ToString(),e);
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


        public void MarkOffline(INxlFile nxlFile, OnDownloadComplete callback)
        {
            // todo, need to check file if has been in localddd
            DownloadFile(nxlFile, false, callback);
        }

        public bool UnmarkOffline(INxlFile nxlFile)
        {
            try
            {
                nxlFile.Remove();
                return true;
            }
            catch (Exception e)
            {
                SkydrmLocalApp.Singleton.Log.Error(" UnmarkOffline failed!"+e.Message, e);
            }

            return false;
        }

        public IList<INxlFile> GetOfflines()
        {
            IList<INxlFile> rt = new List<INxlFile>();
            foreach (var i in SkydrmLocalApp.Singleton.MyVault.GetOfflines())
            {
                var of = new MyVaultRmsDoc((MyVaultFile)i);
                of.SourcePath = GetSourcePath(of);

                rt.Add(of);
            }
            return rt;
        }

        public IList<INxlFile> GetPendingUploads()
        {
            IList<INxlFile> rt = new List<INxlFile>();
            foreach (var i in SkydrmLocalApp.Singleton.MyVault.GetPendingUploads())
            {
                var pf = new PendingUploadFile(i as IMyVaultLocalFile);
                pf.SourcePath = GetSourcePath(pf);

                rt.Add(pf);
            }
            return rt;
        }

        public string GetSourcePath(INxlFile nxlfile)
        {
            return MYVAULT + "://" + nxlfile.Name;
        }

        public void UpdateToRms(INxlFile nxlFile)
        {
            throw new NotImplementedException();
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

        public void Export(string destFolder,INxlFile nxlFile)
        {
            if (nxlFile is MyVaultRmsDoc)
            {
                var doc = (MyVaultRmsDoc)nxlFile;
                doc.MyVaultFile.Export(destFolder);
            }           
        }

        public void Edit(INxlFile nxlFile, Action<EditCallBack> onFinishedCallback)
        {
            throw new NotImplementedException();
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
            this.DateModified = CommonUtils.DateTimeToTimestamp(myVaultFile.Last_Modified_Time.ToLocalTime()).ToString();
            this.RawDateModified = myVaultFile.Last_Modified_Time;
            this.Location = (myVaultFile.Is_Offline || myVaultFile.Status == EnumNxlFileStatus.AvailableOffline) ? EnumFileLocation.Local : EnumFileLocation.Online;
            this.SharedWith = myVaultFile.Shared_With_List;

            this.LocalPath = myVaultFile.Nxl_Local_Path;
            this.RmsRemotePath = myVaultFile.Display_Path;

            this.IsMarkedOffline = myVaultFile.Is_Offline;
            this.FileStatus = myVaultFile.Status;
            this.FileRepo = EnumFileRepo.REPO_MYVAULT;
            this.IsCreatedLocal = false;
            this.Emails = myVaultFile.Shared_With_List.Split(new char[] { ' ', ';', ',' });
            this.FileId = "";

            // bind partial local path.
            this.PartialLocalPath = myVaultFile.Partial_Local_Path;

            this.Duid = myVaultFile.Duid;

            this.IsEdit = myVaultFile.Is_Edit;
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

        public override IFingerPrint FingerPrint
        {
            get
            {
                return new InnerFingerPrint(MyVaultFile?.FileInfo);
            }
        }

        public override IFileInfo FileInfo => MyVaultFile.FileInfo;


        public override void Remove()
        {
            MyVaultFile?.Remove();
        }

        public void ChangeSharedWithList(string[] emails)
        {
            MyVaultFile?.ChangeSharedWithList(emails);

            this.SharedWith = MyVaultFile.Shared_With_List;
            this.Emails= MyVaultFile.Shared_With_List.Split(new char[] { ' ', ';', ',' });
            // Tell UI to notify
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
    }


    class InnerFingerPrint : IFingerPrint
    {
        private IFileInfo fileInfo;
        public InnerFingerPrint(IFileInfo fileInfo)
        {
            this.fileInfo = fileInfo;
        }

        public FileRights[] Rights => fileInfo.Rights;

        public Dictionary<string, List<string>> Tags => fileInfo.Tags;

        public string WaterMark => fileInfo.WaterMark;

        public Expiration Expiration => fileInfo.Expiration;

        public string RawTags => fileInfo.RawTags;
    }

}
