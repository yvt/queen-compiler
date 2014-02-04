using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public sealed class CodeClassConstructExpression : CodeExpression
    {
        public CodeType Type { get; set; }
        // FIXME: arguments?
        public override object Accept(ICodeExpressionVisitor visitor)
        {
            return visitor.Visit(this);
        }
    }
}
