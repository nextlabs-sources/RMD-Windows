using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.upgrade.exception
{
    public class FileTypeNoSupportedException : NxlFileException
    {
        public FileTypeNoSupportedException()
        {
        }

        public FileTypeNoSupportedException(string message) : base(message)
        {
        }
    }
}
