using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.core
{
    public interface ITransferData : IResultContext
    {
        void Transfer(byte[] buffer, long offset, long length);
    }
}
