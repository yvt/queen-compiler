using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Queen.Language.IntermediateTree;

namespace Queen.Language.CliCompiler
{
    public partial class CliCompiler
    {

        internal ConstructorInfo GetCliConstructor(ITType type)
        {
            ITClassType cls = type as ITClassType;
            ITInstantiatedGenericType inst = type as ITInstantiatedGenericType;
            if (cls != null)
            {
                // TODO: class type must have "is constructible?" attribute
                if (cls.UnderlyingEnumType != null)
                {
                    throw new InvalidOperationException("Enum type is not constructible");
                }
                return (cls.UserData as CliTypeInfo).constructor;
            }
            else if (inst != null)
            {
                CliTypeInfo typeInfo = (CliTypeInfo)inst.GenericTypeDefinition.UserData;
                Type cliClass = GetCliType(inst);
                Type cliClassBase = GetCliType(inst.GenericTypeDefinition);
                TypeBuilder typBuilder = cliClassBase as TypeBuilder;
                if (typBuilder == null)
                {
                    // imported
                    try
                    {
                        return cliClass.GetConstructor(new Type[] { });
                    }
                    catch (NotSupportedException)
                    {
                        // FIXME: switch to this route without try/catch
                        ConstructorInfo superconstructor = typeInfo.constructor;
                        return TypeBuilder.GetConstructor(cliClass, superconstructor);
                    }
                }
                else
                {
                    ConstructorInfo superconstructor = typeInfo.constructor;
                    return TypeBuilder.GetConstructor(cliClass, superconstructor);
                }
            }
            else
            {
                throw new InvalidOperationException("Not constructible: " + type.ToString());
            }
        }

        internal MethodInfo GetCliMethod(ITFunctionEntity globFunc)
        {

            var genParams = new ITType[] { };
            ITMutatedFunctionEntity mutat = globFunc as ITMutatedFunctionEntity;
            if (mutat != null)
            {
                genParams = mutat.genericTypeParams;
                globFunc = mutat.BaseEntity;
            }

            CliGlobalFunctionInfo info = globFunc.UserData as CliGlobalFunctionInfo;
            if (info == null)
                throw new InvalidOperationException("CliGlobalFunctionInfo not found");


            Type typ = info.containedType;
            MethodInfo meth = info.method;
            var genArgs = info.containedType.GetGenericArguments(); 
            int clsGens = genArgs != null ? genArgs.Length : 0;
            Type[] classGenParams = new Type[clsGens];
            for (int i = 0; i < classGenParams.Length; i++)
                classGenParams[i] = GetCliType(genParams[i]);
            if (classGenParams.Length > 0)
            {
                
                // find corresponding generic method
                if (typ is TypeBuilder)
                {
                    typ = typ.MakeGenericType(classGenParams); 
                    meth = TypeBuilder.GetMethod(typ, meth);
                }
                else
                {
                    typ = typ.MakeGenericType(classGenParams); 

                    // TODO: support overload
                    meth = typ.GetMethod(meth.Name);
                }
            }

            Type[] methGenParams = new Type[genParams.Length - clsGens];
            for (int i = 0; i < methGenParams.Length; i++)
                methGenParams[i] = GetCliType(genParams[genParams.Length - methGenParams.Length + i]);
            if(methGenParams.Length > 0)
                meth = meth.MakeGenericMethod(methGenParams);

            return meth;
        }


        internal MethodInfo GetCliMethod(ITType owner, ITMemberFunction globFunc, ITType[] genParams)
        {
            if (genParams == null)
                genParams = new ITType[0];

            if (globFunc.Name == "ToStr")
            {
                return typeof(object).GetMethod("ToString");
            }

            // FIXME: owner should not be given?
            owner = globFunc.Owner;
            CliMemberFunctionInfo info = globFunc.UserData as CliMemberFunctionInfo;
            if (info == null)
            {
                ITMutatedMemberFunction mutated = globFunc as ITMutatedMemberFunction;
                if (mutated == null)
                {
                    throw new InvalidOperationException("CliMemberFunctionInfo not found");
                }
                else
                {
                    ITMemberFunction baseMember = mutated.Base;
                    info = baseMember.UserData as CliMemberFunctionInfo;

                    Type mutateBase = GetCliType(((ITInstantiatedGenericType)owner).GenericTypeDefinition);
                    Type mutator = GetCliType(owner);
                    if (mutateBase is TypeBuilder)
                    {
                        // not imported
                        MethodInfo mutatedMethod = TypeBuilder.GetMethod(mutator, info.method);
                        info = new CliMemberFunctionInfo()
                        {
                            method = mutatedMethod,
                            ownerITType = owner
                        };
                    }
                    else
                    {
                        // imported
                        // TODO: support overloads
                        try
                        {
                            MethodInfo mutatedMethod = mutator.GetMethod(info.method.Name);
                            info = new CliMemberFunctionInfo()
                            {
                                method = mutatedMethod,
                                ownerITType = owner
                            };
                        }
                        catch (NotSupportedException)
                        {
                            // FIXME: detect this case (generic parameter is TypeBuilder) without try/catch
                            MethodInfo mutatedMethod = TypeBuilder.GetMethod(mutator, info.method);
                            info = new CliMemberFunctionInfo()
                            {
                                method = mutatedMethod,
                                ownerITType = owner
                            };
                        }
                    }
                }
            }

            MethodInfo member = info.method;
            Type[] methGenParams = new Type[genParams.Length];
            for (int i = 0; i < methGenParams.Length; i++)
                methGenParams[i] = GetCliType(genParams[i]);
            if (methGenParams.Length > 0)
                member = member.MakeGenericMethod(methGenParams);

            return member;
        }

        internal struct CliProperty
        {
            public MethodInfo getter, setter;
        }

        internal CliProperty GetCliProperty(ITType owner, ITMemberProperty globFunc, ITType[] genParams)
        {
            if (genParams == null)
                genParams = new ITType[0];

            CliMemberPropertyInfo info = globFunc.UserData as CliMemberPropertyInfo;
            if (info == null)
            {
                ITMutatedMemberProperty mutated = globFunc as ITMutatedMemberProperty;
                if (mutated == null)
                {
                    throw new InvalidOperationException("CliMemberFunctionInfo not found");
                }
                else
                {
                    ITMemberProperty baseMember = mutated.Base;
                    info = baseMember.UserData as CliMemberPropertyInfo;

                    Type mutateBase = GetCliType(((ITInstantiatedGenericType)owner).GenericTypeDefinition);
                    Type mutator = GetCliType(owner);
                    try // FIXME: remove use of try/catch
                    {
                        // not imported
                        // NOTE: this route should be used when not only when
                        // the class is not imported, but its generic instantiation parameters are
                        // imported.
                        MethodInfo mutatedGetter = info.getter != null ? TypeBuilder.GetMethod(mutator, info.getter) : null;
                        MethodInfo mutatedSetter = info.setter != null ? TypeBuilder.GetMethod(mutator, info.setter) : null;
                        info = new CliMemberPropertyInfo()
                        {
                            getter = mutatedGetter,
                            setter = mutatedSetter,
                            ownerITType = owner
                        };
                    }
                    catch
                    {
                        // imported
                        var prop = mutator.GetProperty(info.property.Name);
                        MethodInfo mutatedGetter = info.getter != null ? prop.GetGetMethod() : null;
                        MethodInfo mutatedSetter = info.setter != null ? prop.GetSetMethod() : null;
                        info = new CliMemberPropertyInfo()
                        {
                            getter = mutatedGetter,
                            setter = mutatedSetter,
                            ownerITType = owner
                        };
                    }
                }
            }

            Type[] methGenParams = new Type[genParams.Length];
            for (int i = 0; i < methGenParams.Length; i++)
                methGenParams[i] = GetCliType(genParams[i]);

            CliProperty p = new CliProperty();
            if (info.getter != null)
            {
                MethodInfo member = info.getter;
                if (methGenParams.Length > 0)
                    member = member.MakeGenericMethod(methGenParams);
                p.getter = member;
            }
            if (info.setter != null)
            {
                MethodInfo member = info.setter;
                if (methGenParams.Length > 0)
                    member = member.MakeGenericMethod(methGenParams);
                p.setter = member;
            }

            return p;
        }


        internal FieldInfo GetCliField(ITType owner, ITMemberVariable globFunc)
        {
            CliMemberVariableInfo info = globFunc.UserData as CliMemberVariableInfo;
            if (info == null)
            {
                ITMutatedMemberVariable mutated = globFunc as ITMutatedMemberVariable;
                if (mutated == null)
                {
                    throw new InvalidOperationException("CliMemberVariableInfo not found");
                }
                else
                {
                    ITMemberVariable baseMember = mutated.Base;
                    info = baseMember.UserData as CliMemberVariableInfo;

                    Type mutator = GetCliType(owner);
                    FieldInfo mutatedMethod = TypeBuilder.GetField(mutator, info.field);
                    info = new CliMemberVariableInfo()
                    {
                        field = mutatedMethod,
                        ownerITType = owner
                    };
                }
            }

            FieldInfo member = info.field;
            return member;
        }

        internal FieldInfo GetCliField(ITGlobalVariableEntity globFunc, ITType[] genParams)
        {
            if (genParams == null)
                genParams = new ITType[0];

            CliGlobalVariableInfo info = globFunc.UserData as CliGlobalVariableInfo;
            if (info == null)
                throw new InvalidOperationException("CliGlobalVariableInfo not found");

            Type typ = info.containedType;
            // root enum classes are created with EnumBuilder, which doesn't support GetGenericArguments.
            var gens = (typ is EnumBuilder) ? null : typ.GetGenericArguments();
            int clsGens = gens != null ? gens.Length : 0;
            Type[] classGenParams = new Type[clsGens];
            for (int i = 0; i < classGenParams.Length; i++)
                classGenParams[i] = GetCliType(genParams[i]);

            FieldInfo fInfo = info.field;
            if (classGenParams.Length > 0)
            {
                typ = typ.MakeGenericType(classGenParams);
                fInfo = TypeBuilder.GetField(typ, fInfo);
            }

            if (clsGens != genParams.Length)
            {
                throw new InvalidOperationException();
            }

            return fInfo;
        }

        private sealed class FunctionCompiler : IDelayedRegistrar
        {
            CliCompiler compiler;
            ITType thisType;
            ITFunctionBody body;
            MethodBuilder method;
            ConstructorBuilder constructor;
            ILGenerator il;

            // "return block" is used when there's "return" in "try" block
            private LocalBuilder returnBlockVariable = null;
            private Label? returnBlockLabel = null;

            public FunctionCompiler(CliCompiler compiler,
                ITType thisType, ITFunctionBody body, MethodBuilder method)
            {
                this.compiler = compiler;
                this.thisType = thisType;
                this.body = body;
                this.method = method;
            }

            public FunctionCompiler(CliCompiler compiler,
                ITType thisType, ITFunctionBody body, ConstructorBuilder method)
            {
                this.compiler = compiler;
                this.thisType = thisType;
                this.body = body;
                this.constructor = method;
            }

            private class BlockInfo
            {
                public Label StartLabel;
                public Label EndLabel;
                public bool IsExceptionBlock;
            };

            public void InitializeForGGeneralUse(ILGenerator il)
            {
                this.il = il;
            }

            public void EmitReturnInExceptionBlock()
            {
                if (returnBlockLabel == null)
                {
                    if (body.ReturnType != null)
                    {
                        returnBlockVariable = il.DeclareLocal(compiler.GetCliType(body.ReturnType));
                    }
                    returnBlockLabel = il.DefineLabel();
                }

                il.Emit(OpCodes.Stloc, returnBlockVariable);
                il.Emit(OpCodes.Leave, (Label)returnBlockLabel);
            }

            public void Register()
            {
                if (method != null)
                {
                    il = method.GetILGenerator();
                }
                else
                {
                    il = constructor.GetILGenerator();
                }

                if (constructor != null)
                {
                    // constructor behavior
                    ConstructorInfo superconstructor;
                    if (thisType.Superclass == null)
                    {
                        superconstructor = typeof(Queen.Kuin.CClass).GetConstructor(new Type[] { });
                    }
                    else
                    {
                        superconstructor = compiler.GetCliConstructor(thisType.Superclass);
                    }

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Call, superconstructor);

                    // initialize members
                    foreach (ITMemberVariable stat in thisType.GetMemberVariables())
                    {
                        if (stat.InitialValue != null)
                        {
                            il.Emit(OpCodes.Ldarg_0);
                            EmitExpression(stat.InitialValue);
                            CliMemberVariableInfo info = (stat.UserData as CliMemberVariableInfo);
                            il.Emit(OpCodes.Stfld, info.field);
                        }
                    }
                }

                int idx = 0;
                if (thisType != null)
                    idx = 1;
                if (body != null)
                {
                    foreach (ITFunctionParameter prm in body.Parameters)
                    {
                        prm.UserData = idx;
                        idx += 1;
                    }


                    EmitBlock(body.Block);
                }
                else
                {
                    if (constructor == null)
                    {
                        throw new InvalidOperationException();
                    }

                    // default constructor.
                }

                if (body != null && body.ReturnType != null)
                {
                    il.Emit(OpCodes.Ldc_I4_0);
                }
                il.Emit(OpCodes.Ret);
                if (returnBlockLabel != null)
                {
                    il.MarkLabel((Label)returnBlockLabel);
                    if (returnBlockVariable != null)
                    {
                        il.Emit(OpCodes.Ldloc, returnBlockVariable);
                    }
                    il.Emit(OpCodes.Ret);
                }
            }

