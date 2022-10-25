using SkydrmDesktop.rmc.ui.windows.mainWindow.model;
using SkydrmLocal.rmc.fileSystem;
using SkydrmLocal.rmc.fileSystem.basemodel;
using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.ui.windows.fileOperationWindow.operation
{
    public interface IProtect : IBase
    {
        /// <summary>
        /// Use for init TreeView
        /// </summary>
        IList<IFileRepo> TreeList { get; }

       /// <summary>
       /// Use for display and save dest path
       /// </summary>
        CurrentSelectedSavePath CurrentSelectedSavePath { get; set; }

        List<FileRights> PreviewRightsByCentralPolicy(int id, Dictionary<string, List<string>> selectedTags, out string warterMark);

        List<INxlFile> ProtectFile(List<FileRights> rights,
            string waterMarkTxt, Expiration expiration, Dictionary<string, List<string>> selectedTags);
    }
}
