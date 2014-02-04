using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public class ITThrowNumericStatement: ITStatement
    {
        public ITExpression Code { get; set; }
        public ITExpression Message { get; set; }
        public override T Accept<T>(IITStatementVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
