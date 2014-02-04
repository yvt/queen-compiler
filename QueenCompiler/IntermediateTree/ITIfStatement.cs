using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public sealed class ITIfStatement: ITStatement
    {
        public ITExpression Condition { get; set; }
        public ITBlock TrueBlock { get; set; }
        public ITBlock FalseBlock { get; set; }
        public override T Accept<T>(IITStatementVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
