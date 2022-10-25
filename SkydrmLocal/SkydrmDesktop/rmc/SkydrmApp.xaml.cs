using SkydrmDesktop.Resources.languages;
using SkydrmDesktop.rmc.app;
using SkydrmDesktop.rmc.common.helper;
using SkydrmDesktop.rmc.featureProvider.MessageNotify;
using Skydrmlocal.rmc.database2;
using SkydrmLocal;
using SkydrmLocal.rmc.app.process;
using SkydrmLocal.rmc.common.component;
using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.database.table.systembucket;
using SkydrmLocal.rmc.Edit;
using SkydrmLocal.rmc.featureProvider;
using SkydrmLocal.rmc.featureProvider.SystemProject;
using SkydrmLocal.rmc.fileSystem.external;
using SkydrmLocal.rmc.namePipesServer;
using SkydrmLocal.rmc.process;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui.utils;
using SkydrmLocal.rmc.ui.windows.mainWindow.helper;
using SkydrmLocal.rmc.ui.windows.mainWindow.view;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using SkydrmDesktop.rmc.featureProvider;
using SkydrmDesktop.rmc.featureProvider.WorkSpace;
using System.Text;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.fileSystem.basemodel;
using System.Net.Http;
using SkydrmDesktop.rmc.featureProvider.externalDrive.RmsRepoMgr;
using SkydrmLocal.rmc.fileSystem;

