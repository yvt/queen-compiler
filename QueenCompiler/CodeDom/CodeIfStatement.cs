using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public class CodeIfCondition : CodeObject
    {
        public CodeExpression Condition { get; set; }

        public CodeBlock Statements { get; set; }
    }
    public class CodeIfStatement : CodeStatement
    {
        public CodeIdentifier Name { get; set; }

        public IList<CodeIfCondition> Conditions { get; set; }
        public CodeBlock DefaultStatements { get; set; }

        public CodeIfStatement()
        {
            Conditions = new List<CodeIfCondition>();
        }
        public override T Accept<T>(ICodeStatementVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
