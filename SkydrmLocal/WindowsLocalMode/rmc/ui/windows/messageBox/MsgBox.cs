using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.ui.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.ui.windows.messageBox
{
    public class MsgBox
    {
        public static CustomMessageBoxWindow.CustomMessageBoxResult ShowRemoveDialog(INxlFile nxlFile)
        {
            string fileSize = CommonUtils.GetSizeString(nxlFile.Size);
            string dateModified = "";
            if (nxlFile.RawDateModified != null)
            {
                dateModified = nxlFile.RawDateModified.ToString("dd MMMM yyyy");
            }
            string name = nxlFile.Name;
            string details = name + "\n"
                + "Size: " + fileSize + "\n"
                + "Date modified: " + dateModified;

            return CustomMessageBoxWindow.Show(
                     CultureStringInfo.RemoveFile_DlgBox_Title,
                     CultureStringInfo.RemoveFile_DlgBox_Subject,
                     details,
                     CustomMessageBoxWindow.CustomMessageBoxIcon.None,
                     CustomMessageBoxWindow.CustomMessageBoxButton.BTN_DELETE,
                     CustomMessageBoxWindow.CustomMessageBoxButton.BTN_CANCEL
                 );
        }
    }
}
