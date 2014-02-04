using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public interface ICodeExpressionVisitor
    {
        object Visit(CodeArrayConstructExpression expr);
        object Visit(CodeArrayLiteralExpression expr);
        object Visit(CodeBinaryOperatorExpression expr);
        object Visit(CodeCastExpression expr);
        object Visit(CodeClassConstructExpression expr);
        object Visit(CodeEntityExpression expr);
        object Visit(CodeIndexExpression expr);
        object Visit(CodeInvocationExpression expr);
        object Visit(CodeMemberAccessExpression expr);
        object Visit(CodeTypeEqualityExpression expr);
        object Visit(CodeTypeInequalityExpression expr);
        object Visit(CodeTernaryConditionalExpression expr);
        object Visit(CodeUnaryOperatorExpression expr);
        object Visit(CodeValueExpression expr);
        object Visit(CodeAnonymousFunctionExpression expr);
    }
}
