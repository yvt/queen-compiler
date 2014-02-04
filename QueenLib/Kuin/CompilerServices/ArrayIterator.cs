using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Kuin.CompilerServices
{
    public class ArrayIterator<T>: IIter<T>
    {
        T[] arr;
        int index = -1;
        public ArrayIterator(T[] arr)
        {
            this.arr = arr;
        }

        public T Current
        {
            get
            {
                return arr[index];
            }
            set
            {
                arr[index] = value;
            }
        }

        public bool MoveNext()
        {
            index += 1;
            return index < arr.Length;
        }
    }
}
