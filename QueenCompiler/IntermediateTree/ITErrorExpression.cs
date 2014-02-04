using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public class ITErrorExpression: ITExpression
    {

        public override T Accept<T>(IITExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
