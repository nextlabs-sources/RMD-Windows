using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.database.table.externalrepo
{
    public class ExternalDriveLocalFile
    {
        public int Id { get; set; }
        public int RmsExternalRepoTablePk { get; set; }
        public int ExternalDriveFileTablePk { get; set; }

        public string Name { get; set; }
        public string LocalPath { get; set; }
        public DateTime ModifiedTime { get; set; }
        public long Size { get; set; }
        public int OperationStatus { get; set; }
        // Maybe also support uploading native file in offline model.
        public bool IsNxlFile { get; set; }

        // Reserved
        public string Reserved1 { get; set; }
        public string Reserved2 { get; set; }
        public string Reserved3 { get; set; }
        public string Reserved4 { get; set; }

        public static ExternalDriveLocalFile NewByReader(SQLiteDataReader reader)
        {
            ExternalDriveLocalFile file = new ExternalDriveLocalFile()
            {
                Id = int.Parse(reader["id"].ToString()),
                RmsExternalRepoTablePk = int.Parse(reader["rms_external_repo_table_pk"].ToString()),
                ExternalDriveFileTablePk = int.Parse(reader["external_drive_file_table_pk"].ToString()),
                Name = reader["nxl_name"].ToString(),
                LocalPath = reader["nxl_local_path"].ToString(),
                ModifiedTime = DateTime.Parse(reader["last_modified_time"].ToString()).ToUniversalTime(),
                Size = long.Parse(reader["file_size"].ToString()),
                OperationStatus = int.Parse(reader["operation_status"].ToString()),
                IsNxlFile = int.Parse(reader["is_nxl_file"].ToString()) == 1,
                Reserved1 = reader["reserved1"].ToString()
            };

            return file;
        }
    }
}
