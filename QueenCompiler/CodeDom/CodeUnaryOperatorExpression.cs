using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public enum CodeUnaryOperatorType
    {
        Negate,
        PassThrough,
        Not,
        Copy
    }
    public class CodeUnaryOperatorExpression : CodeExpression
    {
        public CodeExpression Expression { get; set; }
        public CodeUnaryOperatorType Type { get; set; }
        public override object Accept(ICodeExpressionVisitor visitor)
        {
            return visitor.Visit(this);
        }
    }
}
