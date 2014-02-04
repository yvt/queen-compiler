using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public abstract class CodeExpression : CodeObject
    {
        public abstract object Accept(ICodeExpressionVisitor visitor);
    }
}
