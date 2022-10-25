using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.common.helper;
using SkydrmDesktop.rmc.featureProvider;
using SkydrmDesktop.rmc.featureProvider.WorkSpace;
using SkydrmDesktop.rmc.fileSystem.utils;
using SkydrmLocal.rmc.common.component;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.Edit;
using SkydrmLocal.rmc.exception;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.featureProvider.common;
using SkydrmLocal.rmc.fileSystem;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui.utils;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.viewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.fileSystem.workspace
{
    // Sync rms workspace data complete
    public delegate void OnSyncWorkSpaceComplete(bool bSucceed, IList<INxlFile> results);

    public class WorkSpaceRepo : AbstractFileRepo
    {
        private readonly SkydrmApp App = SkydrmApp.Singleton;

        // WorkSpace file pool:
        // mainly used for treeview model data binding.
        private IList<INxlFile> FilePool { get; set; }

        // Flag that if is executing sync all data from rms.
        public bool IsLoading { get; private set; }

        public WorkSpaceRepo()
        {
            FilePool = new List<INxlFile>();
        }

        public override string RepoDisplayName { get => FileSysConstant.WORKSPACE; set => new NotImplementedException(); }
        public override string RepoType => FileSysConstant.WORKSPACE;

        public override IList<INxlFile> GetFilePool()
        {
            return FilePool;
        }

        /// <summary>
        /// Get all files (including remote nodes and local created files) in local db.
        /// </summary>
        /// <returns></returns>
        public IList<INxlFile> GetAllData()
        {
            App.Log.Info("Get WorkSpace all files form DB.");

            IList<INxlFile> rt = new List<INxlFile>();

            try
            {
                var files = App.WorkSpace.ListAll(true);
                InnerGetAllFilesRecursivelyFromDB(files, "/", rt);
            }
            catch (Exception e)
            {
                App.Log.Error("Get WorkSpace all files from DB failed.", e);
            }

            // store file object pool
            FilePool = rt;

            return rt;
        }

        /// <summary>
        /// Inner impl to get files recursively from DB, including local protected file and remote file nodes.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="results"></param>
        private void InnerGetAllFilesRecursivelyFromDB(IWorkSpaceFile[] files, string pathId, IList<INxlFile> results)
        {
            // Get local created files from db.
            foreach (var one in GetPendingFilesFromDB(pathId))
            {
                results.Add(one);
            }

            foreach (var f in files)
            {
                if (FileHelper.IsDirectChild(f.Path_Id, pathId))
                {
                    if (f.Is_Folder)
                    {
                        NxlFolder folder = new WorkSpaceFolder(f);
                        //  Recusively get the foler children.
                        folder.Children = new List<INxlFile>();
                        InnerGetAllFilesRecursivelyFromDB(files, folder.PathId, folder.Children);

                        results.Add(folder);
                    }
                    else
                    {
                        var doc = new WorkSpaceRmsDoc(f);
                        results.Add(doc);

                        // Submit the file that downloading failed (may caused by crash, killed when downloading) into task queue.
                        // -- restore download
                        if (doc.FileStatus == EnumNxlFileStatus.Downloading)
                        {
                            DownloadManager.GetSingleton().SubmitToTaskQueue(doc);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Inner impl to get files from DB, including local protected file and remote file nodes.
        /// </summary>
        /// <param name="pathId"></param>
        /// <returns></returns>
        private void InnerGetFilesFromDB(string pathId, IList<INxlFile> results)
        {
            try
            {
                // Get local created files from db.
                foreach (var one in GetPendingFilesFromDB(pathId))
                {
                    results.Add(one);
                }

                IWorkSpaceFile[] files = App.WorkSpace.List(pathId);
                foreach (var f in files)
                {
                    if (f.Is_Folder)
                    {
                        NxlFolder folder = new WorkSpaceFolder(f);

                        results.Add(folder);
                    }
                    else
                    {
                        var doc = new WorkSpaceRmsDoc(f);
                        results.Add(doc);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                App.Log.Error("Invoke InnerGetFilesFromDB failed.", e);
            }

        }

        /// <summary>
        /// Get specified pathId's local added files 
        /// </summary>
        /// <param name="pathId"></param>
        /// <returns></returns>
        private IList<INxlFile> GetPendingFilesFromDB(string pathId)
        {
            IList<INxlFile> ret = new List<INxlFile>();
            foreach (var one in App.WorkSpace.ListLocalAdded(pathId))
            {
                ret.Add(new PendingUploadFile(one));
            }

            return ret;
        }

        #region Use for display all folder in UI, not contain files
        
        /// <summary>
        /// Get all folder nodes from local, used for select destination folder window
        /// </summary>
        /// <returns></returns>
        public IList<INxlFile> GetAllFolders()
        {
            App.Log.Info("Get WorkSpace all folders");

            IList<INxlFile> rt = new List<INxlFile>();
            try
            {
                var files = App.WorkSpace.ListAll(true);
                BuildFolders(files, "/", rt);
            }
            catch (Exception e)
            {
                App.Log.Error("Get WorkSpace all folders failed.", e);
            }

            return rt;
        }
        // Inner impl to get all local folders from cache.
        private void BuildFolders(IWorkSpaceFile[] files, string pathId, IList<INxlFile> results)
        {
            foreach (var f in files)
            {
                if (FileHelper.IsDirectChild(f.Path_Id, pathId) && f.Is_Folder)
                {
                    NxlFolder folder = new WorkSpaceFolder(f);
                    results.Add(folder);

                    // Recusively get the folder children.
                    folder.Children = new List<INxlFile>();
                    BuildFolders(files, folder.PathId, folder.Children);
                }
            }
        }
        #endregion

        #region Main public interface impl

        /// <summary>
        /// Get current working folder files from local db, including local protected file and remote file nodes.
        /// </summary>
        /// <returns></returns>
        public override IList<INxlFile> GetWorkingFolderFilesFromDB()
        {
            IList<INxlFile> rt = new List<INxlFile>();

            InnerGetFilesFromDB(GetFolderPathId(), rt);

            return rt;
        }

        /// <summary>
        /// Search specified file in File Pool by file name.
        /// </summary>
        /// <returns></returns>
        public INxlFile SearchFileInFilePool(string localPath)
        {
            INxlFile ret = null;
            foreach (var one in FilePool)
            {
                if(one.LocalPath == localPath)
                {
                    ret = one;
                }
            }

            return ret;
        }

        /// <summary>
        /// Sync workspace all files from rms.
        ///     Actually after aync, the implementation mechanism underlying is:
        ///     will merge with locals first and update into local db again then return directly locals.
        /// </summary>
        /// <param name="callback"></param>
        public void SyncAllFiles(OnSyncWorkSpaceComplete callback)
        {
            IsLoading = true;
            // Async worker
            Func<object> asyncTask = new Func<object>(()=> {

                bool bSucceed = true;
                List<INxlFile> ret = new List<INxlFile>();
                try
                {
                    SyncFilesRecursively("/", ret);
                }
                catch (Exception e)
                {
                    App.Log.Error(e.ToString());
                    bSucceed = false;
                }

                // store into file pool
                FilePool = ret;

                return new RetValue(bSucceed, ret);
            });

            // Async callback
            Action<object> cb = new Action<object>((rt) => {
                IsLoading = false;
                RetValue rtValue = (RetValue)rt;
                callback?.Invoke(rtValue.IsSuc, rtValue.results);
            });

            // Invoke
            AsyncHelper.RunAsync(asyncTask, cb); 
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

        /// <summary>
        /// Sync specified folder's all remote nodes from rms.
        /// </summary>
        /// <param name="results">callback reuslts</param>
        /// <param name="pathId">PathId, is root folder in default</param>
        public override void SyncFiles(OnRefreshComplete results, string itemFlag = null)
        {
            BackgroundWorker syncWorker = new BackgroundWorker();
            syncWorker.DoWork += SyncDataHander;
            syncWorker.RunWorkerCompleted += SyncDataCompleted;

            if (!syncWorker.IsBusy)
            {
                syncWorker.RunWorkerAsync(new RefreshConfig(GetFolderPathId(), syncWorker, results));
            }
        }

        /// <summary>
        /// Sync the specify file from rms to check if it is modified or not, apply for Modify Rights feature.
        /// </summary>
        /// <param name="selectedFile">the file will to update</param>
        /// <param name="result">the callback results</param>
        /// </param>
        public override void SyncDestFile(INxlFile selectedFile, OnSyncDestComplete result, bool bNeedFindParent = false)
        {
            BackgroundWorker syncWorker = new BackgroundWorker();
            syncWorker.DoWork += SyncDataHander;
            syncWorker.RunWorkerCompleted += SyncDataCompleted;

            if (syncWorker.IsBusy)
            {
                return;
            }

            var pathId = ParseParentFolderByRmsPath(selectedFile.PathId);
            syncWorker.RunWorkerAsync(new RefreshConfig(pathId, syncWorker, selectedFile, result));
        }

        public override void MarkOffline(INxlFile nxlFile, OnDownloadComplete callback)
        {
            DownloadFile(nxlFile, false, callback);
        }

        public override void DownloadFile(INxlFile nxl, bool isViewOnly, OnDownloadComplete callback, bool isDownloadPartial = false, bool isOnlineView = false)
        {
            DownloadManager.GetSingleton()
            .SubmitToTaskQueue(nxl)
            .TryDownload(callback, isViewOnly, isDownloadPartial, isOnlineView);
        }

        public override IList<INxlFile> GetOfflines()
        {
            IList<INxlFile> rt = new List<INxlFile>();
            foreach(var i in App.WorkSpace.GetOfflines())
            {
                var of = new WorkSpaceRmsDoc((WorkSpaceFile)i);
                rt.Add(of);
            }

            return rt;
        }

        public override IList<INxlFile> GetPendingUploads()
        {
            IList<INxlFile> rt = new List<INxlFile>();
            foreach (var i in App.WorkSpace.GetPendingUploads())
            {
                var of = new PendingUploadFile(i);
                rt.Add(of);
            }

            return rt;
        }

        // These files will need to be added to queue and then uploaded to rms.
        public IList<INxlFile> GetEditedOfflineFiles()
        {
            IList<INxlFile> rt = new List<INxlFile>();

            foreach(var one in App.WorkSpace.GetOfflines())
            {
                if ((one.Status == EnumNxlFileStatus.AvailableOffline || one.Status == EnumNxlFileStatus.CachedFile
                        || one.Status == EnumNxlFileStatus.Uploading) && one.IsOfflineFileEdit) // fix Bug 56962 - File fail to upload after session is expired, add file status is uploading
                {
                    WorkSpaceRmsDoc offlineFile = new WorkSpaceRmsDoc((WorkSpaceFile)one);
                    rt.Add(offlineFile);
                }
            }

            return rt;
        }

        public override IList<INxlFile> GetSharedByMeFiles()
        {
            throw new NotImplementedException();
        }

        public override IList<INxlFile> GetSharedWithMeFiles()
        {
            throw new NotImplementedException();
        }

        public override void SyncSharedWithMeFiles(OnSyncComplete callback)
        {
            throw new NotImplementedException();
        }

        public override void SyncSharedByMeFiles(OnSyncComplete callback)
        {
            throw new NotImplementedException();
        }

        #endregion // Main public interface impl

        public override void GetRmsFilesRecursivelyFromDB(string pathId, IList<INxlFile> outRet)
        {
            if (outRet == null)
            {
                return;
            }

            try
            {
                IWorkSpaceFile[] files = App.WorkSpace.List(pathId);
                foreach (var one in files)
                {
                    if (one.Is_Folder)
                    {
                        NxlFolder folder = new WorkSpaceFolder(one);
                        outRet.Add(folder);

                        // get the folder children recursively.
                        folder.Children = new List<INxlFile>();
                        GetRmsFilesRecursivelyFromDB(one.Path_Display, folder.Children);
                    }
                    else
                    {
                        outRet.Add(new WorkSpaceRmsDoc(one));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                App.Log.Error("Invoke GetRemoteFiles failed.");
            }
        }

        public override void SyncFilesRecursively(string pathId, IList<INxlFile> results)
        {
            try
            {
                IWorkSpaceFile[] files = App.WorkSpace.Sync(pathId);
                foreach (var f in files)
                {
                    if (f.Is_Folder)
                    {
                        NxlFolder folder = new WorkSpaceFolder(f);
                        results.Add(folder);

                        // Recursively get its children nodes
                        folder.Children = new List<INxlFile>();
                        SyncFilesRecursively(folder.PathId, folder.Children);
                    }
                    else
                    {
                        results.Add(new WorkSpaceRmsDoc(f));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                App.Log.Error("Invoke InnerSyncRemoteAllFiles failed.");
                throw;
            }
        }

        public override void SyncParentNodeFile(INxlFile nxl, ref IList<INxlFile> results)
        {
            try
            {
                string pathId = nxl.DisplayPath;

                int lastIndex = pathId.LastIndexOf('/');
                if (lastIndex > 0)
                {
                    pathId = pathId.Substring(0, pathId.LastIndexOf('/') + 1);
                }
                else
                {
                    pathId = "/";
                }

                IWorkSpaceFile[] files = App.WorkSpace.Sync(pathId.ToLower());
                foreach (var f in files)
                {
                    if (f.Is_Folder)
                    {
                        NxlFolder folder = new WorkSpaceFolder(f);
                        results.Add(folder);
                    }
                    else
                    {
                        results.Add(new WorkSpaceRmsDoc(f));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                App.Log.Error("Invoke SyncParentNodeFile failed.");
                throw;
            }
        }

        public override bool CheckFileExists(string pathId)
        {
            return App.WorkSpace.CheckFileExists(pathId);
        }

        private void SyncDataHander(object sender, DoWorkEventArgs args)
        {
            RefreshConfig config = (RefreshConfig)args.Argument;

            IList<INxlFile> rt = new List<INxlFile>();
            bool bSuc = true;
            try
            {
                IWorkSpaceFile[] files = App.WorkSpace.Sync(config.PathId);
                foreach (var f in files)
                {
                    if (f.Is_Folder)
                    {
                        NxlFolder folder = new WorkSpaceFolder(f);
                        rt.Add(folder);
                    }
                    else
                    {
                        rt.Add(new WorkSpaceRmsDoc(f));
                    }
                }
            }
            catch (Exception e)
            {
                App.Log.Error(e.ToString());
                bSuc = false;

                // Handler session expiration
                GeneralHandler.TryHandleSessionExpiration(e);
            }
            finally
            {
                // un-register event
                config.BackgroundWorker.DoWork -= SyncDataHander;
                // padding data
                config.bSucess = bSuc;
                config.Results = rt;
                args.Result = config;

                // At the same time, update the filePool
                if (bSuc)
                {
                    if(config.PathId == "/")
                    {
                        FilePool = rt;
                    } else
                    {
                        NxlFolder toFind = null;
                        FindParentNode(FilePool, config.PathId, ref toFind);
                        if (toFind != null)
                        {
                            toFind.Children = rt;
                        }
                    }
                }
            }
        }

        private void SyncDataCompleted(object sender, RunWorkerCompletedEventArgs args)
        {
            RefreshConfig config = (RefreshConfig)args.Result;
            // un-register
            config.BackgroundWorker.RunWorkerCompleted -= SyncDataCompleted;

            // callback
            if (config.OnRefreshComplete != null)
            {
                config.OnRefreshComplete?.Invoke(config.bSucess, config.Results, config.PathId);
            }
            else if (config.OnSyncDestComplete != null)
            {
                INxlFile updatedNode = null;
                // find the specify updated node
                if (config.Results != null)
                {
                    foreach (var one in config.Results)
                    {
                        if (config.SpecifyFile != null && config.SpecifyFile.Equals(one))
                        {
                            updatedNode = one;
                            break;
                        }
                    }
                }

                config.OnSyncDestComplete?.Invoke(config.bSucess, updatedNode);
            }
        }

        // Actually Should supply tool class.
        private void FindParentNode(IList<INxlFile> fileNodes, string configPathId, ref NxlFolder findNode)
        {
            if (fileNodes == null)
            {
                return;
            }

            foreach (var file in fileNodes)
            {
                if (file.IsFolder)
                {
                    NxlFolder folder = file as NxlFolder;
                    if (folder.PathId == configPathId)
                    {
                        findNode = folder;
                        break;
                    }
                    else
                    {
                        FindParentNode(folder.Children, configPathId, ref findNode);
                    }
                }
            }
        }

        private string GetFolderPathId()
        {
            return CurrentWorkingFolder.PathId;
        }

        private string ParseParentFolderByRmsPath(string displayPath)
        {
            string FolderId = "/";

            if (string.IsNullOrEmpty(displayPath))
            {
                throw new Exception("Fatal error, illegal parameters found.");
            }

            int lastIndex = displayPath.LastIndexOf("/");
            if (lastIndex != 0)
            {
                FolderId = displayPath.Substring(0, lastIndex + 1);
            }

            return FolderId.ToLower();
        }

        #region RefreshConfig
        private class RefreshConfig
        {
            public string PathId { get; }
            public BackgroundWorker BackgroundWorker { get; }
            public OnRefreshComplete OnRefreshComplete { get; }
            // Used to sync specify file node.
            public OnSyncDestComplete OnSyncDestComplete { get; }
            public bool bSucess { get; set; }
            public IList<INxlFile> Results { get; set; }
            public INxlFile SpecifyFile { get; }

            public RefreshConfig(string pathId, BackgroundWorker bgworker, OnRefreshComplete callback)
            {
                this.PathId = pathId;
                this.BackgroundWorker = bgworker;
                this.OnRefreshComplete = callback;

                this.OnSyncDestComplete = null;
                this.SpecifyFile = null;
            }

            public RefreshConfig(string pathId, BackgroundWorker bgworker, INxlFile selectFile, OnSyncDestComplete syncCallback)
            {
                this.PathId = pathId;
                this.BackgroundWorker = bgworker;
                this.SpecifyFile = selectFile;
                this.OnSyncDestComplete = syncCallback;

                this.OnRefreshComplete = null;
            }

        }
        #endregion // RefreshConfig

        #region WorkSpaceRmsDoc inner class
        public sealed class WorkSpaceRmsDoc : NxlDoc
        {
            public IWorkSpaceFile Raw { get; set; }

            public WorkSpaceRmsDoc(IWorkSpaceFile raw)
            {
                this.Raw = raw;

                this.Name = raw.Nxl_Name;
                this.Size = raw.Size;
                this.DateModified = DateTimeHelper.DateTimeToTimestamp(raw.Last_Modified.ToLocalTime()).ToString();
                this.RawDateModified = raw.Last_Modified;
                this.SharedWith = new List<string>(); // Should support?
                this.Location = InitLocation(raw);

                this.LocalPath = raw.Nxl_Local_Path;
                this.DisplayPath = raw.Path_Display;
                this.PathId = raw.Path_Id;

                this.IsMarkedOffline = raw.Is_Offline;
                this.FileStatus = raw.Status;
                this.FileRepo = EnumFileRepo.REPO_WORKSPACE;
                this.IsCreatedLocal = false;
                this.FileId = raw.File_Id;
                this.PartialLocalPath = raw.Partial_Local_Path;
                this.SourcePath = "SkyDRM://" + FileSysConstant.WORKSPACE + Raw.Path_Display;
            }

            public override EnumNxlFileStatus FileStatus
            {
                get
                {
                    return Raw.Status;
                }

                set
                {
                    Raw.Status = value;// Will update into db in low level.
                    NotifyPropertyChanged("FileStatus");
                }
            }

            public override bool IsEdit
            {
                get
                {
                    return Raw.Is_Edit;
                }

                set
                {
                    Raw.Is_Edit = value;
                    NotifyPropertyChanged("IsEdit");
                }
            }

            public override bool IsMarkedFileRemoteModified
            {
                get
                {
                    return Raw.Is_Dirty;
                }

                set
                {

                    Raw.Is_Dirty = value;
                    NotifyPropertyChanged("IsMarkedFileRemoteModified");
                }
            }

            public override bool IsModifiedRights
            {
                get
                {
                    return Raw.Is_ModifyRights;
                }
                set
                {
                    // Will also update into db in low level.
                    Raw.Is_ModifyRights = value;
                }
            }

            public override IFileInfo FileInfo => Raw.FileInfo;

            public WorkspaceMetaData GetMetaData()
            {
                return Raw.GetMetaData();
            }

            public override void Remove()
            {
                Raw?.Remove();
            }

            public override bool UnMark()
            {
                try
                {
                    Raw?.Remove();
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
                Raw?.Download(isViewOnly);
                // update
                LocalPath = Raw?.Nxl_Local_Path;
            }

            public override void DownloadPartial()
            {
                //Raw?.DownloadPartial();
                Raw?.GetNxlHeader();
                PartialLocalPath = Raw?.Partial_Local_Path;
            }

            public override void UploadEditedFile()
            {
                Raw?.UploadEditedFile();
            }

            public override void Export(string destFolder)
            {
                Raw?.Export(destFolder);
            }

            private EnumFileLocation InitLocation(IWorkSpaceFile raw)
            {
                return (raw.Status == EnumNxlFileStatus.CachedFile
                       || raw.Is_Offline) ? EnumFileLocation.Local : EnumFileLocation.Online;
            }
        }
        #endregion // WorkSpaceRmsDoc inner class

        #region WorkSpaceFolder inner class
        public sealed class WorkSpaceFolder : NxlFolder
        {
            public IWorkSpaceFile Raw { get; set; }

            public WorkSpaceFolder(IWorkSpaceFile raw)
            {
                this.Raw = raw;

                this.Name = raw.Nxl_Name;
                this.Size = raw.Size;
                this.Location = EnumFileLocation.Online;
                this.FileStatus = EnumNxlFileStatus.Online;
                this.FileRepo = EnumFileRepo.REPO_WORKSPACE;
                this.DateModified = DateTimeHelper.DateTimeToTimestamp(raw.Last_Modified.ToLocalTime()).ToString();
                this.RawDateModified = raw.Last_Modified;

                this.LocalPath = raw.Nxl_Local_Path;
                this.DisplayPath = raw.Path_Display;
                this.PathId = raw.Path_Id;
                this.FileId = raw.File_Id;
            }
        }
        #endregion // WorkSpaceFolder inner class

    }
}
