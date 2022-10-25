using SkydrmLocal.rmc.ui.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.Resources.languages
{
    // Used to define the common string info.
    class CultureStringInfo
    {
        public static string ApplicationFindResource(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }
            try
            {
                string ResourceString = SkydrmApp.Current.FindResource(key).ToString();
                return ResourceString;
            }
            catch (Exception e)
            {
                SkydrmApp.Singleton.Log.Error(e.Message);
                return string.Empty;
            }
        }

        #region // For message dialog box info
        public static string Common_DlgBox_Title = ApplicationFindResource("DlgBox_Title");
        public static string Common_DlgBox_Subject = ApplicationFindResource("DlgBox_Subject");

        // remove file box
        public static string RemoveFile_DlgBox_Title = ApplicationFindResource("DlgBox_Title");
        public static string RemoveFile_DlgBox_Subject = ApplicationFindResource("RemoveFile_DlgBox_Subject");

        // update file box
        public static string UpdateFile_DlgBox_Title = ApplicationFindResource("DlgBox_Title");
        public static string UpdateFile_DlgBox_Subject = ApplicationFindResource("UpdateFile_DlgBox_Subject");

        // overwrite file box
        public static string OverwriteFile_DlgBox_Title = ApplicationFindResource("DlgBox_Title");
        public static string OverwriteFile_DlgBox_Subject = ApplicationFindResource("OverwriteFile_DlgBox_Subject");

        // enforce update box
        public static string EnforceUpdate_DlgBox_Title = ApplicationFindResource("DlgBox_Title");
        public static string EnforceUpdate_DlgBox_Subject = ApplicationFindResource("EnforceUpdate_DlgBox_Subject"); 
        public static string EnforceUpdate_DlgBox_Subject_For_overwrite = ApplicationFindResource("EnforceUpdate_DlgBox_Subject_For_overwrite"); 

        // replace prompt dialog info
        public static string ReplaceFile_DlgBox_Title = ApplicationFindResource("DlgBox_Title");
        public static string ReplaceFile_DlgBox_Subject = ApplicationFindResource("ReplaceFile_DlgBox_Subject");

        // session expiration
        public static string SessionInvalid_DlgBox_Title = ApplicationFindResource("DlgBox_Title");
        public static string SessionInvalid_DlgBox_Details = ApplicationFindResource("SessionInvalid_DlgBox_Details");

        // Internet connection
        public static string CheckConnection_DlgBox_Title = ApplicationFindResource("DlgBox_Title");
        public static string CheckConnection_DlgBox_Subject = ApplicationFindResource("CheckConnection_DlgBox_Subject");
        public static string CheckConnection_DlgBox_Details = ApplicationFindResource("CheckConnection_DlgBox_Details");

        #endregion // For message box info

        //for window Tag
        public const string ShowFileInfoWin_Operation_ShowDetail = "ShowFileInfoDetail";

        //SelectRights Component
        public static string SelectRights_View = ApplicationFindResource("SelectRights_View");
        public static string SelectRights_Edit = ApplicationFindResource("SelectRights_Edit");
        public static string SelectRights_Share = ApplicationFindResource("SelectRights_Share");
        public static string SelectRights_Print = ApplicationFindResource("SelectRights_Print");
        public static string SelectRights_SaveAs = ApplicationFindResource("SelectRights_SaveAs");
        public static string SelectRights_Download = ApplicationFindResource("SelectRights_Download");
        public static string SelectRights_Extract = ApplicationFindResource("SelectRights_Extract");
        public static string SelectRights_Watermark = ApplicationFindResource("SelectRights_Watermark");
        public static string SelectRights_Validity = ApplicationFindResource("SelectRights_Validity");

    }
}
