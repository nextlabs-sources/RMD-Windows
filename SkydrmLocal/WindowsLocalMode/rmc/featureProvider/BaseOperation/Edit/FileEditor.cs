using SkydrmLocal.rmc.process;
using System;
using System.Collections.Generic;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SkydrmLocal.rmc.process.pdf;
using System.Diagnostics;
using SkydrmLocal.rmc.common.helper;
using System.Collections.Concurrent;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui.windows.mainWindow.helper;
using Microsoft.Win32;
using SkydrmLocal.rmc.ui;

namespace SkydrmLocal.rmc.Edit
{

    public interface IEditFile
    {
         bool CanEdit(string nxlFileLocalPath);

         void DoEdit(string NxlFileLocalPath, Action<EditCallBack> OnEditCompleteCallback);
    }

    public class FileEditorHelper
    {
        public static readonly ConcurrentDictionary<string, FileEditor> EditingFileMap = new ConcurrentDictionary<string, FileEditor>();

        public static bool IsbeingFileEdit()
        {
            return !EditingFileMap.IsEmpty && EditingFileMap.Count > 0;
        }

        public static bool IsFileEditing(string nxlDiskPath)
        {
            return EditingFileMap.ContainsKey(nxlDiskPath);
        }

        public static bool IsHasEditRights(string filePath)
        {
            bool ret = false;

            try
            {
                var fp = SkydrmLocalApp.Singleton.Rmsdk.User.GetNxlFileFingerPrint(filePath);
                ret = fp.HasRight(FileRights.RIGHT_EDIT);
            }
            catch (Exception e)
            {
                SkydrmLocalApp.Singleton.Log.Error(e.ToString());
            }
            return ret;
        }

        public static void DoEdit(string nxlFileDiskPath, Action<EditCallBack> OnEditCompleteCallback)
        {
            FileEditor fileEditor = null;

            if (EditingFileMap.ContainsKey(nxlFileDiskPath))
            {
                EditingFileMap.TryGetValue(nxlFileDiskPath, out fileEditor);
            }
            else
            {
                fileEditor = new FileEditor();
            }

            var t = new Thread(()=> {
                try
                {
                    fileEditor.DoEdit(nxlFileDiskPath, OnEditCompleteCallback);
                }
                catch (Exception ex)
                {
                    // Switch into main thread
                    SkydrmLocalApp.Singleton.Dispatcher.Invoke(() =>
                    {
                        SkydrmLocalApp.Singleton.ShowBalloonTip(CultureStringInfo.Common_System_Internal_Error);
                        SkydrmLocalApp.Singleton.Log.Error("\t\t Some Error Happended In Edit \r\n");
                        SkydrmLocalApp.Singleton.Log.Error(ex.Message, ex);
                    });

                    ViewerProcess.KillActiveViewer(nxlFileDiskPath);
                    if (null != fileEditor)
                    {
                        IEditProcess editProcess = fileEditor.GetIEditProcess();
                        if (null != editProcess)
                        {
                            editProcess.FinalReleaseComObject();
                        }
                    }
                }
            });

            t.Name = "EditThread";
            t.IsBackground = true;
            t.Start();
        }
    }

    public class FileEditor : IEditFile
    {
        private SkydrmLocalApp SkydrmLocalApp = SkydrmLocalApp.Singleton;

        private log4net.ILog Log = SkydrmLocalApp.Singleton.Log;

        private string NxlFileLocalPath = string.Empty;

        private IEditProcess EditProcess;

        private Process Process;

        public Process GetProcess()
        {
            return this.Process;
        }

        public IEditProcess GetIEditProcess()
        {
            return this.EditProcess;
        }

        public bool CanEdit(string nxlFileLocalPath)
        {
            return FileEditorHelper.IsHasEditRights(nxlFileLocalPath);
        }

