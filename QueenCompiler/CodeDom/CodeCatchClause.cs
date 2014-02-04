using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public abstract class CodeCatchClause : CodeObject
    {
        public CodeBlock Handler { get; set; }
    }
}
