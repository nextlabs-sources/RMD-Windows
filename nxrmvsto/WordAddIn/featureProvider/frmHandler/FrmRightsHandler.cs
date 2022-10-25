using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace WordAddIn.featureProvider
{
    class FrmRightsHandler
    {
        private string plainFilePath { get; set; }
        private SdkHandler appSdkHandler { get; set; }

        private Action<string> PositiveBtnCallBack;
        private Action CancelBtnCallBack;

        private FrmRightsSelect protectFrmRights;
        private BackgroundWorker DoProtect_BgWorker = new BackgroundWorker();

        private string nxlFilePath { get; set; }
        /// <summary>
        /// use for return dialog result
        /// </summary>
        private bool isProtectSuccess { get; set; }

        public FrmRightsHandler(string filePath, RightsSelectDataModel dataModel, SdkHandler appSdk, Action<string> positiveBtnCallBack = null, Action cancelBtnCallBack = null)
        {
            plainFilePath = filePath;
            appSdkHandler = appSdk;
            PositiveBtnCallBack = positiveBtnCallBack;
            CancelBtnCallBack = cancelBtnCallBack;

            protectFrmRights = new FrmRightsSelect(dataModel);
            protectFrmRights.PositiveBtnEvent += ProtectFrmRights_PositiveBtnEvent;
            protectFrmRights.CancelBtnEvent += ProtectFrmRights_CancelBtnEvent;

            DoProtect_BgWorker.WorkerReportsProgress = true;
            DoProtect_BgWorker.WorkerSupportsCancellation = true;
            DoProtect_BgWorker.DoWork += DoProtect_Handler;
            DoProtect_BgWorker.RunWorkerCompleted += DoProtectCompleted_Handler;
        }

        /// <summary>
        /// Show FrmRights dialog, retun true is protect successful.
        /// </summary>
        /// <param name="nxlPath"></param>
        /// <returns></returns>
        public bool ShowDialog(out string nxlPath, IWin32Window owner = null)
        {
            bool result = false;
            nxlPath = "";
            try
            {
                if (owner != null)
                {
                    protectFrmRights.ShowDialog(owner);
                }
                else
                {
                    protectFrmRights.ShowDialog();
                }
                
                result = isProtectSuccess;
                if (result)
                {
                    nxlPath = nxlFilePath;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            return result;
        }

        private void ProtectFrmRights_CancelBtnEvent(object sender, EventArgs e)
        {
            CancelBtnCallBack?.Invoke();
            protectFrmRights.Close();
        }

        private void ProtectFrmRights_PositiveBtnEvent(object sender, EventArgs e)
        {
            if (!DoProtect_BgWorker.IsBusy)
            {
                protectFrmRights.OpenProgress();

                DoProtect_BgWorker.RunWorkerAsync();
            }
        }

        private void DoProtect_Handler(object sender, DoWorkEventArgs e)
        {
            bool invoke = ProtectHandleFile(plainFilePath, out string nxlPath);
            if (invoke)
            {
                nxlFilePath = nxlPath;
            }

            e.Result = invoke;
        }
        private void DoProtectCompleted_Handler(object sender, RunWorkerCompletedEventArgs e)
        {
            protectFrmRights.CloseProgress();

            bool invoke = (bool)e.Result;
            if (invoke)
            {
                appSdkHandler.NotifyMsg(Path.GetFileName(nxlFilePath),
                        DataConvert.ProtectSucString,
                        SdkHandler.EnumMsgNotifyType.LogMsg, "",
                        SdkHandler.EnumMsgNotifyResult.Succeed,
                        SdkHandler.EnumMsgNotifyIcon.Offline);

                PositiveBtnCallBack?.Invoke(nxlFilePath);
                isProtectSuccess = true;
                protectFrmRights.Close();
            }
        }
        private bool ProtectHandleFile(string filePath, out string nxlFilePath)
        {
            bool result = true;
            nxlFilePath = "";
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    return false;
                }
                // Now we should support protect a file in RPM folder
                //if (appSdkHandler.IsRPMFolder(Path.GetDirectoryName(filePath)))
                //{
                //    appSdkHandler.NotifyMsg("",
                //       DataConvert.RPM_Protect_Failed,
                //       SdkHandler.EnumMsgNotifyType.PopupBubble, "",
                //       SdkHandler.EnumMsgNotifyResult.Failed,
                //       SdkHandler.EnumMsgNotifyIcon.Unknown);
                //    return false;
                //}

                // Create .nxl file
                if (protectFrmRights.DataModel.AdhocRadioDefultChecked)
                {
                    List<FileRights> fileRights = DataConvert.FrmRights2SdkRights(protectFrmRights.DataModel.SelectedRights);
                    WaterMarkInfo waterMark = new WaterMarkInfo()
                    {
                        fontName = "",
                        fontColor = "",
                        fontSize = 10,
                        text = ""
                    };

                    if (fileRights.Contains(FileRights.RIGHT_WATERMARK))
                    {
                        waterMark.text = protectFrmRights.DataModel.Watermark;
                    }
                    if (DataConvert.IsExpiry(protectFrmRights.DataModel.Expiry))
                    {
                        appSdkHandler.NotifyMsg(Path.GetFileName(filePath),
                        DataConvert.ExpiryString,
                        SdkHandler.EnumMsgNotifyType.PopupBubble, "",
                        SdkHandler.EnumMsgNotifyResult.Failed,
                        SdkHandler.EnumMsgNotifyIcon.Unknown);
                        return false;
                    }

                    sdk.Expiration expiration = DataConvert.FrmExpt2SdkExpt(protectFrmRights.DataModel.Expiry);
                    nxlFilePath = appSdkHandler.ProtectFileAdhoc(protectFrmRights, filePath, fileRights, waterMark, expiration);
                }
                else
                {
                    if (!protectFrmRights.DataModel.IsValidTags)
                    {
                        appSdkHandler.NotifyMsg("",
                        DataConvert.MandatoryTagString,
                        SdkHandler.EnumMsgNotifyType.PopupBubble, "",
                        SdkHandler.EnumMsgNotifyResult.Failed,
                        SdkHandler.EnumMsgNotifyIcon.Unknown);
                        return false;
                    }

                    UserSelectTags tags = new UserSelectTags();
                    foreach (var item in protectFrmRights.DataModel.SelectedTags)
                    {
                        tags.AddTag(item.Key, item.Value);
                    }
                    nxlFilePath = appSdkHandler.ProtectFileCentrolPolicy(protectFrmRights, filePath, tags);
                }
            }
            catch (SkydrmException skyEx)
            {
                result = false;
                appSdkHandler.NotifyMsg(Path.GetFileName(filePath),
                    skyEx.Message,
                    SdkHandler.EnumMsgNotifyType.PopupBubble, "",
                    SdkHandler.EnumMsgNotifyResult.Failed,
                    SdkHandler.EnumMsgNotifyIcon.Unknown);
            }
            catch (Exception ex)
            {
                result = false;
                if (!ex.Message.Equals("Cancel"))
                {
                    appSdkHandler.NotifyMsg(Path.GetFileName(filePath),
                    DataConvert.Protect_Failed,
                    SdkHandler.EnumMsgNotifyType.PopupBubble, "",
                    SdkHandler.EnumMsgNotifyResult.Failed,
                    SdkHandler.EnumMsgNotifyIcon.Unknown);
                }
                
                Debug.WriteLine(ex);
            }
            return result;
        }

    }
}
