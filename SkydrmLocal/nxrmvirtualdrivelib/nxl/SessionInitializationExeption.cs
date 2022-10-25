using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.nxl
{
    public class SessionInitializationExeption : Exception
    {
        public SessionInitializationExeption()
        {

        }

        public SessionInitializationExeption(string message) : base(message)
        {

        }

        public SessionInitializationExeption(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}
