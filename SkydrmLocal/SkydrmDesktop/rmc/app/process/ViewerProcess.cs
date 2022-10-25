using SkydrmLocal.rmc.common.helper;
using SkydrmLocal.rmc.drive;
using SkydrmLocal.rmc.Edit;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.ui.windows.mainWindow.helper;
using SkydrmLocal.rmc.ui.windows.mainWindow.model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Alphaleonis.Win32.Filesystem;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SkydrmDesktop;

namespace SkydrmLocal.rmc.process
{
    public class ViewerProcess
    {      
        /// <summary>
        /// key FileName
        /// value 
        /// </summary>
        public static readonly ConcurrentDictionary<string, ViewerProcess> KeyValuePairs = new ConcurrentDictionary<string, ViewerProcess>();

        private SkydrmApp SkydrmLocalApp = SkydrmApp.Singleton;
        private log4net.ILog Log = SkydrmApp.Singleton.Log;

        private string Key = string.Empty;

        private System.Diagnostics.Process p = null;
   
        public INxlFile CurrentSelectedFile { get; set; }

        private NxlConverterResult converterResult;

        public NxlConverterResult ConverterResult
        {
            get
            {
                return converterResult;
            }

            set
            {
                converterResult = value;
                if (null == p || p.HasExited)
                {
                    DeleteFileInRPM(ConverterResult);
                }
            }
        }

        /// <summary>
        /// Init Viewer Process
        /// </summary>
        /// <param name="intention">Intention</param>
        /// <param name="json">Json</param>
        public ViewerProcess(string intention, string json)
        {
            p = new System.Diagnostics.Process();
            p.StartInfo.FileName = "Viewer.exe";
            p.StartInfo.WorkingDirectory = System.Windows.Forms.Application.StartupPath;
            p.StartInfo.Arguments += "Viewer.exe";
            p.StartInfo.Arguments += " ";
            p.StartInfo.Arguments += intention;
            p.StartInfo.Arguments += " ";
            p.StartInfo.Arguments += json;
        }

        public IntPtr GetMainWindowHandle()
        {
            Log.Info("ViewerProcess -> GetMainWindowHandle()");
            return p.MainWindowHandle;
        }

        public static bool GetValueByKey(string fileLocalPath,out ViewerProcess viewerProcess)
        {
            return KeyValuePairs.TryGetValue(fileLocalPath.ToLower(), out viewerProcess);
        }

        public void Start(string key)
        {
            Log.Info("ViewerProcess -> Start(string key); string key =" + Key);
     
            this.Key = key.ToLower();

            KeyValuePairs.AddOrUpdate(this.Key,
                new Func<string, ViewerProcess>(AddValue),
                new Func<string, ViewerProcess, ViewerProcess>(UpdateValue));
        }

        private ViewerProcess AddValue(string arg)
        {
            Log.Info("ViewerProcess -> AddValue(string arg); string arg =" + arg);
            try
            {
                this.p.EnableRaisingEvents = true;
                this.p.Exited += new EventHandler(Exited);
                this.p.Start();           
            }
            catch (System.ObjectDisposedException d)
            {
                Log.Error("ViewerProcess -> AddValue(string arg); System.ObjectDisposedException ; Message ="+d.Message.ToString());
            }
            catch (InvalidOperationException i)
            {
                Log.Error("ViewerProcess -> AddValue(string arg); InvalidOperationException ; Message =" + i.Message.ToString());
            }    
            catch (System.ComponentModel.Win32Exception w)
            {
                Log.Error("ViewerProcess -> AddValue(string arg); System.ComponentModel.Win32Exception ; Message =" + w.Message.ToString());
            }
            catch (Exception e)
            {
                Log.Error("ViewerProcess -> AddValue(string arg); Exception ; Message =" + e.Message.ToString());
            }
            return this;
        }

