using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.process.pdf
{
    public class AdobeProcess : IEditProcess
    {
        private log4net.ILog Log = SkydrmLocalApp.Singleton.Log;

        private Process Process { get; set; }

        private int Pid { get; set; }

        public AdobeProcess()
        {
            Log.Info("AdobeProcess AdobeProcess()");
            Process = new Process();   
        }

        public void Launch()
        {
            Log.Info("\t\t\t\t AdobeProcess Launch \r\n");
            Process.StartInfo.FileName = "Launch.pdf";
            Process.Start();
            Pid = Process.Id;
        }

        public void OpenFile(string filePath)
        {
            Log.Info("\t\t AdobeProcess OpenFile \r\n" +
                 "\t\t\t\t filePath :" + filePath + "\r\n");

            Process.StartInfo.FileName = filePath;
            Process.Start();
        }

        public int GetPid()
        {
            Log.Info("\t\t\t\t AdobeProcess Pid :" +Pid +"\r\n");
            return Pid;
        }

        public void Close()
        {
           
        }

        public IntPtr MainWindowHandle()
        {
            return Process.MainWindowHandle;
        }

        public Process GetProcess()
        {
            return Process;
        }


        public void FinalReleaseComObject()
        {
        }
    }
}
