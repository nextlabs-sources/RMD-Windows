using Microsoft.Office.Core;
using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.edit.office
{
    public class ExcelProcess : OfficeProcess
    {
        private log4net.ILog mLog;

        private Microsoft.Office.Interop.Excel.Application Application { get; set; }

        private Microsoft.Office.Interop.Excel.Workbook Workbook;

        private int PROPERTY_BROWSER_IS_SUSPENDED = System.Convert.ToInt32("0x800AC472", 16);

        [DllImport("user32.dll")]
        public static extern int GetWindowThreadProcessId(int hWnd, out int lpdwProcessId);

        public ExcelProcess(log4net.ILog log)
        {
            this.mLog = log;
        }

        public override void Launch()
        {
            mLog.Info("\t\t\t\t ExcelProcess Launch \r\n");

            //Set the AppId
            // string AppId = "" + DateTime.Now.Ticks; //A random title
            try
            {
                Application = new Microsoft.Office.Interop.Excel.Application();
                Application.EnableEvents = true;
                Application.MergeInstances = false;

            }
            catch (COMException ex)
            {
                throw ex;
            }
        }

        public override void OpenFile(string filePath)
        {
            mLog.Info("\t\t ExcelProcess OpenFile \r\n" +
                     "\t\t\t\t filePath :" + filePath + "\r\n");
            Application.Visible = true;

            object paramMissing = Type.Missing;

            try
            {
               
                Workbook = Application.Workbooks.Open(filePath,
                                                    paramMissing,
                                                    paramMissing,
                                                    paramMissing,
                                                    paramMissing,
                                                    paramMissing, 
                                                    paramMissing,
                                                    paramMissing,
                                                    paramMissing,
                                                    true, /*fix bug 52104 it should not open the decrypted file for Excel template NXL file(xlt, xltx) 
                                                           *when click edit button */
                                                    paramMissing,
                                                    paramMissing,
                                                    paramMissing,
                                                    paramMissing,
                                                    paramMissing);
                Workbook.BeforeClose += new WorkbookEvents_BeforeCloseEventHandler(WorkbookEvents_BeforeClose);
            }
            catch (Exception ex)
            {
                mLog.Info("\t\t ExcelProcess OpenFile Error \r\n" +
                   "\t\t\t\t Message :" + ex.Message + "\r\n"+
                   "\t\t\t\t HResult :" + ex.HResult + "\r\n" +
                   "\t\t\t\t ERROR_CODE_REJECTED_BY_CALLEE :" + ERROR_CODE_REJECTED_BY_CALLEE + "\r\n");
                if (ex.HResult != ERROR_CODE_REJECTED_BY_CALLEE && ex.HResult != PROPERTY_BROWSER_IS_SUSPENDED)
                {
                    throw;
                }      
            }
        }

        public void WorkbookEvents_BeforeClose(ref bool Cancel)
        {
            if (!Cancel)
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
                    if (null != Workbook)
                    {
                        Marshal.FinalReleaseComObject(Workbook);
                    }

                    if (null != Application)
                    {
                        Marshal.FinalReleaseComObject(Application);
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        public override int GetPid()
        {
            mLog.Info("\t\t\t\t ExcelProcess GetPid \r\n");
            int processId = Int32.MaxValue;
            try
            {
                GetWindowThreadProcessId(Application.Hwnd, out processId);       
            }
            catch (Exception ex)
            {
                mLog.Error("\t\t\t\t ExcelProcess GetPid Error \r\n");
                mLog.Error(ex.Message.ToString(),ex);
            }
            mLog.Info("\t\t\t\t\t\t ExcelProcess Pid :"+ processId+"\r\n");
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
                    Application.Visible = false;
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
            mLog.Info("\t\t\t\t Excel GetProcess Obj \r\n");
            return Process.GetProcessById(GetPid());
        }

    }
}
