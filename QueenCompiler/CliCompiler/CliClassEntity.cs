using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Queen.Language.IntermediateTree;

namespace Queen.Language.CliCompiler
{
    internal class CliClassEntity: ITClassEntity
    {
        public CliClassEntity(CliClassType type): base(type.iCompiler)
        {
            this.type = type;

            type.BindEntity(this);
            this.Name = type.ImportedType.Name;

        }

        public Type ImportedType
        {
            get
            {
                return ((CliClassType)type).ImportedType;
            }
        }

    }
}