        private ViewerProcess UpdateValue(string arg, ViewerProcess viewerProcess)
        {
            Log.Info("ViewerProcess -> UpdateValue(string arg, Process p); string arg =" + arg+"; "+ "Process p =" + p.ToString());
            try
            {
                ICollection<string> keys = FileEditorHelper.EditingFileMap.Keys;

                bool isFound = false;
                string result = string.Empty;
                foreach (string key in keys)
                {
                    if (string.Equals(key, arg, StringComparison.CurrentCultureIgnoreCase))
                    {
                        result = key;
                        isFound = true;
                        break;
                    }
                }

                if (isFound)
                {
                    FileEditor fileEditor = null;
                    FileEditorHelper.EditingFileMap.TryGetValue(result, out fileEditor);
                    if (null != fileEditor)
                    {
                        Process process = fileEditor.GetProcess();
                        if (null != process)
                        {
                            IntPtr ptr = process.MainWindowHandle;
                            Win32Common.BringWindowToTop(ptr, process);
                        }
                    }
                }
                else
                {
                    Process p = viewerProcess.p;
                    IntPtr intPtr = p.MainWindowHandle;
                    Win32Common.BringWindowToTop(intPtr, p);
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
                Log.Error(e.Message.ToString(),e);
            }
            return viewerProcess;
        }

        private static void DeleteFileInRPM(ViewerProcess viewerProcess)
        {

            SkydrmApp.Singleton.Log.Info("\t\t Begin DeleteFileInRPM(ViewerProcess viewerProcess) \r\n");

            if (null == viewerProcess)
            {
                return;
            }
          
            if (null == viewerProcess.ConverterResult)
            {
                return;
            }

            NxlConverterResult nxlConverterResult = viewerProcess.ConverterResult;
            DeleteFileInRPM(nxlConverterResult);
        }

        public static void DeleteFileInRPM(NxlConverterResult nxlConverterResult)
        {
            SkydrmApp SkydrmLocalApp = SkydrmApp.Singleton;

            SkydrmLocalApp.Log.Info("\t\t DeleteFileInRPM(NxlConverterResult nxlConverterResult) \r\n");

            string tempPath = nxlConverterResult.TmpPath;

            if (!string.IsNullOrEmpty(tempPath))
            {
                RightsManagementService.RPMDeleteDirectory(SkydrmLocalApp, Path.GetDirectoryName(tempPath));
            }

            if (nxlConverterResult.IsDisplayPrintButton && !string.IsNullOrEmpty(nxlConverterResult.ForPrintFilePath))
            {
                RightsManagementService.RPMDeleteDirectory(SkydrmLocalApp, Path.GetDirectoryName(nxlConverterResult.ForPrintFilePath));
            }
        }

        public static void KillActiveViewer(string fileDiskPath)
        {
            List<ViewerProcess> viewerInfoList = new List<ViewerProcess>();

            if (ContainsKey(fileDiskPath))
            {
                ViewerProcess viewerProcess = null;

                if (KeyValuePairs.TryRemove(fileDiskPath.ToLower(), out viewerProcess))
                {
                    if (null != viewerProcess)
                    {
                        CloseProcess(viewerProcess.p);
                        viewerInfoList.Add(viewerProcess);
                    }
                }
            }

            if (viewerInfoList.Count() == 0)
            {
                return;
            }

            Thread.Sleep(2000);//wait office process exited

            foreach (ViewerProcess item in viewerInfoList)
            {
                DeleteFileInRPM(item);
            }
        }

        public static void KillAllActiveViewer()
        {     
            ICollection<string> keys = KeyValuePairs.Keys;

            List<ViewerProcess> viewerInfoList = new List<ViewerProcess>();

            foreach (string key in keys)
            {
                ViewerProcess viewerProcess = null;

                if (KeyValuePairs.TryRemove(key, out viewerProcess))
                {
                    CloseProcess(viewerProcess.p);

                    viewerInfoList.Add(viewerProcess);
                }
            }

            if (viewerInfoList.Count() == 0)
            {
                return;
            }

             Thread.Sleep(2000);//wait office process exited

            foreach (ViewerProcess item in viewerInfoList)
            {               
                DeleteFileInRPM(item);

                // Fixed bug 54726, Now don't delete file when viewer closed.
                // Delete cache file for online.
                //DeleteOnlineFileCache(item.CurrentSelectedFile);
            }
        }


        private static void CloseProcess(Process p)
        {
            if (null != p)
            {
                try
                {
                    p.EnableRaisingEvents = false;
                    if (!p.CloseMainWindow())
                    {
                        p.Kill();
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                       p.Kill();
                    }
                    catch (Exception e)
                    {

                    }
                }
                finally
                {
                   
                }
            }
        }


        public static bool BringWindowToTop(string filePath)
        {
            bool result = false;
            log4net.ILog Log = SkydrmApp.Singleton.Log;

            result = ContainsKey(filePath.ToLower());
            if (result)
            {
                ViewerProcess viewerProcess = null;

                if (GetValueByKey(filePath, out viewerProcess))
                {
                    try
                    {
                        Process p = viewerProcess.p;
                        IntPtr intPtr = p.MainWindowHandle;
                        Win32Common.BringWindowToTop(intPtr, p);
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
            }
            return result;
        }

        public static bool ContainsKey(string filePath)
        {
            return KeyValuePairs.ContainsKey(filePath.ToLower());             
        }

        public void Exited(object sender, EventArgs e)
        {
            Log.Info("ViewerProcess -> Exited(object sender, EventArgs e)");
            //Send view process exit event.
            
            ViewerProcess viewerProcess = null;

            if (KeyValuePairs.TryRemove(Key,out viewerProcess))
            {
                if (null!= viewerProcess)
                {
                    DeleteFileInRPM(viewerProcess);

                    // Fixed bug 54726, Now don't delete file when viewer closed.
                    // Delete cache file for online.
                    //DeleteOnlineFileCache(viewerProcess.CurrentSelectedFile);
                }   
            }
        }
     
        public int GetProcessId()
        {
            return p.Id;
        }

        // Delete the cache file for online view after close viewer.
        private static void DeleteOnlineFileCache(INxlFile nxlFile)
        { 
            if (nxlFile != null && nxlFile.Location == EnumFileLocation.Online)
            {
                FileHelper.Delete_NoThrow(nxlFile.LocalPath);
            }
        }

        public static void HideViewer(string filePath)
        {
            SkydrmApp.Singleton.Log.Info("\t\t HideViewer \r\n" +
                                     "\t\t\t\t filePath : " + filePath + "\r\n");
            if (ViewerProcess.ContainsKey(filePath))
            {
                ViewerProcess viewerProcess;
                if (ViewerProcess.GetValueByKey(filePath, out viewerProcess))
                {
                    if (null != viewerProcess)
                    {
                        IntPtr hwnd = viewerProcess.GetMainWindowHandle();
                        IPCManager.SendData(hwnd, IPCManager.WM_HIDE_VIEWER, filePath);
                    }
                }
            }
        }
    }
}
