using SkydrmDesktop.rmc.fileSystem.utils;
using SkydrmDesktop.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.fileSystem;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.fileSystem.project;
using SkydrmLocal.rmc.ui.utils;
using SkydrmLocal.rmc.ui.windows.mainWindow.helper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SkydrmDesktop.rmc.ui.windows.fileOperationWindow.view
{
    #region Data Model
    public enum DIsplayFileType
    {
        OnlyNormal,
        OnlyNxlFile,
        All
    }
    public class RepoItem
    {
        private string repoGroupName;
        private IFileRepo repo;

        public RepoItem(string repoGroupN, IFileRepo fileRepo)
        {
            repoGroupName = repoGroupN;
            repo = fileRepo;
        }

        public string RepoGroupName { get => repoGroupName; }
        public IFileRepo Repo { get => repo; }
    }
    public class ProjectRepoItem
    {
        private string pRepoGroupName;
        private ProjectData pData;
        private ProjectRepo projectRepo;

        public ProjectRepoItem(ProjectRepo pRepo, ProjectData projectData)
        {
            pRepoGroupName = FileSysConstant.PROJECT;
            projectRepo = pRepo;
            pData = projectData;
        }

        public string PRepoGroupName { get => pRepoGroupName; }
        public ProjectData PData { get => pData; }
        public ProjectRepo ProjectRepo { get => projectRepo; }
    }
    public class FileItem : INotifyPropertyChanged
    {
        private bool isChecked;
        private INxlFile file;

        public FileItem(INxlFile item)
        {
            file = item;
        }

        public bool IsChecked { get => isChecked; set { isChecked = value; OnPropertyChanged("IsChecked"); } }
        public INxlFile File { get => file; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public class SelectedFolderItem : INotifyPropertyChanged
    {
        private INxlFile folder;
        private bool isCanSelect;
        private Visibility showSlash;

        public SelectedFolderItem(INxlFile _folder, bool _canSelect = true, Visibility _showSlash = Visibility.Visible)
        {
            folder = _folder;
            isCanSelect = _canSelect;
            showSlash = _showSlash;
        }

        public INxlFile Folder { get => folder; }
        public bool IsCanSelect { get => isCanSelect; set { isCanSelect = value; OnPropertyChanged("IsCanSelect"); } }
        public Visibility ShowSlash { get => showSlash; set { showSlash = value; OnPropertyChanged("ShowSlash"); } }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    #endregion

    #region StyleSelector
    public class FileListStyleSelector : StyleSelector
    {
        public Style FileStyle { get; set; }
        public Style FolderStyle { get; set; }

        public string PropertyToEvaluate { get; set; }

        public string PropertyVaIueIsFolder { get; set; }

        public override Style SelectStyle(object item, DependencyObject container)
        {
            FileItem fileItem = (FileItem)item;
            INxlFile nxl = fileItem.File;

            // Use reflection to get the property to check.
            Type type = nxl.GetType();
            var property = type.GetProperty(PropertyToEvaluate);

            if (property.GetValue(nxl, null).ToString() == PropertyVaIueIsFolder)
            {
                return FolderStyle;
            }
            else
            {
                return FileStyle;
            }
        }
    }
    #endregion

    #region Convert
    public class RepoTypeIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string repoType = (string)value;
            if (repoType.Equals(FileSysConstant.LOCAL_DRIVE, StringComparison.CurrentCultureIgnoreCase))
            {
                return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/localdrive.png";
            }
            if (repoType.Equals(FileSysConstant.HOME, StringComparison.CurrentCultureIgnoreCase))
            {
                return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/home.png";
            }
            if (repoType.Equals(FileSysConstant.MYSPACE, StringComparison.CurrentCultureIgnoreCase))
            {
                return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/myspace.png";
            }
            if (repoType.Equals(FileSysConstant.MYDRIVE, StringComparison.CurrentCultureIgnoreCase))
            {
                return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/myspace.png";
            }
            if (repoType.Equals(FileSysConstant.MYVAULT, StringComparison.CurrentCultureIgnoreCase))
            {
                return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/myspace.png";
            }
            if (repoType.Equals(FileSysConstant.WORKSPACE, StringComparison.CurrentCultureIgnoreCase))
            {
                return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/workspace.png";
            }
            if (repoType.Equals(FileSysConstant.SHAREDWITHME, StringComparison.CurrentCultureIgnoreCase))
            {
                return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/sharedWithMe.png";
            }
            if (repoType.Equals(FileSysConstant.PROJECT, StringComparison.CurrentCultureIgnoreCase))
            {
                return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/project.png";
            }
            if (repoType.Equals(FileSysConstant.REPOSITORIES, StringComparison.CurrentCultureIgnoreCase))
            {
                return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/repositories.png";
            }
            if (repoType.Equals(FileSysConstant.DROPBOX, StringComparison.CurrentCultureIgnoreCase))
            {
                return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/dropbox.png";
            }
            if (repoType.Equals(FileSysConstant.BOX, StringComparison.CurrentCultureIgnoreCase))
            {
                return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/box.png";
            }
            if (repoType.Equals(FileSysConstant.GOOGLE_DRIVE, StringComparison.CurrentCultureIgnoreCase))
            {
                return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/googleDrive.png";
            }
            if (repoType.Equals(FileSysConstant.ONEDRIVE, StringComparison.CurrentCultureIgnoreCase))
            {
                return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/oneDrive.png";
            }
            if (repoType.Equals(FileSysConstant.SHAREPOINT, StringComparison.CurrentCultureIgnoreCase)
                || repoType.Equals(FileSysConstant.SHAREPOINT_ONLINE, StringComparison.CurrentCultureIgnoreCase)
                || repoType.Equals(FileSysConstant.SHAREPOINT_ONPREMISE, StringComparison.CurrentCultureIgnoreCase))
            {
                return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/sharepoint.png";
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
    public class ProjectIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool IsCreateByMe = (bool)value;
            if (IsCreateByMe)
            {
                return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/projectByMe.png";
            }
            return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/projectByOthers.png";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
    public class RepoNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string repoName = (string)value;
            if (repoName.Equals(FileSysConstant.MYVAULT) || repoName.Equals(FileSysConstant.MYDRIVE))
            {
                return FileSysConstant.MYSPACE;
            }
            return repoName;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
    public class EllipseColorBeforeRepoClassIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            RepositoryProviderClass repoType = (RepositoryProviderClass)value;

            switch (repoType)
            {
                case RepositoryProviderClass.UNKNOWN:
                    break;
                case RepositoryProviderClass.PERSONAL:
                case RepositoryProviderClass.BUSINESS:
                case RepositoryProviderClass.APPLICATION:
                    return new SolidColorBrush(Color.FromRgb(0X8B, 0X8B, 0X8B));
                default:
                    break;
            }

            return new SolidColorBrush(Color.FromArgb(0X00, 0XFF, 0XFF, 0XFF));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class RepoClassTypeIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            RepositoryProviderClass repoType = (RepositoryProviderClass)value;

            switch (repoType)
            {
                case RepositoryProviderClass.UNKNOWN:
                    break;
                case RepositoryProviderClass.PERSONAL:
                    return @"/rmc/resources/icons/externalrepo/type/personal.png";
                case RepositoryProviderClass.BUSINESS:
                    return @"/rmc/resources/icons/externalrepo/type/company.png";
                case RepositoryProviderClass.APPLICATION:
                    return @"/rmc/resources/icons/externalrepo/type/application.png";
                default:
                    break;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FileListVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility checkBoxVisible = (Visibility)value;

            if (checkBoxVisible == Visibility.Visible)
            {
                return Visibility.Collapsed;
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BrowserVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            IFileRepo currenRepo = (IFileRepo)value;

            if (currenRepo is fileSystem.localDrive.LocalDriveRepo)
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EmptyVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int count = (int)value;

            if (count > 0)
            {
                return Visibility.Collapsed;
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    /// <summary>
    /// FileSelectPage.xaml  DataCommands
    /// </summary>
    public class FS_DataCommands
    {
        private static RoutedCommand browser;
        private static RoutedCommand positive;
        private static RoutedCommand cancel;
        static FS_DataCommands()
        {
            browser = new RoutedCommand(
              "Browser", typeof(FS_DataCommands));

            // if the IsEnable property of positive button have bindinged,
            // best not binding input gesture in Command. 
            // otherwise user can trigger command by input gesture when button isEnable property is false.
            positive = new RoutedCommand(
              "Positive", typeof(FS_DataCommands));

            InputGestureCollection input = new InputGestureCollection();
            input.Add(new KeyGesture(Key.Escape));
            cancel = new RoutedCommand(
              "Cancel", typeof(FS_DataCommands), input);
        }
        /// <summary>
        /// FileSelectPage.xaml browser button command
        /// </summary>
        public static RoutedCommand Browser
        {
            get { return browser; }
        }
        /// <summary>
        ///  FileSelectPage.xaml positive button command
        /// </summary>
        public static RoutedCommand Positive
        {
            get { return positive; }
        }
        /// <summary>
        /// FileSelectPage.xaml cancel button command
        /// </summary>
        public static RoutedCommand Cancel
        {
            get { return cancel; }
        }
    }

    /// <summary>
    /// ViewModel for FileSelectPage.xaml
    /// </summary>
    public class FileSelectViewModel : INotifyPropertyChanged
    {
        #region DataBinding
        private string title;
        private string desc;

        private Visibility checkBoxVisible = Visibility.Visible;

        private bool? isAllChecked = false;
        private bool selectAllCbIsEnable = false;
        private int selectedFilesCount = 0;

        private ObservableCollection<RepoItem> repoList = new ObservableCollection<RepoItem>();
        private Visibility projectRepoVisible = Visibility.Collapsed;
        private ObservableCollection<ProjectRepoItem> projectRepoList = new ObservableCollection<ProjectRepoItem>();

        private ObservableCollection<SelectedFolderItem> selectedPaths = new ObservableCollection<SelectedFolderItem>();
        private ObservableCollection<FileItem> fileList = new ObservableCollection<FileItem>();

        private IFileRepo currentWorkingRepo;
        /// <summary>
        ///  Record current repository working directory(repoId + pathId)
        /// </summary>
        private string currentWorkingDirectoryFlag { get; set; }

        private bool positiveBtnIsEnable;

        /// <summary>
        /// Title, defult value is null
        /// </summary>
        public string Title { get => title; set { title = value; OnPropertyChanged("Title"); } }
        
        /// <summary>
        /// Description, defult value is null
        /// </summary>
        public string Desc { get => desc; set { desc = value; OnPropertyChanged("Desc"); } }

        /// <summary>
        /// File list CheckBox and select all CheckBox visible, defult value is Visible.
        /// </summary>
        public Visibility CheckBoxVisible { get => checkBoxVisible; set { checkBoxVisible = value; OnPropertyChanged("CheckBoxVisible"); } }

        /// <summary>
        /// (Internal binding) Select all Checked state, defult value is false.
        /// </summary>
        public bool? IsAllChecked { get => isAllChecked; set { isAllChecked = value; OnPropertyChanged("IsAllChecked"); } }

        /// <summary>
        /// Selected All check box IsEnable, defult value is false.
        /// </summary>
        public bool SelectAllCbIsEnable { get => selectAllCbIsEnable; set { selectAllCbIsEnable = value; OnPropertyChanged("SelectAllCbIsEnable"); } }

        /// <summary>
        /// (Internal binding) Selected files count, defult value is 0.
        /// </summary>
        public int SelectedFilesCount { get => selectedFilesCount; set { selectedFilesCount = value; OnPropertyChanged("SelectedFilesCount"); } }

        /// <summary>
        /// Repo list, contain LocalDrive, MySpace, WorkSpace, SharedWorkSpace.
        /// </summary>
        public ObservableCollection<RepoItem> RepoList { get => repoList; }

        /// <summary>
        /// Poject repo list visible, defult value is Collapsed.
        /// </summary>
        public Visibility ProjectRepoVisible { get => projectRepoVisible; set { projectRepoVisible = value; OnPropertyChanged("ProjectRepoVisible"); } }

        /// <summary>
        /// Project repo list
        /// </summary>
        public ObservableCollection<ProjectRepoItem> ProjectRepoList { get => projectRepoList; }

        /// <summary>
        /// (Internal binding) Record the selected folder path
        /// </summary>
        public ObservableCollection<SelectedFolderItem> SelectedPaths { get => selectedPaths; }

        /// <summary>
        /// (Internal binding to ListBox) File ListBox Itemsource. if ListBox have CheckBox, user can use FileList item IsChecked property to get selected files.
        /// if ListBox don't have CheckBox and SelectionMode is Single, user can use FileSelectViewModel.CurrentSelectFile property to get file.
        /// </summary>
        public ObservableCollection<FileItem> FileList { get => fileList; }

        /// <summary>
        /// (Internal binding) Record current selected repo
        /// </summary>
        public IFileRepo CurrentWorkingRepo { get=>currentWorkingRepo; set { currentWorkingRepo = value; OnPropertyChanged("CurrentWorkingRepo"); } }

        /// <summary>
        /// Used to record the selected file in the file list ( not checkBox file list)
        /// </summary>
        public INxlFile CurrentSelectFile { get; set; }

        /// <summary>
        /// Used to record current select folder
        /// </summary>
        public CurrentSelectedSavePath SourceFileDirPath { get; set; }

        /// <summary>
        /// Decide which file type should be displayed
        /// </summary>
        public DIsplayFileType ShowFileType { get; set; }

        /// <summary>
        /// (Internal binding) Positive button isEnable, defult value is false
        /// </summary>
        public bool PositiveBtnIsEnable { get => positiveBtnIsEnable; set { positiveBtnIsEnable = value; OnPropertyChanged("PositiveBtnIsEnable"); } }

        /// <summary>
        /// (Internal binding) Switch selected folder command
        /// </summary>
        public DelegateCommand SwitchSelectedFolderCommand { get; set; }

        /// <summary>
        /// (Internal binding) CheckBox select all Command
        /// </summary>
        public DelegateCommand FileSelectCommand { get; set; }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public FileSelectViewModel()
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(RepoList);
            view.GroupDescriptions.Add(new PropertyGroupDescription("RepoGroupName"));

            ICollectionView pview = CollectionViewSource.GetDefaultView(ProjectRepoList);
            pview.GroupDescriptions.Add(new PropertyGroupDescription("PRepoGroupName"));

            SwitchSelectedFolderCommand = new DelegateCommand(SwitchSelectedFolder);
            FileSelectCommand = new DelegateCommand(SelectCmdDispatch);
        }

        #region Binding Command Dispatch
        private void SwitchSelectedFolder(object args)
        {
            SelectedFolderItem selectedItem = args as SelectedFolderItem;

            // update SelectedPaths list
            List<SelectedFolderItem> recordSelected = new List<SelectedFolderItem>();
            foreach (var item in SelectedPaths)
            {
                if (item.Folder.Equals(selectedItem.Folder))
                {
                    recordSelected.Add(item);
                    break;
                }
                else
                {
                    recordSelected.Add(item);
                }
            }

            SelectedPaths.Clear();
            foreach (var item in recordSelected)
            {
                SelectedPaths.Add(item);
            }

            // reset 'Select all' checkBox
            IsAllChecked = false;
            SelectedFilesCount = 0;
            PositiveBtnIsEnable = false;

            // update file list
            CurrentWorkingRepo.CurrentWorkingFolder = (NxlFolder)selectedItem.Folder;
            var filePool = CurrentWorkingRepo.GetWorkingFolderFilesFromDB();
            var filePool2 = filePool.OrderBy(f => f.Name).OrderByDescending(f => f.IsFolder);
            SetFileList(filePool2.ToList());
            // set sync flag
            currentWorkingDirectoryFlag = CurrentWorkingRepo.RepoId + CurrentWorkingRepo.CurrentWorkingFolder.PathId;
            // sync file
            RefreshWorkingFolder();
        }

        private void SelectCmdDispatch(object args)
        {
            switch (args.ToString())
            {
                case "Cmd_CheckedAllFileItem":
                    CheckedAllFile();
                    break;
                case "Cmd_CheckedFileItem":
                    CheckedFile();
                    break;
                default:
                    break;
            }
        }
        private void CheckedAllFile()
        {
            if (recordCbAllState == null)
            {
                IsAllChecked = true;
            }

            if (IsAllChecked == true)
            {
                recordCbAllState = true;
                int count = 0;
                foreach (var item in FileList)
                {
                    if (!item.File.IsFolder)
                    {
                        item.IsChecked = true;
                        count++;
                    }
                }
                SelectedFilesCount = count;
            }
            else
            {
                recordCbAllState = false;
                foreach (var item in FileList)
                {
                    if (!item.File.IsFolder)
                    {
                        item.IsChecked = false;
                    }
                }
                SelectedFilesCount = 0;
            }
            if (SelectedFilesCount > 0)
            {
                PositiveBtnIsEnable = true;
            }
            else
            {
                PositiveBtnIsEnable = false;
            }
        }
        private bool? recordCbAllState = false;
        private void CheckedFile()
        {
            var filterFileList = FileList.Where(f=>!f.File.IsFolder);

            if (filterFileList.All(f => f.IsChecked))
            {
                IsAllChecked = true;
                recordCbAllState = true;
            }
            else if (filterFileList.All(f => !f.IsChecked))
            {
                IsAllChecked = false;
                recordCbAllState = false;
            }
            else
            {
                IsAllChecked = null;
                recordCbAllState = null;
            }

            SelectedFilesCount = filterFileList.Where(f => f.IsChecked).Count();
            if (SelectedFilesCount > 0)
            {
                PositiveBtnIsEnable = true;
            }
            else
            {
                PositiveBtnIsEnable = false;
            }
        }
        #endregion

        #region Event
        /// <summary>
        /// Repo list selected item 
        /// </summary>
        private ListBoxItem currentSelectRepoItem;
        internal void RepoListItem_Selected(object sender, RoutedEventArgs e)
        {
            ListBoxItem selectedItem = sender as ListBoxItem;
            if (!selectedItem.IsSelected)
            {
                return;
            }
            if (selectedItem.Content is RepoItem)
            {
                if (currentSelectProjectRepoItem != null && currentSelectProjectRepoItem.IsSelected)
                {
                    // reset Project repo list selected item
                    currentSelectProjectRepoItem.IsSelected = false;
                }

                // record current selected item
                currentSelectRepoItem = selectedItem;

                // record current working repo
                RepoItem repoItem = (RepoItem)selectedItem.Content;
                repoItem.Repo.CurrentWorkingFolder = new NxlFolder();
                CurrentWorkingRepo = repoItem.Repo;

                // update file list
                var filePool = repoItem.Repo.GetWorkingFolderFilesFromDB();
                var filePool2 = filePool.OrderBy(f => f.Name).OrderByDescending(f => f.IsFolder);
                SetFileList(filePool2.ToList());
                // set sync flag
                currentWorkingDirectoryFlag = CurrentWorkingRepo.RepoId + CurrentWorkingRepo.CurrentWorkingFolder.PathId;
                // sync file
                RefreshWorkingFolder();

                // reset 'Select all' checkBox
                IsAllChecked = false;
                SelectedFilesCount = 0;
                PositiveBtnIsEnable = false;

                // reset selected path
                ResetSelectedPaths(CurrentWorkingRepo);

                SetCurSavePath(CurrentWorkingRepo);
            }
        }
        
        /// <summary>
        /// Project repo list selected item 
        /// </summary>
        private ListBoxItem currentSelectProjectRepoItem;
        internal void ProjectRepoListItem_Selected(object sender, RoutedEventArgs e)
        {
            ListBoxItem selectedItem = sender as ListBoxItem;
            if (!selectedItem.IsSelected)
            {
                return;
            }
            if (selectedItem.Content is ProjectRepoItem)
            {
                // reset Repo list selected item
                if (currentSelectRepoItem != null && currentSelectRepoItem.IsSelected)
                {
                    currentSelectRepoItem.IsSelected = false;
                }

                // record current selected item
                currentSelectProjectRepoItem = selectedItem;

                // record current working repo
                ProjectRepoItem pRepoItem = (ProjectRepoItem)selectedItem.Content;
                pRepoItem.ProjectRepo.CurrentWorkingProject = pRepoItem.PData;
                pRepoItem.ProjectRepo.CurrentWorkingFolder = new NxlFolder();
                CurrentWorkingRepo = pRepoItem.ProjectRepo;

                // update file list
                var filePool = pRepoItem.ProjectRepo.GetWorkingFolderFilesFromDB();
                var filePool2 = filePool.OrderBy(f => f.Name).OrderByDescending(f => f.IsFolder);
                SetFileList(filePool2.ToList());
                // set sync flag
                currentWorkingDirectoryFlag = CurrentWorkingRepo.RepoId + CurrentWorkingRepo.CurrentWorkingFolder.PathId;
                // sync file
                RefreshWorkingFolder();

                // reset 'Select all' checkBox
                IsAllChecked = false;
                SelectedFilesCount = 0;
                PositiveBtnIsEnable = false;

                // reset selected path
                ResetSelectedPaths(CurrentWorkingRepo);

                SetCurSavePath(CurrentWorkingRepo);
            }
        }

        private void ResetSelectedPaths(IFileRepo selectedRepo)
        {
            SelectedPaths.Clear();

            if (selectedRepo is fileSystem.localDrive.LocalDriveRepo)
            {
                NxlFolder currentFolder = selectedRepo.CurrentWorkingFolder;
                currentFolder.Name = "This PC";
                SelectedFolderItem folderItem = new SelectedFolderItem(currentFolder, true, Visibility.Collapsed);
                SelectedPaths.Add(folderItem);
            }
            else
            {
                NxlFolder sky = new NxlFolder() { Name = "SkyDRM://", PathId=null, DisplayPath = null };
                SelectedFolderItem skyItem = new SelectedFolderItem(sky, false, Visibility.Collapsed);
                SelectedPaths.Add(skyItem);
                if (selectedRepo is fileSystem.workspace.WorkSpaceRepo)
                {
                    NxlFolder currentFolder = selectedRepo.CurrentWorkingFolder;
                    currentFolder.Name = selectedRepo.RepoDisplayName;
                    SelectedFolderItem folderItem = new SelectedFolderItem(currentFolder, true, Visibility.Collapsed);
                    SelectedPaths.Add(folderItem);
                }
                else if (selectedRepo is fileSystem.mySpace.MyDriveRepo
                    || selectedRepo is SkydrmLocal.rmc.fileSystem.myvault.MyVaultRepo)
                {
                    NxlFolder currentFolder = selectedRepo.CurrentWorkingFolder;
                    currentFolder.Name = FileSysConstant.MYSPACE;
                    SelectedFolderItem folderItem = new SelectedFolderItem(currentFolder, true, Visibility.Collapsed);
                    SelectedPaths.Add(folderItem);
                }
                else if (selectedRepo is fileSystem.sharedWorkspace.SharedWorkspaceRepo)
                {
                    NxlFolder repository = new NxlFolder() { Name = FileSysConstant.REPOSITORIES, PathId = null, DisplayPath = null };
                    SelectedFolderItem reposi = new SelectedFolderItem(repository, false, Visibility.Collapsed);
                    SelectedPaths.Add(reposi);

                    NxlFolder currentFolder = selectedRepo.CurrentWorkingFolder;
                    currentFolder.Name = selectedRepo.RepoDisplayName;
                    SelectedFolderItem folderItem = new SelectedFolderItem(currentFolder);
                    SelectedPaths.Add(folderItem);
                }
                else if (selectedRepo is ProjectRepo)
                {
                    ProjectRepo projectRepo = selectedRepo as ProjectRepo;
                    NxlFolder project = new NxlFolder() { Name = FileSysConstant.PROJECT, PathId = null, DisplayPath = null };
                    SelectedFolderItem prj = new SelectedFolderItem(project, false, Visibility.Collapsed);
                    SelectedPaths.Add(prj);

                    NxlFolder currentFolder = projectRepo.CurrentWorkingFolder;
                    currentFolder.Name = projectRepo.CurrentWorkingProject.ProjectInfo.DisplayName;
                    SelectedFolderItem folderItem = new SelectedFolderItem(currentFolder);
                    SelectedPaths.Add(folderItem);
                }
            }
        }

        /// <summary>
        /// CheckBox File List and File List double click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        internal void FileListItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListBoxItem selectedItem = sender as ListBoxItem;
            FileItem selectedFile = (FileItem)selectedItem.Content;
            if (selectedFile.File.IsFolder)
            {
                // record current working repo working folder
                CurrentWorkingRepo.CurrentWorkingFolder = (NxlFolder)selectedFile.File;

                // reset 'Select all' checkBox
                IsAllChecked = false;
                SelectedFilesCount = 0;
                PositiveBtnIsEnable = false;

                // update file list
                var filePool = CurrentWorkingRepo.GetWorkingFolderFilesFromDB();
                var filePool2 = filePool.OrderBy(f => f.Name).OrderByDescending(f => f.IsFolder);
                SetFileList(filePool2.ToList());
                // set sync flag
                currentWorkingDirectoryFlag = CurrentWorkingRepo.RepoId + CurrentWorkingRepo.CurrentWorkingFolder.PathId;
                // sync file
                RefreshWorkingFolder();

                // add select path
                SelectedFolderItem selectedFolder = new SelectedFolderItem(CurrentWorkingRepo.CurrentWorkingFolder);
                SelectedPaths.Add(selectedFolder);

                SetCurSavePath(CurrentWorkingRepo);
            }
        }

        private void SetCurSavePath(IFileRepo selectedRepo)
        {
            if (selectedRepo is fileSystem.localDrive.LocalDriveRepo)
            {
                SourceFileDirPath = new CurrentSelectedSavePath(DataTypeConvertHelper.SYSTEMBUCKET, selectedRepo.CurrentWorkingFolder.PathId,
                        selectedRepo.CurrentWorkingFolder.PathId, SkydrmApp.Singleton.SystemProject.Id.ToString());
            }
            else
            {
                if (selectedRepo is fileSystem.workspace.WorkSpaceRepo)
                {
                    SourceFileDirPath = new CurrentSelectedSavePath(DataTypeConvertHelper.WORKSPACE, selectedRepo.CurrentWorkingFolder.PathId, 
                        "SkyDRM://" + DataTypeConvertHelper.WORKSPACE + selectedRepo.CurrentWorkingFolder.DisplayPath, SkydrmApp.Singleton.SystemProject.Id.ToString());
                }
                else if (selectedRepo is fileSystem.mySpace.MyDriveRepo)
                {
                    SourceFileDirPath = new CurrentSelectedSavePath(DataTypeConvertHelper.MY_VAULT, "/", "SkyDRM://" + DataTypeConvertHelper.MY_SPACE);
                }
                else if (selectedRepo is fileSystem.sharedWorkspace.SharedWorkspaceRepo)
                {
                    SourceFileDirPath = new CurrentSelectedSavePath(DataTypeConvertHelper.REPOSITORIES, selectedRepo.CurrentWorkingFolder.PathId,
                         "SkyDRM://" + DataTypeConvertHelper.REPOSITORIES + "/" + selectedRepo.RepoDisplayName + selectedRepo.CurrentWorkingFolder.DisplayPath,
                          selectedRepo.RepoId);
                }
                else if (selectedRepo is ProjectRepo)
                {
                    ProjectRepo projectRepo = selectedRepo as ProjectRepo;
                    SourceFileDirPath = new CurrentSelectedSavePath(DataTypeConvertHelper.PROJECT, projectRepo.CurrentWorkingFolder.PathId,
                        "SkyDRM://" + DataTypeConvertHelper.PROJECT + "/" + projectRepo.CurrentWorkingProject.ProjectInfo.DisplayName + projectRepo.CurrentWorkingFolder.DisplayPath,
                        projectRepo.CurrentWorkingProject.ProjectInfo.ProjectId.ToString());
                }
            }
        }

        internal void FileListItem_Selected(object sender, RoutedEventArgs e)
        {
            ListBoxItem selectedItem = sender as ListBoxItem;
            FileItem selectedFile = (FileItem)selectedItem.Content;
            if (selectedFile.File.IsFolder)
            {
                PositiveBtnIsEnable = false;
            }
            else
            {
                CurrentSelectFile = selectedFile.File;
                PositiveBtnIsEnable = true;
            }
        }

        internal void ChBxFolderListItem_Selected(object sender, RoutedEventArgs e)
        {
            ListBoxItem selectedItem = sender as ListBoxItem;
            if (!selectedItem.IsSelected)
            {
                return;
            }
            FileItem selectedFile = (FileItem)selectedItem.Content;
            if (selectedFile.File.IsFolder)
            {
                selectedItem.IsSelected = false;
            }
        }

        internal void ChBxFileListItem_Selected(object sender, RoutedEventArgs e)
        {
            ListBoxItem selectedItem = sender as ListBoxItem;
            if (selectedItem == null)
            {
                return;
            }
            if (selectedItem.Content is FileItem)
            {
                FileItem selectedFile = (FileItem)selectedItem.Content;
                if (selectedFile.IsChecked)
                {
                    selectedFile.IsChecked = false;
                }
                else
                {
                    selectedFile.IsChecked = true;
                }

                CheckedFile();
            }
        }
        #endregion

        private void RefreshWorkingFolder()
        {
            // If refresh is called here, the underlying database will also be updated, 
            // causing the tree view of the MainWindow to be unable to update
            return;
            CurrentWorkingRepo?.SyncFiles((bool bSuc, IList<INxlFile> results, string originalWorkingFlag) =>
            {
                if (bSuc)
                {
                    if (originalWorkingFlag == currentWorkingDirectoryFlag)
                    {
                        MergeFileList(results);
                    }
                }
            }, currentWorkingDirectoryFlag);
        }

        private void MergeFileList(IList<INxlFile> newfiles)
        {
            List<INxlFile> added = new List<INxlFile>();

            for (int i = FileList.Count - 1; i >= 0; i--)
            {
                INxlFile one = FileList[i].File;

                INxlFile find = null;
                for (int j = 0; j < newfiles.Count; j++)
                {
                    INxlFile f = newfiles[j];
                    if (one.Equals(f) && one.RawDateModified == f.RawDateModified)
                    {
                        // "one" belongs to old set and belongs to new set
                        find = f;
                        break;
                    }
                }

                // "one" belongs to old set but not belongs to new set -- should remove it from old set.
                // should exclude local created file
                // If local file status is waiting upload | uploading we should keep it in the file list.
                if (find == null
                    && one.FileStatus != EnumNxlFileStatus.WaitingUpload
                    && one.FileStatus != EnumNxlFileStatus.Uploading
                    && one.FileStatus != EnumNxlFileStatus.UploadFailed)
                {
                    FileList.Remove(FileList[i]);
                }
            }

            for (int j = 0; j < newfiles.Count; j++)
            {
                INxlFile one = newfiles[j];

                INxlFile find = null;
                for (int i = 0; i < FileList.Count; i++)
                {
                    INxlFile f = FileList[i].File;
                    if (one.Equals(f) && one.RawDateModified == f.RawDateModified)
                    {
                        // "one" belongs to new set and belongs to old set
                        find = f;
                        break;
                    }
                }

                // "one" belongs to new set but not belongs to old set -- should add it 
                if (find == null)
                {
                    added.Add(one);
                }
            }

            foreach (var item in added)
            {
                if (item.IsFolder)
                {
                    FileList.Insert(0, new FileItem(item));
                }
                else
                {
                    if (ShowFileType == DIsplayFileType.OnlyNormal)
                    {
                        // only show normal file list
                        if (!item.IsNxlFile)
                        {
                            FileList.Add(new FileItem(item));
                        }
                    }
                    else if (ShowFileType == DIsplayFileType.OnlyNxlFile)
                    {
                        // only show nxl file list
                        if (item.IsNxlFile)
                        {
                            FileList.Add(new FileItem(item));
                        }
                    }
                    else
                    {
                        // all file
                        FileList.Add(new FileItem(item));
                    }
                }
            }

            // reset checkBox isEnable
            SelectAllCbIsEnable = false;
            foreach (var item in FileList)
            {
                if (!item.File.IsFolder)
                {
                    SelectAllCbIsEnable = true;
                    break;
                }
            }
        }

        private void SetFileList(IList<INxlFile> files)
        {
            FileList.Clear();
            foreach (var item in files)
            {
                if (item.IsFolder)
                {
                    FileList.Add(new FileItem(item));
                }
                else
                {
                    if (ShowFileType == DIsplayFileType.OnlyNormal)
                    {
                        // only show normal file list
                        if (!item.IsNxlFile)
                        {
                            FileList.Add(new FileItem(item));
                        }
                    }
                    else if (ShowFileType == DIsplayFileType.OnlyNxlFile)
                    {
                        // only show nxl file list
                        if (item.IsNxlFile)
                        {
                            FileList.Add(new FileItem(item));
                        }
                    }
                    else
                    {
                        // all file
                        FileList.Add(new FileItem(item));
                    }
                }
            }

            // reset checkBox isEnable
            SelectAllCbIsEnable = false;
            foreach (var item in FileList)
            {
                if (!item.File.IsFolder)
                {
                    SelectAllCbIsEnable = true;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Interaction logic for FileSelectPage.xaml
    /// </summary>
    public partial class FileSelectPage : Page
    {
        private FileSelectViewModel viewModel;
        public FileSelectPage()
        {
            InitializeComponent();

            this.DataContext = viewModel = new FileSelectViewModel();
        }

        /// <summary>
        /// ViewModel for FileSelectPage.xaml
        /// </summary>
        public FileSelectViewModel ViewModel { get => viewModel; set => this.DataContext = viewModel = value; }

        /// <summary>
        /// Set defult selected repo, selected index is 0
        /// </summary>
        public void SetDefultSelectRepo()
        {
            this.lstRepo.SelectedIndex = 0;
        }

        /// <summary>
        /// Repo list and Project repo list mouse wheel event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AllRepoList_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
            //set routedEvent Type
            eventArg.RoutedEvent = UIElement.MouseWheelEvent;
            eventArg.Source = sender;
            (sender as ListBox).RaiseEvent(eventArg);
        }

        /// <summary>
        /// Repo list selected item 
        /// </summary>
        private void RepoListItem_Selected(object sender, RoutedEventArgs e)
        {
            ViewModel.RepoListItem_Selected(sender, e);
        }

        /// <summary>
        /// Project repo list selected item 
        /// </summary>
        private void ProjectRepoListItem_Selected(object sender, RoutedEventArgs e)
        {
            ViewModel.ProjectRepoListItem_Selected(sender, e);
        }

        /// <summary>
        /// CheckBox File List and File List double click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FileListItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ViewModel.FileListItem_MouseDoubleClick(sender, e);
        }

        private void FileListItem_Selected(object sender, RoutedEventArgs e)
        {
            ViewModel.FileListItem_Selected(sender, e);
        }

        private void ChBxFolderListItem_Selected(object sender, RoutedEventArgs e)
        {
            ViewModel.ChBxFolderListItem_Selected(sender, e);
        }

        private void ChBxFileListItem_Selected(object sender, RoutedEventArgs e)
        {
            ViewModel.ChBxFileListItem_Selected(sender, e);
        }

        private void ChBxFileListItem_UnSelected(object sender, RoutedEventArgs e)
        {
            ViewModel.ChBxFileListItem_Selected(sender, e);
        }
    }
}
