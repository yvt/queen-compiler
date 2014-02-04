using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public class ITFunctionType: ITType
    {
        public ITType ReturnType;
        public ITFunctionParameter[] Parameters;
        public ITFunctionType(IntermediateCompiler iCompiler, ITType returnType, ITFunctionParameter[] prms): base(iCompiler)
        {
            ReturnType = returnType;
            Parameters = prms;

            if (prms == null)
                throw new NullReferenceException();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("func<(");

            var prms = Parameters;
            for (int i = 0; i < prms.Length; i++)
            {
                if (i > 0)
                    sb.Append(',');
                sb.Append(prms[i].ToString(true));
            }

            sb.Append(')');
            if (ReturnType != null)
            {
                sb.Append(':');
                sb.Append(ReturnType.ToString());
            }
            sb.Append('>');
            return sb.ToString();
        }

        public override bool Equals(object obj)
        {
            ITFunctionType other = obj as ITFunctionType;
            if (other != null)
            {
                if (ReturnType == null && other.ReturnType != null)
                    return false;
                if (ReturnType != null && other.ReturnType == null)
                    return false;
                if (ReturnType != null && !ReturnType.Equals(other.ReturnType))
                    return false;
                if (Parameters.Length != other.Parameters.Length)
                    return false;
                var param1 = Parameters;
                var param2 = other.Parameters;
                for (int i = 0; i < param1.Length; i++)
                {
                    if (!param1[i].IsTypeEquivalentTo(param2[i]))
                        return false;
                }
                return true;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            int hash = 23423;
            if (ReturnType != null)
                hash ^= ReturnType.GetHashCode();
            for (int i = 0; i < Parameters.Length; i++)
            {
                hash *= 91;
                hash ^= Parameters[i].Type.GetHashCode();
                if (Parameters[i].IsByRef)
                    hash += i + 3;
            }
            return hash;
        }

        public override ITType Superclass
        {
            get { return null; }
        }

        private static ITType[] interfaces = { };
        public override ITType[] Interfaces
        {
            get { return interfaces; }
        }
        public override bool IsValueType
        {
            get { return false; }
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
