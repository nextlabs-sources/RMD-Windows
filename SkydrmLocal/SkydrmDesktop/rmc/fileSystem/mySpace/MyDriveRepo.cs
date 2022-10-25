using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.common.helper;
using SkydrmDesktop.rmc.featureProvider;
using SkydrmDesktop.rmc.featureProvider.MySpace;
using SkydrmDesktop.rmc.fileSystem.utils;
using SkydrmLocal.rmc.common.component;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.exception;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.fileSystem;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.ui.utils;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.viewModel;

namespace SkydrmDesktop.rmc.fileSystem.mySpace
{
    public delegate void OnSyncComplete(bool bSucceed, IList<INxlFile> results);

    // We can look myDrive as external drives, since its behavior is the same with them.
    public class MyDriveRepo : AbstractFileRepo
    {
        private readonly SkydrmApp App = SkydrmApp.Singleton;
        private string repoId;

        private IList<INxlFile> FilePool { get; set; }

        // Flag that if is executing sync all data from rms.
        public bool IsLoading { get; private set; }

        public MyDriveRepo()
        {
            FilePool = new List<INxlFile>();
        }

        public IList<INxlFile> GetAllData()
        {
            App.Log.Info("Get MyDrive all files from DB.");

            IList<INxlFile> results = new List<INxlFile>();
            try
            {
                var files = App.MyDrive.ListAll(true);
                InnerGetAllFilesRecursivelyFromDB(files, "/", results);
            }
            catch (Exception e)
            {
                App.Log.Error("Get MyDrive all files from DB failed.", e);
            }

            // store file object pool
            FilePool = results;

            return results;
        }

