using System;
using System.Collections.Generic;
using System.Text;
using Queen.Language.IntermediateTree;
using Queen.Language.CodeDom;

namespace Queen.Language
{
    public partial class IntermediateCompiler
    {
        private class EntityCompiler
        {
            public IntermediateCompiler compiler;
            public ITScope rootScope;
            public IList<CodeStatement> childStatements = new List<CodeStatement>();

            // all existing classes are added to this list to do member hierarchy verification.
            // WARNING: don't add enum class.
            private IList<ITClassEntity> classes = new List<ITClassEntity>();

            // ???
            private IList<ITFunctionEntity> funcs = new List<ITFunctionEntity>();

            private IList<ITUnresolvedConstantExpression> unresolvedConstants = new List<ITUnresolvedConstantExpression>();

            private IList<ITFunctionEntity> globalFunctionsToCompile = new List<ITFunctionEntity>();
            private IList<ITMemberFunction> memberFunctionsToCompile = new List<ITMemberFunction>();

            // when false, function children of a root scope class are not treated as member functions
            public bool classFunctionAsMember = true;

            private class ClassInheritanceValidator
            {
                private IList<ITClassType> classes = new List<ITClassType>();
                private IntermediateCompiler compiler;
                private int nextIndex = 1;
                private HashSet<int> set = new HashSet<int>();
                private Stack<int> stack = new Stack<int>();

                private List<int> tmpList = new List<int>();

                private IDictionary<int, ITClassType> clsMap = new Dictionary<int, ITClassType>();

                public void Add(ITClassType cls)
                {
                    classes.Add(cls);
                }

                private void StrongConnect(ITClassType cls)
                {
                    int currentIndex = nextIndex;
                    cls.tarjan_index = currentIndex;
                    cls.tarjan_lowlink = currentIndex;
                    set.Add(currentIndex);
                    stack.Push(currentIndex);
                    clsMap.Add(currentIndex, cls);
                    nextIndex += 1;

                    ITClassType supercls = cls.Superclass as ITClassType;
                    if (supercls != null && supercls.tarjan_beingCheced)
                    {
                        if (supercls.tarjan_index == -1)
                        {
                            StrongConnect(supercls);
                            cls.tarjan_lowlink = Math.Min(cls.tarjan_lowlink, supercls.tarjan_lowlink);
                        }
                        else if (set.Contains(supercls.tarjan_index))
                        {
                            cls.tarjan_lowlink = Math.Min(cls.tarjan_lowlink, supercls.tarjan_index);
                        }
                    }

                    // root
                    if (cls.tarjan_index == cls.tarjan_lowlink)
                    {
                        int index;
                        do
                        {
                            index = stack.Pop();
                            set.Remove(index);
                            tmpList.Add(index);
                        } while (index != currentIndex);
                        if (tmpList.Count > 1)
                        {
                            // circular inheritance!
                            // check is this already reported
                            for (int i = 0; i < tmpList.Count; i++)
                            {
                                ITClassType typ = clsMap[tmpList[i]].Superclass as ITClassType;
                                if (typ == null || !typ.tarjan_beingCheced)
                                {
                                    return;
                                }
                            }
                            compiler.ReportError(string.Format(Properties.Resources.ICCircularInheritance, tmpList.Count), clsMap[tmpList[0]].Location);
                            for (int i = 0; i < tmpList.Count; i++)
                            {
                                var c = clsMap[tmpList[i]];
                                compiler.ReportError(string.Format(Properties.Resources.ICCircularInheritanceClass, i + 1, tmpList.Count,
                                    c.ToString(), c.Superclass.ToString()), cls.Location);
                            }

                            // cut the ring to avoid an infinite recursion
                            clsMap[tmpList[0]].SetSuperclass(compiler.GetRootClass());
                        }
                        tmpList.Clear();
                    }
                    

                }

                // makes sure that there's no circular inheritance using tarjan's algorithm (O(N))
                public void Validate(IntermediateCompiler compiler)
                {
                    this.compiler = compiler;

                    foreach (ITClassType cls in classes)
                    {
                        cls.tarjan_index = -1;
                        cls.tarjan_lowlink = -1;
                        cls.tarjan_beingCheced = true;
                    }

                    foreach (ITClassType cls in classes)
                    {
                        if (cls.tarjan_index == -1)
                            StrongConnect(cls);
                    }

                    foreach (ITClassType cls in classes)
                    {
                        cls.tarjan_beingCheced = false;
                    }
                }
            }
            private ClassInheritanceValidator inheritanceValidator = new ClassInheritanceValidator();

