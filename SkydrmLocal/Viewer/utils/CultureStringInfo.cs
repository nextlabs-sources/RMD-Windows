using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Viewer.utils
{
    public class CultureStringInfo
    {
        public static string Common_Microsoft_Corporation ;
        public static string View_DlgBox_Title ;
        public static string VIEW_DLGBOX_DETAILS_DECRYPTFAILED ;
        public static string VIEW_DLGBOX_DETAILS_NOTSUPPORT ;
        public static string VIEW_DLGBOX_DETAILS_NOT_AUTHORIZED ;
        public static string VIEW_DLGBOX_DETAILS_IMAGE_DAMAGED ;
        public static string VIEW_DLGBOX_DETAILS_SYSTEM_INTERNAL_ERROR ;
        public static string VIEW_DLGBOX_DETAILS_PRINT_SERVER_BUSY ;
        public static string VIEW_DLGBOX_DETAILS_FILE_HAS_EXPIRED ;
        public static string Exception_ExportFeature_Succeeded ;
        public static string Common_DenyOp_InRMP ;
        public static string Common_System_Internal_Error ;
        public static string Common_Initialize_failed ;

        //SelectRights Component
        public static string SelectRights_View ;
        public static string SelectRights_Edit ;
        public static string SelectRights_Share ;
        public static string SelectRights_Print ;
        public static string SelectRights_SaveAs ;
        public static string SelectRights_Download ;
        public static string SelectRights_Extract ;
        public static string SelectRights_Watermark ;
        public static string SelectRights_Validity ;
        public static string Share_Email_Details ;

        // Outlook error message
        public static string Outlook_Install ;
        public static string Outlook_LogIn ;
        public static string Outlook_Open ;
        public static string Outlook_Dialog_Caption ;

        //Validity Never expire description
        public static string ValidityWin_Never_Description2 ;
        public static string Validity_ShareFile_Expired ;
        public static string ShareFileWin_Notify_File_Share_Failed_Because_revoked ;
        public static string ShareFileWin_Notify_File_Share_Failed ;
        public static string Windows_Btn_Close ;
        public static string Notify_PopBubble_Forbid_Logout ;

        // <!-- ***************************** Notify message to service manager: Record Log! ********************************* -->
        public static string Notify_RecordLog_Extract_Content_Failed ;

        public static string Exception_ExportFeature_Failed ;
        public static string Successfully_Shared ;
        public static string Unknown_File ;
        public static string Expired ;
        public static string Protected_Successfully ;
        public static string Uploaded_Successfully ;
        public static string Unsuccessful_Shared ;
        public static string ShareFileWin_Notify_File_Share_Failed_No_network ;

        public static string Cannot_Set_RpmFolder_Under_System_Directory;
        public static string Remove_directory_failed;

        public static void Init(Application application)
        {
             Common_Microsoft_Corporation = ApplicationFindResource(application,"Microsoft_Corporation");
             View_DlgBox_Title = ApplicationFindResource(application ,"View_DlgBox_Title");
             VIEW_DLGBOX_DETAILS_DECRYPTFAILED = ApplicationFindResource(application ,"VIEW_DLGBOX_DETAILS_DECRYPTFAILED");
             VIEW_DLGBOX_DETAILS_NOTSUPPORT = ApplicationFindResource(application ,"VIEW_DLGBOX_DETAILS_NOTSUPPORT");
             VIEW_DLGBOX_DETAILS_NOT_AUTHORIZED = ApplicationFindResource(application ,"VIEW_DLGBOX_DETAILS_NOT_AUTHORIZED");
             VIEW_DLGBOX_DETAILS_IMAGE_DAMAGED = ApplicationFindResource(application ,"VIEW_DLGBOX_DETAILS_IMAGE_DAMAGED");
             VIEW_DLGBOX_DETAILS_SYSTEM_INTERNAL_ERROR = ApplicationFindResource(application ,"VIEW_DLGBOX_DETAILS_SYSTEM_INTERNAL_ERROR");
             VIEW_DLGBOX_DETAILS_PRINT_SERVER_BUSY = ApplicationFindResource(application ,"VIEW_DLGBOX_DETAILS_PRINT_SERVER_BUSY");
             VIEW_DLGBOX_DETAILS_FILE_HAS_EXPIRED = ApplicationFindResource(application ,"VIEW_DLGBOX_DETAILS_FILE_HAS_EXPIRED");
             Exception_ExportFeature_Succeeded = ApplicationFindResource(application ,"Exception_ExportFeature_Succeeded");
             Common_DenyOp_InRMP = ApplicationFindResource(application ,"Common_DenyOp_InRMP");
             Common_System_Internal_Error = ApplicationFindResource(application ,"Common_System_Internal_Error");
             Common_Initialize_failed = ApplicationFindResource(application ,"Common_Initialize_failed");

             //SelectRights Component
             SelectRights_View = ApplicationFindResource(application ,"SelectRights_View");
             SelectRights_Edit = ApplicationFindResource(application ,"SelectRights_Edit");
             SelectRights_Share = ApplicationFindResource(application ,"SelectRights_Share");
             SelectRights_Print = ApplicationFindResource(application ,"SelectRights_Print");
             SelectRights_SaveAs = ApplicationFindResource(application ,"SelectRights_SaveAs");
             SelectRights_Download = ApplicationFindResource(application ,"SelectRights_Download");
             SelectRights_Extract = ApplicationFindResource(application ,"SelectRights_Extract");
             SelectRights_Watermark = ApplicationFindResource(application ,"SelectRights_Watermark");
             SelectRights_Validity = ApplicationFindResource(application ,"SelectRights_Validity");

             Share_Email_Details = ApplicationFindResource(application ,"Share_Email_Details");

             // Outlook error message
             Outlook_Install = ApplicationFindResource(application ,"Outlook_Install");
             Outlook_LogIn = ApplicationFindResource(application ,"Outlook_LogIn");
             Outlook_Open = ApplicationFindResource(application ,"Outlook_Open");

             Outlook_Dialog_Caption = ApplicationFindResource(application ,"Outlook_Dialog_Caption");

            //Validity Never expire description
             ValidityWin_Never_Description2 = ApplicationFindResource(application ,"ValidityWin_Never_Description2");

             Validity_ShareFile_Expired = ApplicationFindResource(application ,"Validity_ShareFile_Expired");

             ShareFileWin_Notify_File_Share_Failed_Because_revoked = ApplicationFindResource(application ,"ShareFileWin_Notify_File_Share_Failed_Because_revoked");
             ShareFileWin_Notify_File_Share_Failed = ApplicationFindResource(application ,"ShareFileWin_Notify_File_Share_Failed");
             Windows_Btn_Close = ApplicationFindResource(application ,"Windows_Btn_Close");

             Notify_PopBubble_Forbid_Logout = ApplicationFindResource(application ,"Notify_PopBubble_Forbid_Logout");

             // <!-- ***************************** Notify message to service manager: Record Log! ********************************* -->
             Notify_RecordLog_Extract_Content_Failed = ApplicationFindResource(application ,"Notify_RecordLog_Extract_Content_Failed");

             Exception_ExportFeature_Failed = ApplicationFindResource(application ,"Exception_ExportFeature_Failed");
             Successfully_Shared = ApplicationFindResource(application ,"Successfully_Shared");
             Unknown_File = ApplicationFindResource(application ,"Unknown_File");
             Expired = ApplicationFindResource(application ,"Expired");
             Protected_Successfully = ApplicationFindResource(application ,"Protected_Successfully");
             Uploaded_Successfully = ApplicationFindResource(application ,"Uploaded_Successfully");
             Unsuccessful_Shared = ApplicationFindResource(application ,"Unsuccessful_Shared");
             ShareFileWin_Notify_File_Share_Failed_No_network = ApplicationFindResource(application ,"ShareFileWin_Notify_File_Share_Failed_No_network");

             Cannot_Set_RpmFolder_Under_System_Directory = ApplicationFindResource(application, "Cannot_Set_RpmFolder_Under_System_Directory");

             Remove_directory_failed = ApplicationFindResource(application, "Remove_directory_failed");
        }

        public static string ApplicationFindResource(Application application, string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }
            try
            {
                string ResourceString = application.FindResource(key).ToString();
                return ResourceString;
            }
            catch (Exception e)
            {
                return string.Empty;
            }
        }
    }
}
