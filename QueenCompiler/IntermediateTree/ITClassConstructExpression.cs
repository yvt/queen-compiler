using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public sealed class ITClassConstructExpression: ITExpression
    {
        public ITType Type { get; set; }
        // TODO: constructor parameters
        public override T Accept<T>(IITExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
