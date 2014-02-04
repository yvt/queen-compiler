using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public class CodeNumericCatchClause : CodeCatchClause
    {
        public IList<CodeRange> Ranges { get; set; }
        public CodeNumericCatchClause()
        {
            Ranges = new List<CodeRange>();
        }
    }
}
