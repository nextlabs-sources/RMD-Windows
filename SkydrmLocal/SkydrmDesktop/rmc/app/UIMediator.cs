using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui;
using SkydrmLocal.rmc.ui.utils;
using SkydrmLocal.rmc.ui.windows;
using SkydrmLocal.rmc.ui.windows.mainWindow.view;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using System;
using System.Collections.Generic;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.fileSystem.basemodel;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Threading;
using System.ComponentModel;
using SkydrmLocal.rmc.common.decryptor;
using Newtonsoft.Json.Linq;
using SkydrmLocal.rmc.database2.table.project;
using System.Windows.Forms;
using SkydrmLocal.rmc.Export;
using SkydrmLocal.rmc.Edit;
using System.Text.RegularExpressions;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.search;
using SkydrmLocal.rmc.removeProtection;
using SkydrmLocal.rmc.helper;
using SkydrmLocal;
using SkydrmDesktop.Resources.languages;
using SkydrmLocal.rmc.process;
using SkydrmDesktop.rmc.ui.windows;
using SkydrmLocal.rmc.exception;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.view;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.operation;
using SkydrmDesktop.rmc.ui.windows.AddExternalRepoWindow.addRepo;
using SkydrmDesktop.rmc.ui.windows.AddExternalRepoWindow.view;
using SkydrmDesktop.rmc.ui.windows.renameFileWindow.rename;
using SkydrmDesktop.rmc.ui.windows.renameFileWindow.view;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.operation.UpdateRecipient;
using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.model;
using SkydrmDesktop.rmc.ui.windows.fileInformation.view;
using SkydrmDesktop.rmc.ui.windows.mainWindow.model;

namespace SkydrmDesktop.rmc.app
{
    // Designed for handle interactings between windows
    public class UIMediator
    {
        SkydrmApp app;

        log4net.ILog log { get => app.Log; }

        Session session { get => app.Rmsdk; }

        MainWindow mw { get => app.MainWin; set => app.MainWin = value; }
        public UIMediator(SkydrmApp app)
        {
            this.app = app;
        }

        //for display InitializeWindow
        private Window loginWin;

        /// <summary>
        ///  Get the opened window through the window tag.
        /// </summary>
        private Window GetOpennedWindow(string tag)
        {
            foreach (Window one in app.Windows)
            {
                if (one.Tag != null && one.Tag.Equals(tag))
                {
                    return one;
                }
            }
            return null;
        }

        public void OnShowLogin(Window sender = null)
        {
            //try
            //{
            //    //for nxrmshell Judgment display window
            //    bool isFirstLogin = true;
            //    foreach (Window one in System.Windows.Application.Current.Windows)
            //    {
            //        if (one.GetType() == typeof(ChooseServerWindow))
            //        {
            //            isFirstLogin = false;
            //            one.Show();
            //            one.Activate();
            //            one.Focus();
            //            one.WindowState = WindowState.Normal;
            //        }
            //        if (one.GetType() == typeof(LoginWindow))
            //        {
            //            isFirstLogin = false;
            //            one.Show();
            //            one.Activate();
            //            one.Focus();
            //            one.WindowState = WindowState.Normal;
            //        }
            //    }

            //    if (isFirstLogin)
            //    {
            //        ChooseServerWindow chooseWindow = new ChooseServerWindow();
            //        chooseWindow.Topmost = true;
            //        chooseWindow.Show();
            //        chooseWindow.Activate();
            //        chooseWindow.Focus();
            //        chooseWindow.WindowState = WindowState.Normal;
            //    }
            //}
            //catch (Exception e)
            //{
            //    log.Fatal("Error:OnShowLogin", e);
            //}

            //if (sender != null)
            //{
            //    sender.Close();
            //}

        }


        public void OnShowFeedBack(Window sender)
        {
            try
            {
                FeedBackWindow feedBackWindow = new FeedBackWindow();
                feedBackWindow.Owner = sender;
                feedBackWindow.ShowDialog();
            }
            catch (Exception e)
            {
                log.Fatal("Error:OnShowFeedBackWindow", e);
            }
        }

