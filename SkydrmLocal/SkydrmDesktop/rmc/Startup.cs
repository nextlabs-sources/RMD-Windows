using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc
{
    /// <summary>
    /// Create single instance application wrapper System.Application using WindowsFormsApplicationBase.
    ///  Note: must set the 'Build Action' of ServiceManagerApp.xaml property as 'Page'.
    /// </summary>
    public class Startup
    {
        [STAThread]
        public static void Main(string[] args)
        {
            SingleInstanceAppWrapper wrapper = new SingleInstanceAppWrapper();
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

    public class SingleInstanceAppWrapper : WindowsFormsApplicationBase
    {
        private SkydrmApp app;
        public SingleInstanceAppWrapper()
        {
            // Enable single instance mode
            this.IsSingleInstance = true;
        }

        protected override bool OnStartup(StartupEventArgs eventArgs)
        {
            app = new SkydrmApp(); 
            app.ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown; 
            app.InitializeComponent();
            app.Run();

            return false;
        }

        // Callback this when second or more instance get started, and the first is still running,
        // Good Point to handle second command line here
        protected override void OnStartupNextInstance(StartupNextInstanceEventArgs eventArgs)
        {
            if (app == null)
            {
                Console.WriteLine("SkyDRM app object is null.");
                return;
            }
            app.SignalExternalCommandLineArgs(eventArgs.CommandLine);
        }

        protected override bool OnUnhandledException(Microsoft.VisualBasic.ApplicationServices.UnhandledExceptionEventArgs e)
        {
            return base.OnUnhandledException(e);
        }

    }

}
