using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public class CodeThrowStatement : CodeStatement
    {
        public CodeExpression FirstParameter { get; set; }
        public CodeExpression SecondParameter { get; set; }
        public override T Accept<T>(ICodeStatementVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
