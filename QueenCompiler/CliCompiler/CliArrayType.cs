using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Queen.Language.IntermediateTree;
using System.Reflection.Emit;

namespace Queen.Language.CliCompiler
{
    internal class CliArrayType: ITArrayType
    {
        private ITMemberFunction lenFunction = null;
        private ITMemberFunction lenMultiFunction = null;
        private ITMemberFunction subFunction = null;
        private ITMemberFunction shuffleFunction = null;
        private ITMemberFunction reverseFunction = null;
        private ITMemberFunction sortFunction = null;
        private ITMemberFunction sortRFunction = null;

        public CliArrayType(ITType elemType, int dimensions,
            CliIntermediateCompiler iCompiler): base(elemType.iCompiler)
        {
            this.ElementType = elemType;
            this.Dimensions = dimensions;
        }

        private class LenFunction : CliVirtualMemberFunction
        {
            public LenFunction(CliArrayType arr)
                :base(arr.iCompiler.GetBuiltinType("int32"), new ITFunctionParameter[] {}, "Len")
            {
            }
            public override void EmitIL(System.Reflection.Emit.ILGenerator generator, CliCompiler comp)
            {
                generator.Emit(OpCodes.Ldlen);
            }
        }

        private class LenMultiFunction : CliVirtualMemberFunction
        {
            CliArrayType arr;
            public LenMultiFunction(CliArrayType arr)
                : base(arr.iCompiler.GetBuiltinType("int32"), new ITFunctionParameter[] { 
                    new ITFunctionParameter(arr.iCompiler.GetBuiltinType("int"))  // prevents 'cannot convert from int64 to int32' error
                }, "LenMulti")
            {
                this.arr = arr;
            }
            public override void EmitIL(System.Reflection.Emit.ILGenerator generator, CliCompiler comp)
            {
                Type arrayType = ((CliTypeInfo)arr.UserData).cliType;
                var meth = arrayType.GetMethod("GetLength", new Type[]{typeof(int)});
                generator.Emit(OpCodes.Conv_I4); // int64 to int32
                generator.Emit(OpCodes.Callvirt, meth);
            }
        }


        private class SubFunction : CliVirtualMemberFunction
        {
            CliArrayType arr;
            public SubFunction(CliArrayType arr)
                : base(arr, new ITFunctionParameter[] { 
                    new ITFunctionParameter(arr.iCompiler.GetBuiltinType("int")),
                    new ITFunctionParameter(arr.iCompiler.GetBuiltinType("int"))
                }, "Sub")
            {
                this.arr = arr;
            }
            public override void EmitIL(System.Reflection.Emit.ILGenerator generator, CliCompiler comp)
            {
                Type elmType = comp.GetCliType(arr.ElementType);
                var meth = typeof(Queen.Kuin.CompilerServices.RuntimeHelper).GetMethod("ArraySub");
                meth = meth.MakeGenericMethod(elmType);
                generator.Emit(OpCodes.Call, meth);
            }
        }

        private class ShuffleFunction : CliVirtualMemberFunction
        {
            CliArrayType arr;
            public ShuffleFunction(CliArrayType arr)
                : base(null, new ITFunctionParameter[] { }, "Shuffle")
            {
                this.arr = arr;
            }
            public override void EmitIL(System.Reflection.Emit.ILGenerator generator, CliCompiler comp)
            {
                Type elmType = comp.GetCliType(arr.ElementType);
                var meth = typeof(Queen.Kuin.CompilerServices.RuntimeHelper).GetMethod("ArrayShuffle");
                meth = meth.MakeGenericMethod(elmType);
                generator.Emit(OpCodes.Call, meth);
            }
        }

        private class ReverseFunction : CliVirtualMemberFunction
        {
            CliArrayType arr;
            public ReverseFunction(CliArrayType arr)
                : base(null, new ITFunctionParameter[] { }, "Reverse")
            {
                this.arr = arr;
            }
            public override void EmitIL(System.Reflection.Emit.ILGenerator generator, CliCompiler comp)
            {
                Type elmType = comp.GetCliType(arr.ElementType);
                var meth = typeof(Array).GetMethod("Reverse", new Type[] {typeof(Array)});
                generator.Emit(OpCodes.Call, meth);
            }
        }
        private class SortFunction : CliVirtualMemberFunction
        {
            CliArrayType arr;
            public SortFunction(CliArrayType arr)
                : base(null, new ITFunctionParameter[] { }, "Sort")
            {
                this.arr = arr;
            }
            public override void EmitIL(System.Reflection.Emit.ILGenerator generator, CliCompiler comp)
            {
                Type elmType = comp.GetCliType(arr.ElementType);
                var meth = typeof(Array).GetMethod("Sort", new Type[] { typeof(Array) });
                generator.Emit(OpCodes.Call, meth);
            }
        }
        private class SortRFunction : CliVirtualMemberFunction
        {
            CliArrayType arr;
            public SortRFunction(CliArrayType arr)
                : base(null, new ITFunctionParameter[] { }, "SortR")
            {
                this.arr = arr;
            }
            public override void EmitIL(System.Reflection.Emit.ILGenerator generator, CliCompiler comp)
            {
                Type elmType = comp.GetCliType(arr.ElementType);
                var meth = typeof(Array).GetMethod("Sort", new Type[] {typeof(Array)});
                generator.Emit(OpCodes.Dup);
                generator.Emit(OpCodes.Call, meth);
                meth = typeof(Array).GetMethod("Reverse", new Type[] { typeof(Array) });
                generator.Emit(OpCodes.Call, meth);
            }
        }

        public override ITMemberFunction GetMemberFunction(string ent, bool searchSuper)
        {
            if (ent == "Len")
            {
                if (lenFunction == null)
                {
                    lenFunction = new LenFunction(this);
                }
                return lenFunction;
            }
            if (ent == "LenMulti")
            {
                if (lenMultiFunction == null)
                {
                    lenMultiFunction = new LenMultiFunction(this);
                }
                return lenMultiFunction;
            }
            if (ent == "Reverse")
            {
                if (reverseFunction == null)
                {
                    reverseFunction = new ReverseFunction(this);
                }
                return reverseFunction;
            }
            if (ent == "Shuffle")
            {
                if (shuffleFunction == null)
                {
                    shuffleFunction = new ShuffleFunction(this);
                }
                return shuffleFunction;
            }
            if (ent == "Sort")
            {
                if (sortFunction == null)
                {
                    sortFunction = new SortFunction(this);
                }
                return sortFunction;
            }
            if (ent == "SortR")
            {
                if (sortRFunction == null)
                {
                    sortRFunction = new SortRFunction(this);
                }
                return sortRFunction;
            }
            if (Dimensions == 1)
            {
                if (ent == "Sub")
                {
                    if (subFunction == null)
                    {
                        subFunction = new SubFunction(this);
                    }
                    return subFunction;
                }
            }
            return base.GetMemberFunction(ent, searchSuper);
        }
    }
}
