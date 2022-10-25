using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.upgrade.ui.common.viewerWindow.view;

namespace Viewer.upgrade.ui.common.viewerWindow.viewModel
{
    public class ErrorViewModel : AbsViewModel
    {
        private string mErrorMsg;
        private string mFileName;

        public ErrorViewModel(string errorMsg, string fileName, ViewerWindow viewerWindow) : base(viewerWindow)
        {
            mErrorMsg = errorMsg;
            mFileName = fileName;
            mViewerWindow = viewerWindow;
        }

        public override void Window_Closed()
        {
           
        }

        public override void Window_ContentRendered()
        {
        
        }

        public override void Window_Loaded()
        {
            LoadErrorPage(mFileName, mErrorMsg);
        }
    }
}
