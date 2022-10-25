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
using SkydrmLocal.rmc.common.communicator;
using SkydrmLocal.rmc.common.communicator.annotation;
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
using SkydrmLocal.rmc.fileSystem.sharedWithMe;
using SkydrmLocal.rmc.helper;
using SkydrmLocal.rmc.modifyRights;
using SkydrmLocal.rmc.process;
using SkydrmLocal.rmc.removeProtection;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.shareNxlFeature;
using SkydrmLocal.rmc.ui.components;
using SkydrmLocal.rmc.ui.components.sortListView;
using SkydrmLocal.rmc.ui.pages.model;
using SkydrmLocal.rmc.ui.utils;
using SkydrmLocal.rmc.ui.windows.mainWindow.helper;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent;
using SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.model;
using SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.viewModel;
using SkydrmLocal.rmc.ui.windows.mainWindow.view;
using SkydrmLocal.rmc.ui.windows.messageBox;
using SkydrmLocal.rmc.ui.windows.nxlConvert;
using static SkydrmLocal.rmc.fileSystem.project.ProjectRepo;
using static SkydrmLocal.rmc.ui.components.CustomSearchBox;
using static SkydrmLocal.rmc.ui.utils.CommonUtils;
using static SkydrmLocal.rmc.ui.windows.CustomMessageBoxWindow;
using Helper = SkydrmLocal.rmc.Edit.Helper;

namespace SkydrmLocal.rmc.ui.windows.mainWindow.viewModel
{
    public class ViewModelMainWindow : INotifyPropertyChanged
    {
        //******************************** For members ***************************************//

        #region // For other members
        private SkydrmLocalApp App = (SkydrmLocalApp)SkydrmLocalApp.Current;
        private MainWindow win;

        private static readonly string MY_VAULT = CultureStringInfo.MainWin__TreeView_MyVault;
        private static readonly string SHARE_WITH_ME = CultureStringInfo.MainWin__TreeView_ShareWithMe;
        private static readonly string PROJECT = CultureStringInfo.MainWin__TreeView_Project;

        // For selectProjectFolderWin transmit path
        public string CurrentSaveFilePath { get; set; }

        // Used to record the content that user is searching
        private string searchText = "";

        // For OpenFileDialog, Save selectFile path
        private string selectFilePath= Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
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

        // Will refresh UI when add or remove one entry because of ObservableCollection
        private ObservableCollection<INxlFile> nxlFileList = new ObservableCollection<INxlFile>();
        public ObservableCollection<INxlFile> NxlFileList
        {
            get { return nxlFileList; }
            set { nxlFileList = value; }
        }

        // Used for do search.
        private ObservableCollection<INxlFile> copyFileList = new ObservableCollection<INxlFile>(); 

        private bool issearch=false;
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
        public ObservableCollection<ProjectData> CollectionProject = new ObservableCollection<ProjectData>();
        // For binding IsEmptyFolder visibility
        private int collectionProjectCount;
        public int CollectionProjectCount
        {
            get
            {
                return collectionProjectCount;
            }
            set
            {
                collectionProjectCount = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CollectionProjectCount"));
            }
        }
        #endregion // For nxl files


        #region For event handler
        public event PropertyChangedEventHandler PropertyChanged;
        // Notification delegate
        private CreateSuccessDelegate createSuccessCallBack;
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


        #region For TreeView model
        // Record user currently selected treeview item, in order to cancel the selecttion status when user click filter button.
        private TreeViewItemViewModel currentTreeViewItem;
        public RepoViewModel RepoViewModel { get; set; }
        #endregion // For treeView model


        #region For filter
        private SolidColorBrush colorBule = new SolidColorBrush(Color.FromRgb(0X9F, 0XD9, 0XFD));
        private SolidColorBrush colorTransparent = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        // Filter set
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
        #endregion // For flags


        #region For working area
        private EnumCurrentWorkingArea currentWorkingArea;
        public EnumCurrentWorkingArea CurrentWorkingArea
        {
            get
            { return currentWorkingArea; }
            set
            {
                projectRepo.WorkingArea = value;

                currentWorkingArea = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentWorkingArea"));
            }
        }
        #endregion // For working area


        #region For repos
        // Current working repo
        private IFileRepo currentWorkRepo;
        public List<IFileRepo> FileRepos { get; set; }

        private MyVaultRepo myVaultRepo;
        private ShareWithRepo sharedWithRepo;
        public ProjectRepo projectRepo;
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

            // init notification delegate
            // createSuccessCallBack = new CreateSuccessDelegate(GetCreatedFile);

            // register update status notification & upload complete callback.
            App.Log.Info("register update status notification & upload complete callback.");
            UploadManagerEx.GetInstance().NotifyChangeStatus += SyncCurrentListViewFileStatus;
            UploadManagerEx.GetInstance().SyncModifiedFileCallback += UpdateModifiedFileAndItemNodeUi;
            UploadManagerEx.GetInstance().SetCallback(UploadComplete);

            //get user name
            App.Log.Info("get user name");
            UserName = GetUserName();
            AvatarText = CommonUtils.ConvertNameToAvatarText(userName, " ");
            AvatarBackground = CommonUtils.SelectionBackgroundColor(userName);
            AvatarTextColor = CommonUtils.SelectionTextColor(userName);

            // init "IsStartUpload" flag.
            App.Log.Info("init 'IsStartUpload' flag.");
            InitStartUploadFlag();

            // Register Event
            App.UserNameUpdated += () =>
            {
                UserName = GetUserName();
                this.win.user_name.Text = UserName;
                AvatarText = CommonUtils.ConvertNameToAvatarText(userName, " ");
                AvatarBackground = CommonUtils.SelectionBackgroundColor(userName);
                AvatarTextColor = CommonUtils.SelectionTextColor(userName);
            };


            App.MyVaultFileOrSharedWithMeLowLevelUpdated += () =>
             {
                 App.Dispatcher.Invoke(() =>
                 {
                     DoRefresh();
                 });
             };

            // instantiat repo
            App.Log.Info("init repo.");
            InitRepo();

            // init data
            App.Log.Info("init data.");
            InitData();

            App.Log.Info("init Project Page UI.");
            InitProjectPageUI();

            // Register FileList refresh notification
            projectRepo.notifyProjectFileListRefresh += UpdateViewFromTreeView;

        }

        private void InitRepo()
        {
            RepoViewModel = new RepoViewModel();
            myVaultRepo = new MyVaultRepo();
            sharedWithRepo = new ShareWithRepo();
            projectRepo = new ProjectRepo();
        }

        private void InitData()
        {
            // myvault
            if (myVaultRepo.GetLocalFiles().Count == 0)
            {
                SkydrmLocalApp.Singleton.Log.Info("Sync myvault files in InitData");

                myVaultRepo.IsLoading = true;
                myVaultRepo.SyncFiles((bool bSuccess, IList<INxlFile> result, string reserved) =>
                {               
                      Console.WriteLine(MY_VAULT);
                      myVaultRepo.IsLoading = false;

                      LoadData();
                });
            }
            else
            {
                myVaultRepo.IsLoading = false;
            }

            // shared with me 
            if (sharedWithRepo.GetLocalFiles().Count == 0)
            {
                SkydrmLocalApp.Singleton.Log.Info("Sync sharedWithMe files in InitData");

                sharedWithRepo.IsLoading = true;
                sharedWithRepo.SyncFiles((bool bSuccess, IList<INxlFile> result, string reserved) =>
                {
                    Console.WriteLine("sharedWith");
                    sharedWithRepo.IsLoading = false;

                    LoadData();
                });
            }
            else
            {
                sharedWithRepo.IsLoading = false;
            }

            // project
            if (projectRepo.GetAllData().Count == 0)
            {
                SkydrmLocalApp.Singleton.Log.Info("Sync project files in InitData");

                projectRepo.IsLoading = true;
                projectRepo.SyncRemoteData((bool bSuccess, IList<ProjectData> result) =>
                {
                    Console.WriteLine("project");
                    projectRepo.IsLoading = false;

                    //if (!RepoViewModel.IsHasProjectViewModel())
                    //{
                    //    RepoViewModel.AddProjectRepoViewModel(projectRepo);
                    //}

                    LoadData();
                });
            }
            else
            {
                projectRepo.IsLoading = false;
            }

            LoadData();
        }

