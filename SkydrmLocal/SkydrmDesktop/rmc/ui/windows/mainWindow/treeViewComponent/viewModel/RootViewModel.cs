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
using SkydrmDesktop.rmc.featureProvider.externalDrive.RmsRepoMgr;
using SkydrmDesktop.rmc.fileSystem.externalDrive;
using SkydrmDesktop.rmc.fileSystem.externalRepo;
using SkydrmDesktop.rmc.fileSystem.mySpace;
using SkydrmDesktop.rmc.fileSystem.sharedWorkspace;
using SkydrmDesktop.rmc.fileSystem.utils;
using SkydrmDesktop.rmc.fileSystem.workspace;
using SkydrmDesktop.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.fileSystem;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.fileSystem.project;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.helper;
using SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.model;

namespace SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.viewModel
{
    // Wrapper for Root.
    public class RootViewModel : TreeViewItemViewModel
    {
        private IFileRepo repo;
        private BackgroundWorker refreshProjectWorker = new BackgroundWorker();

        // Record old results for Project.
        private IList<ProjectData> oldResults = new List<ProjectData>();
        public IList<ProjectData> GetProjectData()
        {
            return oldResults;
        }

        // Record old external repositories(repeId)
        private static List<IFileRepo> oldExternalRepos = new List<IFileRepo>();

        public Root Root { get; set; }

        public string RootName 
        {
            get { return Root.RepoName; }
            set { Root.RepoName = value; OnPropertyChanged("RootName"); }
        }

        public string RepoId
        {
            get { return repo?.RepoId; }
        }

        public string RootType
        {
            get { return Root.RepoType; }
        }

        public RepositoryProviderClass RootClassType
        {
            get { return Root.RepoClass; }
        }

        // use for mySpace, add virtual RootViewModel
        public RootViewModel(Root root, bool isLazyLoad = false) : base(null, isLazyLoad)
        {
            this.Root = root;
        }

        public RootViewModel(Root root, bool isLazyLoad, IFileRepo repo, RootViewModel parentRegion = null) 
            : base(parentRegion, isLazyLoad)
        {
            this.Root = root;
            this.repo = repo;

            // Init project
            if(Root.Projects != null)
            {
                InitProject();
            }

            // Init external repositories
            if(repo != null && (repo is ExternalRepo || repo is SharedWorkspaceRepo))
            {
                oldExternalRepos.Add(repo);
            }

        }

        // Will load its children when expand the treeview item. 
        protected override void LoadChildren(bool bFirstLoad)
        {
            if (!bFirstLoad)
            {
                SkydrmApp.Singleton.Log.Info("RootViewModel LoadChildren do refresh()");
                if (SkydrmApp.Singleton.MainWin.viewModel.IsNetworkAvailable)
                {
                    Refresh();
                }

                return;
            }

            //
            // Will enter this when user FIRST EXPAND the root treeview item.
            //
            // For project repo, the first expand is set in default since 'IsExpanded = true' in init function.
            if (repo is ProjectRepo)
            {
                AddProjectViewModel();
            }
            // Other repo
            else
            {
                foreach (var one in Root.RepoFiles)
                {
                    AddFolderViewModel(one);
                }
            }
        }

        private void AddProjectViewModel()
        {
            // Get all projects
            foreach (ProjectData project in Root.Projects)
            {

                if (TreeViewHelper.HasFolderChildren(project.FileNodes))
                {
                    Children.Add(new ProjectViewModel(project, this, true, repo));
                }
                else
                {
                    Children.Add(new ProjectViewModel(project, this, false, repo));
                }
            }
        }

        private void AddFolderViewModel(INxlFile nxlFile)
        {
            if (nxlFile.IsFolder)
            {
                var inxlFolder = nxlFile as NxlFolder;
                if (inxlFolder.Children == null || !TreeViewHelper.HasFolderChildren(inxlFolder))
                {
                    var folderNode = new FolderViewModel(inxlFolder, this, false, repo);
                    TreeViewHelper.AddFolderNodeChild(Children, folderNode);
                }
                else
                {
                    var folderNode = new FolderViewModel(inxlFolder, this, true, repo);
                    TreeViewHelper.AddFolderNodeChild(Children, folderNode);
                }
            }
        }

