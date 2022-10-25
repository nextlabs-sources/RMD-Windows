using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkydrmDesktop;
using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.featureProvider.MessageNotify;
using SkydrmLocal.rmc.common.component;
using SkydrmLocal.rmc.common.decryptor;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.Edit;
using SkydrmLocal.rmc.exception;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.featureProvider.common;
using SkydrmLocal.rmc.fileSystem;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.fileSystem.myvault;
using SkydrmLocal.rmc.fileSystem.project;
using SkydrmLocal.rmc.helper;
using SkydrmLocal.rmc.process;
using SkydrmLocal.rmc.removeProtection;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui.components.sortListView;
using SkydrmLocal.rmc.ui.utils;
using SkydrmLocal.rmc.ui.windows.mainWindow.helper;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent;
using SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.model;
using SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.viewModel;
using SkydrmLocal.rmc.ui.windows.mainWindow.view;
using SkydrmLocal.rmc.ui.windows.messageBox;
using static SkydrmLocal.rmc.fileSystem.project.ProjectRepo;
using static SkydrmLocal.rmc.ui.components.CustomSearchBox;
using static SkydrmLocal.rmc.ui.windows.CustomMessageBoxWindow;
using Helper = SkydrmLocal.rmc.Edit.Helper;
using SkydrmDesktop.rmc.fileSystem.workspace;
using SkydrmDesktop.rmc.ui.windows.mainWindow.viewModel;
using SkydrmDesktop.rmc.ui.windows.mainWindow.model;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.operation;
using SkydrmDesktop.rmc.fileSystem.mySpace;
using static SkydrmDesktop.rmc.fileSystem.workspace.WorkSpaceRepo;
using SkydrmDesktop.rmc.ui.windows.AddExternalRepoWindow.addRepo;
using SkydrmDesktop.rmc.featureProvider.externalDrive.RmsRepoMgr;
using SkydrmDesktop.rmc.featureProvider.externalDrive;
using SkydrmDesktop.rmc.fileSystem.externalRepo;
using SkydrmDesktop.rmc.fileSystem.externalDrive;
using SkydrmDesktop.rmc.fileSystem.externalDrive.externalBase;
using SkydrmDesktop.rmc.fileSystem.utils;
using SkydrmDesktop.rmc.featureProvider.externalDrive.errorHandler;
using SkydrmDesktop.rmc.common.helper;
using SkydrmDesktop.rmc.ui.windows.renameFileWindow.rename;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.view;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.operation.UpdateRecipient;
using SkydrmDesktop.rmc.fileSystem.sharedWorkspace;
using Microsoft.Win32;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.model;
using SkydrmDesktop.rmc.ui.windows.fileInformation.view;

namespace SkydrmLocal.rmc.ui.windows.mainWindow.viewModel
{
    public class ViewModelMainWindow : INotifyPropertyChanged
    {
        //******************************** For members ***************************************//

        #region // For other members
        private SkydrmApp App = (SkydrmApp)SkydrmApp.Current;
        private MainWindow win;

        private static readonly string HOME = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_Home");
        private static readonly string WORKSPACE = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_WorkSpace");
        private static readonly string MY_SPACE = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_MySpace");
        private static readonly string MY_VAULT = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_MyVault");
        private static readonly string MY_DRIVE = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_MyDrive");
        private static readonly string SHARE_WITH_ME = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_ShareWithMe");
        private static readonly string PROJECT = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_Project");
        private static readonly string SYSTEMBUCKET = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_SystemBucket");
        private static readonly string REPOSITORIES = CultureStringInfo.ApplicationFindResource("MainWin__TreeView_Repositories");

        // current selsected dest folder path.
        public CurrentSelectedSavePath CurrentSaveFilePath { get; set; }

        // Used to record the content that user is searching
        private string searchText = "";

        // For OpenFileDialog, Save selectFile path
        private string selectFilePath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        #endregion // For other members


        #region For IPC
        public IPCManager IPCManager { get; set; }

        #endregion // For IPC


        #region For comamnd
        public DelegateCommand DelegateCommand { get; set; }
        public DelegateCommand<SearchEventArgs> SearchCommand { get; set; }
        // Filter main window local files
        public DelegateCommand<SelectionChangedEventArgs> FilterFilesCommand { get; set; }

        public DelegateCommand ContextMenuCommand { get; set; }
        // For menu item command.
        public DelegateCommand MenuCommand { get; set; }
        #endregion // For comamnd


        #region For nxl files
        private INxlFile currentSelectedFile;
        public INxlFile CurrentSelectedFile
        {
            get
            {
                return currentSelectedFile;
            }
            set
            {
                currentSelectedFile = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentSelectedFile"));
            }
        }

        private List<INxlFile> oldAllFiles = new List<INxlFile>();
        // Will refresh UI when add or remove one entry because of ObservableCollection
        private ObservableCollection<INxlFile> nxlFileList = new ObservableCollection<INxlFile>();
        public ObservableCollection<INxlFile> NxlFileList
        {
            get { return nxlFileList; }
            set { nxlFileList = value; }
        }

        // Used for do search.
        private ObservableCollection<INxlFile> copyFileList = new ObservableCollection<INxlFile>();

        private bool issearch = false;
        public bool IsSearch
        {
            get
            {
                return issearch;
            }
            set
            {
                issearch = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsSearch"));
            }
        }
        // For ui Binding
        public ObservableCollection<INxlFile> CopyFileList
        {
            get { return copyFileList; }
            set { copyFileList = value; }
        }

        // For display Select Project UI
        private ObservableCollection<ProjectData> collectionProject = new ObservableCollection<ProjectData>();
        public ObservableCollection<ProjectData> CollectionProject { get => collectionProject; set => collectionProject = value; }

        #endregion // For nxl files


        #region For event handler
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion //  For event handler


        #region For network check
        // network status
        private bool isNetworkAvailable;
        public bool IsNetworkAvailable
        {
            get { return isNetworkAvailable; }
            set
            {
                isNetworkAvailable = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsNetworkAvailable"));
            }
        }
        #endregion //  For network check


        #region For Upload
        // The flag that start upload \ stop upload
        private bool isStartUpload = true; // for test
        public bool IsStartUpload
        {
            get { return isStartUpload; }
            set
            {
                isStartUpload = value;

                // trigger event
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsStartUpload"));
            }
        }
        #endregion // For Upload


        #region For UserName
        private string userName;
        public string UserName
        {
            get { return userName; }
            set { userName = value; }
        }
        private string GetUserName()
        {
            return App.Rmsdk.User.Name;
        }
        #endregion // For user name

        #region For Avatar
        private string avatarText;
        public string AvatarText
        {
            get { return avatarText; }
            set
            {
                avatarText = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AvatarText"));
            }
        }

        private string avatarTextColor;
        public string AvatarTextColor
        {
            get { return avatarTextColor; }
            set
            {
                avatarTextColor = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AvatarTextColor"));
            }
        }

        private string avatarBackground;
        public string AvatarBackground
        {
            get { return avatarBackground; }
            set
            {
                avatarBackground = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AvatarBackground"));
            }
        }
        #endregion // For Avatar

        #region UserStorageUsage
        private string usageDescribe;
        public string UsageDescribe
        {
            get { return usageDescribe; }
            set { usageDescribe = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("UsageDescribe")); }
        }
        private string driveUsage;
        public string DriveUsage
        {
            get { return driveUsage; }
            set { driveUsage = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DriveUsage")); }
        }
        private string vaultUsage;
        public string VaultUsage
        {
            get { return vaultUsage; }
            set { vaultUsage = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("VaultUsage")); }
        }
        private double drivePrgBarRatio;
        public double DrivePrgBarRatio
        {
            get { return drivePrgBarRatio; }
            set { drivePrgBarRatio = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DrivePrgBarRatio")); }
        }
        private double mySpacePrgBarValue;
        public double MySpacePrgBarValue
        {
            get { return mySpacePrgBarValue; }
            set { mySpacePrgBarValue = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MySpacePrgBarValue")); }
        }
        private double mySpacePrgBarMaximum;
        public double MySpacePrgBarMaximum
        {
            get { return mySpacePrgBarMaximum; }
            set { mySpacePrgBarMaximum = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MySpacePrgBarMaximum")); }
        }
        private int driveFileCount;
        public int DriveFileCount
        {
            get { return driveFileCount; }
            set { driveFileCount = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DriveFileCount")); }
        }
        private int vaultFileCount;
        public int VaultFileCount
        {
            get { return vaultFileCount; }
            set { vaultFileCount = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("VaultFileCount")); }
        }
        private int sharedWithMeFileCount;
        public int SharedWithMeFileCount
        {
            get { return sharedWithMeFileCount; }
            set { sharedWithMeFileCount = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SharedWithMeFileCount")); }
        }
        private int mySpaceFileCount;
        public int MySpaceFileCount
        {
            get { return mySpaceFileCount; }
            set { mySpaceFileCount = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MySpaceFileCount")); }
        }
        private int workSpaceFileCount;
        public int WorkSpaceFileCount
        {
            get { return workSpaceFileCount; }
            set { workSpaceFileCount = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("WorkSpaceFileCount")); }
        }
        private void GetUserStorageSpace()
        {
            string result = "{0} of {1} used";
            long usageSize = 0;
            long totalSize = 0;
            long vaultUsage = 0;
            long vaultQuota = 0;

            App.Rmsdk.User.GetMyDriveInfo(ref usageSize, ref totalSize, ref vaultUsage, ref vaultQuota);

            long driveUsage = usageSize - vaultUsage;
            
            string usage = FileSizeHelper.GetSizeString(usageSize);
            string vaultQ = FileSizeHelper.GetSizeString(vaultQuota);

            UsageDescribe = string.Format(result, usage, vaultQ);
            DriveUsage= FileSizeHelper.GetSizeString(driveUsage);
            VaultUsage= FileSizeHelper.GetSizeString(vaultUsage);
            DrivePrgBarRatio = (double)driveUsage / usageSize;
            MySpacePrgBarValue = usageSize;
            MySpacePrgBarMaximum = vaultQuota;
        }
        #endregion

        #region For TreeView model

        // Record current user selected treeview item view model(root\projcet\folder view model),
        // used for change corresponding treeview item when listview item(folder) changes during refresh.
        // Note: must cancel the selection item when user click filter button.
        private TreeViewItemViewModel currentTreeViewItem;

        public RepoViewModel RepoViewModel { get; set; }
        #endregion // For treeView model


        #region For filter
        private SolidColorBrush colorGreen = new SolidColorBrush(Color.FromRgb(0XC6, 0XEE, 0XDC));
        private SolidColorBrush colorTransparent = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        // Filter set
        private IList<INxlFile> shareWithMeFiles = new List<INxlFile>();
        private IList<INxlFile> outboxFiles = new List<INxlFile>();
        private IList<INxlFile> offlineFiles = new List<INxlFile>();
        #endregion // For filter


        #region For Flags
        // Used to falg if trigger refresh when Parse Save Path, which will results in TreeViewItemChanged event execute.
        private bool IsTriggerRefresh = true;

        // Flag that indicates if is refreshing after upload or remove.
        private bool IsRefreshingAfterUpload = false;

        // For init to load data, binding UI
        private bool isInitializing = true;
        public bool IsInitializing
        {
            get
            {
                return isInitializing;
            }
            set
            {
                isInitializing = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsInitializing"));
            }
        }

        // Flag that indicates data is loading.(Now not used, resverved.)
        private bool isDataLoading = false;
        public bool IsDataLoading
        {
            get
            {
                return isDataLoading;
            }
            set
            {
                isDataLoading = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsDataLoading"));
            }
        }


        //If MyVault and Project are not null,  display treeview 、offlineButton、 outboxButton
        private bool isDisplayTreeview = false;
        public bool IsDisplayTreeview
        {
            get
            {
                return isDisplayTreeview;
            }
            set
            {
                isDisplayTreeview = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsDisplayTreeview"));
            }
        }

        private bool isSaasRouter;
        public bool IsSaasRouter
        {
            get
            {
                return isSaasRouter;
            }
            set
            {
                isSaasRouter = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsSaasRouter"));
            }
        }
        #endregion // For flags

        // Default tab item is 'AllFiles'
        private EnumMainWinTabItems currentTabItem = EnumMainWinTabItems.ALL_FILES;
        private UploadCallback uploadCallback;

        #region For working area
        private CurrentWorkingAreaInfo currentWorkingAreaInfo = new CurrentWorkingAreaInfo(EnumCurrentWorkingArea.FILTERS_OUTBOX, "");
        public CurrentWorkingAreaInfo CurrentWorkingAreaInfo
        {
            get { return currentWorkingAreaInfo; }
            set
            {
                currentWorkingAreaInfo = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentWorkingAreaInfo"));
            }
        }

        // Record current repository working directory(repoId + pathId), should set as empty string for Filter area.
        // which used to record whether the user switches the current working directory when performing the sync(refresh) operation, 
        // if yes, won't update listview after sync complete.
        private string currentWorkingDirectoryFlag;
        public string CurrentWorkingDirectoryFlag
        {
            get { return currentWorkingDirectoryFlag; }
            set
            {
                currentWorkingDirectoryFlag = value;
            }
        }

        private EnumCurrentWorkingArea currentWorkingArea = EnumCurrentWorkingArea.FILTERS_OUTBOX;
        public EnumCurrentWorkingArea CurrentWorkingArea
        {
            get
            { return currentWorkingArea; }
            set
            {
                currentWorkingArea = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentWorkingArea"));
            }
        }
        #endregion // For working area


        #region For repos
        // Current working repo
        private IFileRepo currentWorkRepo;
        public List<IFileRepo> FileRepos { get; set; }

        private WorkSpaceRepo workSpaceRepo;
        private MyVaultRepo myVaultRepo;
        private MyDriveRepo myDriveRepo;
        public  ProjectRepo projectRepo;
        
        // Used to store extrernal repository, including personal account, business account, application account(shared workspace).
        private ObservableCollection<IFileRepo> externalRepos = new ObservableCollection<IFileRepo>();
        /// <summary>
        /// UI will binding this ExternalRepos property to display 'Manage repositories'
        /// </summary>
        public ObservableCollection<IFileRepo> ExternalRepos
        {
            get { return externalRepos; }
            set { externalRepos = value; }
        }
        #endregion // For repos

        //******************************** For methods ****************************************//

        #region For Init
        public ViewModelMainWindow(MainWindow window)
        {
            this.win = window;

            // register delegate
            App.Log.Info("register delegate");
            DelegateCommand = new DelegateCommand(CommandDispatcher);
            SearchCommand = new DelegateCommand<SearchEventArgs>(DoSearch);
            FilterFilesCommand = new DelegateCommand<SelectionChangedEventArgs>(DoFilterLocalFiles);
            ContextMenuCommand = new DelegateCommand(ContextMenuCmd);
            MenuCommand = new DelegateCommand(MenuCommand_Executed, MenuCommand_CanExecute);

            //get user name
            App.Log.Info("get user name");
            UserName = GetUserName();
            AvatarText = NameConvertHelper.ConvertNameToAvatarText(userName, " ");
            AvatarBackground = NameConvertHelper.SelectionBackgroundColor(userName);
            AvatarTextColor = NameConvertHelper.SelectionTextColor(userName);

            // init "IsStartUpload" flag.
            App.Log.Info("init 'IsStartUpload' flag.");
            InitStartUploadFlag();

            // instantiat repo
            App.Log.Info("init rms repo.");
            InitRepo();

            // init data
            App.Log.Info("init data.");
            InitData();

            App.Log.Info("init project listView group name.");
            InitProjectListViewGroup();

            App.Log.Info("init event notify.");
            InitEventNotify();
        }



        private void InitEventNotify()
        {
            // register update status notification & upload complete callback.
            App.Log.Info("register update status notification & upload complete callback.");
            UploadManagerEx.GetInstance().NotifyChangeStatus += SyncCurrentListViewFileStatus;
            UploadManagerEx.GetInstance().SyncModifiedFileCallback += UpdateModifiedFileAndItemNodeUi;
            UploadManagerEx.GetInstance().SetCallback(UploadComplete);

            // Register Event
            App.UserNameUpdated += () =>
            {
                UserName = GetUserName();
                this.win.user_name.Text = UserName;
                AvatarText = NameConvertHelper.ConvertNameToAvatarText(userName, " ");
                AvatarBackground = NameConvertHelper.SelectionBackgroundColor(userName);
                AvatarTextColor = NameConvertHelper.SelectionTextColor(userName);
            };


            App.MyVaultFileOrSharedWithMeLowLevelUpdated += () =>
            {
                App.Dispatcher.Invoke(() =>
                {
                    DoRefresh();
                });
            };

            // Register FileList refresh notification
            App.NotifyRefreshFileListView += UpdateFileListWhenRefreshTreeview;

            // Register project list refresh notification.
            App.NotifyRefreshProjectListView += UpdateProjectListView;

            // Register externalRepo list refresh notification.
            App.NotifyRefreshExternalRepoListView += UpdateExternalRepoListView;

            App.MyVaultQuataUpdated += () => { GetUserStorageSpace(); };

        }

        private void InitRepo()
        {
            RepoViewModel = new RepoViewModel();
            workSpaceRepo = new WorkSpaceRepo();
            myVaultRepo = new MyVaultRepo();
            myDriveRepo = new MyDriveRepo();
            projectRepo = new ProjectRepo();

            if (SkydrmApp.Singleton.IsEnableExternalRepo)
            {
                // Get external repos from local firstly if have.
                List<IRmsRepo> repos = App.RmsRepoMgr.ListRepositories();
                AddExternalRepositories(repos);

                // Request access token from server by async task. 
                AsyncToken(repos);
            }
        }

        private void AsyncToken(List<IRmsRepo> repos)
        {
            if (repos.Count == 0)
                return;

            AsyncHelper.RunAsync(() => {
                foreach(var one in repos)
                {
                    one.Token = App.RmsRepoMgr.GetAccessToken(one.RepoId);
                }
            });
        }

        private void InitExternalRepoData()
        {
            // Maybe first loading, need to sync from server for this case. 
            // Note: will try to init 'externalRepos' from local db in 'InitRepo' method.
            if (externalRepos.Count == 0)
            {
                AsyncHelper.RunAsync(() =>
                {
                    bool bSucceed = true;

                    List<IRmsRepo> ret = new List<IRmsRepo>();
                    try
                    {
                        App.Log.Info("Sync SyncRepositories in InitData");
                        // sync repos from rms
                        ret = SkydrmApp.Singleton.RmsRepoMgr.SyncRepositories();
                    }
                    catch (Exception e)
                    {
                        App.Log.Error("Invoke SyncRepositories failed", e);

                        bSucceed = false;
                    }

                    return new RefreshRepositoriesInfo(bSucceed, ret);
                },
                (rt) =>
                {
                    RefreshRepositoriesInfo rtValue = (RefreshRepositoriesInfo)rt;
                    if (!rtValue.IsSuc)
                    {
                        return;
                    }

                    // Add repo
                    AddExternalRepositories(rtValue.Results);

                    foreach (var one in externalRepos)
                    {
                        if (one is ExternalRepo)
                        {
                            // Acquire access token 
                            var token = App.RmsRepoMgr.GetAccessToken(one.RepoId);
                            one.UpdateToken(token);

                            // Sync data
                            (one as ExternalRepo).SyncAllFiles((bool bS, List<INxlFile> r) =>
                            {
                                LoadData();
                            });
                        }
                        else if(one is SharedWorkspaceRepo)
                        {
                            // Sync data
                            App.Log.Info("Sync SharedWorkspace repo files in InitData");
                            (one as SharedWorkspaceRepo).SyncAllFiles((bool bS, List<INxlFile> r) =>
                            {
                                App.Log.Info("Sync SharedWorkspace repo completed in InitData");
                                LoadData();
                            });
                        }

                    }
                });
            }

            // Get from local db.
            else
            {
                foreach (var one in externalRepos)
                {
                    // one.GetFilePool();
                    if(one is SharedWorkspaceRepo)
                    {
                        var swRepo = one as SharedWorkspaceRepo;
                        if(swRepo.GetAllData().Count == 0)
                        {
                            App.Log.Info("Sync SharedWorkspace repo files in InitData");
                            swRepo.SyncAllFiles((bool bS, List<INxlFile> r) =>
                            {
                                App.Log.Info("Sync SharedWorkspace repo completed in InitData");
                                LoadData();
                            });
                        }
                    }
                }
            }
        }

        private void InitData()
        {
            // load external repo data
            if (App.IsEnableExternalRepo)
            {
               InitExternalRepoData();
            }

            // myvault
            if (myVaultRepo.GetAllData().Count == 0)
            {
                App.Log.Info("Sync myvault files in InitData");
                myVaultRepo.SyncFiles((bool bSuccess, IList<INxlFile> result, string reserved) =>
                {
                    App.Log.Info("Sync myvault completed in InitData");
                    LoadData();
                });
            }
            // mydrive
            if (myDriveRepo.GetAllData().Count == 0)
            {
                App.Log.Info("Sync myDrive files in InitData");
                myDriveRepo.SyncAllData((bool bSuc, IList<INxlFile> results) => {
                    App.Log.Info("Sync mydrive completed in InitData");
                    LoadData();
                });
            }

            // WorkSpace
            if (workSpaceRepo.GetAllData().Count == 0)
            {
                App.Log.Info("Sync workSpace files in InitData");
                workSpaceRepo.SyncAllFiles((bool bSuccess, IList<INxlFile> results) =>
                {
                    App.Log.Info("Sync workSpace completed in InitData");
                    LoadData();
                });
            }

            // project
            if (projectRepo.GetAllData().Count == 0)
            {
                SkydrmApp.Singleton.Log.Info("Sync project files in InitData");
                projectRepo.SyncAllRemoteData((bool bSuccess, IList<ProjectData> result) =>
                {
                    App.Log.Info("Sync project completed in InitData");

                    LoadData();
                });
            }

            LoadData();
        }

        private void AddExternalRepositories(List<IRmsRepo> repos)
        {
            externalRepos.Clear();
            if (repos.Count == 0)
            {
                return;
            }

            foreach (var one in repos)
            {
                // Filter out the local drive
                if (one.Type == ExternalRepoType.LOCAL_DRIVE)
                {
                    continue;
                }

                // Now don't support this class
                if(one.ProviderClass == FileSysConstant.REPO_CLASS_PERSONAL)
                {
                    /*
                    var drive = ExternalRepoFactory.Create(one);
                    if (drive != null)
                    {
                        externalRepos.Add(drive);
                    } */

                    continue;
                }
                else if(one.ProviderClass == FileSysConstant.REPO_CLASS_APPLICATION)
                {
                    var repo = SharedWorkspaceRepo.Create(one);
                    externalRepos.Add(repo);
                }

            }
        }

        private void LoadData()
        {
            try
            {
                if (!projectRepo.IsLoading && !myVaultRepo.IsLoading && !myDriveRepo.IsLoading
                      && !workSpaceRepo.IsLoading && !ViewModelHelper.IsExternalRepoLoading(externalRepos))
                {
                    App.Log.Info("Load data.");

                    IsInitializing = false;
                    InitTreeView();

                    // defult choose OutBoxBtn
                    OutBoxClick();

                    InitMySpaceFileCount();

                    InitWorkSpaceFileCount();

                    InitAndTryUploadPendingFile();
                }
            }
            catch (Exception e)
            {
                SkydrmApp.Singleton.Log.Error("Error in LoadData", e);
            }

        }

        private void InitMySpaceFileCount()
        {
            int dvFileCount = 0;
            GetFilesCountRecursively(myDriveRepo.GetFilePool(), ref dvFileCount);
            DriveFileCount = dvFileCount;
            VaultFileCount = myVaultRepo.GetFilePool().Count;
            SharedWithMeFileCount = myVaultRepo.GetSharedWithMeFiles().Count;
            MySpaceFileCount = DriveFileCount + VaultFileCount + SharedWithMeFileCount;
        }

        private void InitWorkSpaceFileCount()
        {
            int workSpFileCount = 0;
            GetFilesCountRecursively(workSpaceRepo.GetFilePool(), ref workSpFileCount);
            WorkSpaceFileCount = workSpFileCount;
        }

        private void GetFilesCountRecursively(IList<INxlFile> nxlFiles, ref int count)
        {
            foreach (var nxl in nxlFiles)
            {
                if (nxl.IsFolder)
                {
                    NxlFolder folder = nxl as NxlFolder;
                    GetFilesCountRecursively(folder.Children, ref count);
                }
                else
                {
                    count++;
                }
            }
        }

        // get all pending files to add into queue 
        private void InitAndTryUploadPendingFile()
        {
            // get all pending files to add into queue, then  try to upload.
            // Note: may include uploading file(if current file is uploading, should not add the queue.)
            IList<INxlFile> pfiles = new List<INxlFile>();
            GetAllPendingFiles(ref pfiles);
            UploadManagerEx.GetInstance().ClearWaitingQueue();
            UploadManagerEx.GetInstance().SubmitToWaitingQueue(pfiles);

            // Get all edited files(not yet updated to rms), and also need to submit to upload queue
            UploadManagerEx.GetInstance().SubmitToWaitingQueue(GetAllEditedFiles());

            // upload local file if existed.
            TryToUpload();

            // set default sort for outbox, which is our default current working area.
            DefaultSort();
        }

        private void InitTreeView()
        {
            App.Log.Info("Init tree view");

            FileRepos = new List<IFileRepo>();

            // if router is Saas, not have workSpace

            bool router= App.IsSaasRouter();
            bool workspace = SkydrmApp.Singleton.Rmsdk.User.IsEnabledWorkSpace();

            IsSaasRouter = App.IsSaasRouter() || ! workspace;

            if (!router&& workspace)
            {
                FileRepos.Add(workSpaceRepo);
            }

            FileRepos.Add(myDriveRepo);
            FileRepos.Add(myVaultRepo);
            FileRepos.Add(projectRepo);

            RepoViewModel.Start(GetFileRepos());

            // Whether UI is displayed or not
            IsDisplayTreeview = RepoViewModel.RootVMList.Count > 0 ? true : false;

            // Note: must set DataContext after setting data source.
            //win.UserControl_TreeView.DataContext = RepoViewModel;
        }

        public List<IFileRepo> GetFileRepos()
        {
            List<IFileRepo> repos = new List<IFileRepo>();
            try
            {
                foreach (var one in FileRepos)
                {
                    repos.Add(one);
                }
                foreach (var one in externalRepos)
                {
                    repos.Add(one);
                }
            }
            catch (Exception e)
            {
                App.Log.Error(e);
            }
            
            return repos;
        }

        private void InitProjectListViewGroup()
        {
            // set Project ListView GroupName
            ICollectionView cv = CollectionViewSource.GetDefaultView(CollectionProject);
            cv.GroupDescriptions.Add(new PropertyGroupDescription("ProjectInfo.BOwner"));
        }

        /// <summary>
        ///  Init IsStartUpload flag.
        /// </summary>
        private void InitStartUploadFlag()
        {
            // get from register.
            //App.Config.GetPreferences();

            if (!App.User.StartUpload)
            {
                IsStartUpload = false;
            }
            else
            {
                IsStartUpload = true;
            }
        }

        /// <summary>
        /// Init viewer process. 
        /// </summary>
        public void InitIPC()
        {
            IPCManager = new IPCManager(new Action<int, int, string>(ReceiveData));
        }

        #endregion // For Init


        #region For event handler

        public void TreeViewItemSelectChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // Display "All Files" table in default.
            this.win.TabCtl.SelectedIndex = 0;

            // fix bug 50718
            win.SearchBox.TbxInput.Text = "";

            TreeViewItemViewModel treeViewItem = e.NewValue as TreeViewItemViewModel;
            if (treeViewItem == null) // when user click filter button, we set treeview IsSelect as false.
            {
                return;
            }

            currentTreeViewItem = treeViewItem;

            IList<INxlFile> fileList = new List<INxlFile>();
            try
            {
                if (treeViewItem is RootViewModel)
                {
                    OnRootViewModel(treeViewItem, ref fileList);
                }
                else if (treeViewItem is ProjectViewModel)
                {
                    OnProjectViewModel(treeViewItem, ref fileList);
                }
                else if (treeViewItem is FolderViewModel)
                {
                    OnFolderViewModel(treeViewItem, ref fileList);
                }
            }
            catch (Exception ex)
            {
                App.Log.Warn("Exception in TreeViewItemSelectChanged," + ex, ex);
            }

            ResetCurrentSelectedFile();

            SetCurSavePath();

            // Set offlineBtn and OutboxBtn bg color
            ChangeFilterBtnBackground();

            if (IsTriggerRefresh)
            {
                // set listView item source.
                SetListView(fileList);  // -- Actually here can ignore if set it in DoRefresh() function.

                // async refresh at the same time.
                DoRefresh();
            }

        }

        private void OnRootViewModel(TreeViewItemViewModel treeViewItem, ref IList<INxlFile> fileList)
        {
            // Initialize as null before selection.
            currentWorkRepo = null;

            RootViewModel rootView = treeViewItem as RootViewModel;
            if (rootView.RootType.Equals(HOME, StringComparison.CurrentCultureIgnoreCase))
            {
                CurrentWorkingArea = EnumCurrentWorkingArea.HOME;

                fileList = new List<INxlFile>();
                CurrentWorkingAreaInfo = new CurrentWorkingAreaInfo(CurrentWorkingArea, HOME);
            }
            else if (rootView.RootType.Equals(WORKSPACE, StringComparison.CurrentCultureIgnoreCase))
            {
                CurrentWorkingArea = EnumCurrentWorkingArea.WORKSPACE;
                currentWorkRepo = workSpaceRepo;
                workSpaceRepo.CurrentWorkingFolder = new NxlFolder(); 

                fileList = rootView.Root.RepoFiles;
                CurrentWorkingAreaInfo = new CurrentWorkingAreaInfo(CurrentWorkingArea, WORKSPACE);
            }
            else if (rootView.RootType.Equals(MY_SPACE, StringComparison.CurrentCultureIgnoreCase))
            {
                CurrentWorkingArea = EnumCurrentWorkingArea.MYSPACE;

                fileList = new List<INxlFile>();
                CurrentWorkingAreaInfo = new CurrentWorkingAreaInfo(CurrentWorkingArea, MY_SPACE);
            }
            else if (rootView.RootType.Equals(MY_VAULT, StringComparison.CurrentCultureIgnoreCase))
            {
                CurrentWorkingArea = EnumCurrentWorkingArea.MYVAULT;
                currentWorkRepo = myVaultRepo;

                fileList = rootView.Root.RepoFiles; // all myVault files. 

                CurrentWorkingAreaInfo = new CurrentWorkingAreaInfo(CurrentWorkingArea, MY_VAULT);
            }
            else if (rootView.RootType.Equals(MY_DRIVE, StringComparison.CurrentCultureIgnoreCase))
            {
                CurrentWorkingArea = EnumCurrentWorkingArea.MYDRIVE;
                currentWorkRepo = myDriveRepo;
                myDriveRepo.CurrentWorkingFolder = new NxlFolder(); // root folder.

                fileList = rootView.Root.RepoFiles;
                CurrentWorkingAreaInfo = new CurrentWorkingAreaInfo(CurrentWorkingArea, MY_DRIVE);
            }
            else if (rootView.RootType.Equals(SHARE_WITH_ME, StringComparison.CurrentCultureIgnoreCase))
            {
                CurrentWorkingArea = EnumCurrentWorkingArea.SHARED_WITH_ME;

                fileList = rootView.Root.RepoFiles; // all ShareWithMe files. 

                CurrentWorkingAreaInfo = new CurrentWorkingAreaInfo(CurrentWorkingArea, SHARE_WITH_ME);
            }
            else if (rootView.RootType.Equals(PROJECT, StringComparison.CurrentCultureIgnoreCase))
            {
                CurrentWorkingArea = EnumCurrentWorkingArea.PROJECT;

                // Set ListBox instead of ListView
                LoadProjectPageData((List<ProjectData>)rootView.GetProjectData());
                fileList = new List<INxlFile>();

                CurrentWorkingAreaInfo = new CurrentWorkingAreaInfo(CurrentWorkingArea, PROJECT);
            }
            // External repository
            else if (rootView.RootType.Equals(REPOSITORIES, StringComparison.CurrentCultureIgnoreCase))
            {
                CurrentWorkingArea = EnumCurrentWorkingArea.EXTERNAL_REPO;

                fileList = new List<INxlFile>();
                CurrentWorkingAreaInfo = new CurrentWorkingAreaInfo(CurrentWorkingArea, REPOSITORIES);
            }
            else if(ViewModelHelper.IsExternalRepo(rootView.RootType))
            {
                CurrentWorkingArea = EnumCurrentWorkingArea.EXTERNAL_REPO_ROOT;
                currentWorkRepo = ViewModelHelper.GetExternalRepo(rootView.RepoId, externalRepos);
                currentWorkRepo.CurrentWorkingFolder = new NxlFolder();

                fileList = rootView.Root.RepoFiles;
                CurrentWorkingAreaInfo = new CurrentWorkingAreaInfo(rootView.RootType, rootView.RootName, rootView.RootClassType);
            }

            // Set flag, used to distinguish the working directory refresh between different repo roots.
            if (currentWorkRepo != null)
            {
                // The 'RepoId' is empty string if don't exist.
                CurrentWorkingDirectoryFlag = currentWorkRepo.RepoId + "/";
            }

        }

        private void OnProjectViewModel(TreeViewItemViewModel treeViewItem, ref IList<INxlFile> fileList)
        {
            ProjectViewModel projectView = treeViewItem as ProjectViewModel;
            fileList = projectView.Project.FileNodes; // project all files(doc and folder)

            CurrentWorkingArea = EnumCurrentWorkingArea.PROJECT_ROOT;
            currentWorkRepo = projectRepo;

            // project section.
            projectRepo.CurrentWorkingProject = projectView.Project;
            projectRepo.CurrentWorkingFolder = new NxlFolder(); // root folder

            // Set flag, used to distinguish the working directory refresh between different project.
            CurrentWorkingDirectoryFlag = projectRepo.RepoId + "/";

            CurrentWorkingAreaInfo = new CurrentWorkingAreaInfo(CurrentWorkingArea, projectRepo.CurrentWorkingProject.ProjectInfo.DisplayName,
                projectRepo.CurrentWorkingProject.ProjectInfo.Description, projectRepo.CurrentWorkingProject.ProjectInfo.BOwner);
        }

        private void OnFolderViewModel(TreeViewItemViewModel treeViewItem, ref IList<INxlFile> fileList)
        {
            FolderViewModel folderView = treeViewItem as FolderViewModel;
            fileList = folderView.NxlFolder.Children;

            if (folderView.NxlFolder.FileRepo == EnumFileRepo.REPO_PROJECT)
            {
                CurrentWorkingArea = EnumCurrentWorkingArea.PROJECT_ROOT;
                currentWorkRepo = projectRepo;

                // project section.
                projectRepo.CurrentWorkingProject = ViewModelHelper.FindProject(folderView);
                projectRepo.CurrentWorkingFolder = folderView.NxlFolder;

                CurrentWorkingAreaInfo = new CurrentWorkingAreaInfo(CurrentWorkingArea, projectRepo.CurrentWorkingProject.ProjectInfo.DisplayName,
                projectRepo.CurrentWorkingProject.ProjectInfo.Description, projectRepo.CurrentWorkingProject.ProjectInfo.BOwner);
            }
            else if (folderView.NxlFolder.FileRepo == EnumFileRepo.REPO_WORKSPACE)
            {
                CurrentWorkingArea = EnumCurrentWorkingArea.WORKSPACE;
                currentWorkRepo = workSpaceRepo;
                // workspace section
                workSpaceRepo.CurrentWorkingFolder = folderView.NxlFolder;

                CurrentWorkingAreaInfo = new CurrentWorkingAreaInfo(CurrentWorkingArea, WORKSPACE);
            }
            else if (folderView.NxlFolder.FileRepo == EnumFileRepo.REPO_MYDRIVE)
            {
                CurrentWorkingArea = EnumCurrentWorkingArea.MYDRIVE;
                currentWorkRepo = myDriveRepo;
                // workspace section
                myDriveRepo.CurrentWorkingFolder = folderView.NxlFolder;

                CurrentWorkingAreaInfo = new CurrentWorkingAreaInfo(CurrentWorkingArea, MY_DRIVE);
            }
            else if(folderView.NxlFolder.FileRepo == EnumFileRepo.REPO_EXTERNAL_DRIVE)
            {
                CurrentWorkingArea = EnumCurrentWorkingArea.EXTERNAL_REPO_ROOT;
                // Find the root view model
                RootViewModel rvm = ViewModelHelper.FindRootViewModel(folderView);
                if(rvm != null)
                {
                    currentWorkRepo = ViewModelHelper.GetExternalRepo(rvm.RepoId, externalRepos);
                    if(currentWorkRepo != null)
                    {
                        currentWorkRepo.CurrentWorkingFolder = folderView.NxlFolder;
                    }

                    CurrentWorkingAreaInfo = new CurrentWorkingAreaInfo(rvm.RootType, rvm.RootName, rvm.RootClassType);
                }           
            }

            // Set flag, used to distinguish the working directory refresh between different repo folders.
            if (currentWorkRepo != null)
            {
                CurrentWorkingDirectoryFlag = currentWorkRepo.RepoId + folderView.NxlFolder.PathId;
            }

        }

        public void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TabControl tc = (TabControl)sender;
            currentTabItem = (EnumMainWinTabItems)tc.SelectedIndex;
            switch (currentTabItem)
            {
                case EnumMainWinTabItems.ALL_FILES:
                    // Firstly, display original data, then do refresh
                    SetListView(oldAllFiles);
                    RefreshTabItem_AllFiles();
                    break;
                case EnumMainWinTabItems.SHARED_WITH_ME:
                    // Save 'All Files' first, in order to restore when switch into 'All Files' tab.
                    DoSaveAllFiles(e.RemovedItems);
                    RefreshTabItem_SharedWithMe();
                    break;
                case EnumMainWinTabItems.SHARED_BY_ME:

                    DoSaveAllFiles(e.RemovedItems);
                    RefreshTabItem_SharedByMe();
                    break;
            }
        }

        private void DoSaveAllFiles(System.Collections.IList list)
        {
            if (list.Count > 0)
            {
                foreach (var i in list)
                {
                    TabItem ti = i as TabItem;
                    if (ti != null && ti.Tag.ToString() == "AllFiles")
                    {
                        // Save all files node
                        SaveOldAllFiles();

                        break;
                    }
                }
            }
        }

        public void UserControl_TreeView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
            //set routedEvent Type
            eventArg.RoutedEvent = UIElement.MouseWheelEvent;
            eventArg.Source = sender;
            (sender as UserControl).RaiseEvent(eventArg);
        }

        // Fix bug 53796
        public void TreeViewItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var treeViewItem = VisualUpwardSearch<TreeViewItem>(e.OriginalSource as DependencyObject) as TreeViewItem;
            if (treeViewItem != null)
            {
                treeViewItem.Focus();
                e.Handled = true;
            }
        }
        private DependencyObject VisualUpwardSearch<T>(DependencyObject source)
        {
            while (source != null && source.GetType() != typeof(T))
                source = VisualTreeHelper.GetParent(source);

            return source;
        }

