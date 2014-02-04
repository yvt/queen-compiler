using System;
using System.Collections.Generic;
using System.Text;
using Queen.Language.IntermediateTree;
using Queen.Language.CodeDom;

namespace Queen.Language
{
    public partial class IntermediateCompiler
    {

        private class StatementCompiler: ICodeStatementVisitor<ITStatement>
        {
            CompileFunctionContext context;
            ITBlock block;
            IntermediateCompiler compiler;

            public StatementCompiler(CompileFunctionContext context)
            {
                this.context = context;
                this.block = null;
                this.compiler = context.compiler;
            }



            public ITBlock CompileStatements(CodeBlock blk, ITScope parentScope, string blockName = null, ITBlock loopBlock = null,
                bool canBeTransferredOut = true, ITSurrogateClassEntity surrogateClassEntity = null)
            {
                if (block != null)
                {
                    throw new IntermediateCompilerException(Properties.Resources.InternalError);
                }
                block = new ITBlock();
                block.Location = blk.Location;
                block.Name = blockName;
                block.ParentBlock = parentScope as ITBlock;
                block.ParentScope = parentScope;
                block.CanControlBeTransferedOutOfBlock = canBeTransferredOut;
                if (parentScope == null)
                    throw new IntermediateCompilerException(Properties.Resources.InternalError);
                if (loopBlock != null)
                    loopBlock.LoopBodyBlock = block;

                var func = parentScope as ITFunctionBody;
                if (func != null)
                    func.Block = block;

                if (surrogateClassEntity == null)
                {
                    ITBlock surrogatableBlockParent = (ITBlock)parentScope;
                    while (surrogatableBlockParent.SurrogateClassEntity == null)
                    {
                        surrogatableBlockParent = surrogatableBlockParent.ParentBlock;
                    }
                    surrogateClassEntity = new ITSurrogateClassEntity(compiler);
                    surrogatableBlockParent.SurrogateClassEntity.Type.AddChildEntity(surrogateClassEntity);
                    surrogateClassEntity.ParentScope = surrogateClassEntity.Type.ParentScope =
                        surrogatableBlockParent.SurrogateClassEntity.Type;
                    surrogateClassEntity.Type.InheritGenericParameters(new string[] { });
                }
                block.SurrogateClassEntity = surrogateClassEntity;

                // add local entities
                {
                    EntityCompiler ecomp = new EntityCompiler(context.compiler, surrogateClassEntity.Type);
                    new LocalEntityFinder(ecomp).FindInBlock(blk);
                    ecomp.classFunctionAsMember = false;
                    ecomp.CompileBySingleCall();
                }

                ITBlock old = context.currentBlock;
                context.currentBlock = block;
                context.scope = block;
                foreach (CodeStatement stat in blk.Statements)
                {
                    ITStatement st = stat.Accept<ITStatement>(this);
                    if (st != null)
                    {
                        block.Statements.Add(st);
                    }
                }
                context.scope = block;
                context.currentBlock = old;

                return block;
            }

            private string IdentifierToNameOrNull(CodeIdentifier idt)
            {
                if (idt == null) return null;
                return idt.Text;
            }

            private ITBlock FindSurroundingBlock(string blockName, CodeLocation loc)
            {
                ITBlock blk = block;
                while (blk != null)
                {
                    string idt = blk.Name;
                    if (idt != null && idt == blockName)
                        return blk;
                    blk = blk.ParentBlock;
                }
                compiler.ReportError(string.Format(Properties.Resources.ICBlockNotFound, blockName), loc);
                return null;
            }

            private void CheckLeavingFinallyBlock(ITBlock target, CodeLocation loc)
            {
                ITBlock blk = block;
                while (blk != null && blk != target)
                {
                    if (!blk.CanControlBeTransferedOutOfBlock)
                    {
                        compiler.ReportError(string.Format(Properties.Resources.ICLeavingOutOfFinally), loc);
                        return;
                    }
                    blk = blk.ParentBlock;
                }
            }

            private ITBlock FindSurroundingBlock(CodeIdentifier blockName)
            {
                return FindSurroundingBlock(blockName.Text, blockName.Location);
            }

            public ITStatement Visit(CodeAssertStatement statement)
            {
                if (compiler.Options.IsReleaseBuild)
                    return null;

                ITAssertStatement st = new ITAssertStatement();
                st.Location = statement.Location;
                st.Expression = compiler.CompileExpression(statement.Condition, context);
                st.Expression = compiler.CastImplicitly(st.Expression, compiler.GetPrimitiveType(ITPrimitiveTypeType.Bool),
                    statement.Location);
                return st;
            }

            public ITStatement Visit(CodeBlockStatement statement)
            {
                ITBlockStatement st = new ITBlockStatement();
                st.Location = statement.Location;
                StatementCompiler comp = new StatementCompiler(context);
                st.Block = comp.CompileStatements(statement.Statements, block, IdentifierToNameOrNull(statement.Name));
                return st;
            }

            public ITStatement Visit(CodeBreakStatement statement)
            {
                ITBlock targetBlock = FindSurroundingBlock(statement.Name);
                ITExitBlockStatement stat = new ITExitBlockStatement();
                stat.ExitingBlock = targetBlock;
                stat.Location = statement.Location;
                CheckLeavingFinallyBlock(targetBlock, statement.Location);
                return stat;
            }

            public ITStatement Visit(CodeClassStatement statement)
            {
                return null;
            }

            public ITStatement Visit(CodeContinueStatement statement)
            {
                ITBlock targetBlock = FindSurroundingBlock(statement.Name);
                if (targetBlock != null)
                {
                    targetBlock = targetBlock.LoopBodyBlock;
                    if (targetBlock == null)
                    {
                        compiler.ReportError(string.Format(Properties.Resources.ICNonLoopingBlock, IdentifierToNameOrNull(statement.Name)),
                            statement.Name.Location);
                    }
                }
                ITExitBlockStatement stat = new ITExitBlockStatement();
                stat.ExitingBlock = targetBlock;
                stat.Location = statement.Location;
                CheckLeavingFinallyBlock(targetBlock, statement.Location);
                return stat;
            }

            public ITStatement Visit(CodeEnumStatement statement)
            {
                return null;
            }

            public ITStatement Visit(CodeExpressionStatement statement)
            {
                ITExpression expr = compiler.CompileExpression(statement.Expression, context);
                ITExpressionStatement stat = new ITExpressionStatement();
                stat.Expression = expr;
                stat.Location = statement.Location;
                return stat;
            }

            public ITStatement Visit(CodeFunctionStatement statement)
            {
                return null;
            }

            public ITStatement Visit(CodeReturnStatement statement)
            {
                ITFunctionBody body = context.FunctionBody;

                if (statement.ReturnedValue == null)
                {
                    ITReturnStatement stat = new ITReturnStatement();
                    stat.ReturnedValue = null;
                    stat.Location = statement.Location;

                    if (body.ReturnType != null)
                    {
                        compiler.ReportError(Properties.Resources.ICNoReturnValue, statement.Location);
                        stat.ReturnedValue = compiler.CreateErrorExpression();
                        stat.ReturnedValue.ExpressionType = body.ReturnType;
                    }

                    return stat;
                }
                else
                {
                    ITExpression expr = compiler.CompileExpression(statement.ReturnedValue, context);
                    ITReturnStatement stat = new ITReturnStatement();
                    if (body.ReturnType == null)
                    {
                        compiler.ReportError(Properties.Resources.ICReturningValue, statement.Location);
                    }
                    else
                    {
                        expr = compiler.CastImplicitly(expr, body.ReturnType, statement.Location);
                    }
                    stat.ReturnedValue = expr;
                    stat.Location = statement.Location;
                    return stat;
                }
            }

