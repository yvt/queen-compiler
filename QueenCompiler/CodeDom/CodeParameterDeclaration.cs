using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public class CodeParameterDeclaration : CodeObject
    {
        public CodeIdentifier Identifier { get; set; }
        public bool IsByRef { get; set; }
        public CodeType Type { get; set; }
    }
}
