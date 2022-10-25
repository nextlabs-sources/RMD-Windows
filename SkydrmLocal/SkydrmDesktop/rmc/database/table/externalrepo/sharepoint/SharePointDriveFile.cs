using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.database.table.externalrepo.sharepoint
{
    public class SharePointDriveFile : ExternalDriveFile
    {
        public bool IsSite { get; set; }
        public string PathId { get; set; }
        public bool IsNxlFile { get; set; }
        public string Reserved5 { get; set; }
        public string Reserved6 { get; set; }
        public string Reserved7 { get; set; }
        public string Reserved8 { get; set; }

        public static SharePointDriveFile NewByLocal(SQLiteDataReader reader)
        {
            var rt = new SharePointDriveFile();
            {
                rt.Id = int.Parse(reader["id"].ToString());
                // remote
                rt.FileId = reader["file_id"].ToString();
                rt.IsFolder = int.Parse(reader["is_folder"].ToString()) == 1;
                rt.IsSite = int.Parse(reader["is_site"].ToString()) == 1;
                rt.Name = reader["name"].ToString();
                rt.Size = int.Parse(reader["size"].ToString());
                rt.ModifiedTime = new DateTime(long.Parse(reader["last_modified_time"].ToString()));
                rt.PathId = reader["path_id"].ToString();
                rt.DisplayPath = reader["path_display"].ToString();
                rt.CloudPathId = reader["cloud_path_id"].ToString();

                // loal
                rt.IsOffline = int.Parse(reader["is_offline"].ToString()) == 1;
                rt.IsFavorite = int.Parse(reader["is_favorite"].ToString()) == 1;
                rt.LocalPath = reader["local_path"].ToString();
                rt.IsNxlFile = int.Parse(reader["is_nxl_file"].ToString()) == 1;
                rt.Status = int.Parse(reader["status"].ToString());
                rt.CustomString = reader["custom_string"].ToString();
                rt.Edit_Status = int.Parse(reader["edit_status"].ToString());
                rt.ModifyRightsStatus = int.Parse(reader["modify_rights_status"].ToString());
            }
            return rt;
        }

        public static SharePointDriveFile NewByRemote(string fileId, bool isFolder, bool isSite,
            string name, long size, DateTime lastModifiedTime,
            string pathId, string pathDisplay, string cloudPathId,bool isNxlFile)
        {
            SharePointDriveFile ret = new SharePointDriveFile()
            {
                FileId = fileId,
                IsFolder = isFolder,
                IsSite = isSite,
                Name = name,
                Size = size,
                ModifiedTime = lastModifiedTime,
                PathId = pathId,
                DisplayPath = pathDisplay,
                CloudPathId = cloudPathId,
                IsNxlFile = isNxlFile
            };
            return ret;
        }

    }
}
