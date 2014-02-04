using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public class CodeTryStatement : CodeStatement
    {
        public CodeIdentifier Name { get; set; }
        public CodeBlock ProtectedStatements { get; set; }
        public IList<CodeCatchClause> Handlers { get; set; }
        public CodeFinallyClause FinallyClause { get; set; }

        public CodeTryStatement()
        {
            Handlers = new List<CodeCatchClause>();
        }
        public override T Accept<T>(ICodeStatementVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