        public void OnShowPreference(Window sender=null)
        {
            try
            {
                foreach (Window win in SkydrmApp.Current.Windows)
                {
                    if (win.GetType() == typeof(PreferencesWindow))
                    {
                        win.Show();
                        win.Activate();
                        win.Focus();//will display top
                        win.WindowState = WindowState.Normal;

                        return;
                    }
                }
                PreferencesWindow aboutSkyDrmWindow = new PreferencesWindow();
                //if use Window.Show(), not set Owner; use ShowDialog(), can set Owner
                //aboutSkyDrmWindow.Owner = sender;
                aboutSkyDrmWindow.Show();
            }
            catch (Exception e)
            {
                log.Fatal("Error:OnShowPeferenceWindow", e);
            }
        }

        public void OnShowMain(Window sender)
        {
            Window target=null;
            try
            {
                // find or create main wind
                foreach (Window win in SkydrmApp.Current.Windows)
                {
                    if (win.GetType() == typeof(MainWindow))
                    {
                        target = win;
                        break;
                    }
                }
                if (target == null)
                {
                    mw = new MainWindow();
                    target =mw;
                }
                //fix a bug 51008
                target.Topmost = true;
                // activate and show the ?
                target.Show();
                target.Activate();
                target.Focus();
                target.Topmost = false;

                if (target.WindowState == WindowState.Minimized)
                {
                    target.WindowState = WindowState.Normal;
                }
                
            }
            catch (Exception e)
            {
                log.Fatal("Error:OnShowMainWindow", e);
            }
        }

        public void OnShowAboutTheProject(Window sender = null)
        {
            try
            {
                //fix bug 49880
                foreach (Window win in SkydrmApp.Current.Windows)
                {
                    if (win.GetType() == typeof(AboutSkyDrmWindow))
                    {
                        win.Show();
                        win.Activate();
                        win.Focus();
                        win.WindowState = WindowState.Normal;

                        return;
                    }
                }
                AboutSkyDrmWindow aboutSkyDrmWindow = new AboutSkyDrmWindow();
                //if use Window.Show(), not set Owner; use ShowDialog(), can set Owner
                //aboutSkyDrmWindow.Owner = sender;
                aboutSkyDrmWindow.Show();
            }
            catch (Exception e)
            {
                log.Fatal("Error:OnShowAboutTheProject", e);
            }
        }

        // by comments, use app.OnShowAppHelpInformation() instead
        //public void OnShowProjectHelpWebSite(Window sender)
        //{
        //    try
        //    {
        //        // in dec-2018 release ,we changed the help as local provided
        //        // use app.OnShowAppHelpInformation() instead
        //        //System.Diagnostics.Process.Start("https://help.skydrm.com/docs/windows/help/1.0/en-us/home.htm#t=skydrmintro.htm");
        //        System.Diagnostics.Process.Start("https://help.skydrm.com/docs/windows/help/1.0/en-us/home.htm#t=skydrmintro.htm");
        //    }
        //    catch (Exception e)
        //    {
        //        app.Log.Warn("Error occured when invoke OnShowProjectHelpWebSite\n", e);
        //    }
        //}


        public void OnShowFileInfo(string FileInfoJson)
        {
            try
            {
                // add new feature, explorer will pass a nxl file to it, we need to convert it
                if (!StringHelper.IsValidBase64String(FileInfoJson))
                {
                    var result=DecryptAgent.ConvertNxlFileToJson(FileInfoJson);
                    // based64
                    var data = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(result));
                    OnShowFileInfo(data);
                    return;
                }

                // decapsulate base64 into a json string;
                string json = Encoding.UTF8.GetString(System.Convert.FromBase64String(FileInfoJson));

                if (!StringHelper.IsValidJsonStr_Fast(json))
                {
                    throw new Exception("invalid basic json format");
                }

                // prepare the wnd-tag, prevent show more than one. 
                string wndTag = json + "|" + CultureStringInfo.ShowFileInfoWin_Operation_ShowDetail;

                // if have be shown
                Window old = GetOpennedWindow(wndTag);
                if (old == null)
                {
                    // show new wind with tag
                    Window win = new FileInfoWin(json);
                    win.Tag = wndTag;
                    win.Show();
                    win.Activate();
                    win.Topmost = true;
                    win.Topmost = false;
                    win.Focus();
                }
                else
                {
                    // has been showned, just activaite;
                    old.Show();
                    old.Activate();
                    old.Topmost = true;
                    old.Topmost = false;
                    old.Focus();
                }
            }
            catch (Exception e)
            {
                app.Log.Warn(e.Message, e);
                app.ShowBalloonTip(e.Message, false);
            }
        }