        public void ListViewItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            App.Log.Info("fileListViewItem_DoubleClick -->");

            try
            {
                if (sender is ListViewItem)
                {
                    ListViewItem selectedItem = sender as ListViewItem;
                    CurrentSelectedFile = (INxlFile)selectedItem.Content;

                    if (CurrentSelectedFile.IsFolder)
                    {
                        DoubleClickFolder();
                    }
                    else
                    {
                        DoubleClickFile();
                    }
                }
            }
            catch (Exception ex)
            {
                App.Log.Warn("Exception in ListViewItem_DoubleClick," + ex, ex);
            }

        }

        private void DoubleClickFolder()
        {
            var folder = (NxlFolder)CurrentSelectedFile;
            CurrentWorkingDirectoryFlag = currentWorkRepo?.RepoId + CurrentSelectedFile.PathId;

            switch (folder.FileRepo)
            {
                case EnumFileRepo.REPO_PROJECT:
                    CurrentWorkingArea = EnumCurrentWorkingArea.PROJECT_ROOT;
                    projectRepo.CurrentWorkingFolder = (NxlFolder)CurrentSelectedFile;
                    break;
                case EnumFileRepo.REPO_WORKSPACE:
                    CurrentWorkingArea = EnumCurrentWorkingArea.WORKSPACE;
                    workSpaceRepo.CurrentWorkingFolder = (NxlFolder)CurrentSelectedFile;
                    break;
                case EnumFileRepo.REPO_MYDRIVE:
                    CurrentWorkingArea = EnumCurrentWorkingArea.MYDRIVE;
                    myDriveRepo.CurrentWorkingFolder = (NxlFolder)CurrentSelectedFile;
                    break;
                case EnumFileRepo.REPO_EXTERNAL_DRIVE:
                    CurrentWorkingArea = EnumCurrentWorkingArea.EXTERNAL_REPO_ROOT;
                    currentWorkRepo.CurrentWorkingFolder = (NxlFolder)CurrentSelectedFile;

                    break;
                default:
                    break;
            }

            SetCurSavePath();

            IList<INxlFile> children = ((NxlFolder)CurrentSelectedFile).Children;

            // First, display original data.
            SetListView(children);

            // default sort
            DefaultSort();

            TreeviewFolderNavigate(currentTreeViewItem.Children, currentWorkRepo.CurrentWorkingFolder.PathId);
        }

        // Cached file(Leave a copy file) also looked as offline file.
        public bool IsOffline()
        {
            return CurrentSelectedFile.FileStatus == EnumNxlFileStatus.AvailableOffline
                    || CurrentSelectedFile.FileStatus == EnumNxlFileStatus.CachedFile;
        }

        private void InnerViewFile(INxlFile nxlFile)
        {
            if (nxlFile == null || nxlFile.FileStatus == EnumNxlFileStatus.Uploading ||
                 nxlFile.FileStatus == EnumNxlFileStatus.Downloading)
            {
                return;
            }

            // View the local file.
            if (nxlFile.Location == EnumFileLocation.Local)
            {
                // For project & workspace offline file, need to check file version before view  .
                if (ViewModelHelper.IsNeedCheckVersion(nxlFile) && IsOffline())
                {
                    ViewOfflineFile(nxlFile);
                }
                else
                {
                    StartViewerProcess(nxlFile);
                }

            }

            // View the online file.
            else
            {
                // If network is offline, forbid to view online file.
                if (IsNetworkAvailable)
                {
                    OnlineView(nxlFile);
                }
            }
        }

        private void DoubleClickFile()
        {
            InnerViewFile(CurrentSelectedFile);
        }

        private void OnlineView(INxlFile file)
        {
            // Filter out unsupported file type first.
            var fileType = FileTypeHelper.GetFileTypeByExtension(file.Name);
            //if (fileType == FileTypeHelper.EnumFileType.FILE_TYPE_NOT_SUPPORT)
            //{

            //    App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Notify_PopBubble_UnSupport_File"), false, file.Name, MsgNotifyOperation.VIEW,
            //        EnumMsgNotifyIcon.Online);

            //    return;
            //}

            // Normal file.
            if (!file.IsNxlFile)
            {
                InnerOnlineView(file);
                return;
            }

            // Nxl file: download partial for check rights
            currentWorkRepo.DownloadFile(file, true, (bool result) =>
            {
                // sanity check
                if (!result)
                {
                    // fix Bug 62968 - Pops 2 notification when open file without rights
                    //App.ShowBalloonTip(CultureStringInfo.Notify_PopBubble_Download_Failed, false, file.Name, MsgNotifyOperation.VIEW,
                    //    EnumMsgNotifyIcon.Online);

                    // fix Bug 66586 - No pop bubble when double deleted Repository nxl file before refresh repository list
                    App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Notify_PopBubble_Download_Failed"), false, file.Name, MsgNotifyOperation.VIEW,
                        EnumMsgNotifyIcon.Online);
                    return;
                }

                string PartialLocalPath = file.PartialLocalPath;
                if (!IsHasViewRights(PartialLocalPath))
                {
                    // record log
                    SkydrmApp.Singleton.User.AddNxlFileLog(PartialLocalPath, NxlOpLog.View, false);

                    App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Notify_PopBubble_Deny_View"), false, file.Name, MsgNotifyOperation.VIEW,
                        EnumMsgNotifyIcon.Online);

                    return;
                }

                try
                {
                    App.Log.Info(PartialLocalPath + "has view rights");

                    // delete the partial file
                    FileHelper.Delete_NoThrow(PartialLocalPath);

                    InnerOnlineView(file);
                    viewFiles.Add(file);
                }
                catch (Exception e)
                {
                    App.Log.Info(e.ToString(), e);
                }
            }, true);

        }

        private void InnerOnlineView(INxlFile f)
        {
            var file = f;
            // Firstly, we set true, used to ui binding
            file.FileStatus = EnumNxlFileStatus.Downloading;

            // download full again
            DownloadFile(file, (bool isSucceeded) =>
            {
                // reset
                file.FileStatus = EnumNxlFileStatus.Online;

                if (isSucceeded)
                {
                    StartViewerProcess(file);
                }
                else
                {
                    int result = Win32Common.FileStatus.FileIsOpen(file.LocalPath);
                    if (result == 1)
                    {
                        App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Notify_PopBubble_Download_FileOccupied"), false, file.Name, MsgNotifyOperation.VIEW,
                         EnumMsgNotifyIcon.Online);
                    }
                    else
                    {
                        App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Notify_PopBubble_Download_Failed"), false, file.Name, MsgNotifyOperation.VIEW,
                         EnumMsgNotifyIcon.Online);
                    }
                    
                }
            });
        }

        // Record the viewed offline files, which used for edited file uploading.
        private List<INxlFile> viewFiles = new List<INxlFile>();

        /// <summary>
        /// When view offline file, need to check file if is updated or not(the case include: edit, modify rights, overwrite).
        /// </summary>
        private void ViewOfflineFile(INxlFile nxlFile, bool isFromContextMenu = false)
        {
            App.Log.Info("View Project Offline File -->");
            viewFiles.Add(nxlFile);

            if (!IsNetworkAvailable)
            {
                StartViewerProcess(nxlFile);
                return;
            }

            FileOperateHelper.CheckOfflineFileVersion(GetRepoByNxlFile(nxlFile), currentWorkingArea, nxlFile, (bool isModified) =>
            {
                if (isModified)
                {
                    // Handle mydrive repo, since native files. (overwrite)
                    if(nxlFile.FileRepo == EnumFileRepo.REPO_MYDRIVE)
                    {
                        // select update
                        if (CustomMessageBoxResult.Positive == Edit.Helper.ShowUpdateDialog(nxlFile.Name))
                        {
                            SyncModifiedFileFromRms(nxlFile, true);
                        }
                        else
                        {
                            StartViewerProcess(nxlFile);
                        }

                        return;
                    }

                    // Other repo, need to partial download(get nxl header) to check if rights is modified.
                    try
                    {
                        // Triggered View operation by right context menu, in this condition, Partial file has downloaded already.
                        if (isFromContextMenu)
                        {
                            HandleConflictWhenView(nxlFile);
                        }

                        // Triggered View operation by double click, should first download partial
                        else
                        {
                            // re-download partial file -- may consume time when network is not good, how optimize this.
                            PartialDownloadEx((bool result, INxlFile nxl) => {
                                if (result && !string.IsNullOrEmpty(nxl.PartialLocalPath))
                                {
                                    this.win.Dispatcher.Invoke(new Action(delegate () {
                                        HandleConflictWhenView(nxl);
                                    }));
                                }
                                else
                                {
                                    App.Log.Info("PartialDownloadEx failed...");
                                    StartViewerProcess(nxl);
                                }
                            });

                        }

                    }
                    catch (Exception e)
                    {
                        App.Log.Info(e.ToString());
                    }

                }
                else
                {
                    StartViewerProcess(nxlFile);
                }
            });
        }

        /// <summary>
        /// Handle conflict issue when view the offline file
        /// </summary>
        private void HandleConflictWhenView(INxlFile nxlFile)
        {
            // Rights is modified, should enforce user to update file first.
            if (ViewModelHelper.CheckConflict(nxlFile) == NxlFileConflictType.FILE_IS_MODIFIED_RIGHTS)
            {
                if (CustomMessageBoxResult.Positive == Helper.ShowEnforceUpdateDialog(nxlFile.Name))
                {
                    SyncModifiedFileFromRms(nxlFile, true);
                }
            }
            else if (ViewModelHelper.CheckConflict(nxlFile) == NxlFileConflictType.FILE_IS_OVERWROTE)
            {
                if (CustomMessageBoxResult.Positive == Helper.ShowEnforceUpdateDialogForOverwrite(nxlFile.Name))
                {
                    SyncModifiedFileFromRms(nxlFile, true);
                }
            }
            // File content is edited, prompt user whether update from server or not.
            else if (ViewModelHelper.CheckConflict(nxlFile) == NxlFileConflictType.FILE_IS_EDITED)
            {
                // select update
                if (CustomMessageBoxResult.Positive == Helper.ShowUpdateDialog(nxlFile.Name))
                {
                    SyncModifiedFileFromRms(nxlFile, true);
                }
                else
                {
                    StartViewerProcess(nxlFile);
                }
            }
            else
            {
                StartViewerProcess(nxlFile);
            }
        }

        /// <summary>
        /// Sync the modified file node info and re-download from rms.
        /// </summary>
        private void SyncModifiedFileFromRms(INxlFile nxlFile, bool bIsView = true)
        {
            // Fix bug 58794
            if (!IsNetworkAvailable)
            {
                App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Notify_PopBubble_Sync_Failed"), false, nxlFile.Name);
                return;
            }

            // Actually, we can ignore the special handle from "Offline" filter if re-get repo like this.
            IFileRepo repo = GetRepoByNxlFile(nxlFile);

            // user select sync from rms, should reset this field(means begin to update the dirty data of local)
            nxlFile.IsMarkedFileRemoteModified = false;

            // user view modified file from "offline" filter.
            if (currentWorkingArea == EnumCurrentWorkingArea.FILTERS_OFFLINE
                && ViewModelHelper.IsNeedCheckVersion(nxlFile))
            {
                repo.SyncDestFile(nxlFile, (bool bSuccess, INxlFile updatedFile) =>
                {
                    OnSyncModifiedFinished(bSuccess, updatedFile, bIsView);
                }, true);

            }
            else
            {
                // 1. Will update the modified file info into db
                repo.SyncDestFile(nxlFile, (bool bSuccess, INxlFile updatedFile) =>
                {
                    // 2. Re-download file after updating file info.
                    OnSyncModifiedFinished(bSuccess, updatedFile, bIsView);
                }, false);
            }

        }

        private void OnSyncModifiedFinished(bool bSuccess, INxlFile updatedFile, bool bIsView = true)
        {
            if (bSuccess)
            {
                if (updatedFile != null)
                {
                    // Notify update the item ui
                    UpdateModifiedItem(updatedFile);

                    // Re-download the file.
                    ReDownloadFile(updatedFile, bIsView);
                }
                else
                {
                    App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Notify_PopBubble_Sync_Failed"), false);
                }
            }
            else
            {
                // updatedFile is null
                App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Notify_PopBubble_Sync_Failed"), false);
            }
        }

        private void ReDownloadFile(INxlFile updatedFile, bool bIsView = false)
        {
            updatedFile.FileStatus = EnumNxlFileStatus.Downloading;
            (updatedFile as NxlDoc).IsMarkedOffline = true; // used to bind ui
            (updatedFile as NxlDoc).IsEdit = false; // update db

            IFileRepo repo = GetRepoByNxlFile(updatedFile);
            repo?.MarkOffline(updatedFile, (bool result) =>
            {
                App.Log.Info("in ReDownloadProjectFile, result: " + result.ToString());

                // update file status
                NxlDoc doc = updatedFile as NxlDoc;
                doc.FileStatus = EnumNxlFileStatus.AvailableOffline;
                doc.Location = EnumFileLocation.Local;

                if (result)
                {
                    if (bIsView)
                    {
                        this.win.Dispatcher.Invoke(() => {
                            StartViewerProcess(updatedFile);
                        });
                    }
                }
                else
                {
                    App.ShowBalloonTip(string.Format(CultureStringInfo.ApplicationFindResource("Sync_ModifiedFile_download_Failed"), updatedFile.Name), false, doc.Name);
                }
            });
        }

        // Reset current working repo when is in Offline Filter and select file. -- fix bug 53558
        public void ResetCurrentWorkRepoWhenInFilterArea()
        {
            if ( CurrentSelectedFile != null 
                && (CurrentWorkingArea == EnumCurrentWorkingArea.FILTERS_OFFLINE 
                    || CurrentWorkingArea == EnumCurrentWorkingArea.SHARED_WITH_ME))
            {
                if (CurrentSelectedFile.FileRepo == EnumFileRepo.REPO_PROJECT)
                {
                    currentWorkRepo = projectRepo;
                } else if (CurrentSelectedFile.FileRepo == EnumFileRepo.REPO_WORKSPACE)
                {
                    currentWorkRepo = workSpaceRepo;
                } else if (CurrentSelectedFile.FileRepo == EnumFileRepo.REPO_MYDRIVE)
                {
                    currentWorkRepo = myDriveRepo;
                } 
                else // for other, default is myVault repo.
                {
                    currentWorkRepo = myVaultRepo;
                }
            }
        }

        private void ManualRefreshShareWithMe()
        {
            ShareWithMeClick();

            myVaultRepo?.SyncSharedWithMeFiles((bool bSuc, IList<INxlFile> results) => {
                if (bSuc)
                {
                    // after refresh
                    ShareWithMeClick();
                    DefaultSort();
                    CheckSearchWhenRefresh();
                }
            });
        }

        private void ShareWithMeClick()
        {
            CurrentWorkingArea = EnumCurrentWorkingArea.SHARED_WITH_ME;
            CurrentWorkingDirectoryFlag = "";

            SetCurSavePath();

            ResetCurrentSelectedFile();

            // Set offlineBtn as selected status, and cancel treeview item selected status.
            ChangeFilterBtnBackground();
            if (currentTreeViewItem != null)
            {
                // This will trigger TreeViewItemChangedEvent, then return directly.
                currentTreeViewItem.IsSelected = false;
            }

            try
            {
                // init data
                shareWithMeFiles.Clear();
                AddSrcFileToDest(myVaultRepo.GetSharedWithMeFiles(), shareWithMeFiles);
                SetListView(shareWithMeFiles);
            }
            catch (Exception e)
            {
                App.Log.Warn("Exception in ShareWithMeClick," + e, e);
            }
        }

        private void AllOfflineClick()
        {
            CurrentWorkingArea = EnumCurrentWorkingArea.FILTERS_OFFLINE;
            CurrentWorkingDirectoryFlag = "";

            SetCurSavePath();

            ResetCurrentSelectedFile();

            // Set offlineBtn as selected status, and cancel treeview item selected status.
            ChangeFilterBtnBackground();
            if (currentTreeViewItem != null)
            {
                // This will trigger TreeViewItemChangedEvent, then return directly.
                currentTreeViewItem.IsSelected = false;
            }

            try
            {
                // init data
                offlineFiles.Clear();
                AddSrcFileToDest(projectRepo.GetOfflines(), offlineFiles);
                AddSrcFileToDest(myVaultRepo.GetOfflines(), offlineFiles);
                AddSrcFileToDest(myDriveRepo.GetOfflines(), offlineFiles);
                AddSrcFileToDest(workSpaceRepo.GetOfflines(), offlineFiles);

                // External repo
                foreach(var one in externalRepos)
                {
                    AddSrcFileToDest(one.GetOfflines(), offlineFiles);
                }

                SetListView(offlineFiles);
            }
            catch (Exception e)
            {
                App.Log.Warn("Exception in AllOfflineClick," + e, e);
            }

        }

        public void ManualRefreshOutBox()
        {
            // First, get from local cache
            OutBoxClick();

            // Then should sync project from rms, because the selected dest folder(Project or project folder) maybe have been deleted. (fix bug 55652)
            //
            // Now comment out this, for bug 55625, now the more friendly approach should be given the tip that dest folder not found; 
            // it is up to the user to delete the file.
            //
            /*
            projectRepo.SyncAllRemoteData((bool bSuccess, IList<ProjectData> result) =>
            {
                if (bSuccess && CurrentWorkingArea == EnumCurrentWorkingArea.FILTERS_OUTBOX)
                {
                    // Display again after sync.
                    OutBoxClick();
                }
            }, false); */

        }

        private void OutBoxClick()
        {
            CurrentWorkingArea = EnumCurrentWorkingArea.FILTERS_OUTBOX;
            CurrentWorkingDirectoryFlag = "";

            SetCurSavePath();

            ResetCurrentSelectedFile();

            // Set outboxBtn as selected status, and cancel treeview item selected status.
            ChangeFilterBtnBackground();
            if (currentTreeViewItem != null)
            {
                // This will trigger TreeViewItemChangedEvent, then return directly.
                currentTreeViewItem.IsSelected = false;
            }

            try
            {
                // init data.
                outboxFiles.Clear();
                GetAllPendingFiles(ref outboxFiles);
                SetListView(outboxFiles);
            }
            catch (Exception e)
            {
                App.Log.Warn("Exception in OutBoxClick," + e, e);
            }

        }

        #endregion // For event handler.


        #region For other logic

        /// <summary>
        /// Default sort -- by DateModified descending.
        /// </summary>
        private void DefaultSort()
        {
            if (currentWorkingArea == EnumCurrentWorkingArea.PROJECT)
            {
                return;
            }

            try
            {
                if (currentWorkingArea == EnumCurrentWorkingArea.FILTERS_OFFLINE ||
                    currentWorkingArea == EnumCurrentWorkingArea.FILTERS_OUTBOX)
                {
                    // init  for filter list
                    ListViewBehavior.InitSortDirection(win.Filter_fileList, win.Filter_ColumnHeader_DateModified);
                    ListViewBehavior.DefaultSort(win.Filter_fileList, win.Filter_ColumnHeader_DateModified);
                }
                else
                {
                    // init for file list
                    ListViewBehavior.InitSortDirection(win.fileList, win.ColumnHeader_DateModified);
                    ListViewBehavior.DefaultSort(win.fileList, win.ColumnHeader_DateModified);
                }
            }
            catch (Exception e)
            {
                App.Log.Warn("Exception in DefaultSort," + e, e);
            }

        }

        // fixed bug 51000
        private void ResetCurrentSelectedFile()
        {
            CurrentSelectedFile = null;
        }

        // Init current save file path
        private void SetCurSavePath()
        {
            switch (currentWorkingArea)
            {
                case EnumCurrentWorkingArea.MYDRIVE:
                    CurrentSaveFilePath = new CurrentSelectedSavePath(MY_DRIVE,
                        myDriveRepo.CurrentWorkingFolder.PathId,
                        "SkyDRM://" + MY_DRIVE + myDriveRepo.CurrentWorkingFolder.DisplayPath);
                    break;
                case EnumCurrentWorkingArea.MYSPACE:
                case EnumCurrentWorkingArea.MYVAULT:
                    CurrentSaveFilePath = new CurrentSelectedSavePath(MY_VAULT, "/", "SkyDRM://" + MY_SPACE);
                    break;
                // workspace    
                case EnumCurrentWorkingArea.WORKSPACE:
                    CurrentSaveFilePath = new CurrentSelectedSavePath(WORKSPACE, workSpaceRepo.CurrentWorkingFolder.PathId,
                        "SkyDRM://" + WORKSPACE + workSpaceRepo.CurrentWorkingFolder.DisplayPath, App.SystemProject.Id.ToString());
                    break;
                case EnumCurrentWorkingArea.PROJECT_ROOT:
                    CurrentSaveFilePath = new CurrentSelectedSavePath(PROJECT,
                        projectRepo.CurrentWorkingFolder.PathId,
                        "SkyDRM://" + PROJECT + "/" + projectRepo.CurrentWorkingProject.ProjectInfo.DisplayName + projectRepo.CurrentWorkingFolder.DisplayPath,
                        projectRepo.CurrentWorkingProject.ProjectInfo.ProjectId.ToString());
                    break;
                // external repo
                case EnumCurrentWorkingArea.EXTERNAL_REPO_ROOT:
                    CurrentSaveFilePath = new CurrentSelectedSavePath(REPOSITORIES, currentWorkRepo.CurrentWorkingFolder.PathId,
                                             "SkyDRM://" + REPOSITORIES + "/" + currentWorkRepo.RepoDisplayName + currentWorkRepo.CurrentWorkingFolder.DisplayPath,
                                             currentWorkRepo.RepoId);
                    break;
                default:
                    CurrentSaveFilePath = null;
                    break;
            }
        }

        public void ChangeFilterBtnBackground()
        {
            switch (currentWorkingArea)
            {
                case EnumCurrentWorkingArea.SHARED_WITH_ME:
                    win.BtnShareWith.Background = colorGreen;
                    win.BtnOffline.Background = colorTransparent;
                    win.BtnOutbox.Background = colorTransparent;
                    break;
                case EnumCurrentWorkingArea.FILTERS_OFFLINE:
                    win.BtnShareWith.Background = colorTransparent;
                    win.BtnOffline.Background = colorGreen;
                    win.BtnOutbox.Background = colorTransparent;
                    break;
                case EnumCurrentWorkingArea.FILTERS_OUTBOX:
                    win.BtnShareWith.Background = colorTransparent;
                    win.BtnOffline.Background = colorTransparent;
                    win.BtnOutbox.Background = colorGreen;
                    break;
                default:
                    win.BtnShareWith.Background = colorTransparent;
                    win.BtnOffline.Background = colorTransparent;
                    win.BtnOutbox.Background = colorTransparent;
                    break;
            }
        }

        private void GetAllPendingFiles(ref IList<INxlFile> outSet)
        {
            AddSrcFileToDest(projectRepo.GetPendingUploads(), outSet);
            AddSrcFileToDest(myVaultRepo.GetPendingUploads(), outSet);
            AddSrcFileToDest(workSpaceRepo.GetPendingUploads(), outSet);
            AddSrcFileToDest(myDriveRepo.GetPendingUploads(), outSet);
            foreach (var item in externalRepos)
            {
                AddSrcFileToDest(item.GetPendingUploads(), outSet);
            }
        }

        private IList<INxlFile> GetAllEditedFiles()
        {
            IList<INxlFile> outSet = new List<INxlFile>();
            AddSrcFileToDest(projectRepo.GetEditedOfflineFiles(), outSet);
            AddSrcFileToDest(workSpaceRepo.GetEditedOfflineFiles(), outSet);
            foreach (var item in externalRepos)
            {
                if(item is SharedWorkspaceRepo)
                {
                    var files = (item as SharedWorkspaceRepo).GetEditedOfflineFiles();
                    AddSrcFileToDest(files, outSet);
                }
            }
            return outSet;
        }

        private void AddSrcFileToDest(IList<INxlFile> sourceSet, IList<INxlFile> destSet)
        {
            if (sourceSet == null || destSet == null)
            {
                return;
            }

            foreach (INxlFile one in sourceSet)
            {
                destSet.Add(one);
            }
        }

        /// <summary>
        /// Update the modified file from rms and update its ui node at the same time.
        /// </summary>
        private void UpdateModifiedFileAndItemNodeUi(INxlFile nxlFile)
        {
            if (nxlFile == null)
            {
                return;
            }

            UpdateModifiedItem(nxlFile);

            ReDownloadFile(nxlFile);
        }


        /// <summary>
        /// Need to notify current workinig listview corresponding file status to change.
        ///   --May the file is reproduced from db by refresh, and the original file object that added into the queue is a different object.     
        /// </summary>
        private void SyncCurrentListViewFileStatus(INxlFile nxlFile, EnumNxlFileStatus status)
        {
            if (nxlFile == null || string.IsNullOrEmpty(nxlFile.Name))
            {
                return;
            }

            foreach (var one in nxlFileList)
            {
                if (nxlFile.Equals(one)
                    && nxlFile.IsCreatedLocal == one.IsCreatedLocal /*fix bug 62598*/) 
                {
                    one.FileStatus = status;
                    one.Location = nxlFile.Location;
                    break;
                }
            }

            foreach (var one in copyFileList)
            {
                if (nxlFile.Equals(one)
                    && nxlFile.IsCreatedLocal == one.IsCreatedLocal)
                {
                    one.FileStatus = status;
                    one.Location = nxlFile.Location;
                    break;
                }
            }

        }

        // Display all project list.
        private void LoadProjectPageData(List<ProjectData> projects)
        {
            if (projects.Count > 0)
            {
                CollectionProject.Clear();
                var list = projects.OrderBy(p => p.ProjectInfo.Name).ToList();
                var fpList = list.OrderByDescending(p => p.ProjectInfo.BOwner).ToList();
                foreach (var item in fpList)
                {
                    CollectionProject.Add(item);
                }
            }
            App.Log.Info("LoadProjectPageData: project count " + CollectionProject.Count);
        }

        /// <summary>
        /// Update project listview when refresh treeview 'Project' item
        /// </summary>
        /// <param name="addProjects"></param>
        /// <param name="removeProjects"></param>
        private void UpdateProjectListView(List<ProjectData> addProjects, List<ProjectData> removeProjects)
        {
            if (addProjects.Count > 0)
            {
                foreach (var item in addProjects)
                {
                    CollectionProject.Add(item);
                }
            }
            if (removeProjects.Count > 0)
            {
                foreach (var item in removeProjects)
                {
                    CollectionProject.Remove(item);
                }
            }
            App.Log.Info("RefreshProjectPageData: addProjects Count " + addProjects.Count + ", removeProjects Count" + removeProjects.Count
                + ", Collection Project Count " + CollectionProject.Count);
        }

        /// <summary>
        /// Update externalRepo listview when refresh treeview 'REPOSITORIES' item
        /// </summary>
        /// <param name="addList"></param>
        /// <param name="removeList"></param>
        private void UpdateExternalRepoListView(List<IFileRepo> addList, List<IFileRepo> removeList)
        {
            if (addList.Count > 0)
            {
                foreach (var item in addList)
                {
                    externalRepos.Add(item);
                }
            }
            if (removeList.Count > 0)
            {
                foreach (var item in removeList)
                {
                    externalRepos.Remove(item);
                }
            }
            App.Log.Info("UpdateExternalRepoListView: addList Count " + addList.Count + ", removeList Count" + removeList.Count
                + ", ExternalRepo Count " + externalRepos.Count);
        }

        public void SetListView(IList<INxlFile> list)
        {
            if (list == null)
            {
                return;
            }

            nxlFileList.Clear();
            copyFileList.Clear();

            for (int i = 0; i < list.Count; i++)
            {
                INxlFile one = list[i];
                // restore status 
                if (one.FileStatus != EnumNxlFileStatus.Uploading 
                    && (one is PendingUploadFile) // Add this condition for fix bug 63248
                    && UploadManagerEx.GetInstance().FileIfIsUploading(one))
                {
                    one.FileStatus = EnumNxlFileStatus.Uploading;
                }

                nxlFileList.Add(one);
                copyFileList.Add(one);
            }
        }

        private void SaveOldAllFiles()
        {
            oldAllFiles.Clear();
            foreach (var i in nxlFileList)
            {
                oldAllFiles.Add(i);
            }
        }

        private void ClearListView()
        {
            nxlFileList.Clear();
            copyFileList.Clear();
        }

        private void AddToListView(INxlFile nxlFile)
        {
            nxlFileList.Add(nxlFile);
            copyFileList.Add(nxlFile);

            // default sort
            DefaultSort();
        }

        /// <summary>
        /// Try to upload local file if exist have not been uploaded.
        /// </summary>
        public void TryToUpload()
        {
            UploadManagerEx.GetInstance().TryToUpload();
        }

        // Do protect from drop
        public void DoProtectFromDrop(string[] path)
        {
            for (int i = 0; i < path.Length; i++)
            {
                FileInfo fInfor = new FileInfo(path[i]);
                if (fInfor.Attributes == System.IO.FileAttributes.Directory) // Folder
                {
                    SkydrmApp.Singleton.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Notify_PopBubble_CanNot_Protect"), false, fInfor.Name);
                    return;
                }
            }

            string[] selectedFile;
            selectedFile = path;

            // when drop one file, consider whether it is nxl file, nxl file do add file, or else continue do upload or protect 
            if (selectedFile.Length == 1)
            {
                if (new FileInfo(selectedFile[0]).Length == 0)
                {
                    App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Common_Notify_EmptyFile_Not_Operate"), false, Path.GetFileName(selectedFile[0]));
                    return;
                }

                if (App.Rmsdk.SDWL_RPM_GetFileStatus(selectedFile[0], out int dirstatus, out bool filestatus))
                {
                    if (dirstatus == 1 && filestatus) //if file is rpm folder nxl file should deny add nxl 
                    {
                        App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Common_DenyAdd_InRPM"), false, Path.GetFileName(selectedFile[0]));
                        return;
                    }
                    if (filestatus) // nxl file
                    {
                        App.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(()=> 
                        {
                            App.Mediator.OnShowOperationAddNxlWinByDrag(selectedFile[0], this.win, CurrentSaveFilePath);
                        }));
                        return;
                    }
                }
            }
            // fix bug 66596 Drag multiple files(include nxl file) from RPM folder to RMD main window, nxl file will be redecrypt with different rights.
            if (selectedFile.Length > 1)
            {
                if (!ProtectFileHelper.CheckFilePathDoProtect(selectedFile, out string tag, out List<string> rightFilePath))
                {
                    return;
                }
                selectedFile = rightFilePath.ToArray();
            }

            App.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                if (CurrentWorkingArea == EnumCurrentWorkingArea.MYDRIVE)
                {
                    App.Mediator.OnShowOperationUploadWinByMainWin(selectedFile, CurrentSaveFilePath, win);
                }
                else
                {
                    App.Mediator.OnShowOperationProtectWinByDrag(selectedFile, GetFileRepos(), CurrentSaveFilePath, win,
                        FileFromSource.SkyDRM_PlugIn);
                }
            }));
            
        }

        #endregion // For other logic


        #region For start viewer

        private INxlFile GetSelectedFileFromMapKeyValue(string SecretSignal)
        {
            ViewerProcess vp = null;
            if (!ViewerProcess.GetValueByKey(SecretSignal, out vp))
            {
                App.Log.Info("Can not find key");
                return null;
            }
            if (null == vp)
            {
                App.Log.Info("vp is null");
                return null;
            }

            var file = vp.CurrentSelectedFile;
            if (null == file)
            {
                App.Log.Info("current selsected file is null");
                return null;
            }

            return file;
        }

        private void ReceiveData(int msg, int wParam, string data)
        {
            switch (msg)
            {
                //case IPCManager.WM_VIEWER_WINDOW_LOADED:
                //    // Decrypt and send data to Viewer when viwer window is loaded. 
                //    ExecuteView(data);
                //    break;

                case IPCManager.WM_COPYDATA:

                    //
                    // For Viewer
                    //
                    if (IPCManager.WM_VIEWER_WINDOW_LOADED == wParam)
                    {
                        string SecretSignal = string.Empty;
                        bool isOnlieView = false;

                        JObject jobect = (JObject)JsonConvert.DeserializeObject(data);
                        if (jobect.ContainsKey("SecretSignal"))
                        {
                            SecretSignal = jobect["SecretSignal"].ToString();
                        }
                        if (jobect.ContainsKey("IsOnlieView"))
                        {
                            isOnlieView = (bool)jobect.GetValue("IsOnlieView");
                        }

                        if (!isOnlieView) // view for local file
                        {
                            ExecuteView(SecretSignal);
                        }
                        else // view for online file
                        {
                            OnlineView(SecretSignal);
                        }
                        return;
                    }
                    break;
                default:
                    break;
            }
        }

        private bool IsHasViewRights(string localPath)
        {
            try
            {
                return App.Rmsdk.User
                        .GetNxlFileFingerPrint(localPath)
                        .HasRight(FileRights.RIGHT_VIEW);
            }
            catch (SkydrmException e)
            {
                App.Log.Info(localPath + "regard as no view rights", e);

            }
            return false;
        }

        private void OnlineView(string SecretSignal)
        {
            var file = GetSelectedFileFromMapKeyValue(SecretSignal);

            // Filter out unsupported file type first.
            var fileType = FileTypeHelper.GetFileTypeByExtension(file.Name);
            //if (fileType == FileTypeHelper.EnumFileType.FILE_TYPE_NOT_SUPPORT)
            //{
            //    NotifyViewer(file.PathId, IPCManager.WM_UNSUPPORTED_FILE_TYPE, file.Name);
            //    return;
            //}

            // download partial for check rights
            currentWorkRepo.DownloadFile(file, true, (bool result) =>
            {
                // sanity check
                if (!result)
                {
                    NotifyViewer(file.PathId, IPCManager.WM_DOWNLOAD_FAILED, file.Name);
                    return;
                }
                string PartialLocalPath = file.PartialLocalPath;
                if (!IsHasViewRights(PartialLocalPath))
                {
                    // record log
                    SkydrmApp.Singleton.User.AddNxlFileLog(PartialLocalPath, NxlOpLog.View, false);
                    NotifyViewer(file.PathId, IPCManager.WM_HAS_NO_RIGHTS, file.Name);
                    return;
                }
                try
                {
                    App.Log.Info(PartialLocalPath + "has view rights");
                    // by comment, if online view ok, log will be send at ExecuteView();
                    //SkydrmApp.Singleton.User.AddNxlFileLog(localPath, NxlOpLog.View, true);

                    // delete the partial file
                    FileHelper.Delete_NoThrow(PartialLocalPath);

                    // download full again
                    DownloadFile(file, (bool isSucceeded) =>
                    {
                        if (isSucceeded)
                        {
                            ExecuteView(file.PathId);
                        }
                        else
                        {
                            NotifyViewer(file.PathId, IPCManager.WM_DOWNLOAD_FAILED, file.Name);
                        }
                    });
                }
                catch (Exception e)
                {
                    App.Log.Info(e.ToString(), e);
                    NotifyViewer(file.PathId, IPCManager.WM_DOWNLOAD_FAILED, file.Name);
                }
            }, true);
        }

        // Download file for online view
        public void DownloadFile(INxlFile nxlFile, Action<bool> callBack)
        {

            NxlDoc doc = nxlFile as NxlDoc;
            currentWorkRepo.DownloadFile(doc, true, (bool result) =>
            {
                callBack?.Invoke(result);
            }, false, true);
        }

        [Serializable]
        private class StartViewerInfo
        {
            public string MainWindowIntPtr { get; }

            public string SecretSignal { get; }

            public bool IsOnlieView { get; }

            public StartViewerInfo(string MainWindowIntPtr, string SecretSignal, bool IsOnlieView)
            {
                this.MainWindowIntPtr = MainWindowIntPtr;
                this.SecretSignal = SecretSignal;
                this.IsOnlieView = IsOnlieView;
            }
        }

        public bool IsDisplayEditButton(INxlFile localNxlFile, NxlFileFingerPrint fp)
        {
            bool result = false;

            switch (localNxlFile.FileRepo)
            {
                case EnumFileRepo.REPO_PROJECT:

                    if (localNxlFile.FileStatus == EnumNxlFileStatus.AvailableOffline
                        || localNxlFile.FileStatus == EnumNxlFileStatus.CachedFile
                        || localNxlFile.IsEdit)
                    {
                        result = SkydrmApp.Singleton.ExternalMgr.IsNxlFileCanEdit(fp);
                    }
                    break;

                default:
                    break;
            }

            return result;
        }

        public class FileStatus
        {
            public static Int32 ClickFromMainWindow = 0x01;
            public static Int32 Edit = 0x02;
            public static Int32 Share = 0x04;
        }

        public class FileExternalInfo
        {
            public string FileRepo;
            public string FileStatus;
            public bool IsEdit;  // File if is edited in local or not.
            public bool IsClickFromSkydrmDesktop;
            public string RepoId;
            public string DisplayPath;
            public string[] emails;
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        private bool QueryFileAssciation(string fileExt)
        {
            bool result = false;
            try
            {
                RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\NextLabs\SkyDRM\nxrmhandler");
                RegistryKey subKey = registryKey.OpenSubKey(fileExt);
                string applicationPath =(string) subKey.GetValue("");
                if (!string.IsNullOrEmpty(applicationPath))
                {
                    result = true;
                }
            }
            catch (Exception ex)
            {

            }
            return result;
        }

        private void MonitorNxlFile(string nxlFilePath, string mappingFilePath)
        {
            Alphaleonis.Win32.Filesystem.FileInfo oriFileInfo = new Alphaleonis.Win32.Filesystem.FileInfo(nxlFilePath);
            DateTime lastWriteTime = oriFileInfo.LastWriteTime;
            long length = oriFileInfo.Length;

            System.IO.FileSystemWatcher watcher = new System.IO.FileSystemWatcher();
            watcher.Path = Alphaleonis.Win32.Filesystem.Path.GetDirectoryName(nxlFilePath);

            // Watch for changes in LastAccess and LastWrite times, and
            // the renaming of files or directories.
            // watcher.NotifyFilter = NotifyFilters.Size;
            //| NotifyFilters.CreationTime
            //| NotifyFilters.LastWrite;

            // Only watch inputted file.
            watcher.Filter = Alphaleonis.Win32.Filesystem.Path.GetFileName(nxlFilePath);

            // Add event handlers.
            watcher.Created += (object source, System.IO.FileSystemEventArgs e) =>
            {
                if (e.ChangeType == System.IO.WatcherChangeTypes.Created && string.Equals(e.FullPath, nxlFilePath, StringComparison.CurrentCultureIgnoreCase))
                {
                    try
                    {
                        watcher.EnableRaisingEvents = false;
                        Alphaleonis.Win32.Filesystem.FileInfo fileInfo = new Alphaleonis.Win32.Filesystem.FileInfo(nxlFilePath);
                        if (DateTime.Equals(lastWriteTime, fileInfo.LastWriteTime) && length == fileInfo.Length)
                        {
                           // SkydrmApp.Singleton.MainWin.viewModel.SyncFileAfterEditFromViewer(nxlFilePath, false);
                        }
                        else
                        {
                            Alphaleonis.Win32.Filesystem.File.Copy(nxlFilePath, mappingFilePath, true);
                            SkydrmApp.Singleton.MainWin.viewModel.SyncFileAfterEditFromViewer(mappingFilePath, true);
                        }
                    }
                    catch (Exception ex)
                    {
                     
                    }
                }
            };
            // Begin watching.
            watcher.EnableRaisingEvents = true;
        }

        public void StartViewerProcess(INxlFile nxlFile)
        {
            try
            {
                if (!nxlFile.IsNxlFile)
                {
                    //Process.Start(nxlFile.LocalPath);
                    //string destination = Path.GetTempPath() + Path.GetFileName(nxlFile.LocalPath);
                    //string destination = Path.GetTempPath() + Guid.NewGuid().ToString() + Path.GetExtension(longFilePath);
                    //File.Copy(nxlFile.LocalPath, destination, true);
                    Process p = new System.Diagnostics.Process();
                    p.StartInfo.FileName = "nxrmhandler.exe";
                    p.StartInfo.WorkingDirectory = Path.GetDirectoryName(System.Windows.Forms.Application.StartupPath) + "\\" + "RPM" + "\\" + "bin";
                    p.StartInfo.Arguments += "\"" + nxlFile.LocalPath + "\"";
                    p.Start();
                }
                else
                {
                    FileExternalInfo fileInfo = new FileExternalInfo();
                    fileInfo.FileRepo = nxlFile.FileRepo.ToString();
                    fileInfo.FileStatus = nxlFile.FileStatus.ToString();
                    fileInfo.IsEdit = nxlFile.IsEdit;
                    fileInfo.IsClickFromSkydrmDesktop = true;
                    fileInfo.RepoId = nxlFile.RepoId;
                    fileInfo.DisplayPath = nxlFile.DisplayPath;
                    fileInfo.emails = nxlFile.FileInfo.Emails;
                    string jsonStr = JsonConvert.SerializeObject(fileInfo);
                    string base64Str = Base64Encode(jsonStr);

                    //Process.Start(nxlFile.LocalPath, base64Str);
                    Process p = new System.Diagnostics.Process();
                    p.StartInfo.FileName = "nxrmhandler.exe";
                    p.StartInfo.WorkingDirectory = Path.GetDirectoryName(System.Windows.Forms.Application.StartupPath) + "\\" + "RPM" + "\\" + "bin";
                    p.StartInfo.Arguments += "\"" + nxlFile.LocalPath + "\"";
                    p.StartInfo.Arguments += " ";
                    p.StartInfo.Arguments += base64Str;
                    p.Start();
                }
            }
            catch (Exception ex)
            {

            }
        }

        //public void StartViewerProcess(INxlFile nxlFile) // default is view local file
        //{
        //    try
        //    {
        //        if (null == nxlFile)
        //        {
        //            return;
        //        }

        //        const int EXCEL_MAX_PATH_LENGTH = 218;
        //        const int MAX_PATH_LENGTH = 260;
        //        string extention = string.Empty;
        //        if (nxlFile.LocalPath.EndsWith(".nxl", StringComparison.CurrentCultureIgnoreCase))
        //        {
        //            extention = Path.GetExtension(Path.GetFileNameWithoutExtension(nxlFile.LocalPath)).ToLower();
        //        }
        //        else
        //        {
        //            extention = Path.GetExtension(nxlFile.LocalPath).ToLower();
        //        }
         
        //        if (nxlFile.LocalPath.Length < MAX_PATH_LENGTH)
        //        {
        //            if (SkydrmLocal.rmc.fileSystem.external.Helper.ExcelExtensions.Contains(extention))
        //            {
        //                if (nxlFile.LocalPath.Length >= EXCEL_MAX_PATH_LENGTH)
        //                {
        //                    StartNxrmViewer(nxlFile);
        //                    return;
        //                }
        //            }

        //            if (!nxlFile.IsNxlFile)
        //            {
        //                Process.Start(nxlFile.LocalPath);
        //                return;
        //            }

        //            if (QueryFileAssciation(extention))
        //            {
        //                Process.Start(nxlFile.LocalPath);
        //                return;
        //            }
 
        //        }

        //        StartNxrmViewer(nxlFile);
        //    }
        //    catch (Exception ex)
        //    {

        //    }
        //}

        //private void StartNxrmViewer(INxlFile nxlFile)
        //{
        //    FileExternalInfo fileInfo = new FileExternalInfo();
        //   // fileInfo.FilePath = nxlFile.LocalPath;
        //    fileInfo.FileRepo = nxlFile.FileRepo.ToString();
        //    fileInfo.FileStatus = nxlFile.FileStatus.ToString();
        //    fileInfo.IsEdit = nxlFile.IsEdit;
        //    fileInfo.IsClickFromSkydrmDesktop = true;
        //   // fileInfo.Intent = "-view";
        //    fileInfo.RepoId = nxlFile.RepoId;
        //    fileInfo.DisplayPath = nxlFile.DisplayPath;

        //    string jsonStr = JsonConvert.SerializeObject(fileInfo);
        //    string base64Str = Base64Encode(jsonStr);

        //    Process p = new System.Diagnostics.Process();
        //    p.StartInfo.FileName = "nxrmviewer.exe";
        //    p.StartInfo.WorkingDirectory = System.Windows.Forms.Application.StartupPath;
        //    p.StartInfo.Arguments += base64Str;
        //    p.Start();
        //    return;
        //}


        private bool IsDisplayEditButton(INxlFile nxlFile)
        {
            bool result = false;
            if (nxlFile.FileRepo == EnumFileRepo.REPO_PROJECT || nxlFile.FileRepo == EnumFileRepo.REPO_WORKSPACE)
            {
                if (nxlFile.FileStatus == EnumNxlFileStatus.AvailableOffline
                         || nxlFile.FileStatus == EnumNxlFileStatus.CachedFile
                         || nxlFile.IsEdit)
                {
                    result = true;
                }
            }
            return result;
        }

        private bool IsDisplayShareButton(INxlFile nxlFile)
        {
            bool result = false;
            if (nxlFile.FileStatus == EnumNxlFileStatus.AvailableOffline
                        || nxlFile.FileStatus == EnumNxlFileStatus.CachedFile
                        || nxlFile.FileStatus == EnumNxlFileStatus.Online
                        || nxlFile.FileStatus == EnumNxlFileStatus.DownLoadedSucceed)
            {
                result = true;
            }

            return result;
        }


        private void ExecuteView(string key)
        {
            new Thread(new ParameterizedThreadStart(ExecuteViewInBackground)) { Name = "ExecuteViewInBackground", IsBackground = true, Priority = ThreadPriority.Normal }.Start(key);
        }


        private void NotifyViewer(string key, Int32 action, string message)
        {
            ViewerProcess vp = null;

            if (!ViewerProcess.GetValueByKey(key, out vp))
            {
                return;
            }

            IPCManager.SendData(vp.GetMainWindowHandle(), action, message);
        }

        private void ExecuteViewInBackground(object obj)
        {
            ViewerProcess vp = null;

            if (!ViewerProcess.GetValueByKey(obj.ToString(), out vp)) {

                return;
            }

            if (null == vp)
            {
                return;
            }

            INxlFile CurrentSelectedFile = vp.CurrentSelectedFile;

            if (null == CurrentSelectedFile)
            {
                return;
            }

            try
            {
                DecryptAgent decryptAgent = new DecryptAgent();
                decryptAgent.Decrypt(CurrentSelectedFile, delegate (NxlConverterResult result)
                {

                    vp.ConverterResult = result;
                    IntPtr hwnd = vp.GetMainWindowHandle();
                    string data = JsonConvert.SerializeObject(result);
                    IPCManager.SendData(hwnd, IPCManager.WM_DECRYPTED_RESULT, data);
                });
            }
            catch (Exception e)
            {
                App.Log.Info("Failed when executing Decrypt..." + e.ToString());
            }

        }

        #endregion // For start viewer


        #region For menu command

        /// <summary>
        /// Dispatch menu "CanExecute" command handler
        /// 
        /// Can execute if return true(and in this case, the menu item will be disabled automatically), will not execute if return false.
        /// </summary>
        private bool MenuCommand_CanExecute(object parameter)
        {
            return true;
        }

        // Handle shortcut disable.
        private bool IsEnableMenuCommand()
        {
            if (CurrentSelectedFile == null)
            {
                return false;
            }

            if (CurrentSelectedFile.IsFolder)
            {
                return false;
            }

            if (CurrentSelectedFile.FileStatus == EnumNxlFileStatus.Downloading
                || CurrentSelectedFile.FileStatus == EnumNxlFileStatus.Uploading)
            {
                return false;
            }

            if (CurrentSelectedFile.FileStatus == EnumNxlFileStatus.Online
                && !IsNetworkAvailable)
            {
                return false;
            }

            return true;
        }

        // Handle shortcut disable, only for "Del shortcut". 
        private bool IsEnableMenuCommandEx()
        {
            return (CurrentSelectedFile != null
                && CurrentSelectedFile.FileStatus != EnumNxlFileStatus.Uploading
                && CurrentSelectedFile.FileStatus != EnumNxlFileStatus.Online
                && CurrentSelectedFile.FileStatus != EnumNxlFileStatus.AvailableOffline);
        }

        /// <summary>
        /// Dispatch menu "Execute" command handler.
        /// </summary>
        private void MenuCommand_Executed(object parameter)
        {
            if (parameter == null)
            {
                return;
            }

            switch (parameter.ToString())
            {
                /********* File Menu ****************/
                case Constant.MENU_OPEN_FILE:

                    if (!IsEnableMenuCommand())
                    {
                        return;
                    }

                    Menu_OpenFile();
                    break;

                case Constant.MENU_PROTECT_FILE:
                    if (IsInitializing)
                    {
                        return;
                    }

                    // Handle shortcut disable -- fix bug 61686.
                    //if (CurrentWorkingArea == EnumCurrentWorkingArea.PROJECT
                    //    || CurrentWorkingArea == EnumCurrentWorkingArea.MYDRIVE
                    //    || CurrentWorkingArea == EnumCurrentWorkingArea.EXTERNAL_REPO)
                    //{
                    //    return;
                    //}

                    Menu_ProtectFile();
                    break;
                case Constant.MENU_ADD_FILE:
                    DoAddNxl();
                    break;

                case Constant.MENU_SHARE_FILE:
                    if (IsInitializing)
                    {
                        return;
                    }

                    // Handle shortcut disable -- fix bug 51118.
                    //if (CurrentWorkingArea == EnumCurrentWorkingArea.PROJECT 
                    //    || CurrentWorkingArea == EnumCurrentWorkingArea.PROJECT_ROOT 
                    //    || CurrentWorkingArea == EnumCurrentWorkingArea.WORKSPACE
                    //    || CurrentWorkingArea == EnumCurrentWorkingArea.MYDRIVE
                    //    || CurrentWorkingArea == EnumCurrentWorkingArea.EXTERNAL_REPO
                    //    || CurrentWorkingArea == EnumCurrentWorkingArea.EXTERNAL_REPO_ROOT)
                    //{
                    //    return;
                    //}

                    Menu_ShareFile();
                    break;

                case Constant.MENU_REMOVE_FILE:

                    if (!IsEnableMenuCommandEx())
                    {
                        return;
                    }

                    Menu_RemoveFile();
                    break;

                case Constant.MENU_STOPE_UPLOAD:
                    Menu_StopUpload();
                    break;

                case Constant.MENU_START_UPLOAD:
                    Menu_StartUpload();
                    break;

                case Constant.MENU_VIEW_FILEINFO: // need to pass paras

                    if (!IsEnableMenuCommand())
                    {
                        return;
                    }

                    // Forbid to execute for shortcut operation: fix bug 64320
                    if(CurrentSelectedFile != null && !CurrentSelectedFile.IsNxlFile)
                    {
                        return;
                    }

                    Menu_ViewFileInfo();
                    break;

                case Constant.MENU_ACTIVITY_LOG:
                    Menu_ActiviityLog();
                    break;

                case Constant.MENU_SIGNOUT:
                    Menu_Signout();
                    break;

                case Constant.MENU_EXIT:
                    Menu_Exit();
                    break;

                /********* View Menu ****************/

                case Constant.MENU_REFRESH:
                    Menu_Refresh();
                    break;

                case Constant.MENU_SORT_BY_NAME:
                    Menu_SortByFileName();
                    break;

                case Constant.MENU_SORT_BY_MODIFIED:
                    Menu_SortByLastModified();
                    break;

                case Constant.MENU_SORT_BY_SIZE:
                    Menu_SortByFileSize();
                    break;

                /*********** Preferences Menu *********/
                case Constant.MENU_PREFERENCES:
                    Menu_Prefrences();
                    break;

                /********* Help Menu ****************/
                case Constant.MENU_GETTING_STARTED:
                    Menu_GettingStarted();
                    break;

                case Constant.MENU_HELP:
                    Menu_Help();
                    break;

                case Constant.MENU_CHECK_UPDATES:
                    Menu_CheckForUpdates();
                    break;

                case Constant.MENU_REPORT_ISSUE:
                    Menu_ReportIssue();
                    break;

                case Constant.MENU_ABOUT:
                    Menu_AboutSkyDrm();
                    break;
                default:
                    break;

            }

        }

        /// <summary>
        /// Dispatch tool bar command handler
        /// </summary>
        private void CommandDispatcher(object args)
        {
            switch (args.ToString())
            {
                // Tool bar command
                case Constant.COMMAND_PROTECT:
                    DoProtect();
                    break;
                case Constant.COMMAND_SHARE:
                    DoShare();
                    break;
                case Constant.COMMAND_ADD_NXL:
                    DoAddNxl();
                    break;
                case Constant.COMMAND_OPEN_WEB:
                    DoOpenWeb();
                    break;
                case Constant.COMMAND_START_UPLOAD:
                    DoUpload();
                    break;
                case Constant.COMMAND_STOP_UPLOAD:
                    StopUpload();
                    break;
                case Constant.COMMAND_SETTINGS:
                    OpenSettingBtnMenu();
                    break;
                case Constant.COMMAND_PREFERENCE:
                    Menu_Prefrences();
                    break;
                case Constant.COMMAND_ABOUT:
                    Menu_AboutSkyDrm();
                    break;
                case Constant.COMMAND_REFRESH:
                    DoRefresh();
                    ResetCurrentSelectedFile(); // Fix bug 55400
                    break;
                case Constant.COMMAND_UPLOAD_FOLDER:
                    //todo
                    break;
                case Constant.COMMAND_OFFLINE_FILES:
                    AllOfflineClick();
                    // default sort
                    DefaultSort();
                    break;
                case Constant.COMMAND_OUTBOX:
                    OutBoxClick();
                    // default sort
                    DefaultSort();
                    break;
                case Constant.COMMAND_UPLOAD_FILE:
                    DoUploadFile();
                    break;
                case Constant.COMMAND_SHAREWITHME:
                    ManualRefreshShareWithMe();
                    break;
                case Constant.COMMAND_MYSPACE_NAVIGATE:
                    DoMySpaceNavigate();
                    break;
                case Constant.COMMAND_WorkSpace_NAVIGATE:
                    DoWorkSpaceNavigate();
                    break;
                case Constant.COMMAND_MYDRIVE_NAVIGATE:
                    DoMyDriveNavigate();
                    break;
                case Constant.COMMAND_MYVAULT_NAVIGATE:
                    DoMyVaultNavigate();
                    break;
                case Constant.COMMAND_SHAREWITHME_NAVIGATE:
                    DoShareWithMeNavigate();
                    break;
                case Constant.COMMAND_ADD_REPO:
                    ContextMenu_TreeViewAddRepo();
                    break;
                default:
                    break;
            }
        }

        private void TestUpload(string local)
        {
            uploadCallback = new UploadCallback(this);
            currentWorkRepo.UploadFileEx(local, Path.GetFileName(local),
                (currentWorkRepo as ExternalRepo).CurrentWorkingFolder.PathId, false, uploadCallback);
        }

        private class UploadCallback : IUploadProgressCallback
        {
            ViewModelMainWindow Host;
            public UploadCallback(ViewModelMainWindow host)
            {
                this.Host = host;
            }
            // Will call this when user perform CANCEL operation.
            public void OnCancel(ICancelable cancel)
            {
                cancel?.Cancel();
            }

            public void OnComplete(bool bSuccess, string uploadFilePath, RepoApiException except)
            {
                SkydrmApp.Singleton.Dispatcher.Invoke(() =>
                {
                    if (bSuccess)
                    {
                        Host.DoRefresh();
                    }
                });
            }

            public void OnProgress(long value, long total)
            {
                SkydrmApp.Singleton.Dispatcher.Invoke(() =>
                {
                    Console.WriteLine("Upload progress is :{0}%", value/(float)total * 100);
                });
            }
        }


        /// <summary>
        /// Dispatch right click context menu command handler
        /// </summary>
        private void ContextMenuCmd(object args)
        {
            if (args is ContextMenuCmdArgs)
            {
                ContextMenuCmdArgs cmdArgs = args as ContextMenuCmdArgs;
                switch (cmdArgs.CmdName)
                {
                    // Context menu
                    case Constant.CONTEXT_MENU_CMD_UPLOAD:
                        ContextMenu_Upload(cmdArgs.SelectedFile);
                        break;
                    case Constant.CONTEXT_MENU_CMD_OVERWRITE_UPLOAD:
                        ContextMenu_OverWriteUpload(cmdArgs.SelectedFile);
                        break;
                    case Constant.CONTEXT_MENU_CMD_RENAME_UPLOAD:
                        ContextMenu_RenameUpload(cmdArgs.SelectedFile);
                        break;
                    case Constant.CONTEXT_MENU_CMD_VIEW_FILE:
                        ContextMenu_DoViewFile(cmdArgs);
                        break;
                    case Constant.CONTEXT_MENU_CMD_VIEW_FILE_INFO:
                        ContextMenu_DoViewFileInfo(cmdArgs.SelectedFile);
                        break;
                    case Constant.CONTEXT_MENU_CMD_REMOVE:
                        ContextMenu_DoRemove(cmdArgs);
                        break;
                    case Constant.CONTEXT_MENU_CMD_OPEN_SKYDRM:
                        ContextMenu_DoOpenSkyDRM(cmdArgs);
                        break;
                    case Constant.CONTEXT_MENU_CMD_MAKE_OFFLINE:
                        ContextMenu_MarkOffline(cmdArgs);
                        break;
                    case Constant.CONTEXT_MENU_CMD_UNMAKE_OFFLINE:
                        ContextMenu_UnmarkOffline(cmdArgs);
                        break;
                    case Constant.CONTEXT_MENU_CMD_SAVE_AS:
                        ContextMenu_SaveAs(cmdArgs);
                        break;
                    case Constant.CONTEXT_MENU_CMD_PROTECT:
                        ContextMenu_DoProtect(cmdArgs.SelectedFile);
                        break;
                    case Constant.CONTEXT_MENU_CMD_SHARE:
                        ContextMenu_DoShare(cmdArgs.SelectedFile);
                        break;
                    case Constant.CONTEXT_MENU_CMD_ADD_FILE:
                        ContextMenu_AddFile(cmdArgs.SelectedFile);
                        break;
                    case Constant.CONTEXT_MENU_CMD_EDIT:
                        ContextMenu_Edit(cmdArgs);
                        break;
                    case Constant.CONTEXT_MENU_CMD_EXTRACT_CONTENT:
                        ContextMenu_ExtractContents(cmdArgs);
                        break;
                    case Constant.CONTEXT_MENU_CMD_MODIFY_RIGHTS:
                        ContextMenu_ModifyRights(cmdArgs.SelectedFile);
                        break;
                    case Constant.CONTEXT_MENU_CMD_TREE_ADD_REPO:
                        ContextMenu_TreeViewAddRepo();
                        break;
                    default:
                        break;
                }
            }
        }

        #endregion // For menu command


        #region Menu Bar
        // File Menu
        private void Menu_OpenFile()
        {
            InnerViewFile(CurrentSelectedFile);
        }

        private void Menu_ProtectFile()
        {
            DoProtect();
        }

        private void Menu_ShareFile()
        {
            DoShare();
        }

        private void Menu_RemoveFile()
        {
            ManualRemove(CurrentSelectedFile);
        }

        private void Menu_StopUpload()
        {
            StopUpload();
        }

        private void Menu_StartUpload()
        {
            DoUpload();
        }


        class ViewFileInfoFeature
        {
            ViewModelMainWindow host;
            INxlFile target;

            public ViewFileInfoFeature(ViewModelMainWindow host, INxlFile target)
            {
                this.host = host;
                this.target = target;
            }

            public void PartialDownload(OnDownloadComplete callback)
            {
                // When mainWin first open,the currentWorkRepo is null and PendingUploadFile will use LocalPath getFingerprint.
                // In MyVault and Project, onlineFile and offlineFile will use parital_localPath getFingerprint, if partial_localpath not exist will check localPath and return.
                if (target.Location == EnumFileLocation.Local)
                {
                    callback?.Invoke(true);
                    return;
                }

                //When user right-click menuitem will re-download partial onlineFile in project.(Project file maybe modify rights)
                //So don't need re-downloal partial file again.
                if (FileHelper.Exist(target.PartialLocalPath))
                {
                    callback?.Invoke(true);
                    return;
                }
                host.currentWorkRepo.DownloadFile(target, true, (bool result) => {
                    host.win.Dispatcher.Invoke(() => { callback?.Invoke(result); });
                },
                true
                );
            }

            public void Do()
            {
                try
                {
                    string winTag = CultureStringInfo.ShowFileInfoWin_Operation_ShowDetail + "|" + target.Name;
                    Window opennedWin = host.GetOpennedWindow(winTag);
                    if (opennedWin == null)
                    {
                        Window win = new FileInfoWin(target.FileInfo);
                        win.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                        // Use file name as the tag(since the file name in local list is unique.)
                        win.Tag = winTag;
                        win.Show();
                        win.Activate();
                        win.Topmost = true;
                        win.Topmost = false;
                        win.Focus();
                    }
                    else
                    {
                        opennedWin.Show();
                        opennedWin.Activate();
                        opennedWin.Topmost = true;
                        opennedWin.Topmost = false;
                        opennedWin.Focus();
                        host.win.WindowState = WindowState.Normal;
                    }
                }
                catch (Exception e)
                {
                    host.App.Log.Warn("Exception in Menu_ViewFileInfo," + e, e);
                }
            }


        }

        private void Menu_ViewFileInfo()
        {
            if (CurrentSelectedFile == null)
            {
                return;
            }
            ViewFileInfoFeature feature = new ViewFileInfoFeature(this, currentSelectedFile);
            feature.PartialDownload((bool rt) =>
            {
                feature.Do();
            });
        }

        private void Menu_ActiviityLog()
        {
        }

        private void Menu_Signout()
        {
            // If menu item is disabled(but can't effect Input Gesture, so intercept here).
            if (!win.Menu_logout.IsEnabled)
            {
                return;
            }

            App.RequestLogout(RequestLogoutOps.CheckIfAllow);
        }

        private void Menu_Exit()
        {
            // If menu item is disabled(but can't effect Input Gesture, so intercept here).
            if (!win.Menu_exit.IsEnabled)
            {
                return;
            }

            App.ManualExit();
        }

        private void Menu_Refresh()
        {
            DoRefresh();
            ResetCurrentSelectedFile();
        }

        private void Menu_SortByFileName()
        {
            ListViewBehavior.DoSortByClickMenu(win.fileList, win.ColumnHeader_Name);
        }

        private void Menu_SortByLastModified()
        {
            ListViewBehavior.DoSortByClickMenu(win.fileList, win.ColumnHeader_DateModified);
        }

        private void Menu_SortByFileSize()
        {
            ListViewBehavior.DoSortByClickMenu(win.fileList, win.ColumnHeader_Size);
        }

        // Prefrence Menu
        private void Menu_Prefrences()
        {
            App.Mediator.OnShowPreference();
        }

        // Help Menu
        private void Menu_GettingStarted()
        {

        }

        private void Menu_Help()
        {
            App.OnShowAppHelpInformation();
        }

        private void Menu_CheckForUpdates()
        {
            Window checkWin = new CheckForUpdates();
            checkWin.Owner = this.win;
            checkWin.ShowDialog();
        }

        private void Menu_ReportIssue()
        {
            App.Mediator.OnShowFeedBack(win);
        }

        private void Menu_AboutSkyDrm()
        {
            App.Mediator.OnShowAboutTheProject(win);
        }

        #endregion // Menu Bar


        #region Tool Bar
        private bool OpenFileDialog(out string[] selectedFile, bool isMultiselect = true)
        {
            selectedFile = null;
            string fileName;

            try
            {
                // --- Also can use System.Windows.Forms.FolderBrowserDialog!
                Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
                dialog.Title = "Select a File";

                if (!Directory.Exists(selectFilePath))
                {
                    // Get system desktop dir.
                    selectFilePath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory); ;
                }
                dialog.InitialDirectory = selectFilePath; // set init Dir.

                // all files
                dialog.Filter = "All Files|*.*";
                dialog.Multiselect = isMultiselect;

                if (dialog.ShowDialog() == true) // when user click ok button
                {
                    fileName = dialog.SafeFileName;
                    selectedFile = dialog.FileNames;

                    //Save select File path
                    selectFilePath = selectedFile[0].Substring(0, selectedFile[0].LastIndexOf(Path.DirectorySeparatorChar) + 1);

                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                App.Log.Warn("Exception in OpenFileDialog," + e, e);
                return false;
            }
        }

        private void DoProtect()
        {
            try
            {
                App.Mediator.OnShowOperationProtectWin(new string[0], GetFileRepos(), CurrentSaveFilePath, win, FileFromSource.SkyDRM_Window_Button);
            }
            catch (Exception e)
            {
                App.Log.Warn("Exception in DoProtect," + e, e);
            }

        }

        private void DoShare()
        {
            try
            {
                string[] selectedFile;
                if (OpenFileDialog(out selectedFile))
                {
                    App.Mediator.OnShowOperationShareWinByMainWin(selectedFile, this.win, FileFromSource.SkyDRM_Window_Button);
                }

            }
            catch (Exception e)
            {
                App.Log.Warn("Exception in DoShare," + e, e);
            }

        }

        private void DoAddNxl()
        {
            App.Mediator.OnShowOperationAddNxlWinByMainWin(GetFileRepos(), CurrentSaveFilePath, this.win, FileFromSource.SkyDRM_Window_Button);
        }

        private void DoUploadFile()
        {
            try
            {
                string[] selectedFile;
                if (OpenFileDialog(out selectedFile))
                {
                    // Test external repo upload feature
                    if(CurrentWorkingArea == EnumCurrentWorkingArea.EXTERNAL_REPO_ROOT)
                    {
                        TestUpload(selectedFile[0]);
                        return;
                    }

                    
                    App.Mediator.OnShowOperationUploadWinByMainWin(selectedFile, CurrentSaveFilePath, win);

                }
            }
            catch (Exception e)
            {
                App.Log.Warn("Exception in DoUploadFile," + e, e);
            }
        }

        private void DoOpenWeb()
        {
            App.OpenSkyDrmWeb();
        }

        private void DoUpload()
        {
            // Popup dialog if the network is not available
            if (!IsNetworkAvailable)
            {
                CustomMessageBoxWindow.Show(CultureStringInfo.CheckConnection_DlgBox_Title,
               CultureStringInfo.CheckConnection_DlgBox_Subject,
               CultureStringInfo.CheckConnection_DlgBox_Details,
               CustomMessageBoxWindow.CustomMessageBoxIcon.Question,
               CustomMessageBoxWindow.CustomMessageBoxButton.BTN_CLOSE);

                return;
            }

            IsStartUpload = true;
            UploadManagerEx.GetInstance().SetUploadKey(true);
            TryToUpload();

        }

        private void StopUpload()
        {
            IsStartUpload = false;
            UploadManagerEx.GetInstance().SetUploadKey(false);
            UploadManagerEx.GetInstance().StopUpload();
        }

        private void OpenSettingBtnMenu()
        {
            win.settingBtnCtMenu.IsOpen = true;
            win.settingBtnCtMenu.PlacementTarget = win.settingBtn;
        }

        /// <summary>
        ///  Refresh current working folder.
        /// </summary>
        /// <param name="isSort"> If do sorting in default when refresh. </param>
        public void DoRefresh(bool isSort = true)
        {
            Console.WriteLine("=== refresh ===");
            try
            {
                if (currentTabItem == EnumMainWinTabItems.ALL_FILES)
                {
                    RefreshTabItem_AllFiles();
                }
                else if (currentTabItem == EnumMainWinTabItems.SHARED_BY_ME)
                {
                    RefreshTabItem_SharedByMe();
                }
                else if (currentTabItem == EnumMainWinTabItems.SHARED_WITH_ME)
                {
                    RefreshTabItem_SharedWithMe();
                }
            }
            catch (Exception e)
            {
                App.Log.Warn("Exception in DoRefresh," + e, e);
            }

        }

        // For refresh, we'll refactor later, and will make it one independant module,
        // and optimize lots of repetitive code.
        private void RefreshTabItem_AllFiles(bool isSort = true)
        {
            switch (currentWorkingArea)
            {
                //
                // Handle some special area
                //
                case EnumCurrentWorkingArea.HOME:
                    // update myvault, mydrive, shareWithme file count
                    AsyncHelper.RunAsync(() =>
                    {
                        VaultFileCount = myVaultRepo.GetFilePool().Count;
                        SharedWithMeFileCount = myVaultRepo.GetSharedWithMeFiles().Count;

                        int dvFileCount = 0;
                        // update MySpace drive file count should not use 'FilePool', 
                        // because the 'FilePool' has updated when the root node is refreshed, the 'FilePool' only has the first level data
                        GetFilesCountRecursively(myDriveRepo.GetAllData(), ref dvFileCount);
                        return dvFileCount;
                    }, (rt) =>
                    {
                        DriveFileCount = rt;
                        // update myspace file count
                        MySpaceFileCount = VaultFileCount + DriveFileCount + SharedWithMeFileCount;
                    });

                    // update WorkSpace file count
                    AsyncHelper.RunAsync(() =>
                    {
                        int workSpFileCount = 0;
                        // update WorkSpace file count should not use 'FilePool', 
                        // because the 'FilePool' has updated when the root node is refreshed, the 'FilePool' only has the first level data
                        GetFilesCountRecursively(workSpaceRepo.GetAllData(), ref workSpFileCount);
                        return workSpFileCount;
                    }, (rt) =>
                    {
                        WorkSpaceFileCount = rt;
                    });
                    break;
                case EnumCurrentWorkingArea.MYSPACE:
                    // update myvault, mydrive, shareWithme file count
                    AsyncHelper.RunAsync(() =>
                    {
                        VaultFileCount = myVaultRepo.GetFilePool().Count;
                        SharedWithMeFileCount = myVaultRepo.GetSharedWithMeFiles().Count;

                        int dvFileCount = 0;
                        // update MySpace drive file count should not use 'FilePool', 
                        // because the 'FilePool' has updated when the root node is refreshed, the 'FilePool' only has the first level data
                        GetFilesCountRecursively(myDriveRepo.GetAllData(), ref dvFileCount);
                        return dvFileCount;
                    }, (rt) =>
                    {
                        DriveFileCount = rt;
                        // update myspace file count
                        MySpaceFileCount = VaultFileCount + DriveFileCount + SharedWithMeFileCount;
                    });
                    break;
                // Separately handle the refresh of MyVault, to fix Bug 63672 - Share with email list update from second time
                case EnumCurrentWorkingArea.MYVAULT:
                    if (currentWorkRepo != null)
                    {
                        SetListView(currentWorkRepo.GetWorkingFolderFilesFromDB());

                        if (!IsNetworkAvailable)
                        {
                            return;
                        }

                        currentWorkRepo?.SyncFiles((bool bSuc, IList<INxlFile> results, string originalWorkingFlag) =>
                        {
                            if (bSuc)
                            {
                                SetListView(results);
                                DefaultSort();
                                // Should continue to do search if user is searching.
                                CheckSearchWhenRefresh();
                            }
                        }, CurrentWorkingDirectoryFlag);
                    }
                    break;
                case EnumCurrentWorkingArea.SHARED_WITH_ME:
                    SetListView(myVaultRepo.GetSharedWithMeFiles());
                    if (!IsNetworkAvailable)
                    {
                        return;
                    }
                    myVaultRepo?.SyncSharedWithMeFiles((bool bSuc, IList<INxlFile> results) => {
                        if (bSuc)
                        {
                            // after refresh
                            SetListView(results);
                            DefaultSort();
                            CheckSearchWhenRefresh();
                        }
                    });
                    break;

                case EnumCurrentWorkingArea.FILTERS_OFFLINE:
                    AllOfflineClick();
                    break;

                case EnumCurrentWorkingArea.FILTERS_OUTBOX:
                    ManualRefreshOutBox();
                    break;

                // Used to refresh project pages list when user click "refresh button"
                case EnumCurrentWorkingArea.PROJECT:
                    // fix bug 55730, but not contain waitUpload file.
                    RefreshProject();
                    break;

                // Used to refresh repository list.
                case EnumCurrentWorkingArea.EXTERNAL_REPO:
                    RefreshExternalRepo();
                    break;

                default:
                    //
                    // Refresh other repo working folder.
                    //
                    if(currentWorkRepo != null)
                    {
                        SetListView(currentWorkRepo.GetWorkingFolderFilesFromDB());
                        RefreshWorkingFolder(isSort);
                    }
                    break;
            }

            // before refresh
            DefaultSort();
            CheckSearchWhenRefresh();
        }

        private void RefreshTabItem_SharedWithMe()
        {
            // Display local first before refresh
            SetListView(currentWorkRepo.GetSharedWithMeFiles());
            DefaultSort();
            CheckSearchWhenRefresh();

            // Record the fields before refresh in order to compare with the ones after refresh.
            var area = CurrentWorkingArea;
            var tabItem = currentTabItem;

            // At the same time, do refresh from server.
            currentWorkRepo?.SyncSharedWithMeFiles((bool bSuc, IList<INxlFile> results) => {
                if (bSuc && area == CurrentWorkingArea && tabItem == currentTabItem)
                {
                    // after refresh
                    ListOperateHelper.MergeListView(results, nxlFileList, copyFileList);
                    DefaultSort();
                    CheckSearchWhenRefresh();
                }
            });
        }

        private void RefreshTabItem_SharedByMe()
        {
            // before refresh
            SetListView(currentWorkRepo.GetSharedByMeFiles());
            DefaultSort();
            CheckSearchWhenRefresh();

            // Record the fields before refresh in order to compare with the ones after refresh.
            var area = CurrentWorkingArea;
            var tabItem = currentTabItem;

            currentWorkRepo?.SyncSharedByMeFiles((bool bSuc, IList<INxlFile> results) => {
                if (bSuc && area == CurrentWorkingArea && tabItem == currentTabItem)
                {
                    // after refresh
                    ListOperateHelper.MergeListView(results, nxlFileList, copyFileList);
                    DefaultSort();
                    CheckSearchWhenRefresh();
                }
            });
        }

        // Check if user is also doing search when refreshing, if yes, should still keeping search status.
        private void CheckSearchWhenRefresh()
        {
            // means doing search
            if (!string.IsNullOrEmpty(searchText))
            {
                InnerSearch(searchText);
            }
        }

        private void RefreshWorkingFolder(bool isSort)
        {
            if (!IsNetworkAvailable)
            {
                return;
            }

            var area = CurrentWorkingArea;
            var tabItem = currentTabItem;

            currentWorkRepo?.SyncFiles((bool bSuc, IList<INxlFile> results, string originalWorkingFlag) =>
            {
                if (bSuc)
                {
                    if (CurrentWorkingArea == area 
                    && tabItem == currentTabItem
                    && originalWorkingFlag == CurrentWorkingDirectoryFlag)
                    {
                        UpdateView(results, isSort);

                        // Should continue to do search if user is searching.
                        CheckSearchWhenRefresh();
                    }
                }
            }, CurrentWorkingDirectoryFlag);

        }

        private void RefreshExternalRepo()
        {
            if (!IsNetworkAvailable)
                return;

            try
            {
                AsyncHelper.RunAsync(()=> 
                {
                    bool bSucceed = true;

                    List<IRmsRepo> ret = new List<IRmsRepo>();
                    try
                    {
                        // sync repos from rms
                        ret = SkydrmApp.Singleton.RmsRepoMgr.SyncRepositories();
                    }
                    catch (Exception e)
                    {
                        App.Log.Error("Invoke SyncRepositories failed", e);

                        bSucceed = false;
                    }

                    return new RefreshRepositoriesInfo(bSucceed, ret);
                }, 
                (rt)=> 
                {
                    RefreshRepositoriesInfo rtValue = (RefreshRepositoriesInfo)rt;
                    if (!rtValue.IsSuc)
                    {
                        return;
                    }

                    App.Log.Info("RefreshExternalRepo: result count " + rtValue.Results.Count);

                    AddExternalRepositories(rtValue.Results);

                    // Update TreeView 
                    List<IFileRepo> addList = new List<IFileRepo>();
                    List<IFileRepo> removeList = new List<IFileRepo>();
                    RepoViewModel.GetRootViewModelByName(REPOSITORIES)?.MergeExternalRepoTreeView(rtValue.Results, addList, removeList);

                    App.Log.Info("RefreshExternalRepo: addExternalRepo count " + addList.Count + ", removeExternalRepo count " + removeList.Count);
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void RefreshProject()
        {
            if (IsNetworkAvailable)
            {
                projectRepo.SyncAllRemoteData((bool bSuccess, IList<ProjectData> result) =>
                {
                    if (bSuccess && CurrentWorkingArea == EnumCurrentWorkingArea.PROJECT && result.Count > 0)
                    {
                        App.Log.Info("RefreshProject: result count " + result.Count);
                        // Display again after sync.
                        LoadProjectPageData(result.ToList());

                        // Update TreeView 
                        List<ProjectData> addProject = new List<ProjectData>();
                        List<ProjectData> removeProject = new List<ProjectData>();
                        RepoViewModel.GetRootViewModelByName(PROJECT)?.MergeProjectTreeView(result, addProject, removeProject);
                        App.Log.Info("RefreshProject: addProject count " + addProject.Count + ", removeProject count " + removeProject.Count);
                    }
                }, false);
            }
        }

        private void UpdateListView(IList<INxlFile> syncResults, bool isSort, bool isHandleLeaveCopy = false)
        {
            ListOperateHelper.MergeListView(syncResults, nxlFileList, copyFileList);

            if (isHandleLeaveCopy)
            {
                ListOperateHelper.MergeLeaveAcopyFile(syncResults, nxlFileList, copyFileList);
            }

            if (isSort)
            {
                DefaultSort();
            }

        }

        /// <summary>
        /// Update listView when treeView refresh, which include heartbeat auto refresh and user manual expand/collapse treeview.
        /// Used for repo that contains folder.
        /// </summary>
        /// <param name="repoName"></param>
        /// <param name="syncResults"></param>
        /// <param name="workingDirFlag">Repo working directory flag, constructed by repoId and pathid generally.</param>
        private void UpdateFileListWhenRefreshTreeview(string repoName, IList<INxlFile> syncResults, string workingDirFlag)
        {
            Console.WriteLine("--> UpdateFileListWhenRefreshTreeview");
            if (repoName == currentWorkRepo?.RepoDisplayName 
                && workingDirFlag == CurrentWorkingDirectoryFlag
                && currentTabItem == EnumMainWinTabItems.ALL_FILES)
            {
                Console.WriteLine("--> UpdateFileListWhenRefreshTreeview-->Excute Merge ListView");
                IList<INxlFile> added = null;
                IList<INxlFile> removed = null;
                ListOperateHelper.MergeListView(syncResults, nxlFileList, copyFileList, out added, out removed);
                DefaultSort();
                CheckSearchWhenRefresh();
            }
        }

        /// <summary>
        /// Update lisview and do sort after refresh, and if find have folder change,
        /// also will update the corresponding treeview node.
        /// </summary>
        private void UpdateView(IList<INxlFile> syncResults, bool isSort)
        {
            IList<INxlFile> added = null;
            IList<INxlFile> removed = null;
            ListOperateHelper.MergeListView(syncResults, nxlFileList, copyFileList, out added, out removed);

            if (isSort)
            {
                DefaultSort();
            }

            // Update treeview item nodes.
            UpdateTreeviewFolder(added, removed);
        }

        /// <summary>
        /// Also should update coreresponding treeview folder nodes when find folder changes during update listview.
        /// </summary>
        private void UpdateTreeviewFolder(IList<INxlFile> added, IList<INxlFile> removed)
        {
            // For the new added folder, also should update the treeView, and also need to try to get its children if have.
            // -- Fix bug 52302

            // For remvoed folder, also need to remove from treeview.
            if (removed != null)
            {
                foreach (var one in removed)
                {
                    if (one.IsFolder)
                    {
                        NxlFolder folder = one as NxlFolder;
                        RemoveTreeviewNode(folder);
                    }
                }
            }

            if (added != null)
            {
                foreach (var one in added)
                {
                    if (one.IsFolder)
                    {
                        NxlFolder folder = one as NxlFolder;
                        AddTreeviewNode(folder);
                    }
                }
            }

        }

        private void RemoveTreeviewNode(NxlFolder folder)
        {
            currentTreeViewItem?.RemoveFolderItem(folder);
            App.Log.Info("Remove folder item: " + folder.Name);
        }

        private void AddTreeviewNode(NxlFolder folder)
        {
            // Add the new one into treeview.
            currentTreeViewItem?.AddFolderItemToCurrentItem(folder);
            App.Log.Info("Add folder item: " + folder.Name);

            // try to get the children of the new node if have.
            currentTreeViewItem?.TryToGetItemChildren(folder.PathId);
        }


        // The flag that indicates whether the files are partial refreshed in current working folder, 
        // if yes, will do refresh again.(Fix bug 63202 and the refresh issue of uploading many files in leave copy model)
        private bool bIsPartialRefresh = false;

        /// <summary>
        /// Refresh current listView after uploading leave a copy files.
        /// </summary>
        private void RefreshAfterLeaveCopyFileUpload(INxlFile uploadingFile)
        {
            // Is refreshing current working folder after uploaded.
            if (IsRefreshingAfterUpload)
            {
                bIsPartialRefresh = true;
                return;
            }

            switch (CurrentWorkingArea)
            {
                // Support 'Leave a copy file'(uploading protected file under 'Leave a copy file' mode).
                case EnumCurrentWorkingArea.WORKSPACE:
                case EnumCurrentWorkingArea.MYVAULT:
                case EnumCurrentWorkingArea.PROJECT_ROOT:
                case EnumCurrentWorkingArea.MYDRIVE:
                case EnumCurrentWorkingArea.EXTERNAL_REPO_ROOT:
                    InnerRefreshAfterLeaveCopyFileUpload(uploadingFile);
                    break;

                default:
                    IsRefreshingAfterUpload = false;
                    break;
            }
        }
        private void InnerRefreshAfterLeaveCopyFileUpload(INxlFile uploadingFile)
        {
            if (!IsNetworkAvailable)
            {
                return;
            }

            var area = CurrentWorkingArea;

            IsRefreshingAfterUpload = true;
            bIsPartialRefresh = false;

            currentWorkRepo.SyncFiles((bool bSuc, IList<INxlFile> results, string originalWorkingFlag) =>
            {
                IsRefreshingAfterUpload = false;

                if (bSuc)
                {
                    if (CurrentWorkingArea == area && originalWorkingFlag == CurrentWorkingDirectoryFlag)
                    {
                        // fix bug 64444, In MyVault, the DateModifyTime of the uploading file is different with RMS file, so the RMS file 
                        // will be added to the NxlFileList and should remove uploading file.
                        // And in Project\WorkSpace, the DateModifyTime of the uploading file is same as the RMS file, so the RMS file 
                        // will not be added to the NxlFileList and should not remove uploading file.
                        if (CurrentWorkingArea == EnumCurrentWorkingArea.MYVAULT)
                        {
                            RemoveFromListView(uploadingFile);
                        }

                        UpdateListView(results, true, true);
                        // Should continue to do search if user is searching.
                        CheckSearchWhenRefresh();
                    }
                }
                else
                {
                    // Should also update the ui node status if Sync failed(or else, file is still uploading status)
                    uploadingFile.Location = EnumFileLocation.Local;
                    uploadingFile.FileStatus = EnumNxlFileStatus.CachedFile;
                }

                // Fix the refresh issue after uploading complete for protecting multiple files in "Leave copy" model. 
                if (bIsPartialRefresh)
                {
                    DoRefresh();
                    bIsPartialRefresh = false;
                }
            },
            CurrentWorkingDirectoryFlag);
        }

        private static object obj = new object(); 
        /// <summary>
        /// Refresh upload file repo when working are is 'All Offline file'
        /// </summary>
        private void RefreshFileRepoAfterLeaveCopyFileUpload(INxlFile uploadingFile)
        {
            lock (obj)
            {
                var repo = GetRepoByNxlFile(uploadingFile);

                IList<INxlFile> results = new List<INxlFile>();

                repo?.SyncParentNodeFile(uploadingFile, ref results);

                foreach (var item in results)
                {
                    if (item.Name.Equals(uploadingFile.Name))
                    {
                        // update ui
                        App.Dispatcher.Invoke((Action)delegate
                        {
                            AddToListView(item);
                        });
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Refresh current working folder after upload or remove local file.
        /// </summary>
        private void RefreshAfterUploadOrRemove(INxlFile file, bool isUpdateList = false, bool isManualRemove = false)
        {
            if (CurrentWorkingArea == EnumCurrentWorkingArea.HOME
                || CurrentWorkingArea == EnumCurrentWorkingArea.MYSPACE
                || CurrentWorkingArea == EnumCurrentWorkingArea.PROJECT
                || CurrentWorkingArea == EnumCurrentWorkingArea.EXTERNAL_REPO
                || CurrentWorkingArea == EnumCurrentWorkingArea.SHARED_WITH_ME
                || CurrentWorkingArea == EnumCurrentWorkingArea.FILTERS_OFFLINE
                || CurrentWorkingArea == EnumCurrentWorkingArea.FILTERS_OUTBOX)
            {
                return;
            }

            // Is refreshing current working folder after uploaded.
            if (IsRefreshingAfterUpload)
            {
                bIsPartialRefresh = true;
                return;
            }

            InnerRefreshAfterUploadOrRemove(file, currentWorkingArea, isUpdateList, isManualRemove);
        }


        private void InnerRefreshAfterUploadOrRemove(INxlFile fileToDelete, EnumCurrentWorkingArea curWorkArea, 
            bool isUpdateList = false, bool isManualRemove = false)
        {
            if (IsNetworkAvailable)
            {
                IsRefreshingAfterUpload = true;
                bIsPartialRefresh = false;

                currentWorkRepo?.SyncFiles((bool bSuccess, IList<INxlFile> results, string originalWorkingFlag) =>
                {
                    IsRefreshingAfterUpload = false;

                    if (!isManualRemove)
                    {
                        RemoveFromListView(fileToDelete);
                    }

                    // May current treeview item has been switched into others before sync complete.
                    if (bSuccess && isUpdateList 
                       && CurrentWorkingArea == curWorkArea
                       && originalWorkingFlag == CurrentWorkingDirectoryFlag
                       // Exclude "SharedWithMe" and "SharedByMe" tab item. (Fix bug 61486)
                       && currentTabItem == EnumMainWinTabItems.ALL_FILES)
                    {
                        UpdateListView(results, true);
                    }

                    if (bIsPartialRefresh)
                    {
                        DoRefresh();
                        bIsPartialRefresh = false;
                    }

                }, CurrentWorkingDirectoryFlag);
            }
            else
            {
                // Also need to get remote node when user manual remove "leave a copy" file in offline mode(fix bug 53838).
                if (isManualRemove)
                {
                    SetListView(currentWorkRepo?.GetWorkingFolderFilesFromDB());
                    DefaultSort();
                }
            }
        }


        private void UploadComplete(object result)
        {
            try
            {
                UploadResult ur = (UploadResult)result;
                INxlFile uploadingFile = ur.UploadingFile;

                if (ur.bUploadSuccess)
                {
                    App.Log.Info("********** Upload succeed! ********** " + ur.UploadingFile.Name);

                    // remaing 200ms, then remove it.
                    Thread.Sleep(200);

                    OnUploadSucceed(ur);
                }
                else // failed
                {
                    App.Log.Info("********** Upload Failed! ********** " + ur.UploadingFile.Name);

                    OnUploadFailed(ur);
                }

            }
            catch (Exception e)
            {
                App.Log.Error("Error happen in UploadComplete:" + e.Message, e);
            }

        }

        private void OnUploadSucceed(UploadResult ur)
        {
            if (SkydrmApp.Singleton.User.LeaveCopy && ur.UploadingFile.FileRepo != EnumFileRepo.REPO_MYDRIVE)
            {
                // For edited file upload complete in "Leave a copy" mode
                if (ur.UploadingFile.IsEdit)
                {
                    // Should restore original file status after uploading.
                    ur.UploadingFile.Location = EnumFileLocation.Local;
                    ur.UploadingFile.FileStatus = EnumNxlFileStatus.CachedFile;
                    // reset
                    ur.UploadingFile.IsEdit = false;

                    SyncEditedNode(ur.UploadingFile);
                }

                // For waitingUpload file upload complete in "Leave a copy" mode
                else
                {
                    HandleLeaveACopy(ur.UploadingFile);

                    // Notify msg
                    App.MessageNotify.NotifyMsg(ur.UploadingFile.Name, CultureStringInfo.ApplicationFindResource("Notify_RecordLog_Upload_Succeed"), EnumMsgNotifyType.LogMsg,
                      MsgNotifyOperation.UPLOAD, EnumMsgNotifyResult.Succeed, EnumMsgNotifyIcon.Offline);

                }
            }
            else
            {
                // For edited file upload complete.
                if (ur.UploadingFile.IsEdit)
                {
                    // Should restore original file status after uploading.
                    ur.UploadingFile.Location = EnumFileLocation.Local;
                    ur.UploadingFile.FileStatus = EnumNxlFileStatus.AvailableOffline;
                    // reset
                    ur.UploadingFile.IsEdit = false;

                    SyncEditedNode(ur.UploadingFile);
                }

                // For waitingUpload file upload complete.
                else
                {
                    AutoRemove(ur.UploadingFile);

                    // Must notify this after after 'AutoRemove' operation.
                    App.MessageNotify.NotifyMsg(ur.UploadingFile.Name, CultureStringInfo.ApplicationFindResource("Notify_RecordLog_Upload_Succeed"), EnumMsgNotifyType.LogMsg,
                        MsgNotifyOperation.UPLOAD, EnumMsgNotifyResult.Succeed, EnumMsgNotifyIcon.Online);
                }
            }
        }

        /// <summary>
        /// Now when file upload failed, the file also will be out of the queue, and will be labeled in "Outbox" filter,
        /// then user can try to restore upload manually by right context menu or remove it. 
        /// </summary>
        private void OnUploadFailed(UploadResult ur)
        {
            ur.UploadingFile.FileStatus = EnumNxlFileStatus.UploadFailed;

            if (ur.UploadingFile.IsEdit)
            {
                // update current listview the corresponding file status if needed.
                SyncCurrentListViewFileStatus(ur.UploadingFile, EnumNxlFileStatus.AvailableOffline);
            }
            else
            {
                // update current listview the corresponding file status if needed.
                SyncCurrentListViewFileStatus(ur.UploadingFile, EnumNxlFileStatus.UploadFailed);

                UpdateNxlFileListItemValue(ur.UploadingFile);
            }

            // Should sort after updating listView. (fix bug 63506)
            win.Dispatcher.Invoke(() =>
            {
                DefaultSort();
            });
        }

        /// <summary>
        /// Because upload nxlFile object not equal nxlFileList object, and when upload file failed maybe will change some property
        /// in featureProvider level. so need update nxlFileList object property
        /// </summary>
        /// <param name="nxlFile"></param>
        private void UpdateNxlFileListItemValue(INxlFile nxlFile)
        {
            if (nxlFile == null || string.IsNullOrEmpty(nxlFile.Name))
            {
                return;
            }

            foreach (var one in nxlFileList)
            {
                if (one.Equals(nxlFile)
                    && one is PendingUploadFile
                    && nxlFile is PendingUploadFile)
                {
                    (one as PendingUploadFile).Raw = (nxlFile as PendingUploadFile).Raw;
                    break;
                }
            }

            foreach (var one in copyFileList)
            {
                if (one.Equals(nxlFile)
                    && one is PendingUploadFile
                    && nxlFile is PendingUploadFile)
                {
                    (one as PendingUploadFile).Raw = (nxlFile as PendingUploadFile).Raw;
                    break;
                }
            }
        }

        // Refresh to get the new node after uploading. Fixed bug 54428
        private void SyncEditedNode(INxlFile nxlFile)
        {
            GetRepoByNxlFile(nxlFile)?.SyncDestFile(nxlFile, (bool bSuccess, INxlFile updatedFile) =>
            {
                if (bSuccess && updatedFile != null && updatedFile.IsMarkedFileRemoteModified)
                {
                    UpdateEditedItemUI(updatedFile);
                }
            },
            true);
        }

        private void UpdateEditedItemUI(INxlFile updatedFile)
        {
            // reset
            updatedFile.IsMarkedFileRemoteModified = false;

            // User do edit project/workspace/sharedWorkSpace file from "offline" filter.
            if (currentWorkingArea == EnumCurrentWorkingArea.FILTERS_OFFLINE
                && (updatedFile.FileRepo == EnumFileRepo.REPO_PROJECT
                || updatedFile.FileRepo == EnumFileRepo.REPO_WORKSPACE
                || updatedFile.FileRepo == EnumFileRepo.REPO_EXTERNAL_DRIVE))
            {
                GetRepoByNxlFile(updatedFile)?.SyncDestFile(updatedFile, (bool bSuccess, INxlFile newNode) =>
                {
                    if (bSuccess)
                    {
                        NotifyUpdateItem(newNode);
                    }
                },
                true);

            }
            else
            {
                currentWorkRepo.SyncDestFile(updatedFile, (bool bSuccess, INxlFile newNode) =>
                {
                    if (bSuccess)
                    {
                        NotifyUpdateItem(newNode);
                    }
                },
                false);

            }

        }

        private void NotifyUpdateItem(INxlFile newNode)
        {
            if (newNode != null)
            {
                // update this list item ui about the file node.
                win.Dispatcher.Invoke(() =>
                {
                    UpdateModifiedItem(newNode);
                });
            }
        }


        private void HandleLeaveACopy(INxlFile uploadingFile)
        {
            // For "Leave a copy", don't need to Auto remove after uploading, 
            // but if current working folder is "Outbox filter", also need to remove from listview after complete uploading.
            if (CurrentWorkingArea == EnumCurrentWorkingArea.FILTERS_OUTBOX)
            {
                RemoveFromListView(uploadingFile);
            }

            // If current working folder is "Offline filter", also need to add into listview after complete uploading.
            else if (CurrentWorkingArea == EnumCurrentWorkingArea.FILTERS_OFFLINE)
            {
                RefreshFileRepoAfterLeaveCopyFileUpload(uploadingFile);
            }
            else
            {
                // For fix this bug -- that user protect some files then upload(in the setting of "Leave a Copy"), after upload, then 
                // switch into "Offline filter" to check the files, can't get them immediately, must click again.(Actually must call sycFile, 
                // then add one record for "LeaveACopy" in low level).
                App.Dispatcher.Invoke((Action)delegate
                {
                    RefreshAfterLeaveCopyFileUpload(uploadingFile);
                });
            }
        }


        // Handle the file remove by user manully.
        //  -- this must happen in current working folder.
        private void ManualRemove(INxlFile fileToDelete)
        {
            if (fileToDelete == null)
            {
                return;
            }

            try
            {
                // popup prompt dialog.
                if (MsgBox.ShowRemoveDialog(fileToDelete) == CustomMessageBoxResult.Positive)
                {
                    //  common
                    bool bSuccess = true;
                    var status = fileToDelete.FileStatus;

                    try
                    {
                        fileToDelete.Remove();
                        fileToDelete.IsEdit = false;

                        // Tell service mgr
                        EnumMsgNotifyIcon notifyIcon = EnumMsgNotifyIcon.Online;
                        if (fileToDelete.FileStatus == EnumNxlFileStatus.WaitingUpload ||
                            fileToDelete.FileStatus == EnumNxlFileStatus.UploadFailed)
                        {
                            notifyIcon = EnumMsgNotifyIcon.WaitingUpload;
                        }
                        if (fileToDelete.FileStatus == EnumNxlFileStatus.AvailableOffline ||
                            fileToDelete.FileStatus == EnumNxlFileStatus.CachedFile)
                        {
                            notifyIcon = EnumMsgNotifyIcon.Offline;
                        }
                        App.MessageNotify.NotifyMsg(fileToDelete.Name, CultureStringInfo.ApplicationFindResource("Notify_RecordLog_Remove_Succeed"), EnumMsgNotifyType.LogMsg,
                                             MsgNotifyOperation.REMOVE, EnumMsgNotifyResult.Succeed, notifyIcon);
                    }
                    catch (Exception e)
                    {
                        bSuccess = false;
                        App.Log.Info("********** Remove file failed manually! **********", e);
                    }
                    finally
                    {
                        if (bSuccess)
                        {
                            RemoveFromListView(fileToDelete);

                            //  Also need to remove from waiting queue if exist.
                            UploadManagerEx.GetInstance().RemoveFromQueue(fileToDelete);

                            // fix bug 65124 Use UnMark item instead of delete item to remove Cachedfile, the following code will be deleted later.
                            // ->
                            // Should refresh when user remove a cached file to get remote one(For "Offline filter", don't need to refresh.)
                            if (status == EnumNxlFileStatus.CachedFile)
                            {
                                if (CurrentWorkingArea != EnumCurrentWorkingArea.FILTERS_OFFLINE)
                                {
                                    if(currentTabItem == EnumMainWinTabItems.SHARED_BY_ME) // Fix bug 63716
                                    {
                                        RefreshTabItem_SharedByMe();
                                    }
                                    else
                                    {
                                        RefreshAfterUploadOrRemove(fileToDelete, true, true);
                                    }
                                }
                            }

                        }
                    }

                }
            }
            catch (Exception e)
            {
                App.Log.Warn("Exception in ManualRemove," + e, e);
            }
        }

        // Handle auto remove after uploading
        private void AutoRemove(INxlFile fileToDelete)
        {
            //  common
            bool bSuccess = true;
            try
            {
                fileToDelete.Remove();
            }
            catch (Exception e)
            {
                bSuccess = false;
                App.Log.Info("********** Remove file failed! **********", e);
            }
            finally
            {
                if (bSuccess)
                {
                    // refresh to get remote new one.
                    App.Dispatcher.Invoke((Action)delegate
                    {
                        if (CurrentWorkingArea == EnumCurrentWorkingArea.FILTERS_OUTBOX)
                        {
                            RemoveFromListView(fileToDelete);
                        }
                        else
                        {
                            RefreshAfterUploadOrRemove(fileToDelete, true, false);
                        }
                    });

                }
            }

        }

        private void RemoveFromListView(INxlFile fileToDelete)
        {
            App.Dispatcher.Invoke((Action)delegate
            {
                // Fix bug 64688 Retry upload and replace in myvault,file is always in the status of being uploaded
                bool r1 = ListOperateHelper.RemoveListFileByDateTime(nxlFileList, fileToDelete);
                bool r2 = ListOperateHelper.RemoveListFileByDateTime(copyFileList, fileToDelete);

                // May are different INxlFile objects when re-get from local db.
                if (!r1 || !r2)
                {
                    ListOperateHelper.RemoveListFile(nxlFileList, copyFileList, fileToDelete);
                }

            });

            // If the nxl file removed is current selected file, must reset "CurrentSelectedFile" as null after remove.
            if (CurrentSelectedFile != null && fileToDelete == CurrentSelectedFile)
            {
                ResetCurrentSelectedFile();
            }
        }

        /// <summary>
        ///  Get created files
        /// </summary>
        /// <param name="newFiles">newly created files</param>
        public void GetCreatedFile(List<INxlFile> newFiles)
        {
            try
            {
                // Get new created files from db
                if (CurrentWorkingArea == EnumCurrentWorkingArea.FILTERS_OUTBOX)
                {
                    OutBoxClick();
                }
                else if (CurrentWorkingArea == EnumCurrentWorkingArea.FILTERS_OFFLINE)
                {
                    AllOfflineClick();
                }
                else
                {
                    // Note: the file Object that added to listview is not the same Object with the one that added to waiting queue.
                    if(currentTabItem == EnumMainWinTabItems.ALL_FILES)
                    {
                        SetListView(currentWorkRepo?.GetWorkingFolderFilesFromDB());
                        DefaultSort();
                    }
                }

                foreach (var item in newFiles)
                {
                    UploadManagerEx.GetInstance().AddToWaitingQueue(item);
                }

                if (App.User.StartUpload)
                {
                    TryToUpload();
                }

            }
            catch (Exception e)
            {
                App.Log.Warn("exception in GetCreatedFile, e:" + e, e);
            }
        }

        private void DoMySpaceNavigate()
        {
            DbClickHomePageNavigateTV(MY_SPACE);
        }

        private void DoWorkSpaceNavigate()
        {
            DbClickHomePageNavigateTV(WORKSPACE);
        }

        private void DoMyDriveNavigate()
        {
            DbClickMySpacePageNavigateTV(MY_DRIVE);
        }

        private void DoMyVaultNavigate()
        {
            DbClickMySpacePageNavigateTV(MY_VAULT);
        }

        private void DoShareWithMeNavigate()
        {
            DbClickMySpacePageNavigateTV(SHARE_WITH_ME);
        }

        #region TreeView Navigate

        /// <summary>
        /// When double click folder in fileList, navigate treeView folder item 
        /// </summary>
        /// <param name="Children"></param>
        /// <param name="destPathId"></param>
        /// <returns></returns>
        private bool TreeviewFolderNavigate(ObservableCollection<TreeViewItemViewModel> Children, string destPathId)
        {
            bool res = false;
            foreach (var childrenItem in Children)
            {
                if (childrenItem is FolderViewModel)
                {
                    FolderViewModel folderView = childrenItem as FolderViewModel;
                    if (string.Equals(destPathId, folderView.FolderPathId, StringComparison.CurrentCultureIgnoreCase))
                    {
                        folderView.IsExpanded = true; // will trigger TreeView LoadChildren() get really data
                        folderView.IsSelected = true; // will trigger "TreeViewItemChanged"
                        res = true;
                        break;
                    }
                }
            }
            return res;
        }

        /// <summary>
        /// Click Home page item(MySpace, WorkSpace) navigate treeView item
        /// </summary>
        private void DbClickHomePageNavigateTV(string repoName)
        {
            if (string.IsNullOrEmpty(repoName))
            {
                return;
            }

            foreach (var item in RepoViewModel.RootVMList) // RootViewModel
            {
                // MySpace -- since its treeview item is below 'MySpace', should handle with it specially.
                if (item.RootName.Equals(repoName, StringComparison.CurrentCultureIgnoreCase))
                {
                    item.IsExpanded = true; // will trigger TreeView LoadChildren() get really data
                    item.IsSelected = true; // will trigger "TreeViewItemChanged"

                    break;
                }
            }
        }

        /// <summary>
        /// Click MySpace page item(MyDrive, MyVault) navigate treeView item
        /// </summary>
        private void DbClickMySpacePageNavigateTV(string repoName)
        {
            if (string.IsNullOrEmpty(repoName))
            {
                return;
            }

            foreach (var item in RepoViewModel.RootVMList) // RootViewModel
            {
                // MySpace -- since its treeview item is below 'MySpace', should handle with it specially.
                if (item.RootName.Equals(MY_SPACE, StringComparison.CurrentCultureIgnoreCase))
                {
                    foreach (var one in item.Children)
                    {
                        var rootView = one as RootViewModel;
                        // myDrive or myVault
                        if (rootView.RootName.Equals(repoName, StringComparison.CurrentCultureIgnoreCase))
                        {
                            rootView.IsExpanded = true; // will trigger TreeView LoadChildren() get really data
                            rootView.IsSelected = true; // will trigger "TreeViewItemChanged"

                            break;
                        }
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Double click project list item navigate treeView item
        /// </summary>
        /// <param name="selectProject"></param>
        public void DbClickProjectListNavigateTV(ProjectData selectProject)
        {
            if (selectProject == null)
            {
                return;
            }

            foreach (var item in RepoViewModel.RootVMList) //RootViewModel
            {
                if (item.RootName.Equals(PROJECT,StringComparison.CurrentCultureIgnoreCase))
                {
                    RootViewModel rootView = item as RootViewModel;
                    rootView.IsExpanded = true;
                    rootView.IsSelected = true; 

                    ObservableCollection<TreeViewItemViewModel> Children = new ObservableCollection<TreeViewItemViewModel>();
                    Children = rootView.Children; // ProjectViewModel

                    foreach (var childrenItem in Children) // ProjectViewModel
                    {
                        if (childrenItem is ProjectViewModel)
                        {
                            ProjectViewModel projectView = childrenItem as ProjectViewModel;
                            if (projectView.ProjectName == selectProject.ProjectInfo.DisplayName
                                && projectView.Project.ProjectInfo.ProjectId == selectProject.ProjectInfo.ProjectId)
                            {
                                projectView.IsExpanded = true; // will trigger TreeView LoadChildren() get really data
                                projectView.IsSelected = true; // will trigger "TreeViewItemChanged"
                                break;
                            }
                        }

                    }
                }
            }
        }

        /// <summary>
        /// Double click external repo list item navigate treeView item
        /// </summary>
        /// <param name="selectProject"></param>
        public void DbClickExterRepoListNavigateTV(IFileRepo selectExternalRepo)
        {
            if (selectExternalRepo == null)
            {
                return;
            }

            foreach (var item in RepoViewModel.RootVMList) //RootViewModel
            {
                if (item.RootName.Equals(REPOSITORIES, StringComparison.CurrentCultureIgnoreCase))
                {
                    RootViewModel rootView = item as RootViewModel;
                    //rootView.IsExpanded = true;
                    //rootView.IsSelected = true;

                    ObservableCollection<TreeViewItemViewModel> Children = new ObservableCollection<TreeViewItemViewModel>();
                    Children = rootView.Children; // ProjectViewModel

                    foreach (var childrenItem in Children) // ProjectViewModel
                    {
                        if (childrenItem is RootViewModel)
                        {
                            RootViewModel rootVM = childrenItem as RootViewModel;
                            if (rootVM.RepoId.Equals(selectExternalRepo.RepoId, StringComparison.CurrentCultureIgnoreCase))
                            {
                                rootVM.IsExpanded = true; // will trigger TreeView LoadChildren() get really data
                                rootVM.IsSelected = true; // will trigger "TreeViewItemChanged"
                                break;
                            }
                        }

                    }
                }
            }
        }

        #endregion

        private void DoFilterLocalFiles(SelectionChangedEventArgs args)
        {}


        /// <summary>
        /// Since the UI binded lNxlFile, then change this will automatically refresh ui, but we should do one copy in copyFileList.
        /// </summary>
        /// <param name="args"></param>
        private void DoSearch(SearchEventArgs args)
        {
            InnerSearch(args.SearchText);
        }

        private void InnerSearch(string text)
        {
            nxlFileList.Clear();
            IsSearch = false;
            try
            {
                searchText = text;
                // search text is empty
                if (string.IsNullOrEmpty(searchText))
                {
                    foreach (INxlFile one in copyFileList)
                    {
                        nxlFileList.Add(one);
                    }

                    return;
                }

                // search text is not empty.
                foreach (INxlFile one in copyFileList)
                {
                    if (one.Name.ToLower().Contains(searchText.ToLower()))
                    {
                        nxlFileList.Add(one);
                    }
                }
                // If IsSearch is true and nxlFileList.count <1, will display SearchPromptText("No items match your search")
                // the nxlFileList.count will judege in ConvertersEx.cs
                IsSearch = true;

            }
            catch (Exception e)
            {
                App.Log.Warn("Exception in DoSearch," + e, e);
            }

            // the selected item will become non-selected when do search, so need to reset CurrentSelectedFile as null.
            ResetCurrentSelectedFile();
        }

        #endregion // Tool Bar


        #region Right Click Context Menu
        private void ContextMenu_Upload(INxlFile nxlFile)
        {
            // do re-upload
            App.Log.Info("Try to re-upload:"+ nxlFile.Name);

            UploadManagerEx.GetInstance().UploadSpecifiedFile(nxlFile);
        }

        private void ContextMenu_OverWriteUpload(INxlFile nxlFile)
        {
            // do re-upload
            App.Log.Info("Try to overwrite-upload:" + nxlFile.Name);

            UploadManagerEx.GetInstance().UploadSpecifiedFile(nxlFile, true);
        }

        private void ContextMenu_RenameUpload(INxlFile nxlFile)
        {
            // do re-upload
            App.Log.Info("Try to rename-upload:" + nxlFile.Name);

            string nxlName = nxlFile.Name;
            string firstExt = Path.GetExtension(nxlName); // return first extension

            string orgFileName = nxlName;
            string secExt = string.Empty;
            int lastIndex = nxlName.LastIndexOf('.');
            if (lastIndex != -1)
            {
                orgFileName = nxlName.Substring(0, lastIndex); // remove first extension
                secExt = Path.GetExtension(orgFileName); // get second extension .txt ... or null, empty
            }
            string orgFileNameNoExt = Path.GetFileNameWithoutExtension(orgFileName);

            int count = 1;
            string newFileName = string.Format("{0}({1}){2}{3}", orgFileNameNoExt, count, secExt, firstExt);

            if (CurrentWorkingArea != EnumCurrentWorkingArea.FILTERS_OUTBOX)
            {
                while (CheckCurrentWorkingFolderFileExist(nxlFile.FileRepo, newFileName))
                {
                    count++;
                    newFileName = string.Format("{0}({1}){2}{3}", orgFileNameNoExt, count, secExt, firstExt);
                }
            }

            IRenameFile renameFile = new RenameFile(nxlFile, newFileName);
            App.Mediator.OnShowRenameWin(renameFile, win);

            if (renameFile.RenameResult)
            {
                UploadManagerEx.GetInstance().UploadSpecifiedFile(nxlFile);
            }
        }

        private bool CheckCurrentWorkingFolderFileExist(EnumFileRepo fileRepo, string fileName)
        {
            bool result = false;
            IList<INxlFile> nxlFiles = new List<INxlFile>();

            switch (fileRepo)
            {
                case EnumFileRepo.UNKNOWN:
                    break;
                case EnumFileRepo.EXTERN:
                    break;
                case EnumFileRepo.REPO_MYVAULT:
                    nxlFiles = myVaultRepo.GetWorkingFolderFilesFromDB();
                    break;
                case EnumFileRepo.REPO_PROJECT:
                    nxlFiles = projectRepo.GetWorkingFolderFilesFromDB();
                    break;
                case EnumFileRepo.REPO_SHARED_WITH_ME:
                    break;
                case EnumFileRepo.REPO_WORKSPACE:
                    nxlFiles = workSpaceRepo.GetWorkingFolderFilesFromDB();
                    break;
                case EnumFileRepo.REPO_MYDRIVE:
                    nxlFiles = myDriveRepo.GetWorkingFolderFilesFromDB();
                    break;
                case EnumFileRepo.REPO_EXTERNAL_DRIVE:
                    break;
                default:
                    break;
            }
            result = nxlFiles.Any(f => f.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));
            return result;
        }

        public bool GetNxlFileSharedByMeteData(INxlFile nxlFile)
        {
            bool result = false;
            if (nxlFile.FileRepo == EnumFileRepo.REPO_MYVAULT)
            {
                if (nxlFile is MyVaultRmsDoc)
                {
                    var doc = (MyVaultRmsDoc)nxlFile;
                    var metData = doc.GetMetaData();
                    result = metData.isShared;
                }
            }
            return result;
        }

        private void ContextMenu_DoProtect(INxlFile nxlFile)
        {
            if (nxlFile.IsNxlFile)
            {
                return;
            }
            if (nxlFile.Location == EnumFileLocation.Online && !ViewerProcess.ContainsKey(nxlFile.PathId)) // online 
            {
                this.win.Cursor = Cursors.Wait;
                currentWorkRepo.DownloadFile(nxlFile, true, (bool result) =>
                {
                    this.win.Dispatcher.Invoke(() =>
                    {
                        this.win.Cursor = Cursors.Arrow;
                    });
                    if (result)
                    {
                        string[] normalFile = new string[1];
                        normalFile[0] = nxlFile.LocalPath;

                        CurrentSelectedSavePath selectedSavePath = new CurrentSelectedSavePath(MY_VAULT, "/", "SkyDRM://" + MY_SPACE);
                        if (nxlFile.FileRepo == EnumFileRepo.REPO_MYDRIVE)
                        {
                            selectedSavePath = new CurrentSelectedSavePath(MY_VAULT, "/", "SkyDRM://" + MY_SPACE);
                        }
                        else
                        {
                            selectedSavePath = CurrentSaveFilePath;
                        }
                        this.win.Dispatcher.Invoke(() =>
                        {
                            App.Mediator.OnShowOperationProtectWin(normalFile, GetFileRepos(), selectedSavePath, win, FileFromSource.SkyDRM_Window_FileContextMenu);
                        });
                    }
                    else
                    {
                        // todo, error handle
                        App.Log.Info("Download failed:" + nxlFile.Name);
                        App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Notify_PopBubble_Download_Failed"), false, nxlFile.Name, MsgNotifyOperation.PROTECT,
                     EnumMsgNotifyIcon.Online);
                    }

                }, false, true);
            }
            else
            {
                string[] normalFile = new string[1];
                normalFile[0] = nxlFile.LocalPath;

                CurrentSelectedSavePath selectedSavePath = new CurrentSelectedSavePath(MY_VAULT, "/", "SkyDRM://" + MY_SPACE);
                if (nxlFile.FileRepo == EnumFileRepo.REPO_MYDRIVE)
                {
                    selectedSavePath = new CurrentSelectedSavePath(MY_VAULT, "/", "SkyDRM://" + MY_SPACE);
                }
                else
                {
                    selectedSavePath = CurrentSaveFilePath;
                }

                if (selectedSavePath == null)
                {
                    selectedSavePath = new CurrentSelectedSavePath(MY_VAULT, "/", "SkyDRM://" + MY_SPACE);
                }

                App.Mediator.OnShowOperationProtectWin(normalFile, GetFileRepos(), selectedSavePath, win, FileFromSource.SkyDRM_Window_FileContextMenu);
            }
        }

        private void ContextMenu_DoShare(INxlFile nxlFile)
        {
            if (!nxlFile.IsNxlFile)
            {
                PlainFileDoShare(nxlFile);
                return;
            }

            if (nxlFile.FileRepo == EnumFileRepo.REPO_MYVAULT || nxlFile.FileRepo == EnumFileRepo.REPO_SHARED_WITH_ME)
            {
                MyVaultOrShareWithMeFileDoShare(nxlFile);
                return;
            }

            if (nxlFile.FileRepo == EnumFileRepo.REPO_PROJECT)
            {
                ProjectFileDoShare(nxlFile);
                return;
            }

        }
        private void PlainFileDoShare(INxlFile nxlFile)
        {
            if (nxlFile.IsNxlFile)
            {
                return;
            }

            if (nxlFile.Location == EnumFileLocation.Online && !ViewerProcess.ContainsKey(nxlFile.PathId)) // online 
            {
                this.win.Cursor = Cursors.Wait;
                currentWorkRepo.DownloadFile(nxlFile, true, (bool result) =>
                {
                    this.win.Dispatcher.Invoke(() =>
                    {
                        this.win.Cursor = Cursors.Arrow;
                    });
                    if (result)
                    {
                        string[] normalFile = new string[1];
                        normalFile[0] = nxlFile.LocalPath;
                        this.win.Dispatcher.Invoke(() =>
                        {
                            App.Mediator.OnShowOperationShareWinByMainWin(normalFile, this.win, FileFromSource.SkyDRM_Window_FileContextMenu);
                        });
                    }
                    else
                    {
                        // todo, error handle
                        App.Log.Info("Download failed:" + nxlFile.Name);
                        App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Notify_PopBubble_Download_Failed"), false, nxlFile.Name, MsgNotifyOperation.SHARE,
                     EnumMsgNotifyIcon.Online);
                    }

                }, false, true);
            }
            else
            {
                string[] normalFile = new string[1];
                normalFile[0] = nxlFile.LocalPath;
                App.Mediator.OnShowOperationShareWinByMainWin(normalFile, this.win, FileFromSource.SkyDRM_Window_FileContextMenu);
            }
        }
        private void MyVaultOrShareWithMeFileDoShare(INxlFile nxlFile)
        {
            if (nxlFile.FileRepo != EnumFileRepo.REPO_MYVAULT && nxlFile.FileRepo != EnumFileRepo.REPO_SHARED_WITH_ME)
            {
                return;
            }

            // Share online file when don't load viewer.
            if (nxlFile.Location == EnumFileLocation.Online && !ViewerProcess.ContainsKey(nxlFile.PathId)) // online 
            {
                App.Log.Info("File download, in Share NxlFile To Person");
                currentWorkRepo.DownloadFile(nxlFile, true, (bool result) =>
                {
                    this.win.Dispatcher.Invoke(() =>
                    {
                        if (result)
                        {
                            // Add nxl file
                            try
                            {
                                var fp = App.Rmsdk.User.GetNxlFileFingerPrint(nxlFile.LocalPath);

                                if (!(fp.isFromMyVault && fp.HasRight(FileRights.RIGHT_SHARE)))
                                {
                                    App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Common_Not_Authorized"), false, nxlFile.Name);
                                    operationWin?.Close();
                                    return;
                                }

                                string[] filePath = new string[1];
                                filePath[0] = nxlFile.LocalPath;
                                IBase update = new UpdateRecipient(fp, new OperateFileInfo(filePath, null, FileFromSource.SkyDRM_Window_FileContextMenu));

                                operationWin?.ChangeViewModel(update);
                            }
                            catch (Exception e)
                            {
                                App.Log.Error(e.Message, e);
                                App.ShowBalloonTip(e.Message, false, nxlFile.Name);
                                operationWin?.Close();
                            }
                        }
                        else
                        {
                            // todo, error handle
                            App.Log.Info("Download failed:" + nxlFile.Name);
                            App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Notify_PopBubble_Download_Failed"), false, nxlFile.Name, "Update Recipients",
                             EnumMsgNotifyIcon.Online);
                            operationWin?.Close();
                        }
                    });

                }, false, true);

                App.Mediator.OnShowOperationWin(out operationWin, this.win, "UpdateRecipients");
            }
            else
            {
                // 1. share online file when viewer is loading.
                // 2. share offline file
                App.Mediator.OnShowOperationUpdateRecipiWinByMainWin(nxlFile.LocalPath, this.win, FileFromSource.SkyDRM_Window_FileContextMenu);
            }
        }
        private void ProjectFileDoShare(INxlFile nxlFile)
        {
            if (nxlFile.FileRepo != EnumFileRepo.REPO_PROJECT)
            {
                return;
            }

            if (nxlFile.IsRevoked)
            {
                App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("ReShareUpOperation_RevokedFile"), false, nxlFile.Name, "",
                    nxlFile.IsMarkedOffline ? EnumMsgNotifyIcon.Offline : EnumMsgNotifyIcon.Online);
                return;
            }
            if (projectRepo.FilePool.Count == 1)
            {
                App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("ReShareOperation_No_OtherProjects"), false, nxlFile.Name, "",
                    nxlFile.IsMarkedOffline ? EnumMsgNotifyIcon.Offline : EnumMsgNotifyIcon.Online);
                return;
            }
            IBase operat = null;
            if (nxlFile.IsShared)
            {
                List<int> projectID = new List<int>();
                foreach (var item in nxlFile.SharedWith)
                {
                    projectID.Add(int.Parse(item));
                }
                operat = new ReShareUpdate(nxlFile, projectRepo.FilePool.ToList(), projectID);
            }
            else
            {
                operat = new ReShare(nxlFile, projectRepo.FilePool.ToList());
            }
            App.Mediator.OnShowOperationWin(operat, win);
        }

        /// <summary>
        /// Use for init data
        /// </summary>
        FileOperationWin operationWin;

        private void ContextMenu_AddFile(INxlFile nxlFile)
        {
            // Share online file when don't load viewer.
            if (nxlFile.Location == EnumFileLocation.Online && !ViewerProcess.ContainsKey(nxlFile.PathId)) // online 
            {
                // 1. download file at bg, and disable window
                App.Log.Info("File download, in AddNxlFileToProject");
                currentWorkRepo.DownloadFile(nxlFile, true, (bool result) =>
                {
                    this.win.Dispatcher.Invoke(() =>
                    {
                        if (result)
                        {
                            // Add nxl file
                            try
                            {
                                var fp = App.Rmsdk.User.GetNxlFileFingerPrint(nxlFile.PartialLocalPath);

                                if (!(fp.isFromMyVault
                                           || fp.isFromSystemBucket
                                           || (fp.isFromPorject && (fp.hasAdminRights || fp.HasRight(FileRights.RIGHT_DECRYPT)))))
                                {
                                    App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Common_Not_Authorized"), false, nxlFile.Name);
                                    operationWin?.Close();
                                    return;
                                }

                                string[] filePath = new string[1];
                                filePath[0] = nxlFile.LocalPath;
                                string[] fileName = new string[1];
                                fileName[0] = nxlFile.Name;
                                IBase addNxl = new AddNxlFile(fp, new OperateFileInfo(filePath, fileName, FileFromSource.SkyDRM_Window_FileContextMenu),
                                        GetFileRepos(), nxlFile);

                                operationWin?.ChangeViewModel(addNxl);
                            }
                            catch (Exception e)
                            {
                                App.Log.Error(e.Message, e);
                                App.ShowBalloonTip(e.Message, false, nxlFile.Name);
                                operationWin?.Close();
                            }
                        }
                        else
                        {
                            // todo, error handle
                            App.Log.Info("Download failed:" + nxlFile.Name);
                            App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Notify_PopBubble_Download_Failed"), false, nxlFile.Name, "Add file to project",
                             EnumMsgNotifyIcon.Online);
                            operationWin?.Close();
                        }
                    });

                }, true, true);
                // at the same time popup the share "Share nxl window" --- disabled
                //  win = new window and show, disabled
                App.Mediator.OnShowOperationWin(out operationWin, this.win, "AddFileToProject");
            }
            else
            {
                // 1. share online file when viewer is loading.
                // 2. share offline file
                CheckModifiedRights(nxlFile, (INxlFile nxl) => {
                    App.Mediator.OnShowOperationAddNxlWinByFileList(nxl.LocalPath, this.win, nxl, FileFromSource.SkyDRM_Window_FileContextMenu);
                });
            }
        }

        private void ContextMenu_ModifyRights(INxlFile nxlFile)
        {
            // If the file is View,the menuItem will ban modifyright item. In here,don't need to judge the file is View.
            if (nxlFile.Location == EnumFileLocation.Online)
            {
                // 1. download file at bg, and disable window
                currentWorkRepo.DownloadFile(nxlFile, true, (bool result) =>
                {
                    this.win.Dispatcher.Invoke(() =>
                    {
                        if (result)
                        {
                            try
                            {
                                var fp = App.Rmsdk.User.GetNxlFileFingerPrint(nxlFile.LocalPath);

                                if (!fp.hasAdminRights)
                                {
                                    App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Common_Not_Authorized"), false, nxlFile.Name);

                                    operationWin?.Close();
                                    return;
                                }

                                string[] filePath = new string[1];
                                filePath[0] = nxlFile.LocalPath;
                                IBase modifyRight = new ModifyNxlFileRight(fp, new OperateFileInfo(filePath, null, FileFromSource.SkyDRM_Window_FileContextMenu),
                                        GetFileRepos(), CurrentSaveFilePath);

                                operationWin?.ChangeViewModel(modifyRight);
                            }
                            catch (Exception e)
                            {
                                App.Log.Error(e.Message, e);
                                App.ShowBalloonTip(e.Message, false, nxlFile.Name);

                                operationWin?.Close();
                            }
                        }
                        else
                        {
                            // todo, error handle
                            App.Log.Info("Download failed:" + nxlFile.Name);
                            App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Notify_PopBubble_Download_Failed"), false, nxlFile.Name, MsgNotifyOperation.MODIFY_RIGHTS,
                             EnumMsgNotifyIcon.Online);

                            operationWin?.Close();
                        }
                    });
                    
                }, false, true);
                // at the same time popup the "Modify rights win" --- disabled
                //  win = new window and show, disabled
                App.Mediator.OnShowOperationWin(out operationWin, this.win, "ModifyRights");
            }
            else if (nxlFile.Location == EnumFileLocation.Local)
            {
                CheckModifiedRights(nxlFile, (INxlFile nxl) =>
                {
                    if (CurrentWorkingArea == EnumCurrentWorkingArea.FILTERS_OFFLINE)
                    {
                        App.Mediator.OnShowOperationModifyRightWinByFilterFileList(nxl.LocalPath, this.win, nxl.SourcePath, FileFromSource.SkyDRM_Window_FileContextMenu);
                    }
                    else
                    {
                        App.Mediator.OnShowOperationModifyRightWinByFileList(nxl.LocalPath, this.win, FileFromSource.SkyDRM_Window_FileContextMenu);
                    }
                });
            }
        }

        /// <summary>
        /// Check the nxl file rights if is modified.
        /// </summary>
        private void CheckModifiedRights(INxlFile nxlFile, Action<INxlFile> action)
        {
            FileOperateHelper.CheckOfflineFileVersion(GetRepoByNxlFile(nxlFile), currentWorkingArea, nxlFile, (bool isModified) =>
            {
                // if conflict, will popup dialog have user to select.
                if (isModified)
                {
                    try
                    {
                        // Rights is modified, should enforce user to update file first.
                        if (ViewModelHelper.CheckConflict(nxlFile) == NxlFileConflictType.FILE_IS_MODIFIED_RIGHTS) 
                        {
                            if (CustomMessageBoxResult.Positive == Edit.Helper.ShowEnforceUpdateDialog(nxlFile.Name))
                            {
                                SyncModifiedFileFromRms(nxlFile, false);
                            }
                        }
                        else
                        {
                            action?.Invoke(nxlFile);
                        }
                    }
                    catch (Exception e)
                    {
                        App.Log.Info(e.ToString());
                        App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Common_System_Internal_Error"), false, nxlFile.Name);
                    }
                }
                else
                {
                    action?.Invoke(nxlFile);
                }
            });
        }

        private void ContextMenu_DoViewFile(ContextMenuCmdArgs args)
        {
            CurrentSelectedFile = args.SelectedFile;
            InnerViewFile(CurrentSelectedFile);
        }

        /// <summary>
        /// Show file info window
        /// </summary>
        /// <param name="localNxlFile">localNxlFile</param>
        public void ContextMenu_DoViewFileInfo(INxlFile nxlFile)
        {
            Menu_ViewFileInfo();
        }

        /// <summary>
        ///  Get the opened window through the window tag.
        /// </summary>
        private Window GetOpennedWindow(string tag)
        {
            foreach (Window one in App.Windows)
            {
                if (one.Tag != null && one.Tag.Equals(tag))
                {
                    return one;
                }
            }

            return null;
        }

        private void ContextMenu_DoRemove(ContextMenuCmdArgs args)
        {
            ManualRemove(CurrentSelectedFile);
        }

        private void ContextMenu_DoOpenSkyDRM(ContextMenuCmdArgs args)
        {
            DoOpenWeb();
        }

        private void ContextMenu_MarkOffline(ContextMenuCmdArgs args)
        {
            try
            {
                // disabled
                args.MenuItem.IsEnabled = false;

                // common --  project\myvault\sharedwith file.
                if (args.SelectedFile is NxlDoc)
                {
                    NxlDoc doc = args.SelectedFile as NxlDoc;
                    // Firstly, we set true, used to ui binding
                    doc.IsMarkedOffline = true; 

                    currentWorkRepo.MarkOffline(doc, (bool result) =>
                    {
                        UpdateStatusAccordMarkOfflineResult(doc, result);
                    });

                }
            }catch(Exception e)
            {
                App.Log.Warn("Exception in ContextMenu_MarkOffline," + e, e);
            }

        }

        private void UpdateStatusAccordMarkOfflineResult(NxlDoc doc, bool result)
        {
            if (result)
            {
                // update file status
                doc.FileStatus = EnumNxlFileStatus.AvailableOffline;
                doc.Location = EnumFileLocation.Local;
                // Fix bug 51388
                ListOperateHelper.UpdateListViewFileStatusForMarkOffline(nxlFileList, copyFileList, doc);
                // Notify to enable "View file info" menu item after offline succeed.
                // --- fix bug 51360
                CurrentSelectedFile = doc;

                App.MessageNotify.NotifyMsg(doc.Name, CultureStringInfo.ApplicationFindResource("Notify_RecordLog_MarkOffline_Succeed"),
                     EnumMsgNotifyType.LogMsg, MsgNotifyOperation.MARK_OFFLINE, EnumMsgNotifyResult.Succeed, EnumMsgNotifyIcon.Offline);
            }
            else
            {
                // todo, error handle
                doc.IsMarkedOffline = false;
                doc.FileStatus = EnumNxlFileStatus.DownLoadedFailed;

                App.MessageNotify.NotifyMsg(doc.Name, CultureStringInfo.ApplicationFindResource("Notify_RecordLog_MarkOffline_Failed"),
                     EnumMsgNotifyType.LogMsg, MsgNotifyOperation.MARK_OFFLINE, EnumMsgNotifyResult.Failed, EnumMsgNotifyIcon.Online);
            }
        }

        public void OnRestoreDownload(bool result, NxlDoc nxlFile)
        {
            try
            {
                if (result)
                {
                    // update file status
                    nxlFile.FileStatus = EnumNxlFileStatus.AvailableOffline;
                    nxlFile.Location = EnumFileLocation.Local;

                    // refresh. -- this is invoked in sub-thread
                    App.Dispatcher.Invoke((Action)delegate
                    {
                        DoRefresh();
                    });
                }
                else
                {
                    // todo, error handle
                    nxlFile.IsMarkedOffline = false; // reset
                    nxlFile.FileStatus = EnumNxlFileStatus.DownLoadedFailed;
                }
            }
            catch (Exception e)
            {
                App.Log.Warn("Exception in OnRestoreDownload," + e, e);
            }
        }

        class ExtractContentsFeature
        {
            SkydrmApp App;
            ViewModelMainWindow host;
            INxlFile target;
      
            public ExtractContentsFeature(SkydrmApp App, ViewModelMainWindow host, INxlFile target)
            {
                this.App = App;
                this.host = host;
                this.target = target;
            }

            private void Download(OnDownloadComplete callback)
            {

                if (target.Location == EnumFileLocation.Local)
                {
                    callback?.Invoke(true);
                    return;
                }

                NxlDoc doc = target as NxlDoc;
                host.currentWorkRepo.DownloadFile(doc, true, (bool result) =>
                {
                    callback?.Invoke(result);
                }, false, true);
            }

            public bool ExtractContents()
            {
                bool result = false;
                try
                {
                    bool isOnline = (target.Location == EnumFileLocation.Online);
                    if (!string.IsNullOrEmpty(target.LocalPath) && File.Exists(target.LocalPath))
                    {
                        bool isCancled;
                        result = ExtractContentHelper.ExtractContent(App, host.win, target.LocalPath,out isCancled, isOnline);
                        if (!isCancled)
                        {
                            ExtractContentHelper.SendLog(target.LocalPath, NxlOpLog.Decrypt, result);
                        }
                    }
                    else
                    {
                        // remote file.
                        Download((bool b) =>
                        {
                            if (b)
                            {
                                //Download Succeeded
                                host.win.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
                                {
                                    bool isCancled;
                                    result = ExtractContentHelper.ExtractContent(App, host.win, target.LocalPath, out isCancled, isOnline);
                                    if (!isCancled)
                                    {
                                        ExtractContentHelper.SendLog(target.LocalPath, NxlOpLog.Decrypt, result);
                                    }

                                }));
                            }
                            else
                            {
                                App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Notify_PopBubble_Download_Failed"), false, target.Name);
                            }

                        });
                    }
                }
                catch (Exception ex)
                {
                    App.Log.Info(ex);
                    ExtractContentHelper.SendLog(target.LocalPath, NxlOpLog.Decrypt, false);
                }

                return result;
            }
        }


        class SaveAsFeature
        {
            ViewModelMainWindow host;
            INxlFile target;
            string dest;

            public string Dest { get => dest;}
            public INxlFile Target { get => target; }

            public SaveAsFeature(ViewModelMainWindow host, INxlFile target)
            {
                this.host = host;
                this.target = target;
                this.dest = "";
            }

            public void PartialDownload(OnDownloadComplete callback)
            {
                if (FileHelper.Exist(target.PartialLocalPath))
                {
                    callback?.Invoke(true);
                    return;
                }

                host.currentWorkRepo.DownloadFile(target, true, (bool result) => {
                    callback?.Invoke(result);
                },
                true
                );
            }

            public void CheckRights()
            {
                var fp = host.App.Rmsdk.User.GetNxlFileFingerPrint(target.PartialLocalPath);
                bool rView = fp.HasRight(FileRights.RIGHT_VIEW);
                bool rSaveAs = fp.HasRight(FileRights.RIGHT_SAVEAS);
                bool rDownload= fp.HasRight(FileRights.RIGHT_DOWNLOAD);
                if (!(rSaveAs || rDownload || (fp.hasAdminRights && rView)))
                {
                    throw new InsufficientRightsException();
                }
            }

            public bool CheckRights_NoThrow()
            {
                try
                {
                    var fp = host.App.Rmsdk.User.GetNxlFileFingerPrint(target.PartialLocalPath);
                    return fp.HasRight(FileRights.RIGHT_SAVEAS) || fp.HasRight(FileRights.RIGHT_DOWNLOAD);
                }
                catch (Exception)
                {
                }
                return false;
            }

            public bool ChooseDestination()
            {
                bool bUserHasSelect = false;
                host.win.Dispatcher.Invoke(() =>
                {
                    // fix bug 53134 add new feature, 
                    // extract timestamp in target.Name and replaced it as local lastest one
                    // ModifyExportedFileNameReplacedWithLatestTimestamp(target.Name)

                    // 2020.10 Release, fix bug 64276 not change timestamp
                    bUserHasSelect = ShowSaveFileDialog(out dest, target.Name, host.win);
                });
                return bUserHasSelect;
            }

            public void Export()
            {
                try
                {
                    target.Export(Dest);

                    // Notify msg
                    EnumMsgNotifyIcon icon = target.IsMarkedOffline ? EnumMsgNotifyIcon.Offline : EnumMsgNotifyIcon.Online;
                    SkydrmApp.Singleton.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Exception_ExportFeature_Succeeded") + Dest + ".",
                        true,
                        target.Name,
                        MsgNotifyOperation.SAVE_AS,
                        icon
                        );
                }
                catch (Exception e)
                {
                    // Notify msg
                    EnumMsgNotifyIcon icon = target.IsMarkedOffline ? EnumMsgNotifyIcon.Offline : EnumMsgNotifyIcon.Online;
                    SkydrmApp.Singleton.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Exception_ExportFeature_Failed") + Dest + ".",
                        false,
                        target.Name,
                        MsgNotifyOperation.SAVE_AS,
                        icon
                        );
                }
            }


            public void NotifyError(string error)
            {
                host.App.ShowBalloonTip(error, false);
            }

            private string ModifyExportedFileNameReplacedWithLatestTimestamp(string fname)
            {
                // like log-2019-01-24-07-04-28.txt
                // pattern-match "-2019-01-24-07-04-28" replaced with latest lcoal timestamp
                string pattern = @"-\d{4}-\d{2}-\d{2}-\d{2}-\d{2}-\d{2}";
                // new stime string
                string newTimeStamp = DateTime.Now.ToLocalTime().ToString("-yyyy-MM-dd-HH-mm-ss");
                Regex r = new Regex(pattern);
                string newName = fname;
                if (r.IsMatch(fname))
                {
                    newName = r.Replace(fname, newTimeStamp);
                }
                return newName;
            }

            private bool ShowSaveFileDialog(out string destinationPath, string fileName, Window owner)
            {
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.CheckFileExists = false;
                dlg.FileName = fileName; // Default file name
                dlg.DefaultExt = Path.GetExtension(fileName); // .nxl Default file extension
                dlg.Filter = "NextLabs Protected Files (*.nxl)|*.nxl"; // Filter files by extension

                // Show save file dialog box
                Nullable<bool> result = dlg.ShowDialog(owner);
                // Process save file dialog box results
                if (result == true)
                {
                    destinationPath = dlg.FileName;

                    if (Path.HasExtension(destinationPath))
                    {
                        if (!string.Equals(Path.GetExtension(destinationPath), ".nxl", StringComparison.CurrentCultureIgnoreCase))
                        {
                            // destinationPath = destinationPath.Substring(0, destinationPath.LastIndexOf(".")) + ".nxl";
                            destinationPath += ".nxl";
                        }
                    }

                }
                else
                {
                    destinationPath = string.Empty;
                }

                return result.Value;
            }
        }

        public void PartialDownlaod(Action<INxlFile> callback)
        {
            INxlFile tmp = CurrentSelectedFile;
            PartialDownload(tmp, (bool result)=>{
                if (result)
                {
                    callback.Invoke(tmp);
                   
                }
                else
                {
                    callback.Invoke(null);
                }
            });
        }

        private void PartialDownload( INxlFile nxl,  OnDownloadComplete callback)
        {
            // Note: PendingUploadFile should not execute this.
            // Rms onlineFile and offlineFile will use parital_localPath getFingerprint.
            // But In MyVault file, if partial_localpath not exist will check localPath.
            if (nxl.Location == EnumFileLocation.Local)
            {
                callback?.Invoke(true);
                return;
            }

            // For project and workSpace online file, always download; 
            if (nxl.FileRepo == EnumFileRepo.REPO_PROJECT || nxl.FileRepo == EnumFileRepo.REPO_WORKSPACE)
            {
                currentWorkRepo.DownloadFile(nxl, true, (bool result) => {
                    callback?.Invoke(result);
                },
                true
                );
            }

            // For other repo file
            else
            {
                // Always download, fix bug 63676 that file info don't update in 'sharedWithMe' when user do overwrite and share.
                currentWorkRepo.DownloadFile(nxl, true, (bool result) => {
                    callback?.Invoke(result);
                },
                true
                );

            }

        }

        /// <summary>
        /// This will still download the partial file
        /// </summary>
        /// <param name="callback">Download callback, if is null, means don't need it.</param>
        public void PartialDownloadEx(OnDownloadCompleteEx callback = null)
        {
            App.Log.Info("PartialDownloadEx -->");
            INxlFile tmp = CurrentSelectedFile;

            currentWorkRepo.DownloadFile(tmp, true, (bool result) => {
                if (result)
                {
                    callback?.Invoke(result, tmp);
                }
                else
                {
                    callback?.Invoke(result, tmp);
                }
            }, true);
        }

        public void IsCanSaveAs(INxlFile nxlFile, Action<bool> rt)
        {
            try
            {
                SaveAsFeature feature = new SaveAsFeature(this, nxlFile);
                feature.PartialDownload((bool result) =>
                {
                    bool r = result;
                    if (result)
                    {
                        r= r && feature.CheckRights_NoThrow();
                        
                    }
               
                    // Using "BeginInvoke" in order to avoid that right click menu ui block when another file is exporting. 
                    this.win.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
                    {
                        rt.Invoke(r);
                    }));

                });
            }
            catch (Exception e)
            {
                App.Log.Warn("Exception in OnRestoreDownload," + e, e);
            }
            
        }

        private void ContextMenu_SaveAs(ContextMenuCmdArgs args)
        {
            try
            {
                SaveAsFeature feature = new SaveAsFeature(this, args.SelectedFile);
                /*
                 *  1 download
                 *  2 check right
                 *  3 choose dest
                 *  4 export
                 */

                feature.PartialDownload((bool result) =>
                 {
                     try
                     {
                         if (!result)
                         {
                             throw new Exception("download filed");
                         }
                         feature.CheckRights();
                         if (!feature.ChooseDestination())
                         {
                             // use canceled;
                             return;
                         }   
                         
                         feature.Export();
                     }
                     catch (InsufficientRightsException e)
                     {
                         App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Notify_PopBubble_No_Permission_Download"), false, feature.Target.Name);
                     }
                     catch (Exception e )
                     {

                         App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Exception_ExportFeature_Failed"), false, feature.Target.Name);                      
                         App.Log.Warn("Exception in OnRestoreDownload," + e, e);
                     }

                 });
            }
            catch (Exception e)
            {
                App.Log.Warn("Exception in OnRestoreDownload," + e, e);
                throw;
            }
        }


        private void ContextMenu_ExtractContents(ContextMenuCmdArgs args)
        {
            Console.WriteLine("ContextMenu_Remove_Protection");
        
            ExtractContentsFeature extractContentsFeature = new ExtractContentsFeature(App, this, args.SelectedFile);
            extractContentsFeature.ExtractContents();

        }

        /// <summary>
        /// Handle edit project offline file by viewer Edit button trigger.
        /// </summary>
        public void EditFromViewer(string localPath)
        {
           var nxlFile = FileOperateHelper.GetFileFromListByLocalPath(localPath, nxlFileList);
            if (nxlFile != null)
            {
                SkydrmApp.Singleton.Log.Info("Edit offline file by viewer edit.");
                InnerEdit(nxlFile);
            }
        }

        /// <summary>
        /// Sync file to rms after edit from viewer.
        /// </summary>
        /// <param name="localPath"></param>
        /// <param name="isEdit"></param>
        public void SyncFileAfterEditFromViewer(string localPath, bool isEdit)
        {
            App.Log.InfoFormat("Enter SyncFileAfterEditFromViewer , local path:{0} , isEdit:{1}" , localPath, isEdit);
            var nxlFile = FileOperateHelper.GetFileFromListByLocalPath(localPath, nxlFileList, viewFiles);
            if(nxlFile == null)
            {
                App.Log.Info("Failed to search the edited file in local.");
                return;
            }

            App.Log.Info("havd find edited file in local , the path is:"+ nxlFile.LocalPath);

            // if nxl is PendingUploadFile will not do check version and add upload list
            // fix bug 64792, 64784
            if (nxlFile is PendingUploadFile)
            {
                return;
            }

            if (isEdit)
            {
                // Now Project & WorkSpace support Edit.
                IEditFeature ef = new EditFeature(GetRepoByNxlFile(nxlFile), currentWorkingArea);
                // sync to rms
                ef.UpdateToRms(nxlFile, (INxlFile updatedFile) => {
                    UpdateModifiedItem(updatedFile);
                });
            }
        }

        public IFileRepo GetRepoByNxlFile(INxlFile nxlFile)
        {
            IFileRepo fileRepo = null;
            if (nxlFile.FileRepo == EnumFileRepo.REPO_PROJECT)
            {
                fileRepo = projectRepo;
            }
            else if (nxlFile.FileRepo == EnumFileRepo.REPO_WORKSPACE)
            {
                fileRepo = workSpaceRepo;
            } else if(nxlFile.FileRepo == EnumFileRepo.REPO_MYVAULT)
            {
                fileRepo = myVaultRepo;
            } else if(nxlFile.FileRepo == EnumFileRepo.REPO_MYDRIVE)
            {
                fileRepo = myDriveRepo;
            }
            else if (nxlFile.FileRepo == EnumFileRepo.REPO_EXTERNAL_DRIVE)
            {
                foreach (var item in externalRepos)
                {
                    if (item.RepoId.Equals(nxlFile.RepoId))
                    {
                        fileRepo = item;
                        break;
                    }
                }
            }
            else if (nxlFile.FileRepo == EnumFileRepo.REPO_SHARED_WITH_ME)
            {
                fileRepo = myVaultRepo;
            }
            return fileRepo;
        }

        #region For Edit in RMD (now disabled the operation in context menu, but reserve the code.)
        private void InnerEdit(INxlFile nxlFile)
        {
            try
            {
                IEditFeature ef = EditMap.GetValue(nxlFile.LocalPath);
                if (ef == null)
                {
                    // Now only project support edit.
                    ef = new EditFeature(GetRepoByNxlFile(nxlFile), currentWorkingArea);
                    EditMap.Add(nxlFile.LocalPath, ef);
                }

                // online mode
                if (IsNetworkAvailable)
                {
                    if (nxlFile.IsMarkedFileRemoteModified)
                    {
                        // If found remote file is modified before edit local file, will popup dialog to prompt user if sync first.
                        ef.HandleIfSyncFromRms(nxlFile, (INxlFile updatedFile) => {
                            UpdateModifiedItem(updatedFile);
                        });
                    }
                    else
                    {
                        // Check remote file if is modified before edit locals.
                        ef.CheckVersionFromRms(nxlFile, (bool bIsModified) => {
                            if (bIsModified)
                            {
                                ef.HandleIfSyncFromRms(nxlFile, (INxlFile updatedFile) => {
                                    UpdateModifiedItem(updatedFile);
                                });
                            }
                            else
                            {
                                // execute edit
                                ef.EditFromMainWin(nxlFile, (IEditComplete cb) => {
                                    OnEditFinish(ef, cb, nxlFile);
                                });

                            }
                        });
                    }
                }

                // offline mode
                else
                {
                    ef.EditFromMainWin(nxlFile, (IEditComplete cb) => {
                        OnEditFinish(ef, cb, nxlFile);
                    });
                }
            }
            catch (Exception e)
            {
                SkydrmApp.Singleton.Log.Error("Edit offline file failed: " + e.ToString());
            }

        }
        
        private void ContextMenu_Edit(ContextMenuCmdArgs args)
        {
            SkydrmApp.Singleton.Log.Info("Edit offline file by context menu.");

            // Now hide it in rmd.
            //InnerEdit(args.SelectedFile);
        }

        private void OnEditFinish(IEditFeature ef, IEditComplete cb, INxlFile nxlFile)
        {
            if (cb.IsEdit)
            {
                ef.UpdateToRms(nxlFile, (INxlFile updatedFile)=> {
                    UpdateModifiedItem(updatedFile);
                });
            }
            EditMap.Remove(cb.LocalPath);
        }
        #endregion // For Edit in RMD


        // Update the specified modified listview item ui after sync.
        private void UpdateModifiedItem(INxlFile updatedFile)
        {
            int index = -1;
            for(int i = 0; i < nxlFileList.Count; i++)
            {
                if (nxlFileList[i].Equals(updatedFile))
                {
                    index = i;
                    break;
                }
            }

            Console.WriteLine("========= in UpdateModifiedItem, file name: " + updatedFile.Name);
            Console.WriteLine("========= in UpdateModifiedItem, index: " + index.ToString());

            if (index != -1)
            {
                // set source path
                updatedFile.SourcePath = nxlFileList[index].SourcePath;

                nxlFileList[index] = updatedFile;
                copyFileList[index] = updatedFile;

                // Make the latest modified display in the top of listview(fix bug 53870)
                DefaultSort();
            }

        }

        /// <summary>
        /// Update the listview item 'Share with' ui in MyVault
        /// </summary>
        /// <param name="duid"></param>
        public void UpdateFileShareWith(string duid)
        {
            foreach (var item in nxlFileList)
            {
                if (item.Duid == duid)
                {
                    // refresh "SharedWith"                        
                    if (item.FileRepo == EnumFileRepo.REPO_MYVAULT)
                    {
                        // notify ui to update
                        // if myVault file partialPath not exist, will check and return localPath 
                        if (FileHelper.Exist(item.PartialLocalPath))
                        {
                            myVaultRepo.NotifySharedRecipentsChanged(item);
                        }
                        else
                        {
                            new NoThrowTask(true, ()=> {
                                myVaultRepo.DownloadFile(item, true, (bool result) => {
                                    if (result)
                                    {
                                        myVaultRepo.NotifySharedRecipentsChanged(item);
                                    }
                                },true);
                            }).Do();
                        }
                        
                    }
                    break;
                }
            }
        }

        private void ContextMenu_UnmarkOffline(ContextMenuCmdArgs args)
        {
            try
            {
                if (args.SelectedFile.IsEdit)
                {
                    if (MsgBox.EditFileShowRemoveDialog(args.SelectedFile) != CustomMessageBoxResult.Positive)
                    {
                        return;
                    }
                }
                // unmark
                if (args.SelectedFile.UnMark())
                {

                    if (CurrentWorkingArea == EnumCurrentWorkingArea.FILTERS_OFFLINE)
                    {
                        nxlFileList.Remove(args.SelectedFile);
                        copyFileList.Remove(args.SelectedFile);

                        // Must reset "CurrentSelectedFile" as null after unmark if in current working.
                        if (CurrentSelectedFile != null && args.SelectedFile == CurrentSelectedFile)
                        {
                            ResetCurrentSelectedFile();
                        }

                    }

                    // remove from offline filter files
                    offlineFiles.Remove(args.SelectedFile);

                    // update status.
                    args.SelectedFile.FileStatus = EnumNxlFileStatus.Online;
                    args.SelectedFile.Location = EnumFileLocation.Online;

                    args.SelectedFile.IsEdit = false;

                    // Notify msg
                    App.MessageNotify.NotifyMsg(args.SelectedFile.Name, CultureStringInfo.ApplicationFindResource("Notify_RecordLog_UnMarkOffline_Succeed"), EnumMsgNotifyType.LogMsg,
                        MsgNotifyOperation.UNMARK_OFFLINE, EnumMsgNotifyResult.Succeed, EnumMsgNotifyIcon.Online);

                }
                else
                {
                    App.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Notify_PopBubble_Remove_Failed"), false, args.SelectedFile.Name);
                }
            }
            catch(Exception e)
            {
                App.Log.Warn("Exception in ContextMenu_UnmarkOffline," + e, e);
            }
        }
        /// <summary>
        /// OpenFileDialog Select a NextLabs Protected Documents
        /// </summary>
        /// <param name="selectedNxlFile"></param>
        /// <returns></returns>
        private bool OpenFileDialog(out string selectedNxlFile)
        {
            selectedNxlFile = null;
            try
            {
                // --- Also can use System.Windows.Forms.FolderBrowserDialog!
                Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
                dialog.Title = "Select a NextLabs Protected Files";

                if (!Directory.Exists(selectFilePath))
                {
                    // Get system desktop dir.
                    selectFilePath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory); ;
                }
                dialog.InitialDirectory = selectFilePath; // set init Dir.

                // .nxl files
                dialog.Filter = "NextLabs Protected Files (*.nxl)|*.nxl";

                if (dialog.ShowDialog() == true) // when user click ok button
                {
                    selectedNxlFile = dialog.FileName;

                    //Save select File path
                    selectFilePath = selectedNxlFile.Substring(0, selectedNxlFile.LastIndexOf(Path.DirectorySeparatorChar) + 1);

                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                App.Log.Warn("Exception in OpenFileDialog," + e, e);
                return false;
            }
        }

        private void ContextMenu_TreeViewAddRepo()
        {
            IAddExternalRepo addExternalRepo = new AddExternalRepo();
            App.Mediator.OnShowAddRepoWin(addExternalRepo, win);
        }

        #endregion // Right Click Context Menu

    }
}

