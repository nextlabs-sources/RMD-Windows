using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.upgrade.exception
{
    public class EssentialInitializeException : ViewerSystemException
    {
        public EssentialInitializeException()
        {

        }
        public EssentialInitializeException(string message) : base(message)
        {

        }
    }
}
