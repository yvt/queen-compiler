using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Queen.Language.IntermediateTree;
using System.Reflection;
using System.Reflection.Emit;

namespace Queen.Language.CliCompiler
{
    public class CliPrimitiveType: ITPrimitiveType
    {
        private ITMemberFunction absFunc = null;
        private ITMemberFunction subFunc = null;
        private ITMemberFunction lenFunc = null;
        private ITMemberFunction toStrFFunc = null;
        private ITMemberFunction orFunc = null;
        private ITMemberFunction andFunc = null;
        private ITMemberFunction xorFunc = null;
        private ITMemberFunction shlFunc = null;
        private ITMemberFunction shrFunc = null;
        private ITMemberFunction sarFunc = null;

        public CliPrimitiveType(CliIntermediateCompiler iCompiler, ITPrimitiveTypeType type): base(iCompiler, type)
        {
        }

        private class AbsMemberFunction : CliVirtualMemberFunction
        {
            private CliPrimitiveType prim;
            public AbsMemberFunction(CliPrimitiveType prim)
                : base(prim, new ITFunctionParameter[] { }, "Abs")
            {
                this.prim = prim;
            }
            public override void EmitIL(System.Reflection.Emit.ILGenerator generator, CliCompiler cliCompiler)
            {
                var meth = typeof(Math).GetMethod("Abs", new Type[] { prim.GetCliType() });
                generator.Emit(OpCodes.Call, meth);
            }
        }
        private class SubMemberFunction : CliVirtualMemberFunction
        {
            private CliPrimitiveType prim;
            public SubMemberFunction(CliPrimitiveType prim)
                : base(prim, new ITFunctionParameter[] { 
                   new ITFunctionParameter(prim.iCompiler.GetBuiltinType("int")),
                   new ITFunctionParameter(prim.iCompiler.GetBuiltinType("int"))
                }, "Sub")
            {
                this.prim = prim;
            }
            public override void EmitIL(System.Reflection.Emit.ILGenerator generator, CliCompiler cliCompiler)
            {
                var meth = typeof(Queen.Kuin.CompilerServices.RuntimeHelper).GetMethod("StringSub");
                generator.Emit(OpCodes.Call, meth);
            }
        }
        private class LenMemberFunction : CliVirtualMemberFunction
        {
            private CliPrimitiveType prim;
            public LenMemberFunction(CliPrimitiveType prim)
                : base(prim.iCompiler.GetBuiltinType("int"), new ITFunctionParameter[] { }, "Len")
            {
                this.prim = prim;
            }
            public override void EmitIL(System.Reflection.Emit.ILGenerator generator, CliCompiler cliCompiler)
            {
                var meth = typeof(string).GetProperty("Length").GetGetMethod();
                generator.Emit(OpCodes.Call, meth);
                generator.Emit(OpCodes.Conv_I8);
            }
        }
        private class ToStrFMemberFunction : CliVirtualMemberFunction
        {
            private CliPrimitiveType prim;
            public ToStrFMemberFunction(CliPrimitiveType prim)
                : base(prim.iCompiler.GetPrimitiveType(ITPrimitiveTypeType.String), new ITFunctionParameter[] { 
                    new ITFunctionParameter(prim.iCompiler.GetPrimitiveType(ITPrimitiveTypeType.String))
                }, "ToStrF")
            {
                this.prim = prim;
            }
            public override void EmitIL(System.Reflection.Emit.ILGenerator generator, CliCompiler cliCompiler)
            {
                var meth = typeof(Queen.Kuin.CompilerServices.RuntimeHelper).GetMethod("ToStrF",
                    new Type[] { prim.GetCliType(), typeof(string) });
                generator.Emit(OpCodes.Call, meth);
            }
        }
        private class OrMemberFunction : CliVirtualMemberFunction
        {
            private CliPrimitiveType prim;
            public OrMemberFunction(CliPrimitiveType prim)
                : base(prim, new ITFunctionParameter[] {
                    new ITFunctionParameter(prim)
                }, "Or")
            {
                this.prim = prim;
            }
            public override void EmitIL(System.Reflection.Emit.ILGenerator generator, CliCompiler cliCompiler)
            {
                generator.Emit(OpCodes.Or);
            }
        }
        private class AndMemberFunction : CliVirtualMemberFunction
        {
            private CliPrimitiveType prim;
            public AndMemberFunction(CliPrimitiveType prim)
                : base(prim, new ITFunctionParameter[] {
                    new ITFunctionParameter(prim)
                }, "And")
            {
                this.prim = prim;
            }
            public override void EmitIL(System.Reflection.Emit.ILGenerator generator, CliCompiler cliCompiler)
            {
                generator.Emit(OpCodes.And);
            }
        }
        private class XorMemberFunction : CliVirtualMemberFunction
        {
            private CliPrimitiveType prim;
            public XorMemberFunction(CliPrimitiveType prim)
                : base(prim, new ITFunctionParameter[] {
                    new ITFunctionParameter(prim)
                }, "Xor")
            {
                this.prim = prim;
            }
            public override void EmitIL(System.Reflection.Emit.ILGenerator generator, CliCompiler cliCompiler)
            {
                generator.Emit(OpCodes.Xor);
                switch (prim.Type)
                {
                    case ITPrimitiveTypeType.Int8:
                        generator.Emit(OpCodes.Conv_I1);
                        break;
                    case ITPrimitiveTypeType.Int16:
                        generator.Emit(OpCodes.Conv_I2);
                        break;
                }
            }
        }
        private class ShlMemberFunction : CliVirtualMemberFunction
        {
            private CliPrimitiveType prim;
            public ShlMemberFunction(CliPrimitiveType prim)
                : base(prim, new ITFunctionParameter[] {
                    new ITFunctionParameter(prim.iCompiler.GetPrimitiveType(ITPrimitiveTypeType.Integer))
                }, "Shl")
            {
                this.prim = prim;
            }
            public override void EmitIL(System.Reflection.Emit.ILGenerator generator, CliCompiler cliCompiler)
            {
                generator.Emit(OpCodes.Conv_I);
                generator.Emit(OpCodes.Shl);
                switch (prim.Type)
                {
                    case ITPrimitiveTypeType.Int8:
                        generator.Emit(OpCodes.Conv_I1);
                        break;
                    case ITPrimitiveTypeType.Int16:
                        generator.Emit(OpCodes.Conv_I2);
                        break;
                }
            }
        }
        private class ShrMemberFunction : CliVirtualMemberFunction
        {
            private CliPrimitiveType prim;
            public ShrMemberFunction(CliPrimitiveType prim)
                : base(prim, new ITFunctionParameter[] {
                    new ITFunctionParameter(prim.iCompiler.GetPrimitiveType(ITPrimitiveTypeType.Integer))
                }, "Shr")
            {
                this.prim = prim;
            }
            public override void EmitIL(System.Reflection.Emit.ILGenerator generator, CliCompiler cliCompiler)
            {
                generator.Emit(OpCodes.Conv_I);
                generator.Emit(OpCodes.Shr_Un);
                switch (prim.Type)
                {
                    case ITPrimitiveTypeType.Int8:
                        generator.Emit(OpCodes.Conv_I1);
                        break;
                    case ITPrimitiveTypeType.Int16:
                        generator.Emit(OpCodes.Conv_I2);
                        break;
                }
            }
        }
        private class SarMemberFunction : CliVirtualMemberFunction
        {
            private CliPrimitiveType prim;
            public SarMemberFunction(CliPrimitiveType prim)
                : base(prim, new ITFunctionParameter[] {
                    new ITFunctionParameter(prim.iCompiler.GetPrimitiveType(ITPrimitiveTypeType.Integer))
                }, "Sar")
            {
                this.prim = prim;
            }
            public override void EmitIL(System.Reflection.Emit.ILGenerator generator, CliCompiler cliCompiler)
            {
                generator.Emit(OpCodes.Conv_I);
                generator.Emit(prim.IsUnsignedInteger() ? OpCodes.Shr_Un : OpCodes.Shr);
            }
        }
        private class NotMemberFunction : CliVirtualMemberFunction
        {
            private CliPrimitiveType prim;
            public NotMemberFunction(CliPrimitiveType prim)
                : base(prim, new ITFunctionParameter[] { }, "Abs")
            {
                this.prim = prim;
            }
            public override void EmitIL(System.Reflection.Emit.ILGenerator generator, CliCompiler cliCompiler)
            {
                generator.Emit(OpCodes.Not);
                switch (prim.Type)
                {
                    case ITPrimitiveTypeType.UInt8:
                        generator.Emit(OpCodes.Conv_U1);
                        break;
                    case ITPrimitiveTypeType.UInt16:
                        generator.Emit(OpCodes.Conv_U2);
                        break;
                }
            }
        }


