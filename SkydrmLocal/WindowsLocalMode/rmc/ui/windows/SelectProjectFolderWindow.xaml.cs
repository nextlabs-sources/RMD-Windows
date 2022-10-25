using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.fileSystem;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.fileSystem.myvault;
using SkydrmLocal.rmc.fileSystem.project;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.viewModel;
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
using System.Windows.Shapes;

namespace SkydrmLocal.rmc.ui.windows
{
    /// <summary>
    /// Interaction logic for SelectProjectFolderWindow.xaml
    /// </summary>
    public partial class SelectProjectFolderWindow : Window, INotifyPropertyChanged
    {
        private SkydrmLocalApp App = (SkydrmLocalApp)SkydrmLocalApp.Current;

        private static readonly string MY_VAULT = CultureStringInfo.MainWin__TreeView_MyVault;
        private static readonly string PROJECT = CultureStringInfo.MainWin__TreeView_Project;

        public event PropertyChangedEventHandler PropertyChanged;

        #region // TreeView model
        public RepoViewModel RepoViewModel { get; set; }
        #endregion // For treeView model

        public EnumCurrentWorkingArea CurrentWorkingArea { get; set; }
        // Current working repo
        private IFileRepo currentWorkRepo;
        public NxlFolder CurrentWorkingFolder { get; set; }

        private MyVaultRepo myVaultRepo;
        private ProjectRepo projectRepo;


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

        // Will refresh UI when add or remove one entry because of ObservableCollection
        private ObservableCollection<INxlFile> nxlFileList = new ObservableCollection<INxlFile>();
        public ObservableCollection<INxlFile> NxlFileList
        {
            get { return nxlFileList; }
            set { nxlFileList = value; }
        }

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

        //SelectProjectFolderWin ui display path
        private string selectPath;
        public string SelectPath
        {
            get
            {
                return selectPath;
            }
            set
            {
                selectPath = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectPath"));
            }
        }

        private IMyProject defultProject;

        //for SelectProjectFolderWin ui display path
        private string Rootpath = "";
        private string Folderpath = "";

        //for transmit path and display CreateFileWin UI
        public event EventHandler<PathEventArgs> PathChangedEvent;

        //for display loading text, binding UI
        private bool isLoading = true;
        public bool IsLoading
        {
            get
            {
                return isLoading;
            }
            set
            {
                isLoading = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsLoading"));
            }
        }

        public SelectProjectFolderWindow(String path, IMyProject project)
        {
            InitializeComponent();

            SelectPath = path;
            defultProject = project;

             // instantiat repo
            RepoViewModel = new RepoViewModel();
            myVaultRepo = new MyVaultRepo();
            projectRepo = new ProjectRepo();

            // project
            if (projectRepo.GetAllFolder().Count == 0)
            {
                //this.GridProBar.Visibility = Visibility.Visible;
                //projectRepo.IsLoading = true;
                //projectRepo.SyncRemoteData((IList<ProjectData> result) =>
                //{
                //    Console.WriteLine("project");
                //    projectRepo.IsLoading = false;
                //    //if (!RepoViewModel.IsHasProjectViewModel())
                //    //{
                //    //    RepoViewModel.AddProjectRepoViewModel(projectRepo);
                //    //}

                //    LoadData();
                //});
            }
            else
            {
                projectRepo.IsLoading = false;
            }

            LoadData();     
            
        }
        private void LoadData()
        {
            if (!projectRepo.IsLoading)
            {
                //IsLoading = false;
                this.GridProBar.Visibility = Visibility.Collapsed;
                InitTreeView();
            }
        }
    
        private void InitTreeView()
        {
            // Now the TreeView layout display including follow 4 cases:
            // 1. todo:  try to get myVault files and project Info, if both of them don't have any data(including their local files), we will hide TreeView layout. -- figma ui
            // 2. myVault have data, project no data: --- only display myVault treeView
            // 3. myVault no data, project have data: --- only display project treeView
            // 4. myVault have data, project have data: --- both display myVault and project treeView.


            IList<IFileRepo> fileRepos = new List<IFileRepo>();
            fileRepos.Add(myVaultRepo);
            fileRepos.Add(projectRepo);
            RepoViewModel.Start(fileRepos, true);

            //Whether UI is displayed or not
            IsDisplayTreeview = RepoViewModel.Roots.Count > 0 ? true : false;

            // Note: must set DataContext after setting data source.
            this.UserControl_TreeView.DataContext = RepoViewModel;

            // TreeView item select changed event.
            this.UserControl_TreeView.treeView.SelectedItemChanged += TreeViewItemSelectChanged;

            this.UserControl_TreeView.PreviewMouseWheel += UserControl_TreeView_PreviewMouseWheel;

            SetTreeViewItemSelected(SelectPath, defultProject);

        }

