using Microsoft.Win32;
using System;
using System.Diagnostics;
using Alphaleonis.Win32.Filesystem;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using SkydrmLocal.rmc.sdk;
using Viewer.upgrade.database;
using System.Windows.Threading;
using Viewer.upgrade.application;
using System.Collections.Concurrent;
using log4net;
using Viewer.upgrade.session;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Viewer.upgrade.file.basic;
using Viewer.upgrade.utils;
using System.Windows.Input;
using Viewer.upgrade.communication.message;
using Viewer.upgrade.cookie;
using Viewer.upgrade.ui.common.viatalError.view;
using Viewer.upgrade.exception;
using Viewer.upgrade.ui.common.viewerWindow.view;

namespace Viewer
{
    public partial class ViewerApp : Application
    {
        public ConcurrentDictionary<string, ISession> Sessions => mSessions;
        public ILog Log => mLog;
        public SkydrmLocal.rmc.sdk.Session SdkSession => mSdkSession;
        public string RmSdkFolder => mRmSdkDir;
        public string WorkingFolder => mWorkingDir;
        public string Def_RPM_Folder => mDef_RPM_Dir;
        public UInt64 StatusCode => mStatusCode;
        public string DataBaseFolder => mDataBaseDir;
        public FunctionProvider FunctionProvider => DatabaseInitialize(); 
        public CancellationToken Token => mToken;
        public ConcurrentDictionary<string, string> Temp_RPM_Folders => mTempRPMFolders; 

        private string mAppID = "Nextlabs.Rmc.SkyDRM.LocalApp";
        private ConcurrentDictionary<string, ISession> mSessions = new ConcurrentDictionary<string, ISession>();
        private log4net.ILog mLog;
        private SkydrmLocal.rmc.sdk.Session mSdkSession;
        private string mRmSdkDir = string.Empty;
        private string mWorkingDir = string.Empty;
        private string mDef_RPM_Dir = string.Empty;
        private UInt64 mStatusCode = AppStatusCode.DEFAULT;
        private string mDataBaseDir = string.Empty;
        private volatile FunctionProvider mDbProvider;
        private CancellationToken mToken;
        private ConcurrentDictionary<string, string> mTempRPMFolders = new ConcurrentDictionary<string, string>();
        private CancellationTokenSource mTokenSource = new CancellationTokenSource();
        private static readonly object padlock = new object();
        private Cookie mCookie;

        protected override void OnStartup(StartupEventArgs e)
        {
            ToolKit.DebugPurpose_PopupMsg_CheckSpecificRegistryItem();
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            try
            {
                InitLogConfigurator();
                InitStyle();
                ToolKit.DumpArgsToLog(e.Args);

                Log.Info("\t\t Parse cmd args... \r\n");
                mCookie = Cookie.ParseCmdArgs(e.Args);

                if (null == mCookie)
                {
                    Log.Info("\t\t Parse cmd args error \r\n");
                    ErrorHandler(this.FindResource("Common_Initialize_failed").ToString(), string.Empty);
                    return;
                }
             
                IntPtr hWnd = IntPtr.Zero;
                if (CheckFileIsOpened(mCookie.FilePath, out hWnd))
                {
                    Log.Info("\t\t The file is opened, redirect to has exist window \r\n");
                    if (Win32Common.BringWindowToTopEx(hWnd))
                    {
                        Log.Info("\t\t Shutdown current process \r\n");
                        this.Shutdown(0);
                        return;
                    }           
                    else
                    {
                        Log.Info("\t\t Some error happend in redirection, clear item of the registry relative this file path , keep going.. \r\n");
                        ToolKit.DeleteHwndFromRegistry(mCookie.FilePath);
                    }
                }

                EssentialInitialize();
                Apis.SdkLibInit();
                bool LoggedInUser = SkydrmLocal.rmc.sdk.Apis.GetCurrentLoggedInUser(out mSdkSession);
                if (LoggedInUser)
                {
                    if (IsDriverExist())
                    {
                        if (!AddCurrentProcessToWhiteList())
                        {
                            throw new ViewerSystemException(this.FindResource("Common_Initialize_failed").ToString());
                        }

                        if (!StartSafePrint())
                        {
                            throw new ComponentInitializeException(this.FindResource("Common_Initialize_failed").ToString());
                        }

                        upgrade.session.Session session = CreateSession(mCookie);
                        mSessions.TryAdd(session.Id, session);
                        session.DoIntent();
                    }
                    else
                    {
                        throw new ViewerSystemException(this.FindResource("RPM_Drive_Does_Not_Exist").ToString());
                    }
                }
                else
                {
                    RequestUserLoginAndProcessExit();
                }
            }
            catch (LogInitializeException ex)
            {
                ErrorHandler(ex.Message, string.Empty);
            }
            catch (EssentialInitializeException ex)
            {
                ErrorHandler(ex.Message, string.Empty);
            }
            catch (NotSupportedException ex)
            {
                ErrorHandler(ex.Message, string.Empty);
            }
            catch (ParseCmdArgsException ex)
            {
                ErrorHandler(ex.Message, string.Empty);
            }
            catch (ComponentInitializeException ex)
            {
                ErrorHandler(ex.Message, mCookie?.FilePath);
            }
            catch (ViewerSystemException ex)
            {
                ErrorHandler(ex.Message, mCookie?.FilePath);
            }
            catch (Exception ex)
            {
                ErrorHandler(this.FindResource("Common_System_Internal_Error").ToString(), mCookie?.FilePath);
            }
        }

