using System;
using System.Collections.Generic;
using SkydrmLocal.rmc.sdk;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Print
{
    public class AppConfig
    {
        private string mRmsdkDir;
        private string mWorkingDir;
        private string mExecutable;
        private string mRPM_FolderPath;

        public string RmSdkFolder { get => mRmsdkDir; }
        public string WorkingFolder { get => mWorkingDir; }
        public string AppPath { get => mExecutable; }
        public string RPM_FolderPath { get => mRPM_FolderPath; }

        public AppConfig()
        {
            try
            {
                // init basic frame, write all files into User/LocalApp/SkyDRM/
                // mWorkingDir = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Nextlabs\SkyDRM";

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


                // make sure folder exist, and set it as hiden
                Directory.CreateDirectory(mWorkingDir);

                DirectoryInfo wd_di = new DirectoryInfo(mWorkingDir);
                wd_di.Attributes |= System.IO.FileAttributes.Hidden;

                // user may change our app into other folder, so set the two values every time
                mExecutable = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

                // rmsdk
                mRmsdkDir = mWorkingDir + "\\" + "rmsdk";
                Directory.CreateDirectory(mRmsdkDir);

                //RPM folder
                mRPM_FolderPath = mWorkingDir + "\\" + "Intermediate";

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

    }
}
