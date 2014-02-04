using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public class CodeForStatement : CodeStatement
    {
        public CodeExpression InitialValue { get; set; }
        public CodeExpression LimitValue { get; set; }
        public CodeExpression Step { get; set; }
        public CodeIdentifier Name { get; set; }
        public CodeBlock Statements { get; set; }
        public override T Accept<T>(ICodeStatementVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
