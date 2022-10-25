using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.upgrade.exception
{
    public class LogInitializeException : ViewerSystemException
    {
        public LogInitializeException()
        {
        }

        public LogInitializeException(string message) : base(message)
        {
        }
    }
}
