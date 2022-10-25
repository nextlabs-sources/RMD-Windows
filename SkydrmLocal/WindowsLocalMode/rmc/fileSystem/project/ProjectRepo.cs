using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SkydrmLocal.rmc.common.component;
using SkydrmLocal.rmc.Edit;
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
    // Sync rms project data complete
    public delegate void OnSyncProjectComplete(bool bSucceed, IList<ProjectData> result);

    public delegate void NotifyProjectPageRefresh(List<ProjectData> addList, List<ProjectData> removeList);

    public delegate void NotifyProjectFileListRefresh(IList<INxlFile> nxlFiles, string param, bool isFolder);

    public class ProjectRepo : IFileRepo
    {
        private readonly SkydrmLocalApp App = SkydrmLocalApp.Singleton;
        private static readonly string PROJECT = CultureStringInfo.MainWin__TreeView_Project;
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

        // Record project viewModel of current working project
        public ProjectViewModel ProjectViewModel { get; set; }

        // Used to store user selected dest project when doing protect operation by explorer.
        public IMyProject SelectedSaveProject { get; set; }

        public NxlFolder CurrentWorkingFolder { get; set; }
        // Record folder viewModel of current working folder.
        public FolderViewModel FolderViewModel { get; set; }


        public EnumCurrentWorkingArea WorkingArea { get; set; }

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

        //Flag that is loading data, Binding UI to display
        public bool IsLoading { get; set; }

        public ProjectRepo()
        {
            this.filePool = new List<ProjectData>();

            sycnDataWorker.WorkerReportsProgress = false;
            sycnDataWorker.WorkerSupportsCancellation = true;
            sycnDataWorker.DoWork += SyncData_Handler;
            sycnDataWorker.RunWorkerCompleted += SyncDataCompleted_Handler;
        }

        // Sync worker
        private BackgroundWorker sycnDataWorker = new BackgroundWorker();

        private OnSyncProjectComplete syncResult;

        public NotifyProjectPageRefresh notifyProjectPageRefresh;

        public NotifyProjectFileListRefresh notifyProjectFileListRefresh;

        // Try to get all projects remote data by async bg way.
        public void SyncRemoteData(OnSyncProjectComplete callback)
        {
            this.syncResult = callback;
            if (!sycnDataWorker.IsBusy)
            {
                sycnDataWorker.RunWorkerAsync();
            }
        }

        private void SyncData_Handler(object sender, DoWorkEventArgs args)
        {
            bool result = InnerSyncRemoteData();
            args.Result = result;
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
                    IList<ProjectInfo> projectInfos = SyncProjects();

                    filePool.Clear();
                    if (projectInfos.Count > 0)
                    {
                        foreach (ProjectInfo one in projectInfos)
                        {
                            // get each project data
                            IList<INxlFile> files = new List<INxlFile>();
                            SyncRemoteAllFiles(one, "/", files);

                            ProjectData projectData = new ProjectData(one, files);
                            filePool.Add(projectData);
                        }
                    }

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
            bool result = (bool)args.Result;
            this.syncResult?.Invoke(result, filePool);
        }

        // Get all data: local created files and remote files
        public IList<ProjectData> GetAllData()
        {
            SkydrmLocalApp.Singleton.Log.Info("Get project local files.");

            IList<ProjectData> tmp = new List<ProjectData>();
            try
            {
                // get each project data
                foreach (ProjectInfo pi in GetProjects())
                {
                    // fill this projects all file first
                    IList<INxlFile> fsBypi = new List<INxlFile>();
                    InnerGetAllData(pi, "/", fsBypi);
                    // insert
                    tmp.Add(new ProjectData(pi, fsBypi));
                }

                // clear old and assign it to new
                filePool.Clear();
                filePool = tmp;
            }
            catch (Exception e)
            {
                App.Log.Error("Get projects failed: " + e.ToString());
            }

            return filePool;
        }
        private void InnerGetAllData(ProjectInfo project, string pathId, IList<INxlFile> results)
        {
            if (pathId.Equals("/"))
            {
                // get local files from db --  root
                foreach (INxlFile i in GetLocalFilsFromDB(project, "/"))
                {
                    i.SourcePath = project.Name + "/" + i.Name;
                    results.Add(i);
                }
            }

            try
            {
                // Get project remote all files from db
                IProjectFile[] files = project.Raw.ListFiles(pathId);
                foreach (IProjectFile f in files)
                {
                    if (f.isFolder)
                    {
                        NxlFolder folder = new NxlFolder(f);
                        // recusively get the folder children.
                        folder.Children = new List<INxlFile>();
                        InnerGetAllData(project, folder.PathId, folder.Children);

                        // get local created files from db -- subFolder.
                        IList<INxlFile> localFiles = GetLocalFilsFromDB(project, folder.PathId);
                        foreach (INxlFile i in localFiles)
                        {
                            i.SourcePath = project.Name + folder.PathId + i.Name;
                            folder.Children.Add(i);
                        }

                        results.Add(folder);
                    }
                    else
                    {
                        INxlFile pDoc = new ProjectRmsDoc(f, project.ProjectId);
                        if (f.Status == EnumNxlFileStatus.AvailableOffline)
                        {
                            pDoc.SourcePath = project.Name + pathId + pDoc.Name;
                        }

                        results.Add(pDoc);

                        // Submit the file that downloading failed (may caused by crash, killed when downloading) into task queue. -- restore download
                        if (pDoc.FileStatus == EnumNxlFileStatus.Downloading)
                        {
                            DownloadManager.GetSingleton().SubmitToTaskQueue(pDoc);
                        }

                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                App.Log.Error("Invoke GetRemoteCurrentFolderFiles failed.", e);
            }
        }

        #region Get all folder data to display In ProtectFile window
        public IList<ProjectData> GetProjectsInfo()
        {
            SkydrmLocalApp.Singleton.Log.Info("Get projects info.");

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

        public IList<ProjectData> GetAllFolder()
        {
            SkydrmLocalApp.Singleton.Log.Info("Get project local folder.");

            IList<ProjectData> tmp = new List<ProjectData>();
            try
            {
                // get each project data
                foreach (ProjectInfo pi in GetProjects())
                {
                    // fill this projects all file first
                    IList<INxlFile> fsBypi = new List<INxlFile>();
                    InnerGetFolder(pi, "/", fsBypi);
                    // insert
                    tmp.Add(new ProjectData(pi, fsBypi));
                }
            }
            catch (Exception e)
            {
                App.Log.Error("Get projects failed: " + e.ToString());
            }

            // In Protect file window will create a new ProjectRepo, will not affect other projectRepo filePool
            filePool.Clear();
            filePool = tmp;

            return tmp;
        }
        private void InnerGetFolder(ProjectInfo project, string pathId, IList<INxlFile> results)
        {
            try
            {
                // Get project remote all files from db
                IProjectFile[] files = project.Raw.ListFiles(pathId);
                foreach (IProjectFile f in files)
                {
                    if (f.isFolder)
                    {
                        NxlFolder folder = new NxlFolder(f);
                        // recusively get the folder children.
                        folder.Children = new List<INxlFile>();
                        InnerGetFolder(project, folder.PathId, folder.Children);

                        results.Add(folder);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                App.Log.Error("Invoke GetRemoteCurrentFolderFiles failed from db", e);
            }
        }
        #endregion

        // Get all projects info from db
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

        // Get project current working folder remote files from db recursively.
        public void GetRemoteCurrentFolderFiles(ProjectInfo project, string pathId, IList<INxlFile> outRet)
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
                        NxlFolder folder =  new NxlFolder(one);
                        outRet.Add(folder);

                        // get the folder children recursively
                        folder.Children = new List<INxlFile>();
                        GetRemoteCurrentFolderFiles(project, one.RMSDisplayPath, folder.Children);
                    }
                    else
                    {
                        outRet.Add(new ProjectRmsDoc(one, project.ProjectId));
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                App.Log.Error("Invoke GetRemoteCurrentFolderFiles failed.");
            }
        }

        // Get project current working folder remote files from rms
        //  -- Can't invoke in ui thread
        private IList<INxlFile> SyncRemoteCurrentForderFiles(ProjectInfo project, string pathId)
        {
            IList<INxlFile> list = new List<INxlFile>();

            try
            {
                IProjectFile[] files = project.Raw.SyncFiles(pathId);
                foreach (IProjectFile f in files)
                {
                    if (f.isFolder)
                    {
                        list.Add(new NxlFolder(f));
                    }
                    else // file
                    {
                        list.Add(new ProjectRmsDoc(f, project.ProjectId));
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

        // Get project all remote files from rms
        //  -- Can't invoke in ui thread
        public void SyncRemoteAllFiles(ProjectInfo project, string pathId, IList<INxlFile> results)
        {
            try
            {
                IProjectFile[] files = project.Raw.SyncFiles(pathId);
                foreach (IProjectFile f in files)
                {
                    if (f.isFolder)
                    {
                        NxlFolder folder = new NxlFolder(f);
                        results.Add(folder);
                        // recusively get the folder children.
                        folder.Children = new List<INxlFile>();
                        SyncRemoteAllFiles(project, f.RsmPathId, folder.Children);
                    }
                    else
                    {
                        results.Add(new ProjectRmsDoc(f, project.ProjectId));
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

        public IList<INxlFile> GetLocalFilsFromDB(ProjectInfo project, string pathId)
        {
            try
            {
                IList<INxlFile> ret = new List<INxlFile>();
                foreach (IProjectLocalFile f in project.Raw.ListLocalAdded(pathId))
                {
                    ret.Add(new PendingUploadFile(f, project.ProjectId));
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

        private void GetLocalFilsFromDbRecursive(ProjectInfo project, string pathId, IList<INxlFile> outRet)
        {
            if (outRet == null)
            {
                return;
            }

            try
            {
                foreach (IProjectLocalFile f in project.Raw.ListLocalAdded(pathId))
                {
                    if (f.RMSRemotePath.EndsWith("/")) // folder
                    {
                        NxlFolder folder = new NxlFolder(f);
                        outRet.Add(folder);

                        folder.Children = new List<INxlFile>();
                        GetLocalFilsFromDbRecursive(project, f.RMSRemotePath, folder.Children);
                    }
                    else
                    {
                        outRet.Add(new PendingUploadFile(f, project.ProjectId));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                App.Log.Error("Invoke GetLocalFilsFromDB failed.");
                throw;
            }

        }


        // Get current working folder all local files: local & remote files in db.
        public IList<INxlFile> GetLocalFiles()
        {
            IList<INxlFile> ret = new List<INxlFile>();

            IList<INxlFile> dbLocalFiles = new List<INxlFile>();
            GetLocalFilsFromDbRecursive(CurrentWorkingProject.ProjectInfo, GetFolder(), dbLocalFiles);
            foreach (var one in dbLocalFiles)
            {
                ret.Add(one);
            }

            IList<INxlFile> dbRemoteFiles = new List<INxlFile>();
            GetRemoteCurrentFolderFiles(CurrentWorkingProject.ProjectInfo, GetFolder(), dbRemoteFiles);
            foreach (var one in dbRemoteFiles)
            {
                ret.Add(one);
            }

            return ret;
        }


        private string GetFolder()
        {
            string ret = "";
            if (WorkingArea == EnumCurrentWorkingArea.PROJECT_ROOT)
                ret = "/";
            else if (WorkingArea == EnumCurrentWorkingArea.PROJECT_FOLDER)
            {
                ret = CurrentWorkingFolder.PathId;
            }

            return ret;
        }

        #region //  refresh current current working folder from rms
        public void SyncFiles(OnRefreshComplete results, string itemFlag = null)
        {
            Console.WriteLine("SyncFiles -->");
            App.Log.Info("SyncFiles -->");

            BackgroundWorker refreshWorker = new BackgroundWorker();
            refreshWorker.WorkerReportsProgress = false;
            refreshWorker.WorkerSupportsCancellation = true;
            refreshWorker.DoWork += Refresh_Handler;
            refreshWorker.RunWorkerCompleted += RefreshCompleted_Handler;

            if (!refreshWorker.IsBusy)
            {
                refreshWorker.RunWorkerAsync(new RefreshConfig(CurrentWorkingProject.ProjectInfo, GetFolder(), refreshWorker, results, itemFlag));
            }
        }

        /// <summary>
        ///  Sync the specified file to see if this file is modified.
        /// </summary>
        /// <param name="selectedFile">the file will to update</param>
        /// <param name="result">the callback results</param>
        /// <param name="bNeedFindParent">Flag that indicates if need to find the parent folder,
        /// if make sure that the selected file is under current working folder, don't need to find parent folder(including project and folder),
        /// or else, need to find the parent folder first.</param>
        public void SyncDestFile(INxlFile selectedFile, OnSyncDestComplete result, bool bNeedFindParent = false)
        {
            Console.WriteLine("SyncDestFile -->");
            App.Log.Info("SyncDestFile -->");

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
                refreshWorker.RunWorkerAsync(new RefreshConfig(CurrentWorkingProject.ProjectInfo, GetFolder(), refreshWorker, selectedFile, result));
            }
            else
            {
                ProjectData findProject = null;
                foreach (var pro in filePool)
                {
                    bool ret = false;
                    IsFileInThisProject(selectedFile, pro.FileNodes, ref ret);
                    if (ret)
                    {
                        findProject = pro;
                        break;
                    }
                }

                if (findProject != null)
                {
                    var pathId = GetFolderId(selectedFile.RmsRemotePath);
                    refreshWorker.RunWorkerAsync(new RefreshConfig(findProject.ProjectInfo, pathId, refreshWorker, selectedFile, result));
                }
                else
                {
                    result?.Invoke(false, null);
                }
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
                App.Log.Info("OnRefreshComplete Callback -->");
                config.OnRefreshComplete?.Invoke(config.bSucess, config.Results, config.ItemFlag);
            }
            else if(config.OnSyncDestComplete != null)
            {
                App.Log.Info("OnRefreshComplete Callback -->");

                INxlFile updatedNode = null;
                // find the specify updated node
                if (config.Results != null)
                {
                    foreach (var one in config.Results)
                    {
                        if (config.SpecifyFile?.Name == one.Name)
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


        #region download

        public void DownloadFile(INxlFile nxlFile, bool isViewOnly, OnDownloadComplete callback, bool isDownloadPartial = false, bool isOnlineView = false)
        {
            DownloadManager.GetSingleton()
             .SubmitToTaskQueue(nxlFile)
             .TryDownload(callback, isViewOnly,isDownloadPartial, isOnlineView);
        }

        private void Download_Handler(object sender, DoWorkEventArgs args)
        {
            DownloadConfig para = (DownloadConfig)args.Argument;

            ProjectRmsDoc docRMS = para.File as ProjectRmsDoc;
            IProjectFile projectFile = docRMS.Raw;

            bool ret = true;
            try
            {
                // projectFile.DownlaodFile(para.DestFolder, para.IsViewOnly);
                projectFile.DownlaodFile();

                docRMS.LocalPath = projectFile.LocalDiskPath;

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

        #endregion // download


        public void MarkOffline(INxlFile nxlFile, OnDownloadComplete callback)
        {
            // todo, need to check file if has been in local
            // and the if has view rights --- need sdk support.

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
                App.Log.Error(" UnmarkOffline failed!",e);
            }                

            return false;
        }

        public IList<INxlFile> GetOfflines()
        {
            IList<INxlFile> rt = new List<INxlFile>();
            foreach (var i in App.MyProjects.List())
            {
                foreach (var j in i.GetOfflines())
                {
                    ProjectRmsDoc offlineFile =  new ProjectRmsDoc((ProjectFile)j, i.Id);
           
                    offlineFile.SourcePath = "Project://"+i.DisplayName+""+ offlineFile.Raw.RMSDisplayPath;
                    rt.Add(offlineFile);
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
                        || j.Status==EnumNxlFileStatus.Uploading) && j.IsOfflineFileEdit)// fix Bug 56962 - File fail to upload after session is expired, add file status is uploading
                    {
                        ProjectRmsDoc offlineFile = new ProjectRmsDoc((ProjectFile)j, i.Id);

                        offlineFile.SourcePath = "Project://" + i.DisplayName + "" + offlineFile.Raw.RMSDisplayPath;
                        rt.Add(offlineFile);
                    }
                }

            }
            return rt;

        }


        public IList<INxlFile> GetPendingUploads()
        {
            IList<INxlFile> rt = new List<INxlFile>();
            foreach (var i in App.MyProjects.List())
            {
                foreach (var j in i.GetPendingUploads())
                {
                    var pf = new PendingUploadFile((IProjectLocalFile)j, i.Id);
                    pf.SourcePath = PROJECT+ "://" + i.DisplayName + "" + pf.RmsRemotePath;

                    rt.Add(pf);
                }

            }
            return rt;
        }

        public string GetSourcePath(INxlFile nxlfile)
        {
            string ret = "";
            if (CurrentWorkingProject == null || nxlfile == null)
            {
                return ret;
            }

            if (nxlfile is ProjectRmsDoc)
            {

                ret = "Project://" + CurrentWorkingProject.ProjectInfo.Name + (nxlfile as ProjectRmsDoc).Raw.RsmPathId;
            }
            else // doc local
            {
                if (WorkingArea == EnumCurrentWorkingArea.PROJECT_ROOT)
                {
                    ret = "Project://" + CurrentWorkingProject.ProjectInfo.Name + "/" + nxlfile.Name;
                } else if (WorkingArea == EnumCurrentWorkingArea.PROJECT_FOLDER)
                {
                    ret = "Project://" + CurrentWorkingProject.ProjectInfo.Name + CurrentWorkingFolder.PathId + nxlfile.Name;
                }
            }

            return ret;
        }

        public void UpdateToRms(INxlFile nxlFile)
        {
            throw new NotImplementedException();
        }

        public void Export(string destFolder, INxlFile nxlFile)
        {
            if (nxlFile is ProjectRmsDoc)
            {
                var doc = (ProjectRmsDoc)nxlFile;
                doc.Raw.Export(destFolder);
            }
        }

        public void Edit(INxlFile nxlFile, Action<EditCallBack> onFinishedCallback) 
        {
            if (nxlFile is ProjectRmsDoc)
            {
                var doc = nxlFile as ProjectRmsDoc;
                doc.Raw.DoEdit(onFinishedCallback);
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

            public ProjectRmsDoc(IProjectFile raw, int projectId)
            {
                this.Raw = raw;

                this.ProjectId = projectId;
                this.Name = raw.Name;
                this.Size = raw.FileSize;
                // In order to sort, we'll get timesStap format, then will convert dateTime again when display in ui.
                this.DateModified = CommonUtils.DateTimeToTimestamp(raw.LastModifiedTime.ToLocalTime()).ToString();
                this.RawDateModified = raw.LastModifiedTime;
                this.SharedWith = "";
                this.Location = InitLocation(raw);

                this.LocalPath = raw.LocalDiskPath;
                this.RmsRemotePath = raw.RMSDisplayPath;

                this.IsMarkedOffline = raw.isOffline;
                this.FileStatus = raw.Status;
                this.FileRepo = EnumFileRepo.REPO_PROJECT;
                this.IsCreatedLocal = false;
                this.Emails = new string[0]; // project file does not supprot share.
                this.FileId = ""; // should export fileId
                // bind partial local path.
                this.PartialLocalPath = raw.PartialLocalPath;

                this.IsEdit = raw.IsEdit;
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

    // UI will bind this date struct 
    public class ProjectInfo
    {
        public Int32 ProjectId { get; set; }

        public string Name { get; set; }

        public string DisplayName { get; set; }

        public string Description { get; set; }

        public bool BOwner { get; set; } // Flag that by me or by others.

        public Int64 TotalFiles { get; set; }

        public string TenantId { get; set; }

        public string MemberShipId { get; set; }

        public IMyProject Raw { get; set; }

        public ProjectInfo(IMyProject myProject)
        {
            this.ProjectId = myProject.Id;
            this.Name = myProject.DisplayName;
            this.DisplayName = myProject.DisplayName;
            this.Description = myProject.Description;
            this.BOwner = myProject.IsOwner;
            this.MemberShipId = myProject.MemberShipId;
            this.TenantId = "";

            this.Raw = myProject;
        }

        public static bool operator ==(ProjectInfo lhs, ProjectInfo rhs)
        {
            return Equals(lhs, rhs);
        }

        public static bool operator !=(ProjectInfo lhs, ProjectInfo rhs)
        {
            return !Equals(lhs, rhs);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (this.GetType() != obj.GetType())
            {
                return false;
            }

            return this.ProjectId == (obj as ProjectInfo).ProjectId &&
                    this.Name == (obj as ProjectInfo).Name &&
                    this.DisplayName == (obj as ProjectInfo).DisplayName &&
                    this.Description == (obj as ProjectInfo).Description &&
                    this.BOwner == (obj as ProjectInfo).BOwner &&
                    this.MemberShipId == (obj as ProjectInfo).MemberShipId &&
                    this.TenantId == (obj as ProjectInfo).TenantId;
        }

        public override int GetHashCode()
        {
            return ProjectId.GetHashCode();
        }
    }

    // UI will bind this data struct as All projects current user owned
    public class ProjectData
    {
        public ProjectData()
        {
        }

        public ProjectData(ProjectInfo info, IList<INxlFile> nodes)
        {
            ProjectInfo = info;
            FileNodes = nodes;
        }

        public ProjectInfo ProjectInfo { get; set; }

        // Including project online nodes and local created nodes.
        public IList<INxlFile> FileNodes { get; set; }
    }
}
