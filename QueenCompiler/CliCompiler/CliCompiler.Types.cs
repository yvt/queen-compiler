using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Queen.Language.IntermediateTree;

namespace Queen.Language.CliCompiler
{
    internal interface IDelayedRegistrar
    {
        void Register();
    }
    public partial class CliCompiler
    {

        private IList<IDelayedRegistrar> delayedHierarchizators = new List<IDelayedRegistrar>();
        private IList<IDelayedRegistrar> delayedRegistrars = new List<IDelayedRegistrar>();
        private IList<IDelayedRegistrar> delayedCompilations = new List<IDelayedRegistrar>();
        private IList<IDelayedRegistrar> delayedTypeCompletions = new List<IDelayedRegistrar>();
        private IList<IDelayedRegistrar> typeInitializerCreator = new List<IDelayedRegistrar>();

        private sealed class TypeInitializerCreator : IDelayedRegistrar
        {
            public CliCompiler compiler;
            public ITType thisType;
            public TypeBuilder builder;
            public List<ITGlobalVariableEntity> vars = new List<ITGlobalVariableEntity>();
            public void Register()
            {
                
                ConstructorBuilder cctor = builder.DefineTypeInitializer();
                ILGenerator il = cctor.GetILGenerator();
                var fcomp = new FunctionCompiler(compiler, thisType, null, cctor);
                fcomp.InitializeForGGeneralUse(il);

                foreach (ITGlobalVariableEntity ent in vars)
                {
                    if (ent.IsConst)
                        continue;
                    if (ent.InitialValue == null)
                        continue;
                    il.Emit(OpCodes.Ldnull);
                    fcomp.EmitExpression(ent.InitialValue);
                    il.Emit(OpCodes.Stfld, ((CliGlobalVariableInfo)ent.UserData).field);
                }

                il.Emit(OpCodes.Ret);
            }
        }

        private sealed class TypeCompletor: IDelayedRegistrar
        {
            public TypeBuilder builder;
            public IDelayedRegistrar superClass;
            public IDelayedRegistrar containingClass;

            public void Register()
            {
                if (builder == null)
                    return;
                var b = builder;
                builder = null;
                if (superClass != null)
                {
                    superClass.Register();
                }
                if (containingClass != null)
                {
                    containingClass.Register();
                }
                try
                {
                    b.CreateType();
                }
                catch (TypeLoadException)
                {
                    // mcs says:
                    //
                    // This is fine, the code still created the type
                    //
                    // but this doesn't work...
                    // TODO: do with impossible cases of CreateType
                }
            }

            public override string ToString()
            {
                return builder.ToString();
            }
        }

        private sealed class Hierarchizator: IDelayedRegistrar
        {
            private CliCompiler compiler;
            public ITClassType subclass;
            public Hierarchizator(CliCompiler compiler)
            {
                this.compiler = compiler;
            }
            public void Register()
            {
                CliTypeInfo subInfo = (CliTypeInfo)subclass.UserData;
                
                // setup inheritance
                if (subclass.Superclass != null)
                {
                    CliTypeInfo superInfo = (CliTypeInfo)(subclass.Superclass.UserData);
                    if (superInfo != null)
                    {
                        ((TypeBuilder)subInfo.cliType).SetParent(superInfo.cliType);
                        ((TypeCompletor)subInfo.delayedTypeCompletor).superClass = superInfo.delayedTypeCompletor;
                    }
                    else
                    {
                        // superInfo can be null for generic class
                        var instClass = (ITInstantiatedGenericType)subclass.Superclass;
                        superInfo = (CliTypeInfo)(instClass.GenericTypeDefinition.UserData);
                        ((TypeBuilder)subInfo.cliType).SetParent(compiler.GetCliType(subclass.Superclass));
                        ((TypeCompletor)subInfo.delayedTypeCompletor).superClass = superInfo.delayedTypeCompletor;
                    }
                }

                // setup interfaces
                foreach (ITType inf in subclass.Interfaces)
                {
                    ((TypeBuilder)subInfo.cliType).AddInterfaceImplementation(compiler.GetCliType(inf));
                }
            }
        }

