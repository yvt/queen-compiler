using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public abstract class CodeStatement : CodeObject
    {
        public abstract T Accept<T>(ICodeStatementVisitor<T> visitor);
    }
}
