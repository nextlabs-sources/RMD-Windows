using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmLocal.rmc.fileSystem;
using SkydrmLocal.rmc.fileSystem.basemodel;

namespace SkydrmDesktop.rmc.fileSystem.localDrive
{
    public abstract class AbstractLocalDriveRepo : AbstractFileRepo
    {
        public override IList<INxlFile> GetOfflines()
        {
            throw new NotImplementedException();
        }

        public override void DownloadFile(INxlFile nxl, bool isViewOnly, OnDownloadComplete callback, bool isDownloadPartial = false, bool isOnlineView = false)
        {
            throw new NotImplementedException();
        }

    }
}
