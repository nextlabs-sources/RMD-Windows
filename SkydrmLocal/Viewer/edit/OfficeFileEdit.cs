using Microsoft.Win32;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Viewer.edit.office;
using Viewer.edit.pdf;
using Viewer.utils;

namespace Viewer.edit
{
    public interface IEditFile
    {     
        bool CanEdit(string nxlFileLocalPath);

        void Edit(string NxlFileLocalPath);
    }

    public class EditCallBack
    {
        public bool IsEdit { get; set; }
        public string LocalPath { get; set; }

        public EditCallBack(bool ie, string lp)
        {
            this.IsEdit = ie;
            this.LocalPath = lp;
        }
    }

    public class FileEditorHelper
    {
        public static readonly ConcurrentDictionary<string, OfficeFileEdit> EditingFileMap = new ConcurrentDictionary<string, OfficeFileEdit>();

        public static bool IsbeingFileEdit()
        {
            return !EditingFileMap.IsEmpty && EditingFileMap.Count > 0;
        }

        public static bool IsFileEditing(string nxlDiskPath)
        {
            return EditingFileMap.ContainsKey(nxlDiskPath);
        }

        public static bool HasEditRights(log4net.ILog log, User user, string filePath)
        {
            bool ret = false;

            try
            {
                var fp = user.GetNxlFileFingerPrint(filePath);
                ret = fp.HasRight(FileRights.RIGHT_EDIT);
            }
            catch (Exception e)
            {
                log.Error(e.ToString());
            }
            return ret;
        }

        //public static void Edit(log4net.ILog log, Session session, string nxlFileDiskPath, Action<EditCallBack> OnEditCompleteCallback)
        //{
        //    OfficeFileEdit fileEditor = null;

        //    if (EditingFileMap.ContainsKey(nxlFileDiskPath))
        //    {
        //        EditingFileMap.TryGetValue(nxlFileDiskPath, out fileEditor);
        //    }
        //    else
        //    {
        //        fileEditor = new OfficeFileEdit(log, session);
        //    }

        //    try
        //    {
        //        fileEditor.Edit(nxlFileDiskPath, OnEditCompleteCallback);
        //    }
        //    catch (Exception ex)
        //    {
        //        CommonUtils.ShowBalloonTip(CultureStringInfo.VIEW_DLGBOX_DETAILS_SYSTEM_INTERNAL_ERROR ,false);
        //        log.Error("\t\t Some Error Happended In Edit \r\n");
        //        log.Error(ex.Message, ex);

        //        if (null != fileEditor)
        //        {
        //            IEditProcess editProcess = fileEditor.GetIEditProcess();
        //            if (null != editProcess)
        //            {
        //                editProcess.FinalReleaseComObject();
        //            }
        //        }
        //    }
        //}
    }

    public class OfficeFileEdit : IEditFile
    {
        private log4net.ILog mLog;

        private Session mSession;

        private string mNxlFileLocalPath = string.Empty;

        private IEditProcess mEditProcess;

        private Process mProcess;

        public event Action<Process> FileOpend;

        public event Action<EditCallBack> OnEditCompleteCallback;

        public event Action OnEditProcessExited;

        public OfficeFileEdit(log4net.ILog log , Session session)
        {
            this.mLog = log;
            this.mSession = session;
        }

        public Process GetProcess()
        {
            return this.mProcess;
        }

        public IEditProcess GetIEditProcess()
        {
            return this.mEditProcess;
        }

        public bool CanEdit(string nxlFileLocalPath)
        {
            return FileEditorHelper.HasEditRights(mLog , mSession.User, nxlFileLocalPath);
        }

