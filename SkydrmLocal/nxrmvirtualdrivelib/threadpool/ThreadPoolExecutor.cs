using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.threadpool
{
    public class ThreadPoolExecutor
    {
        public delegate object Caller(object state);
        public delegate void Callback(object state);

        public static void Execute(Config config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("Parameter Config must not be null when invoke Method Load.");
            }
            ThreadPool.QueueUserWorkItem(WaitCallback, config);
        }

        public static async Task Execute<Item>(IEnumerable<Item> items, Func<Item, Task> itemHandler,
            Action<Item, Exception> exceptionHandler, int initialCount = 1000, CancellationToken cancellationToken = default)
        {
            SemaphoreSlim semaphore = new SemaphoreSlim(initialCount);
            try
            {
                List<Task> list = new List<Task>();
                foreach (var f in items)
                {
                    try
                    {
                        semaphore.Wait(cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {

                    }
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    Task item = Task.Run(async delegate
                    {
                        try
                        {
                            await itemHandler(f);
                        }
                        catch (Exception e)
                        {
                            Exception arg = e;
                            exceptionHandler(f, arg);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, cancellationToken);

                    list.Add(item);
                }
                try
                {
                    await Task.WhenAll(list.ToArray());
                }
                catch (TaskCanceledException)
                {

                }
            }
            finally
            {
                if (semaphore != null)
                {
                    semaphore.Dispose();
                }
            }
        }

        private static void WaitCallback(object state)
        {
            Config config = state as Config;
            try
            {
                object ret = config.Caller?.Invoke(config.Bundle);
                config.Callback?.Invoke(ret);
            }
            catch (Exception e)
            {
                config.Callback?.Invoke(e);
            }
        }

        public class Config
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

    }
}
