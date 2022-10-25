using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.app
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

        string databaseDir;
        string workingDir;
        string rmsdkDir;
        string executable;
        string rpmDir;

        string userName;
        string userEmail;
        string userCode;
        string userRouter;
        string userTenant;

        string router;
        string tenant;

        string companyrouter;

        int heartbeatIntervalSec;
        int folderProtect;


        public string WorkingFolder { get => workingDir; }
        public string RmSdkFolder { get => rmsdkDir; }
        public string DataBaseFolder { get => databaseDir; }
        public string RpmDir { get => rpmDir; }

        public string AppPath { get => executable; }

        public string UserName { get => userName; }
        public string UserEmail { get => userEmail; }
        public string UserCode { get => userCode; }
        public string UserRouter { get => userRouter; set => userRouter = value; }
        public string UserTenant { get => userTenant; set => userTenant = value; }


        public string Router { get => router; }
        public string Tenant { get => tenant; }

       public string CompanyRouter { get => companyrouter; }

        public int HeartBeatIntervalSec { get => heartbeatIntervalSec; set => SetHeartBeatFrequence(value); }

        public bool IsFolderProtect { get => folderProtect==1; }


        public AppConfig()
        {
            RegistryKey localApp = Registry.CurrentUser.CreateSubKey(@"Software\Nextlabs\SkyDRM\LocalApp");
            RegistryKey User = null;
            RegistryKey preferences = null;
            try
            {
                // init basic frame, write all files into User/LocalApp/SkyDRM/
                workingDir = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Nextlabs\SkyDRM";
                // make sure folder exist, and set it as hiden
                System.IO.Directory.CreateDirectory(workingDir);
                System.IO.DirectoryInfo wd_di = new System.IO.DirectoryInfo(workingDir);
                wd_di.Attributes |= System.IO.FileAttributes.Hidden;
                localApp.SetValue("Directory", workingDir, RegistryValueKind.String);
               
                // user may change our app into other folder, so set the two values every time
                executable = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                localApp.SetValue("Executable", executable, RegistryValueKind.String);
                // rmsdk
                rmsdkDir = workingDir + "\\" + "rmsdk";
                System.IO.Directory.CreateDirectory(rmsdkDir);
                // database
                databaseDir = workingDir + "\\" + "database";
                System.IO.Directory.CreateDirectory(databaseDir);
                // home
                string homeDir = workingDir + "\\" + "home";
                System.IO.Directory.CreateDirectory(homeDir);

                rpmDir = workingDir + "\\" + "Intermediate";

                // for Router&Tenant
                router = (string)localApp.GetValue("Router", sdk.Config.Default_Router);
                if (router == sdk.Config.Default_Router)
                {
                    localApp.SetValue("Router", router, RegistryValueKind.String);
                }
                tenant = (string)localApp.GetValue("Tenant", sdk.Config.Default_Tenant);
                if (tenant == sdk.Config.Default_Tenant)
                {
                    localApp.SetValue("Tenant", tenant, RegistryValueKind.String);
                }

                //for company Router, get value from local_machine
                try
                {
                    RegistryKey localMachine = Registry.LocalMachine.OpenSubKey(@"Software\Nextlabs\SkyDRM\LocalApp", false);
                    if (localMachine != null)
                    {
                        companyrouter = (string)localMachine.GetValue("CompanyRouter", "");
                        localMachine.Close();
                    }
                   
                }
                catch (Exception)
                {
                    companyrouter = "";
                }
                


                heartbeatIntervalSec = (int)localApp.GetValue("HeartbeatIntervalSec", sdk.Config.Deault_Heartbeat);
                if (heartbeatIntervalSec == sdk.Config.Deault_Heartbeat)
                {
                    localApp.SetValue("HeartbeatIntervalSec", heartbeatIntervalSec, RegistryValueKind.DWord);
                }
                else if(heartbeatIntervalSec< minHeartBeatSeconds || heartbeatIntervalSec>maxHeartBeatSeconds)
                {
                    localApp.SetValue("HeartbeatIntervalSec", sdk.Config.Deault_Heartbeat, RegistryValueKind.DWord);
                    heartbeatIntervalSec = sdk.Config.Deault_Heartbeat;
                }

                // protectfolder
                folderProtect = (int)localApp.GetValue("FolderProtect", -1);
                if (folderProtect == -1)
                {
                    // this item is a hiden one,
                    folderProtect = 1;                   
                }

                // maybe user has logined, if yes, store user info
                User = localApp.CreateSubKey(@"User");
                // user info
                userName = (string)User.GetValue("Name", "");
                userEmail = (string)User.GetValue("Email", "");
                userCode = (string)User.GetValue("Code", "");
                userRouter = (string)User.GetValue("Router", "");
                userTenant = (string)User.GetValue("Tenant", "");

            }
            finally
            {
                // release
                localApp.Close();
                if (User != null) User.Close();
                if (preferences != null) preferences.Close();
            }

        }

        public bool SetTenantInfo(string router, string tenant)
        {
            if (router == null || router.Length == 0)
            {
                return false;
            }
            if (tenant == null || tenant.Length == 0)
            {
                return false;
            }

            RegistryKey user = Registry.CurrentUser.CreateSubKey(@"Software\Nextlabs\SkyDRM\LocalApp\User");
            user.SetValue("Router", router, RegistryValueKind.String);
            user.SetValue("Tenant", tenant, RegistryValueKind.String);
            user.Close();

            return true;
        }

        public bool SetUserInfo(string name, string email, string code)
        {
            // sanity check
            if (name == null || name.Length == 0)
            {
                return false;
            }
            if (email == null || email.Length == 0)
            {
                return false;
            }
            if (code == null || code.Length == 0)
            {
                return false;
            }

            RegistryKey user = Registry.CurrentUser.CreateSubKey(@"Software\Nextlabs\SkyDRM\LocalApp\User");

            user.SetValue("Name", name, RegistryValueKind.String);
            user.SetValue("Email", email, RegistryValueKind.String);
            user.SetValue("Code", code, RegistryValueKind.String);
            // get cuttent time value
            user.SetValue("Login Time", System.DateTime.Now.ToString(), RegistryValueKind.String);
            user.Close();

            return true;
        }

        // Clear the user info when logout
        // Fix bug 54824, and allow user to logout even though the network is offline.
        public bool ClearUserInfo()
        {
            try
            {
                RegistryKey user = Registry.CurrentUser.CreateSubKey(@"Software\Nextlabs\SkyDRM\LocalApp\User");
                user.SetValue("Name", "", RegistryValueKind.String);
                user.SetValue("Email", "", RegistryValueKind.String);
                user.SetValue("Code", "", RegistryValueKind.String);
                user.Close();

                return true;
            }
            catch (Exception e)
            {
                SkydrmLocalApp.Singleton.Log.Info(e.ToString());
            }

            return false;
        }

        public void GetRegistryLocalApp()
        {
            RegistryKey localApp = Registry.CurrentUser.CreateSubKey(@"Software\Nextlabs\SkyDRM\LocalApp");
            try
            {
                // for Router&Tenant
                router = (string)localApp.GetValue("Router", sdk.Config.Default_Router);

                tenant = (string)localApp.GetValue("Tenant", sdk.Config.Default_Tenant);

                // for CompanyRouter
                RegistryKey localMachine = Registry.LocalMachine.OpenSubKey(@"Software\Nextlabs\SkyDRM\LocalApp", false);
                if (localMachine !=null)
                {
                    companyrouter = (string)localMachine.GetValue("CompanyRouter", "");
                    localMachine.Close();
                }           

                heartbeatIntervalSec = (int)localApp.GetValue("HeartbeatIntervalSec", sdk.Config.Deault_Heartbeat);
                if (heartbeatIntervalSec < 60 || heartbeatIntervalSec > 3600 * 24)
                {
                    localApp.SetValue("HeartbeatIntervalSec", sdk.Config.Deault_Heartbeat, RegistryValueKind.DWord);
                    heartbeatIntervalSec = sdk.Config.Deault_Heartbeat;
                }

                // protectfolder
                folderProtect = (int)localApp.GetValue("FolderProtect", -1);
                if (folderProtect == -1)
                {
                    // this item is a hiden one,
                    folderProtect = 1;
                }
            }
            finally
            {
                localApp.Close();
            }

        }

        public bool GetRegistryAutoStart()
        {
            string executable = "";
            string ShortFileName = "SkydrmLocal";
            executable = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            RegistryKey rgkRun = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");

            string ShortFileNameTemp = (string)rgkRun.GetValue(ShortFileName);
            rgkRun.Close();
            if (!string.Equals(executable, ShortFileNameTemp, StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }
            return true;

        }

        /// <summary>
        ///  Modify Auto Run according to the Preferences of Startlogin's status
        /// </summary>
        /// <param name="isStartlogin"></param>
        public void ModifyRegistryAutoStart(bool isStartlogin)
        {
            string executable = "";
            string ShortFileName = "SkydrmLocal";
            executable = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            RegistryKey rgkRun = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
            if (isStartlogin)
            {
                string ShortFileNameTemp = (string)rgkRun.GetValue(ShortFileName);
                if ( !string.Equals(executable, ShortFileNameTemp,StringComparison.CurrentCultureIgnoreCase))
                {
                    rgkRun.SetValue(ShortFileName, executable);
                }
            }
            else
            {
                string ShortFileNameTemp = (string)rgkRun.GetValue(ShortFileName);
                if (!string.IsNullOrEmpty(ShortFileNameTemp))
                {
                    rgkRun.DeleteValue(ShortFileName, false);
                }
            }
            rgkRun.Close();
        }

        private void SetHeartBeatFrequence(int newSeconds)
        {
            // Sanity check
            if(newSeconds<minHeartBeatSeconds || newSeconds > maxHeartBeatSeconds)
            {
                SkydrmLocalApp.Singleton.Log.Info(
                    String.Format("new hertbeat values {0} out from rang,set it as default {1}", 
                    newSeconds, 
                    sdk.Config.Deault_Heartbeat)
                );
                newSeconds = sdk.Config.Deault_Heartbeat;
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

    }
}
