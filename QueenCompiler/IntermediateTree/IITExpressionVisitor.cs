using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public interface IITExpressionVisitor<T>
    {
        T Visit(ITArrayConstructExpression expr);
        T Visit(ITArrayLiteralExpression expr);
        T Visit(ITBinaryOperatorExpression expr);
        T Visit(ITCallMemberFunctionExpression expr);
        T Visit(ITCallGlobalFunctionExpression expr);
        T Visit(ITCallFunctionReferenceExpression expr);
        T Visit(ITCastExpression expr);
        T Visit(ITClassConstructExpression expr);
        T Visit(ITConditionalExpression expr);
        T Visit(ITErrorExpression expr);
        T Visit(ITMemberVariableStorage expr);
        T Visit(ITMemberPropertyStorage expr);
        T Visit(ITGlobalVariableStorage expr);
        T Visit(ITLocalVariableStorage expr);
        T Visit(ITParameterStorage expr);
        T Visit(ITArrayElementStorage expr);
        T Visit(ITGlobalFunctionStorage expr);
        T Visit(ITMemberFunctionStorage expr);
        T Visit(ITAssignExpression expr);
        T Visit(ITReferenceCalleeExpression expr);
        T Visit(ITTypeCheckExpression expr);
        T Visit(ITUnaryOperatorExpression expr);
        T Visit(ITUnresolvedConstantExpression expr);
        T Visit(ITValueExpression expr);
    }
}
