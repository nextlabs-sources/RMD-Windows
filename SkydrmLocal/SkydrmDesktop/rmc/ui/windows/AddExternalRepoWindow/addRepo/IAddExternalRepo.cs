using SkydrmDesktop.rmc.featureProvider.externalDrive.RmsRepoMgr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.ui.windows.AddExternalRepoWindow.addRepo
{
    public interface IAddExternalRepo
    {
        void ListRepo();
        string GetAuthUri(string name, ExternalRepoType type, string authUrl="");
    }
}
