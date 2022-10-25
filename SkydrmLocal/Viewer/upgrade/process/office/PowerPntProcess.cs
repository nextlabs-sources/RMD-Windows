//using Microsoft.Office.Interop.PowerPoint;
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
//using Viewer.upgrade.process.basic;
//using Viewer.upgrade.utils;

//namespace Viewer.upgrade.process.office
//{

//    public class PowerPntProcess : IProcess
//    {
//        public  Process Process { get => mProcess; }
//        public event EventHandler OfficeProcessExited;
//        public event Action<bool> EditSaved;
//        public int ERROR_CODE_REJECTED_BY_CALLEE = System.Convert.ToInt32("0x80010001", 16);

//        private log4net.ILog mLog;
//        private string mFilePath = string.Empty;
//        private Microsoft.Office.Interop.PowerPoint.Application mApplication;
//        private Process mProcess = null;
//        private Presentation mPresentation;
//        private EditWatcher mWatcher;

//        [DllImport("user32.dll")]
//        public static extern int GetWindowThreadProcessId(int hWnd, out int lpdwProcessId);

//        public PowerPntProcess(string filePath)
//        {
//            ViewerApp viewerApp = (ViewerApp)ViewerApp.Current;
//            this.mLog = viewerApp.Log;
//            this.mFilePath = filePath;
//            try
//            {
//                mApplication = new Microsoft.Office.Interop.PowerPoint.Application();
//                mApplication.Visible = Microsoft.Office.Core.MsoTriState.msoTrue;
//                int processId = int.MaxValue;
//                GetWindowThreadProcessId(mApplication.HWND, out processId);
//                if (int.MaxValue != processId)
//                {
//                    mProcess = Process.GetProcessById(processId);
//                    mProcess.EnableRaisingEvents = true;
//                    viewerApp.SdkSession.RPM_RegisterApp(mProcess.MainModule.FileName);
//                    viewerApp.SdkSession.SDWL_RPM_NotifyRMXStatus(true);
//                    viewerApp.SdkSession.RMP_AddTrustedProcess(mProcess.Id);
//                    SkydrmLocal.rmc.sdk.Apis.WaitInstanceInitFinish();
             
//                    mPresentation = mApplication.Presentations.Open(filePath);
//                    mApplication.PresentationCloseFinal += new EApplication_PresentationCloseFinalEventHandler(EApplication_PresentationCloseFinalEvent);

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
//                if (ex.HResult != ERROR_CODE_REJECTED_BY_CALLEE)
//                {
//                    FinalReleaseComObject();
//                    mLog.Error(ex.Message, ex);
//                    throw ex;
//                }
//            }
//        }


//        private void Process_Exited_EventHandler(object sender, EventArgs e)
//        {
//            OfficeProcessExited?.Invoke(sender,e);
//        }

//        private void Edited(bool obj)
//        {
//            Exit();
//            EditSaved?.Invoke(obj);
//        }

//        //public override void Launch()
//        //{
//        //    mLog.Info("\t\t\t\t PowerPntProcess Launch \r\n");
//        //    try
//        //    {
//        //        Application = new Microsoft.Office.Interop.PowerPoint.Application();

//        //    }
//        //    catch (COMException ex)
//        //    {
//        //        throw ex;
//        //    }
//        //}

//        //public override void OpenFile(string filePath)
//        //{
//        //    mLog.Info("\t\t PowerPnt OpenFile \r\n" +
//        //            "\t\t\t\t filePath :" + filePath + "\r\n");
//        //    this.FileDiskPath = filePath;
//        //    Application.Visible = Microsoft.Office.Core.MsoTriState.msoTrue;
//        //    try
//        //    {
//        //        Presentation presentation = Application.Presentations.Open(filePath);
//        //        Application.PresentationCloseFinal += new EApplication_PresentationCloseFinalEventHandler(EApplication_PresentationCloseFinalEvent);
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        mLog.Info("\t\t PowerPnt OpenFile Error\r\n" +
//        //         "\t\t\t\t Message :" + ex.Message + "\r\n" +
//        //         "\t\t\t\t HResult :" + ex.HResult + "\r\n" +
//        //         "\t\t\t\t ERROR_CODE_REJECTED_BY_CALLEE :" + ERROR_CODE_REJECTED_BY_CALLEE + "\r\n");

//        //        if (ex.HResult != ERROR_CODE_REJECTED_BY_CALLEE)
//        //        {
//        //            throw;
//        //        }
//        //    }
//        //}

//        public void EApplication_PresentationCloseFinalEvent(Presentation Pres)
//        {
//            if (string.Equals(mFilePath, Pres.FullName, StringComparison.CurrentCultureIgnoreCase))
//            {
//              //  FinalReleaseComObject();
//            }
//        }

//        private void FinalReleaseComObject()
//        {
//            try
//            {
//                if (null != mPresentation)
//                {
//                   // mPresentation.Close();
//                    Marshal.FinalReleaseComObject(mPresentation);
//                }
//                if (null != mApplication)
//                {
//                    Marshal.FinalReleaseComObject(mApplication);
//                }
//            }
//            catch (Exception ex)
//            {

//            }
//        }

//        //public override int GetPid()
//        //{
//        //    mLog.Info("\t\t\t\t PowerPntProcess GetPid \r\n");
//        //    int processId = Int32.MaxValue;
//        //    try
//        //    {
//        //        GetWindowThreadProcessId(Application.HWND, out processId);
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        mLog.Error("\t\t\t\t PowerPntProcess GetPid Error \r\n");
//        //        mLog.Error(ex.Message.ToString(), ex);
//        //    }
//        //    mLog.Info("\t\t\t\t PowerPntProcess Pid :" + processId + "\r\n");
//        //    return processId;
//        //}

//        public void Exit()
//        {
//            try
//            {
//                //// Quit Word and release the ApplicationClass object.  
//                //if (mApplication != null)
//                //{
//                //    mApplication.Quit();
//                //    mApplication = null;
//                //}
//                GC.Collect();
//            }
//            finally
//            {
//            }
//        }


//        //public override void Close()
//        //{
//        //    base.Close();

//        //    try
//        //    {
//        //        // Quit Word and release the ApplicationClass object.  
//        //        if (Application != null)
//        //        {
//        //            Application.Quit();
//        //            Application = null;
//        //        }

//        //        GC.Collect();
//        //    }
//        //    finally
//        //    {
//        //    }
//        //}

//        //public override IntPtr MainWindowHandle()
//        //{
//        //    return Process.GetProcessById(GetPid()).MainWindowHandle;
//        //}

//        //public override Process GetProcess()
//        //{
//        //    mLog.Info("\t\t\t\t PowerPnt GetProcess Obj \r\n");
//        //    return Process.GetProcessById(GetPid());
//        //}
//    }
//}
