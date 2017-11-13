using System;
using System.Collections.Generic;
using System.Text;
using Queen.Language.CodeDom;
using System.Diagnostics;

namespace Queen.Language
{
    public sealed partial class Parser
    {
        private Tokenizer tokenizer;
        private CodeSourceFile sourceFile;

        public event ParserErrorEventHandler ErrorReported;

        private CodeLocation ConvertLocation(TokenLocation loc)
        {
            return new CodeLocation
            {
                Line = loc.line,
                Column = loc.column,
                SourceFile = sourceFile
            };
        }

        private void ReportError(string message, TokenLocation loc)
        {
            ErrorReported(this, new ParserErrorEventArgs(
                loc.line, loc.column, message));
        }

        private void ReportError(string message, CodeLocation loc)
        {
            ErrorReported(this, new ParserErrorEventArgs(
                loc.Line, loc.Column, message));
        }

        private void NameConflict(CodeObject after, CodeObject before, string name)
        {
            ReportError(string.Format(Properties.Resources.ParserNameConfliction, name), after.Location);
            ReportError(string.Format(Properties.Resources.ParserNameConflictionPreviousDefinition), before.Location);
        }

        private void UnexpectedToken(Token token)
        {
            if (token is InvalidToken) // already reported
                return;
            ReportError(string.Format(Properties.Resources.ParserUnexpectedToken,
                token.Description), token.Location);
        }

        private bool EnsureToken(string text)
        {
            Token token = tokenizer.Current;
            if (token.Equals(text))
            {
                return true;
            }
            else
            {
                UnexpectedToken(token);
                return false;
            }
        }

        private bool EnsureLineBreak()
        {
            IgnoreSpacesExceptEOL();
            Token token = tokenizer.Current;
            if (token is EndOfLineToken ||
                token is EndOfFileToken)
            {
                return true;
            }
            else
            {
                UnexpectedToken(token);
                return false;
            }
        }

        private bool IsReservedWord(string text, bool allowType = false,
            bool allowValue = false)
        {
            switch (text)
            {
                case "byte8": case "byte16":  case "byte32": case "byte64":
                case "dict": case "bool": case "float": case "func": case "int":
                case "list": case "queue": case "stack": case "iter":
                case "int8": case "int16": case "int32": case "int64":
                case "uint8": case "uint16": case "uint32": case "uint64":
                case "string":
                    return !allowType;

                case "inf": case "null": case "this": case "true": case "false":
                    return !allowValue;

                case "assert": case "block": case "break": 
                case "do": case "elif": case "else": case "end":
                case "enum": case "finally": case "for":
                case "foreach": case "if": case "ifdef": 
                case "return": case "rls": case "skip":
                case "switch": case "throw": case "to":
                case "try": case "var": case "while":
                case "catch": case "ctor": case "dtor":
                case "const": case "class": case "interface":
                    return true;
                default:
                    return false;
            }
        }

        private void OnTokenizerError(object sender,
            TokenizerErrorEventArgs e)
        {
            ErrorReported(this, new ParserErrorEventArgs(
                e.Line, e.Column, e.Message));
        }

        private void IgnoreComments()
        {
            while (tokenizer.Current is CommentToken)
            {
                tokenizer.MoveNext();
            }
        }

        private void IgnoreSpacesExceptEOL()
        {
            while (tokenizer.Current is CommentToken)
            {
                tokenizer.MoveNext();
            }
        }

        private void IgnoreSpaces()
        {
            while (tokenizer.Current is CommentToken ||
                tokenizer.Current is EndOfLineToken)
            {
                tokenizer.MoveNext();
            }
        }

        private void SkipToNextLine()
        {
            while (!(tokenizer.Current is EndOfLineToken) &&
                !(tokenizer.Current is EndOfFileToken))
            {
                tokenizer.MoveNext();
            }
            tokenizer.MoveNext();
        }

        private CodeIdentifier ParseIdentifier()
        {
            Token token = tokenizer.Current;
            tokenizer.MoveNext();
            if (token is IdentifierToken)
            {
                CodeIdentifier self = new CodeIdentifier();
                self.Location = ConvertLocation(token.Location);
                self.Text = token.ToString();
                return self;
            }
            else
            {
                UnexpectedToken(token);
                return null;
            }
        }

        private CodeRange ParseRange()
        {
            CodeRange range = new CodeRange();
            range.LowerBound = ParseExpression();
            if (range.LowerBound == null)
            {
                return null;
            }
            range.Location = range.LowerBound.Location;
            range.LowerBound.Parent = range;

            IgnoreSpacesExceptEOL();
            if (tokenizer.Current.Equals("to"))
            {
                tokenizer.MoveNext();
                IgnoreSpacesExceptEOL();
                range.UpperBound = ParseExpression();
            }
            return range;
        }

        private CodeEntitySpecifier ParseGenericEntirySpecifier(CodeEntitySpecifier genEnt)
        {
            IgnoreSpacesExceptEOL();
            CodeGenericsEntitySpecifier gen = null;
            if (tokenizer.Current.Equals("`"))
            {
                // generic
                List<CodeType> types = new List<CodeType>();
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
                            types.Add(typ);
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
                    if(typ != null)
                        types.Add(typ);
                }

                if(types.Count == 0) return genEnt;

                gen = new CodeGenericsEntitySpecifier();
                gen.GenericEntity = genEnt;
                genEnt.Parent = gen;
                gen.GenericParameters = types;
                gen.Location = types[0].Location;
                return gen;
            }
            else
            {
                return genEnt;
            }
        }

        private CodeEntitySpecifier ParseEntitySpecifier(bool allowBuiltinType = true, bool allowBuiltinValue = false)
        {
            
            CodeEntitySpecifier spec = null;
            CodeIdentifier idt;

            if (tokenizer.Current.Equals("@"))
            {
                // root global scope
                // global scope
                CodeGlobalScopeSpecifier sp = new CodeGlobalScopeSpecifier();
                sp.Location = ConvertLocation(tokenizer.Current.Location);
                sp.Identifier = null;
                spec = sp;
                tokenizer.MoveNext();
            }
            else
            {
                idt = ParseIdentifier();
                if (idt == null)
                {
                    return null;
                }
                IgnoreSpacesExceptEOL();
                if (tokenizer.Current.Equals("@"))
                {
                    // global scope
                    CodeGlobalScopeSpecifier sp = new CodeGlobalScopeSpecifier();
                    sp.Location = idt.Location;
                    sp.Identifier = idt;
                    idt.Parent = sp;
                    spec = sp;
                    tokenizer.MoveNext();
                    if (IsReservedWord(idt.Text))
                    {
                        ReportError(string.Format(Properties.Resources.ParserReservedIdentifier, idt.Text),
                        idt.Location);
                    }
                }
                else
                {
                    CodeImplicitEntitySpecifier sp = new CodeImplicitEntitySpecifier();
                    sp.Location = idt.Location;
                    sp.Idenfitifer = idt;
                    idt.Parent = sp;
                    spec = sp;

                    if (IsReservedWord(idt.Text, allowBuiltinType,
                        allowBuiltinValue))
                    {
                        ReportError(string.Format(Properties.Resources.ParserReservedIdentifier, idt.Text),
                        idt.Location);
                    }

                    spec = ParseGenericEntirySpecifier(spec);

                    IgnoreSpacesExceptEOL();
                    if (!tokenizer.Current.Equals("#"))
                    {
                        return spec;
                    }

                    tokenizer.MoveNext();

                }
            }

            while (true)
            {
                IgnoreSpacesExceptEOL();
                idt = ParseIdentifier();
                if (idt == null)
                {
                    return null;
                }

                CodeScopedEntitySpecifier sp = new CodeScopedEntitySpecifier();
                sp.ParentEntity = spec;
                spec.Parent = sp;
                sp.Identifier = idt;
                sp.Location = sp.Identifier.Location;

                if (IsReservedWord(idt.Text))
                {
                    ReportError(string.Format(Properties.Resources.ParserReservedIdentifier, idt.Text),
                        idt.Location);
                }

                spec = sp;
                spec = ParseGenericEntirySpecifier(spec);

                IgnoreSpacesExceptEOL();
                if (!tokenizer.Current.Equals("#"))
                {
                    break;
                }


                tokenizer.MoveNext();
            }

            return spec;
        }

