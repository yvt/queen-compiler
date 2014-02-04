using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public class CodeSwitchCondition : CodeObject
    {
        public IList<CodeRange> Ranges { get; set; }
        public CodeBlock Statements { get; set; }
        public CodeSwitchCondition()
        {
            Ranges = new List<CodeRange>();
        }
    }

    public class CodeSwitchStatement : CodeStatement
    {
        public CodeExpression Value { get; set; }
        public IList<CodeSwitchCondition> Ranges { get; set; }
        public CodeBlock DefaultStatements { get; set; }
        public CodeIdentifier Name { get; set; }
        public CodeSwitchStatement()
        {
            Ranges = new List<CodeSwitchCondition>();
        }
        public override T Accept<T>(ICodeStatementVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
