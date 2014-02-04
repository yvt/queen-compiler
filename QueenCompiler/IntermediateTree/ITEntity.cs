using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public abstract class ITEntity: ITObject
    {
        public string Name { get; set; }
        public ITScope ParentScope { get; set; }
        public virtual bool IsPublic { get; set; }
        public virtual bool IsPrivate { get; set; }

        public ITEntity()
        {
            IsPublic = true;
        }

        public override string ToString()
        {
            return Name;
        }

    }
}
