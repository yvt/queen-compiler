using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public interface IITSubScopedEntity
    {
        ITGlobalScope Scope { get; }
    }
}
