using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public abstract class ITType: ITGlobalScope
    {
        public abstract ITType Superclass { get; }
        public abstract ITType[] Interfaces { get; }

        internal int tarjan_index, tarjan_lowlink;
        internal bool tarjan_beingCheced;
        public IntermediateCompiler iCompiler { get; protected set; }
        public string Name { get; set; }

        public ITType(IntermediateCompiler iCompiler)
        {
            this.iCompiler = iCompiler;
        }

        public virtual bool CanBeCastedTo(ITType otherType, bool implicitCast)
        {
            if (implicitCast)
            {
                if (InheritsFrom(otherType))
                {
                    return true;
                }
            }
            return false;
        }
        public virtual bool CanBeCastedFrom(ITType otherType, bool implictCast)
        {
            return false;
        }
        public virtual bool IsComparableTo(ITType otherType)
        {
            if (Superclass == null)
                return false;
            else
                return Superclass.IsComparableTo(otherType);
        }

        public bool InheritsFrom(ITType typ)
        {
            if (typ.Equals(this)) return true;
            if (Superclass == null)
                return false;
            if (Superclass.InheritsFrom(typ))
                return true;
            foreach (var intf in Interfaces)
            {
                if (intf.InheritsFrom(typ))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Does a static "typ1(x) is typ2" check. x is assumed to be a non-null object reference.
        /// </summary>
        /// <param name="typ1"></param>
        /// <param name="typ2"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        static public bool TryStaticTypeHierarchyCheck(ITType typ1, ITType typ2, out bool result)
        {
            if (typ1 == typ2)
            {
                result = true;
                return true;
            }

            // subclass?
            for (ITType typ = typ1; typ != null; typ = typ.Superclass)
            {
                if (typ == typ2)
                {
                    result = true;
                    return true;
                }
            }

            // another branch?
            List<ITType> chain1 = new List<ITType>();
            List<ITType> chain2 = new List<ITType>();
            for (ITType typ = typ1; typ != null; typ = typ.Superclass)
                chain1.Add(typ);
            for (ITType typ = typ2; typ != null; typ = typ.Superclass)
                chain2.Add(typ);

            int len1 = chain1.Count;
            int len2 = chain2.Count;
            int len = Math.Min(len1, len2);

            for (int i = 1; i <= len; i++)
            {
                if (chain1[len1 - i] != chain2[len2 - i])
                {
                    result = false;
                    return true;
                }
            }

            // not sure
            result = false;
            return false;
        }

        public virtual ITMemberVariable[] GetMemberVariables()
        {
            return new ITMemberVariable[] { };
        }

        public virtual ITMemberProperty[] GetMemberProperties()
        {
            return new ITMemberProperty[] { };
        }

        public virtual ITMemberFunction[] GetMemberFunctions()
        {
            return new ITMemberFunction[] { };
        }

        public virtual ITMemberVariable GetMemberVariable(string ent, bool searchSuperclass = true)
        {
            if (Superclass != null && searchSuperclass)
                return Superclass.GetMemberVariable(ent, true);
            return null;
        }

        public virtual ITMemberProperty GetMemberProperty(string ent, bool searchSuperclass = true)
        {
            if (Superclass != null && searchSuperclass)
                return Superclass.GetMemberProperty(ent, true);
            return null;
        }

        private static ITMemberFunction toStrFunc = null;

        public virtual ITMemberFunction GetMemberFunction(string ent, bool searchSuperclass = true)
        {
            if (Superclass != null && searchSuperclass)
                return Superclass.GetMemberFunction(ent, true);
            if (ent == "ToStr")
            {
                if (toStrFunc == null)
                {
                    toStrFunc = new ITMemberFunction()
                    {
                        IsPublic = true,
                        Name = "ToStr",
                        Body = new ITFunctionBody()
                        {
                            Name = "ToStr",
                            ReturnType = iCompiler.GetPrimitiveType(ITPrimitiveTypeType.String),
                            GenericParameters = new ITGenericTypeParameter[]{}
                        }
                    };
                }
                return toStrFunc;
            }
            return null;
        }

        public virtual ITType MakeGenericType(ITType[] types)
        {
            ITType[] genParams = GetGenericParameters();
            if (types.Length != genParams.Length)
            {
                throw new IntermediateCompilerException(Properties.Resources.InternalError);
            }
            if (types.Length == 0)
            {
                return this;
            }
            return new ITInstantiatedGenericType(iCompiler, this, types);
        }

        public abstract bool IsValueType { get; }

        public ITType ContainingType
        {
            get
            {
                return this.ParentScope as ITType;
            }
        }

        public virtual ITType[] GetGenericParameters()
        {
            return new ITType[] { };
        }

        public virtual bool CanConstruct()
        {
            return !IsAbstract();
        }


        public override string ToString()
        {
            List<string> lst = new List<string>();
            ITScope scp = this;
            string rootName = null;

            while (scp != null)
            {
                if (scp is ITType)
                {
                    lst.Add(((ITType)scp).GetDisplayName());
                }
                else if (scp is ITLocalScope)
                {
                    // local scope can no longer hold a type.
                    throw new InvalidOperationException();
                }
                else if (scp is ITRootGlobalScope)
                {
                    rootName = ((scp as ITRootGlobalScope).Name);
                    break;
                }
                else
                {
                    throw new InvalidOperationException();
                }
                scp = scp.ParentScope;
            }

            lst.Reverse();

            string name = string.Join("#", lst.ToArray());
            if (rootName != null)
            {
                name = rootName + "@" + name;
            }

            return name;
        }

        public abstract bool IsSealed();

        public abstract bool IsAbstract();

        public virtual bool IsInterface() { return false; }

        public virtual bool IsClosed() { return true; }

        public override int GetNumLocalGenericParameters()
        {
            int cnt = GetGenericParameters().Length;
            var outer = ContainingType;
            if (outer != null) cnt -= outer.GetGenericParameters().Length;
            return cnt;
        }

        // display name is used in error messages.
        public virtual string GetDisplayName()
        {   
            return iCompiler.GetBuiltinTypeName(this) ?? Name;
        }
    }
}
