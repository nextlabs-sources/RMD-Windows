using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

using System.Text;

using System.Collections.ObjectModel;

namespace Print
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

    public class SingleInstanceApplicationWrapper : Microsoft.VisualBasic.ApplicationServices.WindowsFormsApplicationBase
    {
        public SingleInstanceApplicationWrapper()
        {
            // Enable single-instance mode.
            this.IsSingleInstance = true;
        }

        // Create the WPF application class.
        private App app;
        protected override bool OnStartup(
            Microsoft.VisualBasic.ApplicationServices.StartupEventArgs e)
        {
            app = new App();
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            app.Run();

            return false;
        }

        // Direct multiple instances
        protected override void OnStartupNextInstance(Microsoft.VisualBasic.ApplicationServices.StartupNextInstanceEventArgs e)
        {
            if (e.CommandLine.Count > 0)
            {
         
            }
        }
    }

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static string AppID = "Nextlabs.Rmc.SkyDRM.LocalApp"; // use the same ID in all process, if you want all processes' window displayed in one group button in Taskbar
        protected override void OnStartup(StartupEventArgs e)
        {
            SetCurrentProcessExplicitAppUserModelID(AppID);
            // Load the main window.
            PrintWindow printWindow = new PrintWindow();
            this.MainWindow = printWindow;
            printWindow.Show();
          
            if (e.Args.Length > 0)
            {

            }
        }

        public void OnStartupNextInstance()
        {

        }


        [DllImport("shell32.dll", SetLastError = true)]
        static extern void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

    }
}