        public void IsShowInitializeWin(bool isShow)
        {
            try
            {
                if (isShow)
                {
                    Window win = new InitializeWindow();
                    win.Show();
                    win.Activate();
                    win.Focus();
                }
                else
                {
                    foreach (Window win in SkydrmApp.Current.Windows)
                    {
                        if (win.GetType() == typeof(InitializeWindow))
                        {
                            win.Close();
                            return;
                        }
                    }
                }
                
            }
            catch (Exception e)
            {
                app.Log.Warn(e.Message, e);
                app.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Notify_PopBubble_Faile_Show_FileInfo"), false);
            }
        }

        //public void OnViewNxl(string nxlFilePath)
        //{
        //   if(!ViewerProcess.BringWindowToTop(nxlFilePath))
        //    {
        //        DecryptAgent decryptAgent = new DecryptAgent();

        //        decryptAgent.Decrypt(nxlFilePath, delegate (NxlConverterResult converterResult)
        //        {
        //            try
        //            {
        //                string data = JsonConvert.SerializeObject(converterResult);
        //                // encapsulate json into base64 format
        //                data = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(data));
        //                ViewerProcess ViewerProcess = new ViewerProcess("View", data);
        //                ViewerProcess.Start(converterResult.LocalDiskPath);
        //                ViewerProcess.ConverterResult = converterResult;
        //            }
        //            catch (Exception e)
        //            {
        //                app.Log.Warn(e.Message, e);
        //            }
        //        });
        //    }     
        //}

        public void OnShowRenameWin(IRenameFile renameFile, Window owner)
        {
            try
            {
                Window win = new RenameFileWin(renameFile);
                win.Owner = owner;
                win.ShowDialog();
            }
            catch (Exception e)
            {
                log.Fatal("Error:OnShowRenameWin", e);
                app.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Common_System_Internal_Error"), false);
            }
        }

        public void OnShowAddRepoWin(IAddExternalRepo addExternalRepo, Window owner)
        {
            try
            {
                Window win = new AddExternalRepoWin(addExternalRepo);
                win.Owner = owner;
                win.ShowDialog();
            }
            catch (Exception e)
            {
                log.Fatal("Error:OnShowAddRepoWin", e);
                app.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Common_System_Internal_Error"), false);
            }
        }

        /// <summary>
        /// Used to prepare data, first display the progress bar Operation Window
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="owner"></param>
        public void OnShowOperationWin(out FileOperationWin win, Window owner, string winTag = null)
        {
            win = null;
            try
            {
                win = new FileOperationWin();
                win.Tag = winTag;
                if (owner != null)
                {
                    win.Owner = owner;
                    win.ShowDialog();
                }
                else
                {
                    win.Topmost = true;
                    win.Topmost = false;
                    win.Show();
                    win.Activate();
                    win.Focus();
                }
            }
            catch (Exception e)
            {
                log.Fatal("Error:OnShowOperationWin", e);
                app.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Common_System_Internal_Error"), false);
            }
        }

        /// <summary>
        /// Operation Window
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="owner"></param>
        public void OnShowOperationWin(IBase operation, Window owner = null)
        {
            try
            {
                string winTag = operation.FileAction.ToString() + "|" + operation.FileInfo.ToString();
                Window opennedWin = GetOpennedWindow(winTag);
                if (opennedWin == null)
                {
                    Window win = new FileOperationWin(operation);
                    win.Tag = winTag;
                    if (owner != null)
                    {
                        win.Owner = owner;
                        win.ShowDialog();
                    }
                    else
                    {
                        win.Show();
                        win.Activate();
                        win.Topmost = true;
                        win.Topmost = false;
                        win.Focus();
                    }
                }
                else
                {
                    opennedWin.Show();
                    opennedWin.Activate();
                    opennedWin.Topmost = true;
                    opennedWin.Topmost = false;
                    opennedWin.Focus();
                    if (owner != null)
                    {
                        owner.WindowState = WindowState.Normal;
                    }
                }
            }
            catch (Exception e)
            {
                log.Fatal("Error:OnShowOperationWin", e);
                app.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Common_System_Internal_Error"), false);
            }
        }

