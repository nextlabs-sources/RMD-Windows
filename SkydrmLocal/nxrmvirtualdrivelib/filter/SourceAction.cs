using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.filter
{
    public enum SourceAction
    {
        CREATE,
        UPDATE,
        DELETE,
        DELETE_COMPLETION,
        MOVE,
        MOVE_COMPLETION,
    }
}
