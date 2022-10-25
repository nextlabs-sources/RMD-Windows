using System;
using System.Collections.Generic;
using System.Diagnostics;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.app.process
{
    public class PrintProcess
    {
        private static Process mProc = null;

        private static void StarPrintProcess()
        {
                mProc = new Process();
                mProc.StartInfo.FileName = "Print.exe";
                // Set Print.exe process dir
                mProc.StartInfo.WorkingDirectory = System.Windows.Forms.Application.StartupPath;
                mProc.StartInfo.CreateNoWindow = true;
                mProc.EnableRaisingEvents = true;
                mProc.Start();
        }


        public static bool Start()
        {
            bool result = false;          
            if (null == mProc)
            {
                try
                {
                    if(!FindPrintProcess())
                    {
                        StarPrintProcess();
                    }
                    result = true;
                }
                catch (Exception ex)
                {

                }
            }
            return result;
        }

        public static bool Kill()
        {
            bool result = false;
            if (null != mProc)
            {
                try
                {    
                    mProc.Kill();
                    result = true;
                }
                catch (Exception ex)
                {

                }
            }
            return result;
        }


        public static bool FindPrintProcess()
        {
            //
            // find a proc named Print.exe and lies in the same dir with current main module
            //
            bool result = false;
            string PrintProcName = "Print";
            string skydrm_dir = Directory.GetParent(Process.GetCurrentProcess().MainModule.FileName).ToString();
            foreach (var p in Process.GetProcessesByName(PrintProcName))
            {
                try
                {
                    // same parent dir
                    if (skydrm_dir.Equals(Directory.GetParent(p.MainModule.FileName).ToString(), StringComparison.CurrentCultureIgnoreCase))
                    {
                        mProc = p;
                        result = true;
                        break;
                    }
                }
                catch
                {

                }

            }
            return result;
        }
    }
}
