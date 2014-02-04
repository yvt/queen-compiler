using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public sealed class CodeArrayLiteralExpression : CodeExpression
    {
        public IList<CodeExpression> Values { get; set; }
        public CodeType ElementType { get; set; }

        public CodeArrayLiteralExpression()
        {
            Values = new List<CodeExpression>();
        }

        public override object Accept(ICodeExpressionVisitor visitor)
        {
            return visitor.Visit(this);
        }
    }
}
