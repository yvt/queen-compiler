using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public interface ICodeStatementVisitor<T>
    {
        T Visit(CodeAssertStatement statement);
        T Visit(CodeBlockStatement statement);
        T Visit(CodeBreakStatement statement);
        T Visit(CodeClassStatement statement);
        T Visit(CodeContinueStatement statement);
        T Visit(CodeEnumStatement statement);
        T Visit(CodeExpressionStatement statement);
        T Visit(CodeFunctionStatement statement);
        T Visit(CodeReturnStatement statement);
        T Visit(CodeSwitchStatement statement);
        T Visit(CodeTryStatement statement);
        T Visit(CodeWhileStatement statement);
        T Visit(CodeIfDefStatement statement);
        T Visit(CodeIfStatement statement);
        T Visit(CodeForStatement statement);
        T Visit(CodeForEachStatement statement);
        T Visit(CodeThrowStatement statement);
        T Visit(CodeVariableDeclarationStatement statement);
    }
}
