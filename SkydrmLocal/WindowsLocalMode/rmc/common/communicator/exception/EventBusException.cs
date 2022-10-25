using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.common.communicator.exception
{
    public class EventBusException : Exception
    {
        #region private region
        private readonly string message;
        private readonly Exception e;
        #endregion

        public override string Message
        {
            get
            {
                return message;
            }
        }

        public Exception Detail
        {
            get
            {
                return e;
            }
        }

        public EventBusException(string msg) : base(msg)
        {
            this.message = msg;
        }

        public EventBusException(Exception inner) : base("An error occured.", inner)
        {
            this.e = inner;
        }

        public EventBusException(string msg, Exception inner) : base(msg, inner)
        {
            this.message = msg;
            this.e = inner;
        }
    }
}
