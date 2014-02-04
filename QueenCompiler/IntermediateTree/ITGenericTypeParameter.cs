using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public class ITGenericTypeParameter: ITType
    {
        public object Owner { get; set; }

        public override bool IsValueType
        {
            get { return false; }
        }

        public override ITType Superclass
        {
            get { return null; }
        }

        public ITGenericTypeParameter(IntermediateCompiler iCompiler)
            : base(iCompiler)
        {
        }

        public ITGenericTypeParameter(IntermediateCompiler iCompiler, string name, object owner)
            : this(iCompiler)
        {
            Name = name; Owner = owner;
        }

        private static ITType[] interfaces = new ITType[0];
        public override ITType[] Interfaces
        {
            get { return interfaces; }
        }

        public override int GetHashCode()
        {
            return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
        }

        public override string ToString()
        {
            return Name;
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