            private enum SwitchOptimizeMode
            {
                None,
                Integer,
                Real,
                UInteger64,
                String,
                Boolean
            }

            private static SwitchOptimizeMode GetSwitchOptimizeMode(ITType type)
            {
                var primType = type as ITPrimitiveType;
                if (primType == null)
                    return SwitchOptimizeMode.None;

                switch (primType.Type)
                {
                    case ITPrimitiveTypeType.Bool:
                        return SwitchOptimizeMode.Boolean;
                    case ITPrimitiveTypeType.Char:
                    case ITPrimitiveTypeType.Int8:
                    case ITPrimitiveTypeType.Int16:
                    case ITPrimitiveTypeType.Int32:
                    case ITPrimitiveTypeType.Int64:
                    case ITPrimitiveTypeType.Integer:
                    case ITPrimitiveTypeType.UInt8:
                    case ITPrimitiveTypeType.UInt16:
                    case ITPrimitiveTypeType.UInt32:
                        return SwitchOptimizeMode.Integer;
                    case ITPrimitiveTypeType.UInt64:
                        return SwitchOptimizeMode.UInteger64;
                    case ITPrimitiveTypeType.Float:
                    case ITPrimitiveTypeType.Double:
                        return SwitchOptimizeMode.Real;
                    case ITPrimitiveTypeType.String:
                        return SwitchOptimizeMode.String;
                }

                return SwitchOptimizeMode.None;
            }

            private struct IntermediateSwitchRange
            {
                public ITExpression Lower, Upper;
            }
            private struct IntermediateSwitchCase
            {
                public IntermediateSwitchRange[] Ranges;
                public ITBlock Block;
                public bool IsConstant;
                public CodeLocation Location;
            }

            private abstract class SwitchSorter
            {
                public abstract void Add(IntermediateSwitchCase cas);
                public abstract ITBlock Emit(ITBlock parentBlock, ITLocalVariable var);
            }

            private sealed class IntegerSwitchSorter
            {
                private struct Entry {
                    long minValue;
                    int blockIndex;
                    public Entry(long minValue, int block) {
                        this.minValue = minValue;
                        this.blockIndex = block;
                    }

                }
                private SortedDictionary<long, Entry> entries = new SortedDictionary<long, Entry>();
                private long maxValue;
                public IntegerSwitchSorter(long maxValue)
                {
                    this.maxValue = maxValue;
                }
                public void Add(long lower, long upper, int blockIndex)
                {
                    if (entries.Count == 0)
                    {
                        // first entry
                        entries.Add(lower, new Entry(lower, blockIndex));
                        if (upper < maxValue)
                        {
                            entries.Add(upper + 1, new Entry(upper + 1, -1));
                        }
                    }
                    else
                    {
                        // TODO: use external red-black tree library
                    }
                }
            }

            public ITStatement Visit(CodeSwitchStatement statement)
            {
                var switchBlock = new ITBlock();
                switchBlock.Name = statement.Name != null ? statement.Name.Text : null;
                switchBlock.ParentScope = switchBlock.ParentBlock = block;
                switchBlock.Location = statement.Location;

                var controlValueExpr = compiler.CompileExpression(statement.Value, context);
                
                var controlValue = new ITLocalVariable()
                {
                    Location = statement.Value.Location, Name = "$SwitchControlValue",
                    Type = controlValueExpr.ExpressionType
                };
                switchBlock.LocalVariables.Add("$SwitchControlValue", controlValue);

                var controlValueStorage = new ITLocalVariableStorage()
                {
                    Variable = controlValue,
                    Location = statement.Value.Location,
                    ExpressionType = controlValueExpr.ExpressionType
                };

                var controlValueStoreStatement = new ITAssignExpression()
                {
                    AssignType = ITAssignType.Assign,
                    ExpressionType = controlValueStorage.ExpressionType,
                    Location = statement.Value.Location,
                    Storage = controlValueStorage,
                    Value = controlValueExpr
                };
                switchBlock.Statements.Add(new ITExpressionStatement()
                {
                    Expression = controlValueStoreStatement,
                    Location = statement.Location
                });

                var controlValueType = controlValueExpr.ExpressionType;
                var optimizeMode = GetSwitchOptimizeMode(controlValueType);

                ITBlock prevBlock = switchBlock;
                var conditions = statement.Ranges;

                var cases = new IntermediateSwitchCase[conditions.Count];
                for (int i = 0; i < cases.Length; i++)
                {
                    var scase = new IntermediateSwitchCase();
                    bool isConstant = true;

                    var cond = conditions[i];
                    var domRanges = cond.Ranges;
                    var ranges = new IntermediateSwitchRange[domRanges.Count];
                    for (int j = 0; j < ranges.Length; j++)
                    {
                        var domRange = domRanges[j];
                        var range = new IntermediateSwitchRange();
                        range.Lower = compiler.CompileExpression(domRange.LowerBound, context);
                        if(!controlValueType.IsComparableTo(range.Lower.ExpressionType))
                            range.Lower = compiler.CastImplicitly(range.Lower, controlValueType, domRange.Location);
                        if (isConstant && !(range.Lower is ITValueExpression))
                        {
                            isConstant = false;
                        }
                        if (domRange.UpperBound != null)
                        {
                            range.Upper = compiler.CompileExpression(domRange.UpperBound, context);
                            if (!controlValueType.IsComparableTo(range.Upper.ExpressionType))
                                range.Upper = compiler.CastImplicitly(range.Upper, controlValueType, domRange.Location);
                            if (isConstant && !(range.Upper is ITValueExpression))
                            {
                                isConstant = false;
                            }
                        }
                        else
                        {
                            range.Upper = null;
                        }
                        ranges[j] = range;
                    }

                    scase.Ranges = ranges;
                    scase.IsConstant = isConstant;
                    scase.Location = cond.Location;
                    scase.Block = new StatementCompiler(context).CompileStatements(cond.Statements, block);
                    cases[i] = scase;
                }

                SwitchSorter sorter = null;

                for (int i = 0; i < cases.Length; i++)
                {
                    var cas = cases[i];
                    if (cas.IsConstant)
                    {
                        // TODO: create sorter to optimize switch statement
                    }
                    else if (sorter != null)
                    {
                        prevBlock = sorter.Emit(prevBlock, controlValue);
                        sorter = null;
                    }

                    if (sorter == null)
                    {
                        ITIfStatement ifStat = new ITIfStatement();
                        ifStat.Location = cas.Location;

                        ITExpression expr = null;
                        var ranges = cas.Ranges;
                        foreach (var range in ranges)
                        {
                            ITExpression rangeExpr;
                            if (range.Upper == null)
                            {
                                ITBinaryOperatorExpression eqExpr = new ITBinaryOperatorExpression()
                                {
                                    ExpressionType = compiler.builtinTypes["bool"],
                                    Left = controlValueStorage,
                                    Right = range.Lower,
                                    OperatorType = ITBinaryOperatorType.Equality,
                                    Location = range.Lower.Location
                                };
                                rangeExpr = eqExpr;
                            }
                            else
                            {
                                ITBinaryOperatorExpression geExpr = new ITBinaryOperatorExpression()
                                {
                                    ExpressionType = compiler.builtinTypes["bool"],
                                    Left = controlValueStorage,
                                    Right = range.Lower,
                                    OperatorType = ITBinaryOperatorType.GreaterThanOrEqual,
                                    Location = range.Lower.Location
                                };
                                ITBinaryOperatorExpression leExpr = new ITBinaryOperatorExpression()
                                {
                                    ExpressionType = compiler.builtinTypes["bool"],
                                    Left = controlValueStorage,
                                    Right = range.Upper,
                                    OperatorType = ITBinaryOperatorType.LessThanOrEqual,
                                    Location = range.Upper.Location
                                };
                                rangeExpr = new ITBinaryOperatorExpression()
                                {
                                    ExpressionType = compiler.builtinTypes["bool"],
                                    Left = geExpr,
                                    Right = leExpr,
                                    OperatorType = ITBinaryOperatorType.And,
                                    Location = range.Lower.Location
                                };
                            }

                            if (expr != null)
                            {
                                ITBinaryOperatorExpression binExpr = new ITBinaryOperatorExpression()
                                {
                                    ExpressionType = compiler.builtinTypes["bool"],
                                    Left = expr,
                                    Right = rangeExpr,
                                    OperatorType = ITBinaryOperatorType.Or,
                                    Location = ifStat.Location
                                };
                                expr = binExpr;
                            }
                            else
                            {
                                expr = rangeExpr;
                            }
                        }

                        // emit if statement
                        ifStat.Condition = expr;
                        ifStat.TrueBlock = cas.Block;

                        ifStat.FalseBlock = new ITBlock()
                        {
                            ParentBlock = prevBlock,
                            ParentScope = prevBlock
                        };

                        prevBlock.Statements.Add(ifStat);
                        prevBlock = ifStat.FalseBlock;
                    }
                    else
                    {
                        // rely on sorter
                        sorter.Add(cas);
                    }
                }

                if (sorter != null)
                {
                    prevBlock = sorter.Emit(prevBlock, controlValue);
                }

                if (statement.DefaultStatements != null)
                {
                    var blk = new StatementCompiler(context).CompileStatements(statement.DefaultStatements, block);
                    var st = new ITBlockStatement()
                    {
                        Block = blk,
                        Location = statement.DefaultStatements.Location
                    };
                    prevBlock.Statements.Add(st);
                }

                return new ITBlockStatement()
                {
                    Block = switchBlock,
                    Location = switchBlock.Location
                };
            }

