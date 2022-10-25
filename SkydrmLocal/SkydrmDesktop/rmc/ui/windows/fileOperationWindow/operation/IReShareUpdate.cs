using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.ui.windows.fileOperationWindow.operation
{
    public interface IReShareUpdate : IReShare
    {
        bool IsAdmin { get; }
        List<int> SharedWithProject { get; }
        bool ReShareUpdateFile(List<string> addProjectIdList, List<string> removedProjectIdList, string comment);
        bool RevokeSharing();
    }
}
