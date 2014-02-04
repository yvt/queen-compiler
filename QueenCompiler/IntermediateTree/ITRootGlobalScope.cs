using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public class ITRootGlobalScope: ITGlobalScope
    {
        public IDictionary<string, ITEntity> Children { get; set; }
        public string Name { get; set; }

        public ITRootGlobalScope()
        {
            Children = new Dictionary<string, ITEntity>();
        }

        public virtual ITEntity ImportExternalRootGlobalScopeEntity(string ent)
        {
            return null;
        }

        public override ITEntity GetChildEntity(string ent)
        {
            if (Children.ContainsKey(ent))
                return Children[ent];
            var e = ImportExternalRootGlobalScopeEntity(ent);
            if (e != null)
            {
                Children[ent] = e;
                return e;
            }
            return base.GetChildEntity(ent);
        }

        public override void AddChildEntity(ITEntity ent)
        {
            Children[ent.Name] = ent;
        }
    }
}
