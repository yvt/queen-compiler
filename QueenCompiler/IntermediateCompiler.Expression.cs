using System;
using System.Collections.Generic;
using System.Text;
using Queen.Language.IntermediateTree;
using Queen.Language.CodeDom;

namespace Queen.Language
{
    public partial class IntermediateCompiler
    {
        private class CompileFunctionContext : CodeDom.ICodeExpressionVisitor
        {
            public IntermediateCompiler compiler;
			public ITType type;
            public ITScope scope;
			public ITBlock currentBlock;
            public ITRoot root;

            private HashSet<ITScope> lineage;

            // function to compile, which may be null
            private ITFunctionBody funBody = null;

			public IDictionary<string, ITFunctionParameter> parameters = new Dictionary<string, ITFunctionParameter>();
            public IDictionary<string, ITMemberVariable> memberVariableCache = new Dictionary<string, ITMemberVariable>();
            public IDictionary<string, ITMemberProperty> memberPropertyCache = new Dictionary<string, ITMemberProperty>();
            public IDictionary<string, ITMemberFunction> memberFunctionCache = new Dictionary<string, ITMemberFunction>();

            public CompileFunctionContext(IntermediateCompiler compiler,
                ITType type, ITScope scope)
            {
                this.compiler = compiler;
                this.type = type;
                this.scope = scope;
                root = scope.Root;

                ITFunctionBody body = scope as ITFunctionBody;
                if (body != null)
                {
                    funBody = body;
                    foreach (ITFunctionParameter param in body.Parameters)
                    {
                        parameters[param.Name] = param;
                    }
                }
            }

            public ITFunctionBody FunctionBody
            {
                get { return funBody;  }
            }
			/*
			public string TryGetImplicitEntityName(CodeEntityExpression spec){
				CodeEntitySpecifier sp = spec.Entity;
				if(sp is CodeImplicitEntitySpecifier){
					return ((CodeImplicitEntitySpecifier)sp).Idenfitifer.Text;
                }else{
					return null;
                }
            }*/

            public ITGlobalVariableEntity AllocImplementationInternalGlobalVariable()
            {
                ITScope sc = scope;
                while (!(sc is ITRootGlobalScope))
                {
                    sc = sc.ParentScope;
                }

                ITGlobalVariableEntity ent = new ITGlobalVariableEntity();
                ent.Name = "__Internal" + Guid.NewGuid().ToString();
                ent.IsPrivate = true;
                return ent;
            }

            private HashSet<ITScope> ComputeLineage()
            {
                if (lineage != null) return lineage;

                var l = new HashSet<ITScope>();
                var scp = scope;
                while (scp != null)
                {
                    l.Add(scp);
                    scp = scp.ParentScope;
                }

                lineage = l;
                return lineage;
            }

            public bool CanAccessPrivateMemberOf(ITType ent)
            {
                for (var scp = scope; scp != null; scp = scp.ParentScope)
                {
                    ITClassType cls = scp as ITClassType;
                    if (cls != null)
                    {
                        // find inheritance tree
                        if (ent.Equals(cls))
                            return true;
                    }
                }
                return false;
            }

            public bool CanAccessNonPublicMemberOf(ITType ent)
            {
                for (var scp = scope; scp != null; scp = scp.ParentScope)
                {
                    ITClassType cls = scp as ITClassType;
                    if (cls != null)
                    {
                        // find inheritance tree
                        for (ITType typ = cls; typ != null; typ = typ.Superclass)
                        {
                            if (ent.Equals(typ))
                                return true;
                        }
                    }
                }
                return false;
            }

            public bool CanAccessMemberOf(ITType ent, bool isPublic, bool isPrivate)
            {
                if (isPublic)
                {
                    // ent cannot be accessible; such a construct must be prohibited.
                    return true;
                }
                if (isPrivate)
                {
                    return CanAccessPrivateMemberOf(ent);
                }
                return CanAccessNonPublicMemberOf(ent);
            }

            public bool CanAccessMemberOf(ITType ent, ITMember member)
            {
                return CanAccessMemberOf(ent, member.IsPublic, member.IsPrivate);
            }

            public bool CheckAccessibility(ITEntity entity)
            {
                var lineage = ComputeLineage();

                ITScope scp = entity.ParentScope;
                if (scp == null)
                {
                    throw new InvalidOperationException();
                }

                if (entity.IsPrivate)
                {
                    return lineage.Contains(scp);
                }
                if (!entity.IsPublic)
                {
                    ITClassType clsScp = scp as ITClassType;
                    if (CanAccessNonPublicMemberOf(clsScp))
                        return true;
                }

                // public
                while (true)
                {
                    var lscp = scp as ITLocalScope;
                    if (lscp != null)
                    {
                        scp = lscp.ParentScope;
                    }
                    else
                    {
                        break;
                    }
                }
                // reached root
                if (scp == null || scp is ITRootGlobalScope)
                    return true;
                if (lineage.Contains(scp))
                {
                    return true;
                }
                else
                {
                    ITClassType clsScp = scp as ITClassType;
                    if (clsScp != null)
                    {
                        if (clsScp.Entity == null)
                            return true;
                        return CheckAccessibility(clsScp.Entity);
                    }
                    else
                    {
                        // cannot access through function scope
                        return false;
                    }
                }

            }

            public ITExpression ResolveVirtualLocalVariable(string name, ITBlock block = null){
                if(name == null)
					return null;

				if(block == null)
					block = currentBlock;

                if (block == null)
                    return null;

				ITExpression var;
                if (block.VirtualLocalVariables.TryGetValue(name, out var))
                {
					return var;
                }

                if (block.ParentBlock != null)
                {
                    return ResolveVirtualLocalVariable(name, block.ParentBlock);
                }
				return null;
            }

			public ITLocalVariable ResolveLocalVariable(string name, ITBlock block = null) {
				if(name == null)
					return null;

				if(block == null)
					block = currentBlock;

                if (block == null)
                    return null;

				ITLocalVariable var;
                if (block.LocalVariables.TryGetValue(name, out var))
                {
					return var;
                }

                if (block.ParentBlock != null)
                {
                    return ResolveLocalVariable(name, block.ParentBlock);
                }
				return null;
            }

			public ITFunctionParameter ResolveFunctionParameter(string name){
				if(name == null)
					return null;
				ITFunctionParameter param;
				if(parameters.TryGetValue(name, out param)){
					return param;
                }
				return null;
            }

			public ITMemberVariable ResolveCalleeMemberVariable(string name, ITType typ = null){
				if(name == null)
                    return null;
				ITMemberVariable var;
				if(memberVariableCache.TryGetValue(name, out var)){
					return var;
                }
                if (typ == null)
                {
                    typ = type;
                }
                if (type == null)
                    return null;
				var = typ.GetMemberVariable(name);
				if(var != null){
					memberVariableCache[name] = var;
					return var;
                }
                
                // superclass(es) is/are done by GetMemberVariable
				return null;
            }

            public ITMemberProperty ResolveCalleeMemberProperty(string name, ITType typ = null)
            {
                if (name == null)
                    return null;
                ITMemberProperty var;
                if (memberPropertyCache.TryGetValue(name, out var))
                {
                    return var;
                }
                if (typ == null)
                {
                    typ = type;
                }
                if (type == null)
                    return null;
                var = typ.GetMemberProperty(name);
                if (var != null)
                {
                    memberPropertyCache[name] = var;
                    return var;
                }

                // superclass(es) is/are done by GetMemberProperty
                return null;
            }
            public ITMemberFunction ResolveCalleeMemberFunction(string name, ITType typ = null)
            {
                if (name == null)
                    return null;
                ITMemberFunction var;
                if (memberFunctionCache.TryGetValue(name, out var))
                {
                    return var;
                }
                if (typ == null)
                {
                    typ = type;
                }
                if (type == null)
                    return null;
                var = typ.GetMemberFunction(name);
                if (var != null)
                {
                    memberFunctionCache[name] = var;
                    return var;
                }
                
                // superclass(es) is/are done by GetMemberFunction
                return null;
            }


            public object Visit(CodeArrayConstructExpression expr)
            {
                ITArrayConstructExpression outExpr = new ITArrayConstructExpression();
                ITType type = compiler.ResolveType(expr.ElementType, scope);

                ITArrayType outType = compiler.CreateArrayType(type, expr.NumElements.Count);

