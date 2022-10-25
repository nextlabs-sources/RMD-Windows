using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.Shell;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Skydrmlocal.rmc.database2;
using SkydrmLocal.rmc;
using SkydrmLocal.rmc.app;
using SkydrmLocal.rmc.app.process;
using SkydrmLocal.rmc.common.component;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.database.table.systembucket;
using SkydrmLocal.rmc.database2;
using SkydrmLocal.rmc.database2.manager;
using SkydrmLocal.rmc.drive;
using SkydrmLocal.rmc.Edit;
using SkydrmLocal.rmc.exception;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.featureProvider.LocalFile;
using SkydrmLocal.rmc.featureProvider.RecentTouchFile;
using SkydrmLocal.rmc.featureProvider.SystemProject;
using SkydrmLocal.rmc.fileSystem.external;
using SkydrmLocal.rmc.helper;
using SkydrmLocal.rmc.namePipesServer;
using SkydrmLocal.rmc.process;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui;
using SkydrmLocal.rmc.ui.pages.model;
using SkydrmLocal.rmc.ui.utils;
using SkydrmLocal.rmc.ui.windows;
using SkydrmLocal.rmc.ui.windows.mainWindow.helper;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.ui.windows.mainWindow.view;
using SkydrmLocal.rmc.ui.windows.mainWindow.viewModel;
using SkydrmLocal.rmc.ui.windows.serviceManagerWindow.viewModel;
using static SkydrmLocal.rmc.ui.windows.CustomMessageBoxWindow;

namespace SkydrmLocal.rmc
{
    public partial class SkydrmLocalApp : Application/*, ISingleInstanceApp*/
    {
        // every other class object can directly use this avoid creating a same one
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        // use the same ID in all process, if you want all windows owned by different processes 
        // displayed in the same group button in Taskbar
        private static string AppID = "Nextlabs.Rmc.SkyDRM.LocalApp"; 

        public static bool clickEventfromNotifyIcon = false;
        public static bool IsOpenSM = false;

        // Flag that user request logout operation
        public bool IsRequestLogout = false;
        // Flag that if has poppup session invalid dlg
        private bool isPopUpSessionExpirateDlg = false;
        public bool IsPopUpSessionExpirateDlg
        {
            get { return isPopUpSessionExpirateDlg; }
            set { isPopUpSessionExpirateDlg = value; }
        }

        #region AppLevelComponents
        public static SkydrmLocalApp Singleton { get => (SkydrmLocalApp)SkydrmLocalApp.Current; }

        private AppConfig config;
        private ServiceManager sm;
        private MainWindow mw;
        private Session session;
        private UIMediator mediator;
        private HeartBeater heartbeater;
        private FunctionProvider dbProvider;
        public Session Rmsdk { get => session; }

        public AppConfig Config { get => config; }

        public log4net.ILog Log { get => SkydrmLocalApp.log; }

        public UIMediator Mediator { get => mediator; }

        public FunctionProvider DBFunctionProvider { get => dbProvider; }

        public TrayIconManager TrayIconMger { get; set; }

        public MainWindow MainWin { get => mw; set => mw = value; }

        public ServiceManager ServiceManager { get => sm; }

        public IExternalMgr ExternalMgr { get; set; }

        #endregion
        
        #region Feature_Provider
        private IUser user;
        public IUser User { get => user; }
        private ISystemProject systemProject;
        public ISystemProject SystemProject { get => systemProject; }
        private IMyProjects myProjects;
        public IMyProjects MyProjects { get => myProjects; }
        private IRecentTouchedFiles myRecentTouchedFile;
        public IRecentTouchedFiles UserRecentTouchedFile { get => myRecentTouchedFile; }
        private IMyVault myVault;
        public IMyVault MyVault { get => myVault; }
        private ISharedWithMe feature_sharedWithMe;
        public ISharedWithMe SharedWithMe { get => feature_sharedWithMe; }
        private ILocalFile Feature_LocalFile;
        public ILocalFile MyLocalFile { get => Feature_LocalFile; }
        #endregion

