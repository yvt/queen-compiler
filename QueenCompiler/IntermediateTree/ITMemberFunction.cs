using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public class ITMemberFunction: ITMember
    {
        public ITFunctionBody Body { get; set; }
        public string Name { get; set; }

        public bool IsAbstract { get; set; }

        // used while checking overrides
        public bool MarkedAsOverriding { get; set; }

        // set while checking override, might be null for imported types
        public ITMemberFunction OverriddenMember { get; set; }

        public bool IsConstructor()
        {
            return Name == "Ctor";
        }

        public bool IsDestructor()
        {
            return Name == "Dtor";
        }

        public virtual ITType GetReturnType()
        {
            return Body.ReturnType;
        }
        public virtual ITType[] GetParameterTypes()
        {
            var prms = Body.Parameters;
            var ret = new ITType[prms.Count];
            for (int i = 0; i < ret.Length; i++)
            {
                ITFunctionParameter prm = prms[i];
                ret[i] = prm.Type;
            }
            return ret;
        }
        public override string ToString()
        {
            if (Name == "Ctor")
            {
                return "ctor()";
            }
            else if (Name == "Dtor")
            {
                return "dtor()";
            }

            var sb = new StringBuilder();
            sb.Append("func ");
            sb.Append(Name);
            var gens = Body.GenericParameters;
            if (gens != null && gens.Length > 0)
            {
                sb.Append('`');
                if (gens.Length == 1)
                {
                    sb.Append(gens[0].ToString());
                }
                else
                {
                    sb.Append('[');
                    for (int i = 0; i < gens.Length; i++)
                    {
                        if (i > 0) sb.Append(',');
                        sb.Append(gens[i].ToString());
                    }
                    sb.Append(']');
                }
            }
            sb.Append('(');
            ITType[] prmTypes = GetParameterTypes();
            var prms = Body.Parameters;
            for (int i = 0; i < prmTypes.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(prms[i].Name);
                sb.Append(':');
                if (prms[i].IsByRef)
                    sb.Append('&');
                sb.Append(prmTypes[i].ToString());
            }
            sb.Append(')');
            ITType ret = GetReturnType();
            if (ret != null)
            {
                sb.Append(':');
                sb.Append(ret.ToString());
            }
            return sb.ToString();
        }
    }

    public sealed class ITMutatedMemberFunction : ITMemberFunction
    {
        public ITMemberFunction Base;

        public ITType mutatedReturnType;
        public ITType[] mutatedParameterTypes;

        public override ITType GetReturnType()
        {
            return mutatedReturnType;
        }
        public override ITType[] GetParameterTypes()
        {
            return mutatedParameterTypes;
        }
    }
}
