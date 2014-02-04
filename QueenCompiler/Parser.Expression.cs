using System;
using System.Collections.Generic;
using System.Text;
using Queen.Language.CodeDom;
using System.Diagnostics;

namespace Queen.Language
{
    public sealed partial class Parser
    {
        private CodeExpression ParseExpression()
        {
            CodeExpression expr = ParseTernaryConditionExpression();
            if (expr == null)
                return null;
            while (true)
            {
                IgnoreSpacesExceptEOL();
                if (tokenizer.Current is SymbolToken)
                {
                    CodeBinaryOperatorType op; 
                    switch (((SymbolToken)tokenizer.Current).SymbolType)
                    {
                        case SymbolTokenType.AdditionAssign:
                            op = CodeBinaryOperatorType.AdditionAssign;
                            break;
                        case SymbolTokenType.SubtractAssign:
                            op = CodeBinaryOperatorType.SubtractionAssign;
                            break;
                        case SymbolTokenType.MultiplicationAssign:
                            op = CodeBinaryOperatorType.MultiplicationAssign;
                            break;
                        case SymbolTokenType.DivisionAssign:
                            op = CodeBinaryOperatorType.DivisionAssign;
                            break;
                        case SymbolTokenType.ConcatAssign:
                            op = CodeBinaryOperatorType.ConcatAssign;
                            break;
                        case SymbolTokenType.Swap:
                            op = CodeBinaryOperatorType.Swap;
                            break;
                        case SymbolTokenType.Assign:
                            op = CodeBinaryOperatorType.Assign;
                            break;
                        case SymbolTokenType.PowerAssign:
                            op = CodeBinaryOperatorType.PowerAssign;
                            break;
                        case SymbolTokenType.ModulusAssign:
                            op = CodeBinaryOperatorType.ModulusAssign;
                            break;
                        default:
                            return expr;
                    }

                    CodeBinaryOperatorExpression ex = new CodeBinaryOperatorExpression();
                    ex.Location = ConvertLocation(tokenizer.Current.Location);
                    expr.Parent = ex;
                    ex.Left = expr;
                    ex.Type = op;
                    tokenizer.MoveNext();
                    IgnoreSpacesExceptEOL();
                    ex.Right = ParseTernaryConditionExpression();
                    if (ex.Right == null) return null;
                    ex.Right.Parent = ex;
                    expr = ex;
                    continue;
                }
                break;
            }
            return expr;
        }

        private CodeExpression ParseTernaryConditionExpression()
        {
            CodeExpression expr = ParseOrExpression();
            if (expr == null)
                return null;
            IgnoreSpacesExceptEOL();
            if (tokenizer.Current is SymbolToken &&
                ((SymbolToken)tokenizer.Current).Equals("?"))
            {
                CodeTernaryConditionalExpression ex = new CodeTernaryConditionalExpression();
                ex.Condition = expr;
                ex.Location = ConvertLocation(tokenizer.Current.Location);
                expr.Parent = ex;
                tokenizer.MoveNext();
                IgnoreSpacesExceptEOL();
                if (!EnsureToken("("))
                {
                    return null;
                }
                tokenizer.MoveNext();
                IgnoreSpacesExceptEOL();
                ex.TrueValue = ParseTernaryConditionExpression();
                if (ex.TrueValue == null)
                    return null;
                IgnoreSpacesExceptEOL();
                if (!EnsureToken(","))
                {
                    // no separator
                    expr.Parent = null;
                    return expr;
                }
                tokenizer.MoveNext();
                ex.FalseValue = ParseTernaryConditionExpression();
                if (ex.FalseValue == null)
                    return null;
                ex.TrueValue.Parent = ex;
                ex.FalseValue.Parent = ex;
                IgnoreSpacesExceptEOL();
                if (!EnsureToken(")"))
                {
                    return null;
                }
                tokenizer.MoveNext();
                return ex;
            }
            else
            {
                return expr;
            }
        }

