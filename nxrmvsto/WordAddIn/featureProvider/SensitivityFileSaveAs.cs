using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using WinFormControlLibrary;
using WordAddIn.featureProvider.helper;
using WordAddIn.sdk;
using WordAddIn.winApi;
using Office = Microsoft.Office.Core;

namespace WordAddIn.featureProvider
{
    class SensitivityFileSaveAs
    {
        #region Do SaveAs

        public void SaveAsShowDialog(string fileName, string label)
        {
            var saveAsDlg = Globals.ThisAddIn.Application.FileDialog[Office.MsoFileDialogType.msoFileDialogSaveAs];
            saveAsDlg.InitialFileName = fileName;

            if (-1 == saveAsDlg.Show())
            {
                System.Diagnostics.Debug.WriteLine("d.showed" + saveAsDlg.Item);
                string filePath = saveAsDlg.SelectedItems.Item(1);

                Globals.ThisAddIn.SdkHandler.isDeletePlainFile = true;

                // Workaround: fix open nxl file crash in word
                Globals.ThisAddIn.SdkHandler.IsAddSpecificSymbol = false;

                saveAsDlg.Execute();

                ProtectAndOpenFile(filePath, label);
            }
            else
            {
                Globals.ThisAddIn.IsDoingSaveAs = false;
            }
        }

        private void ProtectAndOpenFile(string filePath, string label)
        {
            var sdkH = Globals.ThisAddIn.SdkHandler;
            string nxlFilePath = string.Empty;
            sdk.UserSelectTags tags = new sdk.UserSelectTags();
            tags.AddTag("Sensitivity", new List<string> { label });
            try
            {
                nxlFilePath = sdkH.ProtectFileCentrolPolicy(filePath, tags);
            }
            catch (sdk.SkydrmException skyEx)
            {
                sdkH.NotifyMsg(Path.GetFileName(filePath),
                    skyEx.Message,
                    SdkHandler.EnumMsgNotifyType.PopupBubble, "",
                    SdkHandler.EnumMsgNotifyResult.Failed,
                    SdkHandler.EnumMsgNotifyIcon.Unknown);
            }
            catch (Exception ex)
            {
                if (!ex.Message.Equals("Cancel"))
                {
                    sdkH.NotifyMsg(Path.GetFileName(filePath),
                    DataConvert.Protect_Failed,
                    SdkHandler.EnumMsgNotifyType.PopupBubble, "",
                    SdkHandler.EnumMsgNotifyResult.Failed,
                    SdkHandler.EnumMsgNotifyIcon.Unknown);
                }

                Debug.WriteLine(ex);
            }

            // notify success msg
            if (File.Exists(nxlFilePath))
            {
                sdkH.NotifyMsg(Path.GetFileName(nxlFilePath),
                        DataConvert.ProtectSucString,
                        SdkHandler.EnumMsgNotifyType.LogMsg, "",
                        SdkHandler.EnumMsgNotifyResult.Succeed,
                        SdkHandler.EnumMsgNotifyIcon.Offline);
            }
            else
            {
                return;
            }

            Task.Factory.StartNew(() => {

                Thread.Sleep(1000);
                //var currentWindow = Globals.ThisAddIn.Application.ActiveWindow;
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
                    Globals.ThisAddIn.IsDoingSaveAs = false;
                }

                if (Globals.ThisAddIn.SdkHandler.isDeletePlainFile)
                {
                    sdkH.Rmsdk.RPM_DeleteFile(filePath);
                }
            });
        }
        #endregion
    }
}
