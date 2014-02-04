using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Queen.Language.IntermediateTree;

namespace Queen.Language
{
    public partial class IntermediateCompiler
    {
        /*
         * Casts/operates on values of primitive types according to the language specification.
         */
        internal class ConstantFold
        {
            IntermediateCompiler compiler;
            public ConstantFold(IntermediateCompiler cmp)
            {
                compiler = cmp;
            }

            public object Apply(object operand, ITPrimitiveTypeType type, ITUnaryOperatorType oper)
            {
                switch (type)
                {
                    case ITPrimitiveTypeType.Bool:
                        switch (oper)
                        {
                            case ITUnaryOperatorType.Not:
                                return !(bool)operand;
                            default:
                                return null;
                        }
                    case ITPrimitiveTypeType.Integer:
                        switch (oper)
                        {
                            case ITUnaryOperatorType.Negate:
                                return checked(-(long)operand);
                            default:
                                return null;
                        }
                    case ITPrimitiveTypeType.Int8:
                        switch (oper)
                        {
                            case ITUnaryOperatorType.Negate:
                                return -(sbyte)operand;
                            default:
                                return null;
                        }
                    case ITPrimitiveTypeType.Int16:
                        switch (oper)
                        {
                            case ITUnaryOperatorType.Negate:
                                return -(short)operand;
                            default:
                                return null;
                        }
                    case ITPrimitiveTypeType.Int32:
                        switch (oper)
                        {
                            case ITUnaryOperatorType.Negate:
                                return -(int)operand;
                            default:
                                return null;
                        }
                    case ITPrimitiveTypeType.Int64:
                        switch (oper)
                        {
                            case ITUnaryOperatorType.Negate:
                                return -(long)operand;
                            default:
                                return null;
                        }
                    case ITPrimitiveTypeType.Float:
                        switch (oper)
                        {
                            case ITUnaryOperatorType.Negate:
                                return -(float)operand;
                            default:
                                return null;
                        }
                    case ITPrimitiveTypeType.Double:
                        switch (oper)
                        {
                            case ITUnaryOperatorType.Negate:
                                return -(double)operand;
                            default:
                                return null;
                        }
                    default:
                        return null;
                }
            } /* end of Apply */