        #region Event
        // detected RMS changed myVaultQuata
        public event Action MyVaultQuataUpdated;
        public void InvokeEvent_MyVaultQuataUpdated()
        {
            MyVaultQuataUpdated?.Invoke();
        }

        // detected RMS changed userName
        public event Action UserNameUpdated;
        public void InvokeEvent_UserNameUpdated()
        {
            UserNameUpdated?.Invoke();
        }

        public event Action MyVaultFileOrSharedWithMeLowLevelUpdated;
        public void InvokeEvent_MyVaultOrSharedWithmeFileLowLevelUpdated()
        {
            MyVaultFileOrSharedWithMeLowLevelUpdated?.Invoke();
        }


        // Project user remove -- should notify automatically ui to refresh when heartbeat detect.
        public event Action ProjectUpdate;
        public void InvokeEvent_ProjectUpdate()
        {
            Log.Info("Invoke project update event");
            ProjectUpdate?.Invoke();
        }
            
        #endregion

        #region CommandLine
        // Callback when second instance get started, and the first is still running,
        // Good Point to handle second command line here
        public bool SignalExternalCommandLineArgs(IList<string> args)
        {
            // Added feature when user still not login, 
            // just parse command line, it will store outside client's indent
            // and defer to handle it after user has logined.

            if (null!= session && session.User == null)
            {
                bool isSplash = false;

                foreach (Window w in Current.Windows)
                {
                    if (w.GetType() == typeof(SplashWindow))
                    {
                        isSplash = true;
                        w.Show();
                        w.Activate();
                        w.Focus();
                        w.WindowState = WindowState.Normal;
                        break;
                    }
                }
                if (!isSplash)
                {
                    //Fix bug 52794
                    bool parsed1 = CommandParser.Parse(args.ToArray()); // store cmd-line
                    if(parsed1)
                    {
                        mediator.OnShowLogin(); // deter handle cmd until user has signed in
                    }
                    else
                    {
                        //Handle failed situation that file not exists.
                        //Hint people detail info
                        Ballon_FileNotExistsIfNecessary(CommandParser.Path);
                        return false;
                    }
                }
                return true;
            }

            // handle command line arguments of second instance
            bool parsed2 = CommandParser.Parse(args.ToArray());
            if (parsed2)
            {
                Handle_CommandLine();
            }
            else
            {
                //Handle failed situation that file not exists.
                //Hint people detail info
                Ballon_FileNotExistsIfNecessary(CommandParser.Path);
                return false;
            }
            return true;
        }

        public void Ballon_FileNotExistsIfNecessary(string path)
        {
            //Handle situation that file path get from command is empty.
            if (string.IsNullOrEmpty(path))
            {
                ShowBalloonTip(CultureStringInfo.CommandParse_Path_Empty);
                return;
            }
            //Get file name from path if exists.
            string filename = path.Substring(path.LastIndexOf('\\') + 1);
            //Handle situation that file is not exists.
            if (!File.Exists(path))
            {
                ShowBalloonTip(CultureStringInfo.CommandParse_File_Not_Found);
                return;
            }
            //Handle situation that file size is zero.
            if (new FileInfo(path).Length == 0)
            {
                ShowBalloonTip(CultureStringInfo.CommandParse_File_Size_Zero);
            }
        }

