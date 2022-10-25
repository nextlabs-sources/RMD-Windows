using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.featureProvider.externalDrive.RmsRepoMgr
{
    public interface IRmsRepo
    {
        string RepoId { get; }
        ExternalRepoType Type { get; }
        string ProviderClass { get; }
        string DisplayName { get; set; }
        bool IsShared { get; }
        bool IsDefault { get; }
        string AccountName { get; }
        string AccountId { get; }
        string Token { get; set; }
        DateTime CreationTime { get; }
        DateTime UpdateTime { get; }
        string Preference { get; }
    }
}
