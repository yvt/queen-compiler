using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public class CodeIndexExpression : CodeExpression
    {
        public CodeExpression Expression { get; set; }
        public IList<CodeInvocationParameter> Parameters { get; set; }
        public CodeIndexExpression()
        {
            Parameters = new List<CodeInvocationParameter>();
        }
        public override object Accept(ICodeExpressionVisitor visitor)
        {
            return visitor.Visit(this);
        }
    }
}
