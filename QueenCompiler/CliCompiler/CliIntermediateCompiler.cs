using System;
using System.Collections.Generic;
using System.Text;
using Queen.Language.IntermediateTree;
using Queen.Language;
using System.Reflection;

namespace Queen.Language.CliCompiler
{
    public class CliIntermediateCompiler: IntermediateCompiler
    {
        internal class RootGlobalScope : ITRootGlobalScope
        {
            private bool imported = false;
            private CliIntermediateCompiler compiler;

            private class ImportedType
            {
                public Type type;
                public CliClassEntity entity;
            }
            private class ImportedFunction
            {
                public MethodInfo method;
                public CliGlobalFunctionEntity entity;
            }
            private class ImportedVariable
            {
                public FieldInfo field;
                public ITGlobalVariableEntity entity;
            }
            private Dictionary<string, ImportedType> types = new Dictionary<string, ImportedType>();
            private Dictionary<string, ImportedFunction> funcs = new Dictionary<string, ImportedFunction>();
            private Dictionary<string, ImportedVariable> vars = new Dictionary<string, ImportedVariable>();

            public RootGlobalScope(CliIntermediateCompiler compiler)
            {
                this.compiler = compiler;
            }

            private void RegisterGlobals(Type type)
            {
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Static | BindingFlags.Public))
                {
                    ImportedFunction func = new ImportedFunction();
                    func.method = method;
                    funcs.Add(method.Name, func);
                }

                foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    ImportedVariable var = new ImportedVariable();
                    var.field = field;
                    vars.Add(field.Name, var);
                }
            }

            private void ImportNamespace(string ns)
            {
                List<Type> types;
                if (compiler.namespaceTypes.TryGetValue(ns, out types))
                {
                    foreach (Type type in types)
                    {
                        if (type.GetCustomAttributes(typeof(Queen.Kuin.GlobalAttribute), false).Length > 0)
                        {
                            if (!type.IsSealed)
                            {
                                throw new InvalidOperationException("Class " + type.FullName + " has Queen.Kuin.GlobalAttribute but is not sealed");
                            }
                            // global
                            RegisterGlobals(type);
                        }
                        else
                        {
                            // normal class

                            ImportedType imp = new ImportedType();
                            imp.entity = null;
                            imp.type = type;
                            this.types.Add(type.Name, imp);
                        }
                    }
                }
            }

            private void DoImport()
            {
                if (imported)
                    return;

                List<string> nss;
                string autoNs = "Queen." + this.Name;
                if (compiler.globalImports.TryGetValue(this.Name, out nss))
                {
                    foreach (string ns in nss)
                    {
                        if(ns != autoNs)
                            ImportNamespace(ns);
                    }
                }
                ImportNamespace(autoNs);
                if (this.Name == "Q")
                {
                    ImportNamespace("Queen.Kuin");
                    ImportNamespace("Queen");
                }

                imported = true;
            }

            public override ITEntity ImportExternalRootGlobalScopeEntity(string ent)
            {
                DoImport();

                ImportedType outType;
                if (types.TryGetValue(ent, out outType))
                {
                    if (outType.entity == null)
                    {
                        outType.entity = new CliClassEntity((CliClassType)compiler.typeManager.GetImportedClassType(outType.type));
                        outType.entity.ParentScope = this;
                        outType.entity.Type.ParentScope = this;
                    }
                    return outType.entity;
                }

                ImportedFunction outFunc;
                if (funcs.TryGetValue(ent, out outFunc))
                {
                    if (outFunc.entity == null)
                    {
                        outFunc.entity = compiler.typeManager.GetImportedGlobalFunction(outFunc.method);
                        outFunc.entity.ParentScope = this;
                        outFunc.entity.Body.ParentScope = this;
                    }
                    return outFunc.entity;
                }

                ImportedVariable outVar;
                if (vars.TryGetValue(ent, out outVar))
                {
                    if (outVar.entity == null)
                    {
                        outVar.entity = compiler.typeManager.GetImportedGlobalVariable(outVar.field);
                        outVar.entity.ParentScope = this;
                    }
                    return outVar.entity;
                }

                // TODO: import from external assembly
                return base.ImportExternalRootGlobalScopeEntity(ent);
            }
        }
        internal class Root : ITRoot
        {
            private CliTypeManager typeManager;
            private CliIntermediateCompiler compiler;

            public Root(CliTypeManager typeManager, CliIntermediateCompiler compiler)
            {
                this.typeManager = typeManager;
                this.compiler = compiler;
            }

            public CliTypeManager TypeManager
            {
                get { return typeManager; }
            }

            public override ITRootGlobalScope CreateRootGlobalScope(string ent)
            {
                return new RootGlobalScope(compiler) { Name = ent };
            }
        }

        private CliTypeManager typeManager;
        private CliClassType rootClass = null;

        private List<Assembly> references = new List<Assembly>();
        private Dictionary<string, List<string>> globalImports = new Dictionary<string, List<string>>();
        private List<Type> allImportedTypes = new List<Type>();
        private Dictionary<string, List<Type>> namespaceTypes = new Dictionary<string, List<Type>>();

        public void AddAssemblyReference(System.Reflection.Assembly asm)
        {
            references.Add(asm);

            var types = asm.GetTypes();
            foreach (Type type in types)
            {
                if (!type.IsPublic)
                {
                    continue;
                }
                allImportedTypes.Add(type);

                List<Type> nsList;
                string ns = type.Namespace;
                if (!namespaceTypes.TryGetValue(ns, out nsList))
                {
                    nsList = new List<Type>();
                    namespaceTypes.Add(ns, nsList);
                }
                nsList.Add(type);
            }
            
        }

        public System.Reflection.Assembly[] GetReferencingAssemblies()
        {
            return references.ToArray();
        }

        public void ImportNamespace(string ns, string rootGlobalScopeName)
        {
            List<string> lst;
            if (!globalImports.TryGetValue(rootGlobalScopeName, out lst))
            {
                lst = new List<string>();
                globalImports.Add(rootGlobalScopeName, lst);
            }

            lst.Add(ns);
        }

        public override ITRoot CreateITRoot()
        {
            return new Root(typeManager, this);
        }

        public override ITClassType GetRootClass()
        {
            if (rootClass == null)
                rootClass = (CliClassType)typeManager.GetImportedClassType(typeof(Queen.Kuin.CClass));
            return rootClass;
        }

        public override ITType GetNumericExceptionType()
        {
            return typeManager.GetImportedClassType(typeof(Queen.Kuin.CExcpt));
        }

        public override ITArrayType CreateArrayType(ITType elementType, int numDimensions)
        {
            return new CliArrayType(elementType, numDimensions, this);
        }

        public override ITPrimitiveType CreatePrimitiveType(ITPrimitiveTypeType type)
        {
            return new CliPrimitiveType(this, type);
        }

        public CliIntermediateCompiler()
        {
            typeManager = new CliTypeManager(this);

            // TODO: move 'AddAssemblyReference's to compiler options
            AddAssemblyReference(typeof(Queen.Kuin.CClass).Assembly);
            // Queen.% is automatically imported to %

            Options = new CliCompilerOptions();
        }

        public override ITRoot IntermediateCompile(CodeDom.CodeSourceFile[] sourceFiles)
        {
            RegisterBuiltinType("dict", typeManager.GetImportedClassType(typeof(Queen.Kuin.CDict<,>)));
            RegisterBuiltinType("stack", typeManager.GetImportedClassType(typeof(Queen.Kuin.CStack<>)));
            RegisterBuiltinType("list", typeManager.GetImportedClassType(typeof(Queen.Kuin.CList<>)));
            RegisterBuiltinType("queue", typeManager.GetImportedClassType(typeof(Queen.Kuin.CQueue<>)));
            RegisterBuiltinType("iter", typeManager.GetImportedClassType(typeof(Queen.Kuin.IIter<>)));

            return base.IntermediateCompile(sourceFiles);
        }

        internal CliCompilerOptions GetCompilerOptions()
        {
            return (CliCompilerOptions)Options;
        }
    }
}
