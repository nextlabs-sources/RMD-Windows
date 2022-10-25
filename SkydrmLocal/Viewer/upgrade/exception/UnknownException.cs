using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.upgrade.exception
{
    public class UnknownException : ViewerSystemException
    {
        public UnknownException()
        {
        }

        public UnknownException(string message) : base(message)
        {
        }
    }
}