            public object Apply(object left, object right, ITPrimitiveTypeType type, ITBinaryOperatorType oper)
            {
                switch (type)
                {
                    case ITPrimitiveTypeType.Bool:
                        switch (oper)
                        {
                            case ITBinaryOperatorType.And:
                                return (bool)left && (bool)right;
                            case ITBinaryOperatorType.Or:
                                return (bool)left && (bool)right;
                            case ITBinaryOperatorType.Equality:
                                return (bool)left == (bool)right;
                            case ITBinaryOperatorType.Inequality:
                                return (bool)left != (bool)right;
                            default:
                                return null;
                        }
                    case ITPrimitiveTypeType.Integer:
                        checked
                        {
                            switch (oper)
                            {
                                case ITBinaryOperatorType.Add:
                                    return (long)left + (long)right;
                                case ITBinaryOperatorType.Subtract:
                                    return (long)left - (long)right;
                                case ITBinaryOperatorType.Multiply:
                                    return (long)left * (long)right;
                                case ITBinaryOperatorType.Divide:
                                    return (long)left / (long)right;
                                case ITBinaryOperatorType.GreaterThan:
                                    return (long)left > (long)right;
                                case ITBinaryOperatorType.LessThan:
                                    return (long)left < (long)right;
                                case ITBinaryOperatorType.GreaterThanOrEqual:
                                    return (long)left >= (long)right;
                                case ITBinaryOperatorType.LessThanOrEqual:
                                    return (long)left <= (long)right;
                                case ITBinaryOperatorType.Equality:
                                    return (long)left == (long)right;
                                case ITBinaryOperatorType.Inequality:
                                    return (long)left != (long)right;
                                case ITBinaryOperatorType.Modulus:
                                    return (long)left % (long)right;
                                case ITBinaryOperatorType.Power:
                                    return (long)Queen.Kuin.CompilerServices.RuntimeHelper.IntPower64Checked((long)left, (long)right);
                                default:
                                    return null;
                            }
                        }
                    case ITPrimitiveTypeType.Int8:
                        switch (oper)
                        {
                            case ITBinaryOperatorType.Add:
                                return (sbyte)((sbyte)left + (sbyte)right);
                            case ITBinaryOperatorType.Subtract:
                                return (sbyte)((sbyte)left - (sbyte)right);
                            case ITBinaryOperatorType.Multiply:
                                return (sbyte)((sbyte)left * (sbyte)right);
                            case ITBinaryOperatorType.Divide:
                                return (sbyte)((sbyte)left / (sbyte)right);
                            case ITBinaryOperatorType.GreaterThan:
                                return ((sbyte)left > (sbyte)right);
                            case ITBinaryOperatorType.LessThan:
                                return ((sbyte)left < (sbyte)right);
                            case ITBinaryOperatorType.GreaterThanOrEqual:
                                return ((sbyte)left >= (sbyte)right);
                            case ITBinaryOperatorType.LessThanOrEqual:
                                return ((sbyte)left <= (sbyte)right);
                            case ITBinaryOperatorType.Equality:
                                return ((sbyte)left == (sbyte)right);
                            case ITBinaryOperatorType.Inequality:
                                return ((sbyte)left != (sbyte)right);
                            case ITBinaryOperatorType.Modulus:
                                return (sbyte)((sbyte)left % (sbyte)right);
                            case ITBinaryOperatorType.Power:
                                return (sbyte)Queen.Kuin.CompilerServices.RuntimeHelper.IntPower32((sbyte)left, (sbyte)right);
                            default:
                                return null;
                        }
                    case ITPrimitiveTypeType.Int16:
                        switch (oper)
                        {
                            case ITBinaryOperatorType.Add:
                                return (short)((short)left + (short)right);
                            case ITBinaryOperatorType.Subtract:
                                return (short)((short)left - (short)right);
                            case ITBinaryOperatorType.Multiply:
                                return (short)((short)left * (short)right);
                            case ITBinaryOperatorType.Divide:
                                return (short)((short)left / (short)right);
                            case ITBinaryOperatorType.GreaterThan:
                                return (short)left > (short)right;
                            case ITBinaryOperatorType.LessThan:
                                return (short)left < (short)right;
                            case ITBinaryOperatorType.GreaterThanOrEqual:
                                return (short)left >= (short)right;
                            case ITBinaryOperatorType.LessThanOrEqual:
                                return (short)left <= (short)right;
                            case ITBinaryOperatorType.Equality:
                                return (short)left == (short)right;
                            case ITBinaryOperatorType.Inequality:
                                return (short)left != (short)right;
                            case ITBinaryOperatorType.Modulus:
                                return (short)((short)left % (short)right);
                            case ITBinaryOperatorType.Power:
                                return (short)Queen.Kuin.CompilerServices.RuntimeHelper.IntPower32((short)left, (short)right);
                            default:
                                return null;
                        }
                    case ITPrimitiveTypeType.Int32:
                        switch (oper)
                        {
                            case ITBinaryOperatorType.Add:
                                return (int)((int)left + (int)right);
                            case ITBinaryOperatorType.Subtract:
                                return (int)((int)left - (int)right);
                            case ITBinaryOperatorType.Multiply:
                                return (int)((int)left * (int)right);
                            case ITBinaryOperatorType.Divide:
                                return (int)((int)left / (int)right);
                            case ITBinaryOperatorType.GreaterThan:
                                return (int)left > (int)right;
                            case ITBinaryOperatorType.LessThan:
                                return (int)left < (int)right;
                            case ITBinaryOperatorType.GreaterThanOrEqual:
                                return (int)left >= (int)right;
                            case ITBinaryOperatorType.LessThanOrEqual:
                                return (int)left <= (int)right;
                            case ITBinaryOperatorType.Equality:
                                return (int)left == (int)right;
                            case ITBinaryOperatorType.Inequality:
                                return (int)left != (int)right;
                            case ITBinaryOperatorType.Modulus:
                                return (int)((int)left % (int)right);
                            case ITBinaryOperatorType.Power:
                                return (int)Queen.Kuin.CompilerServices.RuntimeHelper.IntPower32((int)left, (int)right);
                            default:
                                return null;
                        }
                    case ITPrimitiveTypeType.Int64:
                        switch (oper)
                        {
                            case ITBinaryOperatorType.Add:
                                return (long)left + (long)right;
                            case ITBinaryOperatorType.Subtract:
                                return (long)left - (long)right;
                            case ITBinaryOperatorType.Multiply:
                                return (long)left * (long)right;
                            case ITBinaryOperatorType.Divide:
                                return (long)left / (long)right;
                            case ITBinaryOperatorType.GreaterThan:
                                return (long)left > (long)right;
                            case ITBinaryOperatorType.LessThan:
                                return (long)left < (long)right;
                            case ITBinaryOperatorType.GreaterThanOrEqual:
                                return (long)left >= (long)right;
                            case ITBinaryOperatorType.LessThanOrEqual:
                                return (long)left <= (long)right;
                            case ITBinaryOperatorType.Equality:
                                return (long)left == (long)right;
                            case ITBinaryOperatorType.Inequality:
                                return (long)left != (long)right;
                            case ITBinaryOperatorType.Modulus:
                                return (long)left % (long)right;
                            case ITBinaryOperatorType.Power:
                                return (long)Queen.Kuin.CompilerServices.RuntimeHelper.IntPower64((long)left, (long)right);
                            default:
                                return null;
                        }
                    case ITPrimitiveTypeType.UInt8:
                        switch (oper)
                        {
                            case ITBinaryOperatorType.Add:
                                return (byte)((byte)left + (byte)right);
                            case ITBinaryOperatorType.Subtract:
                                return (byte)((byte)left - (byte)right);
                            case ITBinaryOperatorType.Multiply:
                                return (byte)((byte)left * (byte)right);
                            case ITBinaryOperatorType.Divide:
                                return (byte)((byte)left / (byte)right);
                            case ITBinaryOperatorType.GreaterThan:
                                return (byte)left > (byte)right;
                            case ITBinaryOperatorType.LessThan:
                                return (byte)left < (byte)right;
                            case ITBinaryOperatorType.GreaterThanOrEqual:
                                return (byte)left >= (byte)right;
                            case ITBinaryOperatorType.LessThanOrEqual:
                                return (byte)left <= (byte)right;
                            case ITBinaryOperatorType.Equality:
                                return (byte)left == (byte)right;
                            case ITBinaryOperatorType.Inequality:
                                return (byte)left != (byte)right;
                            case ITBinaryOperatorType.Modulus:
                                return (byte)((byte)left % (byte)right);
                            case ITBinaryOperatorType.Power:
                                return (byte)Queen.Kuin.CompilerServices.RuntimeHelper.IntPowerU32((byte)left, (byte)right);
                            default:
                                return null;
                        }
                    case ITPrimitiveTypeType.UInt16:
                        switch (oper)
                        {
                            case ITBinaryOperatorType.Add:
                                return (ushort)((ushort)left + (ushort)right);
                            case ITBinaryOperatorType.Subtract:
                                return (ushort)((ushort)left - (ushort)right);
                            case ITBinaryOperatorType.Multiply:
                                return (ushort)((ushort)left * (ushort)right);
                            case ITBinaryOperatorType.Divide:
                                return (ushort)((ushort)left / (ushort)right);
                            case ITBinaryOperatorType.GreaterThan:
                                return (ushort)left > (ushort)right;
                            case ITBinaryOperatorType.LessThan:
                                return (ushort)left < (ushort)right;
                            case ITBinaryOperatorType.GreaterThanOrEqual:
                                return (ushort)left >= (ushort)right;
                            case ITBinaryOperatorType.LessThanOrEqual:
                                return (ushort)left <= (ushort)right;
                            case ITBinaryOperatorType.Equality:
                                return (ushort)left == (ushort)right;
                            case ITBinaryOperatorType.Inequality:
                                return (ushort)left != (ushort)right;
                            case ITBinaryOperatorType.Modulus:
                                return (ushort)((ushort)left % (ushort)right);
                            case ITBinaryOperatorType.Power:
                                return (ushort)Queen.Kuin.CompilerServices.RuntimeHelper.IntPowerU32((ushort)left, (ushort)right);
                            default:
                                return null;
                        }
                    case ITPrimitiveTypeType.UInt32:
                        switch (oper)
                        {
                            case ITBinaryOperatorType.Add:
                                return (uint)((uint)left + (uint)right);
                            case ITBinaryOperatorType.Subtract:
                                return (uint)((uint)left - (uint)right);
                            case ITBinaryOperatorType.Multiply:
                                return (uint)((uint)left * (uint)right);
                            case ITBinaryOperatorType.Divide:
                                return (uint)((uint)left / (uint)right);
                            case ITBinaryOperatorType.GreaterThan:
                                return (uint)left > (uint)right;
                            case ITBinaryOperatorType.LessThan:
                                return (uint)left < (uint)right;
                            case ITBinaryOperatorType.GreaterThanOrEqual:
                                return (uint)left >= (uint)right;
                            case ITBinaryOperatorType.LessThanOrEqual:
                                return (uint)left <= (uint)right;
                            case ITBinaryOperatorType.Equality:
                                return (uint)left == (uint)right;
                            case ITBinaryOperatorType.Inequality:
                                return (uint)left != (uint)right;
                            case ITBinaryOperatorType.Modulus:
                                return (uint)((uint)left % (uint)right);
                            case ITBinaryOperatorType.Power:
                                return (uint)Queen.Kuin.CompilerServices.RuntimeHelper.IntPowerU32((uint)left, (uint)right);
                            default:
                                return null;
                        }
                    case ITPrimitiveTypeType.UInt64:
                        switch (oper)
                        {
                            case ITBinaryOperatorType.Add:
                                return (ulong)left + (ulong)right;
                            case ITBinaryOperatorType.Subtract:
                                return (ulong)left - (ulong)right;
                            case ITBinaryOperatorType.Multiply:
                                return (ulong)left * (ulong)right;
                            case ITBinaryOperatorType.Divide:
                                return (ulong)left / (ulong)right;
                            case ITBinaryOperatorType.GreaterThan:
                                return (ulong)left > (ulong)right;
                            case ITBinaryOperatorType.LessThan:
                                return (ulong)left < (ulong)right;
                            case ITBinaryOperatorType.GreaterThanOrEqual:
                                return (ulong)left >= (ulong)right;
                            case ITBinaryOperatorType.LessThanOrEqual:
                                return (ulong)left <= (ulong)right;
                            case ITBinaryOperatorType.Equality:
                                return (ulong)left == (ulong)right;
                            case ITBinaryOperatorType.Inequality:
                                return (ulong)left != (ulong)right;
                            case ITBinaryOperatorType.Modulus:
                                return (ulong)left % (ulong)right;
                            case ITBinaryOperatorType.Power:
                                return (ulong)Queen.Kuin.CompilerServices.RuntimeHelper.IntPowerU64((ulong)left, (ulong)right);
                            default:
                                return null;
                        }
                    case ITPrimitiveTypeType.Float:
                        switch (oper)
                        {
                            case ITBinaryOperatorType.Add:
                                return (float)left + (float)right;
                            case ITBinaryOperatorType.Subtract:
                                return (float)left - (float)right;
                            case ITBinaryOperatorType.Multiply:
                                return (float)left * (float)right;
                            case ITBinaryOperatorType.Divide:
                                return (float)left / (float)right;
                            case ITBinaryOperatorType.GreaterThan:
                                return (float)left > (float)right;
                            case ITBinaryOperatorType.LessThan:
                                return (float)left < (float)right;
                            case ITBinaryOperatorType.GreaterThanOrEqual:
                                return (float)left >= (float)right;
                            case ITBinaryOperatorType.LessThanOrEqual:
                                return (float)left <= (float)right;
                            case ITBinaryOperatorType.Equality:
                                return (float)left == (float)right;
                            case ITBinaryOperatorType.Inequality:
                                return (float)left != (float)right;
                            case ITBinaryOperatorType.Modulus:
                                return (float)left % (float)right;
                            case ITBinaryOperatorType.Power:
                                return (float)Math.Pow((float)left, (float)right);
                            default:
                                return null;
                        }
                    case ITPrimitiveTypeType.Double:
                        switch (oper)
                        {
                            case ITBinaryOperatorType.Add:
                                return (double)left + (double)right;
                            case ITBinaryOperatorType.Subtract:
                                return (double)left - (double)right;
                            case ITBinaryOperatorType.Multiply:
                                return (double)left * (double)right;
                            case ITBinaryOperatorType.Divide:
                                return (double)left / (double)right;
                            case ITBinaryOperatorType.GreaterThan:
                                return (double)left > (double)right;
                            case ITBinaryOperatorType.LessThan:
                                return (double)left < (double)right;
                            case ITBinaryOperatorType.GreaterThanOrEqual:
                                return (double)left >= (double)right;
                            case ITBinaryOperatorType.LessThanOrEqual:
                                return (double)left <= (double)right;
                            case ITBinaryOperatorType.Equality:
                                return (double)left == (double)right;
                            case ITBinaryOperatorType.Inequality:
                                return (double)left != (double)right;
                            case ITBinaryOperatorType.Modulus:
                                return (double)left % (double)right;
                            case ITBinaryOperatorType.Power:
                                return (double)Math.Pow((double)left, (double)right);
                            default:
                                return null;
                        }
                    case ITPrimitiveTypeType.String:
                        switch (oper)
                        {
                            case ITBinaryOperatorType.Concat:
                                return string.Concat((string)left, (string)right);
                            default:
                                return null;
                        }
                    default:
                        return null;
                }
            } /* end of Apply */

