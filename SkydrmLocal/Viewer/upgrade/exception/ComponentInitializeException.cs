using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.upgrade.exception
{
    public class ComponentInitializeException:Exception
    {
        public ComponentInitializeException()
        {
        }

        public ComponentInitializeException(string message) : base(message)
        {

        }
    }
}
