using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public class CodeSourceFile : CodeGlobalScope
    {
        public override string ToString()
        {
            return Name.Text;
        }
    }
}
