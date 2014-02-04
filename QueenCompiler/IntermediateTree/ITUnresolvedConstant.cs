using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public abstract class ITUnresolvedConstantExpression : ITExpression
    {
        private bool beingResolved;
        private bool resolved;

        protected abstract void SetValue(ITExpression result);
        protected abstract void CircularReferenceDetected();
        protected abstract void NonConstantDetected();
        protected abstract ITExpression Evaluate();

        public void Resolve()
        {
            if (resolved)
            {
                return;
            }
            if (beingResolved)
            {
                CircularReferenceDetected();
                resolved = true;
                return;
            }
            beingResolved = true;
            // TODO: evaluate
            ITExpression expr = Evaluate();
            if (!(expr is ITValueExpression))
            {
                NonConstantDetected();
            }
            else
            {
                SetValue(expr);
            }
            beingResolved = false;
            resolved = true;
        }

        public override T Accept<T>(IITExpressionVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}
