using Alphaleonis.Win32.Filesystem;
using CustomControls;
using CustomControls.componentPages.Preference;
using CustomControls.components.RightsDisplay.model;
using CustomControls.components.TreeView.model;
using CustomControls.components.ValiditySpecify.model;
using CustomControls.components.CentralPolicy.model;
using SkydrmDesktop;
using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.featureProvider.externalDrive.RmsRepoMgr;
using SkydrmDesktop.rmc.fileSystem.externalDrive;
using SkydrmDesktop.rmc.fileSystem.sharedWorkspace;
using SkydrmDesktop.rmc.fileSystem.workspace;
using SkydrmDesktop.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.fileSystem;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.fileSystem.myvault;
using SkydrmLocal.rmc.fileSystem.project;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.view;
using SkydrmDesktop.rmc.fileSystem.localDrive;
using SkydrmDesktop.rmc.fileSystem.mySpace;

namespace SkydrmLocal.rmc.ui.utils
{
    /// <summary>
    /// CustomControl data type and RMD data type converter
    /// </summary>
    static class DataTypeConvertHelper
    {
        public static readonly string WORKSPACE = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_WorkSpace");
        public static readonly string MY_SPACE = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_MySpace");
        public static readonly string MY_VAULT = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_MyVault");
        public static readonly string MY_DRIVE = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_MyDrive");
        public static readonly string PROJECT = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_Project");
        public static readonly string SYSTEMBUCKET = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_SystemBucket");
        // external repository name
        public static string REPOSITORIES = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_Repositories");
        private static string DROPBOX = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_DropBox");
        private static string ONEDRIVE = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_OneDrive");
        private static string BOX = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_Box");
        private static string SHAREPOINT = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_SharePoint");
        private static string SHAREPOINT_ONLINE = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_SharePointOnline");
        private static string SHAREPOINT_ONPREMISE = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_SharePointOnPremise");
        private static string GOOGLE_DRIVE = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_GoogleDrive");

        /// <summary>
        /// Create repo list, used to FileSelectPage.xaml
        /// </summary>
        /// <param name="useForProtect">true is used to protect, false is used to add nxl</param>
        /// <returns></returns>
        public static ObservableCollection<RepoItem> CreateFileRepoList2RepoItem(bool useForProtect)
        {
            ObservableCollection<RepoItem> repoItems = new ObservableCollection<RepoItem>();
            RepoItem localDrive = new RepoItem("", new LocalDriveRepo());
            repoItems.Add(localDrive);

            if (useForProtect)
            {
                // protect file
                RepoItem mydrive = new RepoItem("", new MyDriveRepo());
                repoItems.Add(mydrive);
            }
            else
            {
                // add nxl file

                bool isEnableWorkSpace = SkydrmApp.Singleton.Rmsdk.User.IsEnabledWorkSpace();
                if (isEnableWorkSpace)
                {
                    RepoItem workspace = new RepoItem("", new WorkSpaceRepo());
                    repoItems.Add(workspace);
                }


                RepoItem myvault = new RepoItem("", new MyVaultRepo());
                repoItems.Add(myvault);
            }

            List<IFileRepo> externalRepos = new List<IFileRepo>();
            if (SkydrmApp.Singleton.IsEnableExternalRepo)
            {
                // Get external repos from local firstly if have.
                List<IRmsRepo> repos = SkydrmApp.Singleton.RmsRepoMgr.ListRepositories();
                externalRepos = AddExternalRepositories(repos);
            }

            foreach (var repo in externalRepos)
            {
                repoItems.Add(new RepoItem(REPOSITORIES, repo));
            }
            return repoItems;
        }

        private static List<IFileRepo> AddExternalRepositories(List<IRmsRepo> repos)
        {
            List<IFileRepo> externalRepos = new List<IFileRepo>();
            if (repos.Count == 0)
            {
                return externalRepos;
            }

            foreach (var one in repos)
            {
                // Filter out the local drive
                if (one.Type == ExternalRepoType.LOCAL_DRIVE)
                {
                    continue;
                }

                // Now don't support this class
                if (one.ProviderClass == SkydrmDesktop.rmc.fileSystem.utils.FileSysConstant.REPO_CLASS_PERSONAL)
                {
                    /*
                    var drive = ExternalRepoFactory.Create(one);
                    if (drive != null)
                    {
                        externalRepos.Add(drive);
                    } */

                    continue;
                }
                else if (one.ProviderClass == SkydrmDesktop.rmc.fileSystem.utils.FileSysConstant.REPO_CLASS_APPLICATION)
                {
                    var repo = SharedWorkspaceRepo.Create(one);
                    externalRepos.Add(repo);
                }
            }
            return externalRepos;
        }

        /// <summary>
        /// Create ProjectRepoItem list, used to FileSelectPage.xaml
        /// </summary>
        /// <returns></returns>
        public static ObservableCollection<ProjectRepoItem> CreatProjectRepo2ProjectRepoItem()
        {
            ObservableCollection<ProjectRepoItem> repoItems = new ObservableCollection<ProjectRepoItem>();

            ProjectRepo projectRepo = new ProjectRepo();
            var plist = projectRepo.GetProjectsData();
            var plist2 = plist.OrderBy(p => p.ProjectInfo.Name).ToList();
            var pList3 = plist2.OrderByDescending(p => p.ProjectInfo.BOwner).ToList();

            foreach (var item in pList3)
            {
                repoItems.Add(new ProjectRepoItem(projectRepo, item));
            }
            return repoItems;
        }

