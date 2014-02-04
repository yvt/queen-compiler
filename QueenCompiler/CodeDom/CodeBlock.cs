using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public class CodeBlock : CodeLocalScope
    {
        public CodeIdentifier Name { get; set; }
        public IList<CodeStatement> Statements { get; set; }
        public CodeBlock()
        {
            Statements = new List<CodeStatement>();
        }
    }
}
