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
        private static readonly int db_version = 3;
        private readonly DBUpgradeManager UpgradeManager;

        public DbVersionControl()
        {
            UpgradeManager = new DBUpgradeManager();
        }

        public void DetectVersion(string DataBaseConnectionString)
        {

            OnCreateDatabase(DataBaseConnectionString);

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
                    // table recentTouchedFile
                    command.CommandText = Config.SQL_Create_Table_RecentTouchedFile;
                    command.ExecuteNonQuery();
                    // table sharedWithMe
                    command.CommandText = Config.SQL_Create_Table_SharedWithMe;
                    command.ExecuteNonQuery();
                    // talbe 
                    command.CommandText = Config.SQL_Create_Table_SystemBucket;
                    command.ExecuteNonQuery();
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
                SkydrmLocalApp.Singleton.Log.Info(e);
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

    }
}
