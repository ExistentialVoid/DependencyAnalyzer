using System;
using System.Reflection;
using System.Text;

namespace SelfReferencing
{
    internal sealed class SignatureBuilder
    {
        private MemberInfo _member;
        private StringBuilder builder;

        private void AppendGenericInfo(TypeInfo info)
        {
            if (!info.IsGenericType) return;

            string genInfo = "[";
            Type[] genArgs = info.GetGenericArguments();
            foreach (Type genArg in genArgs) genInfo += $"{genArg.Name}, ";

            builder.Append(genInfo.TrimEnd().TrimEnd(',') + ']');
        }
        private void AppendMethodParamsInfo()
        {
            if (_member is not MethodBase info) return;

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
            builder.Append(paramInfo.TrimEnd().TrimEnd(',') + ')');
        }
        private void AppendPropertyAccessorInfo()
        {
            if (_member is not PropertyInfo info) return;

            string propertyInfo = " { get; ";
            MethodInfo accessor = info.SetMethod;
            if (accessor is not null)
            {
                string modifier = accessor.IsPublic ? "public " :
                    (accessor.IsPrivate ? "private " :
                    (accessor.IsAssembly ? "internal " :
                    (accessor.IsFamily ? "protected " :
                    (accessor.IsFamilyAndAssembly ? "protected internal " : "private protected "))));
                propertyInfo += $"{modifier}set; ";
            }
            propertyInfo += "}";
            builder.Append(propertyInfo);
        }
        internal string GetSignature(MemberInfo member)
        {
            _member = member;
            builder = new();

            switch (_member.MemberType)
            {
                case MemberTypes.Field:
                    SignatureOfField();
                    break;
                case MemberTypes.Property:
                    SignatureOfProperty();
                    break;
                case MemberTypes.Method:
                    SignatureOfMethod();
                    break;
                case MemberTypes.Constructor:
                    SignatureOfConstructor();
                    break;
                case MemberTypes.Event:
                    SignatureOfEvent();
                    break;
                case MemberTypes.NestedType:
                case MemberTypes.TypeInfo:
                    SignatureOfType();
                    break;
                default: break;
            }

            return builder.ToString();
        }
        private void SignatureOfConstructor()
        {
            ConstructorInfo info = _member as ConstructorInfo;

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
            AppendMethodParamsInfo();
        }
        private void SignatureOfEvent()
        {
            EventInfo info = _member as EventInfo;

            if (info.AddMethod.IsPublic) builder.Append("public ");
            else if (info.AddMethod.IsPrivate) builder.Append("private ");
            else if (info.AddMethod.IsAssembly) builder.Append("internal ");
            else if (info.AddMethod.IsFamily) builder.Append("protected ");
            else if (info.AddMethod.IsFamilyAndAssembly) builder.Append("internal protected ");
            else if (info.AddMethod.IsFamilyOrAssembly) builder.Append("private protected ");

            Type handlerType = info.EventHandlerType;
            builder.Append($"event {handlerType.Name}");

            AppendGenericInfo(handlerType as TypeInfo);
            builder.Append($" {info.Name}");
        }
        private void SignatureOfField()
        {
            FieldInfo info = _member as FieldInfo;

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

            AppendGenericInfo(type as TypeInfo);
            builder.Append($" {info.Name}");
        }
        private void SignatureOfMethod()
        {
            MethodInfo info = _member as MethodInfo;

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
            else if ((info.DeclaringType.BaseType?.GetMethod(info.Name)?.GetMethodBody() 
                ?? info.GetMethodBody()) != info.GetMethodBody()) builder.Append("override ");

            Type returnType = info.ReturnType ?? null;
            builder.Append(returnType?.Name ?? "void");

            AppendGenericInfo(returnType as TypeInfo);
            builder.Append($" {info.Name}");
            AppendMethodParamsInfo();
        }
        private void SignatureOfProperty()
        {
            PropertyInfo info = _member as PropertyInfo;

            MethodInfo accessor = info.CanRead ? info.GetMethod : info.SetMethod;
            if (accessor.IsPublic) builder.Append("public ");
            else if (accessor.IsPrivate) builder.Append("private ");
            else if (accessor.IsAssembly) builder.Append("internal ");
            else if (accessor.IsFamily) builder.Append("protected ");
            else if (accessor.IsFamilyAndAssembly) builder.Append("internal protected ");
            else if (accessor.IsFamilyOrAssembly) builder.Append("private protected ");

            Type type = info.PropertyType;
            builder.Append(type.Name);

            AppendGenericInfo(type as TypeInfo);
            builder.Append($" {info.Name}");
            AppendPropertyAccessorInfo();
        }
        private void SignatureOfType()
        {
            TypeInfo info = _member as TypeInfo;

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

            AppendGenericInfo(info);
        }
    }
}