            public ITStatement Visit(CodeTryStatement statement)
            {
                string statementName = statement.Name != null ? statement.Name.Text : null;

                ITTryStatement tryStatement = new ITTryStatement();
                tryStatement.Location = statement.Location;
                tryStatement.ProtectedBlock = new StatementCompiler(context).CompileStatements(statement.ProtectedStatements, block, statementName);

                if (statement.FinallyClause != null)
                {
                    tryStatement.FinallyBlock = new StatementCompiler(context).CompileStatements(statement.FinallyClause.Statements, block, statementName, null, false);
                }

                var domHandlers = statement.Handlers;
                var handlers = new ITTryHandler[domHandlers.Count];

                for (int i = 0; i < handlers.Length; i++)
                {
                    var domHandler = domHandlers[i];
                    var wrapBlock = new ITBlock();
                    wrapBlock.Name = statementName;
                    wrapBlock.ParentBlock = block;
                    wrapBlock.ParentScope = block;
                    wrapBlock.Location = domHandler.Location;

                    ITLocalVariable infoVar = null;
                    if (statementName != null)
                    {
                        infoVar = new ITLocalVariable();
                        infoVar.Location = domHandler.Location;
                        infoVar.Name = statementName;
                        wrapBlock.LocalVariables.Add(statementName, infoVar);
                    }

                    ITTryHandler handler = null;
                    var numericDomHandler = domHandler as CodeNumericCatchClause;
                    if (numericDomHandler != null)
                    {
                        var h = new ITNumericTryHandler();

                        var domRanges = numericDomHandler.Ranges;
                        var ranges = new ITNumericTryHandlerRange[domRanges.Count];

                        for (int j = 0; j < ranges.Length; j++)
                        {
                            var domRange = domRanges[j];
                            var range = new ITNumericTryHandlerRange();
                            range.LowerBound = compiler.CompileExpression(domRange.LowerBound, context);
                            range.LowerBound = compiler.CastImplicitly(range.LowerBound, compiler.builtinTypes["int"], domRange.Location);
                            if (domRange.UpperBound != null)
                            {
                                range.UpperBound = compiler.CompileExpression(domRange.UpperBound, context);
                                range.UpperBound = compiler.CastImplicitly(range.UpperBound, compiler.builtinTypes["int"], domRange.Location);
                            }
                            ranges[j] = range;
                        }

                        h.Ranges = ranges;
                        h.InfoVariable = infoVar;

                        handler = h;
                        if(infoVar != null)
                            infoVar.Type = compiler.GetNumericExceptionType();
                    }
                    else if (domHandler is CodeDefaultCatchClause)
                    {
                        var h = new ITNumericTryHandler();

                        var ranges = new ITNumericTryHandlerRange[0];

                        h.Ranges = ranges;
                        h.InfoVariable = infoVar;

                        handler = h;
                        if (infoVar != null)
                            infoVar.Type = compiler.GetNumericExceptionType();
                    }
                    else
                    {
                        var typedDomHandler = domHandler as CodeTypedCatchClause;
                        if (typedDomHandler != null)
                        {
                            var h = new ITTypedTryHandler();
                            h.InfoVariable = infoVar;
                            h.ExceptionType = compiler.ResolveType(typedDomHandler.Type, block);
                            if(infoVar != null)
                                infoVar.Type = h.ExceptionType;

                            handler = h;
                        }
                        else
                        {
                            throw new InvalidOperationException("Invalid catch clause type: " + domHandler.GetType().FullName);
                        }
                    }

                    var handlerBlock = new StatementCompiler(context).CompileStatements(domHandler.Handler, wrapBlock);
                    var blockStat = new ITBlockStatement()
                    {
                        Block = handlerBlock,
                        Location = handlerBlock.Location
                    };
                    wrapBlock.Statements.Add(blockStat);

                    handler.Block = wrapBlock;
                    handlers[i] = handler;
                }

                tryStatement.Handlers = handlers;
                
                return tryStatement;
            }

