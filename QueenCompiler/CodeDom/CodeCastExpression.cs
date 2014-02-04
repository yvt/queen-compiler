using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public class CodeCastExpression : CodeExpression
    {
        public CodeExpression Expression { get; set; }
        public CodeType Type { get; set; }
        public override object Accept(ICodeExpressionVisitor visitor)
        {
            return visitor.Visit(this);
        }
    }
}
