using Microsoft.Office.Core;
using Microsoft.Office.Interop.PowerPoint;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.process
{
    public class PowerPntProcess : OfficeProcess
    {
        private log4net.ILog Log = SkydrmLocalApp.Singleton.Log;

        private Microsoft.Office.Interop.PowerPoint.Application Application { get; set; }

        private string FileDiskPath = string.Empty;

        

        [DllImport("user32.dll")]
        public static extern int GetWindowThreadProcessId(int hWnd, out int lpdwProcessId);

        public override void Launch()
        {
            Log.Info("\t\t\t\t PowerPntProcess Launch \r\n");
            try
            {                
                Application = new Microsoft.Office.Interop.PowerPoint.Application();                
            }
            catch (COMException ex)
            {
                throw ex;
            }
            DisableAutoRecoveryFeature(Application.Version);

        }

        public override void OpenFile(string filePath)
        {
            Log.Info("\t\t PowerPnt OpenFile \r\n" +
                    "\t\t\t\t filePath :" + filePath + "\r\n");
            this.FileDiskPath = filePath;
            Application.Visible = Microsoft.Office.Core.MsoTriState.msoTrue;
            try
            {
                Presentation presentation = Application.Presentations.Open(filePath);
                Application.PresentationCloseFinal += new EApplication_PresentationCloseFinalEventHandler(EApplication_PresentationCloseFinalEvent);
            }
            catch (Exception ex)
            {
                Log.Info("\t\t PowerPnt OpenFile Error\r\n" +
                 "\t\t\t\t Message :" + ex.Message + "\r\n"+
                 "\t\t\t\t HResult :" + ex.HResult + "\r\n" +
                 "\t\t\t\t ERROR_CODE_REJECTED_BY_CALLEE :" + ERROR_CODE_REJECTED_BY_CALLEE + "\r\n");

                if (ex.HResult != ERROR_CODE_REJECTED_BY_CALLEE)
                {
                    throw;
                }
            }
        }

        public void EApplication_PresentationCloseFinalEvent(Presentation Pres)
        {
            Log.Info("\t\t PowerPnt EApplication_PresentationCloseFinalEvent \r\n");
            if (string.Equals(FileDiskPath, Pres.FullName, StringComparison.CurrentCultureIgnoreCase))
            {
                FinalReleaseComObject();
            }
        }

        public override void FinalReleaseComObject()
        {
            Log.Info("\t\t PowerPnt FinalReleaseComObject \r\n");
            try
            {
                 Marshal.FinalReleaseComObject(Application);
                 Log.Info("\t\t PowerPnt end: FinalReleaseComObject \r\n");
            }
            catch (Exception ex)
            {
                Log.Error("\t\t PowerPnt FinalReleaseComObject failed.\r\n" + ex.ToString());
            }
        }

        public override int GetPid()           
        {
            Log.Info("\t\t\t\t PowerPntProcess GetPid \r\n");
            int processId = Int32.MaxValue;
            try
            {
                GetWindowThreadProcessId(Application.HWND, out processId);
            }
            catch (Exception ex)
            {
                Log.Error("\t\t\t\t PowerPntProcess GetPid Error \r\n");
                Log.Error(ex.Message.ToString(), ex);
            }
            Log.Info("\t\t\t\t PowerPntProcess Pid :" + processId+"\r\n");
            return processId;
        }
        public override void Close()
        {
            base.Close();

            try
            {
                // Quit Word and release the ApplicationClass object.  
                if (Application != null)
                {                   
                    Application.Quit();
                    Application = null;
                }

                GC.Collect();                      
            }
            finally
            {

            }
        }

        public override IntPtr MainWindowHandle()
        {
            return Process.GetProcessById(GetPid()).MainWindowHandle;
        }

        public override Process GetProcess()
        {
            Log.Info("\t\t\t\t PowerPnt GetProcess Obj \r\n");
            return Process.GetProcessById(GetPid());
        }


        private void DisableAutoRecoveryFeature(string ver /*like 15.0, 16.0*/)
        {
            /*
             * HKCU\Software\Microsoft\Office\{Version}\PowerPoint\Options
             * set:
             *      KeepUnsavedChanges 0
             *      SaveAutoRecoveryInfo 0
             */
            string path = String.Format(@"Software\Microsoft\Office\{0}\PowerPoint\Options", ver);
            try
            {
                RegistryKey key32 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
                RegistryKey keyPPt = key32.OpenSubKey(path, true);
                keyPPt.SetValue("KeepUnsavedChanges", 0, RegistryValueKind.DWord);
                keyPPt.SetValue("SaveAutoRecoveryInfo", 0, RegistryValueKind.DWord);
                keyPPt.Close();
                key32.Close();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);               
            }

            try
            {
                RegistryKey key64 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
                RegistryKey keyPPt = key64.OpenSubKey(path, true);
                keyPPt.SetValue("KeepUnsavedChanges", 0, RegistryValueKind.DWord);
                keyPPt.SetValue("SaveAutoRecoveryInfo", 0, RegistryValueKind.DWord);
                keyPPt.Close();
                key64.Close();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }

        }

    }
}
