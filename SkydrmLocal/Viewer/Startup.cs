using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Collections.ObjectModel;
using Viewer.upgrade.application;
using Viewer.upgrade.utils;

namespace Viewer
{
    public class Startup
    {
        [STAThread]
        public static void Main(string[] args)
        {
            SingleInstanceApplicationWrapper wrapper = new SingleInstanceApplicationWrapper();
            wrapper.Run(args);
        }
    }

    public class SingleInstanceApplicationWrapper :
        Microsoft.VisualBasic.ApplicationServices.WindowsFormsApplicationBase
    {
        // Create the WPF application class.
        private ViewerApp app;

        public SingleInstanceApplicationWrapper()
        {
            // Enable single-instance mode.

            ToolKit.DebugPurpose_PopupMsg_CheckSpecificRegistryItem();
            Int64 code = ToolKit.RunningMode();
            if (code == 1)
            {
                this.IsSingleInstance = true;
            }
            else
            {
                this.IsSingleInstance = false;
            }
        }

        protected override bool OnStartup(
            Microsoft.VisualBasic.ApplicationServices.StartupEventArgs e)
        {
            app = new ViewerApp();
            if (IsSingleInstance)
            {
                ViewerApp.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            }
            else
            {
                ViewerApp.Current.ShutdownMode = ShutdownMode.OnLastWindowClose;
            }
            app.Run();
            return false;
        }

        // Direct multiple instances
        protected override void OnStartupNextInstance(
            Microsoft.VisualBasic.ApplicationServices.StartupNextInstanceEventArgs e)
        {
            if (e.CommandLine.Count > 0)
            {
                app.SignalExternalCommandLineArgs(e.CommandLine);
            }
        }
    }

}
