using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public sealed class ITConditionalExpression: ITExpression
    {
        public ITExpression Conditional { get; set; }
        public ITExpression TrueValue { get; set; }
        public ITExpression FalseValue { get; set; }
        public override T Accept<T>(IITExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
