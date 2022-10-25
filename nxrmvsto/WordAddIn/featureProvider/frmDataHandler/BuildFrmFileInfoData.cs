using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinFormControlLibrary;
using WordAddIn.featureProvider.helper;

namespace WordAddIn.featureProvider
{
    public class BuildFrmFileInfoData
    {
        private FileRightsInfoDataModel dataMode;

        public BuildFrmFileInfoData(string filePath, Dictionary<string, List<string>> tags, HashSet<Rights> filerRights, string warterMark)
        {
            dataMode = new FileRightsInfoDataModel()
            {
                FilePath = filePath,
                FileTags = tags,
                Filerights = filerRights,
                Wartemark = warterMark,
                IsModifyBtnVisible = false
            };
        }

        public FileRightsInfoDataModel DataMode { get => dataMode; }

    }
}
