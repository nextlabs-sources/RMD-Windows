using SkydrmDesktop.rmc.database.table.externalrepo;
using SkydrmDesktop.rmc.database.table.externalrepo.Box;
using SkydrmDesktop.rmc.database.table.externalrepo.dropbox;
using SkydrmDesktop.rmc.database.table.externalrepo.googledrive;
using SkydrmDesktop.rmc.database.table.externalrepo.oneDrive;
using SkydrmDesktop.rmc.database.table.externalrepo.sharepoint;
using SkydrmDesktop.rmc.database.table.myspace;
using SkydrmDesktop.rmc.database.table.project;
using SkydrmDesktop.rmc.database.table.sharedworkspace;
using SkydrmDesktop.rmc.database.table.workspace;
using SkydrmLocal.rmc.database.table.myvault;
using SkydrmLocal.rmc.database.table.recentTouchedFile;
using SkydrmLocal.rmc.database.table.sharedwithme;
using SkydrmLocal.rmc.database.table.systembucket;
using SkydrmLocal.rmc.database2.table.myvault;
using SkydrmLocal.rmc.database2.table.operation;
using SkydrmLocal.rmc.database2.table.project;
using SkydrmLocal.rmc.database2.table.recipient;
using SkydrmLocal.rmc.database2.table.server;
using SkydrmLocal.rmc.database2.table.user;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.database2.manager
{

    class Config
    {
        public static readonly string Database_Name = "DataBase2.db";

        public static readonly string ConnectionString = "Data Source=" + Database_Name;

        public static readonly string SQL_Create_Table_Server = ServerDao.SQL_Create_Table_Server;

        public static readonly string SQL_Create_Table_User = UserDao.SQL_Create_Table_User;

        public static readonly string SQL_Create_Table_WorkSpaceFile = WorkSpaceFileDao.SQL_Create_Table_WorkspaceFile;

        public static readonly string SQL_Create_Table_WorkSpaceLocalFile = WorkSpaceLocalFileDao.SQL_Create_Table_WorkSpaceLocalFile;

        public static readonly string SQL_Create_Table_MyDriveFile = MyDriveFileDao.SQL_Create_Table_MyDriveFile;

        public static readonly string SQL_Create_Table_MyDriveLocalFile = MyDriveLocalFileDao.SQL_Create_Table_MyDriveLocalFile;

        public static readonly string SQL_Create_Table_MyVaultFile = MyVaultFileDao.SQL_Create_Table_MyVaultFile;

        public static readonly string SQL_Create_Table_MyVaultLocalFile = MyVaultLocalFileDao.SQL_Create_Table_MyVaultLocalFile;

        public static readonly string SQL_Create_Table_Recipients = RecipientDao.SQL_Create_Table_Recipients;

        public static readonly string SQL_Create_Table_NxlOperLog = NxlOperLogDao.SQL_Create_Table_NxlOperLog;

        public static readonly string SQL_Create_Table_Project = ProjectDao.SQL_Create_Table_Project;

        public static readonly string SQL_Alter_Table_Project_V1 = ProjectDao.SQL_Alter_Table_Project_V1;

        public static readonly string SQL_Create_Table_ProjectFile = ProjectFileDao.SQL_Create_Table_ProjectFile;

        public static readonly string SQL_Create_Table_ProjectLocalFile = ProjectLocalFileDao.SQL_Create_Table_ProjectLocalFile;

        public static readonly string SQL_Create_Table_SharedWithProjectFile = SharedWithProjectDao.SQL_Create_Table_SharedWithProjectFile;

        public static readonly string SQL_Create_Table_RecentTouchedFile = RecentTouchedFileDao.SQL_Create_Table_RecentTouchedFile;

        public static readonly string SQL_Create_Table_SharedWithMe = SharedWithMeFileDao.SQL_Create_Table_SharedWithMeFileDao;

        public static readonly string SQL_Create_Table_SystemBucket = SystemBucketDao.SQL_Create_Table_SystemBucket;

        #region Alter ProjectFile table(V2)
        public static readonly string SQL_Alter_Table_ProjectFile_Add_Edit_Status_V2 = ProjectFileDao.SQL_Alter_Table_ProjectFile_Add_Edit_Status_V2;

        public static readonly string SQL_Alter_Table_ProjectFile_Add_ModifyRights_Status_V2 = ProjectFileDao.SQL_Alter_Table_ProjectFile_Add_ModifyRights_Status_V2;

        public static readonly string SQL_Alter_Table_ProjectFile_Add_Reserved1_V2 = ProjectFileDao.SQL_Alter_Table_ProjectFile_Add_Reserved1_V2;

        public static readonly string SQL_Alter_Table_ProjectFile_Add_Reserved2_V2 = ProjectFileDao.SQL_Alter_Table_ProjectFile_Add_Reserved2_V2;

        public static readonly string SQL_Alter_Table_ProjectFile_Add_Reserved3_V2 = ProjectFileDao.SQL_Alter_Table_ProjectFile_Add_Reserved3_V2;

        public static readonly string SQL_Alter_Table_ProjectFile_Add_Reserved4_V2 = ProjectFileDao.SQL_Alter_Table_ProjectFile_Add_Reserved4_V2;
        #endregion

        #region Alter MyVaultFile table(V2)
        public static readonly string SQL_Alter_Table_MyVaultFile_Add_Edit_Status_V2 = MyVaultFileDao.SQL_Alter_Table_MyVaultFile_Add_Edit_Status_V2;

        public static readonly string SQL_Alter_Table_MyVaultFile_Add_ModifyRights_Status_V2 = MyVaultFileDao.SQL_Alter_Table_MyVaultFile_Add_ModifyRights_Status_V2;

        public static readonly string SQL_Alter_Table_MyVaultFile_Add_Reserved1_V2 = MyVaultFileDao.SQL_Alter_Table_MyVaultFile_Add_Reserved1_V2;

        public static readonly string SQL_Alter_Table_MyVaultFile_Add_Reserved2_V2 = MyVaultFileDao.SQL_Alter_Table_MyVaultFile_Add_Reserved2_V2;

        public static readonly string SQL_Alter_Table_MyVaultFile_Add_Reserved3_V2 = MyVaultFileDao.SQL_Alter_Table_MyVaultFile_Add_Reserved3_V2;

        public static readonly string SQL_Alter_Table_MyVaultFile_Add_Reserved4_V2 = MyVaultFileDao.SQL_Alter_Table_MyVaultFile_Add_Reserved4_V2;
        #endregion

        #region Alter SharedWithMeFile table(V2)
        public static readonly string SQL_Alter_Table_SharedWithMeFile_Add_Edit_Status_V2 = SharedWithMeFileDao.SQL_Alter_Table_SharedWithMeFile_Add_Edit_Status_V2;

        public static readonly string SQL_Alter_Table_SharedWithMeFile_Add_ModifyRights_Status_V2 = SharedWithMeFileDao.SQL_Alter_Table_SharedWithMeFile_Add_ModifyRights_Status_V2;

        public static readonly string SQL_Alter_Table_SharedWithMeFile_Add_Reserved1_V2 = SharedWithMeFileDao.SQL_Alter_Table_SharedWithMeFile_Add_Reserved1_V2;

        public static readonly string SQL_Alter_Table_SharedWithMeFile_Add_Reserved2_V2 = SharedWithMeFileDao.SQL_Alter_Table_SharedWithMeFile_Add_Reserved2_V2;

        public static readonly string SQL_Alter_Table_SharedWithMeFile_Add_Reserved3_V2 = SharedWithMeFileDao.SQL_Alter_Table_SharedWithMeFile_Add_Reserved3_V2;

        public static readonly string SQL_Alter_Table_SharedWithMeFile_Add_Reserved4_V2 = SharedWithMeFileDao.SQL_Alter_Table_SharedWithMeFile_Add_Reserved4_V2;
        #endregion

        #region Alter MyVaultLocalFile table(V3)
        public static readonly string SQL_Alter_Table_MyVaultLocalFile_Add_Comment_V3 = MyVaultLocalFileDao.SQL_Alter_Table_MyVaultLocalFile_Add_Comment_V3;
        public static readonly string SQL_Alter_Table_MyVaultLocalFile_Add_OriginalPath_V3 = MyVaultLocalFileDao.SQL_Alter_Table_MyVaultLocalFile_Add_OriginalPath_V3;
        #endregion

        #region Alter MyVaultLocalFile table(V4)
        public static readonly string SQL_Alter_Table_MyVaultLocalFile_Add_Reserved1_V4 = MyVaultLocalFileDao.SQL_Alter_Table_MyVaultLocalFile_Add_Reserved1_V4;
        public static readonly string SQL_Alter_Table_MyVaultLocalFile_Add_Reserved2_V4 = MyVaultLocalFileDao.SQL_Alter_Table_MyVaultLocalFile_Add_Reserved2_V4;
        public static readonly string SQL_Alter_Table_MyVaultLocalFile_Add_Reserved3_V4 = MyVaultLocalFileDao.SQL_Alter_Table_MyVaultLocalFile_Add_Reserved3_V4;
        public static readonly string SQL_Alter_Table_MyVaultLocalFile_Add_Reserved4_V4 = MyVaultLocalFileDao.SQL_Alter_Table_MyVaultLocalFile_Add_Reserved4_V4;
        public static readonly string SQL_Alter_Table_MyVaultLocalFile_Add_Reserved5_V4 = MyVaultLocalFileDao.SQL_Alter_Table_MyVaultLocalFile_Add_Reserved5_V4;
        #endregion

        #region Alter ProjectLocalFile table(V4)
        public static readonly string SQL_Alter_Table_ProjectLocalFile_Add_Reserved1_V4 = ProjectLocalFileDao.SQL_Alter_Table_ProjectLocalFile_Add_Reserved1_V4;
        public static readonly string SQL_Alter_Table_ProjectLocalFile_Add_Reserved2_V4 = ProjectLocalFileDao.SQL_Alter_Table_ProjectLocalFile_Add_Reserved2_V4;
        public static readonly string SQL_Alter_Table_ProjectLocalFile_Add_Reserved3_V4 = ProjectLocalFileDao.SQL_Alter_Table_ProjectLocalFile_Add_Reserved3_V4;
        public static readonly string SQL_Alter_Table_ProjectLocalFile_Add_Reserved4_V4 = ProjectLocalFileDao.SQL_Alter_Table_ProjectLocalFile_Add_Reserved4_V4;
        public static readonly string SQL_Alter_Table_ProjectLocalFile_Add_Reserved5_V4 = ProjectLocalFileDao.SQL_Alter_Table_ProjectLocalFile_Add_Reserved5_V4;
        #endregion

        #region External repository
        public static readonly string SQL_Create_Table_RmsExternalRepo = RmsExternalRepoDao.SQL_Create_Table_RmsExternalRepo;
        public static readonly string SQL_Create_Table_GoogleDriveFile = GooglDriveFileDao.SQL_Create_Table_GoogleDriveFile;
        public static readonly string SQL_Create_Table_GoogleDriveLocalFile = GoogleDriveLocalFileDao.SQL_Create_Table_GoogleDriveLocalFile;


        // dorpbox 
        public static readonly string SQL_Create_Table_DropBoxFile = DropBoxFileDao.SQL_Create_Table_DropBoxFile;
        public static readonly string SQL_Create_Table_DropBoxLocalFile = DropBoxLocalFileDao.SQL_Create_Table_DropBoxLocalFile;

        // box 
        public static readonly string SQL_Create_Table_BoxFile = BoxFileDao.SQL_Create_Table_BoxFile;
        public static readonly string SQL_Create_Table_BoxLocalFile = BoxLocalFileDao.SQL_Create_Table_BoxLocalFile;

        // sharepoint
        public static readonly string SQL_Create_Table_SharePointFile = SharePointFileDao.SQL_Create_Table_SharePointFile;
        public static readonly string SQL_Create_Table_SharePointLocalFile = SharePointLocalFileDao.SQL_Create_Table_SharePointLocalFile;

        //oneDrive
        public static readonly string SQL_Create_Table_OneDriveFileCommon = OneDriveFileCommonDao.SQL_Create_Table_OneDriveFileCommon;
        public static readonly string SQL_Create_Table_OneDriveFiles = OneDriveFilesDao.SQL_Create_Table_OneDriveFiles;
        public static readonly string SQL_Create_Table_OneDriveFileLocalStatus = OneDriveFileLocalStatusDao.SQL_Create_Table_OneDriveFileLocalStatusDao;
        public static readonly string SQL_Create_Table_OneDriveFolders = OneDriveFoldersDao.SQL_Create_Table_OneDriveFolders;
        public static readonly string SQL_Create_Table_OneDriveLocalFile = OneDriveLocalFileDao.SQL_Create_Table_OneDriveLocalFile;

        #endregion // External repository

        #region Shared workspace
        public static readonly string SQL_Create_Table_SharedWorkspaceFile = SharedWorkspaceFileDao.SQL_Create_Table_SharedWorkspaceFile;
        public static readonly string SQL_Create_Table_SharedWorkspaceLocalFile = SharedWorkspaceLocalFileDao.SQL_Create_Table_SharedWorkspaceLocalFile;
        #endregion // Shared workspace
    }
}
