using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmLocal.rmc.common.component;
using SkydrmLocal.rmc.Edit;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.featureProvider.common;
using SkydrmLocal.rmc.featureProvider.SharedWithMe;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui.utils;

namespace SkydrmLocal.rmc.fileSystem.sharedWithMe
{
    public class ShareWithRepo : IFileRepo
    {
        private readonly SkydrmLocalApp App = SkydrmLocalApp.Singleton;
        // files
        private IList<INxlFile> filePool = new List<INxlFile>();
        public IList<INxlFile> FilePool { get => filePool; }
        // Sync worker
        private BackgroundWorker sycnDataWorker = new BackgroundWorker();
        private OnRefreshComplete syncResult;

        //Flag that is loading data, Binding UI to display
        public bool IsLoading { get; set; }

        public ShareWithRepo()
        {
            sycnDataWorker.WorkerReportsProgress = false;
            sycnDataWorker.WorkerSupportsCancellation = true;
            sycnDataWorker.DoWork += SyncData_Handler;
            sycnDataWorker.RunWorkerCompleted += SyncDataCompleted_Handler;
        }
               
        /// <summary>
        /// Get shard with me files from rms, should invoke in db thread.
        /// </summary>
        /// 
        public void SyncFiles(OnRefreshComplete callback, string itemFlag = null)
        {
            this.syncResult = callback;
            if (!sycnDataWorker.IsBusy)
            {
                sycnDataWorker.RunWorkerAsync();
            }
        }

        /// <summary>
        /// Get shard with me files from db
        /// </summary>
        public IList<INxlFile> GetLocalFiles()
        {
            SkydrmLocalApp.Singleton.Log.Info("Get sharedWithme local files.");

            IList<INxlFile> ret = new List<INxlFile>();
            filePool.Clear();

            try
            {
                foreach (var one in App.SharedWithMe.List())
                {
                    SharedWithDoc doc = new SharedWithDoc(one);

                    if (doc.FileStatus == EnumNxlFileStatus.Online)
                    {
                        doc.Location = EnumFileLocation.Online;
                    }

                    ret.Add(doc);
                    filePool.Add(doc);

                    // Submit the file that downloading failed (may caused by crash, killed when downloading) into task queue. -- restore download
                    if (doc.FileStatus == EnumNxlFileStatus.Downloading)
                    {
                        DownloadManager.GetSingleton().SubmitToTaskQueue(doc);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                App.Log.Error("Invoke SharedWithMe ListFile failed.");
            }

            return ret;
        }

        private void SyncData_Handler(object sender, DoWorkEventArgs args)
        {
            IList<INxlFile> ret = new List<INxlFile>();
            filePool.Clear();

            bool bSucess = true;
            try
            {
                foreach (var one in App.SharedWithMe.Sync())
                {
                    SharedWithDoc doc = new SharedWithDoc(one);

                    if (doc.FileStatus == EnumNxlFileStatus.Online)
                    {
                        doc.Location = EnumFileLocation.Online;
                    }

                    ret.Add(doc);
                    filePool.Add(doc);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                App.Log.Error("Invoke SharedWithMe SyncFile failed." + ToString());
                bSucess = false;
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

        #region // For download
        public void DownloadFile(INxlFile nxlFile, bool isViewOnly, OnDownloadComplete callback, bool isDownloadPartial = false, bool isOnlineView = false)
        {

            DownloadManager.GetSingleton()
                .SubmitToTaskQueue(nxlFile)
                .TryDownload(callback, isViewOnly, isDownloadPartial, isOnlineView);
        }

        private void Download_Handler(object sender, DoWorkEventArgs args)
        {
            DownloadConfig para = (DownloadConfig)args.Argument;

            SharedWithDoc nxlShareWithDoc = para.File as SharedWithDoc;

            ISharedWithMeFile file = nxlShareWithDoc.Raw;

            bool ret = true;
            try
            {
                file.Download();

                nxlShareWithDoc.LocalPath = file.LocalDiskPath;
            }
            catch (Exception e)
            {
                App.Log.Error(e.ToString());
                ret = false;
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

        

        public  void MarkOffline(INxlFile nxlFile, OnDownloadComplete callback)
        {
            // todo, need to check file if has been in local
            // modify offline downlod file as IsView=true, 
            DownloadFile(nxlFile, true, callback);
        }

        public  bool UnmarkOffline(INxlFile nxlFile)
        {
            try
            {
                nxlFile.Remove();
                return true;
            }
            catch (Exception e)
            {
                App.Log.Error(" UnmarkOffline failed!", e);
            }

            return false;
        }

        public IList<INxlFile> GetOfflines()
        {
            IList<INxlFile> rt = new List<INxlFile>();
            foreach (var i in App.SharedWithMe.GetOfflines())
            {
                var of = new SharedWithDoc((SharedWithMeFile)i);
                of.SourcePath = GetSourcePath(of);

                rt.Add(of);
            }
            return rt;
        }

        public  IList<INxlFile> GetPendingUploads()
        {
            IList<INxlFile> rt = new List<INxlFile>();

            // For sharedWithMe, don't support pending files.
            return rt;
        }

        public string GetSourcePath(INxlFile nxlfile)
        {
            return "SharedWithMe://" + nxlfile.Name;
        }

        public void UpdateToRms(INxlFile nxlFile)
        {
            throw new NotImplementedException();
        }

        public void Export(string destFolder, INxlFile nxlFile)
        {
            if(nxlFile is SharedWithDoc)
            {
                var doc = (SharedWithDoc)nxlFile;
                doc.Raw.Export(destFolder);
            }
        }

        public void Edit(INxlFile nxlFile, Action<EditCallBack> onFinishedCallback)
        {
            throw new NotImplementedException();
        }

        class DownloadConfig
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


    }

    public sealed class SharedWithDoc: NxlDoc
    {
        public ISharedWithMeFile Raw { get; set; }

        public SharedWithDoc(ISharedWithMeFile raw)
        {
            this.Raw = raw;

            this.Name = raw.Name;
            this.Size = raw.FileSize;
            // In order to sort, we'll get timesStap format, then will convert dateTime again when display in ui.
            this.DateModified = CommonUtils.DateTimeToTimestamp(raw.SharedDate.ToLocalTime()).ToString();
            this.RawDateModified = raw.SharedDate; // Display "Shared date"
            this.SharedWith = raw.SharedBy;  // This SharedWith in UI display "SharedBy" column

            this.Location = (raw.Status == EnumNxlFileStatus.AvailableOffline || raw.IsOffline) ?
                            EnumFileLocation.Local : EnumFileLocation.Online;

            this.LocalPath = raw.LocalDiskPath;
            this.RmsRemotePath = "/" + raw.Name;

            this.FileStatus = raw.Status;
            this.FileRepo = EnumFileRepo.REPO_SHARED_WITH_ME;
            this.IsCreatedLocal = false;
            this.Emails = new string[1] { raw.SharedBy };
            this.FileId = "";

            // bind partial local path.
            this.PartialLocalPath = raw.PartialLocalPath;

            this.IsMarkedOffline = raw.IsOffline;
            this.IsEdit = raw.IsEdit;
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

        public override IFingerPrint FingerPrint
        {
            get
            {
                return new InnerFingerPrint(Raw.FileInfo);
            }
        }

        public override IFileInfo FileInfo => Raw.FileInfo;

        public override void Remove()
        {
            Raw?.Remove();
        }

        public override void Upload()
        {
            // Should never reach here, if yes, must be a bug
            throw new NotImplementedException("should never reach here, if yes, must be a bug");
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
