using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public class CodeClassStatement : CodeStatement
    {
        public CodeGlobalScope Scope { get; set; }
        public IList<CodeType> BaseClasses { get; set; }
        public CodeIdentifier Name { get; set; }
        public IList<CodeIdentifier> GenericParameters { get; set; }
        public bool IsInterface { get; set; }

        public CodeClassStatement()
        {
            GenericParameters = new List<CodeIdentifier>();
            BaseClasses = new List<CodeType>();
        }
        public override T Accept<T>(ICodeStatementVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
