using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.fileSystem.project;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Interaction logic for FileLocaltionDest.xaml
    /// </summary>
    public partial class FileLocaltionDest : UserControl
    {
        FileLocationDataMode dataMode;

        PageSelectCentralLocation pageCenter;
        PageSelectLocalDrive pageLocal;

        public FileLocaltionDest()
        {
            InitializeComponent();

            dataMode = new FileLocationDataMode();
            this.DataContext = dataMode;

            pageCenter = new PageSelectCentralLocation();
            pageCenter.ProjectChangedEvent += PageCenter_ProjectChangedEvent;

            pageLocal = new PageSelectLocalDrive();
            pageLocal.LocalPathChangedEvent += PageLocal_LocalPathChangedEvent;
            pageLocal.ProjectChangedEvent += PageLocal_ProjectChangedEvent;        
  
        }
        #region Event callbacks
        public event RoutedEventHandler OkBtnClicked;
        public event RoutedEventHandler CancelBtnClicked;
        #endregion

        #region Setters&Getters
        /// <summary>
        /// Set PageLocalDrive path.
        /// </summary>
        public string LocalPath
        {
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    return;
                }
                pageLocal.LocalDrivePath = value;
            }
        }

        /// <summary>
        /// Set PageCentral Title
        /// </summary>
        /// <param name="visibility"></param>
        public void ProcessCentral_Title(Visibility visibility)
        {
            pageCenter.TitleVisible = visibility;
        }

        public void ProcessCentral_RemoveTvItem(int id)
        {
            pageCenter?.RemoveTreeViewItem(id);
        }

        public void ProcessCentral_ReInitTreeView()
        {
            //pageCenter = new PageSelectCentralLocation();
            pageCenter.ProjectChangedEvent -= PageCenter_ProjectChangedEvent;
            pageCenter.ProjectChangedEvent += PageCenter_ProjectChangedEventDoShare;
        }

        /// <summary>
        ///  Set treeView defult selected in central location page
        /// </summary>
        /// <param name="path"></param>
        /// <param name="project"></param>
        public void ProcessCentral_TreeViewSelected(string path, IMyProject project)
        {
            pageCenter.SetTreeViewItemSelected(path, project);
        }

        /// <summary>
        ///  Set pageLocalDrive combox itemSource
        ///  if param is null,will set server project as itemSource
        /// </summary>
        /// <param name="projectDatas"></param>
        public void ProcessLocal_CbItemSource(List<ProjectData> projectDatas)
        {
            pageLocal.SetCmBoxItemSource(projectDatas);
        }

        /// <summary>
        ///  Set pageLocalDrive combox defult selected item
        /// </summary>
        /// <param name="project"></param>
        public void ProcessLocal_CbSelectItem(IMyProject project)
        {
            pageLocal.SetCmBoxSelectItem(project);
        }

        /// <summary>
        /// Set pageLocalDrive comboBox visibility
        /// </summary>
        /// <param name="visibility"></param>
        public void ProcessLocal_CbVisible(Visibility visibility)
        {
            pageLocal.CmBoxVisibility = visibility;
        }

        /// <summary>
        /// Set pageLocalDrive Frame TreeView visibility
        /// </summary>
        /// <param name="visibility"></param>
        public void ProcessLocal_TreeViewVisible(Visibility visibility)
        {
            pageLocal.FramTreeView = visibility;
        }

        /// <summary>
        ///  Set treeView defult selected in local drive page
        /// </summary>
        /// <param name="path"></param>
        /// <param name="project"></param>
        public void ProcessLocal_TreeViewSelected(string path, IMyProject project)
        {
            pageLocal.ProcessCentralLocal(path, project);
        }

        /// <summary>
        /// Set central radio isChecked and isEnable status
        /// if isChecked is true, will trigger radioChecked event
        /// </summary>
        /// <param name="isChecked"></param>
        /// <param name="isEnable"></param>
        public void SetCentralRadio(bool isChecked, bool isEnable = true)
        {
            rb_Central.IsChecked = isChecked;
            rb_Central.IsEnabled = isEnable;     
        }

        /// <summary>
        /// Set local radio isChecked and isEnable status
        /// if isChecked is true, will trigger radioChecked event
        /// </summary>
        /// <param name="isChecked"></param>
        /// <param name="isEnable"></param>
        public void SetLocalRadio(bool isChecked, bool isEnable = true)
        {
            rb_Local.IsChecked = isChecked;
            rb_Local.IsEnabled = isEnable;     
        }

        public bool LocalRadioChecked { get => (bool)rb_Local.IsChecked; }
        public string Path
        {
            get => dataMode.Path;
            set
            {
                dataMode.Path = value;
            }
        }
        public featureProvider.IMyProject Project { get => dataMode.project; }
        #endregion

        #region Events
        private void PageCenter_ProjectChangedEventDoShare(featureProvider.IMyProject project, string path)
        {
            if (rb_Central.IsChecked == true)
            {
                dataMode.project = project;
                if (project != null)
                {
                    dataMode.Path = path;
                    this.OkBtn.IsEnabled = true;
                }
                else
                {
                    dataMode.Path = "";
                    this.OkBtn.IsEnabled = false;
                }
                
            }
        }

        private void PageCenter_ProjectChangedEvent(featureProvider.IMyProject project, string path)
        {
            if (rb_Central.IsChecked==true)
            {
                dataMode.project = project;
                dataMode.Path = path;
            }        
        }

        /// <summary>
        /// PageLocal project selected changed event
        /// </summary>
        /// <param name="project"></param>
        private void PageLocal_ProjectChangedEvent(featureProvider.IMyProject project)
        {
            if (rb_Local.IsChecked == true)
            {
                dataMode.project = project;
            }
            if (project != null)
            {
                this.OkBtn.IsEnabled = true;
            }
        }

        /// <summary>
        ///  Browse button event trigger
        /// </summary>
        /// <param name="path"></param>
        private void PageLocal_LocalPathChangedEvent(string path)
        {
            dataMode.Path = path;
        }

        private void On_Central_RadioChecked(object sender, RoutedEventArgs e)
        {
            dataMode.project = pageCenter.MyProject;
            dataMode.Path = pageCenter.CentralPath;
            SwitchPage(pageCenter);

            this.OkBtn.IsEnabled = true;
        }

        private void On_Local_RadioChecked(object sender, RoutedEventArgs e)
        {
            if (pageLocal.FramTreeView == Visibility.Visible)
            {
                dataMode.project = pageLocal.TreeViewSelectProject;
            }
            else if (pageLocal.CmBoxVisibility == Visibility.Visible)
            {
                dataMode.project = pageLocal.CbSelectProject;
                if (!pageLocal.CheckCmBoxIsSelected())
                {
                    this.OkBtn.IsEnabled = false;
                }
            }
            else
            {
                dataMode.project = null;
            }
            
            dataMode.Path = pageLocal.LocalDrivePath;
            SwitchPage(pageLocal);

        }

        private void On_Ok_Btn(object sender, RoutedEventArgs e)
        {
            this.OkBtnClicked?.Invoke(sender, e);
        }

        private void On_Cacle_Btn(object sender, RoutedEventArgs e)
        {
            this.CancelBtnClicked?.Invoke(sender, e);
        }
        #endregion

        private void SwitchPage(Page page)
        {
            this.fm_Body.Content = page;
        }

    }
    /// <summary>
    /// DataMode for FileLocaltionDest.xaml
    /// </summary>
    public class FileLocationDataMode : INotifyPropertyChanged
    {
        public FileLocationDataMode()
        {
        }

        string mPath;

        public featureProvider.IMyProject project;

        public string Path { get => mPath; set { mPath = value; OnBindUIPropertyChanged("Path"); } }


        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnBindUIPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }


}
