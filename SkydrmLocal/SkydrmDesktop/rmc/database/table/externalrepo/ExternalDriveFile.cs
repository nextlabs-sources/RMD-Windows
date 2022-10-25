using CustomControls.common.helper;
using SkydrmLocal.rmc.common.helper;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.database.table.externalrepo
{
    /// <summary>
    /// Common db data model for all external drive file node.
    /// </summary>
    public class ExternalDriveFile
    {   
        public int Id { get; set; }
        // remote
        public string FileId { get; set; }
        public bool IsFolder { get; set; }
        public string Name { get; set; }
        public long Size { get; set; }
        public string DisplayPath { get; set; }
        public string CloudPathId { get; set; }
        public DateTime ModifiedTime { get; set; }

        // local
        public bool IsOffline { get; set; }
        public bool IsFavorite { get; set; }
        public string LocalPath { get; set; }
        public bool IsNxlFile { get; set; }
        public int Status { get; set; }
        public string CustomString { get; set; } // store some common custom data.

        // For nxl file, maybe used in the future
        public int Edit_Status { get; set; }
        public int ModifyRightsStatus { get; set; }

        // Reserved
        public string Reserved1 { get; set; }
        public string Reserved2 { get; set; }
        public string Reserved3 { get; set; }
        public string Reserved4 { get; set; }
        
        public static ExternalDriveFile NewByReader(SQLiteDataReader reader)
        {
            var rt = new ExternalDriveFile();
            {
                rt.Id = int.Parse(reader["id"].ToString());
                // remote
                rt.FileId = reader["ser_file_id"].ToString();
                rt.IsFolder = int.Parse(reader["ser_isFolder"].ToString()) == 1;
                rt.Name = reader["ser_file_name"].ToString();
                rt.Size = int.Parse(reader["ser_size"].ToString());
                rt.DisplayPath = reader["ser_display_path"].ToString();
                rt.CloudPathId = reader["ser_cloud_pathid"].ToString();
                rt.ModifiedTime = new DateTime(Int64.Parse(reader["ser_modified_time"].ToString()));
                // loal
                rt.IsOffline = int.Parse(reader["is_offline"].ToString()) == 1;
                rt.IsFavorite = int.Parse(reader["is_favorite"].ToString()) == 1;
                rt.LocalPath = reader["local_path"].ToString();
                rt.IsNxlFile = int.Parse(reader["is_nxl_file"].ToString()) == 1;
                rt.Status = int.Parse(reader["status"].ToString());
                rt.CustomString = reader["custom_string"].ToString();
                rt.Edit_Status = int.Parse(reader["edit_status"].ToString());
                rt.ModifyRightsStatus = int.Parse(reader["modify_rights_status"].ToString());
                // resereved
                rt.Reserved1 = "";
                rt.Reserved2 = "";
                rt.Reserved3 = "";
                rt.Reserved4 = "";
            }

            return rt;
        }

        public static database.table.externalrepo.ExternalDriveFile AdapterOneDriveDataStructure(SQLiteDataReader reader)
        {
            database.table.externalrepo.ExternalDriveFile rt = new database.table.externalrepo.ExternalDriveFile();
            {
                rt.Id = int.Parse(reader["id"].ToString());
                rt.FileId = reader["item_id"].ToString();
                rt.IsFolder = int.Parse(reader["isFolder"].ToString()) == 1;
                rt.Name = reader["name"].ToString();
                rt.Size = long.Parse(reader["size"].ToString());
               // rt.DisplayPath = WebUtility.UrlDecode(reader["parentReference_path"].ToString());
                rt.DisplayPath = Uri.UnescapeDataString(reader["parentReference_path"].ToString());
                rt.CloudPathId = reader["item_id"].ToString();
                DateTime dateTime = DateTime.Parse(reader["lastModifiedDateTime"].ToString());
                rt.ModifiedTime = new DateTime(Int64.Parse(JavaTimeConverter.ToCSLongTicks(SkydrmLocal.rmc.common.helper.DateTimeHelper.DateTimeToTimestamp(dateTime)).ToString()));

                if (!rt.IsFolder)
                {
                    // local
                    rt.IsOffline = int.Parse(reader["is_offline"].ToString()) == 1;
                    rt.IsFavorite = int.Parse(reader["is_favorite"].ToString()) == 1;
                    rt.LocalPath = reader["local_path"].ToString();
                    rt.IsNxlFile = int.Parse(reader["is_nxl_file"].ToString()) == 1;
                    rt.Status = int.Parse(reader["status"].ToString());
                    rt.CustomString = reader["custom_string"].ToString();
                    rt.Edit_Status = int.Parse(reader["edit_status"].ToString());
                    rt.ModifyRightsStatus = int.Parse(reader["modify_rights_status"].ToString());
                }

                // resereved
                rt.Reserved1 = "";
                rt.Reserved2 = "";
                rt.Reserved3 = "";
                rt.Reserved4 = "";
            }
            return rt;
        }
    }
}