            private void EmitFunctionParameter(ITFunctionParameter param, ITExpression val)
            {
                if (param.IsByRef)
                {
                    if (val is ITStorage)
                    {
                        ITLocalVariableStorage lvarStor = val as ITLocalVariableStorage;
                        if (lvarStor != null)
                        {
                            LocalBuilder local = lvarStor.Variable.UserData as LocalBuilder;
                            il.Emit(OpCodes.Ldloca, local);
                            return;
                        }

                        ITGlobalVariableStorage gvarStor = val as ITGlobalVariableStorage;
                        if (gvarStor != null)
                        {
                            FieldInfo fld = compiler.GetCliField(gvarStor.Variable, gvarStor.GenericTypeParameters);
                            il.Emit(OpCodes.Ldnull);
                            il.Emit(OpCodes.Ldflda, fld);
                            return;
                        }

                        ITArrayElementStorage elemStor = val as ITArrayElementStorage;
                        if (elemStor != null)
                        {
                            EmitExpression(elemStor.Variable);
                            ITArrayType arr = elemStor.Variable.ExpressionType as ITArrayType;
                            if (arr.Dimensions == 1)
                            {
                                EmitExpression(elemStor.Indices[0]);
                                il.Emit(OpCodes.Ldelema, compiler.GetCliType(elemStor.ExpressionType));
                            }
                            else
                            {
                                foreach (ITExpression idx in elemStor.Indices)
                                    EmitExpression(idx);

                                Type[] prms = new Type[elemStor.Indices.Count];
                                for (int i = 0; i < prms.Length; i++) prms[i] = typeof(int);
                                MethodInfo meth = compiler.GetCliType(arr).GetMethod("Address", prms);
                                il.Emit(OpCodes.Call, meth);
                            }
                            return;
                        }

                        ITParameterStorage prmStor = val as ITParameterStorage;
                        if (prmStor != null)
                        {
                            ITFunctionParameter prm = prmStor.Variable;
                            int idx = (int)prm.UserData;
                            if (prm.IsByRef)
                            {
                                il.Emit(OpCodes.Ldarg, idx);
                            }
                            else
                            {
                                il.Emit(OpCodes.Ldarga, idx);
                            }
                            return;
                        }

                        ITMemberVariableStorage memVarStor = val as ITMemberVariableStorage;
                        if (memVarStor != null)
                        {
                            FieldInfo info = compiler.GetCliField(memVarStor.Instance.ExpressionType, memVarStor.Member);
                            EmitExpression(memVarStor.Instance);
                            il.Emit(OpCodes.Ldflda, info);
                            return;
                        }


                    }
                    
                    // address is not available; pass temporal variable
                    LocalBuilder lvar = il.DeclareLocal(compiler.GetCliType(val.ExpressionType));
                    EmitExpression(val);
                    il.Emit(OpCodes.Stloc, lvar);
                    il.Emit(OpCodes.Ldloca, lvar);
                }
                else
                {
                    EmitExpression(val);
                }
            }

            internal struct ExpressionOutputInfo
            {
                public bool logicNegated;
            }

            [Flags]
            internal enum AcceptedExpressionState
            {
                LogicNegated = 1
            }

            internal ExpressionOutputInfo FilterValue(ExpressionOutputInfo info, AcceptedExpressionState state)
            {
                if (info.logicNegated && (state & AcceptedExpressionState.LogicNegated) == 0)
                {
                    // caller cannot accept negated logic.
                    info.logicNegated = false;
                    Label endLabel = il.DefineLabel();
                    Label falseLabel = il.DefineLabel();
                    il.Emit(OpCodes.Brfalse, falseLabel);
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Br, endLabel);
                    il.MarkLabel(falseLabel);
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.MarkLabel(endLabel);
                }
                return info;
            }

            internal ExpressionOutputInfo EmitExpression(ITExpression expr, bool discardValue = false, AcceptedExpressionState acceptedStates = 0)
            {
                var gen = new ExpressionGenerator(compiler, this, discardValue);
                ExpressionOutputInfo info = expr.Accept<ExpressionOutputInfo>(gen);
                if ((!discardValue) && expr.ExpressionType != null)
                {
                    return FilterValue(info, acceptedStates);
                }
                else
                {
                    return info;
                }
            }

            private void EmitBlock(ITBlock block, bool isExceptionBlock = false)
            {
                var gen = new StatementGenerator(compiler, this, block);
                var info = new BlockInfo();
                block.UserData = info;
                info.StartLabel = il.DefineLabel();
                info.EndLabel = il.DefineLabel();
                info.IsExceptionBlock = isExceptionBlock;
                il.MarkLabel(info.StartLabel);

                foreach (ITLocalVariable var in block.LocalVariables.Values)
                {
                    LocalBuilder varInfo = var.UserData != null ? (LocalBuilder)var.UserData :
                        il.DeclareLocal(compiler.GetCliType(var.Type));
                    var.UserData = varInfo;
                    if (var.IsConst && var.ConstantValue != null)
                    {
                        EmitExpression(var.ConstantValue);
                        il.Emit(OpCodes.Stloc, varInfo);
                    }
                }

                foreach (ITStatement stat in block.Statements)
                {
                    stat.Accept<int>(gen);
                }

                if (block.IsLoop)
                {
                    il.Emit(OpCodes.Br, info.StartLabel);
                }
                il.MarkLabel(info.EndLabel);
            }

            private sealed class StatementGenerator : IITStatementVisitor<int>
            {
                CliCompiler compiler;
                FunctionCompiler fCompiler;
                ILGenerator il;
                ITBlock curBlock;

                public StatementGenerator(CliCompiler compiler, FunctionCompiler fCompiler, ITBlock curBlock)
                {
                    this.compiler = compiler;
                    this.fCompiler = fCompiler;
                    this.il = fCompiler.il;
                    this.curBlock = curBlock;
                }

                private bool IsLeavingExceptionBlock(ITBlock targetBlock = null)
                {
                    ITBlock blk = curBlock;
                    while (blk != null)
                    {
                        if (blk == targetBlock)
                            return false;
                        BlockInfo info = blk.UserData as BlockInfo;
                        if (info.IsExceptionBlock)
                        {
                            return true;
                        }
                        blk = blk.ParentBlock;
                    }
                    return false;
                }

                public int Visit(ITBlockStatement statement)
                {
                    fCompiler.EmitBlock(statement.Block);
                    return 0;
                }

                public int Visit(ITExitBlockStatement statement)
                {
                    ITBlock block = statement.ExitingBlock;
                    BlockInfo info = block.UserData as BlockInfo;
                    if (info == null)
                        throw new InvalidOperationException("no BlockInfo");

                    if(IsLeavingExceptionBlock(block))
                        il.Emit(OpCodes.Leave, info.EndLabel);
                    else
                        il.Emit(OpCodes.Br, info.EndLabel);

                    return 0;
                }

                public int Visit(ITExpressionStatement statement)
                {
                    fCompiler.EmitExpression(statement.Expression, true);
                    return 0;
                }

                public int Visit(ITIfStatement statement)
                {
                    ExpressionOutputInfo info = fCompiler.EmitExpression(statement.Condition, false, AcceptedExpressionState.LogicNegated);
                    bool invert = info.logicNegated;
                    if (statement.TrueBlock == null)
                    {
                        if (statement.FalseBlock == null)
                        {
                            il.Emit(OpCodes.Pop);
                        }
                        else
                        {
                            Label label = il.DefineLabel();
                            if(invert)
                                il.Emit(OpCodes.Brfalse, label);
                            else
                                il.Emit(OpCodes.Brtrue, label);
                            fCompiler.EmitBlock(statement.FalseBlock);
                            il.MarkLabel(label);
                        }
                    }
                    else
                    {
                        if (statement.FalseBlock == null)
                        {
                            Label label = il.DefineLabel();
                            if (invert)
                                il.Emit(OpCodes.Brtrue, label);
                            else
                                il.Emit(OpCodes.Brfalse, label);
                            fCompiler.EmitBlock(statement.TrueBlock);
                            il.MarkLabel(label);
                        }
                        else
                        {
                            Label lastLabel = il.DefineLabel();
                            Label label = il.DefineLabel();
                            if (invert)
                                il.Emit(OpCodes.Brtrue, label);
                            else
                                il.Emit(OpCodes.Brfalse, label);
                            fCompiler.EmitBlock(statement.TrueBlock);
                            il.Emit(OpCodes.Br, lastLabel);
                            il.MarkLabel(label);
                            fCompiler.EmitBlock(statement.FalseBlock);
                            il.MarkLabel(lastLabel);
                        }
                    }
                    return 0;
                }

                public int Visit(ITReturnStatement statement)
                {
                    if (statement.ReturnedValue != null)
                    {
                        fCompiler.EmitExpression(statement.ReturnedValue);
                    }

                    // TODO: use "leave" in try block
                    if (IsLeavingExceptionBlock())
                    {
                        fCompiler.EmitReturnInExceptionBlock();
                    }
                    else
                    {
                        il.Emit(OpCodes.Ret);
                    }
                    return 0;
                }


                public int Visit(ITTableSwitchStatement statement)
                {
                    throw new NotImplementedException();
                }

                public int Visit(ITTryStatement statement)
                {
                    Label labelEndOfStatement = il.BeginExceptionBlock();
                    fCompiler.EmitBlock(statement.ProtectedBlock, true);

                    // if there is at least one "numeric" catch clause,
                    // switch to super slow handler.
                    int firstNumericHandler = 0;
                    var handlers = statement.Handlers;
                    foreach (var c in handlers)
                    {
                        if (c is ITNumericTryHandler)
                        {
                            break;
                        }
                        else
                        {
                            firstNumericHandler++;
                        }
                    }

                    // first do some native catch clauses
                    for (int i = 0; i < firstNumericHandler; i++)
                    {
                        var handler = (ITTypedTryHandler)handlers[i];

                        il.BeginCatchBlock(compiler.GetCliType(handler.InfoVariable.Type));

                        var locVar = handler.InfoVariable;
                        if (locVar != null)
                        {
                            LocalBuilder loc = il.DeclareLocal(compiler.GetCliType(locVar.Type));
                            il.Emit(OpCodes.Stloc, loc);
                        }
                        else
                        {
                            il.Emit(OpCodes.Pop);
                        }

                        fCompiler.EmitBlock(handler.Block, true);
                        il.Emit(OpCodes.Leave, labelEndOfStatement);
                    }

                    if (firstNumericHandler < handlers.Length)
                    {
                        il.BeginCatchBlock(typeof(Exception)); // oh...

                        // try creating CExcpt
                        il.Emit(OpCodes.Dup);
                        il.Emit(OpCodes.Call, typeof(Queen.Kuin.CExcpt).GetMethod("TryConvertFromFrameworkException"));

                        LocalBuilder numEx = il.DeclareLocal(typeof(Queen.Kuin.CExcpt));
                        il.Emit(OpCodes.Stloc, numEx);

                        LocalBuilder nativeEx = il.DeclareLocal(typeof(Exception));
                        il.Emit(OpCodes.Stloc, nativeEx);

                        // go through handlers...
                        for (int i = firstNumericHandler; i < handlers.Length; i++)
                        {
                            var handler = handlers[i];
                            var typedHandler = handler as ITTypedTryHandler;
                            var nextLabel = il.DefineLabel();
                            if (typedHandler != null)
                            {
                                il.Emit(OpCodes.Ldloc, nativeEx);
                                il.Emit(OpCodes.Isinst, compiler.GetCliType(typedHandler.InfoVariable.Type));

                                // not instance?
                                il.Emit(OpCodes.Dup);
                                il.Emit(OpCodes.Brfalse, nextLabel);

                                var locVar = handler.InfoVariable;
                                if (locVar != null)
                                {
                                    LocalBuilder loc = il.DeclareLocal(compiler.GetCliType(locVar.Type));
                                    il.Emit(OpCodes.Stloc, loc);
                                }
                                else
                                {
                                    il.Emit(OpCodes.Pop);
                                }

                                fCompiler.EmitBlock(handler.Block, true);
                                il.Emit(OpCodes.Leave, labelEndOfStatement);

                                il.MarkLabel(nextLabel);
                                continue;
                            }

                            var numericHandler = handler as ITNumericTryHandler;
                            if (numericHandler != null)
                            {
                                var handlerLabel = il.DefineLabel();

                                il.Emit(OpCodes.Ldloc, numEx);

                                // cannot be converted to numeric?
                                il.Emit(OpCodes.Brfalse, nextLabel);

                                if (numericHandler.Ranges.Length == 0)
                                {
                                    // no condition; always handled for numeric exceptions.
                                }
                                else
                                {

                                    // get code
                                    il.Emit(OpCodes.Ldloc, numEx);
                                    var getCodeMethod = typeof(Queen.Kuin.CExcpt).GetProperty("Code").GetGetMethod();
                                    il.Emit(OpCodes.Callvirt, getCodeMethod);

                                    var ranges = numericHandler.Ranges;
                                    for (int j = 0; j < ranges.Length; j++)
                                    {
                                        var range = ranges[j];
                                        Label skipToNextRange;

                                        if (j < ranges.Length - 1)
                                        {
                                            skipToNextRange = il.DefineLabel();
                                            il.Emit(OpCodes.Dup);
                                        }
                                        else
                                        {
                                            skipToNextRange = nextLabel;
                                        }

                                        if (range.UpperBound == null)
                                        {
                                            fCompiler.EmitExpression(range.LowerBound);
                                            il.Emit(OpCodes.Bne_Un, skipToNextRange);

                                            // equal
                                            if (j < ranges.Length - 1)
                                            {
                                                il.Emit(OpCodes.Pop);
                                            }
                                        }
                                        else
                                        {
                                            var upper = il.DefineLabel();
                                            // lower boundary check
                                            il.Emit(OpCodes.Dup);

                                            fCompiler.EmitExpression(range.LowerBound);
                                            il.Emit(OpCodes.Bge, upper);

                                            // out of boundary
                                            il.Emit(OpCodes.Pop);
                                            il.Emit(OpCodes.Br, skipToNextRange);

                                            // >= lower, check for upper bound
                                            il.MarkLabel(upper);
                                            fCompiler.EmitExpression(range.UpperBound);
                                            il.Emit(OpCodes.Bgt, skipToNextRange);

                                            // in range.
                                            if (j < ranges.Length - 1)
                                            {
                                                il.Emit(OpCodes.Pop);
                                            }
                                        }
                                        il.Emit(OpCodes.Br, handlerLabel);

                                        if (j < ranges.Length - 1)
                                        {
                                            il.MarkLabel(skipToNextRange);
                                        }
                                    }

                                }

                                var locVar = handler.InfoVariable;
                                if (locVar != null)
                                {
                                    locVar.UserData = numEx; // use CExcpt
                                }

                                il.MarkLabel(handlerLabel);
                                fCompiler.EmitBlock(handler.Block, true);
                                il.Emit(OpCodes.Leave, labelEndOfStatement);

                                il.MarkLabel(nextLabel);

                                continue;
                            }

                            // unknown catch clause type
                            throw new InvalidOperationException();
                        }

                        // all handlers failed to handle the exception; rethrow

                        il.Emit(OpCodes.Rethrow);

                    }

                    if (statement.FinallyBlock != null)
                    {
                        il.BeginFinallyBlock();
                        fCompiler.EmitBlock(statement.FinallyBlock);
                    }

                    il.EndExceptionBlock();
                    return 0;
                }


