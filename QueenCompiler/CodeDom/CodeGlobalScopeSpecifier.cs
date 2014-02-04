using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    /// <summary>
    /// globalScopeIdentifier@
    /// </summary>
    public class CodeGlobalScopeSpecifier : CodeEntitySpecifier
    {
        public CodeIdentifier Identifier { get; set; }
    }
}
