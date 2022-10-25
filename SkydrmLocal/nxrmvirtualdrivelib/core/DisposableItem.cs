using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.core
{
    public class DisposableItem<TKey, TValue> : IDisposable where TValue : DisposableItem<TKey, TValue>, new()
    {
        private TKey m_key;
        private ConcurrentDictionary<TKey, TValue> m_pool;
        private bool m_disposable = false;
        private bool disposed = false;

        public TKey Key
        {
            get => m_key;
            set
            {
                m_key = value;
            }
        }

        public ConcurrentDictionary<TKey, TValue> Pool
        {
            get => m_pool;
            set
            {
                m_pool = value;
            }
        }

        public bool Disposable
        {
            get => m_disposable;
            set
            {
                m_disposable = value;
            }
        }

        public static TValue CreateItem(TKey key, ConcurrentDictionary<TKey, TValue> pool)
        {
            TValue val = new TValue
            {
                Key = key,
                Pool = pool,
                Disposable = true
            };
            return val;
        }

        protected virtual void Dispose(bool allow)
        {
            if (!disposed)
            {
                if (!allow || !Disposable || !Pool.TryRemove(Key, out var _))
                {

                }
                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
