using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public sealed class ITCastExpression: ITExpression
    {
        public ITExpression Expression { get; set; }
        public ITType CastTarget { get; set; }

        public override T Accept<T>(IITExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