                outExpr.Root = type.Root;
                outExpr.ExpressionType = outType;

               

				ITPrimitiveType typ = compiler.GetIndexerType();
                var lst = new List<ITExpression>(expr.NumElements.Count);
                foreach (CodeExpression numelm in expr.NumElements)
                {
                    ITExpression outNumElm = (ITExpression)numelm.Accept(this);
                    // auto cast from int64 to int32
                    outNumElm = compiler.CastImplicitToIndexerType(outNumElm, expr.Location);
                    outNumElm = compiler.CastImplicitly(outNumElm, typ, outNumElm.Location);
                    lst.Add(outNumElm);
                }
                outExpr.NumElements = lst;

                outExpr.ExpressionType = outType;
                outExpr.ElementType = type;

                return outExpr;
            }

            public object Visit(CodeArrayLiteralExpression expr)
            {
                ITArrayLiteralExpression outExpr = new ITArrayLiteralExpression();
                ITType type = compiler.ResolveType(expr.ElementType, scope);

                ITArrayType outType = compiler.CreateArrayType(type, 1);
                if (type == null)
                {
                    return compiler.CreateErrorExpression();
                }
                outExpr.ExpressionType = outType;
                outExpr.ElementType = outType.ElementType;

                var lst = outExpr.Elements = new List<ITExpression>(expr.Values.Count);
                foreach (CodeExpression val in expr.Values)
                {
                    ITExpression outval = (ITExpression)val.Accept(this);
                    outval = compiler.CastImplicitly(outval, type, outval.Location);
                    lst.Add(outval);
                }

                return outExpr;
            }

            private ITExpression DoArithmeticBinaryOperator(CodeBinaryOperatorExpression expr)
            {
				ITExpression expr1 = (ITExpression)(expr.Left.Accept(this));
                ITExpression expr2 = (ITExpression)(expr.Right.Accept(this));
				
                compiler.CastImplicitly(expr1, expr2, expr.Location,
                    out expr1, out expr2);

				ITPrimitiveType pType = expr1.ExpressionType as ITPrimitiveType;
				
                ITBinaryOperatorExpression ex = new ITBinaryOperatorExpression();
				string op = null;
                ex.Location = expr.Location;
                switch (expr.Type)
                {
                    case CodeBinaryOperatorType.Add:
                        ex.OperatorType = ITBinaryOperatorType.Add; op = "+";
                        break;
                    case CodeBinaryOperatorType.Subtract:
                        ex.OperatorType = ITBinaryOperatorType.Subtract; op = "-";
                        break;
                    case CodeBinaryOperatorType.Multiply:
                        ex.OperatorType = ITBinaryOperatorType.Multiply; op = "*";
                        break;
                    case CodeBinaryOperatorType.Divide:
                        ex.OperatorType = ITBinaryOperatorType.Divide; op = "/";
                        break;
                    case CodeBinaryOperatorType.Modulus:
                        ex.OperatorType = ITBinaryOperatorType.Modulus; op = "%";
                        break;
                    case CodeBinaryOperatorType.Power:
                        ex.OperatorType = ITBinaryOperatorType.Power; op = "^";
                        break;
                    default:
						throw new IntermediateCompilerException(Properties.Resources.InternalError);
                }

				if(pType == null){
					compiler.ReportError(string.Format(Properties.Resources.ICInvalidBinaryOperatorOperation,
						op, expr1.ExpressionType.ToString(), expr2.ExpressionType.ToString()), expr.Location);
                }else{
					switch(pType.Type){
                        case ITPrimitiveTypeType.Int8:
                        case ITPrimitiveTypeType.Int16:
                        case ITPrimitiveTypeType.Int32:
                        case ITPrimitiveTypeType.Int64:
                        case ITPrimitiveTypeType.Integer:
                        case ITPrimitiveTypeType.UInt8:
                        case ITPrimitiveTypeType.UInt16:
                        case ITPrimitiveTypeType.UInt32:
                        case ITPrimitiveTypeType.UInt64:
                        case ITPrimitiveTypeType.Float:
                        case ITPrimitiveTypeType.Double:
							break;
						default:
							compiler.ReportError(string.Format(Properties.Resources.ICInvalidBinaryOperatorOperation,
								op, expr1.ExpressionType.ToString(), expr2.ExpressionType.ToString()), expr.Location);
							break;
                    }
                }

				ex.Left = expr1; ex.Right = expr2;
				ex.ExpressionType = expr1.ExpressionType;
				ex.Root = expr1.Root;
				return compiler.constantFold.TryFold(ex);
            }

			private ITExpression DoBooleanOperator(CodeBinaryOperatorExpression expr)
            {
				ITExpression expr1 = (ITExpression)(expr.Left.Accept(this));
                ITExpression expr2 = (ITExpression)(expr.Right.Accept(this));
				
                compiler.CastImplicitly(expr1, expr2, expr.Location,
                    out expr1, out expr2);

				ITPrimitiveType pType = expr1.ExpressionType as ITPrimitiveType;
				
                ITBinaryOperatorExpression ex = new ITBinaryOperatorExpression();
				string op = null;
                ex.Location = expr.Location;
                switch (expr.Type)
                {
                    case CodeBinaryOperatorType.Or:
                        ex.OperatorType = ITBinaryOperatorType.Or; op = "|";
                        break;
                    case CodeBinaryOperatorType.And:
                        ex.OperatorType = ITBinaryOperatorType.And; op = "&";
                        break;
                    default:
						throw new IntermediateCompilerException(Properties.Resources.InternalError);
                }

				if(pType == null){
					compiler.ReportError(string.Format(Properties.Resources.ICInvalidBinaryOperatorOperation,
						op, expr1.ExpressionType.ToString(), expr2.ExpressionType.ToString()), expr.Location);
                }else{
					switch(pType.Type){
                        case ITPrimitiveTypeType.Bool:
							break;
						default:
							compiler.ReportError(string.Format(Properties.Resources.ICInvalidBinaryOperatorOperation,
								op, expr1.ExpressionType.ToString(), expr2.ExpressionType.ToString()), expr.Location);
							break;
                    }
                }

				ex.Left = expr1; ex.Right = expr2;
				ex.ExpressionType = expr1.ExpressionType;
				ex.Root = expr1.Root;
				return compiler.constantFold.TryFold(ex);
            }

            private ITExpression DoConcatOperator(CodeBinaryOperatorExpression expr)
            {
                ITExpression expr1 = (ITExpression)(expr.Left.Accept(this));
                ITExpression expr2 = (ITExpression)(expr.Right.Accept(this));

                compiler.CastImplicitly(expr1, expr2, expr.Location,
                    out expr1, out expr2);

                ITPrimitiveType pType = expr1.ExpressionType as ITPrimitiveType;
                ITArrayType arrType = expr1.ExpressionType as ITArrayType;

                ITBinaryOperatorExpression ex = new ITBinaryOperatorExpression();
                string op = null;
                ex.Location = expr.Location;
                switch (expr.Type)
                {
                    case CodeBinaryOperatorType.Concat:
                        ex.OperatorType = ITBinaryOperatorType.Concat; op = "~";
                        break;
                    default:
                        throw new IntermediateCompilerException(Properties.Resources.InternalError);
                }

                if (pType == null && (arrType == null || arrType.IsConcatable() == false))
                {
                    compiler.ReportError(string.Format(Properties.Resources.ICInvalidBinaryOperatorOperation,
                        op, expr1.ExpressionType.ToString(), expr2.ExpressionType.ToString()), expr.Location);
                }
                else if (arrType == null || arrType.IsConcatable() == false)
                {
                    switch (pType.Type)
                    {
                        case ITPrimitiveTypeType.String:
                            break;
                        default:
                            compiler.ReportError(string.Format(Properties.Resources.ICInvalidBinaryOperatorOperation,
                                op, expr1.ExpressionType.ToString(), expr2.ExpressionType.ToString()), expr.Location);
                            break;
                    }
                }

                ex.Left = expr1; ex.Right = expr2;
                ex.ExpressionType = expr1.ExpressionType;
                ex.Root = expr1.Root;
                return compiler.constantFold.TryFold(ex);
            }

