using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SkydrmDesktop;
using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.fileSystem.externalDrive;
using SkydrmDesktop.rmc.fileSystem.mySpace;
using SkydrmDesktop.rmc.fileSystem.sharedWorkspace;
using SkydrmDesktop.rmc.fileSystem.workspace;
using SkydrmLocal.rmc.fileSystem;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.fileSystem.project;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.helper;
using SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.model;

namespace SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.viewModel
{
    public class FolderViewModel : TreeViewItemViewModel
    {
        public NxlFolder NxlFolder { get; set; }
        private IFileRepo repo;
        private BackgroundWorker refreshWorker = new BackgroundWorker();
        private bool bRefreshSuccess = true;

        public string FolderName
        {
            get { return NxlFolder.Name; }
        }

        public string FolderPathId
        {
            get { return NxlFolder.PathId; }
        }

        #region Constructor
        // From project
        public FolderViewModel(NxlFolder nxlFolder, ProjectViewModel parent, bool isLazyLoad, IFileRepo fileRepo)
            : base(parent, isLazyLoad)
        {
            this.NxlFolder = nxlFolder;
            this.repo = fileRepo;
        }

        // From workSpace
        public FolderViewModel(NxlFolder nxlFolder, RootViewModel parent, bool isLazyLoad, IFileRepo fileRepo)
            : base(parent, isLazyLoad)
        {
            this.NxlFolder = nxlFolder;
            this.repo = fileRepo;
        }

        // Add another constructor.
        public FolderViewModel(NxlFolder nxlFolder, FolderViewModel parent, bool isLazyLoad, IFileRepo fileRepo) 
        : base(parent, isLazyLoad)
        {
            this.NxlFolder = nxlFolder;
            this.repo = fileRepo;
        }
        #endregion // Constructor


        protected override void LoadChildren(bool bFirstLoad)
        {

            if (bFirstLoad)
            {
                foreach (INxlFile one in NxlFolder.Children)
                {

                    if (one.IsFolder && one is NxlFolder)
                    {
                        NxlFolder inxlFolder = one as NxlFolder;

                        if (inxlFolder.Children == null || !TreeViewHelper.HasFolderChildren(inxlFolder))
                        {
                            FolderViewModel folderViewModel = new FolderViewModel(inxlFolder, this, false, repo);
                            TreeViewHelper.AddFolderNodeChild(Children, folderViewModel);
                        }
                        else
                        {
                            FolderViewModel folderViewModel = new FolderViewModel(inxlFolder, this, true, repo);
                            TreeViewHelper.AddFolderNodeChild(Children, folderViewModel);

                            // Get children
                            GetChildren(inxlFolder, folderViewModel);
                        }
                    }

                }
            }
            else
            {
                SkydrmApp.Singleton.Log.Info("FolderViewModel LoadChildren do refresh()");
                if (repo != null && SkydrmApp.Singleton.MainWin.viewModel.IsNetworkAvailable)
                {
                    Refresh();
                }
            }

        }

        #region Refresh one project folder TreeView item 
        public override void Refresh()
        {
            refreshWorker.WorkerReportsProgress = false;
            refreshWorker.WorkerSupportsCancellation = true;
            refreshWorker.DoWork += RefreshWorker;
            refreshWorker.RunWorkerCompleted += OnRefreshComplete;

            SkydrmApp.Singleton.Log.Info("FolderViewModel RefreshWorker IsBusy:" + refreshWorker.IsBusy);
            if (!refreshWorker.IsBusy)
            {
                refreshWorker.RunWorkerAsync();
            }
        }

        private void RefreshWorker(object sender, DoWorkEventArgs args)
        {
            SkydrmApp.Singleton.Log.Info("FolderViewModel RefreshWorker invoke DoWork event");
            bRefreshSuccess = false;

            // Used to store new nodes.
            IList<INxlFile> files = new List<INxlFile>(); 
            try
            {
                // For project, handle with it specially.
                if(repo is ProjectRepo)
                {
                    ProjectData p = FindProjectView(this).Project;
                    (repo as ProjectRepo).SyncRemoteAllFiles(p.ProjectInfo, NxlFolder.PathId, files);
                    bRefreshSuccess = true;
                }
                else
                {
                    // Note: must use field 'Path' to get data for Shared Workspace.
                    string para = (NxlFolder is SharedWorkspaceFolder) ? NxlFolder.DisplayPath : NxlFolder.PathId;
                    repo?.SyncFilesRecursively(para, files);
                    bRefreshSuccess = true;
                }
            }
            catch(Exception e)
            {
                SkydrmApp.Singleton.Log.Error("FolderViewModel RefreshWorker error: ", e);
            }
            finally
            {
                if (bRefreshSuccess)
                {
                    args.Result = files;
                }

                refreshWorker.DoWork -= RefreshWorker;
            }
        }

