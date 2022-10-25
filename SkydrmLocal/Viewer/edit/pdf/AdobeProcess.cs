using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Viewer.edit.pdf
{
    public class AdobeProcess : IEditProcess
    {
        private log4net.ILog mLog;

        private Process Process { get; set; }

        private int Pid { get; set; }

        public AdobeProcess(log4net.ILog log)
        {
            mLog.Info("AdobeProcess AdobeProcess()");
            this.mLog = log;
            Process = new Process();   
        }

        public void Launch()
        {
            mLog.Info("\t\t\t\t AdobeProcess Launch \r\n");
            Process.StartInfo.FileName = "Launch.pdf";
            Process.Start();
            Pid = Process.Id;
        }

        public void OpenFile(string filePath)
        {
            mLog.Info("\t\t AdobeProcess OpenFile \r\n" +
                 "\t\t\t\t filePath :" + filePath + "\r\n");

            Process.StartInfo.FileName = filePath;
            Process.Start();
        }

        public int GetPid()
        {
            mLog.Info("\t\t\t\t AdobeProcess Pid :" +Pid +"\r\n");
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
