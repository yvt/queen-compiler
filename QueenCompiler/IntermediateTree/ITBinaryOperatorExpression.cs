using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public enum ITBinaryOperatorType
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

        Equality,
        Inequality,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,

        ReferenceEquality,
        ReferenceInequality,

    }
    public sealed class ITBinaryOperatorExpression: ITExpression
    {
        public ITBinaryOperatorType OperatorType { get; set; }
        public ITExpression Left { get; set; }
        public ITExpression Right { get; set; }
        public override T Accept<T>(IITExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