        /// <summary>
        /// RMD list FileRepos data type 
        /// convert to 
        /// CustomControls.components.TreeView.model.Node data type
        /// </summary>
        /// <param name="repos"></param>
        /// <returns></returns>
        public static List<Node> FileRepoList2CustomControlNodes(IList<IFileRepo> repos, CurrentSelectedSavePath currentSavePath, bool addMyVault=true)
        {
            List<Node> resultNodes = new List<Node>();
            List<IFileRepo> externalRepos = new List<IFileRepo>();

            if (repos == null)
            {
                return resultNodes;
            }

            foreach (var repo in repos)
            {
                // For myVault, should add into mySpace children, so single handle with it.
                if (repo is MyVaultRepo)
                {
                    if (!addMyVault)
                    {
                        continue;
                    }
                    // 'MySpace' is dummy root node
                    List<Node> mySpaceChild = new List<Node>(1);
                    //Node mySpace = new Node(MY_SPACE, "-1", MY_SPACE,
                    //    new BitmapImage(new Uri("/rmc/ui/windows/mainWindow/treeViewComponent/Images/myspace.png", UriKind.Relative)),
                    //    mySpaceChild, "", "");
                    bool isFirstSelect = (currentSavePath.RepoName.Equals(MY_VAULT)) && (currentSavePath.OwnerId.Equals("0"));
                    Node mySpace = new Node(MY_VAULT, "0", MY_SPACE,
                        new BitmapImage(new Uri("/rmc/ui/windows/mainWindow/treeViewComponent/Images/myspace.png", UriKind.Relative)),
                        mySpaceChild, "/nxl_myvault_nxl/", "SkyDRM://" + MY_SPACE, isFirstSelect);

                    // 'MyValut' node
                    //bool isFirstSelect = (currentSavePath.RepoName.Equals(MY_VAULT)) && (currentSavePath.OwnerId.Equals("0"));
                    //Node myVault = new Node(MY_VAULT, "0", MY_VAULT,
                    //    new BitmapImage(new Uri("/rmc/ui/windows/mainWindow/treeViewComponent/Images/myvault.png", UriKind.Relative)),
                    //    new List<Node>(), "/", MY_VAULT, isFirstSelect);

                    //mySpaceChild.Add(myVault);

                    resultNodes.Add(mySpace);
                }
                // For project, special handle.
                else if (repo is ProjectRepo)
                {
                    IList<ProjectData> projects = (repo as ProjectRepo).GetAllProjectAndFolder();
                    var list = projects.OrderBy(p => p.ProjectInfo.Name).ToList();
                    var fpList = list.OrderByDescending(p => p.ProjectInfo.BOwner).ToList();
                    //var fpList = (repo as ProjectRepo).FilePool;

                    // 'Project' node
                    List<Node> projectNodes = new List<Node>();
                    Node projectAll = new Node(PROJECT, "-1", PROJECT,
                        new BitmapImage(new Uri("/rmc/ui/windows/mainWindow/treeViewComponent/Images/project.png", UriKind.Relative)),
                        projectNodes, "", "");

                    // each project node 
                    foreach (var project in fpList)
                    {
                        bool isFirstSelect = (currentSavePath.RepoName.Equals(PROJECT)) && (currentSavePath.OwnerId.Equals(project.ProjectInfo.ProjectId.ToString()))
                            && (currentSavePath.DestPathId.Equals("/"));

                        List<Node> child = new List<Node>();

                        Node node = new Node(PROJECT, project.ProjectInfo.ProjectId.ToString(), project.ProjectInfo.DisplayName, project.ProjectInfo.BOwner ?
                            new BitmapImage(new Uri("/rmc/ui/windows/mainWindow/treeViewComponent/Images/projectByMe.png", UriKind.Relative)) :
                            new BitmapImage(new Uri("/rmc/ui/windows/mainWindow/treeViewComponent/Images/projectByOthers.png", UriKind.Relative)),
                            child, "/", "SkyDRM://" + PROJECT + "/"+ project.ProjectInfo.DisplayName + "/", isFirstSelect);

                        InnerGetFolder(currentSavePath, PROJECT, project.ProjectInfo.ProjectId.ToString(), project.FileNodes, child, "/" + project.ProjectInfo.DisplayName);

                        projectNodes.Add(node);
                    }

                    resultNodes.Add(projectAll);
                }
                else if (repo is WorkSpaceRepo)
                {
                    IList<INxlFile> folders = (repo as WorkSpaceRepo).GetAllFolders();
                    //IList<INxlFile> folders = (repo as WorkSpaceRepo).GetFilePool();

                    // 'WorkSpace' node
                    bool isFirstSelect = (currentSavePath.OwnerId.Equals(SkydrmApp.Singleton.SystemProject.Id.ToString())) 
                        && (currentSavePath.RepoName.Equals(WORKSPACE))
                        && (currentSavePath.DestPathId.Equals("/"));

                    List<Node> child = new List<Node>();

                    Node workSpace = new Node(WORKSPACE, SkydrmApp.Singleton.SystemProject.Id.ToString(), WORKSPACE,
                        new BitmapImage(new Uri("/rmc/ui/windows/mainWindow/treeViewComponent/Images/workspace.png", UriKind.Relative)),
                        child, "/", "SkyDRM://" + WORKSPACE + "/", isFirstSelect);

                    InnerGetFolder(currentSavePath, WORKSPACE, SkydrmApp.Singleton.SystemProject.Id.ToString(), folders, child);

                    resultNodes.Add(workSpace);
                }
                // Other repo
                else if(repo is ExternalRepo || repo is SharedWorkspaceRepo)
                {
                    externalRepos.Add(repo);
                }
            }
            // ExternalRepo
            if (externalRepos.Count > 0)
            {
                // 'REPOSITORIES' node
                List<Node> externalRepoNodes = new List<Node>();
                Node externalRepoAll = new Node(REPOSITORIES, "-1", REPOSITORIES,
                    new BitmapImage(new Uri("/rmc/ui/windows/mainWindow/treeViewComponent/Images/repositories.png", UriKind.Relative)),
                    externalRepoNodes, "", "");

                // each externalRepo node 
                foreach (var externalRepo in externalRepos)
                {
                    IList<INxlFile> folders = null;
                    if (externalRepo is ExternalRepo)
                    {
                        //ExternalRepo GetAllFoldersFromDB() method need to change in the future.
                        folders = (externalRepo as ExternalRepo).GetAllFoldersFromDB();
                        //folders = (externalRepo as ExternalRepo).GetFilePool();
                    }
                    if (externalRepo is SharedWorkspaceRepo)
                    {
                        folders = (externalRepo as SharedWorkspaceRepo).GetAllFolders();
                        //folders = (externalRepo as SharedWorkspaceRepo).GetFilePool();
                    }

                    bool isFirstSelect = (currentSavePath.RepoName.Equals(REPOSITORIES)) && (currentSavePath.OwnerId.Equals(externalRepo.RepoId))
                        && (currentSavePath.DestPathId.Equals("/"));

                    List<Node> child = new List<Node>();

                    Node node = new Node(REPOSITORIES, externalRepo.RepoId, externalRepo.RepoDisplayName, GetExternalRepoIcon(externalRepo.RepoType),
                        child, "/", "SkyDRM://" + REPOSITORIES + "/" + externalRepo.RepoDisplayName + "/", isFirstSelect);

                    InnerGetFolder(currentSavePath, REPOSITORIES, externalRepo.RepoId, folders, child, "/" + externalRepo.RepoDisplayName);

                    externalRepoNodes.Add(node);
                }

                int pNodeIndex = 0;
                for (int i = 0; i < resultNodes.Count; i++)
                {
                    if (resultNodes[i].Name.Equals(PROJECT))
                    {
                        pNodeIndex = i;
                        break;
                    }
                }
      
                resultNodes.Insert(pNodeIndex, externalRepoAll);
            }
            
            return resultNodes;
        }

