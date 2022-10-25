using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.upgrade.exception
{
    public class NotAuthorizedException : NxlFileException
    {
        public NotAuthorizedException()
        {

        }

        public NotAuthorizedException(string message) : base(message)
        {

        }

    }
}