        private void ErrorHandler(string errorMsg, string fileName)
        {
            ViewerWindow viewerWindow = new ViewerWindow(errorMsg , fileName);
            viewerWindow.Show();
        }

        public void DispatcherUnhandledExceptionEventHandler(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception.Message, e.Exception);
            e.Handled = true;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            mTokenSource.Cancel();
            mTokenSource.Dispose();
            CleaerSessions();
            RemoveTempRPMDir();
            //  Apis.SdkLibCleanup();
            base.OnExit(e);
        }

        private void RemoveTempRPMDir()
        {
            foreach (string value in Temp_RPM_Folders.Values)
            {
                int option;
                string tags;
                if (SdkSession.RMP_IsSafeFolder(value, out option, out tags))
                {
                    try
                    {
                        SdkSession.RPM_RemoveDir(value, out string errorMsg);
                    }
                    catch (Exception)
                    {
                        MessageNotify.ShowBalloonTip(SdkSession, this.FindResource("Remove_directory_failed").ToString(), false);
                    }
                }
            }
            Temp_RPM_Folders.Clear();
        }


        private bool CheckFileIsOpened(string filePath, out IntPtr hWnd)
        {
            Log.Info("\t\t Check File Is Opened \r\n");
            bool result = false;
            hWnd = IntPtr.Zero;
            try
            {
                IntPtr intPtr = ToolKit.GetHwndFromRegistry(filePath);
                if (IntPtr.Zero == intPtr)
                {
                    return result;
                }

                if (Win32Common.IsWindow(intPtr))
                {
                    hWnd = intPtr;
                    result = true;
                }
                else
                {
                    Log.Info("\t\t The attained Hwnd from registry is invalid, clear item of the registry relative this file path , keep going... \r\n");
                    ToolKit.DeleteHwndFromRegistry(filePath);
                }

                return result;
            }
            catch (Exception ex)
            {
                Log.Error("\t\t A exception occured in methond CheckFileIsOpened. \r\n");
                Log.Error(ex);
                throw ex;
            }
        }

        public upgrade.session.Session CreateSession(Cookie cookie)
        {
            return new upgrade.session.Session(cookie);
        }

        private void CleaerSessions()
        {
            foreach(string key in mSessions.Keys)
            {
                DeleteSession(key);
            }
        }