namespace SkydrmDesktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class SkydrmApp : Application
    {
        // Log
        private static readonly log4net.ILog log = log4net.LogManager.
            GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // Specifies a unique application-defined Application User Model ID (AppUserModelID), that identifies the current process to the taskbar.
        // This identifier allows an application to group its associated processes and windows under a single taskbar button.  
        private static string AppID = "Nextlabs.Rmc.SkyDRM.LocalApp";

        // Command line para when request login
        private static string ReqLoginCmdPara = "-RequestLogin";

        // Flag that if recover session succeed.
        private bool bRecoverSucceed = true;

        /// <summary>
        /// Flag that explorer right click command is triggered under the case that logout or RMD exit.
        /// For this case, rmd process will start but can't display main window, so should show then hide,
        /// or else, main window can't receive logout msg when execute logout.
        /// </summary>
        private bool bIsCommandSentByExplorer = false;

        // Flag that if has poppup session invalid dlg
        public bool IsPopUpSessionExpirateDlg { get; set; }

        // Flag that if RMD is launching.
        // Fix bug that RMD is launching after the first login, then user immediately click "Open Skydrm desktop" again by nxrmtray.
        public bool IsRMDLaunching { get; set; }

        public bool IsEnableExternalRepo { get; } = true;

        public bool IsPersonRouter { get; private set; }

        #region AppLevelComponents
        public static SkydrmApp Singleton { get => (SkydrmApp)Current; }

      //  private System.Net.Http.HttpClient mHttpClient = new System.Net.Http.HttpClient(new HttpClientHandler() { UseProxy = false });
        private Session session;
        private UIMediator mediator;
        private HeartBeater heartbeater;
        public Session Rmsdk { get => session; }
        public AppConfig Config { get; private set; }
        public log4net.ILog Log { get => SkydrmApp.log; }
        public UIMediator Mediator { get => mediator; }
        public FunctionProvider DBFunctionProvider { get; private set; }
        public MainWindow MainWin { get; set; }
        public IExternalMgr ExternalMgr { get; set; }

      //  public HttpClient HttpClient { get => mHttpClient; }

        #endregion // AppLevelComponents

        #region Feature_Provider
        public IUser User { get; private set; }
        public ISystemProject SystemProject { get; private set; }
        public IMyProjects MyProjects { get; private set; }
        public rmc.featureProvider.IMessageNotify MessageNotify { get; private set; }
        public IMyVault MyVault { get; private set; }
        public IMyDrive MyDrive { get; private set; }
        public IWorkSpace WorkSpace { get; private set; }
        public ISharedWithMe SharedWithMe { get; private set; }
        public IRmsRepoMgr RmsRepoMgr { get; private set; }
        #endregion // Feature_Provider

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

        public event NotifyRefreshFileListView NotifyRefreshFileListView;
        // Note: for project, the 'pathId' parameter is prefix projectId add pathId.
        // for external repo, the 'pathId' parameter is repo id add pathid.
        public void InvokeEvent_NotifyRefreshFileListView(string repoName, IList<INxlFile> syncResults, string pathId)
        {
            NotifyRefreshFileListView?.Invoke(repoName, syncResults, pathId);
        }

        public event NotifyRefreshProjectListView NotifyRefreshProjectListView;
        public void InvokeEvent_NotifyRefreshProjectListView(List<SkydrmLocal.rmc.fileSystem.project.ProjectData> addProjects, 
            List<SkydrmLocal.rmc.fileSystem.project.ProjectData> removeProjects)
        {
            Log.Info("Invoke notify refresh project listview when refresh treeview 'Project' item.");
            NotifyRefreshProjectListView?.Invoke(addProjects, removeProjects);
        }

        public event NotifyRefreshExternalRepoListView NotifyRefreshExternalRepoListView;
        public void InvokeEvent_NotifyRefreshExternalRepoListView(List<IFileRepo> addList,
            List<IFileRepo> removeList)
        {
            Log.Info("Invoke notify refresh externalRepo listview when refresh treeview 'REPOSITORIES' item.");
            NotifyRefreshExternalRepoListView?.Invoke(addList, removeList);
        }

        #endregion // Event

        #region CommandLine
        public void Handle_CommandLine()
        {
            try
            {
                if (CommandParser.IsProtect)
                {
                    mediator.OnShowOperationProtectWinByPlugIn(CommandParser.Path, CommandParser.ParamDetail);
                }
                else if (CommandParser.IsShare)
                {
                    // share plain file or Nxl?
                    if (CommandParser.Path.EndsWith(".nxl", StringComparison.CurrentCultureIgnoreCase))
                    {
                        // reshare nxl file
                        mediator.OnShowOperationUpdateRecipiWinByPlugIn(CommandParser.Path);
                    }
                    else
                    {
                        // share plain file
                        mediator.OnShowOperationShareWinByPlugIn(CommandParser.Path);
                    }
                }
                else if (CommandParser.IsView)
                {
                    //mediator.OnViewNxl(CommandParser.Path);
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
                    OpenSkyDrmWeb();
                }
                else if (CommandParser.IsReShare)// Viewer project file do share
                {
                    mediator.OnShowOperationReShareWin(CommandParser.Path);
                }
                else if (CommandParser.IsAddFileToProject)
                {
                    mediator.OnShowOperationAddNxlWinByPlugIn(CommandParser.Path);
                }
                else if (CommandParser.IsModifyRights)
                {
                    mediator.OnShowOperationModifyRightWinByPlugIn(CommandParser.Path);
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

        // Callback when second instance get started, and the first is still running,
        // Good Point to handle second command line here
        public void SignalExternalCommandLineArgs(IList<string> args)
        {
            if (IsRMDLaunching)
            {
                Log.Info("IsRMDLaunching value is true, can't excute command line.");
                return;
            }

            string msg = $"SignalExternalCommandLineArgs: args count {args.Count}, args value {string.Join(";", args.ToArray())}";
            Log.Info(msg);
            bool isParsed = CommandParser.Parse(args.ToArray());
            if (isParsed)
            {
                Handle_CommandLine();
            }
            else
            {
                Log.Warn("Command line args parse failed");
            }
        }

        #endregion // CommandLine

        #region WPF Defined
        private bool ShowDebugDialog()
        {
            bool result = false;
            try
            {
                Microsoft.Win32.RegistryKey localApp = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Nextlabs\SkyDRM\LocalApp");
                int debugValue = (int)localApp.GetValue("Debug", 0);
                if (debugValue == 1)
                {
                    result = true;
                }
            }
            catch (Exception)
            { }
            return result;
        }
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            IsRMDLaunching = true;

            if (ShowDebugDialog())
            {
                MessageBox.Show("Debug nxrmdapp!");
            }
            try
            {
                log.Info("Enter --> Application_Startup");

                Log.Info("Application_Startup: init sdk");
                Apis.SdkLibInit();

                // If log init ok, it will record "app.init" in InitComponents();
                // Win-shell requried for windwos owned by different process can be grouped in one group.
                Win32Common.SetCurrentProcessExplicitAppUserModelID(AppID);

                // Init Log first
                log4net.Config.XmlConfigurator.Configure();

                // 
                // Here check if is login startup or not.
                // ----Note: Must get the Request Login command paras in this way.This command is sent by nxrmserv.exe.
                // 
                string[] cmdArgs = Environment.GetCommandLineArgs();

                Log.Info("Application_Startup: cmdArgs length:"+ cmdArgs.Length);

                // Request login, need to pass cmd para: "-RequestLogin"
                if (cmdArgs.Length > 0 && cmdArgs[0] == ReqLoginCmdPara)
                {
                    // Do normal login only.
                    if (cmdArgs.Length == 1)
                    {
                        // Login startUp
                        StartUpByLogin(e.Args);
                    }

                    // the command sent by explorer plugin(such as: protect\share) in un-login scenario.
                    else if (cmdArgs.Length > 1)
                    {
                        bIsCommandSentByExplorer = true;

                        // Extract explorer plugin cmd line paras exclude "ReqLoginCmdPara" para,
                        // so the index begins from 1, since index 0 indicates "ReqLoginCmdPara" para.
                        List<string> cmds = new List<string>();
                        for (int i = 1; i < cmdArgs.Length; i++)
                        {
                            cmds.Add(cmdArgs[i]);
                        }

                        StartUpByLogin(cmds.ToArray());
                    }
                }
                else
                {
                    // The assignment here is confusing. 
                    // If user double click nxrmdapp.exe or click open MainWin in nxrmtray.exe, it will all be excuted here.
                    // And it will also hide MainWindow, then show the MainWindow in Handle_CommandLine() method. 
                    bIsCommandSentByExplorer = true;

                    // Try to recover session to startup
                    StartUpByRecover(e.Args);
                }

                //log.Fatal("Leave --> Application_Startup");
            }
            catch (Exception msg)
            {
                log.Error("Error in Application_Startup:",msg);
                throw;
            }
            
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (!bRecoverSucceed)
            {
                Log.Info("Application_Exit: RecoverSucceed is false");
                // this is temp work around method to kill self process forcefully
                Process.GetCurrentProcess().Kill();
                return;
            }

            ViewerProcess.KillAllActiveViewer();

            NamedPipesServer.ShudownActivePipes();

            PrintProcess.Kill();

            log.Info("Application_Exit");

            // release all used res here
            try
            {
                if (session != null)
                {
                    session.SaveSession(Config.RmSdkFolder);
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

        /// <summary>
        /// Will be triggered when windows session ends, such as user logout or shutdown the PC.
        /// </summary>
        protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
        {
            base.OnSessionEnding(e);

            Log.Info("OnSessionEnding");
        }

        #endregion // WPF Defined

        #region Global Funcs

        public void OpenSkyDrmWeb()
        {
            try
            {
                //string url = App.DBFunctionProvider.GetUrl();
                string url = Config.UserUrl;
                if (string.IsNullOrEmpty(url))
                {
                    url = "http://www.skydrm.com/";
                }
                System.Diagnostics.Process.Start(url);
            }
            catch (Exception e)
            {
                Log.Error("Error happend in Get Url:", e);
            }
        }

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

        /// <summary>
        /// Init repos and load main window
        /// </summary>
        public void StartLoading()
        {
            Log.Info("Enter --> StartLoading");

            // 
            // Init User from db.
            //
            Log.Info("Prepare User session");
            User = new SkydrmLocal.rmc.featureProvider.User.User(DBFunctionProvider.GetUser());

            // 
            // Set RPM folder
            //
            //Log.Info("Prepare for add RPM dir");
            //if (!session.RMP_IsSafeFolder(User.RPMFolder))
            //{
            //    RightsManagementService.AddRPMDir(session, User.RPMFolder);
            //}

            //
            // Initialize repositories.
            //
            MyProjects = new MyProjects(this);
            MessageNotify = new MessageNotify(this);
            WorkSpace = new WorkSpace(this);
            MyVault = new SkydrmLocal.rmc.featureProvider.MyVault.MyVault(this);
            MyDrive = new rmc.featureProvider.MySpace.MyDrive(this);
            SharedWithMe = new SkydrmLocal.rmc.featureProvider.SharedWithMe.SharedWithMe(this);
            RmsRepoMgr = new RmsRepoMgr(this);

            // 
            // Init Main Window
            //
            Log.Info("Init Main Window");
            MainWin = new MainWindow();
            MainWin.Show();

            // Workaround -- should show main window first, then hide it, or else can't receive 'Logout' broadcast message since wndproc is invalid.
            if (bIsCommandSentByExplorer)
            {
                // if MainWin is initializing, should display MainWindow.
                // The initialization usually occurs when there is no data in the local database
                if (!MainWin.viewModel.IsInitializing)
                {
                    MainWin.Hide();
                }
            }

            // init systemBucket repositories
            InitSystemBucket();

            //
            // Fire heartbeart for this user
            //
            this.heartbeater = new HeartBeater(this);
            this.heartbeater.WorkingBackground();

            // 
            // Register trusted process(here is app name - can hold all processes).
            //
            log.Info("Register trusted process");
            Process currentProcess = Process.GetCurrentProcess();
            this.Rmsdk.RPM_RegisterApp(currentProcess.MainModule.FileName);
            this.Rmsdk.SDWL_RPM_NotifyRMXStatus(true);

            // 
            // Init External mgr.
            //
            ExternalMgr = new ExternalMgrImpl();

            //
            // After user has succeffully logined ,we must check command line to find user intend
            //
            log.Info("Handle CommandLine.");
            Handle_CommandLine();


            log.Info("Reset IsRMDLaunching value to false");
            IsRMDLaunching = false;

            // Now Saas not support (myVault)token group instead of (systemBucket)defult token when protect file in external repo,
            // so rmd still use defult token when router is Saas, set IsPersonRouter = false
            //IsPersonRouter = IsSaasRouter();
            IsPersonRouter = false;

            /*
            AsyncHelper.RunAsync(()=> 
            {
                // init systemBucket repositories
                InitSystemBucket();

                //
                // Fire heartbeart for this user
                //
                this.heartbeater = new HeartBeater(this);
                this.heartbeater.WorkingBackground();


                // Wait for MainWindow initialization to complete
                while (MainWin.viewModel.IsInitializing)
                { }
                
                // 
                // Register trusted process(here is app name - can hold all processes).
                //
                Process currentProcess = Process.GetCurrentProcess();
                this.Rmsdk.RPM_RegisterApp(currentProcess.MainModule.FileName);
                this.Rmsdk.SDWL_RPM_NotifyRMXStatus(true);

                // 
                // Init External mgr.
                //
                ExternalMgr = new ExternalMgrImpl();

            }, ()=> 
            {
                //
                // After user has succeffully logined ,we must check command line to find user intend
                //
                log.Info("Handle CommandLine.");
                Handle_CommandLine();

                log.Info("Reset IsRMDLaunching value to false");
                IsRMDLaunching = false;

                // Now Saas not support (myVault)token group instead of (systemBucket)defult token when protect file in external repo,
                // so rmd still use defult token when router is Saas, set IsPersonRouter = false
                //IsPersonRouter = IsSaasRouter();
                IsPersonRouter = false;
            });
            */

            Log.Info("Leave --> StartLoading");
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

            SystemProject = new SystemProject(raw);
        }

        public void ManualExit()
        {
            try
            {
                Apis.SdkLibCleanup();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                this.Shutdown(0);
            }
        }


        public uint WM_CHECK_IF_ALLOW_LOGOUT = 50005; 
        public uint WM_START_LOGOUT_ACTION = 50006; 

        // Try to request to logout.
        public void RequestLogout(RequestLogoutOps operation)
        {
            bool isAllow = true;
            try
            {
                if(operation == RequestLogoutOps.CheckIfAllow)
                {
                    session.SDWL_RPM_RequestLogout(out isAllow, Convert.ToUInt32(RequestLogoutOps.CheckIfAllow));
                    if (isAllow)
                    {
                        session.SDWL_RPM_RequestLogout(out isAllow);
                        if (isAllow)
                        {
                            Log.Info("Execute logout action succeed.");
                        }
                    }
                    else
                    {
                        Log.Info("Deny logout now.");
                    }
                }
                else if(operation == RequestLogoutOps.ExecuteLogout)
                {
                    session.SDWL_RPM_RequestLogout(out isAllow);
                    if (isAllow)
                    {
                        Log.Info("Execute logout action succeed.");
                    }
                }
            }
            catch (Exception e)
            {
                this.Log.Info(e.ToString());
            }
        }

        // Execute inner(rmd) actual logout action. 
        public void InnerLogout()
        {
            Log.Info("User logout session");

            //ViewerProcess.KillAllActiveViewer();

            NamedPipesServer.ShudownActivePipes();

            PrintProcess.Kill();

            try
            {
                // update db
                DBFunctionProvider.OnUserLogout();
                heartbeater.Stop = true;
                session.SaveSession(Config.RmSdkFolder);

                // Now logout through service manager, rmd only do itself some logout task and then exit the process.
                //session.User.Logout(); // will connect server.

                session.DeleteSession();
                session = null;
            }
            catch (Exception e)
            {
                Log.Info("app.Session.User.Logout is failed, msg:" + e.Message, e);
            }

            // Now directly exit app after logout according to Raymond advice. --- fix bug 52648.        
            ManualExit();
        }

        /// <summary>
        /// Easy use func, to show msg in bubble in right-lower corner of windows explorer.
        /// And nxrmtray only use three parameters(msg, isPositive, fileName) to display message.
        /// </summary>
        /// <param name="msg">message details</param>
        /// <param name="isPositive">is positive or negative</param>
        /// <param name="fileName">the operated file name, call be empty if not specify some one.</param>
        /// <param name="operation">the detail operation if want to specify, or else can be empty.</param>
        /// <param name="fileStatusIcon">the icon type that indicates the file status.</param>
        public void ShowBalloonTip(string msg, bool isPositive, string fileName = "", string operation = "", 
            EnumMsgNotifyIcon fileStatusIcon = EnumMsgNotifyIcon.Unknown)
        {
            // Send log to service manager.
            if (isPositive)
            {
                this.MessageNotify.NotifyMsg(fileName, msg, EnumMsgNotifyType.PopupBubble, operation, EnumMsgNotifyResult.Succeed, fileStatusIcon);
            }
            else
            {
                this.MessageNotify.NotifyMsg(fileName, msg, EnumMsgNotifyType.PopupBubble, operation, EnumMsgNotifyResult.Failed, fileStatusIcon);
            }
        }
        #endregion // Global Funcs

        #region Private Funcs

        /// <summary>
        /// Build compound command line paras that sent by explorer plugin in un-login case.
        ///  For example: "-RequestLogin -protect C:\test\allen.txt".
        ///   -- means first request login, then execute protect operation.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private string BuildCmdParas(IList<string> args)
        {
            StringBuilder ret = new StringBuilder();
            ret.Append(ReqLoginCmdPara);
            ret.Append(" ");

            foreach(var i in args)
            {
                // Should add quotes if command parameter contains space.
                if(i.Contains(" "))
                {
                    var tmp = "\"" + i + "\"";
                    ret.Append(tmp);
                }else
                {
                    ret.Append(i);
                }

                ret.Append(" ");
            }

            return ret.ToString();
        }

        private void StartUpByRecover(IList<string> args)
        {
            if (!RecoverSession())
            {
                bRecoverSucceed = false;

                string exePath = Process.GetCurrentProcess().MainModule.FileName;

                // Recover session failed, try to request login, there are following two case:
                //
                // Case 1: Do normal login only.
                if (args != null && args.Count == 0) 
                {
                    Log.Info($"RPM_RequestLogin: cmdParas-> {ReqLoginCmdPara}");
                    session?.SDWL_RPM_RequestLogin(exePath, ReqLoginCmdPara);
                }

                // Case 2: The command sent by explorer plugin(such as: -protect\-share) in un-login scenario.
                else
                {
                    // Build 'cmdParas' like: "-RequestLogin -protect C:\test\allen.txt"
                    var cmdParas = BuildCmdParas(args);
                    Log.Info($"RPM_RequestLogin: cmdParas-> {cmdParas}");
                    session?.SDWL_RPM_RequestLogin(exePath, cmdParas);
                }

                // Will exit current process after send request login.
                log.Info("**** Now exit current process after send login request. ****");
                ManualExit();

                return;
            }

            if (!InitComponents())
            {
                log.Fatal("Failed init components");
            }

            //
            // If query user pk failed and return false, means this is one new user account(maybe logined by Service Manager),
            // for this case, will insert the new user into db.
            // 
            var user = Rmsdk.User;
            Tenant tenant = session.GetCurrentTenant();

            if (!DBFunctionProvider.QueryUserPK(session.User.Email, tenant.RouterURL, tenant.Name))
            {
                if (!string.IsNullOrEmpty(tenant.RouterURL))
                {
                    // Now the 'url' and 'isOnPremise' field can't get, so set the default value, it looks doesn't matter much.    
                    DBFunctionProvider.UpsertServer(tenant.RouterURL, "", tenant.Name, false);
                }

                DBFunctionProvider.UpsertUser((int)user.UserId,
                                    user.Name, user.Email, user.PassCode, (int)user.UserType, "");
            }

            // Init db and do pre-load.
            DBFunctionProvider.InitDb();

            // parse commandline and story user intent
            CommandParser.Parse(args.ToArray());

            // Start loading main window
            StartLoading();

            // Init pipe server.
            InitPipeServer();
        }

        private void StartUpByLogin(IList<string> args)
        {
            if (!RecoverSession())
            {
                // It looks that can't reach here, if reach here, means login failed, shoud prompt user.
                Log.Warn("Login failed!");

                MessageBox.Show("Login failed!");

                // Force exit for this case now.
                ManualExit();
            }

            if (!InitComponents())
            {
                log.Fatal("Failed init components");
            }

            CommandParser.Parse(args.ToArray());

            // Do some init in async task after login.
            DoInitAfterLogin();

            // Init pipe server.
            InitPipeServer();
        }

        private void DoInitAfterLogin()
        {
            // Display init progress.
            Mediator.IsShowInitializeWin(true);

            //
            // Do some initialization in async task.
            //
            Func<bool> asyncTask = new Func<bool>(() =>
            {
                try
                {
                    var user = Rmsdk.User;
                    Tenant tenant = session.GetCurrentTenant();

                    // Upsert server table 
                    if (!string.IsNullOrEmpty(tenant.RouterURL))
                    {
                        // Tell db weburl will be displayed -- using routerUrl and tenant as primary key intead of serverUrl -- fix bug 52730.
                        //
                        // Now the 'url' and 'isOnPremise' field can't get, so set the default value, it looks doesn't matter much.
                        //
                        DBFunctionProvider.UpsertServer(tenant.RouterURL, "", tenant.Name, false);
                    }

                    Log.Info("*******UserId*********: " + user.UserId);

                    // Upsert user table
                    DBFunctionProvider.UpsertUser((int)user.UserId,
                                        user.Name, user.Email, user.PassCode, (int)user.UserType, "");

                    // Init db and do pre-load when user login.
                    DBFunctionProvider.InitDb();

                    // Cleanup the sessions left
                    SkydrmLocal.rmc.fileSystem.external.Helper.cleanup_edit_mapping();

                    return true;
                }
                catch (Exception e)
                {
                    Log.Info(e.ToString());
                    return false;
                }
            });

            // Async callback
            Action<bool> callback = new Action<bool>((bool result) => {

                // Close init progress.
                Mediator.IsShowInitializeWin(false);

                if (result)
                {
                    // Start loading main window
                    StartLoading();
                }
                else
                {
                    Log.Error(CultureStringInfo.ApplicationFindResource("Common_Initialize_failed"));
                    ShowBalloonTip(CultureStringInfo.ApplicationFindResource("Common_Initialize_failed"), false);
                }
            });

            // Invoke
            AsyncHelper.RunAsync(asyncTask, callback);

        }

        private void InitPipeServer()
        {
            Log.Info("Launch NamedPipeServer in Application_Startup");
            NamedPipesServer.Launch();
        }

        // Recover session and user, and init rmSdk(Session & User).
        private bool RecoverSession()
        {
            if (Apis.GetCurrentLoggedInUser(out session))
            {
                log.Info(" RecoverSession Succeed!");
                return true;
            }
            else
            {
                log.Info(" RecoverSession Failed!");
                return false;
            }
        }

        private bool InitComponents()
        {
            log.Info("Enter --> InitComponents");

            try
            {              

                // Init Config, store all Const_vars thats are used in APP running
                Config = new AppConfig();

                // Init UI mediator
                mediator = new UIMediator(this);

                // Init db
                DBFunctionProvider = new FunctionProvider(Config.DataBaseFolder);
     
                log.Info("Leave --> InitComponents");
                return true;
            }
            catch (Exception e)
            {
                log.Error("Init Compoent Failed", e);
            }

            // normal flow should never reach here
            return false;
        }

        public bool IsSaasRouter()
        {
            bool result = false;

            if (Config.UserUrl.StartsWith(Config.PersonRouter, StringComparison.CurrentCultureIgnoreCase))
            {
                result = true;
            }
            return result;
        }
        #endregion // Private Funcs
    }

    public enum RequestLogoutOps
    {
        ExecuteLogout = 0,
        CheckIfAllow = 1
    }

}
