using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Viewer.upgrade.ui.common.previewer2.viewModel
{
    public interface ISensor
    {
        event Action<Window> OnOverlayWindowLoaded;
        event Action<Exception> OnUnhandledExceptionOccurrence;
        event Action OnLoadFileSucceed;
    }
}
