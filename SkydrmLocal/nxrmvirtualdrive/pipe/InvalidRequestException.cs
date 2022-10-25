using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrive.pipe
{
    public class InvalidRequestException : Exception
    {
        private int m_statusCode;

        public int StatusCode
        {
            get => m_statusCode;
        }

        public InvalidRequestException(int statusCode, string message) : base(message)
        {
            this.m_statusCode = statusCode;
        }
    }
}