            public ITStatement Visit(CodeWhileStatement statement)
            {
                ITBlock par = block;
                ITExpression condExpr = statement.Condition != null ? compiler.CompileExpression(statement.Condition, context) : null;
                if (condExpr != null)
                {
                    condExpr = compiler.CastImplicitly(condExpr, compiler.GetPrimitiveType(ITPrimitiveTypeType.Bool), statement.Condition.Location);
                }

                // check for infinite loop
                ITValueExpression valExpr = condExpr as ITValueExpression;
                if (valExpr != null)
                {
                    if (Convert.ToBoolean(valExpr.Value))
                    {
                        condExpr = null;
                    }
                }

                if ((!statement.SkipFirstConditionEvaluation) && condExpr != null)
                {
                    par = new ITBlock();

                    ITIfStatement ifStat = new ITIfStatement();
                    ifStat.TrueBlock = par;
                    ifStat.Condition = condExpr;
                    ifStat.Location = condExpr.Location;
                    par.ParentBlock = block;
                    par.ParentScope = block;

                    block.Statements.Add(ifStat);
                }

                ITBlock loopBlock = new ITBlock();
                loopBlock.IsLoop = true;
                loopBlock.ParentBlock = par;
                loopBlock.ParentScope = par;
                loopBlock.Location = statement.Location;
                loopBlock.Name = IdentifierToNameOrNull(statement.Statements.Name);

                ITBlockStatement loopStat = new ITBlockStatement();
                loopStat.Block = loopBlock;
                loopStat.Location = statement.Location;
                par.Statements.Add(loopStat);

                StatementCompiler comp = new StatementCompiler(context);
                ITBlock bodyBlock = comp.CompileStatements(statement.Statements, loopBlock, null, loopBlock);

                ITBlockStatement bodyStat = new ITBlockStatement();
                bodyStat.Block = bodyBlock;
                bodyStat.Location = bodyBlock.Location;

                loopBlock.Statements.Add(bodyStat);

                // add break 
                if (condExpr != null)
                {
                    ITBlock breakBlock = new ITBlock();
                    breakBlock.ParentBlock = loopBlock;
                    breakBlock.ParentScope = loopBlock;
                    breakBlock.Location = statement.Location;

                    ITExitBlockStatement breakStat = new ITExitBlockStatement();
                    breakStat.ExitingBlock = loopBlock;
                    breakStat.Location = breakBlock.Location;
                    breakBlock.Statements.Add(breakStat);

                    {
                        ITIfStatement ifStat = new ITIfStatement();
                        ifStat.FalseBlock = breakBlock;
                        ifStat.Condition = condExpr;

                        loopBlock.Statements.Add(ifStat);
                    }
                }

                return null;
            }

            public ITStatement Visit(CodeIfDefStatement statement)
            {
                if (statement.Variable == null)
                {
                    return null;
                }
                string idt = statement.Variable.Text;
                bool defined = false;
                if (idt == "rls")
                {
                    if (compiler.Options.IsReleaseBuild)
                    {
                        defined = true;
                    }
                }
                else if (idt == "dbg")
                {
                    if (!compiler.Options.IsReleaseBuild)
                    {
                        defined = true;
                    }
                }
                if (defined)
                {
                    return new ITBlockStatement()
                    {
                        Block = new StatementCompiler(context).CompileStatements(statement.Statements, block, 
                        statement.Name != null ? statement.Name.Text : null), Location = statement.Location
                    };
                }
                else
                {
                    return null;
                }
            }

            public ITStatement Visit(CodeIfStatement statement)
            {
                ITIfStatement st = new ITIfStatement();
                st.Location = statement.Location;
                string name = IdentifierToNameOrNull(statement.Name);
                ITPrimitiveType boolType = compiler.GetPrimitiveType(ITPrimitiveTypeType.Bool);
                {
                    StatementCompiler comp = new StatementCompiler(context);
                    st.Condition = compiler.CompileExpression(statement.Conditions[0].Condition, context);
                    st.Condition = compiler.CastImplicitly(st.Condition, boolType, st.Condition.Location);
                    st.TrueBlock = comp.CompileStatements(statement.Conditions[0].Statements, block, name);
                }

                ITIfStatement parentStatement = st;
                ITBlock parentBlock = block;
                var conds = statement.Conditions;
                for (int i = 1, count = conds.Count; i < count; i++)
                {
                    var cond = conds[i];
                    ITBlock blk = new ITBlock();
                    blk.ParentBlock = parentBlock;
                    blk.ParentScope = parentBlock;
                    blk.Location = cond.Location;
                    parentStatement.FalseBlock = blk;

                    ITIfStatement stat = new ITIfStatement();
                    stat.Location = cond.Location;
                    stat.Condition = compiler.CompileExpression(cond.Condition, context);
                    stat.Condition = compiler.CastImplicitly(stat.Condition, boolType, stat.Condition.Location);
                    StatementCompiler comp = new StatementCompiler(context);
                    stat.TrueBlock = comp.CompileStatements(cond.Statements, blk, name);
                    blk.Statements.Add(stat);

                    parentBlock = blk;
                    parentStatement = stat;
                }

                if (statement.DefaultStatements != null)
                {
                    StatementCompiler comp = new StatementCompiler(context);
                    parentStatement.FalseBlock = comp.CompileStatements(statement.DefaultStatements, parentBlock, name);
                }

                return st;
            }

