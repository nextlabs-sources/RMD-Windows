using SkydrmDesktop.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.ui.windows.fileOperationWindow.operation
{
    public interface ISpecialProtect : IBase
    {
        /// <summary>
        /// Use for display and save dest path
        /// </summary>
        CurrentSelectedSavePath CurrentSelectedSavePath { get; }

        Dictionary<string, List<string>> SelectedTags { get; }

        List<FileRights> PreviewRightsByCentralPolicy(int id, Dictionary<string, List<string>> selectedTags, out string warterMark);

        List<INxlFile> ProtectFile( Dictionary<string, List<string>> selectedTags);
    }
}
