using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Queen.Language.IntermediateTree;
using System.Reflection;

namespace Queen.Language.CliCompiler
{
    internal class CliGlobalFunctionEntity: ITFunctionEntity
    {
        private CliTypeManager manager;
        private MethodInfo method;
        public CliGlobalFunctionEntity(CliTypeManager manager, MethodInfo method, ITFunctionBody body)
        {
            this.manager = manager;
            this.method = method;
            this.Body = body;
            this.UserData = new CliGlobalFunctionInfo()
            {
             method = method, containedType = method.DeclaringType
            };
            IsPublic = true;
            this.Name = method.Name;

        }
    }
}
