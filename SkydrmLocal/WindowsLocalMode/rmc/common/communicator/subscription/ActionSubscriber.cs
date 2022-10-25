using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.common.communicator.subscription
{
    internal class ActionSubscriber<TEventBase> : ISubscriber where TEventBase : EventBase
    {
        private readonly Action<TEventBase> action;

        public SubscriptionToken SubscriptionToken { get; }

        public ActionSubscriber(Action<TEventBase> action, SubscriptionToken token)
        {
            this.action = action ?? throw new ArgumentNullException(nameof(action));
            this.SubscriptionToken = token ?? throw new ArgumentNullException(nameof(token));
        }

        public void Invoke(object eventItem)
        {
            if (!(eventItem is TEventBase))
                throw new ArgumentException("Event Item is not the correct type.");

            action.Invoke(eventItem as TEventBase);
        }
    }
}
