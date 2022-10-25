using SkydrmLocal.rmc.common.communicator.annotation;
using SkydrmLocal.rmc.common.communicator.exception;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.common.communicator.subscription
{
    internal class IntanceSubscriber : ISubscriber
    {
        private readonly object target;
        private readonly SubscriptionMethod method;

        public ThreadMode Mode
        {
            get
            {
                return method.Mode;
            }
        }

        public SubscriptionToken SubscriptionToken { get; }

        public static IntanceSubscriber Create(object subsriber, SubscriptionMethod method)
        {
            return new IntanceSubscriber(subsriber, method);
        }

        private IntanceSubscriber(object subsriber, SubscriptionMethod method)
        {
            this.SubscriptionToken = new SubscriptionToken(subsriber.GetType());
            this.target = subsriber;
            this.method = method;
        }

        public void Invoke(object paramEvent)
        {
            try
            {
                InvokeSubscriberMethod(target, method.Info.Name, paramEvent);
            }
            catch (Exception e)
            {
                throw new EventBusException(e);
            }
        }

        private void InvokeSubscriberMethod(object classInstance, string methodName, object parameterObject = null)
        {

            if (classInstance != null)
            {
                Type typeInstance = classInstance.GetType();

                MethodInfo methodInfo = typeInstance.GetMethod(methodName);
                ParameterInfo[] parameterInfo = methodInfo.GetParameters();
                //object classInstance = Activator.CreateInstance(typeInstance, null);

                if (parameterInfo.Length == 0)
                {
                    // there is no parameter we can call with 'null'
                    var result = methodInfo.Invoke(classInstance, null);
                }
                else
                {
                    var result = methodInfo.Invoke(classInstance, new object[] { parameterObject });
                }
            }
        }

        public override bool Equals(object o)
        {
            if (o == null)
            {
                return false;
            }

            IntanceSubscriber other = o as IntanceSubscriber;

            return this.SubscriptionToken.Token == other.SubscriptionToken.Token;
        }

        public override int GetHashCode()
        {
            return this.SubscriptionToken.Token.GetHashCode() * 31;
        }

        public class SubscriptionMethod
        {
            private readonly ThreadMode mode;
            private readonly MethodInfo methodInfo;

            public SubscriptionMethod(ThreadMode mode, MethodInfo info)
            {
                this.mode = mode;
                this.methodInfo = info;
            }

            public ThreadMode Mode
            {
                get
                {
                    return mode;
                }
            }

            public MethodInfo Info
            {
                get
                {
                    return methodInfo;
                }
            }
        }
    }
}