        internal Type GetCliType(ITType iType)
        {
            CliPrimitiveType prim = iType as CliPrimitiveType;
            if (prim != null)
            {
                return prim.GetCliType();
            }

            ITInstantiatedGenericType inst = iType as ITInstantiatedGenericType;
            if (inst != null)
            {
                // generic type
                ITType gene = inst.GenericTypeDefinition;
                Type typ = GetCliType(gene);

                // instantiate
                ITType[] iTypes = inst.GetGenericParameters();
                Type[] typs = new Type[iTypes.Length];
                for (int i = 0; i < typs.Length; i++)
                {
                    typs[i] = GetCliType(iTypes[i]);
                }

                return typ.MakeGenericType(typs);
            }

            CliTypeInfo info = iType.UserData as CliTypeInfo;
            if (info == null)
            {

                ITArrayType iArray = iType as ITArrayType;

                if (iArray != null)
                {
                    Type typ = GetCliType(iArray.ElementType);
                    typ = typ.MakeArrayType(iArray.Dimensions);

                    info = new CliTypeInfo();
                    info.cliType = typ;
                    iArray.UserData = info;
                    return typ;
                }

                ITFunctionType iFunc = iType as ITFunctionType;
                if (iFunc != null)
                {
                    var delInfo = new LocalDelegateInfo(iFunc);
                    var delType = RegisterDelegateTemplate(delInfo);

                    var genParams = new Type[delInfo.isParamByRef.Length + (delInfo.hasReturnValue ? 1 : 0)];

                    if (iFunc.ReturnType != null)
                    {
                        genParams[genParams.Length - 1] = GetCliType(iFunc.ReturnType);
                    }
                    var prms = iFunc.Parameters;
                    for (int i = 0; i < prms.Length; i++)
                    {
                        genParams[i] = GetCliType(prms[i].Type);
                    }

                    MethodInfo invokeMethod;
                    ConstructorInfo ctorInfo;

                    invokeMethod = delType.GetMethod("Invoke");
                    ctorInfo = delType.GetConstructor(new Type[] { typeof(object), typeof(IntPtr)});
                    if (genParams.Length > 0)
                    {
                        delType = delType.MakeGenericType(genParams);
                        // TODO: do this without try/catch
                        try
                        {
                            invokeMethod = delType.GetMethod("Invoke");
                            ctorInfo = delType.GetConstructor(new Type[] { typeof(object), typeof(IntPtr) });
                        }
                        catch (NotSupportedException)
                        {
                            invokeMethod = TypeBuilder.GetMethod(delType, invokeMethod);
                            ctorInfo = TypeBuilder.GetConstructor(delType, ctorInfo);
                        }
                    }

                    info = new CliFuncionTypeInfo()
                    {
                        invokeMethod = invokeMethod
                    };
                    info.cliType = delType;
                    info.constructor = ctorInfo;
                    iFunc.UserData = info;
                    return delType;
                }

                throw new InvalidOperationException("Non-registered type being used.");
            }
            return info.cliType;
        }

        private class LocalDelegateInfo: IEquatable<LocalDelegateInfo>
        {
            public bool hasReturnValue;
            public bool[] isParamByRef;

            public LocalDelegateInfo(ITFunctionType body)
            {
                hasReturnValue = body.ReturnType != null;

                var prms = body.Parameters;
                var prms2 = new bool[prms.Length];
                for (int i = 0; i < prms.Length; i++)
                {
                    prms2[i] = prms[i].IsByRef;
                }
                isParamByRef = prms2;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as LocalDelegateInfo);
            }

            public bool Equals(LocalDelegateInfo other)
            {
                if (hasReturnValue != other.hasReturnValue)
                    return false;
                var ls1 = isParamByRef;
                var ls2 = other.isParamByRef;
                if (ls1.Length != ls2.Length)
                    return false;
                for (int i = 0; i < ls2.Length; i++)
                    if (ls1[i] != ls2[i])
                        return false;
                return true;
            }

            public override int GetHashCode()
            {
                int hash = 0;
                foreach (var b in isParamByRef)
                {
                    if (b)
                        hash += 1;
                    hash <<= 1;
                }
                if (hasReturnValue)
                    hash += 1;
                return hash;
            }

            public bool IsAllByVal()
            {
                foreach (var b in isParamByRef)
                    if (b)
                        return false;
                return true;
            }
        }

