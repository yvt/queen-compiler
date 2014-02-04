using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public class CodeEnumStatement : CodeStatement
    {
        public IList<CodeEnumItem> Items { get; set; }
        public CodeIdentifier Name { get; set; }

        public CodeEnumStatement()
        {
            Items = new List<CodeEnumItem>();
        }
        public override T Accept<T>(ICodeStatementVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
