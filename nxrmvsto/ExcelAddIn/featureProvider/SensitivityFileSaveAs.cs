using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;
using Office = Microsoft.Office.Core;
using WordAddIn;
using WordAddIn.featureProvider;
using WordAddIn.featureProvider.helper;
using WordAddIn.sdk;
using WordAddIn.winApi;
using System.Threading;
using System.Diagnostics;

namespace ExcelAddIn.featureProvider
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

                // Handle pdf specially.
                string ext = Path.GetExtension(filePath);
                if (!string.IsNullOrEmpty(ext) && ext.Equals(".pdf", StringComparison.CurrentCultureIgnoreCase))
                {
                    try
                    {
                        Microsoft.Office.Interop.Excel.Application excelApplication = Globals.ThisAddIn.Application;

                        Excel.Workbook wb = excelApplication.ActiveWorkbook;
                        if (wb != null)
                        {
                            wb.ExportAsFixedFormat(Excel.XlFixedFormatType.xlTypePDF, filePath);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
                // workaround: our save as can't handle '.xltx' format, so will popup the raw built in dialog to save as again.
                else if (!string.IsNullOrEmpty(ext) && ext.Equals(".xltx", StringComparison.CurrentCultureIgnoreCase))
                {
                    //  send notification to popup bubble
                    Globals.ThisAddIn.SdkHandler?.NotifyMsg(Path.GetFileName(filePath),
                    DataConvert.UingRawBuiltinSaveAs,
                    SdkHandler.EnumMsgNotifyType.PopupBubble, "",
                    SdkHandler.EnumMsgNotifyResult.Failed,
                    SdkHandler.EnumMsgNotifyIcon.Unknown);

                    // Save as again
                    Excel.Dialog dialog = Globals.ThisAddIn.Application.Dialogs[Excel.XlBuiltInDialog.xlDialogSaveAs];
                    Globals.ThisAddIn.IsHasShownDlg = true;
                    dialog.Show(); // block dialog

                    return;
                }
                else
                {
                    saveAsDlg.Execute();
                }

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

            Task.Factory.StartNew(() => {

                Thread.Sleep(1000);
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
