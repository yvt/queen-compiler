using System;
using System.Collections.Generic;
using System.Text;

namespace Queen.Language.CliCompiler
{
    public class CliCompilerOptions: CompilerOptions
    {
        public string AssemblyName { get; set; }
        public string RootNamespace { get; set; }
        public System.Reflection.Emit.AssemblyBuilderAccess AssemblyBuilderAccess { get; set; }
        public string AssemblyDirectory { get; set; }
        public bool IsLibrary { get; set; }
        public string ModuleName { get; set; }

        public CliCompilerOptions()
        {
            AssemblyBuilderAccess = System.Reflection.Emit.AssemblyBuilderAccess.RunAndSave;
        }
    }
}