        private CodeParameterDeclaration ParseParameterDeclaration(bool isNameless = false)
        {
            CodeParameterDeclaration param = new CodeParameterDeclaration();
            if (!isNameless)
            {
                param.Identifier = ParseIdentifier();

                IgnoreSpacesExceptEOL();
                EnsureToken(":");
                tokenizer.MoveNext();
            }

            IgnoreSpacesExceptEOL();
            if (tokenizer.Current.Equals("&"))
            {
                param.IsByRef = true;
                tokenizer.MoveNext();
            }

            IgnoreSpacesExceptEOL();
            param.Type = ParseType();

            return param;
        }

        private CodeType ParseType()
        {
            Token token = tokenizer.Current;
            if (token.Equals("["))
            {
                // array
                int dims = 1;
                tokenizer.MoveNext();
                IgnoreSpacesExceptEOL();
                while (tokenizer.Current.Equals(","))
                {
                    dims++;
                    tokenizer.MoveNext();
                    IgnoreSpacesExceptEOL();
                }
                EnsureToken("]");
                tokenizer.MoveNext();
                IgnoreSpacesExceptEOL();

                CodeArrayType type = new CodeArrayType();

                type.Dimensions = dims;
                type.Location = ConvertLocation(token.Location);
                type.ElementType = ParseType();
                if(type.ElementType != null)
                    type.ElementType.Parent = type;
                return type;
            }
            else
            {
                CodeEntitySpecifier spec = ParseEntitySpecifier();
                if (spec == null)
                {
                    return null;
                }

                CodeImplicitEntitySpecifier implSpec = spec as CodeImplicitEntitySpecifier;

                IgnoreSpacesExceptEOL();
                // check function type
                if (implSpec != null &&
                    implSpec.Idenfitifer.Text.Equals("func"))
                {
                    CodeFunctionType typ = new CodeFunctionType();
                    typ.Location = spec.Location;
                    if (!EnsureToken("<"))
                        return null;
                    tokenizer.MoveNext();
                    if (!EnsureToken("("))
                        return null;
                    tokenizer.MoveNext();
                    typ.Parameters = new List<CodeParameterDeclaration>();
                    while (true)
                    {
                        IgnoreSpacesExceptEOL();
                        if (tokenizer.Current.Equals(")"))
                        {
                            // end of parameter list
                            tokenizer.MoveNext();
                            break;
                        }

                        CodeParameterDeclaration param = ParseParameterDeclaration(true);
                        if (param != null)
                        {
                            typ.Parameters.Add(param);
                            param.Parent = typ;
                        }

                        IgnoreSpacesExceptEOL();
                        if (tokenizer.Current.Equals(")"))
                        {
                            // end of parameter list
                            tokenizer.MoveNext();
                            break;
                        }

                        if (!EnsureToken(","))
                            return null;
                        tokenizer.MoveNext();
                    }

                    // return type?
                    if (tokenizer.Current.Equals(":"))
                    {
                        tokenizer.MoveNext();
                        typ.ReturnType = ParseType();
                        if(typ.ReturnType != null)
                            typ.ReturnType.Parent = typ;
                    }

                    IgnoreSpacesExceptEOL();
                    if (EnsureToken(">"))
                    {
                        tokenizer.MoveNext();
                    }

                    return typ;
                }
                else
                {
                    CodeEntityType typ = new CodeEntityType();
                    typ.Entity = spec;
                    spec.Parent = typ;
                    typ.Location = spec.Location;

                    // compatible generics parameter syntax
                    if (implSpec != null &&
                        (implSpec.Idenfitifer.Text.Equals("list") ||
                        implSpec.Idenfitifer.Text.Equals("dict") ||
                        implSpec.Idenfitifer.Text.Equals("queue") ||
                        implSpec.Idenfitifer.Text.Equals("iter") ||
                        implSpec.Idenfitifer.Text.Equals("stack")))
                    {
                        if (tokenizer.Current.Equals("<"))
                        {
                            CodeGenericsEntitySpecifier gen = new CodeGenericsEntitySpecifier();
                            gen.Location = spec.Location;
                            gen.GenericEntity = spec;

                            // template parameters
                            tokenizer.MoveNext();
                            while (true)
                            {
                                IgnoreSpacesExceptEOL();
                                if (tokenizer.Current.Equals(">"))
                                {
                                    // end of generic parameter list
                                    tokenizer.MoveNext();
                                    break;
                                }

                                CodeType type = ParseType();
                                if (type == null)
                                    return null;
                                gen.GenericParameters.Add(type);
                                type.Parent = typ;

                                IgnoreSpacesExceptEOL();
                                if (tokenizer.Current.Equals(">"))
                                {
                                    // end of generic parameter list
                                    tokenizer.MoveNext();
                                    break;
                                }
                                if (!EnsureToken(","))
                                    return null;
                                tokenizer.MoveNext();
                            }
                            /*
                            if (spec is CodeGenericsEntitySpecifier)
                            {
                                ReportError(Properties.Resources.ParserMixedGenericParameters, spec.Location);
                            }
                            else
                            {*/
                                spec.Parent = gen;
                                typ.Entity = gen;
                            //}
                        }
                    }
                    return typ;
                }
            }
        }

       

        private bool HandleStatementInBlock(CodeBlock block)
        {
            Token token = tokenizer.Current;

            if (token is IdentifierToken)
            {
                string text = ((IdentifierToken)token).Text;

                // TODO: return, break, continue, ...
                switch (text)
                {
                    case "if":
                        {
                            CodeIfStatement stat = ParseIfStatement();
                            if (stat != null)
                            {
                                stat.Parent = block;
                                block.Statements.Add(stat);
                            }
                        }
                        return true;
                    case "ifdef":
                        {
                            CodeIfDefStatement stat = ParseIfDefStatement();
                            if (stat != null)
                            {
                                stat.Parent = block;
                                block.Statements.Add(stat);
                            }
                        }
                        return true;
                    case "while":
                        {
                            CodeWhileStatement stat = ParseWhileStatement();
                            if (stat != null)
                            {
                                stat.Parent = block;
                                block.Statements.Add(stat);
                            }
                        }
                        return true;
                    case "for":
                        {
                            CodeForStatement stat = ParseForStatement();
                            if (stat != null)
                            {
                                stat.Parent = block;
                                block.Statements.Add(stat);
                            }
                        }
                        return true;
                    case "block":
                        {
                            CodeBlockStatement stat = ParseBlockStatement();
                            if (stat != null)
                            {
                                stat.Parent = block;
                                block.Statements.Add(stat);
                            }
                        }
                        return true;
                    case "foreach":
                        {
                            CodeForEachStatement stat = ParseForEachStatement();
                            if (stat != null)
                            {
                                stat.Parent = block;
                                block.Statements.Add(stat);
                            }
                        }
                        return true;
                    case "switch":
                        {
                            CodeSwitchStatement stat = ParseSwitchStatement();
                            if (stat != null)
                            {
                                stat.Parent = block;
                                block.Statements.Add(stat);
                            }
                        }
                        return true;
                    case "try":
                        {
                            CodeTryStatement stat = ParseTryStatement();
                            if (stat != null)
                            {
                                stat.Parent = block;
                                block.Statements.Add(stat);
                            }
                        }
                        return true;
                    case "throw":
                        {
                            CodeThrowStatement stat = ParseThrowStatement();
                            if (stat != null)
                            {
                                stat.Parent = block;
                                block.Statements.Add(stat);
                            }
                        }
                        return true;
                    case "assert":
                        {
                            CodeAssertStatement stat = ParseAssertStatement();
                            if (stat != null)
                            {
                                stat.Parent = block;
                                block.Statements.Add(stat);
                            }
                        }
                        return true;
                    case "do":
                        {
                            tokenizer.MoveNext();
                            IgnoreSpacesExceptEOL();

                            // may be expression
                            CodeExpression expr = ParseExpression();
                            if (expr != null)
                            {
                                CodeExpressionStatement stat = new CodeExpressionStatement();
                                stat.Expression = expr;
                                expr.Parent = stat;
                                stat.Location = expr.Location;
                                stat.Parent = block;
                                block.Statements.Add(stat);
                                return true;
                            }
                        }
                        return true;
                    case "return":
                        {
                            CodeStatement stat = ParseReturnStatement();
                            if (stat != null)
                            {
                                stat.Parent = block;
                                block.Statements.Add(stat);
                            }
                            return true;
                        }
                    case "continue":
                        {
                            CodeStatement stat = ParseContinueStatement();
                            if (stat != null)
                            {
                                stat.Parent = block;
                                block.Statements.Add(stat);
                            }
                            return true;
                        }
                    case "break":
                        {
                            CodeStatement stat = ParseBreakStatement();
                            if (stat != null)
                            {
                                stat.Parent = block;
                                block.Statements.Add(stat);
                            }
                            return true;
                        }
                    case "var":
                        {
                            CodeStatement stat = ParseVariableDeclerationStatement();
                            if (stat != null)
                            {
                                stat.Parent = block;
                                block.Statements.Add(stat);
                            }
                            return true;
                        }
                    case "const":
                        {
                            CodeStatement stat = ParseVariableDeclerationStatement();
                            if (stat != null)
                            {
                                stat.Parent = block;
                                block.Statements.Add(stat);
                            }
                            return true;
                        }
                }

                if (IsReservedWord(text, false, true) && !text.Equals("func")) // might be an anonymous function, so allow "func"
                {
                    ReportError(string.Format(Properties.Resources.ParserReservedIdentifier, text),
                        token.Location);
                    SkipToNextLine();
                    return true;
                }
            }

            // may be expression
            {
                CodeExpression expr = ParseExpression();
                if (expr != null)
                {
                    CodeExpressionStatement stat = new CodeExpressionStatement();
                    stat.Expression = expr;
                    expr.Parent = stat;
                    stat.Location = expr.Location;
                    stat.Parent = block;
                    block.Statements.Add(stat);
                    return true;
                }
            }

            IgnoreSpacesExceptEOL();
            if (EnsureLineBreak())
            {
                tokenizer.MoveNext();
            }
            else
            {
                SkipToNextLine();
            }

            return false;
        }

