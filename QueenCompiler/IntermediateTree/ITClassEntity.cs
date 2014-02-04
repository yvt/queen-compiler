using System;
using System.Collections.Generic;
using System.Text;
using Queen.Language.CodeDom;

namespace Queen.Language.IntermediateTree
{
    public class ITClassEntity: ITEntity, IITSubScopedEntity
    {
        protected ITClassType type;

        public ITClassType Type
        {
            get
            {
                return type;
            }
        }
        public CodeClassStatement Statement { get; set; }

        public ITClassEntity(IntermediateCompiler iCompiler)
        {
            type = new ITClassType(iCompiler);
            type.BindEntity(this);
        }

        public ITClassEntity(IntermediateCompiler iCompiler, ITClassType cls)
        {
            type = cls;
            type.BindEntity(this);
        }
        
        public ITGlobalScope Scope
        {
            get { return type; }
        }

    }
}
