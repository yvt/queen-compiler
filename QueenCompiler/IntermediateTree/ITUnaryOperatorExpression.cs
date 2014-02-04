using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public enum ITUnaryOperatorType
    {
        Negate,
        Not
    }
    public sealed class ITUnaryOperatorExpression: ITExpression
    {
        public ITUnaryOperatorType Type { get; set; }
        public ITExpression Expression { get; set; }
        public override T Accept<T>(IITExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