        // Refresh enter, we can't directly using SyncRemoteData(),
        // which will cause the bug 52664 that RootViewModel.Refresh InvalidCastException(the reproduce rate is low).
        public override void Refresh()
        {
            // Refresh repositories
            if(RootType == CultureStringInfo.ApplicationFindResource("MainWin__TreeView_Repositories"))
            {
                RefreshExternalRepositories();
                return;
            }

            // Fix bug 52806, need to filter other repo that can't expand treeview such as MyVault, SharedWithMe.
            if (repo is ProjectRepo)
            {
                RefreshProjectWorkerRun();
            }
            else // Other repo
            {
                AsyncRefresh();
            }
        }

        #region Refresh external repository list('REPOSITORIES')
        private bool bIsRefreshingRepoList = false;
        public void RefreshExternalRepositories()
        {
            if (bIsRefreshingRepoList)
                return;

            // Async worker
            Func<object> asyncTask = new Func<object>(() => {

                bool bSucceed = true;

                List<IRmsRepo> ret = new List<IRmsRepo>();
                try
                {
                     bIsRefreshingRepoList = true;
                     ret = SkydrmApp.Singleton.RmsRepoMgr.SyncRepositories();
                }
                catch (Exception e)
                {
                    SkydrmApp.Singleton.Log.Warn(e.ToString());
                    bSucceed = false;
                }

                return new RefreshRepositoriesInfo(bSucceed, ret);
            });

            // Async callback
            Action<object> cb = new Action<object>((rt) => {

                bIsRefreshingRepoList = false;
                RefreshRepositoriesInfo rtValue = (RefreshRepositoriesInfo)rt;
                if (rtValue.IsSuc)
                {
                    List<IRmsRepo> rmsRepos = rtValue.Results;
                    AfterRefreshExternalRepos(rmsRepos);
                }
            });

            // Invoke
            AsyncHelper.RunAsync(asyncTask, cb);
        }

        private void AfterRefreshExternalRepos(List<IRmsRepo> rmsRepos)
        {
            // Refresh repository treeview dynamically.
            List<IFileRepo> adds = new List<IFileRepo>();
            List<IFileRepo> removeds = new List<IFileRepo>();
            MergeExternalRepoTreeView(rmsRepos, adds, removeds);

            // todo --> notify main window listview to refresh ui.
            SkydrmApp.Singleton.InvokeEvent_NotifyRefreshExternalRepoListView(adds, removeds);
        }

        public void MergeExternalRepoTreeView(List<IRmsRepo> newRepos, List<IFileRepo> adds, List<IFileRepo> removeds)
        {
            // Dynamic remove the external repo from treeview
            for (int i = oldExternalRepos.Count - 1; i >= 0; i--)
            {
                var one = oldExternalRepos[i];
                IRmsRepo find = null;
                foreach(IRmsRepo r in newRepos)
                {
                    if(one.RepoId == r.RepoId)
                    {
                        find = r;

                        // Should update repo display name if changed(fix bug 64114)
                        if(one.RepoDisplayName != r.DisplayName)
                        {
                            UpdateOldExternalRepoName(r);
                        }

                        break;
                    }
                }

                // Can't find in new set, means should remove it from old set.
                if (find == null)
                {
                    RootViewModel toDel = null;
                    for(int j=0; j < this.Children.Count; j++)
                    {
                        if(Children[j] is RootViewModel)
                        {
                            var rvm = Children[j] as RootViewModel;
                            if(rvm.RepoId == one.RepoId)
                            {
                                // found
                                toDel = rvm;
                                break;
                            }
                        }
                    }

                    if(toDel != null)
                    {
                        this.Children.Remove(toDel);
                        // update
                        oldExternalRepos.Remove(one);
                        removeds.Add(one);
                    }
                }
            }

            // Dynamic add the external repo to treeview.
            foreach(var one in newRepos)
            {
                IFileRepo find = null;
                for(int i = 0; i < oldExternalRepos.Count; i++)
                {
                    IFileRepo e = oldExternalRepos[i];
                    if(one.RepoId == e.RepoId)
                    {
                        find = e;
                        break;
                    }
                }

                // Can't find in old set, means should add into old set.
                if (find == null && one.Type != ExternalRepoType.LOCAL_DRIVE)
                {
                    // If refresh repository before click item(and REPOSITORIES not expand),
                    // the Children may have DummyChild(like bug 53222)
                    if (this.HasDummyChild)
                    {
                        Children.RemoveAt(0);
                    }

                    // "one" belongs to new set but not belongs to old set -- should add it.
                    var repo = DynamicAddExternalRepo(one);

                    adds.Add(repo);
                }
            }
        }