        private CodeExpression ParseOrExpression()
        {
            CodeExpression expr = ParseAndExpression();
            if (expr == null)
                return null;
            while (true)
            {
                IgnoreSpacesExceptEOL();
                if (tokenizer.Current is SymbolToken)
                {
                    CodeBinaryOperatorType op;
                    switch (((SymbolToken)tokenizer.Current).SymbolType)
                    {
                        case SymbolTokenType.Or:
                            op = CodeBinaryOperatorType.Or;
                            break;
                        default:
                            return expr;
                    }

                    CodeBinaryOperatorExpression ex = new CodeBinaryOperatorExpression();
                    ex.Location = ConvertLocation(tokenizer.Current.Location);
                    expr.Parent = ex;
                    ex.Left = expr;
                    ex.Type = op;
                    tokenizer.MoveNext();
                    IgnoreSpacesExceptEOL();
                    ex.Right = ParseAndExpression();
                    if (ex.Right == null)
                        return null;
                    ex.Right.Parent = ex;
                    expr = ex;
                    continue;
                }
                break;
            }
            return expr;
        }

        private CodeExpression ParseAndExpression()
        {
            CodeExpression expr = ParseComparasionExpression();
            if (expr == null)
                return null;
            while (true)
            {
                IgnoreSpacesExceptEOL();
                if (tokenizer.Current is SymbolToken)
                {
                    CodeBinaryOperatorType op;
                    switch (((SymbolToken)tokenizer.Current).SymbolType)
                    {
                        case SymbolTokenType.And:
                            op = CodeBinaryOperatorType.And;
                            break;
                        default:
                            return expr;
                    }

                    CodeBinaryOperatorExpression ex = new CodeBinaryOperatorExpression();
                    ex.Location = ConvertLocation(tokenizer.Current.Location);
                    expr.Parent = ex;
                    ex.Left = expr;
                    ex.Type = op;
                    tokenizer.MoveNext();
                    IgnoreSpacesExceptEOL();
                    ex.Right = ParseComparasionExpression();
                    if (ex.Right == null)
                        return null;
                    ex.Right.Parent = ex;
                    expr = ex;
                    continue;
                }
                break;
            }
            return expr;
        }

        private CodeExpression ParseComparasionExpression()
        {
            CodeExpression expr = ParseConcatExpression();
            if (expr == null)
                return null;
            while (true)
            {
                IgnoreSpacesExceptEOL();
                if (tokenizer.Current is SymbolToken)
                {
                    CodeBinaryOperatorType op;

                    switch (((SymbolToken)tokenizer.Current).SymbolType)
                    {
                        case SymbolTokenType.LessThan:
                            op = CodeBinaryOperatorType.LessThan;
                            break;
                        case SymbolTokenType.GreaterThan:
                            op = CodeBinaryOperatorType.GreaterThan;
                            break;
                        case SymbolTokenType.LessThanOrEqual:
                            op = CodeBinaryOperatorType.LessThanOrEqual;
                            break;
                        case SymbolTokenType.GreaterThanOrEqual:
                            op = CodeBinaryOperatorType.GreaterThanOrEqual;
                            break;
                        case SymbolTokenType.Equality:
                            op = CodeBinaryOperatorType.Equality;
                            break;
                        case SymbolTokenType.Inequality:
                            op = CodeBinaryOperatorType.Inequality;
                            break;
                        case SymbolTokenType.ReferenceEquality:
                            op = CodeBinaryOperatorType.ReferenceEquality;
                            break;
                        case SymbolTokenType.ReferenceInequality:
                            op = CodeBinaryOperatorType.ReferenceInequality;
                            break;
                        case SymbolTokenType.TypeEquality:
                            {
                                CodeTypeEqualityExpression ex = new CodeTypeEqualityExpression();
                                ex.Location = ConvertLocation(tokenizer.Current.Location);
                                expr.Parent = ex;
                                ex.Expression = expr;
                                tokenizer.MoveNext();
                                IgnoreSpacesExceptEOL();
                                ex.Type = ParseType();
                                if (ex.Type == null)
                                    return null;
                                ex.Type.Parent = ex;
                                expr = ex;
                                continue;
                            }
                        case SymbolTokenType.TypeInequality:
                            {
                                CodeTypeInequalityExpression ex = new CodeTypeInequalityExpression();
                                ex.Location = ConvertLocation(tokenizer.Current.Location);
                                expr.Parent = ex;
                                ex.Expression = expr;
                                tokenizer.MoveNext();
                                IgnoreSpacesExceptEOL();
                                ex.Type = ParseType();
                                if (ex.Type == null)
                                    return null;
                                ex.Type.Parent = ex;
                                expr = ex;
                                continue;
                            }
                        default:
                            return expr;
                    }

                    {
                        CodeBinaryOperatorExpression ex = new CodeBinaryOperatorExpression();
                        ex.Location = ConvertLocation(tokenizer.Current.Location);
                        expr.Parent = ex;
                        ex.Left = expr;
                        ex.Type = op;
                        tokenizer.MoveNext();
                        IgnoreSpacesExceptEOL();
                        ex.Right = ParseConcatExpression();
                        if (ex.Right == null)
                            return null;
                        ex.Right.Parent = ex;
                        expr = ex;
                    }
                    continue;
                }
                break;
            }
            return expr;
        }

