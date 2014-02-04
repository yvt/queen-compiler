using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public class CodeGenericsEntitySpecifier: CodeEntitySpecifier
    {
        public CodeEntitySpecifier GenericEntity { get; set; }
        public IList<CodeType> GenericParameters { get; set; }
        public CodeGenericsEntitySpecifier()
        {
            GenericParameters = new List<CodeType>();
        }
    }
}
