using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public abstract class ITObject
    {
        public CodeDom.CodeLocation Location { get; set; }
        public ITRoot Root { get { return null;  } set { } } // TODO: delete this
        public object UserData { get; set; }
    }
}
