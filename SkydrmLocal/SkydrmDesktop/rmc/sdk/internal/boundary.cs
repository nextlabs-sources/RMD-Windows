using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.sdk
{
    // Impl all stubs that cross c# and c++ boundaris
    public class Boundary
    {
        #region LocalFiles
        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_File_GetListNumber")]
        public static extern uint SDWL_File_GetListNumber(IntPtr hLocalFiles, out int size);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_File_GetList")]
        public static extern uint SDWL_File_GetFiles(IntPtr hLocalFile, out IntPtr array, out int arraySize);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_File_GetFile")]
        public static extern uint SDWL_File_GetFile(IntPtr hLocalFiles, int index, out IntPtr hNxlFile);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_File_GetFile2")]
        public static extern uint SDWL_File_GetFile2(IntPtr hLocalFiles, string name, out IntPtr hNxlFile);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_File_RemoveFile")]
        public static extern uint SDWL_File_RemoveFile(IntPtr hLocalFiles, IntPtr hNxlFile, out bool result);
        #endregion

        #region NxlFile
        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_NXL_File_GetFileName")]
        public static extern uint SDWL_NXL_File_GetFileName(IntPtr hNxlFile, out string name);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_NXL_File_GetTags")]
        public static extern uint SDWL_NXL_File_GetTags(IntPtr hNxlFile, out string tags);


        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_NXL_File_GetFileSize")]
        public static extern uint SDWL_NXL_File_GetFileSize(IntPtr hNxlFile, out Int64 size);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_NXL_File_IsValidNxl")]
        public static extern uint SDWL_NXL_File_IsValidNxl(IntPtr hNxlFile, out bool result);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_NXL_File_GetRights")]
        public static extern uint SDWL_NXL_File_GetRights(IntPtr hNxlFile, out IntPtr pArray, out int pSize);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_NXL_File_GetWaterMark")]
        public static extern uint SDWL_NXL_File_GetWaterMark(IntPtr hNxlFile, out WaterMarkInfo pWaterMark);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_NXL_File_GetExpiration")]
        public static extern uint SDWL_NXL_File_GetExpiration(IntPtr hNxlFile, out Expiration pWaterMark);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, EntryPoint = "SDWL_NXL_File_CheckRights")]
        public static extern uint SDWL_NXL_File_CheckRights(IntPtr hNxlFile, int right, out bool result);

        //[DllImport(Config.DLL_NAME, 
        //    CallingConvention = CallingConvention.Cdecl, 
        //    CharSet = CharSet.Ansi, 
        //    EntryPoint = "SDWL_NXL_File_GetClassificationSetting")]
        //private static extern uint SDWL_NXL_File_GetClassificationSetting(IntPtr hNxlFile, out string result);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_NXL_File_IsUploadToRMS")]
        public static extern uint SDWL_NXL_File_IsUploadToRMS(IntPtr hNxlFile, out bool result);


        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_NXL_File_GetActivityInfo")]
        public static extern uint SDWL_NXL_File_GetActivityInfo(
            [MarshalAs(UnmanagedType.LPWStr)]string fileName, out IntPtr pArray, out int pSize);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_NXL_File_GetAdhocWatermarkString")]
        public static extern uint SDWL_NXL_File_GetAdhocWatermarkString(IntPtr hNxlFile, out string watermark);

        #endregion

        #region User -- User base, project, workspace, myVault, sharedWithMe and so on

        #region User Base
        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_User_GetUserName")]
        public static extern uint SDWL_User_GetUserName(IntPtr hUser, out string name);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_User_GetUserEmail")]
        public static extern uint SDWL_User_GetUserEmail(IntPtr hUser, out string email);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_User_GetUserType")]
        public static extern uint SDWL_User_GetUserType(IntPtr hUser, ref int type);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_User_GetPasscode")]
        public static extern uint SDWL_User_GetPasscode(IntPtr hUser, out string code);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
         CharSet = CharSet.Unicode, EntryPoint = "SDWL_User_GetUserId")]
        public static extern uint SDWL_User_GetUserId(IntPtr hUser, out uint userId);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_User_UpdateUserInfo")]
        public static extern uint SDWL_User_UpdateUserInfo(IntPtr hUser);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_User_UpdateMyDriveInfo")]
        public static extern uint SDWL_User_UpdateMyDriveInfo(IntPtr hUser);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_User_LogoutUser")]
        public static extern uint SDWL_User_LogoutUser(IntPtr hUser);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_User_GetMyDriveInfo")]
        public static extern uint SDWL_User_GetMyDriveInfo(IntPtr hUser, ref Int64 usage, ref Int64 total,
            ref Int64 vaultUsage, ref Int64 vaultQuota);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_User_GetLocalFile")]
        public static extern uint SDWL_User_GetLocalFile(IntPtr hUser, out IntPtr hLocalFile);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_User_RemoveLocalFile")]
        public static extern uint SDWL_User_RemoveLocalFile(
            IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)] string nxlFilePath,
            out bool result);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SDWL_User_ProtectFile")]
        public static extern uint SDWL_User_ProtectFile(IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)] string path,
            int[] rights,
            int lenRights,
            WaterMarkInfo waterMark,
            Expiration expiration,
            [MarshalAs(UnmanagedType.LPWStr)]string tags,
            [MarshalAs(UnmanagedType.LPWStr)] out string outPath
            );

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SDWL_User_UpdateRecipients")]
        public static extern uint SDWL_User_UpdateRecipients(IntPtr hUser,
                                    IntPtr hNxlFile,
                                   [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)] string[] adds,
                                   int lenAdd,
                                   [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)]string[] dels,
                                   int lenDels,
                                   [MarshalAs(UnmanagedType.LPWStr)] string comments = ""
                                   );

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SDWL_User_UpdateRecipients2")]
        public static extern uint SDWL_User_UpdateRecipients(IntPtr hUser,
                                   [MarshalAs(UnmanagedType.LPWStr)] string nxlFilePath,
                                   [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)] string[] adds,
                                   int lenAdd,
                                   [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)]string[] dels,
                                   int lenDels,
                                   [MarshalAs(UnmanagedType.LPWStr)] string comments = ""
                                   );


        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SDWL_User_GetRecipients")]
        public static extern uint SDWL_User_GetRecipients(IntPtr hUser,
                                    IntPtr hNxlFile,
                                    out IntPtr array, out int arraySize,
                                    out IntPtr arrayAdd, out int arraySizeAdd,
                                    out IntPtr arrayRemove, out int arraySizeRemove
                                   );

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SDWL_User_GetRecipients2")]
        public static extern uint SDWL_User_GetRecipients(IntPtr hUser,
                                    [MarshalAs(UnmanagedType.LPWStr)] string nxlFilePath,
                                    out IntPtr array, out int arraySize,
                                    out IntPtr arrayAdd, out int arraySizeAdd,
                                    out IntPtr arrayRemove, out int arraySizeRemove
                                   );

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SDWL_User_GetRecipents3")]
        public static extern uint SDWL_User_GetRecipents(IntPtr hUser,
                            [MarshalAs(UnmanagedType.LPWStr)] string nxlFilePath,
                            [MarshalAs(UnmanagedType.LPWStr)] out string recipents,
                            [MarshalAs(UnmanagedType.LPWStr)] out string recipentsAdd,
                            [MarshalAs(UnmanagedType.LPWStr)] out string recipentsRemove);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SDWL_User_UploadFile")]
        public static extern uint SDWL_User_UploadFile(IntPtr hUser, IntPtr hNxlFile);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_User_OpenFile")]
        public static extern uint SDWL_User_OpenFile(IntPtr hUser,
                                    string nxlPath,
                                    out IntPtr hNxlFile
                                   );

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, EntryPoint = "SDWL_User_CacheRPMFileToken")]
        public static extern uint SDWL_User_CacheRPMFileToken(IntPtr hUser,
                                    string nxlPath
                                   );

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, EntryPoint = "SDWL_User_DecryptNXLFile")]
        public static extern uint SDWL_User_DecryptNXLFile(IntPtr hUser,
                                    IntPtr hNxlFile,
                                    string outPath
                                   );

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_User_CloseFile")]
        public static extern uint SDWL_User_CloseNxlFile(
                                    IntPtr hUser,
                                    IntPtr hNxlFile
                                   );

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_User_ForceCloseFile")]
        public static extern uint SDWL_User_ForceCloseFile(
                                    IntPtr hUser,
                                    [MarshalAs(UnmanagedType.LPWStr)] string nxlFilePath
                                   );


        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_GetWaterMarkInfo")]
        public static extern uint SDWL_User_GetWaterMarkInfo(IntPtr hUser,
                                    out WaterMarkInfo pWaterMark
                                   );

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "SDWL_User_UploadActivityLogs")]
        public static extern uint SDWL_User_UploadActivityLogs(IntPtr hUser);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_GetHeartBeatInfo")]
        public static extern uint SDWL_User_GetHeartBeatInfo(IntPtr hUser);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_GetHeartBeatFrequency")]
        public static extern uint SDWL_User_GetHeartBeatFrequency(IntPtr hUser, out Int32 nHeartBeatFrequenceSeconds);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_AddNxlFileLog")]
        public static extern uint SDWL_User_AddNxlFileLog(
            IntPtr hUser,
            string filePath,
            int Oper,
            bool isAllow);
        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_EvaulateNxlFileRights")]
        public static extern uint SDWL_User_EvaulateNxlFileRights(
            IntPtr hUser,
            string filePath,
            out IntPtr pArray,
            out int pArrSize,
            bool doOwnerCheck = false);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_GetPreference")]
        public static extern uint SDWL_User_GetPreference(
            IntPtr hUser,
            out Expiration expiration,
            out string watermark
            );

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_UpdatePreference")]
        public static extern uint SDWL_User_UpdatePreference(
            IntPtr hUser,
            Expiration expiration,
            [MarshalAs(UnmanagedType.LPWStr)] string watermark
            );

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_GetNxlFileFingerPrint")]
        public static extern uint SDWL_User_GetNxlFileFingerPrint(
            IntPtr hUser,
            string nxlPath,
            out User.InternalFingerPrint fingerprint,
            bool doOwnerCheck = false);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_GetNxlFileTagsWithoutToken")]
        public static extern uint SDWL_User_GetNxlFileTagsWithoutToken(
            IntPtr hUser,
            string nxlPath,
            out string tags);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_UpdateNxlFileRights")]
        public static extern uint SDWL_User_UpdateNxlFileRights(
            IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)] string nxlPath,
            int[] rights,
            int lenRights,
            WaterMarkInfo waterMark, Expiration expiration,
            [MarshalAs(UnmanagedType.LPWStr)]string tags);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_UpdateProjectNxlFileRights")]
        public static extern uint SDWL_User_UpdateProjectNxlFileRights(
            IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string nxlFilePath,
            UInt32 projectId,
            [MarshalAs(UnmanagedType.LPWStr)]string fileName,
            [MarshalAs(UnmanagedType.LPWStr)]string parentPathId,
            int[] rights, int rightsLength,
            WaterMarkInfo waterMark, Expiration expiration,
            [MarshalAs(UnmanagedType.LPWStr)]string tags);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_ResetSourcePath")]
        public static extern uint SDWL_User_ResetSourcePath(
            IntPtr pUser,
            [MarshalAs(UnmanagedType.LPWStr)]string nxlFilePath,
            [MarshalAs(UnmanagedType.LPWStr)]string sourcePath);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_GetFileRightsFromCentralPoliciesByTenant")]
        public static extern uint SDWL_User_GetFileRightsFromCentralPolicyByTenant(
            IntPtr pUser,
            [MarshalAs(UnmanagedType.LPWStr)]string tenantName,
            [MarshalAs(UnmanagedType.LPWStr)]string tags,
            out IntPtr pArray,
            out int pArrSize);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_GetFileRightsFromCentralPolicyByProjectId")]
        public static extern uint SDWL_User_GetFileRightsFromCentralPolicyByProjectID(
            IntPtr pUser,
            UInt32 projectId,
            [MarshalAs(UnmanagedType.LPWStr)]string tags,
            out IntPtr pArray,
            out int pArrSize,
            bool doOwnerCheck = false);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_ProtectFileFrom")]
        public static extern uint SDWL_User_ProtectFileFrom(
            IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string srcplainfile,
            [MarshalAs(UnmanagedType.LPWStr)]string origianlnxlfile,
            [MarshalAs(UnmanagedType.LPWStr)] out string outPath
            );

        #endregion // User Base

        #region User Project

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_ProjectFileIsExist")]
        public static extern uint SDWL_User_ProjectFileIsExist(
            IntPtr hUser,
            int projectid,
            [MarshalAs(UnmanagedType.LPWStr)]string pathId,
            out bool bExist);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_ProjectGetNxlFileHeader")]
        public static extern uint SDWL_User_ProjectGetNxlFileHeader(
            IntPtr hUser,
            int projectId,
            [MarshalAs(UnmanagedType.LPWStr)]string pathId,
            [MarshalAs(UnmanagedType.LPWStr)]string targetFolder,
            [MarshalAs(UnmanagedType.LPWStr)] out string outPath
            );

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_ProjectFileOverwrite")]
        public static extern uint SDWL_User_ProjectFileOverwrite(
            IntPtr hUser,
            int projectId,
            [MarshalAs(UnmanagedType.LPWStr)]string parentPathid,
            [MarshalAs(UnmanagedType.LPWStr)]string nxlfilePath,
            bool bOverwrite = false
            );

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "SDWL_User_GetProjectsInfo")]
        public static extern uint SDWL_User_GetProjectsInfo(IntPtr hUser, out IntPtr pArray, out int pSize);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_GetListProjtects")]
        public static extern uint SDWL_User_GetListProjtects(IntPtr hUser, int pagedId, int pageSize,
            string orderBy, ProjectFilterType filter);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_CheckProjectEnableAdhoc")]
        public static extern uint SDWL_User_CheckProjectEnableAdhoc(IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string projectTenandId,
            ref bool isEnabled
            );

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_CheckWorkSpaceEnable")]
        public static extern uint SDWL_User_CheckWorkSpaceEnable(IntPtr hUser, ref bool isEnabled);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_CheckSystemBucketEnableAdhoc")]
        public static extern uint SDWL_User_CheckSystemBucketEnableAdhoc(IntPtr hUser, ref bool isEnabled);


        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_CheckInPlaceProtection")]
        public static extern uint SDWL_User_CheckInPlaceProtection(IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string projectTenandId,
            ref bool deleteSource
            );

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_CheckSystemProject")]
        public static extern uint SDWL_User_CheckSystemProject(IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string projectTenandId,
            ref int sysprojectid,
            [MarshalAs(UnmanagedType.LPWStr)]out string sysProjectTenandId
            );

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_ProjectUploadFileEx")]
        public static extern uint SDWL_User_ProjectUploadFileEx(
            IntPtr hUser,
            int projectId,
            [MarshalAs(UnmanagedType.LPWStr)]string rmsParentFolder,
            [MarshalAs(UnmanagedType.LPWStr)] string nxlFilePath,
            int uploadType,
            bool overwrite = false);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "SDWL_User_ProtectFileToProject")]
        public static extern uint SDWL_User_ProtectFileToProject(
            int projectId,
            IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)] string path,
            int[] rights,
            int lenRights,
            WaterMarkInfo waterMark,
            Expiration expiration,
            [MarshalAs(UnmanagedType.LPWStr)]string tags,
            [MarshalAs(UnmanagedType.LPWStr)] out string outPath
            );

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_ProjectListAllFiles")]
        public static extern uint SDWL_User_ProjectListAllFiles(IntPtr hUser,
            int projectId, string orderby,
            [MarshalAs(UnmanagedType.LPWStr)]string pathId,
            [MarshalAs(UnmanagedType.LPWStr)]string searchStr,
            out IntPtr pArray, out int pSize);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_ProjectListFiles")]
        public static extern uint SDWL_User_ProjectListFiles(IntPtr hUser,
            int projectId, int pagedId,
            int pagedSize, string orderby,
            [MarshalAs(UnmanagedType.LPWStr)]string pathId,
            [MarshalAs(UnmanagedType.LPWStr)]string searchStr,
            out IntPtr pArray, out int pSize);

        [DllImport(Config.DLL_NAME,
            CharSet = CharSet.Unicode,
            CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "SDWL_User_ProjectClassifacation")]
        public static extern uint SDWL_User_ProjectClassifacation(IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string tenantid,
            out IntPtr pArray,
            out int pSize);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_ProjectDownloadFile")]
        public static extern uint SDWL_User_ProjectDownloadFile(IntPtr hUser,
            int projectId,
            [MarshalAs(UnmanagedType.LPWStr)]string pathId,
            [MarshalAs(UnmanagedType.LPWStr)]string downloadPath,
            int type,
            [MarshalAs(UnmanagedType.LPWStr)] out string outPath
            );

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_ProjectDownloadPartialFile")]
        public static extern uint SDWL_User_ProjectDownloadPartialFile(IntPtr hUser,
            int projectId,
            [MarshalAs(UnmanagedType.LPWStr)]string pathId,
            [MarshalAs(UnmanagedType.LPWStr)]string downloadPath,
            int type,
            [MarshalAs(UnmanagedType.LPWStr)] out string outPath
            );
        #endregion // User project

        #region Sharing transaction for Project

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_ProjectListFile")]
        public static extern uint SDWL_User_ProjectListFile(IntPtr hUser,
            uint projectId, uint pageId, uint pageSize,
            [MarshalAs(UnmanagedType.LPWStr)]string orderBy,
            [MarshalAs(UnmanagedType.LPWStr)]string pathId,
            [MarshalAs(UnmanagedType.LPWStr)]string searchStr,
            FilterType type,
            out IntPtr pArr,
            out uint pSize);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_ProjectListTotalFile")]
        public static extern uint SDWL_User_ProjectListTotalFile(IntPtr hUser,
            uint projectId, 
            [MarshalAs(UnmanagedType.LPWStr)]string orderBy,
            [MarshalAs(UnmanagedType.LPWStr)]string pathId,
            [MarshalAs(UnmanagedType.LPWStr)]string searchStr,
            FilterType type,
            out IntPtr pArr,
            out uint pSize);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_ProjectListSharedWithMeFiles")]
        public static extern uint SDWL_User_ProjectListSharedWithMeFiles(IntPtr hUser,
            uint projectId, uint pageId, uint pageSize,
            [MarshalAs(UnmanagedType.LPWStr)]string orderBy,
            [MarshalAs(UnmanagedType.LPWStr)]string searchStr,
            out IntPtr pListFiles,
            out uint pSize);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_ProjectListTotalSharedWithMeFiles")]
        public static extern uint SDWL_User_ProjectListTotalSharedWithMeFiles(IntPtr hUser,
            uint projectId,
            [MarshalAs(UnmanagedType.LPWStr)]string orderBy,
            [MarshalAs(UnmanagedType.LPWStr)]string searchStr,
            out IntPtr pListFiles,
            out uint pSize);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_ProjectDownloadSharedWithMeFile")]
        public static extern uint SDWL_User_ProjectDownloadSharedWithMeFile(IntPtr hUser,
            uint projectId,
            [MarshalAs(UnmanagedType.LPWStr)]string transactionCode,
            [MarshalAs(UnmanagedType.LPWStr)]string transactionId,
            [MarshalAs(UnmanagedType.LPWStr)]string destPath,
            bool forViewer,
            [MarshalAs(UnmanagedType.LPWStr)] out string outPath);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_ProjectPartialDownloadSharedWithMeFile")]
        public static extern uint SDWL_User_ProjectPartialDownloadSharedWithMeFile(IntPtr hUser,
            uint projectId,
            [MarshalAs(UnmanagedType.LPWStr)]string transactionCode,
            [MarshalAs(UnmanagedType.LPWStr)]string transactionId,
            [MarshalAs(UnmanagedType.LPWStr)]string destPath,
            bool forViewer,
            [MarshalAs(UnmanagedType.LPWStr)] out string outPath);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_ProjectReshareSharedWithMeFile")]
        public static extern uint SDWL_User_ProjectReshareSharedWithMeFile(IntPtr hUser,
            uint prjectId,
            [MarshalAs(UnmanagedType.LPWStr)]string transactionId,
            [MarshalAs(UnmanagedType.LPWStr)]string transactionCode,
            [MarshalAs(UnmanagedType.LPWStr)]string emailList,  // mandatory for myVault only, optional otherwise
            uint[]  recipients,
            uint len,
            out User.InternalProjectReshareFileResult result);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_ProjectUpdateSharedFileRecipients")]
        public static extern uint SDWL_User_ProjectUpdateSharedFileRecipients(IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string duid,
            uint[] addRecipients, uint addLen,
            uint[] removedRecipients, uint removedLen,
            [MarshalAs(UnmanagedType.LPWStr)]string comment,
            out IntPtr pAddList, out uint addListLen,
            out IntPtr pRemovedList, out uint removedListLen);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_ProjectShareFile")]
        public static extern uint SDWL_User_ProjectShareFile(IntPtr hUser,
            uint proId,
            uint[] recipients, uint len,
            [MarshalAs(UnmanagedType.LPWStr)]string name,
            [MarshalAs(UnmanagedType.LPWStr)]string filePathId,
            [MarshalAs(UnmanagedType.LPWStr)]string filePath,
            [MarshalAs(UnmanagedType.LPWStr)]string comment,
            out User.InternalProjectShareFileResult result);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_ProjectRevokeShareFile")]
        public static extern uint SDWL_User_ProjectRevokeShareFile(IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string duid);

        #endregion // Sharing transaction for Project

        #region User MyVault

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_MyVaultFileIsExist")]
        public static extern uint SDWL_User_MyVaultFileIsExist(
            IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string pathId,
            out bool bExist);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_MyVaultGetNxlFileHeader")]
        public static extern uint SDWL_User_MyVaultGetNxlFileHeader(
            IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string pathId,
            [MarshalAs(UnmanagedType.LPWStr)]string targetFolder,
            [MarshalAs(UnmanagedType.LPWStr)] out string outPath
            );


        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_ListMyVaultAllFiles")]
        public static extern uint SDWL_User_ListMyVaultAllFiles(IntPtr hUser,
                string orderBy, string searchString,
                out IntPtr pArray, out int psize);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_ListMyVaultFiles")]
        public static extern uint SDWL_User_ListMyVaultFiles(IntPtr hUser,
                UInt32 pageId, UInt32 pageSize,
                string orderBy, string searchString,
                out IntPtr pArray, out int psize);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_DownloadMyVaultFiles")]
        public static extern uint SDWL_User_DownloadMyVaultFiles(
            IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string rmsFilePathId,
            [MarshalAs(UnmanagedType.LPWStr)]string downloadPath,
            int type,
            [MarshalAs(UnmanagedType.LPWStr)] out string outPath
            );

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_DownloadMyVaultPartialFiles")]
        public static extern uint SDWL_User_DownloadMyVaultPartialFiles(
            IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string rmsFilePathId,
            [MarshalAs(UnmanagedType.LPWStr)]string downloadPath,
            int type,
            [MarshalAs(UnmanagedType.LPWStr)] out string outPath
            );

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            EntryPoint = "SDWL_User_UploadMyVaultFile")]
        public static extern uint SDWL_User_UploadMyVaultFile(IntPtr hUser,
                            [MarshalAs(UnmanagedType.LPWStr)] string nxlFilePath,
                            [MarshalAs(UnmanagedType.LPWStr)] string sourcePath,
                            [MarshalAs(UnmanagedType.LPWStr)] string recipents = "",
                            [MarshalAs(UnmanagedType.LPWStr)] string comments = "",
                            bool overwrite = false);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_GetMyVaultFileMetaData")]
        public static extern uint SDWL_User_GetMyVaultFileMetaData(
            IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)] string nxlPath,
            [MarshalAs(UnmanagedType.LPWStr)] string pathId,
            out User.InternalMyVaultMetaData metaData);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_MyVaultShareFile")]
        public static extern uint SDWL_User_MyVaultShareFile(
            IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string nxlLocalPath,
            [MarshalAs(UnmanagedType.LPWStr)]string recipents,
            [MarshalAs(UnmanagedType.LPWStr)]string repositoryId,
            [MarshalAs(UnmanagedType.LPWStr)]string fileName,
            [MarshalAs(UnmanagedType.LPWStr)]string filePathId,
            [MarshalAs(UnmanagedType.LPWStr)]string filePath,
            [MarshalAs(UnmanagedType.LPWStr)]string comments);
        #endregion // User MyVault

        #region User MyDrive
        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_MyDriveListFiles")]
        public static extern uint SDWL_User_MyDriveListFiles(IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string pathId,
                out IntPtr pArr,
                out uint pLen);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_MyDriveDownloadFile")]
        public static extern uint SDWL_User_MyDriveDownloadFile(IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string pathId,
            [MarshalAs(UnmanagedType.LPWStr)]string targetFolder,
            [MarshalAs(UnmanagedType.LPWStr)] out string outPath);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_MyDriveUploadFile")]
        public static extern uint SDWL_User_MyDriveUploadFile(IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string pathId,
            [MarshalAs(UnmanagedType.LPWStr)]string destFolder,
            bool overwrite = false);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_MyDriveCreateFolder")]
        public static extern uint SDWL_User_MyDriveCreateFolder(IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string name,
            [MarshalAs(UnmanagedType.LPWStr)]string parentFolder);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_MyDriveDeleteItem")]
        public static extern uint SDWL_User_MyDriveDeleteItem(IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string pathId);

        #endregion // User MyDrive

        #region User WorkSpace

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_WorkSpaceFileIsExist")]
        public static extern uint SDWL_User_WorkSpaceFileIsExist(
            IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string pathId,
            out bool bExist);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_WorkSpaceGetNxlFileHeader")]
        public static extern uint SDWL_User_WorkSpaceGetNxlFileHeader(
            IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string pathId,
            [MarshalAs(UnmanagedType.LPWStr)]string targetFolder,
            [MarshalAs(UnmanagedType.LPWStr)] out string outPath
            );

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_WorkSpaceFileOverwrite")]
        public static extern uint SDWL_User_WorkSpaceFileOverwrite(
            IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string parentPathid,
            [MarshalAs(UnmanagedType.LPWStr)]string nxlfilePath,
            bool bOverwrite = false
            );


        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_DownloadWorkSpaceFile")]
        public static extern uint SDWL_User_DownloadWorkSpaceFile(
            IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string rmsFilePathId,
            [MarshalAs(UnmanagedType.LPWStr)]string downloadPath,
            int type,
            [MarshalAs(UnmanagedType.LPWStr)] out string outPath
            );

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_DownloadWorkSpacePartialFile")]
        public static extern uint SDWL_User_DownloadWorkSpacePartialFile(
            IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string rmsFilePathId,
            [MarshalAs(UnmanagedType.LPWStr)]string downloadPath,
            int type,
            [MarshalAs(UnmanagedType.LPWStr)] out string outPath
            );

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_ListWorkSpaceAllFiles")]
        public static extern uint SDWL_User_ListWorkSpaceAllFiles(
            IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string pathId,
            [MarshalAs(UnmanagedType.LPWStr)]string orderBy,
            [MarshalAs(UnmanagedType.LPWStr)]string searchString,
            out IntPtr pArray,
            out int pSize
            );

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_UpdateWorkSpaceNxlFileRights")]
        public static extern uint SDWL_User_UpdateWorkSpaceNxlFileRights(
            IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)] string originalNxlPath,
            [MarshalAs(UnmanagedType.LPWStr)] string name,
            [MarshalAs(UnmanagedType.LPWStr)] string parentPathId,
            int[] rights,
            int lenRights,
            WaterMarkInfo waterMark, Expiration expiration,
            [MarshalAs(UnmanagedType.LPWStr)]string tags);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_ClassifyWorkSpaceFile")]
        public static extern uint SDWL_User_ClassifyWorkSpaceFile(
            IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string nxlFilepath,
            [MarshalAs(UnmanagedType.LPWStr)]string filename,
            [MarshalAs(UnmanagedType.LPWStr)]string parentPathId,
            [MarshalAs(UnmanagedType.LPWStr)]string tags);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_UploadWorkSpaceFile")]
        public static extern uint SDWL_User_UploadWorkSpaceFile(
            IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string destFolser,
            [MarshalAs(UnmanagedType.LPWStr)]string nxlFilePath,
            bool overwrite);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_GetWorkSpaceFileMetadata")]
        public static extern uint SDWL_User_GetWorkSpaceFileMetadata(
            IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)] string pathId,
            out User.InternalWorkSpaceMetaData metaData);
        #endregion // User workspace

        #region User Shared workspace

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_ListSharedWorkspaceAllFiles")]
        public static extern uint SDWL_User_ListSharedWorkspaceAllFiles(
            IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string repoId,
            [MarshalAs(UnmanagedType.LPWStr)]string orderBy,
            [MarshalAs(UnmanagedType.LPWStr)]string searchString,
            [MarshalAs(UnmanagedType.LPWStr)]string path,
            out IntPtr pArray,
            out int pSize
            );

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_UploadSharedWorkspaceFile")]
        public static extern uint SDWL_User_UploadSharedWorkspaceFile(
            IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string repoId,
            [MarshalAs(UnmanagedType.LPWStr)]string destFolser,
            [MarshalAs(UnmanagedType.LPWStr)]string filePath,
            int uploadType,
            bool overwrite);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_DownloadSharedWorkspaceFile")]
        public static extern uint SDWL_User_DownloadSharedWorkspaceFile(
            IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string repoId,
            [MarshalAs(UnmanagedType.LPWStr)]string path,
            [MarshalAs(UnmanagedType.LPWStr)]string targetFolder,
            int type,
            bool isNxl,
            [MarshalAs(UnmanagedType.LPWStr)] out string outPath
            );

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_IsSharedWorkspaceFileExist")]
        public static extern uint SDWL_User_IsSharedWorkspaceFileExist(
            IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string repoId,
            [MarshalAs(UnmanagedType.LPWStr)]string path,
            out bool bExist);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_GetSharedWorkspaceNxlFileHeader")]
        public static extern uint SDWL_User_GetSharedWorkspaceNxlFileHeader(
            IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string repoId,
            [MarshalAs(UnmanagedType.LPWStr)]string path,
            [MarshalAs(UnmanagedType.LPWStr)]string targetFolder,
            [MarshalAs(UnmanagedType.LPWStr)] out string outPath
            );

        #endregion // User Shared workspace

        #region User SharedWithMe
        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_ListSharedWithMeAllFiles")]
        public static extern uint SDWL_User_ListSharedWithMeAllFiles(
            IntPtr hFile,
            [MarshalAs(UnmanagedType.LPWStr)]string orderBy,
            [MarshalAs(UnmanagedType.LPWStr)]string searchString,
            out IntPtr pArray,
            out int pSize
            );

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_ListSharedWithMeFiles")]
        public static extern uint SDWL_User_ListSharedWithMeFiles(
            IntPtr hFile,
            int pageId, int pageSize,
            [MarshalAs(UnmanagedType.LPWStr)]string orderBy,
            [MarshalAs(UnmanagedType.LPWStr)]string searchString,
            out IntPtr pArray,
            out int pSize
            );

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_DownloadSharedWithMeFiles")]
        public static extern uint SDWL_User_DownloadSharedWithMeFiles(
            IntPtr hUser,
            string transactionId,
            string transactionCode,
            string downlaodDestLocalFolder,
            bool forViewer,
            [MarshalAs(UnmanagedType.LPWStr)] out string outPath
            );

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_DownloadSharedWithMePartialFiles")]
        public static extern uint SDWL_User_DownloadSharedWithMePartialFiles(
            IntPtr hUser,
            string transactionId,
            string transactionCode,
            string downlaodDestLocalFolder,
            bool forViewer,
            [MarshalAs(UnmanagedType.LPWStr)] out string outPath
            );

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_SharedWithMeReshareFile")]
        public static extern uint SDWL_User_SharedWitheMeReshareFile(
            IntPtr pUser,
            [MarshalAs(UnmanagedType.LPWStr)] string transactionId,
            [MarshalAs(UnmanagedType.LPWStr)] string transactionCode,
            [MarshalAs(UnmanagedType.LPWStr)] string emails);
        #endregion // User SharedWithMe

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_CopyNxlFile")]
        public static extern uint SDWL_User_CopyNxlFile(
            IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string fileName,
            [MarshalAs(UnmanagedType.LPWStr)]string filePath,
            NxlFileSpaceType spaceType,
            [MarshalAs(UnmanagedType.LPWStr)]string spaceId,
            [MarshalAs(UnmanagedType.LPWStr)]string destFileName,
            [MarshalAs(UnmanagedType.LPWStr)]string destFolderPath,
            NxlFileSpaceType destSpaceType,
            [MarshalAs(UnmanagedType.LPWStr)]string destSpaceId,
            bool overwrite,
            [MarshalAs(UnmanagedType.LPWStr)]string transactionCode,
            [MarshalAs(UnmanagedType.LPWStr)]string transactionId);

        #endregion

        #region Repository
        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_GetRepositories")]
        public static extern uint SDWL_User_GetRepositories(IntPtr hUser,
                out IntPtr pArr,
                out uint pLen);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_GetRepositoryAccessToken")]
        public static extern uint SDWL_User_GetRepositoryAccessToken(IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string repoId,
            [MarshalAs(UnmanagedType.LPWStr)] out string accessToken);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_GetRepositoryAuthorizationUrl")]
        public static extern uint SDWL_User_GetRepositoryAuthorizationUrl(IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string repoType,
            [MarshalAs(UnmanagedType.LPWStr)]string name,
            [MarshalAs(UnmanagedType.LPWStr)] out string authUrl);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_UpdateRepository")]
        public static extern uint SDWL_User_UpdateRepository(IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string repoid,
            [MarshalAs(UnmanagedType.LPWStr)]string token,
            [MarshalAs(UnmanagedType.LPWStr)]string name);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_User_RemoveRepository")]
        public static extern uint SDWL_User_RemoveRepository(IntPtr hUser,
            [MarshalAs(UnmanagedType.LPWStr)]string repoid);

        #endregion // Repository

        #region Tenant
        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_Tenant_GetTenant")]
        public static extern uint SDK_SDWL_GetTenant(
            IntPtr hTenant, out string Tenant);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_Tenant_GetRouterURL")]
        public static extern uint SDK_SDWL_GetRouterURL(
            IntPtr hTenant, out string RouterURL);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_Tenant_GetRMSURL")]
        public static extern uint SDK_SDWL_GetRMSURL(
            IntPtr hTenant, out string RMSURL);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_Tenant_ReleaseTenant")]
        public static extern uint SDK_SDWL_ReleaseTenant(
            IntPtr hTenant);





        #endregion

        #region API

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SdkLibInit")]
        public static extern void SdkLibInit();

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SdkLibCleanup")]
        public static extern void SdkLibCleanup();

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "GetSDKVersion")]
        public static extern uint GetSDKVersion();

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "CreateSDKSession")]
        public static extern uint CreateSDKSession(string TempPath, out IntPtr SessionHandle);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "GetCurrentLoggedInUser")]
        public static extern uint GetCurrentLoggedInUser(out IntPtr SessionHandle, out IntPtr UserHandle);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
        CharSet = CharSet.Unicode, EntryPoint = "WaitInstanceInitFinish")]
        public static extern uint WaitInstanceInitFinish();
        
        #endregion

        #region Session
        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "DeleteSDKSession")]
        public static extern uint DeleteSDKSession(IntPtr SessionHandle);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_Session_Initialize")]
        public static extern uint SDK_Initialize(IntPtr hSession, string Router, string Tenant);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_Session_Initialize2")]
        public static extern uint SDK_Initialize(IntPtr hSession, string WorkingFolder, string Router, string Tenant);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_Session_SaveSession")]
        public static extern uint SDK_SaveSession(IntPtr hSession, string folder);

        //NXSDK_API DWORD GetCurrentTenant(HANDLE hSession, HANDLE* hTenant);
        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_Session_GetCurrentTenant")]
        public static extern uint SDK_GetCurrentTenant(IntPtr hSession, out IntPtr hTenant);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_Session_SetLoginRequest")]
        public static extern uint SDK_SetLoginRequest(IntPtr hSession, string loginstr, string security, out IntPtr hUser);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_Session_GetLoginParams")]
        public static extern uint SDWL_Session_GetLoginParams(IntPtr hSession, out string loginUrl, out IntPtr Cookies, out int size);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_Session_GetLoginUser")]
        public static extern uint SDWL_Session_GetLoginUser(IntPtr hSession,
            string UserEmail, string PassCode, out IntPtr hUser);

        #endregion

        #region RMP

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_RPM_GetFileRights")]
        public static extern uint SDWL_RPM_GetFileRights(
            IntPtr hSession,
            string filePath,
            out IntPtr pArray,
            out int pSize,
            out WaterMarkInfo pWaterMark, int option = 1);

        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_RPM_GetFileInfo")]
        public static extern uint SDWL_RPM_GetFileInfo(
            IntPtr hSession,
            string filePath,
            out string duid,
            out IntPtr pArray, /* userRightsAndWatermark */
            out int pSize,
            out IntPtr pArrRights,
            out int pArrRightsSize, /* rights */
            out WaterMarkInfo pWaterMark,
            out Expiration pExpiration,
            out string tags,
            out string tokenGroup,
            out string creatorId,
            out string infoExt,
            out long attributes,
            out long isRPMFolder,
            out long isNxlFile);

        // add RMP features
        [DllImport(Config.DLL_NAME,
            CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode,
            EntryPoint = "SDWL_RPM_GetRPMDir")]
        public static extern uint SDWL_RPM_GetRPMDir(
            IntPtr hSession,
            out IntPtr pArray,
            out int pSize,
            int option = 1);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_RPM_ReadFileTags")]
        public static extern uint SDWL_RPM_ReadFileTags(IntPtr hSession,
            [MarshalAs(UnmanagedType.LPWStr)] string filePath,
            [MarshalAs(UnmanagedType.LPWStr)] out string rpmPath);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_RPM_IsRPMDriverExist")]
        public static extern uint SDWL_RPM_IsRPMDriverExist(IntPtr hSession, out bool IsExist);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_RPM_AddRPMDir")]
        public static extern uint SDWL_RPM_AddRPMDir(
            IntPtr hSession,
            [MarshalAs(UnmanagedType.LPWStr)] string path, int option, [MarshalAs(UnmanagedType.LPWStr)] string filetags);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_RPM_RemoveRPMDir")]
        public static extern uint SDWL_RPM_RemoveRPMDir(
            IntPtr hSession,
            [MarshalAs(UnmanagedType.LPWStr)] string path, bool bForce,
            [MarshalAs(UnmanagedType.LPWStr)] out string errorMsg);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_RPM_EditFile")]
        public static extern uint SDWL_RPM_EditFile(
            IntPtr hSession,
            [MarshalAs(UnmanagedType.LPWStr)] string path,
            out string outPath);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_RPM_DeleteFile")]
        public static extern uint SDWL_RPM_DeleteFile(
            IntPtr hSession,
            [MarshalAs(UnmanagedType.LPWStr)] string path);


        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_RPMDeleteFolder")]
        public static extern uint SDWL_RPMDeleteFolder(
        IntPtr hSession,
        [MarshalAs(UnmanagedType.LPWStr)] string folderPath);


        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_RPM_RegisterApp")]
        public static extern uint SDWL_RPM_RegisterApp(
            IntPtr hSession,
            [MarshalAs(UnmanagedType.LPWStr)] string path);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_RPM_NotifyRMXStatus")]
        public static extern uint SDWL_RPM_NotifyRMXStatus(
            IntPtr hSession,
            bool running);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_RPM_UnregisterApp")]
        public static extern uint SDWL_RPM_UnregisterApp(
            IntPtr hSession,
            [MarshalAs(UnmanagedType.LPWStr)] string path);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_RPM_AddTrustedApp")]
        public static extern uint SDWL_RPM_AddTrustedApp(
            IntPtr hSession,
            [MarshalAs(UnmanagedType.LPWStr)] string appPath);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_RPM_AddTrustedProcess")]
        public static extern uint SDWL_RPM_AddTrustedProcess(
            IntPtr hSession,
            int pid);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_RPM_RemoveTrustedProcess")]
        public static extern uint SDWL_RPM_RemoveTrustedProcess(
            IntPtr hSession,
            int pid);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_RPM_IsSafeFolder")]
        public static extern uint SDWL_RPM_IsSafeFolder(
            IntPtr hSession,
            [MarshalAs(UnmanagedType.LPWStr)] string path,
            ref bool result,
            out int option,
            [MarshalAs(UnmanagedType.LPWStr)] out string tags);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
        CharSet = CharSet.Unicode, EntryPoint = "SDWL_RPM_GetFileStatus")]
        public static extern uint SDWL_RPM_GetFileStatus(
            IntPtr hSession,
            [MarshalAs(UnmanagedType.LPWStr)] string path,
            out int dirstatus,
            out bool filestatus);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
        CharSet = CharSet.Unicode, EntryPoint = "SDWL_RPM_RequestLogin")]
        public static extern uint SDWL_RPM_RequestLogin(
            IntPtr hSession,
            [MarshalAs(UnmanagedType.LPWStr)] string callbackCmd,
            [MarshalAs(UnmanagedType.LPWStr)] string callbackCmdPara = "");

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
        CharSet = CharSet.Unicode, EntryPoint = "SDWL_RPM_RequestLogout")]
        public static extern uint SDWL_RPM_RequestLogout(
            IntPtr hSession,
            out bool isAllow,
            uint option = 0);

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
        CharSet = CharSet.Unicode, EntryPoint = "SDWL_RPM_NotifyMessage")]
        public static extern uint SDWL_RPM_NotifyMessage(
            IntPtr hSession,
            [MarshalAs(UnmanagedType.LPWStr)] string app,
            [MarshalAs(UnmanagedType.LPWStr)] string target,
            [MarshalAs(UnmanagedType.LPWStr)] string message,
            uint msgtype = 0,
            [MarshalAs(UnmanagedType.LPWStr)] string operation = "",
            uint result = 0,
            uint fileStatus = 0
            );

        #endregion 

        // callback used by SDWL_SYSHELPER_RegKeyChangeMonitorSynced,
        // when c++ world deteack {regValueBeDeleted} had been deleted, notify c#
        public delegate void RegChangedCallback(
            [MarshalAs(UnmanagedType.LPWStr)]string regValueBeDeleted);

        #region SysHelper
        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "SDWL_SYSHELPER_MonitorRegValueDeleted")]
        public static extern uint SDWL_SYSHELPER_MonitorRegValueDeleted(
            [MarshalAs(UnmanagedType.LPWStr)] string rmpPath,
            RegChangedCallback callback);
        #endregion

        #region Offlice plugin

        //bool IsPluginWell(const wchar_t* wszAppType, const wchar_t* wszPlatform)

        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, EntryPoint = "IsPluginWell")]
        public static extern bool IsPluginWell(
            string wszAppType,
            string wszPlatform);

        #endregion

        #region RegistryServiceEntry
        [DllImport(Config.DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, EntryPoint = "SDWL_Register_SetValue")]
        public static extern bool SDWL_Register_SetValue(
            IntPtr session,
            UIntPtr root,
            [MarshalAs(UnmanagedType.LPWStr)] string strKey,
            [MarshalAs(UnmanagedType.LPWStr)] string strItemName,
            UInt32 u32ItemValue);
        #endregion

        #region Hoops Exchange Api
        [DllImport("hoopswrapper.dll", 
            CallingConvention = CallingConvention.Cdecl, 
            CharSet = CharSet.Unicode, 
            EntryPoint = "HOOPS_EXCHANGE_GetAssemblyPathsFromModelFile")]
        public static extern int HOOPS_EXCHANGE_GetAssemblyPathsFromModelFile(
            [MarshalAs(UnmanagedType.LPWStr)] string filepath,
            [MarshalAs(UnmanagedType.LPWStr)] out string paths,
            [MarshalAs(UnmanagedType.LPWStr)] out string missingpaths,
            out UInt32 pmissingCounts);
        #endregion
    }
}
