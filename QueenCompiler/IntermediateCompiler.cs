using System;
using System.Collections.Generic;
using System.Text;
using Queen.Language.IntermediateTree;
using Queen.Language.CodeDom;

namespace Queen.Language
{
    public partial class IntermediateCompiler
    {
        private ITRoot root;
        public event IntermediateCompileErrorEventHandler ErrorReported;
        private Dictionary<string, ITType> builtinTypes = new Dictionary<string, ITType>();
        private Dictionary<ITPrimitiveTypeType, ITPrimitiveType> primitives = new Dictionary<ITPrimitiveTypeType, ITPrimitiveType>();
        public CompilerOptions Options = new CompilerOptions();
        private ConstantFold constantFold;

        protected void RegisterBuiltinType(string name, ITType type)
        {
            builtinTypes[name] = type;
        }

        public virtual ITType GetNumericExceptionType()
        {
            throw new NotImplementedException();
        }

        public virtual ITRoot CreateITRoot()
        {
            return new ITRoot();
        }

        public virtual ITClassType GetRootClass()
        {
            return null;
        }

        public virtual ITArrayType CreateArrayType(ITType elementType, int numDimensions)
        {
            return new ITArrayType(this)
            {
                 Dimensions = numDimensions, ElementType = elementType
            };
        }

        private class DefaultPrimitiveType : ITPrimitiveType
        {
            public DefaultPrimitiveType(IntermediateCompiler iCompiler, ITPrimitiveTypeType t) : base(iCompiler, t) { }
        }
        public virtual ITPrimitiveType CreatePrimitiveType(ITPrimitiveTypeType type)
        {
            return new DefaultPrimitiveType(this, type);
        }

        public ITPrimitiveType GetPrimitiveType(ITPrimitiveTypeType type)
        {
            ITPrimitiveType typ;
            if (primitives.TryGetValue(type, out typ))
            {
                return typ;
            }
            typ = CreatePrimitiveType(type);
            primitives[type] = typ;
            return typ;
        }

        public virtual ITFunctionType CreateFunctionType(ITType returnType, ITFunctionParameter[] prms) {
            return new ITFunctionType(this, returnType, prms);
        }

        protected internal ITFunctionType CreateFunctionTypeForGlobalFunction(ITFunctionEntity entity, ITType[] genericInstantiation)
        {
            // FIXME: genericInstantiation is not used
            var baseParams = entity.Body.Parameters;
            var instParams = new ITFunctionParameter[baseParams.Count];
            var typs = entity.GetParameterTypes();
            for (int i = 0; i < instParams.Length; i++)
            {
                var prm = new ITFunctionParameter();
                var basePrm = baseParams[i];
                prm.Type = typs[i];
                prm.IsByRef = basePrm.IsByRef;
                instParams[i] = prm;
            }
            return CreateFunctionType(entity.GetReturnType(), instParams);
        }
        protected internal ITFunctionType CreateFunctionTypeForMemberFunction(ITMemberFunction member, ITType[] genericInstantiation)
        {
            var mutat = new GenericTypeMutator(member.Body.GenericParameters, genericInstantiation);
            var baseParams = member.Body.Parameters;
            var mutatParams = member.GetParameterTypes();
            var instParams = new ITFunctionParameter[baseParams.Count];
            for (int i = 0; i < instParams.Length; i++)
            {
                var prm = new ITFunctionParameter();
                var basePrm = baseParams[i];
                prm.Type = mutat.Mutate(mutatParams[i]);
                prm.IsByRef = basePrm.IsByRef;
                instParams[i] = prm;
            }
            return CreateFunctionType(mutat.Mutate(member.GetReturnType()), instParams);
        }

        public void ReportError(string message, CodeLocation loc)
        {
            ErrorReported(this, new IntermediateCompileErrorEventArgs(loc.SourceFile, loc.Line, loc.Column, message));
        }

        private class ResolveEntityResult
        {
            public ITEntity entity;
            public ITType[] GenericParameters;
            public bool hasEnoughGenericParametersExceptTop;
            public bool hasEnoughGenericParametersForTop;
        }

        public ITType GetBuiltinType(string name)
        {
            return builtinTypes[name];
        }

