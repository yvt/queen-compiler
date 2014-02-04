using System;
using System.Collections.Generic;
using System.Text;
using Queen.Language.IntermediateTree;
using Queen.Language.CodeDom;

namespace Queen.Language
{
    public partial class IntermediateCompiler
    {
        private static object ConvertNum<T>(object val)
        {
            return (T)val;
        }

        private ITExpression CastForced(ITExpression expr,
            ITType typ, CodeLocation loc)
        {
            ITType fromType = expr.ExpressionType;
            if (typ == fromType)
                return expr;
            // do conversion in compile-time
            if (expr is ITValueExpression &&
                typ is ITPrimitiveType &&
                    fromType is ITPrimitiveType)
            {
                ITPrimitiveTypeType fromPrim = ((ITPrimitiveType)fromType).Type;
                ITPrimitiveTypeType toPrim = ((ITPrimitiveType)typ).Type;
                ITValueExpression oldExpr = (ITValueExpression)expr;
                ITValueExpression newExpr = new ITValueExpression();
                newExpr.ExpressionType = typ;
                newExpr.Location = oldExpr.Location;
                newExpr.Root = oldExpr.Root;
                try
                {
                    newExpr.Value = constantFold.Cast(oldExpr.Value, fromPrim, toPrim);
                    if (newExpr.Value == null)
                    {
                        ReportError(string.Format(Properties.Resources.ICInvalidConstantCast,
                        oldExpr.Value, oldExpr.ExpressionType.ToString(), typ.ToString()), loc);
                        return CreateErrorExpression(typ);
                    }
                }
                catch (InvalidCastException)
                {
                    ReportError(string.Format(Properties.Resources.ICInvalidConstantCast,
                        oldExpr.Value, oldExpr.ExpressionType.ToString(), typ.ToString()), loc);
                    return CreateErrorExpression(typ);
                }
                return newExpr;
            }
            else
            {
                ITCastExpression cast = new ITCastExpression();
                cast.CastTarget = typ;
                cast.Expression = expr;
                cast.ExpressionType = typ;
                cast.Location = loc;
                cast.Root = root;
                return cast;
            }
        }

        private void CastImplicitly(ITExpression expr1, ITExpression expr2, CodeLocation loc,
            out ITExpression outExpr1, out ITExpression outExpr2)
        {
            ITType typ1 = expr1.ExpressionType;
            ITType typ2 = expr2.ExpressionType;
            outExpr1 = expr1;
            outExpr2 = expr2;

            if (typ1.Equals(typ2))
                return;

            // special case
            ITPrimitiveType prim1 = typ1 as ITPrimitiveType;
            ITPrimitiveType prim2 = typ2 as ITPrimitiveType;
            if (prim1 != null && prim2 != null)
            {
                if (prim1.Type == ITPrimitiveTypeType.Int64 &&
                    prim2.Type == ITPrimitiveTypeType.Integer)
                {
                    outExpr1 = CastForced(expr1, prim2, loc);
                    return;
                }
                if (prim1.Type == ITPrimitiveTypeType.Integer &&
                    prim2.Type == ITPrimitiveTypeType.Int64)
                {
                    outExpr2 = CastForced(expr2, prim1, loc);
                    return;
                }
            }

            if (typ1.CanBeCastedTo(typ2, true) ||
                typ2.CanBeCastedFrom(typ1, true))
            {
                outExpr1 = CastForced(expr1, typ2, loc);
            }
            else if (typ2.CanBeCastedTo(typ1, true) ||
               typ1.CanBeCastedFrom(typ2, true))
            {
                outExpr2 = CastForced(expr2, typ1, loc);
            }
            else
            {
                if (typ2.CanBeCastedTo(typ1, true) ||
               typ1.CanBeCastedFrom(typ2, true) ||
                    typ2.CanBeCastedTo(typ1, false) ||
               typ1.CanBeCastedFrom(typ2, false))
                {
                    ReportError(string.Format(Properties.Resources.ICInvalidImplicitCastBiDi,
                        typ1.ToString(), typ2.ToString()), loc);
                }
                else
                {
                    ReportError(string.Format(Properties.Resources.ICInvalidImplicitCastNoExplicitBiDi,
                        typ1.ToString(), typ2.ToString()), loc);
                }
                outExpr2 = CreateErrorExpression();
                outExpr2.ExpressionType = typ1;
            }
        }

        // handles certain cases where indexer type is required but int64 is given.
        private ITExpression CastImplicitToIndexerType(ITExpression expr, CodeLocation loc)
        {
            if (expr == null) return null;
            ITPrimitiveType prim = expr.ExpressionType as ITPrimitiveType;
            if (prim == null || prim.Type != ITPrimitiveTypeType.Integer) return expr;
            return CastForced(expr, GetIndexerType(), loc);
        }

        private ITExpression CastImplicitly(ITExpression expr,
            ITType typ, CodeLocation loc)
        {
            if (expr == null)
                return expr;
            if (typ == null)
                return expr;
            if (expr.ExpressionType == null)
            {
                // void!
                ReportError(string.Format(Properties.Resources.ICInvalidImplicitCastNoExplicit,
                    "void", typ.ToString()), loc);
                return CreateErrorExpression(typ);
            }
            if (expr is ITErrorExpression)
            {
                expr.ExpressionType = typ;
                return expr;
            }
            ITType fromType = expr.ExpressionType;
            if (typ.Equals(fromType))
                return expr;
            if (fromType.CanBeCastedTo(typ, true) ||
                typ.CanBeCastedFrom(fromType, true))
            {
                return CastForced(expr, typ, loc);
            }
            else
            {
                if (expr.ExpressionType.CanBeCastedTo(typ, false) ||
                    typ.CanBeCastedFrom(expr.ExpressionType, false))
                {
                    ReportError(string.Format(Properties.Resources.ICInvalidImplicitCast,
                        fromType.ToString(), typ.ToString()), loc);
                }
                else
                {
                    ReportError(string.Format(Properties.Resources.ICInvalidImplicitCastNoExplicit,
                        fromType.ToString(), typ.ToString()), loc);
                }
                return expr;
            }
        }
    }
}