        private static void InnerGetFolder(CurrentSelectedSavePath currentSavePath, string repoName, string ownerId, IList<INxlFile> childNodes, List<Node> result, string repoRootName = "")
        {
            if (childNodes == null)
            {
                return;
            }

            foreach (var item in childNodes)
            {
                if (item.IsFolder)
                {
                    NxlFolder folder = item as NxlFolder;

                    List<Node> child = new List<Node>();
                    string pathDest = folder.PathId;

                    bool isFirstSelect = (currentSavePath.RepoName.Equals(repoName)) && (currentSavePath.OwnerId.Equals(ownerId))
                            && (currentSavePath.DestPathId.Equals(pathDest));

                    Node node = new Node(repoName, ownerId, item.Name,
                        new BitmapImage(new Uri("/rmc/ui/windows/mainWindow/treeViewComponent/Images/Folder.png", UriKind.Relative)),
                        child, pathDest, "SkyDRM://" + repoName + repoRootName+ folder.DisplayPath, isFirstSelect);

                    InnerGetFolder(currentSavePath, repoName, ownerId, folder.Children, child, repoRootName);

                    result.Add(node);
                }
            }
        }

        #region Use for Add Nxl File
        /// <summary>
        /// Use for Add Nxl file,
        /// RMD list FileRepos data type 
        /// convert to 
        /// CustomControls.components.TreeView.model.Node data type
        /// </summary>
        /// <param name="repos"></param>
        /// <returns></returns>
        public static List<Node> FileRepoList2CustomControlNodes(IList<IFileRepo> repos)
        {
            List<Node> resultNodes = new List<Node>();
            List<IFileRepo> externalRepos = new List<IFileRepo>();

            foreach (var repo in repos)
            {
                // For project, special handle.
                if (repo is ProjectRepo)
                {
                    IList<ProjectData> projects = (repo as ProjectRepo).GetAllProjectAndFolder();
                    var list = projects.OrderBy(p => p.ProjectInfo.Name).ToList();
                    var fpList = list.OrderByDescending(p => p.ProjectInfo.BOwner).ToList();
                    //var fpList = (repo as ProjectRepo).FilePool;

                    // 'Project' node
                    List<Node> projectNodes = new List<Node>();
                    Node projectAll = new Node(PROJECT, "-1", PROJECT,
                        new BitmapImage(new Uri("/rmc/ui/windows/mainWindow/treeViewComponent/Images/project.png", UriKind.Relative)),
                        projectNodes, "", "");

                    // each project node 
                    foreach (var project in fpList)
                    {
                        List<Node> child = new List<Node>();

                        Node node = new Node(PROJECT, project.ProjectInfo.ProjectId.ToString(), project.ProjectInfo.DisplayName, project.ProjectInfo.BOwner ?
                            new BitmapImage(new Uri("/rmc/ui/windows/mainWindow/treeViewComponent/Images/projectByMe.png", UriKind.Relative)) :
                            new BitmapImage(new Uri("/rmc/ui/windows/mainWindow/treeViewComponent/Images/projectByOthers.png", UriKind.Relative)),
                            child, "/", "SkyDRM://" + PROJECT + "/" + project.ProjectInfo.DisplayName + "/", false);

                        InnerGetFolder(PROJECT, project.ProjectInfo.ProjectId.ToString(), project.FileNodes, child, "/" + project.ProjectInfo.DisplayName);

                        projectNodes.Add(node);
                    }

                    resultNodes.Add(projectAll);
                }
                else if (repo is WorkSpaceRepo)
                {
                    IList<INxlFile> folders = (repo as WorkSpaceRepo).GetAllFolders();
                    //IList<INxlFile> folders = (repo as WorkSpaceRepo).GetFilePool();

                    // 'WorkSpace' node
                    List<Node> child = new List<Node>();
                    Node workSpace = new Node(WORKSPACE, SkydrmApp.Singleton.SystemProject.Id.ToString(), WORKSPACE,
                        new BitmapImage(new Uri("/rmc/ui/windows/mainWindow/treeViewComponent/Images/workspace.png", UriKind.Relative)),
                        child, "/", "SkyDRM://" + WORKSPACE + "/", false);

                    InnerGetFolder(WORKSPACE, SkydrmApp.Singleton.SystemProject.Id.ToString(), folders, child);

                    resultNodes.Add(workSpace);
                }
                // Other repo
                else if (repo is ExternalRepo || repo is SharedWorkspaceRepo)
                {
                    externalRepos.Add(repo);
                }
                else if (repo is MyVaultRepo)
                {
                    // 'MySpace' node
                    Node myspace = new Node(MY_SPACE, "0", MY_SPACE,
                        new BitmapImage(new Uri("/rmc/ui/windows/mainWindow/treeViewComponent/Images/myspace.png", UriKind.Relative)),
                        null, "/nxl_myvault_nxl/", "SkyDRM://" + MY_SPACE + "/", false);

                    resultNodes.Add(myspace);
                }
            }
            // ExternalRepo
            if (externalRepos.Count > 0)
            {
                // 'REPOSITORIES' node
                List<Node> externalRepoNodes = new List<Node>();
                Node externalRepoAll = new Node(REPOSITORIES, "-1", REPOSITORIES,
                    new BitmapImage(new Uri("/rmc/ui/windows/mainWindow/treeViewComponent/Images/repositories.png", UriKind.Relative)),
                    externalRepoNodes, "", "");

                // each externalRepo node 
                foreach (var externalRepo in externalRepos)
                {
                    IList<INxlFile> folders = null;
                    if (externalRepo is ExternalRepo)
                    {
                        //ExternalRepo GetAllFoldersFromDB() method need to change in the future.
                        folders = (externalRepo as ExternalRepo).GetAllFoldersFromDB();
                        //folders = (externalRepo as ExternalRepo).GetFilePool();
                    }
                    if (externalRepo is SharedWorkspaceRepo)
                    {
                        folders = (externalRepo as SharedWorkspaceRepo).GetAllFolders();
                        //folders = (externalRepo as SharedWorkspaceRepo).GetFilePool();
                    }

                    List<Node> child = new List<Node>();

                    Node node = new Node(REPOSITORIES, externalRepo.RepoId, externalRepo.RepoDisplayName, GetExternalRepoIcon(externalRepo.RepoType),
                        child, "/", "SkyDRM://" + REPOSITORIES + "/" + externalRepo.RepoDisplayName + "/", false);

                    InnerGetFolder(REPOSITORIES, externalRepo.RepoId, folders, child, "/" + externalRepo.RepoDisplayName);

                    externalRepoNodes.Add(node);
                }

                int pNodeIndex = 0;
                for (int i = 0; i < resultNodes.Count; i++)
                {
                    if (resultNodes[i].Name.Equals(PROJECT))
                    {
                        pNodeIndex = i;
                        break;
                    }
                }

                resultNodes.Insert(pNodeIndex, externalRepoAll);
            }

            return resultNodes;
        }