        private CodeExpression ParseConcatExpression()
        {
            CodeExpression expr = ParseAddionSubtractionExpression();
            if (expr == null)
                return null;
            while (true)
            {
                IgnoreSpacesExceptEOL();
                if (tokenizer.Current is SymbolToken)
                {
                    CodeBinaryOperatorType op;
                    switch (((SymbolToken)tokenizer.Current).SymbolType)
                    {
                        case SymbolTokenType.Concat:
                            op = CodeBinaryOperatorType.Concat;
                            break;
                        default:
                            return expr;
                    }

                    CodeBinaryOperatorExpression ex = new CodeBinaryOperatorExpression();
                    ex.Location = ConvertLocation(tokenizer.Current.Location);
                    expr.Parent = ex;
                    ex.Left = expr;
                    ex.Type = op;
                    tokenizer.MoveNext();
                    IgnoreSpacesExceptEOL();
                    ex.Right = ParseAddionSubtractionExpression();
                    if (ex.Right == null)
                        return null;
                    ex.Right.Parent = ex;
                    expr = ex;
                    continue;
                }
                break;
            }
            return expr;
        }

        private CodeExpression ParseAddionSubtractionExpression()
        {
            CodeExpression expr = ParseMultiplicationDivisonExpression();
            if (expr == null)
                return null;
            while (true)
            {
                IgnoreSpacesExceptEOL();
                if (tokenizer.Current is SymbolToken)
                {
                    CodeBinaryOperatorType op;
                    switch (((SymbolToken)tokenizer.Current).SymbolType)
                    {
                        case SymbolTokenType.Add:
                            op = CodeBinaryOperatorType.Add;
                            break;
                        case SymbolTokenType.Subtract:
                            op = CodeBinaryOperatorType.Subtract;
                            break;
                        default:
                            return expr;
                    }

                    CodeBinaryOperatorExpression ex = new CodeBinaryOperatorExpression();
                    ex.Location = ConvertLocation(tokenizer.Current.Location);
                    expr.Parent = ex;
                    ex.Left = expr;
                    ex.Type = op;
                    tokenizer.MoveNext();
                    IgnoreSpacesExceptEOL();
                    ex.Right = ParseMultiplicationDivisonExpression();
                    if (ex.Right == null)
                        return null;
                    ex.Right.Parent = ex;
                    expr = ex;
                    continue;
                }
                break;
            }
            return expr;
        }
        private CodeExpression ParseMultiplicationDivisonExpression()
        {
            CodeExpression expr = ParsePowerExpression();
            if (expr == null)
                return null;
            while (true)
            {
                IgnoreSpacesExceptEOL();
                if (tokenizer.Current is SymbolToken)
                {
                    CodeBinaryOperatorType op;
                    switch (((SymbolToken)tokenizer.Current).SymbolType)
                    {
                        case SymbolTokenType.Multiply:
                            op = CodeBinaryOperatorType.Multiply;
                            break;
                        case SymbolTokenType.Divide:
                            op = CodeBinaryOperatorType.Divide;
                            break;
                        case SymbolTokenType.Modulus:
                            op = CodeBinaryOperatorType.Modulus;
                            break;
                        default:
                            return expr;
                    }

                    CodeBinaryOperatorExpression ex = new CodeBinaryOperatorExpression();
                    ex.Location = ConvertLocation(tokenizer.Current.Location);
                    expr.Parent = ex;
                    ex.Left = expr;
                    ex.Type = op;
                    tokenizer.MoveNext();
                    IgnoreSpacesExceptEOL();
                    ex.Right = ParsePowerExpression();
                    if (ex.Right == null)
                        return null;
                    ex.Right.Parent = ex;
                    expr = ex;
                    continue;
                }
                break;
            }
            return expr;
        }