            private ITExpression DoEqualityOperator(CodeBinaryOperatorExpression expr)
            {
                ITExpression expr1 = (ITExpression)(expr.Left.Accept(this));
                ITExpression expr2 = (ITExpression)(expr.Right.Accept(this));

                compiler.CastImplicitly(expr1, expr2, expr.Location,
                    out expr1, out expr2);

                ITPrimitiveType pType = expr1.ExpressionType as ITPrimitiveType;
                ITClassType cType = expr1.ExpressionType as ITClassType;

                ITBinaryOperatorExpression ex = new ITBinaryOperatorExpression();
                string op = null;
                ex.Location = expr.Location;
                switch (expr.Type)
                {
                    case CodeBinaryOperatorType.Equality:
                        ex.OperatorType = ITBinaryOperatorType.Equality; op = "=";
                        break;
                    case CodeBinaryOperatorType.Inequality:
                        ex.OperatorType = ITBinaryOperatorType.Inequality; op = "<>";
                        break;
                    default:
                        throw new IntermediateCompilerException(Properties.Resources.InternalError);
                }

                if (!expr1.ExpressionType.IsComparableTo(expr2.ExpressionType))
                {
                    if (pType == null && (cType == null || cType.UnderlyingEnumType == null))
                    {
                        compiler.ReportError(string.Format(Properties.Resources.ICInvalidBinaryOperatorOperation,
                            op, expr1.ExpressionType.ToString(), expr2.ExpressionType.ToString()), expr.Location);
                    }
                }

                ex.Left = expr1; ex.Right = expr2;
                ex.ExpressionType = compiler.GetPrimitiveType(ITPrimitiveTypeType.Bool);
                ex.Root = expr1.Root;
                return compiler.constantFold.TryFold(ex);
            }

            private ITExpression DoReferenceEqualityOperator(CodeBinaryOperatorExpression expr)
            {
                ITExpression expr1 = (ITExpression)(expr.Left.Accept(this));
                ITExpression expr2 = (ITExpression)(expr.Right.Accept(this));

                ITBinaryOperatorExpression ex = new ITBinaryOperatorExpression();
                string op = null;
                ex.Location = expr.Location;
                switch (expr.Type)
                {
                    case CodeBinaryOperatorType.ReferenceEquality:
                        ex.OperatorType = ITBinaryOperatorType.ReferenceEquality; op = "=&";
                        break;
                    case CodeBinaryOperatorType.ReferenceInequality:
                        ex.OperatorType = ITBinaryOperatorType.ReferenceInequality; op = "<>&";
                        break;
                    default:
                        throw new IntermediateCompilerException(Properties.Resources.InternalError);
                }

                if (expr1.ExpressionType.IsValueType != expr2.ExpressionType.IsValueType ||
                    (expr1.ExpressionType.IsValueType && expr1.ExpressionType != expr2.ExpressionType))
                {
                    compiler.ReportError(string.Format(Properties.Resources.ICInvalidBinaryOperatorOperation,
                        op, expr1.ExpressionType.ToString(), expr2.ExpressionType.ToString()), expr.Location);
                }

                ex.Left = expr1; ex.Right = expr2;
                ex.ExpressionType = compiler.GetPrimitiveType(ITPrimitiveTypeType.Bool);
                ex.Root = expr1.Root;
                return ex;
            }

            private ITExpression DoComparsionOperator(CodeBinaryOperatorExpression expr)
            {
                ITExpression expr1 = (ITExpression)(expr.Left.Accept(this));
                ITExpression expr2 = (ITExpression)(expr.Right.Accept(this));

                compiler.CastImplicitly(expr1, expr2, expr.Location,
                    out expr1, out expr2);

                ITPrimitiveType pType = expr1.ExpressionType as ITPrimitiveType;

                ITBinaryOperatorExpression ex = new ITBinaryOperatorExpression();
                string op = null;
                ex.Location = expr.Location;
                switch (expr.Type)
                {
                    case CodeBinaryOperatorType.LessThan:
                        ex.OperatorType = ITBinaryOperatorType.LessThan; op = "<";
                        break;
                    case CodeBinaryOperatorType.LessThanOrEqual:
                        ex.OperatorType = ITBinaryOperatorType.LessThanOrEqual; op = "<=";
                        break;
                    case CodeBinaryOperatorType.GreaterThan:
                        ex.OperatorType = ITBinaryOperatorType.GreaterThan; op = ">";
                        break;
                    case CodeBinaryOperatorType.GreaterThanOrEqual:
                        ex.OperatorType = ITBinaryOperatorType.GreaterThanOrEqual; op = ">=";
                        break;
                    default:
                        throw new IntermediateCompilerException(Properties.Resources.InternalError);
                }

                if (pType == null)
                {
                    if (!expr1.ExpressionType.IsComparableTo(expr2.ExpressionType))
                    {
                        compiler.ReportError(string.Format(Properties.Resources.ICInvalidBinaryOperatorOperation,
                            op, expr1.ExpressionType.ToString(), expr2.ExpressionType.ToString()), expr.Location);

                    }
                }
                else
                {
                    switch (pType.Type)
                    {
                        case ITPrimitiveTypeType.Float:
                        case ITPrimitiveTypeType.Double:
                        case ITPrimitiveTypeType.Int8:
                        case ITPrimitiveTypeType.Int16:
                        case ITPrimitiveTypeType.Int32:
                        case ITPrimitiveTypeType.Int64:
                        case ITPrimitiveTypeType.Integer:
                        case ITPrimitiveTypeType.UInt8:
                        case ITPrimitiveTypeType.UInt16:
                        case ITPrimitiveTypeType.UInt32:
                        case ITPrimitiveTypeType.UInt64:
                        case ITPrimitiveTypeType.String:
                        case ITPrimitiveTypeType.Char:
                            break;
                        default:
                            compiler.ReportError(string.Format(Properties.Resources.ICInvalidBinaryOperatorOperation,
                                op, expr1.ExpressionType.ToString(), expr2.ExpressionType.ToString()), expr.Location);
                            break;
                    }
                }

                ex.Left = expr1; ex.Right = expr2;
                ex.ExpressionType = compiler.GetPrimitiveType(ITPrimitiveTypeType.Bool);
                ex.Root = expr1.Root;
                return compiler.constantFold.TryFold(ex);
            }

            private ITExpression DoAssignOperator(CodeBinaryOperatorExpression expr)
            {
                ITExpression exprTarg = (ITExpression)(expr.Left.Accept(this));
                ITExpression exprBy = (ITExpression)(expr.Right.Accept(this));
                ITStorage storage = exprTarg as ITStorage;
                if (storage == null)
                {
                    compiler.ReportError(Properties.Resources.ICInvalidAssignment, expr.Location);
                    return exprBy;
                }

                exprBy = compiler.CastImplicitly(exprBy, exprTarg.ExpressionType, expr.Location);

                ITAssignExpression assign = new ITAssignExpression();
                assign.ExpressionType = exprTarg.ExpressionType;
                assign.Location = expr.Location;
                assign.Storage = storage;
                assign.Value = exprBy;

                ITPrimitiveType prim = exprTarg.ExpressionType as ITPrimitiveType;
                bool arithable = false;
                bool concatable = false;
                bool isBool = false;
                bool valid = true;
                string op= null;
                if (prim != null)
                {
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
                        case ITPrimitiveTypeType.Float:
                        case ITPrimitiveTypeType.Double:
                            arithable = true;
                            break;
						case ITPrimitiveTypeType.String:
                            concatable = true;
                            break;
						case ITPrimitiveTypeType.Bool:
                            isBool = true;
                            break;
                    }
                }