        // Should update repo name of the treeview node when find name changed.
        private void UpdateOldExternalRepoName(IRmsRepo newNode)
        {
            foreach(var one in oldExternalRepos)
            {
                if(one.RepoId == newNode.RepoId)
                {
                    one.RepoDisplayName = newNode.DisplayName;
                    break;
                }
            }

            foreach(var one in this.Children)
            {
                if(one is RootViewModel)
                {
                    var rvm = one as RootViewModel;
                    if (rvm.RepoId.Equals(newNode.RepoId))
                    {
                        rvm.RootName = newNode.DisplayName;
                        break;
                    }
                }
            }
        }

        private IFileRepo DynamicAddExternalRepo(IRmsRepo rmsRepo)
        {
            bool bNeedRefreshToken = false;
            IFileRepo repo = null;
            if(rmsRepo.ProviderClass == FileSysConstant.REPO_CLASS_PERSONAL)
            {
                repo = ExternalRepoFactory.Create(rmsRepo);
                bNeedRefreshToken = true;
            } else
            {
                repo = SharedWorkspaceRepo.Create(rmsRepo);
            }
                
            
            // 1. First directly add it without lasy loading.
            var root = new Root(repo.RepoDisplayName, repo.GetFilePool(), repo.RepoType);
            RootViewModel rvm = new RootViewModel(root, false, repo, this);
            this.Children.Add(rvm);

            // 2. Then sync and add its sub children, need to get token first since it is newly added repository.
            rvm.AsyncRefresh(bNeedRefreshToken);

            return repo;
        }

        #endregion // Refresh external repository list('REPOSITORIES')

        // This is more general way that conveniently expand other repo in the futurn.
        #region Refresh repository treeview item when expand 
        private bool bIsAsyncRefreshing = false;
        public void AsyncRefresh(bool bNeedRefreshToken = false)
        {
            // Means is refreshing, and have not yet completed for last.
            if (repo == null || bIsAsyncRefreshing)
            {
                return;
            }

            // Async worker
            Func<object> asyncTask = new Func<object>(() => {

                bIsAsyncRefreshing = true;
                bool bSucceed = true;
                // Used to store the new file nodes.
                List<INxlFile> ret = new List<INxlFile>();
                try
                {
                    if (bNeedRefreshToken)
                    {
                        string token = SkydrmApp.Singleton.RmsRepoMgr.GetAccessToken(repo.RepoId);
                        repo.UpdateToken(token);
                    }

                    repo?.SyncFilesRecursively("/", ret);
                }
                catch (Exception e)
                {
                    SkydrmApp.Singleton.Log.Warn(e.ToString());
                    bSucceed = false;
                }

                return new RefreshInfo(bSucceed, ret);
            });

            // Async callback
            Action<object> cb = new Action<object>((rt) => {

                RefreshInfo rtValue = (RefreshInfo)rt;

                if (rtValue.IsSuc)
                {
                    IList<INxlFile> newFiles = rtValue.results;
                    InnerUpdate(newFiles);
                }

                bIsAsyncRefreshing = false;

            });

            // Invoke
            AsyncHelper.RunAsync(asyncTask, cb);
        }

