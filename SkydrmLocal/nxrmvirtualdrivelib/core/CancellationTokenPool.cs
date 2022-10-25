using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace nxrmvirtualdrivelib.core
{
    public class CancellationTokenPool
    {
        private readonly BlockingCollection<int> m_pool;

        public CancellationTokenPool(int capacity)
        {
            this.m_pool = new BlockingCollection<int>(capacity);
        }

        public CancellationTokenWrapper AddOne(CancellationToken token)
        {
            m_pool.Add(m_pool.Count + 1, token);

            return new CancellationTokenWrapper(this, token);
        }

        public void TakeOne(CancellationToken token)
        {
            try
            {
                m_pool.Take(token);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
