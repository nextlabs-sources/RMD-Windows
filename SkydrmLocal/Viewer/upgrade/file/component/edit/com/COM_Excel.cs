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
    public class COM_Excel
    {
        public int PROPERTY_BROWSER_IS_SUSPENDED = System.Convert.ToInt32("0x800AC472", 16);
        public int ERROR_CODE_REJECTED_BY_CALLEE = System.Convert.ToInt32("0x80010001", 16);
        private ViewerApp mViewerApp;
        private log4net.ILog mLog;
        private string mFilePath = string.Empty;
        private Process mProcess = null;

        [DllImport("user32.dll")]
        public static extern int GetWindowThreadProcessId(int hWnd, out int lpdwProcessId);

        public COM_Excel()
        {
            mViewerApp = (ViewerApp)ViewerApp.Current;
            mLog = mViewerApp.Log;
        }

        public Process Open(string filePath)
        {
            mFilePath = filePath;
            Microsoft.Office.Interop.Excel.Application application = null;
            Microsoft.Office.Interop.Excel.Workbook workbook = null;
            try
            {
                application = new Microsoft.Office.Interop.Excel.Application();
                application.EnableEvents = true;
                application.MergeInstances = false;
                application.Visible = true;
                int processId = Int32.MaxValue;
                GetWindowThreadProcessId(application.Hwnd, out processId);

                if (Int32.MaxValue != processId)
                {
                    mProcess = Process.GetProcessById(processId);
                    mProcess.EnableRaisingEvents = true;
                    //mViewerApp.SdkSession.RPM_RegisterApp(mProcess.MainModule.FileName);
                    //mViewerApp.SdkSession.SDWL_RPM_NotifyRMXStatus(true);
                    //mViewerApp.SdkSession.RMP_AddTrustedProcess(mProcess.Id);
                    //SkydrmLocal.rmc.sdk.Apis.WaitInstanceInitFinish();

                    try
                    {
                        object paramMissing = Type.Missing;
                        workbook = application.Workbooks.Open(filePath,
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
                    }
                    catch (Exception e)
                    {
                        if (e.HResult != ERROR_CODE_REJECTED_BY_CALLEE && e.HResult != PROPERTY_BROWSER_IS_SUSPENDED)
                        {
                            throw e;
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
                //    application.Quit();
                //}
                throw ex;
            }
            finally
            {
                if (null != workbook)
                {
                    // mWorkbook.Close();
                    Marshal.FinalReleaseComObject(workbook);
                }

                if (null != application)
                {
                    Marshal.FinalReleaseComObject(application);
                }
            }
        }
    }
}
