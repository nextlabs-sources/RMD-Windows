using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmDesktop.rmc.database.table.externalrepo.sharepoint;
using SkydrmLocal.rmc.fileSystem.basemodel;

namespace SkydrmDesktop.rmc.featureProvider.externalDrive.sharepoint
{
    public class SharePointOnlineFile : SharePointBaseFile
    {
        public SharePointOnlineFile(NxSharePointBase host) : base(host)
        {

        }

        public SharePointOnlineFile(NxSharePointBase host, SharePointDriveFile item) : base(host, item)
        {

        }

    }
}
