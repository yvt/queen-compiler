using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public abstract class ITStorage : ITExpression
    {
    }
    public sealed class ITMemberVariableStorage: ITStorage
    {
        public ITExpression Instance { get; set; }
        public ITMemberVariable Member { get; set; }
        public override T Accept<T>(IITExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
    public sealed class ITMemberPropertyStorage : ITStorage
    {
        public ITExpression Instance { get; set; }
        public ITMemberProperty Member { get; set; }
        public IList<ITExpression> Parameters { get; set; }
        public override T Accept<T>(IITExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
    public sealed class ITGlobalVariableStorage: ITStorage
    {
        public ITGlobalVariableEntity Variable { get; set; }
        public ITType[] GenericTypeParameters { get; set; }
        public override T Accept<T>(IITExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
    public sealed class ITLocalVariableStorage : ITStorage
    {
        public ITLocalVariable Variable { get; set; }
        public override T Accept<T>(IITExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
    public sealed class ITParameterStorage: ITStorage
    {
        public ITFunctionParameter Variable { get; set; }
        public override T Accept<T>(IITExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
    public sealed class ITArrayElementStorage : ITStorage
    {
        public ITExpression Variable { get; set; }
        public IList<ITExpression> Indices { get; set; }
        public ITArrayElementStorage()
        {
            Indices = new List<ITExpression>();
        }
        public override T Accept<T>(IITExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
    public sealed class ITGlobalFunctionStorage : ITStorage
    {
        public ITFunctionEntity Function { get; set; }
        public override T Accept<T>(IITExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
    public sealed class ITMemberFunctionStorage : ITStorage
    {
        public ITExpression Object { get; set; }
        public ITMemberFunction Function { get; set; }
        public ITType[] GenericTypeParameters { get; set; }
        public override T Accept<T>(IITExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
    public enum ITAssignType
    {
        AdditionAssign,
        SubtractionAssign,
        DivisionAssign,
        MultiplicationAssign,
        PowerAssign,
        ModulusAssign,
        ConcatAssign,
        AndAssign,
        OrAssign,
        Assign
    }
    public class ITAssignExpression: ITExpression
    {
        public ITStorage Storage { get; set; }
        public ITExpression Value { get; set; }
        public ITAssignType AssignType { get; set; }
        public override T Accept<T>(IITExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }

    public class ITReferenceCalleeExpression : ITExpression
    {
        public override T Accept<T>(IITExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }


}
