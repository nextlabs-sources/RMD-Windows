using Microsoft.Office.Core;
using Microsoft.Office.Interop.PowerPoint;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Viewer.upgrade.exception;
using Viewer.upgrade.utils;

namespace Viewer.upgrade.file.component.edit.com
{
    public class COM_PowerPnt
    {
        public int ERROR_CODE_REJECTED_BY_CALLEE = System.Convert.ToInt32("0x80010001", 16);
        private ViewerApp mViewerApp;
        private log4net.ILog mLog;
        private string mFilePath = string.Empty;
        private Process mProcess = null;
        private const string ADDIN_PROGID = "NxlRMAddin";
        private const string ADDIN_GUID = "{0CCA3189-F325-4D58-AB6D-212CD76C3322}";

        [DllImport("user32.dll")]
        public static extern int GetWindowThreadProcessId(int hWnd, out int lpdwProcessId);

        public COM_PowerPnt()
        {
            mViewerApp = (ViewerApp)ViewerApp.Current;
            mLog = mViewerApp.Log;
        }

        public Process Open(string filePath)
        {
            mFilePath = filePath;
            Microsoft.Office.Interop.PowerPoint.Application application = null;
            Presentation presentation = null;
            try
            {
                application = new Microsoft.Office.Interop.PowerPoint.Application();
                application.Visible = Microsoft.Office.Core.MsoTriState.msoTrue;
                CheckStatusOfNxlRMAddin(application);
                int processId = int.MaxValue;
                GetWindowThreadProcessId(application.HWND, out processId);
                if (int.MaxValue != processId)
                {
                    mProcess = Process.GetProcessById(processId);
                    mProcess.EnableRaisingEvents = true;
                    //mViewerApp.SdkSession.RPM_RegisterApp(mProcess.MainModule.FileName);
                    //mViewerApp.SdkSession.SDWL_RPM_NotifyRMXStatus(true);
                    //mViewerApp.SdkSession.RMP_AddTrustedProcess(mProcess.Id);
                    //SkydrmLocal.rmc.sdk.Apis.WaitInstanceInitFinish();

                    try
                    {
                        presentation = application.Presentations.Open(filePath);
                    }
                    catch (Exception ex)
                    {
                        if (ex.HResult != ERROR_CODE_REJECTED_BY_CALLEE)
                        {
                            throw ex;
                        }
                    }

                    Win32Common.BringWindowToTopEx(mProcess.MainWindowHandle);

                    return mProcess;
                }
                else
                {
                    throw new UnknownException();
                }
            }
            catch (Exception ex)
            {
                mLog.Error(ex.Message, ex);
                //if (null != application)
                //{
                //    try
                //    {
                //        application.Quit();
                //    }
                //    catch (Exception eex)
                //    {
                //    }
                //}
                throw ex;
            }
            finally
            {
                if (null != presentation)
                {
                    // mPresentation.Close();
                    Marshal.FinalReleaseComObject(presentation);
                }
                if (null != application)
                {
                    Marshal.FinalReleaseComObject(application);
                }
            }
        }

        private void CheckStatusOfNxlRMAddin(Microsoft.Office.Interop.PowerPoint.Application application)
        {
            bool haveFound = false;
            COMAddIn cOMAddIn = null;
            COMAddIns cOMAddIns = application.COMAddIns;
            for (int i = 1; i <= cOMAddIns.Count; i++)
            {
                cOMAddIn = cOMAddIns.Item(i);
                string guid = cOMAddIn.Guid;
                string progId = cOMAddIn.ProgId;
                if (String.Equals(ADDIN_GUID, guid, StringComparison.CurrentCultureIgnoreCase)
                    && String.Equals(ADDIN_PROGID, progId, StringComparison.CurrentCultureIgnoreCase))
                {
                    haveFound = true;
                    break;
                }
            }

            if (haveFound && (null != cOMAddIn))
            {
                bool connect = cOMAddIn.Connect;
                if (!connect)
                {
                    throw new NxlRMAddinUnloadException();
                }
            }
            else
            {
                throw new NxlRMAddinUnloadException();
            }
        }
    }
}
