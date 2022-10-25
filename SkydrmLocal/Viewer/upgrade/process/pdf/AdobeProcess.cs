//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Viewer.upgrade.application;
//using Viewer.upgrade.process.basic;

//namespace Viewer.upgrade.process.pdf
//{

//    public class AdobeProcess : IProcess
//    {
//        public  Process Process { get => mProcess; }
//        private log4net.ILog mLog;
//        private string mFilePath = string.Empty;
//        private Process mProcess = null;

//        public event EventHandler OfficeProcessExited;
//        public event Action<bool> EditSaved;

//        public AdobeProcess(string filePath)
//        {
//            mLog = ((ViewerApp)ViewerApp.Current).Log;
//            mLog.Info("AdobeProcess ");
//            mFilePath = filePath;
//        }

//        public void Exit()
//        {
//        }
//    }
//}
