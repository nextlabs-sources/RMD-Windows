using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using WinFormControlLibrary;

using WordAddIn.featureProvider.helper;
using WordAddIn.sdk;
using WordAddIn.winApi;

using Word = Microsoft.Office.Interop.Word;

namespace WordAddIn.featureProvider
{
    class FileProtect
    {
        #region Do Protect

        private bool isDocumentSaved { get; set; }

        public void ProtectShowDialog()
        {
            Globals.ThisAddIn.SdkHandler.isDeletePlainFile = false;


            string name = Globals.ThisAddIn.Application.ActiveDocument.FullName;
            string fullName = Globals.ThisAddIn.SdkHandler.LocalPath(name);

            string directory = String.Empty;
            try
            {
                directory = Path.GetDirectoryName(fullName);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "SkyDRM", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Debug.WriteLine(e.Message);
                return;
            }


            if (!string.IsNullOrEmpty(directory))
            {
                isDocumentSaved = Globals.ThisAddIn.Application.ActiveDocument.Saved;
                if (isDocumentSaved)
                {
                    ShowProtectFrmRights(fullName);
                }
                else
                {
                    Globals.ThisAddIn.IsFromProtectAddin = true;
                    Globals.ThisAddIn.Application.ActiveDocument.Save();
                    Globals.ThisAddIn.IsFromProtectAddin = false;
                    ShowProtectFrmRights(fullName);
                }
                return;
            }

            // show  winForm saveAs dialog
            SaveFileDialog dialog = new SaveFileDialog();

            dialog.FileName = Globals.ThisAddIn.Application.ActiveDocument.Name;
            dialog.DefaultExt = "docx";

            dialog.Filter = "NextLabs Protected Document(*.docx)|*.docx";

            dialog.FilterIndex = 1;
            dialog.RestoreDirectory = true;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = dialog.FileName.ToString();

                if (!isDocumentSaved)
                {
                    Word.WdSaveFormat saveFormat = Word.WdSaveFormat.wdFormatDocumentDefault;

                    Globals.ThisAddIn.IsFromProtectAddin = true;
                    Globals.ThisAddIn.Application.ActiveDocument.SaveAs2(filePath, saveFormat);
                    Globals.ThisAddIn.IsFromProtectAddin = false;
                }
                ShowProtectFrmRights(filePath);
            }
        }

        private void ShowProtectFrmRights(string filePath)
        {
            var sdkH = Globals.ThisAddIn.SdkHandler;
            ProjectClassification[] classifications;

            string label = Globals.ThisAddIn.GetActiveDocumentSensitivityLabel();
            bool isEnable = Globals.ThisAddIn.IsSensitiveLabelEnable();
            if (isEnable && !string.IsNullOrEmpty(label))
            {
                classifications = new ProjectClassification[1];
                classifications[0].isMandatory = true;
                Dictionary<string, bool> valuePairs = new Dictionary<string, bool>();
                valuePairs.Add(label, true);
                classifications[0].labels = valuePairs;
                classifications[0].name = "Sensitivity";
            }
            else
            {
                classifications = sdkH.SysBucketClassifications;
            }

            BuildFrmRightsData build = new BuildFrmRightsData(Properties.Resources.word, filePath,
                sdkH.SysBucketIsEnableAdhoc, sdkH.RmsWaterMarkInfo.text,
                DataConvert.SdkExpt2FrmExpt(sdkH.RmsExpiration), DataConvert.SdkTag2FrmTag(classifications));

            FrmRightsHandler frmRights = new FrmRightsHandler(filePath, build.DataModel, sdkH);
            string nxlFilePath = "";
            bool isProtectSuccess = frmRights.ShowDialog(out nxlFilePath);
            if (isProtectSuccess)
            {
                if (!File.Exists(nxlFilePath))
                {
                    return;
                }

                // Don't open nxl file, because in close event will handle senesitivity lable again.
               // return;
                int ret = (int)Win32.ShellExecuteW(IntPtr.Zero, "open", nxlFilePath, null, null, Win32.ShowWindowCommands.SW_NORMAL);
                if (ret < 32)
                {
                    Globals.ThisAddIn.SdkHandler?.NotifyMsg(Path.GetFileName(nxlFilePath),
                        DataConvert.OpenNxlString + ret,
                        SdkHandler.EnumMsgNotifyType.PopupBubble, "",
                        SdkHandler.EnumMsgNotifyResult.Failed,
                        SdkHandler.EnumMsgNotifyIcon.Unknown);
                }
                else
                {
                    Globals.ThisAddIn.SdkHandler?.NotifyMsg(Path.GetFileName(nxlFilePath),
                        DataConvert.ProtectChanged,
                        SdkHandler.EnumMsgNotifyType.PopupBubble, "",
                        SdkHandler.EnumMsgNotifyResult.Succeed,
                        SdkHandler.EnumMsgNotifyIcon.Offline);

                    Globals.ThisAddIn.Application.ActiveWindow.Close();
                }
            }
        }
        #endregion
    }
}
