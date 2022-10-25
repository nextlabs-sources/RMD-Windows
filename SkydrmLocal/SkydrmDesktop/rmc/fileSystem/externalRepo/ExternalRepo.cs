using CustomControls.components.DigitalRights.model;
using SkydrmDesktop.rmc.common.helper;
using SkydrmDesktop.rmc.featureProvider.externalDrive;
using SkydrmDesktop.rmc.fileSystem.externalDrive.externalBase;
using SkydrmDesktop.rmc.fileSystem.utils;
using SkydrmLocal.rmc.common.component;
using SkydrmLocal.rmc.fileSystem;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.fileSystem.externalDrive
{
    public delegate void OnSyncComplete(bool isSucceed, List<INxlFile> results);

    public class ExternalRepo: AbstractFileRepo
    {
        private readonly SkydrmApp App = SkydrmApp.Singleton;
        private IExternalDrive drive;

        // Mainly used for treeview model data binding.
        private IList<INxlFile> FilePool { get; set; }

        // Flag that if is executing sync all data from rms.
        public bool IsLoading { get; private set; }

        public ExternalRepo(IExternalDrive ed)
        {
            this.drive = ed;

            FilePool = new List<INxlFile>();
        }

        public override string RepoDisplayName { get => drive.DisplayName; set => drive.DisplayName = value; }
        public override string RepoType => FileSysConstant.GetExternalRepoName(drive.Type);
        public override string RepoId { get => drive.RepoId; }
        public override RepositoryProviderClass RepoProviderClass { get => RepositoryProviderClass.PERSONAL; }

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
            IList<INxlFile> rt = new List<INxlFile>();

            InnerGetFilesRecursivelyFromDB("/", rt);

            // store file object pool
            FilePool = rt;

            return rt;
        }

        /// <summary>
        /// Get files recursively from DB (inluding local created and remote nodes.)
        /// </summary>
        /// <param name="pathId"></param>
        /// <param name="results"></param>
        /// <returns></returns>
        private void InnerGetFilesRecursivelyFromDB(string pathId, IList<INxlFile> results)
        {
            try
            {
                // Get local created files from db.
                foreach (var one in GetPendingFilesFromDB(pathId))
                {
                    results.Add(one);
                }

                IExternalDriveFile[] files = drive.ListFiles(pathId);
                foreach (var f in files)
                {
                    if (f.IsFolder)
                    {
                        NxlFolder folder = new DriveFolder(f);

                        // Recusiovely get the folder children.
                        folder.Children = new List<INxlFile>();
                        InnerGetFilesRecursivelyFromDB(folder.PathId, folder.Children);

                        results.Add(folder);
                    }
                    else
                    {
                        var doc = new DriveDoc(drive, f);
                        results.Add(doc);

                        // Submit the file that downloading failed (may caused by crash, 
                        // killed when downloading) into task queue.
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
                App.Log.Error("Invoke InnerGetFilesRecursivelyFromDB failed.", e);
            }
        }

        /// <summary>
        /// Get files from DB (inluding local created and remote nodes.)
        /// </summary>
        /// <param name="pathId"></param>
        /// <param name="results"></param>
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

                IExternalDriveFile[] files = drive.ListFiles(pathId);
                foreach (var f in files)
                {
                    if (f.IsFolder)
                    {
                        NxlFolder folder = new DriveFolder(f);
                        results.Add(folder);
                    }
                    else
                    {
                        var doc = new DriveDoc(drive, f);
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
        /// Get specified pathId's local added files from db.
        /// </summary>
        /// <param name="pathId"></param>
        /// <returns></returns>
        private IList<INxlFile> GetPendingFilesFromDB(string pathId)
        {
            IList<INxlFile> ret = new List<INxlFile>();
            foreach (var one in drive.ListLocalFiles(pathId))
            {
                ret.Add(new PendingUploadFile(one));
            }

            return ret;
        }

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

        public override void GetRmsFilesRecursivelyFromDB(string pathId, IList<INxlFile> outRet)
        {
            if (outRet == null)
            {
                return;
            }

            try
            {
                IExternalDriveFile[] files = drive.ListFiles(pathId);
                foreach (var one in files)
                {
                    if (one.IsFolder)
                    {
                        NxlFolder folder = new DriveFolder(one);
                        outRet.Add(folder);

                        // get the folder children recursively.
                        folder.Children = new List<INxlFile>();
                        GetRmsFilesRecursivelyFromDB(one.CloudPathId, folder.Children);
                    }
                    else
                    {
                        outRet.Add(new DriveDoc(drive,one));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                App.Log.Error("Invoke GetRemoteFiles failed.");
            }
        }

        #region Use for display all folder in UI, not contain files
        /// <summary>
        /// Get all folder nodes from local, used for select destination folder window
        /// </summary>
        /// <returns></returns>
        public IList<INxlFile> GetAllFoldersFromDB()
        {
            IList<INxlFile> rt = new List<INxlFile>();
            GetRmsFoldersRecursivelyFromDB("/", rt);

            return rt;
        }
        private void GetRmsFoldersRecursivelyFromDB(string pathId, IList<INxlFile> outRet)
        {
            if (outRet == null)
            {
                return;
            }

            try
            {
                IExternalDriveFile[] files = drive.ListFiles(pathId);
                foreach (var one in files)
                {
                    if (one.IsFolder)
                    {
                        NxlFolder folder = new DriveFolder(one);
                        outRet.Add(folder);

                        // get the folder children recursively.
                        folder.Children = new List<INxlFile>();
                        GetRmsFoldersRecursivelyFromDB(one.CloudPathId, folder.Children);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                App.Log.Error("Invoke GetRemoteFiles failed.");
            }
        }
        #endregion

        /// <summary>
        /// Get current working folder's file nodes from remote.
        /// </summary>
        /// <param name="callback">callback when complete</param>
        /// <param name="itemFlag">Joint the repoId and its couldPathId together as the 'itemFlag'.</param>
        public override void SyncFiles(OnRefreshComplete callback, string itemFlag = null)
        {
            // Async worker
            Func<object> asyncTask = new Func<object>(() => {

                bool bSucceed = true;
                List<IExternalDriveFile> ret = null;
                try
                {
                    string pathid = GetFolderPathId();
                    ret = drive.SyncFiles(pathid).ToList();
                }
                catch (Exception e)
                {
                    App.Log.Error(e.ToString());
                    bSucceed = false;
                }
                return new SyncRetValue(bSucceed, ret, string.IsNullOrEmpty(itemFlag) ? GetFolderPathId() : itemFlag);
            });

            // Async callback
            Action<object> cb = new Action<object>((rt) => {

                SyncRetValue rtValue = (SyncRetValue)rt;

                // handle the results
                List<INxlFile> r = new List<INxlFile>();
                if (rtValue.IsSuc)
                {
                    var ret = rtValue.results;
                    foreach (var f in ret)
                    {
                        if (f.IsFolder)
                        {
                            NxlFolder folder = new DriveFolder(f);
                            r.Add(folder);
                        }
                        else
                        {
                            r.Add(new DriveDoc(drive, f));
                        }
                    }

                    // update file pool
                    UpdateFilePool(GetFolderPathId(), r);
                }
                else
                {
                    Console.WriteLine("External repository sync file failed!");
                }

                callback.Invoke(rtValue.IsSuc, r, rtValue.ItemFlag);
            });

            // Invoke
            AsyncHelper.RunAsync(asyncTask, cb);
        }

        /// <summary>
        /// Sync workspace all files from rms.
        ///     Actually after aync, the implementation mechanism underlying is:
        ///     will merge with locals first and update into local db again then return directly locals.
        /// </summary>
        /// <param name="callback"></param>
        public void SyncAllFiles(OnSyncComplete callback)
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

        // Note: pathId is made by joint fId (/1_C4Hc0n0ZScw90V1fHhfUPHR4uh7aaEi/), the root is "/".
        public override void SyncFilesRecursively(string pathId, IList<INxlFile> results)
        {
            try
            {
                IExternalDriveFile[] files = drive.SyncFiles(pathId);
                foreach (var f in files)
                {
                    if (f.IsFolder)
                    {
                        NxlFolder folder = new DriveFolder(f);
                        results.Add(folder);

                        // Recursively get its children nodes
                        folder.Children = new List<INxlFile>();
                        SyncFilesRecursively(folder.PathId, folder.Children);
                    }
                    else
                    {
                        results.Add(new DriveDoc(drive, f));
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

        public override IList<INxlFile> GetOfflines()
        {
            IList<INxlFile> rt = new List<INxlFile>();
            foreach (var i in drive.GetOfflines())
            {
                var of = new DriveDoc(drive, (IExternalDriveFile)i);
                rt.Add(of);
            }

            return rt;
        }

        public override IList<INxlFile> GetPendingUploads()
        {
            IList<INxlFile> rt = new List<INxlFile>();
            foreach (var i in drive.GetPendingUploads())
            {
                var of = new PendingUploadFile(i);
                rt.Add(of);
            }

            return rt;
        }

        public override void UploadFileEx(string localPath, string name, string cloudPathId,
            bool isOverwrite = false, IUploadProgressCallback callback = null)
        {
            Action action = new Action(() => {
                drive.Upload(localPath, name, cloudPathId, isOverwrite, callback); 
            });

            AsyncHelper.RunAsync(action);
        }

        public override void UpdateToken(string newToken)
        {
            if (!string.IsNullOrEmpty(newToken))
            {
                drive.AccessToken = newToken;
            }
        }

        public PendingUploadFile AddLocalFile(string cloudPathId, string filePath,
                                   List<SkydrmLocal.rmc.sdk.FileRights> rights, WaterMarkInfo waterMark,
                                   Expiration expiration, UserSelectTags tags)
        {
            var rt = drive.AddLocalFile(cloudPathId, filePath, rights, waterMark, expiration, tags);
            return new PendingUploadFile(rt);
        }

        public override void DownloadFile(INxlFile nxl, bool isViewOnly, OnDownloadComplete callback,
             bool isDownloadPartial = false, bool isOnlineView = false)
        {
            DownloadManager.GetSingleton()
             .SubmitToTaskQueue(nxl)
             .TryDownload(callback, isViewOnly, isDownloadPartial, isOnlineView);
        }

        #region Private methods

        private string GetFolderPathId()
        {
            return CurrentWorkingFolder.PathId;
        }

        private void UpdateFilePool(string pathid, List<INxlFile> rt)
        {
            if (pathid == "/")
            {
                FilePool = rt;
            }
            else
            {
                NxlFolder toFind = null;
                FindParentNode(FilePool, pathid, ref toFind);
                if (toFind != null)
                {
                    toFind.Children = rt;
                }
            }
        }

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

        #endregion // Private methods

        private sealed class SyncRetValue
        {
            public bool IsSuc { get; }
            public string ItemFlag { get; }
            public List<IExternalDriveFile> results { get; }

            public SyncRetValue(bool isSuc, List<IExternalDriveFile> rt, string itemFlag = "")
            {
                this.IsSuc = isSuc;
                this.ItemFlag = itemFlag;
                this.results = rt;
            }
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

    }

}
