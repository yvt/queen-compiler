using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Kuin
{
    public class CClass: IComparable<CClass>, IEquatable<CClass>
    {
        public virtual long Cmp(CClass other)
        {
            throw new NotImplementedException();
        }

        public virtual byte[] ToBins()
        {
            // TODO: implement ToBins?
            throw new NotImplementedException();
        }

        public int CompareTo(CClass other)
        {
            return (int)Cmp(other);
        }

        public bool Equals(CClass other)
        {
            return Cmp(other) == 0;
        }
    }
}
