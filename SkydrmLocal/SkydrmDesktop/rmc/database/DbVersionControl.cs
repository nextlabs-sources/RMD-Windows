using SkydrmDesktop;
using SkydrmLocal.rmc.database2.manager;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.database
{
    class DbVersionControl
    {
        // Should increase the 'db_version' value whenever upgrade db table(Namely changed table structure).
        private static readonly int db_version = 4;
        private readonly DBUpgradeManager UpgradeManager;

        public DbVersionControl()
        {
            UpgradeManager = new DBUpgradeManager();
        }

        public void DetectVersion(string DataBaseConnectionString)
        {
            // Create db
            OnCreateDatabase(DataBaseConnectionString);

            // Check db version
            int version = SqliteOpenHelper.GetVersion(DataBaseConnectionString);
            int newVersion = db_version;

            if (version != newVersion)
            {
                if (version == 0)
                {
                    //If detect current db user_version is 0 there are two conditons:
                    //Condition1:Intialize,in this condtion our OnCreateDataBase invoked doen not contain updated column.
                    //Condition2:Database file recover create,in this case OnCreateDataBase contains updated column. 

                    //Compatiable for condition1.
                    if (newVersion == 1)
                    {
                        using (SQLiteConnection connection = new SQLiteConnection(DataBaseConnectionString))
                        {
                            connection.Open();
                            using (SQLiteCommand command = new SQLiteCommand(connection))
                            {
                                OnUpgrade(command, 0, 1);
                            }
                        }
                    }
                    //Avoid upgrad one by one version. We make every version change contains into create table sql(not alter sql).
                    version = newVersion;
                }
                else
                {
                    if (version > newVersion)
                    {
                        using (SQLiteConnection connection = new SQLiteConnection(DataBaseConnectionString))
                        {
                            connection.Open();
                            using (SQLiteCommand command = new SQLiteCommand(connection))
                            {
                                OnDowngrade(command, version, newVersion);
                            }
                        }
                    }
                    else
                    {
                        using (SQLiteConnection connection = new SQLiteConnection(DataBaseConnectionString))
                        {
                            connection.Open();
                            using (SQLiteCommand command = new SQLiteCommand(connection))
                            {
                                OnUpgrade(command, version, newVersion);
                            }
                        }
                    }
                }
                SqliteOpenHelper.SetVersion(DataBaseConnectionString, newVersion);
            }
        }
        public void OnCreateDatabase(string DataBaseConnectionString)
        {
            using (SQLiteConnection connection = new SQLiteConnection(DataBaseConnectionString))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    // table server
                    command.CommandText = Config.SQL_Create_Table_Server;
                    command.ExecuteNonQuery();

                    // table user
                    command.CommandText = Config.SQL_Create_Table_User;
                    command.ExecuteNonQuery();
                    // table workspace file
                    command.CommandText = Config.SQL_Create_Table_WorkSpaceFile;
                    command.ExecuteNonQuery();
                    //table workspace local file
                    command.CommandText = Config.SQL_Create_Table_WorkSpaceLocalFile;
                    command.ExecuteNonQuery();
                    // table mydrive
                    command.CommandText = Config.SQL_Create_Table_MyDriveFile;
                    command.ExecuteNonQuery();
                    // table mydrivelocalfile
                    command.CommandText = Config.SQL_Create_Table_MyDriveLocalFile;
                    command.ExecuteNonQuery();
                    // table myvaultfile
                    command.CommandText = Config.SQL_Create_Table_MyVaultFile;
                    command.ExecuteNonQuery();
                    // table myvaultlocalfile
                    command.CommandText = Config.SQL_Create_Table_MyVaultLocalFile;
                    command.ExecuteNonQuery();
                    // table recipients
                    command.CommandText = Config.SQL_Create_Table_Recipients;
                    command.ExecuteNonQuery();
                    // table nxloperlog
                    command.CommandText = Config.SQL_Create_Table_NxlOperLog;
                    command.ExecuteNonQuery();
                    // table project
                    command.CommandText = Config.SQL_Create_Table_Project;
                    command.ExecuteNonQuery();
                    // table prjectfile
                    command.CommandText = Config.SQL_Create_Table_ProjectFile;
                    command.ExecuteNonQuery();
                    // table projectlocalfile
                    command.CommandText = Config.SQL_Create_Table_ProjectLocalFile;
                    command.ExecuteNonQuery();
                    // table sharedWithProjectFile
                    command.CommandText = Config.SQL_Create_Table_SharedWithProjectFile;
                    command.ExecuteNonQuery();
                    // table recentTouchedFile
                    command.CommandText = Config.SQL_Create_Table_RecentTouchedFile;
                    command.ExecuteNonQuery();
                    // table sharedWithMe
                    command.CommandText = Config.SQL_Create_Table_SharedWithMe;
                    command.ExecuteNonQuery();
                    // talbe systemBucket
                    command.CommandText = Config.SQL_Create_Table_SystemBucket;
                    command.ExecuteNonQuery();
                    // table sharedWorksapce file
                    command.CommandText = Config.SQL_Create_Table_SharedWorkspaceFile;
                    command.ExecuteNonQuery();
                    // table sharedWorksapce local file
                    command.CommandText = Config.SQL_Create_Table_SharedWorkspaceLocalFile;
                    command.ExecuteNonQuery();

                    // table repository
                    command.CommandText = Config.SQL_Create_Table_RmsExternalRepo;
                    command.ExecuteNonQuery();

                    if (SkydrmApp.Singleton.IsEnableExternalRepo)
                    {
                        // google drive
                        command.CommandText = Config.SQL_Create_Table_GoogleDriveFile;
                        command.ExecuteNonQuery();
                        command.CommandText = Config.SQL_Create_Table_GoogleDriveLocalFile;
                        command.ExecuteNonQuery();

                        // dropbox
                        command.CommandText = Config.SQL_Create_Table_DropBoxFile;
                        command.ExecuteNonQuery();
                        command.CommandText = Config.SQL_Create_Table_DropBoxLocalFile;
                        command.ExecuteNonQuery();
                        //  box
                        command.CommandText = Config.SQL_Create_Table_BoxFile;
                        command.ExecuteNonQuery();
                        command.CommandText = Config.SQL_Create_Table_BoxLocalFile;
                        command.ExecuteNonQuery();
                        // sharepoint
                        command.CommandText = Config.SQL_Create_Table_SharePointFile;
                        command.ExecuteNonQuery();
                        command.CommandText = Config.SQL_Create_Table_SharePointLocalFile;
                        command.ExecuteNonQuery();

                        //oneDrive
                        command.CommandText = Config.SQL_Create_Table_OneDriveFileCommon;
                        command.ExecuteNonQuery();
                        command.CommandText = Config.SQL_Create_Table_OneDriveFiles;
                        command.ExecuteNonQuery();
                        command.CommandText = Config.SQL_Create_Table_OneDriveFileLocalStatus;
                        command.ExecuteNonQuery();
                        command.CommandText = Config.SQL_Create_Table_OneDriveFolders;
                        command.ExecuteNonQuery();
                        command.CommandText = Config.SQL_Create_Table_OneDriveLocalFile;
                        command.ExecuteNonQuery();
                    }

                }
            }
        }

        protected void OnDowngrade(SQLiteCommand command, int oldVersion, int newVersion)
        {

        }

        protected void OnUpgrade(SQLiteCommand command, int oldVersion, int newVersion)
        {
            UpgradeManager.HandleUpgrade(command, oldVersion, newVersion);
        }
    }

    internal class DBUpgradeManager
    {

        public void HandleUpgrade(SQLiteCommand command, int oldVersion, int newVersion)
        {
            for (int i = oldVersion; i < newVersion; i++)
            {
                switch (i)
                {
                    case 0:
                        UpgradeToVersion1(command);
                        break;
                    case 1:
                        UpgradeToVersion2(command);
                        break;
                    case 2:
                        UpgradeToVersion3(command);
                        break;
                    case 3:
                        UpgradeToVersion4(command);
                        break;
                }
            }
        }

        private void UpgradeToVersion1(SQLiteCommand command)
        {
            try
            {
                bool hascol = false;
                hascol = SqliteOpenHelper.CheckCloumnExist(command, "Project", "rms_is_enable_adhoc");
                if (!hascol)
                {
                    command.CommandText = Config.SQL_Alter_Table_Project_V1;
                    command.ExecuteNonQuery();
                }

            }
            catch (Exception e)
            {
                SkydrmApp.Singleton.Log.Info(e);
            }
        }

        private void UpgradeToVersion2(SQLiteCommand command)
        {
            //Alter project file table.
            {
                // Add column edit_status.
                bool hasEditColumn = SqliteOpenHelper.CheckCloumnExist(command, "ProjectFile", "edit_status");
                if (!hasEditColumn)
                {
                    command.CommandText = Config.SQL_Alter_Table_ProjectFile_Add_Edit_Status_V2;
                    command.ExecuteNonQuery();
                }
                // Add column modify_rights_status
                bool hasModifyRightsColumn = SqliteOpenHelper.CheckCloumnExist(command, "ProjectFile", "modify_rights_status");
                if (!hasModifyRightsColumn)
                {
                    command.CommandText = Config.SQL_Alter_Table_ProjectFile_Add_ModifyRights_Status_V2;
                    command.ExecuteNonQuery();
                }
                // Add column reserved1
                bool hasReserved1Column = SqliteOpenHelper.CheckCloumnExist(command, "ProjectFile", "reserved1");
                if (!hasReserved1Column)
                {
                    command.CommandText = Config.SQL_Alter_Table_ProjectFile_Add_Reserved1_V2;
                    command.ExecuteNonQuery();
                }
                // Add column reserved2
                bool hasReserved2Column = SqliteOpenHelper.CheckCloumnExist(command, "ProjectFile", "reserved2");
                if (!hasReserved2Column)
                {
                    command.CommandText = Config.SQL_Alter_Table_ProjectFile_Add_Reserved2_V2;
                    command.ExecuteNonQuery();
                }
                // Add column reserved3
                bool hasReserved3Column = SqliteOpenHelper.CheckCloumnExist(command, "ProjectFile", "reserved3");
                if (!hasReserved3Column)
                {
                    command.CommandText = Config.SQL_Alter_Table_ProjectFile_Add_Reserved3_V2;
                    command.ExecuteNonQuery();
                }
                // Add column reserved4
                bool hasReserved4Column = SqliteOpenHelper.CheckCloumnExist(command, "ProjectFile", "reserved4");
                if (!hasReserved4Column)
                {
                    command.CommandText = Config.SQL_Alter_Table_ProjectFile_Add_Reserved4_V2;
                    command.ExecuteNonQuery();
                }
            }

            //Alter myVault file table.
            {
                // Add column edit_status.
                bool hasEditColumn = SqliteOpenHelper.CheckCloumnExist(command, "MyVaultFile", "edit_status");
                if (!hasEditColumn)
                {
                    command.CommandText = Config.SQL_Alter_Table_MyVaultFile_Add_Edit_Status_V2;
                    command.ExecuteNonQuery();
                }
                // Add column modify_rights_status
                bool hasModifyRightsColumn = SqliteOpenHelper.CheckCloumnExist(command, "MyVaultFile", "modify_rights_status");
                if (!hasModifyRightsColumn)
                {
                    command.CommandText = Config.SQL_Alter_Table_MyVaultFile_Add_ModifyRights_Status_V2;
                    command.ExecuteNonQuery();
                }
                // Add column reserved1
                bool hasReserved1Column = SqliteOpenHelper.CheckCloumnExist(command, "MyVaultFile", "reserved1");
                if (!hasReserved1Column)
                {
                    command.CommandText = Config.SQL_Alter_Table_MyVaultFile_Add_Reserved1_V2;
                    command.ExecuteNonQuery();
                }
                // Add column reserved2
                bool hasReserved2Column = SqliteOpenHelper.CheckCloumnExist(command, "MyVaultFile", "reserved2");
                if (!hasReserved2Column)
                {
                    command.CommandText = Config.SQL_Alter_Table_MyVaultFile_Add_Reserved2_V2;
                    command.ExecuteNonQuery();
                }
                // Add column reserved3
                bool hasReserved3Column = SqliteOpenHelper.CheckCloumnExist(command, "MyVaultFile", "reserved3");
                if (!hasReserved3Column)
                {
                    command.CommandText = Config.SQL_Alter_Table_MyVaultFile_Add_Reserved3_V2;
                    command.ExecuteNonQuery();
                }
                // Add column reserved4
                bool hasReserved4Column = SqliteOpenHelper.CheckCloumnExist(command, "MyVaultFile", "reserved4");
                if (!hasReserved4Column)
                {
                    command.CommandText = Config.SQL_Alter_Table_MyVaultFile_Add_Reserved4_V2;
                    command.ExecuteNonQuery();
                }
            }
            //Alter sharedwithme file table.
            {
                // Add column edit_status.
                bool hasEditColumn = SqliteOpenHelper.CheckCloumnExist(command, "SharedWithMeFile", "edit_status");
                if (!hasEditColumn)
                {
                    command.CommandText = Config.SQL_Alter_Table_SharedWithMeFile_Add_Edit_Status_V2;
                    command.ExecuteNonQuery();
                }
                // Add column modify_rights_status
                bool hasModifyRightsColumn = SqliteOpenHelper.CheckCloumnExist(command, "SharedWithMeFile", "modify_rights_status");
                if (!hasModifyRightsColumn)
                {
                    command.CommandText = Config.SQL_Alter_Table_SharedWithMeFile_Add_ModifyRights_Status_V2;
                    command.ExecuteNonQuery();
                }
                // Add column reserved1
                bool hasReserved1Column = SqliteOpenHelper.CheckCloumnExist(command, "SharedWithMeFile", "reserved1");
                if (!hasReserved1Column)
                {
                    command.CommandText = Config.SQL_Alter_Table_SharedWithMeFile_Add_Reserved1_V2;
                    command.ExecuteNonQuery();
                }
                // Add column reserved2
                bool hasReserved2Column = SqliteOpenHelper.CheckCloumnExist(command, "SharedWithMeFile", "reserved2");
                if (!hasReserved2Column)
                {
                    command.CommandText = Config.SQL_Alter_Table_SharedWithMeFile_Add_Reserved2_V2;
                    command.ExecuteNonQuery();
                }
                // Add column reserved3
                bool hasReserved3Column = SqliteOpenHelper.CheckCloumnExist(command, "SharedWithMeFile", "reserved3");
                if (!hasReserved3Column)
                {
                    command.CommandText = Config.SQL_Alter_Table_SharedWithMeFile_Add_Reserved3_V2;
                    command.ExecuteNonQuery();
                }
                // Add column reserved4
                bool hasReserved4Column = SqliteOpenHelper.CheckCloumnExist(command, "SharedWithMeFile", "reserved4");
                if (!hasReserved4Column)
                {
                    command.CommandText = Config.SQL_Alter_Table_SharedWithMeFile_Add_Reserved4_V2;
                    command.ExecuteNonQuery();
                }
            }
        }
        private void UpgradeToVersion3(SQLiteCommand command)
        {
            //Alter MyVaultLocal file table.
            // Add column comment
            bool hasCommentColumn = SqliteOpenHelper.CheckCloumnExist(command, "MyVaultLocalFile", "nxl_comment");
            if (!hasCommentColumn)
            {
                command.CommandText = Config.SQL_Alter_Table_MyVaultLocalFile_Add_Comment_V3;
                command.ExecuteNonQuery();
            }
            // Add column originalPath
            bool hasOriginalPathColumn = SqliteOpenHelper.CheckCloumnExist(command, "MyVaultLocalFile", "nxl_original_path");
            if (!hasOriginalPathColumn)
            {
                command.CommandText = Config.SQL_Alter_Table_MyVaultLocalFile_Add_OriginalPath_V3;
                command.ExecuteNonQuery();
            }
        }

        // Externd some reserved fields for myVault(and project) local file table.
        private void UpgradeToVersion4(SQLiteCommand command)
        {
            //
            // Alter MyVaultLocal file table.
            //

            // Add column reserved1
            bool hasReserved1Column = SqliteOpenHelper.CheckCloumnExist(command, "MyVaultLocalFile", "reserved1");
            if (!hasReserved1Column)
            {
                command.CommandText = Config.SQL_Alter_Table_MyVaultLocalFile_Add_Reserved1_V4;
                command.ExecuteNonQuery();
            }

            // Add column reserved2
            bool hasReserved2Column = SqliteOpenHelper.CheckCloumnExist(command, "MyVaultLocalFile", "reserved2");
            if (!hasReserved2Column)
            {
                command.CommandText = Config.SQL_Alter_Table_MyVaultLocalFile_Add_Reserved2_V4;
                command.ExecuteNonQuery();
            }

            // Add column reserved3
            bool hasReserved3Column = SqliteOpenHelper.CheckCloumnExist(command, "MyVaultLocalFile", "reserved3");
            if (!hasReserved3Column)
            {
                command.CommandText = Config.SQL_Alter_Table_MyVaultLocalFile_Add_Reserved3_V4;
                command.ExecuteNonQuery();
            }

            // Add column reserved4
            bool hasReserved4Column = SqliteOpenHelper.CheckCloumnExist(command, "MyVaultLocalFile", "reserved4");
            if (!hasReserved4Column)
            {
                command.CommandText = Config.SQL_Alter_Table_MyVaultLocalFile_Add_Reserved4_V4;
                command.ExecuteNonQuery();
            }

            // Add column reserved5
            bool hasReserved5Column = SqliteOpenHelper.CheckCloumnExist(command, "MyVaultLocalFile", "reserved5");
            if (!hasReserved5Column)
            {
                command.CommandText = Config.SQL_Alter_Table_MyVaultLocalFile_Add_Reserved5_V4;
                command.ExecuteNonQuery();
            }


            //
            // Alter ProjectLocal file table.
            //

            // Add column reserved1
            bool hasReserved1Column_ = SqliteOpenHelper.CheckCloumnExist(command, "ProjectLocalFile", "reserved1");
            if (!hasReserved1Column_)
            {
                command.CommandText = Config.SQL_Alter_Table_ProjectLocalFile_Add_Reserved1_V4;
                command.ExecuteNonQuery();
            }

            // Add column reserved2
            bool hasReserved2Column_ = SqliteOpenHelper.CheckCloumnExist(command, "ProjectLocalFile", "reserved2");
            if (!hasReserved2Column_)
            {
                command.CommandText = Config.SQL_Alter_Table_ProjectLocalFile_Add_Reserved2_V4;
                command.ExecuteNonQuery();
            }

            // Add column reserved3
            bool hasReserved3Column_ = SqliteOpenHelper.CheckCloumnExist(command, "ProjectLocalFile", "reserved3");
            if (!hasReserved3Column_)
            {
                command.CommandText = Config.SQL_Alter_Table_ProjectLocalFile_Add_Reserved3_V4;
                command.ExecuteNonQuery();
            }

            // Add column reserved4
            bool hasReserved4Column_ = SqliteOpenHelper.CheckCloumnExist(command, "ProjectLocalFile", "reserved4");
            if (!hasReserved4Column_)
            {
                command.CommandText = Config.SQL_Alter_Table_ProjectLocalFile_Add_Reserved4_V4;
                command.ExecuteNonQuery();
            }

            // Add column reserved5
            bool hasReserved5Column_ = SqliteOpenHelper.CheckCloumnExist(command, "ProjectLocalFile", "reserved5");
            if (!hasReserved5Column_)
            {
                command.CommandText = Config.SQL_Alter_Table_ProjectLocalFile_Add_Reserved5_V4;
                command.ExecuteNonQuery();
            }

        }

    }
}