            public EntityCompiler(IntermediateCompiler compiler, ITScope rootScope)
            {
                this.compiler = compiler;
                this.rootScope = rootScope;
            }

            public void AddChildStatement(CodeStatement stat)
            {
                childStatements.Add(stat);
            }

            private ITFunctionBody CreateFunctionBody(CodeFunctionStatement stat, ITScope parentScope)
            {
                ITFunctionBody body = new ITFunctionBody();

                body.Name = stat.Name.Text;

                body.Root = parentScope.Root;
                body.ParentScope = parentScope;
                body.Location = stat.Location;

                body.Statement = stat;

                var gparams = new ITGenericTypeParameter[stat.GenericParameters.Count];
                int i = 0;
                foreach (CodeIdentifier param in stat.GenericParameters)
                {
                    ITGenericTypeParameter prm = new ITGenericTypeParameter(compiler)
                    {
                        Location = param.Location,
                        Name = param.Text,
                        Owner = body
                    };
                    gparams[i++] = prm;
                }
                body.GenericParameters = gparams;

                if (stat.ReturnType != null)
                {
                    body.ReturnType = compiler.ResolveType(stat.ReturnType, body);
                }

                foreach (CodeParameterDeclaration param in stat.Parameters)
                {
                    ITFunctionParameter prm = new ITFunctionParameter();
                    prm.Name = param.Identifier.Text;
                    prm.IsByRef = param.IsByRef;
                    param.ITObject = prm;
                    prm.Type = compiler.ResolveType(param.Type, body);
                    prm.Root = parentScope.Root;
                    prm.Location = param.Location;
                    body.Parameters.Add(prm);
                }

                
                return body;
            }

            private void AddGlobalTypes(CodeClassStatement stat, ITScope parent)
            {
                ITClassEntity cls = new ITClassEntity(compiler);
                cls.Name = stat.Name.Text;

                // superclass shouldn't be set here; not all classes are ready.
                cls.Location = stat.Location;
                cls.Statement = stat;
                cls.Root = parent.Root;
                cls.Type.ParentScope = parent;
                cls.ParentScope = parent;
                cls.Type.Location = stat.Location;
                cls.Type.Name = stat.Name.Text;

                ITClassFlags flags = 0;
                if (stat.IsInterface)
                {
                    flags |= ITClassFlags.Interface;
                }
                cls.Type.Flags = flags;
                // TODO: cls.IsPublic = !stat.IsPublic;

                inheritanceValidator.Add(cls.Type);

                var gparams = new string[stat.GenericParameters.Count];
                int i = 0;
                foreach (CodeIdentifier param in stat.GenericParameters)
                {
                    gparams[i++] = param.Text;
                }
                cls.Type.InheritGenericParameters(gparams);

                classes.Add(cls);
                stat.ITObject = cls;

                parent.AddChildEntity(cls);

                AddGlobalTypes(stat.Scope, cls.Type);
            }

            private class UnresolvedEnumValue : ITUnresolvedConstantExpression
            {
                IntermediateCompiler compiler;
                CodeExpression codeExpr;
                ITGlobalVariableEntity ent;
                ITGlobalVariableEntity lastEnt;
                ITClassType typ;
                ITType enumType;
                public UnresolvedEnumValue(IntermediateCompiler compiler, ITGlobalVariableEntity ent, ITClassType typ, CodeEnumItem stat,
                    ITGlobalVariableEntity prevItem)
                {
                    this.compiler = compiler;
                    codeExpr = stat.Value;
                    this.ent = ent;
                    this.typ = typ;
                    enumType = typ.UnderlyingEnumType;
                    lastEnt = prevItem;
                }

                protected override void CircularReferenceDetected()
                {
                    compiler.ReportError(string.Format(Properties.Resources.ICCircularReference,
                        ent.ToString()), ent.Location);
                    ent.InitialValue = compiler.CreateErrorExpression();
                    ent.InitialValue.ExpressionType = typ;
                }

                protected override void NonConstantDetected()
                {
                    compiler.ReportError(string.Format(Properties.Resources.ICNonConstantValue,
                        ent.ToString()), ent.Location);
                    ent.InitialValue = compiler.CreateErrorExpression();
                    ent.InitialValue.ExpressionType = typ;
                }

                protected override void SetValue(ITExpression result)
                {
                    ent.InitialValue = result;
                }