            public object Cast(object operand, ITPrimitiveTypeType from, ITPrimitiveTypeType to)
            {
                switch (to)
                {
                    case ITPrimitiveTypeType.Bool:
                        if (from == ITPrimitiveTypeType.Bool)
                            return operand;
                        return null; // no primitive type is castable to bool
                    case ITPrimitiveTypeType.String:
                        if (from == ITPrimitiveTypeType.String)
                            return operand;
                        return null; // no primitive type is castable to string
                    case ITPrimitiveTypeType.Char:
                        switch (from)
                        {
                            case ITPrimitiveTypeType.Int8:
                                return Convert.ToChar((ushort)(sbyte)operand);
                            case ITPrimitiveTypeType.Int16:
                                return Convert.ToChar((ushort)(short)operand);
                            case ITPrimitiveTypeType.Int32:
                                return Convert.ToChar((ushort)(int)operand);
                            case ITPrimitiveTypeType.Int64:
                            case ITPrimitiveTypeType.Integer:
                                return Convert.ToChar((ushort)(long)operand);
                            case ITPrimitiveTypeType.UInt8:
                                return Convert.ToChar((ushort)(byte)operand);
                            case ITPrimitiveTypeType.UInt16:
                                return Convert.ToChar((ushort)operand);
                            case ITPrimitiveTypeType.UInt32:
                                return Convert.ToChar((ushort)(uint)operand);
                            case ITPrimitiveTypeType.UInt64:
                                return Convert.ToChar((ushort)(ulong)operand);
                            case ITPrimitiveTypeType.Float:
                                return Convert.ToChar((ushort)(float)operand);
                            case ITPrimitiveTypeType.Double:
                                return Convert.ToChar((ushort)(double)operand);
                            default:
                                return null;
                        }
                    case ITPrimitiveTypeType.Int8:
                        switch (from)
                        {
                            case ITPrimitiveTypeType.Int8:
                                return operand;
                            case ITPrimitiveTypeType.Int16:
                                return (sbyte)(short)operand;
                            case ITPrimitiveTypeType.Int32:
                                return (sbyte)(int)operand;
                            case ITPrimitiveTypeType.Int64:
                            case ITPrimitiveTypeType.Integer:
                                return (sbyte)(long)operand;
                            case ITPrimitiveTypeType.UInt8:
                                return (sbyte)(byte)operand;
                            case ITPrimitiveTypeType.UInt16:
                                return (sbyte)(ushort)operand;
                            case ITPrimitiveTypeType.UInt32:
                                return (sbyte)(uint)operand;
                            case ITPrimitiveTypeType.UInt64:
                                return (sbyte)(ulong)operand;
                            case ITPrimitiveTypeType.Char:
                                return (sbyte)(char)operand;
                            case ITPrimitiveTypeType.Float:
                                return (sbyte)(float)operand;
                            case ITPrimitiveTypeType.Double:
                                return (sbyte)(double)operand;
                            default:
                                return null;
                        }
                    case ITPrimitiveTypeType.Int16:
                        switch (from)
                        {
                            case ITPrimitiveTypeType.Int8:
                                return (short)(sbyte)operand;
                            case ITPrimitiveTypeType.Int16:
                                return (short)(short)operand;
                            case ITPrimitiveTypeType.Int32:
                                return (short)(int)operand;
                            case ITPrimitiveTypeType.Int64:
                            case ITPrimitiveTypeType.Integer:
                                return (short)(long)operand;
                            case ITPrimitiveTypeType.UInt8:
                                return (short)(byte)operand;
                            case ITPrimitiveTypeType.UInt16:
                                return (short)(ushort)operand;
                            case ITPrimitiveTypeType.UInt32:
                                return (short)(uint)operand;
                            case ITPrimitiveTypeType.UInt64:
                                return (short)(ulong)operand;
                            case ITPrimitiveTypeType.Char:
                                return (short)(char)operand;
                            case ITPrimitiveTypeType.Float:
                                return (short)(float)operand;
                            case ITPrimitiveTypeType.Double:
                                return (short)(double)operand;
                            default:
                                return null;
                        }
                    case ITPrimitiveTypeType.Int32:
                        switch (from)
                        {
                            case ITPrimitiveTypeType.Int8:
                                return (int)(sbyte)operand;
                            case ITPrimitiveTypeType.Int16:
                                return (int)(short)operand;
                            case ITPrimitiveTypeType.Int32:
                                return (int)(int)operand;
                            case ITPrimitiveTypeType.Int64:
                            case ITPrimitiveTypeType.Integer:
                                return (int)(long)operand;
                            case ITPrimitiveTypeType.UInt8:
                                return (int)(byte)operand;
                            case ITPrimitiveTypeType.UInt16:
                                return (int)(ushort)operand;
                            case ITPrimitiveTypeType.UInt32:
                                return (int)(uint)operand;
                            case ITPrimitiveTypeType.UInt64:
                                return (int)(ulong)operand;
                            case ITPrimitiveTypeType.Char:
                                return (int)(char)operand;
                            case ITPrimitiveTypeType.Float:
                                return (int)(float)operand;
                            case ITPrimitiveTypeType.Double:
                                return (int)(double)operand;
                            default:
                                return null;
                        }
                    case ITPrimitiveTypeType.Integer:
                        checked
                        {
                            switch (from)
                            {
                                case ITPrimitiveTypeType.Int8:
                                    return (long)(sbyte)operand;
                                case ITPrimitiveTypeType.Int16:
                                    return (long)(short)operand;
                                case ITPrimitiveTypeType.Int32:
                                    return (long)(int)operand;
                                case ITPrimitiveTypeType.Int64:
                                case ITPrimitiveTypeType.Integer:
                                    return (long)(long)operand;
                                case ITPrimitiveTypeType.UInt8:
                                    return (long)(byte)operand;
                                case ITPrimitiveTypeType.UInt16:
                                    return (long)(ushort)operand;
                                case ITPrimitiveTypeType.UInt32:
                                    return (long)(uint)operand;
                                case ITPrimitiveTypeType.UInt64:
                                    return (long)(ulong)operand;
                                case ITPrimitiveTypeType.Char:
                                    return (long)(char)operand;
                                case ITPrimitiveTypeType.Float:
                                    return (long)(float)operand;
                                case ITPrimitiveTypeType.Double:
                                    return (long)(double)operand;
                                default:
                                    return null;
                            }
                        }
                    case ITPrimitiveTypeType.Int64:
                        switch (from)
                        {
                            case ITPrimitiveTypeType.Int8:
                                return (long)(sbyte)operand;
                            case ITPrimitiveTypeType.Int16:
                                return (long)(short)operand;
                            case ITPrimitiveTypeType.Int32:
                                return (long)(int)operand;
                            case ITPrimitiveTypeType.Int64:
                            case ITPrimitiveTypeType.Integer:
                                return (long)(long)operand;
                            case ITPrimitiveTypeType.UInt8:
                                return (long)(byte)operand;
                            case ITPrimitiveTypeType.UInt16:
                                return (long)(ushort)operand;
                            case ITPrimitiveTypeType.UInt32:
                                return (long)(uint)operand;
                            case ITPrimitiveTypeType.UInt64:
                                return (long)(ulong)operand;
                            case ITPrimitiveTypeType.Char:
                                return (long)(char)operand;
                            case ITPrimitiveTypeType.Float:
                                return (long)(float)operand;
                            case ITPrimitiveTypeType.Double:
                                return (long)(double)operand;
                            default:
                                return null;
                        }
                    case ITPrimitiveTypeType.UInt8:
                        switch (from)
                        {
                            case ITPrimitiveTypeType.Int8:
                                return (byte)(sbyte)operand;
                            case ITPrimitiveTypeType.Int16:
                                return (byte)(short)operand;
                            case ITPrimitiveTypeType.Int32:
                                return (byte)(int)operand;
                            case ITPrimitiveTypeType.Int64:
                            case ITPrimitiveTypeType.Integer:
                                return (byte)(long)operand;
                            case ITPrimitiveTypeType.UInt8:
                                return (byte)(byte)operand;
                            case ITPrimitiveTypeType.UInt16:
                                return (byte)(ushort)operand;
                            case ITPrimitiveTypeType.UInt32:
                                return (byte)(uint)operand;
                            case ITPrimitiveTypeType.UInt64:
                                return (byte)(ulong)operand;
                            case ITPrimitiveTypeType.Char:
                                return (byte)(char)operand;
                            case ITPrimitiveTypeType.Float:
                                return (byte)(float)operand;
                            case ITPrimitiveTypeType.Double:
                                return (byte)(double)operand;
                            default:
                                return null;
                        }
                    case ITPrimitiveTypeType.UInt16:
                        switch (from)
                        {
                            case ITPrimitiveTypeType.Int8:
                                return (ushort)(sbyte)operand;
                            case ITPrimitiveTypeType.Int16:
                                return (ushort)(short)operand;
                            case ITPrimitiveTypeType.Int32:
                                return (ushort)(int)operand;
                            case ITPrimitiveTypeType.Int64:
                            case ITPrimitiveTypeType.Integer:
                                return (ushort)(long)operand;
                            case ITPrimitiveTypeType.UInt8:
                                return (ushort)(byte)operand;
                            case ITPrimitiveTypeType.UInt16:
                                return (ushort)(ushort)operand;
                            case ITPrimitiveTypeType.UInt32:
                                return (ushort)(uint)operand;
                            case ITPrimitiveTypeType.UInt64:
                                return (ushort)(ulong)operand;
                            case ITPrimitiveTypeType.Char:
                                return (ushort)(char)operand;
                            case ITPrimitiveTypeType.Float:
                                return (ushort)(float)operand;
                            case ITPrimitiveTypeType.Double:
                                return (ushort)(double)operand;
                            default:
                                return null;
                        }
                    case ITPrimitiveTypeType.UInt32:
                        switch (from)
                        {
                            case ITPrimitiveTypeType.Int8:
                                return (uint)(sbyte)operand;
                            case ITPrimitiveTypeType.Int16:
                                return (uint)(short)operand;
                            case ITPrimitiveTypeType.Int32:
                                return (uint)(int)operand;
                            case ITPrimitiveTypeType.Int64:
                            case ITPrimitiveTypeType.Integer:
                                return (uint)(long)operand;
                            case ITPrimitiveTypeType.UInt8:
                                return (uint)(byte)operand;
                            case ITPrimitiveTypeType.UInt16:
                                return (uint)(ushort)operand;
                            case ITPrimitiveTypeType.UInt32:
                                return (uint)(uint)operand;
                            case ITPrimitiveTypeType.UInt64:
                                return (uint)(ulong)operand;
                            case ITPrimitiveTypeType.Char:
                                return (uint)(char)operand;
                            case ITPrimitiveTypeType.Float:
                                return (uint)(float)operand;
                            case ITPrimitiveTypeType.Double:
                                return (uint)(double)operand;
                            default:
                                return null;
                        }
                    case ITPrimitiveTypeType.UInt64:
                        switch (from)
                        {
                            case ITPrimitiveTypeType.Int8:
                                return (ulong)(sbyte)operand;
                            case ITPrimitiveTypeType.Int16:
                                return (ulong)(short)operand;
                            case ITPrimitiveTypeType.Int32:
                                return (ulong)(int)operand;
                            case ITPrimitiveTypeType.Int64:
                            case ITPrimitiveTypeType.Integer:
                                return (ulong)(long)operand;
                            case ITPrimitiveTypeType.UInt8:
                                return (ulong)(byte)operand;
                            case ITPrimitiveTypeType.UInt16:
                                return (ulong)(ushort)operand;
                            case ITPrimitiveTypeType.UInt32:
                                return (ulong)(uint)operand;
                            case ITPrimitiveTypeType.UInt64:
                                return (ulong)(ulong)operand;
                            case ITPrimitiveTypeType.Char:
                                return (ulong)(char)operand;
                            case ITPrimitiveTypeType.Float:
                                return (ulong)(float)operand;
                            case ITPrimitiveTypeType.Double:
                                return (ulong)(double)operand;
                            default:
                                return null;
                        }
                    case ITPrimitiveTypeType.Float:
                        switch (from)
                        {
                            case ITPrimitiveTypeType.Int8:
                                return (float)(sbyte)operand;
                            case ITPrimitiveTypeType.Int16:
                                return (float)(short)operand;
                            case ITPrimitiveTypeType.Int32:
                                return (float)(int)operand;
                            case ITPrimitiveTypeType.Int64:
                            case ITPrimitiveTypeType.Integer:
                                return (float)(long)operand;
                            case ITPrimitiveTypeType.UInt8:
                                return (float)(byte)operand;
                            case ITPrimitiveTypeType.UInt16:
                                return (float)(ushort)operand;
                            case ITPrimitiveTypeType.UInt32:
                                return (float)(uint)operand;
                            case ITPrimitiveTypeType.UInt64:
                                return (float)(ulong)operand;
                            case ITPrimitiveTypeType.Char:
                                return (float)(char)operand;
                            case ITPrimitiveTypeType.Float:
                                return (float)operand;
                            case ITPrimitiveTypeType.Double:
                                return (float)(double)operand;
                            default:
                                return null;
                        }
                    case ITPrimitiveTypeType.Double:
                        switch (from)
                        {
                            case ITPrimitiveTypeType.Int8:
                                return (double)(sbyte)operand;
                            case ITPrimitiveTypeType.Int16:
                                return (double)(short)operand;
                            case ITPrimitiveTypeType.Int32:
                                return (double)(int)operand;
                            case ITPrimitiveTypeType.Int64:
                            case ITPrimitiveTypeType.Integer:
                                return (double)(long)operand;
                            case ITPrimitiveTypeType.UInt8:
                                return (double)(byte)operand;
                            case ITPrimitiveTypeType.UInt16:
                                return (double)(ushort)operand;
                            case ITPrimitiveTypeType.UInt32:
                                return (double)(uint)operand;
                            case ITPrimitiveTypeType.UInt64:
                                return (double)(ulong)operand;
                            case ITPrimitiveTypeType.Char:
                                return (double)(char)operand;
                            case ITPrimitiveTypeType.Float:
                                return (double)(float)operand;
                            case ITPrimitiveTypeType.Double:
                                return (double)operand;
                            default:
                                return null;
                        }
                    default:
                        return null;
                }
            } /* end of Cast */

