using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public sealed class ITArrayConstructExpression: ITExpression
    {
        public ITType ElementType { get; set; }
        public IList<ITExpression> NumElements { get; set; }
        public ITArrayConstructExpression()
        {
            NumElements = new List<ITExpression>();
        }

        public override T Accept<T>(IITExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
