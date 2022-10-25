using Newtonsoft.Json;
using SkydrmLocal.rmc.helper;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.app
{
    /* User.Session HeartBeater
   Responsible:
    - call SDK.save 
    - call SDK.updateUserName
    //- call SDK.updateWaterMark
    - call SDK.updateUserDiskQuota
*/
    class HeartBeater
    {
        SkydrmApp host;

        int count = 0;

        bool stop = false;
        public bool Stop
        {
            get => stop;
            set
            {
                stop = value;
                if (thisThread != null) thisThread.Interrupt();
            }
        }

        Thread thisThread = null;

        public HeartBeater(SkydrmApp host)
        {
            this.host = host;
        }

        public void WorkingBackground()
        {
            new Thread(HeartBeat) { Name = "UserHeartBeat", IsBackground = true }.Start();
        }

        private void HeartBeat()
        {
            thisThread = Thread.CurrentThread;
            Console.WriteLine("====HeartBeat started!====");
            SkydrmApp.Singleton.Log.Info("====HeartBeat started!====");
            try
            {
                // each subtask can not throw out excpetion
                while (!stop)
                {

                    new NoThrowTask(false, () =>
                    {// need move to IUser
                        host.Rmsdk.User.UpdateMyDriveInfo();
                        host.Dispatcher.Invoke(() =>
                        {
                            host.InvokeEvent_MyVaultQuataUpdated();
                        });
                    }).Do();
                    if (stop) break;

                    new NoThrowTask(false, () =>
                    {// need move to IUser
                        //
                        //  by osmond, wait for SDK fixed a bug
                        //
                        host.Rmsdk.User.UpdateUserInfo();
                        host.Dispatcher.Invoke(() =>
                        {
                            host.InvokeEvent_UserNameUpdated();
                        });

                        // update db
                        host.DBFunctionProvider.UpdateUserName(host.Rmsdk.User.Name);
                    }).Do();
                    if (stop) break;

                    // System bucket 
                    new NoThrowTask(false, () =>{host.SystemProject.OnHeartBeat();}).Do();
                    if (stop) break;

                    new NoThrowTask(() =>{host.SharedWithMe.OnHeartBeat();}).Do();
                    if (stop) break;

                    new NoThrowTask(() => { host.MyVault.OnHeartBeat(); }).Do();
                    if (stop) break;

                    new NoThrowTask(() => {host.MyProjects.OnHeartBeat();}).Do();
                    if (stop) break;

                    new NoThrowTask(() =>{host.User.OnHeartBeat();}).Do();
                    if (stop) break;

                    new NoThrowTask(() => { host.Rmsdk.SaveSession(host.Config.RmSdkFolder); }).Do();
                    if (stop) break;

                    Console.WriteLine(String.Format("====HearBeat {0}s====", count++));
                    SkydrmApp.Singleton.Log.Info(String.Format("====HearBeat {0}s====", count++));
                    Thread.Sleep(host.User.HeartBeatIntervalSec * 1000);
                }
            }
            catch (Exception e)
            {
                host.Log.Error(e);
            }
            Console.WriteLine("====HeartBeat finished!====");
            SkydrmApp.Singleton.Log.Info("====HeartBeat finished!====");
        }
    }
}
