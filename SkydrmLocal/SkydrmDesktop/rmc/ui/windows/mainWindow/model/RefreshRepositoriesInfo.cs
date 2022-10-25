using SkydrmDesktop.rmc.featureProvider.externalDrive.RmsRepoMgr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.ui.windows.mainWindow.model
{
    public sealed class RefreshRepositoriesInfo
    {
        public bool IsSuc { get; }
        public List<IRmsRepo> Results { get; }

        public RefreshRepositoriesInfo(bool isSuc, List<IRmsRepo> rt)
        {
            this.IsSuc = isSuc;
            this.Results = rt;
        }
    }
}
