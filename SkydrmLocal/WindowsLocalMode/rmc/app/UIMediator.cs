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
using SkydrmLocal.rmc.process;
using Newtonsoft.Json.Linq;
using SkydrmLocal.rmc.database2.table.project;
using System.Windows.Forms;
using SkydrmLocal.rmc.Export;
using SkydrmLocal.rmc.Edit;
using SkydrmLocal.rmc.drive;
using System.Text.RegularExpressions;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.ui.windows.nxlConvert;
using SkydrmLocal.rmc.search;
using SkydrmLocal.rmc.shareNxlFeature;
using SkydrmLocal.rmc.ui.pages.model;
using SkydrmLocal.rmc.modifyRights;
using SkydrmLocal.rmc.removeProtection;
using SkydrmLocal.rmc.helper;

namespace SkydrmLocal.rmc.app
{
    // Designed for handle interactings between windows
    public class UIMediator
    {
        SkydrmLocalApp app;

        log4net.ILog log { get => app.Log; }

        Session session { get => app.Rmsdk; }

        MainWindow mw { get => app.MainWin; set => app.MainWin = value; }
        public UIMediator(SkydrmLocalApp app)
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
            try
            {
                //for nxrmshell Judgment display window
                bool isFirstLogin = true;
                foreach (Window one in System.Windows.Application.Current.Windows)
                {
                    if (one.GetType() == typeof(ChooseServerWindow))
                    {
                        isFirstLogin = false;
                        one.Show();
                        one.Activate();
                        one.Focus();
                        one.WindowState = WindowState.Normal;
                    }
                    if (one.GetType() == typeof(LoginWindow))
                    {
                        isFirstLogin = false;
                        one.Show();
                        one.Activate();
                        one.Focus();
                        one.WindowState = WindowState.Normal;
                    }
                }

                if (isFirstLogin)
                {
                    ChooseServerWindow chooseWindow = new ChooseServerWindow();
                    chooseWindow.Topmost = true;
                    chooseWindow.Show();
                    chooseWindow.Activate();
                    chooseWindow.Focus();
                    chooseWindow.WindowState = WindowState.Normal;
                }
            }
            catch (Exception e)
            {
                log.Fatal("Error:OnShowLogin", e);
            }

            if (sender != null)
            {
                sender.Close();
            }

        }

       
        private void DoWork_Handler(object sender, DoWorkEventArgs args)
        {
            bool invoke = true;

            LoginPara para = (LoginPara)args.Argument;

            string loginString = para.LoginStr;
            try
            {
                app.Rmsdk.SetLoginRequest(loginString);
                // set user info into app.config
                var user = app.Rmsdk.User;
                if (!app.Config.SetUserInfo(user.Name, user.Email, user.PassCode))
                {
                    args.Result = false;
                    return;
                }
                rmc.sdk.Tenant tenant = session.GetCurrentTenant();
                if (!app.Config.SetTenantInfo(tenant.RouterURL, tenant.Name))
                {
                    args.Result = false;
                    return;
                }

                // upsert server table 
                if (!string.IsNullOrEmpty(tenant.RouterURL))
                {
                    // tell db weburl will be displayed -- using routerUrl and tenant as primary key intead of serverUrl -- fix bug 52730
                    app.DBFunctionProvider.UpsertServer(tenant.RouterURL, para.WebUrl, tenant.Name, !para.IsPersonal);
                }

                uint userId = CommonUtils.GetUserId(loginString);
                user.UserId = userId;

                app.Log.Info("*******UserId*********: " + user.UserId);

                // tell DB User login
                app.DBFunctionProvider.OnUserLogin((int)user.UserId,
                                    user.Name, user.Email, user.PassCode, (int)user.UserType, loginString);

                // cleanup the sessions left
                SkydrmLocal.rmc.fileSystem.external.Helper.cleanup_edit_mapping();

                //

            }
            catch (Exception e)
            {
                app.Log.Warn("error when preparing login user info," + e.Message, e);
                invoke = false;
            }

            args.Result = invoke;
        }
        private void RunWorkerCompleted_Handler(object sender, RunWorkerCompletedEventArgs args)
        {
            bool invoke = (bool)args.Result;
            if (invoke)
            {
                // This will takes a few seconds sometimes, now put it here, and will optimize later.
                app.OnUserLogin();

                //If InitializeWin is opened, close InitializeWindow
                IsShowInitializeWin(false);

                //app.OnUserLogin();

                loginWin.Close();
            }
            else
            {
                IsShowInitializeWin(false);
                loginWin.Close();
                app.ShowBalloonTip(CultureStringInfo.Common_Initialize_failed);
                //Goto chooseServerWindow
                OnShowLogin();
            }
            
        }
        