        public void Handle_CommandLine()
        {
            try
            {
                if (CommandParser.IsProtect)
                {
                    mediator.OnShowProtect(CommandParser.Path);
                }
                else if (CommandParser.IsShare)
                {
                    // share plain file or Nxl?
                    if (CommandParser.Path.EndsWith(".nxl", StringComparison.CurrentCultureIgnoreCase))
                    {
                        // reshare nxl file
                        //mediator.OnShowShareNxl(CommandParser.Path, null);
                        mediator.OnShareNxlToPerson(CommandParser.Path);
                    }
                    else
                    {
                        // share plain file
                        mediator.OnShowShare(CommandParser.Path);
                    }
                }
                else if (CommandParser.IsView)
                {
                    mediator.OnViewNxl(CommandParser.Path);
                }
                else if (CommandParser.IsShowMain)
                {
                    mediator.OnShowMain(null);
                }
                else if (CommandParser.IsShowFileInfo)
                {
                    mediator.OnShowFileInfo(CommandParser.ParamDetail);
                }
                else if (CommandParser.IsAddNxlLog)
                {
                    this.User.AddNxlFileLog(CommandParser.ParamDetail);
                }
                else if (CommandParser.IsEdit)
                {
                    mediator.TryEdit(CommandParser.ParamDetail);
                }
                else if (CommandParser.IsExport)
                {
                    mediator.OnExportFileDialog(CommandParser.ParamDetail);
                }         
                else if (CommandParser.IsOpenWeb)
                {
                    CommonUtils.OpenSkyDrmWeb();
                }
                else if (CommandParser.IsAddFileToProject)
                {
                    mediator.OnAddNxlFileToProject(CommandParser.Path);
                }
                else if (CommandParser.IsModifyRights)
                {
                    mediator.OnModifyNxlFileRights(CommandParser.Path);
                }
                else if (CommandParser.IsExtractContent)
                {
                    mediator.OnExtractContent(CommandParser.ParamDetail);
                }
                else
                {
                    mediator.OnShowMain(null);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
                //throw;
            }
            finally
            {
                CommandParser.Reset();
            }

        }

        #endregion

        // Used to communicate with explorer plugin.
        public ExplorerNamedPipeServer PipeServer;

        #region WPFDefined
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // if log init ok, it will record "app.init" in InitComponents();
            // Win-shell requried for windwos owned by different process can be grouped in one group.
            Win32Common.SetCurrentProcessExplicitAppUserModelID(AppID);
            if (!InitComponents())
            {
                log.Fatal("Failed init components");
                MessageBox.Show("Failed to launch application. Please contact your system administrator for further help.",
                    "SkyDRM DESKTOP",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Environment.Exit(1);
            }

            Log.Info("Success for initializing system components,Next to initialize App level component.");

            // parse commandline and story user intent
            CommandParser.Parse(e.Args);

            OnAppInitialized();

            Log.Info("Launch NamedPipeServer in Application_Startup");
            NamedPipesServer.Launch();

            
            PipeServer = new ExplorerNamedPipeServer(); 
            PipeServer.StartServer();

           
        }
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            ViewerProcess.KillAllActiveViewer();

            NamedPipesServer.ShudownActivePipes();

            PipeServer.StopServer();

            PrintProcess.Kill();

            log.Info("Application_Exit");
            // release all used res here
            try
            {
                if (TrayIconMger != null)
                {
                    TrayIconMger.ni.Dispose();
                }
            }
            catch (Exception ee)
            {
                log.Error("error in Application_Exit", ee);
            }

            try
            {
                if (session != null)
                {
                    session.SaveSession(config.RmSdkFolder);
                    session.DeleteSession();
                }
            }
            catch (Exception ee)
            {
                log.Error("error in Application_Exit", ee);
            }

            // comments by osmond, this is temp work around method to kill self process forcefully
            // the reason is that we may launch namedpipe which may denied process kill it self.
            Process.GetCurrentProcess().Kill();
        }
        #endregion

        #region GlobalFuncs

        public void OnShowAppHelpInformation()
        {
            try
            {
                string executable = Config.AppPath;
                int index = executable.LastIndexOf("\\");
                string binPath = executable.Substring(0, index);

                int index2 = binPath.LastIndexOf("\\");
                string skydrmPath = binPath.Substring(0, index2);

                string helpPath = string.Format(@"{0}\help\index.html", skydrmPath);

                if (!FileHelper.Exist(helpPath))
                {
                    helpPath = Constant.HELP_PAGE;
                }
                Process.Start(helpPath);
            }
            catch (Exception e)
            {
                Log.Warn("Error occured when invke open Menu_Help:\n", e);
            }
        }