        private bool HandleStatementInScope(CodeScope scope, bool localScope, bool isInterface = false, bool isClass = false)
        {

            bool isPrivate = false;
            bool isOverride = false;
            TokenLocation accessModifierLoc = new TokenLocation();

            if ((!localScope) && (!isInterface))
            {
                if (tokenizer.Current.Equals("-"))
                {
                    isPrivate = true;
                    accessModifierLoc = tokenizer.Current.Location;
                    tokenizer.MoveNext();
                }
                if (tokenizer.Current.Equals("+"))
                {
                    isOverride = true;
                    accessModifierLoc = tokenizer.Current.Location;
                    tokenizer.MoveNext();
                }
                while (tokenizer.Current.Equals("-") || tokenizer.Current.Equals("+"))
                {
                    ReportError(Properties.Resources.ParserMalformedAccessModifier, tokenizer.Current.Location);
                    tokenizer.MoveNext();
                }
            }
                
            Token token = tokenizer.Current;
            if (token is IdentifierToken)
            {
                string text = ((IdentifierToken)token).Text;
                switch (text)
                {
                    case "func":
                        {
                            if (localScope)
                            {
                                if (isOverride || isPrivate)
                                {
                                    ReportError(Properties.Resources.ParserInvalidAccessModifier, accessModifierLoc);
                                }
                            }
                            CodeFunctionStatement stat = ParseFunctionStatement(isInterface, null,
                                localScope);
                            if (stat == anonymousFunctionConstant)
                            {
                                // this is anonymous function, thus this is a part of an expression.
                                return false;
                            }
                            if (stat != null)
                            {
                                stat.Parent = scope;
                                if (scope.Children.ContainsKey(stat.Name.Text))
                                {
                                    NameConflict(stat, scope.Children[stat.Name.Text], stat.Name.Text);
                                }
                                stat.IsPrivate = isPrivate;
                                stat.IsOverride = isOverride;
                                scope.Children[stat.Name.Text] = stat;
                            }
                            return true;
                        }
                    case "ctor":
                        {
                            if (localScope)
                            {
                                if (isOverride || isPrivate)
                                {
                                    ReportError(Properties.Resources.ParserInvalidAccessModifier, accessModifierLoc);
                                }
                            }
                            if (!isClass)
                            {
                                ReportError(Properties.Resources.ParserConstructorInNonClass, tokenizer.Current.Location);
                            }
                            CodeFunctionStatement stat = ParseFunctionStatement(isInterface, SpecialFunctionType.Constructor);
                            if (stat != null)
                            {
                                stat.Parent = scope;
                                if (scope.Children.ContainsKey(stat.Name.Text))
                                {
                                    NameConflict(stat, scope.Children[stat.Name.Text], stat.Name.Text);
                                }
                                stat.IsPrivate = isPrivate;
                                stat.IsOverride = isOverride;
                                scope.Children[stat.Name.Text] = stat;
                            }
                            return true;
                        }
                    case "dtor":
                        {
                            if (localScope)
                            {
                                if (isOverride || isPrivate)
                                {
                                    ReportError(Properties.Resources.ParserInvalidAccessModifier, accessModifierLoc);
                                }
                            }
                            if (!isClass)
                            {
                                ReportError(Properties.Resources.ParserConstructorInNonClass, tokenizer.Current.Location);
                            }
                            CodeFunctionStatement stat = ParseFunctionStatement(isInterface, SpecialFunctionType.Destructor);
                            if (stat != null)
                            {
                                stat.Parent = scope;
                                if (scope.Children.ContainsKey(stat.Name.Text))
                                {
                                    NameConflict(stat, scope.Children[stat.Name.Text], stat.Name.Text);
                                }
                                stat.IsPrivate = isPrivate;
                                stat.IsOverride = isOverride;
                                scope.Children[stat.Name.Text] = stat;
                            }
                            return true;
                        }
                    case "class":
                    case "interface":
                        {
                            if (isOverride || isPrivate) // FIXME: private class should be supported?
                            {
                                ReportError(Properties.Resources.ParserInvalidAccessModifier, accessModifierLoc);
                            }
                            CodeClassStatement stat = ParseClassStatement();
                            if (stat != null)
                            {
                                stat.Parent = scope;
                                if (scope.Children.ContainsKey(stat.Name.Text))
                                {
                                    NameConflict(stat, scope.Children[stat.Name.Text], stat.Name.Text);
                                }
                                scope.Children[stat.Name.Text] = stat;
                            }
                            return true;
                        }
                    case "enum":
                        {
                            if (isOverride || isPrivate) // FIXME: private class should be supported?
                            {
                                ReportError(Properties.Resources.ParserInvalidAccessModifier, accessModifierLoc);
                            }
                            CodeEnumStatement stat = ParseEnumStatement();
                            if (stat != null)
                            {
                                stat.Parent = scope;
                                if (scope.Children.ContainsKey(stat.Name.Text))
                                {
                                    NameConflict(stat, scope.Children[stat.Name.Text], stat.Name.Text);
                                }
                                scope.Children[stat.Name.Text] = stat;
                            }
                            return true;
                        }
                    case "var":
                        if (localScope)
                            return false;
                        {
                            if (localScope && isPrivate)
                            {
                                ReportError(Properties.Resources.ParserInvalidAccessModifier, accessModifierLoc);
                            }
                            CodeVariableDeclarationStatement stat = ParseVariableDeclerationStatement();
                            if (stat != null)
                            {
                                stat.Parent = scope;
                                if (scope.Children.ContainsKey(stat.Identifier.Text))
                                {
                                    NameConflict(stat, scope.Children[stat.Identifier.Text], stat.Identifier.Text);
                                }
                                stat.IsPrivate = isPrivate;
                                scope.Children[stat.Identifier.Text] = stat;
                            }
                            return true;
                        }
                    case "const":
                        if (localScope)
                            return false;
                        {
                            if (localScope && isPrivate)
                            {
                                ReportError(Properties.Resources.ParserInvalidAccessModifier, accessModifierLoc);
                            }
                            CodeVariableDeclarationStatement stat = ParseVariableDeclerationStatement();
                            if (stat != null)
                            {
                                stat.Parent = scope;
                                if (scope.Children.ContainsKey(stat.Identifier.Text))
                                {
                                    NameConflict(stat, scope.Children[stat.Identifier.Text], stat.Identifier.Text);
                                }
                                stat.IsPrivate = isPrivate;
                                scope.Children[stat.Identifier.Text] = stat;
                            }
                            return true;
                        }
                }
            }

            if (isOverride || isPrivate)
            {
                ReportError(Properties.Resources.ParserEntityDeclarationExpected, tokenizer.Current.Location);
            }

            return false;
        }

