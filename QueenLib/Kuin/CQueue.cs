using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Kuin
{
    public class CQueue<T>: CClass
    {
        private LinkedList<T> list = new LinkedList<T>();
        public CQueue()
        {
        }
        public void Enq(T val)
        {
            list.AddLast(val);
        }
        public T Deq()
        {
            LinkedListNode<T> node = list.First;
            T val = node.Value;
            list.Remove(node);
            return val;
        }
        public T Peek()
        {
            return list.First.Value;
        }

        private class Iterator : IIter<T>
        {
            LinkedList<T> list;
            bool first;
            LinkedListNode<T> node;

            public Iterator(LinkedList<T> list)
            {
                this.list = list;
                first = true;
            }

            public T Current
            {
                get
                {
                    if (first)
                    {
                        throw new NotImplementedException();
                    }
                    return node.Value;
                }
                set
                {
                    if (first)
                    {
                        throw new NotImplementedException();
                    }
                    node.Value = value;
                }
            }

            public bool MoveNext()
            {
                if (first)
                {
                    node = list.First;
                    first = false;
                }
                else
                {
                    node = node.Next;
                }
                return node != null;
            }
        }

        public IIter<T> GetIter()
        {
            return new Iterator(list);
        }
    }
}
