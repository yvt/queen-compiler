using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public abstract class ITStatement: ITObject
    {
        public virtual ITEntity GetChildEntity(string ent)
        {
            // TODO: error report?
            return null;
        }

        public abstract T Accept<T>(IITStatementVisitor<T> visitor);
    }
}
