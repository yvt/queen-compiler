using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CodeDom
{
    public abstract class CodeObject
    {
        public object UserData { get; set; }
        public CodeObject Parent { get; set; }
        public CodeLocation Location { get; set; }
        internal IntermediateTree.ITObject ITObject { get; set; }
    }
}
