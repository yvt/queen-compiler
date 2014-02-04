using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public class CodeIfDefStatement : CodeStatement
    {
        public CodeIdentifier Name { get; set; }
        public CodeBlock Statements { get; set; }
        public CodeIdentifier Variable { get; set; }
        public override T Accept<T>(ICodeStatementVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
