using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public class ITMemberVariable: ITMember
    {
        public string Name { get; set; }
        public ITType Type { get; set; }
        public bool IsConst { get; set; }
        public ITExpression InitialValue { get; set; }

        public override string ToString()
        {
            return (IsConst ? "const" : "var") + " " + Name + ":" + Type.ToString();
        }
    }

    public sealed class ITMutatedMemberVariable : ITMemberVariable
    {
        public ITMemberVariable Base;
    }
}