                protected override ITExpression Evaluate()
                {
                    if (codeExpr == null)
                    {
                        // default value; zero or previous item + 1
                        if (lastEnt == null)
                        {
                            return new ITValueExpression()
                            {
                                ExpressionType = typ,
                                Location = typ.Location,
                                Value = 0
                            };
                        }
                        else
                        {
                            ITUnresolvedConstantExpression unres = lastEnt.InitialValue as ITUnresolvedConstantExpression;
                            if (unres != null)
                            {
                                unres.Resolve();
                            }
                            ITExpression val = lastEnt.InitialValue;
                            ITValueExpression sval = val as ITValueExpression;
                            if (sval == null)
                            {
                                // resolve error
                                return val;
                            }
                            long vl = Convert.ToInt64(sval.Value);
                            return new ITValueExpression()
                            {
                                ExpressionType = typ,
                                Location = typ.Location,
                                Value = vl + 1
                            };
                        }
                    }
                    CompileFunctionContext ctx = new CompileFunctionContext(compiler, null, typ);
                    ITExpression expr = compiler.CompileExpression(codeExpr, ctx);
                    expr = compiler.CastImplicitly(expr, enumType, typ.Location);

                    ITValueExpression retVal = expr as ITValueExpression;
                    if (retVal != null)
                    {
                        retVal.ExpressionType = typ;
                    }
                    return expr;
                }
            }
            private void AddGlobalTypes(CodeEnumStatement stat, ITScope parent)
            {
                ITClassEntity cls = new ITClassEntity(compiler);
                cls.Name = stat.Name.Text;

                // superclass shouldn't be set here; not all classes are ready.
                cls.Location = stat.Location;
                cls.Root = parent.Root;
                cls.Type.ParentScope = parent;
                cls.ParentScope = parent;
                cls.Type.UnderlyingEnumType = (ITPrimitiveType)compiler.builtinTypes["int"];
                cls.Type.Location = stat.Location;
                cls.Type.Flags |= ITClassFlags.Sealed;
                cls.Type.Name = cls.Name;

                stat.ITObject = cls;

                parent.AddChildEntity(cls);

                cls.Type.InheritGenericParameters(new string[] { });

                ITGlobalVariableEntity lastItem = null;
                foreach (CodeEnumItem item in stat.Items)
                {
                    ITGlobalVariableEntity nitem = new ITGlobalVariableEntity();
                    nitem.Location = item.Location;
                    nitem.Name = item.Name.Text;
                    nitem.Type = cls.Type;
                    nitem.UserData = item;
                    nitem.ParentScope = cls.Type;
                    nitem.IsConst = true;
                    nitem.InitialValue = new UnresolvedEnumValue(compiler, nitem, cls.Type, item, lastItem);
                    unresolvedConstants.Add((ITUnresolvedConstantExpression)nitem.InitialValue);
                    cls.Type.AddChildEntity(nitem);
                    lastItem = nitem;
                }
            }
            private void AddGlobalTypes(CodeGlobalScope scope, ITGlobalScope parent)
            {
                ITClassType clsType = parent as ITClassType;
                foreach (CodeStatement stat in scope.Children.Values)
                {
                    if (clsType != null && clsType.IsInterface() &&
                        (stat is CodeClassStatement ||
                        stat is CodeEnumStatement))
                    {
                        compiler.ReportError(Properties.Resources.ICNestedClassOfInterface, parent.Location);
                        return;
                    }

                    if (stat is CodeClassStatement)
                        AddGlobalTypes((CodeClassStatement)stat, parent);
                    // don't touch variables now
                    if (stat is CodeEnumStatement)
                        AddGlobalTypes((CodeEnumStatement)stat, parent);
                }
            }
            public void SetupGlobalTypes()
            {
                foreach (ITClassEntity cls in classes)
                {
                    var clsType = (ITClassType)cls.Type;
                    CodeClassStatement stat = cls.Statement;
                    ITType supercls = null;
                    var infs = new HashSet<ITType>();
                    foreach (var typ in stat.BaseClasses)
                    {
                        ITType tp = compiler.ResolveType(typ, cls.Scope);
                        if (tp == null)
                            continue;

                        // TODO: accessibility check for base type specification


                        if (tp.IsSealed())
                        {
                            compiler.ReportError(Properties.Resources.ICInheritFromSealed, cls.Location);
                            continue;
                        }

                        if (tp.IsInterface())
                        {
                            infs.Add(tp);
                        }
                        else
                        {
                            if (supercls == null)
                            {
                                supercls = tp;
                            }
                            else
                            {
                                compiler.ReportError(Properties.Resources.ICMultipleInheritance, cls.Location);
                            }
                        }
                    }

                    if (supercls == clsType)
                    {
                        compiler.ReportError(Properties.Resources.ICSelfInheritance, cls.Location);
                        supercls = null;
                    }

                    if (clsType.IsInterface())
                    {
                        if (supercls != null)
                        {
                            compiler.ReportError(Properties.Resources.ICInterfaceInheritingClass, cls.Location);
                        }
                        if (infs.Count > 0)
                        {
                            compiler.ReportError(Properties.Resources.ICInterfaceInheritance, cls.Location);
                            infs.Clear();
                        }
                    }
                    else
                    {

                        if (supercls != null)
                        {
                            clsType.SetSuperclass(supercls);
                        }
                        else
                        {
                            clsType.SetSuperclass(compiler.GetRootClass());
                        }

                    }

                    var infsArray = new ITType[infs.Count];
                    infs.CopyTo(infsArray);
                    clsType.SetInterfaces(infsArray);
                }

                ValidateInheritance();
            }