        public string GetBuiltinTypeName(ITType typ)
        {
            foreach (var pair in builtinTypes)
            {
                if (pair.Value == typ)
                    return pair.Key;
            }
            return null;
        }

        public ITPrimitiveType GetIndexerType()
        {
            return (ITPrimitiveType)builtinTypes["int32"];
        }

        public ITType GetIteratorType()
        {
            ITType typ;
            if (builtinTypes.TryGetValue("iter", out typ))
            {
                return typ;
            }
            return null;
        }

        public ITType GetExceptionType()
        {
            var r = root.GetRootGlobalScope("Q");
            ITEntity ent = r.GetChildEntity("CExcpt");
            return ((ITClassEntity)ent).Type;
        }

        private ITType ResolveType(CodeType domType, ITScope scope)
        {
            if (domType == null)
                return null;
            CodeEntityType entType = domType as CodeEntityType;
            CodeLocation loc = domType.Location;
            ITType[] gparams = null;
            if (entType != null)
            {
                CodeEntitySpecifier spec = entType.Entity;
                ITEntity ent;

                if (spec is CodeGenericsEntitySpecifier)
                {
                    CodeGenericsEntitySpecifier genSpec = spec as CodeGenericsEntitySpecifier;
                    CodeEntitySpecifier inEntspec = genSpec.GenericEntity;

                    // builtin type with generic parameters?
                    // NOTE: this cannot be another generic parameter; they cannot have generic parameters
                    var inEntspecImpl = inEntspec as CodeImplicitEntitySpecifier;
                    if (inEntspecImpl != null)
                    {
                        string name = inEntspecImpl.Idenfitifer.Text;

                        // check for built-in type
                        ITType outType;
                        if (builtinTypes.TryGetValue(name, out outType))
                        {
                            gparams = new ITType[genSpec.GenericParameters.Count];
                            for (int i = 0; i < gparams.Length; i++)
                            {
                                ITType typRes = ResolveType(genSpec.GenericParameters[i], scope);
                                if (typRes == null)
                                {
                                    return null;
                                }
                                gparams[i] = typRes;
                            }

                            int gparamdefs = outType.GetGenericParameters().Length;
                            if (gparamdefs != gparams.Length)
                            {
                                ReportError(string.Format(Properties.Resources.ICWrongNumGenericParameters,
                                    gparams.Length, gparamdefs), loc);

                                // FIXME: save as many parameters as possible in case of errors?
                                gparams = new ITType[gparamdefs];
                                for (int i = gparams.Length - 1; i >= 0; i--)
                                    gparams[i] = builtinTypes["int"];
                            }
                            outType = outType.MakeGenericType(gparams);
                            return outType;
                        }
                    }

                    // must be normal entity
                    // auto search
                    ResolveEntityResult entResult = ResolveEntity(spec, scope);
                    if (entResult == null)
                    {
                        return null;
                    }
                    if (!(entResult.hasEnoughGenericParametersForTop && entResult.hasEnoughGenericParametersExceptTop))
                    {
                        ReportError(Properties.Resources.ICGenericGenericTypeParameter, spec.Location);
                    }
                    ent = entResult.entity;
                    gparams = entResult.GenericParameters;
                }
                else if (spec is CodeImplicitEntitySpecifier)
                {
                    string name = ((CodeImplicitEntitySpecifier)spec).Idenfitifer.Text;

                    // check for built-in type
                    ITType outType;
                    if (builtinTypes.TryGetValue(name, out outType))
                    {
                        gparams = new ITType[] { };
                        outType = outType.MakeGenericType(gparams); // to raise an error when generic parameters are required
                        return outType;
                    }

                    // check for template parameter type defined in this/outer class and functions
                    // this can be done after built-in type search because all built-in type names are 
                    // reserved and thus they cannot be used as a name of a generic parameter.
                    // FIXME: improve performance
                    {
                        ITScope scp = scope;
                        while (scp != null)
                        {
                            ITType typeScope = scp as ITType;
                            if (typeScope != null)
                            {
                                var gprms = typeScope.GetGenericParameters();
                                foreach (var gprm in gprms)
                                {
                                    if (gprm.Name.Equals(name))
                                        return gprm;
                                }
                                break;
                            }

                            ITFunctionBody bodyScope = scp as ITFunctionBody;
                            if (bodyScope != null)
                            {
                                var gprms = bodyScope.GenericParameters;
                                if (gprms != null)
                                {
                                    foreach (var gprm in gprms)
                                    {
                                        if (gprm.Name.Equals(name))
                                            return gprm;
                                    }
                                }
                                // continue searching for type
                            }

                            scp = scp.ParentScope;
                        }
                    }
                    

                    // auto search
                    ResolveEntityResult entResult = ResolveEntity(spec, scope);
                    if (entResult != null)
                    {
                        if (!(entResult.hasEnoughGenericParametersForTop && entResult.hasEnoughGenericParametersExceptTop))
                        {
                            ReportError(Properties.Resources.ICGenericGenericTypeParameter, spec.Location);
                        }
                        ent = entResult.entity;
                        gparams = entResult.GenericParameters;
                    }
                    else
                    {
                        ent = null;
                    }

                    

                    // end if (spec is CodeImplicitEntitySpecifier)
                }
                else if (spec is CodeGlobalScopeSpecifier)
                {
                    ReportError(Properties.Resources.ICRootGlobalScopeIsNotType, domType.Location);
                    return null;
                }
                else // what is this?
                {
                    ResolveEntityResult entResult = ResolveEntity(spec, scope);
                    if (entResult == null)
                    {
                        return null;
                    }
                    if (!(entResult.hasEnoughGenericParametersForTop && entResult.hasEnoughGenericParametersExceptTop))
                    {
                        ReportError(Properties.Resources.ICGenericGenericTypeParameter, spec.Location);
                    }
                    ent = entResult.entity;
                    gparams = entResult.GenericParameters;
                }

                if (ent == null)
                {
                    return null;
                }

                {
                    ITType outType = null;
                    ITClassEntity classEntity = ent as ITClassEntity;
                    if (classEntity != null)
                    {
                        outType = classEntity.Type;
                    }

                    if (outType != null)
                    {
                        if (gparams == null)
                            gparams = new ITType[] { };
                        int gparamdefs = outType.GetGenericParameters().Length;
                        if (gparamdefs != gparams.Length)
                        {
                            ReportError(string.Format(Properties.Resources.ICWrongNumGenericParameters,
                                gparams.Length, gparamdefs), loc);

                            // FIXME: save as many parameters as possible in case of errors?
                            gparams = new ITType[gparamdefs];
                            for (int i = gparams.Length - 1; i >= 0; i--)
                                gparams[i] = builtinTypes["int"];
                        }
                        outType = outType.MakeGenericType(gparams);
                        return outType;
                    }
                    else
                    {
                        ReportError(string.Format(Properties.Resources.ICEntityIsNotType,
                            ent.ToString()), domType.Location);
                        return null;
                    }
                }
            }
            else if (domType is CodeArrayType)
            {
                ITType typ = ResolveType(((CodeArrayType)domType).ElementType, scope);
                ITArrayType arr = CreateArrayType(typ, ((CodeArrayType)domType).Dimensions);
                arr.Root = scope.Root;
                return arr;
            }
            else if (domType is CodeFunctionType)
            {
                var codeFun = (CodeFunctionType)domType;
                var ret = ResolveType(codeFun.ReturnType, scope);
                var codePrms = codeFun.Parameters;
                var iPrms = new ITFunctionParameter[codePrms != null ?codePrms.Count : 0];
                for (int i = 0; i < iPrms.Length; i++)
                {
                    var codePrm = codePrms[i];
                    var prm = new ITFunctionParameter();
                    prm.IsByRef = codePrm.IsByRef;
                    prm.Type = ResolveType(codePrm.Type, scope);
                    iPrms[i] = prm;
                }
                return CreateFunctionType(ret, iPrms);
            }
            else
            {
                throw new IntermediateCompilerException(Properties.Resources.InternalError);
            }
        }