        private void InnerUpdate(IList<INxlFile> newFiles)
        {
            SkydrmApp.Singleton.Log.Info($"Invoke notify refresh file listview when refresh RootViewModel. " +
                $"RepoName: {repo.RepoDisplayName}, file count: {newFiles.Count}, pathId: {repo?.RepoId + "/"}");

            SkydrmApp.Singleton.InvokeEvent_NotifyRefreshFileListView(repo.RepoDisplayName, newFiles, repo?.RepoId + "/");
            MergeTreeView(newFiles, Root.RepoFiles);

            GetAndMergeSubFolder(newFiles);
        }

        private void MergeTreeView(IList<INxlFile> newFiles, IList<INxlFile> oldFiles)
        {
            #region  Merge the first level nodes(file and folder)

            if (oldFiles == null)
            {
                return;
            }

            for (int i = oldFiles.Count - 1; i >= 0; i--)
            {
                INxlFile one = oldFiles[i];

                if (!one.IsFolder)
                {
                    continue;
                }

                INxlFile find = null;
                foreach (INxlFile f in newFiles)
                {
                    // Using "PathId" not "Name" to judge, since maybe exist same name folder(externl repo: googledrive) in current list.
                    if (f.IsFolder && one.PathId == f.PathId)
                    {
                        // "one" belongs to old set and belongs to new set
                        find = f;
                        break;
                    }
                }

                // Can't find in new set, means should remove it from old set.
                if (find == null)
                {
                    FolderViewModel toDel = null;
                    for (int j = 0; j < this.Children.Count; j++)
                    {
                        if (this.Children[j] is FolderViewModel)
                        {
                            var fvm = this.Children[j] as FolderViewModel;
                            if (fvm.NxlFolder.PathId == one.PathId)
                            {
                                // found
                                toDel = fvm;
                                break;
                            }
                        }
                    }

                    if (toDel != null)
                    {
                        this.Children.Remove(toDel);

                        // update node.
                        oldFiles.Remove(one);
                    }
                }

            }


            foreach (INxlFile one in newFiles)
            {
                if (!one.IsFolder)
                {
                    continue;
                }

                INxlFile find = null;
                for (int i = 0; i < oldFiles.Count; i++)
                {
                    INxlFile f = oldFiles[i];
                    if (f.IsFolder && one.PathId == f.PathId)
                    {
                        // "one" belongs to new set and belongs to old set
                        find = f;
                        break;
                    }
                }

                if (find == null)
                {
                    if (this.HasDummyChild)
                    {
                        SkydrmApp.Singleton.Log.Info("Prepare to remove dummy child.");
                        base.Children.RemoveAt(0);
                    }

                    NxlFolder inxlFolder = one as NxlFolder;

                    // "one" belongs to new set but not belongs to old set -- should add it
                    if (inxlFolder.Children == null || !TreeViewHelper.HasFolderChildren(inxlFolder))
                    {
                        var folderNode = new FolderViewModel(inxlFolder, this, false, repo);
                        TreeViewHelper.AddFolderNodeChildInFirst(Children, folderNode);
                    }
                    else
                    {
                        var folderNode = new FolderViewModel(inxlFolder, this, true, repo);
                        TreeViewHelper.AddFolderNodeChildInFirst(Children, folderNode);
                    }

                    // update project node.
                    oldFiles.Add(one);
                }
            }

            #endregion //  Merge the first level nodes(file and folder)
        }

        // When user do refresh, original folder also may changed, folder without children may become have children(added folder)
        // folder with children may become not have children(remove folder).
        private void GetAndMergeSubFolder(IList<INxlFile> newFiles)
        {
            try
            {
                foreach (var item in newFiles)
                {
                    if (item.IsFolder)
                    {
                        NxlFolder inxlFolder = item as NxlFolder;
                        FolderViewModel folderViewmodel = GeFolderViewModel(inxlFolder.PathId);
                        if (folderViewmodel == null)
                        {
                            break;
                        }
                        folderViewmodel?.GetFolderLocalAllFile(folderViewmodel);
                    }
                }
            }
            catch (Exception e)
            {
                SkydrmApp.Singleton.Log.Error("GetAndMergeSubFolder error: ", e);
            }
        }
        #endregion // Refresh workspace treeview item


