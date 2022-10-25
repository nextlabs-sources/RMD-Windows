using SkydrmDesktop.rmc.ui.windows.fileOperationWindow.model;
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
    public interface IAddNxlFile: IBase
    {
        /// <summary>
        /// Use for init TreeView
        /// </summary>
        IList<IFileRepo> TreeList { get; }

        /// <summary>
        /// Use for display and save dest path
        /// </summary>
        CurrentSelectedSavePath CurrentSelectedSavePath { get; set; }

        EnumFileRepo OriginalFileRepo { get; }
        /// <summary>
        /// Use to external repo
        /// </summary>
        string RepoId { get; }

        int ProjectId { get; }
        NxlFileType NxlType { get; }
        //Nxl File info
        List<FileRights> NxlRights { get; }
        WaterMarkInfo NxlAdhocWaterMark { get; }
        Expiration NxlExpiration { get; }
        Dictionary<string, List<string>> NxlTags { get; }

        List<FileRights> PreviewRightsByCentralPolicy(int id, Dictionary<string, List<string>> selectedTags, out WaterMarkInfo warterMark);

        //bool DecryptNxlFile(out string decryptPath);

        //void DeleteDecryptNxlFile();

        //List<INxlFile> ProtectFile(string[] filePath, List<FileRights> rights,
        //    WaterMarkInfo waterMark, Expiration expiration, Dictionary<string, List<string>> selectedTags);

        //void AddLog();

        string FileName { get; }

        string AvailableDestFileName { get; }

        bool CheckNxlFileExists();

        void CopyNxlFile(string destName = "", bool overwrite = true);
    }
}
