using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public abstract class ITScope: ITObject
    {
        public ITScope ParentScope { get; set; }


        public virtual ITEntity GetChildEntity(string ent)
        {
            return null;
        }

        public virtual ITEntity[] GetChildEntities()
        {
            return new ITEntity[] { };
        }

        public virtual void AddChildEntity(ITEntity ent)
        {
            throw new IntermediateCompilerException(Properties.Resources.InternalError);
        }

        public T FindSurroundingScope<T>() where T : class
        {
            T v = this as T;
            if (v != null) return v;
            if (ParentScope == null) return null;
            return ParentScope.FindSurroundingScope<T>();
        }

        public virtual int GetNumLocalGenericParameters()
        {
            return 0;
        }

        public ITGenericTypeParameter[] GetStackedGenericParameters()
        {
            ITGenericTypeParameter[] funcParam = null;
            ITGenericTypeParameter[] typeParam = new ITGenericTypeParameter[] { };
            ITScope scp = this;
            while (scp != null)
            {
                ITFunctionBody funcScope = scp as ITFunctionBody;
                if (funcScope != null)
                {
                    if (funcParam != null)
                    {
                        throw new InvalidOperationException("nested function");
                    }
                    funcParam = funcScope.GenericParameters;
                    scp = scp.ParentScope;
                    continue;
                }

                ITType typeScope = scp as ITType;
                if (typeScope != null)
                {
                    ITType[] gn = typeScope.GetGenericParameters();
                    typeParam = new ITGenericTypeParameter[gn.Length];
                    for (int i = 0; i < gn.Length; i++)
                        typeParam[i] = (ITGenericTypeParameter)gn[i];
                    break;
                }

                scp = scp.ParentScope;
            }
            if (funcParam == null)
                return typeParam;
            return Queen.Kuin.CompilerServices.RuntimeHelper.ArrayConcat<ITGenericTypeParameter>(typeParam, funcParam);
        }
    }
}