        #region Refresh project TreeView item
        private void InitProject()
        {
            foreach (ProjectData pd in Root.Projects)
            {
                oldResults.Add(pd);
            }

            // Must InitRefreshWorker before treeView expanded
            InitProjectRefreshWorker();

            // Fix bug 53562, the Children data are inconsistent with oldResults data when AUTO refresh,
            // so have it expanded in default to keep the data consisitent.
            SkydrmApp.Singleton.Log.Info("Set IsExpanded:");
            IsExpanded = true;

            // register automatically refresh listen event when project is remvoed(user in one project is removed.)
            SkydrmApp.Singleton.Log.Info("register automatically refresh");
            SkydrmApp.Singleton.ProjectUpdate += AutoRefreshProject;
        }

        private void InitProjectRefreshWorker()
        {
            refreshProjectWorker.WorkerReportsProgress = false;
            refreshProjectWorker.WorkerSupportsCancellation = true;
            refreshProjectWorker.DoWork += RefreshWorker;
            refreshProjectWorker.RunWorkerCompleted += OnRefreshComplete;
        }

        private void RefreshProjectWorkerRun()
        {
            SkydrmApp.Singleton.Log.Info("RootViewModel RefreshWorker IsBusy:" + refreshProjectWorker.IsBusy);
            if (!refreshProjectWorker.IsBusy)
            {
                refreshProjectWorker.RunWorkerAsync();
            }
        }

        // Sync to get all project nodes recursively
        private void RefreshWorker(object sender, DoWorkEventArgs args)
        {
            SkydrmApp.Singleton.Log.Info("RootViewModel RefreshWorker invoke DoWork event");
            bool ret = true;
            try
            {
                // Sync remote and update into FilePool then return it, so should record 'oldResults' in order to merge later.
                ret = (repo as ProjectRepo).InnerSyncRemoteData();
            }
            catch (Exception e)
            {
                ret = false;
                SkydrmApp.Singleton.Log.Error("RootViewModel RefreshWorker error:", e);
            }
            finally
            {
                args.Result = ret;
            }
        }

        private void OnRefreshComplete(object sender, RunWorkerCompletedEventArgs args)
        {
            SkydrmApp.Singleton.Log.Info("RootViewModel RefreshWorker invoke RunWorkerCompleted event result:" + (bool)args.Result);
            bool bSucess = (bool)args.Result;
            if (!bSucess)
            {
                return;
            }

            IList<ProjectData> newResults = (repo as ProjectRepo).FilePool;

            // For refresh Project List Page
            List<ProjectData> addProject = new List<ProjectData>();
            List<ProjectData> removeProject = new List<ProjectData>();

            SkydrmApp.Singleton.Log.Info("RootViewModel MergeTreeView: oldResults Count " + oldResults.Count
                + ", newResults Count " + newResults.Count);

            // Merge
            MergeProjectTreeView(newResults, addProject, removeProject);

            // Get project all nodes from local and merge its first level folder, fix bug 52438.
            GetAndMergeProjectFolder();

            // At the same time, notify the project page list to refresh.
            SkydrmApp.Singleton.InvokeEvent_NotifyRefreshProjectListView(addProject, removeProject);
        }

        // Will execute auto refresh ui when background heartbeat detect project is deleted(user is removed from invited project)
        private void AutoRefreshProject()
        {
            Console.WriteLine("-->AutoRefreshProject");

            // Note: should switch into ui thread to refresh ui from bg thread.
            SkydrmApp.Singleton.Log.Info("RootViewModel AutoRefreshProject");
            SkydrmApp.Singleton.Dispatcher.Invoke(new Action(() => {
                RefreshProjectWorkerRun();
            }));
        }

