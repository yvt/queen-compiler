using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public sealed class ITNullType: ITType
    {
        public override bool CanBeCastedFrom(ITType otherType, bool implictCast)
        {
            return false;
        }
        public override bool CanBeCastedTo(ITType otherType, bool implicitCast)
        {
            return !otherType.IsValueType;
        }
        public override bool IsValueType
        {
            get { return false; }
        }

        
        public ITNullType(IntermediateCompiler iCompiler)
            : base(iCompiler)
        {
        }

        public override ITType Superclass
        {
            get { return null; }
        }

        public override ITType[] Interfaces
        {
            get { return null; }
        }



        public override bool IsSealed()
        {
            return true;
        }

        public override bool IsAbstract()
        {
            return true;
        }

        public override string ToString()
        {
            return "null";
        }

        public override bool Equals(object obj)
        {
            return obj is ITNullType;
        }

        public override int GetHashCode()
        {
            return 234356532;
        }
    }
}
