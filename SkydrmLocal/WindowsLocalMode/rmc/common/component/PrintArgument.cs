using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.helper
{
    [Serializable]
    public class PrintArgument
    {
        private int shutdown = 0;

        private int intPtr;

        private string filePath;

        private string watermark;

        private string displayWatermark;

        public int Shutdown { get => shutdown; set => shutdown = value; }
        public int IntPtr { get => intPtr; set => intPtr = value; }
        public string FilePath { get => filePath; set => filePath = value; }
        public string Watermark { get => watermark; set => watermark = value; }
        public string DisplayWatermark { get => displayWatermark; set => displayWatermark = value; }
    }
}
