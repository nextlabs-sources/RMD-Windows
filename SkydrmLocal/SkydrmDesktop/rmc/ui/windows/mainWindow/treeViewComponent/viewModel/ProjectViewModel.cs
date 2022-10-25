using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SkydrmDesktop;
using SkydrmDesktop.Resources.languages;
using SkydrmLocal.rmc.fileSystem;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.fileSystem.project;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.helper;
using SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.model;

namespace SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.viewModel
{
   public class ProjectViewModel : TreeViewItemViewModel
    {
        public ProjectData Project { get; set; }

        private ProjectRepo repo;

        private BackgroundWorker refreshWorker = new BackgroundWorker();
        private bool bRefreshSuccess = true;

        public ProjectViewModel(ProjectData project, RootViewModel parentRegion, bool isLazyLoad, IFileRepo repo)
            : base(parentRegion, isLazyLoad)
        {
            this.Project = new ProjectData
            {
                ProjectInfo = project.ProjectInfo,
                FileNodes = project.FileNodes
            };
            this.repo = repo as ProjectRepo;
        }

        public string ProjectName
        {
            get { return Project.ProjectInfo.Name; }
        }

        // For display different Icon
        public bool ProjectIsCreateByMe
        {
            get { return Project.ProjectInfo.BOwner; }
        }

        protected override void LoadChildren(bool bFirstLoad)
        {

            if (bFirstLoad)
            {
                if (Project.FileNodes == null)
                {
                    return;
                }

                // Get and add folder children of one project.
                foreach (INxlFile one in Project.FileNodes)
                {
                    if (one.IsFolder && one is NxlFolder)
                    {
                        NxlFolder inxlFolder = one as NxlFolder;

                        if (inxlFolder.Children == null || !TreeViewHelper.HasFolderChildren(inxlFolder))
                        {
                            base.Children.Add(new FolderViewModel(inxlFolder, this, false, repo));
                        }
                        else
                        {
                            base.Children.Add(new FolderViewModel(inxlFolder, this, true, repo));
                        }
                    }
                }

            }
            else
            {
                SkydrmApp.Singleton.Log.Info("ProjectViewModel LoadChildren do refresh()");
                // do refresh
                if ( repo != null && SkydrmApp.Singleton.MainWin.viewModel.IsNetworkAvailable)
                {
                    Refresh();
                }
            }
        }

        #region Refresh one project TreeView item 
        public override void Refresh()
        {
            refreshWorker.WorkerReportsProgress = false;
            refreshWorker.WorkerSupportsCancellation = true;
            refreshWorker.DoWork += RefreshWorker;
            refreshWorker.RunWorkerCompleted += OnRefreshComplete;

            SkydrmApp.Singleton.Log.Info("ProjectViewModel RefreshWorker IsBusy:" + refreshWorker.IsBusy);
            if (!refreshWorker.IsBusy)
            {
                refreshWorker.RunWorkerAsync();
            }
        }

