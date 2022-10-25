using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinFormControlLibrary;
using WordAddIn.featureProvider;
using WordAddIn.featureProvider.helper;
using WordAddIn.sdk;

namespace ExcelAddIn.featureProvider
{
    class NxlFilePermission
    {
        #region Do FileInfo
        public void FileInfoShowFrm()
        {
            string filePath = Globals.ThisAddIn.Application.ActiveWorkbook.FullName;
            string displayPath = Path.GetFileName(filePath) +".nxl";

            try
            {
                Globals.ThisAddIn.SdkHandler.GetRPMFileRights(filePath, out List<WordAddIn.sdk.FileRights> rights, out WordAddIn.sdk.WaterMarkInfo waterMark);
                Dictionary<string, List<string>> tags = Globals.ThisAddIn.SdkHandler.ReadFileTags(filePath);

                BuildFrmFileInfoData build = new BuildFrmFileInfoData(displayPath, tags, DataConvert.SdkRights2FrmRights(rights.ToArray()), waterMark.text);
                FrmFileInfo frmFileInfo = new FrmFileInfo(build.DataMode);
                frmFileInfo.ShowDialog();
            }
            catch (SkydrmException skyEx)
            {
                Globals.ThisAddIn.SdkHandler.NotifyMsg(Globals.ThisAddIn.Application.ActiveWorkbook.Name,
                    skyEx.Message,
                    WordAddIn.SdkHandler.EnumMsgNotifyType.PopupBubble, "",
                    WordAddIn.SdkHandler.EnumMsgNotifyResult.Failed,
                    WordAddIn.SdkHandler.EnumMsgNotifyIcon.Unknown);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }
        #endregion
    }
}
