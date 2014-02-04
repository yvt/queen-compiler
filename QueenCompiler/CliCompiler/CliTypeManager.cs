using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Queen.Language.IntermediateTree;
using System.Reflection;

namespace Queen.Language.CliCompiler
{
    internal class CliTypeManager
    {
        private Dictionary<Type, ITType> ImportedClasses = new Dictionary<Type, ITType>();
        private Dictionary<MethodInfo, CliMemberFunction> ImportedMemberFuncs = new Dictionary<MethodInfo, CliMemberFunction>();
        private Dictionary<MethodInfo, CliGlobalFunctionEntity> ImportedGlobalFuncs = new Dictionary<MethodInfo, CliGlobalFunctionEntity>();
        private Dictionary<FieldInfo, ITGlobalVariableEntity> ImportedGlobalVars = new Dictionary<FieldInfo, ITGlobalVariableEntity>();

        public CliIntermediateCompiler IntermediateCompiler { get; private set; }

        public CliTypeManager(CliIntermediateCompiler compiler)
        {
            IntermediateCompiler = compiler;
            ImportedClasses.Add(typeof(char), compiler.CreatePrimitiveType(ITPrimitiveTypeType.Char));
            ImportedClasses.Add(typeof(bool), compiler.CreatePrimitiveType(ITPrimitiveTypeType.Bool));
            ImportedClasses.Add(typeof(sbyte), compiler.CreatePrimitiveType(ITPrimitiveTypeType.Int8));
            ImportedClasses.Add(typeof(short), compiler.CreatePrimitiveType(ITPrimitiveTypeType.Int16));
            ImportedClasses.Add(typeof(int), compiler.CreatePrimitiveType(ITPrimitiveTypeType.Int32));
            ImportedClasses.Add(typeof(long), compiler.CreatePrimitiveType(ITPrimitiveTypeType.Integer)); // TODO: what CLI type for Integer?
            ImportedClasses.Add(typeof(byte), compiler.CreatePrimitiveType(ITPrimitiveTypeType.UInt8));
            ImportedClasses.Add(typeof(ushort), compiler.CreatePrimitiveType(ITPrimitiveTypeType.UInt16));
            ImportedClasses.Add(typeof(uint), compiler.CreatePrimitiveType(ITPrimitiveTypeType.UInt32));
            ImportedClasses.Add(typeof(ulong), compiler.CreatePrimitiveType(ITPrimitiveTypeType.UInt64));
            ImportedClasses.Add(typeof(float), compiler.CreatePrimitiveType(ITPrimitiveTypeType.Float));
            ImportedClasses.Add(typeof(double), compiler.CreatePrimitiveType(ITPrimitiveTypeType.Double));
            ImportedClasses.Add(typeof(string), compiler.CreatePrimitiveType(ITPrimitiveTypeType.String));
        }

        public ITType GetImportedClassType(Type typ)
        {
            ITType cls;
            if (ImportedClasses.TryGetValue(typ, out cls))
            {
                return cls;
            }

            if (typ.IsGenericParameter)
            {
                var declTyp = typ.DeclaringType;
                if (declTyp != null)
                {
                    // this generic parameter belongs to a class.
                    cls = GetImportedClassType(declTyp);

                    var genParams = cls.GetGenericParameters();
                    foreach (var t in genParams)
                    {
                        // FIXME: matching by name is a bad idea..
                        if (t.Name == typ.Name)
                        {
                            return t;
                        }
                    }
                    throw new InvalidOperationException(string.Format(
                        "Couldn't find a generic parameter named '{0}' of '{1}'.", 
                        typ.Name, cls.ToString()));
                }
                cls = new CliGenericTypeParameter(IntermediateCompiler, typ);
            }
            else if(typ.IsGenericType && typ.GetGenericTypeDefinition() != typ)
            {
                ITType genType = GetImportedClassType(typ.GetGenericTypeDefinition());
                Type[] args = typ.GetGenericArguments();
                ITType[] iArgs = new ITType[args.Length];
                for (int i = 0; i < args.Length; i++)
                    iArgs[i] = GetImportedClassType(args[i]);
                return genType.MakeGenericType(iArgs);
            }
            else
            {
                CliClassType c = new CliClassType(this, typ);
                cls = c;
                ImportedClasses.Add(typ, c);
                c.SetupType();
            }

            return cls;
        }

        private ITFunctionBody CreateFunctionBody(MethodInfo method)
        {
            ITFunctionBody body = new ITFunctionBody();
            body.Name = method.Name;

            foreach (ParameterInfo param in method.GetParameters())
            {
                ITFunctionParameter prm = new ITFunctionParameter();
                prm.Name = param.Name;
                prm.Type = GetImportedClassType(param.ParameterType);
                prm.IsByRef = param.IsOut;
                body.Parameters.Add(prm);
            }

            if(method.ReturnType.Name != "Void")
                body.ReturnType = GetImportedClassType(method.ReturnType);

            Type[] gens = method.GetGenericArguments();
            ITGenericTypeParameter[] genParams = new ITGenericTypeParameter[gens.Length];
            for (int i = 0; i < genParams.Length; i++)
            {
                var g = new CliGenericTypeParameter(IntermediateCompiler, gens[i]);
                genParams[i] = g;
            }

            body.GenericParameters = genParams;

            return body;
        }

        public CliMemberFunction GetImportedMemberFunction(MethodInfo method)
        {
            CliMemberFunction func;
            if (ImportedMemberFuncs.TryGetValue(method, out func))
            {
                return func;
            }
            
            if ((method.Attributes & MethodAttributes.Static) != 0)
            {
                throw new InvalidOperationException("Static method cannot be a member function.");
            }

            func = new CliMemberFunction(this, method, CreateFunctionBody(method));
            return func;
        }

        public CliGlobalFunctionEntity GetImportedGlobalFunction(MethodInfo method)
        {
            CliGlobalFunctionEntity func;
            if (ImportedGlobalFuncs.TryGetValue(method, out func))
            {
                return func;
            }

            if ((method.Attributes & MethodAttributes.Static) == 0)
            {
                throw new InvalidOperationException("Non-static method cannot be a global function.");
            }

            func = new CliGlobalFunctionEntity(this, method, CreateFunctionBody(method));
            return func;
        }

        public ITGlobalVariableEntity GetImportedGlobalVariable(FieldInfo field)
        {
            ITGlobalVariableEntity var;
            if (ImportedGlobalVars.TryGetValue(field, out var))
            {
                return var;
            }

            if ((field.Attributes & FieldAttributes.Static) == 0)
            {
                throw new InvalidOperationException("Non-static method cannot be a global function.");
            }
            var = new ITGlobalVariableEntity();
            var.UserData = new CliGlobalVariableInfo()
            {
                 containedType = field.DeclaringType, field = field
            };
            var.Type = GetImportedClassType(field.FieldType);
            var.IsConst = (field.Attributes & FieldAttributes.Literal) != 0;
            var.InitialValue = new ITValueExpression()
            {
             ExpressionType = var.Type, Value = field.GetValue(null)
            };
            var.Name = field.Name;
            return var;
        }
    }
}
