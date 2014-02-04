using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public sealed class CodeArrayConstructExpression: CodeExpression
    {
        public IList<CodeExpression> NumElements { get; set; }
        public CodeType ElementType { get; set; }

        public CodeArrayConstructExpression()
        {
            NumElements = new List<CodeExpression>();
        }

        public override object Accept(ICodeExpressionVisitor visitor)
        {
            return visitor.Visit(this);
        }
    }
}
