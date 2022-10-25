using SkydrmLocal.rmc.common.communicator.annotation;
using SkydrmLocal.rmc.common.communicator.exception;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static SkydrmLocal.rmc.common.communicator.subscription.IntanceSubscriber;

namespace SkydrmLocal.rmc.common.communicator.subscription
{
    internal sealed class SubscriberRegistry
    {
        private static readonly Dictionary<Type, List<SubscriptionMethod>> subscriberMethodsCache = new Dictionary<Type, List<SubscriptionMethod>>();
        private static readonly object subscriberMethodsCache_lock = new object();

        private static readonly Dictionary<Type, List<IntanceSubscriber>> instanceSubscribers = new Dictionary<Type, List<IntanceSubscriber>>();
        private static readonly object instanceSubscribers_lock = new object();

        private static readonly Dictionary<Type, List<ISubscriber>> eventSubscribers = new Dictionary<Type, List<ISubscriber>>();
        private static readonly object eventSubscribers_lock = new object();

        private static readonly Dictionary<Type, SubscriptionToken> eventTokens = new Dictionary<Type, SubscriptionToken>();
        private static readonly object eventToken_lock = new object();

        public void Register(object classIntance)
        {
            var listenerMethods = FindAllSubscribers(classIntance);
            foreach (var entry in listenerMethods)
            {
                Type eventType = entry.Key;
                List<IntanceSubscriber> eventListeners = entry.Value;

                List<IntanceSubscriber> eventSubscribers = null;

                lock (instanceSubscribers_lock)
                {
                    if (instanceSubscribers.ContainsKey(eventType))
                    {
                        eventSubscribers = instanceSubscribers[eventType];
                    }

                    if (eventSubscribers == null)
                    {
                        List<IntanceSubscriber> newList = new List<IntanceSubscriber>();
                        instanceSubscribers.Add(eventType, newList);
                        eventSubscribers = newList;
                    }

                    eventSubscribers.AddRange(eventListeners);
                }
            }
        }

        public void Register<TEventBase>(Action<TEventBase> action) where TEventBase : EventBase
        {
            if (action == null)
                throw new EventBusException(nameof(action));

            SubscriptionToken token = null;
            lock (eventSubscribers_lock)
            {
                if (!eventSubscribers.ContainsKey(typeof(TEventBase)))
                    eventSubscribers.Add(typeof(TEventBase), new List<ISubscriber>());

                token = new SubscriptionToken(typeof(TEventBase));
                eventSubscribers[typeof(TEventBase)].Add(new ActionSubscriber<TEventBase>(action, token));
            }
            
            if(token == null)
            {
                throw new EventBusException(nameof(token));
            }

            lock(eventToken_lock)
            {
                eventTokens.Add(typeof(TEventBase), token);
            }
        }

        public void UnRegister(object classIntance)
        {
            var listenerMethods = FindAllSubscribers(classIntance);
            foreach (var entry in listenerMethods)
            {
                Type eventType = entry.Key;
                List<IntanceSubscriber> eventListeners = entry.Value;

                lock(instanceSubscribers_lock)
                {
                    List<IntanceSubscriber> currentSubscribers = instanceSubscribers[eventType];
                    //If removeAll returns size != 0, all we really know is that at least one subscriber was
                    //removed.
                    if (currentSubscribers == null || currentSubscribers.RemoveAll(i => eventListeners.Contains(i)) != 0)
                    {
                        throw new EventBusException(
                            "Missing event subscriber for an annotated method. Is " + classIntance + " registered?");
                    }
                }
            }
        }

        public void UnRegister<TEventBase>(Action<TEventBase> action) where TEventBase : EventBase
        {
            if (action == null)
                throw new EventBusException(nameof(action));

            SubscriptionToken token = null;

            lock (eventToken_lock)
            {
                if(eventTokens.ContainsKey(typeof(TEventBase)))
                {
                    token = eventTokens[typeof(TEventBase)];
                }
            }
            //Remove listener from eventSubscribers
            UnRegisterInternal(token);
            //Remove register token from eventTokens.
            eventTokens.Remove(typeof(TEventBase));
        }

        public List<IntanceSubscriber> GetSubscribers(object events)
        {
            if (events == null)
                throw new EventBusException(nameof(events));

            List<IntanceSubscriber> subscriberRet = new List<IntanceSubscriber>();
            Type eventType = events.GetType();

            lock (instanceSubscribers_lock)
            {

                subscriberRet = instanceSubscribers[eventType];
            }

            return subscriberRet;
        }

