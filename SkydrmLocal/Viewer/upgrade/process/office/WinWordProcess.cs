//using Microsoft.Office.Interop.Word;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Runtime.InteropServices;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using Viewer.upgrade.application;
//using Viewer.upgrade.exception;
//using Viewer.upgrade.file.basic.utils;
//using Viewer.upgrade.process.basic;
//using Viewer.upgrade.utils;

//namespace Viewer.upgrade.process.office
//{
//    public class WinWordProcess : IProcess
//    {
//        public  Process Process { get => mProcess; }
//        public event EventHandler OfficeProcessExited;
//        public event Action<bool> EditSaved;
//        public int ERROR_CODE_REJECTED_BY_CALLEE = System.Convert.ToInt32("0x80010001", 16);

//        private log4net.ILog mLog;
//        private string mFilePath = string.Empty;
//        private Process mProcess = null;
//        private Microsoft.Office.Interop.Word.Application mApplication;
//        private DocumentEvents2_Event mDocumentEvents2_Event;
//        private EditWatcher mWatcher;

//        public WinWordProcess(string filePath)
//        {
//            ViewerApp viewerApp =(ViewerApp)ViewerApp.Current;
//            this.mLog = viewerApp.Log;
//            this.mFilePath = filePath;
//            try
//            {
//                string appId = "" + DateTime.Now.Ticks; //A random title
//                mApplication = new Microsoft.Office.Interop.Word.Application();
//                mApplication.Visible = true;
//                mApplication.Caption = appId;
//                int pid = LoopGetPid(appId);
//                if (int.MaxValue != pid)
//                {
//                    mProcess = Process.GetProcessById(pid);
//                    mProcess.EnableRaisingEvents = true;
//                    viewerApp.SdkSession.RPM_RegisterApp(mProcess.MainModule.FileName);
//                    viewerApp.SdkSession.SDWL_RPM_NotifyRMXStatus(true);
//                    viewerApp.SdkSession.RMP_AddTrustedProcess(mProcess.Id);
//                    SkydrmLocal.rmc.sdk.Apis.WaitInstanceInitFinish();

//                    mApplication.Caption = string.Empty;
//                    mDocumentEvents2_Event = mApplication.Documents.Open(filePath);
//                    mDocumentEvents2_Event.Close += new DocumentEvents2_CloseEventHandler(DocumentEvents2_CloseEvent);

//                    mProcess.Exited += Process_Exited_EventHandler;
//                    mWatcher = new EditWatcher(viewerApp.SdkSession, filePath);
//                    mWatcher.MonitorEditAction(viewerApp, filePath, Edited);
//                    Win32Common.BringWindowToTopEx(mProcess.MainWindowHandle);
//                }
//                else
//                {
//                    mApplication.Caption = string.Empty;
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

//        protected int LoopGetPid(string AppId)
//        {
//            int pid = Int32.MaxValue;

//            int count = 10;
//            while (count > 0) //Loop till u get
//            {
//                count--;
//                pid = ToolKit.GetProcessIdByWindowTitle(AppId);
//                if (pid == Int32.MaxValue)
//                {
//                    Thread.Sleep(500);
//                    continue;
//                }
//                else
//                {
//                    break;
//                }
//            }
//            return pid;
//        }

//        private void Process_Exited_EventHandler(object sender, EventArgs e)
//        {
//            OfficeProcessExited?.Invoke(sender, e);
//        }

//        private void Edited(bool obj)
//        {
//            Exit();
//            EditSaved?.Invoke(obj);
//        }

//        //public override void Launch()
//        //{
//        //    mLog.Info("\t\t\t\t WinWordProcess Launch \r\n");
//        //    try
//        //    {
//        //        //Set the AppId
//        //        string AppId = "" + DateTime.Now.Ticks; //A random title
//        //        Application = new Microsoft.Office.Interop.Word.Application();
//        //        Application.Visible = true;
//        //        Application.Caption = AppId;
//        //        Pid = TryGetPid(AppId);
//        //        Application.Caption = string.Empty;
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        throw ex;
//        //    }
//        //}

//        //public override void OpenFile(string filePath)
//        //{
//        //    mLog.Info("\t\t WinWord OpenFile... \r\n" +
//        //          "\t\t\t\t filePath :" + filePath + "\r\n");

//        //    Application.Visible = true;
//        //    try
//        //    {
//        //        DocumentEvents2_Event = Application.Documents.Open(filePath);
//        //        DocumentEvents2_Event.Close += new DocumentEvents2_CloseEventHandler(DocumentEvents2_CloseEvent);
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        mLog.Info("\t\t WinWord OpenFile Error\r\n" +
//        //         "\t\t\t\t Message :" + ex.Message + "\r\n" +
//        //         "\t\t\t\t HResult :" + ex.HResult + "\r\n" +
//        //         "\t\t\t\t ERROR_CODE_REJECTED_BY_CALLEE :" + ERROR_CODE_REJECTED_BY_CALLEE + "\r\n");
//        //        if (ex.HResult != ERROR_CODE_REJECTED_BY_CALLEE)
//        //        {
//        //            throw;
//        //        }
//        //    }
//        //}


//        public void DocumentEvents2_CloseEvent()
//        {
//         //   FinalReleaseComObject();
//        }

//        private void FinalReleaseComObject()
//        {
//            try
//            {
//                if (null != mDocumentEvents2_Event)
//                {
//                    // _Document document = (_Document)mDocumentEvents2_Event;
//                    //  document.Close();
//                    Marshal.FinalReleaseComObject(mDocumentEvents2_Event);
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
//        //    mLog.Info("\t\t\t\t WinWordProcess Pid :" + Pid + "\r\n");
//        //    return Pid;
//        //}

//        public void Exit()
//        {
//            try
//            {
//                mProcess?.Kill();
//                //if (null != mDocumentEvents2_Event)
//                //{
//                //    _Document document = (_Document)mDocumentEvents2_Event;
//                //    document.Close();
//                //}

//                //if (mApplication != null)
//                //{
//                //    mApplication.Quit(WdSaveOptions.wdDoNotSaveChanges, WdOriginalFormat.wdOriginalDocumentFormat);
//                //    mApplication = null;
//                //}
//                GC.Collect();
//            }
//            catch (Exception ex)
//            {
//            }
//        }

//        //public override IntPtr MainWindowHandle()
//        //{
//        //    return Process.GetProcessById(GetPid()).MainWindowHandle;
//        //}

//        //public override Process GetProcess()
//        //{
//        //    mLog.Info("\t\t\t\t WinWord GetProcess Obj \r\n");
//        //    Process process = null;
//        //    int pid = GetPid();

//        //    if (pid != Int32.MaxValue)
//        //    {
//        //        process = Process.GetProcessById(pid);
//        //    }
//        //    return process;
//        //}


//    }
//}
