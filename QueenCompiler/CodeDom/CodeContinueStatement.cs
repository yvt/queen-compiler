using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public class CodeContinueStatement : CodeBlockControlStatement
    {

        public override T Accept<T>(ICodeStatementVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