        public void Edit(string NxlFileLocalPath)
        {
            mLog.Info("\t\t DoEdit \r\n" +
                      "\t\t\t\t NxlFileLocalPath :" + NxlFileLocalPath +
                      "\t\t\t\t OnEditCompleteCallback");

            this.mNxlFileLocalPath = NxlFileLocalPath;

            if (!FileEditorHelper.EditingFileMap.ContainsKey(NxlFileLocalPath))
            {
                mLog.Info("\t\t\t\t CacheRPMFileToken NXlFilePath :" + NxlFileLocalPath + "\r\n");
                //cache token
                mSession.User.CacheRPMFileToken(NxlFileLocalPath);

                mLog.Info("\t\t\t\t ChangeRegeditOfOfficeAddin \r\n");
                //for normal start addin
                Helper.ChangeRegeditOfOfficeAddin(mSession);

                mLog.Info("\t\t\t\t ForceCloseFile_NoThrow NXlFilePath :" + NxlFileLocalPath + "\r\n");
                mSession.User.ForceCloseFile_NoThrow(NxlFileLocalPath);

                mLog.Info("\t\t RPM_EditFile \r\n" +
                         "\t\t\t\t NxlFileLocalPath : " + NxlFileLocalPath + "\r\n");
                //decrypting
                string RpmFilePath = mSession.RPM_EditFile(NxlFileLocalPath);

                FileInfo file = new FileInfo(RpmFilePath + ".nxl");

                FileStream fileStream = null;
                try
                {
                    fileStream = file.OpenRead();
                }
                finally
                {
                    if (null != fileStream)
                    {
                        fileStream.Close();
                    }
                }

                mLog.Info("\t\t PrepareOfficeProcess \r\n" +
                         "\t\t\t\t RpmFilePath : " + RpmFilePath + "\r\n");
                mEditProcess = PrepareOfficeProcess(mLog,RpmFilePath);

                try
                {
                    if (null != mEditProcess)
                    {
                        mEditProcess.Launch();

                        int pid = mEditProcess.GetPid();

                        if (pid != Int32.MaxValue)
                        {
                            mProcess = mEditProcess.GetProcess();

                            if (null != mProcess)
                            {
                                mProcess.EnableRaisingEvents = true;

                                mProcess.Exited += new EventHandler(OnProcessExited);

                                mLog.Info("\t\t RPM_RegisterApp \r\n" +
                                         "\t\t\t\t AppPath : " + mProcess.MainModule.FileName + "\r\n");
                                mSession.RPM_RegisterApp(mProcess.MainModule.FileName);

                                mLog.Info("\t\t Thread.Sleep(500) \r\n");

                                Thread.Sleep(500); //wait RPM RegisterApp

                                mEditProcess.OpenFile(RpmFilePath);

                                FileOpend?.Invoke(mProcess);

                                mLog.Info("\t\t record \r\n");
                                FileEditorHelper.EditingFileMap.TryAdd(NxlFileLocalPath, this);

                                mLog.Info("\t\t BringWindowToTop \r\n");
                                BringWindowToTop(mProcess);

                                mLog.Info("\t\t StartEditMonitor \r\n");
                                // watch edit complete.
                                StartEditMonitor(ViewerApp.Current, mSession,NxlFileLocalPath, RpmFilePath, OnEditCompleteCallback);
                            }
                            else
                            {
                                throw new Exception("Get Process Failed");
                            }
                        }
                        else
                        {
                            throw new Exception("Get Pid Failed");
                        }
                    }
                    else
                    {
                        throw new Exception("Launch Edit Process Failed");
                    }
                }
                catch (Exception ex)
                {
                    mLog.Error("\t\t Some Error Happended In Edit \r\n");
                    mLog.Error(ex.Message, ex);

                    //Transactional rollback
                    DeleteSubKeyValue(RpmFilePath);
                    IEditProcess editProcess = GetIEditProcess();
                    if (null != editProcess)
                    {
                        editProcess.FinalReleaseComObject();
                    }
                    throw ex;
                }
            }
            else
            {
                OfficeFileEdit fileEditor = null;
                FileEditorHelper.EditingFileMap.TryGetValue(NxlFileLocalPath, out fileEditor);
                if (null != fileEditor)
                {
                    Process process = fileEditor.GetProcess();
                    if (null != process)
                    {
                        BringWindowToTop(process);
                    }
                    else
                    {
                        throw new Exception("Get Process Failed");
                    }
                }
            }
        }

        public void DeleteSubKeyValue(string RpmFilePath)
        {
            mLog.Info("\t\t DeleteSubKeyValue \r\n" +
                "\t\t\t\t RpmFilePath :" + RpmFilePath + "\r\n");
            string subKeySession = @"Software\NextLabs\SkyDRM\Session\";
            try
            {
                RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(subKeySession, true);
                registryKey.DeleteValue(RpmFilePath);
            }
            catch (Exception ex)
            {

            }
        }

        public void OnProcessExited(object sender, EventArgs e)
        {
            mProcess.EnableRaisingEvents = false;
            OfficeFileEdit fileEditor = null;
            if (FileEditorHelper.EditingFileMap.ContainsKey(mNxlFileLocalPath))
            {
                if (FileEditorHelper.EditingFileMap.TryRemove(mNxlFileLocalPath, out fileEditor))
                {
                    //mLog.Info("\t\t KillActiveViewer \r\n" +
                    //                 "\t\t\t\t NxlFileLocalPath : " + mNxlFileLocalPath + "\r\n");
                    //ViewerProcess.KillActiveViewer(mNxlFileLocalPath);
                    OnEditProcessExited?.Invoke();
                }
            }
        }

        private bool BringWindowToTop(Process editProcess)
        {
            mLog.Info("BringWindowToTop");

            bool result = false;
            try
            {
                if (null != editProcess)
                {
                    result = Win32Common.BringWindowToTopEx(editProcess.MainWindowHandle);
                }
            }
            catch (PlatformNotSupportedException e)
            {
                mLog.Error(e.Message.ToString(), e);
            }
            catch (InvalidOperationException e)
            {
                mLog.Error(e.Message.ToString(), e);
            }
            catch (NotSupportedException e)
            {
                mLog.Error(e.Message.ToString(), e);
            }
            catch (Exception e)
            {
                mLog.Error(e.Message.ToString(), e);
            }
            return result;
        }


