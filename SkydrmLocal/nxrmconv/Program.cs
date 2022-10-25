using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace nxrmconv
{
    class Program
    {
        // every other class object can directly use this avoid creating a same one
         private static log4net.ILog mLog;
         public static log4net.ILog Log { get => mLog; }

        static void Main(string[] args)
        {
            mLog = log4net.LogManager.GetLogger("nxrmconv");
            System.Console.WriteLine("Welcome");


            System.Console.WriteLine("Press any key to exit");
            Console.ReadKey();

            //bool bLogInited = false;
            //try
            //{
            //    //init Log
            //    log4net.GlobalContext.Properties["pid"] = Process.GetCurrentProcess().Id;
            //    string assemblyFilePath = Assembly.GetExecutingAssembly().Location;
            //    string assemblyDirPath = Path.GetDirectoryName(assemblyFilePath);
            //    string configFilePath = assemblyDirPath + "//nxrmconv.exe.config";
            //    log4net.Config.XmlConfigurator.ConfigureAndWatch(new FileInfo(configFilePath));
            //    bLogInited = true;
            //    Log.Info("\t\t -------------------------------- Init Log Ok ---------------------------------\r\n");
            //}
            //catch (Exception e)
            //{
            //    if (bLogInited)
            //    {
            //        Log.Error(e);
            //    }
            //}

            //Protect protect = new Protect();
            //if (!protect.InitSystemLevelComponents())
            //{             
        
          //  }
        }

        public class Protect
        {
            #region Private
            // use the same ID in all process, if you want all processes' window displayed in one group button in Taskbar
            private Session mSession = null;
            private bool mLoggedInUser = false;
        
            #endregion Private

            #region Public
            // public static ViewerApp Instance { get => (ViewerApp)ViewerApp.Current; }
          
            public Session Session { get => mSession; }
            #endregion Public

            public Protect()
            {
  
            }

            public bool InitSystemLevelComponents()
            {
                //get RPM user and added trust process
                mLoggedInUser = SkydrmLocal.rmc.sdk.Apis.GetCurrentLoggedInUser(out mSession);
                if (mLoggedInUser)
                {
                    Process process = Process.GetCurrentProcess();
                    Session.RPM_RegisterApp(process.MainModule.FileName);
                    Session.SDWL_RPM_NotifyRMXStatus(true);
                    Session.RMP_AddTrustedProcess(process.Id);
                    SkydrmLocal.rmc.sdk.Apis.WaitInstanceInitFinish();
                }
                else
                {
                  //  Log.Error("\t\t No user login \r\n");
                    System.Console.WriteLine("No user login");
                    return false;
                }
                return true;
            }
        }
    }
}