        public void MergeProjectTreeView(IList<ProjectData> newResults, List<ProjectData> addProject, List<ProjectData> removeProject)
        {
            #region  merge current all project nodes.

            // Will execute remove ope, so can't use foreach.
            for (int i = oldResults.Count - 1; i >= 0; i--)
            {
                ProjectData one = oldResults[i];

                ProjectData find = null;
                foreach (ProjectData p in newResults)
                {
                    if (one.ProjectInfo.Equals(p.ProjectInfo))
                    {
                        // "one" belongs to old set and belongs to new set
                        find = p;
                        break;
                    }
                }

                // Can't find in new set, means should remove it from old set.
                if (find == null)
                {
                    ProjectViewModel toDel = null;

                    for (int j = 0; j < this.Children.Count; j++)
                    {
                        // May the node is dummy child, fix bug 52892
                        if (this.Children[j] is ProjectViewModel)
                        {
                            var pvm = this.Children[j] as ProjectViewModel;
                            if (pvm.Project.ProjectInfo.Equals(one.ProjectInfo))
                            {
                                // found
                                toDel = pvm;
                                break;
                            }
                        }
                    }

                    if (toDel != null)
                    {
                        this.Children.Remove(toDel);
                        //  update
                        oldResults.Remove(one);
                        removeProject.Add(one);
                    }
                }

            }

            foreach (ProjectData one in newResults)
            {
                ProjectData find = null;
                for (int i = 0; i < oldResults.Count; i++)
                {
                    ProjectData p = oldResults[i];
                    if (one.ProjectInfo.Equals(p.ProjectInfo))
                    {
                        // "one" belongs to new set and belongs to old set
                        find = p;
                        break;
                    }
                }

                // Can't find in old set, means should add into old set.
                if (find == null)
                {
                    // Fix bug 53222, When user select treeview item will judge DummyChild and remove. 
                    // If refresh project before click item,the Children may have DummyChild  -- need test again.
                    SkydrmApp.Singleton.Log.Info("Projects Children count:" + Children.Count.ToString());
                    if (this.HasDummyChild)
                    {
                        SkydrmApp.Singleton.Log.Info("Prepare to remove dummy child.");
                        Children.RemoveAt(0);
                    }

                    // "one" belongs to new set but not belongs to old set -- should add it.
                    if (TreeViewHelper.HasFolderChildren(one.FileNodes))
                    {
                        Children.Add(new ProjectViewModel(one, this, true, repo));
                    }
                    else
                    {
                        Children.Add(new ProjectViewModel(one, this, false, repo));
                    }
                    List<ProjectViewModel> sortp = new List<ProjectViewModel>();
                    foreach (var item in Children)
                    {
                        sortp.Add((ProjectViewModel)item);
                    }
                    var list = sortp.OrderBy(p => p.Project.ProjectInfo.Name).ToList();
                    var fpList = list.OrderByDescending(p => p.Project.ProjectInfo.BOwner).ToList();
                    Children.Clear();
                    foreach (var item in fpList)
                    {
                        Children.Add(item);
                    }

                    // update
                    oldResults.Add(one);
                    addProject.Add(one);
                }
            }

            #endregion  // merge current all project nodes.
        }

        // When user do refresh, original project also may changed, project without children may become have children(added folder)
        // project with children may become not have children(removed folder).
        private void GetAndMergeProjectFolder()
        {
            try
            {
                foreach (ProjectViewModel item in Children)
                {
                    item.GetProjectLocalAllFile(item.Project);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString() + "GetLocalAllFile failed in RootViewModel.cs");
            }
        }
        #endregion // Refresh project TreeView item

        // Also update treeview corresponding nodes when found listview nodes changed after refresh listview.
        // Used for general repo with folders like WorkSpace repo.
        #region Update(Add\Remove) treeview item nodes

        /// <summary>
        /// Now directly add the newly folder node, then later will dynamically get and add its children.
        /// </summary>
        /// <param name="inxlFolder"></param>
        public override void AddFolderItemToCurrentItem(NxlFolder inxlFolder)
        {
            foreach (FolderViewModel one in base.Children)
            {
                // have existed, don't add again.
                if (one.NxlFolder?.PathId == inxlFolder.PathId)
                {
                    return;
                }
            }

            base.Children?.Insert(0, new FolderViewModel(inxlFolder, this, false, repo));

            // update node.
            Root.RepoFiles?.Add(inxlFolder);
        }

