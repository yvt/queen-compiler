using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    /// <summary>
    /// (parentEntity)#childEntity
    /// </summary>
    public class CodeScopedEntitySpecifier : CodeEntitySpecifier
    {
        public CodeEntitySpecifier ParentEntity { get; set; }
        public CodeIdentifier Identifier { get; set; }
    }
}
