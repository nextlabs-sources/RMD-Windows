//using Microsoft.Office.Interop.Excel;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Runtime.InteropServices;
//using System.Text;
//using System.Threading.Tasks;
//using Viewer.upgrade.application;
//using Viewer.upgrade.exception;
//using Viewer.upgrade.file.basic.utils;

//using Viewer.upgrade.utils;

//namespace Viewer.upgrade.process.office
//{

//    public class ExcelProcess 
//    {
//        public Process Process { get => mProcess; }
//        public int PROPERTY_BROWSER_IS_SUSPENDED = System.Convert.ToInt32("0x800AC472", 16);
//        public int ERROR_CODE_REJECTED_BY_CALLEE = System.Convert.ToInt32("0x80010001", 16);
//        public event EventHandler OfficeProcessExited;
//        public event Action<bool> EditSaved;

//        private log4net.ILog mLog;
//        private string mFilePath = string.Empty;
//        private Process mProcess = null;
//        private Microsoft.Office.Interop.Excel.Application mApplication;
//        private Microsoft.Office.Interop.Excel.Workbook mWorkbook;
//        private EditWatcher mWatcher;

//        [DllImport("user32.dll")]
//        public static extern int GetWindowThreadProcessId(int hWnd, out int lpdwProcessId);

//        public ExcelProcess(string filePath)
//        {
//            ViewerApp viewerApp = (ViewerApp)ViewerApp.Current;
//            this.mLog = viewerApp.Log;
//            this.mFilePath = filePath;
//            try
//            {
//                mApplication = new Microsoft.Office.Interop.Excel.Application();
//                mApplication.EnableEvents = true;
//                mApplication.MergeInstances = false;
//                mApplication.Visible = true;
//                int processId = Int32.MaxValue;
//                GetWindowThreadProcessId(mApplication.Hwnd, out processId);

//                if (Int32.MaxValue != processId)
//                {
//                    mProcess = Process.GetProcessById(processId);
//                    mProcess.EnableRaisingEvents = true;
//                    viewerApp.SdkSession.RPM_RegisterApp(mProcess.MainModule.FileName);
//                    viewerApp.SdkSession.SDWL_RPM_NotifyRMXStatus(true);
//                    viewerApp.SdkSession.RMP_AddTrustedProcess(mProcess.Id);
//                    SkydrmLocal.rmc.sdk.Apis.WaitInstanceInitFinish();

//                    object paramMissing = Type.Missing;
//                    mWorkbook = mApplication.Workbooks.Open(filePath,
//                                                        paramMissing,
//                                                        paramMissing,
//                                                        paramMissing,
//                                                        paramMissing,
//                                                        paramMissing,
//                                                        paramMissing,
//                                                        paramMissing,
//                                                        paramMissing,
//                                                        true, /*fix bug 52104 it should not open the decrypted file for Excel template NXL file(xlt, xltx) 
//                                                    *when click edit button */
//                                                        paramMissing,
//                                                        paramMissing,
//                                                        paramMissing,
//                                                        paramMissing,
//                                                        paramMissing);

//                    mWorkbook.BeforeClose += new WorkbookEvents_BeforeCloseEventHandler(WorkbookEvents_BeforeClose);
//                    mProcess.Exited += Process_Exited_EventHandler;
//                    mWatcher = new EditWatcher(viewerApp.SdkSession, filePath);
//                    mWatcher.MonitorEditAction(viewerApp, filePath, Edited);
//                    Win32Common.BringWindowToTopEx(mProcess.MainWindowHandle);
//                }
//                else
//                {
//                    throw new UnknownException();
//                }
//            }
//            catch (Exception ex)
//            {
//                if (ex.HResult != ERROR_CODE_REJECTED_BY_CALLEE && ex.HResult != PROPERTY_BROWSER_IS_SUSPENDED)
//                {
//                    FinalReleaseComObject();
//                    mLog.Error(ex.Message, ex);
//                    throw ex;
//                }
//            }
//        }

//        private void Process_Exited_EventHandler(object sender, EventArgs e)
//        {
//            OfficeProcessExited?.Invoke(sender, e);
//        }

//        private void Edited(bool obj)
//        {
//            // Exit();
//            EditSaved?.Invoke(obj);
//        }

