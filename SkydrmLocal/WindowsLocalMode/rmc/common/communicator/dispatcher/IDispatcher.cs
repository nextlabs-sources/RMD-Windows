using SkydrmLocal.rmc.common.communicator.subscription;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.common.communicator.dispatcher
{
    public interface IDispatcher<T>
    {
        void Dispatch(T subscriber, object events);
    }
}
