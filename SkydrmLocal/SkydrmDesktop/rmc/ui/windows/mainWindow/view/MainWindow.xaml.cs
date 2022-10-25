using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using System;
using SkydrmLocal.rmc.ui.windows.mainWindow.helper;
using System.Windows.Input;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Text;
using SkydrmLocal.rmc.helper;
using static SkydrmLocal.rmc.helper.NetworkStatus;
using SkydrmLocal.rmc.ui.components.sortListView;
using SkydrmLocal.rmc.ui.windows.mainWindow.viewModel;
using SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.viewModel;
using SkydrmLocal.rmc.fileSystem.basemodel;
using System.Collections.Generic;
using SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent.model;
using SkydrmLocal.rmc.ui.windows.mainWindow.treeViewComponent;
using SkydrmLocal.rmc.ui.utils;
using SkydrmLocal.rmc.fileSystem.project;
using SkydrmLocal.rmc.common.component;
using System.Windows.Threading;
using SkydrmLocal.rmc.process;
using SkydrmLocal.rmc.fileSystem.external;
using SkydrmLocal.rmc.removeProtection;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.Edit;
using System.Threading;
using SkydrmDesktop;
using SkydrmDesktop.Resources.languages;
using System.ComponentModel;
using System.Configuration;
using Alphaleonis.Win32.Filesystem;
using System.Reflection;

