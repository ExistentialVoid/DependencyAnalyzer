using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DependencyAnalyzer
{
    internal sealed class MemberInterpreter
    {
        /*  Do not handle logic for compiler types, handling member impartially
         */

        /// <summary>
        /// The constructor-provided types without closures
        /// </summary>
        private readonly List<Type> types;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="types">All types to be considered</param>
        public MemberInterpreter(List<Type> types)
        {
            this.types = types.FindAll(t => !t.Name.Contains(">c"));
        }


        private List<MemberInfo> GetConstructorReferences(ConstructorInfo member)
        {
            if (member is null) return new();
            List<MemberInfo> refMembers = new();

            // parameter types
            member.GetParameters()
                .ToList()
                .ForEach(p => refMembers.AddRange(GetParameterReferences(p)));
            // local types will be considered in body IL parsing
            // body
            refMembers.AddRange(GetMethodBodyReferences(member));

            return refMembers;
        }
        private List<MemberInfo> GetEventReferences(EventInfo member)
        {
            if (member is null) return new();
            // EventHandlerType
            List<MemberInfo> refMembers = GetTypeReferences(member.EventHandlerType);

            // EventArgsType (Invoke method)
            // associated methods (OtherMethods)
            // Add and Remove methods are auto-generated
            // RaiseMethod will always return null in C#
            List<MethodInfo> methods = member.GetOtherMethods(true).ToList();
            if (member.EventHandlerType?.GetMethod("Invoke") is MethodInfo invokeM) methods.Add(invokeM);
            methods.ForEach(m => refMembers.AddRange(GetMethodReferences(m)));

            return refMembers;
        }
        private List<MemberInfo> GetFieldReferences(FieldInfo? member)
        {
            if (member is null) return new();
            return GetTypeReferences(member.FieldType);
        }
        internal List<MemberInfo> GetReferencedMembers(MemberInfo? member)
        {
            if (member is null) return new();
            return member.MemberType switch
            {
                MemberTypes.Constructor => GetConstructorReferences((ConstructorInfo)member),
                MemberTypes.Event => GetEventReferences((EventInfo)member),
                MemberTypes.Field => GetFieldReferences((FieldInfo)member),
                MemberTypes.Method => GetMethodReferences((MethodInfo)member),
                MemberTypes.Property => GetPropertyReferences((PropertyInfo)member),
                _ => new()
            };
        }
        private List<MemberInfo> GetMethodBodyReferences(MethodBase? methodbase)
        {
            if (methodbase is null) return new();
            List<MemberInfo> refMembers = new();

            MetadataObject mdo = new(methodbase);
            List<Instruction> instructions = mdo.GetInstructions();
            foreach (Instruction instruction in instructions)
            {
                foreach (Type type in types)
                {
                    List<MemberInfo> matchedMembers = 
                        type.GetMembers(Architecture.Filter)
                            .ToList()
                            .FindAll(m => instruction.Operand.Contains($"{type.Name}::{m.Name}"));
                    refMembers.AddRange(matchedMembers);
                    //if (instruction.Operand.Contains($" {type.Name}::")) refMembers.Add(type);
                }
            }

            return refMembers;
        }
        private List<MemberInfo> GetMethodReferences(MethodInfo? member)
        {
            if (member is null) return new();
            // return type
            List<MemberInfo> refMembers = GetTypeReferences(member.ReturnType);
            // parameter types
            member.GetParameters()
                .ToList()
                .ForEach(p => refMembers.AddRange(GetParameterReferences(p)));
            // generic args
            member.GetGenericArguments()
                .ToList()
                .ForEach(garg => refMembers.AddRange(GetTypeReferences(garg)));
            // local types will be handled in body IL parsing
            // method body
            refMembers.AddRange(GetMethodBodyReferences(member));

            return refMembers;
        }
        private List<MemberInfo> GetParameterReferences(ParameterInfo? param)
        {
            if (param is null) return new();
            return GetTypeReferences(param.ParameterType);
        }
        private List<MemberInfo> GetPropertyReferences(PropertyInfo? member)
        {
            if (member is null) return new();
            //type
            List<MemberInfo> refMembers = GetTypeReferences(member.PropertyType);
            //get & set accessor methods
            List<MethodInfo> methods = new();
            if (member.GetMethod is MethodInfo getM) methods.Add(getM);
            if (member.SetMethod is MethodInfo setM) methods.Add(setM);
            methods.ForEach(m => refMembers.AddRange(GetMethodReferences(m)));

            return refMembers;
        }
        private List<MemberInfo> GetTypeReferences(Type? t)
        {
            if (t is null) return new();
            List<MemberInfo> refMembers = new();

            if (types.Contains(t)) refMembers.Add(t);
            refMembers.AddRange(
                t.GenericTypeArguments
                    .ToList()
                    .FindAll(garg => types.Contains(garg))
            );

            return refMembers;
        }
    }
}
