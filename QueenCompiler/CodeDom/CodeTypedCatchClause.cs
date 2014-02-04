using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public class CodeTypedCatchClause : CodeCatchClause
    {
        public CodeType Type { get; set; }
    }
}