            public ITStatement Visit(CodeForStatement statement)
            {
                ITExpression initialValue = compiler.CompileExpression(statement.InitialValue, context);
                ITExpression limitValue = compiler.CompileExpression(statement.LimitValue, context);
                ITExpression stepValue = statement.Step != null ? compiler.CompileExpression(statement.Step, context) : null;

                ITPrimitiveType counterType = compiler.GetPrimitiveType(ITPrimitiveTypeType.Integer);
                ITPrimitiveType boolType = compiler.GetPrimitiveType(ITPrimitiveTypeType.Bool);

                
                if (stepValue == null)
                {
                    // FIXME: -1 for reversed range?
                    stepValue = new ITValueExpression()
                    {
                        ExpressionType = counterType,
                        Value = 1,
                        Location = statement.Location
                    };
                }

                initialValue = compiler.CastImplicitly(initialValue, counterType, initialValue.Location);
                limitValue = compiler.CastImplicitly(limitValue, counterType, initialValue.Location);
                stepValue = compiler.CastImplicitly(stepValue, counterType, initialValue.Location);

                // static check -- is this loop run at least once?
                // TODO: output warning for eliminated 'for' loop
                ITValueExpression initialValueStatic = initialValue as ITValueExpression;
                ITValueExpression limitValueStatic = limitValue as ITValueExpression;
                ITValueExpression stepValueStatic = stepValue as ITValueExpression;
                if (initialValueStatic != null &&
                    limitValueStatic != null &&
                    stepValueStatic != null)
                {
                    // static loop
                    long iVal = Convert.ToInt64(initialValueStatic.Value);
                    long limit = Convert.ToInt64(limitValueStatic.Value);
                    long stp = Convert.ToInt64(stepValueStatic.Value);
                    if (stp >= 0) // according to Visual Basic .net specification
                    {
                        if (iVal > limit)
                        {
                            return null;
                        }
                    }
                    else
                    {
                        if (iVal < limit)
                        {
                            return null;
                        }
                    }
                }

                ITBlock wrapper = new ITBlock();
                wrapper.ParentBlock = block;
                wrapper.ParentScope = block;
                wrapper.Location = statement.Location;

                ITBlockStatement wrapperStat = new ITBlockStatement();
                wrapperStat.Block = wrapper;
                wrapperStat.Location = statement.Location;

                ITLocalVariable counter = new ITLocalVariable();
                if (statement.Statements.Name != null)
                {
                    counter.Name = statement.Statements.Name.Text;
                }
                else
                {
                    counter.Name = "internal_for_counter";
                }
                counter.Type = counterType;
                counter.Location = statement.Location;
                wrapper.LocalVariables.Add(counter.Name, counter);

                // limit/step should be evaluated only once
                ITLocalVariable limitVar = null;
                if (limitValueStatic == null)
                {
                    limitVar = new ITLocalVariable();
                    limitVar.Name = "internal_for_limit";
                    limitVar.Type = counterType;
                    limitVar.Location = statement.Location;
                    wrapper.LocalVariables.Add(limitVar.Name, limitVar);
                }

                ITLocalVariable stepVar = null;
                if (stepValueStatic == null)
                {
                    stepVar = new ITLocalVariable();
                    stepVar.Name = "internal_for_step";
                    stepVar.Type = counterType;
                    stepVar.Location = statement.Location;
                    wrapper.LocalVariables.Add(stepVar.Name, stepVar);
                }

                // 'counter :: initialValue'
                ITLocalVariableStorage counterStorage = new ITLocalVariableStorage()
                {
                    Variable = counter,
                    Location = statement.Location,
                    ExpressionType = counter.Type
                };
                ITAssignExpression counterInitialValueAssignExpr = new ITAssignExpression()
                {
                    Storage = counterStorage,
                    AssignType = ITAssignType.Assign,
                    ExpressionType = counter.Type,
                    Location = statement.Location,
                    Value = initialValue
                };
                ITExpressionStatement counterInitialValueAssignStat = new ITExpressionStatement()
                {
                    Expression = counterInitialValueAssignExpr,
                    Location = statement.Location
                };
                wrapper.Statements.Add(counterInitialValueAssignStat);

                if (limitVar != null)
                {
                    ITLocalVariableStorage limitStorage = new ITLocalVariableStorage()
                    {
                        Variable = limitVar,
                        Location = statement.Location,
                        ExpressionType = limitVar.Type
                    };
                    ITAssignExpression limitAssignExpr = new ITAssignExpression()
                    {
                        Storage = limitStorage,
                        AssignType = ITAssignType.Assign,
                        ExpressionType = limitVar.Type,
                        Location = statement.Location,
                        Value = limitValue
                    };
                    ITExpressionStatement limitAssignStat = new ITExpressionStatement()
                    {
                        Expression = limitAssignExpr,
                        Location = statement.Location
                    };
                    wrapper.Statements.Add(limitAssignStat);

                    limitValue = limitStorage;
                }

                if (stepVar != null)
                {
                    ITLocalVariableStorage stepStorage = new ITLocalVariableStorage()
                    {
                        Variable = stepVar,
                        Location = statement.Location,
                        ExpressionType = stepVar.Type
                    };
                    ITAssignExpression stepAssignExpr = new ITAssignExpression()
                    {
                        Storage = stepStorage,
                        AssignType = ITAssignType.Assign,
                        ExpressionType = stepVar.Type,
                        Location = statement.Location,
                        Value = stepValue
                    };
                    ITExpressionStatement stepAssignStat = new ITExpressionStatement()
                    {
                        Expression = stepAssignExpr,
                        Location = statement.Location
                    };
                    wrapper.Statements.Add(stepAssignStat);

                    stepValue = stepStorage;
                }

                // build condition test expression
                ITExpression endConditionTestExpr = null;
                if (stepValueStatic != null)
                {
                    // static direction
                    long stp = Convert.ToInt64(stepValueStatic.Value);
                    if (stp >= 0)
                    {
                        endConditionTestExpr = new ITBinaryOperatorExpression()
                        {
                            OperatorType = ITBinaryOperatorType.LessThanOrEqual,
                            ExpressionType = boolType,
                            Location = statement.Location,
                            Left = counterStorage,
                            Right = limitValue
                        };
                    }
                    else
                    {
                        endConditionTestExpr = new ITBinaryOperatorExpression()
                        {
                            OperatorType = ITBinaryOperatorType.GreaterThanOrEqual,
                            ExpressionType = boolType,
                            Location = statement.Location,
                            Left = counterStorage,
                            Right = limitValue
                        };
                    }
                }
                else
                {
                    ITExpression compExpr = new ITBinaryOperatorExpression()
                    {
                        OperatorType = ITBinaryOperatorType.GreaterThanOrEqual,
                        ExpressionType = boolType,
                        Location = statement.Location,
                        Left = stepValue,
                        Right = new ITValueExpression()
                        {
                            Value = (int)0,
                            ExpressionType = counterType,
                            Location = statement.Location
                        }
                    };
                    ITExpression riseExpr = new ITBinaryOperatorExpression()
                    {
                        OperatorType = ITBinaryOperatorType.LessThanOrEqual,
                        ExpressionType = boolType,
                        Location = statement.Location,
                        Left = counterStorage,
                        Right = limitValue
                    };
                    ITExpression fallExpr = new ITBinaryOperatorExpression()
                    {
                        OperatorType = ITBinaryOperatorType.GreaterThanOrEqual,
                        ExpressionType = boolType,
                        Location = statement.Location,
                        Left = counterStorage,
                        Right = limitValue
                    };
                    endConditionTestExpr = new ITConditionalExpression()
                    {
                        Conditional = compExpr,
                        FalseValue = fallExpr,
                        TrueValue = riseExpr,
                        ExpressionType = counterType,
                        Location = statement.Location
                    };
                }

                ITBlock loopBlock = new ITBlock();
                loopBlock.ParentScope = wrapper;
                loopBlock.ParentBlock = wrapper;
                loopBlock.IsLoop = true;
                if (statement.Name != null)
                {
                    loopBlock.Name = statement.Name.Text;
                }

                ITIfStatement initialIfStat = new ITIfStatement()
                {
                    Condition = endConditionTestExpr,
                    TrueBlock = loopBlock
                };
                wrapper.Statements.Add(initialIfStat);

                StatementCompiler comp = new StatementCompiler(context);
                ITBlock bodyBlock = comp.CompileStatements(statement.Statements, loopBlock, null, loopBlock);

                ITBlockStatement bodyStat = new ITBlockStatement();
                bodyStat.Block = bodyBlock;
                bodyStat.Location = bodyBlock.Location;

                loopBlock.Statements.Add(bodyStat);

                // increment statement
                {
                    var countAssignExpr = new ITAssignExpression()
                    {
                        AssignType = ITAssignType.AdditionAssign,
                        Storage = counterStorage,
                        Location = statement.Location,
                        ExpressionType = counterStorage.ExpressionType,
                        Value = stepValue
                    };
                    var assignStat = new ITExpressionStatement()
                    {
                        Expression = countAssignExpr,
                        Location = statement.Location
                    };
                    loopBlock.Statements.Add(assignStat);
                }

                // add break 
                ITBlock breakBlock = new ITBlock();
                breakBlock.ParentBlock = loopBlock;
                breakBlock.ParentScope = loopBlock;
                breakBlock.Location = statement.Location;

                ITExitBlockStatement breakStat = new ITExitBlockStatement();
                breakStat.ExitingBlock = loopBlock;
                breakStat.Location = breakBlock.Location;
                breakBlock.Statements.Add(breakStat);

                {
                    ITIfStatement ifStat = new ITIfStatement()
                    {
                        FalseBlock = breakBlock,
                        Condition = endConditionTestExpr
                    };
                    loopBlock.Statements.Add(ifStat);
                }

                return wrapperStat;
            }

