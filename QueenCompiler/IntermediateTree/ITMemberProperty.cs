using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public class ITMemberProperty: ITMember
    {
        public string Name { get; set; }
        public ITType Type { get; set; }
        public IList<ITFunctionParameter> Parameters { get; set; }
        public ITFunctionBody Setter { get; set; }
        public ITFunctionBody Getter { get; set; }

        // used while checking overrides
        public bool MarkedAsOverriding { get; set; }

        // set while checking override, might be null for imported types
        public ITMemberProperty OverriddenMember { get; set; }

        public ITMemberProperty()
        {
            Parameters = new List<ITFunctionParameter>();
        }
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("property ");
            sb.Append(Name);
            sb.Append('(');
            var prms = Parameters;
            for (int i = 0; i < prms.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(prms[i].Name);
                sb.Append(':');
                if (prms[i].IsByRef)
                    sb.Append('&');
                sb.Append(prms[i].Type.ToString());
            }
            sb.Append(')');
            ITType ret = Type;
            if (ret != null)
            {
                sb.Append(':');
                sb.Append(ret.ToString());
            }
            return sb.ToString();
        }
    }

    public sealed class ITMutatedMemberProperty : ITMemberProperty
    {
        public ITMemberProperty Base;
    }
}
