using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public sealed class ITGlobalVariableEntity: ITEntity
    {
        public ITType Type { get; set; }
        public bool IsConst { get; set; }

        public ITExpression InitialValue { get; set; }

    }
}
