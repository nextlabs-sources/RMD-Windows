using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmLocal.rmc.fileSystem;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.fileSystem.myvault;
using SkydrmLocal.rmc.fileSystem.project;
using SkydrmLocal.rmc.fileSystem.sharedWithMe;
using SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.model;

namespace SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.viewModel
{
    public class RepoViewModel : TreeViewItemViewModel
    {
        private static readonly string MY_VAULT = CultureStringInfo.MainWin__TreeView_MyVault;
        private static readonly string SHARE_WITH_ME = CultureStringInfo.MainWin__TreeView_ShareWithMe;
        private static readonly string PROJECT = CultureStringInfo.MainWin__TreeView_Project;

        //ReadOnlyCollection<RootViewModel> _roots;
        private ObservableCollection<RootViewModel> _roots = new ObservableCollection<RootViewModel>();

        public RepoViewModel() { }

        public void Start(IList<IFileRepo> repos, bool bAsSelectFolder = false)
        {
            if (repos == null || repos.Count == 0)
            {
                return;
            }

           // List<RootViewModel> list = new List<RootViewModel>();
            foreach (IFileRepo repo in repos)
            {
                if (repo is MyVaultRepo)
                {
                    IList<INxlFile> myVaultFiles = (repo as MyVaultRepo).FilePool;
                    // false means lazy loading.
                    _roots.Add(new RootViewModel(new Root(MY_VAULT, myVaultFiles, null), false, repo, bAsSelectFolder));

                    SkydrmLocalApp.Singleton.Log.Info("Add MyVault treeView item.");
                }
                else if (repo is ShareWithRepo)
                {
                    // todo.
                    IList<INxlFile> shredWithFiles = (repo as ShareWithRepo).FilePool;

                    if (!bAsSelectFolder)
                    {
                        // false means lazy loading.
                        _roots.Add(new RootViewModel(new Root(SHARE_WITH_ME, null, shredWithFiles), false, repo, bAsSelectFolder));

                        SkydrmLocalApp.Singleton.Log.Info("Add SharedWithMe treeView item.");
                    }

                }
                else if (repo is ProjectRepo)
                {
                    IList<ProjectData> projects = (repo as ProjectRepo).FilePool;
                  
                    if (projects != null && projects.Count > 0)   // For project treeView: has sub nodes
                    {
                        _roots.Add(new RootViewModel(new Root(PROJECT, projects), true, repo, bAsSelectFolder));
                    } else // Do not have any project.
                    {
                        _roots.Add(new RootViewModel(new Root(PROJECT, projects), false, repo, bAsSelectFolder));
                    }

                    SkydrmLocalApp.Singleton.Log.Info("Add Project treeView item.");
                }
            }

            SkydrmLocalApp.Singleton.Log.Info("Root view model count: " + _roots.Count);
        }

        public bool IsHasMyVaultViewModel()
        {
            bool ret = false;

            foreach (RootViewModel root in _roots)
            {
                if (root.Root.RepoName.Equals(MY_VAULT, StringComparison.CurrentCultureIgnoreCase))
                {
                    ret = true;
                    break;
                }
            }

            return ret;
        }

        public void AddMyVaultRepo(MyVaultRepo repo)
        {
            if (repo == null)
            {
                return;
            }

            if (!IsHasMyVaultViewModel())
            {
                _roots.Insert(0, new RootViewModel(new Root(MY_VAULT, repo.FilePool, null), false, repo));
            }
        }

        public bool IsHasProjectViewModel()
        {
            bool ret = false;

            foreach (RootViewModel root in _roots)
            {
                if (root.Root.RepoName.Equals(PROJECT, StringComparison.CurrentCultureIgnoreCase))
                {
                    ret = true;
                    break;
                }
            }

            return ret;
        }

        public RootViewModel GetRootViewModel()
        {
            foreach (RootViewModel root in _roots)
            {
                if (root.Root.RepoName.Equals(PROJECT, StringComparison.CurrentCultureIgnoreCase))
                {
                    return root;
                }
            }

            return null;
        }

        public void AddProjectRepoViewModel(ProjectRepo repo)
        {
            if (repo == null)
            {
                return;
            }

            if (!IsHasProjectViewModel())
            {
                IList<ProjectData> projects = (repo as ProjectRepo).FilePool;
                // For project treeView: has sub nodes
                if (projects != null && projects.Count > 0)
                {
                    _roots.Add(new RootViewModel(new Root(PROJECT, projects), true, repo));
                }
            }

        }

        public ObservableCollection<RootViewModel> Roots
        {
            get { return _roots; }
        }

    }

}
