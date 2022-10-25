using SkydrmLocal.rmc.ui.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.ui
{
    // Used to define the common string info.
    class CultureStringInfo
    {
        /// <summary>
        /// Common
        /// </summary>
        public static string Common_File_Not_Exist = CommonUtils.ApplicationFindResource("Common_File_Not_Exist");
        public static string Common_Upload_File_Failed = CommonUtils.ApplicationFindResource("Common_Upload_File_Failed");
        public static string Common_Initialize_failed = CommonUtils.ApplicationFindResource("Common_Initialize_failed");
        public static string Common_DenyOp_InRMP = CommonUtils.ApplicationFindResource("Common_DenyOp_InRMP");
        public static string Common_System_Internal_Error = CommonUtils.ApplicationFindResource("Common_System_Internal_Error");
        public static string Common_Not_Authorized = CommonUtils.ApplicationFindResource("Common_Not_Authorized");
        public static string Common_Mandatory_Require = CommonUtils.ApplicationFindResource("Common_Mandatory_Require");
        public static string Common_CentralPolicyFile_Not_Share = CommonUtils.ApplicationFindResource("Common_CentralPolicyFile_Not_Share");
        public static string Common_AdhocFile_Not_AddToProject = CommonUtils.ApplicationFindResource("Common_AdhocFile_Not_AddToProject");
        public static string Common_Wait_Downloading = CommonUtils.ApplicationFindResource("Common_Wait_Downloading");
        public static string Common_ForbidLogoutExit_WhenEditing = CommonUtils.ApplicationFindResource("Common_ForbidLogoutExit_WhenEditing");
        public static string Common_NxLFile_Rights_Cannot_Modified = CommonUtils.ApplicationFindResource("Common_NxLFile_Rights_Cannot_Modified");

        // Can't find the dest folder, may it has been deleted.
        public static string Common_Upload_Not_Found_DestFolder = CommonUtils.ApplicationFindResource("Common_Upload_Not_Found_DestFolder");

        // Sync modified file failed after check version
        public static string Sync_ModifiedFile_download_Failed = CommonUtils.ApplicationFindResource("Sync_ModifiedFile_download_Failed");

        // Outlook error message
        public static string Outlook_Install = CommonUtils.ApplicationFindResource("Outlook_Install");
        public static string Outlook_LogIn = CommonUtils.ApplicationFindResource("Outlook_LogIn");
        public static string Outlook_Open = CommonUtils.ApplicationFindResource("Outlook_Open");

        public static string Outlook_Dialog_Caption = CommonUtils.ApplicationFindResource("Outlook_Dialog_Caption");

        /// <summary>
        /// Edit offline file
        /// </summary>
        public static string EditOfflineFile_Redownload_Succeed = CommonUtils.ApplicationFindResource("EditOfflineFile_Redownload_Succeed");
        public static string EditOfflineFile_Redownload_Failed = CommonUtils.ApplicationFindResource("EditOfflineFile_Redownload_Failed");
        public static string EditOfflineFile_Overwrite_Succeed = CommonUtils.ApplicationFindResource("EditOfflineFile_Overwrite_Succeed");
        public static string EditOfflineFile_Discard_Overwrite = CommonUtils.ApplicationFindResource("EditOfflineFile_Discard_Overwrite");


        #region Exceptions
        public static string Exception_ExportFeature_Failed = CommonUtils.ApplicationFindResource("Exception_ExportFeature_Failed");

        public static string Exception_ExportFeature_Succeeded = CommonUtils.ApplicationFindResource("Exception_ExportFeature_Succeeded");

        public static string Exception_Sdk_General =CommonUtils.ApplicationFindResource("Exception_Sdk_General");
        public static string Exception_Sdk_Network_IO = CommonUtils.ApplicationFindResource("Exception_Sdk_Network_IO");
        public static string Exception_Sdk_Insufficient_Rights =CommonUtils.ApplicationFindResource("Exception_Sdk_Insufficient_Rights");
        public static string Exception_Sdk_Rest_General = CommonUtils.ApplicationFindResource("Exception_Sdk_Rest_General");
        public static string Exception_Sdk_Rest_400_InvalidParam = CommonUtils.ApplicationFindResource("Exception_Sdk_Rest_400_InvalidParam");
        public static string Exception_Sdk_Rest_401_Authentication_Failed = CommonUtils.ApplicationFindResource("Exception_Sdk_Rest_401_Authentication_Failed");
        public static string Exception_Sdk_Rest_403_AccessForbidden = CommonUtils.ApplicationFindResource("Exception_Sdk_Rest_403_AccessForbidden");
        public static string Exception_Sdk_Rest_404_NotFound = CommonUtils.ApplicationFindResource("Exception_Sdk_Rest_404_NotFound");
        public static string Exception_Sdk_Rest_500_ServerInternal = CommonUtils.ApplicationFindResource("Exception_Sdk_Rest_500_ServerInternal");
        public static string Exception_Sdk_Rest_6001_StorageExceeded = CommonUtils.ApplicationFindResource("Exception_Sdk_Rest_6001_StorageExceeded");
        //for myvault 
        public static string Exception_Sdk_Rest_MyVault_304_RevokedFile = CommonUtils.ApplicationFindResource("Exception_Sdk_Rest_MyVault_304_RevokedFile");
        public static string Exception_Sdk_Rest_MyVault_4003_ExpiredFile = CommonUtils.ApplicationFindResource("Exception_Sdk_Rest_MyVault_4003_ExpiredFile");
        public static string Exception_Sdk_Rest_MyVault_5001_InvalidNxl = CommonUtils.ApplicationFindResource("Exception_Sdk_Rest_MyVault_5001_InvalidNxl");
        public static string Exception_Sdk_Rest_MyVault_5002_InvalidRepoMetadata = CommonUtils.ApplicationFindResource("Exception_Sdk_Rest_MyVault_5002_InvalidRepoMetadata");
        public static string Exception_Sdk_Rest_MyVault_5003_InvalidFileName = CommonUtils.ApplicationFindResource("Exception_Sdk_Rest_MyVault_5003_InvalidFileName");
        public static string Exception_Sdk_Rest_MyVault_5004_InvalidFileExtension = CommonUtils.ApplicationFindResource("Exception_Sdk_Rest_MyVault_5004_InvalidFileExtension");
        public static string Exception_Sdk_Rest_MyVault_5005_InvalidFileExtension = CommonUtils.ApplicationFindResource("Exception_Sdk_Rest_MyVault_5005_InvalidFileExtension");

        #endregion


        /// <summary>
        /// ServiceManager 
        /// </summary>
        public static string MenuItem_About = CommonUtils.ApplicationFindResource("ServiceManageWin_MenuItem_About");
        public static string MenuItem_Logout = CommonUtils.ApplicationFindResource("ServiceManageWin_MenuItem_Logout");
        public static string MenuItem_Exit = CommonUtils.ApplicationFindResource("ServiceManageWin_MenuItem_Exit");

        public static string ServiceManageWin_One_Month= CommonUtils.ApplicationFindResource("ServiceManageWin_One_Month");
        public static string ServiceManageWin_One_Week = CommonUtils.ApplicationFindResource("ServiceManageWin_One_Week");
        public static string ServiceManageWin_Two_Weeks = CommonUtils.ApplicationFindResource("ServiceManageWin_Two_Weeks");
        public static string ServiceManageWin_Day_Ago = CommonUtils.ApplicationFindResource("ServiceManageWin_Day_Ago");
        public static string ServiceManageWin_Days_Ago = CommonUtils.ApplicationFindResource("ServiceManageWin_Days_Ago");
        public static string ServiceManageWin_Hour_Ago = CommonUtils.ApplicationFindResource("ServiceManageWin_Hour_Ago");
        public static string ServiceManageWin_Hours_Ago = CommonUtils.ApplicationFindResource("ServiceManageWin_Hours_Ago");
        public static string ServiceManageWin_Minute_Ago = CommonUtils.ApplicationFindResource("ServiceManageWin_Minute_Ago");
        public static string ServiceManageWin_Minutes_Ago = CommonUtils.ApplicationFindResource("ServiceManageWin_Minutes_Ago");
        public static string ServiceManageWin_Uploaded_Just = CommonUtils.ApplicationFindResource("ServiceManageWin_Uploaded_Just");
        public static string ServiceManageWin_Uploaded = CommonUtils.ApplicationFindResource("ServiceManageWin_Uploaded");      
        public static string ServiceManageWin_Updating = CommonUtils.ApplicationFindResource("ServiceManageWin_Updating");
        public static string ServiceManageWin_Uploading = CommonUtils.ApplicationFindResource("ServiceManageWin_Uploading");
        public static string ServiceManageWin_Resume_Updating = CommonUtils.ApplicationFindResource("ServiceManageWin_Resume_Updating");
        public static string ServiceManageWin_Waiting_Upload = CommonUtils.ApplicationFindResource("ServiceManageWin_Waiting_Upload");
        public static string ServiceManageWin_Removed_Local = CommonUtils.ApplicationFindResource("ServiceManageWin_Removed_Local");

        public static string ServiceManageWin_Downloaded_Failed = CommonUtils.ApplicationFindResource("ServiceManageWin_Downloaded_Failed");
        public static string ServiceManageWin_Downloaded_Succeed = CommonUtils.ApplicationFindResource("ServiceManageWin_Downloaded_Succeed");
        public static string ServiceManageWin_Downloading = CommonUtils.ApplicationFindResource("ServiceManageWin_Downloading");
        public static string ServiceManageWin_File_Missing_In_Local = CommonUtils.ApplicationFindResource("ServiceManageWin_File_Missing_In_Local");
        public static string ServiceManageWin_UnknownError = CommonUtils.ApplicationFindResource("ServiceManageWin_UnknownError");
        public static string ServiceManageWin_AvailableOffline = CommonUtils.ApplicationFindResource("ServiceManageWin_AvailableOffline");
        public static string ServiceManageWin_Online = CommonUtils.ApplicationFindResource("ServiceManageWin_Online");
        public static string ServiceManageWin_CachedFile = CommonUtils.ApplicationFindResource("ServiceManageWin_CachedFile");
        public static string ServiceManageWin_ProtectFailed = CommonUtils.ApplicationFindResource("ServiceManageWin_ProtectFailed");
        public static string ServiceManageWin_ProtectSucceeded = CommonUtils.ApplicationFindResource("ServiceManageWin_ProtectSucceeded");
        public static string ServiceManageWin_Edit_In_Local = CommonUtils.ApplicationFindResource("ServiceManageWin_Edit_In_Local");



        ///<summary>
        /// Main Window
        ///<summary>
        //for MainWindow UI display
        public static string MainWin__TreeView_MyVault = CommonUtils.ApplicationFindResource("MainWin__TreeView_MyVault");
        public static string MainWin__TreeView_ShareWithMe = CommonUtils.ApplicationFindResource("MainWin__TreeView_ShareWithMe");
        public static string MainWin__TreeView_Project = CommonUtils.ApplicationFindResource("MainWin__TreeView_Project");

        public static string MainWindow__Start_Upload = CommonUtils.ApplicationFindResource("MainWin__Start_Upload");
        public static string MainWindow__Stop_Upload = CommonUtils.ApplicationFindResource("MainWin__Stop_Upload");

        public static string MainWin__FileListView_Date_modified= CommonUtils.ApplicationFindResource("MainWin__FileListView_Date_modified");
        public static string MainWin__FileListView_Shared_with = CommonUtils.ApplicationFindResource("MainWin__FileListView_Shared_with");
        public static string MainWin__FileListView_Shared_date = CommonUtils.ApplicationFindResource("MainWin__FileListView_Shared_date");
        public static string MainWin__FileListView_Shared_by = CommonUtils.ApplicationFindResource("MainWin__FileListView_Shared_by");

        public static string MainWin__ProjectListView_GroupByMe = CommonUtils.ApplicationFindResource("MainWin__ProjectListView_GroupByMe");
        public static string MainWin__ProjectListView_GroupByMe2 = CommonUtils.ApplicationFindResource("MainWin__ProjectListView_GroupByMe2");
        public static string MainWin__ProjectListView_GroupByOther = CommonUtils.ApplicationFindResource("MainWin__ProjectListView_GroupByOther");
        public static string MainWin__ProjectListView_GroupByOther2 = CommonUtils.ApplicationFindResource("MainWin__ProjectListView_GroupByOther2");

        public static string MainWindow_List_item = CommonUtils.ApplicationFindResource("MainWin_List_item");
        public static string MainWindow_List_items = CommonUtils.ApplicationFindResource("MainWin_List_items");

        // special file status
        public static string MainWin_List_Updating = CommonUtils.ApplicationFindResource("MainWin_List_Updating");
        public static string MainWin_List_UploadFailed = CommonUtils.ApplicationFindResource("MainWin_List_UploadFailed");
        public static string MainWin_List_Edited_In_Local = CommonUtils.ApplicationFindResource("MainWin_List_Edited_In_Local");

        // network status
        public static string NETWORK_CONNECTED = CommonUtils.ApplicationFindResource("MainWin_NETWORK_CONNECTED");
        public static string NETWORK_ERROR = CommonUtils.ApplicationFindResource("MainWin_NETWORK_ERROR");
        // line status
        public static string STATUS_ON_LINE = CommonUtils.ApplicationFindResource("MainWin_STATUS_ON_LINE");
        public static string STATUS_OFF_LINE = CommonUtils.ApplicationFindResource("MainWin_STATUS_OFF_LINE");

        // Filter local files
        public static string FILTER_VIEW_ALL = CommonUtils.ApplicationFindResource("MainWin_FILTER_VIEW_ALL");
        public static string FILTER_WAITINIG_UPLOAD = CommonUtils.ApplicationFindResource("MainWin_FILTER_WAITINIG_UPLOAD");
        public static string FILTER_ALL_LOCAL_FILES = CommonUtils.ApplicationFindResource("MainWin_FILTER_ALL_LOCAL_FILES");

        public static string Common_Remove_File_Failed = CommonUtils.ApplicationFindResource("MainWin_Common_Remove_File_Failed");

        public static string Common_SavePath_BalloonTip= CommonUtils.ApplicationFindResource("MainWin_SavePath_BalloonTip");

        public static string MainWin_PromptText_Empty= CommonUtils.ApplicationFindResource("MainWin_PromptText_Empty");
        public static string MainWin_PromptText_Loading = CommonUtils.ApplicationFindResource("MainWin_PromptText_Loading");
        public static string MainWin_PromptText_Select = CommonUtils.ApplicationFindResource("MainWin_PromptText_Select");

        public static string MainWin_DoShare = CommonUtils.ApplicationFindResource("MainWin_DoShare");

        public static string MainWin_ContextMenu_Share = CommonUtils.ApplicationFindResource("MainWin_ContextMenu_Share");
        public static string MainWin_ContextMenu_View = CommonUtils.ApplicationFindResource("MainWin_ContextMenu_View");
        public static string MainWin_ContextMenu_ViewFileInfo = CommonUtils.ApplicationFindResource("MainWin_ContextMenu_ViewFileInfo");
        public static string MainWin_ContextMenu_Remove = CommonUtils.ApplicationFindResource("MainWin_ContextMenu_Remove");
        public static string MainWin_ContextMenu_OpenWeb = CommonUtils.ApplicationFindResource("MainWin_ContextMenu_OpenWeb");
        public static string MainWin_ContextMenu_Mark = CommonUtils.ApplicationFindResource("MainWin_ContextMenu_Mark");
        public static string MainWin_ContextMenu_UnMark = CommonUtils.ApplicationFindResource("MainWin_ContextMenu_UnMark");
        public static string MainWin_ContextMenu_SaveAs = CommonUtils.ApplicationFindResource("MainWin_ContextMenu_SaveAs");
		public static string MainWin_ContextMenu_AddFile = CommonUtils.ApplicationFindResource("MainWin_ContextMenu_AddFile");
        public static string MainWin_ContextMenu_Edit = CommonUtils.ApplicationFindResource("MainWin_ContextMenu_Edit");
        public static string MainWin_ContextMenu_ExtractContent = CommonUtils.ApplicationFindResource("MainWin_ContextMenu_ExtractContent");
        public static string MainWin_ContextMenu_ModifyRights = CommonUtils.ApplicationFindResource("MainWin_ContextMenu_ModifyRights");
        public static string MainWin_ContextMenu_Upload = CommonUtils.ApplicationFindResource("MainWin_ContextMenu_Upload");

        ///<summary>
        /// ChooseServer Window
        ///<summary>
        public static string CheckUrl_Notify_UrlEmpty = CommonUtils.ApplicationFindResource("ChooseServerWin_CheckUrl_Notify_UrlEmpty");
        public static string CheckUrl_Notify_NetDisconnect = CommonUtils.ApplicationFindResource("ChooseServerWin_CheckUrl_Notify_NetDisconnect");
        public static string CheckUrl_Notify_UrlError = CommonUtils.ApplicationFindResource("ChooseServerWin_CheckUrl_Notify_UrlError");
        public static string CheckUrl_Notify_NetworkOrUrlError = CommonUtils.ApplicationFindResource("ChooseServerWin_CheckUrl_Notify_NetworkOrUrlError");



        /// <summary>
        ///  CreateFile Window
        /// </summary>
        //for window Tag
        public const string CreateFileWin_Operation_Protect = "Protect";
        public const string CreateFileWin_Operation_Share = "Share";
        public const string ShowFileInfoWin_Operation_ShowDetail = "ShowFileInfoDetail";


        public static string CreateFileWin_Notify_File_Not_Created = CommonUtils.ApplicationFindResource("CreateFileWin_Notify_File_Not_Created");
        public static string CreateFileWin_Notify_NxlFile_Not_Protect = CommonUtils.ApplicationFindResource("CreateFileWin_Notify_NxlFile_Not_Protect");
        public static string CreateFileWin_Notify_File_Protect_Failed = CommonUtils.ApplicationFindResource("CreateFileWin_Notify_File_Protect_Failed");
        public static string CreateFileWin_Notify_Wait_Protect = CommonUtils.ApplicationFindResource("CreateFileWin_Notify_Wait_Protect");

        public static string CreateFileWin__Operation_Title_MProtect = CommonUtils.ApplicationFindResource("CreateFileWin__Operation_Title_MProtect");
        public static string CreateFileWin_Operation_Title_MShare = CommonUtils.ApplicationFindResource("CreateFileWin_Operation_Title_MShare");
        public static string CreateFileWin__Operation_Title_Protect = CommonUtils.ApplicationFindResource("CreateFileWin__Operation_Title_Protect");
        public static string CreateFileWin_Operation_Title_Share = CommonUtils.ApplicationFindResource("CreateFileWin_Operation_Title_Share");
        public static string CreateFileWin_Operation_Info_ADhoc = CommonUtils.ApplicationFindResource("CreateFileWin_Operation_Info_ADhoc");
        public static string CreateFileWin_Operation_Info_Central = CommonUtils.ApplicationFindResource("CreateFileWin_Operation_Info_Central");
        public static string CreateFileWin_Btn_Protect = CommonUtils.ApplicationFindResource("CreateFileWin_Btn_Protect");
        public static string CreateFileWin_Btn_Share = CommonUtils.ApplicationFindResource("CreateFileWin_Btn_Share");

        public static string CreateFileWin_Btn_Change = CommonUtils.ApplicationFindResource("CreateFileWin_Btn_Change");
        public static string CreateFileWin_Btn_Edit = CommonUtils.ApplicationFindResource("CreateFileWin_Btn_Edit");

        // NxlFileToCvetWindow
        public static string NxlFileToCvetWin_Header_Title = CommonUtils.ApplicationFindResource("NxlFileToCvetWin_Header_Title");
        public static string NxlFileToCvetWin_Header_Descriptio = CommonUtils.ApplicationFindResource("NxlFileToCvetWin_Header_Descriptio");
        public static string NxlFileToCvetWin_Btn_Proceed = CommonUtils.ApplicationFindResource("NxlFileToCvetWin_Btn_Proceed");
        public static string NxlFileToCvetWin_Btn_AddFile = CommonUtils.ApplicationFindResource("NxlFileToCvetWin_Btn_AddFile");
        public static string NxlFileToCvetWin_Btn_Share = CommonUtils.ApplicationFindResource("NxlFileToCvetWin_Btn_Share");
        public static string NxlFileToCvetWin_Btn_Next = CommonUtils.ApplicationFindResource("NxlFileToCvetWin_Btn_Next");
        public static string NxlFileToCvetWin_Btn_Modify = CommonUtils.ApplicationFindResource("NxlFileToCvetWin_Btn_Modify");
        public static string NxlFileToCvetWin_Header_Title_ModifyRights = CommonUtils.ApplicationFindResource("NxlFileToCvetWin_Header_Title_ModifyRights");
        public static string NxlFileToCvetWin_FileLocation_Title_Addfile = CommonUtils.ApplicationFindResource("NxlFileToCvetWin_FileLocation_Title_Addfile");

        public static string NxlFileToCvetWin_Error_Message = CommonUtils.ApplicationFindResource("NxlFileToCvetWin_Error_Message");

        // FileRightsDisplay component
        public static string FileTagRightDisplay_SysBucktTitle = CommonUtils.ApplicationFindResource("FileTagRightDisplay_SysBucktTitle");

        public static string ProtectSuccessPage_Modify_Title = CommonUtils.ApplicationFindResource("ProtectSuccessPage_Modify_Title");
        public static string ProtectSuccessPage_PathtextHas = CommonUtils.ApplicationFindResource("ProtectSuccessPage_PathtextHas");
        public static string ProtectSuccessPage_PathtextHave = CommonUtils.ApplicationFindResource("ProtectSuccessPage_PathtextHave");
        public static string ProtectSuccessPage_RightsTypeTB = CommonUtils.ApplicationFindResource("ProtectSuccessPage_RightsTypeTB");
        public static string ProtectSuccessPage_RightsDescriptionTB = CommonUtils.ApplicationFindResource("ProtectSuccessPage_RightsDescriptionTB");

        //SelectRights Component
        public static string SelectRights_View = CommonUtils.ApplicationFindResource("SelectRights_View");
        public static string SelectRights_Edit = CommonUtils.ApplicationFindResource("SelectRights_Edit");
        public static string SelectRights_Share = CommonUtils.ApplicationFindResource("SelectRights_Share");
        public static string SelectRights_Print = CommonUtils.ApplicationFindResource("SelectRights_Print");
        public static string SelectRights_SaveAs = CommonUtils.ApplicationFindResource("SelectRights_SaveAs");
        public static string SelectRights_Download = CommonUtils.ApplicationFindResource("SelectRights_Download");
        public static string SelectRights_Extract = CommonUtils.ApplicationFindResource("SelectRights_Extract");
        public static string SelectRights_Watermark = CommonUtils.ApplicationFindResource("SelectRights_Watermark");
        public static string SelectRights_Validity = CommonUtils.ApplicationFindResource("SelectRights_Validity");

        //EditWatermark component
        public static string EditWatermarkCom_Tb_PromptInfo_Text1 = CommonUtils.ApplicationFindResource("EditWatermarkCom_Tb_PromptInfo_Text1");
        public static string EditWatermarkCom_Tb_PromptInfo_Text2 = CommonUtils.ApplicationFindResource("EditWatermarkCom_Tb_PromptInfo_Text2");

        //Validity Never expire description
        public static string ValidityWin_Never_Description2 = CommonUtils.ApplicationFindResource("ValidityWin_Never_Description2");

        //ClassfifiedRights component
        public static string ClassifiedRight_No_Permission = CommonUtils.ApplicationFindResource("ClassifiedRight_No_Permission");

        ///<summary>
        /// Share Window
        ///<summary>
        public static string ShareFileWin_Notify_File_Share_Failed = CommonUtils.ApplicationFindResource("ShareFileWin_Notify_File_Share_Failed");
        public static string ShareFileWin_Notify_File_Share_Failed_No_network = CommonUtils.ApplicationFindResource("ShareFileWin_Notify_File_Share_Failed_No_network");
        public static string ShareFileWin_Notify_File_Share_Failed_Because_revoked = CommonUtils.ApplicationFindResource("ShareFileWin_Notify_File_Share_Failed_Because_revoked");
        public static string ShareFileWin_Protect_MyVault = CommonUtils.ApplicationFindResource("ShareFileWin_Protect_MyVault");
        public static string ShareFileWin_Rights_Revoked = CommonUtils.ApplicationFindResource("ShareFileWin_Rights_Revoked");
        public static string ShareFileWin_Has_SharedWith = CommonUtils.ApplicationFindResource("ShareFileWin_Has_SharedWith");

        public static string ShareFileWin_Protect_Successful = CommonUtils.ApplicationFindResource("ShareFileWin_Protect_Successful");

        public static string ProtectOrShareSuccessPage_Protect_Failed = CommonUtils.ApplicationFindResource("ProtectOrShareSuccessPage_Protect_Failed");

        //FileInforWindow
        public static string FileInfoWin_Shared_With = CommonUtils.ApplicationFindResource("FileInfoWin_Shared_With");
        public static string FileInfoWin_Shared_By = CommonUtils.ApplicationFindResource("FileInfoWin_Shared_By");
        public static string FileInfoWin_Members = CommonUtils.ApplicationFindResource("FileInfoWin_Members");
        public static string FileInfoWin_Member = CommonUtils.ApplicationFindResource("FileInfoWin_Member");

        // Logout prompt
        public static string Uploading_Logout_Prompt = CommonUtils.ApplicationFindResource("Uploading_Logout_Prompt");


        #region // For message dialog box info
        public static string Common_DlgBox_Title = CommonUtils.ApplicationFindResource("DlgBox_Title");
        public static string Common_DlgBox_Subject = CommonUtils.ApplicationFindResource("DlgBox_Subject");

        // remove file box
        public static string RemoveFile_DlgBox_Title = CommonUtils.ApplicationFindResource("DlgBox_Title");
        public static string RemoveFile_DlgBox_Subject = CommonUtils.ApplicationFindResource("RemoveFile_DlgBox_Subject");

        // update file box
        public static string UpdateFile_DlgBox_Title = CommonUtils.ApplicationFindResource("DlgBox_Title");
        public static string UpdateFile_DlgBox_Subject = CommonUtils.ApplicationFindResource("UpdateFile_DlgBox_Subject");

        // overwrite file box
        public static string OverwriteFile_DlgBox_Title = CommonUtils.ApplicationFindResource("DlgBox_Title");
        public static string OverwriteFile_DlgBox_Subject = CommonUtils.ApplicationFindResource("OverwriteFile_DlgBox_Subject");

        // enforce update box
        public static string EnforceUpdate_DlgBox_Title = CommonUtils.ApplicationFindResource("DlgBox_Title");
        public static string EnforceUpdate_DlgBox_Subject = CommonUtils.ApplicationFindResource("EnforceUpdate_DlgBox_Subject");

        // replace prompt dialog info
        public static string ReplaceFile_DlgBox_Title = CommonUtils.ApplicationFindResource("DlgBox_Title");
        public static string ReplaceFile_DlgBox_Subject = CommonUtils.ApplicationFindResource("ReplaceFile_DlgBox_Subject");

        // session expiration
        public static string SessionInvalid_DlgBox_Title = CommonUtils.ApplicationFindResource("DlgBox_Title");
        public static string SessionInvalid_DlgBox_Details = CommonUtils.ApplicationFindResource("SessionInvalid_DlgBox_Details");

        // Internet connection
        public static string CheckConnection_DlgBox_Title = CommonUtils.ApplicationFindResource("DlgBox_Title");
        public static string CheckConnection_DlgBox_Subject = CommonUtils.ApplicationFindResource("CheckConnection_DlgBox_Subject");
        public static string CheckConnection_DlgBox_Details = CommonUtils.ApplicationFindResource("CheckConnection_DlgBox_Details");

        // Edit watermark 
        public static string EditWatermark_DlgBox_Title = CommonUtils.ApplicationFindResource("DlgBox_Title");
        public static string EditWatermark_DlgBox_Subject = CommonUtils.ApplicationFindResource("DlgBox_Subject");
        public static string EditWatermark_DlgBox_Details = CommonUtils.ApplicationFindResource("EditWatermark_DlgBox_Details");

        //share email
        public static string Share_Email_Title = CommonUtils.ApplicationFindResource("DlgBox_Title");
        public static string Share_Email_Subject = CommonUtils.ApplicationFindResource("DlgBox_Subject");
        public static string Share_Email_Details = CommonUtils.ApplicationFindResource("Share_Email_Details");

        //expiration time
        public static string Validity_DlgBox_Title = CommonUtils.ApplicationFindResource("DlgBox_Title");
        public static string Validity_DlgBox_Subject = CommonUtils.ApplicationFindResource("DlgBox_Subject");
        public static string Validity_DlgBox_Details = CommonUtils.ApplicationFindResource("Validity_DlgBox_Details");
        public static string Validity_ShareFile_Expired = CommonUtils.ApplicationFindResource("Validity_ShareFile_Expired");

        #endregion // For message box info


        public static string CommandParse_Path_Empty = CommonUtils.ApplicationFindResource("Hint_CommandParse_Path_Empty");
        public static string CommandParse_File_Not_Found = CommonUtils.ApplicationFindResource("Hint_CommandParse_File_Not_Found");
        public static string CommandParse_File_Size_Zero = CommonUtils.ApplicationFindResource("Hint_CommandParse_File_Size_Zero");


    }
}