        public void OnUserLogin()
        {
            Log.Info("Prepare User session");
            //
            // new user features provdiers 
            //
            user = new featureProvider.User.User(dbProvider.GetUser());

          //  we do not nedd add RPM dir, use SDK's RPM folder uniformly
          //  Log.Info("Prepare for add RPM dir");
          //  RightsManagementService.AddRPMDir(session,user.RPMFolder);

            myProjects = new MyProjects(this);
            myRecentTouchedFile = new MyRecentTouchedFiles(this);
            myVault = new featureProvider.MyVault.MyVault(this);
            feature_sharedWithMe = new featureProvider.SharedWithMe.SharedWithMe(this);
            Feature_LocalFile = new LocalFile(this);
            InitSystemBucket();

            Process currentProcess = Process.GetCurrentProcess();

            this.Rmsdk.RPM_RegisterApp(currentProcess.MainModule.FileName);
            this.Rmsdk.SDWL_RPM_NotifyRMXStatus(true);

            //
            // Init Main Window, May takes a few seconds sometimes, will optimize later.
            //
            Log.Info("Init Main Window");
            mw = new MainWindow();

            //
            // Init Service Manager
            //
            Log.Info("Init ServiceManager Window");
            sm = new ServiceManager();
            //myRecentTouchedFile.Notification +=new FileStatusHandler(sm.viewModel.OnFileStatusChanged);

            // Init External mgr.
            ExternalMgr = new ExternalMgrImpl();

            // Init icon mgr.
            TrayIconMger.IsLogin = true;
            TrayIconMger.RefreshMenuItem();
            TrayIconMger.PopupTargetWin = sm;

            // init logout flag.
            IsRequestLogout = false;

            //
            // after user has succeffully logined ,we must check command line to find user intend
            //

            Handle_CommandLine();

            // fire heartbeart for this user
            this.heartbeater = new HeartBeater(this);
            this.heartbeater.WorkingBackground();

  
        }

        private void InitSystemBucket()
        {
            SystemBucket raw = null;
            try
            {
                // first time, db can not find value,
                raw = DBFunctionProvider.GetSystemBucket();
            }
            catch (Exception e)
            {
                Log.Warn(e.Message, e);
            }

            if (raw == null)
            {
                raw = SystemBucket.NewDefault();
            }

            systemProject = new SystemProject(raw);
        }
    

        public bool MaunalExit()
        {
            bool result = true;

            if (FileEditorHelper.IsbeingFileEdit())
            {
                this.ShowBalloonTip(CultureStringInfo.Common_ForbidLogoutExit_WhenEditing);
                result = false;
            }
            else
            {
                this.Shutdown(0);
            }
            return result;
        }

        public void Logout(Window win = null,bool IsSessionExpiration=false)
        {
            Log.Info("User logout session");

            if (FileEditorHelper.IsbeingFileEdit() && !IsSessionExpiration)
            {
                this.ShowBalloonTip(CultureStringInfo.Common_ForbidLogoutExit_WhenEditing);
                return;
            }

            IsRequestLogout = true;

            ViewerProcess.KillAllActiveViewer();

            NamedPipesServer.ShudownActivePipes();

            PipeServer.StopServer();

            PrintProcess.Kill();


            // we used SDK's RPM folder uniform ,so we do not need call remove rpm folder
            //try
            //{
            //    session.RPM_RemoveDir(user.SDkWorkingFolder);
            //}
            //catch (Exception)
            //{
            //    Log.Info("Invoke RemoveRPMDir Error");
            //}

            try
            {
                // update db
                DBFunctionProvider.OnUserLogout();
                heartbeater.Stop = true;
                session.SaveSession(config.RmSdkFolder);
                session.User.Logout(); // will connect server.
                session.DeleteSession();
                session = null;
            }
            catch (Exception e)
            {
                Log.Info("app.Session.User.Logout is failed, msg:"+e.Message,e);
            }
            finally
            {
                if (!config.ClearUserInfo())
                {
                    Log.Info("Clear user info failed.");
                }
            }

            // Now directly exit app after logout according to Raymond advice. --- fix bug 52648.        
            this.MaunalExit();

            // init tray icon.
            //TrayIconMger.IsLogin = false;
            //TrayIconMger.RefreshMenuItem();

            //try
            //{
            //    // Means an application shuts down only when Shutdown is called, or else app will exit when close all windows.
            //    this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            //    // try to close all windows.
            //    foreach (Window one in SkydrmLocalApp.Current.Windows)
            //    {
            //        if (one != null)
            //        {
            //            one.Close();
            //        }
            //    }
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine(e.ToString());
            //}

            //// create a new clean session 
            //session = rmc.sdk.Apis.CreateSession(config.RmSdkFolder);

            //// login
            //Mediator.OnShowLogin();

            //// reset default after loading login.
            //this.ShutdownMode = ShutdownMode.OnLastWindowClose;

        }
        // Easy use func, to show msg in bubble in right-lower corner of windows explorer 
        public void ShowBalloonTip(string text, int timeout = 1000)
        {
            if (TrayIconMger != null)
            {
                TrayIconMger.ni.BalloonTipText = text;
                TrayIconMger.ni.ShowBalloonTip(timeout);
            }
        }
        #endregion

