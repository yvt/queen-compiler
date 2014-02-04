using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Kuin
{
    // read/write iterator to meet the requirements of the language's "foreach" statement.
    public interface IIter<T>
    {
        T Current { get; set; }
        bool MoveNext();
    }

    internal class ListIter<T> : IIter<T>
    {
        IList<T> list;
        bool first = true;
        int index = 0;

        public ListIter(IList<T> list)
        {
            this.list = list;
        }


        public T Current
        {
            get
            {
                if (first) throw new InvalidOperationException();
                return list[index];
            }
            set
            {
                if (first) throw new InvalidOperationException();
                list[index] = value;
            }
        }

        public bool MoveNext()
        {
            if (first)
            {
                first = false;
            }
            else
            {
                index += 1;
            }
            return index < list.Count;
        }
    }
}
