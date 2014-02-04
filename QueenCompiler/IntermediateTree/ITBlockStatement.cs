using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public sealed class ITBlockStatement: ITStatement
    {
        public ITBlock Block { get; set; }
        public override T Accept<T>(IITStatementVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
