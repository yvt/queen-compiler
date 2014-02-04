using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public sealed class ITFunctionParameter: ITObject
    {
        public ITType Type { get; set; }
        public string Name { get; set; }
        public bool IsByRef { get; set; } // FIXME: out only parameter

        public ITFunctionParameter() { }
        public ITFunctionParameter(ITType type)
        {
            Type = type;
        }

        public string ToString(bool omitName)
        {
            if (Name == null || omitName)
            {
                if (IsByRef)
                    return "&" + Type.ToString();
                return Type.ToString();
            }
            else
            {
                if (IsByRef)
                    return Name + ":&" + Type.ToString();
                return Name + ":" + Type.ToString();
            }
        }
        public override string ToString()
        {
            return ToString(false);
        }

        public bool IsTypeEquivalentTo(ITFunctionParameter other)
        {
            if (other == null) return false;
            if (!Type.Equals(other.Type))
                return false;
            return IsByRef == other.IsByRef;
        }
    }
}
