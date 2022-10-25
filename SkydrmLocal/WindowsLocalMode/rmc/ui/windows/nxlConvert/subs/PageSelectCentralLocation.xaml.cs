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

namespace SkydrmLocal.rmc.ui.windows.nxlConvert.subs
{
    /// <summary>
    /// Interaction logic for PageSelectCentralLocation.xaml
    /// </summary>
    public partial class PageSelectCentralLocation : Page
    {
        private static readonly string MY_VAULT = CultureStringInfo.MainWin__TreeView_MyVault;
        private static readonly string PROJECT = CultureStringInfo.MainWin__TreeView_Project;

        private string centralPath = MY_VAULT;
        private IMyProject myProject;

        #region // TreeView model
        private RepoViewModel repoViewModel;

        private MyVaultRepo myVaultRepo;
        private ProjectRepo projectRepo;

        private EnumCurrentWorkingArea currentWorkingArea;
        // Current working repo
        private IFileRepo currentWorkRepo;

        #endregion // For treeView model

        public PageSelectCentralLocation()
        {
            InitializeComponent();

            InitData();
        }

        #region Use for external
        // Select project notification.
        public delegate void ProjectChangedHandler(IMyProject project, string path);
        public event ProjectChangedHandler ProjectChangedEvent;

        public IMyProject MyProject { get => myProject; }
        public string CentralPath { get => centralPath; }

        public Visibility TitleVisible { set => tb_Title.Visibility = value; }

