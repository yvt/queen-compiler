using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    /// <summary>
    /// "block NAME" ... "end block" statement.
    /// </summary>
    public class CodeBlockStatement : CodeStatement
    {
        public CodeIdentifier Name { get; set; }
        public CodeBlock Statements { get; set; }
        public override T Accept<T>(ICodeStatementVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
