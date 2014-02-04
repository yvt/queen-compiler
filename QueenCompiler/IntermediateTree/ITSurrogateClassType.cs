using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public class ITSurrogateClassType: ITClassType
    {
        int index = 0;
        public string DisplayName { get; set; }
        public ITFunctionBody AssociatedFunctionBody { get; private set; }

        public override string GetDisplayName()
        {
            return DisplayName ?? Properties.Resources.SurrogateClassDefaultName;
        }

        public ITSurrogateClassType(IntermediateCompiler ic) :
            base(ic) {
            Flags = ITClassFlags.Abstract | ITClassFlags.Sealed;
        }
        public ITSurrogateClassType(IntermediateCompiler ic, ITFunctionBody body) :
            base(ic)
        {
            Flags = ITClassFlags.Sealed;
            AssociatedFunctionBody = body;
        }
        public override void AddChildEntity(ITEntity ent)
        {
            if (ent is ITSurrogateClassEntity)
            {
                ent.Name = "$innerblock" + index.ToString();
                ((ITSurrogateClassEntity)ent).Type.Name = ent.Name;
                index += 1;
            }
            base.AddChildEntity(ent);
        }

        // remove unneeded nested entities, and returns false when itself is not needed.
        public bool Purge()
        {
            ITEntity[] ents = GetChildEntities();
            bool hasone = false;
            foreach (var ent in ents)
            {
                ITSurrogateClassEntity sur = ent as ITSurrogateClassEntity;
                if (sur != null)
                {
                    if (sur.Purge())
                    {
                        hasone = true;
                    }
                    else
                    {
                        RemoveChildEntity(ent);
                    }
                    continue;
                }
                hasone = true;
            }

            if (AssociatedFunctionBody != null)
            {
                if (AssociatedFunctionBody.HasCapturedAnything())
                {
                    hasone = true;
                }
            }

            // TODO: compact the hierarchy

            return hasone;
        }
    }

    public class ITSurrogateClassEntity : ITClassEntity
    {
        public ITSurrogateClassEntity(IntermediateCompiler iCompiler):
            base(iCompiler, new ITSurrogateClassType(iCompiler))
        {
            
        }
        public ITSurrogateClassEntity(IntermediateCompiler iCompiler, ITFunctionBody body) :
            base(iCompiler, new ITSurrogateClassType(iCompiler, body))
        {

        }

        public ITSurrogateClassType SurrogateClassType
        {
            get { return (ITSurrogateClassType)Type; }
        }

        public bool Purge()
        {
            return SurrogateClassType.Purge();
        }
    }
}