        private static void InnerGetFolder(string repoName, string ownerId, IList<INxlFile> childNodes, List<Node> result, string repoRootName = "")
        {
            if (childNodes == null)
            {
                return;
            }
            foreach (var item in childNodes)
            {
                if (item.IsFolder)
                {
                    NxlFolder folder = item as NxlFolder;

                    List<Node> child = new List<Node>();
                    string pathDest = folder.PathId;

                    Node node = new Node(repoName, ownerId, item.Name,
                        new BitmapImage(new Uri("/rmc/ui/windows/mainWindow/treeViewComponent/Images/Folder.png", UriKind.Relative)),
                        child, pathDest, "SkyDRM://" + repoName + repoRootName + folder.DisplayPath, false);

                    InnerGetFolder(repoName, ownerId, folder.Children, child, repoRootName);

                    result.Add(node);
                }
            }
        }
        #endregion

        private static BitmapImage GetExternalRepoIcon(string externalRepoType)
        {
            if (externalRepoType.Equals(DROPBOX))
            {
                return new BitmapImage(new Uri("/rmc/ui/windows/mainWindow/treeViewComponent/Images/dropbox.png", UriKind.Relative));
            }
            if (externalRepoType.Equals(ONEDRIVE))
            {
                return new BitmapImage(new Uri("/rmc/ui/windows/mainWindow/treeViewComponent/Images/oneDrive.png", UriKind.Relative));
            }
            if (externalRepoType.Equals(BOX))
            {
                return new BitmapImage(new Uri("/rmc/ui/windows/mainWindow/treeViewComponent/Images/box.png", UriKind.Relative));
            }
            if (externalRepoType.Equals(GOOGLE_DRIVE))
            {
                return new BitmapImage(new Uri("/rmc/ui/windows/mainWindow/treeViewComponent/Images/googleDrive.png", UriKind.Relative));
            }
            if (externalRepoType.Equals(SHAREPOINT)
                || externalRepoType.Equals(SHAREPOINT_ONLINE)
                || externalRepoType.Equals(SHAREPOINT_ONPREMISE))
            {
                return new BitmapImage(new Uri("/rmc/ui/windows/mainWindow/treeViewComponent/Images/sharepoint.png", UriKind.Relative));
            }
            return null;
        }

        /// <summary>
        /// SDK 'ProjectClassification' type 
        /// convert to 
        /// CustomControls.pages.CentralPolicy.model 'Classification' type
        /// </summary>
        /// <param name="sdkTag"></param>
        /// <returns></returns>
        public static Classification[] SdkTag2CustomControlTag(ProjectClassification[] sdkTag)
        {
            if (sdkTag == null || sdkTag.Length == 0)
            {
                return new Classification[0];
            }

            Classification[] tags = new Classification[sdkTag.Length];
            for (int i = 0; i < sdkTag.Length; i++)
            {
                tags[i].name = sdkTag[i].name;
                tags[i].isMultiSelect = sdkTag[i].isMultiSelect;
                tags[i].isMandatory = sdkTag[i].isMandatory;
                tags[i].labels = sdkTag[i].labels;
            }
            return tags;
        }

