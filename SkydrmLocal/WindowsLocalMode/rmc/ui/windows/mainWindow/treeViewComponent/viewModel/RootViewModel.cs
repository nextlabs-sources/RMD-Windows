using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmLocal.rmc.fileSystem;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.fileSystem.project;
using SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.helper;
using SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.model;

namespace SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.viewModel
{
    // Wrapper for Root.
    public class RootViewModel : TreeViewItemViewModel
    {
        BackgroundWorker refreshWorker = new BackgroundWorker();

        // MyVault or Project
        public Root Root { get; set; }
        private IFileRepo repo;

        // record old projects.
        public IList<ProjectData> oldResults = new List<ProjectData>();

        public RootViewModel(Root root, bool isLazyLoad, IFileRepo repo, bool bAsSelectFolder = false) : base(null, isLazyLoad)
        {
            this.Root = root;
            this.repo = repo;

            init(bAsSelectFolder);
        }

        private void init(bool bAsSelectFolder)
        {
            if (Root.Projects == null)
            {
                return;
            }

            foreach (ProjectData pd in Root.Projects)
            {
                oldResults.Add(pd);
            }
            // Must InitRefreshWorker before treeView expanded
            InitRefreshWorker();

            // Fix bug 53562,  the Children data are inconsistent with oldResults data when auto refresh,
            // so have it expanded in default to keep the data consisitent.
            IsExpanded = true;

            // register project user remvoe automatically refresh listen event
            if (!bAsSelectFolder)
            {
                SkydrmLocalApp.Singleton.ProjectUpdate += AutoRefresh;
            }
        }

        private void InitRefreshWorker()
        {
            refreshWorker.WorkerReportsProgress = false;
            refreshWorker.WorkerSupportsCancellation = true;
            refreshWorker.DoWork += RefreshWorker;
            refreshWorker.RunWorkerCompleted += OnRefreshComplete;
        }

        public string RootName // project name
        {
            get { return Root.RepoName; }
        }

        protected override void LoadChildren(bool bFirstLoad)
        {

            if (bFirstLoad)
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
            } else
            {
                SkydrmLocalApp.Singleton.Log.Info("RootViewModel LoadChildren do refresh()");
                if (repo != null && SkydrmLocalApp.Singleton.MainWin.viewModel.IsNetworkAvailable)
                {
                    Refresh();
                }

            }

        }

        // Will execute auto refresh ui when background heartbeat detect project is deleted(user is removed from invited project)
        private void AutoRefresh()
        {
            // Note: should switch into ui thread to refresh ui from bg thread.
            SkydrmLocalApp.Singleton.Log.Info("RootViewModel Auto refresh");
            SkydrmLocalApp.Singleton.Dispatcher.Invoke(new Action(() => {
                RefreshWorkerRun();
            }));
        }

        private void RefreshWorkerRun()
        {
            SkydrmLocalApp.Singleton.Log.Info("RootViewModel RefreshWorker IsBusy:" + refreshWorker.IsBusy);
            if (!refreshWorker.IsBusy)
            {
                refreshWorker.RunWorkerAsync();
            }
        }

        // Refresh root projects
        // we can't directly SyncRemoteData() -- fix bug 52664 that RootViewModel.Refresh InvalidCastException(the reproduce rate is low).
        public override void Refresh()
        {
            if (repo is ProjectRepo) // fix bug 52806: since when double clicking MY_VAULT or "ShareWithMe" item, also will access this.
            {
                RefreshWorkerRun();
            }
        }

        // Sync to get all project nodes recursively
        private void RefreshWorker(object sender, DoWorkEventArgs args)
        {
            SkydrmLocalApp.Singleton.Log.Info("RootViewModel RefreshWorker invoke DoWork event");
            bool ret = true;
            try
            {
                ret =  (repo as ProjectRepo).InnerSyncRemoteData();
            }
            catch (Exception e)
            {
                ret = false;
                SkydrmLocalApp.Singleton.Log.Error("RootViewModel RefreshWorker error:", e);
            }
            finally
            {
                args.Result = ret;
            }
        }

        private void OnRefreshComplete(object sender, RunWorkerCompletedEventArgs args)
        {
            SkydrmLocalApp.Singleton.Log.Info("RootViewModel RefreshWorker invoke RunWorkerCompleted event result:" + (bool)args.Result);
            bool bSucess = (bool)args.Result;
            if (!bSucess)
            {
                return;
            }

            IList<ProjectData> newResults = (repo as ProjectRepo).FilePool;

            // For refresh Project List Page
            List<ProjectData> addProject = new List<ProjectData>();
            List<ProjectData> removeProject = new List<ProjectData>();

            // Merge
            SkydrmLocalApp.Singleton.Log.Info("RootViewModel MergeTreeView: oldResults Count "+oldResults.Count
                +", newResults Count "+newResults.Count);
            MergeTreeView(newResults, addProject, removeProject);

            // Get project all nodes from local and merge its first level folder,  fix bug 52438.
            GetAndMergeProjectFolder();

            // At the same time, notify the project page list to refresh.
            (repo as ProjectRepo).notifyProjectPageRefresh?.Invoke(addProject, removeProject);

        }


        public void MergeTreeView(IList<ProjectData> newResults, List<ProjectData> addProject, List<ProjectData> removeProject)
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

                    for(int j = 0; j < this.Children.Count; j++)
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
                    // Fix bug 53222, When user select treeview item will judge DummyChild and remove. If refresh project before click item,the Children
                    // may have DummyChild  -- need test again.
                    SkydrmLocalApp.Singleton.Log.Info("Projects Children count:" + Children.Count.ToString());
                    if (this.HasDummyChild)
                    {
                        SkydrmLocalApp.Singleton.Log.Info("Prepare to remove dummy child.");
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

                    // update
                    oldResults.Add(one);
                    addProject.Add(one);
                }
            }

            #endregion  // merge current all project nodes.
        }


        // when user do refresh, original project also may changed, project without children may become have children(added folder)
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

    }

}
