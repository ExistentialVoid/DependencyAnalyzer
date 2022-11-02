using System;
using System.Reflection;
using System.Text;

namespace DependencyAnalyzer
{
    internal static class SignatureBuilder
    {
        private static string GetGenericInfo(TypeInfo? info)
        {
            if (info is null) return string.Empty;
            if (!info.IsGenericType) return string.Empty;

            string genInfo = "[";
            Type[] genArgs = info.GetGenericArguments();
            foreach (Type genArg in genArgs) genInfo += $"{genArg.Name}, ";

            return genInfo.TrimEnd().TrimEnd(',') + ']';
        }
        private static string GetMethodParamsInfo(MethodBase info)
        {
            if (info is null) return string.Empty;

            string paramInfo = "(";
            ParameterInfo[] parameters = info.GetParameters();
            foreach (ParameterInfo param in parameters)
            {
                Type t = param.ParameterType;
                if (t.IsGenericType)
                {
                    paramInfo += t.Name + '[';
                    Type[] genArgs = t.GetGenericArguments();
                    foreach (Type gen in genArgs) paramInfo += gen.Name + ", ";

                    paramInfo = paramInfo.TrimEnd().TrimEnd(',') + ']';
                }
                else paramInfo += t.FullName;

                paramInfo += ", ";
            }
            return paramInfo.TrimEnd().TrimEnd(',') + ')';
        }
        internal static string GetSignature(MemberInfo? member)
        {
            if (member is null) return string.Empty;

            if (member is ConstructorInfo ctor) return SignatureOfConstructor(ctor);
            else if (member is FieldInfo field) return SignatureOfField(field);
            else if (member is EventInfo evnt) return SignatureOfEvent(evnt);
            else if (member is MethodInfo method) return SignatureOfMethod(method);
            else if (member is PropertyInfo prop) return SignatureOfProperty(prop);
            else if (member is TypeInfo type) return SignatureOfType(type);

            return string.Empty;
        }
        private static string SignatureOfConstructor(ConstructorInfo info)
        {
            StringBuilder builder = new();
            if (info.IsPublic) builder.Append("public ");
            else if (info.IsPrivate) builder.Append("private ");
            else if (info.IsAssembly) builder.Append("internal ");
            else if (info.IsFamily) builder.Append("protected ");
            else if (info.IsFamilyAndAssembly) builder.Append("internal protected ");
            else if (info.IsFamilyOrAssembly) builder.Append("private protected ");

            if (info.IsStatic) builder.Append("static ");
            else if (info.IsAbstract) builder.Append("abstract ");
            else if (info.IsVirtual) builder.Append("virtual ");
            else if (info.IsFinal) builder.Append("sealed ");

            builder.Append($"void {info.Name}");
            builder.Append(GetMethodParamsInfo(info));
            return builder.ToString();
        }
        private static string SignatureOfEvent(EventInfo info)
        {
            StringBuilder builder = new();
            if (info.AddMethod is MethodInfo addMethod)
            {
                if (addMethod.IsPublic) builder.Append("public ");
                else if (addMethod.IsPrivate) builder.Append("private ");
                else if (addMethod.IsAssembly) builder.Append("internal ");
                else if (addMethod.IsFamily) builder.Append("protected ");
                else if (addMethod.IsFamilyAndAssembly) builder.Append("internal protected ");
                else if (addMethod.IsFamilyOrAssembly) builder.Append("private protected ");
            }

            Type? handlerType = info.EventHandlerType;
            builder.Append($"event {handlerType?.Name ?? "?"}");
            builder.Append(GetGenericInfo(handlerType as TypeInfo));
            builder.Append($" {info.Name}");
            return builder.ToString();
        }
        private static string SignatureOfField(FieldInfo info)
        {
            StringBuilder builder = new();
            if (info.IsPublic) builder.Append("public ");
            else if (info.IsPrivate) builder.Append("private ");
            else if (info.IsAssembly) builder.Append("internal ");
            else if (info.IsFamily) builder.Append("protected ");
            else if (info.IsFamilyAndAssembly) builder.Append("internal protected ");
            else if (info.IsFamilyOrAssembly) builder.Append("private protected ");

            if (info.IsStatic) builder.Append("static ");

            if (info.IsLiteral) builder.Append("const ");
            else if (info.IsInitOnly) builder.Append("readonly ");

            Type type = info.FieldType;
            builder.Append(type.Name);
            builder.Append(GetGenericInfo(type as TypeInfo));
            builder.Append($" {info.Name}");
            return builder.ToString();
        }
        private static string SignatureOfMethod(MethodInfo info)
        {
            StringBuilder builder = new();
            if (info.IsPublic) builder.Append("public ");
            else if (info.IsPrivate) builder.Append("private ");
            else if (info.IsAssembly) builder.Append("internal ");
            else if (info.IsFamily) builder.Append("protected ");
            else if (info.IsFamilyAndAssembly) builder.Append("internal protected ");
            else if (info.IsFamilyOrAssembly) builder.Append("private protected ");

            if (info.IsStatic) builder.Append("static ");
            else if (info.IsAbstract) builder.Append("abstract ");
            else if (info.IsVirtual) builder.Append("virtual ");
            else if (info.IsFinal) builder.Append("sealed ");
            else if (info.IsHideBySig) builder.Append("new ");
            else if ((info.DeclaringType?.BaseType?.GetMethod(info.Name)?.GetMethodBody() 
                ?? info.GetMethodBody()) != info.GetMethodBody()) builder.Append("override ");

            Type returnType = info.ReturnType;
            builder.Append(returnType?.Name ?? "void");
            builder.Append(GetGenericInfo(returnType as TypeInfo));
            builder.Append($" {info.Name}");
            builder.Append(GetMethodParamsInfo(info));
            return builder.ToString();
        }
        private static string SignatureOfProperty(PropertyInfo info)
        {
            StringBuilder builder = new();
            MethodInfo? getter = info.GetMethod;
            MethodInfo? setter = info.SetMethod;

            string getterAccessModifiers;
            if (getter is null) getterAccessModifiers = string.Empty;
            else if (getter.IsPublic) getterAccessModifiers = "public ";
            else if (getter.IsAssembly) getterAccessModifiers = "internal ";
            else if (getter.IsFamily) getterAccessModifiers = "protected ";
            else if (getter.IsFamilyOrAssembly) getterAccessModifiers ="protected internal";
            else if (getter.IsFamilyAndAssembly) getterAccessModifiers = "private protected ";
            else getterAccessModifiers = "private ";

            string setterAccessModifiers;
            if (setter is null) setterAccessModifiers = string.Empty;
            else if (setter.IsPublic) setterAccessModifiers = "public ";
            else if (setter.IsAssembly) setterAccessModifiers = "internal ";
            else if (setter.IsFamily) setterAccessModifiers = "protected ";
            else if (setter.IsFamilyOrAssembly) setterAccessModifiers = "protected internal ";
            else if (setter.IsFamilyAndAssembly) setterAccessModifiers = "private protected ";
            else setterAccessModifiers = "private ";

            string propertyAccessModifiers;
            if (getter is null) propertyAccessModifiers = setterAccessModifiers;
            else if (setter is null) propertyAccessModifiers = getterAccessModifiers;
            else if (getter.IsPublic || setter.IsPublic) propertyAccessModifiers = "public ";
            else if (getter.IsAssembly || setter.IsAssembly) propertyAccessModifiers = "internal ";
            else if (getter.IsFamily || setter.IsFamily) propertyAccessModifiers = "protected ";
            else if (getter.IsFamilyOrAssembly || setter.IsFamilyOrAssembly) propertyAccessModifiers = "protected internal ";
            else if (getter.IsFamilyAndAssembly || setter.IsFamilyAndAssembly) propertyAccessModifiers = "private protected ";
            else propertyAccessModifiers = "private ";

            if (getterAccessModifiers.Equals(propertyAccessModifiers)) getterAccessModifiers = string.Empty;
            if (setterAccessModifiers.Equals(propertyAccessModifiers)) setterAccessModifiers = string.Empty;

            builder.Append(propertyAccessModifiers);
            if ((getter?.IsStatic ?? false) || (setter?.IsStatic ?? false)) builder.Append("static ");
            Type type = info.PropertyType;
            builder.Append(type.Name);
            builder.Append(GetGenericInfo(type as TypeInfo));
            builder.Append($" {info.Name}");
            builder.Append("{ ");
            if (getter is not null) builder.Append(getterAccessModifiers + "get; ");
            if (setter is not null) builder.Append(setterAccessModifiers + "set; ");
            builder.Append('}');
            return builder.ToString();
        }
        private static string SignatureOfType(TypeInfo info)
        {
            StringBuilder builder = new();
            if (info.IsNested)
            {
                if (info.IsNestedPublic) builder.Append("public ");
                else if (info.IsNestedPrivate) builder.Append("private ");
                else if (info.IsNestedAssembly) builder.Append("internal ");
                else if (info.IsNestedFamily) builder.Append("protected ");
                else if (info.IsNestedFamANDAssem) builder.Append("internal protected ");
                else if (info.IsNestedFamORAssem) builder.Append("private protected ");
            }
            else if (info.IsPublic) builder.Append("public ");
            else if (info.IsNotPublic) builder.Append("internal ");

            if (info.IsClass)
            {
                if (info.IsAbstract && info.IsSealed) builder.Append("static ");
                else if (info.IsAbstract) builder.Append("abstract ");
                else if (info.IsSealed) builder.Append("sealed ");

                builder.Append($"class {info.Name}");
            }
            else if (info.IsInterface) builder.Append($"interface {info.Name}");
            else if (info.IsArray) builder.Append($"Array {info.Name}");
            else if (info.IsEnum) builder.Append($"enum {info.Name}");
            else if (info.IsValueType) builder.Append($"struct {info.Name}");

            builder.Append(GetGenericInfo(info));
            return builder.ToString();
        }
    }
}