                switch (expr.Type)
                {
                    case CodeBinaryOperatorType.Assign:
                        assign.AssignType = ITAssignType.Assign;
                        op = "::";
                        break;
                    case CodeBinaryOperatorType.AdditionAssign:
                        assign.AssignType = ITAssignType.AdditionAssign;
                        if (!arithable) valid = false;
                        op = ":+";
                        break;
                    case CodeBinaryOperatorType.SubtractionAssign:
                        assign.AssignType = ITAssignType.SubtractionAssign;
                        if (!arithable) valid = false;
                        op = ":-";
                        break;
                    case CodeBinaryOperatorType.MultiplicationAssign:
                        assign.AssignType = ITAssignType.MultiplicationAssign;
                        if (!arithable) valid = false;
                        op = ":*";
                        break;
                    case CodeBinaryOperatorType.DivisionAssign:
                        assign.AssignType = ITAssignType.DivisionAssign;
                        if (!arithable) valid = false;
                        op = ":/";
                        break;
                    case CodeBinaryOperatorType.PowerAssign:
                        assign.AssignType = ITAssignType.PowerAssign;
                        if (!arithable) valid = false;
                        op = ":^";
                        break;
                    case CodeBinaryOperatorType.ModulusAssign:
                        assign.AssignType = ITAssignType.ModulusAssign;
                        if (!arithable) valid = false;
                        op = ":%";
                        break;
                    case CodeBinaryOperatorType.ConcatAssign:
                        assign.AssignType = ITAssignType.ConcatAssign;
                        if (!concatable) valid = false;
                        op = ":~";
                        break;
                    case CodeBinaryOperatorType.AndAssign:
                        assign.AssignType = ITAssignType.AndAssign;
                        if (!isBool) valid = false;
                        op = ":&";
                        break;
                    case CodeBinaryOperatorType.OrAssign:
                        assign.AssignType = ITAssignType.OrAssign;
                        if (!isBool) valid = false;
                        op = ":|";
                        break;
					default:
                        throw new IntermediateCompilerException(Properties.Resources.InternalError);
                }

                if (!valid)
                {
                    compiler.ReportError(string.Format(Properties.Resources.ICInvalidBinaryOperatorOperation,
                                   op, exprTarg.ExpressionType.ToString(), exprBy.ExpressionType.ToString()), expr.Location);
                }

                return assign;
            }

            public object Visit(CodeBinaryOperatorExpression expr)
            {
                switch (expr.Type)
                {
                    case CodeBinaryOperatorType.Add:
                    case CodeBinaryOperatorType.Subtract:
                    case CodeBinaryOperatorType.Multiply:
                    case CodeBinaryOperatorType.Divide:
                    case CodeBinaryOperatorType.Power:
                    case CodeBinaryOperatorType.Modulus:
                        return DoArithmeticBinaryOperator(expr);
                    case CodeBinaryOperatorType.Or:
                    case CodeBinaryOperatorType.And:
                        return DoBooleanOperator(expr);
                    case CodeBinaryOperatorType.Equality:
                    case CodeBinaryOperatorType.Inequality:
                        return DoEqualityOperator(expr);
                    case CodeBinaryOperatorType.ReferenceEquality:
                    case CodeBinaryOperatorType.ReferenceInequality:
                        return DoReferenceEqualityOperator(expr);
                    case CodeBinaryOperatorType.LessThan:
                    case CodeBinaryOperatorType.LessThanOrEqual:
                    case CodeBinaryOperatorType.GreaterThan:
                    case CodeBinaryOperatorType.GreaterThanOrEqual:
                        return DoComparsionOperator(expr);
                    case CodeBinaryOperatorType.Concat:
                        return DoConcatOperator(expr);
                    case CodeBinaryOperatorType.AdditionAssign:
                    case CodeBinaryOperatorType.SubtractionAssign:
                    case CodeBinaryOperatorType.MultiplicationAssign:
                    case CodeBinaryOperatorType.DivisionAssign:
                    case CodeBinaryOperatorType.PowerAssign:
                    case CodeBinaryOperatorType.ModulusAssign:
                    case CodeBinaryOperatorType.Assign:
                    case CodeBinaryOperatorType.AndAssign:
                    case CodeBinaryOperatorType.OrAssign:
                    case CodeBinaryOperatorType.ConcatAssign:
                        return DoAssignOperator(expr);
                    default:
                        throw new IntermediateCompilerException(Properties.Resources.InternalError);
                }
                // TODO: operator overloads?
            }

            public object Visit(CodeCastExpression expr)
            {
                ITExpression ex = (ITExpression)(expr.Expression.Accept(this));
				ITType fromType = ex.ExpressionType;
                ITType targetType = compiler.ResolveType(expr.Type, scope);
                if (ex.ExpressionType == targetType || targetType == null)
                {
                    return ex;
                }

                if (fromType.CanBeCastedTo(targetType, false) == false &&
                    targetType.CanBeCastedFrom(fromType, false) == false)
                {

                    bool testOut;
                    if (ITType.TryStaticTypeHierarchyCheck(ex.ExpressionType, targetType, out testOut))
                    {
                        if (!testOut)
                        {
                            compiler.ReportError(string.Format(Properties.Resources.ICInvalidExplicitCast,
                                fromType.ToString(), targetType.ToString()), expr.Location);
                            ITErrorExpression err = compiler.CreateErrorExpression();
                            err.ExpressionType = targetType;
                            return err;
                        }
                    }

                }

                ITClassType fromClass = fromType as ITClassType;
                ITClassType toClass = targetType as ITClassType;
                ITValueExpression fromValue = ex as ITValueExpression;
                if (fromValue != null && fromClass != null && 
                    fromClass.UnderlyingEnumType != null && targetType is ITPrimitiveType)
                {
                    // enum -> primitive
                    fromValue.ExpressionType = targetType;
                    fromValue.Value = compiler.constantFold.Cast(fromValue.Value, fromClass.UnderlyingEnumType.Type,
                        (targetType as ITPrimitiveType).Type);
                    return fromValue;
                }
                else if (fromValue != null && toClass != null &&
                   toClass.UnderlyingEnumType != null && fromValue.ExpressionType is ITPrimitiveType)
                {
                    // primitive -> enum
                    fromValue.ExpressionType = targetType;
                    fromValue.Value = compiler.constantFold.Cast(fromValue.Value, ((ITPrimitiveType)fromValue.ExpressionType).Type,
                        toClass.UnderlyingEnumType.Type);
                    return fromValue;
                }else if (fromValue != null)
                {
                    // primitive -> primitive, constant
                    ITPrimitiveType primFrom = fromType as ITPrimitiveType;
                    ITPrimitiveType primTo = targetType as ITPrimitiveType;
                    if (primTo != null && primFrom != null)
                    {
                        ITValueExpression val = new ITValueExpression();
                        val.ExpressionType = primTo;
                        val.Location = expr.Location;
                        val.Value = compiler.constantFold.Cast(fromValue.Value, primFrom.Type, primTo.Type);
                        return val;
                    }
                }

                ITCastExpression cast = new ITCastExpression();
                cast.Expression = ex;
                cast.Location = expr.Location;
                cast.ExpressionType = targetType;
                cast.CastTarget = targetType;
                cast.Root = ex.Root;
                return cast;
            }

            public object Visit(CodeClassConstructExpression expr)
            {
                ITClassConstructExpression outExpr = new ITClassConstructExpression();
                ITType type = compiler.ResolveType(expr.Type, scope);

                if (type == null)
                {
                    ITErrorExpression err = compiler.CreateErrorExpression();
                    err.ExpressionType = type;
                    return err;
                }

                outExpr.Root = type.Root;
                outExpr.Type = type;
                outExpr.ExpressionType = type;

                ITClassType cls = type as ITClassType;
                if (cls != null && cls.IsInterface())
                {
                    compiler.ReportError(string.Format(Properties.Resources.ICConstructInterface, cls.ToString()), expr.Location);
                }
                else if (cls != null && cls.IsAbstract())
                {
                    compiler.ReportError(string.Format(Properties.Resources.ICConstructAbstructClass, cls.ToString()), expr.Location);
                }
                else if (!type.CanConstruct())
                {
                    compiler.ReportError(string.Format(Properties.Resources.ICNotConstructible, cls.ToString()), expr.Location);
                }

                return outExpr;
            }

