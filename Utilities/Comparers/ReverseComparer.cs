using System;
using System.Collections.Generic;
using System.Text;

namespace Utilities
{
    public class ReverseComparer<T> : IComparer<T>
    {
        private IComparer<T> m_comparer;

        public ReverseComparer(IComparer<T> comparer)
        {
            m_comparer = comparer;
        }

        public int Compare(T x, T y)
        {
            return m_comparer.Compare(y, x);
        }
    }
}