        private CodeVariableDeclarationStatement ParseVariableDeclerationStatement()
        {
            Debug.Assert(tokenizer.Current is IdentifierToken);
            bool IsConst = tokenizer.Current.Equals("const");
            if(!IsConst)
                Debug.Assert(tokenizer.Current.Equals("var"));

            CodeVariableDeclarationStatement stat = new CodeVariableDeclarationStatement();
            stat.Location = ConvertLocation(tokenizer.Current.Location);
            stat.IsConst = IsConst;
            tokenizer.MoveNext();

            IgnoreSpacesExceptEOL();
            stat.Identifier = ParseIdentifier();

            if (stat.Identifier != null)
            {
                stat.Identifier.Parent = stat;
                if (stat.Identifier.Text == "Ctor" ||
                    stat.Identifier.Text == "Dtor")
                {
                    ReportError(string.Format(Properties.Resources.ParserReservedIdentifier, stat.Identifier.Text),
                        stat.Identifier.Location);
                }
            }

            IgnoreSpacesExceptEOL();
            if (tokenizer.Current.Equals(":"))
            {
                tokenizer.MoveNext();
                IgnoreSpacesExceptEOL();

                stat.Type = ParseType();
                IgnoreSpacesExceptEOL();
            }

            // type specification can be omit

            if (tokenizer.Current.Equals("::"))
            {
                // initial value
                tokenizer.MoveNext();
                IgnoreSpacesExceptEOL();
                stat.InitialValue = ParseExpression();
            }
            else
            {
                if (IsConst)
                {
                    ReportError(Properties.Resources.ParserConstantWithoutValue, tokenizer.Current.Location);
                }
            }

            if (EnsureLineBreak())
            {
                tokenizer.MoveNext();
            }

            return stat;
        }

        private CodeBreakStatement ParseBreakStatement()
        {
            Debug.Assert(tokenizer.Current is IdentifierToken);
            Debug.Assert(tokenizer.Current.Equals("break"));

            CodeBreakStatement stat = new CodeBreakStatement();
            stat.Location = ConvertLocation(tokenizer.Current.Location);
            tokenizer.MoveNext();

            IgnoreSpacesExceptEOL();
            stat.Name = ParseIdentifier();

            if (EnsureLineBreak())
            {
                tokenizer.MoveNext();
            }
            return stat;
        }
        private CodeContinueStatement ParseContinueStatement()
        {
            Debug.Assert(tokenizer.Current is IdentifierToken);
            Debug.Assert(tokenizer.Current.Equals("continue"));

            CodeContinueStatement stat = new CodeContinueStatement();
            stat.Location = ConvertLocation(tokenizer.Current.Location);
            tokenizer.MoveNext();

            IgnoreSpacesExceptEOL();
            stat.Name = ParseIdentifier();

            if (EnsureLineBreak())
            {
                tokenizer.MoveNext();
            }
            return stat;
        }
        private CodeReturnStatement ParseReturnStatement()
        {
            Debug.Assert(tokenizer.Current is IdentifierToken);
            Debug.Assert(tokenizer.Current.Equals("return"));

            CodeReturnStatement stat = new CodeReturnStatement();
            stat.Location = ConvertLocation(tokenizer.Current.Location);
            tokenizer.MoveNext();

            IgnoreSpacesExceptEOL();
            if (tokenizer.Current is EndOfFileToken ||
                tokenizer.Current is EndOfLineToken)
            {
                if (EnsureLineBreak())
                {
                    tokenizer.MoveNext();
                }
                return stat;
            }
            stat.ReturnedValue = ParseExpression();

            IgnoreSpacesExceptEOL();
            if (EnsureLineBreak())
            {
                tokenizer.MoveNext();
            }
            return stat;
        }
        private CodeAssertStatement ParseAssertStatement()
        {
            Debug.Assert(tokenizer.Current is IdentifierToken);
            Debug.Assert(tokenizer.Current.Equals("assert"));

            CodeAssertStatement stat = new CodeAssertStatement();
            stat.Location = ConvertLocation(tokenizer.Current.Location);
            tokenizer.MoveNext();

            IgnoreSpacesExceptEOL();
            stat.Condition = ParseExpression();

            IgnoreSpacesExceptEOL();
            if (EnsureLineBreak())
            {
                tokenizer.MoveNext();
            }
            return stat;
        }

        private CodeThrowStatement ParseThrowStatement()
        {
            Debug.Assert(tokenizer.Current is IdentifierToken);
            Debug.Assert(tokenizer.Current.Equals("throw"));

            CodeThrowStatement stat = new CodeThrowStatement();
            stat.Location = ConvertLocation(tokenizer.Current.Location);
            tokenizer.MoveNext();

            IgnoreSpacesExceptEOL();
            stat.FirstParameter = ParseExpression();
            IgnoreSpacesExceptEOL();

            if (tokenizer.Current.Equals(","))
            {
                tokenizer.MoveNext();
                IgnoreSpacesExceptEOL();
                stat.SecondParameter = ParseExpression();
                IgnoreSpacesExceptEOL();
            }

            if (EnsureLineBreak())
            {
                tokenizer.MoveNext();
            }
            return stat;
        }

