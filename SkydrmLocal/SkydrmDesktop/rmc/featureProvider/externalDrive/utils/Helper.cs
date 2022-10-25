using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.featureProvider.externalDrive
{
    public delegate void OnSyncFromRemoteComplete(bool bSuc, List<IExternalDriveFile> results);
}
