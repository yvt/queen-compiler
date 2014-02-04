using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public sealed class ITCallMemberFunctionExpression: ITExpression
    {
        public ITMemberFunctionStorage Function { get; set; }
        public IList<ITExpression> Parameters { get; set; }
        public IList<ITType> GenericParameters { get; set; }

        public ITCallMemberFunctionExpression()
        {
            Parameters = new List<ITExpression>();
            GenericParameters = new List<ITType>();
        }
        public override T Accept<T>(IITExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }

    public sealed class ITCallGlobalFunctionExpression : ITExpression
    {
        public ITGlobalFunctionStorage Function { get; set; }
        public IList<ITExpression> Parameters { get; set; }
        public IList<ITType> GenericParameters { get; set; }

        public ITCallGlobalFunctionExpression()
        {
            Parameters = new List<ITExpression>();
            GenericParameters = new List<ITType>();
        }
        public override T Accept<T>(IITExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }

    public sealed class ITCallFunctionReferenceExpression : ITExpression
    {
        public ITExpression Function { get; set; }
        public ITExpression[] Parameters { get; set; }

        public ITCallFunctionReferenceExpression()
        {
        }
        public override T Accept<T>(IITExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
