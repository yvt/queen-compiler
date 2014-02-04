using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Queen.Language.IntermediateTree;

namespace Queen.Language.CliCompiler
{
    internal class CliVariableInfo
    {
        //FieldInfo cliField;
    }

    internal class CliTypeInfo
    {
        public Type cliType;
        public ConstructorInfo constructor;
        public IDelayedRegistrar delayedTypeCompletor;

        public CliTypeInfo(IntermediateTree.ITGenericTypeParameter prm, System.Reflection.Emit.GenericTypeParameterBuilder param)
        {
            cliType = param;
        }

        public CliTypeInfo()
        {
        }
    }

    internal class CliFuncionTypeInfo: CliTypeInfo
    {
        public MethodInfo invokeMethod;
    }

    internal class CliGlobalFunctionInfo
    {
        public MethodInfo method;
        public Type containedType;
    }

    internal class CliGlobalVariableInfo
    {
        public FieldInfo field;
        public Type containedType;
    }

    internal class CliMemberFunctionInfo
    {
        public MethodInfo method;
        public ITType ownerITType;
    }

    internal class CliConstructorInfo
    {
        public ConstructorInfo constructor;
        public ITType ownerITType;
    }

    internal class CliMemberPropertyInfo
    {
        public PropertyInfo property;
        public MethodInfo getter;
        public MethodInfo setter;
        public ITType ownerITType;
    }

    internal class CliMemberVariableInfo
    {
        public FieldInfo field;
        public ITType ownerITType;
    }
}
