using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmDesktop.rmc.database.table.externalrepo.sharepoint;
using SkydrmLocal.rmc.fileSystem.basemodel;

namespace SkydrmDesktop.rmc.featureProvider.externalDrive.sharepoint
{
    public sealed class SharePointOnlineLocalFile : SharePointBaseLocalFile
    {
        public SharePointOnlineLocalFile(NxSharePointBase host, SharePointDriveLocalFile raw) : base(host, raw)
        {

        }

    }
}
