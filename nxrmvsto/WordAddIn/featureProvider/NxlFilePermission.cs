using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinFormControlLibrary;
using WordAddIn.featureProvider.helper;
using WordAddIn.sdk;

namespace WordAddIn.featureProvider
{
    class NxlFilePermission
    {
        #region Do FileInfo
        public void FileInfoShowFrm()
        {
            string filePath = Globals.ThisAddIn.Application.ActiveDocument.FullName;
            string displayPath = Path.GetFileName(filePath) + ".nxl";

            try
            {
                Globals.ThisAddIn.SdkHandler.GetRPMFileRights(filePath, out List<FileRights> rights, out WaterMarkInfo waterMark);
                Dictionary<string, List<string>>  tags = Globals.ThisAddIn.SdkHandler.ReadFileTags(filePath);

                BuildFrmFileInfoData build = new BuildFrmFileInfoData(displayPath, tags, DataConvert.SdkRights2FrmRights(rights.ToArray()), waterMark.text);
                FrmFileInfo frmFileInfo = new FrmFileInfo(build.DataMode);
                frmFileInfo.ShowDialog();
            }
            catch (SkydrmException skyEx)
            {
                Globals.ThisAddIn.SdkHandler.NotifyMsg(Globals.ThisAddIn.Application.ActiveDocument.Name,
                    skyEx.Message,
                    SdkHandler.EnumMsgNotifyType.PopupBubble, "",
                    SdkHandler.EnumMsgNotifyResult.Failed,
                    SdkHandler.EnumMsgNotifyIcon.Unknown);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }
        #endregion
    }
}
