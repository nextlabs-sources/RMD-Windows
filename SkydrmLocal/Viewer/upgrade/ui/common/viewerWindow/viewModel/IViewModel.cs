using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.upgrade.ui.common.viewerWindow.viewModel
{
    public interface IViewModel
    {
          void Window_Closed();

          void Window_Loaded();

          void Window_ContentRendered();

    }
}