        private ITRootGlobalScope ResolveRootGlobalScope(CodeGlobalScopeSpecifier g, ITScope scope)
        {
            if (g.Identifier == null)
            {
                // current source file
                while (scope != null && (!(scope is ITRootGlobalScope)))
                {
                    scope = scope.ParentScope;
                }
                if (scope == null)
                {
                    throw new IntermediateCompilerException(Properties.Resources.InternalError);
                }
                return (ITRootGlobalScope)scope;
            }
            else
            {
                string name = g.Identifier.Text;
                return root.GetRootGlobalScope(name);
            }
        }

        private ITScope ResolveScope(CodeEntitySpecifier spec, ITScope scope)
        {
            if (spec is CodeImplicitEntitySpecifier)
            {
                CodeImplicitEntitySpecifier impl = ((CodeImplicitEntitySpecifier)spec);
                string text = impl.Idenfitifer.Text;
                while (scope != null)
                {
                    ITEntity ent = scope.GetChildEntity(text);
                    if (ent != null)
                    {
                        if (ent is IITSubScopedEntity)
                        {
                            return ((IITSubScopedEntity)ent).Scope;
                        }
                        else
                        {
                            ReportError(string.Format(Properties.Resources.ICEntityDoesNotHaveScope,
                                ent.Name), spec.Location);
                            return null;
                        }
                    }
                    scope = scope.ParentScope;
                    // FIXME: don't go to global scope?
                }
                ReportError(string.Format(Properties.Resources.ICEntityNotFound, text), spec.Location);
                return null;
            }
            else if (spec is CodeGlobalScopeSpecifier)
            {
                CodeGlobalScopeSpecifier g = (CodeGlobalScopeSpecifier)spec;
                if (g.Identifier == null)
                {
                    // current source file
                    while (scope != null && (!(scope is ITRootGlobalScope)))
                    {
                        scope = scope.ParentScope;
                    }
                    if (scope == null)
                    {
                        throw new IntermediateCompilerException(Properties.Resources.InternalError);
                    }
                    return scope;
                }
                else
                {
                    string name = g.Identifier.Text;
                    return root.GetRootGlobalScope(name);
                }
            }
            else if (spec is CodeScopedEntitySpecifier)
            {
                CodeScopedEntitySpecifier scp = (CodeScopedEntitySpecifier)spec;
                ITScope par = ResolveScope(scp.ParentEntity, scope);
                if (par == null)
                {
                    return null;
                }
                ITEntity ent = scope.GetChildEntity(scp.Identifier.Text);
                if (ent != null)
                {
                    if (ent is IITSubScopedEntity)
                    {
                        return ((IITSubScopedEntity)ent).Scope;
                    }
                    else
                    {
                        ReportError(string.Format(Properties.Resources.ICEntityDoesNotHaveScope,
                            ent.Name), spec.Location);
                        return null;
                    }
                }
                else
                {
                    ReportError(string.Format(Properties.Resources.ICEntityNotFound, scp.Identifier.Text), spec.Location);
                    return null;
                }
            }
            else
            {
                throw new IntermediateCompilerException(Properties.Resources.InternalError);
            }
        }
        private ResolveEntityResult ResolveEntity(CodeEntitySpecifier spec, ITScope scope)
        {
            ResolveEntityResult res = null;
            if (spec is CodeGenericsEntitySpecifier)
            {
                CodeGenericsEntitySpecifier gen = (CodeGenericsEntitySpecifier)spec;
                ResolveEntityResult inner = ResolveEntity(gen.GenericEntity, scope);
                if (inner == null)
                {
                    return null;
                }
                if (!inner.hasEnoughGenericParametersExceptTop)
                {
                    ReportError(Properties.Resources.ICInvalidGenericTypeParameter, spec.Location);
                    return inner;
                }

                if (inner.GenericParameters == null)
                    inner.GenericParameters = new ITType[0];

                int numGenParams = 0;
                int targetGenParams = 0;
                ITClassEntity clsEntity = inner.entity as ITClassEntity;
                if (clsEntity != null)
                {
                    targetGenParams = clsEntity.Type.GetGenericParameters().Length;
                }
                else
                {
                    ITFunctionEntity fun = inner.entity as ITFunctionEntity;
                    targetGenParams = fun.Body.GetStackedGenericParameters().Length;
                }

                numGenParams = gen.GenericParameters.Count;

                if (targetGenParams != inner.GenericParameters.Length + numGenParams)
                {
                    ReportError(string.Format(Properties.Resources.ICWrongNumGenericParameters,
                        gen.GenericParameters.Count, numGenParams), spec.Location);
                    return inner;
                }

                if (numGenParams == 0)
                {
                    return inner;
                }

                res = inner;
                if (res.GenericParameters == null)
                    res.GenericParameters = new ITType[] { };
                int offset = res.GenericParameters.Length;
                Array.Resize<ITType>(ref res.GenericParameters, res.GenericParameters.Length + numGenParams);
                foreach (CodeType typ in gen.GenericParameters)
                {
                    ITType typeRes = ResolveType(typ, scope);
                    res.GenericParameters[offset] = typeRes;
                    offset++;
                }
                res.hasEnoughGenericParametersExceptTop = true;
                res.hasEnoughGenericParametersForTop = true;
                return res;
            }
            else if (spec is CodeImplicitEntitySpecifier)
            {
                CodeImplicitEntitySpecifier impl = ((CodeImplicitEntitySpecifier)spec);
                string text = impl.Idenfitifer.Text;
                while (scope != null)
                {
                    ITEntity ent = scope.GetChildEntity(text);
                    if (ent != null)
                    {
                        res = new ResolveEntityResult();
                        res.entity = ent;

                        res.hasEnoughGenericParametersExceptTop = true;

                        ITClassEntity clsEntity = ent as ITClassEntity;
                        ITFunctionEntity funcEntity = ent as ITFunctionEntity;
                        if (clsEntity != null)
                        {
                            res.hasEnoughGenericParametersForTop = clsEntity.Type.GetGenericParameters().Length ==
                                scope.GetStackedGenericParameters().Length;
                        }
                        else if (funcEntity != null)
                        {
                            res.hasEnoughGenericParametersForTop = funcEntity.Body.GenericParameters.Length > 0;
                        }
                        else
                        {
                            res.hasEnoughGenericParametersForTop = true;
                        }


                        res.GenericParameters = scope.GetStackedGenericParameters();
                        return res;
                    }
                    scope = scope.ParentScope;
                    // FIXME: don't go to global scope?
                }
                ReportError(string.Format(Properties.Resources.ICEntityNotFound, text), spec.Location);
                return null;
            }
            else if (spec is CodeGlobalScopeSpecifier)
            {
                ReportError(Properties.Resources.ICRootGlobalScopeIsNotEntity, spec.Location);
                return null;
            }
            else if (spec is CodeScopedEntitySpecifier)
            {
                CodeScopedEntitySpecifier spc = (CodeScopedEntitySpecifier)spec;
                CodeEntitySpecifier parent = spc.ParentEntity;
                ITEntity ent = null;
                ITScope scp;
                CodeGlobalScopeSpecifier parentGlobal = parent as CodeGlobalScopeSpecifier;
                if (parentGlobal != null)
                {
                    ITScope parScope = ResolveRootGlobalScope(parentGlobal, scope);
                    if (parScope == null)
                    {
                        return null;
                    }
                    scp = parScope;
                }
                else
                {
                    res = ResolveEntity(parent, scope);
                    if (res == null)
                    {
                        return null;
                    }

                    IITSubScopedEntity sscp = res.entity as IITSubScopedEntity;
                    if(sscp == null || sscp.Scope == null){
                        ReportError(string.Format(Properties.Resources.ICEntityDoesNotHaveScope, res.entity.ToString()), spec.Location);
                    }
                    scp = sscp.Scope;
                }

                string text = ((CodeScopedEntitySpecifier)spec).Identifier.Text;
                ent = scp.GetChildEntity(text);
                if (ent != null)
                {
                    // reuse res for performance if possible
                    if (res == null)
                    {
                        res = new ResolveEntityResult();
                        res.hasEnoughGenericParametersExceptTop = true;
                        res.hasEnoughGenericParametersForTop = true;
                    }
                    res.entity = ent;
                    res.hasEnoughGenericParametersExceptTop &= res.hasEnoughGenericParametersForTop;
                    ITClassEntity clsEntity = ent as ITClassEntity;
                    ITFunctionEntity funcEntity = ent as ITFunctionEntity;
                    if (clsEntity != null)
                    {
                        res.hasEnoughGenericParametersForTop = clsEntity.Type.GetGenericParameters().Length ==
                            scp.GetStackedGenericParameters().Length;
                    }
                    else if (funcEntity != null)
                    {
                        res.hasEnoughGenericParametersForTop = funcEntity.Body.GenericParameters.Length > 0;
                    }
                    else
                    {
                        res.hasEnoughGenericParametersForTop = true;
                    }
                    return res;
                }
                ReportError(string.Format(Properties.Resources.ICEntityNotFound, text), spec.Location);
                return null;
            }
            else
            {
                throw new IntermediateCompilerException(Properties.Resources.InternalError);
            }
        }