            private ITStatement EmitArrayForeach(CodeForEachStatement statement,
                ITExpression arrayExpr, ITArrayType arrayType)
            {

                ITType elementType = arrayType.ElementType;
                int numDims = arrayType.Dimensions;

                var wrapperBlock = new ITBlock()
                {
                    ParentScope = block,
                    ParentBlock = block,
                    Location = statement.Location
                };

                var iterVariable = new ITLocalVariable()
                {
                    Location = statement.Location,
                    Type = arrayType,
                    Name = "$iterator"
                };
                wrapperBlock.LocalVariables.Add("$iterator", iterVariable);

                wrapperBlock.Statements.Add(new ITExpressionStatement()
                {
                    Expression = new ITAssignExpression()
                    {
                        Storage = new ITLocalVariableStorage()
                        {
                            ExpressionType = arrayType,
                            Location = statement.Location,
                            Variable = iterVariable
                        },
                        AssignType = ITAssignType.Assign,
                        ExpressionType = arrayType,
                        Location = statement.Location,
                        Value = arrayExpr
                    }
                    ,
                    Location = statement.Location
                });

                // define index variable / array element count variable
                var indexVars = new ITLocalVariable[numDims];
                var indexVarStors = new ITLocalVariableStorage[numDims];
                var numElemVars = new ITLocalVariable[numDims];
                var numElemVarStors = new ITLocalVariableStorage[numDims];
                var indexType = compiler.GetIndexerType();

                for (int i = 0; i < numDims; i++)
                {
                    indexVars[i] = new ITLocalVariable()
                    {
                        Location = statement.Location,
                        Type = indexType,
                        Name = "$index-" + i.ToString()
                    };
                    indexVarStors[i] = new ITLocalVariableStorage()
                    {
                        Location = statement.Location,
                        Variable = indexVars[i],
                        ExpressionType = indexType
                    };
                    wrapperBlock.LocalVariables.Add(indexVars[i].Name, indexVars[i]);
                }

                for (int i = 0; i < numDims; i++)
                {
                    numElemVars[i] = new ITLocalVariable()
                    {
                        Location = statement.Location,
                        Type = indexType,
                        Name = "$numElements-" + i.ToString()
                    };
                    numElemVarStors[i] = new ITLocalVariableStorage()
                    {
                        Location = statement.Location,
                        Variable = numElemVars[i],
                        ExpressionType = indexType
                    };
                    wrapperBlock.LocalVariables.Add(numElemVars[i].Name, numElemVars[i]);

                    ITMemberFunction getter;
                    if (numDims > 1)
                    {
                        getter = arrayType.GetMemberFunction("LenMulti");
                    }
                    else
                    {
                        getter = arrayType.GetMemberFunction("Len");
                    }
                    var call = new ITCallMemberFunctionExpression()
                    {
                        ExpressionType = getter.GetReturnType(),
                        Function = new ITMemberFunctionStorage()
                        {
                            Function = getter,
                            Object = new ITLocalVariableStorage()
                            {
                                ExpressionType = arrayType,
                                Location = statement.Location,
                                Variable = iterVariable
                            }
                        }
                    };
                    if (numDims > 1)
                    {
                        call.Parameters.Add(compiler.CastForced(new ITValueExpression()
                        {
                            Value = i,
                            ExpressionType = compiler.GetBuiltinType("int32")
                        }, getter.GetParameterTypes()[0], statement.Location));
                    }
                    var lenVal = compiler.CastForced(call, indexType, statement.Location);
                    var asgn = new ITAssignExpression()
                    {
                        AssignType = ITAssignType.Assign,
                        Storage = numElemVarStors[i],
                        ExpressionType = indexType,
                        Value = lenVal
                    };
                    wrapperBlock.Statements.Add(new ITExpressionStatement() { Expression = asgn });
                }

                var currentExpr = new ITArrayElementStorage()
                {
                    ExpressionType = elementType,
                    Variable = new ITLocalVariableStorage()
                    {
                        ExpressionType = arrayType,
                        Location = statement.Location,
                        Variable = iterVariable
                    },
                    Location = statement.Location
                };
                foreach (var stor in indexVarStors)
                {
                    currentExpr.Indices.Add(stor);
                }

                string blockName = statement.Name != null ? statement.Name.Text : null;
                if (blockName != null)
                    wrapperBlock.VirtualLocalVariables.Add(blockName, currentExpr);
                
                ITBlock outerBlock = wrapperBlock;

                // emit dimension by dimension
                var blocks = new ITBlock[numDims];
                for (int i = 0; i < numDims; i++)
                {
                    var prevBlock = outerBlock;
                    outerBlock = new ITBlock()
                    {
                        IsLoop = true,
                        ParentBlock = prevBlock,
                        ParentScope = prevBlock,
                        Location = statement.Location
                    };
                    blocks[i] = outerBlock;
                    prevBlock.Statements.Add(new ITExpressionStatement()
                    {
                        Expression = new ITAssignExpression()
                        {
                            AssignType = ITAssignType.Assign,
                            Storage = indexVarStors[i],
                            Value = new ITValueExpression()
                            {
                                ExpressionType = indexType,
                                Value = 0
                            },
                            ExpressionType = indexType
                        }
                    });
                    prevBlock.Statements.Add(new ITBlockStatement() { Block = outerBlock, Location = statement.Location });

                    // emit exit condition check
                    var breakStat = new ITExitBlockStatement()
                    {
                        ExitingBlock = outerBlock
                    };
                    var ifBlock = new ITBlock();
                    ifBlock.Statements.Add(breakStat);
                    var compareExpr = new ITBinaryOperatorExpression()
                    {
                        ExpressionType = compiler.GetBuiltinType("bool"),
                        Left = indexVarStors[i],
                        Location = statement.Location,
                        OperatorType = ITBinaryOperatorType.LessThan,
                        Right = numElemVarStors[i]
                    };
                    var ifStat = new ITIfStatement()
                    {
                        Condition = compareExpr,
                        FalseBlock = ifBlock
                    };
                    outerBlock.Statements.Add(ifStat);

                    if (i == numDims - 1)
                    {
                        // emit body
                        blocks[0].Name = blockName;
                        var bodyBlock = new StatementCompiler(context).CompileStatements(statement.Statements, outerBlock, null, blocks[0]);
                        outerBlock.Statements.Add(new ITBlockStatement() { Block = bodyBlock, Location = statement.Location });
                    }

                    
                }

                for (int i = numDims - 1; i >= 0; i--)
                {
                    // emit increment
                    {
                        var incrExpr = new ITAssignExpression()
                        {
                            ExpressionType = indexType,
                            AssignType = ITAssignType.AdditionAssign,
                            Location = statement.Location,
                            Storage = indexVarStors[i],
                            Value = new ITValueExpression()
                            {
                                Value = 1,
                                ExpressionType = indexType,
                                Location = statement.Location
                            }
                        };
                        blocks[i].Statements.Add(new ITExpressionStatement()
                        {
                            Expression = incrExpr,
                            Location = statement.Location
                        });
                    }
                }


                return new ITBlockStatement() { Block = wrapperBlock, Location = statement.Location };
            }

