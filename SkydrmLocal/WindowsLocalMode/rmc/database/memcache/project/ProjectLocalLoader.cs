using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkydrmLocal.rmc.database.memcache.project
{
    class ProjectLocalLoader
    {
        private static ProjectLocalLoader mInstance;
        private static readonly object Singleton_Lock = new object();

        private ProjectLocalLoader() {
        }

        public static ProjectLocalLoader GetInstance()
        {
            if(mInstance == null)
            {
                lock(Singleton_Lock)
                {
                    if(mInstance==null)
                    {
                        mInstance = new ProjectLocalLoader();
                    }
                }
            }
            return mInstance;
        }

        public void Load(Config config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("Parameter Config must not be null when invoke Method Load.");
            }
            ThreadPool.QueueUserWorkItem(WaitCallback, config);
        }

        private void WaitCallback(object state)
        {
            Config config = state as Config;
            object ret = config.Caller?.Invoke(config.Bundle);
            config.Callback?.Invoke(ret);
        }
    }

    public delegate object Caller(object state);
    public delegate void Callback(object state);

    #region LoaderConfig
    class Config
    {
        public Caller Caller { get; }
        public object Bundle { get; }
        public Callback Callback { get; }

        public Config(Caller caller, object bundle, Callback callback)
        {
            Caller = caller;
            Bundle = bundle;
            Callback = callback;
        }
    }
    #endregion
}
