using SkydrmDesktop.rmc.featureProvider.externalDrive.sharepoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.ui.windows.AddExternalRepoWindow.addRepo
{
    class InputPwdAuth : IInputPwdAuth
    {
        private string site;
        private bool authResult;

        public InputPwdAuth(string siteUrl)
        {
            site = siteUrl;
        }

        public string Site => site;

        public bool AuthResult { get => authResult; }

        public void AuthSite(string userName, string passWord)
        {
            try
            {
                // invoke api
                authResult = NxSharePointOnPremise.TryAuth(Site, userName, passWord);
                if(authResult)
                {
                    // Invoke add repo & sync repository 
                }
            }
            catch (Exception e)
            {
                authResult = false;
                // notify user
                SkydrmApp.Singleton.ShowBalloonTip(e.Message, false);
            }

        }
    }
}
