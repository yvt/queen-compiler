using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public enum ITTypeCheckExpressionType
    {
        Is,
        IsNot
    }
    public sealed class ITTypeCheckExpression: ITExpression
    {

        public ITExpression Object { get; set; }
        public ITType TargetType { get; set; }

        public ITTypeCheckExpressionType Type { get; set; }


        public override T Accept<T>(IITExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
