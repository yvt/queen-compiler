using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public enum CodeBinaryOperatorType
    {
        Add,
        Subtract,
        Divide,
        Multiply,
        Power,
        Modulus,
        Concat,
        And,
        Or,
        AdditionAssign,
        SubtractionAssign,
        DivisionAssign,
        MultiplicationAssign,
        PowerAssign,
        ModulusAssign,
        ConcatAssign,
        AndAssign,
        OrAssign,
        Assign,
        Swap,

        Equality,
        Inequality,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,

        ReferenceEquality,
        ReferenceInequality,

    }
    public sealed class CodeBinaryOperatorExpression : CodeExpression
    {
        public CodeExpression Left { get; set; }
        public CodeExpression Right { get; set; }
        public CodeBinaryOperatorType Type { get; set; }
        public override object Accept(ICodeExpressionVisitor visitor)
        {
            return visitor.Visit(this);
        }
    }
}