        public List<ISubscriber> GetSubscribers<TEventBase>(TEventBase eventType) where TEventBase : EventBase
        {
            if (eventType == null)
                throw new EventBusException(nameof(eventType));

            List<ISubscriber> allSubscriptions = new List<ISubscriber>();

            lock (eventSubscribers_lock)
            {
                if (eventSubscribers.ContainsKey(typeof(TEventBase)))
                    allSubscriptions = eventSubscribers[typeof(TEventBase)];
            }

            return allSubscriptions;
        }

        public static void CheckArgument(bool b, string errorMessageTemplate, object p1, int p2)
        {
            if (!b)
            {
                throw new EventBusException(string.Format(errorMessageTemplate, p1, p2));
            }
        }

        private Dictionary<Type, List<IntanceSubscriber>> FindAllSubscribers(object classInstance)
        {
            Dictionary<Type, List<IntanceSubscriber>> methodsInListener = new Dictionary<Type, List<IntanceSubscriber>>();

            foreach (var info in GetAnnotatedMethods(classInstance.GetType()))
            {
                ParameterInfo[] parameterInfos = info.Info.GetParameters();
                Type eventType = parameterInfos[0].ParameterType;

                if (!methodsInListener.ContainsKey(eventType))
                {
                    methodsInListener.Add(eventType, new List<IntanceSubscriber>());
                }

                methodsInListener[eventType].Add(Create(classInstance, info));
            }

            return methodsInListener;
        }

        private List<SubscriptionMethod> GetAnnotatedMethodsNotCached(Type clazz)
        {
            Dictionary<MethodIdentifier, SubscriptionMethod> identifiers = new Dictionary<MethodIdentifier, SubscriptionMethod>();
            foreach (MethodInfo method in clazz.GetMethods())
            {
                var sa = GetAnnotationPresent(method);
                if (sa != null)
                {
                    ParameterInfo[] parameterTypes = method.GetParameters();
                    CheckArgument(parameterTypes.Length == 1, "Method %s has @Subscribe annotation but has %s parameters."
                  + "Subscriber methods must have exactly 1 parameter.", method.Name, parameterTypes.Length);

                    MethodIdentifier ident = new MethodIdentifier(method);

                    if (!identifiers.ContainsKey(ident))
                    {
                        identifiers.Add(ident, new SubscriptionMethod(sa.Mode, method));
                    }
                }
            }
            return new List<SubscriptionMethod>(identifiers.Values);
        }

        private SubscriberAttribute GetAnnotationPresent(MethodInfo m)
        {
            return m.GetCustomAttributes(false).OfType<SubscriberAttribute>().FirstOrDefault();
        }

        private List<SubscriptionMethod> GetAnnotatedMethods(Type clazz)
        {
            List<SubscriptionMethod> cachedMethods = null;

            lock (subscriberMethodsCache_lock)
            {
                if (subscriberMethodsCache.ContainsKey(clazz))
                {
                    cachedMethods = subscriberMethodsCache[clazz];
                }
            }

            if (cachedMethods == null)
            {
                List<SubscriptionMethod> localMethods = GetAnnotatedMethodsNotCached(clazz);

                if (localMethods == null || localMethods.Count == 0)
                {
                    //Error.
                    throw new EventBusException("No local register method found.");
                }

                lock (subscriberMethodsCache_lock)
                {
                    subscriberMethodsCache.Add(clazz, localMethods);
                }

                cachedMethods = localMethods;
            }

            return cachedMethods;
        }

        private void UnRegisterInternal(SubscriptionToken token)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));

            lock (eventSubscribers_lock)
            {
                if (eventSubscribers.ContainsKey(token.EventItemType))
                {
                    var allSubscriptions = eventSubscribers[token.EventItemType];
                    var subscriptionToRemove = allSubscriptions.FirstOrDefault(x => x.SubscriptionToken.Token == token.Token);
                    if (subscriptionToRemove != null)
                        eventSubscribers[token.EventItemType].Remove(subscriptionToRemove);
                }
            }
        }

        internal class MethodIdentifier
        {
            private string name;
            private List<ParameterInfo> parameters;

            internal MethodIdentifier(MethodInfo method)
            {
                this.name = method.Name;
                this.parameters = method.GetParameters().ToList();
            }

            public string Name
            {
                get { return name; }
            }

            public List<ParameterInfo> Parameters
            {
                get
                {
                    return parameters;
                }
            }

            public override bool Equals(object o)
            {
                if (o == null)
                {
                    return false;
                }

                MethodIdentifier other = o as MethodIdentifier;

                return this.name == other.name &&
                    this.parameters == other.parameters;
            }

            public override int GetHashCode()
            {
                return this.name.GetHashCode() * 31 +
                    this.parameters.GetHashCode() * 31;
            }
        }
    }
}
