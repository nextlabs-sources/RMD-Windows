using SkydrmLocal.rmc.common.communicator.subscription;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SkydrmLocal.rmc.common.communicator.dispatcher
{
    class UIDispatcher : IDispatcher<ISubscriber>
    {
        public void Dispatch(ISubscriber subscriber, object events)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                new Action(() =>
            {
                subscriber.Invoke(events);
            }));
        }
    }
}
