using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    /// <summary>
    /// Entity specifier with one identifier.
    /// </summary>
    public class CodeImplicitEntitySpecifier : CodeEntitySpecifier
    {
        public CodeIdentifier Idenfitifer { get; set; }
    }
}
