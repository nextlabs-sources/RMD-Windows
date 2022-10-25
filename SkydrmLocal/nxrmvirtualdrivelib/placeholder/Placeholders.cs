using nxrmvirtualdrivelib.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.placeholder
{
    public class Placeholders
    {
        private string userFsRootPath;

        public Placeholders(string userFsRootPath)
        {
            this.userFsRootPath = userFsRootPath;
        }

        public PlaceholderFolder GetRootItem()
        {
            return (PlaceholderFolder)PlaceholderItem.GetPlaceholderItem(userFsRootPath, FileSystemItemType.Folder);
        }

        public static PlaceholderItem GetItem(string path)
        {
            return PlaceholderItem.GetPlaceholderItem(path);
        }

        public bool TryGetItem(string path, out PlaceholderItem item)
        {
            return PlaceholderItem.TryGetPlaceholderItem(path, out item);
        }
    }
}
