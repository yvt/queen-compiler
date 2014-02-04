using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.IntermediateTree
{
    public enum ITPrimitiveTypeType
    {
        Bool,
        Int8,
        Int16,
        Int32,
        Int64,
        UInt8,
        UInt16,
        UInt32,
        UInt64,
        Integer,
        Float,
        Double,
        Char,
        String,
        Void
    }
    public abstract class ITPrimitiveType: ITType
    {
        public ITPrimitiveType(IntermediateCompiler iCompiler, ITPrimitiveTypeType typ):base(iCompiler)
        {
            Type = typ;
        }
        public ITPrimitiveTypeType Type {get; set;}

        public static object ConvertToPrimitive(object obj, ITPrimitiveTypeType type)
        {
            switch (type)
            {
                case ITPrimitiveTypeType.Bool:
                    return Convert.ToBoolean(obj);
                case ITPrimitiveTypeType.Char:
                    return Convert.ToChar(obj);
                case ITPrimitiveTypeType.Int8:
                    return Convert.ToSByte(obj);
                case ITPrimitiveTypeType.Int16:
                    return Convert.ToInt16(obj);
                case ITPrimitiveTypeType.Int32:
                    return Convert.ToInt32(obj);
                case ITPrimitiveTypeType.Int64:
                case ITPrimitiveTypeType.Integer:
                    return Convert.ToInt64(obj);
                case ITPrimitiveTypeType.UInt8:
                    return Convert.ToByte(obj);
                case ITPrimitiveTypeType.UInt16:
                    return Convert.ToUInt16(obj);
                case ITPrimitiveTypeType.UInt32:
                    return Convert.ToUInt32(obj);
                case ITPrimitiveTypeType.UInt64:
                    return Convert.ToUInt64(obj);
                case ITPrimitiveTypeType.Float:
                    return Convert.ToSingle(obj);
                case ITPrimitiveTypeType.Double:
                    return Convert.ToDouble(obj);
                default:
                    throw new IntermediateCompilerException(Properties.Resources.InternalError);
            }
        }

        private bool IsNumeric()
        {
            switch (Type)
            {
                case ITPrimitiveTypeType.Int16:
                case ITPrimitiveTypeType.Int32:
                case ITPrimitiveTypeType.Int64:
                case ITPrimitiveTypeType.Integer:
                case ITPrimitiveTypeType.Int8:
                case ITPrimitiveTypeType.UInt16:
                case ITPrimitiveTypeType.UInt32:
                case ITPrimitiveTypeType.UInt64:
                case ITPrimitiveTypeType.UInt8:
                case ITPrimitiveTypeType.Float:
                case ITPrimitiveTypeType.Double:
                    return true;
                default:
                    return false;
            }
        }

        public bool IsIntegral()
        {
            switch (Type)
            {
                case ITPrimitiveTypeType.Int16:
                case ITPrimitiveTypeType.Int32:
                case ITPrimitiveTypeType.Int64:
                case ITPrimitiveTypeType.Integer:
                case ITPrimitiveTypeType.Int8:
                case ITPrimitiveTypeType.UInt16:
                case ITPrimitiveTypeType.UInt32:
                case ITPrimitiveTypeType.UInt64:
                case ITPrimitiveTypeType.UInt8:
                    return true;
                default:
                    return false;
            }
        }

        public bool IsUnsignedInteger()
        {
            switch (Type)
            {
                case ITPrimitiveTypeType.UInt16:
                case ITPrimitiveTypeType.UInt32:
                case ITPrimitiveTypeType.UInt64:
                case ITPrimitiveTypeType.UInt8:
                    return true;
                default:
                    return false;
            }
        }
        public override bool CanBeCastedTo(ITType otherType, bool implicitCast)
        {
            if (Type == ITPrimitiveTypeType.String)
            {
                if (otherType is ITArrayType)
                {
                    ITArrayType other = (ITArrayType)otherType;
                    ITType elm = other.ElementType;
                    if (elm is ITPrimitiveType &&
                        ((ITPrimitiveType)elm).Type == ITPrimitiveTypeType.Char)
                    {
                        return true;
                    }
                }

            }
            else if (Type == ITPrimitiveTypeType.Char)
            {
                if (otherType is ITPrimitiveType && ((ITPrimitiveType)otherType).IsIntegral())
                    return true;
            }
            else if(IsNumeric())
            {
                ITPrimitiveType primOther = otherType as ITPrimitiveType;
                if (primOther != null)
                {
                    if (!implicitCast)
                        return primOther.IsNumeric();

                    // allowed implicit numeric casts
                    switch (Type)
                    {
                        case ITPrimitiveTypeType.Int8:
                            switch (primOther.Type)
                            {
                                case ITPrimitiveTypeType.Int8:
                                case ITPrimitiveTypeType.Int16:
                                case ITPrimitiveTypeType.Int32:
                                case ITPrimitiveTypeType.Int64:
                                case ITPrimitiveTypeType.Integer:
                                case ITPrimitiveTypeType.Float:
                                case ITPrimitiveTypeType.Double:
                                    return true;
                                default:
                                    return false;
                            }
                        case ITPrimitiveTypeType.Int16:
                            switch (primOther.Type)
                            {
                                case ITPrimitiveTypeType.Int16:
                                case ITPrimitiveTypeType.Int32:
                                case ITPrimitiveTypeType.Int64:
                                case ITPrimitiveTypeType.Integer:
                                case ITPrimitiveTypeType.Float:
                                case ITPrimitiveTypeType.Double:
                                    return true;
                                default:
                                    return false;
                            }
                        case ITPrimitiveTypeType.Int32:
                            switch (primOther.Type)
                            {
                                case ITPrimitiveTypeType.Int32:
                                case ITPrimitiveTypeType.Int64:
                                case ITPrimitiveTypeType.Integer:
                                case ITPrimitiveTypeType.Double:
                                    return true;
                                default:
                                    return false;
                            }
                        case ITPrimitiveTypeType.Int64:
                        case ITPrimitiveTypeType.Integer:
                            switch (primOther.Type)
                            {
                                case ITPrimitiveTypeType.Int64:
                                case ITPrimitiveTypeType.Integer:
                                    return true;
                                default:
                                    return false;
                            }

                        case ITPrimitiveTypeType.UInt8:
                            switch (primOther.Type)
                            {
                                case ITPrimitiveTypeType.UInt8:
                                case ITPrimitiveTypeType.UInt16:
                                case ITPrimitiveTypeType.UInt32:
                                case ITPrimitiveTypeType.UInt64:
                                case ITPrimitiveTypeType.Int16:
                                case ITPrimitiveTypeType.Int32:
                                case ITPrimitiveTypeType.Int64:
                                case ITPrimitiveTypeType.Integer:
                                case ITPrimitiveTypeType.Float:
                                case ITPrimitiveTypeType.Double:
                                    return true;
                                default:
                                    return false;
                            }
                        case ITPrimitiveTypeType.UInt16:
                            switch (primOther.Type)
                            {
                                case ITPrimitiveTypeType.UInt16:
                                case ITPrimitiveTypeType.UInt32:
                                case ITPrimitiveTypeType.UInt64:
                                case ITPrimitiveTypeType.Int32:
                                case ITPrimitiveTypeType.Int64:
                                case ITPrimitiveTypeType.Integer:
                                case ITPrimitiveTypeType.Float:
                                case ITPrimitiveTypeType.Double:
                                    return true;
                                default:
                                    return false;
                            }
                        case ITPrimitiveTypeType.UInt32:
                            switch (primOther.Type)
                            {
                                case ITPrimitiveTypeType.UInt32:
                                case ITPrimitiveTypeType.UInt64:
                                case ITPrimitiveTypeType.Int64:
                                case ITPrimitiveTypeType.Integer:
                                case ITPrimitiveTypeType.Double:
                                    return true;
                                default:
                                    return false;
                            }
                        case ITPrimitiveTypeType.UInt64:
                            switch (primOther.Type)
                            {
                                case ITPrimitiveTypeType.UInt64:
                                    return true;
                                default:
                                    return false;
                            }
                        case ITPrimitiveTypeType.Float:
                            switch (primOther.Type)
                            {
                                case ITPrimitiveTypeType.Float:
                                case ITPrimitiveTypeType.Double:
                                    return true;
                                default:
                                    return false;
                            }
                        case ITPrimitiveTypeType.Double:
                            switch (primOther.Type)
                            {
                                case ITPrimitiveTypeType.Double:
                                    return true;
                                default:
                                    return false;
                            }
                    }
                }
            }
            return false;
        }

        public override bool CanBeCastedFrom(ITType otherType, bool implicitCast)
        {
            if (Type == ITPrimitiveTypeType.String)
            {
                if (otherType is ITArrayType)
                {
                    ITArrayType other = (ITArrayType)otherType;
                    ITType elm = other.ElementType;
                    if (elm is ITPrimitiveType &&
                        ((ITPrimitiveType)elm).Type == ITPrimitiveTypeType.Char)
                    {
                        return true;
                    }
                }

            }
            else if (Type == ITPrimitiveTypeType.Char)
            {
                if (otherType is ITPrimitiveType && ((ITPrimitiveType)otherType).IsIntegral())
                    return true;
            }
            return false;
        }

        public override bool IsComparableTo(ITType otherType)
        {
            switch (Type)
            {
                case ITPrimitiveTypeType.Char:
                    // FIXME: why no string?
                case ITPrimitiveTypeType.Float:
                case ITPrimitiveTypeType.Double:
                case ITPrimitiveTypeType.Int8:
                case ITPrimitiveTypeType.Int16:
                case ITPrimitiveTypeType.Int32:
                case ITPrimitiveTypeType.Int64:
                case ITPrimitiveTypeType.Integer:
                case ITPrimitiveTypeType.UInt8:
                case ITPrimitiveTypeType.UInt16:
                case ITPrimitiveTypeType.UInt32:
                case ITPrimitiveTypeType.UInt64:
                    return Equals(otherType);
            }
            return false;
        }

        public override bool IsValueType
        {
            get { return Type != ITPrimitiveTypeType.String; }
        }

        public override ITType Superclass
        {
            get { return null; }
        }

        private static ITType[] interfaces = new ITType[0];
        public override ITType[] Interfaces
        {
            get { return interfaces; }
        }

        public override bool Equals(object obj)
        {
            if (obj is ITPrimitiveType)
            {
                return ((ITPrimitiveType)obj).Type == Type;
            }
            else
            {
                return base.Equals(obj);
            }
        }

        public override int GetHashCode()
        {
            return Type.GetHashCode();
        }

        public override string ToString()
        {
            switch (Type)
            {
                case ITPrimitiveTypeType.Bool:
                    return "bool";
                case ITPrimitiveTypeType.Char:
                    return "char";
                case ITPrimitiveTypeType.Double:
                    return "double";
                case ITPrimitiveTypeType.Float:
                    return "float";
                case ITPrimitiveTypeType.Int8:
                    return "int8";
                case ITPrimitiveTypeType.Int16:
                    return "int16";
                case ITPrimitiveTypeType.Int32:
                    return "int32";
                case ITPrimitiveTypeType.Int64:
                    return "int64";
                case ITPrimitiveTypeType.Integer:
                    return "int";
                case ITPrimitiveTypeType.UInt8:
                    return "byte8";
                case ITPrimitiveTypeType.UInt16:
                    return "byte16";
                case ITPrimitiveTypeType.UInt32:
                    return "byte32";
                case ITPrimitiveTypeType.UInt64:
                    return "byte64";
                case ITPrimitiveTypeType.String:
                    return "string";
                case ITPrimitiveTypeType.Void:
                    return "void";
            }
            throw new InvalidOperationException();
        }

        public override bool IsSealed()
        {
            return true;
        }

        public override bool IsAbstract()
        {
            return false;
        }
    }
}
