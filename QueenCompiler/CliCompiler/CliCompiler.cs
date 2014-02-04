using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using Queen.Language.IntermediateTree;

namespace Queen.Language.CliCompiler
{
    public partial class CliCompiler
    {
        private AssemblyBuilder assembly;
        private ModuleBuilder module;
        private CliCompilerOptions options;

        public CliCompilerOptions Options
        {
            get { return options; }
            set { options = value; }
        }

        public AssemblyBuilder Build(ITRoot root)
        {
            ModuleBuilder m;
            return Build(root, out m);
        }
        public AssemblyBuilder Build(ITRoot root, out ModuleBuilder moduleBuilder)
        {
            AssemblyName name = new AssemblyName(options.AssemblyName);
            assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(name, options.AssemblyBuilderAccess, options.AssemblyDirectory);

            module = assembly.DefineDynamicModule(options.AssemblyName, options.ModuleName);

            // register all types, and find all member/global variables/functions
            foreach (ITRootGlobalScope scp in root.Children.Values)
            {
                RegisterTypes(scp, module);
            }

            // register all member/global variables/functions
            DoDelayedRegistration();
            
            // compile all functions
            DoCompilation();

            // complete all types
            CompleteTypes();

            var ret = assembly;
            moduleBuilder = module;
            assembly = null;
            return ret;
        }

        public static string OperatorFunctionName(ITBinaryOperatorType type)
        {
            switch (type)
            {
                case ITBinaryOperatorType.Add:
                    return "op_Addition";
                case ITBinaryOperatorType.Subtract:
                    return "op_Subtraction";
                case ITBinaryOperatorType.Multiply:
                    return "op_Multiply";
                case ITBinaryOperatorType.Divide:
                    return "op_Divide";
                case ITBinaryOperatorType.Modulus:
                    return "op_Modulus";
                case ITBinaryOperatorType.And:
                    return "op_LogicalAnd";
                case ITBinaryOperatorType.Concat:
                    return "op_Concat";
                case ITBinaryOperatorType.Equality:
                    return "op_Equality";
                case ITBinaryOperatorType.Inequality:
                    return "op_Inequality";
                case ITBinaryOperatorType.Or:
                    return "op_LogicalOr";
                case ITBinaryOperatorType.GreaterThan:
                    return "op_GreaterThan";
                case ITBinaryOperatorType.GreaterThanOrEqual:
                    return "op_GreaterThanOrEqual";
                case ITBinaryOperatorType.LessThan:
                    return "op_LessThan";
                case ITBinaryOperatorType.LessThanOrEqual:
                    return "op_LessThanOrEqual";
                case ITBinaryOperatorType.Power:
                    return "op_Power";
                default:
                    throw new NotImplementedException();
            }
        }

    }
}
