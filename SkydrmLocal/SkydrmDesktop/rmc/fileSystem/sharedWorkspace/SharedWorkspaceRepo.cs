using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmDesktop.rmc.common.helper;
using SkydrmDesktop.rmc.featureProvider.externalDrive.RmsRepoMgr;
using SkydrmDesktop.rmc.featureProvider.SharedWorkspace;
using SkydrmDesktop.rmc.fileSystem.utils;
using SkydrmLocal.rmc.common.component;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.exception;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.fileSystem;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui.utils;

namespace SkydrmDesktop.rmc.fileSystem.sharedWorkspace
{
    public delegate void OnSyncSharedWorkSpaceComplete(bool bSucceed, List<INxlFile> results);

    public sealed class SharedWorkspaceRepo : AbstractFileRepo
    {
        private readonly SkydrmApp App = SkydrmApp.Singleton;
        private ISharedWorkspace repo;

        // Mainly used for treeview model data binding.
        private IList<INxlFile> FilePool { get; set; }

        // Flag that if is executing sync all data from rms.
        public bool IsLoading { get; private set; }

        private SharedWorkspaceRepo(ISharedWorkspace repo)
        {
            this.repo = repo;
            FilePool = new List<INxlFile>();
        }

        public static SharedWorkspaceRepo Create(IRmsRepo rmsRepo)
        {
            return new SharedWorkspaceRepo(new SharedWorkSpace(rmsRepo));
        }

        public override string RepoDisplayName { get => repo.DisplayName; set => repo.DisplayName = value; }

        public override string RepoType
        {
            get
            {
                if(repo.Type == ExternalRepoType.UNKNOWN && repo.DisplayName == "application oneDrive")
                {
                    return "ONEDRIVE";
                }

                return FileSysConstant.GetExternalRepoName(repo.Type);
            }
        }
            

        public override string RepoId { get => repo.RepoId;}

        public override RepositoryProviderClass RepoProviderClass => RepositoryProviderClass.APPLICATION;


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
            App.Log.Info("Get SharedWorkSpace all files from DB.");

            IList<INxlFile> rt = new List<INxlFile>();
            try
            {
                var files = repo.ListAll(true);
                InnerGetAllFilesRecursivelyFromDB(files, "/", rt);
            }
            catch (Exception e)
            {
                App.Log.Error("Get SharedWorkSpace all files from DB failed.", e);
            }

            // store file object pool
            FilePool = rt;

            return rt;
        }

        /// <summary>
        /// Sync shared workspace all files from rms.
        ///     Actually after aync, the implementation mechanism underlying is:
        ///     will merge with locals first and update into local db again then return directly locals.
        /// </summary>
        /// <param name="callback"></param>
        public void SyncAllFiles(OnSyncSharedWorkSpaceComplete callback)
        {
            IsLoading = true;
            // Async worker
            Func<object> asyncTask = new Func<object>(() => {

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

                return new SyncRetValue(bSucceed, ret);
            });

            // Async callback
            Action<object> cb = new Action<object>((rt) => {
                IsLoading = false;
                SyncRetValue rtValue = (SyncRetValue)rt;
                callback?.Invoke(rtValue.IsSuc, rtValue.results);
            });

            // Invoke
            AsyncHelper.RunAsync(asyncTask, cb);
        }

