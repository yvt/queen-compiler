using System;
using System.Collections.Generic;
using System.Text;
using Queen.Language.IntermediateTree;
using System.Reflection;

namespace Queen.Language.CliCompiler
{
    internal class CliGenericTypeParameter: ITGenericTypeParameter
    {
        public Type CliType;
        public CliGenericTypeParameter(CliIntermediateCompiler iCompiler, Type cliType, object owner = null): base(iCompiler)
        {
            CliType = cliType;
            Owner = owner;
            Name = cliType.Name;
            UserData = new CliTypeInfo()
            {
                 cliType = cliType
            };
        }

        public override bool Equals(object obj)
        {
            var gen = obj as CliGenericTypeParameter;
            if (gen != null && gen.CliType == CliType)
                return true;
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return CliType.GetHashCode();
        }
    }

    internal class CliClassType: IntermediateTree.ITClassType
    {
        private CliTypeManager manager;
        private Type importedType;

        public CliClassType(CliTypeManager manager, Type typ): base(manager.IntermediateCompiler){
            importedType = typ;
            this.manager = manager;
        }

        public override bool Equals(object obj)
        {
            CliClassType cli = obj as CliClassType;
            if (cli != null)
            {
                if (importedType.Equals(cli.importedType))
                    return true;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return importedType.GetHashCode();
        }

        internal void SetupType()
        {
            Name = importedType.Name;

            Type supercls = importedType.BaseType;
            // don't expose System.Object to the language; this breaks the language conformance.
            if (supercls != null && supercls != typeof(object) && supercls != typeof(Enum))
            {
                SetSuperclass(manager.GetImportedClassType(supercls));
            }

            // FIXME: GetInterfaces includes base class' ones?
            var intfLst = new List<ITType>();
            foreach (var intf in importedType.GetInterfaces())
            {
                // don't want to import system interfaces... (they're never invisible to program)
                if (intf.FullName.StartsWith("System."))
                    continue;
                intfLst.Add(manager.GetImportedClassType(intf));
            }
            SetInterfaces(intfLst.ToArray());

            var info = new CliTypeInfo();
            ConstructorInfo constructor = importedType.GetConstructor(new Type[] { });
            ITClassFlags flags = ITClassFlags.None;
            if (constructor == null || importedType.IsAbstract)
            {
                flags |= ITClassFlags.Abstract;
            }
            // without a construct with 0 parameters, the class cannot be inherited.
            if (constructor == null && !importedType.IsInterface)
            {
                flags |= ITClassFlags.Sealed;
            }
            if (importedType.IsSealed)
            {
                flags |= ITClassFlags.Sealed;
            }
            if (importedType.IsInterface)
            {
                flags |= ITClassFlags.Interface;
            }
            Flags = flags;
            info.constructor = constructor;
            info.cliType = importedType;
            this.UserData = info;

            Type[] outerGenerics = null;
            Type[] generics = importedType.GetGenericArguments();
            Type outerClass = importedType.DeclaringType;
            if (outerClass != null)
            {
                outerGenerics = outerClass.GetGenericArguments();
            }
            else
            {
                outerGenerics = new Type[] { };
            }

            var genParamNames = new string[generics.Length - outerGenerics.Length];
            for (int i = 0; i < genParamNames.Length; i++)
            {
                genParamNames[i] = generics[i - genParamNames.Length + generics.Length].Name;
            }

            this.InheritGenericParameters(genParamNames);

            ITType[] genParams = this.GetGenericParameters();
            if (genParamNames.Length != generics.Length)
                throw new InvalidOperationException();
            for (int i = 0; i < genParams.Length; i++)
            {
                genParams[i].UserData = new CliTypeInfo()
                {
                 cliType = generics[i]
                };
            }

            if (importedType.IsEnum)
            {
                UnderlyingEnumType = (ITPrimitiveType)manager.GetImportedClassType(importedType.GetField("value__").FieldType);
            }

        }

        public Type ImportedType
        {
            get { return importedType; }
        }

        private Dictionary<string, ITEntity> subentities = new Dictionary<string, ITEntity>();

        public override ITEntity GetChildEntity(string ent)
        {
            ITEntity entity;
            if (subentities.TryGetValue(ent, out entity))
            {
                if (entity != null)
                {
                    return entity;
                }
            }
            else
            {
                // static field?
                var field = importedType.GetField(ent, BindingFlags.Static | BindingFlags.Public);
                if (field != null)
                {
                    entity = manager.GetImportedGlobalVariable(field);
                    entity.ParentScope = this;
                    subentities[ent] = entity;
                    return entity;
                }

                // FIXME: global functions?

                // TODO: nested class
            }

            // TODO: get nested class
            return base.GetChildEntity(ent);
        }

        private Dictionary<string, CliMemberFunction> memberFuncs = new Dictionary<string,CliMemberFunction>();
        private Dictionary<string, ITMemberVariable> memberVars = new Dictionary<string, ITMemberVariable>();
        private Dictionary<string, ITMemberProperty> memberProps = new Dictionary<string, ITMemberProperty>();

        public override ITMemberFunction[] GetMemberFunctions()
        {
            var names = new HashSet<string>();
            foreach (var m in importedType.GetMethods())
            {
                if (m.IsStatic || !m.IsPublic) continue;
                if (m.Name.StartsWith("get_") || m.Name.StartsWith("set_")) continue; // ignore properties
                names.Add(m.Name);
            }

            var funs = new ITMemberFunction[names.Count];
            int i = 0;
            foreach (var name in names)
            {
                funs[i++] = GetMemberFunction(name, true);
            }
            return funs;
        }

        public override ITMemberProperty[] GetMemberProperties()
        {
            var names = new HashSet<string>();
            foreach (var m in importedType.GetProperties())
            {
                if (m.GetGetMethod() == null || m.GetGetMethod().IsStatic || !m.GetGetMethod().IsPublic) continue;
                names.Add(m.Name);
            }

            var funs = new ITMemberProperty[names.Count];
            int i = 0;
            foreach (var name in names)
            {
                funs[i++] = GetMemberProperty(name, true);
            }
            return funs;
        }

        public override ITMemberVariable[] GetMemberVariables()
        {
            throw new NotImplementedException();
        }

        public override ITMemberFunction GetMemberFunction(string ent, bool searchSuper)
        {
            CliMemberFunction fun;
            if (ent.StartsWith("set_") || ent.StartsWith("get_")) return base.GetMemberFunction(ent, searchSuper);
            if (!memberFuncs.TryGetValue(ent, out fun))
            {
                string actualName = ent;
                if (actualName == "ToStr")
                    actualName = "ToString";
                MethodInfo meth = importedType.GetMethod(actualName);
                fun = null;
                if (meth != null)
                {
                    fun = manager.GetImportedMemberFunction(meth);
                    if (fun.Owner != null)
                    {
                        throw new InvalidOperationException();
                    }
                    fun.Owner = this;
                }
                memberFuncs.Add(ent, fun);

                return fun;
            }
            else if(fun != null)
            {
                return fun;
            }
            return base.GetMemberFunction(ent, searchSuper);
        }

        public override string ToString()
        {
            return base.ToString();
        }


        public override bool IsComparableTo(ITType otherType)
        {
            if (this.importedType == typeof(Queen.Kuin.CClass))
            {
                return otherType.InheritsFrom(this);
            }
            return base.IsComparableTo(otherType);
        }

        public override ITMemberProperty GetMemberProperty(string ent, bool searchSuper)
        {
            ITMemberProperty fun;
            if (!memberProps.TryGetValue(ent, out fun))
            {
                string actualName = ent;
                // TODO: get indexer
                PropertyInfo meth = importedType.GetProperty(actualName);
                fun = null;
                if (meth != null)
                {
                    fun = new ITMemberProperty()
                    {
                        Parameters = new List<ITFunctionParameter>(),
                        Owner = this, Type = manager.GetImportedClassType(meth.PropertyType),
                        Name = meth.Name
                    };


                    var getter = meth.GetGetMethod();
                    var setter = meth.GetSetMethod();

                    // FIXME: asymmetric access
                    fun.IsPrivate = getter.IsPrivate;
                    fun.IsPublic = getter.IsPublic;

                    if (getter != null)
                    {
                        fun.Getter = manager.GetImportedMemberFunction(getter).Body;
                    }
                    if (setter != null)
                    {
                        fun.Setter = manager.GetImportedMemberFunction(setter).Body;
                    }

                    // do parameters
                    if (getter != null)
                    {

                        foreach (ParameterInfo param in getter.GetParameters())
                        {
                            ITFunctionParameter prm = new ITFunctionParameter();
                            prm.Name = param.Name;
                            prm.Type = manager.GetImportedClassType(param.ParameterType);
                            prm.IsByRef = param.IsOut;
                            fun.Parameters.Add(prm);
                        }
                    }

                    fun.UserData = new CliMemberPropertyInfo()
                    {
                        getter = getter,
                        setter = setter,
                        ownerITType = this,
                        property = meth
                    };
                }
                memberProps.Add(ent, fun);
                return fun;
            }
            else if (fun != null)
            {
                return fun;
            }
            return base.GetMemberProperty(ent, searchSuper);
        }

        public override ITMemberVariable GetMemberVariable(string ent, bool searchSuper)
        {
            ITMemberVariable fun;
            if (!memberVars.TryGetValue(ent, out fun))
            {
                string actualName = ent;
                FieldInfo meth = importedType.GetField(actualName, BindingFlags.Public);
                fun = null;
                if (meth != null && !meth.IsStatic)
                {
                    fun = new ITMemberVariable()
                    {
                        IsPrivate = false,
                        IsPublic = meth.IsPublic,
                        IsConst = false,
                        Name = ent,
                        Owner = this,
                        UserData = new CliMemberVariableInfo()
                        {
                            field = meth,
                            ownerITType = this
                        }, Type = manager.GetImportedClassType(meth.FieldType)
                    };
                }
                memberVars.Add(ent, fun);
                return fun;
            }
            else if (fun != null)
            {
                return fun;
            }
            return base.GetMemberVariable(ent, searchSuper);
        }
        /*
        public override ITType MakeGenericType(ITType[] types)
        {
            Type[] insts = new Type[types.Length];
            for (int i = 0; i < types.Length; i++)
            {
                
            }


        }*/
    }
}
