using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public abstract class CodeBlockControlStatement: CodeStatement
    {
        public CodeIdentifier Name { get; set; }
    }
}
