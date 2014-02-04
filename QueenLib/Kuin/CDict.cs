using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Kuin
{
    public class CDictPair<TKey, TValue> : CClass
    {
        public TKey Key { get; set; }
        public TValue Value { get; set; }
    }

    public class CDict<TKey, TValue>: CClass
    {
        private Dictionary<TKey, CDictPair<TKey, TValue>> dict = new Dictionary<TKey, CDictPair<TKey, TValue>>();

        public TValue this[TKey key]
        {
            get { return dict[key].Value; }
            set {
                CDictPair<TKey, TValue> pair;
                if (dict.TryGetValue(key, out pair))
                {
                    pair.Value = value;
                }
                else
                {
                    pair = new CDictPair<TKey, TValue>();
                    pair.Key = key;
                    pair.Value = value;
                    dict[key] = pair;
                }
            }
        }

        public long Len()
        {
            return dict.Count;
        }

        public TValue Get(TKey key)
        {
            return this[key];
        }
    }
}
