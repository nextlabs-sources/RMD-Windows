using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.model;
using SkydrmLocal.rmc.fileSystem.project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.ui.windows.fileOperationWindow.operation
{
    public interface IReShare: IBase
    {
        NxlFileType NxlType { get; }
        List<ProjectData> ProjectDatas { get; }
        bool ReShareFile(List<string>projectIdList, string comment);

        //Nxl File info
        SkydrmLocal.rmc.sdk.FileRights[] Rights { get; }
        string WaterMark { get; }
        SkydrmLocal.rmc.sdk.Expiration Expiration { get; }
        Dictionary<string, List<string>> Tags { get; }
        bool IsMarkOffline { get; }
    }
}
