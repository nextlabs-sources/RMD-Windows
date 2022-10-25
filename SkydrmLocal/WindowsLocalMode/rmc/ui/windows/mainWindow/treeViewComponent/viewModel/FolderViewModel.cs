using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SkydrmLocal.rmc.fileSystem;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.fileSystem.project;
using SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.helper;
using SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.model;

namespace SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.viewModel
{
    public class FolderViewModel : TreeViewItemViewModel
    {
        public NxlFolder NxlFolder { get; set; }
        private ProjectRepo repo;
        private BackgroundWorker refreshWorker = new BackgroundWorker();
        private bool bRefreshSuccess = true;

        public FolderViewModel(NxlFolder nxlFolder, ProjectViewModel parent, bool isLazyLoad, IFileRepo repo)
            : base(parent, isLazyLoad)
        {
            this.NxlFolder = nxlFolder;
            this.repo = repo as ProjectRepo;
        }

        // Add another constructor.
        public FolderViewModel(NxlFolder nxlFolder, FolderViewModel parent, bool isLazyLoad, IFileRepo repo) 
        : base(parent, isLazyLoad)
        {
            this.NxlFolder = nxlFolder;
            this.repo = repo as ProjectRepo;
        }

        public string FolderName
        {
            get { return NxlFolder.Name; }
        }

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
                            Children.Add(folderViewModel);
                        }
                        else
                        {
                            FolderViewModel folderViewModel = new FolderViewModel(inxlFolder, this, true, repo);
                            Children.Add(folderViewModel);

                            // Get children
                            GetChildren(inxlFolder, folderViewModel);
                        }
                    }

                }
            }
            else
            {
                SkydrmLocalApp.Singleton.Log.Info("FolderViewModel LoadChildren do refresh()");
                if (repo != null && SkydrmLocalApp.Singleton.MainWin.viewModel.IsNetworkAvailable)
                {
                    Refresh();
                }
            }

        }

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

                        parentNxlFileVm.Children.Add(nxlFileVm);
                    }
                    else 
                    {
                        FolderViewModel nxlFileVm = new FolderViewModel(inxlFolder, parentNxlFileVm, true, repo);

                        // Should remove Dummy Child first if has
                        if (parentNxlFileVm.HasDummyChild)
                        {
                            parentNxlFileVm.Children.RemoveAt(0);
                        }

                        parentNxlFileVm.Children.Add(nxlFileVm);

                        // Recusive get children
                        GetChildren(inxlFolder, nxlFileVm);
                    }
                }

            }
        }

        public override void Refresh()
        {
            refreshWorker.WorkerReportsProgress = false;
            refreshWorker.WorkerSupportsCancellation = true;
            refreshWorker.DoWork += RefreshWorker;
            refreshWorker.RunWorkerCompleted += OnRefreshComplete;

            SkydrmLocalApp.Singleton.Log.Info("FolderViewModel RefreshWorker IsBusy:" + refreshWorker.IsBusy);
            if (!refreshWorker.IsBusy)
            {
                refreshWorker.RunWorkerAsync();
            }
        }

        private void RefreshWorker(object sender, DoWorkEventArgs args)
        {
            SkydrmLocalApp.Singleton.Log.Info("FolderViewModel RefreshWorker invoke DoWork event");
            bRefreshSuccess = true;

            IList<INxlFile> files = new List<INxlFile>(); 
            try
            {
                ProjectData p = FindProjectView(this).Project;
                repo.SyncRemoteAllFiles(p.ProjectInfo, NxlFolder.PathId, files);
            }
            catch(Exception e)
            {
                bRefreshSuccess = false;
                SkydrmLocalApp.Singleton.Log.Error("FolderViewModel RefreshWorker error: ", e);
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
            SkydrmLocalApp.Singleton.Log.Info("FolderViewModel RefreshWorker invoke RunWorkerCompleted event result:" + bRefreshSuccess);
            if (bRefreshSuccess)
            {
                IList<INxlFile> newFiles = (IList<INxlFile>)args.Result;
                InnerUpdate(newFiles);
            }

            refreshWorker.RunWorkerCompleted -= OnRefreshComplete;
        }

        public void GetFolderLocalAllFile(FolderViewModel folderViewModel)
        {
            // When project has dummy child, should not merge data. 
            // If do merge, it's maybe remove dummy child and change old data. 
            // When this node expend, will not excute 'LoadChildren(true)' method so that missing add child.
            if (!this.HasDummyChild)
            {
                IList<INxlFile> files = new List<INxlFile>();
                ProjectData p = FindProjectView(folderViewModel).Project;
                repo.GetRemoteCurrentFolderFiles(p.ProjectInfo, NxlFolder.PathId, files);
                InnerUpdate(files);
            }
        }

        private void InnerUpdate(IList<INxlFile> newFiles)
        {
            if (repo.CurrentWorkingProject?.ProjectInfo.ProjectId == FindProjectView(this).Project.ProjectInfo.ProjectId)
            {
                if (repo.CurrentWorkingFolder?.PathId == NxlFolder.PathId)
                {
                    // At the same time, notify the project file list to refresh.
                    repo.notifyProjectFileListRefresh?.Invoke(newFiles, NxlFolder.PathId, true);
                }
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
                    if (f.IsFolder && one.Name == f.Name)
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
                            if (fvm.NxlFolder.Name == one.Name)
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
                    if (one.Name == f.Name && f.IsFolder)
                    {
                        // "one" belongs to new set and belongs to old set
                        find = f;
                        break;
                    }
                }

                // "one" belongs to new set but not belongs to old set -- should add it
                if (find == null)
                {
                    SkydrmLocalApp.Singleton.Log.Info(NxlFolder.Name + " Children count:" + Children.Count.ToString());
                    if (this.HasDummyChild)
                    {
                        SkydrmLocalApp.Singleton.Log.Info("Prepare to remove dummy child.");
                        this.Children.RemoveAt(0);
                    }

                    NxlFolder inxlFolder = one as NxlFolder;

                    if (inxlFolder.Children == null || !TreeViewHelper.HasFolderChildren(inxlFolder))
                    {
                        this.Children.Add(new FolderViewModel(inxlFolder, this, false, repo));
                    }
                    else
                    {
                        FolderViewModel folderViewModel = new FolderViewModel(inxlFolder, this, true, repo);
                        this.Children.Add(folderViewModel);

                        // Get children
                        GetChildren(inxlFolder, folderViewModel);
                    }

                    // update 
                    oldFiles.Add(one);
                }
            }
           
        }

        // when user do refresh, original folder also may changed, folder without children may become have children(added folder)
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
                SkydrmLocalApp.Singleton.Log.Error("FolderViewModel GetAndMergeSubFolder error: ", e);
            }
        }

        // Get the newly added folders' children, fix bug 52302.
        #region // Try to get children

        public void TryToGetChildren(string pathId)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(GetChildren), pathId);
        }

        private void GetChildren(object para)
        {
            if (repo == null && !SkydrmLocalApp.Singleton.MainWin.viewModel.IsNetworkAvailable)
            {
                return;
            }

            bool ret = true;
            IList<INxlFile> files = new List<INxlFile>();
            try
            {
                ProjectData p = FindProjectView(this).Project;
                repo.SyncRemoteAllFiles(p.ProjectInfo, para.ToString(), files);
            }
            catch (Exception e)
            {
                ret = false;
                SkydrmLocalApp.Singleton.Log.Error("FolderViewModel GetChildren error: ", e);
            }
            finally
            {
                if (ret)
                {
                    // There is treeView ui operation, so should switch main thread.
                    SkydrmLocalApp.Singleton.Dispatcher.Invoke((Action)delegate
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
                        parentViewmodel.Children.Add(new FolderViewModel(inxlFolder, this, false, repo));
                    }
                    else
                    {
                        FolderViewModel folderViewModel = new FolderViewModel(inxlFolder, this, true, repo);
                        parentViewmodel.Children.Add(folderViewModel);

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
        public void AddFolderItem2CurrentWorkingFolder(NxlFolder inxlFolder)
        {
            foreach (FolderViewModel one in base.Children)
            {
                // have existed, don't add again.
                if (one.NxlFolder?.PathId == inxlFolder.PathId)
                {
                    return;
                }
            }

            this.Children?.Add(new FolderViewModel(inxlFolder, this, false, repo));

            NxlFolder.Children?.Add(inxlFolder);

        }

        // Remove the specified folder
        public void RemoveFolderItem(NxlFolder nxlFolder)
        {
            FolderViewModel toDel = null;
            foreach (FolderViewModel fvm in this.Children)
            {
                if (fvm.NxlFolder.Name == nxlFolder.Name)
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

                // remove project node.
                if (NxlFolder.Children != null && !NxlFolder.Children.Remove(nxlFolder)) // This may remove failed since can't find because of the node is mapped to another object(newly creat from db)
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

        #endregion // Try to get children

      
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
