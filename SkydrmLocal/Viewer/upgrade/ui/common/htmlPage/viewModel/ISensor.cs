using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.upgrade.ui.common.htmlPage.viewModel
{
    public interface ISensor
    {
        event Action<Exception> OnUnhandledExceptionOccurrence;
        event Action OnLoadFileSucceed;
    }
}