            public ITExpression TryFold(ITBinaryOperatorExpression expr)
            {
                ITValueExpression val1 = expr.Left as ITValueExpression;
                ITValueExpression val2 = expr.Right as ITValueExpression;
                if (val1 == null || val2 == null)
                    return expr;

                ITPrimitiveType prim1 = expr.Left.ExpressionType as ITPrimitiveType;
                ITPrimitiveType prim2 = expr.Right.ExpressionType as ITPrimitiveType;
                if (prim1 == null || prim2 == null || prim1.Type != prim2.Type)
                    return expr;

                ITValueExpression val = new ITValueExpression();
                val.ExpressionType = val1.ExpressionType;
                switch (expr.OperatorType)
                {
                    case ITBinaryOperatorType.Equality:
                    case ITBinaryOperatorType.Inequality:
                    case ITBinaryOperatorType.GreaterThan:
                    case ITBinaryOperatorType.GreaterThanOrEqual:
                    case ITBinaryOperatorType.LessThan:
                    case ITBinaryOperatorType.LessThanOrEqual:
                        val.ExpressionType = compiler.GetPrimitiveType(ITPrimitiveTypeType.Bool);
                        break;
                }
                val.Location = expr.Location;
                try
                {
                    val.Value = Apply(val1.Value, val2.Value, prim1.Type, expr.OperatorType);
                    if (val.Value == null)
                    {
                        compiler.ReportError(Properties.Resources.InternalError, expr.Location);
                        return compiler.CreateErrorExpression(val.ExpressionType);
                    }
                    return val;
                }
                catch (OverflowException)
                {
                    compiler.ReportError(Properties.Resources.ICOverflow, expr.Location);
                    return compiler.CreateErrorExpression(val.ExpressionType);
                }
                catch (DivideByZeroException)
                {
                    compiler.ReportError(Properties.Resources.ICDivideByZero, expr.Location);
                    return compiler.CreateErrorExpression(val.ExpressionType);
                }
            }

            public ITExpression TryFold(ITUnaryOperatorExpression expr)
            {
                ITValueExpression val1 = expr.Expression as ITValueExpression;
                if (val1 == null)
                    return expr;

                ITPrimitiveType prim1 = expr.Expression.ExpressionType as ITPrimitiveType;
                if (prim1 == null)
                    return expr;

                ITValueExpression val = new ITValueExpression();
                val.ExpressionType = val1.ExpressionType;
                val.Location = expr.Location;
                try
                {
                    val.Value = Apply(val1.Value, prim1.Type, expr.Type);
                    if (val.Value == null)
                    {
                        compiler.ReportError(Properties.Resources.InternalError, expr.Location);
                        return compiler.CreateErrorExpression(val.ExpressionType);
                    }
                    return val;
                }
                catch (OverflowException)
                {
                    compiler.ReportError(Properties.Resources.ICOverflow, expr.Location);
                    return compiler.CreateErrorExpression(val.ExpressionType);
                }
            }



        }
    }
}