        private IEditProcess PrepareOfficeProcess(log4net.ILog log, string NxlFileLocalPath)
        {

            string ext = Path.GetExtension(NxlFileLocalPath);

            if (string.Equals(ext, ".docx", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".doc", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".dot", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".dotx", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".rtf", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".vsdx", StringComparison.CurrentCultureIgnoreCase))
            {
                return new WinWordProcess(log);
            }

            if (string.Equals(ext, ".pptx", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".ppt", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".ppsx", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".potx", StringComparison.CurrentCultureIgnoreCase))
            {
                return new PowerPntProcess(log);
            }

            if (string.Equals(ext, ".xlsx", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".xls", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".xltx", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".xlt", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".xlsb", StringComparison.CurrentCultureIgnoreCase))
            {
                return new ExcelProcess(log);
            }
            if (string.Equals(ext, ".pdf", StringComparison.CurrentCultureIgnoreCase))
            {
                return new AdobeProcess(log);
            }

            return null;
        }

        private void StartEditMonitor(Application application, Session session, string targetLocalPath, string monitorPath, Action<EditCallBack> callback)
        {
            EditWatcher watcher = new EditWatcher(session, targetLocalPath);
            watcher.MonitorEditAction(application, monitorPath,
                (bool modified) =>
                {
                    mEditProcess.FinalReleaseComObject();

                    OfficeFileEdit fileEditor = null;
                    if (FileEditorHelper.EditingFileMap.TryRemove(targetLocalPath, out fileEditor))
                    {
                         //mLog.Info("\t\t KillActiveViewer \r\n" +
                         //               "\t\t\t\t NxlFileLocalPath : " + targetLocalPath + "\r\n");
                         //   ViewerProcess.KillActiveViewer(targetLocalPath);
                    }

                    EditCallBack bundle = new EditCallBack(modified, targetLocalPath);
                    callback?.Invoke(bundle);

                    try
                    {
                        //Send edit log.
                        //  if (modified)
                        // {
                          //  mSession.User.AddLog(targetLocalPath, NxlOpLog.Edit, true);
                        // }
                    }
                    catch
                    {

                    }
                });
        }
    }

    public class EditWatcher
    {
        private RegistryWatcher mInternalWatcher = new RegistryWatcher();
        private string mLocalPath;
        private FileInfo mSFileInfo;
        private long mStartFileSize;
        private DateTime mStartFileLstModifedTime;
        private Session mSession;

        public EditWatcher(Session session ,string localPath)
        {
            mLocalPath = localPath;
            mSFileInfo = new FileInfo(localPath);
            mStartFileSize = mSFileInfo.Length;
            mStartFileLstModifedTime = mSFileInfo.LastWriteTime;
            mSession = session;
        }

        public void MonitorEditAction(Application application, string rpmPath, Action<bool> OnEditCompleteCallback)
        {
            mInternalWatcher.StartMonitorRegValueDeleted(mSession, rpmPath,
                (bool done) =>
                {
                    if (done)
                    {
                        if (IsFileUnModified())
                        {
                            // Edit for view only.
                            // Send edit finish callback with nomodify params which run on UI thread.
                            application.Dispatcher.BeginInvoke(OnEditCompleteCallback, false);
                        }
                        else
                        {
                            // Edit finished.
                            // Send edit finish callback with modify params which run on UI thread.
                            application.Dispatcher.BeginInvoke(OnEditCompleteCallback, true);
                        }
                    }
                });
        }

        private bool IsFileUnModified()
        {
            string path = mLocalPath;
            //Get start fileinfo recordings.
            long sSize = mStartFileSize;
            DateTime sLstModified = mStartFileLstModifedTime;
            Console.WriteLine("-------------Recording START FileInfo with status writeTime:{0} & size:{1} ", sLstModified, sSize);

            //Re-retrieve file info after edit finished.
            FileInfo eFileInfo = new FileInfo(path);
            long eSize = eFileInfo.Length;
            DateTime eLstModified = eFileInfo.LastWriteTime;
            Console.WriteLine("-------------Recording END FileInfo with status writeTime:{0} & size:{1} ", eLstModified, eSize);

            return DateTime.Equals(sLstModified, eLstModified) && sSize == eSize;
        }

        internal class RegistryWatcher
        {
            public void StartMonitorRegValueDeleted(Session session, string rpmPath, Action<bool> onMonitorDone)
            {
                // pass rmpPath to sdk to start monitor
                new Thread(
                    () =>
                    {
                        session.SDWL_RPM_MonitorRegValueDeleted(rpmPath,
                        (string deletedValueinReg) =>
                        {

                            Console.WriteLine("callback occured" + deletedValueinReg);
                            if (deletedValueinReg.Equals(rpmPath))
                            {
                                Console.WriteLine("the value you monitored has been deleted" + deletedValueinReg);
                                if (null != onMonitorDone)
                                {
                                    onMonitorDone?.Invoke(true);
                                }
                            }

                        });
                    }
                    )
                    .Start();
            }
        }
    }
}