        private CodeExpression ParsePowerExpression()
        {
            CodeExpression expr = ParseCastExpression();
            if (expr == null)
                return null;
            while (true)
            {
                IgnoreSpacesExceptEOL();
                if (tokenizer.Current is SymbolToken)
                {
                    CodeBinaryOperatorType op;
                    switch (((SymbolToken)tokenizer.Current).SymbolType)
                    {
                        case SymbolTokenType.Power:
                            op = CodeBinaryOperatorType.Power;
                            break;
                        default:
                            return expr;
                    }

                    CodeBinaryOperatorExpression ex = new CodeBinaryOperatorExpression();
                    ex.Location = ConvertLocation(tokenizer.Current.Location);
                    expr.Parent = ex;
                    ex.Left = expr;
                    ex.Type = op;
                    tokenizer.MoveNext();
                    IgnoreSpacesExceptEOL();
                    ex.Right = ParseCastExpression();
                    if (ex.Right == null)
                        return null;
                    ex.Right.Parent = ex;
                    expr = ex;
                    continue;
                }
                break;
            }
            return expr;
        }


        private CodeExpression ParseCastExpression()
        {
            CodeExpression expr = ParseUnaryOperatorExpression();
            if (expr == null)
                return null;
            while (true)
            {
                IgnoreSpacesExceptEOL();
                if (tokenizer.Current is SymbolToken &&
                    ((SymbolToken)tokenizer.Current).SymbolType == SymbolTokenType.Cast)
                {

                    CodeCastExpression ex = new CodeCastExpression();
                    ex.Location = ConvertLocation(tokenizer.Current.Location);
                    expr.Parent = ex;
                    ex.Expression = expr;
                    tokenizer.MoveNext();
                    IgnoreSpacesExceptEOL();
                    ex.Type = ParseType();
                    if (ex.Type == null)
                        return null;
                    ex.Type.Parent = ex;
                    expr = ex;
                    continue;
                }
                break;
            }
            return expr;
        }

        private CodeExpression ParseUnaryOperatorExpression()
        {
            if (tokenizer.Current is SymbolToken)
            {
                CodeUnaryOperatorType op;
                TokenLocation loc;
                switch (((SymbolToken)tokenizer.Current).SymbolType)
                {
                    case SymbolTokenType.Add:
                        op = CodeUnaryOperatorType.PassThrough;
                        break;
                    case SymbolTokenType.Subtract:
                        op = CodeUnaryOperatorType.Negate;
                        break;
                    case SymbolTokenType.Not:
                        op = CodeUnaryOperatorType.Not;
                        break;
                    case SymbolTokenType.Copy:
                        op = CodeUnaryOperatorType.Copy;
                        break;
                    case SymbolTokenType.Scope: // #
                        loc = tokenizer.Current.Location;
                        tokenizer.MoveNext();
                        if (tokenizer.Current.Equals("["))
                        {
                            // array
                            tokenizer.MoveNext();
                            IgnoreSpacesExceptEOL();
                            CodeArrayConstructExpression exp = new CodeArrayConstructExpression();
                            exp.Location = ConvertLocation(loc);
                            while (true)
                            {
                                if (tokenizer.Current.Equals("]"))
                                {
                                    break;
                                }
                                CodeExpression expr = ParseExpression();
                                if (expr == null)
                                    return null;

                                exp.NumElements.Add(expr);
                                expr.Parent = exp;
                                IgnoreSpacesExceptEOL();
                                if (tokenizer.Current.Equals(","))
                                {
                                    tokenizer.MoveNext();
                                    IgnoreSpacesExceptEOL();
                                    continue;
                                }
                                if (EnsureToken("]"))
                                {
                                    tokenizer.MoveNext();
                                }
                                break;
                            }
                            exp.ElementType = ParseType();
                            if (exp.ElementType == null)
                            {
                                return null;
                            }
                            exp.ElementType.Parent = exp;
                            return exp;
                        }
                        else
                        {
                            // type constructor;
                            IgnoreSpacesExceptEOL();

                            CodeClassConstructExpression exp = new CodeClassConstructExpression();
                            exp.Location = ConvertLocation(loc);
                            exp.Type = ParseType();
                            if (exp.Type == null)
                            {
                                return null;
                            }
                            exp.Type.Parent = exp;

                            // TODO: constructor parameters?

                            return exp;
                        }
                    default:
                        return ParseCallExpression();
                }

                CodeUnaryOperatorExpression ex = new CodeUnaryOperatorExpression();
                ex.Location = ConvertLocation(tokenizer.Current.Location);
                ex.Type = op;
                tokenizer.MoveNext();
                IgnoreSpacesExceptEOL();
                ex.Expression = ParseUnaryOperatorExpression();
                if (ex.Expression == null)
                    return null;
                ex.Expression.Parent = ex;
                return ex;
            }
            else
            {
                return ParseCallExpression();
            }
        }

