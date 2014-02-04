using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public class ITFunctionBody: ITLocalScope
    {
        public ITType ReturnType { get; set; }
        public IList<ITFunctionParameter> Parameters { get; set; }
        public ITBlock Block { get; set; }
        public string Name { get; set; }

        public ITGenericTypeParameter[] GenericParameters { get; set; }

        public CodeDom.CodeFunctionStatement Statement { get; set; }

        // type "this" in this function refers to, or null for non-member (global/static) function.
        public ITType InstanceType { get; set; }

        // block to initialize the surrogate class instance, which located in the very first of the root block,
        // if exists.
        internal ITBlock SurrogateInitializationBlock { get; set; }
        internal Dictionary<ITLocalVariable, ITMemberVariable> Captures = null;
        internal Dictionary<ITFunctionParameter, ITMemberVariable> ArgCaptures = null;

        // generic parameters that is used to instantiate surrogate classes.
        internal ITType[] SurrogateGenericParameters { get; set; }

        // mutator that mutates function's gen-params to surrogate class's ones
        internal IntermediateCompiler.GenericTypeMutator ReverseMutator { get; set; }

        internal ITType ReverseMutate(ITType type)
        {
            return ReverseMutator != null ? ReverseMutator.Mutate(type) : type;
        }

        public override void AddChildEntity(ITEntity ent)
        {
            Children.Add(ent.Name, ent);
        }
        public ITFunctionBody()
        {
            Parameters = new List<ITFunctionParameter>();
        }

        internal virtual bool HasCapturedAnything()
        {
            return Captures != null || ArgCaptures != null || SurrogateInitializationBlock != null ||
                surrogateInstanceVar != null;
        }

        private ITLocalVariableStorage surrogateInstanceVar = null;
        // instance for surrogate class instance; access from inside of the function.
        internal ITExpression GetSurrogateClassInstance()
        {
            if (surrogateInstanceVar != null)
                return surrogateInstanceVar;

            ITType cls = GetInstantiatedSurrogateClassType();
            ITLocalVariable var = new ITLocalVariable()
            {
                Type = cls,
                Name = "$surrogate-instance",
                Location = GetCodeRootBlock().Location
            };
            Block.LocalVariables.Add(var.Name, var);
            surrogateInstanceVar = new ITLocalVariableStorage() {
                 ExpressionType = var.Type, Location = var.Location, Variable = var
            };

            SurrogateInitializationBlock = new ITBlock()
            {
                ParentBlock = Block,
                ParentScope = Block
            };

            SurrogateInitializationBlock.Statements.Add(new ITExpressionStatement()
            {
                Expression = new ITAssignExpression()
                {
                    AssignType = ITAssignType.Assign,
                    ExpressionType = cls,
                    Location = var.Location,
                    Storage = surrogateInstanceVar,
                    Value = new ITClassConstructExpression()
                    {
                        ExpressionType = cls,
                        Location = var.Location,
                        Type = cls
                    }
                }
            });

            Block.Statements.Insert(0, new ITBlockStatement() { Block = SurrogateInitializationBlock });

            return surrogateInstanceVar;
        }

        private ITMemberVariable _capturedThisInstance = null;
        internal ITMemberVariable CaptureThisInstance()
        {
            if (_capturedThisInstance == null)
            {
                if (InstanceType == null)
                    throw new InvalidOperationException("attempted to capture 'this' reference of non-member function");

                var sur = GetSurrogateClassType();
                var mem = new ITMemberVariable();
                mem.Name = "$self$";
                mem.Type = InstanceType;
                mem.Location = Location;
                mem.Owner = sur;
                sur.AddMemberVariable(mem);

                // ensure surrogate instance initializer is inserted
                GetSurrogateClassInstance();

                // get instantiated member variable
                var surInst = GetInstantiatedSurrogateClassType();
                if(sur != surInst)
                    mem = surInst.GetMemberVariable("$self$");

                // initialize 'mem'
                SurrogateInitializationBlock.Statements.Add(new ITExpressionStatement()
                {
                    Expression = new ITAssignExpression()
                    {
                        AssignType = ITAssignType.Assign,
                        ExpressionType = mem.Type,
                        Storage = new ITMemberVariableStorage()
                        {
                            Instance = GetSurrogateClassInstance(),
                            ExpressionType = mem.Type,
                            Member = mem
                        },
                        Value = new ITReferenceCalleeExpression()
                        {
                            ExpressionType = mem.Type,
                            Location = Location
                        }
                    }
                });

                _capturedThisInstance = mem;
            }
            return _capturedThisInstance;
        }

        private static int suffixIndex = 0;

        internal ITMemberVariable CaptureParameter(ITFunctionParameter par)
        {
            if (par.IsByRef)
                throw new InvalidOperationException("attempted to capture reference parameter");

            ITMemberVariable memberVar;
            if (ArgCaptures != null)
            {
                if (ArgCaptures.TryGetValue(par, out memberVar))
                {
                    return memberVar;
                }
            }

            if (ArgCaptures == null)
                ArgCaptures = new Dictionary<ITFunctionParameter, ITMemberVariable>();

            var sur = GetSurrogateClassType();
            var mem = new ITMemberVariable();
            if (sur.GetMemberVariable(par.Name, false) != null)
            {
                mem.Name = par.Name + "$" + System.Threading.Interlocked.Increment(ref suffixIndex).ToString();
            }
            mem.Name = par.Name;
            mem.Type = ReverseMutate(par.Type);
            mem.Location = par.Location;
            mem.Owner = sur;
            sur.AddMemberVariable(mem);

            // ensure surrogate instance initializer is inserted
            GetSurrogateClassInstance();

            var unmutated = mem;
            ArgCaptures.Add(par, mem);

            // get instantiated member variable
            var surInst = GetInstantiatedSurrogateClassType();
            if (surInst != sur)
                mem = surInst.GetMemberVariable(mem.Name);

            // initialize 'mem'
            SurrogateInitializationBlock.Statements.Add(new ITExpressionStatement()
            {
                Expression = new ITAssignExpression()
                {
                    AssignType = ITAssignType.Assign,
                    ExpressionType = mem.Type,
                    Storage = new ITMemberVariableStorage()
                    {
                         Instance = GetSurrogateClassInstance(), ExpressionType = mem.Type,
                         Member = mem
                    }, 
                    Value = new ITParameterStorage() {
                         Variable = par, ExpressionType = par.Type
                    }
                }
            });

            return unmutated;
        }

        internal ITMemberVariable CaptureLocalVariable(ITLocalVariable var)
        {
            ITMemberVariable memberVar;
            if (Captures != null)
            {
                if (Captures.TryGetValue(var, out memberVar))
                {
                    return memberVar;
                }
            }

            if (Captures == null)
                Captures = new Dictionary<ITLocalVariable, ITMemberVariable>();

            var sur = GetSurrogateClassType();
            var mem = new ITMemberVariable();
            if (sur.GetMemberVariable(var.Name, false) != null)
            {
                mem.Name = var.Name + "$" + System.Threading.Interlocked.Increment(ref suffixIndex).ToString();
            }
            mem.Name = var.Name;
            mem.Type = ReverseMutate(var.Type);
            mem.Location = var.Location;
            mem.Owner = sur;
            sur.AddMemberVariable(mem);

            Captures.Add(var, mem);
            return mem;
        }

        public override int GetNumLocalGenericParameters()
        {
            return GenericParameters.Length;
        }

        internal virtual CodeDom.CodeBlock GetCodeRootBlock()
        {
            return Statement.Statements;
        }



        internal ITSurrogateClassType GetSurrogateClassType()
        {
            return (ITSurrogateClassType)Block.SurrogateClassEntity.Type;
        }

        internal ITType GetInstantiatedSurrogateClassType()
        {
            return Block.GetInstantiatedSurrogateClassType();
        }

        internal virtual ITExpression GetExternalValue(string name, CodeDom.CodeLocation loc, IntermediateCompiler iCompiler)
        {
            return null;
        }

        // substitutes captured local variables with the reference to the surrogate class member.
        private class SubstituteContext : IITStatementVisitor<int>, IITExpressionVisitor<ITExpression>
        {
            public ITExpression surrogate;
            public Dictionary<ITLocalVariable, ITMemberVariable> captures;
            public Dictionary<ITFunctionParameter, ITMemberVariable> argCaptures;
            public ITBlock initBlock;

            public void DoBlock(ITBlock block)
            {
                if (block == null)
                    return;

                // skip surrogate initialization block or parameter captures are not initialized
                if (block == initBlock)
                    return;

                foreach (var stat in block.Statements)
                {
                    stat.Accept<int>(this);
                }

                // remove its existence as a local variable
                if (captures != null)
                {
                    foreach (var var in captures.Keys)
                    {
                        ITLocalVariable var2;
                        if (block.LocalVariables.TryGetValue(var.Name, out var2))
                        {
                            if (var2 == var)
                            {
                                block.LocalVariables.Remove(var.Name);
                            }
                        }
                    }
                }
            }

            private ITExpression DoExpression(ITExpression expr)
            {
                if (expr == null)
                    return null;

                return expr.Accept<ITExpression>(this);
            }

            public int Visit(ITBlockStatement statement)
            {
                DoBlock(statement.Block);
                return 0;
            }

            public int Visit(ITExitBlockStatement statement)
            {
                return 0;
            }

            public int Visit(ITExpressionStatement statement)
            {
                statement.Expression = DoExpression(statement.Expression);
                return 0;
            }

            public int Visit(ITIfStatement statement)
            {
                statement.Condition = DoExpression(statement.Condition);
                DoBlock(statement.FalseBlock);
                DoBlock(statement.TrueBlock);
                return 0;
            }

            public int Visit(ITReturnStatement statement)
            {
                statement.ReturnedValue = DoExpression(statement.ReturnedValue);
                return 0;
            }

            public int Visit(ITTableSwitchStatement statement)
            {
                statement.Value = DoExpression(statement.Value);
                var bks = statement.Blocks;
                for (int i = 0; i < bks.Length; i++)
                    DoBlock(bks[i]);
                return 0;
            }

            public int Visit(ITTryStatement statement)
            {
                DoBlock(statement.ProtectedBlock);
                DoBlock(statement.FinallyBlock);
                foreach (var e in statement.Handlers)
                {
                    DoBlock(e.Block);

                    // info variable should remain local variable
                    ITMemberVariable var;
                    if (e.InfoVariable != null)
                    {
                        if (captures != null & captures.TryGetValue(e.InfoVariable, out var))
                        {
                            ITBlock inner = e.Block;
                            ITBlock wrapper = new ITBlock()
                            {
                                ParentScope = inner.ParentScope,
                                ParentBlock = inner.ParentBlock
                            };
                            wrapper.Statements.Add(new ITExpressionStatement()
                            {
                                Expression = new ITAssignExpression()
                                {
                                    Storage = new ITMemberVariableStorage()
                                    {
                                        Instance = surrogate,
                                        ExpressionType = var.Type,
                                        Location = e.InfoVariable.Location,
                                        Member = var
                                    },
                                    AssignType = ITAssignType.Assign,
                                    ExpressionType = var.Type,
                                    Value =
                                        new ITLocalVariableStorage()
                                        {
                                            ExpressionType = var.Type,
                                            Location = e.InfoVariable.Location,
                                            Variable = e.InfoVariable
                                        }
                                }
                            });
                            wrapper.Statements.Add(new ITBlockStatement()
                            {
                                Block = inner
                            });
                            e.Block = wrapper;
                            captures.Remove(e.InfoVariable);
                        }
                    }

                    ITNumericTryHandler numeric = e as ITNumericTryHandler;
                    if (numeric != null)
                    {
                        foreach (var range in numeric.Ranges)
                        {
                            range.LowerBound = DoExpression(range.LowerBound);
                            range.UpperBound = DoExpression(range.UpperBound);
                        }
                    }
                }
                return 0;
            }

            public int Visit(ITAssertStatement statement)
            {
                statement.Expression = DoExpression(statement.Expression);
                return 0;
            }

            public int Visit(ITThrowNumericStatement statement)
            {
                statement.Code = DoExpression(statement.Code);
                statement.Message = DoExpression(statement.Message);
                return 0;
            }

            public int Visit(ITThrowObjectStatement statement)
            {
                statement.Expression = DoExpression(statement.Expression);
                return 0;
            }

            public ITExpression Visit(ITArrayConstructExpression expr)
            {
                var lst = expr.NumElements;
                for (int i = 0, count = lst.Count; i < count; i++)
                {
                    lst[i] = DoExpression(lst[i]);
                }
                return expr;
            }

            public ITExpression Visit(ITArrayLiteralExpression expr)
            {
                var lst = expr.Elements;
                for (int i = 0, count = lst.Count; i < count; i++)
                    lst[i] = DoExpression(lst[i]);
                return expr;
            }

            public ITExpression Visit(ITBinaryOperatorExpression expr)
            {
                expr.Left = DoExpression(expr.Left);
                expr.Right = DoExpression(expr.Right);
                return expr;
            }

            public ITExpression Visit(ITCallMemberFunctionExpression expr)
            {
                expr.Function.Accept<ITExpression>(this);
                var lst = expr.Parameters;
                for (int i = 0, count = lst.Count; i < count; i++)
                    lst[i] = DoExpression(lst[i]);
                return expr;
            }

            public ITExpression Visit(ITCallGlobalFunctionExpression expr)
            {
                var lst = expr.Parameters;
                for (int i = 0, count = lst.Count; i < count; i++)
                    lst[i] = DoExpression(lst[i]);
                return expr;
            }

            public ITExpression Visit(ITCallFunctionReferenceExpression expr)
            {
                expr.Function = DoExpression(expr.Function);
                var lst = expr.Parameters;
                for (int i = 0, count = lst.Length; i < count; i++)
                    lst[i] = DoExpression(lst[i]);
                return expr;
            }

            public ITExpression Visit(ITCastExpression expr)
            {
                expr.Expression = DoExpression(expr.Expression);
                return expr;
            }

            public ITExpression Visit(ITClassConstructExpression expr)
            {
                return expr;
            }

            public ITExpression Visit(ITConditionalExpression expr)
            {
                expr.Conditional = DoExpression(expr.Conditional);
                expr.TrueValue = DoExpression(expr.TrueValue);
                expr.FalseValue = DoExpression(expr.FalseValue);
                return expr;
            }

            public ITExpression Visit(ITErrorExpression expr)
            {
                return expr;
            }

            public ITExpression Visit(ITMemberVariableStorage expr)
            {
                expr.Instance = DoExpression(expr.Instance);
                return expr;
            }

            public ITExpression Visit(ITMemberPropertyStorage expr)
            {
                expr.Instance = DoExpression(expr.Instance);
                return expr;
            }

            public ITExpression Visit(ITGlobalVariableStorage expr)
            {
                return null;
            }

            public ITExpression Visit(ITLocalVariableStorage exprold)
            {
                ITMemberVariable v;
                if (captures == null)
                    return exprold;
                if (captures.TryGetValue(exprold.Variable, out v))
                {
                    var expr = new ITMemberVariableStorage()
                    {
                        Instance = surrogate,
                        ExpressionType = v.Type,
                        Location = exprold.Location,
                        Member = v
                    };
                    return expr;
                }
                return exprold;
            }

            public ITExpression Visit(ITParameterStorage exprold)
            {
                ITMemberVariable v;
                if (argCaptures == null)
                    return exprold;
                if (argCaptures.TryGetValue(exprold.Variable, out v))
                {
                    var expr = new ITMemberVariableStorage()
                    {
                        Instance = surrogate,
                        ExpressionType = v.Type,
                        Location = exprold.Location,
                        Member = v
                    };
                    return expr;
                }
                return exprold;
            }

            public ITExpression Visit(ITArrayElementStorage expr)
            {
                var inds = expr.Indices;
                for (int i = 0, count = inds.Count; i < count; i++)
                    inds[i] = DoExpression(inds[i]);
                expr.Variable = DoExpression(expr.Variable);
                return expr;
            }

            public ITExpression Visit(ITGlobalFunctionStorage expr)
            {
                return expr;
            }

            public ITExpression Visit(ITMemberFunctionStorage expr)
            {
                expr.Object = DoExpression(expr.Object);
                return expr;
            }

            public ITExpression Visit(ITAssignExpression expr)
            {
                expr.Storage = (ITStorage)DoExpression(expr.Storage);
                expr.Value = DoExpression(expr.Value);
                return expr;
            }

            public ITExpression Visit(ITReferenceCalleeExpression expr)
            {
                return expr;
            }

            public ITExpression Visit(ITTypeCheckExpression expr)
            {
                expr.Object = DoExpression(expr.Object);
                return expr;
            }

            public ITExpression Visit(ITUnaryOperatorExpression expr)
            {
                expr.Expression = DoExpression(expr.Expression);
                return expr;
            }

            public ITExpression Visit(ITUnresolvedConstantExpression expr)
            {
                return expr;
            }

            public ITExpression Visit(ITValueExpression expr)
            {
                return expr;
            }
        }

        internal void DoCapturedVariableSubstitution()
        {
            if ((Captures == null || Captures.Count == 0) &&
                (ArgCaptures == null || ArgCaptures.Count == 0))
                return;

            var ctx = new SubstituteContext();
            ctx.surrogate = GetSurrogateClassInstance();

            // reconstruct dictionary with instantiating
            var surType = ctx.surrogate.ExpressionType;
            if (Captures != null)
            {
                var dic = new Dictionary<ITLocalVariable, ITMemberVariable>();
                foreach (var pair in Captures)
                {
                    dic.Add(pair.Key, surType.GetMemberVariable(pair.Value.Name));
                }
                ctx.captures = dic;
            }
            if (ArgCaptures != null)
            {
                var dic = new Dictionary<ITFunctionParameter, ITMemberVariable>();
                foreach (var pair in ArgCaptures)
                {
                    dic.Add(pair.Key, surType.GetMemberVariable(pair.Value.Name));
                }
                ctx.argCaptures = dic;

            }

            ctx.initBlock = SurrogateInitializationBlock;

            ctx.DoBlock(Block);

            Captures = null; 
        }
    }

    public class ITAnonymousFunctionBody : ITFunctionBody
    {
        public CodeDom.CodeAnonymousFunctionExpression AnonymousFunctionExpression { get; set; }

        // true when this function references outer local variables.
        public bool CapturedVariable { get; set; }

        // used when CapturedVariable == false
        public ITFunctionEntity functionEntity { get; set; }

        // used when CapturedVariable == true
        public ITMemberFunction memberFunction { get; set; }

        // function body that defined this function.
        public ITFunctionBody ParentFunctionBody { get; set; }

        public ITBlock DeclaringBlock { get; set; }

        internal override bool HasCapturedAnything()
        {
            return base.HasCapturedAnything() || HasParentFunctionBodySurrogateClassInstanceVariable();
        }

        private ITMemberVariable _parentfunctionBodySurrogateClassInstanceVariable;
        internal bool HasParentFunctionBodySurrogateClassInstanceVariable()
        {
            return _parentfunctionBodySurrogateClassInstanceVariable != null;
        }
        internal ITMemberVariable GetParentFunctionBodySurrogateClassInstanceVariable()
        {
            if (_parentfunctionBodySurrogateClassInstanceVariable == null)
            {
                SetCapturedVariable();

                ITExpression thisInst = GetSurrogateClassInstance();
                ITSurrogateClassType typ = GetSurrogateClassType();
                var mem = new ITMemberVariable()
                {
                    Name = "$parent", Owner = typ, Type = ParentFunctionBody.GetSurrogateClassInstance().ExpressionType
                };
                typ.AddMemberVariable(mem);

                // initialize this
                // [0] is initialization of 'GetSurrogateClassInstance'
                Block.Statements.Insert(1, new ITExpressionStatement()
                {
                    Expression = new ITAssignExpression()
                    {
                        AssignType = ITAssignType.Assign,
                        ExpressionType = mem.Type,
                        Storage = new ITMemberVariableStorage()
                        {
                            Instance = thisInst,
                            ExpressionType = mem.Type,
                            Member = mem
                        },
                        Value = new ITReferenceCalleeExpression()
                        {
                            ExpressionType = mem.Type
                        }
                    }
                });
                _parentfunctionBodySurrogateClassInstanceVariable = mem;

            }
            return _parentfunctionBodySurrogateClassInstanceVariable;
        }

        // mark as this function uses an outer local variable.
        internal void SetCapturedVariable()
        {
            if (CapturedVariable)
                return;

            ITClassType parent = (ITClassType)functionEntity.ParentScope;
            ((ITClassType)functionEntity.ParentScope).RemoveChildEntity(functionEntity);

            memberFunction = new ITMemberFunction();
            memberFunction.Owner = parent;
            memberFunction.Name = Name;
            memberFunction.Body = this;
            parent.AddMemberFunction(memberFunction);

            functionEntity = null;
            CapturedVariable = true;
        }

        internal override CodeDom.CodeBlock GetCodeRootBlock()
        {
            return AnonymousFunctionExpression.Statements;
        }

        // captures outer local variable; use only from statements of the function. 
        internal override ITExpression GetExternalValue(string name, CodeDom.CodeLocation loc, IntermediateCompiler iCompiler)
        {

            ITLocalVariable foundVar = null;
            ITFunctionParameter foundParam = null;
            ITFunctionBody foundVarFunc = null;

            ITMemberFunction foundMemFunc = null;
            ITMemberProperty foundMemProp = null;
            ITMemberVariable foundMemVar = null;
            bool foundMember = false;

            bool foundThis = false;

            ITFunctionBody func = this;
            while (func != null)
            {
                ITAnonymousFunctionBody anonFunc = func as ITAnonymousFunctionBody;
                if (anonFunc == null)
                    break;
                ITFunctionBody scopeFunc = anonFunc.ParentFunctionBody;
                ITBlock blk = anonFunc.DeclaringBlock;

                if (scopeFunc == null)
                    break;

                while (blk != null)
                {
                    // TODO: error for virtual local variable

                    if (blk.LocalVariables.TryGetValue(name, out foundVar))
                    {
                        if (foundVar.IsConst && foundVar.ConstantValue is ITValueExpression)
                        {
                            return foundVar.ConstantValue;
                        }
                        foundVarFunc = scopeFunc;
                        goto VariableFound;
                    }
                    else
                    {
                        foundVar = null;
                    }
                    // if this is a root block, ParentScope becomes ITFunctionBody,
                    // so (blk.ParentScope) as ITBlock becomes null, exiting the loop.
                    blk = (blk.ParentScope) as ITBlock;
                }

                // check param
                if (scopeFunc.Parameters != null)
                {
                    foreach (var prm in scopeFunc.Parameters)
                    {
                        if (prm.Name == name)
                        {
                            if (prm.IsByRef)
                            {
                                iCompiler.ReportError(Properties.Resources.ICCaptureByRefParameter, loc);
                                continue;
                            }
                            foundParam = prm;
                            foundVarFunc = scopeFunc;
                            goto VariableFound;
                        }
                    }
                }

                // check instance member
                ITType instType = scopeFunc.InstanceType;
                if (instType != null)
                {
                    if (name == "this")
                    {
                        foundThis = true;
                        foundVarFunc = scopeFunc;
                        goto VariableFound;
                    }
                    foundMemFunc = instType.GetMemberFunction(name);
                    if (foundMemFunc != null)
                    {
                        foundMember = true;
                        foundVarFunc = scopeFunc;
                        goto VariableFound;
                    }
                    foundMemProp = instType.GetMemberProperty(name);
                    if (foundMemProp != null)
                    {
                        foundMember = true;
                        foundVarFunc = scopeFunc;
                        goto VariableFound;
                    }
                    foundMemVar = instType.GetMemberVariable(name);
                    if (foundMemVar != null)
                    {
                        foundMember = true;
                        foundVarFunc = scopeFunc;
                        goto VariableFound;
                    }
                }

                func = scopeFunc;
            }

            // not found
            return base.GetExternalValue(name, loc, iCompiler);

        VariableFound:

            SetCapturedVariable();

            // repeat search to generate correct reference tree
            ITExpression expr = null;
            func = this;
            while (func != null)
            {
                // reached?
                if (func == foundVarFunc)
                {

                    ITMemberVariable var;
                    if (foundThis || foundMember)
                    {
                        var = func.CaptureThisInstance();
                    }
                    else if (foundParam != null)
                    {
                        var = func.CaptureParameter(foundParam);
                    }
                    else
                    {
                        var = func.CaptureLocalVariable(foundVar);
                    }
                    var stor = new ITMemberVariableStorage()
                    {
                        Instance = expr,
                        ExpressionType = var.Type,
                        Member = var
                    };
                    if (foundMember)
                    {
                        if (foundMemFunc != null)
                        {
                            return new ITMemberFunctionStorage()
                            {
                                ExpressionType = iCompiler.CreateFunctionTypeForMemberFunction(foundMemFunc, new ITType[] { }),
                                Function = foundMemFunc,
                                Object = stor,
                                GenericTypeParameters = new ITType[] { },
                                Location = loc
                            };
                        }
                        else if (foundMemProp != null)
                        {
                            if (foundMemProp.Parameters.Count > 0)
                                throw new NotSupportedException(); // FIXME: support this?
                            return new ITMemberPropertyStorage()
                            {
                                ExpressionType = foundMemProp.Type,
                                Instance = stor,
                                Location = loc,
                                Member = foundMemProp,
                                Parameters = new ITExpression[] { }
                            };
                        }
                        else if (foundMemVar != null)
                        {
                            return new ITMemberVariableStorage()
                            {
                                ExpressionType = foundMemVar.Type,
                                Instance = stor,
                                Member = foundMemVar,
                                Location = loc
                            };
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    }
                    else
                    {
                        return stor;
                    }
                }

                // if not yet reached...

                ITAnonymousFunctionBody anonFunc = func as ITAnonymousFunctionBody;
                if (anonFunc == null)
                    break;

                var par = anonFunc.ParentFunctionBody;
                if (expr == null)
                {
                    expr = new ITReferenceCalleeExpression()
                    {
                        ExpressionType = par.GetSurrogateClassType()
                    };
                }
                else
                {
                    var v = anonFunc.GetParentFunctionBodySurrogateClassInstanceVariable();
                    expr = new ITMemberVariableStorage()
                    {
                        Instance = expr,
                        ExpressionType = v.Type,
                        Member = v
                    };
                }
                func = par;
            }

            throw new InvalidOperationException();
        }
    }
}
