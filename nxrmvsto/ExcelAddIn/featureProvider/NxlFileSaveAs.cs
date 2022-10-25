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
using WordAddIn.featureProvider;
using WordAddIn.featureProvider.helper;
using WordAddIn.sdk;

namespace ExcelAddIn.featureProvider
{
    class NxlFileSaveAs
    {
        #region Do SaveAs
        private string plainFilePath { get; set; }

        private string destFilePath { get; set; }

        private FrmRightsSelect saveAsFrmRights;

        public void SaveAsShowDialog(string filePath)
        {
            plainFilePath = filePath;

            SaveFileDialog dialog = new SaveFileDialog();

            string oldName = Path.GetFileName(filePath);
            string newName = DataConvert.ReplaceNxlFileTimestamp(oldName);

            dialog.FileName = newName + ".nxl";
            dialog.DefaultExt = "nxl";

            dialog.Filter = "NextLabs Protected Document(*.nxl)|*.nxl";

            dialog.FilterIndex = 1;
            dialog.RestoreDirectory = true;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string selectedPath = dialog.FileName.ToString();

                ShowSaveAsFrmRights(selectedPath);
            }
        }

        private void ShowSaveAsFrmRights(string destPath)
        {
            destFilePath = destPath;

            string fileName = Path.GetFileName(destPath);

            var sdkH = Globals.ThisAddIn.SdkHandler;
            BuildFrmRightsData build = new BuildFrmRightsData(Properties.Resources.excel, "\\" + fileName,
                sdkH.SysBucketIsEnableAdhoc, sdkH.RmsWaterMarkInfo.text,
                DataConvert.SdkExpt2FrmExpt(sdkH.RmsExpiration), DataConvert.SdkTag2FrmTag(sdkH.SysBucketClassifications), false, false);

            saveAsFrmRights = new FrmRightsSelect(build.DataModel);
            saveAsFrmRights.PositiveBtnEvent += SaveAsFrmRights_PositiveBtnEvent;
            saveAsFrmRights.SkipBtnEvent += SaveAsFrmRights_SkipBtnEvent;
            saveAsFrmRights.ShowDialog();
        }
        private void SaveAsFrmRights_SkipBtnEvent(object sender, EventArgs e)
        {
        }
        private void SaveAsFrmRights_PositiveBtnEvent(object sender, EventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    Thread.Sleep(500);

                    SaveAsHandleFile(plainFilePath, destFilePath);
                }
                catch (SkydrmException skyEx)
                {
                    Globals.ThisAddIn.SdkHandler.NotifyMsg(Globals.ThisAddIn.Application.ActiveWorkbook.Name,
                        skyEx.Message,
                        WordAddIn.SdkHandler.EnumMsgNotifyType.PopupBubble, "",
                        WordAddIn.SdkHandler.EnumMsgNotifyResult.Failed,
                        WordAddIn.SdkHandler.EnumMsgNotifyIcon.Unknown);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            });

        }
        private void SaveAsHandleFile(string filePath, string destPath)
        {
            // Create .nxl file
            string destFilePath = "";
            if (saveAsFrmRights.DataModel.AdhocRadioDefultChecked)
            {
                List<FileRights> fileRights = DataConvert.FrmRights2SdkRights(saveAsFrmRights.DataModel.SelectedRights);
                WaterMarkInfo waterMark = new WaterMarkInfo()
                {
                    fontName = "",
                    fontColor = "",
                    fontSize = 10,
                    text = ""
                };

                if (fileRights.Contains(FileRights.RIGHT_WATERMARK))
                {
                    waterMark.text = saveAsFrmRights.DataModel.Watermark;
                }
                if (DataConvert.IsExpiry(saveAsFrmRights.DataModel.Expiry))
                {
                    Globals.ThisAddIn.SdkHandler.NotifyMsg("",
                    DataConvert.ExpiryString,
                    WordAddIn.SdkHandler.EnumMsgNotifyType.PopupBubble, "",
                    WordAddIn.SdkHandler.EnumMsgNotifyResult.Failed,
                    WordAddIn.SdkHandler.EnumMsgNotifyIcon.Unknown);
                    return;
                }

                WordAddIn.sdk.Expiration expiration = DataConvert.FrmExpt2SdkExpt(saveAsFrmRights.DataModel.Expiry);
                destFilePath = Globals.ThisAddIn.SdkHandler.ProtectFileAdhoc(filePath, destPath, fileRights, waterMark, expiration);
            }
            else
            {
                if (!saveAsFrmRights.DataModel.IsValidTags)
                {
                    Globals.ThisAddIn.SdkHandler.NotifyMsg("",
                    DataConvert.MandatoryTagString,
                    WordAddIn.SdkHandler.EnumMsgNotifyType.PopupBubble, "",
                    WordAddIn.SdkHandler.EnumMsgNotifyResult.Failed,
                    WordAddIn.SdkHandler.EnumMsgNotifyIcon.Unknown);
                    return;
                }
                UserSelectTags tags = new UserSelectTags();
                foreach (var item in saveAsFrmRights.DataModel.SelectedTags)
                {
                    tags.AddTag(item.Key, item.Value);
                }
                destFilePath = Globals.ThisAddIn.SdkHandler.ProtectFileCentrolPolicy(filePath, destPath, tags);
            }

            //notify
            Globals.ThisAddIn.SdkHandler.NotifyMsg(Path.GetFileName(destFilePath),
                    DataConvert.ProtectSucString,
                    WordAddIn.SdkHandler.EnumMsgNotifyType.LogMsg, "",
                    WordAddIn.SdkHandler.EnumMsgNotifyResult.Succeed,
                    WordAddIn.SdkHandler.EnumMsgNotifyIcon.Offline);
        }
        #endregion
    }
}
