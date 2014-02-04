using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public class CodeRange : CodeObject
    {
        public CodeExpression LowerBound { get; set; }
        public CodeExpression UpperBound { get; set; }
    }
}
