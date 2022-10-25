using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WordAddIn.featureProvider.helper;
using WordAddIn.winApi;
using Word = Microsoft.Office.Interop.Word;

namespace WordAddIn.featureProvider
{
    class SensitivityFileProtect
    {
        #region Do Protect

        public void ProtectFile(string docPath, string label)
        {
            Globals.ThisAddIn.SdkHandler.isDeletePlainFile = true;

            ProtectAndOpenFile(docPath, label, false);
        }

        private void ProtectAndOpenFile(string filePath, string label, bool opennxl)
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
            else if (!sdkH.IsRPMFolder(Path.GetDirectoryName(nxlFilePath)))
            {
                sdkH.NotifyMsg(Path.GetFileName(filePath),
                    DataConvert.SystemInnerError,
                    SdkHandler.EnumMsgNotifyType.PopupBubble, "",
                    SdkHandler.EnumMsgNotifyResult.Failed,
                    SdkHandler.EnumMsgNotifyIcon.Unknown);
            }

            return;

            if (!File.Exists(nxlFilePath) || !opennxl)
            {
                return;
            }

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
        #endregion
    }
}
