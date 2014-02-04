using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public abstract class ITMember: ITObject
    {
        public ITType Owner { get; set; }
        public bool IsPrivate { get; set; }
        public bool IsPublic { get; set; }
    }
}
