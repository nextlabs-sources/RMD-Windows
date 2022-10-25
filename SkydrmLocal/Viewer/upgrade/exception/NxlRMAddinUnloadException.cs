using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.upgrade.exception
{
    public class NxlRMAddinUnloadException : Exception
    {
        public NxlRMAddinUnloadException()
        {

        }

        public NxlRMAddinUnloadException(string message) : base(message)
        {

        }
    }
}