        public void DoEdit(string NxlFileLocalPath, Action<EditCallBack> OnEditCompleteCallback)
        {

            Log.Info("\t\t DoEdit \r\n" +
                     "\t\t\t\t NxlFileLocalPath :" + NxlFileLocalPath +
                     "\t\t\t\t OnEditCompleteCallback");

            this.NxlFileLocalPath = NxlFileLocalPath;

            if (!FileEditorHelper.EditingFileMap.ContainsKey(NxlFileLocalPath))
            {

                Log.Info("\t\t\t\t CacheRPMFileToken NXlFilePath :" + NxlFileLocalPath + "\r\n");
                //cache token
                SkydrmLocalApp.Rmsdk.User.CacheRPMFileToken(NxlFileLocalPath);

                Log.Info("\t\t\t\t ChangeRegeditOfOfficeAddin \r\n");
                //for normal start addin
                fileSystem.external.Helper.ChangeRegeditOfOfficeAddin();

                Log.Info("\t\t\t\t ForceCloseFile_NoThrow NXlFilePath :" + NxlFileLocalPath + "\r\n");
                SkydrmLocalApp.Rmsdk.User.ForceCloseFile_NoThrow(NxlFileLocalPath);

                Log.Info("\t\t RPM_EditFile \r\n" +
                         "\t\t\t\t NxlFileLocalPath : " + NxlFileLocalPath + "\r\n");
                //decrypting
                string RpmFilePath = SkydrmLocalApp.Rmsdk.RPM_EditFile(NxlFileLocalPath);

                FileInfo file = new FileInfo(RpmFilePath + ".nxl");

                System.IO.FileStream fileStream = null;
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

                Log.Info("\t\t PrepareOfficeProcess \r\n" +
                         "\t\t\t\t RpmFilePath : " + RpmFilePath + "\r\n");
                EditProcess = PrepareOfficeProcess(RpmFilePath);

                try
                {
                    if (null != EditProcess)
                    {
                        EditProcess.Launch();

                        int pid = EditProcess.GetPid();

                        if (pid != Int32.MaxValue)
                        {
                            Process = EditProcess.GetProcess();

                            if (null != Process)
                            {
                                Process.EnableRaisingEvents = true;

                                Process.Exited += new EventHandler(OnEditProcessExited);

                                Log.Info("\t\t RPM_RegisterApp \r\n" +
                                         "\t\t\t\t AppPath : " + Process.MainModule.FileName + "\r\n");
                                SkydrmLocalApp.Rmsdk.RPM_RegisterApp(Process.MainModule.FileName);

                                Log.Info("\t\t Thread.Sleep(500) \r\n");
                                Thread.Sleep(500); //wait RPM RegisterApp

                                EditProcess.OpenFile(RpmFilePath);

                                ViewerProcess.HideViewer(NxlFileLocalPath);

                                Log.Info("\t\t record \r\n");
                                FileEditorHelper.EditingFileMap.TryAdd(NxlFileLocalPath, this);

                                Log.Info("\t\t BringWindowToTop \r\n");
                                BringWindowToTop(EditProcess);

                                Log.Info("\t\t StartEditMonitor \r\n");
                                StartEditMonitor(NxlFileLocalPath, RpmFilePath, OnEditCompleteCallback);
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
                    DeleteSubKeyValue(RpmFilePath);
                    throw;
                }
            }
            else
            {
                FileEditor fileEditor = null;
                FileEditorHelper.EditingFileMap.TryGetValue(NxlFileLocalPath, out fileEditor);
                if (null != fileEditor)
                {
                    Process process = fileEditor.GetProcess();
                    if (null != process)
                    {
                        BringWindowToTop(EditProcess);
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
            Log.Info("\t\t DeleteSubKeyValue \r\n" +
                "\t\t\t\t RpmFilePath :"+ RpmFilePath+"\r\n");
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

        public void OnEditProcessExited(object sender, EventArgs e)
        {
            SkydrmLocalApp.Singleton.Log.Info("OnEditProcessExited.");

            Process.EnableRaisingEvents = false;
            FileEditor fileEditor = null;
            if (FileEditorHelper.EditingFileMap.ContainsKey(NxlFileLocalPath))
            {
                if (FileEditorHelper.EditingFileMap.TryRemove(NxlFileLocalPath, out fileEditor))
                {
                    Log.Info("\t\t KillActiveViewer \r\n" +
                                     "\t\t\t\t NxlFileLocalPath : " + NxlFileLocalPath + "\r\n");

                    ViewerProcess.KillActiveViewer(NxlFileLocalPath);
                }
            }

            EditMap.Remove(NxlFileLocalPath);
        }

        private void BringWindowToTop(IEditProcess editProcess)
        {
            Log.Info("BringWindowToTop");
            try
            {
                if (null != editProcess)
                {
                    Win32Common.BringWindowToTop(EditProcess.MainWindowHandle(), EditProcess.GetProcess());
                }
            }
            catch (PlatformNotSupportedException e)
            {
                Log.Error(e.Message.ToString(), e);
            }
            catch (InvalidOperationException e)
            {
                Log.Error(e.Message.ToString(), e);
            }
            catch (NotSupportedException e)
            {
                Log.Error(e.Message.ToString(), e);
            }
            catch (Exception e)
            {
                Log.Error(e.Message.ToString(), e);
            }
        }


        private IEditProcess PrepareOfficeProcess(string NxlFileLocalPath)
        {

            string ext = Path.GetExtension(NxlFileLocalPath);

            if (string.Equals(ext, ".docx", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".doc", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".dot", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".dotx", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".rtf", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".vsdx", StringComparison.CurrentCultureIgnoreCase))
            {
                return new WinWordProcess();
            }

            if (string.Equals(ext, ".pptx", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".ppt", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".ppsx", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".potx", StringComparison.CurrentCultureIgnoreCase))
            {
                return new PowerPntProcess();
            }

            if (string.Equals(ext, ".xlsx", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".xls", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".xltx", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".xlt", StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(ext, ".xlsb", StringComparison.CurrentCultureIgnoreCase))
            {
                return new ExcelProcess();
            }
            if (string.Equals(ext, ".pdf", StringComparison.CurrentCultureIgnoreCase))
            {
                return new AdobeProcess();
            }

            return null;
        }

        private void StartEditMonitor(string targetLocalPath,string monitorPath, Action<EditCallBack> callback)
        {
            EditWatcher watcher = new EditWatcher(targetLocalPath);
            watcher.MonitorEditAction(monitorPath,
                (bool modified) =>
                {
                    EditProcess.FinalReleaseComObject();

                    FileEditor fileEditor = null;
                    if (FileEditorHelper.EditingFileMap.TryRemove(targetLocalPath, out fileEditor))
                    {
                        Log.Info("\t\t KillActiveViewer \r\n" +
                                       "\t\t\t\t NxlFileLocalPath : " + targetLocalPath + "\r\n");
                        ViewerProcess.KillActiveViewer(targetLocalPath);
                    }

                    EditCallBack bundle = new EditCallBack(modified, targetLocalPath);
                    callback?.Invoke(bundle);

                    try
                    {
                        //Send edit log.
                        //  if (modified)
                        // {
                        EditLog.SendLog(targetLocalPath, true);
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

        public EditWatcher(string localPath)
        {
            mLocalPath = localPath;
            mSFileInfo = new FileInfo(localPath);

            mStartFileSize = mSFileInfo.Length;
            mStartFileLstModifedTime = mSFileInfo.LastWriteTime;
        }

        public void MonitorEditAction(string rpmPath, Action<bool> OnEditCompleteCallback)
        {
            mInternalWatcher.StartMonitorRegValueDeleted(rpmPath,
                (bool done) =>
                {
                    if (done)
                    {
                        if (IsFileUnModified())
                        {
                            // Edit for view only.
                            // Send edit finish callback with nomodify params which run on UI thread.
                            SkydrmLocalApp.Current.Dispatcher.BeginInvoke(OnEditCompleteCallback, false);
                        }
                        else
                        {
                            // Edit finished.
                            // Send edit finish callback with modify params which run on UI thread.
                            SkydrmLocalApp.Current.Dispatcher.BeginInvoke(OnEditCompleteCallback, true);
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
            private SkydrmLocalApp app = SkydrmLocalApp.Singleton;

            public void StartMonitorRegValueDeleted(string rpmPath, Action<bool> onMonitorDone)
            {
                // Pass rmpPath to sdk to start monitor.
                // Now this function is invoked in sub-thread.
                app.Rmsdk.SDWL_RPM_MonitorRegValueDeleted(rpmPath,
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
        }
    }

    public class EditLog
    {
        public static void SendLog(string LocalDiskPath, bool isAllow)
        {
            //Send edit log only.
            SendLog(LocalDiskPath, NxlOpLog.Edit, isAllow);
        }

        private static void SendLog(string LocalDiskPath, NxlOpLog op, bool isAllow)
        {
            try
            {
                //AddNxlFileLog
                SkydrmLocalApp.Singleton.User.AddNxlFileLog(LocalDiskPath, op, isAllow);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
