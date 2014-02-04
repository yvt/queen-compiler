using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public class ITArrayType: ITType
    {
        public ITType ElementType { get; set; }

        public override bool IsValueType
        {
            get { return false; }
        }

        public override ITType Superclass
        {
            get { return null; }
        }

        private static ITType[] interfaces = new ITType[0];
        public override ITType[] Interfaces
        {
            get { return interfaces; }
        }

        public int Dimensions { get; set; }

        
        public ITArrayType(IntermediateCompiler iCompiler)
            : base(iCompiler)
        {
        }

        public override bool Equals(object obj)
        {
            if (obj is ITArrayType)
            {
                return ((ITArrayType)obj).ElementType.Equals(ElementType) &&
                    ((ITArrayType)obj).Dimensions == Dimensions;
            }
            else
            {
                return base.Equals(obj);
            }
        }

        public override int GetHashCode()
        {
            return ElementType.GetHashCode() + Dimensions;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            for (int i = Dimensions; i > 1; i--)
                sb.Append(',');
            sb.Append(']');
            sb.Append(ElementType.ToString());
            return sb.ToString();
        }

        public bool IsCompatibleWithString()
        {
            if (Dimensions != 1) return false;
            ITPrimitiveType prim = ElementType as ITPrimitiveType;
            return prim != null && prim.Type == ITPrimitiveTypeType.Char;
        }

        public bool IsConcatable()
        {
            return Dimensions == 1;
        }

        public override bool CanBeCastedTo(ITType otherType, bool implicitCast)
        {
            if (IsCompatibleWithString())
            {
                ITPrimitiveType other = otherType as ITPrimitiveType;
                if (other != null && other.Type == ITPrimitiveTypeType.String)
                    return true;
            }
            return base.CanBeCastedTo(otherType, implicitCast);
        }

        public override bool CanBeCastedFrom(ITType otherType, bool implictCast)
        {
            if (IsCompatibleWithString())
            {
                ITPrimitiveType other = otherType as ITPrimitiveType;
                if (other != null && other.Type == ITPrimitiveTypeType.String)
                    return true;
            }
            return base.CanBeCastedFrom(otherType, implictCast);
        }

        public override bool IsComparableTo(ITType otherType)
        {
            if (Dimensions != 1) return false;
            ITArrayType otherArray = otherType as ITArrayType;
            if (otherArray.Dimensions != 1) return false;
            return ElementType.IsComparableTo(otherArray.ElementType);
        }

        public override bool IsSealed()
        {
            return true;
        }

        public override bool IsAbstract()
        {
            return true;
        }
    }
}
