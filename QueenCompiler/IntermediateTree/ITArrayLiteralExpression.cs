using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public sealed class ITArrayLiteralExpression: ITExpression
    {
        public ITType ElementType { get; set; }
        public IList<ITExpression> Elements { get; set; }
        public ITArrayLiteralExpression()
        {
            Elements = new List<ITExpression>();
        }
        public override T Accept<T>(IITExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
