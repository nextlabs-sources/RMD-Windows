using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.upgrade.ui.common.hoops.viewModel
{
    public interface ISensor
    {
        event Action<Exception> OnUnhandledExceptionOccurrence;
        event Action OnLoadFileSucceed;
        event Action<bool> EndPrint;
        event Action<System.Windows.Forms.PrintDialog> BeforePrint;
    }
}