        private void OnRefreshComplete(object sender, RunWorkerCompletedEventArgs args)
        {
            SkydrmApp.Singleton.Log.Info("FolderViewModel RefreshWorker invoke RunWorkerCompleted event result:" + bRefreshSuccess);
            if (bRefreshSuccess)
            {
                IList<INxlFile> newFiles = (IList<INxlFile>)args.Result;
                InnerUpdate(newFiles);
            }

            refreshWorker.RunWorkerCompleted -= OnRefreshComplete;
        }
        #endregion // Refresh one project folder TreeView item 

        public void GetFolderLocalAllFile(FolderViewModel folderViewModel)
        {
            // When has dummy child, should not merge data. 
            // If do merge, it's maybe remove dummy child and change old data. 
            // When this node expend, will not excute 'LoadChildren(true)' method so that missing add child.
            if (!this.HasDummyChild)
            {
                IList<INxlFile> files = new List<INxlFile>();

                if(repo is ProjectRepo)
                {
                    ProjectData p = FindProjectView(folderViewModel).Project;
                    (repo as ProjectRepo).GetRemoteFilesRecursivelyFromDB(p.ProjectInfo, NxlFolder.PathId, files);
                } else
                {
                    // Note: must use field 'Path' to get data for Shared Workspace.
                    string para = (NxlFolder is SharedWorkspaceFolder) ? NxlFolder.DisplayPath : NxlFolder.PathId;
                    repo?.GetRmsFilesRecursivelyFromDB(para, files);
                }

                InnerUpdate(files);
            }
        }

        private void InnerUpdate(IList<INxlFile> newFiles)
        {
            // At the same time, notify refresh listview.
            if (repo is ProjectRepo)
            {
                var r = repo as ProjectRepo;
                if (r.CurrentWorkingProject?.ProjectInfo.ProjectId == FindProjectView(this).Project.ProjectInfo.ProjectId)
                {
                    if (r.CurrentWorkingFolder?.PathId == NxlFolder.PathId)
                    {
                        SkydrmApp.Singleton.Log.Info($"Invoke notify refresh file listview when refresh FolderViewModel. " + 
                            $"repoName: {repo.RepoDisplayName}, file count: {newFiles.Count}, pathId: {r.CurrentWorkingProject?.ProjectInfo.ProjectId + NxlFolder.PathId}");

                        SkydrmApp.Singleton.InvokeEvent_NotifyRefreshFileListView(repo.RepoDisplayName, newFiles,
                            r.CurrentWorkingProject?.ProjectInfo.ProjectId + NxlFolder.PathId);
                    }
                }
            }
            else
            {
                // For myVault\mySpace\workSpace, repoId is empty.
                SkydrmApp.Singleton.Log.Info($"Invoke notify refresh file listview when refresh FolderViewModel. " +
                           $"repoName: {repo.RepoDisplayName}, file count: {newFiles.Count}, pathId: {repo.RepoId + NxlFolder.PathId}");

                SkydrmApp.Singleton.InvokeEvent_NotifyRefreshFileListView(repo.RepoDisplayName, newFiles, repo.RepoId + NxlFolder.PathId);
            }

            MergeTreeView(newFiles);

            // Get folder all nodes and merge subfolder
            GetAndMergeSubFolder(newFiles);
        }

        // Merge all nodes recursively.
        private void MergeTreeView(IList<INxlFile> newFiles)
        {
            IList<INxlFile> oldFiles = NxlFolder.Children;
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
                    if (f.IsFolder && one.PathId == f.PathId)
                    {
                        // "one" belongs to old set and belongs to new set
                        find = f;
                        break;
                    }
                }

                // "one" belongs to old set but not belongs to new set -- should remove it
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

                        // update 
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

