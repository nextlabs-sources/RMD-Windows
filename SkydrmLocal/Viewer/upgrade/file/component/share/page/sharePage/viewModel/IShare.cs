using SkydrmLocal.rmc.sdk;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.upgrade.file.component.share.page.sharePage.viewModel
{
    public interface IShare
    {
        bool IsBusy { get; }
        void Share(NxlFileFingerPrint nxlFileFingerPrint, List<string> sharedEmailLists, string comment);
    }

    public class ShareResult
    {
        public int Code;
        public object Result;
        public Exception Exception;
    }
}
