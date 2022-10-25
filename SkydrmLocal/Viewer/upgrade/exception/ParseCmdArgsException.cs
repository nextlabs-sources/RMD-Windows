using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.upgrade.exception
{
    public class ParseCmdArgsException : ViewerSystemException
    {
        public ParseCmdArgsException()
        {
        }

        public ParseCmdArgsException(string message) : base(message)
        {
        }
    }
}
