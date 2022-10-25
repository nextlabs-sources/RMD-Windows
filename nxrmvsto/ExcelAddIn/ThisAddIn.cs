using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Excel = Microsoft.Office.Interop.Excel;
using Office = Microsoft.Office.Core;
using Microsoft.Office.Tools.Excel;
using System.Diagnostics;
using ExcelAddIn.featureProvider;
using Microsoft.Win32;

namespace ExcelAddIn
{
    public partial class ThisAddIn
    {
        /// <summary>
        /// Used to determine from the AddIn Protect button
        /// </summary>
        public bool IsFromProtectAddin { get; set; }

        /// <summary>
        /// Flag that the raw built-in save as dialog if is shown.
        /// </summary>
        public bool IsHasShownDlg { get; set; }

        public WordAddIn.SdkHandler SdkHandler { get; private set; }

        public bool IsDoingSaveAs { get; set; } = false;

        public string GetActiveDocumentSensitivityLabel()
        {
            string label = string.Empty;
            try
            {
                List<string> values = new List<string>();
                Office.DocumentProperties properties2 = (Office.DocumentProperties)Application.ActiveWorkbook.CustomDocumentProperties;
                foreach (Office.DocumentProperty dp2 in properties2)
                {
                    if (dp2.Name != null && dp2.Name.EndsWith("_Name"))
                    {
                        label = dp2.Value;
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            return label;
        }

        public void GeRegistryConfigLabel(out string encrypt, out string decrypt)
        {
            encrypt = string.Empty;
            decrypt = string.Empty;
            RegistryKey skydrm = Registry.CurrentUser.CreateSubKey(@"Software\Nextlabs\SkyDRM");
            try
            {
                RegistryKey mip = skydrm.OpenSubKey(@"MIP");
                decrypt = (string)mip.GetValue("Decrypt_Label", "");
                encrypt = (string)mip.GetValue("Encrypt_Label", "");
            }
            finally
            {
                skydrm?.Close();
            }
        }

        public bool IsSensitiveLabelEnable()
        {
            int status = 0;
            RegistryKey skydrm = Registry.CurrentUser.CreateSubKey(@"Software\Nextlabs\SkyDRM");
            try
            {
                RegistryKey mip = skydrm.OpenSubKey(@"MIP");
                status = (int)mip.GetValue("Enable_Label", "");
                if (1 == status)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch(Exception)
            {
                return false;
            }
            finally
            {
                skydrm?.Close();
            }
        }

        private void ThisAddIn_Startup(object sender, System.EventArgs e)
        {
            Application.WindowActivate += Application_WindowActivate;

            Application.WorkbookBeforeSave += Application_WorkbookBeforeSave;
            Application.WorkbookBeforeClose += Application_WorkbookBeforeClose;
            Application.WorkbookAfterSave += Application_WorkbookAfterSave;
            SdkHandler = new WordAddIn.SdkHandler("EXCEL.EXE");
        }

        private void ThisAddIn_Shutdown(object sender, System.EventArgs e)
        {
            //SdkHandler.SdkLibCleanup();

            if (!SdkHandler.bRecoverSucceed)
            {
                return;
            }
        }

        private void Application_WindowActivate(Excel.Workbook Wb, Excel.Window Wn)
        {
            Debug.WriteLine("******Application_WindowActivate Workbook:******" + Wb.FullName);
            ribbon.InvalidateCustomControl();
        }

        private void Application_WorkbookBeforeSave(Excel.Workbook Wb, bool SaveAsUI, ref bool Cancel)
        {
            if (!IsSensitiveLabelEnable() || !SdkHandler.bRecoverSucceed)
            {
                return;
            }

            Debug.WriteLine("*******Application_WorkbookBeforeSave*******");
            Debug.WriteLine("Excel:" + Wb.Name);

            if (IsHasShownDlg)
            {
                IsHasShownDlg = false;
                return;
            }

            if (SaveAsUI)
            {
                if (IsFromProtectAddin)
                {
                    return;
                }
                else
                {
                    Debug.WriteLine("this is for save as");

                    bool isNxl = SdkHandler.IsNxlFile(Wb.FullName);

                    if (isNxl)
                    {
                        //NxlFileSaveAs nxlFileSaveAs = new NxlFileSaveAs();
                        //nxlFileSaveAs.SaveAsShowDialog(Wb.FullName);
                    }
                    else
                    {
                        string label = GetActiveDocumentSensitivityLabel();
                        GeRegistryConfigLabel(out string encrypt, out string decrypt);
                        if (label.Equals(encrypt, StringComparison.OrdinalIgnoreCase))
                        {
                            Cancel = true;
                            IsDoingSaveAs = true;
                            SensitivityFileSaveAs sensitivityFileSaveAs = new SensitivityFileSaveAs();
                            sensitivityFileSaveAs.SaveAsShowDialog(Wb.Name, label);
                        }
                        else
                        {
                            //FileSaveAs fileSaveAs = new FileSaveAs();
                            //fileSaveAs.SaveAsShowDialog(Wb.Name);
                        }
                    }

                }
            }
        }

        private bool IsDoClose = false;
        private void Application_WorkbookBeforeClose(Excel.Workbook Wb, ref bool Cancel)
        {
            if (!IsSensitiveLabelEnable() || !SdkHandler.bRecoverSucceed || IsDoingSaveAs)
            {
                return;
            }

            bool nxl = SdkHandler.IsNxlFile(Wb.FullName);
            if (!nxl && Wb.Saved)
            {
                GeRegistryConfigLabel(out string encrypt, out string decrypt);
                string label = GetActiveDocumentSensitivityLabel();
                if (label.Equals(encrypt, StringComparison.OrdinalIgnoreCase))
                {
                    SensitivityFileProtect sensitivity = new SensitivityFileProtect();
                    sensitivity.ProtectFile(Wb.FullName, label);
                }
            }
            else
            {
                IsDoClose = true;
            }
        }
        private void Application_WorkbookAfterSave(Excel.Workbook Wb, bool Success)
        {
            if (!IsSensitiveLabelEnable() || !SdkHandler.bRecoverSucceed || IsDoingSaveAs)
            {
                return;
            }

            bool nxl = SdkHandler.IsNxlFile(Wb.FullName);

            if (Wb.Saved && IsDoClose && !nxl)
            {
                GeRegistryConfigLabel(out string encrypt, out string decrypt);
                string label = GetActiveDocumentSensitivityLabel();
                if (label.Equals(encrypt, StringComparison.OrdinalIgnoreCase))
                {
                    SensitivityFileProtect sensitivity = new SensitivityFileProtect();
                    sensitivity.ProtectFile(Wb.FullName, label);
                }
            }

            // reset flag
            IsDoClose = false;
        }

        #region VSTO generated code

        private Ribbon ribbon;

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InternalStartup()
        {
            this.Startup += new System.EventHandler(ThisAddIn_Startup);
            this.Shutdown += new System.EventHandler(ThisAddIn_Shutdown);
        }

        protected override Microsoft.Office.Core.IRibbonExtensibility CreateRibbonExtensibilityObject()
        {
            return ribbon = new Ribbon();
        }
        #endregion
    }
}
