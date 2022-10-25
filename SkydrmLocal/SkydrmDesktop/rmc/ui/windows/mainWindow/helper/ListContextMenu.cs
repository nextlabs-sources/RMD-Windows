using SkydrmDesktop;
using SkydrmDesktop.Resources.languages;
using SkydrmLocal.rmc.Edit;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.process;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SkydrmLocal.rmc.ui.windows.mainWindow.helper
{
    static class ListContextMenu
    {
        private static readonly view.MainWindow mainWindow = SkydrmApp.Singleton.MainWin;
        private static readonly viewModel.ViewModelMainWindow viewModel = SkydrmApp.Singleton.MainWin.viewModel;

        public static void PopupTreeViewContextMenu(EnumCurrentWorkingArea currentWorkingArea, ContextMenu treeViewContextMenu)
        {
            treeViewContextMenu.Items.Clear();
            if (currentWorkingArea == EnumCurrentWorkingArea.PROJECT_ROOT)
            {
                // Add a file
                //MenuItem item_AddFile = CreaeteMenuItem(CultureStringInfo.ApplicationFindResource("MainWin_Tree_ContextMenu_AddFile"), "/rmc/resources/icons/Icon_menu_addfile.png");
                //item_AddFile.Command = viewModel.ContextMenuCommand;
                //item_AddFile.CommandParameter = new ContextMenuCmdArgs(null, Constant.CONTEXT_MENU_CMD_TREE_ADD_FILE);
                //treeViewContextMenu.Items.Add(item_AddFile);
                //treeViewContextMenu.Items.Add(new Separator());
            }

            if (currentWorkingArea == EnumCurrentWorkingArea.EXTERNAL_REPO)
            {
                // 2020.10 release not support add external repo

                //// Add repo
                //MenuItem item_AddRepo = CreaeteMenuItem(CultureStringInfo.ApplicationFindResource("MainWin_Tree_ContextMenu_AddRepo"), "/rmc/resources/icons/Icon_menu_addrepo.png");
                //item_AddRepo.IsEnabled = viewModel.IsNetworkAvailable;
                //if (!item_AddRepo.IsEnabled)
                //{
                //    // need provide gray icon
                //    ChangeMenuItemIcon(item_AddRepo, "/rmc/resources/icons/Icon_menu_addrepo.png");
                //}
                //item_AddRepo.Command = viewModel.ContextMenuCommand;
                //item_AddRepo.CommandParameter = new ContextMenuCmdArgs(null, Constant.CONTEXT_MENU_CMD_TREE_ADD_REPO);
                //treeViewContextMenu.Items.Add(item_AddRepo);
                //treeViewContextMenu.Items.Add(new Separator());
            }

            // Item OpenSkyDRM
            MenuItem item_openSkyDRM = CreaeteMenuItem(CultureStringInfo.ApplicationFindResource("MainWin_ContextMenu_OpenWeb"), "/rmc/resources/icons/Icon_openSkyDrm.png");
            item_openSkyDRM.IsEnabled = viewModel.IsNetworkAvailable;
            if (!item_openSkyDRM.IsEnabled)
            {
                ChangeMenuItemIcon(item_openSkyDRM, "/rmc/resources/icons/Icon_openSkyDrm_gray.ico");
            }
            item_openSkyDRM.Command = viewModel.ContextMenuCommand;
            item_openSkyDRM.CommandParameter = new ContextMenuCmdArgs(viewModel.CurrentSelectedFile, Constant.CONTEXT_MENU_CMD_OPEN_SKYDRM);
            treeViewContextMenu.Items.Add(item_openSkyDRM);
        }

        public static void PopupContextMenu(INxlFile selected, ContextMenu contextMenu)
        {
            contextMenu.Items.Clear();

            switch (selected.FileRepo)
            {
                case EnumFileRepo.UNKNOWN:
                    break;
                case EnumFileRepo.EXTERN:
                    break;
                case EnumFileRepo.REPO_MYVAULT:
                    HandMyVaultMenuItems(selected, ref contextMenu);
                    break;
                case EnumFileRepo.REPO_PROJECT:
                    HandProjectMenuItems(selected, ref contextMenu);
                    // PM required hide project share transaction
                    MenuItem item_share = null;
                    foreach (var item in contextMenu.Items)
                    {
                        if (item is MenuItem)
                        {
                            MenuItem menuItem = item as MenuItem;
                            if (menuItem.Header.ToString().Equals(CultureStringInfo.ApplicationFindResource("MainWin_ContextMenu_Share")))
                            {
                                item_share = menuItem;
                            }
                        }
                    }
                    contextMenu.Items.Remove(item_share);
                    break;
                case EnumFileRepo.REPO_SHARED_WITH_ME:
                    HandShareWithMeMenuItems(selected, ref contextMenu);
                    break;
                case EnumFileRepo.REPO_WORKSPACE:
                    HandProjectMenuItems(selected, ref contextMenu);
                    MenuItem item_share2 = null;
                    foreach (var item in contextMenu.Items)
                    {
                        if (item is MenuItem)
                        {
                            MenuItem menuItem = item as MenuItem;
                            if (menuItem.Header.ToString().Equals(CultureStringInfo.ApplicationFindResource("MainWin_ContextMenu_Share")))
                            {
                                item_share2= menuItem;
                            }
                        }
                    }
                    contextMenu.Items.Remove(item_share2);
                    break;
                case EnumFileRepo.REPO_MYDRIVE:
                    HandMyDriveMenuItems(selected, ref contextMenu);
                    break;
                case EnumFileRepo.REPO_EXTERNAL_DRIVE:
                    if (selected.IsNxlFile)
                    {
                        HandProjectMenuItems(selected, ref contextMenu);
                        MenuItem item_share3 = null;
                        // 2020.10 release not support modify rights
                        MenuItem item_modifyRights = null;
                        Separator item_Separator = null;
                        foreach (var item in contextMenu.Items)
                        {
                            if (item is MenuItem)
                            {
                                MenuItem menuItem = item as MenuItem;
                                if (menuItem.Header.ToString().Equals(CultureStringInfo.ApplicationFindResource("MainWin_ContextMenu_Share")))
                                {
                                    item_share3 = menuItem;
                                }
                                if (menuItem.Header.ToString().Equals(CultureStringInfo.ApplicationFindResource("MainWin_ContextMenu_ModifyRights")))
                                {
                                    item_modifyRights = menuItem;
                                }
                            }
                            if (item is Separator)
                            {
                                Separator separator = item as Separator;
                                if (separator.Tag != null && separator.Tag.ToString().Equals("ModifyRightsSeparator"))
                                {
                                    item_Separator = separator;
                                }
                            }
                        }
                        contextMenu.Items.Remove(item_share3);
                        contextMenu.Items.Remove(item_modifyRights);
                        contextMenu.Items.Remove(item_Separator);
                    }
                    else
                    {
                        HandMyDriveMenuItems(selected, ref contextMenu);
                        if (selected.FileStatus != EnumNxlFileStatus.UploadFailed)
                        {
                            MenuItem item_delete = null;
                            MenuItem item_share4 = null;
                            Separator item_Separator = null;
                            foreach (var item in contextMenu.Items)
                            {
                                if (item is MenuItem)
                                {
                                    MenuItem menuItem = item as MenuItem;
                                    if (menuItem.Header.ToString().Equals(CultureStringInfo.ApplicationFindResource("MainWin_ContextMenu_Remove")))
                                    {
                                        item_delete = menuItem;
                                    }
                                    if (menuItem.Header.ToString().Equals(CultureStringInfo.ApplicationFindResource("MainWin_ContextMenu_Share")))
                                    {
                                        item_share4 = menuItem;
                                    }
                                }
                                if (item is Separator)
                                {
                                    Separator separator = item as Separator;
                                    if (separator.Tag != null && separator.Tag.ToString().Equals("RemoveSeparator"))
                                    {
                                        item_Separator = separator;
                                    }
                                }
                            }
                            contextMenu.Items.Remove(item_delete);
                            contextMenu.Items.Remove(item_Separator);
                            contextMenu.Items.Remove(item_share4);
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        private static void HandMyDriveMenuItems(INxlFile selected, ref ContextMenu contextMenu)
        {
            if (selected.FileStatus == EnumNxlFileStatus.UploadFailed)
            {
                if (selected is PendingUploadFile)
                {
                    PendingUploadFile uploadFile = selected as PendingUploadFile;

                    if (uploadFile.Raw.IsExistInRemote)
                    {
                        MenuItemOverWriteUpload(uploadFile, ref contextMenu);
                        MenuItemRenameUpload(uploadFile, ref contextMenu);
                    }
                    else
                    {
                        MenuItemUpload(selected, ref contextMenu);
                    }
                }

                contextMenu.Items.Add(new Separator());

                MenuItemRemove(selected, out MenuItem item_remove, ref contextMenu);
                return;
            }

            MenuItemProtect(selected, out MenuItem item_protect, ref contextMenu);
            MenuItemShare(selected, out MenuItem item_share, ref contextMenu);
            MenuItemMarkAndUnMark_Offline(selected, ref contextMenu);
            contextMenu.Items.Add(new Separator());
            MenuItemView(selected, out MenuItem item_view, ref contextMenu);
            contextMenu.Items.Add(new Separator());
            MenuItemRemove(selected, out MenuItem item_remove2, ref contextMenu);
            contextMenu.Items.Add(new Separator() { Tag = "RemoveSeparator" });
            MenuItemOpenSky(selected, ref contextMenu);

            if (viewModel.IsNetworkAvailable)
            {
                // user can download file do share
                item_share.IsEnabled = true;
                ChangeMenuItemIcon(item_share, "/rmc/resources/icons/Icon_menu_share.png");
            }
            else // network outage
            {
                item_remove2.IsEnabled = false;
                ChangeMenuItemIcon(item_remove2, "/rmc/resources/icons/Icon_remove_gray.ico");

                if (selected.Location == EnumFileLocation.Online)
                {
                    return;
                }
                // offline file
                item_share.IsEnabled = true;
                ChangeMenuItemIcon(item_share, "/rmc/resources/icons/Icon_menu_share.png");
            }
        }

        private static void HandMyVaultMenuItems(INxlFile selected, ref ContextMenu contextMenu)
        {
            // add menuItem
            if (selected.FileStatus == EnumNxlFileStatus.UploadFailed)
            {
                if (selected is PendingUploadFile)
                {
                    PendingUploadFile uploadFile = selected as PendingUploadFile;

                    if (uploadFile.Raw.IsExistInRemote)
                    {
                        MenuItemOverWriteUpload(uploadFile, ref contextMenu);
                        MenuItemRenameUpload(uploadFile, ref contextMenu);
                    }
                    else
                    {
                        MenuItemUpload(selected, ref contextMenu);
                    }
                }

                contextMenu.Items.Add(new Separator());

                MenuItemRemove(selected, out MenuItem item_remove, ref contextMenu);
                return;
            }

            MenuItem item_view;
            MenuItem item_viewFileInfo;
            MenuItem item_share = null;
            MenuItem item_addFile = null;
            MenuItem item_saveAs = null;

            MenuItemView(selected, out item_view, ref contextMenu);
            MenuItemViewFileInfo(selected, out item_viewFileInfo, ref contextMenu);

            if (selected.FileStatus != EnumNxlFileStatus.WaitingUpload)
            {
                MenuItemShare(selected, out item_share, ref contextMenu);
                MenuItemAddFile(selected, out item_addFile, ref contextMenu);
                MenuItemSaveAs(selected, out item_saveAs, ref contextMenu);
                MenuItemMarkAndUnMark_Offline(selected, ref contextMenu);
                contextMenu.Items.Add(new Separator());
            }

            if (selected.FileStatus == EnumNxlFileStatus.WaitingUpload)
            {
                contextMenu.Items.Add(new Separator());
                MenuItemRemove(selected, out MenuItem item_remove, ref contextMenu);
                contextMenu.Items.Add(new Separator());
            }

            MenuItemOpenSky(selected, ref contextMenu);

            // change menuItem isEnable
            if (selected.FileStatus == EnumNxlFileStatus.WaitingUpload)
            {
                return;
            }

            if (viewModel.IsNetworkAvailable)
            {
                // Fisrtly, disable ViewFile & ViewFileInfo it before check.
                item_view.IsEnabled = false;
                ChangeMenuItemIcon(item_view, "/rmc/resources/icons/Icon_viewFile_gray.ico");

                viewModel.PartialDownloadEx((bool result, INxlFile nxlFile) =>
                {
                    if (result && !string.IsNullOrEmpty(nxlFile.PartialLocalPath))
                    {
                        try
                        {
                            // The getFingerPrint maybe throw exception,set item_viewFileInfo before getFingerPrint.
                            mainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
                            {

                                item_view.IsEnabled = true;
                                ChangeMenuItemIcon(item_view, "/rmc/resources/icons/Icon_viewFile.png");

                                // View fileInfo
                                item_viewFileInfo.IsEnabled = true;
                                ChangeMenuItemIcon(item_viewFileInfo, "/rmc/resources/icons/Icon_viewFileInfo.png");
                            }));

                            var fp = SkydrmApp.Singleton.Rmsdk.User.GetNxlFileFingerPrint(nxlFile.PartialLocalPath);
                            // Using "BeginInvoke" in order to avoid that right click menu ui block when another file is exporting. 
                            mainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
                            {
                                // save as, file saveAs right will be replaced by Download right, file should not have saveAs right 
                                item_saveAs.IsEnabled = fp.HasRight(sdk.FileRights.RIGHT_DOWNLOAD) || fp.HasRight(sdk.FileRights.RIGHT_SAVEAS);
                                if (item_saveAs.IsEnabled)
                                {
                                    ChangeMenuItemIcon(item_saveAs, "/rmc/resources/icons/Icon_SaveAs.png");
                                }

                                // share
                                item_share.IsEnabled = fp.HasRight(sdk.FileRights.RIGHT_SHARE);

                                // Share to person
                                if (item_share.IsEnabled)
                                {
                                    ChangeMenuItemIcon(item_share, "/rmc/resources/icons/Icon_menu_share.png");
                                }

                                // add file to, because user is owner for myvault file
                                item_addFile.IsEnabled = true;

                                if (item_addFile.IsEnabled)
                                {
                                    ChangeMenuItemIcon(item_addFile, "/rmc/resources/icons/Icon_menu_addfile.png");
                                }

                            }));
                        }
                        catch (Exception e)
                        {
                            SkydrmApp.Singleton.Log.Error("Error:In MenuItemIsEnable", e);
                        }
                    }
                });
            }
            else
            {
                if (selected.Location == EnumFileLocation.Online)
                {
                    return;
                }
                // add file to, because user is owner for myvault file
                item_addFile.IsEnabled = true;

                if (item_addFile.IsEnabled)
                {
                    ChangeMenuItemIcon(item_addFile, "/rmc/resources/icons/Icon_menu_addfile.png");
                }
            }
        }

        private static void HandShareWithMeMenuItems(INxlFile selected, ref ContextMenu contextMenu)
        {
            // add menuItem
            MenuItemView(selected, out MenuItem item_view, ref contextMenu);
            MenuItemViewFileInfo(selected, out MenuItem item_viewFileInfo, ref contextMenu);
            MenuItemShare(selected, out MenuItem item_share, ref contextMenu);
            MenuItemAddFile(selected, out MenuItem item_addFile, ref contextMenu);
            MenuItemSaveAs(selected, out MenuItem item_saveAs, ref contextMenu);
            MenuItemMarkAndUnMark_Offline(selected, ref contextMenu);
            contextMenu.Items.Add(new Separator());
            MenuItemOpenSky(selected, ref contextMenu);

            // change menuItem isEnable
            if (viewModel.IsNetworkAvailable)
            {
                // Fisrtly, disable ViewFile & ViewFileInfo it before check.
                item_view.IsEnabled = false;
                ChangeMenuItemIcon(item_view, "/rmc/resources/icons/Icon_viewFile_gray.ico");

                viewModel.PartialDownlaod((INxlFile nxlFile) =>
                {
                    if (nxlFile != null && !string.IsNullOrEmpty(nxlFile.PartialLocalPath))
                    {
                        try
                        {
                            // The getFingerPrint maybe throw exception,set item_viewFileInfo before getFingerPrint.
                            mainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
                            {
                                // View file
                                item_view.IsEnabled = true;
                                ChangeMenuItemIcon(item_view, "/rmc/resources/icons/Icon_viewFile.png");

                                // View fileInfo
                                item_viewFileInfo.IsEnabled = true;
                                ChangeMenuItemIcon(item_viewFileInfo, "/rmc/resources/icons/Icon_viewFileInfo.png");
                            }));

                            var fp = SkydrmApp.Singleton.Rmsdk.User.GetNxlFileFingerPrint(nxlFile.PartialLocalPath);
                            // Using "BeginInvoke" in order to avoid that right click menu ui block when another file is exporting. 
                            mainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
                            {
                                // save as, file saveAs right will be replaced by Download right, file should not have saveAs right 
                                item_saveAs.IsEnabled = fp.HasRight(sdk.FileRights.RIGHT_DOWNLOAD) || fp.HasRight(sdk.FileRights.RIGHT_SAVEAS);
                                if (item_saveAs.IsEnabled)
                                {
                                    ChangeMenuItemIcon(item_saveAs, "/rmc/resources/icons/Icon_SaveAs.png");
                                }

                                // share
                                item_share.IsEnabled = fp.HasRight(sdk.FileRights.RIGHT_SHARE);

                                // Share to person
                                if (item_share.IsEnabled)
                                {
                                    ChangeMenuItemIcon(item_share, "/rmc/resources/icons/Icon_menu_share.png");
                                }

                                //Add file to
                                if (fp.HasRight(sdk.FileRights.RIGHT_DECRYPT)
                                || fp.HasRight(sdk.FileRights.RIGHT_SAVEAS)
                                || fp.HasRight(sdk.FileRights.RIGHT_DOWNLOAD))
                                {
                                    item_addFile.IsEnabled = true;
                                }
                                if (item_addFile.IsEnabled)
                                {
                                    // add restriction
                                    if (nxlFile.IsEdit)
                                    {
                                        item_addFile.IsEnabled = false;
                                    }
                                    else
                                    {
                                        ChangeMenuItemIcon(item_addFile, "/rmc/resources/icons/Icon_menu_addfile.png");
                                    }
                                }

                            }));
                        }
                        catch (Exception e)
                        {
                            SkydrmApp.Singleton.Log.Error("Error:In MenuItemIsEnable", e);
                        }
                    }
                    else
                    {
                        mainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
                        {
                            // View file
                            item_view.IsEnabled = true;
                            ChangeMenuItemIcon(item_view, "/rmc/resources/icons/Icon_viewFile.png");

                            // View fileInfo
                            item_viewFileInfo.IsEnabled = true;
                            ChangeMenuItemIcon(item_viewFileInfo, "/rmc/resources/icons/Icon_viewFileInfo.png");
                        }));
                    }
                });
            }
            
            // not care network outage
        }

        private static void HandProjectMenuItems(INxlFile selected, ref ContextMenu contextMenu)
        {
            // add menuItem
            if (selected.FileStatus == EnumNxlFileStatus.UploadFailed)
            {
                if (selected is PendingUploadFile)
                {
                    PendingUploadFile uploadFile = selected as PendingUploadFile;

                    if (uploadFile.Raw.IsExistInRemote)
                    {
                        MenuItemOverWriteUpload(uploadFile, ref contextMenu);
                        MenuItemRenameUpload(uploadFile, ref contextMenu);
                    }
                    else
                    {
                        MenuItemUpload(selected, ref contextMenu);
                    }
                }

                contextMenu.Items.Add(new Separator());

                MenuItemRemove(selected, out MenuItem item_remove, ref contextMenu);
                return;
            }
            /*
             * View file
             * View file info
             * Share
             * Add file to
             * Save As
             * Extract
             * Make available offline
             * --------------------------
             * Modify permissions
             * --------------------------
             * Open SkyDRM Web
             */
            
            MenuItem item_view;
            MenuItem item_viewFileInfo;
            MenuItem item_share = null;
            MenuItem item_addFile = null;
            MenuItem item_saveAs = null;
            MenuItem item_extract_content;
            MenuItem item_modifyRights = null;

            MenuItemView(selected, out item_view, ref contextMenu);
            MenuItemViewFileInfo(selected, out item_viewFileInfo, ref contextMenu);

            if (selected.FileStatus != EnumNxlFileStatus.WaitingUpload)
            {
                MenuItemShare(selected, out item_share, ref contextMenu);
                MenuItemAddFile(selected, out item_addFile, ref contextMenu);
                MenuItemSaveAs(selected, out item_saveAs, ref contextMenu);
            }

            MenuItemExtract(selected, out item_extract_content, ref contextMenu);

            if (selected.FileStatus != EnumNxlFileStatus.WaitingUpload)
            {
                MenuItemMarkAndUnMark_Offline(selected, ref contextMenu);
                contextMenu.Items.Add(new Separator());
                MenuItemModifyRights(selected, out item_modifyRights, ref contextMenu);
                contextMenu.Items.Add(new Separator() { Tag = "ModifyRightsSeparator"});
            }

            if (selected.FileStatus == EnumNxlFileStatus.WaitingUpload)
            {
                contextMenu.Items.Add(new Separator());
                MenuItemRemove(selected, out MenuItem item_remove, ref contextMenu);
                contextMenu.Items.Add(new Separator());
            }

            MenuItemOpenSky(selected, ref contextMenu);


            // change menuItem isEnable
            if (selected.FileStatus == EnumNxlFileStatus.WaitingUpload)
            {
                item_extract_content.IsEnabled = selected.FileInfo.Rights.Contains(sdk.FileRights.RIGHT_DECRYPT);

                if (item_extract_content.IsEnabled)
                {
                    ChangeMenuItemIcon(item_extract_content, "/rmc/resources/icons/Icon_menu_extract2.ico");
                }
                return;
            }

            if (viewModel.IsNetworkAvailable)
            {
                // Fisrtly, disable ViewFile & ViewFileInfo it before check.
                item_view.IsEnabled = false;
                ChangeMenuItemIcon(item_view, "/rmc/resources/icons/Icon_viewFile_gray.ico");

                // download partial file.
                viewModel.PartialDownloadEx((bool result, INxlFile nxlFile) =>
                {
                    try
                    {
                        if (result && !string.IsNullOrEmpty(nxlFile.PartialLocalPath))
                        {
                            // Enable view file info.
                            mainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
                            {
                                item_view.IsEnabled = true;
                                ChangeMenuItemIcon(item_view, "/rmc/resources/icons/Icon_viewFile.png");

                                item_viewFileInfo.IsEnabled = true;
                                ChangeMenuItemIcon(item_viewFileInfo, "/rmc/resources/icons/Icon_viewFileInfo.png");
                            }));

                            var fp = SkydrmApp.Singleton.Rmsdk.User.GetNxlFileFingerPrint(nxlFile.PartialLocalPath);
                            mainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
                            {
                                // save as
                                item_saveAs.IsEnabled = !nxlFile.IsEdit/* fix bug 53826 */&& (fp.HasRight(sdk.FileRights.RIGHT_SAVEAS) 
                                || fp.HasRight(sdk.FileRights.RIGHT_DOWNLOAD) || (fp.hasAdminRights && fp.HasRight(sdk.FileRights.RIGHT_VIEW)));
                                if (item_saveAs.IsEnabled)
                                {
                                    ChangeMenuItemIcon(item_saveAs, "/rmc/resources/icons/Icon_SaveAs.png");
                                }

                                // share
                                item_share.IsEnabled = fp.hasAdminRights || fp.HasRight(sdk.FileRights.RIGHT_SHARE);

                                // Share to person
                                if (item_share.IsEnabled)
                                {
                                    // add restriction
                                    if (nxlFile.IsEdit)
                                    {
                                        item_share.IsEnabled = false;
                                    }
                                    else
                                    {
                                        ChangeMenuItemIcon(item_share, "/rmc/resources/icons/Icon_menu_share.png");
                                    }
                                }

                                // Share to project 
                                //item_shareToProject.IsEnabled = fp.isByCentrolPolicy && (fp.hasAdminRights || fp.HasRight(sdk.FileRights.RIGHT_DECRYPT));

                                // because WorkSpace file is also use this method to show ContextMenu, add another logic to judge 'add file to project' item IsEnable in below code 
                                //item_addFile.IsEnabled = fp.isFromSystemBucket /* WorkSpace file */
                                //                                                              || (fp.isFromPorject && (fp.hasAdminRights || fp.HasRight(sdk.FileRights.RIGHT_DECRYPT))) /* Project file */;

                                if (!fp.isFromSystemBucket && fp.isFromPorject)
                                {
                                    // this file is from project
                                    if (fp.hasAdminRights)
                                    {
                                        item_addFile.IsEnabled = true;
                                    }
                                    else
                                    {
                                        if (fp.HasRight(sdk.FileRights.RIGHT_DECRYPT))
                                        {
                                            item_addFile.IsEnabled = true;
                                        }
                                    }
                                }
                                else if (fp.isFromSystemBucket)
                                {
                                    // this file is from workspace
                                    if (fp.hasAdminRights)  // workSpace file has tenant admin users, only need view right
                                    {
                                        if (fp.HasRight(sdk.FileRights.RIGHT_VIEW))
                                        {
                                            item_addFile.IsEnabled = true;
                                        }
                                    }
                                    else
                                    {
                                        //For non-admin user, RIGHTS EXTRACT OR SAVEAS required to perform action.
                                        if (fp.HasRight(sdk.FileRights.RIGHT_DECRYPT)
                                        || fp.HasRight(sdk.FileRights.RIGHT_SAVEAS)
                                        || fp.HasRight(sdk.FileRights.RIGHT_DOWNLOAD))
                                        {
                                            item_addFile.IsEnabled = true;
                                        }
                                    }
                                }
                                else if (fp.isFromMyVault)
                                {
                                    if(fp.isOwner)
                                    {
                                        item_addFile.IsEnabled = true;
                                    }
                                }
                                else
                                {
                                    item_addFile.IsEnabled = false;
                                }

                                if (item_addFile.IsEnabled)
                                {
                                    // add restriction
                                    if (nxlFile.IsEdit)
                                    {
                                        item_addFile.IsEnabled = false;
                                    }
                                    else
                                    {
                                        ChangeMenuItemIcon(item_addFile, "/rmc/resources/icons/Icon_menu_addfile.png");
                                    }
                                }

                                // Extract Contents
                                item_extract_content.IsEnabled = fp.HasRight(sdk.FileRights.RIGHT_DECRYPT);
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
                                if (nxlFile.Location == EnumFileLocation.Local)
                                {
                                    // In project offline file get partiallocalPath is actually LocalPath(special hinding)
                                    item_modifyRights.IsEnabled = !ViewerProcess.ContainsKey(nxlFile.PartialLocalPath) && fp.hasAdminRights && fp.isByCentrolPolicy;
                                }
                                else if (nxlFile.Location == EnumFileLocation.Online)
                                {
                                    item_modifyRights.IsEnabled = !ViewerProcess.ContainsKey(nxlFile.PathId) && fp.hasAdminRights && fp.isByCentrolPolicy;
                                }

                                // fix Bug 61490 - [10.6]Modify rights buttons should be disabled on the shared with this project page for all users
                                if (nxlFile is fileSystem.project.ProjectRepo.ProjectSharedWithMeDoc)
                                {
                                    item_modifyRights.IsEnabled = false;
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
                            }));
                        }
                    }
                    catch (Exception e)
                    {
                        SkydrmApp.Singleton.Log.Error("Can not get file fingerprint when PopupContextMenu try get item rights.", e);
                    }
                });
            }
            else // network outage
            {
                try
                {
                    if (selected.Location == EnumFileLocation.Online)
                    {
                        return;
                    }

                    var fp = SkydrmApp.Singleton.Rmsdk.User.GetNxlFileFingerPrint(selected.LocalPath);
                    // Using "BeginInvoke" in order to avoid that right click menu ui block when another file is exporting. 
                    mainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate ()
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

                        // share
                        item_share.IsEnabled = false;
                        if (item_share.IsEnabled)
                        {
                            // add restriction
                            if (selected.IsEdit)
                            {
                                item_share.IsEnabled = false;
                            }
                            else
                            {
                                ChangeMenuItemIcon(item_share, "/rmc/resources/icons/Icon_menu_share.png");
                            }
                        }

                        // Share to project 
                        //item_addFile.IsEnabled = fp.isFromSystemBucket /* WorkSpace file */
                        //                                                              || (fp.isFromPorject && (fp.hasAdminRights || fp.HasRight(sdk.FileRights.RIGHT_DECRYPT))) /* Project file */;

                        if (!fp.isFromSystemBucket && fp.isFromPorject)
                        {
                            // this file is from project
                            if (fp.hasAdminRights || fp.HasRight(sdk.FileRights.RIGHT_DECRYPT))
                            {
                                item_addFile.IsEnabled = true;
                            }
                        }
                        else if (fp.isFromSystemBucket)
                        {
                            // this file is from workspace
                            if (fp.hasAdminRights)  // workSpace file has tenant admin users, only need view right
                            {
                                if (fp.HasRight(sdk.FileRights.RIGHT_VIEW))
                                {
                                    item_addFile.IsEnabled = true;
                                }
                            }
                            else
                            {
                                if (fp.HasRight(sdk.FileRights.RIGHT_DOWNLOAD))
                                {
                                    item_addFile.IsEnabled = true;
                                }
                            }
                        }
                        else
                        {
                            item_addFile.IsEnabled = false;
                        }

                        if (item_addFile.IsEnabled)
                        {
                            // add restriction
                            if (selected.IsEdit)
                            {
                                item_addFile.IsEnabled = false;
                            }
                            else
                            {
                                ChangeMenuItemIcon(item_addFile, "/rmc/resources/icons/Icon_menu_addfile.png");
                            }
                        }
                    }));
                }
                catch (Exception e)
                {
                    SkydrmApp.Singleton.Log.Error("Error:In MenuItemIsEnable", e);
                }
            }
        }


        #region Add menuItem for listView contextmenu. if item IsEnable need to be determined by file rights or other time-consuming operations, will defult set false.

        private static void MenuItemUpload(INxlFile selected, ref ContextMenu contextMenu)
        {
            MenuItem item_upload = CreaeteMenuItem(CultureStringInfo.ApplicationFindResource("MainWin_ContextMenu_Upload"), "/rmc/resources/icons/Icon_menu_upload.ico");
            item_upload.Command = viewModel.ContextMenuCommand;
            item_upload.CommandParameter = new ContextMenuCmdArgs(selected, Constant.CONTEXT_MENU_CMD_UPLOAD);
            contextMenu.Items.Add(item_upload);
        }

        private static void MenuItemRenameUpload(INxlFile selected, ref ContextMenu contextMenu)
        {
            MenuItem item_renameUpload = CreaeteMenuItem(CultureStringInfo.ApplicationFindResource("MainWin_ContextMenu_RenameUpload"), "/rmc/resources/icons/Icon_menu_rename.ico");
            item_renameUpload.Command = viewModel.ContextMenuCommand;
            item_renameUpload.CommandParameter = new ContextMenuCmdArgs(selected, Constant.CONTEXT_MENU_CMD_RENAME_UPLOAD);
            contextMenu.Items.Add(item_renameUpload);
        }

        private static void MenuItemOverWriteUpload(INxlFile selected, ref ContextMenu contextMenu)
        {
            MenuItem item_overwriteUpload = CreaeteMenuItem(CultureStringInfo.ApplicationFindResource("MainWin_ContextMenu_OverWriteUpload"), "/rmc/resources/icons/Icon_menu_overwrite.ico");
            item_overwriteUpload.Command = viewModel.ContextMenuCommand;
            item_overwriteUpload.CommandParameter = new ContextMenuCmdArgs(selected, Constant.CONTEXT_MENU_CMD_OVERWRITE_UPLOAD);
            contextMenu.Items.Add(item_overwriteUpload);
        }

        private static void MenuItemMarkAndUnMark_Offline(INxlFile selected, ref ContextMenu contextMenu)
        {
            // mark unmark
            if (selected.Location == EnumFileLocation.Online)
            {
                MenuItem item_makeOffline = CreaeteMenuItem(CultureStringInfo.ApplicationFindResource("MainWin_ContextMenu_Mark"), "/rmc/resources/icons/Icon_menu_offline.png");

                // check network status -- if net is offline, we disable it; and if the file is openning, also disable it -- fix bug 52924
                item_makeOffline.IsEnabled = viewModel.IsNetworkAvailable && !ViewerProcess.ContainsKey(selected.PathId);

                // judge the file format, if not support, disable it 
                FileTypeHelper.EnumFileType type = FileTypeHelper.GetFileTypeByExtension(selected.Name);
                //if (type == FileTypeHelper.EnumFileType.FILE_TYPE_NOT_SUPPORT)
                //{
                //    item_makeOffline.IsEnabled = false;
                //}

                // if item isEnable is false, should replace gray icon.
                if (!item_makeOffline.IsEnabled)
                {
                    ChangeMenuItemIcon(item_makeOffline, "/rmc/resources/icons/Icon_menu_offline_gray.ico");
                }

                item_makeOffline.Command = viewModel.ContextMenuCommand;
                item_makeOffline.CommandParameter = new ContextMenuCmdArgs(item_makeOffline, selected, Constant.CONTEXT_MENU_CMD_MAKE_OFFLINE);
                contextMenu.Items.Add(item_makeOffline);
            }
            //else if (selected.FileStatus == EnumNxlFileStatus.AvailableOffline) // AvailableOffline file is unMark item, CachedFile is Remove item. so can't use location to judge
            else // fix bug 65124 Use unMark to remove CachedFile, so can use location to judge
            {
                // Item unmark offline
                MenuItem item_unmarkOffline = CreaeteMenuItem(CultureStringInfo.ApplicationFindResource("MainWin_ContextMenu_UnMark"), "/rmc/resources/icons/Icon_menu_offline.png");
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

        private static void MenuItemSaveAs(INxlFile selected, out MenuItem item_saveAs, ref ContextMenu contextMenu)
        {
            item_saveAs = CreaeteMenuItem(CultureStringInfo.ApplicationFindResource("MainWin_ContextMenu_SaveAs"), "/rmc/resources/icons/Icon_SaveAs_gray.ico");
            item_saveAs.Command = viewModel.ContextMenuCommand;
            item_saveAs.CommandParameter = new ContextMenuCmdArgs(selected, Constant.CONTEXT_MENU_CMD_SAVE_AS);
            // default is false, then to judge if has Save As rights.
            item_saveAs.IsEnabled = false;
            contextMenu.Items.Add(item_saveAs);
        }

        private static void MenuItemExtract(INxlFile selected, out MenuItem item_extract_content, ref ContextMenu contextMenu)
        {
            item_extract_content = CreaeteMenuItem(CultureStringInfo.ApplicationFindResource("MainWin_ContextMenu_ExtractContent"), "/rmc/resources/icons/Icon_menu_extract_gray2.ico");
            
            item_extract_content.IsEnabled = false;

            item_extract_content.Command = viewModel.ContextMenuCommand;
            item_extract_content.CommandParameter = new ContextMenuCmdArgs(item_extract_content, selected, Constant.CONTEXT_MENU_CMD_EXTRACT_CONTENT);
            contextMenu.Items.Add(item_extract_content);
        }

        private static void MenuItemModifyRights(INxlFile selected, out MenuItem item_modifyRights, ref ContextMenu contextMenu)
        {
            item_modifyRights = CreaeteMenuItem(CultureStringInfo.ApplicationFindResource("MainWin_ContextMenu_ModifyRights"),
                   "/rmc/resources/icons/Icon_menu_modifyrights_gray.ico");

            item_modifyRights.Command = viewModel.ContextMenuCommand;
            item_modifyRights.CommandParameter = new ContextMenuCmdArgs(selected,
                Constant.CONTEXT_MENU_CMD_MODIFY_RIGHTS);

            item_modifyRights.IsEnabled = false;

            contextMenu.Items.Add(item_modifyRights);
        }

        private static void MenuItemProtect(INxlFile selected, out MenuItem item_protect, ref ContextMenu contextMenu)
        {
            item_protect = CreaeteMenuItem(CultureStringInfo.ApplicationFindResource("MainWin_ContextMenu_Protect"), "/rmc/resources/icons/Icon_protect.png");

            item_protect.Command = viewModel.ContextMenuCommand;
            item_protect.CommandParameter = new ContextMenuCmdArgs(selected, Constant.CONTEXT_MENU_CMD_PROTECT);
            if (selected.Location == EnumFileLocation.Online && !viewModel.IsNetworkAvailable)
            {
                item_protect.IsEnabled = false;
                ChangeMenuItemIcon(item_protect, "/rmc/resources/icons/Icon_protect_gray.png");
            }
            contextMenu.Items.Add(item_protect);
        }

        private static void MenuItemShare(INxlFile selected, out MenuItem item_share, ref ContextMenu contextMenu)
        {
            item_share = CreaeteMenuItem(CultureStringInfo.ApplicationFindResource("MainWin_ContextMenu_Share"), "/rmc/resources/icons/Icon_menu_share_gray.ico");

            item_share.Command = viewModel.ContextMenuCommand;
            item_share.CommandParameter = new ContextMenuCmdArgs(selected, Constant.CONTEXT_MENU_CMD_SHARE);
            item_share.IsEnabled = false;
            contextMenu.Items.Add(item_share);
        }

        private static void MenuItemAddFile(INxlFile selected, out MenuItem item_addFile, ref ContextMenu contextMenu)
        {
            item_addFile = CreaeteMenuItem(CultureStringInfo.ApplicationFindResource("MainWin_ContextMenu_AddFile"), "/rmc/resources/icons/Icon_menu_addfile_gray.ico");

            item_addFile.Command = viewModel.ContextMenuCommand;
            item_addFile.CommandParameter = new ContextMenuCmdArgs(selected, Constant.CONTEXT_MENU_CMD_ADD_FILE);
            item_addFile.IsEnabled = false;
            contextMenu.Items.Add(item_addFile);
        }

        private static void MenuItemView(INxlFile selected, out MenuItem item_view, ref ContextMenu contextMenu)
        {
            item_view = CreaeteMenuItem(CultureStringInfo.ApplicationFindResource("MainWin_ContextMenu_View"), "/rmc/resources/icons/Icon_viewFile.png");
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

        private static void MenuItemViewFileInfo(INxlFile selected, out MenuItem item_viewFileInfo, ref ContextMenu contextMenu)
        {
            item_viewFileInfo = CreaeteMenuItem(CultureStringInfo.ApplicationFindResource("MainWin_ContextMenu_ViewFileInfo"), "/rmc/resources/icons/Icon_viewFileInfo.png");
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

        private static void MenuItemRemove(INxlFile selected, out MenuItem item_remove, ref ContextMenu contextMenu)
        {
            item_remove = CreaeteMenuItem(CultureStringInfo.ApplicationFindResource("MainWin_ContextMenu_Remove"), "/rmc/resources/icons/Icon_remove.png");
            
            item_remove.Command = viewModel.ContextMenuCommand;
            item_remove.CommandParameter = new ContextMenuCmdArgs(selected, Constant.CONTEXT_MENU_CMD_REMOVE);
            contextMenu.Items.Add(item_remove);
        }

        private static void MenuItemOpenSky(INxlFile selected, ref ContextMenu contextMenu)
        {
            MenuItem item_openSkyDRM = CreaeteMenuItem(CultureStringInfo.ApplicationFindResource("MainWin_ContextMenu_OpenWeb"), "/rmc/resources/icons/Icon_openSkyDrm.png");
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

        private static MenuItem CreaeteMenuItem(string header, string iconPath)
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
        private static void ChangeMenuItemIcon(MenuItem item, string iconPath)
        {
            item.Icon = new Image()
            {
                Source = new BitmapImage(new Uri(iconPath, UriKind.RelativeOrAbsolute)),
                //Stretch = Stretch.None,
                Width = Convert.ToDouble("15"),
                Height = Convert.ToDouble("15")
            };
        }
    }
}
