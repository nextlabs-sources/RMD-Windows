using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmDesktop.rmc.ui.windows.renameFileWindow.rename
{
    public interface IRenameFile
    {
        string AdviceName { get; }
        bool Rename(string newName);
        bool RenameResult { get; }
    }
}