            private void ReportMissingImplementation(ITType interfaceType, ITMemberFunction func, ITClassType typ)
            {
                compiler.ReportError(string.Format(Properties.Resources.ICMissingImplementation,
                    typ.ToString(), func.ToString(), interfaceType.ToString()), typ.Location);
            }

            private void ReportMissingImplementation(ITType interfaceType, ITMemberProperty func, ITClassType typ)
            {
                compiler.ReportError(string.Format(Properties.Resources.ICMissingImplementation,
                    typ.ToString(), func.ToString(), interfaceType.ToString()), typ.Location);
            }

            private bool TypeEqualsWithSubstitutions(ITType type1, ITType type2, Dictionary<ITType, ITType> subst)
            {
                if (type1 == null)
                {
                    return type2 == null;
                }
                else if (type2 == null)
                {
                    return false;
                }

                // direct use of generic parameter?
                if (subst != null)
                {
                    ITType cvtd;
                    if (subst.TryGetValue(type1, out cvtd))
                    {
                        if (cvtd.Equals(type2))
                            return true;
                    }
                }

                {
                    ITInstantiatedGenericType cls1 = type1 as ITInstantiatedGenericType;
                    ITInstantiatedGenericType cls2 = type2 as ITInstantiatedGenericType;
                    if (cls1 != null)
                    {
                        if (cls2 == null)
                            return false;

                        if (!cls1.GenericTypeDefinition.Equals(cls2.GenericTypeDefinition))
                        {
                            return false;
                        }

                        var inst1 = cls1.GetGenericParameters();
                        var inst2 = cls2.GetGenericParameters();

                        for (int i = 0; i < inst1.Length; i++)
                        {
                            if (!TypeEqualsWithSubstitutions(inst1[i], inst2[i], subst))
                                return false;
                        }

                        return true;
                    }
                }
                {
                    ITFunctionType func1 = type1 as ITFunctionType;
                    ITFunctionType func2 = type2 as ITFunctionType;
                    if (func1 != null)
                    {
                        if (func2 == null) return false;
                        var param1 = func1.Parameters;
                        var param2 = func2.Parameters;
                        if (param1.Length != param2.Length)
                            return false;
                        if (!TypeEqualsWithSubstitutions(func1.ReturnType, func2.ReturnType, subst))
                            return false;
                        for (int i = 0; i < param1.Length; i++)
                        {
                            if (!TypeEqualsWithSubstitutions(param1[i].Type, param2[i].Type, subst))
                                return false;
                            if (param1[i].IsByRef != param2[i].IsByRef)
                                return false;
                        }
                        return true;
                    }
                }
                {
                    ITArrayType arr1 = type1 as ITArrayType;
                    ITArrayType arr2 = type2 as ITArrayType;
                    if (arr1 != null)
                    {
                        if (arr2 == null) return false;
                        if (arr1.Dimensions != arr2.Dimensions)
                            return false;
                        return TypeEqualsWithSubstitutions(arr1.ElementType, arr2.ElementType, subst);
                    }
                }

                return type1.Equals(type2);
            }

            private bool FunctionParametersEquals(ITMemberFunction func1, ITMemberFunction func2)
            {
                if (func1.Body.Parameters.Count != func2.Body.Parameters.Count)
                {
                    return false;
                }

                var genParams1 = func1.Body.GenericParameters;
                var genParams2 = func2.Body.GenericParameters;
                if (genParams1.Length != genParams2.Length)
                {
                    return false;
                }
                Dictionary<ITType, ITType> subst = null;
                if (genParams1.Length > 0)
                {
                    subst = new Dictionary<ITType, ITType>();
                    for (int i = 0; i < genParams1.Length; i++)
                    {
                        subst[genParams1[i]] = genParams2[i];
                    }
                }

                var paramTypes1 = func1.GetParameterTypes();
                var paramTypes2 = func2.GetParameterTypes();
                var params1 = func1.Body.Parameters;
                var params2 = func2.Body.Parameters;

                for (int i = 0; i < paramTypes1.Length; i++)
                {
                    var paramType1 = paramTypes1[i];
                    var paramType2 = paramTypes2[i];
                    var param1 = params1[i];
                    var param2 = params2[i];

                    if (param1.IsByRef != param2.IsByRef)
                    {
                        return false;
                    }

                    if (!TypeEqualsWithSubstitutions(paramType1, paramType2, subst))
                        return false;
                }

                if (!TypeEqualsWithSubstitutions(func1.GetReturnType(), func2.GetReturnType(), subst))
                    return false;

                return true;
            }