        public virtual ITRoot IntermediateCompile(CodeSourceFile[] sourceFiles)
        {
            constantFold = new ConstantFold(this);

            RegisterBuiltinType("int8", CreatePrimitiveType(ITPrimitiveTypeType.Int8));
            RegisterBuiltinType("int16", CreatePrimitiveType(ITPrimitiveTypeType.Int16));
            RegisterBuiltinType("int32", CreatePrimitiveType(ITPrimitiveTypeType.Int32));
            RegisterBuiltinType("int64", CreatePrimitiveType(ITPrimitiveTypeType.Int64));
            RegisterBuiltinType("uint8", CreatePrimitiveType(ITPrimitiveTypeType.UInt8));
            RegisterBuiltinType("uint16", CreatePrimitiveType(ITPrimitiveTypeType.UInt16));
            RegisterBuiltinType("uint32", CreatePrimitiveType(ITPrimitiveTypeType.UInt32));
            RegisterBuiltinType("uint64", CreatePrimitiveType(ITPrimitiveTypeType.UInt64));
            RegisterBuiltinType("byte", CreatePrimitiveType(ITPrimitiveTypeType.UInt8));
            RegisterBuiltinType("byte8", CreatePrimitiveType(ITPrimitiveTypeType.UInt8));
            RegisterBuiltinType("byte16", CreatePrimitiveType(ITPrimitiveTypeType.UInt16));
            RegisterBuiltinType("byte32", CreatePrimitiveType(ITPrimitiveTypeType.UInt32));
            RegisterBuiltinType("byte64", CreatePrimitiveType(ITPrimitiveTypeType.UInt64));
            RegisterBuiltinType("int", CreatePrimitiveType(ITPrimitiveTypeType.Integer));
            RegisterBuiltinType("float", CreatePrimitiveType(ITPrimitiveTypeType.Float));
            RegisterBuiltinType("double", CreatePrimitiveType(ITPrimitiveTypeType.Double));
            RegisterBuiltinType("bool", CreatePrimitiveType(ITPrimitiveTypeType.Bool));
            RegisterBuiltinType("string", CreatePrimitiveType(ITPrimitiveTypeType.String));
            RegisterBuiltinType("char", CreatePrimitiveType(ITPrimitiveTypeType.Char));

            root = CreateITRoot();

            var roots = new Dictionary<string, EntityCompiler>();
            foreach (CodeSourceFile src in sourceFiles)
            {
                ITRootGlobalScope scope = root.GetRootGlobalScope(src.Name.Text);
                EntityCompiler ent = new EntityCompiler(this, scope);
                roots[src.Name.Text] = ent;

                foreach(CodeStatement stat in src.Children.Values){
                    ent.AddChildStatement(stat);
                }
            }

            foreach (EntityCompiler comp in roots.Values)
            {
                comp.AddGlobalTypes();
            }
            foreach (EntityCompiler comp in roots.Values)
            {
                comp.SetupGlobalTypes();
            }
            foreach (EntityCompiler comp in roots.Values)
            {
                comp.AddGlobalDefinitions();
            }
            foreach (EntityCompiler comp in roots.Values)
            {
                comp.VerifyMemberHierarchies();
            }
            foreach (EntityCompiler comp in roots.Values)
            {
                comp.ResolveConstants();
            }
            foreach (EntityCompiler comp in roots.Values)
            {
                comp.CompileFunctions();
            }

            

            ITRoot ret = root;
            root = null;
            return ret;
        }

