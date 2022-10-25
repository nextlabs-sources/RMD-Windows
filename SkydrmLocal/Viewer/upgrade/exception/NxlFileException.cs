using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.upgrade.exception
{
    public class NxlFileException : Exception
    {
        public NxlFileException()
        {

        }

        public NxlFileException(string message) : base(message)
        {

        }
    }
}
