using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmDesktop;
using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.fileSystem.externalDrive;
using SkydrmDesktop.rmc.fileSystem.mySpace;
using SkydrmDesktop.rmc.fileSystem.sharedWorkspace;
using SkydrmDesktop.rmc.fileSystem.workspace;
using SkydrmLocal.rmc.fileSystem;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.fileSystem.myvault;
using SkydrmLocal.rmc.fileSystem.project;
using SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.model;

namespace SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.viewModel
{
    public class RepoViewModel
    {
        private static readonly string HOME = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_Home");
        private static readonly string WORKSPACE = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_WorkSpace");
        private static readonly string MY_SPACE = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_MySpace");
        private static readonly string MY_VAULT = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_MyVault");
        private static readonly string MY_DRIVE = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_MyDrive");
        private static readonly string SHARE_WITH_ME = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_ShareWithMe");
        private static readonly string PROJECT = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_Project");
        private static readonly string REPOSITORIES = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_Repositories");

        private ObservableCollection<RootViewModel> rtVMList = new ObservableCollection<RootViewModel>();

        public RepoViewModel() { }

        /// <summary>
        /// Note: TreeView item display order like following, mainly depends on the parameter "repos", its initialization in InitTreeView.
        /// 1. WorkSpace
        /// 2. MySpace
        ///       -- MyDrive
        ///       -- MySpace
        /// 3. External repository
        /// 4. Project
        /// </summary>
        public void Start(IList<IFileRepo> repos)
        {
            if (repos == null || repos.Count == 0)
            {
                return;
            }

            // Home root
            RootViewModel home = new RootViewModel(new Root(HOME, new List<INxlFile>(), HOME), false);
            rtVMList.Add(home);

            // External repositories(all external repos' root)
            RootViewModel externalRepo = new RootViewModel(new Root(REPOSITORIES, new List<INxlFile>(), REPOSITORIES), false);
            
            // MySpace(myVault & myDrive root)
            RootViewModel mySpace = new RootViewModel(new Root(MY_SPACE, new List<INxlFile>(), MY_SPACE), false);

            foreach (IFileRepo repo in repos)
            {
                // 1. workSpace
                if (repo is WorkSpaceRepo)
                {
                    var root = new Root(repo.RepoDisplayName, repo.GetFilePool(), repo.RepoType);
                    if (IsHasFolderChildren(root.RepoFiles))
                    {
                        rtVMList.Add(new RootViewModel(root, true, repo));
                    }
                    else
                    {
                        rtVMList.Add(new RootViewModel(root, false, repo));
                    }
                }

                // 2. mySpace
                if (!rtVMList.Contains(mySpace))
                {
                    rtVMList.Add(mySpace);
                }


                // 3. external repository
                if (SkydrmApp.Singleton.IsEnableExternalRepo)
                {
                    if (!rtVMList.Contains(externalRepo))
                    {
                        rtVMList.Add(externalRepo);
                    }
                }

                // 4. Project, special handle.
                if (repo is ProjectRepo)
                {
                    IList<ProjectData> projects = (repo as ProjectRepo).FilePool;
                     
                    if (projects != null && projects.Count > 0)  // For project treeView: has sub nodes 
                        rtVMList.Add(new RootViewModel(new Root(repo.RepoDisplayName, projects), true, repo));
                    else // Do not have any project.
                        rtVMList.Add(new RootViewModel(new Root(repo.RepoDisplayName, projects), false, repo));
                }
                // For myVault & myDrive, should add into mySpace children, so single handle with it.
                else if (repo is MyDriveRepo)
                {
                    var root = new Root(repo.RepoDisplayName, repo.GetFilePool(), repo.RepoType);
                    if (IsHasFolderChildren(root.RepoFiles))
                        mySpace.Children.Add(new RootViewModel(root, true, repo, mySpace));
                    else
                        mySpace.Children.Add(new RootViewModel(root, false, repo, mySpace));
                }
                else if (repo is MyVaultRepo)
                {
                    var root = new Root(repo.RepoDisplayName, repo.GetFilePool(), repo.RepoType);
                    // false means lazy loading.
                    mySpace.Children.Add(new RootViewModel(root, false, repo, mySpace));

                    // add SharedWithMe
                    var shareWithMeRoot = new Root(SHARE_WITH_ME, repo.GetSharedWithMeFiles(), SHARE_WITH_ME);
                    mySpace.Children.Add(new RootViewModel(shareWithMeRoot, false, null, mySpace));
                }

                else if(repo is ExternalRepo || repo is SharedWorkspaceRepo) // external repositories
                {
                    var root = new Root(repo.RepoDisplayName, repo.GetFilePool(), repo.RepoType, repo.RepoProviderClass);
                    if (IsHasFolderChildren(root.RepoFiles))
                        externalRepo.Children.Add(new RootViewModel(root, true, repo, externalRepo));
                    else
                        externalRepo.Children.Add(new RootViewModel(root, false, repo, externalRepo));
                }
            }

            SkydrmApp.Singleton.Log.Info("Root view model count: " + rtVMList.Count);
        }

        public bool IsHasMyVaultViewModel()
        {
            bool ret = false;

            foreach (RootViewModel root in rtVMList)
            {
                if (root.Root.RepoName.Equals(MY_VAULT, StringComparison.CurrentCultureIgnoreCase))
                {
                    ret = true;
                    break;
                }
            }

            return ret;
        }

        private bool IsHasProjectViewModel()
        {
            bool ret = false;

            foreach (RootViewModel root in rtVMList)
            {
                if (root.Root.RepoName.Equals(PROJECT, StringComparison.CurrentCultureIgnoreCase))
                {
                    ret = true;
                    break;
                }
            }

            return ret;
        }

        public RootViewModel GetRootViewModelByName(string rootVM_Name)
        {
            foreach (RootViewModel root in rtVMList)
            {
                if (root.Root.RepoName.Equals(rootVM_Name, StringComparison.CurrentCultureIgnoreCase))
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
                    rtVMList.Add(new RootViewModel(new Root(PROJECT, projects), true, repo));
                }
            }

        }

        public ObservableCollection<RootViewModel> RootVMList
        {
            get { return rtVMList; }
        }

        #region private methods
        private bool IsHasFolderChildren(IList<INxlFile> nodes)
        {
            if(nodes == null || nodes.Count == 0)
            {
                return false;
            }

            bool ret = false;
            foreach(var one in nodes)
            {
                if (one.IsFolder)
                {
                    ret = true;
                    break;
                }
            }

            return ret;
        }
        #endregion // private methods

    }

}
