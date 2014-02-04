using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public abstract class ITLocalScope: ITScope
    {
        
        public IDictionary<string, ITEntity> Children { get; set; }

        public ITLocalScope()
        {
            Children = new Dictionary<string, ITEntity>();
        }

        public override ITEntity GetChildEntity(string ent)
        {
            if (Children.ContainsKey(ent))
                return Children[ent];
            return base.GetChildEntity(ent);
        }

        public override void AddChildEntity(ITEntity ent)
        {
            Children.Add(ent.Name, ent);
        }
    }
}