        // registers something like "T Func<A,B,C>(ref A param1, B, ...)"
        private Dictionary<LocalDelegateInfo, Type> localDelegates = new Dictionary<LocalDelegateInfo, Type>();
        private Type RegisterDelegateTemplate(LocalDelegateInfo info)
        {
            Type del;

            if (info.IsAllByVal())
            {
                if (info.hasReturnValue)
                {
                    switch (info.isParamByRef.Length)
                    {
                        case 0:
                            return typeof(Queen.Kuin.CompilerServices.Func0<>);
                        case 1:
                            return typeof(Queen.Kuin.CompilerServices.Func1<,>);
                        case 2:
                            return typeof(Queen.Kuin.CompilerServices.Func2<,,>);
                        case 3:
                            return typeof(Queen.Kuin.CompilerServices.Func3<,,,>);
                        case 4:
                            return typeof(Queen.Kuin.CompilerServices.Func4<,,,,>);
                        case 5:
                            return typeof(Queen.Kuin.CompilerServices.Func5<,,,,,>);
                        case 6:
                            return typeof(Queen.Kuin.CompilerServices.Func6<,,,,,,>);
                        case 7:
                            return typeof(Queen.Kuin.CompilerServices.Func7<,,,,,,,>);
                        case 8:
                            return typeof(Queen.Kuin.CompilerServices.Func8<,,,,,,,,>);
                    }
                }
                else
                {
                    switch (info.isParamByRef.Length)
                    {
                        case 0:
                            return typeof(Queen.Kuin.CompilerServices.Action0);
                        case 1:
                            return typeof(Queen.Kuin.CompilerServices.Action1<>);
                        case 2:
                            return typeof(Queen.Kuin.CompilerServices.Action2<,>);
                        case 3:
                            return typeof(Queen.Kuin.CompilerServices.Action3<,,>);
                        case 4:
                            return typeof(Queen.Kuin.CompilerServices.Action4<,,,>);
                        case 5:
                            return typeof(Queen.Kuin.CompilerServices.Action5<,,,,>);
                        case 6:
                            return typeof(Queen.Kuin.CompilerServices.Action6<,,,,,>);
                        case 7:
                            return typeof(Queen.Kuin.CompilerServices.Action7<,,,,,,>);
                        case 8:
                            return typeof(Queen.Kuin.CompilerServices.Action8<,,,,,,,>);
                    }
                }
            }

            if (localDelegates.TryGetValue(info, out del))
            {
                return del;
            }

            TypeBuilder t = module.DefineType("LocalDelegate" + localDelegates.Count.ToString(),
                TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.Public |
                TypeAttributes.Sealed | TypeAttributes.NotPublic, typeof(MulticastDelegate));

            var genTypeNames = new string[info.isParamByRef.Length + (info.hasReturnValue ? 1 : 0)];
            for (int i = 0; i < genTypeNames.Length; i++)
                genTypeNames[i] = "T" + i.ToString();
            var gens = t.DefineGenericParameters(genTypeNames);

            ConstructorBuilder ctor = t.DefineConstructor(MethodAttributes.RTSpecialName | 
                MethodAttributes.HideBySig | MethodAttributes.Public, CallingConventions.Standard, 
                new Type[] { typeof(object), typeof(System.IntPtr) });
            ctor.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

            Type[] paramTypes = new Type[info.isParamByRef.Length];

            for (int i = 0; i < paramTypes.Length; i++)
            {
                if (info.isParamByRef[i])
                {
                    paramTypes[i] = gens[i].MakeByRefType();
                }
                else
                {
                    paramTypes[i] = gens[i];
                }
            }

            var meth = t.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.HideBySig |
                MethodAttributes.NewSlot | MethodAttributes.Virtual, info.hasReturnValue ? gens[gens.Length - 1] : null, paramTypes);
            meth.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

            del = t.CreateType();
            //delayedTypeCompletions.Add(new TypeCompletor() { builder = t });

            localDelegates[info] = del;