        private CodeIfStatement ParseIfStatement()
        {
            Debug.Assert(tokenizer.Current is IdentifierToken);
            Debug.Assert(tokenizer.Current.Equals("if"));

            CodeIfStatement stat = new CodeIfStatement();
            stat.Location = ConvertLocation(tokenizer.Current.Location);
            tokenizer.MoveNext();

            IgnoreSpacesExceptEOL();

            // maybe has block name?
            if (tokenizer.Current is IdentifierToken)
            {
                stat.Name = ParseIdentifier();
                if (IsReservedWord(stat.Name.Text))
                {
                    ReportError(string.Format(Properties.Resources.ParserReservedIdentifier,
                        stat.Name.Text), stat.Location);
                    stat.Name = null;
                }
                if (stat.Name != null)
                {
                    stat.Name.Parent = stat;
                }
                IgnoreSpacesExceptEOL();
            }

            // first condition

            if (!EnsureToken("("))
            {
                SkipToNextLine();
                return null;
            }
            tokenizer.MoveNext();

            CodeIfCondition cond = new CodeIfCondition();
            stat.Conditions.Add(cond);
            cond.Parent = stat;
            cond.Condition = ParseExpression();
            cond.Location = stat.Location;
            IgnoreSpacesExceptEOL();
            if (!EnsureToken(")"))
            {
                SkipToNextLine();
                return null;
            }
            tokenizer.MoveNext();
            IgnoreSpacesExceptEOL();
            if (EnsureLineBreak())
            {
                tokenizer.MoveNext();
            }

            CodeBlock block = new CodeBlock();
            cond.Statements = block;
            block.Location = cond.Location;
            block.Parent = cond;
            block.Name = stat.Name;

            while (true)
            {
                IgnoreSpaces();

                Token token = tokenizer.Current;
                if (token is EndOfFileToken)
                {
                    ReportError(Properties.Resources.ParserIfUnexpectedEOF,
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
                    if (EnsureToken("if"))
                    {
                        tokenizer.MoveNext();
                        if (EnsureLineBreak())
                        {
                            tokenizer.MoveNext();
                        }
                        else
                        {
                            SkipToNextLine();
                        }
                        return stat;
                    }
                    else
                    {
                        SkipToNextLine();
                        return null;
                    }
                }
                else if (token.Equals("elif"))
                {
                    // make sure we haven't found "else"
                    if (cond == null)
                    {
                        ReportError(Properties.Resources.ParserElifAfterElse, token.Location);
                        SkipToNextLine();
                        return null;
                    }
                    tokenizer.MoveNext();
                    if (!EnsureToken("("))
                    {
                        SkipToNextLine();
                        return null;
                    }
                    tokenizer.MoveNext();

                    cond = new CodeIfCondition();
                    stat.Conditions.Add(cond);
                    cond.Parent = stat;
                    cond.Condition = ParseExpression();
                    cond.Location = ConvertLocation(token.Location);

                    block = new CodeBlock();
                    cond.Statements = block;
                    block.Location = cond.Location;
                    block.Parent = cond;
                    block.Name = stat.Name;

                    IgnoreSpacesExceptEOL();
                    if (!EnsureToken(")"))
                    {
                        SkipToNextLine();
                        return null;
                    }
                    tokenizer.MoveNext();
                    continue;
                }
                else if (token.Equals("else"))
                {
                    // make sure this is the first "else"
                    if (cond == null)
                    {
                        ReportError(Properties.Resources.ParserMultipleElse, token.Location);
                        tokenizer.MoveNext();
                        continue;
                    }

                    cond = null;
                    block = new CodeBlock();
                    stat.DefaultStatements = block;
                    block.Name = stat.Name;
                    block.Parent = stat;
                    block.Location = ConvertLocation(token.Location);
                    tokenizer.MoveNext();
                    // FIXME: should ensure new line?
                    continue;
                }

                if (HandleStatementInBlock(block))
                {
                    continue;
                }

            }

            return stat;
        }

        private CodeIfDefStatement ParseIfDefStatement()
        {
            Debug.Assert(tokenizer.Current is IdentifierToken);
            Debug.Assert(tokenizer.Current.Equals("ifdef"));

            CodeIfDefStatement stat = new CodeIfDefStatement();
            stat.Location = ConvertLocation(tokenizer.Current.Location);
            tokenizer.MoveNext();

            IgnoreSpacesExceptEOL();

            // maybe has block name?
            if (tokenizer.Current is IdentifierToken)
            {
                stat.Name = ParseIdentifier();
                if (IsReservedWord(stat.Name.Text))
                {
                    ReportError(string.Format(Properties.Resources.ParserReservedIdentifier,
                        stat.Name.Text), stat.Location);
                    stat.Name = null;
                }
                if (stat.Name != null)
                {
                    stat.Name.Parent = stat;
                }
                IgnoreSpacesExceptEOL();
            }

            // first condition

            if (!EnsureToken("("))
            {
                SkipToNextLine();
                return null;
            }
            tokenizer.MoveNext();
            IgnoreSpacesExceptEOL();

            stat.Variable = ParseIdentifier();

            IgnoreSpacesExceptEOL();
            if (!EnsureToken(")"))
            {
                SkipToNextLine();
                return null;
            }
            tokenizer.MoveNext();
            IgnoreSpacesExceptEOL();
            if (EnsureLineBreak())
            {
                tokenizer.MoveNext();
            }

            CodeBlock block = new CodeBlock();
            stat.Statements = block;
            block.Location = stat.Location;
            block.Parent = stat;
            block.Name = stat.Name;

            while (true)
            {
                IgnoreSpaces();

                Token token = tokenizer.Current;
                if (token is EndOfFileToken)
                {
                    ReportError(Properties.Resources.ParserIfUnexpectedEOF,
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
                    if (EnsureToken("ifdef"))
                    {
                        tokenizer.MoveNext();
                        if (EnsureLineBreak())
                        {
                            tokenizer.MoveNext();
                        }
                        else
                        {
                            SkipToNextLine();
                        }
                        return stat;
                    }
                    else
                    {
                        SkipToNextLine();
                        return null;
                    }
                }

                if (HandleStatementInBlock(block))
                {
                    continue;
                }

            }

            return stat;
        }

        private CodeSwitchStatement ParseSwitchStatement()
        {
            Debug.Assert(tokenizer.Current is IdentifierToken);
            Debug.Assert(tokenizer.Current.Equals("switch"));

            CodeSwitchStatement stat = new CodeSwitchStatement();
            stat.Location = ConvertLocation(tokenizer.Current.Location);
            tokenizer.MoveNext();

            IgnoreSpacesExceptEOL();

            // maybe has block name?
            CodeIdentifier name = null;
            if (tokenizer.Current is IdentifierToken)
            {
                name = ParseIdentifier();
                if (IsReservedWord(name.Text))
                {
                    ReportError(string.Format(Properties.Resources.ParserReservedIdentifier,
                        name.Text), stat.Location);
                    name = null;
                }
                if (name != null)
                {
                    stat.Name = name;
                    name.Parent = stat;
                }
                IgnoreSpacesExceptEOL();
            }

            // variable

            if (!EnsureToken("("))
            {
                SkipToNextLine();
                return null;
            }
            tokenizer.MoveNext();

            stat.Value = ParseExpression();
            IgnoreSpacesExceptEOL();
            if (!EnsureToken(")"))
            {
                SkipToNextLine();
                return null;
            }
            tokenizer.MoveNext();
            IgnoreSpacesExceptEOL();
            if (EnsureLineBreak())
            {
                tokenizer.MoveNext();
            }

            CodeBlock block = null;

            while (true)
            {
                IgnoreSpaces();

                Token token = tokenizer.Current;
                if (token is EndOfFileToken)
                {
                    ReportError(Properties.Resources.ParserIfUnexpectedEOF,
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
                    if (EnsureToken("switch"))
                    {
                        tokenizer.MoveNext();
                        if (EnsureLineBreak())
                        {
                            tokenizer.MoveNext();
                        }
                        else
                        {
                            SkipToNextLine();
                        }
                        return stat;
                    }
                    else
                    {
                        SkipToNextLine();
                        return null;
                    }
                }
                else if (token.Equals("case"))
                {
                    tokenizer.MoveNext();
                    IgnoreSpacesExceptEOL();
                    if (!EnsureToken("("))
                    {
                        SkipToNextLine();
                        return null;
                    }
                    tokenizer.MoveNext();

                    CodeSwitchCondition cond = new CodeSwitchCondition();
                    while (true)
                    {
                        CodeRange range = ParseRange();
                        if (range == null)
                        {
                            SkipToNextLine();
                            break;
                        }

                        cond.Ranges.Add(range);

                        IgnoreSpacesExceptEOL();
                        if (tokenizer.Current.Equals(")"))
                        {
                            tokenizer.MoveNext();
                            break;
                        }
                        if (!EnsureToken(","))
                        {
                            SkipToNextLine();
                            break;
                        }
                        tokenizer.MoveNext();
                        IgnoreSpacesExceptEOL();
                    }

                    if (cond.Ranges.Count == 0)
                    {
                        ReportError(Properties.Resources.ICSwitchNoRange, token.Location);
                    }

                    stat.Ranges.Add(cond);
                    cond.Parent = stat;

                    block = new CodeBlock();
                    block.Parent = cond;
                    block.Name = name;
                    block.Location = ConvertLocation(token.Location);
                    cond.Statements = block;
                    continue;
                }
                else if (token.Equals("default"))
                {
                    // make sure this is the first "default"
                    if (stat.DefaultStatements != null)
                    {
                        ReportError(Properties.Resources.ParserSwitchMultipleDefault, token.Location);
                        tokenizer.MoveNext();
                        continue;
                    }

                    block = new CodeBlock();
                    stat.DefaultStatements = block;
                    block.Name = name;
                    block.Parent = stat;
                    block.Location = ConvertLocation(token.Location);
                    tokenizer.MoveNext();
                    // FIXME: should ensure new line?
                    continue;
                }

                if (HandleStatementInBlock(block))
                {
                    continue;
                }

            }

            return stat;
        }

        private CodeTryStatement ParseTryStatement()
        {
            Debug.Assert(tokenizer.Current is IdentifierToken);
            Debug.Assert(tokenizer.Current.Equals("try"));

            CodeTryStatement stat = new CodeTryStatement();
            stat.Location = ConvertLocation(tokenizer.Current.Location);
            tokenizer.MoveNext();

            IgnoreSpacesExceptEOL();

            // maybe has block name?
            CodeIdentifier name = null;
            if (tokenizer.Current is IdentifierToken)
            {
                name = ParseIdentifier();
                if (IsReservedWord(name.Text))
                {
                    ReportError(string.Format(Properties.Resources.ParserReservedIdentifier,
                        name.Text), stat.Location);
                    name = null;
                }
                if (name != null)
                {
                    name.Parent = stat;
                    stat.Name = name;
                }
                IgnoreSpacesExceptEOL();
            }


            CodeBlock block = new CodeBlock();
            block.Parent = stat;
            block.Name = name;
            block.Location = stat.Location;
            stat.ProtectedStatements = block;

            while (true)
            {
                IgnoreSpaces();

                Token token = tokenizer.Current;
                if (token is EndOfFileToken)
                {
                    ReportError(Properties.Resources.ParserIfUnexpectedEOF,
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
                    if (EnsureToken("try"))
                    {
                        tokenizer.MoveNext();
                        if (EnsureLineBreak())
                        {
                            tokenizer.MoveNext();
                        }
                        else
                        {
                            SkipToNextLine();
                        }
                        return stat;
                    }
                    else
                    {
                        SkipToNextLine();
                        return null;
                    }
                }
                else if (token.Equals("catch"))
                {
                    tokenizer.MoveNext();
                    IgnoreSpacesExceptEOL();
                    if (!EnsureToken("("))
                    {
                        SkipToNextLine();
                        return null;
                    }
                    tokenizer.MoveNext();

                    CodeCatchClause catcher;
                    if (tokenizer.Current.Equals("$"))
                    {
                        tokenizer.MoveNext();
                        CodeTypedCatchClause cond = new CodeTypedCatchClause();
                        cond.Type = ParseType();
                        IgnoreSpacesExceptEOL();
                        if (!EnsureToken(")"))
                        {
                            SkipToNextLine();
                        }
                        else
                        {
                            tokenizer.MoveNext();
                        }
                        catcher = cond;
                    }
                    else if (tokenizer.Current.Equals(")"))
                    {
                        tokenizer.MoveNext();
                        CodeDefaultCatchClause cond = new CodeDefaultCatchClause();
                        catcher = cond;
                    }
                    else
                    {
                        CodeNumericCatchClause cond = new CodeNumericCatchClause();
                        while (true)
                        {
                            CodeRange range = ParseRange();
                            if (range == null)
                            {
                                SkipToNextLine();
                                break;
                            }

                            cond.Ranges.Add(range);

                            IgnoreSpacesExceptEOL();
                            if (tokenizer.Current.Equals(")"))
                            {
                                tokenizer.MoveNext();
                                break;
                            }
                            if (!EnsureToken(","))
                            {
                                SkipToNextLine();
                                break;
                            }
                            tokenizer.MoveNext();
                            IgnoreSpacesExceptEOL();
                        }
                        catcher = cond;
                    }

                    stat.Handlers.Add(catcher);
                    catcher.Parent = stat;
                    catcher.Location = ConvertLocation(token.Location);

                    block = new CodeBlock();
                    block.Parent = catcher;
                    block.Name = name;
                    block.Location = ConvertLocation(token.Location);
                    catcher.Handler = block;
                    continue;
                }
                else if (token.Equals("finally"))
                {
                    // make sure this is the first "default"
                    if (stat.FinallyClause != null)
                    {
                        ReportError(Properties.Resources.ParserMultipleFinally, token.Location);
                        tokenizer.MoveNext();
                        continue;
                    }

                    stat.FinallyClause = new CodeFinallyClause();
                    stat.FinallyClause.Parent = stat;

                    block = new CodeBlock();
                    stat.FinallyClause.Statements = block;
                    block.Name = name;
                    block.Parent = stat.FinallyClause;
                    block.Location = ConvertLocation(token.Location);
                    tokenizer.MoveNext();
                    // FIXME: should ensure new line?
                    continue;
                }

                if (HandleStatementInBlock(block))
                {
                    continue;
                }

            }

            return stat;
        }

        private CodeWhileStatement ParseWhileStatement()
        {
            Debug.Assert(tokenizer.Current is IdentifierToken);
            Debug.Assert(tokenizer.Current.Equals("while"));

            CodeWhileStatement stat = new CodeWhileStatement();
            stat.Location = ConvertLocation(tokenizer.Current.Location);
            tokenizer.MoveNext();

            IgnoreSpacesExceptEOL();

            // maybe has block name?
            CodeIdentifier name = null;
            if (tokenizer.Current is IdentifierToken)
            {
                name = ParseIdentifier();
                if (IsReservedWord(name.Text))
                {
                    ReportError(string.Format(Properties.Resources.ParserReservedIdentifier,
                        name.Text), stat.Location);
                    name = null;
                }
                if (name != null)
                {
                    name.Parent = stat;
                }
                IgnoreSpacesExceptEOL();
            }

            // condition

            if (!EnsureToken("("))
            {
                SkipToNextLine();
                return null;
            }
            tokenizer.MoveNext();
            IgnoreSpacesExceptEOL();

            if (tokenizer.Current.Equals("skip"))
            {
                // no condition; infinite loop
                stat.Condition = null;
                stat.SkipFirstConditionEvaluation = true;
            }
            else
            if (tokenizer.Current.Equals(")"))
            {
                // no condition; infinite loop
                stat.Condition = null;
            }
            else
            {

                stat.Condition = ParseExpression();
                if (stat.Condition != null) {
                    stat.Condition.Parent = stat;
                }
                IgnoreSpacesExceptEOL();
                if (tokenizer.Current.Equals(","))
                {
                    tokenizer.MoveNext();
                    IgnoreSpacesExceptEOL();
                    if (!EnsureToken("skip"))
                    {
                        SkipToNextLine();
                        return null;
                    }
                    tokenizer.MoveNext();
                    IgnoreSpacesExceptEOL();
                    stat.SkipFirstConditionEvaluation = true;
                }
                if (!EnsureToken(")"))
                {
                    SkipToNextLine();
                    return null;
                }
            }

            tokenizer.MoveNext();
            IgnoreSpacesExceptEOL();
            if (EnsureLineBreak())
            {
                tokenizer.MoveNext();
            }

            CodeBlock block = new CodeBlock();
            block.Parent = stat;
            stat.Statements = block;
            block.Location = stat.Location;
            block.Name = name;

            while (true)
            {
                IgnoreSpaces();

                Token token = tokenizer.Current;
                if (token is EndOfFileToken)
                {
                    ReportError(Properties.Resources.ParserWhileUnexpectedEOF,
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
                    if (EnsureToken("while"))
                    {
                        tokenizer.MoveNext();
                        if (EnsureLineBreak())
                        {
                            tokenizer.MoveNext();
                        }
                        else
                        {
                            SkipToNextLine();
                        }
                        return stat;
                    }
                    else
                    {
                        SkipToNextLine();
                        return null;
                    }
                }

                if (HandleStatementInBlock(block))
                {
                    continue;
                }

            }

            return stat;
        }

        private CodeForEachStatement ParseForEachStatement()
        {
            Debug.Assert(tokenizer.Current is IdentifierToken);
            Debug.Assert(tokenizer.Current.Equals("foreach"));

            CodeForEachStatement stat = new CodeForEachStatement();
            stat.Location = ConvertLocation(tokenizer.Current.Location);
            tokenizer.MoveNext();

            IgnoreSpacesExceptEOL();

            // maybe has block name?
            CodeIdentifier name = null;
            if (tokenizer.Current is IdentifierToken)
            {
                name = ParseIdentifier();
                if (IsReservedWord(name.Text))
                {
                    ReportError(string.Format(Properties.Resources.ParserReservedIdentifier,
                        name.Text), stat.Location);
                    name = null;
                }
                if (name != null)
                {
                    name.Parent = stat;
                    stat.Name = name;
                }
                IgnoreSpacesExceptEOL();
            }

            // condition

            if (!EnsureToken("("))
            {
                SkipToNextLine();
                return null;
            }
            tokenizer.MoveNext();
            IgnoreSpacesExceptEOL();

            stat.Enumerable = ParseExpression();
            stat.Enumerable.Parent = stat;
            IgnoreSpacesExceptEOL();
            if (!EnsureToken(")"))
            {
                SkipToNextLine();
                return null;
            }

            tokenizer.MoveNext();
            IgnoreSpacesExceptEOL();
            if (EnsureLineBreak())
            {
                tokenizer.MoveNext();
            }

            CodeBlock block = new CodeBlock();
            block.Parent = stat;
            stat.Statements = block;
            block.Location = stat.Location;
            block.Name = name;

            while (true)
            {
                IgnoreSpaces();

                Token token = tokenizer.Current;
                if (token is EndOfFileToken)
                {
                    ReportError(Properties.Resources.ParserWhileUnexpectedEOF,
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
                    if (EnsureToken("foreach"))
                    {
                        tokenizer.MoveNext();
                        if (EnsureLineBreak())
                        {
                            tokenizer.MoveNext();
                        }
                        else
                        {
                            SkipToNextLine();
                        }
                        return stat;
                    }
                    else
                    {
                        SkipToNextLine();
                        return null;
                    }
                }

                if (HandleStatementInBlock(block))
                {
                    continue;
                }

            }

            return stat;
        }

        private CodeBlockStatement ParseBlockStatement()
        {
            Debug.Assert(tokenizer.Current is IdentifierToken);
            Debug.Assert(tokenizer.Current.Equals("block"));

            CodeBlockStatement stat = new CodeBlockStatement();
            stat.Location = ConvertLocation(tokenizer.Current.Location);
            tokenizer.MoveNext();

            IgnoreSpacesExceptEOL();

            // maybe has block name?
            CodeIdentifier name = null;
            if (tokenizer.Current is IdentifierToken)
            {
                name = ParseIdentifier();
                if (IsReservedWord(name.Text))
                {
                    ReportError(string.Format(Properties.Resources.ParserReservedIdentifier,
                        name.Text), stat.Location);
                    name = null;
                }
                if (name != null)
                {
                    name.Parent = stat;
                }
                IgnoreSpacesExceptEOL();
            }

            IgnoreSpacesExceptEOL();
            if (EnsureLineBreak())
            {
                tokenizer.MoveNext();
            }

            CodeBlock block = new CodeBlock();
            block.Parent = stat;
            stat.Statements = block;
            block.Location = stat.Location;
            block.Name = name;

            while (true)
            {
                IgnoreSpaces();

                Token token = tokenizer.Current;
                if (token is EndOfFileToken)
                {
                    ReportError(Properties.Resources.ParserWhileUnexpectedEOF,
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
                    if (EnsureToken("block"))
                    {
                        tokenizer.MoveNext();
                        if (EnsureLineBreak())
                        {
                            tokenizer.MoveNext();
                        }
                        else
                        {
                            SkipToNextLine();
                        }
                        return stat;
                    }
                    else
                    {
                        SkipToNextLine();
                        return null;
                    }
                }

                if (HandleStatementInBlock(block))
                {
                    continue;
                }

            }

            return stat;
        }

        private CodeForStatement ParseForStatement()
        {
            Debug.Assert(tokenizer.Current is IdentifierToken);
            Debug.Assert(tokenizer.Current.Equals("for"));

            CodeForStatement stat = new CodeForStatement();
            stat.Location = ConvertLocation(tokenizer.Current.Location);
            tokenizer.MoveNext();

            IgnoreSpacesExceptEOL();

            // maybe has block name?
            CodeIdentifier name = null;
            if (tokenizer.Current is IdentifierToken)
            {
                name = ParseIdentifier();
                if (IsReservedWord(name.Text))
                {
                    ReportError(string.Format(Properties.Resources.ParserReservedIdentifier,
                        name.Text), stat.Location);
                    name = null;
                }
                if (name != null)
                {
                    name.Parent = stat;
                    stat.Name = name;
                }
                IgnoreSpacesExceptEOL();
            }

            // condition

            if (!EnsureToken("("))
            {
                SkipToNextLine();
                return null;
            }
            tokenizer.MoveNext();
            IgnoreSpacesExceptEOL();

          
            stat.InitialValue = ParseExpression();
            stat.InitialValue.Parent = stat;
            IgnoreSpacesExceptEOL();
            if (EnsureToken(","))
            {
                tokenizer.MoveNext();
                IgnoreSpacesExceptEOL();

                stat.LimitValue = ParseExpression();
                stat.LimitValue.Parent = stat;
                IgnoreSpacesExceptEOL();
            }
            else
            {
                SkipToNextLine();
                return null;
            }
            if (tokenizer.Current.Equals(","))
            {
                // step
                tokenizer.MoveNext();
                IgnoreSpacesExceptEOL();

                stat.Step = ParseExpression();
                stat.Step.Parent = stat;
                IgnoreSpacesExceptEOL();
            }
            if (!EnsureToken(")"))
            {
                SkipToNextLine();
                return null;
            }
                
            tokenizer.MoveNext();
            IgnoreSpacesExceptEOL();
            if (EnsureLineBreak())
            {
                tokenizer.MoveNext();
            }

            CodeBlock block = new CodeBlock();
            block.Parent = stat;
            stat.Statements = block;
            block.Location = stat.Location;
            block.Name = name;

            while (true)
            {
                IgnoreSpaces();

                Token token = tokenizer.Current;
                if (token is EndOfFileToken)
                {
                    ReportError(Properties.Resources.ParserWhileUnexpectedEOF,
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
                    if (EnsureToken("for"))
                    {
                        tokenizer.MoveNext();
                        if (EnsureLineBreak())
                        {
                            tokenizer.MoveNext();
                        }
                        else
                        {
                            SkipToNextLine();
                        }
                        return stat;
                    }
                    else
                    {
                        SkipToNextLine();
                        return null;
                    }
                }

                if (HandleStatementInBlock(block))
                {
                    continue;
                }

            }

            return stat;
        }

        private List<CodeIdentifier> ParseGenericTypeDefinitions()
        {
            var idts = new List<CodeIdentifier>();
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
                            CodeIdentifier typ = ParseIdentifier();
                            if (typ == null)
                            {
                                return idts;
                            }
                            idts.Add(typ);
                            IgnoreSpacesExceptEOL();
                            if (tokenizer.Current.Equals("]"))
                            {
                                tokenizer.MoveNext();
                                IgnoreSpacesExceptEOL();
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
                    CodeIdentifier typ = ParseIdentifier();
                    if (typ == null)
                    {
                        return idts;
                    }
                    idts.Add(typ);
                }
            }
            return idts;
        }

        private enum SpecialFunctionType
        {
            Constructor,
            Destructor
        }

        private static readonly CodeFunctionStatement anonymousFunctionConstant = new CodeFunctionStatement();

        private CodeFunctionStatement ParseFunctionStatement(bool isAbstract = false, SpecialFunctionType? special = null,
            bool allowExpression = false)
        {
            string funcType = "func";
            string funcName = null;
            if (special != null)
            {
                switch (special)
                {
                    case SpecialFunctionType.Constructor:
                        funcType = "ctor"; funcName = "Ctor";
                        break;
                    case SpecialFunctionType.Destructor:
                        funcType = "dtor"; funcName = "Dtor";
                        break;
                }
            }

            Token funcTypeToken = tokenizer.Current;
            Debug.Assert(tokenizer.Current is IdentifierToken);
            Debug.Assert(tokenizer.Current.Equals(funcType));

            CodeFunctionStatement func = new CodeFunctionStatement();
            func.Location = ConvertLocation(tokenizer.Current.Location);
            tokenizer.MoveNext();
            IgnoreSpacesExceptEOL();


            if (funcName == null)
            {
                // this might be anonymous function like this:
                // func (PARAMS)
                // end func()
                if (allowExpression && tokenizer.Current.Equals("("))
                {
                    // this is anonymous function
                    tokenizer.MovePrevious();
                    if (tokenizer.Current != funcTypeToken)
                    {
                        tokenizer.UnMove(funcTypeToken);
                    }
                    return anonymousFunctionConstant;
                }

                func.Name = ParseIdentifier();
                if (func.Name == null)
                {
                    SkipToNextLine();
                    return null;
                }
                if (IsReservedWord(func.Name.Text))
                {
                    ReportError(string.Format(Properties.Resources.ParserReservedIdentifier, func.Name.Text),
                    func.Name.Location);
                }
            }
            else
            {
                func.Name = new CodeIdentifier()
                {
                     Location = func.Location, Text = funcName, Parent = func
                };
            }

            // read generic type parameters
            IgnoreSpacesExceptEOL();
            func.GenericParameters = ParseGenericTypeDefinitions();

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

            // there's no body for abstruct functions
            if (isAbstract)
            {
                return func;
            }

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
                    if (EnsureToken(funcType))
                    {
                        tokenizer.MoveNext();
                        if (EnsureLineBreak())
                        {
                            tokenizer.MoveNext();
                        }
                        else
                        {
                            SkipToNextLine();
                        }
                        return func;
                    }
                    else
                    {
                        SkipToNextLine();
                        return null;
                    }
                }

                if (HandleStatementInBlock(block))
                {
                    continue;
                }
                
            }


            return func;
        }

        private CodeEnumStatement ParseEnumStatement()
        {
            Debug.Assert(tokenizer.Current is IdentifierToken);
            Debug.Assert(tokenizer.Current.Equals("enum"));

            CodeEnumStatement type = new CodeEnumStatement();
            type.Location = ConvertLocation(tokenizer.Current.Location);
            tokenizer.MoveNext();

            type.Name = ParseIdentifier();
            if (type.Name == null)
            {
                SkipToNextLine();
                return null;
            }

            if (EnsureLineBreak())
            {
                tokenizer.MoveNext();
            }

            while (true)
            {
                IgnoreSpaces();
                if (tokenizer.Current.Equals("end"))
                {
                    tokenizer.MoveNext();
                    IgnoreSpacesExceptEOL();
                    if (EnsureToken("enum"))
                    {
                        tokenizer.MoveNext();
                        if (EnsureLineBreak())
                        {
                            tokenizer.MoveNext();
                        }
                        else
                        {
                            SkipToNextLine();
                        }
                        return type;
                    }
                    else
                    {
                        SkipToNextLine();
                        return null;
                    }
                }

                CodeEnumItem item = new CodeEnumItem();
                item.Name = ParseIdentifier();
                if (item.Name == null)
                {
                    SkipToNextLine();
                    return null;
                }
                if (IsReservedWord(item.Name.Text))
                {
                    ReportError(string.Format(Properties.Resources.ParserReservedIdentifier, item.Name.Text),
                        item.Name.Location);
                }
                IgnoreSpacesExceptEOL();
                if (tokenizer.Current.Equals("::"))
                {
                    tokenizer.MoveNext();
                    item.Value = ParseExpression();
                }
                type.Items.Add(item);

                if (EnsureLineBreak())
                {
                    tokenizer.MoveNext();
                }
                else
                {
                    SkipToNextLine();
                }
            }

        }

        private CodeClassStatement ParseClassStatement()
        {
            bool isInterface = tokenizer.Current.Equals("interface");
            Debug.Assert(tokenizer.Current is IdentifierToken);
            Debug.Assert(tokenizer.Current.Equals(isInterface?"interface":"class"));

            CodeClassStatement cls = new CodeClassStatement();
            cls.Location = ConvertLocation(tokenizer.Current.Location);
            tokenizer.MoveNext();

            cls.Scope = new CodeGlobalScope();
            cls.Scope.Parent = cls;
            cls.Scope.Location = cls.Location;
            cls.IsInterface = isInterface;

            cls.Scope.Name = ParseIdentifier();
            if (cls.Scope.Name == null)
            {
                SkipToNextLine();
                return null;
            }
            if (IsReservedWord(cls.Scope.Name.Text))
            {
                ReportError(string.Format(Properties.Resources.ParserReservedIdentifier, cls.Scope.Name.Text),
                cls.Scope.Name.Location);
            }
            cls.Name = cls.Scope.Name;
            cls.Name.Parent = cls;

            // read generic type parameters
            IgnoreSpacesExceptEOL();
            cls.GenericParameters = ParseGenericTypeDefinitions();


            // read suprtclass
            IgnoreSpacesExceptEOL();
            if (tokenizer.Current.Equals("("))
            {
                tokenizer.MoveNext();
                IgnoreSpacesExceptEOL();
                if (tokenizer.Current.Equals(")"))
                {
                    // no base class
                    tokenizer.MoveNext();
                }
                else
                {
                    while (true)
                    {
                        cls.BaseClasses.Add(ParseType());

                        IgnoreSpacesExceptEOL();
                        if (tokenizer.Current.Equals(","))
                        {
                            tokenizer.MoveNext();
                            IgnoreSpacesExceptEOL();
                        }
                        else
                        {
                            if (EnsureToken(")"))
                            {
                                tokenizer.MoveNext();
                            }
                            break;
                        }
                    }
                }
            }
            else
            {
                // no ()
            }

            IgnoreSpacesExceptEOL();
            EnsureLineBreak();
            tokenizer.MoveNext();

            while (true)
            {
                IgnoreSpaces();

                Token token = tokenizer.Current;
                if (token is EndOfFileToken)
                {
                    ReportError(Properties.Resources.ParserClassUnexpectedEOF,
                        token.Location);
                    break;
                }

                if (HandleStatementInScope(cls.Scope, false, isInterface, true))
                {
                    continue;
                }

                if (token.Equals("end"))
                {
                    tokenizer.MoveNext();
                    IgnoreSpacesExceptEOL();
                    if (EnsureToken(isInterface?"interface":"class"))
                    {
                        tokenizer.MoveNext();
                        if (EnsureLineBreak())
                        {
                            tokenizer.MoveNext();
                        }
                        else
                        {
                            SkipToNextLine();
                        }
                        return cls;
                    }
                    else
                    {
                        SkipToNextLine();
                        return null;
                    }
                }

                // invalid token.
                UnexpectedToken(token);
                SkipToNextLine();
            }


            return cls;
        }

        private CodeSourceFile ParseGlobalScope()
        {
            CodeSourceFile srcFile = new CodeSourceFile();
            srcFile.Name = new CodeIdentifier();
            srcFile.Name.Text = "Unnamed";
            srcFile.Name.Parent = srcFile;
            sourceFile = srcFile;
            srcFile.Location = ConvertLocation(tokenizer.Current.Location);

            while (true)
            {
                IgnoreSpaces();

                Token token = tokenizer.Current;
                if (token is EndOfFileToken)
                {
                    break;
                }

                if (HandleStatementInScope(srcFile, false))
                {
                    continue;
                }
                
                // invalid token.
                UnexpectedToken(token);
                SkipToNextLine();
            }

            return srcFile;
        }



        public CodeSourceFile Parse(System.IO.TextReader codeStream)
        {
            tokenizer = new Tokenizer(codeStream);
            tokenizer.ErrorReported += OnTokenizerError;
            tokenizer.MoveNext();
            CodeSourceFile outp = ParseGlobalScope();
            tokenizer = null;
            return outp;
        }
    }
}
