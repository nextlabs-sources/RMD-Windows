using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SkydrmDesktop;
using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.common.helper;
using SkydrmDesktop.rmc.fileSystem;
using SkydrmDesktop.rmc.fileSystem.utils;
using SkydrmLocal.rmc.common.component;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.Edit;
using SkydrmLocal.rmc.exception;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.featureProvider.common;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui;
using SkydrmLocal.rmc.ui.utils;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.viewModel;

namespace SkydrmLocal.rmc.fileSystem.project
{
    public class ProjectRepo : AbstractFileRepo
    {
        private readonly SkydrmApp App = SkydrmApp.Singleton;
        private static readonly object locker = new object();

        // Current working project & folder
        private ProjectData currentWorkingProject;
        public ProjectData CurrentWorkingProject
        {
            get { return currentWorkingProject; }

            set
            {
                currentWorkingProject = value;
                SelectedSaveProject = currentWorkingProject.ProjectInfo.Raw;
            }
        }

        // Used to store user selected dest project when doing protect operation by explorer.
        public IMyProject SelectedSaveProject { get; set; }

        // For all project files
        private IList<ProjectData> filePool;
        public IList<ProjectData> FilePool
        {
            get
            {
                // Fix bug 53906, the projects should be ordered by project name, and the projects created by
                // me, invited by others should be separated.
                var list = filePool.OrderBy(p => p.ProjectInfo.Name).ToList();
                var fpList = list.OrderByDescending(p => p.ProjectInfo.BOwner).ToList();
                return fpList;
            }
        }

        // Flag that is loading data, Binding UI to display
        public bool IsLoading { get; private set; }

        public ProjectRepo()
        {
            this.filePool = new List<ProjectData>();

            sycnDataWorker.WorkerReportsProgress = false;
            sycnDataWorker.WorkerSupportsCancellation = true;
            sycnDataWorker.DoWork += SyncData_Handler;
            sycnDataWorker.RunWorkerCompleted += SyncDataCompleted_Handler;
        }

        public override string RepoDisplayName { get => FileSysConstant.PROJECT; set => new NotImplementedException(); }
        public override string RepoType => FileSysConstant.PROJECT;

        public override string RepoId
        {
            get
            {
                //CurrentWorkingProject is null when add file from local.
                return CurrentWorkingProject == null ? "": CurrentWorkingProject.ProjectInfo.ProjectId.ToString();
            }
            set
            {
                CurrentWorkingProject.ProjectInfo.ProjectId = int.Parse(value);
            }
        }

        public override IList<INxlFile> GetFilePool()
        {
            return currentWorkingProject?.FileNodes;
        }

        // Sync worker
        private BackgroundWorker sycnDataWorker = new BackgroundWorker();

        private OnSyncProjectComplete syncResult;

        #region Sync all projects remote data from rms.
        /// <summary>
        /// Sync all projects remote data from rms.
        /// </summary>
        /// <param name="callback">call back</param>
        /// <param name="IsInitialCalled">Flag that means this function if is invoked when initial data.</param>
        public void SyncAllRemoteData(OnSyncProjectComplete callback, bool IsInitialCalled = true)
        {
            if (!sycnDataWorker.IsBusy)
            {
                if (IsInitialCalled)
                {
                    IsLoading = true;
                }

                this.syncResult = callback;
                sycnDataWorker.RunWorkerAsync(new SyncAllRemoteConfig(true, IsInitialCalled));
            }
        }

        private void SyncData_Handler(object sender, DoWorkEventArgs args)
        {
            var config = (SyncAllRemoteConfig)args.Argument;

            config.IsSucceed = InnerSyncRemoteData();
            args.Result = config;
        }

        // Try to get all projects remote data by sync way.
        public bool InnerSyncRemoteData()
        {
            bool ret = false;

            // fix bug 55904, add lock
            lock (locker)
            {
                try
                {
                    // Get all projects
                    IList<ProjectInfo> projectInfos = SyncProjects();

                    IList<ProjectData> projectDatas = new List<ProjectData>();

                    if (projectInfos.Count > 0)
                    {
                        // For each project, sync its all file nodes.
                        foreach (ProjectInfo one in projectInfos)
                        {
                            // get each project data
                            IList<INxlFile> files = new List<INxlFile>();
                            SyncRemoteAllFiles(one, "/", files);

                            ProjectData projectData = new ProjectData(one, files);
                            projectDatas.Add(projectData);
                        }
                    }
                    filePool = projectDatas;

                    ret = true;
                }
                catch (Exception e)
                {
                    ret = false;
                    App.Log.Error("Sync projects failed: " + e.ToString());
                }

            }

            return ret;
        }

        private void SyncDataCompleted_Handler(object sender, RunWorkerCompletedEventArgs args)
        {
            var config = (SyncAllRemoteConfig)args.Result;

            if(config.IsInitialCalled)
                IsLoading = false;
            
            // callback
            this.syncResult?.Invoke(config.IsSucceed, filePool);
        }

