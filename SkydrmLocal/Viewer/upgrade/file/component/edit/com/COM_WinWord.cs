using Microsoft.Office.Interop.Word;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.upgrade.exception;
using Viewer.upgrade.utils;

namespace Viewer.upgrade.file.component.edit.com
{
    public class COM_WinWord
    {
        public int ERROR_CODE_REJECTED_BY_CALLEE = System.Convert.ToInt32("0x80010001", 16);
        private ViewerApp mViewerApp;
        private log4net.ILog mLog;
        private string mFilePath = string.Empty;
        private Process mProcess = null;

        public COM_WinWord()
        {
            mViewerApp = (ViewerApp)ViewerApp.Current;
            mLog = mViewerApp.Log;
        }

        public Process Open(string filePath)
        {
            mFilePath = filePath;
            Microsoft.Office.Interop.Word.Application mApplication = null;
            DocumentEvents2_Event mDocumentEvents2_Event = null;
            try
            {
                string appId = "" + DateTime.Now.Ticks; //A random title
                mApplication = new Microsoft.Office.Interop.Word.Application();
                mApplication.Visible = true;
                mApplication.Caption = appId;
                int pid = LoopGetPid(appId);
                if (int.MaxValue != pid)
                {
                    mProcess = Process.GetProcessById(pid);
                    mProcess.EnableRaisingEvents = true;
                    //mViewerApp.SdkSession.RPM_RegisterApp(mProcess.MainModule.FileName);
                    //mViewerApp.SdkSession.SDWL_RPM_NotifyRMXStatus(true);
                    //mViewerApp.SdkSession.RMP_AddTrustedProcess(mProcess.Id);
                    //SkydrmLocal.rmc.sdk.Apis.WaitInstanceInitFinish();

                    mApplication.Caption = string.Empty;


                    try
                    {
                        mDocumentEvents2_Event = mApplication.Documents.Open(filePath);
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
                    mApplication.Caption = string.Empty;
                    throw new UnknownException();
                }
            }
            catch (Exception ex)
            {
                mLog.Error(ex.Message, ex);
                //if (null != mApplication)
                //{
                //    mApplication.Quit();
                //}
                throw ex;
            }
            finally
            {
                if (null != mDocumentEvents2_Event)
                {
                    // _Document document = (_Document)mDocumentEvents2_Event;
                    //  document.Close();
                    Marshal.FinalReleaseComObject(mDocumentEvents2_Event);
                }

                if (null != mApplication)
                {
                    Marshal.FinalReleaseComObject(mApplication);
                }
            }
        }


        private int LoopGetPid(string AppId)
        {
            int pid = Int32.MaxValue;

            int count = 10;
            while (count > 0) //Loop till u get
            {
                count--;
                pid = ToolKit.GetProcessIdByWindowTitle(AppId);
                if (pid == Int32.MaxValue)
                {
                    Thread.Sleep(500);
                    continue;
                }
                else
                {
                    break;
                }
            }
            return pid;
        }

    }
}