            public ITStatement Visit(CodeForEachStatement statement)
            {
                ITExpression iteratedExpr = compiler.CompileExpression(statement.Enumerable, context);
                ITType typ = iteratedExpr.ExpressionType;
                if (typ == null)
                {
                    return null;
                }

                ITArrayType typArray = typ as ITArrayType;
                if (typArray != null)
                {
                    // special routine!
                    return EmitArrayForeach(statement, iteratedExpr, typArray);
                }

                ITMemberFunction getIterator = typ.GetMemberFunction("GetIter");
                if (getIterator == null)
                {
                    compiler.ReportError(string.Format(Properties.Resources.ICNotIterable, typ.ToString()), statement.Location);
                    return null;
                }

                // check element type
                ITType elementType = null;
                ITType iteratorType = null;
                {
                    ITType iterType = compiler.GetIteratorType();
                    if (iterType == null) // not implemented?
                    {
                        compiler.ReportError(string.Format(Properties.Resources.ICNotIterable, typ.ToString()), statement.Location);
                        return null;
                    }
                    for (ITType t = getIterator.GetReturnType(); t != null; t = t.Superclass)
                    {
                        {
                            var intfClass = t as ITInstantiatedGenericType;
                            if (intfClass != null && intfClass.GenericTypeDefinition.Equals(iterType))
                            {
                                iteratorType = intfClass;
                                elementType = intfClass.GetGenericParameters()[0];
                                break;
                            }
                        }
                        foreach (var intf in t.Interfaces)
                        {
                            var intfClass = intf as ITInstantiatedGenericType;
                            if (intfClass != null && intfClass.GenericTypeDefinition.Equals(iterType))
                            {
                                iteratorType = intfClass;
                                elementType = intfClass.GetGenericParameters()[0];
                                break;
                            }
                        }
                        if (elementType != null)
                            break;
                    }
                }

                if (elementType == null)
                {
                    compiler.ReportError(string.Format(Properties.Resources.ICNotIterable, typ.ToString()), statement.Location);
                    return null;
                }


                var wrapperBlock = new ITBlock()
                {
                    ParentScope = block,
                    ParentBlock = block,
                    Location = statement.Location
                };

                var iterVariable = new ITLocalVariable()
                {
                    Location = statement.Location,
                    Type = iteratorType,
                    Name = "$iterator"
                };
                wrapperBlock.LocalVariables.Add("$iterator", iterVariable);

                wrapperBlock.Statements.Add(new ITExpressionStatement()
                {
                    Expression = new ITAssignExpression()
                     {
                         Storage = new ITLocalVariableStorage()
                         {
                             ExpressionType = iteratorType,
                             Location = statement.Location,
                             Variable = iterVariable
                         },
                         AssignType = ITAssignType.Assign,
                         ExpressionType = iteratorType,
                         Location = statement.Location,
                         Value = compiler.CastForced(new ITCallMemberFunctionExpression()
                         {
                             ExpressionType = getIterator.GetReturnType(),
                             Function = new ITMemberFunctionStorage()
                             {
                                 Object = iteratedExpr,
                                 Function = getIterator,
                                 Location = statement.Location
                             }
                         }, iteratorType, statement.Location)
                     }
                    ,
                    Location = statement.Location
                });

                var currentExpr = new ITMemberPropertyStorage()
                {
                    ExpressionType = elementType,
                    Instance = new ITLocalVariableStorage()
                    {
                        ExpressionType = iteratorType,
                        Location = statement.Location,
                        Variable = iterVariable
                    },
                    Member = iteratorType.GetMemberProperty("Current"),
                    Location = statement.Location
                };

                string blockName = statement.Name != null ? statement.Name.Text : null;
                if (blockName != null)
                    wrapperBlock.VirtualLocalVariables.Add(blockName, currentExpr);

                var outerBlock = new ITBlock()
                {
                    Name = blockName,
                    IsLoop = true,
                    ParentBlock = wrapperBlock,
                    ParentScope = wrapperBlock,
                    Location = statement.Location
                };
                wrapperBlock.Statements.Add(new ITBlockStatement() { Block = outerBlock, Location = statement.Location });

                // emit "MoveNext"
                {
                    var breakStat = new ITExitBlockStatement()
                    {
                        ExitingBlock = outerBlock
                    };
                    var ifBlock = new ITBlock();
                    ifBlock.Statements.Add(breakStat);
                    var moveNextExpr = new ITCallMemberFunctionExpression()
                    {
                        ExpressionType = compiler.GetBuiltinType("bool"),
                        Function = new ITMemberFunctionStorage()
                        {
                            Function = iteratorType.GetMemberFunction("MoveNext"),
                            Object = currentExpr.Instance
                        }
                    };
                    var ifStat = new ITIfStatement()
                    {
                        Condition = moveNextExpr,
                        FalseBlock = ifBlock
                    };
                    outerBlock.Statements.Add(ifStat);
                }

                // emit body
                var bodyBlock = new StatementCompiler(context).CompileStatements(statement.Statements, outerBlock, null, outerBlock);
                outerBlock.Statements.Add(new ITBlockStatement() { Block = bodyBlock, Location = statement.Location });

                return new ITBlockStatement() { Block = wrapperBlock, Location = statement.Location };
            }

            public ITStatement Visit(CodeThrowStatement statement)
            {
                ITExpression firstParam = compiler.CompileExpression(statement.FirstParameter, context);
                if (firstParam.ExpressionType is ITPrimitiveType)
                {
                    // compatible form
                    firstParam = compiler.CastImplicitly(firstParam, compiler.GetBuiltinType("int"), statement.Location);
                    ITExpression second = null;
                    if (statement.SecondParameter != null)
                    {
                        second = compiler.CompileExpression(statement.SecondParameter, context);
                        second = compiler.CastImplicitly(second, compiler.GetBuiltinType("string"), statement.Location);
                    }

                    return new ITThrowNumericStatement()
                    {
                        Code = firstParam,
                        Message = second,
                        Location = statement.Location
                    };
                }else{
                    // objective form
                    if (statement.SecondParameter != null)
                    {
                        compiler.ReportError(Properties.Resources.ICInvalidThrowUsage, statement.Location);
                    }

                    if (!firstParam.ExpressionType.InheritsFrom(compiler.GetExceptionType()))
                    {
                        compiler.ReportError(string.Format(Properties.Resources.ICUnthrowable, firstParam.ExpressionType.ToString()),
                            statement.Location);
                    }

                    return new ITThrowObjectStatement()
                    {
                        Expression = firstParam, Location = statement.Location
                    };
                }
            }

