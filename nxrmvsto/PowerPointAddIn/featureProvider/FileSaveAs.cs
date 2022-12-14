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
using WinFormControlLibrary;
using WordAddIn;
using WordAddIn.featureProvider;
using WordAddIn.featureProvider.helper;
using WordAddIn.sdk;
using WordAddIn.winApi;
using Office = Microsoft.Office.Core;
using PPT = Microsoft.Office.Interop.PowerPoint;

namespace PowerPointAddIn.featureProvider
{
    class FileSaveAs
    {
        #region Do SaveAs

        public void SaveAsShowDialog(string fileName)
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

                    if (File.Exists(filePath))
                    {
                        Globals.ThisAddIn.SdkHandler.isDeletePlainFile = false;
                    }
                    else
                    {
                        Globals.ThisAddIn.SdkHandler.isDeletePlainFile = true;
                    }

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
                        ShowSaveAsFrmRights(filePath);  
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
           
        }

        private void ShowSaveAsFrmRights(string filePath)
        {
            var sdkH = Globals.ThisAddIn.SdkHandler;
            BuildFrmRightsData build = new BuildFrmRightsData(Properties.Resources.ppt, filePath,
                sdkH.SysBucketIsEnableAdhoc, sdkH.RmsWaterMarkInfo.text,
                DataConvert.SdkExpt2FrmExpt(sdkH.RmsExpiration), DataConvert.SdkTag2FrmTag(sdkH.SysBucketClassifications));
            
            FrmRightsHandler frmRights = new FrmRightsHandler(filePath, build.DataModel, sdkH);
            NativeWindow nativeWindow = new NativeWindow();
            nativeWindow.AssignHandle(new IntPtr(Globals.ThisAddIn.Application.HWND));

            string nxlFilePath = "";
            bool isProtectSuccess = frmRights.ShowDialog(out nxlFilePath, nativeWindow);
            nativeWindow.ReleaseHandle();

            if (isProtectSuccess)
            {
                if (!File.Exists(nxlFilePath))
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
                    
                }
            }
        }

        #endregion
    }
}