namespace SkydrmLocal.rmc.ui.windows.mainWindow.view
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        // file list context menu
        private ContextMenu contextMenu = new ContextMenu();
        // treeView context menu
        private ContextMenu treeViewContextMenu = new ContextMenu();
        public ViewModelMainWindow viewModel { get; }

        // For scroll scrollviewer
        private bool isNeedScrollTreeView = false;

        // record the window state
        private WindowState LastWinState = WindowState.Normal;

        private ContextMenuEventHandler contextMenuEventHandler;
        private ContextMenuEventHandler treeViewContextMenuEventHandler;

        private void InitWindowState()
        {
            try
            {
                ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
                LastWinState = (WindowState)SkydrmDesktop.Properties.Settings.Default.WindowState;
                this.WindowState = LastWinState;
            }
            catch (ConfigurationErrorsException ex)
            {
                string filename = ex.Filename;
                SkydrmApp.Singleton.Log.Error("Cannot open config file", ex);

                if (File.Exists(filename) == true)
                {
                    SkydrmApp.Singleton.Log.Error($"Config file {filename} content:\n{File.ReadAllText(filename)}");
                    File.Delete(filename);
                    SkydrmApp.Singleton.Log.Error("Config file deleted");
                }
                else
                {
                    SkydrmApp.Singleton.Log.Error($"Config file {filename} does not exist");
                }
                throw ex;
            }
        }
        private void SaveCurrentWindowState()
        {
            SkydrmDesktop.Properties.Settings.Default.WindowState = (int)this.WindowState == 1 ? 0 : (int)this.WindowState;
            SkydrmDesktop.Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Set WPF menu alignment 
        /// </summary>
        private void SetAlignment()
        {
            try
            {
                //Get System  menu Left-handed（true） or Right-handed（false）
                var ifLeft = SystemParameters.MenuDropAlignment;
                if (ifLeft)
                {
                    // change to false
                    var t = typeof(SystemParameters);
                    var field = t.GetField("_menuDropAlignment", BindingFlags.NonPublic | BindingFlags.Static);
                    field.SetValue(null, false);
                    ifLeft = SystemParameters.MenuDropAlignment;
                }
            }
            catch (Exception ex)
            {
                SkydrmApp.Singleton.Log.Error(ex.Message);
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            InitWindowState();

            // Set window's DataContext with ViewModel.
            SkydrmApp.Singleton.Log.Info("Init MainWindow DataContext with ViewModel.");
            viewModel = new ViewModelMainWindow(this);
            // Binding data by DataContext.
            this.DataContext = viewModel;

            // Control the context menu popup through listening the Context menu opening.
            SkydrmApp.Singleton.Log.Info("Init context menu popup through listening the Context menu opening.");
            contextMenuEventHandler = new ContextMenuEventHandler(FileListContextMenuOpening);
            this.fileList.ContextMenuOpening += contextMenuEventHandler;
            this.fileList.ContextMenu = contextMenu;

            this.Filter_fileList.ContextMenuOpening += contextMenuEventHandler;
            this.Filter_fileList.ContextMenu = contextMenu;
            // shared with this project
            this.fileSharedWithList.ContextMenuOpening += contextMenuEventHandler;
            this.fileSharedWithList.ContextMenu = contextMenu;
            // shared from this project
            this.fileSharedByList.ContextMenuOpening += contextMenuEventHandler;
            this.fileSharedByList.ContextMenu = contextMenu;

            // Init IPC
            SkydrmApp.Singleton.Log.Info("Init viewer process");
            viewModel.InitIPC();

            this.Loaded += new RoutedEventHandler(Window_Loaded);
            //fix a bug 51008
            this.Activated += new EventHandler(delegate (object sender, EventArgs e)
            {
                this.Topmost = false;
            });

            this.Closing += new CancelEventHandler(Window_Closing);

            // TreeView item select changed event.
            SkydrmApp.Singleton.Log.Info("Init TreeView item select changed event.");
            this.UserControl_TreeView.treeView.SelectedItemChanged += viewModel.TreeViewItemSelectChanged;
            this.UserControl_TreeView.PreviewMouseWheel += viewModel.UserControl_TreeView_PreviewMouseWheel;
            this.UserControl_TreeView.treeView.PreviewMouseRightButtonDown += viewModel.TreeViewItem_PreviewMouseRightButtonDown;
            // TreeView context menu
            SkydrmApp.Singleton.Log.Info("Init TreeView context menu.");
            treeViewContextMenuEventHandler = new ContextMenuEventHandler(TreeViewContextMenuOpening);
            this.UserControl_TreeView.treeView.ContextMenuOpening += treeViewContextMenuEventHandler;
            this.UserControl_TreeView.treeView.ContextMenu = treeViewContextMenu;

            // Set dataContext for treeView.
            this.UserControl_TreeView.DataContext = viewModel.RepoViewModel;

            // Regsiter network status event listener
            AvailabilityChanged += new NetworkStatusChangedHandler(OnNetworkStatusChanged);
            // init network status
            viewModel.IsNetworkAvailable = NetworkStatus.IsAvailable;

            // disable\enable logout & eixt
            MenuDisableMgr.GetSingleton().MenuItemDisabled += (string name, bool isDisabled) =>
            {
                if (name == "LOGOUT")
                {
                    this.Menu_logout.IsEnabled = !isDisabled;
                }
                else if (name == "EXIT")
                {
                    this.Menu_exit.IsEnabled = !isDisabled;
                }
            };

            SetAlignment();
        }
     
        private void OnNetworkStatusChanged(object sender, NetworkStatusChangedArgs e)
        {
            viewModel.IsNetworkAvailable = e.IsAvailable;

            if (e.IsAvailable)
            {
                if (SkydrmApp.Singleton.User.StartUpload 
                    && (UploadManagerEx.GetInstance().IsQueueAvailable || UploadManagerEx.GetInstance().IsCopyQueueAvailable))
                {
                    viewModel.TryToUpload();
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Fix bug 51008
            this.Topmost = false;

            // Register hook that use to receive info
            (PresentationSource.FromVisual(this) as HwndSource).AddHook(new HwndSourceHook(viewModel.IPCManager.WndProc));

            // try to restore downlaod if existed.
            DownloadManager.GetSingleton().TryRestoreDownload((bool bsuccess, INxlFile file) =>
            {
                viewModel.OnRestoreDownload(bsuccess, file as NxlDoc);
            });
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            // windwo state become normal or max from min, need to refresh local files. 
            if (LastWinState == WindowState.Minimized)
            {
                if (this.WindowState == WindowState.Normal)
                {
                    LastWinState = WindowState.Normal;

                    viewModel.DoRefresh();

                }
                else if (this.WindowState == WindowState.Maximized)
                {
                    viewModel.DoRefresh();
                }
            }

        }

        protected override void MaximizeWindow(object sender, ExecutedRoutedEventArgs e)
        {
            base.MaximizeWindow(sender, e);
            LastWinState = WindowState.Maximized;
        }

        protected override void MinimizeWindow(object sender, ExecutedRoutedEventArgs e)
        {
            base.MinimizeWindow(sender, e);
            LastWinState = WindowState.Minimized;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            SaveCurrentWindowState();

            // Forbid close the window and only hide this, or else can't receive system broadcast.
            e.Cancel = true;
            this.Hide();
        }

        private void ListExternalItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("---ExternalRepo Page ListView-----");
            SkydrmApp.Singleton.Log.Info("ExternalRepo Page ListView double click:");

            ListView listView = e.Source as ListView;
            if (listView != null)
            {
                isNeedScrollTreeView = true;
                viewModel.DbClickExterRepoListNavigateTV((fileSystem.IFileRepo)listView.SelectedItem);
            }
        }

        // Project Page ListBox
        private void ListProjectItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("---Project page ListView-----");
            SkydrmApp.Singleton.Log.Info("Project Page ListView double click:");

            ListView listView = e.Source as ListView;
            if (listView != null)
            {
                isNeedScrollTreeView = true;
                viewModel.DbClickProjectListNavigateTV((ProjectData)listView.SelectedItem);
            }
        }

        private void ListViewItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            viewModel.ListViewItem_DoubleClick(sender, e);
        }

        // List Item selected event, including Click and Double Click.
        private void ListViewItem_Selected(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("---ListViewItem_Selected-----");

            if (sender is ListViewItem)
            {
                ListViewItem selectedItem = sender as ListViewItem;
                viewModel.CurrentSelectedFile = (INxlFile)selectedItem.Content;
                viewModel.ResetCurrentWorkRepoWhenInFilterArea();
            }
        }

        // Popup different Context menu according to the file status automatically.
        private void FileListContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            Console.WriteLine("---FileListContextMenuOpening-----");

            ListView lv = e.Source as ListView;
            //viewModel.CurrentSelectedFile = lv.SelectedItem as INxlFile;

            // Get the selected file in a new way, because when delete a PendingUpload file with the same name in a list with folders. 
            // After delete, the lv.SelectedItme is PendingUploadFile not RmsFile will display error contextmenu 
            int selectIndex = lv.SelectedIndex;
            if(selectIndex < 0)
            {
                e.Handled = true;
                return;
            }

            viewModel.CurrentSelectedFile = lv.Items[selectIndex] as INxlFile;

            if (viewModel.CurrentSelectedFile == null)
            {
                e.Handled = true;
                return;
            }

            if (viewModel.CurrentSelectedFile.IsFolder)
            {
                e.Handled = true;
                return;
            }

            // Now will prevent popup context menu when file is uploading or downloading.
            if (viewModel.CurrentSelectedFile.FileStatus == EnumNxlFileStatus.Uploading
                || viewModel.CurrentSelectedFile.FileStatus == EnumNxlFileStatus.Downloading)
            {
                e.Handled = true;
                return;
            }

            ListContextMenu.PopupContextMenu(viewModel.CurrentSelectedFile, contextMenu);
        }


        private void TreeViewContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            Console.WriteLine("---TreeViewContextMenuOpening-----");

            if (viewModel.CurrentWorkingArea != EnumCurrentWorkingArea.PROJECT_ROOT
                && viewModel.CurrentWorkingArea != EnumCurrentWorkingArea.EXTERNAL_REPO)
            {
                e.Handled = true;
                return;
            }

            ListContextMenu.PopupTreeViewContextMenu(viewModel.CurrentWorkingArea, treeViewContextMenu);
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            string[] filePath;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                Array array = ((System.Array)e.Data.GetData(DataFormats.FileDrop));

                filePath = new string[array.Length];
                for (int i = 0; i < array.Length; i++)
                {
                    filePath[i] = array.GetValue(i).ToString();
                }

                viewModel.DoProtectFromDrop(filePath);
            }
        }

        // To scroll ScrollViewer
        private void TreeViewItem_Selected(TreeViewItem treeViewItem)
        {
            try
            {
                // update ui
                if (isNeedScrollTreeView)
                {
                    this.Dispatcher.BeginInvoke((Action)delegate
                    {
                        if (treeViewItem.IsDescendantOf(this.TreeView_Scroll))
                        {
                            var transForm = treeViewItem.TransformToAncestor(this.TreeView_Scroll);
                            Point offset = transForm.Transform(new Point(0, 0));
                            this.TreeView_Scroll.ScrollToVerticalOffset(offset.Y + this.TreeView_Scroll.VerticalOffset);
                        }
                    });
                }
                isNeedScrollTreeView = false;
            }
            catch (Exception e)
            {
                SkydrmApp.Singleton.Log.Error("Exception in TreeViewItemSelected :" + e.Message, e);
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Only handle TabControl, for other controls, we ignore, for example, when right click ListView control,
            // also will enter this since the route event mechanism.
            if(e.Source != null && e.Source is TabControl)
            {
                viewModel.TabControl_SelectionChanged(sender, e);
            }
        }
        
    }
}
