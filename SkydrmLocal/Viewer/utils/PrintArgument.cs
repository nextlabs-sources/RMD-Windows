using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.utils
{
    [Serializable]
    public class PrintArgument
    {
        public int IntPtrOfOverlayWindow { get; set; }

        public int IntPtrOfViewerWindow { get; set; }

        public string CopyedFilePath { get; set; }

        public string AdhocWatermark { get; set; }
    }
}