        public void OnLogin(Window loginWindow, string loginString, string webUrl, bool isPersonal)
        {
            try
            {
                //load InitializeWindow
                IsShowInitializeWin(true);

                BackgroundWorker bgWorker = new BackgroundWorker();
                bgWorker.WorkerReportsProgress = true;
                bgWorker.WorkerSupportsCancellation = true;
                bgWorker.DoWork += DoWork_Handler;
                bgWorker.RunWorkerCompleted += RunWorkerCompleted_Handler;
                loginWin = loginWindow;
                if (!bgWorker.IsBusy)
                {
                    bgWorker.RunWorkerAsync(new LoginPara(loginString, webUrl, isPersonal));
                }
               
            }
            catch (Exception e)
            {
                log.Fatal("Error: OnLogin", e);
            }
        }

        //for chooseServer Window
        public void OnShowLoginWin(Window sender = null, bool IsPersonal = true, string routerUrl = "", bool IsRemeber = true)
        {
            try
            {
                LoginWindow loginWindow = new LoginWindow(Intention.REGISTER, IsPersonal, routerUrl, IsRemeber);
                loginWindow.Show();

            }
            catch (Exception e)
            {
                log.Fatal("Error:OnShowLogin", e);
            }

            if (sender != null)
            {
                sender.Close();
            }

        }
        public void OnShowSignUp(Window sender)
        {
            try
            {
                LoginWindow loginWindow = new LoginWindow(Intention.SIGN_UP);
                loginWindow.Show();
            }
            catch (Exception e)
            {
                log.Fatal("Error:OnShowRegister", e);
            }
            if (sender != null)
            {
                sender.Close();
            }
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
                foreach (Window win in SkydrmLocalApp.Current.Windows)
                {
                    if (win.GetType() == typeof(PreferencesWindow2))
                    {
                        win.Show();
                        win.Activate();
                        win.Focus();//will display top
                        win.WindowState = WindowState.Normal;

                        return;
                    }
                }
                PreferencesWindow2 aboutSkyDrmWindow = new PreferencesWindow2();
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
                foreach (Window win in SkydrmLocalApp.Current.Windows)
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
                foreach (Window win in SkydrmLocalApp.Current.Windows)
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

        /// <summary>
        /// Show protect file window, can open the window from explorer context menu and from main window.
        /// </summary>
        /// <param name="filePath">The file path to protect. (normal file)</param>
        /// <param name="owner">the parent window, can be nullable such as from explorer context menu.</param> 
        /// <param name="callback">Create file succeed</param>
        public void OnShowProtect(string[] filePath, Window owner = null, bool IsCreateByMainWin = true, IMyProject myProject = null)
        {
            if (!CommonUtils.CheckFilePathDoProtect(filePath, out string tag, out List<string> rightFilePath))
            {
                return;
            }
          
            try
            {
                //
                // we have 2 style window
                //

                //  onwer is not null, we will show it as a dialog  (designed for other internal Window)
                //  owner is null,  we will show it as a normal window with topmost  (designed for external app used)
                string TagString = tag + "|" + CultureStringInfo.CreateFileWin_Operation_Protect;
                Window opennedWin = GetOpennedWindow(TagString);
                if (opennedWin == null)
                {
                    try
                    {
                        Window win = new ConvertToNxlFileWindow(
                            new FileOperation(rightFilePath.ToArray(), FileOperation.ActionType.Protect), IsCreateByMainWin, myProject);

                        //Window win = new CreateFileWindow(
                        //    new FileOperation(filePath, FileOperation.ActionType.Protect), IsCreateByMainWin, myProject);
                        win.Tag = tag + "|" + CultureStringInfo.CreateFileWin_Operation_Protect;
                        if (owner != null)
                        {
                            win.Owner = owner;
                            win.ShowDialog();
                        }
                        else
                        {
                            win.Topmost = true;
                            win.Show();
                            win.Activate();
                            win.Focus();
                        }
                    }
                    catch (Exception e)
                    {
                        log.Fatal("Error:OnShowProtect", e);
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
                app.ShowBalloonTip("Unexpected error: " + e.Message);
                log.Error(e);
            }
        }
        /// <summary>
        /// Show protect file window, can open the window from explorer context menu and from Plug-in (nxrmshell).
        /// </summary>
        /// <param name="filePath">The file path to protect. (normal file)</param>
        public void OnShowProtect(string normalFilePath)
        {
            string[] filePath = new string[1];
            filePath[0] = normalFilePath;
            OnShowProtect(filePath, null, false);
        }
        /// <summary>
        ///  Show share normal file window, can open the window from explorer context menu, from main window 
        /// </summary>
        /// <param name="normalFilePath">
        /// the normal file path to share. 
        /// </param>
        /// <param name="owner">the parent window, can be nullable, such as from explorer context menu.</param>
        /// <param name="callback">Share file succeed</param>
        public void OnShowShare(string[] normalFilePath, Window owner = null, bool IsCreateByMainWin = true)
        {
            if (!CommonUtils.CheckFilePathDoProtect(normalFilePath, out string tag, out List<string> rightFilePath))
            {
                return;
            }

            try
            {
                //
                // we have 2 style window
                //

                //  onwer is not null, we will show it as a dialog  (designed for other internal Window)
                //  owner is null,  we will show it as a normal window with topmost  (designed for external app used)
                string TagString = tag + "|" + CultureStringInfo.CreateFileWin_Operation_Share;
                Window opennedWin = GetOpennedWindow(TagString);
                if (opennedWin == null)
                {
                    try
                    {
                        Window win = new ConvertToNxlFileWindow(new FileOperation(rightFilePath.ToArray(), FileOperation.ActionType.Share), IsCreateByMainWin);
                        //Window win = new CreateFileWindow(new FileOperation(normalFilePath, FileOperation.ActionType.Share), IsCreateByMainWin);
                        win.Tag = tag + "|" + CultureStringInfo.CreateFileWin_Operation_Share;
                        if (owner != null)
                        {
                            win.Owner = owner;
                            win.ShowDialog();
                        }
                        else
                        {
                            win.Topmost = true;
                            win.Show();
                            win.Activate();
                            win.Focus();
                        }

                    }
                    catch (Exception e)
                    {
                        log.Fatal("Error:OnShowShare", e);
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
                app.ShowBalloonTip("Unexpected error: " + e.Message);
                log.Error(e);
            }

        }

        public void OnShowShare(string normalFilePath)
        {
            string[] filePath = new string[1];
            filePath[0] = normalFilePath;
            OnShowShare(filePath, null, false);
        }

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
                    Window win = new FileInformationWindow(json);
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
                app.ShowBalloonTip("Unexpected error when displaying file info.");
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
                    foreach (Window win in SkydrmLocalApp.Current.Windows)
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
                app.ShowBalloonTip("Unexpected error when initializing window.");
            }
        }

        public void OnViewNxl(string nxlFilePath)
        {
            new Thread(new ParameterizedThreadStart(ExecuteViewInBackground)) { Name = "ExecuteViewInBackground", IsBackground = true, Priority = ThreadPriority.Normal }.Start(nxlFilePath);
        }

        private void ExecuteViewInBackground(object obj)
        {
            if (!ViewerProcess.BringWindowToTop(obj.ToString()))
            {
                DecryptAgent decryptAgent = new DecryptAgent();

                decryptAgent.Decrypt(obj.ToString(), delegate (NxlConverterResult converterResult)
                {
                    try
                    {
                        string data = JsonConvert.SerializeObject(converterResult);
                        // encapsulate json into base64 format
                        data = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(data));
                        ViewerProcess ViewerProcess = new ViewerProcess("View", data);
                        ViewerProcess.Start(converterResult.LocalDiskPath);
                        ViewerProcess.ConverterResult = converterResult;
                    }
                    catch (Exception e)
                    {
                        app.Log.Warn(e.Message, e);
                    }
                });
            }
        }


        #region ShareNxlFileToPerson
        /// <summary>
        ///  Offline nxlFile share to person
        ///  If file from myVault do updateRecipient. If file from project or systemBucket do protect&share(create new file in myVault)
        /// </summary>
        /// <param name="nxlFileLocalPath"></param>
        /// <param name="owner"></param>
        public void OnShareNxlToPerson(string nxlFileLocalPath, Window owner = null)
        {
            // Fix bug 54330 & 54322
            if (!app.MainWin.viewModel.IsNetworkAvailable)
            {
                app.ShowBalloonTip(CultureStringInfo.ShareFileWin_Notify_File_Share_Failed_No_network);
                return;
            }

            app.Log.Info("OnShareNxlToPerson");
            IShareNxlFeature shareNxlFile = new ShareNxlFeature(ShareNxlFeature.ShareNxlAction.Share, nxlFileLocalPath, false);
            ProtectAndShareConfig config = null;

            if (!shareNxlFile.BuildConfig(out config))
            {
                return;
            }
            config.ShareNxlFeature = shareNxlFile;
            if (config.IsAdHoc)
            {
                OnShowShareWin(config, owner, nxlFileLocalPath);
            }
            else
            {
                app.ShowBalloonTip(CultureStringInfo.Common_CentralPolicyFile_Not_Share);
            }

        }

        /// <summary>
        /// For offline nxlFile, Share(create a new file or UpdateRecipient) 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="owner"></param>
        private void OnShowShareWin(ProtectAndShareConfig config, Window owner, string nxlLocalPath)
        {
            try
            {
                string winTag = config.FileOperation.Action.ToString() + "|" + nxlLocalPath;
                Window opennedWin = GetOpennedWindow(winTag);
                if (opennedWin == null)
                {
                    Window win = new ShareWindow(config);
                    win.Tag = winTag;
                    if (owner != null)
                    {
                        win.Owner = owner;
                        win.ShowDialog();
                    }
                    else
                    {
                        win.Topmost = true;
                        win.Show();
                        win.Activate();
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
                log.Fatal("Error:OnShowShareWindow", e);
                app.ShowBalloonTip(CultureStringInfo.Common_System_Internal_Error);
            }
        }

        /// <summary>
        /// For online nxlFile, UpdateReceipient(keep same file) and Share(create a new file) 
        /// </summary>
        /// <param name="owner"></param>
        public void OnShowShareWin(out ShareWindow win, Window owner, string winTag = null)
        {
            win = null;
            try
            {
                win = new ShareWindow();
                win.Tag = winTag;
                if (owner != null)
                {
                    win.Owner = owner;
                    win.ShowDialog();
                }
                else
                {
                    win.Topmost = true;
                    win.Show();
                    win.Activate();
                    win.Focus();
                }
                
            }
            catch (Exception e)
            {
                log.Fatal("Error:OnShowUpdateReceipient", e);
                app.ShowBalloonTip(CultureStringInfo.Common_System_Internal_Error);
            }
        }
        #endregion


        #region ShareNxlFileToProject
        /// <summary>
        /// For offline nxlFile, add to project (Actually do protect)
        /// </summary>
        /// <param name="nxlFileLocalPath"></param>
        /// <param name="owner"></param>
        public void OnAddNxlFileToProject(string nxlFileLocalPath, Window owner=null)
        {
            app.Log.Info("OnAddNxlFileToProject:");

            IShareNxlFeature shareNxlFile = new ShareNxlFeature(ShareNxlFeature.ShareNxlAction.AddFileToProject, nxlFileLocalPath, false);
            ProtectAndShareConfig config = null;

            if (!shareNxlFile.BuildConfig(out config))
            {
                return;
            }
            
            config.ShareNxlFeature = shareNxlFile;
            if (!config.IsAdHoc)
            {
                OnShowNxlConvertWin(config, owner, nxlFileLocalPath);
            }
            else
            {
                app.ShowBalloonTip(CultureStringInfo.Common_AdhocFile_Not_AddToProject);
            }
        }
        #endregion

        /// <summary>
        /// For online nxlFile, add to project,modify rights
        /// </summary>
        /// <param name="win"></param>
        /// <param name="owner"></param>
        public void OnShowNxlConvertWin(out NxlFileToConvertWindow win, Window owner, string winTag = null)
        {
            win = null;
            try
            {
                win = new NxlFileToConvertWindow();
                win.Tag = winTag;
                if (owner != null)
                {
                    win.Owner = owner;
                    win.ShowDialog();
                }
                else
                {
                    win.Topmost = true;
                    win.Show();
                    win.Activate();
                    win.Focus();
                }
            }
            catch (Exception e)
            {
                log.Fatal("Error:OnShowShareNxlWin", e);
                app.ShowBalloonTip(CultureStringInfo.Common_System_Internal_Error);
            }
        }

        /// <summary>
        /// For offline nxlFile, add to project, modifyRights
        /// </summary>
        /// <param name="config"></param>
        /// <param name="owner"></param>
        private void OnShowNxlConvertWin(ProtectAndShareConfig config, Window owner, string nxlLocalPath)
        {
            try
            {
                string winTag = config.FileOperation.Action.ToString()+"|"+ nxlLocalPath;
                Window opennedWin = GetOpennedWindow(winTag);
                if (opennedWin == null)
                {
                    // Set MainWindow cursor state,because the NxlFileToConvertWindow initialization takes time 
                    this.mw.Cursor = System.Windows.Input.Cursors.Wait;

                    Window win = new NxlFileToConvertWindow(config);
                    win.Tag = winTag;

                    this.mw.Cursor = System.Windows.Input.Cursors.Arrow;

                    if (owner != null)
                    {
                        win.Owner = owner;
                        win.ShowDialog();
                    }
                    else
                    {
                        win.Topmost = true;
                        win.Show();
                        win.Activate();
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
                log.Fatal("Error:OnShowShareNxlWin", e);
                app.ShowBalloonTip(CultureStringInfo.Common_System_Internal_Error);
            }
        }

        public void OnExtractContent(string paramdetail)
        {
            SkydrmLocalApp skydrmLocalApp = SkydrmLocalApp.Singleton;
    
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

                    ExtractContentHelper.DecryptFile(skydrmLocalApp, nxlFileLocalPath, out decryptedFilePath);
     
                    bool isExtractContentSuccess = ExtractContentHelper.MoveFile(skydrmLocalApp, decryptedFilePath, DestinationPath);

                    ExtractContentHelper.SendLog(nxlFileLocalPath,NxlOpLog.Decrypt, isExtractContentSuccess);

                }
                catch (Exception ex)
                {
                    ExtractContentHelper.SendLog(nxlFileLocalPath, NxlOpLog.Decrypt, false);
                    skydrmLocalApp.ShowBalloonTip("Extract Contents Failed.");
                }
            }
            else
            {
                FileInfo fileInfo = new FileInfo(paramdetail);

                if (fileInfo.Exists && fileInfo.Length > 16384)// 16384 is file head length
                {
                    bool isCanceled;

                    bool isExtractContentSuccess = ExtractContentHelper.ExtractContent(skydrmLocalApp, skydrmLocalApp.MainWindow, paramdetail,out isCanceled);

                    if (!isCanceled)
                    {
                        ExtractContentHelper.SendLog(paramdetail, NxlOpLog.Decrypt, isExtractContentSuccess);                   
                    }
                }
                else
                {
                    skydrmLocalApp.ShowBalloonTip("Extract Contents Failed, File Not Exists.");
                }
            }     
        }

        public void OnModifyNxlFileRights(string nxlFilePath,INxlFile nxlFile = null, Window owner = null)
        {
            // Fix bug 54374
            if (!app.MainWin.viewModel.IsNetworkAvailable)
            {
                app.ShowBalloonTip(CultureStringInfo.Common_NxLFile_Rights_Cannot_Modified);
                return;
            }

            app.Log.Info("OnModifyNxlFileRights:");

            IModifyRights modifyRights = new ModifyRightsFeature(nxlFilePath, nxlFile, false);

            ProtectAndShareConfig config = null;
            if (modifyRights.GetRights(out config))
            {
                if ( !config.IsAdHoc)
                {
                    config.ModifyRightsFeature = modifyRights;
                    OnShowNxlConvertWin(config, owner, nxlFilePath);
                }
                else
                {
                    app.Log.Info("GetRights: is not Admin");
                    app.ShowBalloonTip(CultureStringInfo.Common_Not_Authorized);
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
                        SkydrmLocalApp.Singleton.MainWin.viewModel.EditFromViewer(nxlFileLocalPath);
                    } //fix bug 53660 edit office local file can't launch office process
                    else if(File.Exists(nxlFileLocalPath) && new FileInfo(nxlFileLocalPath).Length > 0)  // require file exist and file size >0    
                    {
                        // explorer edit
                        FileEditorHelper.DoEdit(nxlFileLocalPath, (EditCallBack EditCallBack) => {

                        });
                    }
                }
                else if (File.Exists(ParamDetail) && new FileInfo(ParamDetail).Length > 0)  // require file exist and file size >0    
                {
                    // explorer edit
                    FileEditorHelper.DoEdit(ParamDetail, (EditCallBack EditCallBack) => {

                    });
                }
            }
            catch (Exception ex)
            {
                SkydrmLocalApp.Singleton.Log.Error(ex.Message.ToString(), ex);
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
                app.ShowBalloonTip(CultureStringInfo.Exception_ExportFeature_Succeeded + p.DestinationPath + ".");
            }
            catch (Exception e)
            {
                app.Log.Warn(e.Message, e);
                // show buble to notify user
                app.ShowBalloonTip(CultureStringInfo.Exception_ExportFeature_Failed);
            }
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
