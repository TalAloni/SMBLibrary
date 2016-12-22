using System;
using System.Collections.Generic;

namespace Utilities
{
    public partial class KeyValuePairList<TKey, TValue> : List<KeyValuePair<TKey, TValue>>
    {
        public bool ContainsKey(TKey key)
        {
            return (this.IndexOf(key) != -1);
        }

        public int IndexOf(TKey key)
        {
            for (int index = 0; index < this.Count; index++)
            {
                if (this[index].Key.Equals(key))
                {
                    return index;
                }
            }

            return -1;
        }

        public TValue ValueOf(TKey key)
        {
            for (int index = 0; index < this.Count; index++)
            {
                if (this[index].Key.Equals(key))
                {
                    return this[index].Value;
                }
            }

            return default(TValue);
        }

        public void Add(TKey key, TValue value)
        {
            this.Add(new KeyValuePair<TKey, TValue>(key, value));
        }

        public List<TKey> Keys
        {
            get
            {
                List<TKey> result = new List<TKey>();
                foreach (KeyValuePair<TKey, TValue> entity in this)
                {
                    result.Add(entity.Key);
                }
                return result;
            }
        }

        public List<TValue> Values
        {
            get
            {
                List<TValue> result = new List<TValue>();
                foreach (KeyValuePair<TKey, TValue> entity in this)
                {
                    result.Add(entity.Value);
                }
                return result;
            }
        }
    }
}