            public object Visit(CodeEntityExpression expr)
            {
                CodeEntitySpecifier entitySpecifier = expr.Entity;
                CodeImplicitEntitySpecifier impl = entitySpecifier as CodeImplicitEntitySpecifier;
                CodeGenericsEntitySpecifier gene = entitySpecifier as CodeGenericsEntitySpecifier;
                bool isGeneric = false;
                if (impl == null && gene != null)
                {
                    impl = gene.GenericEntity as CodeImplicitEntitySpecifier;
                    if (gene.GenericParameters.Count > 0)
                    {
                        isGeneric = true;
                    }
                }
                if (impl != null)
                {
                    string name = impl.Idenfitifer.Text;
                    if (name == "this" && type != null && !isGeneric)
                    {
                        ITReferenceCalleeExpression ex = new ITReferenceCalleeExpression();
                        ex.ExpressionType = type;
                        ex.Location = expr.Location;
                        return ex;
                    }

                    ITExpression virtLocalVar = ResolveVirtualLocalVariable(name);
                    if(virtLocalVar != null & !isGeneric){
                        return virtLocalVar;
                    }

                    ITLocalVariable localVariable = ResolveLocalVariable(name);
                    if (localVariable != null && !isGeneric)
                    {
                        ITLocalVariableStorage stor = new ITLocalVariableStorage();
                        stor.ExpressionType = localVariable.Type;
                        stor.Variable = localVariable;
                        stor.Location = expr.Location;
                        return stor;
                    }

                    ITFunctionParameter param = ResolveFunctionParameter(name);
                    if (param != null && !isGeneric)
                    {
                        ITParameterStorage stor = new ITParameterStorage();
                        stor.ExpressionType = param.Type;
                        stor.Variable = param;
                        stor.Location = expr.Location;
                        return stor;
                    }

                    if (funBody != null)
                    {
                        ITExpression capVar = funBody.GetExternalValue(name, expr.Location, compiler);
                        if (capVar != null && !isGeneric)
                        {
                            return capVar;
                        }
                    }

                    ITMemberVariable memVar = ResolveCalleeMemberVariable(name);
                    if (memVar != null && !isGeneric) // member variable is never generic (currently)
                    {
                        ITMemberVariableStorage stor = new ITMemberVariableStorage();
                        stor.Root = memVar.Root;
                        stor.ExpressionType = memVar.Type;
                        stor.Member = memVar;
                        stor.Location = expr.Location;
                        stor.Instance = new ITReferenceCalleeExpression()
                        {
                            ExpressionType = type,
                            Location = expr.Location
                        };
                        return stor;
                    }

                    ITMemberProperty memProp = ResolveCalleeMemberProperty(name);
                    if (memProp != null && !isGeneric) // member property is never generic (currently)
                    {
                        ITMemberPropertyStorage stor = new ITMemberPropertyStorage();
                        stor.ExpressionType = memProp.Type;
                        stor.Member = memProp;
                        stor.Location = expr.Location;
                        stor.Instance = new ITReferenceCalleeExpression()
                        {
                            ExpressionType = type,
                            Location = expr.Location
                        };
                        return stor;
                    }

                    ITMemberFunction memFunc = ResolveCalleeMemberFunction(name);
                    if (memFunc != null)
                    {
                        ITMemberFunctionStorage stor = new ITMemberFunctionStorage();
                        stor.Function = memFunc;
                        stor.Object = new ITReferenceCalleeExpression();
                        stor.Object.ExpressionType = type;
                        stor.Location = expr.Location;
                        
                        var storGenParams = new ITType[memFunc.Body.GenericParameters.Length];
                        stor.GenericTypeParameters = storGenParams;

                        if (gene != null)
                        {
                            var genParams = gene.GenericParameters.Count;
                            int off = 0;
                            if (storGenParams.Length != gene.GenericParameters.Count)
                            {
                                compiler.ReportError(string.Format(Properties.Resources.ICWrongNumGenericParameters,
                                    gene.GenericParameters.Count, storGenParams.Length), expr.Location);
                                while (off < storGenParams.Length)
                                {
                                    storGenParams[off++] = compiler.builtinTypes["int"];
                                }
                            }
                            else
                            {
                                foreach (CodeType typ in gene.GenericParameters)
                                {
                                    ITType tp = compiler.ResolveType(typ, scope);
                                    storGenParams[off++] = tp;
                                }
                            }
                        }

                        stor.ExpressionType = compiler.CreateFunctionTypeForMemberFunction(memFunc, stor.GenericTypeParameters);

                        return stor;
                    }
                }

                {
                    ResolveEntityResult entityResult = compiler.ResolveEntity(entitySpecifier, scope);
                    if (entityResult == null)
                        return compiler.CreateErrorExpression();
                    ITEntity entity = entityResult.entity;

                    if (!CheckAccessibility(entity))
                    {
                        compiler.ReportError(string.Format(Properties.Resources.ICAccessProhibited, entity.ToString()), entitySpecifier.Location);
                    }

                    ITFunctionEntity gfunc = entity as ITFunctionEntity;
                    if (gfunc != null)
                    {
                        ITGlobalFunctionStorage stor = new ITGlobalFunctionStorage();
                        stor.Root = gfunc.Root;
                        stor.Function = gfunc;
                        stor.ExpressionType = compiler.CreateFunctionTypeForGlobalFunction(gfunc, entityResult.GenericParameters);
                        stor.Location = expr.Location;
                        if (entityResult.GenericParameters != null && entityResult.GenericParameters.Length > 0)
                        {
                            stor.Function = stor.Function.MakeGenericFunction(entityResult.GenericParameters);
                        }
                        return stor;
                    }

                    ITGlobalVariableEntity gvar = entity as ITGlobalVariableEntity;
                    if (gvar != null)
                    {
                        if (gvar.IsConst)
                        {
                            ITExpression val = gvar.InitialValue;
                            ITUnresolvedConstantExpression unres = val as ITUnresolvedConstantExpression;
                            if (unres != null)
                                unres.Resolve();
                            return gvar.InitialValue;
                        }
                        ITGlobalVariableStorage storag = new ITGlobalVariableStorage();
                        storag.ExpressionType = gvar.Type;
                        storag.Location = expr.Location;
                        storag.Root = gvar.Root;
                        storag.Variable = gvar;
                        storag.GenericTypeParameters = entityResult.GenericParameters;
                        return storag;
                    }

                    if (entity is ITClassEntity)
                    {
                        compiler.ReportError(Properties.Resources.ICClassAsValue, entitySpecifier.Location);
                        return compiler.CreateErrorExpression();
                    }
                    throw new IntermediateCompilerException(Properties.Resources.InternalError);
                }
            }

            public object Visit(CodeIndexExpression expr)
            {
                ITExpression val = (ITExpression)(expr.Expression.Accept(this));
                if (val.ExpressionType is ITArrayType)
                {
                    ITArrayType arr = (ITArrayType)val.ExpressionType;
                    if (arr.Dimensions != expr.Parameters.Count)
                    {
                        compiler.ReportError(string.Format(Properties.Resources.ICWrongNumIndices,
                            expr.Parameters.Count, arr.Dimensions), expr.Location);
                    }

                    ITArrayElementStorage loc = new ITArrayElementStorage();
                    loc.Root = val.Root;
                    loc.Variable = val;
                    loc.Location = expr.Location;

                    ITPrimitiveType idxType = compiler.GetIndexerType();
                    foreach (CodeInvocationParameter param in expr.Parameters)
                    {
                        if (param.ByRef)
                        {
                            compiler.ReportError(Properties.Resources.ICIndexArrayByRef, param.Location);
                        }
                        ITExpression pexp = (ITExpression)(param.Value.Accept(this));
                        
                        // auto cast from int64 to int32
                        pexp = compiler.CastImplicitToIndexerType(pexp, expr.Location);

                        pexp = compiler.CastImplicitly(pexp, idxType, expr.Location);
                        loc.Indices.Add(pexp);
                    }
                    loc.ExpressionType = arr.ElementType;
                    return loc;
                }
                else
                {
                    ITMemberProperty prop = val.ExpressionType.GetMemberProperty("Item");
                    if (prop == null)
                    {
                        compiler.ReportError(string.Format(Properties.Resources.ICNotIndexable,
                            val.ExpressionType.ToString()), expr.Location);
                        ITErrorExpression err = compiler.CreateErrorExpression();
                        return err;
                    }

                    if (prop.Parameters.Count != expr.Parameters.Count)
                    {
                        compiler.ReportError(string.Format(Properties.Resources.ICWrongNumIndices,
                                expr.Parameters.Count, prop.Parameters.Count), expr.Location);
                    }

                    ITMemberPropertyStorage stor = new ITMemberPropertyStorage();
                    stor.Root = val.Root;
                    stor.Instance = val;
                    stor.Member = prop;
                    stor.Parameters = new List<ITExpression>();
                    var lst = expr.Parameters;
                    var lst2 = prop.Parameters;
                   
                    for (int i = 0, count = lst.Count; i < count; i++)
                    {
                        CodeInvocationParameter param = lst[i];
                        ITFunctionParameter funcParam = lst2[i];
                        if (param.ByRef != funcParam.IsByRef)
                        {
                            compiler.ReportError(string.Format(Properties.Resources.ICWrongParameterPassingMethod,
                                i + 1), param.Location);
                        }
                        ITExpression pexp = (ITExpression)(param.Value.Accept(this));
                        pexp = compiler.CastImplicitly(pexp, funcParam.Type, expr.Location);
                        stor.Parameters.Add(pexp);
                    }
                    stor.ExpressionType = prop.Type;
                    return stor;
                }
            }

