using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using SkydrmLocal.rmc.sdk;
using Microsoft.Win32;

namespace Viewer.app
{
    public class AppConfig
    {
        private string mRmsdkDir;
        private string mWorkingDir;
        private string mExecutable;
        private string mRPM_FolderPath;
        private bool mInitialFailed = false;
        private string mDatabaseDir;

        private string mUserName;
        private string mUserEmail;
        private string mUserCode;
        private string mUserRouter;
        private string mUserTenant;

        public string UserName { get => mUserName; }
        public string UserEmail { get => mUserEmail; }
        public string UserCode { get => mUserCode; }
        public string UserRouter { get => mUserRouter; set => mUserRouter = value; }
        public string UserTenant { get => mUserTenant; set => mUserTenant = value; }

        public string RmSdkFolder { get => mRmsdkDir; }
        public string WorkingFolder { get => mWorkingDir; }
        public string AppPath { get => mExecutable; }
        public string RPM_FolderPath { get => mRPM_FolderPath; }
        public bool InitialFailed { get => mInitialFailed; }
        public string DataBaseFolder { get => mDatabaseDir; }

        public AppConfig(Session session, User user)
        {
            try
            {
                mInitialFailed = false;

                // init basic frame, write all files into User/LocalApp/SkyDRM/
                mWorkingDir = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Nextlabs\SkyDRM";

                // make sure folder exist, and set it as hiden
                Directory.CreateDirectory(mWorkingDir);

                DirectoryInfo wd_di = new System.IO.DirectoryInfo(mWorkingDir);
                wd_di.Attributes |= System.IO.FileAttributes.Hidden;

                // user may change our app into other folder, so set the two values every time
                mExecutable = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

                // rmsdk
                mRmsdkDir = mWorkingDir + "\\" + "rmsdk";
                Directory.CreateDirectory(mRmsdkDir);

                //RPM folder
                mRPM_FolderPath = mWorkingDir + "\\" + "Intermediate";

                // database
                mDatabaseDir = mWorkingDir + "\\" + "database";

                RegistryKey User = Registry.CurrentUser.CreateSubKey(@"Software\Nextlabs\SkyDRM\ServiceManager\User");

                // user info
                mUserName = (string)User.GetValue("Name", "");
                mUserEmail = (string)User.GetValue("Email", "");
                mUserCode = (string)User.GetValue("Code", "");
                mUserRouter = (string)User.GetValue("Router", "");
                mUserTenant = (string)User.GetValue("Tenant", "");

            }
            catch (Exception ex)
            {
                mInitialFailed = true;
            }
        }
    }
}