        /// <summary>
        /// RMD ProjectData data type 
        /// convert to 
        /// CustomControls.Project data type
        /// </summary>
        /// <param name="projectInfos"></param>
        /// <returns></returns>
        public static List<Project> ProjectInfo2CustomControlProjects(List<SkydrmLocal.rmc.fileSystem.project.ProjectData> projectDatas)
        {
            List<Project> projects = new List<Project>();
            foreach (var item in projectDatas)
            {
                Project project = new Project()
                {
                    Id = item.ProjectInfo.ProjectId,
                    Name = item.ProjectInfo.DisplayName,
                    IsOwner = item.ProjectInfo.BOwner,
                    CreateTime = DateTime.MinValue,
                    FileCount = item.FileNodes.Count,
                    InvitedBy = "",
                    IsChecked = false
                };
                projects.Add(project);
            }
            return projects;
        }

        /// <summary>
        /// RMD SDK FileRights type 
        /// convert to 
        /// string type
        /// </summary>
        /// <param name="rights"></param>
        /// <param name="isAddWaterMark"></param>
        /// <param name="isAddVilidity"></param>
        /// <returns></returns>
        public static List<string> SDKRights2RightsString(List<FileRights> rights, bool isAddVilidity = true)
        {
            List<string> rightsItems = new List<string>();
            foreach (var item in rights)
            {
                switch (item)
                {
                    case FileRights.RIGHT_VIEW:
                        rightsItems.Add(CultureStringInfo.SelectRights_View);
                        break;
                    case FileRights.RIGHT_EDIT:
                        rightsItems.Add(CultureStringInfo.SelectRights_Edit);
                        break;
                    case FileRights.RIGHT_PRINT:
                        rightsItems.Add(CultureStringInfo.SelectRights_Print);
                        break;
                    case FileRights.RIGHT_CLIPBOARD:
                        break;
                    case FileRights.RIGHT_SAVEAS://when protect file will use download right instead of saveAs right
                        break;
                    case FileRights.RIGHT_DECRYPT:
                        rightsItems.Add(CultureStringInfo.SelectRights_Extract);
                        break;
                    case FileRights.RIGHT_SCREENCAPTURE:
                        break;
                    case FileRights.RIGHT_SEND:
                        break;
                    case FileRights.RIGHT_CLASSIFY:
                        break;
                    case FileRights.RIGHT_SHARE:
                        rightsItems.Add(CultureStringInfo.SelectRights_Share);
                        break;
                    case FileRights.RIGHT_DOWNLOAD:
                        rightsItems.Add( CultureStringInfo.SelectRights_SaveAs);
                        break;
                    case FileRights.RIGHT_WATERMARK:
                        rightsItems.Add(CultureStringInfo.SelectRights_Watermark);
                        break;
                }
            }
            if (isAddVilidity)
            {
                rightsItems.Add(CultureStringInfo.SelectRights_Validity);
            }
            return rightsItems;
        }

        /// <summary>
        /// RMD SDK FileRights type 
        /// convert to 
        /// CustomControls.pages.DigitalRights.model.FileRights type
        /// <param name="rights"></param>
        /// <param name="isAddWaterMark"></param>
        /// <param name="isAddVilidity"></param>
        /// <returns></returns>
        public static HashSet<CustomControls.components.DigitalRights.model.FileRights> SDKRights2CustomControlRights(FileRights[] rights, bool isAddWaterMark = true, bool isAddVilidity = true)
        {
            HashSet<CustomControls.components.DigitalRights.model.FileRights> fileRights = new HashSet<CustomControls.components.DigitalRights.model.FileRights>();
            foreach (var item in rights)
            {
                switch (item)
                {
                    case FileRights.RIGHT_VIEW:
                        fileRights.Add(CustomControls.components.DigitalRights.model.FileRights.RIGHT_VIEW);
                        break;
                    case FileRights.RIGHT_EDIT:
                        fileRights.Add(CustomControls.components.DigitalRights.model.FileRights.RIGHT_EDIT);
                        break;
                    case FileRights.RIGHT_PRINT:
                        fileRights.Add(CustomControls.components.DigitalRights.model.FileRights.RIGHT_PRINT);
                        break;
                    case FileRights.RIGHT_CLIPBOARD:
                        fileRights.Add(CustomControls.components.DigitalRights.model.FileRights.RIGHT_CLIPBOARD);
                        break;
                    case FileRights.RIGHT_SAVEAS:
                        fileRights.Add(CustomControls.components.DigitalRights.model.FileRights.RIGHT_SAVEAS);
                        break;
                    case FileRights.RIGHT_DECRYPT:
                        fileRights.Add(CustomControls.components.DigitalRights.model.FileRights.RIGHT_DECRYPT);
                        break;
                    case FileRights.RIGHT_SCREENCAPTURE:
                        fileRights.Add(CustomControls.components.DigitalRights.model.FileRights.RIGHT_SCREENCAPTURE);
                        break;
                    case FileRights.RIGHT_SEND:
                        fileRights.Add(CustomControls.components.DigitalRights.model.FileRights.RIGHT_SEND);
                        break;
                    case FileRights.RIGHT_CLASSIFY:
                        fileRights.Add(CustomControls.components.DigitalRights.model.FileRights.RIGHT_CLASSIFY);
                        break;
                    case FileRights.RIGHT_SHARE:
                        fileRights.Add(CustomControls.components.DigitalRights.model.FileRights.RIGHT_SHARE);
                        break;
                    case FileRights.RIGHT_DOWNLOAD:
                        //when protect file will use download right instead of saveAs right
                        // so nxl file download right should display saveAs right
                        fileRights.Add(CustomControls.components.DigitalRights.model.FileRights.RIGHT_SAVEAS);
                        break;
                }
            }
            if (isAddWaterMark)
            {
                fileRights.Add(CustomControls.components.DigitalRights.model.FileRights.RIGHT_WATERMARK);
            }
            if (isAddVilidity)
            {
                fileRights.Add(CustomControls.components.DigitalRights.model.FileRights.RIGHT_VALIDITY);
            }
            return fileRights;
        }

