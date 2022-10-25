using Microsoft.Office.Core;
using Microsoft.Office.Interop.Word;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.process
{
    public class WinWordProcess:OfficeProcess
    {
        private log4net.ILog Log = SkydrmLocalApp.Singleton.Log;

        private Microsoft.Office.Interop.Word.Application Application { get; set; }

        private int Pid { get; set; }

        private DocumentEvents2_Event DocumentEvents2_Event;

        public override void Launch()
        {
            Log.Info("\t\t\t\t WinWordProcess Launch \r\n");
            try
            {
                //Set the AppId
                string AppId = "" + DateTime.Now.Ticks; //A random title
                Application = new Microsoft.Office.Interop.Word.Application();
                Application.Visible = true;
                Application.Caption = AppId;
            
                Pid = TryGetPid(AppId);

                Application.Caption = string.Empty;

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public override void OpenFile(string filePath)
        {
            Log.Info("\t\t WinWord OpenFile... \r\n" +
                  "\t\t\t\t filePath :" + filePath + "\r\n");

            Application.Visible = true;
            try
            {
               DocumentEvents2_Event = Application.Documents.Open(filePath);
               DocumentEvents2_Event.Close += new DocumentEvents2_CloseEventHandler(DocumentEvents2_CloseEvent);
            }
            catch (Exception ex)
            {
                Log.Info("\t\t WinWord OpenFile Error\r\n" +
                 "\t\t\t\t Message :" + ex.Message + "\r\n"+
                 "\t\t\t\t HResult :" + ex.HResult + "\r\n"+
                 "\t\t\t\t ERROR_CODE_REJECTED_BY_CALLEE :"+ ERROR_CODE_REJECTED_BY_CALLEE+"\r\n");
                if (ex.HResult != ERROR_CODE_REJECTED_BY_CALLEE)
                {
                    throw;
                }       
            }
        }
        public void DocumentEvents2_CloseEvent()
        {
                FinalReleaseComObject();
        }

        public override void FinalReleaseComObject()
        {
            base.FinalReleaseComObject();

            try
            {
                if (null!= Application)
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
            Log.Info("\t\t\t\t WinWordProcess Pid :"+ Pid +"\r\n");
            return Pid;
        }

        public override void Close()
        {
            base.Close();
            try
            {
                if (Application != null)
                {
                    Application.Visible = false;
                    Application.Quit(WdSaveOptions.wdDoNotSaveChanges, WdOriginalFormat.wdOriginalDocumentFormat);
                    Application = null;
                }

                GC.Collect();
            }
            catch (Exception ex)
            {

            }
           
        }

        public override IntPtr MainWindowHandle()
        {
            return Process.GetProcessById(GetPid()).MainWindowHandle;
        }

        public override Process GetProcess()
        {
            Log.Info("\t\t\t\t WinWord GetProcess Obj \r\n");
            Process process = null;
            int pid = GetPid();

            if (pid != Int32.MaxValue)
            {
                process = Process.GetProcessById(pid);
            }

            return process;
        }

    }
}
