using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.fileSystem.project;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Alphaleonis.Win32.Filesystem;
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
    /// Interaction logic for PageSelectLocalDrive.xaml
    /// </summary>
    public partial class PageSelectLocalDrive : Page
    {
        private LocalDriveMode localDriveMode;
        private PageSelectCentralLocation pageCenter;

        public PageSelectLocalDrive()
        {
            InitializeComponent();

            localDriveMode = new LocalDriveMode();

            this.DataContext = localDriveMode;

            // Now, we don't use this page, and use comboBox to display project.
            if (false)
            {
                pageCenter = new PageSelectCentralLocation();
                pageCenter.TitleVisible = Visibility.Collapsed;
                pageCenter.ProjectChangedEvent += PageCenter_ProjectChangedEvent;
                fm_treeView.Content = pageCenter;
            }
            

        }
        #region Event callback
        // Select LocalPath notification.
        public delegate void LocalPathChangedHandler(string path);
        public event LocalPathChangedHandler LocalPathChangedEvent;

        // Select Project notification.
        public delegate void ProjectChangedHandler(IMyProject project);
        public event ProjectChangedHandler ProjectChangedEvent;
        #endregion

        #region Setter&Getter
        public string LocalDrivePath { get => localDriveMode.LocalDrivePath; set => localDriveMode.LocalDrivePath = value;  }

        public IMyProject TreeViewSelectProject { get => pageCenter.MyProject; }

        public IMyProject CbSelectProject { get => localDriveMode.CbSelectedProject; }

        public void SetCmBoxItemSource(List<ProjectData> projectDatas = null)
        {
            if ( projectDatas == null || projectDatas.Count == 0)
            {
                this.cb_projectSelect.ItemsSource = localDriveMode.ProjectFilePool;
            }
            else
            {
                this.cb_projectSelect.ItemsSource = projectDatas;
            }          
        }

        public void SetCmBoxSelectItem(IMyProject project)
        {
            if (project == null)
            {
                this.cb_projectSelect.SelectedIndex = 0; // This will trigger comboBox_SelectionChanged
                return;
            }
            foreach (var item in localDriveMode.ProjectFilePool)
            {              
                if (item.ProjectInfo.Raw.Id==project.Id)
                {
                    this.cb_projectSelect.SelectedItem = item;
                    ProjectChangedEvent?.Invoke(item.ProjectInfo.Raw);
                    break;
                }
            }        
        }

        public bool CheckCmBoxIsSelected()
        {
            var item = this.cb_projectSelect.SelectedItem;
            if (item == null)
            {
                return false;
            }
            return true;
        }

        public Visibility CmBoxVisibility { get=> cb_parent.Visibility; set => cb_parent.Visibility = value; }

        public Visibility FramTreeView { get => fm_treeView.Visibility; set => fm_treeView.Visibility = value; }

        /// <summary>
        ///  Set treeview defult selected item
        /// </summary>
        /// <param name="path"></param>
        /// <param name="project"></param>
        public void ProcessCentralLocal(string path, IMyProject project)
        {
            pageCenter.SetTreeViewItemSelected(path, project);
        }
        #endregion

        #region Events
        private void OnBrowseBtn_Clicked(object sender, RoutedEventArgs e)
        {
            //init Dir.
            string dftRootFolder = this.localDriveMode.LocalDrivePath;
            System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog();
            dlg.Description = "Select a Folder";
            if (Directory.Exists(dftRootFolder))
            {
                dlg.SelectedPath = dftRootFolder;
            }
            var result = dlg.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK || result == System.Windows.Forms.DialogResult.Yes)
            {
                if (!dlg.SelectedPath.EndsWith(Alphaleonis.Win32.Filesystem.Path.DirectorySeparatorChar.ToString()))
                {
                    this.localDriveMode.LocalDrivePath = dlg.SelectedPath + Alphaleonis.Win32.Filesystem.Path.DirectorySeparatorChar;
                }
                else
                {
                    this.localDriveMode.LocalDrivePath = dlg.SelectedPath;
                }

                LocalPathChangedEvent?.Invoke(this.localDriveMode.LocalDrivePath);
            }

        }

        private void cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            if (comboBox != null)
            {
                ProjectData projectData = (ProjectData)comboBox.SelectedItem;
                localDriveMode.CbSelectedProject = projectData.ProjectInfo.Raw;

                ProjectChangedEvent?.Invoke(localDriveMode.CbSelectedProject);
            }
            
        }

        private void PageCenter_ProjectChangedEvent(IMyProject project, string path)
        {
            ProjectChangedEvent?.Invoke(project);
        }
        #endregion

    }

    public class LocalDriveMode : INotifyPropertyChanged
    {
        public LocalDriveMode()
        {
            projectRepo = new ProjectRepo();
        }
        private ProjectRepo projectRepo;

        private IMyProject cbSelectedProject;

        private string localDrivePath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

        public string LocalDrivePath
        {
            get => localDrivePath;
            set
            {
                localDrivePath = value;
                OnPropertyChanged("LocalDrivePath");
            }
        }

        public IList<ProjectData> ProjectFilePool { get => projectRepo.GetProjectsInfo(); }

        public IMyProject CbSelectedProject { get => cbSelectedProject; set => cbSelectedProject = value; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    #region Convert
    public class ProjectIconConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool isOwner = (bool)value;
                if (isOwner)
                {
                    return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/projectByMe.png";
                }
                return @"/rmc/ui/windows/mainWindow/treeViewComponent/Images/projectByOthers.png";
            }
            catch (Exception)
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion


}