        private void RefreshWorker(object sender, DoWorkEventArgs args)
        {
            SkydrmApp.Singleton.Log.Info("ProjectViewModel RefreshWorker invoke DoWork event");
            bRefreshSuccess = true;

            // Used to store new nodes.
            IList<INxlFile> files = new List<INxlFile>();
            try
            {
                repo.SyncRemoteAllFiles(Project.ProjectInfo, "/", files);
            }
            catch (Exception e)
            {
                bRefreshSuccess = false;
                SkydrmApp.Singleton.Log.Error("ProjectViewModel RefreshWorker error: ", e);
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
            SkydrmApp.Singleton.Log.Info("ProjectViewModel RefreshWorker invoke RunWorkerCompleted event result:" + bRefreshSuccess);
            if (bRefreshSuccess)
            {
                IList<INxlFile> newFiles = (IList<INxlFile>)args.Result;
                InnerUpdate(newFiles);
            }

            refreshWorker.RunWorkerCompleted -= OnRefreshComplete;
        }
        #endregion // Refresh one project TreeView item 

        public void GetProjectLocalAllFile(ProjectData project)
        {
            // When project has dummy child, should not merge data. 
            // If do merge, it maybe remove dummy child and change old data. 
            // When this node expend, will not excute 'LoadChildren(true)' method so that missing add child.
            if (!this.HasDummyChild)
            {
                IList<INxlFile> files = new List<INxlFile>();
                // Will get all children by recursively.
                repo.GetRemoteFilesRecursivelyFromDB(project.ProjectInfo, "/", files);
                InnerUpdate(files);
            }
        }

        private void InnerUpdate(IList<INxlFile> newFiles)
        {
            if (repo.CurrentWorkingProject?.ProjectInfo.ProjectId == Project.ProjectInfo.ProjectId)
            {
                // At the same time, notify to refresh project listview.
                SkydrmApp.Singleton.Log.Info($"Invoke notify refresh file listview when refresh ProjectViewModel." +
                $"repoName: {repo.RepoDisplayName}, file count: {newFiles.Count}, pathId: {Project.ProjectInfo.ProjectId.ToString() + "/"}");

                SkydrmApp.Singleton.InvokeEvent_NotifyRefreshFileListView(repo.RepoDisplayName, newFiles,
                    Project.ProjectInfo.ProjectId.ToString() + "/");
            }

            MergeTreeView(newFiles);

            // Get folder all nodes and merge subfolder, fix bug 52438
            GetAndMergeSubFolder(newFiles);
        }

        private void MergeTreeView(IList<INxlFile> newFiles)
        {
            #region  Merge the first level nodes(file and folder)

            IList<INxlFile> oldFiles = Project.FileNodes;
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
                    if (f.IsFolder && one.Equals(f))
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
                            if (fvm.NxlFolder.Equals(one))
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

                        // update project node.
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
                    if (one.Equals(f) && f.IsFolder)
                    {
                        // "one" belongs to new set and belongs to old set
                        find = f;
                        break;
                    }
                }

                if (find == null)
                {
                    SkydrmApp.Singleton.Log.Info(Project.ProjectInfo.DisplayName + " Children count:" + Children.Count.ToString());
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


        // Get the newly added folders' children, fix bug 52302.
        // Also update treeview corresponding nodes when found listview nodes changed after refresh listview.
        #region Update(Add\Remove) treeview item nodes

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
                // recursive
                repo.SyncRemoteAllFiles(Project.ProjectInfo, para?.ToString(), files);
            }
            catch (Exception e)
            {
                ret = false;
                Console.WriteLine(e.ToString());
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

        /// <summary>
        /// Now directly add the newly folder node, then later will dynamically get and add its children.
        /// </summary>
        /// <param name="inxlFolder"></param>
        public override void AddFolderItemToCurrentItem(NxlFolder inxlFolder)
        {
            foreach(FolderViewModel one in base.Children)
            {
                // have existed, don't add again.
                if (one.NxlFolder?.PathId == inxlFolder.PathId)
                {
                    return;
                }
            }

            base.Children?.Insert(0, new FolderViewModel(inxlFolder, this, false, repo));

            // update project node.
            Project.FileNodes?.Add(inxlFolder);
        }

        // Remove the specified folder from treeview.
        public override void RemoveFolderItem(NxlFolder nxlFolder)
        {
            FolderViewModel toDel = null;
            foreach (FolderViewModel fvm in this.Children)
            {
                if (fvm.NxlFolder.Equals(nxlFolder))
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
                if (!Project.FileNodes.Remove(nxlFolder)) 
                {
                     // The folder node(db one record) may be mapped into different object, so need to find the object by pathId, then remove.
                     for(int i = 0; i < Project.FileNodes.Count; i++)
                     {
                        if ( Project.FileNodes[i].IsFolder  && (Project.FileNodes[i] as NxlFolder).PathId == nxlFolder.PathId)
                        {
                            Project.FileNodes.RemoveAt(i);
                            break;
                        }
                     }
                }
                
            }
        }

        #endregion // Update(Add\Remove) treeview item nodes


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

    }

}