        /// <summary>
        /// Inner impl to get files recursively from DB, including local added file and remote file nodes.
        /// </summary>
        /// <param name="pathId"></param>
        /// <returns></returns>
        private void InnerGetAllFilesRecursivelyFromDB(IMyDriveFile[] files, string pathId, IList<INxlFile> results)
        {
            // Get local created files from db.
            foreach (var one in GetPendingFilesFromDB(pathId))
            {
                results.Add(one);
            }

            foreach (var f in files)
            {
                if (FileHelper.IsDirectChild(f.PathId, pathId))
                {
                    if (f.IsFolder)
                    {
                        NxlFolder folder = new MyDriveFolder(f);
                        // Recusively get the folder children.
                        folder.Children = new List<INxlFile>();
                        InnerGetAllFilesRecursivelyFromDB(files, folder.PathId, folder.Children);

                        results.Add(folder);
                    }
                    else
                    {
                        var doc = new MyDriveDoc(f);
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
        /// Inner impl to get files from DB, including local added file and remote file nodes.
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

                IMyDriveFile[] files = App.MyDrive.List(pathId);
                foreach (var f in files)
                {
                    if (f.IsFolder)
                    {
                        NxlFolder folder = new MyDriveFolder(f);

                        results.Add(folder);
                    }
                    else
                    {
                        var doc = new MyDriveDoc(f);
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
            foreach (var one in App.MyDrive.ListLocalAdded(pathId))
            {
                ret.Add(new PendingUploadFile(one));
            }

            return ret;
        }

        public void SyncAllData(OnSyncComplete callback)
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

        #region Impl public interface

        public override string RepoDisplayName { get => FileSysConstant.MYDRIVE; set => new NotImplementedException(); }

        public override string RepoType => FileSysConstant.MYDRIVE;

        public override IList<INxlFile> GetFilePool()
        {
            return FilePool;
        }

        /// <summary>
        /// Get current working folder files from local db, including local added file and remote file nodes.
        /// </summary>
        public override IList<INxlFile> GetWorkingFolderFilesFromDB()
        {
            IList<INxlFile> ret = new List<INxlFile>();

            InnerGetFilesFromDB(GetFolderPathId(), ret);

            return ret;
        }

        public override IList<INxlFile> GetOfflines()
        {
            IList<INxlFile> rt = new List<INxlFile>();
            foreach (var i in App.MyDrive.GetOfflines())
            {
                var of = new MyDriveDoc((MyDriveFile)i);
                rt.Add(of);
            }

            return rt;
        }

        public override IList<INxlFile> GetPendingUploads()
        {
            IList<INxlFile> rt = new List<INxlFile>();
            foreach (var i in App.MyDrive.GetPendingUploads())
            {
                var of = new PendingUploadFile(i);
                rt.Add(of);
            }

            return rt;
        }

        /// <summary>
        /// Sync current working folder all nodes from rms.
        /// </summary>
        public override void SyncFiles(OnRefreshComplete callback, string itemFlag = null)
        {
            // Async worker
            Func<object> asyncTask = new Func<object>(() => {

                bool bSuc = true;
                string pathid = GetFolderPathId();
                List<INxlFile> ret = new List<INxlFile>();
                try
                {
                    IMyDriveFile[] files = App.MyDrive.Sync(pathid);

                    foreach (IMyDriveFile f in files)
                    {
                        if (f.IsFolder)
                        {
                            NxlFolder folder = new MyDriveFolder(f);
                            ret.Add(folder);
                        }
                        else
                        {
                            ret.Add(new MyDriveDoc(f));
                        }
                    }

                }
                catch (Exception e)
                {
                    bSuc = false;
                    App.Log.Error("Invoke GetRemoteCurrentFolderFiles failed.");

                    // Handler session expiration
                    GeneralHandler.TryHandleSessionExpiration(e);
                }
                finally
                {
                    // At the same time, update the filePool
                    if (bSuc)
                    {
                        if (pathid == "/")
                        {
                            FilePool = ret;
                        }
                        else
                        {
                            NxlFolder toFind = null;
                            FindParentNode(FilePool, pathid, ref toFind);
                            if (toFind != null)
                            {
                                toFind.Children = ret;
                            }
                        }
                    }
                }

                return new SyncRetValue(bSuc, ret, string.IsNullOrEmpty(itemFlag) ? GetFolderPathId() : itemFlag);
            });

            // Async callback
            Action<object> cb = new Action<object>((rt) => {
                SyncRetValue rtValue = (SyncRetValue)rt;
                callback?.Invoke(rtValue.IsSuc, rtValue.results, rtValue.ItemFlag);
            });

            // Invoke
            AsyncHelper.RunAsync(asyncTask, cb);
        }

        /// <summary>
        /// Sync the specify file from rms to check if it is modified or not(apply for overwrite).
        /// </summary>
        public override void SyncDestFile(INxlFile selectedFile, OnSyncDestComplete callback, bool bNeedFindParent = false)
        {
            // Async worker
            Func<object> asyncTask = new Func<object>(() => {

                bool bSuc = true;
                string pathid = GetFolderPathId();
                List<INxlFile> ret = new List<INxlFile>();
                try
                {
                    IMyDriveFile[] files = App.MyDrive.Sync(pathid);

                    foreach (IMyDriveFile f in files)
                    {
                        if (f.IsFolder)
                        {
                            NxlFolder folder = new MyDriveFolder(f);
                            ret.Add(folder);
                        }
                        else
                        {
                            ret.Add(new MyDriveDoc(f));
                        }
                    }

                }
                catch (Exception e)
                {
                    bSuc = false;
                    App.Log.Error("Invoke GetRemoteCurrentFolderFiles failed.");

                    // Handler session expiration
                    GeneralHandler.TryHandleSessionExpiration(e);
                }
                finally
                {
                    // At the same time, update the filePool
                    if (bSuc)
                    {
                        if (pathid == "/")
                        {
                            FilePool = ret;
                        }
                        else
                        {
                            NxlFolder toFind = null;
                            FindParentNode(FilePool, pathid, ref toFind);
                            if (toFind != null)
                            {
                                toFind.Children = ret;
                            }
                        }
                    }
                }

                return new SyncRetValue(bSuc, ret, GetFolderPathId());
            });

            // Async callback
            Action<object> cb = new Action<object>((rt) => {
                SyncRetValue rtValue = (SyncRetValue)rt;

                // find the specify updated node
                INxlFile updatedNode = null;
                if (rtValue.IsSuc)
                {
                    foreach (var one in rtValue.results)
                    {
                        if (selectedFile != null && selectedFile.Name == one.Name)
                        {
                            updatedNode = one;
                            break;
                        }
                    }
                }

                callback?.Invoke(rtValue.IsSuc, updatedNode);
            });

            // Invoke
            AsyncHelper.RunAsync(asyncTask, cb);
        }

        public override void DownloadFile(INxlFile nxl, bool isViewOnly, OnDownloadComplete callback,
             bool isDownloadPartial = false, bool isOnlineView = false)
        {
            DownloadManager.GetSingleton()
                .SubmitToTaskQueue(nxl)
                .TryDownload(callback, isViewOnly, isDownloadPartial, isOnlineView);
        }

        // Async task to run
        public override void UploadFile(string fileLocalPath, string destFolder, OnOprationComplete callback, bool overwrite = false)
        {
            // Async worker
            Func<object> asyncTask = new Func<object>(() => {

                bool bSuc = true;
                try
                {
                    App.MyDrive.UploadFile(fileLocalPath, destFolder, overwrite);
                }
                catch (Exception e)
                {
                    bSuc = false;
                    App.Log.Error("Invoke MyDrive UploadFile failed.");
                }

                return bSuc;
            });

            // Async callback
            Action<object> cb = new Action<object>((rt) => {
                callback?.Invoke((bool)rt);
            });

            // Invoke
            AsyncHelper.RunAsync(asyncTask, cb);
        }

        // Sync operate
        public override void CreateFolder(string name, string parantFolder)
        {
            try
            {
                App.MyDrive.CreateFolder(name, parantFolder);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        #endregion // Impl public interface.

        public override void GetRmsFilesRecursivelyFromDB(string pathId, IList<INxlFile> outRet)
        {
            try
            {
                IMyDriveFile[] files = App.MyDrive.List(pathId);
                foreach (var f in files)
                {
                    if (f.IsFolder)
                    {
                        NxlFolder folder = new MyDriveFolder(f);
                        outRet.Add(folder);

                        // Recusively get the folder children.
                        folder.Children = new List<INxlFile>();
                        GetRmsFilesRecursivelyFromDB(folder.PathId, folder.Children);
                    }
                    else
                    {
                        var doc = new MyDriveDoc(f);
                        outRet.Add(doc);

                        // Submit the file that downloading failed (may caused by crash, killed when downloading) into task queue.
                        // -- restore download
                        if (doc.FileStatus == EnumNxlFileStatus.Downloading)
                        {
                            DownloadManager.GetSingleton().SubmitToTaskQueue(doc);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                App.Log.Error("Invoke GetLocalFilesRecursively failed.", e);
            }
        }

        public override void SyncFilesRecursively(string pathId, IList<INxlFile> results)
        {
            try
            {
                IMyDriveFile[] files = App.MyDrive.Sync(pathId);
                foreach (var f in files)
                {
                    if (f.IsFolder)
                    {
                        NxlFolder folder = new MyDriveFolder(f);
                        results.Add(folder);

                        // Recursively get its children nodes
                        folder.Children = new List<INxlFile>();
                        SyncFilesRecursively(folder.PathId, folder.Children);
                    }
                    else
                    {
                        results.Add(new MyDriveDoc(f));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                App.Log.Error("Invoke SyncFilesRecursively failed.");
                throw;
            }
        }


        private string GetFolderPathId()
        {
            return CurrentWorkingFolder.PathId;
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

        private sealed class SyncRetValue
        {
            public bool IsSuc { get; }
            public string ItemFlag { get; }
            public List<INxlFile> results { get; }

            public SyncRetValue(bool isSuc, List<INxlFile> rt, string itemFlag = "")
            {
                this.IsSuc = isSuc;
                this.ItemFlag = itemFlag;
                this.results = rt;
            }
        }
    }

    public sealed class MyDriveDoc: NxlDoc
    {
        public IMyDriveFile Raw { get; set; }
        public MyDriveDoc(IMyDriveFile raw)
        {
            this.Raw = raw;
            //
            this.Name = raw.Name;
            this.Size = raw.FileSize;
            this.DateModified = DateTimeHelper.DateTimeToTimestamp(raw.LastModifiedTime.ToLocalTime()).ToString();
            this.Location = InitLocation(raw);

            this.LocalPath = raw.LocalDiskPath;
            this.DisplayPath = raw.PathDisplay;
            this.PathId = raw.PathId;

            this.IsMarkedOffline = raw.IsOffline;
            this.FileStatus = raw.Status;
            this.FileRepo = EnumFileRepo.REPO_MYDRIVE;
            this.IsCreatedLocal = false;
            this.FileId = "";
            this.PartialLocalPath = "";
            this.SourcePath = "SkyDRM://" + FileSysConstant.MYDRIVE + Raw.PathDisplay;
            this.IsNxlFile = !raw.IsNormalFile;
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

        public override IFileInfo FileInfo => Raw.FileInfo;

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

        public override void Remove()
        {
            try
            {
                Raw?.DeleteItem();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public override bool UnMark()
        {
            try
            {
                Raw?.RemoveFromLocal();
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
            Raw?.Download();
            LocalPath = Raw?.LocalDiskPath;
        }

        private EnumFileLocation InitLocation(IMyDriveFile raw)
        {
            return (raw.Status == EnumNxlFileStatus.CachedFile
                   || raw.IsOffline) ? EnumFileLocation.Local : EnumFileLocation.Online;
        }
    }

    public sealed class MyDriveFolder: NxlFolder
    {
        public IMyDriveFile Raw { get; set; }

        public MyDriveFolder(IMyDriveFile raw)
        {
            this.Raw = raw;

            this.Name = raw.Name;
            this.Size = raw.FileSize;
            this.Location = EnumFileLocation.Online;
            this.FileStatus = EnumNxlFileStatus.Online;
            this.FileRepo = EnumFileRepo.REPO_MYDRIVE;
            this.DateModified = DateTimeHelper.DateTimeToTimestamp(raw.LastModifiedTime.ToLocalTime()).ToString();
            //
            this.LocalPath = raw.LocalDiskPath;
            this.DisplayPath = raw.PathDisplay;
            this.PathId = raw.PathId;
        }
    }
}