        public override void SyncFilesRecursively(string path, IList<INxlFile> results)
        {
            try
            {
                var files = repo.Sync(path);
                foreach (var f in files)
                {
                    if (f.Is_Folder)
                    {
                        NxlFolder folder = new SharedWorkspaceFolder(f);
                        results.Add(folder);

                        // Recursively get its children nodes
                        folder.Children = new List<INxlFile>();
                        SyncFilesRecursively(folder.DisplayPath, folder.Children);
                    }
                    else
                    {
                        results.Add(new SharedWorkspaceDoc(repo, f));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                App.Log.Error("Invoke InnerSyncRemoteAllFiles failed, the path is:--------> \n" + path);
                if (path.Equals("/")) { // Fix bug 64440
                    throw e;
                }
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
                    pathId = pathId.Substring(0, pathId.LastIndexOf('/'));
                }
                else
                {
                    pathId = "/";
                }

                var files = repo.Sync(pathId.ToLower());
                foreach (var f in files)
                {
                    if (f.Is_Folder)
                    {
                        NxlFolder folder = new SharedWorkspaceFolder(f);
                        results.Add(folder);
                    }
                    else
                    {
                        results.Add(new SharedWorkspaceDoc(repo, f));
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

        public override void MarkOffline(INxlFile nxlFile, OnDownloadComplete callback)
        {
            DownloadFile(nxlFile, false, callback);
        }

        public override void DownloadFile(INxlFile nxl, bool isViewOnly, OnDownloadComplete callback, 
            bool isDownloadPartial = false, bool isOnlineView = false)
        {
            DownloadManager.GetSingleton()
            .SubmitToTaskQueue(nxl)
            .TryDownload(callback, isViewOnly, isDownloadPartial, isOnlineView);
        }

        public PendingUploadFile AddLocalFile(string pathId, string displayPath, string filePath,
                                  List<SkydrmLocal.rmc.sdk.FileRights> rights, WaterMarkInfo waterMark,
                                  Expiration expiration, UserSelectTags tags)
        {
            var rt = repo.AddLocalAdded(pathId, displayPath, filePath, rights, waterMark, expiration, tags);
            return new PendingUploadFile(rt, RepoId, RepoDisplayName);
        }

        public override IList<INxlFile> GetOfflines()
        {
            IList<INxlFile> rt = new List<INxlFile>();
            foreach (var i in repo.GetOfflines())
            {
                var of = new SharedWorkspaceDoc(repo, (SharedWorkspaceFile)i);
                rt.Add(of);
            }

            return rt;
        }

        public override IList<INxlFile> GetPendingUploads()
        {
            IList<INxlFile> rt = new List<INxlFile>();
            foreach (var i in repo.GetPendingUploads())
            {
                var of = new PendingUploadFile(i, RepoId, RepoDisplayName);
                rt.Add(of);
            }

            return rt;
        }

        // These files will need to be added to queue and then uploaded to rms.
        public IList<INxlFile> GetEditedOfflineFiles()
        {
            IList<INxlFile> rt = new List<INxlFile>();

            foreach (var one in repo.GetOfflines())
            {
                if ((one.Status == EnumNxlFileStatus.AvailableOffline 
                    || one.Status == EnumNxlFileStatus.CachedFile
                    || one.Status == EnumNxlFileStatus.Uploading) && one.IsOfflineFileEdit) // fix Bug 56962 - File fail to upload after session is expired, add file status is uploading
                {
                    var offlineFile = new SharedWorkspaceDoc(repo, (SharedWorkspaceFile)one);
                    rt.Add(offlineFile);
                }
            }

            return rt;
        }


        /// <summary>
        /// Get current working folder files from local db, including local protected file and remote file nodes.
        /// </summary>
        /// <returns></returns>
        public override IList<INxlFile> GetWorkingFolderFilesFromDB()
        {
            IList<INxlFile> rt = new List<INxlFile>();

            InnerGetFilesFromDB(GetFolderDisplayPath(), rt);

            return rt;
        }

        /// <summary>
        /// Sync specified folder's all remote nodes from rms.
        /// </summary>
        /// <param name="results"></param>
        /// <param name="itemFlag"></param>
        public override void SyncFiles(OnRefreshComplete results, string itemFlag = null)
        {
            BackgroundWorker syncWorker = new BackgroundWorker();
            syncWorker.DoWork += SyncDataHander;
            syncWorker.RunWorkerCompleted += SyncDataCompleted;

            if (!syncWorker.IsBusy)
            {
                syncWorker.RunWorkerAsync(new RefreshConfig(GetFolderDisplayPath(), syncWorker, results, itemFlag));
            }
        }

        /// <summary>
        /// Sync the specify file from rms to check if it is modified or not, apply for Modify Rights feature and so on.
        /// </summary>
        /// <param name="selectedFile"></param>
        /// <param name="result"></param>
        /// <param name="bNeedFindParent"></param>
        public override void SyncDestFile(INxlFile selectedFile, OnSyncDestComplete result, bool bNeedFindParent = false)
        {
            BackgroundWorker syncWorker = new BackgroundWorker();
            syncWorker.DoWork += SyncDataHander;
            syncWorker.RunWorkerCompleted += SyncDataCompleted;

            if (syncWorker.IsBusy)
            {
                return;
            }

            var pathId = ParseParentFolderByRmsPath(selectedFile.DisplayPath);
            syncWorker.RunWorkerAsync(new RefreshConfig(pathId, syncWorker, selectedFile, result));
        }

        public override void GetRmsFilesRecursivelyFromDB(string path, IList<INxlFile> outRet)
        {
            if (outRet == null)
            {
                return;
            }

            try
            {
                var files = repo.List(path);
                foreach (var one in files)
                {
                    if (one.Is_Folder)
                    {
                        NxlFolder folder = new SharedWorkspaceFolder(one);
                        outRet.Add(folder);

                        // get the folder children recursively.
                        folder.Children = new List<INxlFile>();
                        GetRmsFilesRecursivelyFromDB(one.Path_Display, folder.Children);
                    }
                    else
                    {
                        outRet.Add(new SharedWorkspaceDoc(repo,one));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                App.Log.Error("Invoke GetRemoteFiles failed.");
            }
        }

        /// <summary>
        /// Get all folder nodes from local, used for select destination folder window
        /// </summary>
        /// <returns></returns>
        public IList<INxlFile> GetAllFolders()
        {
            App.Log.Info("Get SharedWorkSpace all folders");
            IList<INxlFile> rt = new List<INxlFile>();
            try
            {
                var files = repo.ListAll(true);
                BuildFolders(files, "/", rt);
            }
            catch (Exception e)
            {
                App.Log.Error("Get SharedWorkSpace all folders failed.", e);
            }

            return rt;
        }

        public override bool CheckFileExists(string pathId)
        {
            return repo.CheckFileExists(pathId);
        }
        #region Private methods

        // Sync handler
        private void SyncDataHander(object sender, DoWorkEventArgs args)
        {
            RefreshConfig config = (RefreshConfig)args.Argument;

            IList<INxlFile> rt = new List<INxlFile>();
            bool bSuc = true;
            try
            {
                var files = repo.Sync(config.Path);
                foreach (var f in files)
                {
                    if (f.Is_Folder)
                    {
                        NxlFolder folder = new SharedWorkspaceFolder(f);
                        rt.Add(folder);
                    }
                    else
                    {
                        rt.Add(new SharedWorkspaceDoc(repo, f));
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
                    if (config.Path == "/")
                    {
                        FilePool = rt;
                    }
                    else
                    {
                        NxlFolder toFind = null;
                        FindParentNode(FilePool, config.Path, ref toFind);
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
                config.OnRefreshComplete?.Invoke(config.bSucess, config.Results, config.ItemFlag);
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

        /// <summary>
        /// Inner impl to get files recursively from DB, including local protected file and remote file nodes.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="results"></param>
        private void InnerGetAllFilesRecursivelyFromDB(ISharedWorkspaceFile[] files, string path, IList<INxlFile> results)
        {
            // Get local created files from db.
            foreach (var one in GetPendingFilesFromDB(path))
            {
                results.Add(one);
            }

            if (string.IsNullOrEmpty(path))
            {
                path = "/";
            }
            if (path.Length > 1 && !path.EndsWith("/"))
            {
                path += "/";
            }

            foreach (var f in files)
            {
                if (FileHelper.IsDirectChild(f.Path_Display, path))
                {
                    if (f.Is_Folder)
                    {
                        NxlFolder folder = new SharedWorkspaceFolder(f);
                        //  Recusively get the foler children.
                        folder.Children = new List<INxlFile>();
                        InnerGetAllFilesRecursivelyFromDB(files, folder.DisplayPath, folder.Children);

                        results.Add(folder);
                    }
                    else
                    {
                        var doc = new SharedWorkspaceDoc(repo, f);
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
        /// Get specified path's local added files 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private IList<INxlFile> GetPendingFilesFromDB(string path)
        {
            IList<INxlFile> ret = new List<INxlFile>();
            foreach (var one in repo.ListLocalAdded(path))
            {
                ret.Add(new PendingUploadFile(one, RepoId, RepoDisplayName));
            }

            return ret;
        }

        /// <summary>
        /// Inner impl to get files from DB, including local protected file and remote file nodes.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private void InnerGetFilesFromDB(string path, IList<INxlFile> results)
        {
            try
            {
                // Get local created files from db.
                foreach (var one in GetPendingFilesFromDB(path))
                {
                    results.Add(one);
                }

                var files = repo.List(path);
                foreach (var f in files)
                {
                    if (f.Is_Folder)
                    {
                        NxlFolder folder = new SharedWorkspaceFolder(f);

                        results.Add(folder);
                    }
                    else
                    {
                        var doc = new SharedWorkspaceDoc(repo, f);
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
        /// Inner impl to get all local folders from cache.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="results"></param>
        private void BuildFolders(ISharedWorkspaceFile[] files, string path, IList<INxlFile> results)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = "/";
            }
            if (path.Length > 1 && !path.EndsWith("/"))
            {
                path += "/";
            }

            foreach (var f in files)
            {
                if (FileHelper.IsDirectChild(f.Path_Display, path) && f.Is_Folder)
                {
                    NxlFolder folder = new SharedWorkspaceFolder(f);
                    results.Add(folder);

                    // Recusively get the folder children.
                    folder.Children = new List<INxlFile>();
                    BuildFolders(files, folder.DisplayPath, folder.Children);
                }
            }
        }


        private string GetFolderDisplayPath()
        {
            return CurrentWorkingFolder.DisplayPath;
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
                FolderId = displayPath.Substring(0, lastIndex);
            }

            return FolderId;
        }

        private void FindParentNode(IList<INxlFile> fileNodes, string configDisplayPath, ref NxlFolder findNode)
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
                    if (folder.DisplayPath == configDisplayPath)
                    {
                        findNode = folder;
                        break;
                    }
                    else
                    {
                        FindParentNode(folder.Children, configDisplayPath, ref findNode);
                    }
                }
            }
        }

        #endregion // Private methods


        private sealed class SyncRetValue
        {
            public bool IsSuc { get; }
            public List<INxlFile> results { get; }

            public SyncRetValue(bool isSuc, List<INxlFile> rt)
            {
                this.IsSuc = isSuc;
                this.results = rt;
            }
        }

        private sealed class RefreshConfig
        {
            public string Path { get; }
            public BackgroundWorker BackgroundWorker { get; }
            public OnRefreshComplete OnRefreshComplete { get; }
            // Used to sync specify file node.
            public OnSyncDestComplete OnSyncDestComplete { get; }
            public bool bSucess { get; set; }
            public IList<INxlFile> Results { get; set; }
            public INxlFile SpecifyFile { get; }
            public string ItemFlag { get; } = "";

            public RefreshConfig(string path, BackgroundWorker bgworker, OnRefreshComplete callback, string itemflag)
            {
                this.Path = path;
                this.BackgroundWorker = bgworker;
                this.OnRefreshComplete = callback;
                this.ItemFlag = itemflag;

                this.OnSyncDestComplete = null;
                this.SpecifyFile = null;
            }

            public RefreshConfig(string path, BackgroundWorker bgworker, INxlFile selectFile, OnSyncDestComplete syncCallback)
            {
                this.Path = path;
                this.BackgroundWorker = bgworker;
                this.SpecifyFile = selectFile;
                this.OnSyncDestComplete = syncCallback;

                this.OnRefreshComplete = null;
            }
        }

    }

    public sealed class SharedWorkspaceDoc: NxlDoc
    {
        public ISharedWorkspaceFile Raw { get; set; }

        public SharedWorkspaceDoc(ISharedWorkspace repo, ISharedWorkspaceFile f)
        {
            this.Raw = f;

            this.RepoId = repo.RepoId;

            this.Name = f.Nxl_Name;
            this.Size = f.Size;
            this.DateModified = DateTimeHelper.DateTimeToTimestamp(f.Last_Modified.ToLocalTime()).ToString();
            this.RawDateModified = f.Last_Modified;
            this.SharedWith = new List<string>(); // not support
            this.Location = InitLocation(f);

            this.LocalPath = f.Nxl_Local_Path;
            this.DisplayPath = f.Path_Display;
            this.PathId = f.Path_Id;
            this.IsNxlFile = f.Is_ProtectedFile;

            this.IsMarkedOffline = f.Is_Offline;
            this.FileStatus = f.Status;
            this.FileRepo = EnumFileRepo.REPO_EXTERNAL_DRIVE;
            this.IsCreatedLocal = false;
            this.FileId = f.File_Id;
            this.PartialLocalPath = f.Partial_Local_Path;
            this.SourcePath = "SkyDRM://" + FileSysConstant.REPOSITORIES + "/"+ repo.DisplayName + f.Path_Display;
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

        public override IFileInfo FileInfo => Raw.FileInfo;

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

        private EnumFileLocation InitLocation(ISharedWorkspaceFile raw)
        {
            return (raw.Status == EnumNxlFileStatus.CachedFile
                   || raw.Is_Offline) ? EnumFileLocation.Local : EnumFileLocation.Online;
        }

    }

    public sealed class SharedWorkspaceFolder: NxlFolder
    {
        public ISharedWorkspaceFile Raw { get; set; }

        // Means current repo root.
        public SharedWorkspaceFolder()
        {
            this.PathId = "/";
            this.DisplayPath = "/";
        }

        public SharedWorkspaceFolder(ISharedWorkspaceFile raw)
        {
            this.Raw = raw;

            this.Name = raw.Nxl_Name;
            this.Size = raw.Size;
            this.Location = EnumFileLocation.Online;
            this.FileStatus = EnumNxlFileStatus.Online;
            this.FileRepo = EnumFileRepo.REPO_EXTERNAL_DRIVE;
            this.DateModified = DateTimeHelper.DateTimeToTimestamp(raw.Last_Modified.ToLocalTime()).ToString();
            this.RawDateModified = raw.Last_Modified;

            this.LocalPath = raw.Nxl_Local_Path;
            this.DisplayPath = raw.Path_Display;
            this.PathId = raw.Path_Id;
            this.FileId = raw.File_Id;
        }
    }
}
