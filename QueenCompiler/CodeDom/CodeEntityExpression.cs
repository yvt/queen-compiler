using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public sealed class CodeEntityExpression : CodeExpression
    {
        public CodeEntitySpecifier Entity { get; set; }
        public override object Accept(ICodeExpressionVisitor visitor)
        {
            return visitor.Visit(this);
        }
    }
}
