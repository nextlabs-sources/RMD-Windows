using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Word = Microsoft.Office.Interop.Word;
using Office = Microsoft.Office.Core;
using Microsoft.Office.Tools.Word;
using System.Diagnostics;
using WordAddIn.featureProvider;
using Microsoft.Win32;

namespace WordAddIn
{
    public partial class ThisAddIn
    {
        /// <summary>
        /// Used to determine from the AddIn Protect button
        /// </summary>
        public bool IsFromProtectAddin { get; set; } = true;

        public bool IsDoingSaveAs { get; set; } = false;

        public SdkHandler SdkHandler { get; private set; }

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

        public string GetActiveDocumentSensitivityLabel()
        {
            string label = string.Empty;
            try
            {
                List<string> values = new List<string>();
                Office.DocumentProperties properties2 = (Office.DocumentProperties)Application.ActiveDocument.CustomDocumentProperties;
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

        #region  AddIn Event
        private void ThisAddIn_Startup(object sender, System.EventArgs e)
        {
            Application.WindowActivate += Application_WindowActivate;

            Application.DocumentBeforeSave += Application_DocumentBeforeSave;
            /*
             * After the user edits the file, clicking close button will trigger 'DocumentBeforeClose' event, and then the office will be prompted whether to save dialog,
             * we should process our logic after the user chooses to save, so use the 'DocumentChange' event to handle.
             */
            //Application.DocumentBeforeClose += Application_DocumentBeforeClose;
            Application.DocumentChange += Application_DocumentChange;

            SdkHandler = new SdkHandler("WINWORD.EXE");
        }

        private void ThisAddIn_Shutdown(object sender, System.EventArgs e)
        {
            Debug.WriteLine("*******ThisAddIn_Shutdown********");
            //SdkHandler.SdkLibCleanup();

            if (!SdkHandler.bRecoverSucceed)
            {
                return;
            }
        }

        private void Application_WindowActivate(Word.Document Doc, Word.Window Wn)
        {
            Debug.WriteLine("******Application_WindowActivate Doc:******" + Doc.FullName);
            ribbon.InvalidateCustomControl();
        }


        // for sensitivity label function, save label, path of the file
        struct CloseRequestInfo
        {
            public string DocPath { get; set; }
            public string SensitivityLabel { get; set; }
            public bool Saved { get; set; }
        }
        private CloseRequestInfo closeRequestInfo;
        private void Application_DocumentBeforeSave(Word.Document Doc, ref bool SaveAsUI, ref bool Cancel)
        {
            Debug.WriteLine("this is wordaddin2 test before save event");
            if (!IsSensitiveLabelEnable() || !SdkHandler.bRecoverSucceed)
            {
                return;
            }

            Debug.WriteLine("Application_DocumentBeforeSave");
            Debug.WriteLine("doc:" + Doc.Name);

            if (SaveAsUI)
            {
                if (IsFromProtectAddin)
                {
                    return;
                }
                else
                {
                    Debug.WriteLine("this is for save as");

                    bool isNxl = SdkHandler.IsNxlFile(Doc.FullName);

                    if (isNxl)
                    {
                        //NxlFileSaveAs nxlFileSaveAs = new NxlFileSaveAs();
                        //nxlFileSaveAs.SaveAsShowDialog(Doc.FullName);
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
                            sensitivityFileSaveAs.SaveAsShowDialog(Doc.Name, label);
                        }
                        else
                        {
                            //FileSaveAs fileSaveAs = new FileSaveAs();
                            //fileSaveAs.SaveAsShowDialog(Doc.Name);
                        }
                    }

                }
            }
            else
            {
                // user click save
                string label = GetActiveDocumentSensitivityLabel();
                closeRequestInfo = new CloseRequestInfo { DocPath = Doc.FullName, SensitivityLabel = label, Saved = true  };
            }
        }


        private void Application_DocumentBeforeClose(Word.Document Doc, ref bool Cancel)
        {
            if (!SdkHandler.bRecoverSucceed || IsDoingSaveAs)
            {
                return;
            }

            Debug.WriteLine("Application_DocumentBeforeClose");
            Debug.WriteLine("doc:" + Doc.Name);

            if (!System.IO.File.Exists(Doc.FullName))
            {
                return;
            }

            string label = GetActiveDocumentSensitivityLabel();
            closeRequestInfo = new CloseRequestInfo { DocPath = Doc.FullName, SensitivityLabel = label };
        }
        private void Application_DocumentChange()
        {
            if (!IsSensitiveLabelEnable() || !SdkHandler.bRecoverSucceed || IsDoingSaveAs)
            {
                return;
            }

            GeRegistryConfigLabel(out string encrypt, out string decrypt);

            string docPath = closeRequestInfo.DocPath;
            string label = closeRequestInfo.SensitivityLabel;

            if (!System.IO.File.Exists(docPath) || !closeRequestInfo.Saved)
            {
                return;
            }

            bool isNxl = SdkHandler.IsNxlFile(docPath);
            if (isNxl)
            {
                // should handle in nxrmaddin.dll
                // if label is decrypt, should copy RPM decrypt file to original file path, and delete original nxl file. 
            }
            else
            {
                if (label.Equals(encrypt, StringComparison.OrdinalIgnoreCase))
                {
                    SensitivityFileProtect sensitivity = new SensitivityFileProtect();
                    sensitivity.ProtectFile(docPath, label);
                }
            }
        }
        #endregion


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