            public object Visit(CodeInvocationExpression expr)
            {
                ITExpression val = (ITExpression)(expr.Method.Accept(this));
                if (val is ITGlobalFunctionStorage)
                {
                    ITGlobalFunctionStorage stor = (ITGlobalFunctionStorage)val;
                    ITCallGlobalFunctionExpression callExpr = new ITCallGlobalFunctionExpression();
                    callExpr.Function = stor;
                    callExpr.ExpressionType = stor.Function.GetReturnType();
                    callExpr.Location = expr.Location;
                    callExpr.Root = val.Root;

                    var lst3 = stor.Function.GetParameterTypes();
                    if (lst3.Length != expr.Parameters.Count)
                    {
                        compiler.ReportError(string.Format(Properties.Resources.ICWrongNumParameters,
                                expr.Parameters.Count, stor.Function.Body.Parameters.Count), expr.Location);
                    }

                    var lst = expr.Parameters;
                    var lst2 = stor.Function.Body.Parameters;
                    for (int i = 0, count = lst.Count; i < count; i++)
                    {
                        CodeInvocationParameter param = lst[i];
                        ITFunctionParameter funcParam = lst2[i];
                        if (param.ByRef != funcParam.IsByRef)
                        {
                            compiler.ReportError(string.Format(Properties.Resources.ICWrongParameterPassingMethod,
                                i + 1), param.Location);
                        }
                        ITExpression pexp = (ITExpression)(param.Value.Accept(this));
                        pexp = compiler.CastImplicitly(pexp, lst3[i], expr.Location);
                        callExpr.Parameters.Add(pexp);
                    }

                    return callExpr;
                }
                else if (val is ITMemberFunctionStorage)
                {
                    ITMemberFunctionStorage stor = (ITMemberFunctionStorage)val;
                    ITCallMemberFunctionExpression callExpr = new ITCallMemberFunctionExpression();
                    var mutator = new GenericTypeMutator(stor.Function.Body.GenericParameters, stor.GenericTypeParameters);
                    callExpr.Function = stor;
                    callExpr.ExpressionType = mutator.Mutate(stor.Function.GetReturnType());
                    callExpr.Location = expr.Location;

                    if (stor.Function.Body.Parameters.Count != expr.Parameters.Count)
                    {
                        compiler.ReportError(string.Format(Properties.Resources.ICWrongNumParameters,
                                expr.Parameters.Count, stor.Function.Body.Parameters.Count), expr.Location);
                    }

                    var lst = expr.Parameters;
                    var lst2 = stor.Function.Body.Parameters;
                    ITType[] paramTypes = stor.Function.GetParameterTypes();
                    for (int i = 0, count = lst.Count; i < count; i++)
                    {
                        CodeInvocationParameter param = lst[i];
                        ITFunctionParameter funcParam = lst2[i];
                        if (param.ByRef != funcParam.IsByRef)
                        {
                            compiler.ReportError(string.Format(Properties.Resources.ICWrongParameterPassingMethod,
                                i + 1), param.Location);
                        }
                        ITExpression pexp = (ITExpression)(param.Value.Accept(this));
                        pexp = compiler.CastImplicitly(pexp, mutator.Mutate(paramTypes[i]), expr.Location);
                        callExpr.Parameters.Add(pexp);
                    }
                    return callExpr;
                }
                else if(val.ExpressionType is ITFunctionType)
                {
                    ITFunctionType func = (ITFunctionType)val.ExpressionType;
                    ITCallFunctionReferenceExpression callExpr = new ITCallFunctionReferenceExpression();
                    callExpr.Function = val;
                    callExpr.ExpressionType = func.ReturnType;
                    callExpr.Location = expr.Location;

                    if (func.Parameters.Length != expr.Parameters.Count)
                    {
                        compiler.ReportError(string.Format(Properties.Resources.ICWrongNumParameters,
                                expr.Parameters.Count, func.Parameters.Length), expr.Location);
                    }

                    var lst = expr.Parameters;
                    var lst2 = func.Parameters;
                    var prms = new ITExpression[lst.Count];
                    for (int i = 0, count = lst.Count; i < count; i++)
                    {
                        CodeInvocationParameter param = lst[i];
                        ITFunctionParameter funcParam = lst2[i];
                        if (param.ByRef != funcParam.IsByRef)
                        {
                            compiler.ReportError(string.Format(Properties.Resources.ICWrongParameterPassingMethod,
                                i + 1), param.Location);
                        }
                        ITExpression pexp = (ITExpression)(param.Value.Accept(this));
                        pexp = compiler.CastImplicitly(pexp, funcParam.Type, expr.Location);
                        prms[i] = pexp;
                    }

                    callExpr.Parameters = prms;
                    return callExpr;
                }
                else
                {
                    compiler.ReportError(string.Format(Properties.Resources.ICUncallable, val.ExpressionType.ToString()),
                        expr.Location);
                    return compiler.CreateErrorExpression();
                }
            }

            public object Visit(CodeMemberAccessExpression expr)
            {
                ITExpression val = (ITExpression)(expr.Expression.Accept(this));
                if (val is ITGlobalFunctionStorage ||
                    val is ITMemberFunctionStorage)
                {
                    compiler.ReportError(Properties.Resources.ICMemberAccessForFunction, expr.Location);
                    return compiler.CreateErrorExpression();
                }

                ITType type = val.ExpressionType;
                string memberName = expr.MemberName.Text;
                bool isGeneric = expr.GenericTypeParameters.Count > 0;

                if (memberName == "Ctor")
                {
                    compiler.ReportError(Properties.Resources.ICConstructorCall, expr.Location);
                    return compiler.CreateErrorExpression();
                }
                else if (memberName == "Dtor")
                {
                    compiler.ReportError(Properties.Resources.ICDestructorCall, expr.Location);
                    return compiler.CreateErrorExpression();
                }

                if(!isGeneric){
                    ITMemberVariable var = type.GetMemberVariable(memberName);
					if(var != null){
                        if (!CanAccessMemberOf(type, var))
                        {
                            compiler.ReportError(string.Format(Properties.Resources.ICAccessProhibited, type.ToString() + "." + var.Name),
                                expr.Location);
                        }
                        ITMemberVariableStorage stor = new ITMemberVariableStorage();
                        stor.Instance = val;
                        stor.Location = expr.Location;
                        stor.Member = var;
                        stor.Root = val.Root;
                        stor.ExpressionType = var.Type;
                        return stor;
                    }
                }

                if(!isGeneric){
                    ITMemberProperty var = type.GetMemberProperty(memberName);
                    if (var != null)
                    {
                        if (!CanAccessMemberOf(type, var))
                        {
                            compiler.ReportError(string.Format(Properties.Resources.ICAccessProhibited, type.ToString() + "." + memberName),
                                expr.Location);
                        }
                        ITMemberPropertyStorage stor = new ITMemberPropertyStorage();
                        stor.Instance = val;
                        stor.Location = expr.Location;
                        stor.Member = var;
                        stor.Root = val.Root;
                        stor.ExpressionType = var.Type;
                        return stor;
                    }
                }

