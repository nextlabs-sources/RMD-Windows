using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.upgrade.exception;
using Viewer.upgrade.file.basic;
using Viewer.upgrade.file.component.edit.com;
using Viewer.upgrade.utils;

namespace Viewer.upgrade.file.component.edit
{
    public class EditProcess
    {
        public event Action<bool> EditSaved;
        public event Action ProcessExited;
        private ViewerApp mViewerApp;
        private log4net.ILog mLog;
        private string mRpmFilePath = string.Empty;
        private string mNxlFilePath = string.Empty;
        private Process mProcess = null;
        private DateTime mLastWriteTime;
        private long mLength;
      //  private string mArguments;
        private COM_Excel mCOM_Excel = new COM_Excel();
        private COM_PowerPnt mCOM_PowerPnt = new COM_PowerPnt();
        private COM_WinWord mCOM_WinWord = new COM_WinWord();

        public EditProcess(_StdNxlFile stdNxlFile)
        {
            try
            {
                mViewerApp = (ViewerApp)ViewerApp.Current;
                mLog = mViewerApp.Log;
                mNxlFilePath = stdNxlFile.FilePath;
                FileInfo oriFileInfo = new FileInfo(mNxlFilePath);
                mLastWriteTime = oriFileInfo.LastWriteTime;
                mLength = oriFileInfo.Length;
                mRpmFilePath = mViewerApp.SdkSession.RPM_EditFile(mNxlFilePath);
                FileUtils.WIN32_FIND_DATA pNextInfo;
                FileUtils.FindFirstFile(mRpmFilePath + ".nxl", out pNextInfo);

                if (ToolKit.WORD_EXTENSIONS.Contains(stdNxlFile.Extention))
                {
                    //  mArguments = "/w";
                    //  mProcess = System.Diagnostics.Process.Start(mRpmFilePath, mArguments);
                    mProcess = mCOM_WinWord.Open(mRpmFilePath);
                }
                else if (ToolKit.EXCEL_EXTENSIONS.Contains(stdNxlFile.Extention))
                {
                  //  mArguments = "/x";
                  //  mProcess = System.Diagnostics.Process.Start(mRpmFilePath, mArguments);
                    mProcess = mCOM_Excel.Open(mRpmFilePath);
                }
                else if (ToolKit.POWERPOINT_EXTENSIONS.Contains(stdNxlFile.Extention))
                {
                    //   mArguments = "/O";
                    //   mProcess = System.Diagnostics.Process.Start(mRpmFilePath, mArguments);
                    mProcess = mCOM_PowerPnt.Open(mRpmFilePath);
                }

                //if (ToolKit.PDF_EXTENSIONS.Contains(mExtention))
                //{
                //    return new AdobeProcess();
                //}

                if (null != mProcess)
                {
                    if (!mProcess.HasExited)
                    {
                        mProcess.Exited += ExitedEventHandler;
                    }
                }
                else
                {
                    throw new UnknownException();
                }

                ToolKit.SaveHwndToRegistry(stdNxlFile.FilePath, mProcess.MainWindowHandle);
                MonitorRpmFile();
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(mRpmFilePath))
                {
                    DeleteSubKeyValue(mRpmFilePath);
                }
                throw ex;
            }
        }

        //public EditProcess(string nxlFilePath, string arguments)
        //{
        //    try
        //    {
        //        mViewerApp = (ViewerApp)ViewerApp.Current;
        //        mLog = mViewerApp.Log;
        //        mNxlFilePath = nxlFilePath;
        //        mArguments = arguments;
        //        FileInfo oriFileInfo = new FileInfo(mNxlFilePath);
        //        mLastWriteTime = oriFileInfo.LastWriteTime;
        //        mLength = oriFileInfo.Length;
        //        mRpmFilePath = mViewerApp.SdkSession.RPM_EditFile(nxlFilePath);
        //     // mProcess = mCOM_Excel.Open(mRpmFilePath);
        //        mProcess = System.Diagnostics.Process.Start(mRpmFilePath , arguments);
        //        if (null != mProcess)
        //        {
        //           // LoopGetMainWindowHandle();
        //            if (!mProcess.HasExited)
        //            {
        //                mProcess.Exited += ExitedEventHandler;
        //            }
        //        }
        //        MonitorRpmFile();
        //    }
        //    catch (Exception ex)
        //    {
        //        if (!string.IsNullOrEmpty(mRpmFilePath))
        //        {
        //            DeleteSubKeyValue(mRpmFilePath);
        //        }
        //        throw ex;
        //    }
        //}

        private void DeleteSubKeyValue(string RpmFilePath)
        {
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


        public void ShowWindow()
        {
            if (null != mProcess)
            {
               Win32Common.BringWindowToTopEx(mProcess.MainWindowHandle);
            }
        }

        private void MonitorRpmFile()
        {
            System.IO.FileSystemWatcher watcher = new System.IO.FileSystemWatcher();
            watcher.Path = Path.GetDirectoryName(mRpmFilePath);

            // Watch for changes in LastAccess and LastWrite times, and
            // the renaming of files or directories.
            // watcher.NotifyFilter = NotifyFilters.Size;
            //| NotifyFilters.CreationTime
            //| NotifyFilters.LastWrite;

            // Only watch text files.
            watcher.Filter = Path.GetFileName(mRpmFilePath);

            // Add event handlers.
            watcher.Deleted += (object source, System.IO.FileSystemEventArgs e) =>
            {
                if (e.ChangeType == System.IO.WatcherChangeTypes.Deleted && string.Equals(e.FullPath, mRpmFilePath, StringComparison.CurrentCultureIgnoreCase))
                {
                    try
                    {
                        watcher.EnableRaisingEvents = false;
                        FileInfo fileInfo = new FileInfo(mNxlFilePath);
                        if (DateTime.Equals(mLastWriteTime, fileInfo.LastWriteTime) && mLength == fileInfo.Length)
                        {
                            EditSaved?.Invoke(false);
                        }
                        else
                        {
                            EditSaved?.Invoke(true);
                        }
                    }
                    catch (Exception ex)
                    {
                        EditSaved?.Invoke(false);
                        mLog.Error(ex);
                    }
                }
            };
            // Begin watching.
            watcher.EnableRaisingEvents = true;
        }

        public void ExitedEventHandler(object sender, EventArgs e)
        {
            ProcessExited?.Invoke();
        }


        //public void StartMonitorRegValueDeleted(Session session, string rpmPath, Action<bool> onMonitorDone)
        //{
        //    // pass rmpPath to sdk to start monitor
        //    Thread thread = new Thread(() =>
        //    {
        //        while (true)
        //        {
        //            RegistryKey regSession = null;
        //            try
        //            {
        //                regSession = Registry.CurrentUser.OpenSubKey(@"Software\Nextlabs\SkyDRM\Session");
        //                if(null == regSession.OpenSubKey(rpmPath, false))
        //                {
        //                    onMonitorDone?.Invoke(true);
        //                    return;
        //                }
        //            }
        //            catch
        //            {
        //                // ignroe
        //            }
        //            Thread.Sleep(1000);
        //        }
        //    });
        //    thread.IsBackground = true;
        //    thread.Start();
        //}

    }
}
