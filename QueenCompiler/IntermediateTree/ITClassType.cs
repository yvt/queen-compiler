using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    [Flags]
    public enum ITClassFlags
    {
        None = 0,
        Abstract = 1 << 0,
        Sealed = 1 << 1,
        Interface = 1 << 2
    }
    public class ITClassType: ITType
    {
        private static ITType[] emptyInterfaces = new ITType[0];
        private ITType supercls;
        private ITType[] interfaces = emptyInterfaces;

        private ITClassEntity entity;
        public ITClassEntity Entity
        {
            get
            {
                return entity;
            }
        }

        private IDictionary<string, ITEntity> Children;

        private IDictionary<string, ITMemberVariable> MemberVariables;
        private IDictionary<string, ITMemberProperty> MemberProperties;
        private IDictionary<string, ITMemberFunction> MemberFunctions;

        public ITGenericTypeParameter[] GenericTypeParameters { get; set; }

        /// <summary>
        /// Data type used to represent enum value, or null for non-enum types.
        /// </summary>
        public ITPrimitiveType UnderlyingEnumType { get; set; }

        public ITClassFlags Flags { get; set; }

        public ITClassType(IntermediateCompiler iCompiler): base(iCompiler)
        {
            Children = new Dictionary<string, ITEntity>();
            MemberVariables = new Dictionary<string, ITMemberVariable>();
            MemberProperties = new Dictionary<string, ITMemberProperty>();
            MemberFunctions = new Dictionary<string, ITMemberFunction>();
        }

        public void BindEntity(ITClassEntity ent)
        {
            if (entity != null)
                throw new InvalidOperationException("Class is already bind to an entity.");
            entity = ent;
        }


        public override bool CanBeCastedFrom(ITType otherType, bool implictCast)
        {
            if (otherType is ITNullType)
                return true;
            if (UnderlyingEnumType != null && !implictCast)
            {
                // enum type; actual value is integer
                ITPrimitiveType prim = otherType as ITPrimitiveType;
                switch (prim.Type)
                {
                    case ITPrimitiveTypeType.Int8:
                    case ITPrimitiveTypeType.Int16:
                    case ITPrimitiveTypeType.Int32:
                    case ITPrimitiveTypeType.Int64:
                    case ITPrimitiveTypeType.Integer:
                    case ITPrimitiveTypeType.UInt8:
                    case ITPrimitiveTypeType.UInt16:
                    case ITPrimitiveTypeType.UInt32:
                    case ITPrimitiveTypeType.UInt64:
                        return true;
                }
            }
            return base.CanBeCastedFrom(otherType, implictCast);
        }

        public override bool CanBeCastedTo(ITType otherType, bool implicitCast)
        {
            if (UnderlyingEnumType != null && !implicitCast)
            {
                // enum type; actual value is integer
                ITPrimitiveType prim = otherType as ITPrimitiveType;
                switch (prim.Type)
                {
                    case ITPrimitiveTypeType.Int8:
                    case ITPrimitiveTypeType.Int16:
                    case ITPrimitiveTypeType.Int32:
                    case ITPrimitiveTypeType.Int64:
                    case ITPrimitiveTypeType.Integer:
                    case ITPrimitiveTypeType.UInt8:
                    case ITPrimitiveTypeType.UInt16:
                    case ITPrimitiveTypeType.UInt32:
                    case ITPrimitiveTypeType.UInt64:
                        return true;
                }
            }
            return base.CanBeCastedTo(otherType, implicitCast);
        }

        public override ITEntity GetChildEntity(string ent)
        {
            if (Children.ContainsKey(ent))
                return Children[ent];
            return base.GetChildEntity(ent);
        }

        public override ITEntity[] GetChildEntities()
        {
            var ents = new ITEntity[Children.Count];
            Children.Values.CopyTo(ents, 0);
            return ents;
        }

        public void AddMemberFunction(ITMemberFunction var)
        {
            MemberFunctions.Add(var.Name, var);
        }

        public void AddMemberProperty(ITMemberProperty prop)
        {
            MemberProperties.Add(prop.Name, prop);
        }

        public void AddMemberVariable(ITMemberVariable var)
        {
            MemberVariables.Add(var.Name, var);
        }

        public override ITMemberFunction[] GetMemberFunctions()
        {
            var lst = MemberFunctions.Values;
            var arr = new ITMemberFunction[lst.Count];
            lst.CopyTo(arr, 0);
            return arr;
        }

        public override ITMemberProperty[] GetMemberProperties()
        {
            var lst = MemberProperties.Values;
            var arr = new ITMemberProperty[lst.Count];
            lst.CopyTo(arr, 0);
            return arr;
        }

        public override ITMemberVariable[] GetMemberVariables()
        {
            var lst = MemberVariables.Values;
            var arr = new ITMemberVariable[lst.Count];
            lst.CopyTo(arr, 0);
            return arr;
        }

        public override ITMemberVariable GetMemberVariable(string ent, bool searchSuper)
        {
            ITMemberVariable var;
            if (MemberVariables.TryGetValue(ent, out var))
            {
                return var;
            }
            return base.GetMemberVariable(ent, searchSuper);
        }

        public override ITMemberProperty GetMemberProperty(string ent, bool searchSuper)
        {
            ITMemberProperty var;
            if (MemberProperties.TryGetValue(ent, out var))
            {
                return var;
            }
            return base.GetMemberProperty(ent, searchSuper);
        }

        public override ITMemberFunction GetMemberFunction(string ent, bool searchSuper)
        {
            ITMemberFunction var;
            if (MemberFunctions.TryGetValue(ent, out var))
            {
                return var;
            }
            return base.GetMemberFunction(ent, searchSuper);
        }

        public override ITType Superclass
        {
            get
            {
                return supercls;
            }
        }

        public override ITType[] GetGenericParameters()
        {
            return GenericTypeParameters;
        }

        /// <summary>
        /// Copies generic parameters from the specified outer method.
        /// This should only be used when this class is defined and after ParentScope is set.
        /// </summary>
        public void InheritGenericParameters(string[] gparamNames)
        {
            ITType[] outerParams = null;
            {
                ITType c = ContainingType;
                if (c != null)
                {
                    outerParams = c.GetGenericParameters();
                }
            }

            ITGenericTypeParameter[] gparams;
            int ln = 0;
            if (outerParams == null || outerParams.Length == 0)
            {
                gparams = new ITGenericTypeParameter[gparamNames.Length];
            }
            else
            {
                ln = outerParams.Length;
                gparams = new ITGenericTypeParameter[gparamNames.Length + ln];
            }

            for (int i = 0; i < gparams.Length; i++)
            {
                string name;
                if (i < ln)
                {
                    name = outerParams[i].Name;
                }
                else
                {
                    name = gparamNames[i - ln];
                }

                gparams[i] = new ITGenericTypeParameter(iCompiler, name, this);
            }

            GenericTypeParameters = gparams;
        }

        public void SetSuperclass(ITType sc)
        {
            supercls = sc;
        }

        public void SetInterfaces(ITType[] interfaces)
        {
            this.interfaces = interfaces;
        }

        public override ITType[] Interfaces
        {
            get { return interfaces; }
        }

        public override bool IsValueType
        {
            get { return false; }
        }

        public override void AddChildEntity(ITEntity ent)
        {
            Children.Add(ent.Name, ent);
        }

        public void RemoveChildEntity(ITEntity ent)
        {
            ITEntity en;
            if (Children.TryGetValue(ent.Name, out en))
            {
                if (en != ent)
                    throw new KeyNotFoundException(ent.Name + " was found but different object");
                Children.Remove(ent.Name);
            }
            else
            {
                throw new KeyNotFoundException(ent.Name + " not found");
            }
        }


        public override bool CanConstruct()
        {
            return (!IsAbstract()) && (!IsInterface()) && UnderlyingEnumType == null;
        }

        public override bool IsSealed()
        {
            return (Flags & ITClassFlags.Sealed) != 0;
        }

        public override bool IsAbstract()
        {
            return (Flags & ITClassFlags.Abstract) != 0;
        }

        public override bool IsInterface()
        {
            return (Flags & ITClassFlags.Interface) != 0;
        }

        public override bool IsClosed()
        {
            return GenericTypeParameters.Length == 0;
        }
    }
}
