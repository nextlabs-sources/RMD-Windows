using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.app
{
    /*LocalApp's all supported configurations reside in Windows regiestry
    *  Nextlabs
    *      SkyDRM
    *          LocalApp
    *              Directory :=  working folder         
    *              Executable :=  exe path
    *              Router :=  defualt RMS server
    *              Tenant :=  default Tenant
    *              CompanyRouter:=defualt ""
    *              HeartBeatIntervalSec := Deault_Heartbeat
    *              FolderProtect := Mark some folder in NTFS with remove it list and read permission
    *              CentralLocationRadio:= user select locationRadio status
    *              CentralPlcRadio:= user select rightRadio status
    *              User
    *                  Name: user name
    *                  Email: user email
    *                  Code: user pass code
    *                  Login Time: user login time
    *                  Router:  user used Router
    *                  Tenant:  user used tenant
    */
    public class AppConfig
    {
        const int minHeartBeatSeconds = 60;
        const int maxHeartBeatSeconds = 3600 * 24;

        int heartbeatIntervalSec;
        int folderProtect;

        public string WorkingFolder { get; private set; }
        public string RmSdkFolder { get; private set; }
        public string DataBaseFolder { get; private set; }
        public string RpmDir { get; private set; }

        public string AppPath { get; private set; }

        public string UserUrl { get; private set; }

        public string UserTicket { get; private set; }

        public string PersonRouter { get => GetPersonRouter(); }

        public int HeartBeatIntervalSec { get => heartbeatIntervalSec; set => SetHeartBeatFrequence(value); }

        public bool IsFolderProtect { get => folderProtect==1; }

        public bool LeaveCopy { get => GetSysPreference(0); set => SetSysPreference(0, value); }
        public bool ShowNotifyWin { get => GetSysPreference(1); set => SetSysPreference(1, value); }


        public AppConfig()
        {
            RegistryKey localApp = Registry.CurrentUser.CreateSubKey(@"Software\Nextlabs\SkyDRM\LocalApp");
            RegistryKey tray = Registry.CurrentUser.OpenSubKey(@"Software\Nextlabs\SkyDRM\ServiceManager");
            RegistryKey trayUser = null;
            try
            {
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
                    WorkingFolder = @"C:\ProgramData\Nextlabs\SkyDRM";
                }
                else
                {
                    // Init basic frame, write all files into User/LocalApp/SkyDRM/
                    WorkingFolder = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Nextlabs\SkyDRM";
                }

                // Make sure folder exist, and set it as hiden
                System.IO.Directory.CreateDirectory(WorkingFolder);
                System.IO.DirectoryInfo wd_di = new System.IO.DirectoryInfo(WorkingFolder);
                wd_di.Attributes |= System.IO.FileAttributes.Hidden;
                localApp.SetValue("Directory", WorkingFolder, RegistryValueKind.String);

                // User may change our app into other folder, so set the two values every time
                AppPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                localApp.SetValue("Executable", AppPath, RegistryValueKind.String);
                // rmsdk
                RmSdkFolder = WorkingFolder + "\\" + "rmsdk";
                System.IO.Directory.CreateDirectory(RmSdkFolder);
                // database
                DataBaseFolder = WorkingFolder + "\\" + "database";
                System.IO.Directory.CreateDirectory(DataBaseFolder);
                // home
                string homeDir = WorkingFolder + "\\" + "home";
                System.IO.Directory.CreateDirectory(homeDir);

                RpmDir = WorkingFolder + "\\" + "Intermediate";


                heartbeatIntervalSec = (int)localApp.GetValue("HeartbeatIntervalSec", SkydrmLocal.rmc.sdk.Config.Deault_Heartbeat);
                if (heartbeatIntervalSec == SkydrmLocal.rmc.sdk.Config.Deault_Heartbeat)
                {
                    localApp.SetValue("HeartbeatIntervalSec", heartbeatIntervalSec, RegistryValueKind.DWord);
                }
                else if(heartbeatIntervalSec< minHeartBeatSeconds || heartbeatIntervalSec>maxHeartBeatSeconds)
                {
                    localApp.SetValue("HeartbeatIntervalSec", SkydrmLocal.rmc.sdk.Config.Deault_Heartbeat, RegistryValueKind.DWord);
                    heartbeatIntervalSec = SkydrmLocal.rmc.sdk.Config.Deault_Heartbeat;
                }

                // protectfolder
                folderProtect = (int)localApp.GetValue("FolderProtect",1);

                // fix bug Bug 57642 - Connect to the wrong server URL from the web button in main window
                trayUser = tray.OpenSubKey(@"User");
                UserUrl = (string)trayUser.GetValue("Url", SkydrmLocal.rmc.sdk.Config.Default_Router);
                UserTicket = (string)trayUser.GetValue("Ticket", "");

            }
            finally
            {
                // release
                localApp.Close();
                trayUser?.Close();
                tray?.Close();
            }

        }

        private string GetPersonRouter()
        {
            // for person Router, get value from local_machine
            string router = SkydrmLocal.rmc.sdk.Config.Default_Router;
            try
            {
                RegistryKey localMachine = Registry.LocalMachine.OpenSubKey(@"Software\Nextlabs\SkyDRM\LocalApp", false);
                if (localMachine != null)
                {
                    router = (string)localMachine.GetValue("Router", SkydrmLocal.rmc.sdk.Config.Default_Router);
                    localMachine.Close();
                }
            }
            catch (Exception)
            {
                router = SkydrmLocal.rmc.sdk.Config.Default_Router;
            }
            return router;
        }

        private void SetHeartBeatFrequence(int newSeconds)
        {
            // Sanity check
            if(newSeconds<minHeartBeatSeconds || newSeconds > maxHeartBeatSeconds)
            {
                SkydrmApp.Singleton.Log.Info(
                    String.Format("new hertbeat values {0} out from rang,set it as default {1}", 
                    newSeconds,
                    SkydrmLocal.rmc.sdk.Config.Deault_Heartbeat)
                );
                newSeconds = SkydrmLocal.rmc.sdk.Config.Deault_Heartbeat;
            }
            RegistryKey localApp = Registry.CurrentUser.CreateSubKey(@"Software\Nextlabs\SkyDRM\LocalApp");
            try
            {
                localApp.SetValue("HeartbeatIntervalSec", newSeconds, RegistryValueKind.DWord);
                heartbeatIntervalSec = newSeconds;
            }
            finally
            {
                if (localApp != null)
                {
                    localApp.Close();
                }
            }
        }

        /// <summary>
        /// Get system preference from Registry @"Software\Nextlabs\SkyDRM"
        /// 0:LeaveCopy, 1:ShowNotifyWin
        /// </summary>
        /// <param name="nameType"></param>
        /// <returns></returns>
        private bool GetSysPreference(int nameType)
        {
            string name = string.Empty;
            switch (nameType)
            {
                case 0:
                    name = "LeaveCopy";
                    break;
                case 1:
                    name = "ShowNotifyWin";
                    break;
                default:
                    break;
            }
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            int result;
            RegistryKey skydrm = Registry.CurrentUser.OpenSubKey(@"Software\Nextlabs\SkyDRM");
            try
            {
                result = (int)skydrm.GetValue(name, 0);
            }
            finally
            {
                skydrm?.Close();
            }
            return result == 1;
        }
        /// <summary>
        /// Set system preference in Registry @"Software\Nextlabs\SkyDRM"
        /// 0:LeaveCopy, 1:ShowNotifyWin
        /// </summary>
        /// <param name="nameType"></param>
        /// <param name="value"></param>
        private void SetSysPreference(int nameType, bool value)
        {
            string name = string.Empty;
            switch (nameType)
            {
                case 0:
                    name = "LeaveCopy";
                    break;
                case 1:
                    name = "ShowNotifyWin";
                    break;
                default:
                    break;
            }
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            int result = value == true ? 1 : 0;
            RegistryKey skydrm = Registry.CurrentUser.CreateSubKey(@"Software\Nextlabs\SkyDRM");
            try
            {
                skydrm.SetValue(name, result, RegistryValueKind.DWord);
            }
            catch (Exception e)
            {
                SkydrmApp.Singleton.Log.Error("Error in SetSysPreference", e);
            }
            finally
            {
                skydrm?.Close();
            }
        }

    }
}