        public Type GetCliType()
        {
            switch (this.Type)
            {
                case ITPrimitiveTypeType.Bool:
                    return typeof(bool);
                case ITPrimitiveTypeType.Char:
                    return typeof(char);
                case ITPrimitiveTypeType.Double:
                    return typeof(double);
                case ITPrimitiveTypeType.Float:
                    return typeof(float);
                case ITPrimitiveTypeType.Int16:
                    return typeof(short);
                case ITPrimitiveTypeType.Int32:
                    return typeof(int);
                case ITPrimitiveTypeType.Int64:
                case ITPrimitiveTypeType.Integer:
                    return typeof(long);
                case ITPrimitiveTypeType.Int8:
                    return typeof(sbyte);
                case ITPrimitiveTypeType.String:
                    return typeof(string);
                case ITPrimitiveTypeType.UInt16:
                    return typeof(ushort);
                case ITPrimitiveTypeType.UInt32:
                    return typeof(uint);
                case ITPrimitiveTypeType.UInt64:
                    return typeof(ulong);
                case ITPrimitiveTypeType.UInt8:
                    return typeof(byte);
                default:
                    throw new InvalidOperationException("Unexpected primitive type: " + this.Type.ToString());
            }
        }

        public override ITMemberFunction GetMemberFunction(string ent, bool searchSuper)
        {

            if (ent == "Abs")
            {
                switch (Type)
                {
                    case ITPrimitiveTypeType.Int8:
                    case ITPrimitiveTypeType.Int16:
                    case ITPrimitiveTypeType.Int32:
                    case ITPrimitiveTypeType.Int64:
                    case ITPrimitiveTypeType.Integer:
                    case ITPrimitiveTypeType.Float:
                    case ITPrimitiveTypeType.Double:
                        if (absFunc == null)
                            absFunc = new AbsMemberFunction(this);
                        return absFunc;
                }
            }
            if (ent == "Sub")
            {
                if (Type == ITPrimitiveTypeType.String)
                {
                    if (subFunc == null)
                        subFunc = new SubMemberFunction(this);
                    return subFunc;
                }
            }
            if (ent == "Len")
            {
                if (Type == ITPrimitiveTypeType.String)
                {
                    if (lenFunc == null)
                        lenFunc = new LenMemberFunction(this);
                    return lenFunc;
                }
            }
            if (ent == "ToStrF")
            {
                switch (Type)
                {
                    case ITPrimitiveTypeType.Int8:
                    case ITPrimitiveTypeType.Int16:
                    case ITPrimitiveTypeType.Int32:
                    case ITPrimitiveTypeType.Int64:
                    case ITPrimitiveTypeType.UInt8:
                    case ITPrimitiveTypeType.UInt16:
                    case ITPrimitiveTypeType.UInt32:
                    case ITPrimitiveTypeType.UInt64:
                    case ITPrimitiveTypeType.Integer:
                    case ITPrimitiveTypeType.Float:
                    case ITPrimitiveTypeType.Double:
                        if (toStrFFunc == null)
                            toStrFFunc = new ToStrFMemberFunction(this);
                        return toStrFFunc;
                }
            }
            if (ent == "And")
            {
                if(IsIntegral() && Type != ITPrimitiveTypeType.Integer){
                    if (andFunc == null)
                        andFunc = new AndMemberFunction(this);
                    return andFunc;
                }
            }
            if (ent == "Or")
            {
                if (IsIntegral() && Type != ITPrimitiveTypeType.Integer)
                {
                    if (orFunc == null)
                        orFunc = new OrMemberFunction(this);
                    return orFunc;
                }
            }
            if (ent == "Xor")
            {
                if (IsIntegral() && Type != ITPrimitiveTypeType.Integer)
                {
                    if (xorFunc == null)
                        xorFunc = new XorMemberFunction(this);
                    return xorFunc;
                }
            }
            if (ent == "Shl")
            {
                if (IsIntegral() && Type != ITPrimitiveTypeType.Integer)
                {
                    if (shlFunc == null)
                        shlFunc = new ShlMemberFunction(this);
                    return shlFunc;
                }
            }
            if (ent == "Shr")
            {
                if (IsIntegral() && Type != ITPrimitiveTypeType.Integer)
                {
                    if (shrFunc == null)
                        shrFunc = new ShrMemberFunction(this);
                    return shrFunc;
                }
            }
            if (ent == "Sar")
            {
                if (IsIntegral() && Type != ITPrimitiveTypeType.Integer)
                {
                    if (sarFunc == null)
                        sarFunc = new SarMemberFunction(this);
                    return sarFunc;
                }
            }
            return base.GetMemberFunction(ent, searchSuper);
        }
    }
}
