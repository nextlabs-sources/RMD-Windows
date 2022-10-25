using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmDesktop.rmc.featureProvider.externalDrive.RmsRepoMgr;
using SkydrmLocal.rmc.exception;

namespace SkydrmDesktop.rmc.ui.windows.AddExternalRepoWindow.addRepo
{
    class AddExternalRepo : IAddExternalRepo
    {
        private readonly SkydrmApp app = SkydrmApp.Singleton;

        public void ListRepo()
        {
            throw new NotImplementedException();
        }

        public string GetAuthUri(string name, ExternalRepoType type, string authUrl="")
        {
            try
            {
                return app.RmsRepoMgr.GetAuthorizationURI(name, type, authUrl);
            }
            catch (SkydrmLocal.rmc.sdk.SkydrmException e)
            {
                GeneralHandler.Handle(e, true);
                return "";
            }
        }

    }
}