            private bool FunctionParametersEquals(ITMemberProperty func1, ITMemberProperty func2)
            {
                if (func1.Parameters.Count != func2.Parameters.Count)
                {
                    return false;
                }

                if (func1.Setter != null && func2.Setter == null)
                {
                    return false;
                }
                
                // property doesn't have generic parameters.

                var params1 = func1.Parameters;
                var params2 = func2.Parameters;

                for (int i = 0; i < params1.Count; i++)
                {
                    var param1 = params1[i];
                    var param2 = params2[i];

                    if (param1.IsByRef != param2.IsByRef)
                    {
                        return false;
                    }

                    if (!TypeEqualsWithSubstitutions(param1.Type, param2.Type, null))
                        return false;
                }

                if (!TypeEqualsWithSubstitutions(func1.Type, func2.Type, null))
                    return false;

                return true;
            }

            private bool ReducingAccessibility(ITMember to, ITMember from)
            {
                if (from.IsPrivate) return false; // narrowest
                if (from.IsPublic)
                {
                    return to.IsPrivate || !to.IsPublic;
                }
                else
                {
                    return to.IsPrivate;
                }
            }

            // makes sure class implements all functions required by the interfaces the class says it implements.
            private void CheckClassInterfaceConformance(ITClassType type)
            {
                foreach (var intf in type.Interfaces)
                {
                    foreach (var func in intf.GetMemberFunctions())
                    {
                        var fn = type.GetMemberFunction(func.Name, true);
                        if (fn == null)
                        {
                            ReportMissingImplementation(intf, func, type);
                            continue;
                        }

                        if (!FunctionParametersEquals(func, fn))
                        {
                            ReportMissingImplementation(intf, func, type);
                            continue;
                        }

                        if (!fn.IsPublic)
                        {
                            compiler.ReportError(Properties.Resources.ICNonPublicImplement,
                                func.Location);
                        }
                    }

                    foreach (var prop in intf.GetMemberProperties())
                    {
                        var fn = type.GetMemberProperty(prop.Name, true);
                        if (fn == null)
                        {
                            ReportMissingImplementation(intf, prop, type);
                            continue;
                        }

                        if (!FunctionParametersEquals(prop, fn))
                        {
                            ReportMissingImplementation(intf, prop, type);
                            continue;
                        }

                        if (!fn.IsPublic)
                        {
                            compiler.ReportError(Properties.Resources.ICNonPublicImplement,
                                prop.Location);
                        }
                    }
                }
            }

