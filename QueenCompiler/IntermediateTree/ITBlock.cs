using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public class ITBlock: ITLocalScope
    {
        public IDictionary<string, ITLocalVariable> LocalVariables { get; set; }
        public IDictionary<string, ITExpression> VirtualLocalVariables { get; set; }
        public IList<ITStatement> Statements { get; set; }
        //public ITFunctionBody Function { get; set; }
        public ITBlock ParentBlock { get; set; }
        public bool IsLoop { get; set; }
        public string Name { get; set; }

        public bool CanControlBeTransferedOutOfBlock { get; set; }

        // surrogate class type that holds local classes/functions for the block
        // so that intermediate representation matches the final requirement (no functiones/classes inside a function)
        internal ITSurrogateClassEntity SurrogateClassEntity { get; set; }

        private ITType _InstantiatedSurrogateClassType;

        internal ITType GetInstantiatedSurrogateClassType()
        {
            if (_InstantiatedSurrogateClassType == null)
            {
                var prms = GetDeclaringFunctionBody().SurrogateGenericParameters;
                _InstantiatedSurrogateClassType = SurrogateClassEntity.Type.MakeGenericType(prms);
            }
            return _InstantiatedSurrogateClassType;
        }

        /// <summary>
        /// Auxiliary block that is used to represent the body of a looping statement.
        /// </summary>
        public ITBlock LoopBodyBlock { get; set; }

        public virtual ITFunctionBody GetDeclaringFunctionBody()
        {
            if (ParentBlock == null)
                return (ITFunctionBody)ParentScope;
            return ParentBlock.GetDeclaringFunctionBody();
        }

        public bool MayBeCalledRepeatedly
        {
            get
            {
                ITBlock block = this;
                while (block != null)
                {
                    if (block.IsLoop)
                        return true;
                    block = block.ParentBlock;
                }
                return false;
            }
        }

        public ITBlock()
        {
            LocalVariables = new Dictionary<string, ITLocalVariable>();
            VirtualLocalVariables = new Dictionary<string, ITExpression>();
            Statements = new List<ITStatement>();
            CanControlBeTransferedOutOfBlock = true;
        }

        public override ITEntity GetChildEntity(string ent)
        {
            if (SurrogateClassEntity != null)
            {
                var e = SurrogateClassEntity.Type.GetChildEntity(ent);
                if (e != null)
                {
                    if (e is ITClassEntity)
                        return e;
                    e = this.GetInstantiatedSurrogateClassType().GetChildEntity(ent);
                    return e;
                }
            }
            return base.GetChildEntity(ent);
        }
    }
}
