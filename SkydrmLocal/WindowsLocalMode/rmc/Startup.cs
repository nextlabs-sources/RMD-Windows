using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic.ApplicationServices;

namespace SkydrmLocal.rmc
{
    public class Startup
    {
        [STAThread]
        public static void Main(string[] args)
        {
            SingleInstanceApplicationWrapper wrapper = new SingleInstanceApplicationWrapper();
            try
            {
                wrapper.Run(args);
            }
            catch (Microsoft.VisualBasic.ApplicationServices.CantStartSingleInstanceException ex)
            {
                Console.Write(ex.ToString());
            }         
        }
    }

    public class SingleInstanceApplicationWrapper : Microsoft.VisualBasic.ApplicationServices.WindowsFormsApplicationBase
    {
        public SingleInstanceApplicationWrapper()
        {
            // Enable single-instance mode.
            this.IsSingleInstance = true;
        }

        // Create the WPF application class.
        private SkydrmLocalApp app;
        protected override bool OnStartup(Microsoft.VisualBasic.ApplicationServices.StartupEventArgs e)
        {
            app = new SkydrmLocalApp();
            app.ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;
            app.InitializeComponent();
            app.Run();

            return false;
        }

        // Direct multiple instances
        protected override void OnStartupNextInstance(
            Microsoft.VisualBasic.ApplicationServices.StartupNextInstanceEventArgs e)
        {
            if (null != app)
            {
                app.SignalExternalCommandLineArgs(e.CommandLine);
            }
        }

        protected override bool OnUnhandledException(Microsoft.VisualBasic.ApplicationServices.UnhandledExceptionEventArgs e)
        {        
          return base.OnUnhandledException(e);
        }
    }
}
