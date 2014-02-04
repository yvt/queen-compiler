using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public class ITFunctionEntity: ITEntity
    {
        public ITFunctionBody Body { get; set; }

        public bool IsRootGlobalScopePrivate { get; set; }


        public virtual ITType GetReturnType()
        {
            return Body.ReturnType;
        }
        public virtual ITType[] GetParameterTypes()
        {
            var prms = Body.Parameters;
            var ret = new ITType[prms.Count];
            for (int i = 0; i < ret.Length; i++)
            {
                ITFunctionParameter prm = prms[i];
                ret[i] = prm.Type;
            }
            return ret;
        }

        public virtual ITMutatedFunctionEntity MakeGenericFunction(ITType[] type)
        {
            var mutator = new IntermediateCompiler.GenericTypeMutator(Body.GetStackedGenericParameters(), type);
            var mutated = new ITMutatedFunctionEntity();
            mutated.BaseEntity = this;
            mutated.mutatedReturnType = mutator.Mutate(GetReturnType());

            var prms = GetParameterTypes();
            for (int i = 0; i < prms.Length; i++)
                prms[i] = mutator.Mutate(prms[i]);

            mutated.mutatedParameterTypes = prms;
            mutated.genericTypeParams = type;
            mutated.Body = Body;
            return mutated;
        }
    }

    public class ITMutatedFunctionEntity : ITFunctionEntity
    {
        public ITFunctionEntity BaseEntity;
        public ITType mutatedReturnType;
        public ITType[] mutatedParameterTypes;
        public ITType[] genericTypeParams;

        public override ITType GetReturnType()
        {
            return mutatedReturnType;
        }
        public override ITType[] GetParameterTypes()
        {
            return mutatedParameterTypes;
        }

        public override ITMutatedFunctionEntity MakeGenericFunction(ITType[] type)
        {
            return BaseEntity.MakeGenericFunction(type);
        }
    }
}