        private sealed class SyncAllRemoteConfig
        {
            public bool IsSucceed { get; set; }
            public bool IsInitialCalled { get; set; }
            public SyncAllRemoteConfig(bool is_succeed, bool is_initialCalled)
            {
                this.IsSucceed = is_succeed;
                this.IsInitialCalled = is_initialCalled;
            }
        }

        #endregion // Sync all projects remote data from rms.


        // Get all data: local created files and remote files
        public IList<ProjectData> GetAllData()
        {
            App.Log.Info("Get Project all files from DB.");

            IList<ProjectData> tmp = new List<ProjectData>();

            try
            {
                // get each project data
                foreach (ProjectInfo pi in GetProjects())
                {
                    // fill this projects all file first
                    IList<INxlFile> fsBypi = new List<INxlFile>();
                    var files = pi.Raw.ListAllProjectFile(true);
                    InnerGetAllFilesRecursivelyFromDB(pi, files, "/", fsBypi);
                    // insert
                    tmp.Add(new ProjectData(pi, fsBypi));
                }
                // clear old and assign it to new
                filePool = tmp;
            }
            catch (Exception e)
            {
                App.Log.Error("Get Project all files from DB failed: " + e.ToString());
            }

            return filePool;
        }

        /// <summary>
        /// Inner impl to get one project files recursively from DB, including local protected file and remote file nodes.
        /// </summary>
        private void InnerGetAllFilesRecursivelyFromDB(ProjectInfo project, IProjectFile[] files, string pathId, IList<INxlFile> results)
        {
            // get local created files from db
            IList<INxlFile> localFiles = GetPendingFilesFromDB(project, pathId);
            foreach (INxlFile i in localFiles)
            {
                results.Add(i);
            }

            foreach (var f in files)
            {
                if (FileHelper.IsDirectChild(f.RsmPathId, pathId))
                {
                    if (f.isFolder)
                    {
                        NxlFolder folder = new ProjectFolder(f);
                        // recusively get the folder children.
                        folder.Children = new List<INxlFile>();
                        InnerGetAllFilesRecursivelyFromDB(project, files, folder.PathId, folder.Children);

                        results.Add(folder);
                    }
                    else
                    {
                        INxlFile pDoc = new ProjectRmsDoc(f, project.ProjectId, project.DisplayName);

                        results.Add(pDoc);

                        // Submit the file that downloading failed (may caused by crash, killed when downloading) into task queue. -- restore download
                        if (pDoc.FileStatus == EnumNxlFileStatus.Downloading)
                        {
                            DownloadManager.GetSingleton().SubmitToTaskQueue(pDoc);
                        }

                    }
                }
            }
        }

