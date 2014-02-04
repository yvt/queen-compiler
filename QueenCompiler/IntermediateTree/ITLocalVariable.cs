using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public sealed class ITLocalVariable: ITObject
    {
        public string Name { get; set; }
        public ITType Type { get; set; }
        public bool IsConst { get; set; }
        public ITExpression ConstantValue { get; set; }

        /// <summary>
        /// If this is true, this variable is not explicitly initialized and must be initialized to a default value.
        /// </summary>
        public bool ShouldBeInitializedToDefaultValue { get; set; }
    }
}