                {
                    ITMemberFunction var = type.GetMemberFunction(memberName);
                    if (var != null)
                    {
                        if (!CanAccessMemberOf(type, var))
                        {
                            compiler.ReportError(string.Format(Properties.Resources.ICAccessProhibited, type.ToString() + "." + var.Name),
                                expr.Location);
                        }
                        ITMemberFunctionStorage stor = new ITMemberFunctionStorage();
                        stor.Object = val;
                        stor.Location = expr.Location;
                        stor.Function = var;
                        stor.Root = val.Root;

                        var gtparams = new ITType[var.Body.GenericParameters.Length];
                        var inparams = expr.GenericTypeParameters;
                        if (gtparams.Length != inparams.Count)
                        {
                            compiler.ReportError(string.Format(Properties.Resources.ICWrongNumGenericParameters,
                                inparams.Count, gtparams.Length), expr.Location);
                            for (int i = 0; i < gtparams.Length; i++)
                                gtparams[i] = compiler.builtinTypes["int"];
                        }
                        else
                        {
                            for (int i = 0; i < gtparams.Length; i++)
                            {
                                ITType typ = compiler.ResolveType(inparams[i], scope);
                                if (typ == null) typ = compiler.builtinTypes["int"];
                                gtparams[i] = typ;
                            }
                        }

                        stor.GenericTypeParameters = gtparams;
                        stor.ExpressionType = compiler.CreateFunctionTypeForMemberFunction(var, gtparams);

                        return stor;
                    }
                }

                compiler.ReportError(string.Format(Properties.Resources.ICMemberNotFound,
                    type.ToString(), memberName), expr.Location);
                return compiler.CreateErrorExpression();
            }

            public object Visit(CodeTypeEqualityExpression expr)
            {
                ITExpression val = (ITExpression)(expr.Expression.Accept(this));
                ITType type = compiler.ResolveType(expr.Type, scope);

                if (val is ITValueExpression)
                {
                    ITValueExpression val2 = (ITValueExpression)val;
                    if (val2.Value == null)
                    {
                        ITValueExpression val3 = new ITValueExpression();
                        val3.Root = val2.Root;
                        val3.Location = expr.Location;
                        val3.Value = false;
                        val3.ExpressionType = compiler.GetPrimitiveType(ITPrimitiveTypeType.Bool);
                        return val3;
                    }

                    bool check;
                    if (ITType.TryStaticTypeHierarchyCheck(val.ExpressionType, type, out check))
                    {
                        ITValueExpression val3 = new ITValueExpression();
                        val3.Root = val2.Root;
                        val3.Location = expr.Location;
                        val3.Value = check;
                        val3.ExpressionType = compiler.GetPrimitiveType(ITPrimitiveTypeType.Bool);
                        return val3;
                    }
                }

                // cannot do static check for non-static value

                ITTypeCheckExpression ex = new ITTypeCheckExpression();
                ex.Object = val;
                ex.TargetType = type;
                ex.Type = ITTypeCheckExpressionType.Is;
                ex.Location = expr.Location;
                ex.Root = val.Root;
                ex.ExpressionType = compiler.GetPrimitiveType(ITPrimitiveTypeType.Bool);

                return ex;
            }

            public object Visit(CodeTypeInequalityExpression expr)
            {
                ITExpression val = (ITExpression)(expr.Expression.Accept(this));
                ITType type = compiler.ResolveType(expr.Type, scope);

                if(val is ITValueExpression){
                    ITValueExpression val2 = (ITValueExpression)val;
                    if (val2.Value == null)
                    {
                        ITValueExpression val3 = new ITValueExpression();
                        val3.Root = val2.Root;
                        val3.Location = expr.Location;
                        val3.Value = true;
                        val3.ExpressionType = compiler.GetPrimitiveType(ITPrimitiveTypeType.Bool);
                        return val3;
                    }

                    bool check;
                    if (ITType.TryStaticTypeHierarchyCheck(val.ExpressionType, type, out check))
                    {
                        ITValueExpression val3 = new ITValueExpression();
                        val3.Root = val2.Root;
                        val3.Location = expr.Location;
                        val3.Value = !check;
                        val3.ExpressionType = compiler.GetPrimitiveType(ITPrimitiveTypeType.Bool);
                        return val3;
                    }
                }

                // cannot do static check for non-static value

                ITTypeCheckExpression ex = new ITTypeCheckExpression();
                ex.Object = val;
                ex.TargetType = type;
                ex.Type = ITTypeCheckExpressionType.IsNot;
                ex.Location = expr.Location;
                ex.Root = val.Root;
                ex.ExpressionType = compiler.GetPrimitiveType(ITPrimitiveTypeType.Bool);

                return ex;

            }

            public object Visit(CodeTernaryConditionalExpression expr)
            {
                ITExpression cond = (ITExpression)(expr.Condition.Accept(this));
                cond = compiler.CastImplicitly(cond, compiler.GetPrimitiveType(ITPrimitiveTypeType.Bool), expr.Location);

                ITExpression expr1 = (ITExpression)(expr.TrueValue.Accept(this));
                ITExpression expr2 = (ITExpression)(expr.FalseValue.Accept(this));
                compiler.CastImplicitly(expr1, expr2, expr.Location,
                    out expr1, out expr2);

                if (cond is ITValueExpression)
                {
                    // static branching
                    ITValueExpression ex = (ITValueExpression)cond;
                    bool b = (bool)ex.Value;
                    if (b)
                        return expr1;
                    else
                        return expr2;
                }

                {
                    ITConditionalExpression ex = new ITConditionalExpression();
                    ex.Conditional = cond;
                    ex.FalseValue = expr2;
                    ex.TrueValue = expr1;
                    ex.Root = cond.Root;
                    ex.Location = expr.Location;
                    ex.ExpressionType = expr1.ExpressionType;
                    return ex;
                }
            }

            public object Visit(CodeUnaryOperatorExpression expr)
            {
                ITExpression val = (ITExpression)(expr.Expression.Accept(this));
                ITUnaryOperatorType typ;
                switch (expr.Type)
                {
                    case CodeUnaryOperatorType.Copy:
                        throw new NotImplementedException();
                    case CodeUnaryOperatorType.Negate:
                        typ = ITUnaryOperatorType.Negate;
                        break;
                    case CodeUnaryOperatorType.Not:
                        typ = ITUnaryOperatorType.Not;
                        break;
                    case CodeUnaryOperatorType.PassThrough:
                        return val;
                    default:
                        throw new IntermediateCompilerException(Properties.Resources.InternalError);
                }

                if (val.ExpressionType is ITPrimitiveType)
                {
                    ITPrimitiveTypeType pType = ((ITPrimitiveType)val.ExpressionType).Type;
                    switch (pType)
                    {
                        case ITPrimitiveTypeType.Double:
                        case ITPrimitiveTypeType.Float:
                        case ITPrimitiveTypeType.Int8:
                        case ITPrimitiveTypeType.Int16:
                        case ITPrimitiveTypeType.Int32:
                        case ITPrimitiveTypeType.Int64:
                        case ITPrimitiveTypeType.Integer:
                            break;
                        case ITPrimitiveTypeType.Bool:
                            if (typ == ITUnaryOperatorType.Negate)
                            {
                                compiler.ReportError(string.Format(Properties.Resources.ICInvalidUnaryOperatorOperation,
                                    "-", val.ExpressionType.ToString()), expr.Location);
                                return val;
                            }
                            break;
                        default:
                            if (typ == ITUnaryOperatorType.Negate)
                            {
                                compiler.ReportError(string.Format(Properties.Resources.ICInvalidUnaryOperatorOperation,
                                    "-", val.ExpressionType.ToString()), expr.Location);
                                return val;
                            }
                            if (typ == ITUnaryOperatorType.Not)
                            {
                                compiler.ReportError(string.Format(Properties.Resources.ICInvalidUnaryOperatorOperation,
                                    "-", val.ExpressionType.ToString()), expr.Location);
                                return val;
                            }
                            throw new IntermediateCompilerException(Properties.Resources.InternalError);
                    }

                    
                }
                else
                {
                    // TODO: check for overloaded operator 
                    if (typ == ITUnaryOperatorType.Negate)
                    {
                        compiler.ReportError(string.Format(Properties.Resources.ICInvalidUnaryOperatorOperation,
                            "-", val.ExpressionType.ToString()), expr.Location);
                        return val;
                    }
                    if (typ == ITUnaryOperatorType.Not)
                    {
                        compiler.ReportError(string.Format(Properties.Resources.ICInvalidUnaryOperatorOperation,
                            "-", val.ExpressionType.ToString()), expr.Location);
                        return val;
                    }
                    throw new IntermediateCompilerException(Properties.Resources.InternalError);
                }

                // general pattern
                {
                    ITUnaryOperatorExpression ex = new ITUnaryOperatorExpression();
                    ex.Type = typ;
                    ex.Expression = val;
                    ex.Location = expr.Location;
                    ex.Root = val.Root;
                    ex.ExpressionType = val.ExpressionType;
                    return compiler.constantFold.TryFold(ex);
                }

            }