            // make sure class correctly inherits from the base class.
            private void CheckClassOverride(ITClassType type)
            {
                var baseType = type.Superclass;

                foreach (var func in type.GetMemberFunctions())
                {
                    if (func.IsConstructor() || func.IsDestructor())
                    {
                        // ctor/dtor are automatically marked as overriding
                        func.MarkedAsOverriding = true;

                        if (func.IsConstructor() && !func.IsPublic)
                        {
                            type.Flags |= ITClassFlags.Abstract;
                        }

                        // can skip most of verification
                        func.OverriddenMember = baseType != null ? baseType.GetMemberFunction(func.Name) : null;
                        continue;
                    }


                    var fn = baseType != null ? baseType.GetMemberFunction(func.Name) : null;
                    if (fn == null)
                    {
                        if (func.MarkedAsOverriding)
                        {
                            compiler.ReportError(string.Format(Properties.Resources.ICNoMemberFunctionToOverride,
                                func.ToString(), type.ToString()), func.Location);
                        }

                        // check other kind of members
                        ITMember var = baseType != null ? baseType.GetMemberVariable(func.Name) : null;
                        if (var == null)
                            var = baseType != null ? baseType.GetMemberProperty(func.Name) : null;
                        if (var != null)
                        {
                            compiler.ReportError(string.Format(Properties.Resources.ICSuperclassMemberNameConflict,
                                func.ToString(), type.ToString(), var.Owner.ToString(), var.ToString()), func.Location);
                        }
                    }
                    else
                    {
                        // check signature
                        bool match = FunctionParametersEquals(func, fn);
                        if (!func.MarkedAsOverriding)
                        {
                            if (match)
                            {
                                compiler.ReportError(string.Format(Properties.Resources.ICSuperclassMemberNameConflictFunction,
                                    func.ToString(), type.ToString(), fn.Owner.ToString(), fn.ToString()), func.Location);
                            }
                            else
                            {
                                compiler.ReportError(string.Format(Properties.Resources.ICSuperclassMemberNameConflict,
                                    func.ToString(), type.ToString(), fn.Owner.ToString(), fn.ToString()), func.Location);
                            }
                        }
                        else if (!match)
                        {
                            compiler.ReportError(string.Format(Properties.Resources.ICBadOverrideFunction,
                                    func.ToString(), type.ToString(), fn.ToString(), fn.Owner.ToString()), func.Location);
                        }
                        else if (ReducingAccessibility(func, fn))
                        {
                            compiler.ReportError(string.Format(Properties.Resources.ICOverrideInvalidAccess,
                                    func.ToString(), type.ToString(), fn.ToString(), fn.Owner.ToString()), func.Location);
                        }
                        else
                        {
                            func.OverriddenMember = fn;

                            
                        }
                    }
                }

                foreach (var func in type.GetMemberProperties())
                {
                    var fn = baseType != null ? baseType.GetMemberProperty(func.Name) : null;
                    if (fn == null)
                    {
                        if (func.MarkedAsOverriding)
                        {
                            compiler.ReportError(string.Format(Properties.Resources.ICNoMemberPropertyToOverride,
                                func.ToString(), type.ToString()), func.Location);
                        }

                        // check other kind of members
                        ITMember var = baseType != null ? baseType.GetMemberVariable(func.Name) : null;
                        if (var == null)
                            var = baseType != null ? baseType.GetMemberFunction(func.Name) : null;
                        if (var != null)
                        {
                            compiler.ReportError(string.Format(Properties.Resources.ICSuperclassMemberNameConflict,
                                func.ToString(), type.ToString(), var.Owner.ToString(), var.ToString()), func.Location);
                        }
                    }
                    else
                    {
                        // check signature
                        bool match = FunctionParametersEquals(func, fn);
                        if (!func.MarkedAsOverriding)
                        {
                            if (match)
                            {
                                compiler.ReportError(string.Format(Properties.Resources.ICSuperclassMemberNameConflictProperty,
                                    func.ToString(), type.ToString(), fn.Owner.ToString(), fn.ToString()), func.Location);
                            }
                            else
                            {
                                compiler.ReportError(string.Format(Properties.Resources.ICSuperclassMemberNameConflict,
                                    func.ToString(), type.ToString(), fn.Owner.ToString(), fn.ToString()), func.Location);
                            }
                        }
                        else if (!match)
                        {
                            compiler.ReportError(string.Format(Properties.Resources.ICBadOverrideProperty,
                                    func.ToString(), type.ToString(), fn.ToString(), fn.Owner.ToString()), func.Location);
                        }
                        else if (ReducingAccessibility(func, fn))
                        {
                            compiler.ReportError(string.Format(Properties.Resources.ICOverrideInvalidAccess,
                                    func.ToString(), type.ToString(), fn.ToString(), fn.Owner.ToString()), func.Location);
                        }
                        else
                        {
                            func.OverriddenMember = fn;
                        }
                    }
                }

                if (baseType != null)
                {
                    foreach (var variable in type.GetMemberVariables())
                    {
                        ITMember var = baseType.GetMemberVariable(variable.Name);
                        if (var == null)
                            var = baseType.GetMemberFunction(variable.Name);
                        if (var == null)
                            var = baseType.GetMemberProperty(variable.Name);
                        if (var != null)
                        {
                            compiler.ReportError(string.Format(Properties.Resources.ICSuperclassMemberNameConflict,
                                variable.ToString(), type.ToString(), var.Owner.ToString(), var.ToString()), variable.Location);
                        }
                    }
                }
                
            }

            private void AddGlobalDefinitions(CodeClassStatement stat, ITScope parent)
            {
                AddGlobalDefinitions(stat.Scope, (ITGlobalScope)((ITClassEntity)stat.ITObject).Type);
            }

            private class UnresolvedConstantVariable : ITUnresolvedConstantExpression
            {
                IntermediateCompiler compiler;
                CodeExpression codeExpr;
                ITGlobalVariableEntity ent;
                ITScope scope;
                public UnresolvedConstantVariable(IntermediateCompiler compiler, ITGlobalVariableEntity ent, CodeVariableDeclarationStatement stat,
                    ITScope scope)
                {
                    this.compiler = compiler;
                    codeExpr = stat.InitialValue;
                    this.ent = ent;
                    this.scope = scope;
                }

