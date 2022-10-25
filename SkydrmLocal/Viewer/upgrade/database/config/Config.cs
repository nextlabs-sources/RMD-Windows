using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.upgrade.database
{
    public class Config
    {
        public static readonly string Database_Name = "DataBase2.db";

        public static readonly string ConnectionString = "Data Source=" + Database_Name;

        public static readonly string SQL_Create_Table_Server = ServerDao.SQL_Create_Table_Server;

        public static readonly string SQL_Create_Table_User = UserDao.SQL_Create_Table_User;

        public static readonly string SQL_Create_Table_MyVaultFile = MyVaultFileDao.SQL_Create_Table_MyVaultFile;

        public static readonly string SQL_Create_Table_MyVaultLocalFile = MyVaultLocalFileDao.SQL_Create_Table_MyVaultLocalFile;

        public static readonly string SQL_Create_Table_Project = ProjectDao.SQL_Create_Table_Project;

        public static readonly string SQL_Alter_Table_Project_V1 = ProjectDao.SQL_Alter_Table_Project_V1;

        public static readonly string SQL_Create_Table_ProjectFile = ProjectFileDao.SQL_Create_Table_ProjectFile;

        public static readonly string SQL_Create_Table_ProjectLocalFile = ProjectLocalFileDao.SQL_Create_Table_ProjectLocalFile;

        public static readonly string SQL_Create_Table_SharedWithMe = SharedWithMeFileDao.SQL_Create_Table_SharedWithMeFileDao;


        #region Alter MyVaultLocalFile table(V3)
        public static readonly string SQL_Alter_Table_MyVaultLocalFile_Add_Comment_V3 = MyVaultLocalFileDao.SQL_Alter_Table_MyVaultLocalFile_Add_Comment_V3;
        public static readonly string SQL_Alter_Table_MyVaultLocalFile_Add_OriginalPath_V3 = MyVaultLocalFileDao.SQL_Alter_Table_MyVaultLocalFile_Add_OriginalPath_V3;
        #endregion

    }
}
