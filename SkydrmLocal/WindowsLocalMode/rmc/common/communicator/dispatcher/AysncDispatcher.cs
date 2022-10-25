using SkydrmLocal.rmc.common.communicator.subscription;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.common.communicator.dispatcher
{
    class AysncDispatcher : IDispatcher<ISubscriber>
    {
        public void Dispatch(ISubscriber subscriber, object events)
        {
            ThreadPool.QueueUserWorkItem((paramEvent) =>
            {
                subscriber.Invoke(paramEvent);
            }, events);
        }
    }
}
