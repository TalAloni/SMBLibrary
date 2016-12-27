using System;
using System.Collections.Generic;
using System.Threading;
using System.Text;

namespace Utilities
{
    public class BlockingQueue<T>
    {
        private Queue<T> m_queue = new Queue<T>();
        private int m_count = 0;
        private bool m_stopping;

        public void Enqueue(T item)
        {
            lock (m_queue)
            {
                m_queue.Enqueue(item);
                m_count++;
                if (m_queue.Count == 1)
                {
                    Monitor.Pulse(m_queue);
                }
            }
        }

        public void Enqueue(List<T> items)
        {
            if (items.Count == 0)
            {
                return;
            }
            lock (m_queue)
            {
                foreach (T item in items)
                {
                    m_queue.Enqueue(item);
                    m_count++;
                }
                if (m_queue.Count == items.Count)
                {
                    Monitor.Pulse(m_queue);
                }
            }
        }

        /// <returns>Will return false if the BlockingQueue is stopped</returns>
        public bool TryDequeue(out T item)
        {
            lock (m_queue)
            {
                while (m_queue.Count == 0)
                {
                    Monitor.Wait(m_queue);
                    if (m_stopping)
                    {
                        item = default(T);
                        return false;
                    }
                }

                item = m_queue.Dequeue();
                m_count--;
                return true;
            }
        }

        public void Stop()
        {
            lock (m_queue)
            {
                m_stopping = true;
                Monitor.PulseAll(m_queue);
            }
        }

        public int Count
        {
            get
            {
                return m_count;
            }
        }
    }
}
