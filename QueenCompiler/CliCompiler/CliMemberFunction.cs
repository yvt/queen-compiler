using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Queen.Language.IntermediateTree;
using System.Reflection;

namespace Queen.Language.CliCompiler
{
    internal class CliMemberFunction: ITMemberFunction
    {
        private CliTypeManager manager;
        private MethodInfo method;
        public CliMemberFunction(CliTypeManager manager, MethodInfo method, ITFunctionBody body)
        {
            this.manager = manager;
            this.method = method;
            this.Body = body;
            this.UserData = new CliMemberFunctionInfo()
            {
                 method = method, ownerITType = manager.GetImportedClassType(method.DeclaringType)
            };
            this.Name = method.Name;
            this.IsPublic = method.IsPublic;
            this.IsPrivate = method.IsPrivate;
        }
    }

    internal abstract class CliVirtualMemberFunction: ITMemberFunction
    {
        // emits instructions to emulate the virtual member invocation.
        // before the invocation, [object, param1, param2, ...] is on the stack.
        // for primitive types, object is not boxed.
        // the instructions must store the returned value, or nothing for "void" return type.
        public abstract void EmitIL(System.Reflection.Emit.ILGenerator generator, CliCompiler cliCompiler);

        // TODO: creating delegate for virtual memeber functions
        //public abstract MethodInfo GetWrapperMethod(CliCompiler);

        public CliVirtualMemberFunction(ITType returnType, ITFunctionParameter[] parameters, string name)
        {
            this.Body = new ITFunctionBody()
            {
             Parameters = parameters, Name = name, ReturnType = returnType,
             GenericParameters = new ITGenericTypeParameter[] {}
            };
            this.Name = name;
            this.IsPublic = true;
        }

    }
}