            public ITStatement Visit(CodeVariableDeclarationStatement statement)
            {
                ITLocalVariable lvar = new ITLocalVariable()
                {
                    Name = IdentifierToNameOrNull(statement.Identifier),
                    IsConst = statement.IsConst,
                    Location = statement.Location,
                    Type = compiler.ResolveType(statement.Type, context.scope)
                };

                // check for no type & no initial value pattern
                if (lvar.Type == null && statement.InitialValue == null)
                {
                    compiler.ReportError(Properties.Resources.ICNoninitializedImplicitlyTypedVariable, statement.Location);
                    lvar.Type = compiler.GetBuiltinType("int");
                }

                ITExpression expr;
                if (statement.InitialValue != null)
                {
                    expr = compiler.CompileExpression(statement.InitialValue, context);
                    if (lvar.Type == null && expr != null)
                    {
                        lvar.Type = expr.ExpressionType;
                        if (lvar.Type is ITNullType)
                        {
                            compiler.ReportError(Properties.Resources.ICNullImplicitlyVariable, statement.Location);
                            lvar.Type = compiler.GetRootClass();
                        }
                    }
                    expr = compiler.CastImplicitly(expr, lvar.Type, statement.Location);
                    if (lvar.IsConst)
                    {
                        if (expr is ITValueExpression)
                        {
                            lvar.ConstantValue = expr;
                        }
                        else
                        {
                            lvar.ConstantValue = compiler.CreateErrorExpression();
                            lvar.ConstantValue.ExpressionType = expr.ExpressionType;
                            compiler.ReportError(string.Format(Properties.Resources.ICNonConstantValue, lvar.Name),
                                lvar.Location); // FIXME: correct message parameter?
                        }
                        expr = null;
                    }
                }
                else
                {
                    lvar.ShouldBeInitializedToDefaultValue = true;
                    expr = null;
                    if (lvar.IsConst)
                    {
                        throw new IntermediateCompilerException(Properties.Resources.InternalError);
                    }
                }
                if (lvar.Type == null)
                {
                    lvar.Type = compiler.GetPrimitiveType(ITPrimitiveTypeType.Int32);
                }

                if (block.LocalVariables.ContainsKey(lvar.Name))
                {
                    compiler.ReportError(string.Format(Properties.Resources.ICLocalVariableNameConfliction, lvar.Name),
                        lvar.Location);
                    return null;
                }
                else
                {
                    block.LocalVariables.Add(lvar.Name, lvar);

                    // initializer
                    if (expr != null)
                    {
                        ITLocalVariableStorage stor = new ITLocalVariableStorage()
                        {
                            Variable = lvar,
                            ExpressionType = lvar.Type,
                            Location = lvar.Location
                        };
                        ITAssignExpression assign = new ITAssignExpression()
                        {
                            AssignType = ITAssignType.Assign, ExpressionType = lvar.Type,
                            Value = expr, Location = statement.Location, Storage = stor
                        };
                        ITExpressionStatement exprStat = new ITExpressionStatement()
                        {
                            Expression = assign, Location = statement.Location
                        };
                        return exprStat;
                    }
                }
                return null;
            }


        }

        private class LocalEntityFinder: ICodeStatementVisitor<object>
        {
            private EntityCompiler entCompiler;

            public LocalEntityFinder(EntityCompiler entCompiler)
            {
                this.entCompiler = entCompiler;
            }

            public void FindInBlock(CodeBlock block)
            {
                foreach (CodeStatement stat in block.Statements)
                {
                    stat.Accept<object>(this);
                }
                foreach (CodeStatement stat in block.Children.Values)
                {
                    stat.Accept<object>(this);
                }
            }

            public object Visit(CodeAssertStatement statement)
            {
                return null;
            }

            public object Visit(CodeBlockControlStatement statement)
            {
                return null;
            }

            public object Visit(CodeBlockStatement statement)
            {
                return null;
            }

            public object Visit(CodeBreakStatement statement)
            {
                return null;
            }

            public object Visit(CodeClassStatement statement)
            {
                entCompiler.AddChildStatement(statement);
                return null;
            }

            public object Visit(CodeContinueStatement statement)
            {
                return null;
            }

            public object Visit(CodeEnumStatement statement)
            {
                entCompiler.AddChildStatement(statement);
                return null;
            }

            public object Visit(CodeExpressionStatement statement)
            {
                return null;
            }

            public object Visit(CodeFunctionStatement statement)
            {
                entCompiler.AddChildStatement(statement);
                return null;
            }

            public object Visit(CodeReturnStatement statement)
            {
                return null;
            }

            public object Visit(CodeSwitchStatement statement)
            {
                return null;
            }

            public object Visit(CodeTryStatement statement)
            {
                return null;
            }

            public object Visit(CodeWhileStatement statement)
            {
                return null;
            }

            public object Visit(CodeIfDefStatement statement)
            {
                return null;
            }

            public object Visit(CodeIfStatement statement)
            {
                return null;
            }

            public object Visit(CodeForStatement statement)
            {
                return null;
            }

            public object Visit(CodeForEachStatement statement)
            {
                return null;
            }

            public object Visit(CodeThrowStatement statement)
            {
                return null;
            }

            public object Visit(CodeVariableDeclarationStatement statement)
            {
                return null;
            }
        }

        private void CompileFunctionBody(ITFunctionBody body,
            CompileFunctionContext context)
        {
            StatementCompiler statementCompiler = new StatementCompiler(context);
            ITSurrogateClassEntity surrogate = new ITSurrogateClassEntity(this, body);

            surrogate.ParentScope = surrogate.Type.ParentScope = body.ParentScope;
            ((ITSurrogateClassType)surrogate.Type).DisplayName =
                string.Format(Properties.Resources.SurrogateRootClassName, body.Name);

            // process function's generic parameters to create the surrogate class type
            var genParams = body.GenericParameters;
            var genNames = new string[genParams != null ? genParams.Length : 0];
            for (int i = 0; i < genNames.Length; i++)
                genNames[i] = genParams[i].Name;
            surrogate.Type.InheritGenericParameters(genNames);

            var gens = surrogate.Type.GetGenericParameters();

            if (genNames.Length > 0)
            {
                // create reverse mutator

                var mutat = new IntermediateCompiler.GenericTypeMutator();
                for (int i = 0; i < genParams.Length; i++)
                {
                    mutat.AddMutator(genParams[i], gens[gens.Length - genParams.Length + i]);
                }
                body.ReverseMutator = mutat;
            }

            // prepare to instantiate the surrogate class type to match the generic type parameters
            var surGenParams = new ITType[gens.Length];
            for (int i = 0; i < gens.Length; i++)
                surGenParams[i] = gens[i];
            if (genParams != null)
            {
                for (int i = 0; i < genParams.Length; i++)
                    surGenParams[gens.Length - genParams.Length + i] = genParams[i];
            }
            body.SurrogateGenericParameters = surGenParams;

            // CompileStatements automatically sets body.Block
            ITBlock outBlock = statementCompiler.CompileStatements(body.GetCodeRootBlock(), body, null, null, true,
                surrogate);


            if (surrogate.Purge())
            {
                surrogate.Name = body.Name + "$surrogate";
                surrogate.Type.Name = surrogate.Name;
                surrogate.IsPrivate = true;
                body.ParentScope.AddChildEntity(surrogate);
            }

            body.DoCapturedVariableSubstitution();
        }

        private void CompileFunction(ITFunctionEntity ent)
        {
            CompileFunctionContext ctx = new CompileFunctionContext(this, null, ent.Body);
            ctx.compiler = this;
            CompileFunctionBody(ent.Body, ctx);
        }

        private void CompileFunction(ITMemberFunction ent, ITType ownerType)
        {
            CompileFunctionContext ctx = new CompileFunctionContext(this, ownerType, ent.Body);
            ctx.compiler = this;
            ent.Body.InstanceType = ownerType;
            CompileFunctionBody(ent.Body, ctx);
        }
    }
}
