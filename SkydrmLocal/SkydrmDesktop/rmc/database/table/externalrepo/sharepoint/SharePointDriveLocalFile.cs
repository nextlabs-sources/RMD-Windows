using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.database.table.externalrepo.sharepoint
{
    public class SharePointDriveLocalFile : ExternalDriveLocalFile
    {
        public static SharePointDriveLocalFile NewByDBItem(SQLiteDataReader reader)
        {
            SharePointDriveLocalFile ret = new SharePointDriveLocalFile()
            {
                Id = int.Parse(reader["id"].ToString()),
                RmsExternalRepoTablePk = int.Parse(reader["rms_external_repo_table_pk"].ToString()),
                ExternalDriveFileTablePk = int.Parse(reader["external_drive_file_table_pk"].ToString()),
                Name = reader["nxl_name"].ToString(),
                LocalPath = reader["nxl_local_path"].ToString(),
                ModifiedTime = DateTime.Parse(reader["last_modified_time"].ToString()).ToUniversalTime(),
                Size = long.Parse(reader["file_size"].ToString()),
                OperationStatus = int.Parse(reader["operation_status"].ToString())
            };
            return ret;
        }
    }
}
