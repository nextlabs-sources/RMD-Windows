using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.process;
using SkydrmLocal.rmc.sdk;
using SkydrmLocal.rmc.ui;
using SkydrmLocal.rmc.ui.windows;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.Edit
{
    public class Helper
    {

        public static CustomMessageBoxWindow.CustomMessageBoxResult ShowUpdateDialog(string fileName)
        {
            // string subject = "The file '" + fileName + "' " + CultureStringInfo.UpdateFile_DlgBox_Subject;

            string subject = string.Format(CultureStringInfo.UpdateFile_DlgBox_Subject, fileName);  

            return CustomMessageBoxWindow.Show(
                         CultureStringInfo.UpdateFile_DlgBox_Title,
                         subject,
                         "",
                         CustomMessageBoxWindow.CustomMessageBoxIcon.None,
                         CustomMessageBoxWindow.CustomMessageBoxButton.BTN_YES,
                         CustomMessageBoxWindow.CustomMessageBoxButton.BTN_NO
                     );
        }


        public static CustomMessageBoxWindow.CustomMessageBoxResult ShowOverwriteDialog(string fileName)
        {
            //   string subject = "The file '" + fileName + "' " + CultureStringInfo.OverwriteFile_DlgBox_Subject;

            string subject = string.Format(CultureStringInfo.OverwriteFile_DlgBox_Subject, fileName);

            return CustomMessageBoxWindow.Show(
                 CultureStringInfo.OverwriteFile_DlgBox_Title,
                 subject,
                 "",
                 CustomMessageBoxWindow.CustomMessageBoxIcon.None,
                 CustomMessageBoxWindow.CustomMessageBoxButton.BTN_OVERWRITE,
                 CustomMessageBoxWindow.CustomMessageBoxButton.BTN_DISCARD
             );
        }

        public static CustomMessageBoxWindow.CustomMessageBoxResult ShowEnforceUpdateDialog(string fileName)
        {
            string subject = string.Format(CultureStringInfo.EnforceUpdate_DlgBox_Subject, fileName);

            return CustomMessageBoxWindow.Show(
                 CultureStringInfo.EnforceUpdate_DlgBox_Title,
                 subject,
                 "",
                 CustomMessageBoxWindow.CustomMessageBoxIcon.None,
                 CustomMessageBoxWindow.CustomMessageBoxButton.BTN_OK
             );

        }

    }

    public class EditMap
    {
        // this is <localPath, EditFeature> map.
        private static Dictionary<string, IEditFeature> editMap = new Dictionary<string, IEditFeature>();

        public static void Add(string localPath, IEditFeature editFeature)
        {
            if (!string.IsNullOrEmpty(localPath) && !editMap.ContainsKey(localPath))
            {
                editMap.Add(localPath, editFeature);
            }
        }

        public static IEditFeature GetValue(string localPath)
        {
            if (editMap.ContainsKey(localPath))
            {
                return editMap[localPath];
            }

            return null;
        }

        public static bool Remove(string localPath)
        {
            return editMap.Remove(localPath);
        }
    }

    public class EditCallBack
    {
        public bool IsEdit { get; set; }
        public string LocalPath { get; set; }

        public EditCallBack(bool ie, string lp)
        {
            this.IsEdit = ie;
            this.LocalPath = lp;
        }
    }
}