        /// <summary>
        /// RMD SDK FileRights type 
        /// convert to 
        /// CustomControls.components.RightsDisplay.model.RightsItem type
        /// </summary>
        /// <param name="rights"></param>
        /// <param name="isAddWaterMark"></param>
        /// <param name="isAddVilidity"></param>
        /// <returns></returns>
        public static ObservableCollection<RightsItem> SDKRights2RightsDisplayModel(FileRights[] rights, bool isAddWaterMark = true, bool isAddVilidity = true)
        {
            ObservableCollection<RightsItem> rightsItems = new ObservableCollection<RightsItem>();
            foreach (var item in rights)
            {
                switch (item)
                {
                    case FileRights.RIGHT_VIEW:
                        rightsItems.Add(new RightsItem(new BitmapImage(new Uri("/rmc/resources/icons/icon_rights_view.png", UriKind.Relative)),
                            CultureStringInfo.SelectRights_View));
                        break;
                    case FileRights.RIGHT_EDIT:
                        rightsItems.Add(new RightsItem(new BitmapImage(new Uri("/rmc/resources/icons/icon_rights_edit.png", UriKind.Relative)),
                           CultureStringInfo.SelectRights_Edit));
                        break;
                    case FileRights.RIGHT_PRINT:
                        rightsItems.Add(new RightsItem(new BitmapImage(new Uri("/rmc/resources/icons/icon_rights_print.png", UriKind.Relative)),
                           CultureStringInfo.SelectRights_Print));
                        break;
                    case FileRights.RIGHT_CLIPBOARD:
                        break;
                    case FileRights.RIGHT_SAVEAS://when protect file will use download right instead of saveAs right
                        break;
                    case FileRights.RIGHT_DECRYPT:
                        rightsItems.Add(new RightsItem(new BitmapImage(new Uri("/rmc/resources/icons/icon_rights_extract.png", UriKind.Relative)),
                           CultureStringInfo.SelectRights_Extract));
                        break;
                    case FileRights.RIGHT_SCREENCAPTURE:
                        break;
                    case FileRights.RIGHT_SEND:
                        break;
                    case FileRights.RIGHT_CLASSIFY:
                        break;
                    case FileRights.RIGHT_SHARE:
                        rightsItems.Add(new RightsItem(new BitmapImage(new Uri("/rmc/resources/icons/icon_rights_share.png", UriKind.Relative)),
                           CultureStringInfo.SelectRights_Share));
                        break;
                    case FileRights.RIGHT_DOWNLOAD:
                        rightsItems.Add(new RightsItem(new BitmapImage(new Uri("/rmc/resources/icons/icon_rights_save_as.png", UriKind.Relative)),
                          CultureStringInfo.SelectRights_SaveAs));
                        break;
                }
            }
            if (isAddWaterMark)
            {
                rightsItems.Add(new RightsItem(new BitmapImage(new Uri("/rmc/resources/icons/icon_rights_watermark.png", UriKind.Relative)),
                           CultureStringInfo.SelectRights_Watermark));
            }
            if (isAddVilidity)
            {
                rightsItems.Add(new RightsItem(new BitmapImage(new Uri("/rmc/resources/icons/icon_rights_validity.png", UriKind.Relative)),
                           CultureStringInfo.SelectRights_Validity));
            }
            return rightsItems;
        }

        /// <summary>
        /// CustomControls.pages.DigitalRights.model.FileRights type
        /// convert to 
        /// RMD SDK FileRights type 
        /// </summary>
        /// <param name="rights"></param>
        /// <returns></returns>
        public static List<FileRights> CustomCtrRights2SDKRights(HashSet<CustomControls.components.DigitalRights.model.FileRights> rights)
        {
            List<FileRights> result = new List<FileRights>();
            foreach (var item in rights)
            {
                switch (item)
                {
                    case CustomControls.components.DigitalRights.model.FileRights.RIGHT_VIEW:
                        result.Add(FileRights.RIGHT_VIEW);
                        break;
                    case CustomControls.components.DigitalRights.model.FileRights.RIGHT_EDIT:
                        result.Add(FileRights.RIGHT_EDIT);
                        break;
                    case CustomControls.components.DigitalRights.model.FileRights.RIGHT_PRINT:
                        result.Add(FileRights.RIGHT_PRINT);
                        break;
                    case CustomControls.components.DigitalRights.model.FileRights.RIGHT_CLIPBOARD:
                        result.Add(FileRights.RIGHT_CLIPBOARD);
                        break;
                    case CustomControls.components.DigitalRights.model.FileRights.RIGHT_SAVEAS:
                        //result.Add(FileRights.RIGHT_SAVEAS);
                        // Should write "Download" rights for "Save As", but the ui should display "Save As". -- fix bug 52176
                        result.Add(FileRights.RIGHT_DOWNLOAD);
                        break;
                    case CustomControls.components.DigitalRights.model.FileRights.RIGHT_DECRYPT:
                        result.Add(FileRights.RIGHT_DECRYPT);
                        break;
                    case CustomControls.components.DigitalRights.model.FileRights.RIGHT_SCREENCAPTURE:
                        result.Add(FileRights.RIGHT_SCREENCAPTURE);
                        break;
                    case CustomControls.components.DigitalRights.model.FileRights.RIGHT_SEND:
                        result.Add(FileRights.RIGHT_SEND);
                        break;
                    case CustomControls.components.DigitalRights.model.FileRights.RIGHT_CLASSIFY:
                        result.Add(FileRights.RIGHT_CLASSIFY);
                        break;
                    case CustomControls.components.DigitalRights.model.FileRights.RIGHT_SHARE:
                        result.Add(FileRights.RIGHT_SHARE);
                        break;
                    case CustomControls.components.DigitalRights.model.FileRights.RIGHT_DOWNLOAD:
                        result.Add(FileRights.RIGHT_DOWNLOAD);
                        break;
                    case CustomControls.components.DigitalRights.model.FileRights.RIGHT_VALIDITY:
                        break;
                    case CustomControls.components.DigitalRights.model.FileRights.RIGHT_WATERMARK:
                        result.Add(FileRights.RIGHT_WATERMARK);
                        break;
                    default:
                        break;
                }
            }
            return result;
        }