        #region Privates

        // this is called when all the app needed resource has loaded
        private void OnAppInitialized()
        {
            try
            {
                session.Initialize(config.RmSdkFolder, config.UserRouter, config.UserTenant);
                Log.Info("Try to recover last session for the user " + config.UserEmail);
                if (session.RecoverUser(config.UserEmail, config.UserCode))
                {
                    session.User.UserId = (uint)DBFunctionProvider.OnUserRecovered(session.User.Email, config.UserRouter, config.UserTenant);
                    Log.Info("Recover user " + session.User.Email + "ok");
                    OnUserLogin();
                }
                else
                {
                    Log.Info("Failed recover user, direct to Splash UI");
                    // reset default after loading login.
                    this.ShutdownMode = ShutdownMode.OnLastWindowClose;
                    this.StartupUri = new Uri("/rmc/ui/windows/SplashWindow.xaml", UriKind.RelativeOrAbsolute);
                }
                return;
            }catch(RmSdkException sdkE)
            {
                log.Error(sdkE.LogUsedMessage(), sdkE);
            }
            catch (Exception e)
            {
                log.Error(e);
            }
            Log.Info("recovery user failed, request login and show splash;" );
            // for any Exception occurrs we regard it as Recover User Failed, show Normal
            this.ShutdownMode = ShutdownMode.OnLastWindowClose;
            this.StartupUri = new Uri("/rmc/ui/windows/SplashWindow.xaml", UriKind.RelativeOrAbsolute);
        }
        private bool InitComponents()
        {
            // UI ATTRs
            ResourceDictionary resourceDictionary = new ResourceDictionary();
            resourceDictionary.Source = new Uri("Themes/Generic.xaml", UriKind.Relative);
            this.Resources = resourceDictionary;

            try
            {
                // Init Log first
                log4net.Config.XmlConfigurator.Configure();
                log.Info("App.init");

                // Init Config, store all Const_vars thats are used in APP running
                config = new AppConfig();
                // added requirment, protect some folder                
                //FileHelper.ProtectFolder(config.DataBaseFolder, config.IsFolderProtect);
                //FileHelper.ProtectFolder(config.RmSdkFolder, config.IsFolderProtect);               
                // Init RMCSDK
                session = rmc.sdk.Apis.CreateSession(config.RmSdkFolder);
                // Init UI mediator
                mediator = new UIMediator(this);

                TrayIconMger = new TrayIconManager(this);

                dbProvider = new FunctionProvider(Config.DataBaseFolder);

                return true;
            }
            catch (Exception e)
            {
                log.Error("Init Compoent Failed", e);
                // if recover failed, seesion.user == null; 
            }
            // normal flow should never reach here
            return false;
        }
        #endregion
    }
}