        private void DeleteSession(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }
            mSessions.TryRemove(key, out ISession ss);
        }

        public void SignalExternalCommandLineArgs(IList<string> args)
        {
            //string[] array = new string[args.Count];
            //for (int i = 0; i < args.Count; i++)
            //{
            //    array[i] = args[i];
            //}
        }

        private bool AddCurrentProcessToWhiteList()
        {
            bool result = false;
            try
            {
                Process currentProcess = Process.GetCurrentProcess();
                mSdkSession.RPM_RegisterApp(currentProcess.MainModule.FileName);
                mSdkSession.SDWL_RPM_NotifyRMXStatus(true);
                result = mSdkSession.RMP_AddTrustedProcess(currentProcess.Id);
                SkydrmLocal.rmc.sdk.Apis.WaitInstanceInitFinish();
                return result;
            }
            catch (Exception ex)
            {
                mStatusCode |= AppStatusCode.ADD_TO_WHITE_LIST_FAILED;
                Log.Error("\t\t A exception occured in methond AddCurrentProcessToWhiteList. \r\n");
                Log.Error(ex);
                throw ex;
            }
        }

        //public string ApplicationFindResource(string key)
        //{
        //    if (string.IsNullOrEmpty(key))
        //    {
        //        return string.Empty;
        //    }
        //    try
        //    {
        //        string ResourceString = this.FindResource(key).ToString();
        //        return ResourceString;
        //    }
        //    catch (Exception e)
        //    {
        //        return string.Empty;
        //    }
        //}


        private void RequestUserLoginAndProcessExit()
        {
            try
            {
                Log.Info("\t\t Request user login and process exit. \r\n");
                Process currentProcess = Process.GetCurrentProcess();
                string exePath = currentProcess.MainModule.FileName;
                string[] commandLineArgs = Environment.GetCommandLineArgs();

                string cmdStr = string.Empty;

                for (int i = 0; i < commandLineArgs.Length; i++)
                {
                    cmdStr += "\"" + commandLineArgs[i] + "\"";
                    cmdStr += " ";
                }

                Log.InfoFormat("\t\t Send CMD: {0} \r\n", cmdStr);
                mSdkSession?.SDWL_RPM_RequestLogin(exePath, cmdStr);
                mStatusCode |= AppStatusCode.REQUEST_USER_LOGIN;
            }
            catch (Exception ex)
            {
                mStatusCode |= AppStatusCode.INTERNAL_ERROR;
                Log.Error("\t\t A exception occured in methond RequestUserLoginAndProcessExit. \r\n");
                Log.Error(ex);
                throw ex;
            }
        }

        private bool IsDriverExist()
        {
            try
            {
                if (!mSdkSession.RPM_IsDriverExist())
                {
                    Log.Error("\t\t RPM driver does not exist. \r\n");
                    mStatusCode |= AppStatusCode.RPM_DRIVER_DOES_NOT_EXIST;
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                mStatusCode |= AppStatusCode.INTERNAL_ERROR;
                Log.Error("\t\t A exception occured in methond RPM_IsDriverExist. \r\n");
                Log.Error(ex);
                throw ex;
            }
        }

        private void InitLogConfigurator()
        {
            try
            {
                log4net.GlobalContext.Properties["pid"] = Process.GetCurrentProcess().Id;
                string assemblyFilePath = Assembly.GetExecutingAssembly().Location;
                string assemblyDirPath = Path.GetDirectoryName(assemblyFilePath);
                string configFilePath = assemblyDirPath + "//nxrmviewer.exe.config";
               // log4net.Config.XmlConfigurator.ConfigureAndWatch(new FileInfo(configFilePath));
                log4net.Config.XmlConfigurator.Configure(new System.IO.FileInfo(configFilePath));
                mLog = log4net.LogManager.GetLogger(typeof(ViewerApp));
                Log.Info("\t\t -------------------------------- Init Log Ok ---------------------------------\r\n");
            }
            catch (Exception ex)
            {
                mLog = log4net.LogManager.GetLogger(typeof(ViewerApp));
                mStatusCode |= AppStatusCode.INITIAL_LOG_FAILED;
                throw new LogInitializeException(this.FindResource("Common_Initialize_failed").ToString());
            }
        }

        private void EssentialInitialize()
        {
            try
            {
                Log.Info("\t\t Essential Initialize \r\n");

                //  mWorkingDir = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Nextlabs\SkyDRM";

                RegistryKey amazon = Registry.LocalMachine.OpenSubKey(@"Software\Amazon\MachineImage");
                var AMI = amazon?.GetValue("AMIName");
                bool IsAppStream = false;
                if (AMI != null)
                {
                    if (!string.IsNullOrWhiteSpace(AMI.ToString()))
                    {
                        IsAppStream = true;
                    }
                }
                amazon?.Close();

                if (IsAppStream)
                {
                    // Init basic frame, write all files into C:\ProgramData\Nextlabs\SkyDRM
                    mWorkingDir = @"C:\ProgramData\Nextlabs\SkyDRM";
                }
                else
                {
                    // Init basic frame, write all files into User/LocalApp/SkyDRM/
                    mWorkingDir = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Nextlabs\SkyDRM";
                }

                mRmSdkDir = mWorkingDir + "\\" + "rmsdk";
                mDef_RPM_Dir = mWorkingDir + "\\" + "Intermediate";
                mDataBaseDir = mWorkingDir + "\\" + "database";
                mToken = mTokenSource.Token;
                //win required, makre sure skydrmApp and view will be put in a same button group
                SetCurrentProcessExplicitAppUserModelID(mAppID);
                ApplicationCommands.Close.InputGestures.Add(new KeyGesture(Key.Escape));
                DispatcherUnhandledException += DispatcherUnhandledExceptionEventHandler;

                Log.InfoFormat("WorkingDir :{0}", mWorkingDir);
                Log.InfoFormat("RmSdkDir :{0}", mRmSdkDir);
                Log.InfoFormat("Def_RPM_Dir :{0}", mDef_RPM_Dir);
                Log.InfoFormat("DataBaseDir :{0}", mDataBaseDir);
            }
            catch (Exception ex)
            {
                mStatusCode |= AppStatusCode.INITIAL_ESSENTIAL_FAILED;
                Log.Error("\t\t A exception occured in methond EssentialInitialize. \r\n");
                Log.Error(ex);
                throw new EssentialInitializeException(this.FindResource("Common_Initialize_failed").ToString());
            }
        }


        private FunctionProvider DatabaseInitialize()
        {
            try
            {
                if (null == mDbProvider)
                {
                    lock (padlock)
                    {
                        if (null == mDbProvider)
                        {
                            Log.Info("\t\t Database Initialize \r\n");
                            //get user info from registry
                            RegistryKey User = Registry.CurrentUser.CreateSubKey(@"Software\Nextlabs\SkyDRM\ServiceManager\User");
                            //mUserName = (string)User.GetValue("Name", "");
                            //mUserEmail = (string)User.GetValue("Email", "");
                            //mUserCode = (string)User.GetValue("Code", "");
                            string mUserRouter = (string)User.GetValue("Router", "");
                            string mUserTenant = (string)User.GetValue("Tenant", "");
                            mDbProvider = new FunctionProvider(mDataBaseDir, mSdkSession.User.Email, mUserRouter, mUserTenant);
                        }
                    }
                }
                return mDbProvider;
            }
            catch (Exception ex)
            {
                mStatusCode |= AppStatusCode.INITIAL_DATABASE_FAILED;
                Log.Error("\t\t Init database failed, Will affect file share and export, but file can open normally. \r\n");
                Log.Error(ex);
                throw ex;
            }
        }

        private void InitStyle()
        {
            ResourceDictionary resourceDictionary = new ResourceDictionary();
            resourceDictionary.Source = new Uri("Themes/Generic.xaml", UriKind.Relative);
            this.Resources = resourceDictionary;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);


        [DllImport("nxrmprotectprint64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, EntryPoint = "StartSafePrint")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool StartSafePrint();

    }
}