        private CodeExpression ParseCallExpression()
        {
            CodeExpression expr = ParseValueExpression();
            if (expr == null)
                return null;
            while (true)
            {
                IgnoreSpacesExceptEOL();
                if (tokenizer.Current is SymbolToken)
                {
                    switch (((SymbolToken)tokenizer.Current).SymbolType)
                    {
                        case SymbolTokenType.MemberAccess:
                            {
                                CodeMemberAccessExpression ex = new CodeMemberAccessExpression();
                                ex.Location = ConvertLocation(tokenizer.Current.Location);
                                expr.Parent = ex;
                                ex.Expression = expr;
                                tokenizer.MoveNext();
                                IgnoreSpacesExceptEOL();
                                ex.MemberName = ParseIdentifier();
                                if (ex.MemberName == null)
                                    return null;
                                IgnoreSpacesExceptEOL();
                                if (tokenizer.Current.Equals("`"))
                                {
                                    tokenizer.MoveNext();
                                    IgnoreSpacesExceptEOL();
                                    EnsureToken("[");
                                    if (tokenizer.Current.Equals("["))
                                    {
                                        // multiple/parenthesized generic parameters
                                        tokenizer.MoveNext();
                                        IgnoreSpacesExceptEOL();
                                        if (tokenizer.Current.Equals("]"))
                                        {
                                            tokenizer.MoveNext();
                                            IgnoreSpacesExceptEOL();
                                        }
                                        else
                                        {
                                            do
                                            {
                                                CodeType typ = ParseType();
                                                if (typ == null)
                                                {
                                                    break;
                                                }
                                                ex.GenericTypeParameters.Add(typ);
                                                IgnoreSpacesExceptEOL();
                                                if (tokenizer.Current.Equals("]"))
                                                {
                                                    tokenizer.MoveNext();
                                                    break;
                                                }
                                                if (EnsureToken(","))
                                                {
                                                    tokenizer.MoveNext();
                                                    IgnoreSpacesExceptEOL();
                                                }
                                                else
                                                {
                                                    break;
                                                }
                                            } while (true);
                                        }
                                    }
                                    else
                                    {
                                        // single generic parameters
                                        CodeType typ = ParseType();
                                        if (typ == null)
                                        {
                                            break;
                                        }
                                        ex.GenericTypeParameters.Add(typ);
                                    }
                                }
                                expr = ex;
                            }
                            continue;
                        case SymbolTokenType.ParentheseOpen:
                             {
                                CodeInvocationExpression ex = new CodeInvocationExpression();
                                ex.Location = ConvertLocation(tokenizer.Current.Location);
                                expr.Parent = ex;
                                ex.Method = expr;
                                tokenizer.MoveNext();
                                IgnoreSpacesExceptEOL();
                                while (true)
                                {
                                    if (tokenizer.Current.Equals(")"))
                                    {
                                        tokenizer.MoveNext();
                                        break;
                                    }
                                    CodeInvocationParameter param = new CodeInvocationParameter();
                                    param.Parent = ex;
                                    if (tokenizer.Current.Equals("&"))
                                    {
                                        param.Location = ConvertLocation(tokenizer.Current.Location);
                                        tokenizer.MoveNext();
                                        IgnoreSpacesExceptEOL();
                                        param.ByRef = true;
                                        param.Value = ParseExpression();
                                        if (param.Value == null)
                                            return null;
                                    }
                                    else
                                    {
                                        param.Value = ParseExpression();
                                        if (param.Value == null)
                                            return null;
                                        param.Location = param.Value.Location;
                                    }
                                    param.Value.Parent = param;
                                    ex.Parameters.Add(param);
                                    IgnoreSpacesExceptEOL();
                                    if (tokenizer.Current.Equals(","))
                                    {
                                        tokenizer.MoveNext();
                                        IgnoreSpacesExceptEOL();
                                        continue;
                                    }
                                    if (EnsureToken(")"))
                                    {
                                        tokenizer.MoveNext();
                                    }
                                    
                                    break;
                                }
                                expr = ex;
                            }
                             continue;
                        case SymbolTokenType.SquareBracketOpen:
                             {
                                 CodeIndexExpression ex = new CodeIndexExpression();
                                 ex.Location = ConvertLocation(tokenizer.Current.Location);
                                 expr.Parent = ex;
                                 ex.Expression = expr;
                                 tokenizer.MoveNext();
                                 IgnoreSpacesExceptEOL();
                                 while (true)
                                 {
                                    if (tokenizer.Current.Equals("]"))
                                    {
                                        tokenizer.MoveNext();
                                        break;
                                    }
                                    CodeInvocationParameter param = new CodeInvocationParameter();
                                    param.Parent = ex;
                                    if (tokenizer.Current.Equals("&")) // byref index???
                                    {
                                        param.Location = ConvertLocation(tokenizer.Current.Location);
                                        tokenizer.MoveNext();
                                        IgnoreSpacesExceptEOL();
                                        param.Value = ParseExpression();
                                        if (param.Value == null)
                                            return null;
                                    }
                                    else
                                    {
                                        param.Value = ParseExpression();
                                        if (param.Value == null)
                                            return null;
                                        param.Location = param.Value.Location;
                                    }
                                    param.Value.Parent = param;
                                    ex.Parameters.Add(param);
                                    IgnoreSpacesExceptEOL();
                                    if (tokenizer.Current.Equals(","))
                                    {
                                        tokenizer.MoveNext();
                                        IgnoreSpacesExceptEOL();
                                        continue;
                                    }
                                    if (EnsureToken("]"))
                                    {
                                        tokenizer.MoveNext();
                                    }
                                    break;
                                }
                                 expr = ex;
                             }
                             continue;
                        default:
                            return expr;
                    }
                }
                break;
            }
            return expr;
        }

