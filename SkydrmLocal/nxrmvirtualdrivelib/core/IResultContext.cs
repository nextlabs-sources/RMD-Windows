using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.core
{
    public interface IResultContext
    {
        void ReportProgress(long total, long completed);

        void ReportStatus(string message, uint code);
    }
}
