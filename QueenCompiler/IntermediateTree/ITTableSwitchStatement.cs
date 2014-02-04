using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public sealed class ITTableSwitchStatement: ITStatement
    {
        // Value should be UInt32.
        public ITExpression Value { get; set; }
        public int[] BlockIndices { get; set; }
        public ITBlock[] Blocks { get; set; } 

        // block with index '-1'.
        public ITBlock DefaultBlock { get; set; }
        public override T Accept<T>(IITStatementVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
