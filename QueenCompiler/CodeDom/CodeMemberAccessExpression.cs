using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public class CodeMemberAccessExpression : CodeExpression
    {
        public CodeExpression Expression { get; set; }
        public CodeIdentifier MemberName { get; set; }
        public IList<CodeType> GenericTypeParameters { get; set; }
        public CodeMemberAccessExpression()
        {
            GenericTypeParameters = new List<CodeType>();
        }
        public override object Accept(ICodeExpressionVisitor visitor)
        {
            return visitor.Visit(this);
        }
    }
}
