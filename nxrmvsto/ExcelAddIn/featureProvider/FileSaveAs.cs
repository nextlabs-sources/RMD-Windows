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
using WordAddIn;
using WordAddIn.featureProvider;
using WordAddIn.featureProvider.helper;
using WordAddIn.sdk;
using WordAddIn.winApi;
using Excel = Microsoft.Office.Interop.Excel;
using Office = Microsoft.Office.Core;

namespace ExcelAddIn.featureProvider
{
    class FileSaveAs
    {
        #region Do SaveAs

        public void SaveAsShowDialog(string fileName)
        {
            var saveAsDlg = Globals.ThisAddIn.Application.FileDialog[Office.MsoFileDialogType.msoFileDialogSaveAs];
            saveAsDlg.InitialFileName = fileName;

            if (-1 == saveAsDlg.Show())
            {
                System.Diagnostics.Debug.WriteLine("d.showed" + saveAsDlg.Item);
                string filePath = saveAsDlg.SelectedItems.Item(1);

                if (File.Exists(filePath))
                {
                    Globals.ThisAddIn.SdkHandler.isDeletePlainFile = false;
                }
                else
                {
                    Globals.ThisAddIn.SdkHandler.isDeletePlainFile = true;
                }

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

                ShowSaveAsFrmRights(filePath);
            }
        }
        
        private void ShowSaveAsFrmRights(string filePath)
        {
            var sdkH = Globals.ThisAddIn.SdkHandler;
            BuildFrmRightsData build = new BuildFrmRightsData(Properties.Resources.excel, filePath,
                sdkH.SysBucketIsEnableAdhoc, sdkH.RmsWaterMarkInfo.text,
                DataConvert.SdkExpt2FrmExpt(sdkH.RmsExpiration), DataConvert.SdkTag2FrmTag(sdkH.SysBucketClassifications));

            FrmRightsHandler frmRights = new FrmRightsHandler(filePath, build.DataModel, sdkH);
            string nxlFilePath = "";
            bool isProtectSuccess = frmRights.ShowDialog(out nxlFilePath);
            if (isProtectSuccess)
            {
                if (!File.Exists(nxlFilePath))
                {
                    return;
                }

                Task.Factory.StartNew(()=> {

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

                        // Thread.Sleep(3000);
                        Globals.ThisAddIn.Application.ActiveWindow.Close();
                        //if (Globals.ThisAddIn.Application.Windows.Count == 1)
                        //{
                        //    Globals.ThisAddIn.Application.Quit();
                        //}
                        //else
                        //{
                        //    Globals.ThisAddIn.Application.ActiveWindow.Close();
                        //}
                    }
                });
               
            }
        }

        #endregion
    }
}
