using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public class CodeEnumItem : CodeObject
    {
        public CodeIdentifier Name { get; set; }
        public CodeExpression Value { get; set; }
    }
}
