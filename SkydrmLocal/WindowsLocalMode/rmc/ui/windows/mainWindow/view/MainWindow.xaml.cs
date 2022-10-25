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
using static SkydrmLocal.rmc.ui.utils.CommonUtils;
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
        private bool isDoubleClickProjectList = false;

        // record the window state
        private WindowState LastWinState = WindowState.Normal;

        private ContextMenuEventHandler contextMenuEventHandler;
        private ContextMenuEventHandler treeViewContextMenuEventHandler;

        private void InitWindowState()
        {
            LastWinState = (WindowState)Properties.Settings.Default.WindowState;
            this.WindowState = LastWinState;
        }
        private void SaveCurrentWindowState()
        {
            Properties.Settings.Default.WindowState = (int)this.WindowState == 1 ? 0 : (int)this.WindowState;
            Properties.Settings.Default.Save();
        }

        public MainWindow()
        {
            InitializeComponent();

            InitWindowState();

            // Set window's DataContext with ViewModel.
            SkydrmLocalApp.Singleton.Log.Info("Init MainWindow DataContext with ViewModel.");
            viewModel = new ViewModelMainWindow(this);
            // Binding data by DataContext.
            this.DataContext = viewModel;

            // Control the context menu popup through listening the Context menu opening.
            SkydrmLocalApp.Singleton.Log.Info("Init context menu popup through listening the Context menu opening.");
            contextMenuEventHandler = new ContextMenuEventHandler(FileListContextMenuOpening);
            this.fileList.ContextMenuOpening += contextMenuEventHandler;
            this.fileList.ContextMenu = contextMenu;

            this.Filter_fileList.ContextMenuOpening += contextMenuEventHandler;
            this.Filter_fileList.ContextMenu = contextMenu;

            // Init viewer process
            SkydrmLocalApp.Singleton.Log.Info("Init viewer process");
            viewModel.InitViewerProcess();

            this.Loaded += new RoutedEventHandler(Window_Loaded);
            //fix a bug 51008
            this.Activated += new EventHandler(delegate (object sender, EventArgs e)
            {
                this.Topmost = false;
            });

            this.Closed += new EventHandler(Window_Closed);

            // TreeView item select changed event.
            SkydrmLocalApp.Singleton.Log.Info("Init TreeView item select changed event.");
            this.UserControl_TreeView.treeView.SelectedItemChanged += viewModel.TreeViewItemSelectChanged;
            this.UserControl_TreeView.PreviewMouseWheel += viewModel.UserControl_TreeView_PreviewMouseWheel;
            this.UserControl_TreeView.treeView.PreviewMouseRightButtonDown += viewModel.TreeViewItem_PreviewMouseRightButtonDown;
            // TreeView context menu
            SkydrmLocalApp.Singleton.Log.Info("Init TreeView context menu.");
            treeViewContextMenuEventHandler = new ContextMenuEventHandler(TreeViewContextMenuOpening);
            this.UserControl_TreeView.treeView.ContextMenuOpening += treeViewContextMenuEventHandler;
            this.UserControl_TreeView.treeView.ContextMenu = treeViewContextMenu;

            // Set dataContext for treeView.
            //this.UserControl_TreeView.DataContext = viewModel.RepoViewModel;

            // Regsiter network status event listener
            AvailabilityChanged += new NetworkStatusChangedHandler(OnNetworkStatusChanged);
            // init network status
            viewModel.IsNetworkAvailable = NetworkStatus.IsAvailable;

            // disable\enable logout & eixt
            MenuDisableMgr.GetSingleton().MenuItemDisabled += (string name, bool isDisabled) =>
            {
                if (name == "Log out")
                {
                    this.Menu_logout.IsEnabled = !isDisabled;
                }
                else if (name == "Exit")
                {
                    this.Menu_exit.IsEnabled = !isDisabled;
                }
            };
        }
     
        private void OnNetworkStatusChanged(object sender, NetworkStatusChangedArgs e)
        {
            viewModel.IsNetworkAvailable = e.IsAvailable;

            if (e.IsAvailable)
            {
                if (SkydrmLocalApp.Singleton.User.StartUpload 
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

        // Kill all viewer process when main window close.
        private void Window_Closed(object sender, EventArgs e)
        {
            SaveCurrentWindowState();

            //Process[] player = Process.GetProcessesByName("Viewer");
            //if (player.Length > 0)
            //{
            //    foreach (Process proc in player)
            //    {
            //        proc.Kill();
            //    }
            //}
        }

        // Project Page ListBox
        private void ListProjectItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine("---Project page ListBox-----");
            SkydrmLocalApp.Singleton.Log.Info("Project Page ListBox double click:");

            ListBox boxItem = e.Source as ListBox;
            if (boxItem != null)
            {
                isDoubleClickProjectList = true;
                viewModel.TreeviewProjectNavigate((ProjectData)boxItem.SelectedItem);
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
                viewModel.ResetCurrentWorkRepoWhenInOfflineFilterArea();
            }
        }

        #region ListView contexmenu
        // Popup different Context menu according to the file status automatically.
        private void FileListContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            Console.WriteLine("---FileListContextMenuOpening-----");

            ListView lv = e.Source as ListView;
            viewModel.CurrentSelectedFile = lv.SelectedItem as INxlFile;

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

            PopupContextMenu(viewModel.CurrentSelectedFile); 
        }

        private void PopupContextMenu(INxlFile selected)
        {
            contextMenu.Items.Clear();

            // mark and unmark item
            MenuItemMarkAndUnMark_Offline(selected, ref contextMenu);

            // save as item
            MenuItemSaveAs(selected, out MenuItem item_saveAs, ref contextMenu);

            // extract content item
            MenuItemExtract(selected, out MenuItem item_extract_content, ref contextMenu);

            // modify rights item
            MenuItemModifyRights(selected, out MenuItem item_modifyRights, ref contextMenu);

            // share nxl file item: Share to person
            MenuItemShare(selected, out MenuItem item_shareToPerson, ref contextMenu);

            // share nxl file item: Share to project
            MenuItemAddFileToProject(selected, out MenuItem item_shareToProject, ref contextMenu);

            // view item
            MenuItemView(selected, out MenuItem item_view, ref contextMenu);

            // view file info item
            MenuItemViewFileInfo(selected, out MenuItem item_viewFileInfo, ref contextMenu);

            // add Separator
            contextMenu.Items.Add(new Separator());

            // remove item
            MenuItemRemove(selected, ref contextMenu);

            // openSkyDRM item
            MenuItemOpenSky(selected, ref contextMenu);

            // re-upload
            MenuItemUpload(selected, ref contextMenu);

            // handle all menuItem
            HandleAllMenuItem(selected, item_saveAs, 
                item_extract_content, item_modifyRights, 
                item_shareToPerson, item_shareToProject,
                item_view, item_viewFileInfo);
        }

        #region Add menuItem for listView contextmenu
        private void MenuItemUpload(INxlFile selected, ref ContextMenu contextMenu)
        {
            if (selected.FileStatus == EnumNxlFileStatus.UploadFailed)
            {
                contextMenu.Items.Clear();

                MenuItem item_upload = CreaeteMenuItem(CultureStringInfo.MainWin_ContextMenu_Upload, "/rmc/resources/icons/Icon_menu_upload.ico");
                item_upload.Command = viewModel.ContextMenuCommand;
                item_upload.CommandParameter = new ContextMenuCmdArgs(selected, Constant.CONTEXT_MENU_CMD_UPLOAD);
                contextMenu.Items.Add(item_upload);
                contextMenu.Items.Add(new Separator());

                MenuItem item_remove = CreaeteMenuItem(CultureStringInfo.MainWin_ContextMenu_Remove, "/rmc/resources/icons/Icon_remove.png");
                item_remove.Command = viewModel.ContextMenuCommand;
                item_remove.CommandParameter = new ContextMenuCmdArgs(selected, Constant.CONTEXT_MENU_CMD_REMOVE);
                contextMenu.Items.Add(item_remove);
            }
        }

        private void MenuItemMarkAndUnMark_Offline(INxlFile selected, ref ContextMenu contextMenu)
        {
            // mark unmark
            if (selected.Location == EnumFileLocation.Online)
            {
                MenuItem item_makeOffline = CreaeteMenuItem(CultureStringInfo.MainWin_ContextMenu_Mark, "/rmc/resources/icons/Icon_menu_offline.png");

                // check network status -- if net is offline, we disable it; and if the file is openning, also disable it -- fix bug 52924
                item_makeOffline.IsEnabled = viewModel.IsNetworkAvailable && !ViewerProcess.ContainsKey(selected.RmsRemotePath);

                // judge the file format, if not support, disable it 
                EnumFileType type = CommonUtils.GetFileTypeByExtension(selected.Name);
                if (type == EnumFileType.FILE_TYPE_NOT_SUPPORT)
                {
                    item_makeOffline.IsEnabled = false;
                }

                // if item isEnable is false, should replace gray icon.
                if (!item_makeOffline.IsEnabled)
                {
                    ChangeMenuItemIcon(item_makeOffline, "/rmc/resources/icons/Icon_menu_offline_gray.ico");
                }

                item_makeOffline.Command = viewModel.ContextMenuCommand;
                item_makeOffline.CommandParameter = new ContextMenuCmdArgs(item_makeOffline, selected, Constant.CONTEXT_MENU_CMD_MAKE_OFFLINE);
                contextMenu.Items.Add(item_makeOffline);
            }
            else if (selected.FileStatus == EnumNxlFileStatus.AvailableOffline ||
                 selected.IsEdit)
            {
                // Item unmark offline
                MenuItem item_unmarkOffline = CreaeteMenuItem(CultureStringInfo.MainWin_ContextMenu_UnMark, "/rmc/resources/icons/Icon_menu_offline.png");
                item_unmarkOffline.Command = viewModel.ContextMenuCommand;
                item_unmarkOffline.CommandParameter = new ContextMenuCmdArgs(selected, Constant.CONTEXT_MENU_CMD_UNMAKE_OFFLINE);

                // Should disable the item if the file is openning (fix bug 52924), or if the file is editing(fix bug 54186).
                item_unmarkOffline.IsEnabled = !ViewerProcess.ContainsKey(selected.LocalPath) && !FileEditorHelper.IsFileEditing(selected.LocalPath);
                if (!item_unmarkOffline.IsEnabled)
                {
                    ChangeMenuItemIcon(item_unmarkOffline, "/rmc/resources/icons/Icon_menu_offline_gray.ico");
                }
                contextMenu.Items.Add(item_unmarkOffline);

            }
        }

        private void MenuItemSaveAs(INxlFile selected, out MenuItem item_saveAs, ref ContextMenu contextMenu)
        {
            item_saveAs = CreaeteMenuItem(CultureStringInfo.MainWin_ContextMenu_SaveAs, "/rmc/resources/icons/Icon_SaveAs.png");
            if (selected.FileStatus != EnumNxlFileStatus.WaitingUpload)
            {
                item_saveAs.Command = viewModel.ContextMenuCommand;
                item_saveAs.CommandParameter = new ContextMenuCmdArgs(selected, Constant.CONTEXT_MENU_CMD_SAVE_AS);
                // default is false, then to judge if has Save As rights.
                item_saveAs.IsEnabled = false;
                ChangeMenuItemIcon(item_saveAs, "/rmc/resources/icons/Icon_SaveAs_gray.ico");
                contextMenu.Items.Add(item_saveAs);
            }
        }

        private void MenuItemExtract(INxlFile selected, out MenuItem item_extract_content, ref ContextMenu contextMenu)
        {
            item_extract_content = CreaeteMenuItem(CultureStringInfo.MainWin_ContextMenu_ExtractContent, "/rmc/resources/icons/Icon_menu_extract.png");
            if (IsAddExtractContentMenu(selected.FileStatus) && selected.FileRepo != EnumFileRepo.REPO_MYVAULT)
            {
                item_extract_content.IsEnabled = false;
                ChangeMenuItemIcon(item_extract_content, "/rmc/resources/icons/Icon_menu_extract_gray2.ico");

                item_extract_content.Command = viewModel.ContextMenuCommand;
                item_extract_content.CommandParameter = new ContextMenuCmdArgs(item_extract_content, selected, Constant.CONTEXT_MENU_CMD_EXTRACT_CONTENT);
                contextMenu.Items.Add(item_extract_content);
                contextMenu.Items.Add(new Separator());
            }
        }
        private bool IsAddExtractContentMenu(EnumNxlFileStatus enumNxlFileStatus)
        {
            bool result = false;

            if (enumNxlFileStatus != EnumNxlFileStatus.Uploading
                &&
                enumNxlFileStatus != EnumNxlFileStatus.DownLoadedFailed
                &&
                enumNxlFileStatus != EnumNxlFileStatus.Downloading
                &&
                enumNxlFileStatus != EnumNxlFileStatus.ProtectFailed
                &&
                enumNxlFileStatus != EnumNxlFileStatus.RemovedFromLocal
                &&
                enumNxlFileStatus != EnumNxlFileStatus.UnknownError)
            {
                result = true;
            }

            return result;
        }

        private void MenuItemModifyRights(INxlFile selected, out MenuItem item_modifyRights, ref ContextMenu contextMenu)
        {
            item_modifyRights = CreaeteMenuItem(CultureStringInfo.MainWin_ContextMenu_ModifyRights,
                   "/rmc/resources/icons/Icon_menu_modifyrights.png");
            if (selected.FileRepo == EnumFileRepo.REPO_PROJECT && selected.FileStatus != EnumNxlFileStatus.WaitingUpload)
            {
                item_modifyRights.Command = viewModel.ContextMenuCommand;
                item_modifyRights.CommandParameter = new ContextMenuCmdArgs(selected,
                    Constant.CONTEXT_MENU_CMD_MODIFY_RIGHTS);

                item_modifyRights.IsEnabled = false;

                if (!item_modifyRights.IsEnabled)
                {
                    ChangeMenuItemIcon(item_modifyRights, "/rmc/resources/icons/Icon_menu_modifyrights_gray.ico");
                }
                contextMenu.Items.Add(item_modifyRights);
            }
        }

        private void MenuItemShare(INxlFile selected, out MenuItem item_shareToPerson, ref ContextMenu contextMenu)
        {
            item_shareToPerson = CreaeteMenuItem(CultureStringInfo.MainWin_ContextMenu_Share, "/rmc/resources/icons/Icon_menu_share.png");
            item_shareToPerson.Command = viewModel.ContextMenuCommand;
            //if (status == EnumNxlFileStatus.WaitingUpload && selected.FileRepo == EnumFileRepo.REPO_MYVAULT)
            //{
            //    item_shareToPerson.CommandParameter = new ContextMenuCmdArgs(selected, Constant.CONTEXT_MENU_CMD_SHARE);
            //    item_shareToPerson.IsEnabled = false;
            //    ChangeMenuItemIcon(item_shareToPerson, "/rmc/resources/icons/Icon_menu_share_gray.ico");
            //    contextMenu.Items.Add(item_shareToPerson);
            //}
            if (selected.FileStatus != EnumNxlFileStatus.WaitingUpload)
            {
                item_shareToPerson.CommandParameter = new ContextMenuCmdArgs(selected, Constant.CONTEXT_MENU_CMD_SHARE_TO_PERSON);
                item_shareToPerson.IsEnabled = false;
                ChangeMenuItemIcon(item_shareToPerson, "/rmc/resources/icons/Icon_menu_share_gray.ico");
                contextMenu.Items.Add(item_shareToPerson);
            }
        }

        private void MenuItemAddFileToProject(INxlFile selected, out MenuItem item_shareToProject, ref ContextMenu contextMenu)
        {
            item_shareToProject = CreaeteMenuItem(CultureStringInfo.MainWin_ContextMenu_AddFile, "/rmc/resources/icons/Icon_menu_addfile.png");
            if (selected.FileStatus != EnumNxlFileStatus.WaitingUpload)
            {
                if (selected.FileRepo != EnumFileRepo.REPO_MYVAULT
                    && selected.FileRepo != EnumFileRepo.REPO_SHARED_WITH_ME)
                {
                    item_shareToProject.Command = viewModel.ContextMenuCommand;
                    item_shareToProject.CommandParameter = new ContextMenuCmdArgs(selected, Constant.CONTEXT_MENU_CMD_SHARE_TO_PROJECT);
                    item_shareToProject.IsEnabled = false;
                    ChangeMenuItemIcon(item_shareToProject, "/rmc/resources/icons/Icon_menu_addfile_gray.ico");
                    contextMenu.Items.Add(item_shareToProject);
                }
                contextMenu.Items.Add(new Separator());
            }
        }

        private void MenuItemView(INxlFile selected, out MenuItem item_view, ref ContextMenu contextMenu)
        {
            item_view = CreaeteMenuItem(CultureStringInfo.MainWin_ContextMenu_View, "/rmc/resources/icons/Icon_viewFile.png");
            // Disable online view item if network is offline
            if (selected.Location == EnumFileLocation.Online && !viewModel.IsNetworkAvailable)
            {
                item_view.IsEnabled = false;
                ChangeMenuItemIcon(item_view, "/rmc/resources/icons/Icon_viewFile_gray.ico");
            }
            item_view.Command = viewModel.ContextMenuCommand;
            item_view.CommandParameter = new ContextMenuCmdArgs(selected, Constant.CONTEXT_MENU_CMD_VIEW_FILE);
            contextMenu.Items.Add(item_view);
        }

        private void MenuItemViewFileInfo(INxlFile selected, out MenuItem item_viewFileInfo, ref ContextMenu contextMenu)
        {
            item_viewFileInfo = CreaeteMenuItem(CultureStringInfo.MainWin_ContextMenu_ViewFileInfo, "/rmc/resources/icons/Icon_viewFileInfo.png");
            // Disable online view item
            // When the right click pops up the menu bar and quickly click viewfileInfo.It's maybe have two threads download same file.
            // We will getFingerprint after download partial file, if the another thread download file,the original partial file will be delete.
            // Handle may be invalidated.So should disable this item when file is online, enable item after download.
            if (selected.Location == EnumFileLocation.Online)
            {
                item_viewFileInfo.IsEnabled = false;
                ChangeMenuItemIcon(item_viewFileInfo, "/rmc/resources/icons/Icon_viewFileInfo_gray.ico");
            }
            item_viewFileInfo.Command = viewModel.ContextMenuCommand;
            item_viewFileInfo.CommandParameter = new ContextMenuCmdArgs(selected, Constant.CONTEXT_MENU_CMD_VIEW_FILE_INFO);
            contextMenu.Items.Add(item_viewFileInfo);
        }

        private void MenuItemRemove(INxlFile selected, ref ContextMenu contextMenu)
        {
            if (selected.FileStatus == EnumNxlFileStatus.WaitingUpload || selected.FileStatus == EnumNxlFileStatus.CachedFile)
            {
                MenuItem item_remove = CreaeteMenuItem(CultureStringInfo.MainWin_ContextMenu_Remove, "/rmc/resources/icons/Icon_remove.png");
                item_remove.Command = viewModel.ContextMenuCommand;
                item_remove.CommandParameter = new ContextMenuCmdArgs(selected, Constant.CONTEXT_MENU_CMD_REMOVE);
                contextMenu.Items.Add(item_remove);
                contextMenu.Items.Add(new Separator());
            }
        }

        private void MenuItemOpenSky(INxlFile selected, ref ContextMenu contextMenu)
        {
            MenuItem item_openSkyDRM = CreaeteMenuItem(CultureStringInfo.MainWin_ContextMenu_OpenWeb, "/rmc/resources/icons/Icon_openSkyDrm.png");
            item_openSkyDRM.IsEnabled = viewModel.IsNetworkAvailable;
            if (!item_openSkyDRM.IsEnabled)
            {
                ChangeMenuItemIcon(item_openSkyDRM, "/rmc/resources/icons/Icon_openSkyDrm_gray.ico");
            }
            item_openSkyDRM.Command = viewModel.ContextMenuCommand;
            item_openSkyDRM.CommandParameter = new ContextMenuCmdArgs(selected, Constant.CONTEXT_MENU_CMD_OPEN_SKYDRM);
            contextMenu.Items.Add(item_openSkyDRM);
        }
        #endregion

        #region Handle MenuItem for listView contextmenu
        private void HandleAllMenuItem(INxlFile selected, MenuItem item_saveAs, 
            MenuItem item_extract_content, MenuItem item_modifyRights,
            MenuItem item_shareToPerson, MenuItem item_shareToProject, 
            MenuItem item_view, MenuItem item_viewFileInfo)
        {
            if (selected.FileStatus == EnumNxlFileStatus.UploadFailed)
            {
                return;
            }

            // For waitingUpload file, we special handling.
            if (selected.FileStatus == EnumNxlFileStatus.WaitingUpload)
            {
                HandleMnImExtract_WaitUpFile(selected, item_extract_content);
            }
            else
            {
                // MenuitemIsEnable
                if (viewModel.IsNetworkAvailable) // online mode
                {
                    ///
                    /// For project file, which support Edit and Modify Rights feature, So need to handle it specially.
                    ///
                    if (selected.FileRepo == EnumFileRepo.REPO_PROJECT && selected.Location == EnumFileLocation.Local)
                    {
                        HandleMenuItems_ProjLocalFile(item_saveAs, item_extract_content, item_modifyRights, 
                            item_shareToPerson, item_shareToProject, item_view, item_viewFileInfo);

                        return;
                    }
                    ///
                    /// Handle other repo files.
                    ///
                    HandleMenuItems(item_saveAs, item_extract_content, item_modifyRights,
                            item_shareToPerson, item_shareToProject, item_view, item_viewFileInfo);

                }
                else // offline mode 
                {
                    HandleMenuItemsForOfflineMode(selected, item_saveAs, item_extract_content, item_modifyRights,
                            item_shareToPerson, item_shareToProject, item_view, item_viewFileInfo);
                }
            }
        }

        private void HandleMnImExtract_WaitUpFile(INxlFile selected, MenuItem item_extract_content)
        {
            try
            {
                var fp = SkydrmLocalApp.Singleton.Rmsdk.User.GetNxlFileFingerPrint(selected.LocalPath);

                //Extract Contents
                item_extract_content.IsEnabled = fp.HasRight(sdk.FileRights.RIGHT_DECRYPT) && !fp.isFromMyVault;

                if (item_extract_content.IsEnabled)
                {
                    ChangeMenuItemIcon(item_extract_content, "/rmc/resources/icons/Icon_menu_extract2.ico");
                }
            }
            catch (Exception e)
            {
                SkydrmLocalApp.Singleton.Log.Error("Error:In MenuItemIsEnable", e);
            }
        }

        private void HandleMenuItems_ProjLocalFile(MenuItem item_saveAs,
            MenuItem item_extract_content, MenuItem item_modifyRights,
            MenuItem item_shareToPerson, MenuItem item_shareToProject,
            MenuItem item_view, MenuItem item_viewFileInfo)
        {
            // Fisrtly, disable ViewFile & ViewFileInfo it before check.
            item_view.IsEnabled = false;
            ChangeMenuItemIcon(item_view, "/rmc/resources/icons/Icon_viewFile_gray.ico");
            item_viewFileInfo.IsEnabled = false;
            ChangeMenuItemIcon(item_viewFileInfo, "/rmc/resources/icons/Icon_viewFileInfo_gray.ico");

            // download partial file.
            viewModel.PartialDownloadEx((bool result, INxlFile nxlFile) =>
            {
                try
                {
                    if (result && !string.IsNullOrEmpty(nxlFile.PartialLocalPath))
                    {
                        // Enable view file info.
                        this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
                        {
                            item_view.IsEnabled = true;
                            ChangeMenuItemIcon(item_view, "/rmc/resources/icons/Icon_viewFile.png");

                            item_viewFileInfo.IsEnabled = true;
                            ChangeMenuItemIcon(item_viewFileInfo, "/rmc/resources/icons/Icon_viewFileInfo.png");
                        }));

                        var fp = SkydrmLocalApp.Singleton.Rmsdk.User.GetNxlFileFingerPrint(nxlFile.PartialLocalPath);
                        this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
                        {
                            HandleEnableForOnlineMode(item_saveAs, item_shareToPerson, item_shareToProject,
                                item_extract_content, item_modifyRights, nxlFile, fp);

                        }));
                    }
                }
                catch (Exception e)
                {
                    SkydrmLocalApp.Singleton.Log.Error("Can not get file fingerprint when PopupContextMenu try get item rights.", e);
                }
            });
        }
        private void HandleEnableForOnlineMode(MenuItem item_saveAs,
            MenuItem item_shareToPerson,
            MenuItem item_shareToProject,
            MenuItem item_extract_content,
            MenuItem item_modifyRights,
            INxlFile nxlFile,
            NxlFileFingerPrint fp)
        {
            // save as
            item_saveAs.IsEnabled =
            !nxlFile.IsEdit/* fix bug 53826 */
            && (fp.HasRight(sdk.FileRights.RIGHT_SAVEAS) || fp.HasRight(sdk.FileRights.RIGHT_DOWNLOAD));
            if (item_saveAs.IsEnabled)
            {
                ChangeMenuItemIcon(item_saveAs, "/rmc/resources/icons/Icon_SaveAs.png");
            }

            // share
            Console.WriteLine("***********************Set Item_Share IsEnable*********************");
            if (nxlFile.FileRepo == EnumFileRepo.REPO_MYVAULT)
            {
                item_shareToPerson.IsEnabled = fp.HasRight(sdk.FileRights.RIGHT_SHARE) && fp.isByAdHoc /*&& viewModel.GetNxlFileSharedByMeteData(nxlFile)*/;
            }
            else
            {
                item_shareToPerson.IsEnabled = fp.HasRight(sdk.FileRights.RIGHT_SHARE) && fp.isByAdHoc;
            }

            // Share to person
            if (item_shareToPerson.IsEnabled)
            {
                // add restriction
                if (nxlFile.IsEdit)
                {
                    item_shareToPerson.IsEnabled = false;
                }
                else
                {
                    ChangeMenuItemIcon(item_shareToPerson, "/rmc/resources/icons/Icon_menu_share.png");
                }
            }

            // Share to project  // fix bug 56940 add to project based on extract right, not share right
            item_shareToProject.IsEnabled = fp.HasRight(sdk.FileRights.RIGHT_DECRYPT) && fp.isByCentrolPolicy;
            if (item_shareToProject.IsEnabled)
            {
                // add restriction
                if (nxlFile.IsEdit)
                {
                    item_shareToProject.IsEnabled = false;
                }
                else
                {
                    ChangeMenuItemIcon(item_shareToProject, "/rmc/resources/icons/Icon_menu_addfile.png");
                }
            }

            // Extract Contents
            item_extract_content.IsEnabled = fp.HasRight(sdk.FileRights.RIGHT_DECRYPT) && !fp.isFromMyVault;
            if (item_extract_content.IsEnabled)
            {
                // add restriction
                if (nxlFile.IsEdit)
                {
                    item_extract_content.IsEnabled = false;
                }
                else
                {
                    ChangeMenuItemIcon(item_extract_content, "/rmc/resources/icons/Icon_menu_extract2.ico");
                }
            }

            // modify rights
            if (nxlFile.FileRepo == EnumFileRepo.REPO_PROJECT)
            {
                if (nxlFile.Location == EnumFileLocation.Local)
                {
                    // In project offline file get partiallocalPath is actually LocalPath(special hinding)
                    item_modifyRights.IsEnabled = !ViewerProcess.ContainsKey(nxlFile.PartialLocalPath) && fp.hasAdminRights && fp.isByCentrolPolicy;
                }
                else if (nxlFile.Location == EnumFileLocation.Online)
                {
                    item_modifyRights.IsEnabled = !ViewerProcess.ContainsKey(nxlFile.RmsRemotePath) && fp.hasAdminRights && fp.isByCentrolPolicy;
                }
                if (item_modifyRights.IsEnabled)
                {
                    // add restriction
                    if (nxlFile.IsEdit)
                    {
                        item_modifyRights.IsEnabled = false;
                    }
                    else
                    {
                        ChangeMenuItemIcon(item_modifyRights, "/rmc/resources/icons/Icon_menu_modifyrights.png");
                    }
                }
            }
        }

        private void HandleMenuItems(MenuItem item_saveAs,
           MenuItem item_extract_content, MenuItem item_modifyRights,
           MenuItem item_shareToPerson, MenuItem item_shareToProject,
           MenuItem item_view, MenuItem item_viewFileInfo)
        {
            viewModel.PartialDownlaod((INxlFile nxlFile) =>
            {
                // In project offline file get partiallocalPath is actually LocalPath(special hinding)
                if (nxlFile != null && !string.IsNullOrEmpty(nxlFile.PartialLocalPath))
                {
                    try
                    {
                        // The getFingerPrint maybe throw exception,set item_viewFileInfo before getFingerPrint.
                        this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate () {
                            // View fileInfo
                            item_viewFileInfo.IsEnabled = true;
                            ChangeMenuItemIcon(item_viewFileInfo, "/rmc/resources/icons/Icon_viewFileInfo.png");
                        }));

                        var fp = SkydrmLocalApp.Singleton.Rmsdk.User.GetNxlFileFingerPrint(nxlFile.PartialLocalPath);
                        // Using "BeginInvoke" in order to avoid that right click menu ui block when another file is exporting. 
                        this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
                        {
                            HandleEnableForOnlineMode(item_saveAs, item_shareToPerson, item_shareToProject, item_extract_content, item_modifyRights, nxlFile, fp);

                        }));
                    }
                    catch (Exception e)
                    {
                        SkydrmLocalApp.Singleton.Log.Error("Error:In MenuItemIsEnable", e);
                    }
                }
            });
        }

        private void HandleMenuItemsForOfflineMode(INxlFile selected, MenuItem item_saveAs,
           MenuItem item_extract_content, MenuItem item_modifyRights,
           MenuItem item_shareToPerson, MenuItem item_shareToProject,
           MenuItem item_view, MenuItem item_viewFileInfo)
        {
            try
            {
                if (selected.Location == EnumFileLocation.Online)
                {
                    return;
                }

                var fp = SkydrmLocalApp.Singleton.Rmsdk.User.GetNxlFileFingerPrint(selected.LocalPath);
                // Using "BeginInvoke" in order to avoid that right click menu ui block when another file is exporting. 
                this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
                {
                    //Extract Contents
                    item_extract_content.IsEnabled = fp.HasRight(sdk.FileRights.RIGHT_DECRYPT) && !fp.isFromMyVault;
                    if (item_extract_content.IsEnabled)
                    {
                        // add restriction
                        if (selected.IsEdit)
                        {
                            item_extract_content.IsEnabled = false;
                        }
                        else
                        {
                            ChangeMenuItemIcon(item_extract_content, "/rmc/resources/icons/Icon_menu_extract2.ico");
                        }
                    }

                    // Offline mode not support upate recipients
                    if (selected.FileRepo != EnumFileRepo.REPO_MYVAULT
                    && selected.FileRepo != EnumFileRepo.REPO_SHARED_WITH_ME)
                    {
                        // share
                        Console.WriteLine("***********************Set Item_Share IsEnable*********************");
                        item_shareToPerson.IsEnabled = fp.HasRight(sdk.FileRights.RIGHT_SHARE) && fp.isByAdHoc;
                        if (item_shareToPerson.IsEnabled)
                        {
                            // add restriction
                            if (selected.IsEdit)
                            {
                                item_shareToPerson.IsEnabled = false;
                            }
                            else
                            {
                                ChangeMenuItemIcon(item_shareToPerson, "/rmc/resources/icons/Icon_menu_share.png");
                            }
                        }
                        // fix bug 56940 add to project based on extract right, not share right
                        item_shareToProject.IsEnabled = fp.HasRight(sdk.FileRights.RIGHT_DECRYPT) && fp.isByCentrolPolicy;
                        if (item_shareToProject.IsEnabled)
                        {
                            // add restriction
                            if (selected.IsEdit)
                            {
                                item_shareToProject.IsEnabled = false;
                            }
                            else
                            {
                                ChangeMenuItemIcon(item_shareToProject, "/rmc/resources/icons/Icon_menu_addfile.png");
                            }
                        }

                    }
                }));
            }
            catch (Exception e)
            {
                SkydrmLocalApp.Singleton.Log.Error("Error:In MenuItemIsEnable", e);
            }
        }
        #endregion

        #endregion

        #region TreeView contextmenu
        private void TreeViewContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            Console.WriteLine("---TreeViewContextMenuOpening-----");

            if (viewModel.CurrentWorkingArea == EnumCurrentWorkingArea.MYVAULT
                || viewModel.CurrentWorkingArea == EnumCurrentWorkingArea.SHARED_WITH_ME
                || viewModel.CurrentWorkingArea == EnumCurrentWorkingArea.PROJECT
                || viewModel.CurrentWorkingArea == EnumCurrentWorkingArea.FILTERS_OFFLINE
                || viewModel.CurrentWorkingArea == EnumCurrentWorkingArea.FILTERS_OUTBOX)
            {
                e.Handled = true;
                return;
            }

            PopupTreeViewContextMenu();
        }

        private void PopupTreeViewContextMenu()
        {
            treeViewContextMenu.Items.Clear();
            // Add a file
            MenuItem item_AddFile = CreaeteMenuItem(CultureStringInfo.MainWin_ContextMenu_AddFile, "/rmc/resources/icons/Icon_menu_addfile.png");
            item_AddFile.Command = viewModel.ContextMenuCommand;
            item_AddFile.CommandParameter = new ContextMenuCmdArgs(viewModel.CurrentSelectedFile, Constant.CONTEXT_MENU_CMD_ADD_FILE);
            treeViewContextMenu.Items.Add(item_AddFile);
            treeViewContextMenu.Items.Add(new Separator());
            // Item OpenSkyDRM
            MenuItem item_openSkyDRM = CreaeteMenuItem(CultureStringInfo.MainWin_ContextMenu_OpenWeb, "/rmc/resources/icons/Icon_openSkyDrm.png");
            item_openSkyDRM.IsEnabled = viewModel.IsNetworkAvailable;
            if (!item_openSkyDRM.IsEnabled)
            {
                ChangeMenuItemIcon(item_openSkyDRM, "/rmc/resources/icons/Icon_openSkyDrm_gray.ico");
            }
            item_openSkyDRM.Command = viewModel.ContextMenuCommand;
            item_openSkyDRM.CommandParameter = new ContextMenuCmdArgs(viewModel.CurrentSelectedFile, Constant.CONTEXT_MENU_CMD_OPEN_SKYDRM);
            treeViewContextMenu.Items.Add(item_openSkyDRM);
        }
        #endregion

        private MenuItem CreaeteMenuItem(string header, string iconPath)
        {
            MenuItem item = new MenuItem();
            item.Header = header;
            item.Icon = new Image()
            {
                Source = new BitmapImage(new Uri(iconPath, UriKind.RelativeOrAbsolute)),
                //Stretch = Stretch.None,
                Width = Convert.ToDouble("15"),
                Height = Convert.ToDouble("15")
            };          
            return item;
        }
        private void ChangeMenuItemIcon(MenuItem item, string iconPath)
        {
            item.Icon = new Image()
            {
                Source = new BitmapImage(new Uri(iconPath, UriKind.RelativeOrAbsolute)),
                //Stretch = Stretch.None,
                Width = Convert.ToDouble("15"),
                Height = Convert.ToDouble("15")
            };
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
                if (isDoubleClickProjectList)
                {
                    this.Dispatcher.BeginInvoke((Action)delegate
                    {
                        Point offset = treeViewItem.TransformToAncestor(this.TreeView_Scroll).Transform(new Point(0, 0));
                        this.TreeView_Scroll.ScrollToVerticalOffset(offset.Y + this.TreeView_Scroll.VerticalOffset);
                    });
                }
                isDoubleClickProjectList = false;
            }
            catch (Exception e)
            {
                SkydrmLocalApp.Singleton.Log.Error("Exception in TreeViewItemSelected :" + e.Message, e);
            }
        }
    }
}
