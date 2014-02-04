using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Kuin
{
    public class CStack<T>: CClass
    {
        private List<T> list = new List<T>();
        public CStack()
        {
        }
        public void Push(T val)
        {
            list.Add(val);
        }
        public T Pop()
        {
            T val = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
            return val;
        }
        public T Peek()
        {
            return list[list.Count - 1];
        }

        public IIter<T> GetIterator()
        {
            return new ListIter<T>(list);
        }
    }
}