        #region Protect
        private string GetFileDirectory(string[] path)
        {
            if (path.Length < 1)
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }
            string fileDirectory = Path.GetDirectoryName(path[0]);
            //if (!fileDirectory.EndsWith(Path.DirectorySeparatorChar.ToString()))
            //{
            //    fileDirectory = fileDirectory + Path.DirectorySeparatorChar;
            //}
            if(IsUNCPath(fileDirectory))
            {
                return fileDirectory;
            }

            int option;
            string tags;
            if (app.Rmsdk.RMP_IsSafeFolder(fileDirectory, out option, out tags) || fileDirectory.Contains(app.User.WorkingFolder))
            {
                fileDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }

            return fileDirectory;
        }
        public void OnShowOperationProtectWinByDrag(string[] filePath, List<SkydrmLocal.rmc.fileSystem.IFileRepo> fileRepos,
            ui.windows.mainWindow.model.CurrentSelectedSavePath selectedSavePath, Window owner, FileFromSource fileFrom)
        {
            if (selectedSavePath == null)
            {
                string selectFolder = GetFileDirectory(filePath);
                selectedSavePath = new ui.windows.mainWindow.model.CurrentSelectedSavePath(
                    DataTypeConvertHelper.SYSTEMBUCKET, selectFolder, selectFolder, app.SystemProject.Id.ToString());
            }
            OnShowOperationProtectWin(filePath, fileRepos, selectedSavePath, owner, fileFrom);
        }
        public void OnShowOperationProtectWinByPlugIn(string path, string tag)
        {
            string[] filePath = new string[1];
            filePath[0] = path;

            bool isSpecialProtect = !string.IsNullOrWhiteSpace(tag);
            Dictionary<string, List<string>> tags = new Dictionary<string, List<string>>();
            if (isSpecialProtect)
            {
                //tags = DataConvert.ParseClassificationTag(tag);
                List<string> value = new List<string>();
                value.Add(tag);
                tags.Add("Sensitivity", value);
            }

            if (app.MainWin.viewModel.IsInitializing)
            {
                app.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Notify_PopBubble_Wait_Initial"), false, Path.GetFileName(filePath[0]));
                return;
            }


            if (!ProtectFileHelper.CheckFilePathDoProtect(filePath, out string wintag, out List<string> rightFilePath))
            {
                return;
            }

            string selectFolder = GetFileDirectory(filePath);
            ui.windows.mainWindow.model.CurrentSelectedSavePath currentSavePath = new ui.windows.mainWindow.model.CurrentSelectedSavePath(
                DataTypeConvertHelper.SYSTEMBUCKET, selectFolder, selectFolder, app.SystemProject.Id.ToString());

            if (isSpecialProtect)
            {
                OnShowOperationSpecialProtectWin(rightFilePath.ToArray(), tags, currentSavePath, null, FileFromSource.SkyDRM_PlugIn);
            }
            else
            {
                var repos = app.MainWin.viewModel.GetFileRepos();

                OnShowOperationProtectWin(rightFilePath.ToArray(), repos, currentSavePath, null, FileFromSource.SkyDRM_PlugIn);
            }
        }
        public void OnShowOperationProtectWin(string[] filePath, List<SkydrmLocal.rmc.fileSystem.IFileRepo> fileRepos,
            ui.windows.mainWindow.model.CurrentSelectedSavePath selectedSavePath, Window owner = null,
            FileFromSource fileFrom = FileFromSource.SkyDRM_Window_Button)
        {
            if (selectedSavePath != null && selectedSavePath.RepoName.Equals(DataTypeConvertHelper.MY_DRIVE))
            {
                selectedSavePath = new ui.windows.mainWindow.model.CurrentSelectedSavePath(DataTypeConvertHelper.MY_VAULT, "/", 
                    "SkyDRM://" + DataTypeConvertHelper.MY_SPACE);
            }

            IBase operat = new Protect(new OperateFileInfo(filePath, null, fileFrom),
                fileRepos, selectedSavePath);

            OnShowOperationWin(operat, owner);
        }

        private void OnShowOperationSpecialProtectWin(string[] filePath, Dictionary<string, List<string>> tags,
            ui.windows.mainWindow.model.CurrentSelectedSavePath selectedSavePath, Window owner = null,
           FileFromSource fileFrom = FileFromSource.SkyDRM_PlugIn)
        {
            if (tags.Count < 1)
            {
                app.ShowBalloonTip("File sensitivity label is empty, can not protect.", false, Path.GetFileName(filePath[0]));
                return;
            }

            IBase operat = new SpecialProtect(new OperateFileInfo(filePath, null, fileFrom), selectedSavePath, tags);

            OnShowOperationWin(operat, owner);
        }
        #endregion

        #region Protect&Share
        public void OnShowOperationShareWinByMainWin(string[] filePath, Window owner, FileFromSource fileFrom)
        {
            OnShowOperationShareWin(filePath, owner, fileFrom);
        }
        public void OnShowOperationShareWinByPlugIn(string filepath)
        {
            if (app.MainWin.viewModel.IsInitializing)
            {
                app.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Notify_PopBubble_Wait_Initial"), false, Path.GetFileName(filepath));
                return;
            }

            string[] filePath = new string[1];
            filePath[0] = filepath;

            OnShowOperationShareWin(filePath, null, FileFromSource.SkyDRM_PlugIn);
        }
        private void OnShowOperationShareWin(string[] filePath, Window owner = null, FileFromSource fileFrom = FileFromSource.SkyDRM_Window_Button)
        {
            if (!ProtectFileHelper.CheckFilePathDoProtect(filePath, out string tag, out List<string> rightFilePath))
            {
                return;
            }

            IBase operat = new ProtectAndShare(new OperateFileInfo(rightFilePath.ToArray(), null, fileFrom));

            OnShowOperationWin(operat, owner);
        }
        #endregion

        #region UpdateRecipients
        public void OnShowOperationUpdateRecipiWinByMainWin(string nxlPath, Window owner, FileFromSource fileFrom)
        {
            OnShowOperationUpdateRecipiWin(nxlPath, owner, fileFrom);
        }
        public void OnShowOperationUpdateRecipiWinByPlugIn(string nxlPath)
        {
            OnShowOperationUpdateRecipiWin(nxlPath, null, FileFromSource.SkyDRM_PlugIn);
        }
        private void OnShowOperationUpdateRecipiWin(string nxlPath, Window owner = null, FileFromSource fileFrom = FileFromSource.SkyDRM_Window_Button)
        {
            try
            {
                var fp = app.Rmsdk.User.GetNxlFileFingerPrint(nxlPath);

                if (!(fp.isFromMyVault && fp.HasRight(FileRights.RIGHT_SHARE)))
                {
                    app.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Common_Not_Authorized"), false, Path.GetFileName(nxlPath));
                    return;
                }

                string[] filePath = new string[1];
                filePath[0] = nxlPath;
                IBase update = new UpdateRecipient(fp, new OperateFileInfo(filePath, null, fileFrom));

                OnShowOperationWin(update, owner);
            }
            catch (SkydrmException se)
            {
                app.Log.Error(se.Message, se);
                app.ShowBalloonTip(se.Message, false, Path.GetFileName(nxlPath));
            }
            catch (Exception e)
            {
                app.Log.Error(e.Message, e);
                app.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Common_System_Internal_Error"), false, Path.GetFileName(nxlPath));
            }
        }
        #endregion

        #region Upload file
        public void OnShowOperationUploadWinByMainWin(string[] filePath,
            ui.windows.mainWindow.model.CurrentSelectedSavePath selectedSavePath, Window owner)
        {
            if (!ProtectFileHelper.CheckFilePathDoProtect(filePath, out string tag, out List<string> rightFilePath))
            {
                return;
            }

            IBase operat = new UploadFile(new OperateFileInfo(rightFilePath.ToArray()),
                selectedSavePath);

            OnShowOperationWin(operat, owner);
        }
        #endregion

        #region Add Nxl File
        public void OnShowOperationAddNxlWinByFileList(string nxlPath, Window owner, INxlFile nxlFile, FileFromSource fileFrom)
        {
            OnShowOperationAddNxlWin(nxlPath, owner, fileFrom, nxlFile);
        }
        public void OnShowOperationAddNxlWinByMainWin(List<SkydrmLocal.rmc.fileSystem.IFileRepo> fileRepos,
            ui.windows.mainWindow.model.CurrentSelectedSavePath selectedSavePath, Window owner, FileFromSource fileFrom)
        {
            IBase addNxl = null;

            addNxl = new AddNxlFile(new OperateFileInfo(new string[0], null, fileFrom),
                   fileRepos, selectedSavePath);

            OnShowOperationWin(addNxl, owner);
        }
        public void OnShowOperationAddNxlWinByDrag(string nxlPath,  Window owner, CurrentSelectedSavePath savePath)
        {
            OnShowOperationAddNxlWin(nxlPath, owner, FileFromSource.SkyDRM_PlugIn, null, savePath);
        }
        public void OnShowOperationAddNxlWinByPlugIn(string nxlPath)
        {
            if (app.MainWin.viewModel.IsInitializing)
            {
                app.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Notify_PopBubble_Wait_Initial"), false, Path.GetFileName(nxlPath));
                return;
            }
            OnShowOperationAddNxlWin(nxlPath, null, FileFromSource.SkyDRM_PlugIn);
        }
        private void OnShowOperationAddNxlWin(string nxlPath, Window owner = null,
            FileFromSource fileFrom = FileFromSource.SkyDRM_Window_Button, INxlFile nxlFile = null, CurrentSelectedSavePath savePath = null)
        {
            try
            {
                var fp = app.Rmsdk.User.GetNxlFileFingerPrint(nxlPath);

                if (!(fp.isFromMyVault
                           || fp.isFromSystemBucket
                           || (fp.isFromPorject && (fp.hasAdminRights || fp.HasRight(FileRights.RIGHT_DECRYPT)))))
                {
                    app.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Common_Not_Authorized"), false, Path.GetFileName(nxlPath));
                    return;
                }

                string[] filePath = new string[1];
                filePath[0] = nxlPath;
                IBase addNxl = null;

                addNxl = new AddNxlFile(fp, new OperateFileInfo(filePath, null, fileFrom),
                       app.MainWin.viewModel.GetFileRepos(), savePath, nxlFile);

                OnShowOperationWin(addNxl, owner);
            }
            catch (SkydrmException se)
            {
                app.Log.Error(se.Message, se);
                app.ShowBalloonTip(se.Message, false, Path.GetFileName(nxlPath));
            }
            catch (Exception e)
            {
                app.Log.Error(e.Message, e);
                app.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Common_System_Internal_Error"), false, Path.GetFileName(nxlPath));
            }
        }
        #endregion

        #region Modify Nxl file rights
        public void OnShowOperationModifyRightWinByFileList(string nxlPath, Window owner, FileFromSource fileFrom)
        {
            OnShowOperationModifyRightWin(nxlPath, owner, fileFrom);
        }
        public void OnShowOperationModifyRightWinByFilterFileList(string nxlPath, Window owner, string fileDestPath, FileFromSource fileFrom)
        {
            OnShowOperationModifyRightWin(nxlPath, owner, fileFrom, fileDestPath);
        }
        public void OnShowOperationModifyRightWinByPlugIn(string nxlPath)
        {
            OnShowOperationModifyRightWin(nxlPath, null, FileFromSource.SkyDRM_PlugIn);
        }
        private void OnShowOperationModifyRightWin(string nxlPath, Window owner = null, FileFromSource fileFrom = FileFromSource.SkyDRM_Window_Button,
            string fileDestPath = null)
        {
            try
            {
                var fp = app.Rmsdk.User.GetNxlFileFingerPrint(nxlPath);

                if (!fp.hasAdminRights)
                {
                    app.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Common_Not_Authorized"), false, Path.GetFileName(nxlPath));
                    return;
                }

                string[] filePath = new string[1];
                filePath[0] = nxlPath;
                IBase modifyRight = null;
                if (fileFrom == FileFromSource.SkyDRM_PlugIn)
                {
                    modifyRight = new ModifyNxlFileRight(fp, new OperateFileInfo(filePath, null, fileFrom),
                         app.MainWin.viewModel.GetFileRepos());
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(fileDestPath))
                    {
                        string repoPath = fileDestPath.Substring(9);
                        string repoName = repoPath.Substring(0, repoPath.IndexOf('/'));
                        // in modifyRights viewModel will not use pathId, so set defult value.
                        string pathId = "/";
                        string displayPath = fileDestPath.Substring(0, fileDestPath.LastIndexOf('/'));
                        ui.windows.mainWindow.model.CurrentSelectedSavePath selectedSavePath = 
                            new ui.windows.mainWindow.model.CurrentSelectedSavePath(repoName, pathId, displayPath, fp.projectId.ToString());

                        modifyRight = new ModifyNxlFileRight(fp, new OperateFileInfo(filePath, null, fileFrom),
                        app.MainWin.viewModel.GetFileRepos(), selectedSavePath);
                    }
                    else
                    {
                        modifyRight = new ModifyNxlFileRight(fp, new OperateFileInfo(filePath, null, fileFrom),
                        app.MainWin.viewModel.GetFileRepos(), app.MainWin.viewModel.CurrentSaveFilePath);
                    }
                    
                }
                OnShowOperationWin(modifyRight, owner);
            }
            catch (SkydrmException se)
            {
                app.Log.Error(se.Message, se);
                app.ShowBalloonTip(se.Message, false, Path.GetFileName(nxlPath));
            }
            catch (Exception e)
            {
                app.Log.Error(e.Message, e);
                app.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Common_System_Internal_Error"), false, Path.GetFileName(nxlPath));
            }
        }
        #endregion

        #region Project Share
        /// <summary>
        /// Use for Viewer do share, only support project online file
        /// </summary>
        /// <param name="nxlPath"></param>
        public void OnShowOperationReShareWin(string nxlPath)
        {
            INxlFile nxlFile = GlobalSearchEx.GetInstance().SearchByLocalPath(nxlPath, SearchFileRepo.Project, SearchFileTable.Rms);
            if (nxlFile == null)
            {
                app.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Common_System_Internal_Error"), false, "","Search File");
                return;
            }
            if (nxlFile.FileRepo == EnumFileRepo.REPO_PROJECT)
            {
                if (nxlFile.IsRevoked)
                {
                    app.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("ReShareUpOperation_RevokedFile"), false, nxlFile.Name, "",
                        nxlFile.IsMarkedOffline ? featureProvider.MessageNotify.EnumMsgNotifyIcon.Offline : featureProvider.MessageNotify.EnumMsgNotifyIcon.Online);
                    return;
                }
                if (app.MainWin.viewModel.projectRepo.FilePool.Count == 1)
                {
                    app.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("ReShareOperation_No_OtherProjects"), false, nxlFile.Name, "",
                        nxlFile.IsMarkedOffline ? featureProvider.MessageNotify.EnumMsgNotifyIcon.Offline : featureProvider.MessageNotify.EnumMsgNotifyIcon.Online);
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
                    operat = new ReShareUpdate(nxlFile, app.MainWin.viewModel.projectRepo.FilePool.ToList(), projectID);
                }
                else
                {
                    operat = new ReShare(nxlFile, app.MainWin.viewModel.projectRepo.FilePool.ToList());
                }
                OnShowOperationWin(operat);
            }
        }
        #endregion

        
        public void OnExtractContent(string paramdetail)
        {
            SkydrmApp SkydrmApp = SkydrmApp.Singleton;
    
            // decapsulate base64 into a string;
            if (StringHelper.IsValidBase64String(paramdetail))
            {
                string nxlFileLocalPath = string.Empty;
                string DestinationPath = string.Empty;

                try
                {
                    paramdetail = Encoding.UTF8.GetString(System.Convert.FromBase64String(paramdetail));

                    JObject jo = (JObject)JsonConvert.DeserializeObject(paramdetail);

                    if (jo.ContainsKey("NxlFileLocalPath"))
                    {
                        nxlFileLocalPath = jo["NxlFileLocalPath"].ToString();
                    }

                    if (jo.ContainsKey("DestinationPath"))
                    {
                        DestinationPath = jo["DestinationPath"].ToString();
                    }

                    string decryptedFilePath = string.Empty;

                    ExtractContentHelper.DecryptFile(SkydrmApp, nxlFileLocalPath, out decryptedFilePath);
     
                    bool isExtractContentSuccess = ExtractContentHelper.MoveFile(SkydrmApp, decryptedFilePath, DestinationPath);

                    ExtractContentHelper.SendLog(nxlFileLocalPath,NxlOpLog.Decrypt, isExtractContentSuccess);

                }
                catch (Exception ex)
                {
                    ExtractContentHelper.SendLog(nxlFileLocalPath, NxlOpLog.Decrypt, false);
                    SkydrmApp.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Notify_PopBubble_ExtractContent_Failed"), false, Path.GetFileName(nxlFileLocalPath));
                }
            }
            else
            {
                Alphaleonis.Win32.Filesystem.FileInfo fileInfo = new Alphaleonis.Win32.Filesystem.FileInfo(paramdetail);

                if (fileInfo.Exists && fileInfo.Length > 16384) // 16384 is file head length
                {
                    bool isCanceled;

                    bool isExtractContentSuccess = ExtractContentHelper.ExtractContent(SkydrmApp, SkydrmApp.MainWindow, paramdetail, out isCanceled);

                    if (!isCanceled)
                    {
                        ExtractContentHelper.SendLog(paramdetail, NxlOpLog.Decrypt, isExtractContentSuccess);                   
                    }
                }
                else
                {
                    SkydrmApp.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Notify_PopBubble_ExtractContent_Failed"), false);
                }
            }     
        }


        public void TryEdit(string ParamDetail)
        {
            app.Log.Info("UIMediator  OnEditFile(string ParamDetail) === ParamDetail :" + ParamDetail);
       
            try
            {
                // decapsulate base64 into a json string;
                if (StringHelper.IsValidBase64String(ParamDetail))
                {

                    bool isNeedUpload = false;

                    string nxlFileLocalPath = string.Empty;

                    string json = Encoding.UTF8.GetString(System.Convert.FromBase64String(ParamDetail));

                    JObject jo = (JObject)JsonConvert.DeserializeObject(json);
            
                    if (jo.ContainsKey("IsNeedUpload"))
                    {
                        isNeedUpload = (bool)jo.GetValue("IsNeedUpload");            
                    }

                    if (jo.ContainsKey("NXlFileLocalPath"))
                    {
                        nxlFileLocalPath = jo["NXlFileLocalPath"].ToString();
                    }

                    if (isNeedUpload)
                    {
                        SkydrmApp.Singleton.MainWin.viewModel.EditFromViewer(nxlFileLocalPath);
                    } 

                    // fix bug 53660 edit office local file can't launch office process
                    else if(File.Exists(nxlFileLocalPath) && new Alphaleonis.Win32.Filesystem.FileInfo(nxlFileLocalPath).Length > 0)  // require file exist and file size >0    
                    {
                        // explorer edit
                        FileEditorHelper.DoEdit(nxlFileLocalPath, (EditCallBack EditCallBack) => {

                        });
                    }
                }
                else if (File.Exists(ParamDetail) && new Alphaleonis.Win32.Filesystem.FileInfo(ParamDetail).Length > 0)  // require file exist and file size >0    
                {
                    // explorer edit
                    FileEditorHelper.DoEdit(ParamDetail, (EditCallBack EditCallBack) => {

                    });
                }
            }
            catch (Exception ex)
            {
                SkydrmApp.Singleton.Log.Error(ex.Message.ToString(), ex);
            }
        }

        public void OnExportFileDialog(string exportInfoJson)
        {
            try
            {
                app.Log.Info("UIMediator OnSaveFileDialog(string saveAsInfoJson) === saveAsInfoJson :" + exportInfoJson);
                // decapsulate base64 into a json string;
                exportInfoJson = Encoding.UTF8.GetString(System.Convert.FromBase64String(exportInfoJson));
                FileExport export = new FileExport();
                FileExport.Param p = FileExport.Param.BuildFromJson(exportInfoJson);
                export.Export(p);
                app.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Exception_ExportFeature_Succeeded") + p.DestinationPath + ".", true);
            }
            catch (Exception e)
            {
                app.Log.Warn(e.Message, e);
                // show buble to notify user
                app.ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Exception_ExportFeature_Failed"), false);
            }
        }


        /// <summary>
        /// if  path is UNC( Universal Naming Convention) path return or return false.
        /// formatter：\\servername\sharename
        /// </summary>
        /// <param name="path">path</param>
        /// <returns></returns>
        public bool IsUNCPath(string path)
        {
            string root = Path.GetPathRoot(path);

            // Check if root starts with "\\", clearly an UNC
            if (root.StartsWith(@"\\"))
                return true;

            // Check if the drive is a network drive
            DriveInfo drive = new DriveInfo(root);
            if (drive.DriveType == System.IO.DriveType.Network)
                return true;

            return false;
        }

        private class LoginPara
        {
            public string LoginStr { get; private set; }
            public string WebUrl { get; private set; }
            public bool IsPersonal { get; private set; }
            public LoginPara(string loginStr, string webUrl, bool isPersonal)
            {
                this.LoginStr = loginStr;
                this.WebUrl = webUrl;
                this.IsPersonal = isPersonal;
            }
        }
    }
}