        /// <summary>
        /// RMD SDK Expiration type 
        /// convert to 
        /// CustomControls.components.ValiditySpecify.model.IExpiry type.
        /// Note: When SDK Expiration is from Preference, the RELATIVE_EXPIRE type use 'int years = (int)(expiration.Start >> 32)'
        /// SDK Expiration is from Nxl File, the RELATIVE_EXPIRE type use like ABSOLUTE_EXPIRE type.
        /// </summary>
        /// <param name="expiration"></param>
        /// <param name="expiryDate"></param>
        /// <param name="isFromUserPreference"></param>
        /// <returns></returns>
        public static IExpiry SdkExpiry2CustomCtrExpiry(Expiration expiration, out string expiryDate, bool isFromUserPreference = false)
        {
            expiryDate = CultureStringInfo.ApplicationFindResource("NeverExpire");
            IExpiry expiry = new NeverExpireImpl();
            switch (expiration.type)
            {
                case ExpiryType.NEVER_EXPIRE:
                    expiry = new NeverExpireImpl();
                    expiryDate = CultureStringInfo.ApplicationFindResource("NeverExpire");
                    break;
                case ExpiryType.RELATIVE_EXPIRE:
                    if (isFromUserPreference)
                    {
                        int years = (int)(expiration.Start >> 32);
                        int months = (int)expiration.Start;
                        int weeks = (int)(expiration.End >> 32);
                        int days = (int)expiration.End;
                        expiry = new RelativeImpl(years, months, weeks, days);

                        DateTime dateStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);
                        string dateRelativeS = dateStart.ToString("MMMM dd, yyyy");
                        if (years == 0 && months == 0 && weeks == 0 && days == 0)
                        {
                            days = 1;
                        }
                        DateTime dateEnd = dateStart.AddYears(years).AddMonths(months).AddDays(7 * weeks + days - 1).AddHours(23).AddMinutes(59).AddSeconds(59);
                        string dateRelativeE = dateEnd.ToString("MMMM dd, yyyy");
                        expiryDate = dateRelativeS + " To " + dateRelativeE;
                    }
                    else
                    {
                        string dateRelativeS = DateTimeHelper.TimestampToDateTime(expiration.Start);
                        string dateRelativeE = DateTimeHelper.TimestampToDateTime(expiration.End);
                        expiry = new RelativeImpl(0, 0, 0, CountDays(Convert.ToDateTime(dateRelativeS).Ticks, Convert.ToDateTime(dateRelativeE).Ticks));
                        expiryDate = "Until " + dateRelativeE;
                    }
                    break;
                case ExpiryType.ABSOLUTE_EXPIRE:
                    string dateAbsoluteE = DateTimeHelper.TimestampToDateTime(expiration.End);
                    expiry = new AbsoluteImpl(expiration.End);
                    expiryDate = "Until " + dateAbsoluteE;
                    break;
                case ExpiryType.RANGE_EXPIRE:
                    string dateRangeS = DateTimeHelper.TimestampToDateTime(expiration.Start);
                    string dateRangeE = DateTimeHelper.TimestampToDateTime(expiration.End);
                    expiry = new RangeImpl(expiration.Start, expiration.End);
                    expiryDate = dateRangeS + " To " + dateRangeE;
                    break;
            }
            return expiry;
        }
        private static int CountDays(long startMillis, long endMillis)
        {
            long elapsedTicks = endMillis - startMillis;
            TimeSpan elapsedSpan = new TimeSpan(elapsedTicks);
            return elapsedSpan.Days + 1;
        }

        /// <summary>
        ///  CustomControls.components.ValiditySpecify.model.IExpiry type 
        ///  convert to 
        ///  RMD SDK Expiration type.
        /// Note: When set User preference expiration, the RELATIVE_EXPIRE type is different.
        /// if is for Nxl File, the RELATIVE_EXPIRE type like ABSOLUTE_EXPIRE type.
        /// </summary>
        /// <param name="expiry"></param>
        /// <param name="isUseForPreference"></param>
        /// <returns></returns>
        public static Expiration CustomCtrExpiry2SdkExpiry(IExpiry expiry, bool isUseForPreference = false)
        {
            Expiration expiration = new Expiration();

            int exType = expiry.GetOpetion();
            //Get current year,month,day.
            int year = DateTime.Now.Year;
            int month = DateTime.Now.Month;
            int day = DateTime.Now.Day;
            DateTime dateStart = new DateTime(year, month, day, 0, 0, 0);
            switch (exType)
            {
                case 0:
                    INeverExpire neverExpire = (INeverExpire)expiry;
                    expiration.type = ExpiryType.NEVER_EXPIRE;
                    break;
                case 1:
                    IRelative relative = (IRelative)expiry;
                    int years = relative.GetYears();
                    int months = relative.GetMonths();
                    int weeks = relative.GetWeeks();
                    int days = relative.GetDays();
                    Console.WriteLine("years:{0}-months:{1}-weeks:{2}-days{3}", years, months, weeks, days);

                    expiration.type = ExpiryType.RELATIVE_EXPIRE;
                    if (isUseForPreference)
                    {
                        expiration.Start = ((long)years << 32) + months;
                        expiration.End = ((long)weeks << 32) + days;
                    }
                    else
                    {
                        DateTime relativeEnd = dateStart.AddYears(years).AddMonths(months).AddDays(7 * weeks + days - 1).AddHours(23).AddMinutes(59).AddSeconds(59);
                        expiration.Start = 0;
                        expiration.End = DateTimeHelper.DateTimeToTimestamp(relativeEnd);
                    }
                    break;
                case 2:
                    IAbsolute absolute = (IAbsolute)expiry;
                    long endAbsDate = absolute.EndDate();
                    Console.WriteLine("absEndDate:{0}", endAbsDate);

                    expiration.type = ExpiryType.ABSOLUTE_EXPIRE;
                    expiration.Start = DateTimeHelper.DateTimeToTimestamp(dateStart);
                    expiration.End = endAbsDate;
                    break;
                case 3:
                    IRange range = (IRange)expiry;
                    long startDate = range.StartDate();
                    long endDate = range.EndDate();
                    Console.WriteLine("StartDate:{0},EndDate{1}", startDate, endDate);

                    expiration.type = ExpiryType.RANGE_EXPIRE;
                    expiration.Start = startDate;
                    expiration.End = endDate;
                    break;
            }
            return expiration;
        }