                public int Visit(ITAssertStatement statement)
                {
                    var info = fCompiler.EmitExpression(statement.Expression, false, AcceptedExpressionState.LogicNegated);
                    var endLabel = il.DefineLabel();

                    if (info.logicNegated)
                    {
                        il.Emit(OpCodes.Brfalse, endLabel);
                    }
                    else
                    {
                        il.Emit(OpCodes.Brtrue, endLabel);
                    }

                    il.Emit(OpCodes.Call, typeof(Queen.Kuin.CompilerServices.RuntimeHelper).GetMethod("AssertionFailed"));

                    il.MarkLabel(endLabel);
                    return 0;
                }

                public int Visit(ITThrowNumericStatement statement)
                {
                    Type typ = typeof(Queen.Kuin.CExcpt);

                    fCompiler.EmitExpression(statement.Code);
                    if (statement.Message != null)
                    {
                        fCompiler.EmitExpression(statement.Message);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldnull);
                    }

                    il.Emit(OpCodes.Newobj, typ.GetConstructor(new Type[] {typeof(long), typeof(string)}));
                    il.Emit(OpCodes.Throw);
                    return 0;
                }

                public int Visit(ITThrowObjectStatement statement)
                {
                    fCompiler.EmitExpression(statement.Expression);
                    il.Emit(OpCodes.Throw);
                    return 0;
                }
            }

            private sealed class ExpressionGenerator : IITExpressionVisitor<ExpressionOutputInfo>
            {
                CliCompiler compiler;
                FunctionCompiler fCompiler;
                ILGenerator il;
                bool discardValue;

                public ExpressionGenerator(CliCompiler compiler, FunctionCompiler fCompiler, bool discardValue)
                {
                    this.compiler = compiler;
                    this.fCompiler = fCompiler;
                    this.il = fCompiler.il;
                    this.discardValue = discardValue;
                }

                public ExpressionOutputInfo Visit(ITArrayConstructExpression expr)
                {
                    if (expr.NumElements.Count == 0)
                    {
                        throw new InvalidOperationException("Array with zero dimensions.");
                    }
                    else if (expr.NumElements.Count == 1)
                    {
                        fCompiler.EmitExpression(expr.NumElements[0]);
                        il.Emit(OpCodes.Newarr, compiler.GetCliType(expr.ElementType));
                    }
                    else if (expr.NumElements.Count > 1)
                    {
                        Type typ = compiler.GetCliType(expr.ElementType);
                        typ = typ.MakeArrayType(expr.NumElements.Count);
                        foreach (ITExpression exp in expr.NumElements)
                            fCompiler.EmitExpression(exp);

                        Type[] typs = new Type[expr.NumElements.Count];
                        for (int i = 0; i < typs.Length; i++) typs[i] = typeof(int);
                        ConstructorInfo constructor = typ.GetConstructor(typs);
                        if (constructor == null)
                            throw new InvalidOperationException("No array constructor found.");

                        il.Emit(OpCodes.Newobj, constructor);
                    }
                    else
                    {
                        throw new InvalidOperationException("Array with negative dimensions.");
                    }

                    if (discardValue)
                    {
                        il.Emit(OpCodes.Pop);
                    }
                    return new ExpressionOutputInfo();
                }

                public ExpressionOutputInfo Visit(ITArrayLiteralExpression expr)
                {
                    int numConstants = 0;
                    ITPrimitiveType prim = expr.ElementType as ITPrimitiveType;

                    // do enum type
                    ITClassType cls = expr.ElementType as ITClassType;
                    if (cls != null)
                    {
                        prim = cls.UnderlyingEnumType;
                    }

                    if (prim != null && prim.Type != ITPrimitiveTypeType.String)
                    {
                        foreach (ITExpression exp in expr.Elements)
                        {
                            ITValueExpression val = exp as ITValueExpression;
                            if (val != null && val.Value != null)
                            {
                                numConstants += 1;
                            }
                        }
                    }

                    bool valuesAreInitialized = false;

                    il.Emit(OpCodes.Ldc_I4, (int)expr.Elements.Count);
                    il.Emit(OpCodes.Newarr, compiler.GetCliType(expr.ElementType));

                    if (numConstants > 4)
                    {
                        // pre-initialized elements
                        using (var mem = new System.IO.MemoryStream())
                        {
                            using (var bw = new System.IO.BinaryWriter(mem))
                            {
                                switch (prim.Type)
                                {
                                    case ITPrimitiveTypeType.Bool:
                                        foreach (ITExpression exp in expr.Elements)
                                        {
                                            ITValueExpression val = exp as ITValueExpression;
                                            if (val != null && val.Value != null)
                                            {
                                                bw.Write(Convert.ToBoolean(val.Value) ? (byte)1 : (byte)0);
                                            }
                                            else
                                            {
                                                bw.Write((byte)0);
                                            }
                                        }
                                        break;
                                    case ITPrimitiveTypeType.Float:
                                        foreach (ITExpression exp in expr.Elements)
                                        {
                                            ITValueExpression val = exp as ITValueExpression;
                                            if (val != null && val.Value != null)
                                            {
                                                bw.Write(Convert.ToSingle(val.Value));
                                            }
                                            else
                                            {
                                                bw.Write((float)0.0);
                                            }
                                        }
                                        break;
                                    case ITPrimitiveTypeType.Double:
                                        foreach (ITExpression exp in expr.Elements)
                                        {
                                            ITValueExpression val = exp as ITValueExpression;
                                            if (val != null && val.Value != null)
                                            {
                                                bw.Write(Convert.ToDouble(val.Value));
                                            }
                                            else
                                            {
                                                bw.Write((double)0.0);
                                            }
                                        }
                                        break;
                                    case ITPrimitiveTypeType.Int8:
                                        foreach (ITExpression exp in expr.Elements)
                                        {
                                            ITValueExpression val = exp as ITValueExpression;
                                            if (val != null && val.Value != null)
                                            {
                                                bw.Write(Convert.ToSByte(val.Value));
                                            }
                                            else
                                            {
                                                bw.Write((sbyte)0);
                                            }
                                        }
                                        break;
                                    case ITPrimitiveTypeType.Int16:
                                        foreach (ITExpression exp in expr.Elements)
                                        {
                                            ITValueExpression val = exp as ITValueExpression;
                                            if (val != null && val.Value != null)
                                            {
                                                bw.Write(Convert.ToInt16(val.Value));
                                            }
                                            else
                                            {
                                                bw.Write((short)0);
                                            }
                                        }
                                        break;
                                    case ITPrimitiveTypeType.Int32:
                                        foreach (ITExpression exp in expr.Elements)
                                        {
                                            ITValueExpression val = exp as ITValueExpression;
                                            if (val != null && val.Value != null)
                                            {
                                                bw.Write(Convert.ToInt32(val.Value));
                                            }
                                            else
                                            {
                                                bw.Write((int)0);
                                            }
                                        }
                                        break;
                                    case ITPrimitiveTypeType.Int64:
                                    case ITPrimitiveTypeType.Integer:
                                        foreach (ITExpression exp in expr.Elements)
                                        {
                                            ITValueExpression val = exp as ITValueExpression;
                                            if (val != null && val.Value != null)
                                            {
                                                bw.Write(Convert.ToInt64(val.Value));
                                            }
                                            else
                                            {
                                                bw.Write((long)0);
                                            }
                                        }
                                        break;
                                    case ITPrimitiveTypeType.UInt8:
                                        foreach (ITExpression exp in expr.Elements)
                                        {
                                            ITValueExpression val = exp as ITValueExpression;
                                            if (val != null && val.Value != null)
                                            {
                                                bw.Write(Convert.ToByte(val.Value));
                                            }
                                            else
                                            {
                                                bw.Write((byte)0);
                                            }
                                        }
                                        break;
                                    case ITPrimitiveTypeType.UInt16:
                                        foreach (ITExpression exp in expr.Elements)
                                        {
                                            ITValueExpression val = exp as ITValueExpression;
                                            if (val != null && val.Value != null)
                                            {
                                                bw.Write(Convert.ToUInt16(val.Value));
                                            }
                                            else
                                            {
                                                bw.Write((ushort)0);
                                            }
                                        }
                                        break;
                                    case ITPrimitiveTypeType.UInt32:
                                        foreach (ITExpression exp in expr.Elements)
                                        {
                                            ITValueExpression val = exp as ITValueExpression;
                                            if (val != null && val.Value != null)
                                            {
                                                bw.Write(Convert.ToUInt32(val.Value));
                                            }
                                            else
                                            {
                                                bw.Write((uint)0);
                                            }
                                        }
                                        break;
                                    case ITPrimitiveTypeType.UInt64:
                                        foreach (ITExpression exp in expr.Elements)
                                        {
                                            ITValueExpression val = exp as ITValueExpression;
                                            if (val != null && val.Value != null)
                                            {
                                                bw.Write(Convert.ToUInt64(val.Value));
                                            }
                                            else
                                            {
                                                bw.Write((ulong)0);
                                            }
                                        }
                                        break;
                                    default:
                                        throw new NotImplementedException();
                                }
                            }
                            // TODO: write IL

                                
                            FieldInfo field = compiler.DefineInitializedField(mem.ToArray());
                            il.Emit(OpCodes.Dup);
                            il.Emit(OpCodes.Ldtoken, field);

                            MethodInfo itor = typeof(System.Runtime.CompilerServices.RuntimeHelpers).GetMethod
                                ("InitializeArray", new Type[] {typeof(Array), typeof(RuntimeFieldHandle)});
                            il.Emit(OpCodes.Call, itor);
                        }

                        valuesAreInitialized = true;
                    }

                    int idx = -1;
                    foreach (ITExpression exp in expr.Elements)
                    {
                        idx += 1;
                        ITValueExpression val = exp as ITValueExpression;
                        if (val != null)
                        {
                            if (val.Value != null)
                            {
                                if (valuesAreInitialized)
                                    continue;
                                if (prim != null)
                                {
                                    if (prim.Type == ITPrimitiveTypeType.Bool)
                                    {
                                        if ((bool)val.Value == false)
                                            continue;
                                    }
                                    else if (prim.Type == ITPrimitiveTypeType.Float)
                                    {
                                        if ((float)val.Value == 0.0f)
                                            continue;
                                    }
                                    else if (prim.Type == ITPrimitiveTypeType.Double)
                                    {
                                        if ((double)val.Value == 0.0)
                                            continue;
                                    }
                                    else if (prim.Type == ITPrimitiveTypeType.UInt64)
                                    {
                                        if ((ulong)val.Value == 0UL)
                                            continue;
                                    }
                                    else
                                    {
                                        if ((long)val.Value == 0L)
                                            continue;
                                    }
                                }
                            }
                            else
                            {
                                continue;
                            }
                        }

                        il.Emit(OpCodes.Dup);
                        il.Emit(OpCodes.Ldc_I4, idx);
                        fCompiler.EmitExpression(exp);

                        if (prim == null)
                        {
                            il.Emit(OpCodes.Stelem_Ref);
                        }
                        else
                        {
                            switch (prim.Type)
                            {
                                case ITPrimitiveTypeType.Bool:
                                case ITPrimitiveTypeType.Int8:
                                case ITPrimitiveTypeType.UInt8:
                                    il.Emit(OpCodes.Stelem_I1);
                                    break;
                                case ITPrimitiveTypeType.Char:
                                case ITPrimitiveTypeType.Int16:
                                case ITPrimitiveTypeType.UInt16:
                                    il.Emit(OpCodes.Stelem_I2);
                                    break;
                                case ITPrimitiveTypeType.Int32:
                                case ITPrimitiveTypeType.UInt32:
                                    il.Emit(OpCodes.Stelem_I4);
                                    break;
                                case ITPrimitiveTypeType.Integer:
                                case ITPrimitiveTypeType.Int64:
                                case ITPrimitiveTypeType.UInt64:
                                    il.Emit(OpCodes.Stelem_I8);
                                    break;
                                case ITPrimitiveTypeType.Float:
                                    il.Emit(OpCodes.Stelem_R4);
                                    break;
                                case ITPrimitiveTypeType.Double:
                                    il.Emit(OpCodes.Stelem_R8);
                                    break;
                                case ITPrimitiveTypeType.String:
                                    il.Emit(OpCodes.Stelem_Ref);
                                    break;
                            }
                        }

                    }

                    return new ExpressionOutputInfo();
                }


                private void TryEmitNonPrimitiveComparsion(ITType typeOnStack, ITExpression secondExpr)
                {
                    ITPrimitiveType primType = typeOnStack as ITPrimitiveType;
                    if (primType != null && primType.Type == ITPrimitiveTypeType.String)
                    {
                        fCompiler.EmitExpression(secondExpr);
                        il.Emit(OpCodes.Call, typeof(string).GetMethod("Compare", new Type[] { typeof(string), typeof(string) }));
                        return;
                    }

                    ITArrayType arr = typeOnStack as ITArrayType;
                    if (arr != null && arr.IsComparableTo(secondExpr.ExpressionType))
                    {
                        ITArrayType arr2 = (ITArrayType)secondExpr.ExpressionType;
                        MethodInfo comparator = typeof(Queen.Kuin.CompilerServices.RuntimeHelper).GetMethod("ArrayCompare");
                        comparator = comparator.MakeGenericMethod(new Type[] {compiler.GetCliType(arr.ElementType),
                        compiler.GetCliType(arr2.ElementType)});
                        fCompiler.EmitExpression(secondExpr);
                        il.Emit(OpCodes.Call, comparator);
                        return;
                    }

                    Type typ1 = compiler.GetCliType(typeOnStack);
                    if (typeof(IComparable<Queen.Kuin.CClass>).IsAssignableFrom(typ1))
                    {
                        MethodInfo compMeth = typeof(IComparable<Queen.Kuin.CClass>).GetMethod("CompareTo");
                        fCompiler.EmitExpression(secondExpr);
                        il.Emit(OpCodes.Callvirt, compMeth);
                        return;
                    }

                    throw new InvalidOperationException();
                }

                private ExpressionOutputInfo EmitBinaryOperator(ITBinaryOperatorType type, ITType typeOnStack, ITExpression secondExpr)
                {
                    ExpressionOutputInfo info = new ExpressionOutputInfo();
                    switch (type)
                    {
                        case ITBinaryOperatorType.ReferenceEquality:
                            fCompiler.EmitExpression(secondExpr);
                            il.Emit(OpCodes.Ceq);
                            break;
                        case ITBinaryOperatorType.ReferenceInequality:
                            fCompiler.EmitExpression(secondExpr);
                            il.Emit(OpCodes.Ceq);
                            info.logicNegated = true;
                            return info;
                        default:
                            ITPrimitiveType prim1 = typeOnStack as ITPrimitiveType;
                            ITPrimitiveType prim2 = secondExpr.ExpressionType as ITPrimitiveType;
                            ITArrayType arr1 = typeOnStack as ITArrayType;

                            ITClassType cls1 = typeOnStack as ITClassType;
                            ITClassType cls2 = secondExpr.ExpressionType as ITClassType;

                            if (cls1 != null)
                            {
                                prim1 = cls1.UnderlyingEnumType;
                            }
                            if (cls2 != null)
                            {
                                prim2 = cls2.UnderlyingEnumType;
                            }

                            if (prim1 != null && prim2 != null)
                            {
                                // primitive operation
                                
                                switch (type)
                                {
                                    case ITBinaryOperatorType.Add: 
                                        fCompiler.EmitExpression(secondExpr);
                                        if (prim1.Type == ITPrimitiveTypeType.Integer)
                                            il.Emit(OpCodes.Add_Ovf);
                                        else
                                            il.Emit(OpCodes.Add);
                                        break;
                                    case ITBinaryOperatorType.Subtract: 
                                        fCompiler.EmitExpression(secondExpr);
                                        if (prim1.Type == ITPrimitiveTypeType.Integer)
                                            il.Emit(OpCodes.Sub_Ovf);
                                        else
                                            il.Emit(OpCodes.Sub); 
                                        break;
                                    case ITBinaryOperatorType.Multiply: 
                                        fCompiler.EmitExpression(secondExpr); 
                                        if (prim1.Type == ITPrimitiveTypeType.Integer)
                                            il.Emit(OpCodes.Mul_Ovf);
                                        else
                                            il.Emit(OpCodes.Mul); 
                                        break;
                                    case ITBinaryOperatorType.Divide:
                                        fCompiler.EmitExpression(secondExpr);
                                        switch (prim1.Type)
                                        {
                                            case ITPrimitiveTypeType.UInt8:
                                            case ITPrimitiveTypeType.UInt16:
                                            case ITPrimitiveTypeType.UInt32:
                                            case ITPrimitiveTypeType.UInt64:
                                                il.Emit(OpCodes.Div_Un);
                                                break;
                                            default:
                                                il.Emit(OpCodes.Div);
                                                break;
                                        }
                                        break;
                                    case ITBinaryOperatorType.And:
                                        {
                                            Label evalZero = il.DefineLabel();
                                            Label evalEnd = il.DefineLabel();
                                            il.Emit(OpCodes.Brfalse, evalZero);
                                            fCompiler.EmitExpression(secondExpr);
                                            il.Emit(OpCodes.Br, evalEnd);
                                            il.MarkLabel(evalZero);
                                            il.Emit(OpCodes.Ldc_I4_0);
                                            il.MarkLabel(evalEnd);

                                            // FIXME: might be optimized using De Morgan's laws
                                        }
                                        break;
                                    case ITBinaryOperatorType.Or:
                                        {
                                            Label evalOne = il.DefineLabel();
                                            Label evalEnd = il.DefineLabel();
                                            il.Emit(OpCodes.Brtrue, evalOne);
                                            fCompiler.EmitExpression(secondExpr);
                                            il.Emit(OpCodes.Br, evalEnd);
                                            il.MarkLabel(evalOne);
                                            il.Emit(OpCodes.Ldc_I4_1);
                                            il.MarkLabel(evalEnd);

                                            // FIXME: might be optimized using De Morgan's laws
                                        }
                                        break;
                                    case ITBinaryOperatorType.Concat:
                                        if (arr1 != null && arr1.IsConcatable())
                                        {
                                            fCompiler.EmitExpression(secondExpr);

                                            MethodInfo meth = typeof(Queen.Kuin.CompilerServices.RuntimeHelper).GetMethod("ArrayConcat");
                                            meth = meth.MakeGenericMethod(new Type[] { compiler.GetCliType(arr1.ElementType) });

                                            il.Emit(OpCodes.Call, meth);
                                        }
                                        else
                                        {
                                            if (prim1.Type != ITPrimitiveTypeType.String)
                                            {
                                                throw new InvalidOperationException("Concat is valid only for string.");
                                            }
                                            fCompiler.EmitExpression(secondExpr);
                                            il.Emit(OpCodes.Call, typeof(string).GetMethod("Concat", new Type[] { typeof(string), typeof(string) }));
                                        }
                                        break;
                                    case ITBinaryOperatorType.Equality:
                                        if (prim1.Type == ITPrimitiveTypeType.String)
                                        {
                                            fCompiler.EmitExpression(secondExpr);
                                            il.Emit(OpCodes.Call, typeof(string).GetMethod("Equals", new Type[] { typeof(string), typeof(string) }));
                                        }
                                        else if (prim1 == null)
                                        {
                                            TryEmitNonPrimitiveComparsion(typeOnStack, secondExpr);
                                            il.Emit(OpCodes.Ldc_I4_0);
                                            il.Emit(OpCodes.Ceq);
                                        }
                                        else
                                        {
                                            fCompiler.EmitExpression(secondExpr);
                                            il.Emit(OpCodes.Ceq);
                                        }
                                        break;
                                    case ITBinaryOperatorType.Inequality:
                                        if (prim1.Type == ITPrimitiveTypeType.String)
                                        {
                                            fCompiler.EmitExpression(secondExpr);
                                            il.Emit(OpCodes.Call, typeof(string).GetMethod("Equals", new Type[] { typeof(string), typeof(string) }));
                                        }
                                        else if (prim1 == null)
                                        {
                                            TryEmitNonPrimitiveComparsion(typeOnStack, secondExpr);
                                            il.Emit(OpCodes.Ldc_I4_0);
                                            il.Emit(OpCodes.Ceq);
                                        }
                                        else
                                        {
                                            fCompiler.EmitExpression(secondExpr);
                                            il.Emit(OpCodes.Ceq);
                                        }
                                        info.logicNegated = true;
                                        break;
                                    case ITBinaryOperatorType.GreaterThan:
                                        if (prim1.Type == ITPrimitiveTypeType.String || prim1 == null)
                                        {
                                            TryEmitNonPrimitiveComparsion(typeOnStack, secondExpr);
                                            il.Emit(OpCodes.Ldc_I4_0);
                                            il.Emit(OpCodes.Cgt);
                                        }
                                        else
                                        {
                                            fCompiler.EmitExpression(secondExpr);
                                            il.Emit(OpCodes.Cgt);
                                        }
                                        break;
                                    case ITBinaryOperatorType.LessThan:
                                        if (prim1.Type == ITPrimitiveTypeType.String || prim1 == null)
                                        {
                                            TryEmitNonPrimitiveComparsion(typeOnStack, secondExpr);
                                            il.Emit(OpCodes.Ldc_I4_0);
                                            il.Emit(OpCodes.Clt);
                                        }
                                        else
                                        {
                                            fCompiler.EmitExpression(secondExpr);
                                            il.Emit(OpCodes.Clt);
                                        }
                                        break;

                                    case ITBinaryOperatorType.GreaterThanOrEqual:
                                        if (prim1.Type == ITPrimitiveTypeType.String || prim1 == null)
                                        {
                                            TryEmitNonPrimitiveComparsion(typeOnStack, secondExpr);
                                            il.Emit(OpCodes.Ldc_I4_0);
                                            il.Emit(OpCodes.Clt);
                                            info.logicNegated = true;
                                            return info;
                                        }
                                        else
                                        {
                                            fCompiler.EmitExpression(secondExpr);
                                            il.Emit(OpCodes.Clt);
                                            info.logicNegated = true;
                                            return info;
                                        }

                                    case ITBinaryOperatorType.LessThanOrEqual:
                                        if (prim1.Type == ITPrimitiveTypeType.String || prim1 == null)
                                        {
                                            TryEmitNonPrimitiveComparsion(typeOnStack, secondExpr);
                                            il.Emit(OpCodes.Ldc_I4_0);
                                            il.Emit(OpCodes.Cgt);
                                            info.logicNegated = true;
                                            return info;
                                        }
                                        else
                                        {
                                            fCompiler.EmitExpression(secondExpr);
                                            il.Emit(OpCodes.Cgt);
                                            info.logicNegated = true;
                                            return info;
                                        }
                                    case ITBinaryOperatorType.Modulus:
                                        switch (prim1.Type)
                                        {
                                            case ITPrimitiveTypeType.Int8:
                                            case ITPrimitiveTypeType.Int16:
                                            case ITPrimitiveTypeType.Int32:
                                            case ITPrimitiveTypeType.Int64:
                                            case ITPrimitiveTypeType.Integer:
                                            case ITPrimitiveTypeType.Float:
                                            case ITPrimitiveTypeType.Double:
                                                fCompiler.EmitExpression(secondExpr);
                                                il.Emit(OpCodes.Rem);
                                                break;
                                            case ITPrimitiveTypeType.UInt8:
                                            case ITPrimitiveTypeType.UInt16:
                                            case ITPrimitiveTypeType.UInt32:
                                            case ITPrimitiveTypeType.UInt64:
                                                fCompiler.EmitExpression(secondExpr);
                                                il.Emit(OpCodes.Rem_Un);
                                                break;

                                            default:
                                                throw new NotImplementedException();
                                        }
                                        break;
                                    case ITBinaryOperatorType.Power:
                                        switch (prim1.Type)
                                        {
                                            case ITPrimitiveTypeType.Int8:
                                            case ITPrimitiveTypeType.Int16:
                                            case ITPrimitiveTypeType.Int32:
                                                fCompiler.EmitExpression(secondExpr);
                                                il.Emit(OpCodes.Call, typeof(Queen.Kuin.CompilerServices.RuntimeHelper).GetMethod("IntPower32", new Type[] { typeof(int), typeof(int) }));
                                                break;
                                            case ITPrimitiveTypeType.Int64:
                                                fCompiler.EmitExpression(secondExpr);
                                                il.Emit(OpCodes.Call, typeof(Queen.Kuin.CompilerServices.RuntimeHelper).GetMethod("IntPower64", new Type[] { typeof(long), typeof(long) }));
                                                break;
                                            case ITPrimitiveTypeType.Integer:
                                                fCompiler.EmitExpression(secondExpr);
                                                il.Emit(OpCodes.Call, typeof(Queen.Kuin.CompilerServices.RuntimeHelper).GetMethod("IntPower64Checked", new Type[] { typeof(long), typeof(long) }));
                                                break;
                                            case ITPrimitiveTypeType.UInt8:
                                            case ITPrimitiveTypeType.UInt16:
                                            case ITPrimitiveTypeType.UInt32:
                                                fCompiler.EmitExpression(secondExpr);
                                                il.Emit(OpCodes.Call, typeof(Queen.Kuin.CompilerServices.RuntimeHelper).GetMethod("IntPowerU32", new Type[] { typeof(uint), typeof(uint) }));
                                                break;
                                            case ITPrimitiveTypeType.UInt64:
                                                fCompiler.EmitExpression(secondExpr);
                                                il.Emit(OpCodes.Call, typeof(Queen.Kuin.CompilerServices.RuntimeHelper).GetMethod("IntPowerU64", new Type[] { typeof(ulong), typeof(ulong) }));
                                                break;

                                            case ITPrimitiveTypeType.Float:
                                                il.Emit(OpCodes.Conv_R8);
                                                fCompiler.EmitExpression(secondExpr);
                                                il.Emit(OpCodes.Conv_R8);

                                                il.Emit(OpCodes.Call, typeof(Math).GetMethod("Pow", new Type[] { typeof(double), typeof(double) }));
                                                il.Emit(OpCodes.Conv_R4);
                                                break;
                                            case ITPrimitiveTypeType.Double:
                                                fCompiler.EmitExpression(secondExpr);
                                                il.Emit(OpCodes.Call, typeof(Math).GetMethod("Pow", new Type[] { typeof(double), typeof(double) }));
                                                break;
                                            default:
                                                throw new NotImplementedException();
                                        }
                                        break;
                                    default:
                                        throw new NotImplementedException();
                                }

                                switch (prim1.Type)
                                {
                                    case ITPrimitiveTypeType.Int8:
                                        il.Emit(OpCodes.Conv_I1);
                                        break;
                                    case ITPrimitiveTypeType.Int16:
                                        il.Emit(OpCodes.Conv_I2);
                                        break;
                                    case ITPrimitiveTypeType.UInt8:
                                        il.Emit(OpCodes.Conv_U1);
                                        break;
                                    case ITPrimitiveTypeType.UInt16:
                                        il.Emit(OpCodes.Conv_U2);
                                        break;
                                }
                            }
                            else
                            {


                                switch (type)
                                {
                                    case ITBinaryOperatorType.Concat:
                                        if (arr1 != null && arr1.IsConcatable())
                                        {
                                            fCompiler.EmitExpression(secondExpr);

                                            MethodInfo meth = typeof(Queen.Kuin.CompilerServices.RuntimeHelper).GetMethod("ArrayConcat");
                                            meth = meth.MakeGenericMethod(new Type[] { compiler.GetCliType(arr1.ElementType) });

                                            il.Emit(OpCodes.Call, meth);
                                        }
                                        else
                                        {
                                            if (prim1.Type != ITPrimitiveTypeType.String)
                                            {
                                                throw new InvalidOperationException("Concat is valid only for string.");
                                            }
                                            fCompiler.EmitExpression(secondExpr);
                                            il.Emit(OpCodes.Call, typeof(string).GetMethod("Concat", new Type[] { typeof(string), typeof(string) }));
                                        }
                                        return info;
                                    case ITBinaryOperatorType.Equality:
                                        TryEmitNonPrimitiveComparsion(typeOnStack, secondExpr);
                                        il.Emit(OpCodes.Ldc_I4_0);
                                        il.Emit(OpCodes.Ceq);
                                        return info;
                                    case ITBinaryOperatorType.Inequality:
                                        TryEmitNonPrimitiveComparsion(typeOnStack, secondExpr);
                                        il.Emit(OpCodes.Ldc_I4_0);
                                        il.Emit(OpCodes.Ceq);
                                        info.logicNegated = true;
                                        return info;
                                    case ITBinaryOperatorType.GreaterThan:
                                        TryEmitNonPrimitiveComparsion(typeOnStack, secondExpr);
                                        il.Emit(OpCodes.Ldc_I4_0);
                                        il.Emit(OpCodes.Cgt);
                                        return info;
                                    case ITBinaryOperatorType.LessThan:
                                        TryEmitNonPrimitiveComparsion(typeOnStack, secondExpr);
                                        il.Emit(OpCodes.Ldc_I4_0);
                                        il.Emit(OpCodes.Clt);
                                        return info;

                                    case ITBinaryOperatorType.GreaterThanOrEqual:
                                        TryEmitNonPrimitiveComparsion(typeOnStack, secondExpr);
                                        il.Emit(OpCodes.Ldc_I4_0);
                                        il.Emit(OpCodes.Clt);
                                        info.logicNegated = true;
                                        return info;

                                    case ITBinaryOperatorType.LessThanOrEqual:
                                        TryEmitNonPrimitiveComparsion(typeOnStack, secondExpr);
                                        il.Emit(OpCodes.Ldc_I4_0);
                                        il.Emit(OpCodes.Cgt);
                                        info.logicNegated = true;
                                        return info;
                                    default:
                                        throw new NotImplementedException();
                                }

                            }
                            break;
                    }
                    return info;
                }

                public ExpressionOutputInfo Visit(ITBinaryOperatorExpression expr)
                {
                    fCompiler.EmitExpression(expr.Left);
                    ExpressionOutputInfo info = EmitBinaryOperator(expr.OperatorType, expr.Left.ExpressionType, expr.Right);
                    if (discardValue && expr.ExpressionType != null)
                    {
                        il.Emit(OpCodes.Pop);
                    }
                    return info;
                }

                private bool CheckConditionalMethod(MethodInfo info)
                {
                    if (info.IsGenericMethod || (info is MethodBuilder))
                    {
                        return true;
                    }
                    try
                    {
                        object[] attrs = info.GetCustomAttributes(typeof(System.Diagnostics.ConditionalAttribute), true);
                        foreach (object ob in attrs)
                        {
                            var cond = (System.Diagnostics.ConditionalAttribute)ob;
                            if (compiler.Options.IsReleaseBuild)
                            {
                                if (!cond.ConditionString.Equals("NDEBUG"))
                                {
                                    return false;
                                }
                            }
                            else
                            {
                                if (!cond.ConditionString.Equals("DEBUG"))
                                {
                                    return false;
                                }
                            }
                        }
                        return true;
                    }
                    catch (NotSupportedException)
                    {
                        // built type is not conditionally omit
                        // FIXME: eliminate use of try/catch
                        return true;
                    }
                }

                public ExpressionOutputInfo Visit(ITCallMemberFunctionExpression expr)
                {

                    // virtual member function (doesn't emit normal instructions)
                    CliVirtualMemberFunction virtualMemFun = expr.Function.Function as CliVirtualMemberFunction;

                    ITExpression obj = expr.Function.Object;
                    fCompiler.EmitExpression(obj);

                    // box primitive
                    MethodInfo info = null;
                    if (virtualMemFun == null)
                    {
                        info = compiler.GetCliMethod(expr.Function.Object.ExpressionType,
                            expr.Function.Function, expr.Function.GenericTypeParameters);

                        ITPrimitiveType primType = obj.ExpressionType as ITPrimitiveType;
                        if (primType != null && primType.Type != ITPrimitiveTypeType.String)
                        {
                            il.Emit(OpCodes.Box, compiler.GetCliType(primType));
                        }
                    }

                    // handle ConditionalAttribute
                    if (info != null && expr.Function.Function.Body.ReturnType == null)
                    {
                        if (!CheckConditionalMethod(info))
                        {
                            // FIXME: don't evaluate the object
                            il.Emit(OpCodes.Pop);
                            return new ExpressionOutputInfo();
                        }
                    }

                    IEnumerator<ITFunctionParameter> funParams = expr.Function.Function.Body.Parameters.GetEnumerator();
                    foreach (ITExpression arg in expr.Parameters)
                    {
                        funParams.MoveNext();
                        fCompiler.EmitFunctionParameter(funParams.Current, arg);
                    }
                    if (virtualMemFun != null)
                    {
                        virtualMemFun.EmitIL(il, compiler);
                    }
                    else
                    {
                        il.Emit(OpCodes.Callvirt, info);
                    }
                    if (expr.Function.Function.Body.ReturnType != null && discardValue)
                    {
                        il.Emit(OpCodes.Pop);
                    }
                    return new ExpressionOutputInfo();
                }


                public ExpressionOutputInfo Visit(ITCallFunctionReferenceExpression expr)
                {
                    ITExpression fun = expr.Function;
                    fCompiler.EmitExpression(fun);

                    var funcType = (ITFunctionType)(fun.ExpressionType);
                    compiler.GetCliType(funcType); // ensure UserData is created
                    CliFuncionTypeInfo info = (CliFuncionTypeInfo)(funcType.UserData);
                    var funParams = funcType.Parameters;
                    int idx = 0;
                    foreach (ITExpression arg in expr.Parameters)
                    {
                        fCompiler.EmitFunctionParameter(funParams[idx++], arg);
                    }

                    il.Emit(OpCodes.Callvirt, info.invokeMethod);
                    if (funcType.ReturnType != null && discardValue)
                    {
                        il.Emit(OpCodes.Pop);
                    }
                    return new ExpressionOutputInfo();
                }

                public ExpressionOutputInfo Visit(ITCallGlobalFunctionExpression expr)
                {
                    MethodInfo info = compiler.GetCliMethod(expr.Function.Function);
                    IEnumerator<ITFunctionParameter> funParams = expr.Function.Function.Body.Parameters.GetEnumerator();

                    if (expr.Function.Function.Body.ReturnType == null)
                    {
                        // handle ConditionalAttribute
                        if (!CheckConditionalMethod(info))
                        {
                            return new ExpressionOutputInfo();
                        }
                    }

                    foreach (ITExpression arg in expr.Parameters)
                    {
                        funParams.MoveNext();
                        fCompiler.EmitFunctionParameter(funParams.Current, arg);
                    }
                    il.Emit(OpCodes.Call, info);
                    if (expr.Function.Function.Body.ReturnType != null && discardValue)
                    {
                        il.Emit(OpCodes.Pop);
                    }
                    return new ExpressionOutputInfo();
                }

                public ExpressionOutputInfo Visit(ITCastExpression expr)
                {
                    ITType fromType = expr.Expression.ExpressionType;
                    ITPrimitiveType fromPrimType = fromType as ITPrimitiveType;
                    ITType toType = expr.ExpressionType;
                    ITPrimitiveType toPrimType = toType as ITPrimitiveType;

                    // handle enum -> integer/integer -> enum case
                    ITClassType fromClassType = fromType as ITClassType;
                    if (fromClassType != null)
                    {
                        fromPrimType = fromClassType.UnderlyingEnumType;
                    }

                    ITClassType toClassType = toType as ITClassType;
                    if (toClassType != null)
                    {
                        toPrimType = toClassType.UnderlyingEnumType;
                    }

                    

                    if (fromPrimType != null && toPrimType != null)
                    {
                        // primitive to primitive
                        fCompiler.EmitExpression(expr.Expression);
                        switch (toPrimType.Type)
                        {
                            case ITPrimitiveTypeType.Char:
                                if(fromPrimType.Type != ITPrimitiveTypeType.Char &&
                                    fromPrimType.Type != ITPrimitiveTypeType.UInt16)
                                    il.Emit(OpCodes.Conv_U2); // FIXME: overflow check?
                                break;
                            case ITPrimitiveTypeType.Double:
                                if (fromPrimType.Type != ITPrimitiveTypeType.Double)
                                    il.Emit(OpCodes.Conv_R8);
                                break;
                            case ITPrimitiveTypeType.Float:
                                if (fromPrimType.Type != ITPrimitiveTypeType.Float)
                                    il.Emit(OpCodes.Conv_R4);
                                break;
                            case ITPrimitiveTypeType.Int8:
                                if (fromPrimType.Type != ITPrimitiveTypeType.Int8)
                                    il.Emit(OpCodes.Conv_I1);
                                break;
                            case ITPrimitiveTypeType.Int16:
                                if (fromPrimType.Type != ITPrimitiveTypeType.Int16)
                                    il.Emit(OpCodes.Conv_I2);
                                break;
                            case ITPrimitiveTypeType.Int32:
                                if (fromPrimType.Type != ITPrimitiveTypeType.Int32)
                                    il.Emit(OpCodes.Conv_I4);
                                break;
                            case ITPrimitiveTypeType.Int64:
                                if (fromPrimType.Type != ITPrimitiveTypeType.Int64 &&
                                    fromPrimType.Type != ITPrimitiveTypeType.Integer)
                                    il.Emit(OpCodes.Conv_I8);
                                break;
                            case ITPrimitiveTypeType.Integer:
                                if (fromPrimType.Type != ITPrimitiveTypeType.Int64 &&
                                    fromPrimType.Type != ITPrimitiveTypeType.Integer)
                                il.Emit(OpCodes.Conv_Ovf_I8);
                                break;
                            case ITPrimitiveTypeType.UInt8:
                                if (fromPrimType.Type != ITPrimitiveTypeType.UInt8)
                                    il.Emit(OpCodes.Conv_U1);
                                break;
                            case ITPrimitiveTypeType.UInt16:
                                if (fromPrimType.Type != ITPrimitiveTypeType.UInt16)
                                    il.Emit(OpCodes.Conv_U2);
                                break;
                            case ITPrimitiveTypeType.UInt32:
                                if (fromPrimType.Type != ITPrimitiveTypeType.UInt32)
                                    il.Emit(OpCodes.Conv_U4);
                                break;
                            case ITPrimitiveTypeType.UInt64:
                                if (fromPrimType.Type != ITPrimitiveTypeType.UInt64)
                                    il.Emit(OpCodes.Conv_U8);
                                break;
                            default:
                                throw new NotSupportedException();
                        }
                    }
                    else
                    {
                        // handle implicit string <--> char[]
                        ITArrayType fromArrayType = fromType as ITArrayType;
                        ITArrayType toArrayType = toType as ITArrayType;
                        if (fromArrayType != null && toPrimType != null && 
                            fromArrayType.IsCompatibleWithString() && toPrimType.Type == ITPrimitiveTypeType.String)
                        {
                            fCompiler.EmitExpression(expr.Expression, discardValue);

                            if (discardValue)
                            {
                                return new ExpressionOutputInfo();
                            }

                            ConstructorInfo ctor = typeof(string).GetConstructor(new Type[] { typeof(char[]) });
                            il.Emit(OpCodes.Newobj, ctor);
                        }
                        else if (toArrayType != null && fromPrimType != null &&
                           toArrayType.IsCompatibleWithString() && fromPrimType.Type == ITPrimitiveTypeType.String)
                        {
                            fCompiler.EmitExpression(expr.Expression, discardValue);

                            if (discardValue)
                            {
                                return new ExpressionOutputInfo();
                            }
                            MethodInfo meth = typeof(string).GetMethod("ToCharArray", new Type[] {});
                            il.Emit(OpCodes.Callvirt, meth);
                        }
                        else
                        {
                            fCompiler.EmitExpression(expr.Expression);
                            Type toCliType = compiler.GetCliType(toType);
                            il.Emit(OpCodes.Castclass, toCliType);
                        }
                    }
                    if (discardValue)
                    {
                        il.Emit(OpCodes.Pop);
                    }
                    return new ExpressionOutputInfo();
                }

                public ExpressionOutputInfo Visit(ITClassConstructExpression expr)
                {
                    //Type typ = compiler.GetCliType(expr.Type);
                    ConstructorInfo info = compiler.GetCliConstructor(expr.Type);
                   
                    il.Emit(OpCodes.Newobj, info);
                    if (discardValue)
                    {
                        il.Emit(OpCodes.Pop);
                    }
                    return new ExpressionOutputInfo();
                }

                public ExpressionOutputInfo Visit(ITConditionalExpression expr)
                {
                    fCompiler.EmitExpression(expr.Conditional);
                    Label endLabel = il.DefineLabel();
                    Label falseLabel = il.DefineLabel();
                    il.Emit(OpCodes.Brfalse, falseLabel);
                    fCompiler.EmitExpression(expr.TrueValue, discardValue);
                    il.Emit(OpCodes.Br, endLabel);
                    il.MarkLabel(falseLabel);
                    fCompiler.EmitExpression(expr.FalseValue, discardValue);
                    il.MarkLabel(endLabel);
                    return new ExpressionOutputInfo();
                }

                public ExpressionOutputInfo Visit(ITErrorExpression expr)
                {
                    throw new InvalidOperationException("Error expression encounted");
                }

                public ExpressionOutputInfo Visit(ITMemberVariableStorage expr)
                {
                    
                    FieldInfo info = compiler.GetCliField(expr.Instance.ExpressionType, expr.Member);
                    fCompiler.EmitExpression(expr.Instance);
                    il.Emit(OpCodes.Ldfld, info);
                    if (discardValue)
                    {
                        il.Emit(OpCodes.Pop);
                    }
                    return new ExpressionOutputInfo();
                }

                public ExpressionOutputInfo Visit(ITMemberPropertyStorage expr)
                {
                    CliProperty prop = compiler.GetCliProperty(expr.Instance.ExpressionType, expr.Member, null);
                    fCompiler.EmitExpression(expr.Instance);

                    MethodInfo info = prop.getter;

                    IEnumerator<ITFunctionParameter> funParams = expr.Member.Parameters.GetEnumerator();
                    if (expr.Parameters != null)
                    {
                        foreach (ITExpression arg in expr.Parameters)
                        {
                            funParams.MoveNext();
                            fCompiler.EmitFunctionParameter(funParams.Current, arg);
                        }
                    }
                    il.Emit(OpCodes.Callvirt, info);
                    if (discardValue)
                    {
                        il.Emit(OpCodes.Pop);
                    }
                    return new ExpressionOutputInfo();
                }

                public ExpressionOutputInfo Visit(ITGlobalVariableStorage expr)
                {
                    if (discardValue)
                    {
                        return new ExpressionOutputInfo();
                    }
                    FieldInfo info = compiler.GetCliField(expr.Variable, expr.GenericTypeParameters);
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Ldfld, info);
                    return new ExpressionOutputInfo();
                }

                public ExpressionOutputInfo Visit(ITLocalVariableStorage expr)
                {
                    if (discardValue)
                        return new ExpressionOutputInfo();
                    ITLocalVariable var = expr.Variable;
                    LocalBuilder lb = var.UserData as LocalBuilder;
                    il.Emit(OpCodes.Ldloc, lb);
                    return new ExpressionOutputInfo();
                }

                public ExpressionOutputInfo Visit(ITParameterStorage expr)
                {
                    if (discardValue)
                        return new ExpressionOutputInfo();

                    ITFunctionParameter param = expr.Variable;
                    int index = (int)param.UserData;
                    il.Emit(OpCodes.Ldarg, index);
                    if (param.IsByRef)
                    {
                        ITPrimitiveType prim = param.Type as ITPrimitiveType;
                        if (prim != null)
                        {
                            switch (prim.Type)
                            {
                                case ITPrimitiveTypeType.Bool:
                                case ITPrimitiveTypeType.Int8:
                                    il.Emit(OpCodes.Ldind_I1);
                                    break;
                                case ITPrimitiveTypeType.Int16:
                                    il.Emit(OpCodes.Ldind_I2);
                                    break;
                                case ITPrimitiveTypeType.Int32:
                                    il.Emit(OpCodes.Ldind_I4);
                                    break;
                                case ITPrimitiveTypeType.Int64:
                                case ITPrimitiveTypeType.Integer:
                                    il.Emit(OpCodes.Ldind_I8);
                                    break;
                                case ITPrimitiveTypeType.UInt8:
                                    il.Emit(OpCodes.Ldind_U1);
                                    break;
                                case ITPrimitiveTypeType.UInt16:
                                    il.Emit(OpCodes.Ldind_U2);
                                    break;
                                case ITPrimitiveTypeType.UInt32:
                                    il.Emit(OpCodes.Ldind_U4);
                                    break;
                                case ITPrimitiveTypeType.UInt64:
                                    il.Emit(OpCodes.Ldind_I8);
                                    break;
                                case ITPrimitiveTypeType.Float:
                                    il.Emit(OpCodes.Ldind_R4);
                                    break;
                                case ITPrimitiveTypeType.Double:
                                    il.Emit(OpCodes.Ldind_R8);
                                    break;
                                case ITPrimitiveTypeType.Char:
                                    il.Emit(OpCodes.Ldind_U2);
                                    break;
                                case ITPrimitiveTypeType.String:
                                    il.Emit(OpCodes.Ldind_Ref);
                                    break;
                                default:
                                    throw new NotImplementedException();
                            }
                        }
                        else
                        {
                            il.Emit(OpCodes.Ldind_Ref);
                        }
                    }
                    return new ExpressionOutputInfo();
                }

                public ExpressionOutputInfo Visit(ITArrayElementStorage expr)
                {
                    fCompiler.EmitExpression(expr.Variable);
                    ITArrayType arr = expr.Variable.ExpressionType as ITArrayType;
                    if (arr.Dimensions == 1)
                    {
                        fCompiler.EmitExpression(expr.Indices[0]);
                        EmitLoadArrayElement(expr.ExpressionType);
                    }
                    else
                    {
                        foreach (ITExpression idx in expr.Indices)
                            fCompiler.EmitExpression(idx);

                        Type[] prms = new Type[expr.Indices.Count];
                        for (int i = 0; i < prms.Length; i++) prms[i] = typeof(int);
                        MethodInfo meth = compiler.GetCliType(arr).GetMethod("Get", prms);
                        il.Emit(OpCodes.Call, meth);
                    }
                    if (discardValue)
                    {
                        il.Emit(OpCodes.Pop);
                    }
                    return new ExpressionOutputInfo();
                }

                public ExpressionOutputInfo Visit(ITGlobalFunctionStorage expr)
                {
                    if (discardValue)
                    {
                        return new ExpressionOutputInfo();
                    }
                    il.Emit(OpCodes.Ldnull);
                    MethodInfo info = compiler.GetCliMethod(expr.Function);
                    il.Emit(OpCodes.Ldftn, info);

                    Type typ = compiler.GetCliType(expr.ExpressionType);
                    il.Emit(OpCodes.Newobj, ((CliTypeInfo)(expr.ExpressionType.UserData)).constructor);
                    return new ExpressionOutputInfo();
                }

                public ExpressionOutputInfo Visit(ITMemberFunctionStorage expr)
                {
                    ITExpression obj = expr.Object;
                    fCompiler.EmitExpression(obj);

                    CliVirtualMemberFunction virtualMemFun = expr.Function as CliVirtualMemberFunction;
                    if (virtualMemFun != null)
                    {
                        il.Emit(OpCodes.Call, typeof(Queen.Kuin.CompilerServices.RuntimeHelper).GetMethod("VirtualMemberFunctionPointerError"));
                        il.Emit(OpCodes.Castclass, compiler.GetCliType(expr.ExpressionType));
                        return new ExpressionOutputInfo();
                    }

                    ITPrimitiveType primType = obj.ExpressionType as ITPrimitiveType;
                    if (primType != null && primType.Type != ITPrimitiveTypeType.String)
                    {
                        il.Emit(OpCodes.Box, compiler.GetCliType(primType));
                    }

                    var info = compiler.GetCliMethod(expr.Object.ExpressionType,
                        expr.Function, expr.GenericTypeParameters);

                    il.Emit(OpCodes.Ldftn, info);

                    Type typ = compiler.GetCliType(expr.ExpressionType);
                    il.Emit(OpCodes.Newobj, ((CliTypeInfo)(expr.ExpressionType.UserData)).constructor);

                    return new ExpressionOutputInfo();
                }

                private void EmitLoadArrayElement(ITType elementType)
                {
                    ITPrimitiveType prim = elementType as ITPrimitiveType;
                    if (prim == null)
                    {
                        ITClassType cls = elementType as ITClassType;
                        if (cls != null)
                        {
                            prim = cls.UnderlyingEnumType;
                        }
                    }
                    if (prim != null && prim.Type != ITPrimitiveTypeType.String)
                    {
                        switch (prim.Type)
                        {
                            case ITPrimitiveTypeType.Bool:
                            case ITPrimitiveTypeType.Int8:
                                il.Emit(OpCodes.Ldelem_I1);
                                break;
                            case ITPrimitiveTypeType.Int16:
                                il.Emit(OpCodes.Ldelem_I2);
                                break;
                            case ITPrimitiveTypeType.Int32:
                                il.Emit(OpCodes.Ldelem_I4);
                                break;
                            case ITPrimitiveTypeType.Int64:
                            case ITPrimitiveTypeType.Integer:
                                il.Emit(OpCodes.Ldelem_I8);
                                break;
                            case ITPrimitiveTypeType.UInt8:
                                il.Emit(OpCodes.Ldelem_U1);
                                break;
                            case ITPrimitiveTypeType.Char:
                            case ITPrimitiveTypeType.UInt16:
                                il.Emit(OpCodes.Ldelem_U2);
                                break;
                            case ITPrimitiveTypeType.UInt32:
                                il.Emit(OpCodes.Ldelem_U4);
                                break;
                            case ITPrimitiveTypeType.UInt64:
                                il.Emit(OpCodes.Ldelem_I8);
                                break;
                            case ITPrimitiveTypeType.Float:
                                il.Emit(OpCodes.Ldelem_R4);
                                break;
                            case ITPrimitiveTypeType.Double:
                                il.Emit(OpCodes.Ldelem_R8);
                                break;
                            default:
                                throw new InvalidOperationException();
                        }
                    }
                    else if(elementType is ITGenericTypeParameter)
                    {
                        il.Emit(OpCodes.Ldelem, compiler.GetCliType(elementType));
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldelem_Ref);
                    }
                }

                private void EmitStoreArrayElement(ITType elementType)
                {
                    ITPrimitiveType prim = elementType as ITPrimitiveType;
                    if (prim != null && prim.Type != ITPrimitiveTypeType.String)
                    {
                        switch (prim.Type)
                        {
                            case ITPrimitiveTypeType.Bool:
                            case ITPrimitiveTypeType.Int8:
                                il.Emit(OpCodes.Stelem_I1);
                                break;
                            case ITPrimitiveTypeType.Int16:
                                il.Emit(OpCodes.Stelem_I2);
                                break;
                            case ITPrimitiveTypeType.Int32:
                                il.Emit(OpCodes.Stelem_I4);
                                break;
                            case ITPrimitiveTypeType.Int64:
                            case ITPrimitiveTypeType.Integer:
                                il.Emit(OpCodes.Stelem_I8);
                                break;
                            case ITPrimitiveTypeType.UInt8:
                                il.Emit(OpCodes.Stelem_I1);
                                break;
                            case ITPrimitiveTypeType.Char:
                            case ITPrimitiveTypeType.UInt16:
                                il.Emit(OpCodes.Stelem_I2);
                                break;
                            case ITPrimitiveTypeType.UInt32:
                                il.Emit(OpCodes.Stelem_I4);
                                break;
                            case ITPrimitiveTypeType.UInt64:
                                il.Emit(OpCodes.Stelem_I8);
                                break;
                            case ITPrimitiveTypeType.Float:
                                il.Emit(OpCodes.Stelem_R4);
                                break;
                            case ITPrimitiveTypeType.Double:
                                il.Emit(OpCodes.Stelem_R8);
                                break;
                            default:
                                throw new InvalidOperationException();
                        }
                    }
                    else if (elementType is ITGenericTypeParameter)
                    {
                        il.Emit(OpCodes.Stelem, compiler.GetCliType(elementType));
                    }
                    else
                    {
                        il.Emit(OpCodes.Stelem_Ref);
                    }
                }

                public ExpressionOutputInfo Visit(ITAssignExpression expr)
                {
                    if (expr.AssignType == ITAssignType.Assign)
                    {
                        ITStorage val = expr.Storage;
                        ITLocalVariableStorage lvarStor = val as ITLocalVariableStorage;
                        if (lvarStor != null)
                        {
                            LocalBuilder local = lvarStor.Variable.UserData as LocalBuilder;
                            fCompiler.EmitExpression(expr.Value);
                            il.Emit(OpCodes.Stloc, local);
                            if (!discardValue)
                            {
                                il.Emit(OpCodes.Ldloc, local);
                            }
                            return new ExpressionOutputInfo();
                        }

                        ITGlobalVariableStorage gvarStor = val as ITGlobalVariableStorage;
                        if (gvarStor != null)
                        {
                            FieldInfo fld = compiler.GetCliField(gvarStor.Variable, gvarStor.GenericTypeParameters);

                            il.Emit(OpCodes.Ldnull);
                            fCompiler.EmitExpression(expr.Value);
                            il.Emit(OpCodes.Stfld, fld);
                            if (!discardValue)
                            {
                                il.Emit(OpCodes.Ldnull);
                                il.Emit(OpCodes.Ldfld, fld);
                            }
                            return new ExpressionOutputInfo();
                        }

                        ITArrayElementStorage elemStor = val as ITArrayElementStorage;
                        if (elemStor != null)
                        {
                            fCompiler.EmitExpression(elemStor.Variable);
                            ITArrayType arr = elemStor.Variable.ExpressionType as ITArrayType;
                            LocalBuilder loc = null;
                            if(!discardValue) il.DeclareLocal(compiler.GetCliType(expr.ExpressionType));
                            if (arr.Dimensions == 1)
                            {
                                fCompiler.EmitExpression(elemStor.Indices[0]);
                                fCompiler.EmitExpression(expr.Value);

                                if (!discardValue)
                                {
                                    il.Emit(OpCodes.Dup);
                                    il.Emit(OpCodes.Stloc, loc);
                                }

                                EmitStoreArrayElement(elemStor.ExpressionType);
                            }
                            else
                            {
                                foreach (ITExpression idx in elemStor.Indices)
                                    fCompiler.EmitExpression(idx);
                                fCompiler.EmitExpression(expr.Value);

                                if (!discardValue)
                                {
                                    il.Emit(OpCodes.Dup);
                                    il.Emit(OpCodes.Stloc, loc);
                                }

                                Type[] prms = new Type[elemStor.Indices.Count + 1];
                                for (int i = 0; i < prms.Length; i++) prms[i] = typeof(int);
                                prms[prms.Length - 1] = compiler.GetCliType(elemStor.ExpressionType);
                                MethodInfo meth = compiler.GetCliType(arr).GetMethod("Set", prms);
                                il.Emit(OpCodes.Call, meth);
                            }

                            if (!discardValue)
                            {
                                il.Emit(OpCodes.Ldloc, loc);
                            }
                            return new ExpressionOutputInfo();
                        }

                        ITParameterStorage prmStor = val as ITParameterStorage;
                        if (prmStor != null)
                        {
                            ITFunctionParameter prm = prmStor.Variable;
                            int idx = (int)prm.UserData;
                            if (prm.IsByRef)
                            {
                                il.Emit(OpCodes.Ldarg, idx);
                                fCompiler.EmitExpression(expr.Value);
                                LocalBuilder loc = null;
                                if (!discardValue)
                                {
                                    loc = il.DeclareLocal(compiler.GetCliType(expr.ExpressionType));
                                    il.Emit(OpCodes.Dup);
                                    il.Emit(OpCodes.Stloc, loc);
                                }
                                ITPrimitiveType prim = expr.ExpressionType as ITPrimitiveType;
                                if (prim != null && prim.Type != ITPrimitiveTypeType.String)
                                {
                                    switch (prim.Type)
                                    {
                                        case ITPrimitiveTypeType.Bool:
                                            il.Emit(OpCodes.Stind_I1);
                                            break;
                                        case ITPrimitiveTypeType.Char:
                                            il.Emit(OpCodes.Stind_I2);
                                            break;
                                        case ITPrimitiveTypeType.Int8:
                                            il.Emit(OpCodes.Stind_I1);
                                            break;
                                        case ITPrimitiveTypeType.Int16:
                                            il.Emit(OpCodes.Stind_I2);
                                            break;
                                        case ITPrimitiveTypeType.Int32:
                                            il.Emit(OpCodes.Stind_I4);
                                            break;
                                        case ITPrimitiveTypeType.Int64:
                                        case ITPrimitiveTypeType.Integer:
                                            il.Emit(OpCodes.Stind_I8);
                                            break;
                                        case ITPrimitiveTypeType.UInt8:
                                            il.Emit(OpCodes.Stind_I1);
                                            break;
                                        case ITPrimitiveTypeType.UInt16:
                                            il.Emit(OpCodes.Stind_I2);
                                            break;
                                        case ITPrimitiveTypeType.UInt32:
                                            il.Emit(OpCodes.Stind_I4);
                                            break;
                                        case ITPrimitiveTypeType.UInt64:
                                            il.Emit(OpCodes.Stind_I8);
                                            break;
                                        default:
                                            throw new NotImplementedException();
                                    }
                                }
                                else
                                {
                                    il.Emit(OpCodes.Stind_Ref);
                                }

                                if (!discardValue)
                                {
                                    il.Emit(OpCodes.Ldloc, loc);
                                }
                            }
                            else
                            {
                                fCompiler.EmitExpression(expr.Value);
                                il.Emit(OpCodes.Starg, idx);
                                if (!discardValue)
                                    il.Emit(OpCodes.Ldarg, idx);
                            }

                            
                            return new ExpressionOutputInfo();
                        }

                        ITMemberPropertyStorage memPropStor = val as ITMemberPropertyStorage;
                        if (memPropStor != null)
                        {
                            CliProperty prop = compiler.GetCliProperty(memPropStor.Instance.ExpressionType, memPropStor.Member, null);
                            fCompiler.EmitExpression(memPropStor.Instance);

                            MethodInfo info = prop.setter;

                            IEnumerator<ITFunctionParameter> funParams = memPropStor.Member.Parameters.GetEnumerator();
                            if (memPropStor.Parameters != null)
                            {
                                foreach (ITExpression arg in memPropStor.Parameters)
                                {
                                    funParams.MoveNext();
                                    fCompiler.EmitFunctionParameter(funParams.Current, arg);
                                }
                            }

                            fCompiler.EmitExpression(expr.Value);
                            LocalBuilder loc = null;
                            if (!discardValue)
                            {
                                loc = il.DeclareLocal(compiler.GetCliType(expr.ExpressionType));
                                il.Emit(OpCodes.Dup);
                                il.Emit(OpCodes.Stloc, loc);
                            }

                            il.Emit(OpCodes.Callvirt, info);

                            if (!discardValue)
                            {
                                il.Emit(OpCodes.Ldloc, loc);
                            }
                            return new ExpressionOutputInfo();
                        }

                        ITMemberVariableStorage memVarStor = val as ITMemberVariableStorage;
                        if (memVarStor != null)
                        {
                            FieldInfo info = compiler.GetCliField(memVarStor.Instance.ExpressionType, memVarStor.Member);
                            fCompiler.EmitExpression(memVarStor.Instance);
                            if (!discardValue)
                            {
                                il.Emit(OpCodes.Dup);
                            }
                            fCompiler.EmitExpression(expr.Value);
                            il.Emit(OpCodes.Stfld, info);
                            if (!discardValue)
                            {
                                il.Emit(OpCodes.Ldfld, info);
                            }
                            return new ExpressionOutputInfo();
                        }

                        throw new InvalidOperationException("Unassignable");
                    }
                    else
                    {
                        ITBinaryOperatorType binOpType;
                        switch (expr.AssignType)
                        {
                            case ITAssignType.AdditionAssign:
                                binOpType = ITBinaryOperatorType.Add;
                                break;
                            case ITAssignType.SubtractionAssign:
                                binOpType = ITBinaryOperatorType.Subtract;
                                break;
                            case ITAssignType.MultiplicationAssign:
                                binOpType = ITBinaryOperatorType.Multiply;
                                break;
                            case ITAssignType.DivisionAssign:
                                binOpType = ITBinaryOperatorType.Divide;
                                break;
                            case ITAssignType.ConcatAssign:
                                binOpType = ITBinaryOperatorType.Concat;
                                break;
                            case ITAssignType.ModulusAssign:
                                binOpType = ITBinaryOperatorType.Modulus;
                                break;
                            case ITAssignType.OrAssign:
                                binOpType = ITBinaryOperatorType.Or;
                                break;
                            case ITAssignType.AndAssign:
                                binOpType = ITBinaryOperatorType.And;
                                break;
                            case ITAssignType.PowerAssign:
                                binOpType = ITBinaryOperatorType.Power;
                                break;
                            default:
                                throw new InvalidOperationException();
                        }

                        ITStorage val = expr.Storage;
                        ITLocalVariableStorage lvarStor = val as ITLocalVariableStorage;
                        if (lvarStor != null)
                        {
                            LocalBuilder local = lvarStor.Variable.UserData as LocalBuilder;
                            il.Emit(OpCodes.Ldloc, local);
                            ExpressionOutputInfo info = EmitBinaryOperator(binOpType, lvarStor.Variable.Type, expr.Value);
                            info = fCompiler.FilterValue(info, 0);
                            il.Emit(OpCodes.Stloc, local);
                            if (!discardValue)
                            {
                                il.Emit(OpCodes.Ldloc, local);
                            }
                            return new ExpressionOutputInfo();
                        }

                        ITGlobalVariableStorage gvarStor = val as ITGlobalVariableStorage;
                        if (gvarStor != null)
                        {
                            FieldInfo fld = compiler.GetCliField(gvarStor.Variable, gvarStor.GenericTypeParameters);
                            il.Emit(OpCodes.Ldnull);
                            il.Emit(OpCodes.Ldnull);
                            il.Emit(OpCodes.Ldfld, fld);
                            ExpressionOutputInfo info = EmitBinaryOperator(binOpType, gvarStor.Variable.Type, expr.Value);
                            info = fCompiler.FilterValue(info, 0);
                            il.Emit(OpCodes.Stfld, fld);
                            if (!discardValue)
                            {
                                il.Emit(OpCodes.Ldfld, fld);
                            }
                            return new ExpressionOutputInfo();
                        }

                        ITArrayElementStorage elemStor = val as ITArrayElementStorage;
                        if (elemStor != null)
                        {
                            fCompiler.EmitExpression(elemStor.Variable); // this one for storing
                            il.Emit(OpCodes.Dup);   // this one for loading

                            ITArrayType arr = elemStor.Variable.ExpressionType as ITArrayType;
                            LocalBuilder loc = il.DeclareLocal(compiler.GetCliType(arr.ElementType));

                            LocalBuilder[] indVars = new LocalBuilder[arr.Dimensions];
                            var indices = elemStor.Indices;
                            for (int i = 0; i < indVars.Length; i++)
                            {
                                var index = indices[i];
                                fCompiler.EmitExpression(index);
                                if (!(index is ITValueExpression))
                                {
                                    indVars[i] = il.DeclareLocal(compiler.GetCliType(index.ExpressionType));
                                    il.Emit(OpCodes.Dup);
                                    il.Emit(OpCodes.Stloc, indVars[i]);
                                }
                            }

                            if (arr.Dimensions == 1)
                            {

                                if (!discardValue)
                                {
                                    il.Emit(OpCodes.Dup);
                                    il.Emit(OpCodes.Stloc, loc);
                                }

                                EmitLoadArrayElement(elemStor.ExpressionType);
                            }
                            else
                            {

                                if (!discardValue)
                                {
                                    il.Emit(OpCodes.Dup);
                                    il.Emit(OpCodes.Stloc, loc);
                                }

                                Type[] prms = new Type[elemStor.Indices.Count];
                                for (int i = 0; i < prms.Length; i++) prms[i] = typeof(int);
                                MethodInfo meth = compiler.GetCliType(arr).GetMethod("Get", prms);
                                il.Emit(OpCodes.Call, meth);
                            }

                            // operate
                            ExpressionOutputInfo info = EmitBinaryOperator(binOpType, expr.Value.ExpressionType, expr.Value);
                            info = fCompiler.FilterValue(info, 0);

                            // store
                            il.Emit(OpCodes.Stloc, loc);
                            for (int i = 0; i < indVars.Length; i++)
                            {
                                if (indVars[i] == null)
                                {
                                    var index = indices[i];
                                    fCompiler.EmitExpression(index);
                                }
                                else
                                {
                                    il.Emit(OpCodes.Ldloc, indVars[i]);
                                }
                            }

                            il.Emit(OpCodes.Ldloc, loc);
                            if (arr.Dimensions == 1)
                            {
                                EmitStoreArrayElement(elemStor.ExpressionType);
                            }
                            else
                            {
                                Type[] prms = new Type[elemStor.Indices.Count + 1];
                                for (int i = 0; i < prms.Length; i++) prms[i] = typeof(int);
                                prms[prms.Length - 1] = compiler.GetCliType(elemStor.ExpressionType);
                                MethodInfo meth = compiler.GetCliType(arr).GetMethod("Set", prms);
                                il.Emit(OpCodes.Call, meth);
                            }

                            if (!discardValue)
                            {
                                il.Emit(OpCodes.Ldloc, loc);
                            }
                            return new ExpressionOutputInfo();
                        }

                        ITParameterStorage prmStor = val as ITParameterStorage;
                        if (prmStor != null)
                        {
                            ITFunctionParameter prm = prmStor.Variable;
                            int idx = (int)prm.UserData;
                            if (prm.IsByRef)
                            {
                                il.Emit(OpCodes.Ldarg, idx);
                                il.Emit(OpCodes.Dup);
                                ITPrimitiveType prim = prm.Type as ITPrimitiveType;
                                if (prim != null)
                                {
                                    switch (prim.Type)
                                    {
                                        case ITPrimitiveTypeType.Bool:
                                        case ITPrimitiveTypeType.Int8:
                                            il.Emit(OpCodes.Ldind_I1);
                                            break;
                                        case ITPrimitiveTypeType.Int16:
                                            il.Emit(OpCodes.Ldind_I2);
                                            break;
                                        case ITPrimitiveTypeType.Int32:
                                            il.Emit(OpCodes.Ldind_I4);
                                            break;
                                        case ITPrimitiveTypeType.Int64:
                                        case ITPrimitiveTypeType.Integer:
                                            il.Emit(OpCodes.Ldind_I8);
                                            break;
                                        case ITPrimitiveTypeType.UInt8:
                                            il.Emit(OpCodes.Ldind_U1);
                                            break;
                                        case ITPrimitiveTypeType.UInt16:
                                            il.Emit(OpCodes.Ldind_U2);
                                            break;
                                        case ITPrimitiveTypeType.UInt32:
                                            il.Emit(OpCodes.Ldind_U4);
                                            break;
                                        case ITPrimitiveTypeType.UInt64:
                                            il.Emit(OpCodes.Ldind_I8);
                                            break;
                                        case ITPrimitiveTypeType.Float:
                                            il.Emit(OpCodes.Ldind_R4);
                                            break;
                                        case ITPrimitiveTypeType.Double:
                                            il.Emit(OpCodes.Ldind_R8);
                                            break;
                                        case ITPrimitiveTypeType.Char:
                                            il.Emit(OpCodes.Ldind_U2);
                                            break;
                                        case ITPrimitiveTypeType.String:
                                            il.Emit(OpCodes.Ldind_Ref);
                                            break;
                                        default:
                                            throw new NotImplementedException();
                                    }
                                }
                                else
                                {
                                    il.Emit(OpCodes.Ldind_Ref);
                                }
                                ExpressionOutputInfo info = EmitBinaryOperator(binOpType, prm.Type, expr.Value);
                                info = fCompiler.FilterValue(info, 0);
                                LocalBuilder loc = null;
                                if (!discardValue)
                                {
                                    loc = il.DeclareLocal(compiler.GetCliType(expr.ExpressionType));
                                    il.Emit(OpCodes.Dup);
                                    il.Emit(OpCodes.Stloc, loc);
                                }
                                if (prim != null && prim.Type != ITPrimitiveTypeType.String)
                                {
                                    switch (prim.Type)
                                    {
                                        case ITPrimitiveTypeType.Bool:
                                            il.Emit(OpCodes.Stind_I1);
                                            break;
                                        case ITPrimitiveTypeType.Char:
                                            il.Emit(OpCodes.Stind_I2);
                                            break;
                                        case ITPrimitiveTypeType.Int8:
                                            il.Emit(OpCodes.Stind_I1);
                                            break;
                                        case ITPrimitiveTypeType.Int16:
                                            il.Emit(OpCodes.Stind_I2);
                                            break;
                                        case ITPrimitiveTypeType.Int32:
                                            il.Emit(OpCodes.Stind_I4);
                                            break;
                                        case ITPrimitiveTypeType.Int64:
                                        case ITPrimitiveTypeType.Integer:
                                            il.Emit(OpCodes.Stind_I8);
                                            break;
                                        case ITPrimitiveTypeType.UInt8:
                                            il.Emit(OpCodes.Stind_I1);
                                            break;
                                        case ITPrimitiveTypeType.UInt16:
                                            il.Emit(OpCodes.Stind_I2);
                                            break;
                                        case ITPrimitiveTypeType.UInt32:
                                            il.Emit(OpCodes.Stind_I4);
                                            break;
                                        case ITPrimitiveTypeType.UInt64:
                                            il.Emit(OpCodes.Stind_I8);
                                            break;
                                        default:
                                            throw new NotImplementedException();
                                    }
                                }
                                else
                                {
                                    il.Emit(OpCodes.Stind_Ref);
                                }

                                if (!discardValue)
                                {
                                    il.Emit(OpCodes.Ldloc, loc);
                                }
                            }
                            else
                            {
                                il.Emit(OpCodes.Ldarg, idx);
                                ExpressionOutputInfo info = EmitBinaryOperator(binOpType,prm.Type, expr.Value);
                                info = fCompiler.FilterValue(info, 0);
                                il.Emit(OpCodes.Starg, idx);
                                if (!discardValue)
                                    il.Emit(OpCodes.Ldarg, idx);
                            }


                            return new ExpressionOutputInfo();
                        }

                        ITMemberPropertyStorage memPropStor = val as ITMemberPropertyStorage;
                        if (memPropStor != null)
                        {
                            CliProperty prop = compiler.GetCliProperty(memPropStor.Instance.ExpressionType, memPropStor.Member, null);
                            fCompiler.EmitExpression(memPropStor.Instance); // for storing
                            il.Emit(OpCodes.Dup); // for loading

                            MethodInfo info = prop.getter;

                            LocalBuilder[] indVars = new LocalBuilder[memPropStor.Parameters.Count];
                            var indices = memPropStor.Parameters;
                            IEnumerator<ITFunctionParameter> funParams = memPropStor.Member.Parameters.GetEnumerator();
                            for (int i = 0; i < indVars.Length; i++)
                            {
                                var index = indices[i];
                                funParams.MoveNext();
                                if (funParams.Current.IsByRef)
                                {
                                    throw new InvalidOperationException("ByRef indexer argument");
                                }
                                fCompiler.EmitExpression(index);
                                if (!(index is ITValueExpression))
                                {
                                    indVars[i] = il.DeclareLocal(compiler.GetCliType(index.ExpressionType));
                                    il.Emit(OpCodes.Dup);
                                    il.Emit(OpCodes.Stloc, indVars[i]);
                                }
                            }

                            il.Emit(OpCodes.Callvirt, info);

                            // operate
                            ExpressionOutputInfo outInfo = EmitBinaryOperator(binOpType, memPropStor.Instance.ExpressionType, expr.Value);
                            outInfo = fCompiler.FilterValue(outInfo, 0);

                            // store
                            var loc = il.DeclareLocal(compiler.GetCliType(expr.ExpressionType));
                            il.Emit(OpCodes.Stloc, loc);
                            for (int i = 0; i < indVars.Length; i++)
                            {
                                if (indVars[i] == null)
                                {
                                    var index = indices[i];
                                    fCompiler.EmitExpression(index);
                                }
                                else
                                {
                                    il.Emit(OpCodes.Ldloc, indVars[i]);
                                }
                            }
                            info = prop.setter;

                            il.Emit(OpCodes.Callvirt, info);

                            if (!discardValue)
                            {
                                il.Emit(OpCodes.Ldloc, loc);
                            }
                            return new ExpressionOutputInfo();
                        }

                        ITMemberVariableStorage memVarStor = val as ITMemberVariableStorage;
                        if (memVarStor != null)
                        {
                            FieldInfo info = compiler.GetCliField(memVarStor.Instance.ExpressionType, memVarStor.Member);
                            fCompiler.EmitExpression(memVarStor.Instance); // for storing
                            il.Emit(OpCodes.Dup); // for loading

                            // load
                            il.Emit(OpCodes.Ldfld, info);

                            // operate
                            ExpressionOutputInfo outInfo = EmitBinaryOperator(binOpType, memVarStor.Member.Type, expr.Value);
                            outInfo = fCompiler.FilterValue(outInfo, 0);

                            LocalBuilder loc = null;
                            if (!discardValue)
                            {
                                loc = il.DeclareLocal(compiler.GetCliType(expr.ExpressionType));
                                il.Emit(OpCodes.Dup);
                                il.Emit(OpCodes.Stloc, loc);
                            }
                            il.Emit(OpCodes.Stfld, info);
                            if (!discardValue)
                            {
                                il.Emit(OpCodes.Ldloc, loc);
                            }
                            return new ExpressionOutputInfo();
                        }

                        throw new InvalidOperationException("Unassignable");
                    }
                    throw new NotImplementedException();
                }

                public ExpressionOutputInfo Visit(ITReferenceCalleeExpression expr)
                {
                    if (fCompiler.thisType == null)
                    {
                        throw new InvalidOperationException("no 'this'");
                    }
                    il.Emit(OpCodes.Ldarg_0);
                    return new ExpressionOutputInfo();
                }

                public ExpressionOutputInfo Visit(ITTypeCheckExpression expr)
                {
                    fCompiler.EmitExpression(expr.Object);
                    il.Emit(OpCodes.Isinst, compiler.GetCliType(expr.TargetType));

                    ExpressionOutputInfo info = new ExpressionOutputInfo();
                    if (expr.Type == ITTypeCheckExpressionType.IsNot)
                        info.logicNegated = true;
                    return info;
                }

                public ExpressionOutputInfo Visit(ITUnaryOperatorExpression expr)
                {
                    ITPrimitiveType primType = expr.Expression.ExpressionType as ITPrimitiveType;
                    if (primType != null)
                    {
                        ExpressionOutputInfo info;
                        switch (expr.Type)
                        {
                            case ITUnaryOperatorType.Negate:
                                info = fCompiler.EmitExpression(expr.Expression);
                                if (discardValue)
                                {
                                    il.Emit(OpCodes.Pop);
                                    return new ExpressionOutputInfo();
                                }
                                il.Emit(OpCodes.Neg);
                                switch (primType.Type)
                                {
                                    case ITPrimitiveTypeType.Int8:
                                    case ITPrimitiveTypeType.Bool:
                                        il.Emit(OpCodes.Conv_I1);
                                        break;
                                    case ITPrimitiveTypeType.UInt16:
                                    case ITPrimitiveTypeType.Char:
                                        il.Emit(OpCodes.Conv_U2);
                                        break;
                                    case ITPrimitiveTypeType.Int16:
                                        il.Emit(OpCodes.Conv_I2);
                                        break;
                                    case ITPrimitiveTypeType.Int32:
                                        il.Emit(OpCodes.Conv_I4);
                                        break;
                                    case ITPrimitiveTypeType.Int64:
                                    case ITPrimitiveTypeType.Integer:
                                        il.Emit(OpCodes.Conv_I8);
                                        break;
                                    case ITPrimitiveTypeType.UInt8:
                                        il.Emit(OpCodes.Conv_U1);
                                        break;
                                    case ITPrimitiveTypeType.UInt32:
                                        il.Emit(OpCodes.Conv_U4);
                                        break;
                                    case ITPrimitiveTypeType.UInt64:
                                        il.Emit(OpCodes.Conv_U8);
                                        break;
                                }
                                return new ExpressionOutputInfo();
                            case ITUnaryOperatorType.Not:
                                info = fCompiler.EmitExpression(expr.Expression);
                                if (discardValue)
                                {
                                    il.Emit(OpCodes.Pop);
                                    return new ExpressionOutputInfo();
                                }
                                if (info.logicNegated)
                                {
                                    Label endLabel = il.DefineLabel();
                                    Label falseLabel = il.DefineLabel();
                                    il.Emit(OpCodes.Brtrue, falseLabel);
                                    il.Emit(OpCodes.Ldc_I4_0);
                                    il.Emit(OpCodes.Br, endLabel);
                                    il.MarkLabel(falseLabel);
                                    il.Emit(OpCodes.Ldc_I4_1);
                                    il.MarkLabel(endLabel);
                                }
                                else
                                {
                                    Label endLabel = il.DefineLabel();
                                    Label falseLabel = il.DefineLabel();
                                    il.Emit(OpCodes.Brfalse, falseLabel);
                                    il.Emit(OpCodes.Ldc_I4_0);
                                    il.Emit(OpCodes.Br, endLabel);
                                    il.MarkLabel(falseLabel);
                                    il.Emit(OpCodes.Ldc_I4_1);
                                    il.MarkLabel(endLabel);
                                }
                                return new ExpressionOutputInfo();
                            default:
                                throw new InvalidOperationException("Invalid unary operator");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("Unary operator on non-primitive");
                    }
                }

                public ExpressionOutputInfo Visit(ITUnresolvedConstantExpression expr)
                {
                    throw new InvalidOperationException("Unresolved constant found");
                }

                public ExpressionOutputInfo Visit(ITValueExpression expr)
                {
                    ITPrimitiveType primType = expr.ExpressionType as ITPrimitiveType;
                    ITClassType enumType = expr.ExpressionType as ITClassType;
                    if (primType == null && enumType != null && enumType.UnderlyingEnumType != null)
                    {
                        primType = enumType.UnderlyingEnumType;
                    }
                    if (primType != null)
                    {
                        switch (primType.Type)
                        {
                            case ITPrimitiveTypeType.Bool:
                                if (Convert.ToBoolean(expr.Value))
                                {
                                    il.Emit(OpCodes.Ldc_I4_1);
                                }
                                else
                                {
                                    il.Emit(OpCodes.Ldc_I4_0);
                                }
                                break;
                            case ITPrimitiveTypeType.Char:
                                il.Emit(OpCodes.Ldc_I4, (uint)Convert.ToUInt16(expr.Value));
                                break;
                            case ITPrimitiveTypeType.Int8:
                                il.Emit(OpCodes.Ldc_I4, (int)(sbyte)Convert.ToSByte(expr.Value));
                                break;
                            case ITPrimitiveTypeType.Int16:
                                il.Emit(OpCodes.Ldc_I4, (int)(short)Convert.ToInt16(expr.Value));
                                break;
                            case ITPrimitiveTypeType.Int32:
                                il.Emit(OpCodes.Ldc_I4, Convert.ToInt32(expr.Value));
                                break;
                            case ITPrimitiveTypeType.Int64:
                            case ITPrimitiveTypeType.Integer:
                                il.Emit(OpCodes.Ldc_I8, Convert.ToInt64(expr.Value));
                                break;
                            case ITPrimitiveTypeType.UInt8:
                                il.Emit(OpCodes.Ldc_I4, (int)(byte)Convert.ToByte(expr.Value));
                                break;
                            case ITPrimitiveTypeType.UInt16:
                                il.Emit(OpCodes.Ldc_I4, (int)(ushort)Convert.ToUInt16(expr.Value));
                                break;
                            case ITPrimitiveTypeType.UInt32:
                                il.Emit(OpCodes.Ldc_I4, Convert.ToUInt32(expr.Value));
                                break;
                            case ITPrimitiveTypeType.UInt64:
                                il.Emit(OpCodes.Ldc_I8, Convert.ToUInt64(expr.Value));
                                break;
                            case ITPrimitiveTypeType.Float:
                                il.Emit(OpCodes.Ldc_R4, Convert.ToSingle(expr.Value));
                                break;
                            case ITPrimitiveTypeType.Double:
                                il.Emit(OpCodes.Ldc_R8, Convert.ToDouble(expr.Value));
                                break;
                            case ITPrimitiveTypeType.String:
                                System.Reflection.Emit.StringToken tok = compiler.module.GetStringConstant(Convert.ToString(expr.Value));
                                il.Emit(OpCodes.Ldstr, tok.Token);
                                break;
                            default:
                                throw new InvalidOperationException("unsupported primitive type encounted");
                        }
                        return new ExpressionOutputInfo();
                    }
                    else if (expr.ExpressionType is ITNullType)
                    {
                        il.Emit(OpCodes.Ldnull);
                        return new ExpressionOutputInfo();
                    }

                    
                    throw new InvalidOperationException("unsupported value type: " + expr.ExpressionType.ToString());
                }


            }

            // end class: FunctionCompiler 
        }
    }
}
