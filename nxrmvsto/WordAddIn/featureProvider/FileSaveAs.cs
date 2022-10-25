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
    class FileSaveAs
    {
        #region Do SaveAs

        public void SaveAsShowDialog(string fileName)
        {
            //dynamic dialog = Application.Dialogs[Word.WdWordDialog.wdDialogFileSaveAs];
            //dialog.InitialFileName = fileName;

            //if (-1 == dialog.Show())
            //{
            //    string filePath = dialog.SelectedItems.Item(1);
            //    dialog.Execute();

            //    ShowSaveAsFrmRights(filePath);
            //}
            //return;

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

                // Workaround: fix open nxl file crash in word
                Globals.ThisAddIn.SdkHandler.IsAddSpecificSymbol = false;

                saveAsDlg.Execute();

                ShowSaveAsFrmRights(filePath);
            }
        }
        
        private void ShowSaveAsFrmRights(string filePath)
        {
            var sdkH = Globals.ThisAddIn.SdkHandler;
            BuildFrmRightsData build = new BuildFrmRightsData(Properties.Resources.word, filePath,
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

                        //Thread.Sleep(3000);
                        //currentWindow.Close();

                        Globals.ThisAddIn.Application.ActiveWindow.Close();
                        //Globals.ThisAddIn.Application.ActiveDocument.Close();

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