                // "one" belongs to new set but not belongs to old set -- should add it
                if (find == null)
                {
                    SkydrmApp.Singleton.Log.Info(NxlFolder.Name + " Children count:" + Children.Count.ToString());
                    if (this.HasDummyChild)
                    {
                        SkydrmApp.Singleton.Log.Info("Prepare to remove dummy child.");
                        this.Children.RemoveAt(0);
                    }

                    NxlFolder inxlFolder = one as NxlFolder;

                    if (inxlFolder.Children == null || !TreeViewHelper.HasFolderChildren(inxlFolder))
                    {
                        var folderNode = new FolderViewModel(inxlFolder, this, false, repo);
                        TreeViewHelper.AddFolderNodeChildInFirst(this.Children, folderNode);
                    }
                    else
                    {
                        FolderViewModel folderViewModel = new FolderViewModel(inxlFolder, this, true, repo);
                        TreeViewHelper.AddFolderNodeChildInFirst(this.Children, folderViewModel);

                        // Get children
                        GetChildren(inxlFolder, folderViewModel);
                    }

                    // update 
                    oldFiles.Add(one);
                }
            }
           
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
                        FolderViewModel folderViewmodel = GetFolderViewModel(inxlFolder.PathId);
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
                SkydrmApp.Singleton.Log.Error("FolderViewModel GetAndMergeSubFolder error: ", e);
            }
        }

        // Get the newly added folders' children, fix bug 52302.
        // Also update treeview corresponding nodes when found listview nodes changed after refresh listview.
        #region Update(Add\Remove) treeview item nodes
        public override void TryToGetItemChildren(string pathId)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(GetChildrenWorker), pathId);
        }

        private void GetChildrenWorker(object para)
        {
            if (repo == null && !SkydrmApp.Singleton.MainWin.viewModel.IsNetworkAvailable)
            {
                return;
            }

            bool ret = true;
            IList<INxlFile> files = new List<INxlFile>();
            try
            {
                if(repo is ProjectRepo)
                {
                    ProjectData p = FindProjectView(this).Project;
                    (repo as ProjectRepo).SyncRemoteAllFiles(p.ProjectInfo, para.ToString(), files);
                } else
                {
                    repo?.SyncFilesRecursively(para?.ToString(), files);
                }
            }
            catch (Exception e)
            {
                SkydrmApp.Singleton.Log.Error("FolderViewModel GetChildren error: ", e);
                ret = false;
            }
            finally
            {
                if (ret)
                {
                    // There is treeView ui operation, so should switch main thread.
                    SkydrmApp.Singleton.Dispatcher.Invoke((Action)delegate
                    {
                        AddFolderItems(files, para?.ToString());
                    });
                }
            }

        }

        private void AddFolderItems(IList<INxlFile> newFiles, string pathId)
        {
            FolderViewModel parentViewmodel = GetFolderViewModel(pathId);
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
                        FolderViewModel folderViewModel = new FolderViewModel(inxlFolder, this, true, repo);
                        TreeViewHelper.AddFolderNodeChild(parentViewmodel.Children, folderViewModel);

                        // Get children
                        GetChildren(inxlFolder, folderViewModel);
                    }
                }

                // Add children nodes, the node may be file or folder.
                parentViewmodel.NxlFolder.Children.Add(one);

            }
        }

        private FolderViewModel GetFolderViewModel(string pathId)
        {
            FolderViewModel ret = null;

            foreach (var one in this.Children)
            {
                if (one is FolderViewModel && (one as FolderViewModel).NxlFolder.PathId == pathId)
                {
                    ret = one as FolderViewModel;
                    break;
                }
            }

            return ret;
        }

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

            this.Children?.Insert(0, new FolderViewModel(inxlFolder, this, false, repo));

            NxlFolder.Children?.Add(inxlFolder);

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

            if (toDel != null)
            {
                // remove treeView item
                this.Children.Remove(toDel);

                // Remove project node.
                // This may remove failed since can't find it because of the node is mapped into another object(newly creat from db)
                if (NxlFolder.Children != null && !NxlFolder.Children.Remove(nxlFolder)) 
                {
                    // The folder node(db one record) may be mapped into different object, so need to find the object by pathId, then remove.
                    for (int i = 0; i < NxlFolder.Children.Count; i++)
                    {
                        if (NxlFolder.Children[i].IsFolder && (NxlFolder.Children[i] as NxlFolder).PathId == nxlFolder.PathId)
                        {
                            NxlFolder.Children.RemoveAt(i);
                            break;
                        }
                    }
                }
                
            }
        }

        #endregion // Update(Add\Remove) treeview item nodes.


        // Recusively get folder children.
        private void GetChildren(NxlFolder parent, FolderViewModel parentNxlFileVm)
        {
            foreach (INxlFile one in parent.Children)
            {

                if (one.IsFolder && one is NxlFolder)
                {
                    NxlFolder inxlFolder = one as NxlFolder;
                    if (inxlFolder.Children == null || !TreeViewHelper.HasFolderChildren(inxlFolder))
                    {
                        FolderViewModel nxlFileVm = new FolderViewModel(inxlFolder, parentNxlFileVm, false, repo);

                        // Should remove Dummy Child first if has
                        if (parentNxlFileVm.HasDummyChild)
                        {
                            parentNxlFileVm.Children.RemoveAt(0);
                        }

                        TreeViewHelper.AddFolderNodeChild(parentNxlFileVm.Children, nxlFileVm);
                    }
                    else
                    {
                        FolderViewModel nxlFileVm = new FolderViewModel(inxlFolder, parentNxlFileVm, true, repo);

                        // Should remove Dummy Child first if has
                        if (parentNxlFileVm.HasDummyChild)
                        {
                            parentNxlFileVm.Children.RemoveAt(0);
                        }

                        TreeViewHelper.AddFolderNodeChild(parentNxlFileVm.Children, nxlFileVm);

                        // Recusive get children
                        GetChildren(inxlFolder, nxlFileVm);
                    }
                }

            }
        }


        // Find the parent project view by folder view.
        private ProjectViewModel FindProjectView(FolderViewModel folder)
        {
            TreeViewItemViewModel parent = folder.Parent;
            if (parent is FolderViewModel)
            {
                return FindProjectView(parent as FolderViewModel);
            } 

            return parent as ProjectViewModel;
        }

    }

}