        private void Button_SelectFolder(object sender, RoutedEventArgs e)
        {
            if (CurrentSelectedFile != null)
            {
                if (CurrentSelectedFile.IsFolder)//now ui is banned, will not execute; the CurrentSelectedFile is null
                {
                    CurrentWorkingArea = EnumCurrentWorkingArea.PROJECT_FOLDER;
                    CurrentWorkingFolder = (NxlFolder)CurrentSelectedFile;

                    //for display path
                    NxlFolder folder = CurrentWorkingFolder as NxlFolder;
                    Folderpath = folder.PathId.Substring(1);
                    //when double click listview item before,must be click treeview item and will get Rootpath 
                    SelectPath = Rootpath + Folderpath;


                    IList<INxlFile> children = ((NxlFolder)CurrentSelectedFile).Children;
                    SetListView(children);

                    CurrentSelectedFile = null;
                }
            }
            else
            {
                if (CurrentWorkingArea == EnumCurrentWorkingArea.MYVAULT)
                {
                    PathChangedEvent?.Invoke(this, new PathEventArgs(MY_VAULT,null));
                }
                else
                {
                    SetCurSavePath();
                    //to transmit selectPath
                    PathChangedEvent?.Invoke(this, new PathEventArgs(SelectPath, projectRepo.CurrentWorkingProject));
                }
               
                this.Close();
            }
        
        }

        private void Button_Cancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SetTreeViewItemSelected(string path, IMyProject project)
        {
            if (path.Equals(MY_VAULT, StringComparison.CurrentCultureIgnoreCase) || project==null)
            {
                foreach (var item in RepoViewModel.Roots)//RootViewModel
                {
                    if (item.RootName == path)//MyVault
                    {
                        RootViewModel rootView = item as RootViewModel;
                        rootView.IsSelected = true;
                    }
                }

            }
            else
            {
                if (path.Contains("/"))
                {
                    string[] nodeName = path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);//  allentest1/ allen/
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
                                            && projectView.Project.ProjectInfo.ProjectId == project.Id)
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
        }

        private void ParsingSavePath(string savepath)
        {
            if (savepath.Equals(MY_VAULT, StringComparison.CurrentCultureIgnoreCase))
            {
                foreach (var item in RepoViewModel.Roots)//RootViewModel
                {
                    if (item.RootName == savepath)//MyVault
                    {
                        RootViewModel rootView = item as RootViewModel;
                        rootView.IsSelected = true;
                    }
                }

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
        }

        private void UserControl_TreeView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
            //set routedEvent Type
            eventArg.RoutedEvent = UIElement.MouseWheelEvent;
            eventArg.Source = sender;
            (sender as UserControl).RaiseEvent(eventArg);
        }

        private void ListViewItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("---ListViewItem_DoubleClick-----");
            if (sender is ListViewItem)
            {
                ListViewItem selectedItem = sender as ListViewItem;
                CurrentSelectedFile = (INxlFile)selectedItem.Content;

                if (CurrentSelectedFile.IsFolder)
                {
                    CurrentWorkingArea = EnumCurrentWorkingArea.PROJECT_FOLDER;
                    CurrentWorkingFolder = (NxlFolder)CurrentSelectedFile;

                    //for display path
                    NxlFolder folder = CurrentWorkingFolder as NxlFolder;
                    Folderpath = folder.PathId.Substring(1);
                    //when double click listview item before,must be click treeview item and will get Rootpath 
                    SelectPath = Rootpath + Folderpath;


                    IList<INxlFile> children = ((NxlFolder)CurrentSelectedFile).Children;
                    SetListView(children);

                    //
                    CurrentSelectedFile = null;

                    ParsingSavePath(SelectPath);
                }
            }
        }