                protected override void CircularReferenceDetected()
                {
                    compiler.ReportError(string.Format(Properties.Resources.ICCircularReference,
                        ent.ToString()), ent.Location);
                    ent.InitialValue = compiler.CreateErrorExpression();
                    ent.InitialValue.ExpressionType = ent.Type;
                }

                protected override void NonConstantDetected()
                {
                    compiler.ReportError(string.Format(Properties.Resources.ICNonConstantValue,
                        ent.ToString()), ent.Location);
                    ent.InitialValue = compiler.CreateErrorExpression();
                    ent.InitialValue.ExpressionType = ent.Type;
                }

                protected override void SetValue(ITExpression result)
                {
                    ent.InitialValue = result;
                }
                protected override ITExpression Evaluate()
                {
                    CompileFunctionContext ctx = new CompileFunctionContext(compiler, null, scope);
                    return compiler.CompileExpression(codeExpr, ctx);
                }
            }
            private void AddGlobalDefinitions(CodeVariableDeclarationStatement stat, ITScope parent)
            {
                if (stat.Type == null)
                {
                    compiler.ReportError(Properties.Resources.ICImplicitlyTypedNonLocalVariable, stat.Location);
                }
                // for class type, every member variables are non-static
                ITClassType classParent = parent as ITClassType;
                if (classParent != null && !stat.IsConst)
                {
                    bool isInterface = classParent.IsInterface();

                    if (isInterface)
                    {
                        compiler.ReportError(Properties.Resources.ICMemberVariableOfInterface, stat.Location);
                        return;
                    }

                    ITMemberVariable mem = new ITMemberVariable();
                    mem.Name = stat.Identifier.Text;
                    mem.Type = compiler.ResolveType(stat.Type, parent) ?? compiler.GetBuiltinType("int");
                    mem.IsConst = stat.IsConst;
                    mem.IsPrivate = false;
                    mem.IsPublic = !stat.IsPrivate;
                    mem.Root = parent.Root;
                    mem.Location = stat.Location;
                    mem.Owner = classParent;
                   
                    if (stat.InitialValue != null)
                    {
                        CompileFunctionContext ctx = new CompileFunctionContext(compiler, classParent, parent);
                        mem.InitialValue = compiler.CompileExpression(stat.InitialValue, ctx);
                    }
                    
                    stat.ITObject = mem;

                    
                    ((ITClassType)parent).AddMemberVariable(mem);

                    // static member functions / variables will be added if language supports ones...
                }
                else
                {
                    ITGlobalVariableEntity ent = new ITGlobalVariableEntity();
                    ent.Name = stat.Identifier.Text;
                    ent.Type = compiler.ResolveType(stat.Type, parent) ?? compiler.GetBuiltinType("int");
                    ent.IsConst = stat.IsConst;
                    ent.Root = parent.Root;
                    ent.ParentScope = parent;
                    ent.IsPublic = !stat.IsSourceCodeLocal;
                    ent.Location = stat.Location;
                    stat.ITObject = ent;

                    if (ent.IsConst)
                    {
                        ent.InitialValue = new UnresolvedConstantVariable(compiler, ent, stat, parent);
                        unresolvedConstants.Add((UnresolvedConstantVariable)ent.InitialValue);
                    }
                    else
                    {
                        if (stat.InitialValue != null)
                        {
                            CompileFunctionContext ctx = new CompileFunctionContext(compiler, null, parent);
                            ent.InitialValue = compiler.CompileExpression(stat.InitialValue, ctx);
                            ent.InitialValue = compiler.CastImplicitly(ent.InitialValue, ent.Type, stat.Location);
                        }
                    }

                    parent.AddChildEntity(ent);
                }
            }
            private void AddGlobalDefinitions(CodeFunctionStatement stat, ITScope parent)
            {
                // for class type, every member functions are non-static
                ITClassType classParent = parent as ITClassType;
                if (classParent != null && (classFunctionAsMember || parent != rootScope))
                {
                    bool isInterface = classParent.IsInterface();
                    ITMemberFunction ent = new ITMemberFunction();
                    ent.Body = CreateFunctionBody(stat, parent);
                    ent.Name = ent.Body.Name;
                    ent.IsPublic = !stat.IsPrivate;
                    ent.IsPrivate = false;
                    ent.Owner = (ITClassType)parent;
                    ent.IsAbstract = isInterface;
                    ent.MarkedAsOverriding = stat.IsOverride;
                    
                    if (isInterface && !ent.IsPublic)
                    {
                        compiler.ReportError(Properties.Resources.ICPrivateInterfaceMember, stat.Location);
                    }

                    // check constructor
                    if (ent.Name == "Ctor")
                    {
                        if (ent.Body.Parameters.Count > 0)
                        {
                            compiler.ReportError(Properties.Resources.ICConstructorHavingParameter, stat.Location);
                        }
                        if (ent.Body.GenericParameters.Length > 0)
                        {
                            compiler.ReportError(Properties.Resources.ICConstructorHavingGenericTypeParameter, stat.Location);
                        }
                        if (ent.Body.ReturnType != null)
                        {
                            compiler.ReportError(Properties.Resources.ICConstructorHavingReturnType, stat.Location);
                        }
                    }
                    else if (ent.Name == "Dtor")
                    {
                        if (ent.Body.Parameters.Count > 0)
                        {
                            compiler.ReportError(Properties.Resources.ICDestructorHavingParameter, stat.Location);
                        }
                        if (ent.Body.GenericParameters.Length > 0)
                        {
                            compiler.ReportError(Properties.Resources.ICDestructorHavingParameter, stat.Location);
                        }
                        if (ent.Body.ReturnType != null)
                        {
                            compiler.ReportError(Properties.Resources.ICDestructorHavingReturnType, stat.Location);
                        }
                    }

                    ((ITClassType)parent).AddMemberFunction(ent);
                    if (!ent.IsAbstract)
                    {
                        memberFunctionsToCompile.Add(ent);
                    }
                }
                else
                {
                    ITFunctionEntity ent = new ITFunctionEntity();
                    ent.Body = CreateFunctionBody(stat, parent);
                    ent.Name = ent.Body.Name;
                    ent.IsPublic = !stat.IsPrivate;
                    ent.ParentScope = parent;

                    parent.AddChildEntity(ent);
                    globalFunctionsToCompile.Add(ent);
                }
            }
            private void AddGlobalDefinitions(CodeGlobalScope scope, ITScope parent)
            {
                foreach (CodeStatement stat in scope.Children.Values)
                {
                    if (stat is CodeClassStatement)
                        AddGlobalDefinitions((CodeClassStatement)stat, parent);
                    else if (stat is CodeVariableDeclarationStatement)
                        AddGlobalDefinitions((CodeVariableDeclarationStatement)stat, parent);
                    else if (stat is CodeFunctionStatement)
                        AddGlobalDefinitions((CodeFunctionStatement)stat, parent);
                }
            }

