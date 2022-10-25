using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.upgrade.exception
{
    public class ViewerSystemException : Exception
    {
        public ViewerSystemException()
        {

        }

        public ViewerSystemException(string message) : base(message)
        {

        }
    }
}
