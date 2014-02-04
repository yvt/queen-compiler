using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public class CodeInvocationParameter : CodeObject
    {
        public bool ByRef { get; set; }
        public CodeExpression Value { get; set; }
    };
    public class CodeInvocationExpression : CodeExpression
    {
        public CodeExpression Method { get; set; }
        public IList<CodeInvocationParameter> Parameters { get; set; }
        public CodeInvocationExpression()
        {
            Parameters = new List<CodeInvocationParameter>();
        }
        public override object Accept(ICodeExpressionVisitor visitor)
        {
            return visitor.Visit(this);
        }
    }
}
