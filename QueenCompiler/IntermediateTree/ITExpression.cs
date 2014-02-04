using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public abstract class ITExpression: ITObject
    {
        public ITType ExpressionType { get; set; }

        public abstract T Accept<T>(IITExpressionVisitor<T> visitor);
    }
}
