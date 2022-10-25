using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Diagnostics;
using PowerPointAddIn.featureProvider;
using PowerPoint = Microsoft.Office.Interop.PowerPoint;
using Office = Microsoft.Office.Core;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using Microsoft.Win32;

namespace PowerPointAddIn
{
    public partial class ThisAddIn
    {
        /// <summary>
        /// Used to determine from the AddIn Protect button
        /// </summary>
        public bool IsFromProtectAddin { get; set; }

        /// <summary>
        /// Because Office.SaveAsDlg.Execute() must be called in another thread for it to work in PowerPoint,
        /// Set this flag to avoid getting stuck in an endless loop
        /// </summary>
        private SaveAsFlag IsFromSaveAsAddin = new SaveAsFlag() { FullName = "", Flag = false };
        private struct SaveAsFlag
        {
            public string FullName { get; set; }
            public bool Flag { get; set; }
        }

        /// <summary>
        /// If user select .ppam or ppa type,use office built-in saveAs dialog to do save.
        /// </summary>
        public bool IsNotSupportSaveAsType { get; set; }

        private bool saveAs = false;
        /// <summary>
        /// Use to distinguish SaveAs, Save
        /// </summary>
        public bool SaveAsUI { get => saveAs; set => saveAs = value; }

        public WordAddIn.SdkHandler SdkHandler { get; private set; }

        public bool IsDoingSaveAs { get; set; } = false;

        public string GetActiveDocumentSensitivityLabel()
        {
            string label = string.Empty;
            try
            {
                List<string> values = new List<string>();
                Office.DocumentProperties properties2 = (Office.DocumentProperties)Application.ActivePresentation.CustomDocumentProperties;
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

        private void ThisAddIn_Startup(object sender, System.EventArgs e)
        {
            Application.WindowActivate += Application_WindowActivate;

            Application.PresentationBeforeClose += Application_PresentationBeforeClose;
            Application.PresentationCloseFinal += Application_PresentationCloseFinal;
            Application.PresentationBeforeSave += Application_PresentationBeforeSave;

            SdkHandler = new WordAddIn.SdkHandler("POWERPNT.EXE");
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

        private void ThisAddIn_Shutdown(object sender, System.EventArgs e)
        {
            //SdkHandler.SdkLibCleanup();

            if (!SdkHandler.bRecoverSucceed)
            {
                return;
            }
        }

        private void Application_WindowActivate(PowerPoint.Presentation Pres, PowerPoint.DocumentWindow Wn)
        {
            Debug.WriteLine("******Application_WindowActivate Pres:******" + Pres.FullName);
            ribbon.InvalidateCustomControl();
        }

        private void Application_PresentationBeforeClose(PowerPoint.Presentation Pres, ref bool Cancel)
        {
            Debug.WriteLine("******Application_PresentationBeforeClose Pres:******" + Pres.FullName);

            if (!IsSensitiveLabelEnable() || !SdkHandler.bRecoverSucceed)
            {
                return;
            }

            // fix Bug 59292 - [office RMX]no any response when click "X" button then click "Save" after edit
            // fix Bug 59294 - pop up save as file dialog after click save option
            string fullName = Pres.FullName;
            string directory = Path.GetDirectoryName(fullName);

            if (!string.IsNullOrEmpty(directory))// in drive file
            {
                SaveAsUI = false;
            }
            return;
        }

        private void Application_PresentationCloseFinal(PowerPoint.Presentation Pres)
        {
            Debug.WriteLine("******Application_PresentationCloseFinal Pres:******" + Pres.FullName);
            if (!IsSensitiveLabelEnable() || !SdkHandler.bRecoverSucceed || IsDoingSaveAs)
            {
                return;
            }

            // fix [Bug 59306] New: [office RMX]pop "Save As" window after click "Save As" button on quickly access bar
            string fullName = Pres.FullName;
            string directory = Path.GetDirectoryName(fullName);

            if (!string.IsNullOrEmpty(directory))// in drive file
            {
                SaveAsUI = true;
            }

            bool nxl = SdkHandler.IsNxlFile(Pres.FullName);
            bool isaved = Pres.Saved == Microsoft.Office.Core.MsoTriState.msoTrue;
            if (isaved && !nxl)
            {
                GeRegistryConfigLabel(out string encrypt, out string decrypt);
                string label = GetActiveDocumentSensitivityLabel();
                if (label.Equals(encrypt, StringComparison.OrdinalIgnoreCase))
                {
                    SensitivityFileProtect sensitivity = new SensitivityFileProtect();
                    sensitivity.ProtectFile(Pres.FullName, label);
                }
            }

            return;
        }

        private void Application_PresentationBeforeSave(PowerPoint.Presentation Pres, ref bool Cancel)
        {
            if (!IsSensitiveLabelEnable() || !SdkHandler.bRecoverSucceed || IsDoingSaveAs)
            {
                return;
            }
           
            if (IsNotSupportSaveAsType)
            {
                IsNotSupportSaveAsType = false;
                return;
            }

            Debug.WriteLine("*******Application_PresentationBeforeSave******");
            Debug.WriteLine("ppt:" + Pres.Name);

            if (SaveAsUI)
            {
                if (IsFromProtectAddin)
                {
                    return;
                }
                else if (IsFromSaveAsAddin.Flag)
                {
                    if (IsFromSaveAsAddin.FullName.Equals(Pres.FullName, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                    else
                    {
                        // fix Bug 59312 - should pop up VSTO save as dialog for the second PPT file
                        Cancel = true;
                        return;
                    }
                }
                else
                {
                    Debug.WriteLine("this is for save as");

                    string fileName = Pres.FullName;
                    bool nxl = SdkHandler.IsNxlFile(fileName);

                    string label = "";
                    bool doEncrypt = false;
                    if (nxl)
                    {
                        //reset
                        SaveAsUI = true;
                        return;
                    }
                    else
                    {
                        label = GetActiveDocumentSensitivityLabel();
                        GeRegistryConfigLabel(out string encrypt, out string decrypt);
                        doEncrypt = label.Equals(encrypt, StringComparison.OrdinalIgnoreCase);

                        Cancel = doEncrypt;
                        if (!Cancel)
                        {
                            //reset
                            SaveAsUI = true;
                            return;
                        }
                    }


                    Thread thread = new Thread(new ThreadStart(() =>
                    {
                        try
                        {
                            IsFromSaveAsAddin.FullName = fileName;
                            IsFromSaveAsAddin.Flag = true;
                            if (nxl)
                            {
                                //NxlFileSaveAs nxlFileSaveAs = new NxlFileSaveAs();
                                //nxlFileSaveAs.SaveAsShowDialog(Pres.FullName);
                            }
                            else
                            {
                                if (doEncrypt)
                                {
                                    IsDoingSaveAs = true;
                                    SensitivityFileSaveAs sensitivityFileSaveAs = new SensitivityFileSaveAs();
                                    sensitivityFileSaveAs.SaveAsShowDialog(fileName, label);
                                }
                                else
                                {
                                    //FileSaveAs fileSaveAs = new FileSaveAs();
                                    //fileSaveAs.SaveAsShowDialog(Wb.Name);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e);
                        }
                        finally
                        {
                            IsFromSaveAsAddin.FullName = "";
                            IsFromSaveAsAddin.Flag = false;
                        }
                    }));
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();
                }
            }
            //reset
            SaveAsUI = true;
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