        internal sealed class GenericTypeMutator
        {
            private Dictionary<ITGenericTypeParameter, ITType> typeMap = new Dictionary<ITGenericTypeParameter, ITType>();
            public GenericTypeMutator(GenericTypeMutator mutator)
            {
                foreach (var pair in mutator.typeMap)
                {
                    typeMap.Add(pair.Key, pair.Value);
                }
            }
            public GenericTypeMutator(ITType container, ITType[] instantiation)
            {
                AddMutator(container, instantiation);
            }

            public GenericTypeMutator(IEnumerable<ITGenericTypeParameter> container, ITType[] instantiation)
            {
                AddMutator(container, instantiation);
            }
            public GenericTypeMutator()
            {
            }

            public void AddMutator(ITGenericTypeParameter frm, ITType to)
            {
                typeMap[frm] = to;
            }

            public void AddMutator(IEnumerable<ITGenericTypeParameter> container, ITType[] instantiation)
            {
                int ind = 0;
                if (container == null)
                    return;
                foreach (ITGenericTypeParameter pr in container)
                {
                    if (ind >= instantiation.Length)
                    {
                        throw new InvalidOperationException();
                    }
                    AddMutator(pr, instantiation[ind]);
                    ind += 1;
                }
            }

            public void AddMutator(ITType container, ITType[] instantiation){
                if (instantiation == null)
                    return;
                int ind = instantiation.Length;
                ITType[] prms = container.GetGenericParameters();
                if (prms != null)
                {
                    for (int i = prms.Length - 1; i >= 0; i--)
                    {
                        ind--;
                        if (ind < 0)
                        {
                            throw new InvalidOperationException();
                        }
                        AddMutator((ITGenericTypeParameter)prms[i], instantiation[ind]);
                    }
                }
            }