        public override void TryToGetItemChildren(string pathId)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(GetChildrenWorker), pathId);
        }

        private void GetChildrenWorker(object para)
        {
            // check network
            if (repo == null && !SkydrmApp.Singleton.MainWin.viewModel.IsNetworkAvailable)
            {
                return;
            }

            bool ret = true;
            IList<INxlFile> files = new List<INxlFile>();
            try
            {
                repo?.SyncFilesRecursively(para?.ToString(), files);
            }
            catch (Exception e)
            {
                SkydrmApp.Singleton.Log.Warn(e.ToString());
                ret = false;
            }
            finally
            {
                if (ret)
                {
                    // There is treeView ui operation, so should switch main thread.
                    SkydrmApp.Singleton.Dispatcher.Invoke((Action)delegate
                    {
                        AddedNewItems(files, para?.ToString());
                    });
                }
            }

        }

        // Add new file nodes into specified pathId.
        private void AddedNewItems(IList<INxlFile> newFiles, string pathId)
        {
            FolderViewModel parentViewmodel = GeFolderViewModel(pathId);
            if (parentViewmodel == null)
            {
                return;
            }

            if (parentViewmodel.NxlFolder.Children == null)
            {
                parentViewmodel.NxlFolder.Children = new List<INxlFile>();
            }

            foreach (var one in newFiles)
            {
                if (one is NxlFolder)
                {
                    NxlFolder inxlFolder = one as NxlFolder;

                    // Add folder viewModel children into treeview.
                    if (inxlFolder.Children == null || !TreeViewHelper.HasFolderChildren(inxlFolder))
                    {
                        var folderNode = new FolderViewModel(inxlFolder, this, false, repo);
                        TreeViewHelper.AddFolderNodeChild(parentViewmodel.Children, folderNode);
                    }
                    else
                    {
                        var folderNode = new FolderViewModel(inxlFolder, this, true, repo);
                        TreeViewHelper.AddFolderNodeChild(parentViewmodel.Children, folderNode);
                    }
                }

                // Add children nodes, the node may be file or folder.
                parentViewmodel.NxlFolder.Children.Add(one);
            }
        }

        // Remove the specified folder from treeview.
        public override void RemoveFolderItem(NxlFolder nxlFolder)
        {
            FolderViewModel toDel = null;
            foreach (FolderViewModel fvm in this.Children)
            {
                if (fvm.NxlFolder.PathId == nxlFolder.PathId)
                {
                    // found
                    toDel = fvm;
                    break;
                }
            }

            if( toDel != null)
            {
                // remove treeView item
                this.Children.Remove(toDel);

                //
                // Remove data node.
                //
                // This may remove failed since can't find it because of the node is mapped into another object(newly creat from db)
                if (!Root.RepoFiles.Remove(nxlFolder))
                {
                    for (int i = 0; i < Root.RepoFiles.Count; i++)
                    {
                        if (Root.RepoFiles[i].IsFolder && (Root.RepoFiles[i] as NxlFolder).PathId == nxlFolder.PathId)
                        {
                            Root.RepoFiles.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
        }
        #endregion // Update(Add\Remove) treeview item nodes


        private FolderViewModel GeFolderViewModel(string pathId)
        {
            FolderViewModel ret = null;

            foreach (var one in base.Children)
            {
                if (one is FolderViewModel && (one as FolderViewModel).NxlFolder.PathId == pathId)
                {
                    ret = one as FolderViewModel;
                    break;
                }
            }

            return ret;
        }

    }

    // Used for general repo refresh such as workspace
    public sealed class RefreshInfo
    {
        public bool IsSuc { get; }
        public List<INxlFile> results { get; }

        public RefreshInfo(bool isSuc, List<INxlFile> rt)
        {
            this.IsSuc = isSuc;
            this.results = rt;
        }
    }

}
