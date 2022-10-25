using PowerPointAddIn.featureProvider.helper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WordAddIn;
using WordAddIn.featureProvider;
using WordAddIn.featureProvider.helper;
using WordAddIn.sdk;
using WordAddIn.winApi;
using Office = Microsoft.Office.Core;

namespace PowerPointAddIn.featureProvider
{
    class SensitivityFileSaveAs
    {
        #region Do SaveAs

        public void SaveAsShowDialog(string fileName, string label)
        {
            try
            {
                var saveAsDlg = Globals.ThisAddIn.Application.FileDialog[Office.MsoFileDialogType.msoFileDialogSaveAs];
                saveAsDlg.InitialFileName = fileName;

                if (-1 == saveAsDlg.Show())
                {
                    System.Diagnostics.Debug.WriteLine("d.showed" + saveAsDlg.Item);
                    string filePath = saveAsDlg.SelectedItems.Item(1);

                    // Check not support format.
                    if (CommonUtils.IsNotSupportSaveAs(filePath))
                    {
                        Globals.ThisAddIn.SdkHandler.NotifyMsg(Path.GetFileName(filePath),
                          DataConvert.UingRawBuiltinSaveAs,
                          SdkHandler.EnumMsgNotifyType.PopupBubble, "",
                          SdkHandler.EnumMsgNotifyResult.Failed,
                          SdkHandler.EnumMsgNotifyIcon.Unknown);

                        Globals.ThisAddIn.IsNotSupportSaveAsType = true;
                        Globals.ThisAddIn.Application.CommandBars.ExecuteMso("FileSaveAs");// will trigger BeforeSave event
                        return;
                    }

                    Globals.ThisAddIn.SdkHandler.isDeletePlainFile = true;

                    saveAsDlg.Execute();

                    if (CommonUtils.IsDenyProtectWhenSaveAsFormat(filePath))
                    {
                        Globals.ThisAddIn.SdkHandler.NotifyMsg(Path.GetFileName(filePath),
                          DataConvert.DenyProtectString,
                          SdkHandler.EnumMsgNotifyType.PopupBubble, "",
                          SdkHandler.EnumMsgNotifyResult.Failed,
                          SdkHandler.EnumMsgNotifyIcon.Unknown);
                    }
                    else
                    {
                        ProtectAndOpenFile(filePath, label);
                    }
                }
                else
                {
                    Globals.ThisAddIn.IsDoingSaveAs = false;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

        }

        private void ProtectAndOpenFile(string filePath, string label)
        {
            var sdkH = Globals.ThisAddIn.SdkHandler;
            string nxlFilePath = string.Empty;
            UserSelectTags tags = new UserSelectTags();
            tags.AddTag("Sensitivity", new List<string> { label });
            try
            {
                nxlFilePath = sdkH.ProtectFileCentrolPolicy(filePath, tags);
            }
            catch (SkydrmException skyEx)
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

            // use for close original file window.
            Microsoft.Office.Interop.PowerPoint.DocumentWindow documentWindow = Globals.ThisAddIn.Application.ActiveWindow;

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
                // use for open nxl file quickly
                Thread.Sleep(3000);

                Globals.ThisAddIn.SdkHandler?.NotifyMsg(Path.GetFileName(nxlFilePath),
                    DataConvert.ProtectChanged,
                    SdkHandler.EnumMsgNotifyType.PopupBubble, "",
                    SdkHandler.EnumMsgNotifyResult.Succeed,
                    SdkHandler.EnumMsgNotifyIcon.Offline);

                documentWindow.Close();
                Globals.ThisAddIn.IsDoingSaveAs = false;
            }
            if (Globals.ThisAddIn.SdkHandler.isDeletePlainFile)
            {
                sdkH.Rmsdk.RPM_DeleteFile(filePath);
            }
        }

        #endregion
    }
}