//        //public override void Launch()
//        //{
//        //    mLog.Info("\t\t\t\t ExcelProcess Launch \r\n");

//        //    //Set the AppId
//        //    // string AppId = "" + DateTime.Now.Ticks; //A random title
//        //    try
//        //    {
//        //        Application = new Microsoft.Office.Interop.Excel.Application();
//        //        Application.EnableEvents = true;
//        //        Application.MergeInstances = false;

//        //    }
//        //    catch (COMException ex)
//        //    {
//        //        throw ex;
//        //    }
//        //}

//        //public override void OpenFile(string filePath)
//        //{
//        //    mLog.Info("\t\t ExcelProcess OpenFile \r\n" +
//        //             "\t\t\t\t filePath :" + filePath + "\r\n");
//        //    Application.Visible = true;

//        //    object paramMissing = Type.Missing;

//        //    try
//        //    {

//        //        Workbook = Application.Workbooks.Open(filePath,
//        //                                            paramMissing,
//        //                                            paramMissing,
//        //                                            paramMissing,
//        //                                            paramMissing,
//        //                                            paramMissing,
//        //                                            paramMissing,
//        //                                            paramMissing,
//        //                                            paramMissing,
//        //                                            true, /*fix bug 52104 it should not open the decrypted file for Excel template NXL file(xlt, xltx) 
//        //                                                   *when click edit button */
//        //                                            paramMissing,
//        //                                            paramMissing,
//        //                                            paramMissing,
//        //                                            paramMissing,
//        //                                            paramMissing);
//        //        Workbook.BeforeClose += new WorkbookEvents_BeforeCloseEventHandler(WorkbookEvents_BeforeClose);
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        mLog.Info("\t\t ExcelProcess OpenFile Error \r\n" +
//        //           "\t\t\t\t Message :" + ex.Message + "\r\n" +
//        //           "\t\t\t\t HResult :" + ex.HResult + "\r\n" +
//        //           "\t\t\t\t ERROR_CODE_REJECTED_BY_CALLEE :" + ERROR_CODE_REJECTED_BY_CALLEE + "\r\n");
//        //        if (ex.HResult != ERROR_CODE_REJECTED_BY_CALLEE && ex.HResult != PROPERTY_BROWSER_IS_SUSPENDED)
//        //        {
//        //            throw;
//        //        }
//        //    }
//        //}

//        public void WorkbookEvents_BeforeClose(ref bool Cancel)
//        {
//            if (!Cancel)
//            {
//                FinalReleaseComObject();
//            }
//        }

//        private void FinalReleaseComObject()
//        {
//            try
//            {
//                if (null != mApplication)
//                {
//                    if (null != mWorkbook)
//                    {
//                        // mWorkbook.Close();
//                        Marshal.FinalReleaseComObject(mWorkbook);
//                    }

//                    if (null != mApplication)
//                    {
//                        Marshal.FinalReleaseComObject(mApplication);
//                    }
//                }
//            }
//            catch (Exception ex)
//            {

//            }
//        }

//        //public override int GetPid()
//        //{
//        //    mLog.Info("\t\t\t\t ExcelProcess GetPid \r\n");
//        //    int processId = Int32.MaxValue;
//        //    try
//        //    {
//        //        GetWindowThreadProcessId(Application.Hwnd, out processId);
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        mLog.Error("\t\t\t\t ExcelProcess GetPid Error \r\n");
//        //        mLog.Error(ex.Message.ToString(), ex);
//        //    }
//        //    mLog.Info("\t\t\t\t\t\t ExcelProcess Pid :" + processId + "\r\n");
//        //    return processId;
//        //}

//        public void Exit()
//        {
//            try
//            {
//                // Quit Word and release the ApplicationClass object.  
//                if (mApplication != null)
//                {
//                    // mApplication.Visible = false;
//                    mApplication.Quit();
//                    mApplication = null;
//                }
//                GC.Collect();
//            }
//            finally
//            {
//            }
//        }

//        //public override IntPtr MainWindowHandle()
//        //{
//        //    return Process.GetProcessById(GetPid()).MainWindowHandle;
//        //}

//        //public override Process GetProcess()
//        //{
//        //    mLog.Info("\t\t\t\t Excel GetProcess Obj \r\n");
//        //    return Process.GetProcessById(GetPid());
//        //}

//    }
//}
