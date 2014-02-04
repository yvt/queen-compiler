using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public class CodeScope : CodeObject
    {
        public IDictionary<string, CodeStatement> Children { get; set; }
        public CodeScope()
        {
            Children = new Dictionary<string, CodeStatement>();
        }
    }
}
