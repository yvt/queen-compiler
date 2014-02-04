using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public class CodeTernaryConditionalExpression : CodeExpression
    {
        public CodeExpression Condition { get; set; }
        public CodeExpression TrueValue { get; set; }
        public CodeExpression FalseValue { get; set; }
        public override object Accept(ICodeExpressionVisitor visitor)
        {
            return visitor.Visit(this);
        }
    }
}
