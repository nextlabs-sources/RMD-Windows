using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Viewer.upgrade.ui.common.edgeWebView2Page.viewModel
{
    public interface ISensor
    {
        event Action<Exception> OnUnhandledExceptionOccurrence;
        event Action OnLoadFileSucceed;
    }
}
