using Microsoft.Office.Interop.PowerPoint;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.edit.office
{
    public class PowerPntProcess : OfficeProcess
    {
        private log4net.ILog mLog;

        private Microsoft.Office.Interop.PowerPoint.Application Application { get; set; }

        private string FileDiskPath = string.Empty;

        [DllImport("user32.dll")]
        public static extern int GetWindowThreadProcessId(int hWnd, out int lpdwProcessId);

        public PowerPntProcess(log4net.ILog log)
        {
            this.mLog = log;
        }

        public override void Launch()
        {
            mLog.Info("\t\t\t\t PowerPntProcess Launch \r\n");
            try
            {
                Application = new Microsoft.Office.Interop.PowerPoint.Application();

            }
            catch (COMException ex)
            {
                throw ex;
            }
        }

        public override void OpenFile(string filePath)
        {
            mLog.Info("\t\t PowerPnt OpenFile \r\n" +
                    "\t\t\t\t filePath :" + filePath + "\r\n");
            this.FileDiskPath = filePath;
            Application.Visible = Microsoft.Office.Core.MsoTriState.msoTrue;
            try
            {
                Presentation presentation = Application.Presentations.Open(filePath);
                Application.PresentationCloseFinal += new EApplication_PresentationCloseFinalEventHandler(EApplication_PresentationCloseFinalEvent);
            }
            catch (Exception ex)
            {
                mLog.Info("\t\t PowerPnt OpenFile Error\r\n" +
                 "\t\t\t\t Message :" + ex.Message + "\r\n"+
                 "\t\t\t\t HResult :" + ex.HResult + "\r\n" +
                 "\t\t\t\t ERROR_CODE_REJECTED_BY_CALLEE :" + ERROR_CODE_REJECTED_BY_CALLEE + "\r\n");

                if (ex.HResult != ERROR_CODE_REJECTED_BY_CALLEE)
                {
                    throw;
                }
            }
        }

        public void EApplication_PresentationCloseFinalEvent(Presentation Pres)
        {
            if (string.Equals(FileDiskPath, Pres.FullName, StringComparison.CurrentCultureIgnoreCase))
            {
                FinalReleaseComObject();
            }
        }

        public override void FinalReleaseComObject()
        {
            try
            {
                if (null != Application)
                {
                    Marshal.FinalReleaseComObject(Application);
                }
            }
            catch (Exception ex)
            {

            }
        }

        public override int GetPid()           
        {
            mLog.Info("\t\t\t\t PowerPntProcess GetPid \r\n");
            int processId = Int32.MaxValue;
            try
            {
                GetWindowThreadProcessId(Application.HWND, out processId);
            }
            catch (Exception ex)
            {
                mLog.Error("\t\t\t\t PowerPntProcess GetPid Error \r\n");
                mLog.Error(ex.Message.ToString(), ex);
            }
            mLog.Info("\t\t\t\t PowerPntProcess Pid :" + processId+"\r\n");
            return processId;
        }
        public override void Close()
        {
            base.Close();

            try
            {
                // Quit Word and release the ApplicationClass object.  
                if (Application != null)
                {                   
                    Application.Quit();
                    Application = null;
                }

                GC.Collect();                      
            }
            finally
            {

            }
        }

        public override IntPtr MainWindowHandle()
        {
            return Process.GetProcessById(GetPid()).MainWindowHandle;
        }

        public override Process GetProcess()
        {
            mLog.Info("\t\t\t\t PowerPnt GetProcess Obj \r\n");
            return Process.GetProcessById(GetPid());
        }
    }
}
