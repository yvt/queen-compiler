using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public sealed class ITTryStatement: ITStatement
    {
        public ITBlock ProtectedBlock { get; set; }
        public ITTryHandler[] Handlers { get; set; }
        public ITBlock FinallyBlock { get; set; }
        public override T Accept<T>(IITStatementVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }

    public abstract class ITTryHandler
    {
        public ITBlock Block { get; set; }
        public ITLocalVariable InfoVariable { get; set; }
    }

    public sealed class ITNumericTryHandlerRange
    {
        public ITExpression LowerBound { get; set; }
        public ITExpression UpperBound { get; set; }
    }

    public sealed class ITNumericTryHandler : ITTryHandler
    {
        public ITNumericTryHandlerRange[] Ranges { get; set; }
    }

    public sealed class ITTypedTryHandler : ITTryHandler
    {
        public ITType ExceptionType { get; set; }
    }
}
