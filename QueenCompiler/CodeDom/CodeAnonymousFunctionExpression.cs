using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Queen.Language.CodeDom
{
    public class CodeAnonymousFunctionExpression: CodeExpression
    {

        public CodeType ReturnType { get; set; }
        public CodeBlock Statements { get; set; }
        public IList<CodeParameterDeclaration> Parameters { get; set; }

        public CodeAnonymousFunctionExpression()
        {
            Parameters = new List<CodeParameterDeclaration>();
        }

        public override object Accept(ICodeExpressionVisitor visitor)
        {
            return visitor.Visit(this);
        }

    }
}
