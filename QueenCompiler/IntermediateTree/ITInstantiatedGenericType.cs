using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public class ITInstantiatedGenericType: ITType
    {
        public ITType GenericTypeDefinition { get; private set; }
        private ITType[] genericParams;
        private ITType superclass;
        private IntermediateCompiler.GenericTypeMutator mutator;
        private ITType[] interfaces;

        public ITInstantiatedGenericType(IntermediateCompiler iCompiler, ITType definition, ITType[] instantiation) :
            this(iCompiler, definition, instantiation, null) { }
        internal ITInstantiatedGenericType(IntermediateCompiler iCompiler, ITType definition, ITType[] instantiation,
            IntermediateCompiler.GenericTypeMutator baseMutator):
            base(iCompiler)
        {
            this.GenericTypeDefinition = definition;
            genericParams = instantiation;
            Name = definition.Name;

            if (baseMutator != null)
            {
                mutator = new IntermediateCompiler.GenericTypeMutator(baseMutator);
                mutator.AddMutator(definition, instantiation);
            }
            else
            {
                mutator = new IntermediateCompiler.GenericTypeMutator(definition, instantiation);
            }
            superclass = mutator.Mutate(definition.Superclass);
        }

        public override bool Equals(object obj)
        {
            ITInstantiatedGenericType inst = obj as ITInstantiatedGenericType;
            if (inst != null)
            {
                if (!GenericTypeDefinition.Equals(inst.GenericTypeDefinition))
                    return false;
                var params1 = GetGenericParameters();
                var params2 = inst.GetGenericParameters();
                if (params1.Length != params2.Length)
                    throw new InvalidOperationException();
                for (int i = 0; i < params1.Length; i++)
                {
                    if (!params1[i].Equals(params2[i]))
                        return false;
                }
                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            int hash = GenericTypeDefinition.GetHashCode();
            foreach (var t in GetGenericParameters())
            {
                hash *= 71;
                hash ^= t.GetHashCode();
            }
            return hash;
        }

        public override ITType[] GetGenericParameters()
        {
            return genericParams;
        }

        public override ITType MakeGenericType(ITType[] types)
        {
            return new ITInstantiatedGenericType(iCompiler, GenericTypeDefinition, types, mutator);
        }

        public override ITType Superclass
        {
            get { return superclass; }
        }

        public override ITType[] Interfaces
        {
            get {
                if (interfaces == null)
                {
                    var inf = GenericTypeDefinition.Interfaces;
                    var inf2 = new ITType[inf.Length];
                    for (int i = 0; i < inf.Length; i++)
                    {
                        inf2[i] = mutator.Mutate(inf[i]);
                    }
                    interfaces = inf2;
                }
                return interfaces;
            }
        }

        public override bool IsValueType
        {
            get { return GenericTypeDefinition.IsValueType; }
        }



        public override bool IsSealed()
        {
            return GenericTypeDefinition.IsSealed();
        }

        public override bool IsAbstract()
        {
            return GenericTypeDefinition.IsAbstract();
        }

        public override bool IsInterface()
        {
            return GenericTypeDefinition.IsInterface();
        }

        public override bool IsClosed() { return true; }

        private ITMutatedFunctionEntity MutateGlobalFunction(ITFunctionEntity ent)
        {
            ITMutatedFunctionEntity mutat = ent as ITMutatedFunctionEntity;
            if (mutat != null)
            {
                ITType[] paramTypes = mutat.mutatedParameterTypes;
                for (int i = 0; i < paramTypes.Length; i++)
                {
                    paramTypes[i] = mutator.Mutate(paramTypes[i]);
                }
                mutat.mutatedParameterTypes = paramTypes;
                mutat.mutatedReturnType = mutator.Mutate(mutat.mutatedReturnType);
            }
            else
            {
                mutat = new ITMutatedFunctionEntity()
                {
                    Body = ent.Body,
                    IsPrivate = ent.IsPrivate,
                    IsPublic = ent.IsPublic,
                    IsRootGlobalScopePrivate = ent.IsRootGlobalScopePrivate,
                    Location = ent.Location,
                    Name = ent.Name,
                    ParentScope = ent.ParentScope,
                    BaseEntity = ent
                };
                ITType[] paramTypes = ent.GetParameterTypes();
                for (int i = 0; i < paramTypes.Length; i++)
                {
                    paramTypes[i] = mutator.Mutate(paramTypes[i]);
                }

                mutat.mutatedParameterTypes = paramTypes;
                mutat.mutatedReturnType = mutator.Mutate(ent.GetReturnType());
            }
            return mutat;
        }

        public override ITEntity GetChildEntity(string ent)
        {
            ITEntity defEnt = GenericTypeDefinition.GetChildEntity(ent);
            if (defEnt == null)
                return null;

            var func = defEnt as ITFunctionEntity;
            if (func != null)
            {
                return MutateGlobalFunction(func);
            }

            // TODO: ITInstantiatedGenericType.GetChildEntity for global variable, which is used to access static fields


            throw new NotImplementedException();
        }

        private ITMutatedMemberVariable MutateMemberVariable(ITMemberVariable var)
        {
            var mutat = var as ITMutatedMemberVariable;
            if (mutat != null)
            {
                mutat.Type = mutator.Mutate(mutat.Type);
            }
            else
            {
                mutat = new ITMutatedMemberVariable()
                {
                    Base = var,
                    IsConst = var.IsConst,
                    InitialValue = var.InitialValue,
                    IsPrivate = var.IsPrivate,
                    IsPublic = var.IsPublic,
                    Location = var.Location,
                    Name = var.Name,
                    Owner = this,
                    Type = mutator.Mutate(var.Type)
                };
            }
            return mutat;
        }

        private ITMutatedMemberProperty MutateMemberProperty(ITMemberProperty var)
        {
            var mutat = var as ITMutatedMemberProperty;
            if (mutat != null)
            {
                var prms = mutat.Parameters;
                for (int i = 0; i < prms.Count; i++)
                {
                    prms[i].Type = mutator.Mutate(prms[i].Type);
                }
            }
            else
            {
                mutat = new ITMutatedMemberProperty()
                {
                    Base = var,
                    Getter = var.Getter,
                    Setter = var.Setter,
                    Location = var.Location,
                    Owner = this,
                    Type = mutator.Mutate(var.Type),
                    Name = var.Name
                };
                var prms = new List<ITFunctionParameter>();
                foreach (ITFunctionParameter prm in var.Parameters)
                {
                    ITFunctionParameter prm2 = new ITFunctionParameter()
                    {
                        IsByRef = prm.IsByRef,
                        Location = prm.Location,
                        Name = prm.Name,
                        Type = mutator.Mutate(prm.Type)
                    };
                    prms.Add(prm2);
                }
                mutat.Parameters = prms;
            }
            return mutat;
        }

        private ITMutatedMemberFunction MutateMemberFunction(ITMemberFunction var)
        {
            var mutat = var as ITMutatedMemberFunction;
            if (mutat != null)
            {
                var typs = mutat.mutatedParameterTypes;
                for (int i = 0; i < typs.Length; i++)
                    typs[i] = mutator.Mutate(typs[i]);
                mutat.mutatedReturnType = mutator.Mutate(mutat.mutatedReturnType);
                return mutat;
            }
            else
            {
                mutat = new ITMutatedMemberFunction()
                {
                    Base = var,
                    Location = var.Location,
                    Owner = this,
                    IsPrivate = var.IsPrivate,
                    IsPublic = var.IsPublic,
                    Name = var.Name,
                    Body = var.Body,
                    IsAbstract = var.IsAbstract
                };
                ITType[] paramTypes = var.GetParameterTypes();
                for (int i = 0; i < paramTypes.Length; i++)
                {
                    paramTypes[i] = mutator.Mutate(paramTypes[i]);
                }

                mutat.mutatedParameterTypes = paramTypes;
                mutat.mutatedReturnType = mutator.Mutate(var.GetReturnType());
                return mutat;
            }
        }

        public override ITMemberFunction[] GetMemberFunctions()
        {
            var fns = GenericTypeDefinition.GetMemberFunctions();
            var lst = new ITMemberFunction[fns.Length];
            for (int i = 0; i < fns.Length; i++)
            {
                lst[i] = MutateMemberFunction(fns[i]);
            }
            return lst;
        }

        public override ITMemberProperty[] GetMemberProperties()
        {
            var fns = GenericTypeDefinition.GetMemberProperties();
            var lst = new ITMemberProperty[fns.Length];
            for (int i = 0; i < fns.Length; i++)
            {
                lst[i] = MutateMemberProperty(fns[i]);
            }
            return lst;
        }

        public override ITMemberVariable[] GetMemberVariables()
        {
            var fns = GenericTypeDefinition.GetMemberVariables();
            var lst = new ITMemberVariable[fns.Length];
            for (int i = 0; i < fns.Length; i++)
            {
                lst[i] = MutateMemberVariable(fns[i]);
            }
            return lst;
        }

        public override ITMemberVariable GetMemberVariable(string ent, bool searchSuper)
        {
            ITMemberVariable var;
            var = GenericTypeDefinition.GetMemberVariable(ent, false);
            if (var == null)
                return base.GetMemberVariable(ent, searchSuper);
            return MutateMemberVariable(var);
        }

        public override ITMemberProperty GetMemberProperty(string ent, bool searchSuper)
        {
            ITMemberProperty var;
            var = GenericTypeDefinition.GetMemberProperty(ent, false);
            if (var == null)
                return base.GetMemberProperty(ent, searchSuper);
            return MutateMemberProperty(var);
        }

        public override ITMemberFunction GetMemberFunction(string ent, bool searchSuper)
        {
            ITMemberFunction var;
            var = GenericTypeDefinition.GetMemberFunction(ent, false);
            if (var == null)
                return base.GetMemberFunction(ent, searchSuper);
            return MutateMemberFunction(var);
        }

        private string FormatGenericType(string name, ITType[] prms, int prmIndex, int prmCount)
        {
            if (prmCount == 0)
                return name;

            var sb = new StringBuilder();
            sb.Append(name);
            sb.Append("`[");
            for (int i = 0; i < prmCount; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(prms[i + prmIndex].ToString());
            }
            sb.Append(']');
            return sb.ToString();
        }

        public override string ToString()
        {
            List<string> lst = new List<string>();
            ITScope scp = GenericTypeDefinition;
            string rootName = null;
            var insts = GetGenericParameters();
            int genParamIdx = insts.Length;

            while (scp != null)
            {
                ITType typ = scp as ITType;
                if (typ != null)
                {
                    int cnt = typ.GetNumLocalGenericParameters();
                    genParamIdx -= cnt;
                    lst.Add(FormatGenericType(typ.GetDisplayName(), insts, genParamIdx, cnt));
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
    }
}
