using System;
using System.Collections.Generic;
using System.Text;

namespace Utilities
{
    /// <summary>
    /// Based on:
    /// http://stackoverflow.com/questions/10966331/two-way-bidirectional-dictionary-in-c
    /// </summary>
    public class Map<T1, T2>
    {
        private Dictionary<T1, T2> m_forward = new Dictionary<T1, T2>();
        private Dictionary<T2, T1> m_reverse = new Dictionary<T2, T1>();

        public Map()
        {
            m_forward = new Dictionary<T1, T2>();
            m_reverse = new Dictionary<T2, T1>();
        }

        public void Add(T1 key, T2 value)
        {
            m_forward.Add(key, value);
            m_reverse.Add(value, key);
        }

        public bool ContainsKey(T1 key)
        {
            return m_forward.ContainsKey(key);
        }

        public bool ContainsValue(T2 value)
        {
            return m_reverse.ContainsKey(value);
        }

        public T2 this[T1 key]
        {
            get
            {
                return m_forward[key];
            }
        }

        public T1 GetKey(T2 value)
        {
            return m_reverse[value];
        }
    }
}
