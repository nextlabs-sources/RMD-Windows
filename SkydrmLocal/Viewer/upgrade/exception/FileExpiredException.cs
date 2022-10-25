using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.upgrade.exception
{
    public class FileExpiredException : NxlFileException
    {
        public FileExpiredException()
        {
        }

        public FileExpiredException(string message) : base(message)
        {

        }

    }
}