        /// <summary>
        /// SDK data type
        /// convert to
        /// CustomControls.componentPages.Preference.FolderItem type List
        /// <param name="rpmPaths"></param>
        /// <returns></returns>
        public static ObservableCollection<FolderItem> SdkRpmPaths2CusCtrFolderItem(List<string> rpmPaths)
        {
            ObservableCollection<FolderItem> folderItems = new ObservableCollection<FolderItem>();
            foreach (var item in rpmPaths)
            {
                FolderItem folderItem = new FolderItem()
                {
                    Icon = new BitmapImage(new Uri(@"/rmc/resources/icons/Folder.png", UriKind.Relative)),
                    FolderName = string.IsNullOrWhiteSpace(Path.GetFileName(item)) ? item : Path.GetFileName(item),
                    FolderPath = item
                };
                folderItems.Add(folderItem);
            }
            return folderItems;
        }

        /// <summary>
        /// RMD data type
        /// convert to
        /// CustomControl ExternalRepoItem type List
        /// </summary>
        /// <returns></returns>
        public static List<ExternalRepoItem> SdkExtnRepo2CustCtrExternalRepo()
        {
            List<ExternalRepoItem> testList = new List<ExternalRepoItem>();
            testList.Add(new ExternalRepoItem(new BitmapImage(new Uri("/rmc/resources/icons/externalrepo/googledrive-black.png", UriKind.Relative)),
                new BitmapImage(new Uri("/rmc/resources/icons/externalrepo/googledrive-white.png", UriKind.Relative)),
                GOOGLE_DRIVE));
            testList.Add(new ExternalRepoItem(new BitmapImage(new Uri("/rmc/resources/icons/externalrepo/box-black.png", UriKind.Relative)),
                new BitmapImage(new Uri("/rmc/resources/icons/externalrepo/box-white.png", UriKind.Relative)),
                BOX));
            testList.Add(new ExternalRepoItem(new BitmapImage(new Uri("/rmc/resources/icons/externalrepo/dropbox-black.png", UriKind.Relative)),
                new BitmapImage(new Uri("/rmc/resources/icons/externalrepo/dropbox-white.png", UriKind.Relative)),
                DROPBOX));
            testList.Add(new ExternalRepoItem(new BitmapImage(new Uri("/rmc/resources/icons/externalrepo/onedrive-black.png", UriKind.Relative)),
                new BitmapImage(new Uri("/rmc/resources/icons/externalrepo/onedrive-white.png", UriKind.Relative)),
                ONEDRIVE));
            //testList.Add(new ExternalRepoItem(new BitmapImage(new Uri("/rmc/resources/icons/externalrepo/sharepoint-black.png", UriKind.Relative)),
            //    new BitmapImage(new Uri("/rmc/resources/icons/externalrepo/sharepoint-white.png", UriKind.Relative)),
            //    SHAREPOINT, true));
            testList.Add(new ExternalRepoItem(new BitmapImage(new Uri("/rmc/resources/icons/externalrepo/sharepoint-black.png", UriKind.Relative)),
                new BitmapImage(new Uri("/rmc/resources/icons/externalrepo/sharepoint-white.png", UriKind.Relative)),
                SHAREPOINT_ONLINE, true));
            testList.Add(new ExternalRepoItem(new BitmapImage(new Uri("/rmc/resources/icons/externalrepo/sharepoint-black.png", UriKind.Relative)),
                new BitmapImage(new Uri("/rmc/resources/icons/externalrepo/sharepoint-white.png", UriKind.Relative)),
                SHAREPOINT_ONPREMISE, true));

            return testList;
        }

        /// <summary>
        /// CustomControl ExternalRepoItem type 
        /// convert to
        /// RMD ExternalRepoType type
        /// </summary>
        /// <returns></returns>
        public static ExternalRepoType CustCtrSelectedRepoItem2ExtnRepoType(ExternalRepoItem selectedItem)
        {
            ExternalRepoType externalRepoType = ExternalRepoType.GOOGLEDRIVE;
            if (selectedItem.Name.Equals(GOOGLE_DRIVE))
            {
                externalRepoType = ExternalRepoType.GOOGLEDRIVE;
            }
            else if (selectedItem.Name.Equals(SHAREPOINT))
            {
                externalRepoType = ExternalRepoType.SHAREPOINT;
            }
            else if (selectedItem.Name.Equals(SHAREPOINT_ONLINE))
            {
                externalRepoType = ExternalRepoType.SHAREPOINT_ONLINE;
            }
            else if (selectedItem.Name.Equals(SHAREPOINT_ONPREMISE))
            {
                externalRepoType = ExternalRepoType.SHAREPOINT_ONPREMISE;
            }
            else if (selectedItem.Name.Equals(BOX))
            {
                externalRepoType = ExternalRepoType.BOX;
            }
            else if (selectedItem.Name.Equals(DROPBOX))
            {
                externalRepoType = ExternalRepoType.DROPBOX;
            }
            else if (selectedItem.Name.Equals(ONEDRIVE))
            {
                externalRepoType = ExternalRepoType.ONEDRIVE;
            }
            return externalRepoType;
        }

    }
}