            public void VerifyMemberHierarchies()
            {
                foreach (var cls in classes)
                {
                    CheckClassInterfaceConformance((ITClassType)cls.Type);
                    CheckClassOverride((ITClassType)cls.Type);
                }
            }

            public void ResolveConstants()
            {
                foreach (ITUnresolvedConstantExpression expr in unresolvedConstants)
                {
                    expr.Resolve();
                }
                unresolvedConstants.Clear();
            }

            private void ValidateInheritance()
            {
                inheritanceValidator.Validate(compiler);
            }



            public void AddGlobalTypes()
            {
                foreach (CodeStatement stat in childStatements)
                {
                    if (stat is CodeClassStatement)
                        AddGlobalTypes((CodeClassStatement)stat, rootScope);
                    // don't touch variables now
                    if (stat is CodeEnumStatement)
                        AddGlobalTypes((CodeEnumStatement)stat, rootScope);
                }

            }

            public void AddGlobalDefinitions()
            {
                foreach (CodeStatement stat in childStatements)
                {
                    if (stat is CodeClassStatement)
                        AddGlobalDefinitions((CodeClassStatement)stat, rootScope);
                    else if (stat is CodeVariableDeclarationStatement)
                        AddGlobalDefinitions((CodeVariableDeclarationStatement)stat, rootScope);
                    else if (stat is CodeFunctionStatement)
                        AddGlobalDefinitions((CodeFunctionStatement)stat, rootScope);
                }
            }


            public void CompileFunctions()
            {
                foreach (ITFunctionEntity func in globalFunctionsToCompile)
                {
                    compiler.CompileFunction(func);
                }
                foreach (ITMemberFunction func in memberFunctionsToCompile)
                {
                    compiler.CompileFunction(func, func.Owner);
                }
            }

            public void CompileBySingleCall()
            {
                AddGlobalTypes();
                SetupGlobalTypes();
                AddGlobalDefinitions();
                VerifyMemberHierarchies();
                ResolveConstants();
                CompileFunctions();
            }


        }
    }
}
