using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.ui.windows.AddExternalRepoWindow.addRepo
{
    public interface IInputPwdAuth
    {
        string Site { get; }
        void AuthSite(string userName, string passWord);
        bool AuthResult { get; }
    }
}
