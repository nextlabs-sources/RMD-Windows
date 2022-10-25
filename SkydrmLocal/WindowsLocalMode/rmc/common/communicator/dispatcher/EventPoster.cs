using SkydrmLocal.rmc.common.communicator.annotation;
using SkydrmLocal.rmc.common.communicator.exception;
using SkydrmLocal.rmc.common.communicator.subscription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.common.communicator.dispatcher
{
    class EventPoster
    {
        private readonly static UIDispatcher uiDispatcher = new UIDispatcher();
        private readonly static AysncDispatcher aysncDispatcher = new AysncDispatcher();

        public static void PostEvents(IEnumerator<IntanceSubscriber> iterator, object events)
        {
            //Sanity check.
            if (iterator == null || events == null)
            {
                throw new EventBusException("Argument is null when invoke PostEvents.");
            }

            while (iterator.MoveNext())
            {
                IntanceSubscriber subscriber = iterator.Current;
                //Get the subscription mode.
                var mode = subscriber.Mode;
                //Dispatch invoking method according mode.
                switch (mode)
                {
                    case ThreadMode.MAIN:
                        Dispatch(uiDispatcher, subscriber, events);
                        break;
                    case ThreadMode.ASYNC:
                        Dispatch(aysncDispatcher, subscriber, events);
                        break;
                    default:
                        throw new EventBusException(string.Format("Unrecognized thread mode {0} delivered to post events.", mode));
                }
            }
        }

        public static void PostEvents(IEnumerator<ISubscriber> iterator, EventBase events, bool mainthread)
        {
            //Sanity check.
            if (iterator == null || events == null)
            {
                throw new EventBusException("Argument is null when invoke PostEvents.");
            }

            while (iterator.MoveNext())
            {
                var subscriber = iterator.Current;
                try
                {
                    if (mainthread)
                    {
                        Dispatch(uiDispatcher, subscriber, events);
                    }
                    else
                    {
                        Dispatch(aysncDispatcher, subscriber, events);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        private static void Dispatch(IDispatcher<ISubscriber> dispatcher, ISubscriber subscriber, object events)
        {
            dispatcher.Dispatch(subscriber, events);
        }
    }
}
