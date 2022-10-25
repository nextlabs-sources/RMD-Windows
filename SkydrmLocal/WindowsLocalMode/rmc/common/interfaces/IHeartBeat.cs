using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.common.interfaces
{
    // system point to allow sync at background by heart beat
    public interface IHeartBeat
    {
        void OnHeartBeat();
    }
}
