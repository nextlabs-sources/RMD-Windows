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

        public static readonly string SQL_Create_Table_MyVaultFile = MyVaultFileDao.SQL_Create_Table_MyVaultFile;

        public static readonly string SQL_Create_Table_MyVaultLocalFile = MyVaultLocalFileDao.SQL_Create_Table_MyVaultLocalFile;

        public static readonly string SQL_Create_Table_Recipients = RecipientDao.SQL_Create_Table_Recipients;

        public static readonly string SQL_Create_Table_NxlOperLog = NxlOperLogDao.SQL_Create_Table_NxlOperLog;

        public static readonly string SQL_Create_Table_Project = ProjectDao.SQL_Create_Table_Project;

        public static readonly string SQL_Alter_Table_Project_V1 = ProjectDao.SQL_Alter_Table_Project_V1;

        public static readonly string SQL_Create_Table_ProjectFile = ProjectFileDao.SQL_Create_Table_ProjectFile;

        public static readonly string SQL_Create_Table_ProjectLocalFile = ProjectLocalFileDao.SQL_Create_Table_ProjectLocalFile;

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
    }
}