            return del;
        }

        private void RegisterEnumItems(ITClassType typ, EnumBuilder builder)
        {
            ITPrimitiveTypeType primType = typ.UnderlyingEnumType.Type;
            foreach (ITEntity item in typ.GetChildEntities())
            {
                ITGlobalVariableEntity var = item as ITGlobalVariableEntity;
                ITValueExpression val = var.InitialValue as ITValueExpression;
                object vl = val.Value;
                switch (primType)
                {
                    case ITPrimitiveTypeType.Bool:
                    case ITPrimitiveTypeType.Int8:
                        vl = Convert.ToSByte(vl);
                        break;
                    case ITPrimitiveTypeType.Int16:
                        vl = Convert.ToInt16(vl);
                        break;
                    case ITPrimitiveTypeType.Int32:
                        vl = Convert.ToInt32(vl);
                        break;
                    case ITPrimitiveTypeType.Int64:
                    case ITPrimitiveTypeType.Integer:
                        vl = Convert.ToInt64(vl);
                        break;
                    case ITPrimitiveTypeType.UInt8:
                        vl = Convert.ToByte(vl);
                        break;
                    case ITPrimitiveTypeType.Char:
                    case ITPrimitiveTypeType.UInt16:
                        vl = Convert.ToUInt16(vl);
                        break;
                    case ITPrimitiveTypeType.UInt32:
                        vl = Convert.ToUInt32(vl);
                        break;
                    case ITPrimitiveTypeType.UInt64:
                        vl = Convert.ToUInt64(vl);
                        break;
                }
                FieldBuilder fld = builder.DefineLiteral(item.Name, vl);
                item.UserData = new CliGlobalVariableInfo() { field = fld, containedType = builder };
            }
        }

        private void RegisterEnumItems(ITClassType typ, TypeBuilder builder)
        {
            ITPrimitiveTypeType primType = typ.UnderlyingEnumType.Type;
            foreach (ITEntity item in typ.GetChildEntities())
            {
                ITGlobalVariableEntity var = item as ITGlobalVariableEntity;
                ITValueExpression val = var.InitialValue as ITValueExpression;
                object vl = val.Value;
                FieldBuilder fb = builder.DefineField(item.Name, builder,
                    FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.Public); 
                
                switch(primType){
                    case ITPrimitiveTypeType.Bool:
                    case ITPrimitiveTypeType.Int8:
                        vl = Convert.ToSByte(vl);
                        break;
                    case ITPrimitiveTypeType.Int16:
                        vl = Convert.ToInt16(vl);
                        break;
                    case ITPrimitiveTypeType.Int32:
                        vl = Convert.ToInt32(vl);
                        break;
                    case ITPrimitiveTypeType.Int64:
                    case ITPrimitiveTypeType.Integer:
                        vl = Convert.ToInt64(vl);
                        break;
                    case ITPrimitiveTypeType.UInt8:
                        vl = Convert.ToByte(vl);
                        break;
                    case ITPrimitiveTypeType.Char:
                    case ITPrimitiveTypeType.UInt16:
                        vl = Convert.ToUInt16(vl);
                        break;
                    case ITPrimitiveTypeType.UInt32:
                        vl = Convert.ToUInt32(vl);
                        break;
                    case ITPrimitiveTypeType.UInt64:
                        vl = Convert.ToUInt64(vl);
                        break;
                }
                fb.SetConstant(vl);
                item.UserData = new CliGlobalVariableInfo() { field = fb, containedType = builder };
            }
        }

        // registers local types
        private void RegisterTypes(ITFunctionBody func, TypeBuilder outerClass, IDelayedRegistrar completor, int numOuterGenericTypeParameters)
        {
            RegisterTypes(func.Block, outerClass, completor, numOuterGenericTypeParameters);
        }

        private void RegisterTypes(ITBlock block, TypeBuilder outerClass, IDelayedRegistrar outerCompletor, int numOuterGenericTypeParameters)
        {
            RegisterTypes(block.Children.Values, outerClass, outerCompletor, numOuterGenericTypeParameters, true);
            foreach (ITStatement stat in block.Statements)
            {
                ITIfStatement ifs = stat as ITIfStatement;
                if (ifs != null)
                {
                    if (ifs.FalseBlock != null) RegisterTypes(ifs.FalseBlock, outerClass, outerCompletor, numOuterGenericTypeParameters);
                    if (ifs.TrueBlock != null) RegisterTypes(ifs.TrueBlock, outerClass, outerCompletor, numOuterGenericTypeParameters);
                    continue;
                }
                ITBlockStatement blk = stat as ITBlockStatement;
                if (blk != null)
                {
                    RegisterTypes(blk.Block, outerClass, outerCompletor, numOuterGenericTypeParameters);
                    continue;
                }

                // TODO: RegisterTypes for try/switch
            }
        }

        private void RegisterNonRootType(ITClassEntity type, TypeBuilder outerClass, IDelayedRegistrar outerCompleter, int numOuterClassGenericTypeParameters, bool alwaysPrivate)
        {
            // imported?
            if (type.Type is CliClassType)
            {
                return;
            }

            ITClassType cls = type.Type;

            ITGenericTypeParameter[] itGenParams = cls.GetStackedGenericParameters();
            string[] genParamNames;

            if (itGenParams != null)
            {
                genParamNames = new string[itGenParams.Length];
                for (int i = 0; i < genParamNames.Length; i++)
                    genParamNames[i] = itGenParams[i].Name;
            }
            else
            {
                genParamNames = new string[] { };
            }

            if (cls.UnderlyingEnumType != null)
            {
                TypeAttributes attrs = TypeAttributes.Sealed;
                attrs |= (alwaysPrivate || cls.Entity.IsPrivate) ? TypeAttributes.NestedPrivate : TypeAttributes.NestedPublic;

                TypeBuilder builder = outerClass.DefineNestedType(type.Name, attrs, typeof(Enum));
                FieldBuilder fb = builder.DefineField("value__", GetCliType(cls.UnderlyingEnumType), FieldAttributes.Private | FieldAttributes.SpecialName);
                fb.SetConstant((long)0);

                if (genParamNames.Length > 0 && false)
                {
                    GenericTypeParameterBuilder[] genParams = builder.DefineGenericParameters(genParamNames);

                    if (itGenParams.Length != genParams.Length)
                        throw new InvalidOperationException();

                    for (int i = 0; i < genParamNames.Length; i++)
                    {
                        itGenParams[i].UserData = new CliTypeInfo(itGenParams[i], genParams[i]);
                    }
                }

                RegisterEnumItems(cls, builder);

                CliTypeInfo info = new CliTypeInfo();
                info.cliType = builder;
                cls.UserData = info;

                builder.CreateType();
                //delayedTypeCompletions.Add(new TypeCompletor() { builder = builder });
            }
            else
            {
                TypeAttributes attrs = cls.IsInterface() ? (TypeAttributes.Interface | TypeAttributes.Abstract) : TypeAttributes.Class;
                attrs |= (alwaysPrivate || cls.Entity.IsPrivate) ? TypeAttributes.NestedPrivate : TypeAttributes.NestedPublic;
                // TODO: protected class

                TypeBuilder typ = outerClass.DefineNestedType(type.Name, attrs);
                CliTypeInfo info = new CliTypeInfo();
                info.cliType = typ;
                cls.UserData = info;

                var completor = new TypeCompletor();
                completor.builder = typ;
                completor.containingClass = outerCompleter;
                info.delayedTypeCompletor = completor;

                if (genParamNames.Length > 0)
                {
                    GenericTypeParameterBuilder[] genParams = typ.DefineGenericParameters(genParamNames);
                    if (itGenParams.Length != genParams.Length)
                        throw new InvalidOperationException();
                    for (int i = 0; i < genParamNames.Length; i++)
                    {
                        itGenParams[i].UserData = new CliTypeInfo(itGenParams[i], genParams[i]);
                    }
                }

                RegisterTypes(cls.GetChildEntities(), typ, completor ,type.Type.GetGenericParameters().Length, false);
                RegisterMembers(cls, typ);

                delayedHierarchizators.Add(new Hierarchizator(this) { subclass = cls });
                delayedTypeCompletions.Add(completor);
            }
        }

        private void RegisterRootType(ITClassEntity type, ModuleBuilder modBuilder, IDelayedRegistrar rootRegistrar, string rootGlobalScopeNamespace)
        {
            // imported?
            if (type.Type is CliClassType)
            {
                return;
            }

            ITClassType cls = type.Type;
            if (cls.UnderlyingEnumType != null)
            {
                string name = rootGlobalScopeNamespace + "." + type.Name;
                TypeAttributes attrs = type.IsPrivate ? TypeAttributes.NotPublic :
                    TypeAttributes.Public;

                EnumBuilder builder = modBuilder.DefineEnum(name, attrs, GetCliType(cls.UnderlyingEnumType));
                RegisterEnumItems(cls, builder);

                // there should be no generic parameters...

                CliTypeInfo info = new CliTypeInfo();
                info.cliType = builder;
                cls.UserData = info;

                builder.CreateType();
                //delayedTypeCompletions.Add(new EnumCompletor() { builder = builder });
            }
            else
            {
                string name = rootGlobalScopeNamespace + "." + type.Name;
                TypeAttributes attrs = cls.IsInterface() ? (TypeAttributes.Interface | TypeAttributes.Abstract) : TypeAttributes.Class;
                attrs |= type.IsPrivate ? TypeAttributes.NotPublic :
                    TypeAttributes.Public;

                // TODO: private class

                TypeBuilder typ = modBuilder.DefineType(name, attrs);
                CliTypeInfo info = new CliTypeInfo();
                info.cliType = typ;
                cls.UserData = info;

                // generic parameters
                var itGenParams = cls.GenericTypeParameters;
                var genParamNames = new string[cls.GenericTypeParameters.Length];
                for (int i = 0; i < genParamNames.Length; i++)
                    genParamNames[i] = itGenParams[i].Name;

                if (genParamNames.Length > 0)
                {
                    GenericTypeParameterBuilder[] genParams = typ.DefineGenericParameters(genParamNames);
                    for (int i = 0; i < genParamNames.Length; i++)
                    {
                        itGenParams[i].UserData = new CliTypeInfo(itGenParams[i], genParams[i]);
                    }
                }

                var completor = new TypeCompletor();
                completor.builder = typ;
                completor.containingClass = rootRegistrar;
                info.delayedTypeCompletor = completor;

                RegisterTypes(cls.GetChildEntities(), typ, completor, type.Type.GetGenericParameters().Length, false);
                RegisterMembers(cls, typ);

                delayedHierarchizators.Add(new Hierarchizator(this) { subclass = cls });

                delayedTypeCompletions.Add(completor);
            }
        }

        private class MemberDefaultConstructorRegistrar : IDelayedRegistrar
        {
            public CliCompiler compiler;
            public TypeBuilder outerType;
            public ITType outerITType;
            public void Register()
            {
                // there's explicit constructor?
                if ((outerITType.UserData as CliTypeInfo).constructor != null)
                    return;

                MethodAttributes attr = MethodAttributes.Public;
                ConstructorBuilder constr = outerType.DefineConstructor(attr, CallingConventions.HasThis, new Type[] {});
                compiler.delayedCompilations.Add(new FunctionCompiler(
                    compiler, outerITType, null, constr));

                (outerITType.UserData as CliTypeInfo).constructor = constr;
            }

        }

        private class MemberFunctionRegistrar : IDelayedRegistrar
        {
            public CliCompiler compiler;
            public TypeBuilder outerType;
            public ITType outerITType;
            public ITMemberFunction member;
            public void Register()
            {
                MethodAttributes attr = MethodAttributes.Virtual;
                if (member.IsPrivate) attr |= MethodAttributes.Family;
                if (member.IsPublic) attr |= MethodAttributes.Public;
                if (member.IsAbstract) attr |= MethodAttributes.Abstract;

                if (member.Name == "Ctor")
                {
                    attr &= ~MethodAttributes.Virtual;

                    var iParams = member.Body.Parameters;
                    var cliParams = new Type[iParams.Count];

                    if (member.Body.ReturnType != null)
                    {
                        throw new InvalidOperationException("constructor should not have a returned value.");
                    }
                    if (cliParams.Length > 0)
                    {
                        throw new NotSupportedException("Constructor with a parameter is forbidden.");
                    }
                    if (member.IsAbstract)
                    {
                        throw new InvalidOperationException("abstract constructor is not permitted.");
                    }
                    for (int i = 0; i < cliParams.Length; i++)
                    {
                        var itParam = iParams[i];
                        Type t = compiler.GetCliType(itParam.Type);
                        if (itParam.IsByRef)
                            t = t.MakeByRefType();
                        cliParams[i] = t;
                        itParam.UserData = i;
                    }

                    ConstructorBuilder constr = outerType.DefineConstructor(attr, CallingConventions.HasThis, cliParams);
                    member.UserData = new CliConstructorInfo()
                    {
                        constructor = constr, ownerITType = outerITType
                    };
                    compiler.CompleteFunctionRegistration(constr, member.Body);
                    compiler.delayedCompilations.Add(new FunctionCompiler(
                        compiler, outerITType, member.Body, constr));

                    (outerITType.UserData as CliTypeInfo).constructor = constr;
                }
                else
                {
                    string actualName = member.Name;
                    if (member.Name == "ToStr")
                    {
                        actualName = "ToString";
                    }
                    MethodBuilder method = outerType.DefineMethod(actualName, attr);
                    member.UserData = new CliMemberFunctionInfo()
                    {
                         method = method, ownerITType = outerITType
                    };
                    compiler.CompleteFunctionRegistration(method, member.Body);
                    if (!member.IsAbstract)
                    {
                        compiler.delayedCompilations.Add(new FunctionCompiler(
                            compiler, outerITType, member.Body, method));
                    }
                }
            }
        }
        private class MemberPropertyRegistrar : IDelayedRegistrar
        {
            public CliCompiler compiler;
            public TypeBuilder outerType;
            public ITMemberProperty member;
            public void Register()
            {
                throw new NotImplementedException();
            }
        }
        private class MemberVariableRegistrar : IDelayedRegistrar
        {
            public CliCompiler compiler;
            public TypeBuilder outerType;
            public ITMemberVariable member;
            public ITType outerITType;
            public void Register()
            {
                FieldAttributes attr = 0;
                if (!member.IsPrivate) attr |= FieldAttributes.Public;
                FieldBuilder field = outerType.DefineField(member.Name, compiler.GetCliType(member.Type), attr);
                member.UserData = new CliMemberVariableInfo()
                {
                    field = field,
                    ownerITType = outerITType
                };
            }
        }

        private void CompleteFunctionRegistration(MethodBuilder method, ITFunctionBody func)
        {
            var itGenParams = func.GenericParameters;
            var genParamNames = new string[func.GenericParameters != null ? func.GenericParameters.Length : 0];
            for (int i = 0; i < genParamNames.Length; i++)
                genParamNames[i] = itGenParams[i].Name;

            if (genParamNames.Length > 0)
            {
                GenericTypeParameterBuilder[] genParams = method.DefineGenericParameters(genParamNames);
                for (int i = 0; i < genParamNames.Length; i++)
                {
                    itGenParams[i].UserData = new CliTypeInfo(itGenParams[i], genParams[i]);
                }
            }

            if (func.ReturnType != null)
            {
                method.SetReturnType(GetCliType(func.ReturnType));
            }

            var itParams = func.Parameters;
            var methodParams = new Type[itParams.Count];
            for (int i = 0; i < methodParams.Length; i++)
            {
                var itParam = itParams[i];
                Type t = GetCliType(itParam.Type);
                if (itParam.IsByRef)
                    t = t.MakeByRefType();
                methodParams[i] = t;
                itParam.UserData = i;
            }

            method.SetParameters(methodParams);
            func.UserData = method;
        }

        private void CompleteFunctionRegistration(ConstructorBuilder method, ITFunctionBody func)
        {
            func.UserData = method;
        }

        private void RegisterMembers(ITClassType cls, TypeBuilder type)
        {
            foreach (ITMemberFunction func in cls.GetMemberFunctions())
            {
                delayedRegistrars.Add(new MemberFunctionRegistrar()
                {
                    outerType = type,
                    member = func,
                    compiler = this,
                    outerITType = cls
                });
            }
            foreach (ITMemberProperty prop in cls.GetMemberProperties())
            {
                delayedRegistrars.Add(new MemberPropertyRegistrar()
                {
                    outerType = type,
                    member = prop,
                    compiler = this
                });
            }
            foreach (ITMemberVariable var in cls.GetMemberVariables())
            {
                delayedRegistrars.Add(new MemberVariableRegistrar()
                {
                    outerType = type,
                    member = var,
                    compiler = this, outerITType = cls
                });
            }
            if (!cls.IsInterface())
            {
                delayedRegistrars.Add(new MemberDefaultConstructorRegistrar()
                {
                    outerType = type,
                    compiler = this,
                    outerITType = cls
                });
            }
        }

        private class GlobalFunctionRegistrar : IDelayedRegistrar
        {
            public CliCompiler compiler;
            public TypeBuilder outerType;
            public ITFunctionEntity entity;
            public bool alwaysPrivate;
            public void Register()
            {
                MethodAttributes attr = MethodAttributes.Static;
                if (alwaysPrivate || entity.IsPrivate) {
                    attr |= MethodAttributes.Assembly;
                }else if(entity.IsPublic){
                    attr |= MethodAttributes.Public;
                }
                MethodBuilder method = outerType.DefineMethod(entity.Name, attr);
                entity.UserData = new CliGlobalFunctionInfo()
                {
                     method = method, containedType = outerType
                };
                compiler.CompleteFunctionRegistration(method, entity.Body);
                compiler.delayedCompilations.Add(new FunctionCompiler(
                    compiler, null, entity.Body, method));
            }
        }

        private class GlobalVariableRegistrar : IDelayedRegistrar
        {

            public CliCompiler compiler;
            public TypeBuilder outerType;
            public ITGlobalVariableEntity entity;
            public bool alwaysPrivate;
            public void Register()
            {
                FieldAttributes attr = FieldAttributes.Static;
                if (entity.IsPrivate)
                {
                    attr |= FieldAttributes.Assembly;
                }
                else
                {
                    attr |= FieldAttributes.Public;
                }

                FieldBuilder field = outerType.DefineField(entity.Name, compiler.GetCliType(entity.Type), attr);
                entity.UserData = new CliGlobalVariableInfo()
                {
                    field = field, containedType = outerType
                };
            }
        }

        private void RegisterTypes(IEnumerable<ITEntity> entities, TypeBuilder outerType, IDelayedRegistrar outerCompletor, int numOuterTypeGenericTypeParameters, bool alwaysPrivate)
        {
            

            foreach (ITEntity entity in entities)
            {
                ITFunctionEntity func = entity as ITFunctionEntity;
                if (func != null)
                {
                    RegisterTypes(func.Body, outerType, outerCompletor, 0);
                    delayedRegistrars.Add(new GlobalFunctionRegistrar()
                    {
                        outerType = outerType,
                        entity = func,
                        alwaysPrivate = alwaysPrivate,
                        compiler = this
                    });
                    continue;
                }

                ITClassEntity cls = entity as ITClassEntity;
                if (cls != null)
                {
                    RegisterNonRootType(cls, outerType, outerCompletor,numOuterTypeGenericTypeParameters, alwaysPrivate);
                    continue;
                }

                ITGlobalVariableEntity gvar = entity as ITGlobalVariableEntity;
                if (gvar != null)
                {
                    delayedRegistrars.Add(new GlobalVariableRegistrar()
                    {
                        outerType = outerType,
                        entity = gvar,
                        alwaysPrivate = alwaysPrivate,
                        compiler = this
                    });
                    // TODO: initialize non-const class global variable
                    continue;
                }
                // variable is not defined now
            }
        }

        private void RegisterTypes(ITRootGlobalScope scope, ModuleBuilder modBuilder)
        {
            string scopeNameSpace = options.RootNamespace + "." + scope.Name;
            CliRootGlobalScopeInfo info = new CliRootGlobalScopeInfo();
            info.rootNamespace = scopeNameSpace;

            string globalClassName = "Global_" + scope.Name;
            TypeBuilder gclass = modBuilder.DefineType(scopeNameSpace + "." + globalClassName,
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed);


            // add [Global] custom attribute so that the members of this class are recognized as global entities.
            var attr = new CustomAttributeBuilder(typeof(Queen.Kuin.GlobalAttribute).GetConstructor(new Type[] { }), new object[] { });
            gclass.SetCustomAttribute(attr);

            info.globalClassType = gclass;
            scope.UserData = info;

            TypeInitializerCreator initializer = new TypeInitializerCreator()
            {
                builder = gclass,
                compiler = this,
                thisType = null,
                vars = new List<ITGlobalVariableEntity>()
            };

            var completor = new TypeCompletor();
            completor.builder = gclass;

            delayedTypeCompletions.Add(completor);

            foreach (ITEntity entity in scope.Children.Values)
            {
                // imported members?
                if (entity is CliGlobalFunctionEntity)
                    continue;
                if (entity is CliClassEntity)
                    continue;

                ITFunctionEntity func = entity as ITFunctionEntity;
                if (func != null)
                {
                    RegisterTypes(func.Body, info.globalClassType, completor, 0); 
                    delayedRegistrars.Add(new GlobalFunctionRegistrar()
                    {
                        outerType = gclass,
                        entity = func,
                        alwaysPrivate = false,
                        compiler = this
                    });
                    continue;
                }

                ITClassEntity cls = entity as ITClassEntity;
                if (cls != null)
                {
                    RegisterRootType(cls, modBuilder, completor, scopeNameSpace);
                    continue;
                }

                ITGlobalVariableEntity gvar = entity as ITGlobalVariableEntity;
                if (gvar != null)
                {
                    if (gvar.UserData is CliGlobalVariableInfo)
                    {
                        // imported variable (this doesn't have a unique entity type)
                        continue;
                    }
                    delayedRegistrars.Add(new GlobalVariableRegistrar()
                    {
                        outerType = gclass,
                        entity = gvar,
                        alwaysPrivate = false,
                        compiler = this
                    });
                    initializer.vars.Add(gvar);
                    continue;
                }
            }

            typeInitializerCreator.Add(initializer);

            foreach (IDelayedRegistrar reg in delayedHierarchizators)
                reg.Register();
            delayedHierarchizators.Clear();
        }

        private Assembly ResolveEvent(object sender, ResolveEventArgs args)
        {
            // TODO: handle this
            System.Diagnostics.Debug.WriteLine("TypeResolve: " + args.Name);
            return module.Assembly;
        }

        private void DoDelayedRegistration()
        {
            ResolveEventHandler resolver = new ResolveEventHandler(ResolveEvent);
            AppDomain.CurrentDomain.TypeResolve += resolver;
            try
            {
                foreach (IDelayedRegistrar reg in delayedRegistrars)
                {
                    reg.Register();
                }
                delayedRegistrars.Clear();

                foreach (IDelayedRegistrar reg in typeInitializerCreator)
                {
                    reg.Register();
                }
                typeInitializerCreator.Clear();
            }
            finally
            {
                AppDomain.CurrentDomain.TypeResolve -= resolver;
            }
        }

        private void DoCompilation()
        {
            foreach (IDelayedRegistrar reg in delayedCompilations)
                reg.Register();
            delayedCompilations.Clear();
        }

        private void CompleteTypes()
        {
            foreach (IDelayedRegistrar reg in delayedTypeCompletions)
                reg.Register();
            delayedTypeCompletions.Clear();
            CompletePrivateDataType();
        }
    }
}
