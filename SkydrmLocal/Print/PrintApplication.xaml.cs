using Microsoft.Win32;
using Print.utils;
using SkydrmLocal.rmc.sdk;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace Print
{
    /// <summary>
    /// Interaction logic for PrintApplication.xaml
    /// </summary>
    public partial class PrintApplication : Application
    {
        [DllImport("shell32.dll", SetLastError = true)]
        static extern void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

        [DllImport("nxrmprotectprint64.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, EntryPoint = "StartSafePrint")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool StartSafePrint();

        private static string AppID = "Nextlabs.Rmc.SkyDRM.LocalApp";  // use the same ID in all process, if you want all processes' window displayed in one group button in Taskbar

        private readonly log4net.ILog mLog = log4net.LogManager.GetLogger(typeof(PrintApplication));

        private Session mSession = null;

        private PrintService PrintService = null;

        private string[] mCmdArgs = null;

        private AppConfig mAppconfig = null;

        private IntentParser mIntentParser = null;

        public Session Session { get => mSession; }

        public AppConfig Appconfig { get => mAppconfig; }

        public log4net.ILog Log { get => mLog; }

        public const Int32 RPM_SAFEDIRRELATION_SAFE_DIR = 0x00000001;
        public const Int32 RPM_SAFEDIRRELATION_ANCESTOR_OF_SAFE_DIR = 0x00000002;
        public const Int32 RPM_SAFEDIRRELATION_DESCENDANT_OF_SAFE_DIR = 0x00000004;


        private bool InitSystemLevelComponents()
        {
            bool bLogInited = false;
            try
            {
                //init Log
                log4net.GlobalContext.Properties["pid"] = Process.GetCurrentProcess().Id;
                string assemblyFilePath = Assembly.GetExecutingAssembly().Location;
                string assemblyDirPath = Path.GetDirectoryName(assemblyFilePath);
                string configFilePath = assemblyDirPath + "//nxrmprint.exe.config";
                log4net.Config.XmlConfigurator.ConfigureAndWatch(new FileInfo(configFilePath));
                bLogInited = true;
                Log.Info("\t\t -------------------------------- Init Log Ok ---------------------------------\r\n");
            }
            catch (Exception e)
            {
                if (bLogInited)
                {
                    // Log.Error(e);
                }
                return false;
            }

            //get RPM user and added trust process
            bool res = SkydrmLocal.rmc.sdk.Apis.GetCurrentLoggedInUser(out mSession);
            if (res)
            {
                Process process = Process.GetCurrentProcess();
                Session.RPM_RegisterApp(process.MainModule.FileName);
                Session.SDWL_RPM_NotifyRMXStatus(true);
                Session.RMP_AddTrustedProcess(process.Id);
                SkydrmLocal.rmc.sdk.Apis.WaitInstanceInitFinish();
            }
            else
            {
                // Log.Error("\t\t No user login \r\n");
                CommonUtils.ShowBubble("No user login");

                // Recover session failed, try to request login
                string exePath = Process.GetCurrentProcess().MainModule.FileName;

                string cmdStr = string.Empty;

                for (int i = 0; i < mCmdArgs.Length; i++)
                {
                    cmdStr += "\"" + mCmdArgs[i] + "\"";
                    cmdStr += " ";
                }

                mSession?.SDWL_RPM_RequestLogin(exePath, cmdStr);
                return false;
            }

            try
            {
                if (!Session.RPM_IsDriverExist())
                {
                    // Log.Error("\t\t RPM driver does not exist \r\n");
                    CommonUtils.ShowBubble("RPM driver does not exist");
                    return false;
                }
            }
            catch (Exception ex)
            {
                // Log.Error("\t\t RPM driver does not exist \r\n");
                CommonUtils.ShowBubble("RPM driver does not exist");
                // Log.Error(ex);
                return false;
            }

            try
            {
                mAppconfig = new AppConfig();
            }
            catch (Exception ex)
            {
                // Log.Error("\t\t RPM driver does not exist \r\n");
                CommonUtils.ShowBubble("init App config failed");
                // Log.Error(ex);
                return false;
            }

            int option;
            string tags;
            if (!Session.RMP_IsSafeFolder(mAppconfig.RPM_FolderPath, out option, out tags))
            {
                try
                {
                    Session.RPM_AddDir(mAppconfig.RPM_FolderPath);
                }
                catch (Exception ex)
                {
                    //Log.Error("\t\t RPM Add Dir Failed \r\n");
                    //Log.Error(ex);
                    CommonUtils.ShowBubble("RPM Add Dir Failed ");
                }
            }

            //init string map
            // tbd

            //win required, makre sure skydrmApp and view will be put in a same button group
            SetCurrentProcessExplicitAppUserModelID(AppID);

            //  Log.Info("\t\t Init System Level Components ok \r\n");
            return true;
        }


        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // back door to show msg, easy for user to attach debugger to this process, for debug used only
            DebugPurpose_PopupMsg_CheckSpecificRegistryItem();

            mCmdArgs = Environment.GetCommandLineArgs();

            try
            {
                Apis.SdkLibInit();

                // init component;
                if (!InitSystemLevelComponents())
                {
                    //  Log.Warn("\t\t Init system level compoents failed \r\n");
                    //  Log.Warn("\t\t Shutdown \r\n");
                    this.Shutdown();
                    return;
                }

                // try to extract user intent form command lines, command line must be valid
                mIntentParser = new IntentParser(mCmdArgs/*, mLog*/);

                if (!mIntentParser.Parse())
                {
                    // Log.Warn("\t\t parse command line failed \r\n");
                    this.Shutdown();
                    return;
                }

                if (Session.SDWL_RPM_GetFileStatus(mIntentParser.NxlFilePath, out int dirstatus, out bool filestatus))
                {
                    if (dirstatus == RPM_SAFEDIRRELATION_SAFE_DIR || dirstatus == RPM_SAFEDIRRELATION_DESCENDANT_OF_SAFE_DIR)
                    {
                        if (filestatus)
                        {
                            Log.Error("\t\t Is RPM folder file\r\n");
                            this.Shutdown();
                        }
                    }
                }
                else
                {
                    this.Shutdown();
                }

                if (!StartSafePrint())
                {
                    Log.Error("\t\t Error in start safe print.\r\n");
                    this.Shutdown();
                }

                PrintService = new PrintService(new StartParameters(mIntentParser.IntPtrOfWindowOwner, mIntentParser.NxlFilePath), Printed);

                PrintService.ExecutePrintInBackground();

                return;
            }
            catch (Exception ex)
            {
                //  Log.Error(ex.Message, ex);
                //  Log.Warn("Should never reach here");
                this.Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            //Apis.SdkLibCleanup();
            base.OnExit(e);
        }

        private void DebugPurpose_PopupMsg_CheckSpecificRegistryItem()
        {
            // This is a back-door to check [Registy]\Computer\HKEY_CURRENT_USER\Software\Nextlabs\SkyDRM\LocalApp
            //  DebugViewer = ?
            // if 1, show MessageBoxc

            bool isShowMsgBox = false;

            RegistryKey localApp = null;
            try
            {
                localApp = Registry.CurrentUser.CreateSubKey(@"Software\Nextlabs\SkyDRM\LocalApp");
                isShowMsgBox = (int)localApp.GetValue("DebugPrint", 0) == 1 ? true : false;
                if (isShowMsgBox)
                {
                    MessageBox.Show("for debug, good pint to set breakpoint");
                }
            }
            catch
            {
                // ignroe
            }
            finally
            {
                if (localApp != null)
                {
                    localApp.Close();
                }
            }
        }


        public void Printed()
        {
            //  ResetNextPrintFile();
            //  NamedPipesServer.ShudownActivePipes();

            Application.Current.Shutdown(0);
            Environment.Exit(0);
        }


        private void UnhandledExceptionEventHandler(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Console.Write(e.ToString());
            Log.Error(e.ToString());
        }

        public class IntentParser
        {
            private string[] mCmdArgs;
            private string mModuleName = string.Empty;
            private string mNxlFilePath = string.Empty;
            private EnumIntent mIntent;
            private int mIntPtrOfWindowOwner = -1;

            public string ModuleName
            {
                get
                {
                    return mModuleName;
                }
            }

            public string NxlFilePath
            {
                get
                {
                    return mNxlFilePath;
                }
            }
            public EnumIntent Intent
            {
                get
                {
                    return mIntent;
                }
            }

            public int IntPtrOfWindowOwner
            {
                get
                {
                    return mIntPtrOfWindowOwner;
                }
            }

            public IntentParser(string[] cmdArgs)
            {
                this.mCmdArgs = cmdArgs ?? throw new ArgumentNullException(nameof(cmdArgs));
                DumpArgsToLog();
            }

            private void DumpArgsToLog()
            {
                string l = "";
                foreach (var i in mCmdArgs)
                {
                    l += i;
                    l += " ";
                }
            }

            public bool Parse()
            {
                bool result = false;

                if (this.mCmdArgs.Length < 3)
                {
                    return result;
                }

                GetModuleName(out mModuleName);

                GetIntent(ref mIntent);

                if (this.mCmdArgs.Length == 3)
                {
                    if (GetNxlFilePath(this.mCmdArgs.Length - 1, out mNxlFilePath))
                    {
                        result = true;
                    }
                }
                else if (this.mCmdArgs.Length == 4)
                {
                    if (GetIntPtrOfWindowOwner(this.mCmdArgs.Length - 2, out mIntPtrOfWindowOwner))
                    {
                        if (GetNxlFilePath(this.mCmdArgs.Length - 1, out mNxlFilePath))
                        {
                            result = true;
                        }
                    }
                }
                else
                {
                    // mLog.Warn("\t\t invlaid command line \r\n");
                }
                return result;
            }

            private bool GetModuleName(out string ModuleName)
            {
                //  mLog.Info("\t\t Get Intent \r\n");
                ModuleName = string.Empty;
                bool result = false;
                // first option muse is either View or FromMainInfo
                ModuleName = this.mCmdArgs[0];
                if (System.IO.Path.GetExtension(ModuleName).EndsWith(".exe", StringComparison.CurrentCultureIgnoreCase))
                {
                    result = true;
                }

                if (!result)
                {
                    //  mLog.Warn("\t\t logic error in command line intent, invalid EnumIntent value \r\n");
                }

                return result;
            }

            private bool GetIntent(ref EnumIntent intent)
            {
                // mLog.Info("\t\t Get Intent \r\n");
                bool result = false;
                // first option muse is either View or FromMainInfo
                string second_option = this.mCmdArgs[1];
                if (String.Equals(second_option, "-Print", StringComparison.CurrentCultureIgnoreCase))
                {
                    //  mLog.Info("\t\t Intent : View \r\n");
                    intent = EnumIntent.Print;
                    result = true;
                }

                if (!result)
                {
                    //  mLog.Warn("\t\t logic error in command line intent, invalid EnumIntent value \r\n");
                }

                return result;
            }

            private bool GetNxlFilePath(int index, out string nxlFilePath)
            {
                // mLog.Info("\t\t Get NxlFile Path\r\n");
                nxlFilePath = string.Empty;
                bool result = false;
                // first option muse is either View or FromMainInfo
                nxlFilePath = this.mCmdArgs[index];
                if (System.IO.Path.GetExtension(nxlFilePath).EndsWith(".nxl", StringComparison.CurrentCultureIgnoreCase))
                {
                    result = true;
                }

                if (!result)
                {
                    // mLog.Warn("\t\t logic error in command line intent, invalid EnumIntent value \r\n");
                }
                return result;
            }

            private bool GetIntPtrOfWindowOwner(int index, out int IntPtrOfWindowOwner)
            {
                bool result = false;

                string intPtr = this.mCmdArgs[index];

                result = Int32.TryParse(intPtr, out IntPtrOfWindowOwner);

                return result;
            }

            public enum EnumIntent
            {
                Print
            }
        }

    }
}
