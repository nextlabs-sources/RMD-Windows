using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.model;
using SkydrmDesktop.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.fileSystem;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.ui.windows.fileOperationWindow.operation
{
    public interface IModifyNxlFileRight : IBase
    {
        /// <summary>
        /// Use for get Tags
        /// </summary>
        IList<IFileRepo> RepoList { get; }
        int NxlRepoId { get; }

        /// <summary>
        /// Use for display and save dest path
        /// </summary>
        CurrentSelectedSavePath CurrentSelectedSavePath { get; set; }

        NxlFileType NxlType { get; }
        //Nxl File info
        List<FileRights> NxlRights { get; }
        WaterMarkInfo NxlAdhocWaterMark { get; }
        Expiration NxlExpiration { get; }
        Dictionary<string, List<string>> NxlTags { get; }

        List<FileRights> PreviewRightsByCentralPolicy(Dictionary<string, List<string>> selectedTags, out WaterMarkInfo warterMark);

        bool ModifyRights(List<FileRights> rights,
            WaterMarkInfo waterMark, Expiration expiration, Dictionary<string, List<string>> selectedTags);

        void AddLog();

        /// <summary>
        /// Update nxl status in main window list
        /// </summary>
        void UpdateNxlFile();
    }
}