        private CodeExpression ParseAnonymousFunctionExpression()
        {
            Debug.Assert(tokenizer.Current is IdentifierToken);
            Debug.Assert(tokenizer.Current.Equals("func"));

            CodeAnonymousFunctionExpression func = new CodeAnonymousFunctionExpression();
            func.Location = ConvertLocation(tokenizer.Current.Location);
            tokenizer.MoveNext();
            
            // read parameters
            IgnoreSpacesExceptEOL();
            if (EnsureToken("("))
            {
                tokenizer.MoveNext();
                while (true)
                {
                    IgnoreSpacesExceptEOL();
                    if (tokenizer.Current.Equals(")"))
                    {
                        // end of parameter list
                        tokenizer.MoveNext();
                        break;
                    }

                    CodeParameterDeclaration param = ParseParameterDeclaration();
                    if (param != null)
                    {
                        func.Parameters.Add(param);
                    }

                    IgnoreSpacesExceptEOL();
                    if (tokenizer.Current.Equals(")"))
                    {
                        // end of parameter list
                        tokenizer.MoveNext();
                        break;
                    }

                    EnsureToken(",");
                    tokenizer.MoveNext();
                }
            }
            else
            {
                // no (); this is syntax error, but this is not so fatal
            }

            IgnoreSpacesExceptEOL();

            // return type?
            if (tokenizer.Current.Equals(":"))
            {
                tokenizer.MoveNext();
                func.ReturnType = ParseType();
                func.ReturnType.Parent = func;
            }

            IgnoreSpacesExceptEOL();
            EnsureLineBreak();
            tokenizer.MoveNext();

            // parse statements
            CodeBlock block = new CodeBlock();
            block.Location = func.Location;
            block.Parent = func;
            func.Statements = block;

            while (true)
            {
                IgnoreSpaces();

                Token token = tokenizer.Current;
                if (token is EndOfFileToken)
                {
                    ReportError(Properties.Resources.ParserFunctionUnexpectedEOF,
                        token.Location);
                    break;
                }

                if (HandleStatementInScope(block, true))
                {
                    continue;
                }

                if (token.Equals("end"))
                {
                    tokenizer.MoveNext();
                    IgnoreSpacesExceptEOL();
                    if (EnsureToken("func"))
                    {
                        tokenizer.MoveNext();
                        // no line break needed
                        return func;
                    }
                    else
                    {
                        tokenizer.MoveNext();
                        return func;
                    }
                }

                if (HandleStatementInBlock(block))
                {
                    continue;
                }

            }


            return func;
        }