        private void ListViewItem_Selected(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("---ListViewItem_Selected-----");

            if (sender is ListViewItem)
            {
                ListViewItem selectedItem = sender as ListViewItem;
                CurrentSelectedFile = (INxlFile)selectedItem.Content;
            }
        }

        private void SetCurSavePath()
        {
            switch (CurrentWorkingArea)
            {
                case EnumCurrentWorkingArea.MYVAULT:
                case EnumCurrentWorkingArea.SHARED_WITH_ME:
                    SelectPath = MY_VAULT;
                    break;
                case EnumCurrentWorkingArea.PROJECT:
                    SelectPath = MY_VAULT;
                    break;
                case EnumCurrentWorkingArea.PROJECT_ROOT:
                    SelectPath = projectRepo.CurrentWorkingProject.ProjectInfo.DisplayName + "/";
                    break;
                case EnumCurrentWorkingArea.PROJECT_FOLDER:
                    string rootpath = projectRepo.CurrentWorkingProject.ProjectInfo.DisplayName + "/";
                    if ((projectRepo.CurrentWorkingFolder as NxlFolder).PathDisplay.Length > 1)
                    {
                        string folderpath = (projectRepo.CurrentWorkingFolder as NxlFolder).PathDisplay.Substring(1);
                        SelectPath = rootpath + folderpath;
                    }

                    break;
                default:
                    SelectPath = MY_VAULT;
                    break;
            }
        }

        private void TreeViewItemSelectChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeViewItemViewModel treeViewItem = e.NewValue as TreeViewItemViewModel;

            this.SelectFolderBtn.IsEnabled = true;
            CurrentSelectedFile = null;

            IList<INxlFile> fileList = null;
            if (treeViewItem is RootViewModel)
            {
                RootViewModel rootView = treeViewItem as RootViewModel;
                if (rootView.RootName == MY_VAULT)
                {
                    fileList = rootView.Root.MyVaultFiles; // all myVault files.

                    CurrentWorkingArea = EnumCurrentWorkingArea.MYVAULT;
                    currentWorkRepo = myVaultRepo;

                }
                else
                {
                    CurrentWorkingArea = EnumCurrentWorkingArea.PROJECT;
                    currentWorkRepo = projectRepo;

                    this.SelectFolderBtn.IsEnabled = false;
                }
            }
            else if (treeViewItem is ProjectViewModel)
            {
                ProjectViewModel projectView = treeViewItem as ProjectViewModel;
                fileList = projectView.Project.FileNodes; // project all files(doc and folder)

                ProjectData tmpData = projectView.Project;
                CurrentWorkingArea = EnumCurrentWorkingArea.PROJECT_ROOT;
                currentWorkRepo = projectRepo;
                projectRepo.CurrentWorkingProject = tmpData;

            }
            else if (treeViewItem is FolderViewModel)
            {
                FolderViewModel folderView = treeViewItem as FolderViewModel;
                fileList = folderView.NxlFolder.Children; // here need display right region.

                CurrentWorkingArea = EnumCurrentWorkingArea.PROJECT_FOLDER;
                currentWorkRepo = projectRepo;
                ProjectData tmpData = FindProject(folderView);
                projectRepo.CurrentWorkingProject = tmpData;
                projectRepo.CurrentWorkingFolder = folderView.NxlFolder;
                
            }         

            // set listView item source.
            //SetListView(fileList);
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

        public void SetListView(IList<INxlFile> list)
        {
            nxlFileList.Clear();

            if (list == null)
            {
                return;
            }

            foreach (INxlFile one in list)
            {
                if (one.IsFolder)//filter folder to display
                {
                    nxlFileList.Add(one);
                }
            }
        }


    }


    /// <summary>
    /// Convert folder to display
    /// </summary>
    public class LocalFolder2ImageConverterEx : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            string name = (string)value[0];
            EnumNxlFileStatus status = (EnumNxlFileStatus)value[1];
            bool isFolder = (bool)value[2];
            if (isFolder)
            {
                return new BitmapImage(new Uri(@"/rmc/resources/icons/Folder.png", UriKind.Relative));
            }
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PathEventArgs : EventArgs
    {
        public PathEventArgs(string transmitPath, ProjectData myProjectData)
        {
            this.TransmitPath = transmitPath;
            this.TransmitWorkingProject = myProjectData;
        }
        public string TransmitPath { get; private set; }
        public ProjectData TransmitWorkingProject { get; private set; }
    }
}
