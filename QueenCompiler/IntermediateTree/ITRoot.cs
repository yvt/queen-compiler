using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public class ITRoot: ITObject
    {
        public IDictionary<string, ITRootGlobalScope> Children { get; set; }

        public ITRoot()
        {
            Children = new Dictionary<string, ITRootGlobalScope>();
        }

        public virtual ITRootGlobalScope CreateRootGlobalScope(string ent)
        {
            ITRootGlobalScope scp = new ITRootGlobalScope();
            scp.Root = this;
            scp.Name = ent;
            return scp;
        }

        public ITRootGlobalScope GetRootGlobalScope(string ent)
        {
            if (ent == "Kuin")
                return GetRootGlobalScope("Q");
            if (Children.ContainsKey(ent))
                return Children[ent];

            var scp = CreateRootGlobalScope(ent);
            Children[ent] = scp;
            return scp;
        }
    }
}