        private CodeExpression ParseValueExpression()
        {
            Token token = tokenizer.Current;
            if (token is PrimitiveValueToken)
            {
                CodeValueExpression expr = new CodeValueExpression();
                expr.Location = ConvertLocation(token.Location);
                expr.Value = ((PrimitiveValueToken)token).Value;
                tokenizer.MoveNext();
                return expr;
            }

            if (token is SymbolToken)
            {
                SymbolToken sym = (SymbolToken)token;
                if (sym.SymbolType == SymbolTokenType.ParentheseOpen)
                {
                    // subexpression
                    tokenizer.MoveNext();
                    IgnoreSpacesExceptEOL();

                    CodeExpression expr = ParseExpression();
                    IgnoreSpacesExceptEOL();

                    if (EnsureToken(")"))
                    {
                        tokenizer.MoveNext();
                    }

                    return expr;
                }
                else if (sym.SymbolType == SymbolTokenType.SquareBracketOpen)
                {
                    // array literal
                    CodeArrayLiteralExpression arr = new CodeArrayLiteralExpression();
                    arr.Location = ConvertLocation(tokenizer.Current.Location);
                    tokenizer.MoveNext();
                    IgnoreSpacesExceptEOL();

                    while (true)
                    {
                        IgnoreSpacesExceptEOL();
                        if (tokenizer.Current.Equals("]"))
                        {
                            // end of elements
                            tokenizer.MoveNext();
                            break;
                        }

                        CodeExpression expr = ParseExpression();
                        if (expr != null)
                        {
                            expr.Parent = arr;
                            arr.Values.Add(expr);
                        }

                        IgnoreSpacesExceptEOL();
                        if (tokenizer.Current.Equals("]"))
                        {
                            // end of elements
                            tokenizer.MoveNext();
                            break;
                        }
                        if (EnsureToken(","))
                        {
                            tokenizer.MoveNext();
                        }
                    }

                    arr.ElementType = ParseType();
                    if(arr.ElementType != null)
                        arr.ElementType.Parent = arr.ElementType;
                    return arr;
                }
            }

            if (token.Equals("func"))
            {
                // anonymous function
                return ParseAnonymousFunctionExpression();
            }

            CodeEntitySpecifier spec = ParseEntitySpecifier(false, true);

            if (spec != null)
            {
                // check for built-in values
                if (spec is CodeImplicitEntitySpecifier)
                {
                    string txt = ((CodeImplicitEntitySpecifier)spec).Idenfitifer.Text;
                    CodeValueExpression expr = new CodeValueExpression();
                    expr.Location = ConvertLocation(token.Location);
                    switch (txt)
                    {
                        case "true":
                            expr.Value = true;
                            return expr;
                        case "false":
                            expr.Value = false;
                            return expr;
                        case "null":
                            expr.Value = null;
                            return expr;
                        case "inf":
                            expr.Value = double.PositiveInfinity;
                            return expr;
                    }
                }
                // not a built-in value
                {
                    CodeEntityExpression expr = new CodeEntityExpression();
                    expr.Entity = spec;
                    spec.Parent = expr;
                    expr.Location = spec.Location;
                    return expr;
                }
            }
            return null;
        }

    }
}