        /// <summary>
        /// Inner impl to get one project files from DB, including local protected file and remote file nodes.
        /// </summary>
        /// <param name="project"></param>
        /// <param name="pathId"></param>
        /// <param name="results"></param>
        private void InnerGetProjectFilesFromDB(ProjectInfo project, string pathId, IList<INxlFile> results)
        {
            try
            {
                // get local created files from db
                IList<INxlFile> localFiles = GetPendingFilesFromDB(project, pathId);
                foreach (INxlFile i in localFiles)
                {
                    results.Add(i);
                }

                // Get project remote all files from db
                IProjectFile[] files = project.Raw.ListFiles(pathId);
                foreach (IProjectFile f in files)
                {
                    if (f.isFolder)
                    {
                        NxlFolder folder = new ProjectFolder(f);

                        results.Add(folder);
                    }
                    else
                    {
                        INxlFile pDoc = new ProjectRmsDoc(f, project.ProjectId, project.DisplayName);

                        results.Add(pDoc);
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                App.Log.Error("Invoke InnerGetProjectFilesFromDB failed.", e);
            }
        }

        /// <summary>
        /// Get project specified pathId's local added files 
        /// </summary>
        /// <param name="project"></param>
        /// <param name="pathId"></param>
        /// <returns></returns>
        private IList<INxlFile> GetPendingFilesFromDB(ProjectInfo project, string pathId)
        {
            try
            {
                IList<INxlFile> ret = new List<INxlFile>();
                foreach (IProjectLocalFile f in project.Raw.ListLocalAdded(pathId))
                {
                    ret.Add(new PendingUploadFile(f, project.ProjectId, project.DisplayName));
                }

                return ret;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                App.Log.Error("Invoke GetLocalFilsFromDB failed.");
                throw;
            }
        }

        #region Use for display all folder in UI, not contain files
        public IList<ProjectData> GetProjectsData()
        {
            SkydrmApp.Singleton.Log.Info("Get projects info.");

            IList<ProjectData> tmp = new List<ProjectData>();
            try
            {
                // get each project data
                foreach (ProjectInfo pi in GetProjects())
                {
                    // fill this projects all file first
                    IList<INxlFile> fsBypi = new List<INxlFile>();
                    // insert
                    tmp.Add(new ProjectData(pi, fsBypi));
                }
            }
            catch (Exception e)
            {
                App.Log.Error("Get projects failed: " + e.ToString());
            }

            return tmp;
        }


        public IList<ProjectData> GetAllProjectAndFolder()
        {
            App.Log.Info("Get all project folders.");

            IList<ProjectData> tmp = new List<ProjectData>();
            try
            {
                // get each project data
                foreach (ProjectInfo pi in GetProjects())
                {
                    // fill this projects all file first
                    IList<INxlFile> fsBypi = new List<INxlFile>();
                    IProjectFile[] files = pi.Raw.ListAllProjectFile(true);

                    BuildFolders(files, "/", fsBypi);
                    // insert
                    tmp.Add(new ProjectData(pi, fsBypi));
                }
            }
            catch (Exception e)
            {
                App.Log.Error("Get all project folders failed: " , e);
            }

            return tmp;
        }

        private void BuildFolders(IProjectFile[] files, string pathId, IList<INxlFile> results)
        {
            foreach (var f in files)
            {
                if (FileHelper.IsDirectChild(f.RsmPathId, pathId) && f.isFolder)
                {
                    NxlFolder folder = new ProjectFolder(f);
                    results.Add(folder);

                    // Recusively get the folder children.
                    folder.Children = new List<INxlFile>();
                    BuildFolders(files, folder.PathId, folder.Children);
                }
            }
        }

        #endregion

        // Get all projects info from local cache
        public IList<ProjectInfo> GetProjects()
        {
            try
            {
                IList<ProjectInfo> rt = new List<ProjectInfo>();
                foreach (IMyProject p in App.MyProjects.List())
                {
                    rt.Add(new ProjectInfo((IMyProject)p));
                }
                return rt;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                App.Log.Error("Invoke GetProjects failed." + e.ToString());
                throw;
            }

        }

        // Get all projects info from rms -- Can't invoke in ui thread.
        private IList<ProjectInfo> SyncProjects()
        {
            IList<ProjectInfo> rt = new List<ProjectInfo>();
            try
            {
                foreach (IMyProject p in App.MyProjects.Sync())
                {
                    rt.Add(new ProjectInfo((IMyProject)p));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                App.Log.Error("Invoke GetProjects failed." + e.ToString());
                throw e;
            }

            return rt;
        }

        // Get project specified folder (pathId) remote files from local cache recursively (Namely include subFolder children).
        public void GetRemoteFilesRecursivelyFromDB(ProjectInfo project, string pathId, IList<INxlFile> outRet)
        {
            if (outRet == null)
            {
                return;
            }

            try
            {
                IProjectFile[] files = project.Raw.ListFiles(pathId);
                foreach (IProjectFile one in files)
                {
                    if (one.isFolder)
                    {
                        NxlFolder folder =  new ProjectFolder(one);
                        outRet.Add(folder);

                        // get the folder children recursively
                        folder.Children = new List<INxlFile>();
                        GetRemoteFilesRecursivelyFromDB(project, one.RMSDisplayPath, folder.Children);
                    }
                    else
                    {
                        outRet.Add(new ProjectRmsDoc(one, project.ProjectId, project.DisplayName));
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                App.Log.Error("Invoke GetRemoteCurrentFolderFiles failed.");
            }
        }

        /// <summary>
        ///Get only project current folder (pathId) remote files from rms.
        /// </summary>
        /// <param name="project"></param>
        /// <param name="pathId"></param>
        /// <returns></returns>
        private IList<INxlFile> SyncRemoteCurrentForderFiles(ProjectInfo project, string pathId)
        {
            IList<INxlFile> list = new List<INxlFile>();

            try
            {
                IProjectFile[] files = project.Raw.SyncFilesEx(pathId, FilterType.ALL_FILES);
                foreach (IProjectFile f in files)
                {
                    if (f.isFolder)
                    {
                        list.Add(new ProjectFolder(f));
                    }
                    else // file
                    {
                        list.Add(new ProjectRmsDoc(f, project.ProjectId, project.DisplayName));
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                App.Log.Error("Invoke GetRemoteCurrentFolderFiles failed.");
                throw;
            }

            return list;
        }

        /// <summary>
        /// Get project specified folder (pathId) and sub-folder's all remote files from rms.
        /// </summary>
        /// <param name="project"></param>
        /// <param name="pathId"></param>
        /// <param name="results"></param>
        public void SyncRemoteAllFiles(ProjectInfo project, string pathId, IList<INxlFile> results)
        {
            try
            {
                IProjectFile[] files = project.Raw.SyncFilesEx(pathId, FilterType.ALL_FILES);
                foreach (IProjectFile f in files)
                {
                    if (f.isFolder)
                    {
                        NxlFolder folder = new ProjectFolder(f);
                        results.Add(folder);
                        // recusively get the folder children.
                        folder.Children = new List<INxlFile>();
                        SyncRemoteAllFiles(project, f.RsmPathId, folder.Children);
                    }
                    else
                    {
                        results.Add(new ProjectRmsDoc(f, project.ProjectId, project.DisplayName));
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                App.Log.Error("Invoke GetRemoteCurrentFolderFiles failed.");

                throw e;
            }
        }


        /// <summary>
        /// Get specified project's current working folder file nodes: local & remote files in db.
        /// </summary>
        /// <returns></returns>
        public override IList<INxlFile> GetWorkingFolderFilesFromDB()
        {
            IList<INxlFile> ret = new List<INxlFile>();

            InnerGetProjectFilesFromDB(CurrentWorkingProject.ProjectInfo, GetFolderPathId(), ret);

            return ret;
        }

        // Search specified file in File Pool by file name.
        public INxlFile SearchFileInFilePool(string localPath)
        {
            foreach(var projData in FilePool)
            {
                foreach(var f in projData.FileNodes)
                {
                    if (f.LocalPath == localPath)
                    {
                        return f;
                    }
                }
            }

            return null;
        }

        private string GetFolderPathId()
        {
            return CurrentWorkingFolder.PathId;
        }

        #region //  Refresh current current working folder from rms
        /// <summary>
        /// Sync current folder all nodes from rms.
        /// </summary>
        /// <param name="results"></param>
        /// <param name="itemFlag">Join current projectId and its pahtId together.</param>
        public override void SyncFiles(OnRefreshComplete results, string itemFlag = null)
        {
            Console.WriteLine("SyncFiles -->");

            BackgroundWorker refreshWorker = new BackgroundWorker();
            refreshWorker.WorkerReportsProgress = false;
            refreshWorker.WorkerSupportsCancellation = true;
            refreshWorker.DoWork += Refresh_Handler;
            refreshWorker.RunWorkerCompleted += RefreshCompleted_Handler;

            if (!refreshWorker.IsBusy)
            {
                refreshWorker.RunWorkerAsync(new RefreshConfig(CurrentWorkingProject.ProjectInfo, GetFolderPathId(), refreshWorker, results, itemFlag));
            }
        }

        /// <summary>
        ///  Sync the specified file to see if this file is modified.
        /// </summary>
        /// <param name="selectedFile">the file will to update</param>
        /// <param name="result">the callback results</param>
        /// <param name="bNeedFindParent">Flag that indicates if need to find the parent folder or project.
        /// if make sure that the selected file is under current working folder, don't need to find parent folder(including project and folder),
        /// or else, need to find the parent folder first. </param>
        public override void SyncDestFile(INxlFile selectedFile, OnSyncDestComplete result, bool bNeedFindParent = false)
        {
            Console.WriteLine("SyncDestFile -->");
            //App.Log.Info("SyncDestFile -->");

            BackgroundWorker refreshWorker = new BackgroundWorker();
            refreshWorker.WorkerReportsProgress = false;
            refreshWorker.WorkerSupportsCancellation = true;
            refreshWorker.DoWork += Refresh_Handler;
            refreshWorker.RunWorkerCompleted += RefreshCompleted_Handler;

            if (refreshWorker.IsBusy)
            {
                return;
            }

            if (!bNeedFindParent) 
            {
                refreshWorker.RunWorkerAsync(new RefreshConfig(CurrentWorkingProject.ProjectInfo, GetFolderPathId(), refreshWorker, selectedFile, result));
            }
            else
            {
                ProjectData findProject = null;
                foreach (var pro in filePool)
                {
                    //bool ret = false;
                    //IsFileInThisProject(selectedFile, pro.FileNodes, ref ret);
                    if (selectedFile.RepoId.Equals(pro.ProjectInfo.ProjectId.ToString()))
                    {
                        findProject = pro;
                        break;
                    }
                }

                if (findProject != null)
                {
                    var pathId = GetFolderId(selectedFile.PathId);
                    refreshWorker.RunWorkerAsync(new RefreshConfig(findProject.ProjectInfo, pathId, refreshWorker, selectedFile, result));
                }
                else
                {
                    result?.Invoke(false, null);
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
                    pathId = pathId.Substring(0, pathId.LastIndexOf('/') + 1);
                }
                else
                {
                    pathId = "/";
                }

                bool result = int.TryParse(nxl.RepoId, out int repoId);
                if (!result)
                {
                    return;
                }

                ProjectData findProject = null;
                foreach (var item in FilePool)
                {
                    if (item.ProjectInfo.ProjectId == repoId)
                    {
                        findProject = item;
                        break;
                    }
                }
                if (findProject == null)
                {
                    return;
                }

                results = SyncRemoteCurrentForderFiles(findProject.ProjectInfo, pathId.ToLower());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                App.Log.Error("Invoke SyncParentNodeFile failed.");
                throw;
            }
        }

        private void Refresh_Handler(object sender, DoWorkEventArgs args)
        {
            RefreshConfig config = (RefreshConfig)args.Argument;

            // get remote files
            IList<INxlFile> remoteFiles = null;
            bool bSucess = true;
            try
            {
                remoteFiles = SyncRemoteCurrentForderFiles(config.Project, config.PathId);
            } catch (Exception e)
            {
                App.Log.Error(e.ToString());
                bSucess = false;

                // Handler session expiration
                GeneralHandler.TryHandleSessionExpiration(e);
            }
            finally
            {
                config.BackgroundWorker.DoWork -= Refresh_Handler;
                config.bSucess = bSucess;
                config.Results = remoteFiles;
                args.Result = config;

                if (bSucess)
                {
                    // At the same time, update the filePool
                    foreach (ProjectData one in FilePool)
                    {
                        if (one.ProjectInfo.ProjectId == config.Project.ProjectId)
                        {
                            if (config.PathId == "/")
                            {
                                one.FileNodes = remoteFiles;
                            }
                            else
                            {
                                NxlFolder toFind = null;
                                FindParentNode(one.FileNodes, config.PathId, ref toFind);
                                if(toFind != null)
                                {
                                    toFind.Children = remoteFiles;
                                }

                            }

                            break;
                        }
                    }
                }

            }

        }

        private void FindParentNode(IList<INxlFile> fileNodes, string configPathId, ref NxlFolder findNode)
        {
            if(fileNodes == null)
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

        private void RefreshCompleted_Handler(object sender, RunWorkerCompletedEventArgs args)
        {
            RefreshConfig config = (RefreshConfig)args.Result;
            config.BackgroundWorker.RunWorkerCompleted -= RefreshCompleted_Handler;

            if (config.OnRefreshComplete != null)
            {
                //App.Log.Info("OnRefreshComplete Callback -->");
                config.OnRefreshComplete?.Invoke(config.bSucess, config.Results, config.ItemFlag);
            }
            else if(config.OnSyncDestComplete != null)
            {
                //App.Log.Info("OnRefreshComplete Callback -->");

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

        // Judge specified file if belong to some project.
        private void IsFileInThisProject(INxlFile selectedFile, IList<INxlFile> nodes, ref bool result)
        {
            if (nodes == null || nodes.Count == 0)
            {
                return;
            }

            foreach (var f in nodes)
            {
                if (f.IsFolder)
                {
                    IsFileInThisProject(selectedFile, (f as NxlFolder).Children, ref result);
                }
                else
                {
                    if (f.LocalPath == selectedFile.LocalPath)
                    {
                        result = true;
                        break;
                    }
                }
            }
        }

        // Parse folderId from file display path
        private  string GetFolderId(string displayPath)
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

        #endregion // refresh current working folder.

        public override void DownloadFile(INxlFile nxlFile, bool isViewOnly, OnDownloadComplete callback, bool isDownloadPartial = false, bool isOnlineView = false)
        {
            DownloadManager.GetSingleton()
             .SubmitToTaskQueue(nxlFile)
             .TryDownload(callback, isViewOnly,isDownloadPartial, isOnlineView);
        }

        public override void MarkOffline(INxlFile nxlFile, OnDownloadComplete callback)
        {
            // todo, need to check file if has been in local
            // and the if has view rights --- need sdk support.

            DownloadFile(nxlFile, false, callback);
        }

        public override IList<INxlFile> GetOfflines()
        {
            IList<INxlFile> rt = new List<INxlFile>();
            foreach (var i in App.MyProjects.List())
            {
                foreach (var j in i.GetOfflines())
                {
                    if(j is ProjectFile)
                    {
                        ProjectRmsDoc offlineFile =  new ProjectRmsDoc((ProjectFile)j, i.Id, i.DisplayName);          
                        rt.Add(offlineFile);
                    } else if(j is featureProvider.ProjectSharedWithMeFile)
                    {
                        ProjectSharedWithMeDoc of = new ProjectSharedWithMeDoc((featureProvider.ProjectSharedWithMeFile)j, i.Id, i.DisplayName);
                        rt.Add(of);
                    }

                }

            }
            return rt;

        }

        // These files will need to be added to queue and then uploaded to rms.
        public IList<INxlFile> GetEditedOfflineFiles()
        {
            IList<INxlFile> rt = new List<INxlFile>();
            foreach (var i in App.MyProjects.List())
            {
                foreach (var j in i.GetOfflines())
                {
                    if ((j.Status == EnumNxlFileStatus.AvailableOffline || j.Status == EnumNxlFileStatus.CachedFile
                        || j.Status == EnumNxlFileStatus.Uploading) && j.IsOfflineFileEdit) // fix Bug 56962 - File fail to upload after session is expired, add file status is uploading
                    {
                        ProjectRmsDoc offlineFile = new ProjectRmsDoc((ProjectFile)j, i.Id, i.DisplayName);
                        rt.Add(offlineFile);
                    }
                }

            }
            return rt;

        }


        public override IList<INxlFile> GetPendingUploads()
        {
            IList<INxlFile> rt = new List<INxlFile>();
            foreach (var i in App.MyProjects.List())
            {
                foreach (var j in i.GetPendingUploads())
                {
                    var pf = new PendingUploadFile(j, i.Id, i.DisplayName);
                    rt.Add(pf);
                }

            }
            return rt;
        }

        public override IList<INxlFile> GetSharedByMeFiles()
        {
            IList<INxlFile> ret = new List<INxlFile>();
            try
            {
                var raw = CurrentWorkingProject.ProjectInfo.Raw;
                IProjectFile[] files = raw.ListAllProjectFile();
                foreach(var f in files)
                {
                    if(!f.isFolder && f.IsShared 
                        && !f.IsRevoked) // If file has beeen revoked, won't display it in "SharedByMe" list.
                    {
                        ret.Add(new ProjectRmsDoc(f, raw.Id, raw.DisplayName));
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
                var raw = CurrentWorkingProject.ProjectInfo.Raw;
                IProjectSharedWithMeFile[] files = raw.ListSharedWithMeFiles();
                foreach (var f in files)
                {
                    ret.Add(new ProjectSharedWithMeDoc(f, raw.Id, raw.DisplayName));
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
            Func<object> asyncTask = new Func<object>(()=> {

                bool bSuc = true;
                List<INxlFile> ret = new List<INxlFile>();
                try
                {
                    var raw = CurrentWorkingProject.ProjectInfo.Raw;
                    IProjectSharedWithMeFile[] files = raw.SyncSharedWithMeFiles();
                    foreach (IProjectSharedWithMeFile f in files)
                    {
                        ret.Add(new ProjectSharedWithMeDoc(f, raw.Id, raw.DisplayName));
                    }

                }
                catch (Exception e)
                {
                    bSuc = false;
                    App.Log.Error("Invoke GetRemoteCurrentFolderFiles failed.");
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
                    List<INxlFile> tmp = new List<INxlFile>();
                    SyncRemoteAllFiles(CurrentWorkingProject.ProjectInfo, "/", tmp);
                    // Add
                    AddSharedByMeFiles(tmp, ref ret);
                }
                catch (Exception e)
                {
                    bSuc = false;
                    App.Log.Error("Invoke project SyncSharedByMeFiles failed.");
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

        public override bool CheckFileExists(string pathId)
        {
            return CurrentWorkingProject.ProjectInfo.Raw.CheckFileExists(pathId);
        }

        private void AddSharedByMeFiles(IList<INxlFile> sourace, ref List<INxlFile> results)
        {
            foreach(var i in sourace)
            {
                if (i.IsFolder)
                {
                    NxlFolder folder = i as NxlFolder;
                    AddSharedByMeFiles(folder.Children, ref results);
                }
                else
                {
                    if (i.IsShared)
                    {
                        results.Add(i);
                    }
                }
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

        class DownloadConfig
        {
            public INxlFile File { get; set; }
            public string DestFolder { get; set; }
            public bool IsViewOnly { get; set; }

            public bool IsSuccess { get; set; }
            public OnDownloadComplete Callback { get; set; }
            public BackgroundWorker BackgroundWorker { get; set; }

            public DownloadConfig(INxlFile nxlFile, bool isViewOnly, OnDownloadComplete callback, BackgroundWorker worker, bool isSuccess)
            {
                // paras
                File = nxlFile;
                IsViewOnly = isViewOnly;
                Callback = callback;

                BackgroundWorker = worker;
                IsSuccess = isSuccess;
            }

            public DownloadConfig(INxlFile nxlFile, string destFolder, bool isViewOnly, OnDownloadComplete callback, BackgroundWorker worker, bool isSuccess)
            {
                // paras
                File = nxlFile;
                DestFolder = destFolder;
                IsViewOnly = isViewOnly;
                Callback = callback;

                BackgroundWorker = worker;
                IsSuccess = isSuccess;
            }
        }

        class RefreshConfig
        {
            public ProjectInfo Project { get; }
            public string PathId { get; }
            public BackgroundWorker BackgroundWorker { get; }
            public OnRefreshComplete OnRefreshComplete { get; }
            public bool bSucess { get; set; }
            public string ItemFlag { get; }
            public IList<INxlFile> Results { get; set; }

            // Used to sync specify file node.
            public OnSyncDestComplete OnSyncDestComplete { get; }
            public INxlFile SpecifyFile { get; }

            public RefreshConfig(ProjectInfo project, string pathId, BackgroundWorker bgworker, OnRefreshComplete callback, string itemFlag = "")
            {
                this.Project = project;
                this.PathId = pathId;
                this.BackgroundWorker = bgworker;
                this.OnRefreshComplete = callback;
                this.ItemFlag = itemFlag;

                if (string.IsNullOrEmpty(this.ItemFlag))
                {
                    this.ItemFlag = Project.ProjectId + this.PathId;
                }

                this.OnSyncDestComplete = null;
                this.SpecifyFile = null;
            }

            public RefreshConfig(ProjectInfo project, string pathId, BackgroundWorker bgworker, INxlFile selectFile, OnSyncDestComplete syncCallback)
            {
                this.Project = project;
                this.PathId = pathId;
                this.BackgroundWorker = bgworker;
                this.SpecifyFile = selectFile;
                this.OnSyncDestComplete = syncCallback;

                this.ItemFlag = "";
                this.OnRefreshComplete = null;
            }

        }


        public sealed class ProjectRmsDoc : NxlDoc
        {
            public IProjectFile Raw { get; set; }
            private string projectName;

            public ProjectRmsDoc(IProjectFile raw, int projectId, string projectDisplayName)
            {
                this.Raw = raw;
                this.projectName = projectDisplayName;

                this.RepoId = projectId.ToString();
                this.Name = raw.Name;
                this.Size = raw.FileSize;
                // In order to sort, we'll get timesStap format, then will convert dateTime again when display in ui.
                this.DateModified = DateTimeHelper.DateTimeToTimestamp(raw.LastModifiedTime.ToLocalTime()).ToString();
                this.RawDateModified = raw.LastModifiedTime;
                this.Location = InitLocation(raw);

                this.LocalPath = raw.LocalDiskPath;
                this.DisplayPath = raw.RMSDisplayPath;
                this.PathId = raw.RsmPathId;

                this.IsMarkedOffline = raw.isOffline;
                this.FileStatus = raw.Status;
                this.FileRepo = EnumFileRepo.REPO_PROJECT;
                this.IsCreatedLocal = false;
                this.FileId = ""; // should export fileId
                // bind partial local path.
                this.PartialLocalPath = raw.PartialLocalPath;

                this.IsEdit = raw.IsEdit;

                // sharing transaction.
                isShared = raw.IsShared;
                isRevoked = raw.IsRevoked;
                sharedWith = Helper.ConvertListUint2ListString(Raw.SharedToProjects); 
                this.SourcePath = "SkyDRM://" + FileSysConstant.PROJECT + "/" + projectName + Raw.RMSDisplayPath;
            }

            private EnumFileLocation InitLocation(IProjectFile raw)
            {
                return (raw.Status == EnumNxlFileStatus.CachedFile
                       || raw.isOffline) ? EnumFileLocation.Local : EnumFileLocation.Online;
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

            public override bool IsMarkedFileRemoteModified
            {
                get
                {
                    return Raw.IsDirty;
                }

                set
                {
                   
                    Raw.IsDirty = value;
                    NotifyPropertyChanged("IsMarkedFileRemoteModified");
                }
            }

            public override bool IsModifiedRights
            {
                get
                {
                    return Raw.IsModifyRights;
                }
                set
                {
                    Raw.IsModifyRights = value;
                }
            }

            private bool isShared;
            public override bool IsShared
            {
                get
                {
                    return isShared;
                }
                set
                {
                    // Update to db in low level.
                    Raw.IsShared = value;

                    // Need to notify refresh ui if ui bind this field.
                    isShared = value;
                    NotifyPropertyChanged("IsShared");
                }
            }

            private bool isRevoked;
            public override bool IsRevoked
            {
                get
                {
                    return isRevoked;
                }
                set
                {
                    // Update to db in low level.
                    Raw.IsRevoked = value;

                    // Need to notify refresh ui if ui bind this field.
                    isRevoked = value;
                    NotifyPropertyChanged("IsRevoked");
                }
            }

            private List<string> sharedWith;
            public override List<string> SharedWith
            {
                get
                {
                    return sharedWith;  
                }
                set
                {
                    // update to db in low level.
                    Raw.SharedToProjects = Helper.ConvertListString2ListUint(value);

                    // Need to notify refresh ui if ui bind this field.
                    sharedWith = value;
                    NotifyPropertyChanged("SharedWith");
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
                Raw?.DownlaodFile(isViewOnly);
                // update
                LocalPath = Raw?.LocalDiskPath;
            }

            public override void DownloadPartial()
            {
                //Raw?.DownloadPartial();
                Raw?.GetNxlHeader();
                PartialLocalPath = Raw?.PartialLocalPath;
            }

            public override void UploadEditedFile()
            {
                Raw?.UploadEditedFile();
            }

            public override void Edit(Action<IEditComplete> callback)
            {
                Raw?.DoEdit(callback);
            }

            public override void Export(string destFolder)
            {
                Raw?.Export(destFolder);
            }


            // Sharing transaction
            public override void UpdateRecipients(List<string> addList, List<string> removedList, string comment)
            {
                try
                {
                    Raw?.UpdateRecipients(Helper.ConvertListString2ListUint(addList),
                          Helper.ConvertListString2ListUint(removedList), comment);

                    // Update "SharedWith" if execute success.
                    UpdateSharedWith(addList, removedList);
                }
                catch (Exception)
                {
                    throw;
                }
            }
            public override void Share(List<string> recipients, string comment)
            {
                try
                {
                    Raw?.ShareFile(Helper.ConvertListString2ListUint(recipients), comment);
                    // Update
                    SharedWith = recipients;
                    IsShared = true;
                }
                catch (Exception)
                {
                    throw;
                }
            }
            public override bool Revoke()
            {
                bool result = false;
                if(Raw != null)
                {
                    if (Raw.RevokeFile())
                    {
                        result = true;
                        // Update
                        IsRevoked = true;
                        SharedWith = new List<string>();
                    }
                }
                return result;
            }

            private void UpdateSharedWith(List<string> addList, List<string> removedList)
            {
                List<string> newList = new List<string>();
                foreach (var i in SharedWith)
                {
                    newList.Add(i);
                }

                foreach (var i in addList)
                {
                    if (!newList.Contains(i))
                    {
                        newList.Add(i);
                    }
                }
                foreach (var i in removedList)
                {
                    if (newList.Contains(i))
                    {
                        newList.Remove(i);
                    }
                }

                SharedWith = newList;
            }
        }

        public sealed class ProjectSharedWithMeDoc:NxlDoc
        {
            public IProjectSharedWithMeFile Raw { get; }
            private string projectName;

            public ProjectSharedWithMeDoc(IProjectSharedWithMeFile raw, int projectId, string proName)
            {
                this.Raw = raw;
                this.projectName = proName;

                this.RepoId = projectId.ToString();
                this.Name = raw.Name;
                this.Size = raw.FileSize;

                // In order to sort, we'll get timesStap format, then will convert dateTime again when display in ui.
                this.DateModified = DateTimeHelper.DateTimeToTimestamp(raw.SharedDate.ToLocalTime()).ToString();
                this.RawDateModified = raw.SharedDate;
                this.Location = InitLocation(raw);

                this.LocalPath = raw.LocalDiskPath;
                this.DisplayPath = "/" + Name;
                this.PathId = DisplayPath;

                this.IsMarkedOffline = raw.IsOffline;
                this.FileStatus = raw.Status;
                this.FileRepo = EnumFileRepo.REPO_PROJECT;
                this.IsCreatedLocal = false;
                this.FileId = ""; // should export fileId
                // bind partial local path.
                this.PartialLocalPath = raw.PartialLocalPath;

                //this.IsEdit = raw.IsEdit;

                // sharing transaction.
                this.SharedBy = raw.SharedBy;
                this.SharedByProject = Helper.GetProjectNameById(uint.Parse(raw.sharedByProject)); 
                this.SharedDate = DateTimeHelper.DateTimeToTimestamp(raw.SharedDate.ToLocalTime()).ToString();
                
                this.SourcePath = "SharedWithThisProject://" + projectName + "/" + Raw.Name;
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

            public override IFileInfo FileInfo => Raw.FileInfo;

            public override void Remove()
            {
                Raw?.Remove();
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

            public override void DownloadFile(bool isViewOnly = false)
            {
                Raw?.Download(isViewOnly);
                // update
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

            public override void Share(List<string> recipients, string comment)
            {
                Raw?.ReShare(Helper.ConvertListString2ListUint(recipients), comment);
            }

            private EnumFileLocation InitLocation(IProjectSharedWithMeFile raw)
            {
                return (raw.Status == EnumNxlFileStatus.CachedFile
                       || raw.IsOffline) ? EnumFileLocation.Local : EnumFileLocation.Online;
            }

        }

        public sealed class ProjectFolder:NxlFolder
        {
            public IProjectFile Raw { get; set; }

            public ProjectFolder(IProjectFile raw)
            {
                this.Raw = raw;

                this.Name = raw.Name;
                this.Size = raw.FileSize;
                this.Location = EnumFileLocation.Online;
                this.FileStatus = EnumNxlFileStatus.Online;
                this.FileRepo = EnumFileRepo.REPO_PROJECT;
                this.DateModified = DateTimeHelper.DateTimeToTimestamp(raw.LastModifiedTime.ToLocalTime()).ToString();
                this.RawDateModified = raw.LastModifiedTime;

                this.LocalPath = raw.LocalDiskPath;
                this.DisplayPath = raw.RMSDisplayPath;
                this.PathId = raw.RsmPathId;
                this.FileId = ""; // need export
            }
        }
    
    }

}
