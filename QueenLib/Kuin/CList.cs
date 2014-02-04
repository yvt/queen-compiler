using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Kuin
{
    public class CList<T>: CClass
    {
        private LinkedList<T> list = new LinkedList<T>();
        private LinkedListNode<T> node = null;

        public void Add(T v)
        {
            list.AddLast(v);
        }

        public void Head()
        {
            node = list.First;
        }

        public void Tail()
        {
            node = list.Last;
        }

        public void Next()
        {
            node = node.Next;
        }

        public void Prev()
        {
            node = node.Previous;
        }

        public T Get()
        {
            // TODO: what to happen in case of null node?
            return node.Value;
        }

        public void Set(T val)
        {
            node.Value = val;
        }

        public bool ChkEnd()
        {
            return node == null;
        }

        public void Del()
        {
            var nxt = node.Next;
            list.Remove(node);
            node = nxt;
        }

        public void Ins(T val)
        {
            list.AddBefore(node, val);
        }

        private class Iterator : IIter<T>
        {
            CList<T> list;
            bool first;
            public Iterator(CList<T> list)
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
                    return list.Get();
                }
                set
                {
                    if (first)
                    {
                        throw new NotImplementedException();
                    }
                    list.Set(value);
                }
            }

            public bool MoveNext()
            {
                if (first)
                {
                    list.Head();
                    first = false;
                }
                else
                {
                    list.Next();
                }
                return !list.ChkEnd();
            }
        }

        public IIter<T> GetIter()
        {
            return new Iterator(this);
        }
    }
}
