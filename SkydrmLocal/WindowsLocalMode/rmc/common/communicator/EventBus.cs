using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkydrmLocal.rmc.common.communicator.dispatcher;
using SkydrmLocal.rmc.common.communicator.exception;
using SkydrmLocal.rmc.common.communicator.subscription;

namespace SkydrmLocal.rmc.common.communicator
{
    class EventBus
    {
        #region Private Region
        private static EventBus instance;
        private static readonly object instanceLock = new object();

        private static readonly SubscriberRegistry subscribers = new SubscriberRegistry();
        
        private EventBus()
        {

        }
        #endregion

        public static EventBus GetInstance()
        {
            if (instance == null)
            {
                lock (instanceLock)
                {
                    if (instance == null)
                    {
                        instance = new EventBus();
                    }
                }
            }
            return instance;
        }

        public void Register(object classInstance)
        {
            subscribers.Register(classInstance);
        }

        public void Register<TEvent>(Action<TEvent> action) where TEvent : EventBase
        {
            subscribers.Register(action);
        }

        public void UnRegister(object classInstance)
        {
            subscribers.UnRegister(classInstance);
        }

        public void Post(object eventItem)
        {
            if (eventItem == null)
                throw new EventBusException(nameof(eventItem));

            var eventSubscribers = subscribers.GetSubscribers(eventItem);

            EventPoster.PostEvents(eventSubscribers.GetEnumerator(), eventItem);
        }

        public void Post<TEventBase>(TEventBase eventItem, bool mainthread) where TEventBase : EventBase
        {
            if (eventItem == null)
                throw new EventBusException(nameof(eventItem));

            var eventSubscribers = subscribers.GetSubscribers(eventItem);

            EventPoster.PostEvents(eventSubscribers.GetEnumerator(), eventItem, mainthread);
        }
    }
}
