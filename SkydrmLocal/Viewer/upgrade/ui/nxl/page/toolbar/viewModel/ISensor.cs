using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.upgrade.ui.nxl.page.toolbar.viewModel
{
    public interface ISensor
    {
         event Action OnClickFileInfo;
         event Action OnClickPrint;
         event Action OnClickEdit;
         event Action OnClickShare;
         event Action OnClickExport;
         event Action OnClickExtract;
         event Action OnClickRightRotate;
         event Action OnClickLeftRotate;
         event Action OnClickProtect;
         event Action OnClickReset;
    }
}