            public ITType Mutate(ITType typ)
            {
                if (typ == null)
                    return null;
                ITGenericTypeParameter gparam = typ as ITGenericTypeParameter;
                ITType outType;
                if (gparam == null)
                {
                    ITArrayType arr = typ as ITArrayType;
                    if (arr != null)
                    {
                        var t = Mutate(arr.ElementType);
                        if (t != arr.ElementType)
                        {
                            return arr.iCompiler.CreateArrayType(t, arr.Dimensions);
                        }
                        return arr;
                    }

                    ITFunctionType func = typ as ITFunctionType;
                    if (func != null)
                    {
                        var ret = Mutate(func.ReturnType);
                        var prms1 = func.Parameters;
                        var prms2 = new ITFunctionParameter[prms1.Length];
                        bool insted = false;
                        for (int i = 0; i < prms1.Length; i++)
                        {
                            var prm = prms1[i];
                            var t = Mutate(prm.Type);
                            if (t == prm.Type)
                            {
                                prms2[i] = prm;
                            }
                            else
                            {
                                prms2[i] = new ITFunctionParameter()
                                {
                                    IsByRef = prm.IsByRef,
                                    Location = prm.Location,
                                    Name = prm.Name,
                                    Type = t
                                };
                                insted = true;
                            }
                        }
                        if (insted || ret != func.ReturnType)
                        {
                            return func.iCompiler.CreateFunctionType(ret, prms2);
                        }
                        else
                        {
                            return func;
                        }
                    }

                    {
                        ITType[] insts = typ.GetGenericParameters();
                        if (insts == null || insts.Length == 0)
                        {
                            return typ;
                        }
                        var insts2 = new ITType[insts.Length];
                        bool insted = false;
                        for (int i = 0; i < insts.Length; i++)
                        {
                            var t = Mutate(insts[i]);
                            if (t != insts[i])
                            {
                                insted = true;
                            }
                            insts2[i] = t;
                        }
                        if (insted)
                        {
                            return typ.MakeGenericType(insts2);
                        }
                        return typ;
                    }
                }
                if (typeMap.TryGetValue(gparam, out outType))
                {
                    return Mutate(outType);
                }

                // if calling function F has a generic type parameter T, another function getting called by it
                // may return T which is still a generic type parameter.

                return typ;
            }
        }

    }
}