        public void SetTreeViewItemSelected(string path, IMyProject project)
        {
            if (path.Equals(MY_VAULT, StringComparison.CurrentCultureIgnoreCase) || project == null)
            {
                foreach (var item in repoViewModel.Roots)//RootViewModel
                {
                    if (item.RootName.Equals(path, StringComparison.CurrentCultureIgnoreCase))//MyVault
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
                    foreach (var item in repoViewModel.Roots) //RootViewModel
                    {
                        if (item.RootName.Equals(PROJECT, StringComparison.CurrentCultureIgnoreCase))
                        {
                            RootViewModel rootView = item as RootViewModel;
                            rootView.IsExpanded = true;// will doRefresh
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
                                            projectView.IsExpanded = true;// will doRefresh
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
                                            folderView.IsExpanded = true;// will doRefresh
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

        // For Share nxl File
        public void RemoveTreeViewItem(int projectId)
        {
            ObservableCollection<TreeViewItemViewModel> Children = new ObservableCollection<TreeViewItemViewModel>();

            foreach (RootViewModel root in repoViewModel.Roots)
            {
                if (root.Root.RepoName.Equals(MY_VAULT, StringComparison.CurrentCultureIgnoreCase))
                {
                    repoViewModel.Roots.Remove(root);
                    break;
                }
            }
            foreach (RootViewModel root in repoViewModel.Roots)
            {
                if (root.Root.RepoName.Equals(PROJECT, StringComparison.CurrentCultureIgnoreCase))
                {
                    RootViewModel rootView = root as RootViewModel;
                    rootView.IsExpanded = true;// will loadChildren

                    Children = rootView.Children;// ProjectViewModel
                }
            }
            foreach (var item in Children)
            {
                if (item is ProjectViewModel)
                {
                    ProjectViewModel projectView = item as ProjectViewModel;
                    if (projectView.Project.ProjectInfo.ProjectId == projectId)
                    {
                        item.IsEnable = false;
                        break;
                    }
                }

            }

        }
        #endregion

        private void InitData()
        {
            repoViewModel = new RepoViewModel();
            myVaultRepo = new MyVaultRepo();
            projectRepo = new ProjectRepo();

            // project
            if (projectRepo.GetAllFolder().Count == 0)
            {
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
            repoViewModel.Start(fileRepos, true);

            //Whether UI is displayed or not
            //IsDisplayTreeview = RepoViewModel.Roots.Count > 0 ? true : false;

            // Note: must set DataContext after setting data source.
            this.UserControl_TreeView.DataContext = repoViewModel;

            // TreeView item select changed event.
            this.UserControl_TreeView.treeView.SelectedItemChanged += TreeViewItemSelectChanged;

            this.UserControl_TreeView.PreviewMouseWheel += UserControl_TreeView_PreviewMouseWheel;

        }

        private void UserControl_TreeView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
            //set routedEvent Type
            eventArg.RoutedEvent = UIElement.MouseWheelEvent;
            eventArg.Source = sender;
            (sender as UserControl).RaiseEvent(eventArg);
        }

        private void TreeViewItemSelectChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeViewItemViewModel treeViewItem = e.NewValue as TreeViewItemViewModel;

            if (treeViewItem is RootViewModel)
            {
                RootViewModel rootView = treeViewItem as RootViewModel;
                if (rootView.RootName.Equals(MY_VAULT, StringComparison.CurrentCultureIgnoreCase))
                {
                    currentWorkingArea = EnumCurrentWorkingArea.MYVAULT;
                    currentWorkRepo = myVaultRepo;

                }
                else
                {
                    currentWorkingArea = EnumCurrentWorkingArea.PROJECT;
                    currentWorkRepo = projectRepo;

                }
            }
            else if (treeViewItem is ProjectViewModel)
            {
                ProjectViewModel projectView = treeViewItem as ProjectViewModel;

                ProjectData tmpData = projectView.Project;
                currentWorkingArea = EnumCurrentWorkingArea.PROJECT_ROOT;
                currentWorkRepo = projectRepo;
                projectRepo.CurrentWorkingProject = tmpData;

            }
            else if (treeViewItem is FolderViewModel)
            {
                FolderViewModel folderView = treeViewItem as FolderViewModel;

                currentWorkingArea = EnumCurrentWorkingArea.PROJECT_FOLDER;
                currentWorkRepo = projectRepo;
                ProjectData tmpData = FindProject(folderView);
                projectRepo.CurrentWorkingProject = tmpData;
                projectRepo.CurrentWorkingFolder = folderView.NxlFolder;

            }

            SetCurSavePath();
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

        private void SetCurSavePath()
        {
            switch (currentWorkingArea)
            {
                case EnumCurrentWorkingArea.MYVAULT:
                case EnumCurrentWorkingArea.PROJECT:
                    centralPath = MY_VAULT;
                    break;
                case EnumCurrentWorkingArea.PROJECT_ROOT:
                    centralPath = projectRepo.CurrentWorkingProject.ProjectInfo.DisplayName + "/";
                    break;
                case EnumCurrentWorkingArea.PROJECT_FOLDER:
                    string rootpath = projectRepo.CurrentWorkingProject.ProjectInfo.DisplayName + "/";
                    if (projectRepo.CurrentWorkingFolder.PathDisplay.Length > 1)
                    {
                        string folderpath = projectRepo.CurrentWorkingFolder.PathDisplay.Substring(1);
                        centralPath = rootpath + folderpath;
                    }
                    break;
                default:
                    centralPath = MY_VAULT;
                    break;
            }

            if (centralPath == MY_VAULT)
            {
                myProject = null;
            }
            else
            {
                myProject = projectRepo.CurrentWorkingProject.ProjectInfo.Raw;              
            }
            ProjectChangedEvent?.Invoke(myProject, centralPath);

        }

        private void TreeViewItem_Selected(TreeViewItem treeViewItem)
        {
            try
            {
                // update ui
                this.Dispatcher.BeginInvoke((Action)delegate
                {
                    Point offset = treeViewItem.TransformToAncestor(this.TreeView_Scroll).Transform(new Point(0, 0));
                    this.TreeView_Scroll.ScrollToVerticalOffset(offset.Y + this.TreeView_Scroll.VerticalOffset);
                });
            }
            catch (Exception e)
            {
                SkydrmLocalApp.Singleton.Log.Error("Exception in TreeViewItemSelected :" + e.Message, e);
            }
        }

    }
}
