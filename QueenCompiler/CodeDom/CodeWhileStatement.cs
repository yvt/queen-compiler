using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public class CodeWhileStatement : CodeStatement
    {
        public CodeExpression Condition { get; set; }
        public CodeBlock Statements { get; set; }
        public bool SkipFirstConditionEvaluation { get; set; }
        public override T Accept<T>(ICodeStatementVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