            public object Visit(CodeValueExpression expr)
            {
                ITValueExpression ex = new ITValueExpression();
                object val = expr.Value;
                ex.Root = root;
                ex.Value = val;
                ex.Location = expr.Location;

                if (val == null)
                {
                    ex.ExpressionType = new ITNullType(compiler);
                }
                else
                {
                    Type type = val.GetType();
                    if (type == typeof(int))
                    {
                        ex.ExpressionType = compiler.GetPrimitiveType(ITPrimitiveTypeType.Int32);
                    }
                    else if (type == typeof(uint))
                    {
                        ex.ExpressionType = compiler.GetPrimitiveType(ITPrimitiveTypeType.UInt32);
                    }
                    else if (type == typeof(float))
                    {
                        ex.ExpressionType = compiler.GetPrimitiveType(ITPrimitiveTypeType.Float);
                    }
                    else if (type == typeof(bool))
                    {
                        ex.ExpressionType = compiler.GetPrimitiveType(ITPrimitiveTypeType.Bool);
                    }
                    else if (type == typeof(string))
                    {
                        ex.ExpressionType = compiler.GetPrimitiveType(ITPrimitiveTypeType.String);
                    }
                    else if (type == typeof(char))
                    {
                        ex.ExpressionType = compiler.GetPrimitiveType(ITPrimitiveTypeType.Char);
                    }
                    else if (type == typeof(long))
                    {
                        ex.ExpressionType = compiler.GetPrimitiveType(ITPrimitiveTypeType.Integer);
                    }
                    else if (type == typeof(ulong))
                    {
                        ex.ExpressionType = compiler.GetPrimitiveType(ITPrimitiveTypeType.UInt64);
                    }
                    else if (type == typeof(sbyte))
                    {
                        ex.ExpressionType = compiler.GetPrimitiveType(ITPrimitiveTypeType.Int8);
                    }
                    else if (type == typeof(byte))
                    {
                        ex.ExpressionType = compiler.GetPrimitiveType(ITPrimitiveTypeType.UInt8);
                    }
                    else if (type == typeof(short))
                    {
                        ex.ExpressionType = compiler.GetPrimitiveType(ITPrimitiveTypeType.Int16);
                    }
                    else if (type == typeof(double))
                    {
                        ex.ExpressionType = compiler.GetPrimitiveType(ITPrimitiveTypeType.Double);
                    }
                    else if (type == typeof(ushort))
                    {
                        ex.ExpressionType = compiler.GetPrimitiveType(ITPrimitiveTypeType.UInt16);
                    }
                    else if (type == typeof(sbyte))
                    {
                        ex.ExpressionType = compiler.GetPrimitiveType(ITPrimitiveTypeType.Int8);
                    }
                    else
                    {
                        throw new IntermediateCompilerException(Properties.Resources.InternalError);
                    }
                }
                return ex;
            }

            private static int anonymousFunctionIndex = 1;

            private ITAnonymousFunctionBody CreateFunctionBody(CodeAnonymousFunctionExpression stat, ITScope parentScope)
            {
                ITAnonymousFunctionBody body = new ITAnonymousFunctionBody();
                body.Name = "$anonymous-" + System.Threading.Interlocked.Increment(ref anonymousFunctionIndex).ToString();
                body.Root = parentScope.Root;
                body.ParentScope = parentScope;
                body.Location = stat.Location;

                body.AnonymousFunctionExpression = stat;

                if (stat.ReturnType != null)
                {
                    body.ReturnType = compiler.ResolveType(stat.ReturnType, body);
                }

                if (stat.Parameters != null)
                {
                    foreach (CodeParameterDeclaration param in stat.Parameters)
                    {
                        ITFunctionParameter prm = new ITFunctionParameter();
                        prm.Name = param.Identifier.Text;
                        prm.IsByRef = param.IsByRef;
                        param.ITObject = prm;
                        prm.Type = compiler.ResolveType(param.Type, body);
                        prm.Root = parentScope.Root;
                        prm.Location = param.Location;
                        body.Parameters.Add(prm);
                    }
                }

                return body;
            }

            public object Visit(CodeAnonymousFunctionExpression expr)
            {
                if (funBody == null)
                {
                    // anonymous function out of local scope, creating an anonymous member/global function
                    if (type == null)
                    {
                        // global func
                        ITFunctionEntity ent = new ITFunctionEntity();
                        ent.Body = CreateFunctionBody(expr, scope);
                        ent.Name = ent.Body.Name;
                        ent.IsPrivate = true;
                        ent.ParentScope = scope;

                        scope.AddChildEntity(ent);
                        compiler.CompileFunction(ent);

                        return new ITGlobalFunctionStorage()
                        {
                            ExpressionType = compiler.CreateFunctionTypeForGlobalFunction(ent, new ITType[] { }),
                            Function = ent,
                            Location = expr.Location
                        };
                    }
                    else
                    {
                        // member func
                        ITMemberFunction mem = new ITMemberFunction();
                        mem.Body = CreateFunctionBody(expr, scope);
                        mem.Name = mem.Body.Name;
                        mem.IsPrivate = true;
                        mem.Owner = type;
                        ((ITClassType)type).AddMemberFunction(mem);
                        compiler.CompileFunction(mem, type);

                        return new ITMemberFunctionStorage()
                        {
                            ExpressionType = compiler.CreateFunctionTypeForMemberFunction(mem, new ITType[] { }),
                            Function = mem,
                            GenericTypeParameters = new ITType[] { },
                            Location = expr.Location,
                            Object = new ITReferenceCalleeExpression()
                            {
                                ExpressionType = type,
                                Location = expr.Location
                            }
                        };
                    }
                }

                // anonymous function
                {
                    ITAnonymousFunctionBody anon = CreateFunctionBody(expr, funBody.Block.SurrogateClassEntity.Type);
                    anon.ParentFunctionBody = funBody;
                    anon.DeclaringBlock = currentBlock;

                    ITFunctionEntity ent = new ITFunctionEntity();
                    ent.Body = anon;
                    ent.Name = ent.Body.Name;
                    ent.IsPrivate = true;
                    ent.ParentScope = anon.ParentScope;

                    funBody.Block.SurrogateClassEntity.Type.AddChildEntity(ent);
                    ent.ParentScope = funBody.Block.SurrogateClassEntity.Type;
                    anon.functionEntity = ent;

                    // during compilation, this anonymous function might turn into member function...
                    compiler.CompileFunction(ent);

                    // beaware: when function is generic, surrogate class is generic, so using anon.(memberFunction|functionEntity)
                    // directly is bad; we have to get the instantiated entity/member function.
                    if (anon.CapturedVariable)
                    {
                        // now this anon-func is a member of the surrogate class of the current function.
                        ITMemberFunction mem = funBody.GetInstantiatedSurrogateClassType().GetMemberFunction(anon.memberFunction.Name);
                        return new ITMemberFunctionStorage()
                        {
                            ExpressionType = compiler.CreateFunctionTypeForMemberFunction(mem, new ITType[] { }),
                            Function = mem,
                            GenericTypeParameters = new ITType[] { },
                            Location = expr.Location,
                            Object = funBody.GetSurrogateClassInstance()
                        };
                    }
                    else
                    {
                        ent = (ITFunctionEntity)funBody.GetInstantiatedSurrogateClassType().GetChildEntity(anon.functionEntity.Name);
                        return new ITGlobalFunctionStorage()
                        {
                            ExpressionType = compiler.CreateFunctionTypeForGlobalFunction(ent, new ITType[] { }),
                            Function = ent,
                            Location = expr.Location
                        };

                    }
                }

            }
        }

        private ITErrorExpression CreateErrorExpression()
        {
            return CreateErrorExpression(GetPrimitiveType(ITPrimitiveTypeType.Integer));
        }
        private ITErrorExpression CreateErrorExpression(ITType type)
        {
            ITErrorExpression expr = new ITErrorExpression();
            expr.Root = root;
            expr.ExpressionType = type;
            return expr;
        }

        private ITExpression CompileExpression(CodeExpression expr, CompileFunctionContext context)
        {
            ITExpression ex = (ITExpression)(expr.Accept(context));
            return ex;
        }


    }
}
