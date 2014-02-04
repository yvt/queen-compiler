using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    /// <summary>
    /// Specialized generic type for functions which doesn't use a standard syntax.
    /// Syntax: func&lt;(params...):returnType&gt;
    /// </summary>
    public class CodeFunctionType : CodeType
    {
        public CodeType ReturnType { get; set; }
        public IList<CodeParameterDeclaration> Parameters { get; set; }
    }
}
