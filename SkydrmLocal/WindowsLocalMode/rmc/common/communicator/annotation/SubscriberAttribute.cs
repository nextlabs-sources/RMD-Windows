using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.common.communicator.annotation
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class SubscriberAttribute : Attribute
    {
        private string name;
        private ThreadMode mode;

        public string Name
        {
            get
            {
                return name;
            }
        }

        public ThreadMode Mode
        {
            get
            {
                return mode;
            }
        }

        public SubscriberAttribute(ThreadMode mode)
        {
            this.mode = mode;
        }

        public SubscriberAttribute(string name, ThreadMode mode)
        {
            this.name = name;
            this.mode = mode;
        }
    }
}
