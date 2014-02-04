using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public class CodeVariableDeclarationStatement : CodeStatement
    {
        public CodeIdentifier Identifier { get; set; }
        public bool IsSourceCodeLocal { get; set; }
        public bool IsPrivate { get; set; }
        public bool IsConst { get; set; }
        public CodeType Type { get; set; }
        public CodeExpression InitialValue { get; set; }
        public override T Accept<T>(ICodeStatementVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