        private void LoadData()
        {
            if (!projectRepo.IsLoading && !myVaultRepo.IsLoading && !sharedWithRepo.IsLoading)
            {
                SkydrmLocalApp.Singleton.Log.Info("Load data.");

                IsInitializing = false;
                InitTreeView();

                // defult choose OutBoxBtn
                OutBoxClick();

                InitAndTryUploadPendingFile();
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

            // handle upload project edited files.
            UploadManagerEx.GetInstance().SetFileRepo(projectRepo);

            // Get all edited files(not yet updated to rms), and also need to submit to upload queue
            UploadManagerEx.GetInstance().SubmitToWaitingQueue(GetAllEditedFiles());

            // upload local file if existed.
            TryToUpload();

            // set default sort for outbox, which is our default current working area.
            DefaultSort();
        }

        private void InitTreeView()
        {
            SkydrmLocalApp.Singleton.Log.Info("Init tree view");

            FileRepos = new List<IFileRepo>();
            FileRepos.Add(myVaultRepo);
            FileRepos.Add(sharedWithRepo);
            FileRepos.Add(projectRepo);
            RepoViewModel.Start(FileRepos);

            // Whether UI is displayed or not
            IsDisplayTreeview = RepoViewModel.Roots.Count > 0 ? true : false;

            // Note: must set DataContext after setting data source.
            win.UserControl_TreeView.DataContext = RepoViewModel;

            // Defult save path
            CurrentSaveFilePath = MY_VAULT;
        }

        private void InitProjectPageUI()
        {
            // set Project ListBox GroupName
            this.win.ProjectListBox.ItemsSource = CollectionProject;
            ICollectionView cv = CollectionViewSource.GetDefaultView(CollectionProject);
            cv.GroupDescriptions.Add(new PropertyGroupDescription("ProjectInfo.BOwner"));

            // Register project page refresh notification.
            projectRepo.notifyProjectPageRefresh += RefreshProjectPageData;
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
        public void InitViewerProcess()
        {
            IPCManager = new IPCManager(new Action<int, int, string>(ReceiveData));

        }

        #endregion // For Init


        #region For event handler

        public void TreeViewItemSelectChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            // fix bug 50718
            win.SearchBox.TbxInput.Text = "";

            TreeViewItemViewModel treeViewItem = e.NewValue as TreeViewItemViewModel;
            if (treeViewItem == null) // when user click filter button, we set treeview IsSelect as false.
            {
                return;
            }

            IList<INxlFile> fileList = null;
            try
            {
                if (treeViewItem is RootViewModel)
                {
                    RootViewModel rootView = treeViewItem as RootViewModel;
                    if (rootView.RootName.Equals(MY_VAULT, StringComparison.CurrentCultureIgnoreCase))
                    {
                        CurrentWorkingArea = EnumCurrentWorkingArea.MYVAULT;
                        currentWorkRepo = myVaultRepo;

                        fileList = rootView.Root.MyVaultFiles; // all myVault files.         
                    }
                    else if (rootView.RootName.Equals(SHARE_WITH_ME, StringComparison.CurrentCultureIgnoreCase))
                    {
                        CurrentWorkingArea = EnumCurrentWorkingArea.SHARED_WITH_ME;
                        currentWorkRepo = sharedWithRepo;

                        fileList = rootView.Root.ShareWithFiles;
                    }
                    else if (rootView.RootName.Equals(PROJECT, StringComparison.CurrentCultureIgnoreCase))
                    {
                        CurrentWorkingArea = EnumCurrentWorkingArea.PROJECT;
                        currentWorkRepo = projectRepo;

                        // Set ListBox instead of ListView
                        LoadProjectPageData((List<ProjectData>)rootView.oldResults);
                        fileList = new List<INxlFile>();
                    }

                }
                else if (treeViewItem is ProjectViewModel)
                {
                    ProjectViewModel projectView = treeViewItem as ProjectViewModel;
                    fileList = projectView.Project.FileNodes; // project all files(doc and folder)

                    CurrentWorkingArea = EnumCurrentWorkingArea.PROJECT_ROOT;
                    currentWorkRepo = projectRepo;
                    projectRepo.CurrentWorkingProject = projectView.Project;
                    projectRepo.ProjectViewModel = projectView;
                }
                else if (treeViewItem is FolderViewModel)
                {
                    FolderViewModel folderView = treeViewItem as FolderViewModel;
                    fileList = folderView.NxlFolder.Children; // here need display right region.

                    CurrentWorkingArea = EnumCurrentWorkingArea.PROJECT_FOLDER;
                    currentWorkRepo = projectRepo;
                    projectRepo.CurrentWorkingProject = FindProject(folderView);
                    projectRepo.CurrentWorkingFolder = folderView.NxlFolder;
                    projectRepo.FolderViewModel = folderView;
                }
            }
            catch (Exception ex)
            {
                App.Log.Warn("Exception in TreeViewItemSelectChanged," + ex, ex);
            }

            ResetCurrentSelectedFile();

            SetCurSavePath();

            // Record user currently selected treeview item, in order to cancel the selecttion status when user click filter button.
            currentTreeViewItem = treeViewItem;

            // Set offlineBtn and OutboxBtn bg color
            ChangeFilterBtnBackground();

            // Change share button tags.
            ChangeShareBtnTag();

            if (IsTriggerRefresh)
            {
                // set listView item source.
                SetListView(fileList);  // -- Actually here can ignore if set it in DoRefresh() function.

                // async refresh at the same time.
                DoRefresh();
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
            Console.WriteLine("ListViewItem_DoubleClick -->");
            App.Log.Info("ListViewItem_DoubleClick -->");

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
            CurrentWorkingArea = EnumCurrentWorkingArea.PROJECT_FOLDER;
            projectRepo.CurrentWorkingFolder = (NxlFolder)CurrentSelectedFile;

            SetCurSavePath();

            IList<INxlFile> children = ((NxlFolder)CurrentSelectedFile).Children;

            // First, display original data.
            SetListView(children);

            // default sort
            DefaultSort();

            // At the same time, sync from rms
            //currentWorkRepo.SyncFiles((bool bSuccess, IList<INxlFile> results) =>
            //{
            //    if (bSuccess && currentWorkingArea == EnumCurrentWorkingArea.PROJECT_FOLDER)
            //    {
            //        IList<INxlFile> added = null;
            //        IList<INxlFile> removed = null;
            //        CommonUtils.MergeUI(results, nxlFileList, copyFileList, out added, out removed);
            //    }
            //});

            // Let treeview item switch with it automaticlly.
            TreeviewNavigate();
        }

        // Cached file(Leave a copy file) also looked as offline file.
        public bool IsOffline()
        {
            return CurrentSelectedFile.FileStatus == EnumNxlFileStatus.AvailableOffline
                    || CurrentSelectedFile.FileStatus == EnumNxlFileStatus.CachedFile;
        }

        private void DoubleClickFile()
        {
            if (CurrentSelectedFile.FileStatus == EnumNxlFileStatus.Uploading ||
                CurrentSelectedFile.FileStatus == EnumNxlFileStatus.Downloading)
            {
                return;
            }

            // View the local file.
            if (CurrentSelectedFile.Location == EnumFileLocation.Local)
            {
                // For project offline file, need to check file version before view.
                if (CurrentSelectedFile.FileRepo == EnumFileRepo.REPO_PROJECT && IsOffline())
                {
                    ViewProjectOfflineFile(CurrentSelectedFile);
                }
                else
                {
                    StartViewerProcess(CurrentSelectedFile);
                }
            }

            // View the online file.
            else if (CurrentSelectedFile.Location == EnumFileLocation.Online)
            {
                // If network is offline, forbid to view online file.
                if (IsNetworkAvailable)
                {
                    StartViewerProcess(CurrentSelectedFile);
                }
            }
            else
            {
                App.Log.Info("warning:  failed logic ...");
            }
        }

        /// <summary>
        /// View project offline file, need to check file version to see if is conflict or not before view.
        /// </summary>
        private void ViewProjectOfflineFile(INxlFile nxlFile, bool isFromContextMenu = false)
        {
            App.Log.Info("View Project Offline File -->");

            if (!IsNetworkAvailable)
            {
                StartViewerProcess(nxlFile);
                return;
            }

            CommonUtils.CheckOfflineFileVersion(projectRepo, currentWorkingArea, nxlFile, (bool isModified) =>
            {
                if (isModified)
                {
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
                            PartialDownloadEx((bool result,  INxlFile nxl)=> {
                                if (result && !string.IsNullOrEmpty(nxl.PartialLocalPath))
                                {
                                    this.win.Dispatcher.Invoke(new Action(delegate() {
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
        /// Handle conflict issue when view the project offline file
        /// </summary>
        private void HandleConflictWhenView(INxlFile nxlFile)
        {
            if (IsSameTags(nxlFile)) // means content is edit in remote, prompt user if is update from server or not.
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
            }
            else // means rights is modified, should enforce user to update file first.
            {
                if (CustomMessageBoxResult.Positive == Edit.Helper.ShowEnforceUpdateDialog(nxlFile.Name))
                {
                    SyncModifiedFileFromRms(nxlFile, true);
                }
            }
        }

        /// <summary>
        /// Judge if rights is modified or not.
        /// </summary>
        /// <param name="nxlFile"></param>
        /// <returns></returns>
        private bool IsSameTags(INxlFile nxlFile)
        {
            try
            {
                var fpOld = SkydrmLocalApp.Singleton.Rmsdk.User.GetNxlFileFingerPrint(nxlFile.LocalPath);
                var fpNew = SkydrmLocalApp.Singleton.Rmsdk.User.GetNxlFileFingerPrint(nxlFile.PartialLocalPath);

                return NxlFileFingerPrint.IsSameTags(fpOld.tags, fpNew.tags);
            }
            catch (Exception e)
            {
                App.Log.Error("Error in IsSameTags",e);
                throw e;
            }
        }

        /// <summary>
        /// Sync the modified file node info and re-download from rms.
        /// </summary>
        private void SyncModifiedFileFromRms(INxlFile nxlFile, bool bIsView = true)
        {
            // user select sync from rms, should reset this field
            nxlFile.IsMarkedFileRemoteModified = false;

            // user view edited file from "offline" filter.
            if (currentWorkingArea == EnumCurrentWorkingArea.FILTERS_OFFLINE 
                && nxlFile.FileRepo == EnumFileRepo.REPO_PROJECT)
            {
                projectRepo.SyncDestFile(nxlFile, (bool bSuccess, INxlFile updatedFile) =>
                {
                    OnSyncModifiedFinished(bSuccess, updatedFile, bIsView);
                }, true);

            }
            else
            {
                projectRepo.SyncDestFile(nxlFile, (bool bSuccess, INxlFile updatedFile) =>
                {
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
                    ReDownloadProjectFile(updatedFile, bIsView);
                }
                else
                {
                    App.ShowBalloonTip(string.Format(CultureStringInfo.Sync_ModifiedFile_download_Failed, updatedFile.Name));
                }
            }
            else
            {
                App.ShowBalloonTip(string.Format(CultureStringInfo.Sync_ModifiedFile_download_Failed, updatedFile.Name));
            }
        }

        private void ReDownloadProjectFile(INxlFile updatedFile, bool bIsView = false)
        {
            updatedFile.FileStatus = EnumNxlFileStatus.Downloading;
            (updatedFile as NxlDoc).IsMarkedOffline = true; // used to bind ui
            (updatedFile as NxlDoc).IsEdit = false; // update db

            projectRepo.MarkOffline(updatedFile, (bool result) =>
            {
                App.Log.Info("in ReDownloadProjectFile, result: " + result.ToString());

                // update file status
                NxlDoc doc = updatedFile as NxlDoc;
                doc.FileStatus = EnumNxlFileStatus.AvailableOffline;
                doc.Location = EnumFileLocation.Local;
                doc.SourcePath = projectRepo.GetSourcePath(doc);

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
                    App.ShowBalloonTip(string.Format(CultureStringInfo.Sync_ModifiedFile_download_Failed, updatedFile.Name));
                }
            });
        }

        // Reset current working repo when is in Offline Filter and select file. -- fix bug 53558
        public void ResetCurrentWorkRepoWhenInOfflineFilterArea()
        {
            if (CurrentWorkingArea == EnumCurrentWorkingArea.FILTERS_OFFLINE && CurrentSelectedFile != null)
            {
                if (CurrentSelectedFile.FileRepo == EnumFileRepo.REPO_PROJECT)
                {
                    currentWorkRepo = projectRepo;
                } else if (CurrentSelectedFile.FileRepo == EnumFileRepo.REPO_SHARED_WITH_ME)
                {
                    currentWorkRepo = sharedWithRepo;
                } else // for other, default is myVault repo.
                {
                    currentWorkRepo = myVaultRepo;
                }
            }
        }

        public void AllOfflineClick()
        {
            CurrentWorkingArea = EnumCurrentWorkingArea.FILTERS_OFFLINE;
            SetCurSavePath();

            ResetCurrentSelectedFile();

            ChangeShareBtnTag();

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
                AddSrcFileToDest(sharedWithRepo.GetOfflines(), offlineFiles);
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

            // Then should sync project from rms, because the selected dest folder(Project or project folder) maybe have been deleted. -- fix bug 55652
            projectRepo.SyncRemoteData((bool bSuccess, IList<ProjectData> result) =>
            {
                if (bSuccess && CurrentWorkingArea == EnumCurrentWorkingArea.FILTERS_OUTBOX)
                {
                    // Display again after sync.
                    OutBoxClick();
                }
            });
        }

        public void OutBoxClick()
        {
            CurrentWorkingArea = EnumCurrentWorkingArea.FILTERS_OUTBOX;
            CurrentSaveFilePath = MY_VAULT;

            ResetCurrentSelectedFile();

            ChangeShareBtnTag();

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
        public void DefaultSort()
        {

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
                case EnumCurrentWorkingArea.MYVAULT:
                case EnumCurrentWorkingArea.SHARED_WITH_ME:
                    CurrentSaveFilePath = MY_VAULT;
                    break;
                case EnumCurrentWorkingArea.PROJECT:
                    CurrentSaveFilePath = MY_VAULT;
                    break;
                case EnumCurrentWorkingArea.PROJECT_ROOT:
                    CurrentSaveFilePath = projectRepo.CurrentWorkingProject.ProjectInfo.DisplayName + "/";
                    break;
                case EnumCurrentWorkingArea.PROJECT_FOLDER:
                    string rootpath = projectRepo.CurrentWorkingProject.ProjectInfo.DisplayName + "/";
                    if (projectRepo.CurrentWorkingFolder.PathDisplay.Length > 1)
                    {
                        string folderpath = projectRepo.CurrentWorkingFolder.PathDisplay.Substring(1);
                        CurrentSaveFilePath = rootpath + folderpath;
                    }

                    break;
                default:
                    CurrentSaveFilePath = MY_VAULT;
                    break;
            }
        }

        // Because Share button's style do not support "Button.Tag" binging , we will change icon by Tag
        private void ChangeShareBtnTag()
        {
            switch (currentWorkingArea)
            {
                case EnumCurrentWorkingArea.PROJECT:
                case EnumCurrentWorkingArea.PROJECT_ROOT:
                case EnumCurrentWorkingArea.PROJECT_FOLDER:
                    this.win.Share_File.Tag = "/rmc/resources/icons/Icon_share_gray.png";
                    break;
                default:
                    this.win.Share_File.Tag = "/rmc/resources/icons/Icon_share.png";
                    break;
            }
        }

        public void ChangeFilterBtnBackground()
        {
            switch (currentWorkingArea)
            {
                case EnumCurrentWorkingArea.MYVAULT:
                case EnumCurrentWorkingArea.SHARED_WITH_ME:
                case EnumCurrentWorkingArea.PROJECT:
                case EnumCurrentWorkingArea.PROJECT_ROOT:
                case EnumCurrentWorkingArea.PROJECT_FOLDER:
                    win.BtnOffline.Background = colorTransparent;
                    win.BtnOutbox.Background = colorTransparent;
                    break;
                case EnumCurrentWorkingArea.FILTERS_OFFLINE:
                    win.BtnOffline.Background = colorBule;
                    win.BtnOutbox.Background = colorTransparent;
                    break;
                case EnumCurrentWorkingArea.FILTERS_OUTBOX:
                    win.BtnOffline.Background = colorTransparent;
                    win.BtnOutbox.Background = colorBule;
                    break;
                default:
                    break;
            }
        }

        // Find the project(ProjectViewModel) by user selected folder(FolderViewModel).
        private ProjectData FindProject(FolderViewModel folder)
        {
            TreeViewItemViewModel parent = folder.Parent;
            if (parent is FolderViewModel)
            {
                return FindProject(parent as FolderViewModel);
            }

            return (parent as ProjectViewModel).Project;
        }

        public void GetAllPendingFiles(ref IList<INxlFile> outSet)
        {
            AddSrcFileToDest(projectRepo.GetPendingUploads(), outSet);
            AddSrcFileToDest(myVaultRepo.GetPendingUploads(), outSet);
            AddSrcFileToDest(sharedWithRepo.GetPendingUploads(), outSet);
        }

        public IList<INxlFile> GetAllEditedFiles()
        {
           return projectRepo.GetEditedOfflineFiles();
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

            ReDownloadProjectFile(nxlFile);
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
                if (one.Name == nxlFile.Name)
                {
                    one.FileStatus = status;
                    break;
                }
            }

            foreach (var one in copyFileList)
            {
                if (one.Name == nxlFile.Name)
                {
                    one.FileStatus = status;
                    break;
                }
            }

        }

        // Display all project list.
        private void LoadProjectPageData(List<ProjectData>projects)
        {
            if (projects.Count>0)
            {
                CollectionProject.Clear();
                var sortList = projects.OrderByDescending(a=>a.ProjectInfo.BOwner);
                sortList.ToList();
                foreach (var item in sortList)
                {
                    CollectionProject.Add(item);
                }
            }
            CollectionProjectCount = projects.Count;
            App.Log.Info("LoadProjectPageData: project count "+ CollectionProjectCount);
        }
        // Refresh all project list.
        private void RefreshProjectPageData(List<ProjectData> addProjects, List<ProjectData> removeProjects)
        {
            if (addProjects.Count>0)
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
            CollectionProjectCount = CollectionProject.Count;
            App.Log.Info("RefreshProjectPageData: addProjects Count " + addProjects.Count+ ", removeProjects Count" + removeProjects.Count
                + ", Collection Project Count "+ CollectionProjectCount);
        }

        public void SetListView(IList<INxlFile> list)
        {
            if (list == null)
            {
                return;
            }

            nxlFileList.Clear();
            copyFileList.Clear();

            for(int i = 0; i < list.Count; i++)
            {
                INxlFile one = list[i];
                // restore status 
                if (one.FileStatus != EnumNxlFileStatus.Uploading
                    && UploadManagerEx.GetInstance().FileIfIsUploading(one))
                {
                    one.FileStatus = EnumNxlFileStatus.Uploading;
                }

                nxlFileList.Add(one);
                copyFileList.Add(one);
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
        /// Try to upload local file into MyVault if exist have not been uploaded.
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
                    SkydrmLocalApp.Singleton.ShowBalloonTip("Invalid operation, file folder can't be protected.");
                    return;
                }
            }

            if (CurrentWorkingArea == EnumCurrentWorkingArea.PROJECT)
            {
                SkydrmLocalApp.Singleton.ShowBalloonTip(CultureStringInfo.Common_SavePath_BalloonTip);
                return;
            }

            string[] selectedFile;
            selectedFile = path;
            if (projectRepo.CurrentWorkingProject != null)
            {
                App.Mediator.OnShowProtect(selectedFile, this.win, true, projectRepo.CurrentWorkingProject.ProjectInfo.Raw);
            }
            else
            {
                App.Mediator.OnShowProtect(selectedFile, this.win, true, null);
            }
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
            var fileType = CommonUtils.GetFileTypeByExtension(file.Name);
            if (fileType == EnumFileType.FILE_TYPE_NOT_SUPPORT)
            {
                NotifyViewer(file.RmsRemotePath, IPCManager.WM_UNSUPPORTED_FILE_TYPE, file.Name);
                return;
            }

            // download partial for check rights
            currentWorkRepo.DownloadFile(file, true, (bool result) =>
            {
                // sanity check
                if (!result)
                {
                    NotifyViewer(file.RmsRemotePath, IPCManager.WM_DOWNLOAD_FAILED, file.Name);
                    return;
                }
                string PartialLocalPath = file.PartialLocalPath;
                if (!IsHasViewRights(PartialLocalPath))
                {
                    // record log
                    SkydrmLocalApp.Singleton.User.AddNxlFileLog(PartialLocalPath, NxlOpLog.View, false);
                    NotifyViewer(file.RmsRemotePath, IPCManager.WM_HAS_NO_RIGHTS, file.Name);
                    return;
                }
                try
                {
                    App.Log.Info(PartialLocalPath + "has view rights");
                    // by comment, if online view ok, log will be send at ExecuteView();
                    //SkydrmLocalApp.Singleton.User.AddNxlFileLog(localPath, NxlOpLog.View, true);

                    // delete the partial file
                    FileHelper.Delete_NoThrow(PartialLocalPath);

                    // download full again
                    DownloadFile(file, (bool isSucceeded) =>
                    {
                        if (isSucceeded)
                        {
                            ExecuteView(file.RmsRemotePath);
                        }
                        else
                        {
                            NotifyViewer(file.RmsRemotePath, IPCManager.WM_DOWNLOAD_FAILED, file.Name);
                        }
                    });
                }
                catch (Exception e)
                {
                    App.Log.Info(e.ToString(), e);
                    NotifyViewer(file.RmsRemotePath, IPCManager.WM_DOWNLOAD_FAILED, file.Name);
                }
            }, true);
        }

        // Download file for online view
        public void DownloadFile(INxlFile nxlFile,Action<bool> callBack)
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

            public StartViewerInfo(string MainWindowIntPtr , string SecretSignal, bool IsOnlieView)
            {
                this.MainWindowIntPtr = MainWindowIntPtr;
                this.SecretSignal = SecretSignal;
                this.IsOnlieView = IsOnlieView;
            }
        }

        public void StartViewerProcess(INxlFile nxlFile) // default is view local file
        {
            if (null == nxlFile)
            {
                return;
            }

            WindowInteropHelper wndHelper = new WindowInteropHelper(win);

            IntPtr hwnd = wndHelper.Handle;
            string intPtr = hwnd.ToInt32().ToString();

            string key = string.Empty;

            bool isOnlieView = false;

            // the file is online file
            if (nxlFile.Location == EnumFileLocation.Online)
            {
                key = nxlFile.RmsRemotePath;
                isOnlieView = true;
            }
            else
            {
                key = nxlFile.LocalPath;
                isOnlieView = false;
            }

            StartViewerInfo startViewerInfo = new StartViewerInfo(intPtr, key, isOnlieView);

            string jsonData = JsonConvert.SerializeObject(startViewerInfo);

            jsonData = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonData));

            ViewerProcess ViewerProcess = new ViewerProcess("FromMainInfo", jsonData);

            ViewerProcess.CurrentSelectedFile = nxlFile;

            ViewerProcess.Start(key);

        }

        private void ExecuteView(string key)
        {  
            new Thread(new ParameterizedThreadStart(ExecuteViewInBackground)) { Name = "ExecuteViewInBackground", IsBackground = true ,Priority= ThreadPriority.Normal}.Start(key);     
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

           if( !ViewerProcess.GetValueByKey(obj.ToString(),out vp) ){

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
                    Menu_ProtectFile();
                    break;

                case Constant.MENU_SHARE_FILE:

                    // Handle shortcut disable -- fix bug 51118.
                    if (CurrentWorkingArea == EnumCurrentWorkingArea.PROJECT ||
                      CurrentWorkingArea == EnumCurrentWorkingArea.PROJECT_ROOT ||
                      CurrentWorkingArea == EnumCurrentWorkingArea.PROJECT_FOLDER)
                    {
                        return;
                    }

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
                case Constant.COMMAND_OPEN_WEB:
                    DoOpenWeb();
                    break;
                case Constant.COMMAND_START_UPLOAD:
                    DoUpload();
                    break;
                case Constant.COMMAND_STOP_UPLOAD:
                    StopUpload();
                    break;
                case Constant.COMMAND_PREFERENCE:
                    DoPreference();
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
                default:
                    break;
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
                    case Constant.CONTEXT_MENU_CMD_VIEW_FILE:
                        ContextMenu_DoViewFile(cmdArgs);
                        break;
                    case Constant.CONTEXT_MENU_CMD_VIEW_FILE_INFO:
                        ContextMenu_DoViewFileInfo(cmdArgs.SelectedFile);
                        break;
                    case Constant.CONTEXT_MENU_CMD_REMOVE:
                        ContextMenu_DoViewRemove(cmdArgs);
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
                    case Constant.CONTEXT_MENU_CMD_SHARE_TO_PERSON:
                        ContextMenu_DoShareNxlToPerson(cmdArgs.SelectedFile);
                        break;
                    case Constant.CONTEXT_MENU_CMD_SHARE_TO_PROJECT:
                        ContextMenu_AddFileToProject(cmdArgs.SelectedFile);
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
                    case Constant.CONTEXT_MENU_CMD_ADD_FILE:
                        ContextMenu_TreeViewAddFile();
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
            Console.WriteLine("---Open file-----");
            if (CurrentSelectedFile == null)
            {
                return;
            }

            // start viewer process 
            StartViewerProcess(CurrentSelectedFile);
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
                        Window win = new FileInformationWindow(target.FileInfo);
                        win.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                        // Use file name as the tag(since the file name in local list is unique.)
                        win.Tag = winTag;

                        win.Focus();
                        win.Activate();
                        win.Show();
                    }
                    else
                    {
                        opennedWin.Show();
                        opennedWin.Activate();
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
                if (rt)
                {
                    feature.Do();
                }
                else
                {
                    App.ShowBalloonTip("You have no permission to access the file.");
                }
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

            App.Logout(win);
        }

        private void Menu_Exit()
        {
            // If menu item is disabled(but can't effect Input Gesture, so intercept here).
            if (!win.Menu_exit.IsEnabled)
            {
                return;
            }

            App.MaunalExit();
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
        private bool OpenFileDialog(out string[] selectedFile)
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
                dialog.Multiselect = true;

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

        public void DoProtect()
        {
            if (CurrentWorkingArea == EnumCurrentWorkingArea.PROJECT)
            {
                SkydrmLocalApp.Singleton.ShowBalloonTip(CultureStringInfo.Common_SavePath_BalloonTip);
                return;
            }

            try
            {
                string[] selectedFile;
                if (OpenFileDialog(out selectedFile))
                {
                    
                    if (projectRepo.CurrentWorkingProject != null)
                    {
                        App.Mediator.OnShowProtect(selectedFile, this.win, true, projectRepo.CurrentWorkingProject.ProjectInfo.Raw);
                    }
                    else
                    {
                        App.Mediator.OnShowProtect(selectedFile, this.win, true, null);
                    }
                }            
            }
            catch (Exception e)
            {
                App.Log.Warn("Exception in DoProtect," + e, e);
            }

        }

        public void DoShare()
        {
            if (CurrentWorkingArea == EnumCurrentWorkingArea.PROJECT)
            {
                SkydrmLocalApp.Singleton.ShowBalloonTip(CultureStringInfo.Common_SavePath_BalloonTip);
                return;
            }

            try
            {
                string[] selectedFile;
                if (OpenFileDialog(out selectedFile))
                {
                    // popup share window
                    App.Mediator.OnShowShare(selectedFile, this.win, true);
                }
                
            }
            catch (Exception e)
            {
                App.Log.Warn("Exception in DoShare," + e, e);
            }

        }

        private void DoOpenWeb()
        {
            try
            {
                CommonUtils.OpenSkyDrmWeb();
            }
            catch (Exception e)
            {
                App.Log.Warn("Exception in DoOpenWeb," + e, e);
            }
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

        private void DoPreference()
        {
            App.Mediator.OnShowPreference();
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
                switch (currentWorkingArea)
                {
                    case EnumCurrentWorkingArea.MYVAULT:
                        SetListView(currentWorkRepo.GetLocalFiles());

                        // At the same time, sync from rms.
                        RefreshMyVault(isSort);
                        break;

                    // Used to refresh project pages list when user click "refresh button"
                    case EnumCurrentWorkingArea.PROJECT:
                        // fix bug 55730, but not contain waitUpload file.
                        RefreshProject();
                        
                        break;

                    case EnumCurrentWorkingArea.PROJECT_ROOT:
                        SetListView(currentWorkRepo.GetLocalFiles());
                        RefreshProjectRoot(isSort);
                        break;

                    case EnumCurrentWorkingArea.PROJECT_FOLDER:
                        SetListView(currentWorkRepo.GetLocalFiles());
                        RefreshProjectFolder(isSort);
                        break;

                    case EnumCurrentWorkingArea.SHARED_WITH_ME:
                        SetListView(currentWorkRepo.GetLocalFiles());
                        RefreshShareWithMe(isSort);
                        break;

                    case EnumCurrentWorkingArea.FILTERS_OFFLINE:
                        AllOfflineClick();
                        break;

                    case EnumCurrentWorkingArea.FILTERS_OUTBOX:
                        ManualRefreshOutBox();
                        break;

                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                App.Log.Warn("Exception in DoRefresh," + e, e);
            }

            // default sort
            if (isSort && currentWorkingArea != EnumCurrentWorkingArea.PROJECT)
            {
                DefaultSort();
            }

            // Should continue to do search if user is searching.
            if (!string.IsNullOrEmpty(searchText))
            {
                InnerSearch(searchText);
            }

        }

        private void RefreshMyVault(bool isSort)
        {
            if (IsNetworkAvailable)
            {
                currentWorkRepo.SyncFiles((bool bSuccess, IList<INxlFile> results, string reserved) =>
                {
                    //  May current treeview item has been switched into others before sync complete.
                    if (bSuccess && currentWorkingArea == EnumCurrentWorkingArea.MYVAULT)
                    {
                        UpdateView(results, isSort);

                        // Should continue to do search if user is searching --- fix bug 51382
                        if (!string.IsNullOrEmpty(searchText))
                        {
                            InnerSearch(searchText);
                        }

                    }

                });
            }
        }

        private void RefreshProject()
        {
            if (IsNetworkAvailable)
            {
                projectRepo.SyncRemoteData((bool bSuccess, IList<ProjectData> result) =>
                {
                    if (bSuccess && CurrentWorkingArea == EnumCurrentWorkingArea.PROJECT && result.Count > 0)
                    {
                        App.Log.Info("RefreshProject: result count " +result.Count);
                        // Display again after sync.
                        var list = result.OrderBy(p => p.ProjectInfo.Name).ToList();
                        var fpList = list.OrderByDescending(p => p.ProjectInfo.BOwner).ToList();
                        LoadProjectPageData(fpList);

                        // Update TreeView 
                        List<ProjectData> addProject = new List<ProjectData>();
                        List<ProjectData> removeProject = new List<ProjectData>();
                        RepoViewModel.GetRootViewModel()?.MergeTreeView(fpList, addProject, removeProject);
                        App.Log.Info("RefreshProject: addProject count "+ addProject.Count+", removeProject count "+ removeProject.Count);
                    }
                });
            }
        }

        private void RefreshProjectRoot(bool isSort)
        {
            if (IsNetworkAvailable)
            {
                currentWorkRepo.SyncFiles((bool bSuccess, IList<INxlFile> results, string projectId) =>
                {
                    //  May current treeview item has been switched into others before sync complete.
                    if (bSuccess && currentWorkingArea == EnumCurrentWorkingArea.PROJECT_ROOT
                    && projectId == projectRepo.CurrentWorkingProject.ProjectInfo.ProjectId.ToString())
                    {
                        UpdateView(results, isSort);

                        // Should continue to do search if user is searching.
                        if (!string.IsNullOrEmpty(searchText))
                        {
                            InnerSearch(searchText);
                        }

                    }

                }, projectRepo.CurrentWorkingProject.ProjectInfo.ProjectId.ToString());
            }
        }

        private void RefreshProjectFolder(bool isSort)
        {
            if (IsNetworkAvailable)
            {

                currentWorkRepo.SyncFiles((bool bSuccess, IList<INxlFile> results, string oldFlag) =>
                {

                    if (bSuccess && currentWorkingArea == EnumCurrentWorkingArea.PROJECT_FOLDER
                    && oldFlag == projectRepo.CurrentWorkingProject.ProjectInfo.ProjectId.ToString() +
                        projectRepo.CurrentWorkingFolder.PathId)
                    {
                        UpdateView(results, isSort);

                        // Should continue to do search if user is searching.
                        if (!string.IsNullOrEmpty(searchText))
                        {
                            InnerSearch(searchText);
                        }

                    }

                }, projectRepo.CurrentWorkingProject.ProjectInfo.ProjectId.ToString() +
                     projectRepo.CurrentWorkingFolder.PathId);
            }
        }

        private void RefreshShareWithMe(bool isSort)
        {
            if (IsNetworkAvailable)
            {
                currentWorkRepo.SyncFiles((bool bSuccess, IList<INxlFile> results, string reserved) =>
                {

                    if (bSuccess && currentWorkingArea == EnumCurrentWorkingArea.SHARED_WITH_ME)
                    {
                        UpdateView(results, isSort);

                        // Should continue to do search if user is searching.
                        if (!string.IsNullOrEmpty(searchText))
                        {
                            InnerSearch(searchText);
                        }

                    }

                });
            }
        }

        // Need refresh myvault when user remove or upload "leave a copy file" in offline filter,
        // since default upload into myvault if current area is "offline filter", 
        // --- Fix the comment 10 of bug 51938 about "Leave a copy".
        private void RefreshMyVault()
        {
            myVaultRepo?.GetLocalFiles();

            if (IsNetworkAvailable)
            {
                myVaultRepo?.SyncFiles((bool bSuccess, IList<INxlFile> results, string reserved) =>
                {});
            }
        }


        private void UpdateListView(IList<INxlFile> syncResults, bool isSort, bool isHandleLeaveCopy = false)
        {
            CommonUtils.MergeListView(syncResults, nxlFileList, copyFileList);

            if (isHandleLeaveCopy)
            {
                MergeLeaveAcopyFile(syncResults, nxlFileList, copyFileList);
            }

            if (isSort)
            {
                DefaultSort();
            }

        }

        /// <summary>
        /// Update listView when treeView refresh
        /// </summary>
        /// <param name="syncResults"></param>
        /// <param name="param">projectId if trigger in ProjectViewModel or folder pathId if trigger in FolderViewModel</param>
        /// <param name="isFolder">true if is foler, false if is project root</param>
        private void UpdateViewFromTreeView(IList<INxlFile> syncResults, string param, bool isFolder)
        {
            App.Log.Info("Update listView when treeView refresh(Project autoRefresh)");
            IList<INxlFile> added = null;
            IList<INxlFile> removed = null;

            App.Log.Info(CurrentWorkingArea.ToString());

            if (!isFolder && CurrentWorkingArea==EnumCurrentWorkingArea.PROJECT_ROOT
                && param == projectRepo.CurrentWorkingProject.ProjectInfo.ProjectId.ToString())
            {
                App.Log.Info("Merge project root list:");
                App.Log.Info("syncResults list count:"+ syncResults.Count);
                App.Log.Info("nxlFileList list count:"+ nxlFileList.Count);

                CommonUtils.MergeListView(syncResults, nxlFileList, copyFileList, out added, out removed);
                DefaultSort();

                App.Log.Info("added list count:" + added.Count);
                App.Log.Info("removed list count:" + removed.Count);
            }

            if (isFolder && CurrentWorkingArea==EnumCurrentWorkingArea.PROJECT_FOLDER
                && param == projectRepo.CurrentWorkingFolder.PathId)
            {
                App.Log.Info("Merge project folder list:");
                App.Log.Info("syncResults list count:" + syncResults.Count);
                App.Log.Info("nxlFileList list count:" + nxlFileList.Count);

                CommonUtils.MergeListView(syncResults, nxlFileList, copyFileList, out added, out removed);
                DefaultSort();

                App.Log.Info("added list count:" + added.Count);
                App.Log.Info("removed list count:" + removed.Count);
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
            CommonUtils.MergeListView(syncResults, nxlFileList, copyFileList, out added, out removed);

            if (isSort)
            {
                DefaultSort();
            }

            UpdateTreeviewFolder(added, removed);
        }

        /// <summary>
        /// Also should update coreresponding treeview folder nodes when find folder changes during update listview.
        /// </summary>
        private void UpdateTreeviewFolder(IList<INxlFile> added, IList<INxlFile> removed)
        {
            // For the new added folder, also should update the treeView, and also need to try to get its children if have.
            // --- fix bug 52302

            // For remvoed folder, also need to remove from treeview.
            if (removed != null)
            {
                foreach (var one in removed)
                {
                    if (one.IsFolder)
                    {
                        NxlFolder folder = one as NxlFolder;
                        if (CurrentWorkingArea == EnumCurrentWorkingArea.PROJECT_ROOT)
                        {
                            projectRepo.ProjectViewModel?.RemoveFolderItem(folder);
                        }
                        else if (CurrentWorkingArea == EnumCurrentWorkingArea.PROJECT_FOLDER)
                        {
                            projectRepo.FolderViewModel?.RemoveFolderItem(folder);
                        }
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
                        if (CurrentWorkingArea == EnumCurrentWorkingArea.PROJECT_ROOT)
                        {
                            if (projectRepo.ProjectViewModel != null)
                            {
                                // Add the new one into treeview.
                                projectRepo.ProjectViewModel.AddNewItem2Project(folder);

                                // try to get the children of the new node if have.
                                projectRepo.ProjectViewModel.TryToGetChildren(folder.PathId);
                            }
                        }
                        else if (CurrentWorkingArea == EnumCurrentWorkingArea.PROJECT_FOLDER)
                        {
                            // Should consider that open a folder by double click in listView, this case FolderViewModel may is null.
                            if (projectRepo.FolderViewModel != null)
                            {
                                // try to get the folder's children.
                                projectRepo.FolderViewModel.AddFolderItem2CurrentWorkingFolder(folder);

                                // try to get the children of the new node if have.
                                projectRepo.FolderViewModel.TryToGetChildren(folder.PathId);
                            }
                        }
                    }
                }
            }
           
        }

        /// <summary>
        /// Refresh current listView after uploading leave a copy files.
        /// </summary>
        private void RefreshAfterLeaveCopyFileUpload(INxlFile uploadingFile)
        {
            // Is refreshing current working folder after uploaded.
            if (IsRefreshingAfterUpload)
            {
                return;
            }

            switch (currentWorkingArea)
            {
                case EnumCurrentWorkingArea.MYVAULT:

                    if (IsNetworkAvailable)
                    {
                        IsRefreshingAfterUpload = true;
                        currentWorkRepo.SyncFiles((bool bSuccess, IList<INxlFile> results, string reserved) =>
                        {
                            IsRefreshingAfterUpload = false;

                            // May current treeview item has been switched into others before sync complete.
                            if (bSuccess && currentWorkingArea == EnumCurrentWorkingArea.MYVAULT)
                            {
                                UpdateListView(results, true, true);
                            }
                        });
                    }

                    break;

                case EnumCurrentWorkingArea.PROJECT_ROOT:
                    if (IsNetworkAvailable)
                    {
                        IsRefreshingAfterUpload = true;
                        currentWorkRepo.SyncFiles((bool bSuccess, IList<INxlFile> results, string projectId) =>
                        {
                            IsRefreshingAfterUpload = false;

                            //  May current treeview item has been switched into others before sync complete.
                            if (bSuccess && currentWorkingArea == EnumCurrentWorkingArea.PROJECT_ROOT
                            && projectId == projectRepo.CurrentWorkingProject.ProjectInfo.ProjectId.ToString())
                            {
                                UpdateListView(results, true, true);
                            } 
                            else if (!bSuccess)
                            {
                                // Should also update the ui node status if Sync failed(or else, file is still uploading status)
                                uploadingFile.Location = EnumFileLocation.Local;
                                uploadingFile.FileStatus = EnumNxlFileStatus.CachedFile;
                            }


                        }, projectRepo.CurrentWorkingProject.ProjectInfo.ProjectId.ToString());
                    }

                    break;

                case EnumCurrentWorkingArea.PROJECT_FOLDER:

                    if (IsNetworkAvailable)
                    {
                        IsRefreshingAfterUpload = true;
                        currentWorkRepo.SyncFiles((bool bSuccess, IList<INxlFile> results, string oldFlag) =>
                        {
                            IsRefreshingAfterUpload = false;

                            // Still update the ui node status in case of Sync failed.
                            uploadingFile.Location = EnumFileLocation.Local;
                            uploadingFile.FileStatus = EnumNxlFileStatus.CachedFile;

                            //  May current treeview item has been switched into others before sync complete.
                            if (bSuccess && currentWorkingArea == EnumCurrentWorkingArea.PROJECT_FOLDER
                                && oldFlag == projectRepo.CurrentWorkingProject.ProjectInfo.ProjectId.ToString() +
                                projectRepo.CurrentWorkingFolder.PathId)
                            {
                                UpdateListView(results, true, true);
                            }
                            else if (!bSuccess)
                            {
                                // Should also update the ui node status if Sync failed(or else, file is still uploading status)
                                uploadingFile.Location = EnumFileLocation.Local;
                                uploadingFile.FileStatus = EnumNxlFileStatus.CachedFile;
                            }


                        }, projectRepo.CurrentWorkingProject.ProjectInfo.ProjectId.ToString() +
                           projectRepo.CurrentWorkingFolder.PathId);
                    }

                    break;

                default:
                    IsRefreshingAfterUpload = false;
                    break;
            }
        }

        /// <summary>
        /// Refresh current working folder after upload or remove local file.
        /// </summary>
        private void RefreshAfterUploadOrRemove(bool isUpdateList = false, bool isManualRemove = false)
        {
            // Is refreshing current working folder after uploaded.
            if (IsRefreshingAfterUpload)
            {
                return;
            }

            switch (currentWorkingArea)
            {
                case EnumCurrentWorkingArea.MYVAULT:

                    if (IsNetworkAvailable)
                    {
                        IsRefreshingAfterUpload = true;
                        currentWorkRepo.SyncFiles((bool bSuccess, IList<INxlFile> results, string reserved) =>
                        {
                            IsRefreshingAfterUpload = false;
                            // May current treeview item has been switched into others before sync complete.
                            if (bSuccess && isUpdateList && currentWorkingArea == EnumCurrentWorkingArea.MYVAULT)
                            {
                                UpdateListView(results, true);
                            }
                        });
                    }
                    else
                    {
                        // Also need to get remote node when user manual remove "leave a copy" file in offline mode(fix bug 53838).
                        if (isManualRemove)
                        {
                            SetListView(currentWorkRepo.GetLocalFiles());
                            DefaultSort();
                        }
                    }

                    break;

                case EnumCurrentWorkingArea.PROJECT_ROOT:
                    if (IsNetworkAvailable)
                    {
                        IsRefreshingAfterUpload = true;
                        currentWorkRepo.SyncFiles((bool bSuccess, IList<INxlFile> results, string reserved) =>
                        {
                            IsRefreshingAfterUpload = false;
                            //  May current treeview item has been switched into others before sync complete.
                            if (bSuccess && isUpdateList && currentWorkingArea == EnumCurrentWorkingArea.PROJECT_ROOT)
                            {
                                UpdateListView(results, true);
                            }                          
                        });
                    }
                    else
                    {
                        if (isManualRemove)
                        {
                            SetListView(currentWorkRepo.GetLocalFiles());
                            DefaultSort();
                        }
                    }

                    break;

                case EnumCurrentWorkingArea.PROJECT_FOLDER:

                    if (IsNetworkAvailable)
                    {
                        IsRefreshingAfterUpload = true;
                        currentWorkRepo.SyncFiles((bool bSuccess, IList<INxlFile> results, string reserved) =>
                        {
                            IsRefreshingAfterUpload = false;
                            if (bSuccess && isUpdateList && currentWorkingArea == EnumCurrentWorkingArea.PROJECT_FOLDER)
                            {
                                UpdateListView(results, true);
                            }                          
                        });
                    }
                    else
                    {
                        if (isManualRemove)
                        {
                            SetListView(currentWorkRepo.GetLocalFiles());
                            DefaultSort();
                        }
                    }

                    break;

                default:
                    IsRefreshingAfterUpload = false;
                    break;
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
                    App.Log.Info("********** Upload succeed! ********** "+ ur.UploadingFile.Name);

                    // remaing 200ms, then remove it.
                    Thread.Sleep(200);

                    OnUploadSucceed(ur);
                }
                else // failed
                {
                    App.Log.Info("********** Upload Failed! ********** "+ ur.UploadingFile.Name);

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
            if (SkydrmLocalApp.Singleton.User.LeaveCopy)
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
                    if (ur.UploadingFile.FileRepo == EnumFileRepo.REPO_MYVAULT)
                    {
                        // Here only change ui file status, the db file status don't change(actually, there is not this record in db, since file has been removed from
                        //  localFile table, unless user refresh, then will add new record into file table and file status also automatically update -- auto fix in lower lever.)
                        ur.UploadingFile.Location = EnumFileLocation.Local;
                        ur.UploadingFile.FileStatus = EnumNxlFileStatus.CachedFile;

                        // update current listview the corresponding file status if needed.
                        SyncCurrentListViewFileStatus(ur.UploadingFile, EnumNxlFileStatus.CachedFile);

                        HandleLeaveACopy(ur.UploadingFile);
                    } 
                    else // Project repo file -- fix bug 54896
                    {
                        HandleLeaveACopy(ur.UploadingFile);
                    }
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

                    ur.UploadingFile.Location = EnumFileLocation.Online;
                    ur.UploadingFile.FileStatus = EnumNxlFileStatus.Online;

                    // update current listview the corresponding file status if needed.
                    SyncCurrentListViewFileStatus(ur.UploadingFile, EnumNxlFileStatus.Online);
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
            }

        }

        // Refresh to get the new node after uploading.
        private void SyncEditedNode(INxlFile nxlFile)
        {
            // Now only project file support edit file --- fixed bug 54428
            projectRepo.SyncDestFile(nxlFile, (bool bSuccess, INxlFile updatedFile) =>
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

            // User do edit project file from "offline" filter.
            if (currentWorkingArea == EnumCurrentWorkingArea.FILTERS_OFFLINE
                && updatedFile.FileRepo == EnumFileRepo.REPO_PROJECT)
            {
                projectRepo.SyncDestFile(updatedFile, (bool bSuccess, INxlFile newNode) =>
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
                HandleUI(uploadingFile);
            }

            // If current working folder is "Offline filter", also need to add into listview after complete uploading.
            else if (CurrentWorkingArea == EnumCurrentWorkingArea.FILTERS_OFFLINE)
            {
                // update ui
                App.Dispatcher.Invoke((Action)delegate
                {
                    AddToListView(uploadingFile);

                    // Should auto refreh if current area is offline filter(or else user switch to another any project,then switch back to offline filter again, no files).
                    RefreshMyVault();
                });
            }

            // For fix this bug -- that user protect some files then upload(in the setting of "Leave a Copy"), after upload, then 
            // switch into "Offline filter" to check the files, can't get them immediately, must click again.(Actually must call sycFile, 
            // then add one record for "LeaveACopy" in low level).
            App.Dispatcher.Invoke((Action)delegate
            {
                RefreshAfterLeaveCopyFileUpload(uploadingFile);
            });

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
                            HandleUI(fileToDelete);

                            //  Also need to remove from waiting queue if exist.
                            UploadManagerEx.GetInstance().RemoveFromQueue(fileToDelete);

                            // Should refresh when user remove a cached file to get remote one(For "Offline filter", don't need to refresh.)
                            if (status == EnumNxlFileStatus.CachedFile) 
                            {
                                if (CurrentWorkingArea != EnumCurrentWorkingArea.FILTERS_OFFLINE)
                                {
                                    RefreshAfterUploadOrRemove(true, true);
                                }
                                else // Fix the comment 10 of bug 51938 about "Leave a copy".
                                {
                                    RefreshMyVault();
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

        // Handle auto remove after uploading ---- now, ignore "leave a copy"
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
                App.Log.Info("********** Remove file failed! **********",e);
            }
            finally
            {
                if (bSuccess)
                {
                    HandleUI(fileToDelete);

                    // refresh to get remote new one.
                    App.Dispatcher.Invoke((Action)delegate
                    {
                        RefreshAfterUploadOrRemove(true, false);
                    });

                }
            }

        }

        private void HandleUI(INxlFile fileToDelete)
        {
            // udate ui.
            // remove local
            App.Dispatcher.Invoke((Action)delegate
            {
                bool r1 = nxlFileList.Remove(fileToDelete);
                bool r2 = copyFileList.Remove(fileToDelete);

                // May are different INxlFile objects when re-get from local db.
                if (!r1 || !r2)
                {
                    CommonUtils.RemoveListFile(nxlFileList, copyFileList, fileToDelete.Name);
                }

            });

            // if the nxl file removed is current selected file, must reset "CurrentSelectedFile" as null after remove.
            if (CurrentSelectedFile != null && fileToDelete == CurrentSelectedFile)
            {
                ResetCurrentSelectedFile();
            }
        }

        /// <summary>
        ///  Get created files
        /// </summary>
        /// <param name="newFiles">newly created files</param>
        /// <param name="selectedDestPath">user selected save dest path</param>
        public void GetCreatedFile(List<INxlFile> newFiles, string selectedDestPath)
        {
            try
            {
                //HandleNewFiles(newFiles, selectedDestPath);

                // Get new created files by refresh instead of original way --- 'HandleNewFiles', which need to hold many case, --- fix some bugs.
                DoRefresh();

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


        // Set treeview automatically navigate when user click listview folder.
        private void TreeviewNavigate()
        {
            IsTriggerRefresh = false;

            if (string.IsNullOrEmpty(CurrentSaveFilePath))
            {
                return;
            }

            try
            {
                if (CurrentSaveFilePath.Contains("/"))
                {
                    string[] nodeName = CurrentSaveFilePath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);//  allentest1/ allen/
                    foreach (var item in RepoViewModel.Roots) //RootViewModel
                    {
                        if (item.RootName.Equals(PROJECT, StringComparison.CurrentCultureIgnoreCase))
                        {
                            RootViewModel rootView = item as RootViewModel;
                            rootView.IsExpanded = true;
                            rootView.IsSelected = true; // Will trigger "TreeViewItemChanged"


                            ObservableCollection<TreeViewItemViewModel> Children = new ObservableCollection<TreeViewItemViewModel>();
                            Children = rootView.Children;// ProjectViewModel

                            for (int i = 0; i < nodeName.Length; i++)
                            {
                                foreach (var childrenItem in Children)// ProjectViewModel
                                {
                                    if (childrenItem is ProjectViewModel)
                                    {
                                        ProjectViewModel projectView = childrenItem as ProjectViewModel;
                                        if (projectView.ProjectName == nodeName[i] 
                                            && projectView.Project.ProjectInfo.ProjectId == projectRepo.CurrentWorkingProject.ProjectInfo.ProjectId)
                                        {
                                            Children = projectView.Children;

                                            projectView.IsExpanded = true;
                                            projectView.IsSelected = true; // Will trigger "TreeViewItemChanged"
                                            break;
                                        }
                                    }
                                    else if (childrenItem is FolderViewModel)
                                    {
                                        FolderViewModel folderView = childrenItem as FolderViewModel;
                                        if (folderView.FolderName == nodeName[i])
                                        {
                                            Children = folderView.Children;

                                            folderView.IsExpanded = true;
                                            folderView.IsSelected = true; // Will trigger "TreeViewItemChanged"
                                            break;
                                        }
                                    }

                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                App.Log.Warn("Exception in TreeviewNavigate," + e, e);
            }

            // reset
            IsTriggerRefresh = true;

        }

        // Select project item in treeview when user double click in Project page ListBox
        public void TreeviewProjectNavigate(ProjectData selectProject)
        {
            if (selectProject == null)
            {
                return;
            }
            foreach (var item in RepoViewModel.Roots) //RootViewModel
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
                                projectView.IsExpanded = true;
                                projectView.IsSelected = true; // Will trigger "TreeViewItemChanged"
                                break;
                            }
                        }

                    }
                }
            }
        }


        /// <summary>
        /// Judge the selected dest path if is current working folder when user protect\share file.
        /// </summary>
        /// <param name="savepath">the selected dest path when protect  to myVault or project.</param>
        /// <returns></returns>
        private bool IsSavePathEquelCurrentWorkingFolder(string savepath)
        {
            EnumCurrentWorkingArea cwa = EnumCurrentWorkingArea.MYVAULT;
            try
            {
                if (savepath.Equals(MY_VAULT, StringComparison.CurrentCultureIgnoreCase))
                {
                    foreach (var item in RepoViewModel.Roots)//RootViewModel
                    {
                        if (item.RootName == savepath) //MyVault
                        {
                            RootViewModel rootView = item as RootViewModel;
                        }
                    }

                    cwa = EnumCurrentWorkingArea.MYVAULT;
                }
                else
                {
                    if (savepath.Contains("/"))
                    {
                        string[] nodeName = savepath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);//  allentest1/ allen/
                        foreach (var item in RepoViewModel.Roots) //RootViewModel
                        {
                            if (item.RootName.Equals(PROJECT, StringComparison.CurrentCultureIgnoreCase))
                            {
                                RootViewModel rootView = item as RootViewModel;

                                ObservableCollection<TreeViewItemViewModel> Children = new ObservableCollection<TreeViewItemViewModel>();
                                Children = rootView.Children;// ProjectViewModel

                                for (int i = 0; i < nodeName.Length; i++)
                                {
                                    foreach (var childrenItem in Children)// ProjectViewModel
                                    {
                                        if (childrenItem is ProjectViewModel)
                                        {
                                            ProjectViewModel projectView = childrenItem as ProjectViewModel;
                                            // If use CurrentWorkingProject.ProjectInfo.ProjectId, will have bug 54078 local NXL file 'add file to project' to project, file can show in myvault
                                            if (projectView.ProjectName == nodeName[i] 
                                                && projectView.Project.ProjectInfo.ProjectId == projectRepo.CurrentWorkingProject.ProjectInfo.ProjectId)
                                            {
                                                Children = projectView.Children;

                                                cwa = EnumCurrentWorkingArea.PROJECT_ROOT;
                                                break;
                                            }
                                        }
                                        else if (childrenItem is FolderViewModel)
                                        {
                                            FolderViewModel folderView = childrenItem as FolderViewModel;
                                            if (folderView.FolderName == nodeName[i])
                                            {
                                                Children = folderView.Children;

                                                cwa = EnumCurrentWorkingArea.PROJECT_FOLDER;
                                                break;
                                            }
                                        }

                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                App.Log.Warn("Exception in IsCurrentWorkingFolder," + e, e);
            }

            return cwa == CurrentWorkingArea;
        }

        /// <summary>
        ///   If current working folder is outBox, will add new created file into this, no matter whether  protect/share by main window or explorer.
        /// </summary>
        private void AddNewFiles2Outbox(List<INxlFile> files, bool isAdd2ListView = true)
        {
            foreach (INxlFile one in files)
            {
                if (one.FileRepo == EnumFileRepo.REPO_MYVAULT)
                {
                    one.SourcePath = "MyVault://" + one.Name;
                }
                else if (one.FileRepo == EnumFileRepo.REPO_PROJECT)
                {
                    one.SourcePath = "Project://" + CurrentSaveFilePath + one.Name;
                }

                // Add file into upload queue if not been uploaded.
                if (one.FileStatus == EnumNxlFileStatus.WaitingUpload)
                {
                    UploadManagerEx.GetInstance().AddToWaitingQueue(one);
                }

                if (isAdd2ListView)
                {
                    AddToListView(one);
                }
            }
        }

        private void protect2MyVault(List<INxlFile> files, bool isNeedAddToListView = true)
        {
            foreach (INxlFile one in files)
            {
                one.SourcePath = myVaultRepo.GetSourcePath(one);
                myVaultRepo.FilePool.Add(one);

                // add pending
                myVaultRepo.GetPendingUploads().Add(one);

                // Add file into upload queue if not been uploaded.
                if (one.FileStatus == EnumNxlFileStatus.WaitingUpload)
                {
                    UploadManagerEx.GetInstance().AddToWaitingQueue(one);
                }

                // add to current listview when current working area is myvault or outbox filter.
                if (isNeedAddToListView)
                {
                    AddToListView(one);
                }
            }
        }

        // Handle the case that selected save path equals current working folder.
        private void HandleEqualsWorkingFolder(List<INxlFile> files)
        {
            try
            {
                switch (CurrentWorkingArea)
                {
                    case EnumCurrentWorkingArea.MYVAULT:

                        protect2MyVault(files);

                        // Whether UI is displayed or not
                        IsDisplayTreeview = RepoViewModel.Roots.Count > 0 ? true : false;

                        break;
                    case EnumCurrentWorkingArea.PROJECT_ROOT:

                        foreach (INxlFile one in files)
                        {
                            one.SourcePath = currentWorkRepo.GetSourcePath(one);
                            projectRepo.CurrentWorkingProject.FileNodes.Add(one);

                            // add pending
                            projectRepo.GetPendingUploads().Add(one);

                            // Add file into upload queue if not been uploaded.
                            if (one.FileStatus == EnumNxlFileStatus.WaitingUpload)
                            {
                                UploadManagerEx.GetInstance().AddToWaitingQueue(one);
                            }

                            AddToListView(one);
                        }

                        break;
                    case EnumCurrentWorkingArea.PROJECT_FOLDER:

                        foreach (INxlFile one in files)
                        {
                            one.SourcePath = currentWorkRepo.GetSourcePath(one);
                            projectRepo.CurrentWorkingFolder.Children.Add(one);

                            // add pending
                            projectRepo.GetPendingUploads().Add(one);

                            // Add file into upload queue if not been uploaded.
                            if (one.FileStatus == EnumNxlFileStatus.WaitingUpload)
                            {
                                UploadManagerEx.GetInstance().AddToWaitingQueue(one);
                            }

                            AddToListView(one);
                        }

                        break;
                }
            }
            catch (Exception e)
            {
                App.Log.Warn("Exception in HandleCanSelectPath," + e, e);
            }
        }

        // Handle the case that the selected save path don't equel current working folder.
        // Including follow case: 
        // 1. Selected save path can't be "outbox filter", "offline filter" and "ShareWithMe" and so on, but current working folder can be.
        // 2. Current working folder don't equal selected path when protect/share by explorer(since user can change the dest path).
        private void HandleNotEqualsWorkingFolder(List<INxlFile> files)
        {
            // If current working folder is outBox, will add new created file into this, no matter whether protect/share by main window or explorer.
            if (CurrentWorkingArea == EnumCurrentWorkingArea.FILTERS_OUTBOX)
            {
                AddNewFiles2Outbox(files);
            }
            else  // For other, only add created file into uploading queue.
            {
                AddNewFiles2Outbox(files, false);
            }
        }

        /// <summary>
        /// Try to get new created files.
        /// </summary>
        private void HandleNewFiles(List<INxlFile> files, string selectedDestPath)
        {
            // The selected save path don't equal current working folder.
            // So don't need to add newly created file into current working list(when then user switch into the selected save folder, will add them from db).
            // Here use the passed "selectedDestPath" instead of "CurrentSaveFilePath" --- fix bug 52674
            if (!IsSavePathEquelCurrentWorkingFolder(selectedDestPath))
            {
                HandleNotEqualsWorkingFolder(files);
            }
            else // The selected save path equel current working folder.
            {
                HandleEqualsWorkingFolder(files);
            }

            // Try to upload
            if (App.User.StartUpload) 
            {
                TryToUpload();
            }

        }

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

        ShareWindow shareWin;
        NxlFileToConvertWindow nxlFileConverWin;
        /// <summary>
        /// Share NxlFile to Person
        /// </summary>
        /// <param name="nxlFile"></param>
        private void ContextMenu_DoShareNxlToPerson(INxlFile nxlFile)
        {
            // Share online file when don't load viewer.
            if (nxlFile.Location == EnumFileLocation.Online && !ViewerProcess.ContainsKey(nxlFile.RmsRemotePath)) // online 
            {
                App.Log.Info("File download, in Share NxlFile To Person");
                currentWorkRepo.DownloadFile(nxlFile, true, (bool result) =>
                {
                    if (result)
                    {
                        //Share file to person (contain updateRecipient or protect&share)
                        IShareNxlFeature shareNxlFile = new ShareNxlFeature(ShareNxlFeature.ShareNxlAction.Share, nxlFile.LocalPath, false);
                        ProtectAndShareConfig config = null;
                        if (!shareNxlFile.BuildConfig(out config))
                        {
                            //delete download file?
                            this.win.Dispatcher.Invoke(() =>
                            {
                                shareWin?.Close();
                            });
                            return;
                        }
                        config.ShareNxlFeature = shareNxlFile;
                        this.win.Dispatcher.Invoke(() =>
                        {
                            shareWin?.InitShareConfig(config);
                        });
                    }
                    else
                    {
                        // todo, error handle
                        App.Log.Info("Download failed:"+ nxlFile.Name);
                        App.ShowBalloonTip(CultureStringInfo.Common_System_Internal_Error);
                        this.win.Dispatcher.Invoke(() =>
                        {
                            shareWin?.Close();
                        });
                        
                    }

                }, false, true);

                App.Mediator.OnShowShareWin(out shareWin, this.win, "ShareToPerson");
            }
            else
            {
                // 1. share online file when viewer is loading.
                // 2. share offline file
                App.Mediator.OnShareNxlToPerson(nxlFile.LocalPath, this.win);
            }

        }

        private void ContextMenu_AddFileToProject(INxlFile nxlFile)
        {
            // Share online file when don't load viewer.
            if (nxlFile.Location == EnumFileLocation.Online && !ViewerProcess.ContainsKey(nxlFile.RmsRemotePath)) // online 
            {
                // 1. download file at bg, and disable window
                App.Log.Info("File download, in AddNxlFileToProject");
                currentWorkRepo.DownloadFile(nxlFile, true, (bool result) =>
                {
                    if (result)
                    {
                        // Share file to project.
                        IShareNxlFeature shareNxlFile = new ShareNxlFeature(ShareNxlFeature.ShareNxlAction.AddFileToProject, nxlFile.LocalPath, false);
                        ProtectAndShareConfig config = null;
                        if (!shareNxlFile.BuildConfig(out config))
                        {
                            //delete download file?
                            this.win.Dispatcher.Invoke(() =>
                            {
                                nxlFileConverWin?.Close();
                            });
                            return;
                        }
                        config.ShareNxlFeature = shareNxlFile;
                        this.win.Dispatcher.Invoke(() =>
                        {
                            nxlFileConverWin?.InitConfig(config);
                        });
                    }
                    else
                    {
                        // todo, error handle
                        App.Log.Info("Download failed:" + nxlFile.Name);
                        App.ShowBalloonTip(CultureStringInfo.Common_System_Internal_Error);
                        this.win.Dispatcher.Invoke(() =>
                        {
                            nxlFileConverWin?.Close();
                        });
                    }
                }, false, true);
                // at the same time popup the share "Share nxl window" --- disabled
                //  win = new window and show, disabled
                App.Mediator.OnShowNxlConvertWin(out nxlFileConverWin, this.win, "AddFileToProject");
            }
            else
            {
                // 1. share online file when viewer is loading.
                // 2. share offline file
                CheckModifiedRights(nxlFile, (INxlFile nxl) => {
                    App.Mediator.OnAddNxlFileToProject(nxl.LocalPath, this.win);
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
                    if (result)
                    {
                        IModifyRights modifyRights = new ModifyRightsFeature(nxlFile.LocalPath, nxlFile, false);
                        ProtectAndShareConfig config = null;
                        if (!modifyRights.GetRights(out config))
                        {
                            // need delete download file?
                            this.win.Dispatcher.Invoke(() =>
                            {
                                nxlFileConverWin?.Close();
                            });
                            return;
                        }
                        config.ModifyRightsFeature = modifyRights;
                        this.win.Dispatcher.Invoke(() =>
                        {
                            nxlFileConverWin?.InitConfig(config);
                        });
                    }
                    else
                    {
                        // todo, error handle
                        App.Log.Info("Download failed:" + nxlFile.Name);
                        App.ShowBalloonTip(CultureStringInfo.Common_System_Internal_Error);
                        this.win.Dispatcher.Invoke(() =>
                        {
                            nxlFileConverWin?.Close();
                        });
                    }
                }, false, true);
                // at the same time popup the "Modify rights win" --- disabled
                //  win = new window and show, disabled
                App.Mediator.OnShowNxlConvertWin(out nxlFileConverWin, this.win, "ModifyRights");
            }
            else if (nxlFile.Location == EnumFileLocation.Local)
            {
                CheckModifiedRights(nxlFile, (INxlFile nxl) =>
                {
                    App.Mediator.OnModifyNxlFileRights(nxl.LocalPath, nxl, this.win);
                });
            }
        }

        /// <summary>
        /// Check the nxl file rights if is modified.
        /// </summary>
        private void CheckModifiedRights(INxlFile nxlFile, Action<INxlFile> action)
        {
            CommonUtils.CheckOfflineFileVersion(projectRepo, currentWorkingArea, nxlFile, (bool isModified) =>
            {
                // if conflict, will popup dialog have user to select.
                if (isModified)
                {
                    try
                    {
                        if (!IsSameTags(nxlFile)) // means rights is modified, should enforce user to update file first.
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
                        App.ShowBalloonTip(CultureStringInfo.Common_System_Internal_Error);
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

            // View the local file.
            if (CurrentSelectedFile.Location == EnumFileLocation.Local)
            {
                // For project offline file, need to check file version before view.
                if (CurrentSelectedFile.FileRepo == EnumFileRepo.REPO_PROJECT && IsOffline())
                {
                    ViewProjectOfflineFile(CurrentSelectedFile, true);
                }
                else
                {
                    StartViewerProcess(CurrentSelectedFile);
                }

            }
            else
            {
                // start viewer process
                StartViewerProcess(CurrentSelectedFile);
            }

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

        private void ContextMenu_DoViewRemove(ContextMenuCmdArgs args)
        {
            ManualRemove(CurrentSelectedFile);
        }

        private void ContextMenu_DoOpenSkyDRM(ContextMenuCmdArgs args)
        {
            try
            {
                CommonUtils.OpenSkyDrmWeb();
            }
            catch(Exception e)
            {
                App.Log.Warn("Error occured when DoOpenSkyDRM:\n", e);
            }
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
                        UpdateStatusAccordDownloadResult(doc, result);
                    });

                }
            }catch(Exception e)
            {
                App.Log.Warn("Exception in ContextMenu_MarkOffline," + e, e);
            }

        }

        // Handle the complete operation of downloading.
        private void UpdateStatusAccordDownloadResult(NxlDoc doc, bool result)
        {
            if (result)
            {
                // update file status
                doc.FileStatus = EnumNxlFileStatus.AvailableOffline;
                doc.Location = EnumFileLocation.Local;
                // Fix bug 51388
                CommonUtils.UpdateListViewFileStatus(nxlFileList, copyFileList, doc);
                // set source 
                doc.SourcePath = currentWorkRepo.GetSourcePath(doc);
                // Notify to enable "View file info" menu item after offline succeed.
                // --- fix bug 51360
                CurrentSelectedFile = doc;
            }
            else
            {
                // todo, error handle
                doc.IsMarkedOffline = false;
                doc.FileStatus = EnumNxlFileStatus.DownLoadedFailed;
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
            SkydrmLocalApp App;
            ViewModelMainWindow host;
            INxlFile target;
      
            public ExtractContentsFeature(SkydrmLocalApp App, ViewModelMainWindow host, INxlFile target)
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
                    if (!string.IsNullOrEmpty(target.LocalPath) && File.Exists(target.LocalPath))
                    {
                        bool isCancled;
                        result = ExtractContentHelper.ExtractContent(App, host.win, target.LocalPath,out isCancled);
                        if (!isCancled)
                        {
                            ExtractContentHelper.SendLog(target.LocalPath, NxlOpLog.Decrypt, result);
                        }
                    }
                    else
                    {
                       
                        Download((bool b) =>
                        {
                            if (b)
                            {
                                //Download Succeeded
                                host.win.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
                                {
                                    bool isCancled;
                                    result = ExtractContentHelper.ExtractContent(App, host.win, target.LocalPath,out isCancled);
                                    if (!isCancled)
                                    {
                                        ExtractContentHelper.SendLog(target.LocalPath, NxlOpLog.Decrypt, result);
                                    }

                                }));
                            }
                            else
                            {
                                App.ShowBalloonTip(CultureStringInfo.NETWORK_ERROR);
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
                bool rSaveAs = fp.HasRight(FileRights.RIGHT_SAVEAS);
                bool rDownload= fp.HasRight(FileRights.RIGHT_DOWNLOAD);
                if (  !(rSaveAs || rDownload) )
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
                    //fix bug 53134 add new feature, 
                    // extract timestamp in target.Name and replaced it as local lastest one
                    bUserHasSelect = ShowSaveFileDialog(out dest,
                        ModifyExportedFileNameReplacedWithLatestTimestamp(target.Name),
                        host.win);
                });
                return bUserHasSelect;
            }

            public void Export()
            {
                host.currentWorkRepo.Export(Dest,target);
            }


            public void NotifyError(string error)
            {
                host.App.ShowBalloonTip(error);
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
                dlg.Filter = "NextLabs Protected Documents (*.nxl)|*.nxl"; // Filter files by extension

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

            // When file in project,re-download partial file, because project file maybe modify rights.
            if (nxl.FileRepo != EnumFileRepo.REPO_PROJECT && FileHelper.Exist(nxl.PartialLocalPath))
            {
                callback?.Invoke(true);
                return;
            }

            // For project online file, always download; and for other repo online file, will download partial if not exist.
            currentWorkRepo.DownloadFile(nxl, true, (bool result) => {
                callback?.Invoke(result);
            },
            true
            );
        }

        // This only handle project offline file.
        public void PartialDownloadEx(OnDownloadCompleteEx callback)
        {
            App.Log.Info("PartialDownloadEx -->");
            INxlFile tmp = CurrentSelectedFile;

            currentWorkRepo.DownloadFile(tmp, true, (bool result) => {
                if (result)
                {
                    callback.Invoke(result, tmp);
                }
                else
                {
                    callback.Invoke(result, tmp);
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
                         App.ShowBalloonTip(CultureStringInfo.Exception_ExportFeature_Succeeded + feature.Dest + ".");
                     }
                     catch (InsufficientRightsException e)
                     {
                         App.ShowBalloonTip("You have no permission to download the file.");
                     }
                     catch (Exception e )
                     {

                         App.ShowBalloonTip(CultureStringInfo.Exception_ExportFeature_Failed);                      
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
           var nxlFile = CommonUtils.GetFileFromListByLocalPath(localPath, nxlFileList);
            if (nxlFile != null)
            {
                SkydrmLocalApp.Singleton.Log.Info("Edit offline file by viewer edit.");
                InnerEdit(nxlFile);
            }
        }

        private void InnerEdit(INxlFile nxlFile)
        {
            try
            {
                IEditFeature ef = EditMap.GetValue(nxlFile.LocalPath);
                if (ef == null)
                {
                    ef = new EditFeature(projectRepo, currentWorkingArea);
                    EditMap.Add(nxlFile.LocalPath, ef);
                }

                // online mode
                if (IsNetworkAvailable)
                {
                    if (nxlFile.IsMarkedFileRemoteModified)
                    {
                        ef.HandleIfSyncFromRms(nxlFile, (INxlFile updatedFile) => {
                            UpdateModifiedItem(updatedFile);
                        });
                    }
                    else
                    {
                        // Now will check version again.
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
                                ef.EditFromMainWin(nxlFile, (EditCallBack cb) => {
                                    OnEditFinish(ef, cb, nxlFile);
                                });

                            }
                        });
                    }
                }

                // offline mode
                else
                {
                    ef.EditFromMainWin(nxlFile, (EditCallBack cb) => {
                        OnEditFinish(ef, cb, nxlFile);
                    });
                }
            }
            catch (Exception e)
            {
                SkydrmLocalApp.Singleton.Log.Error("Edit offline file failed: " + e.ToString());
            }

        }
        
        private void ContextMenu_Edit(ContextMenuCmdArgs args)
        {
            SkydrmLocalApp.Singleton.Log.Info("Edit offline file by context menu.");

            InnerEdit(args.SelectedFile);
        }

        private void OnEditFinish(IEditFeature ef, EditCallBack cb, INxlFile nxlFile)
        {
            SkydrmLocalApp.Singleton.Log.Info("OnEditFinish.");

            if (cb.IsEdit)
            {
                SkydrmLocalApp.Singleton.Log.Info("Begin to update edited file to rms.");

                ef.UpdateToRms(nxlFile, (INxlFile updatedFile)=> {
                    UpdateModifiedItem(updatedFile);
                });
            }
            EditMap.Remove(cb.LocalPath);
        }

        // Update the modified listview item ui if user click update.
        private void UpdateModifiedItem(INxlFile updatedFile)
        {
            int index = -1;
            for(int i = 0; i < nxlFileList.Count; i++)
            {
                if (nxlFileList[i].Name == updatedFile.Name)
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
                // unmark
                if (currentWorkRepo.UnmarkOffline(args.SelectedFile))
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
                }
                else
                {
                    App.ShowBalloonTip("Failed to remove the cached file.");
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
                dialog.Title = "Select a NextLabs Protected Documents";

                if (!Directory.Exists(selectFilePath))
                {
                    // Get system desktop dir.
                    selectFilePath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory); ;
                }
                dialog.InitialDirectory = selectFilePath; // set init Dir.

                // .nxl files
                dialog.Filter = "NextLabs Protected Documents (*.nxl)|*.nxl";

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

        private void ContextMenu_TreeViewAddFile()
        {
            string nxlPath;
            if (OpenFileDialog(out nxlPath))
            {
                App.Mediator.OnAddNxlFileToProject(nxlPath, this.win);
            }
        }
        #endregion // Right Click Context Menu

    }
}

