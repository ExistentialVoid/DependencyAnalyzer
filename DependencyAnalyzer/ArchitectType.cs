using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DependencyAnalyzer
{
    /// <summary>
    /// An object to treat class-types as architectural structs
    /// </summary>
    internal class ArchitectType
    {
        internal readonly Architecture Architecture;
        internal readonly Type Class;
        internal readonly Architect.ArchType Type;
        internal List<ArchitectMember> Members;
        internal List<Type> Interfaces;
        private readonly BindingFlags standard = BindingFlags.Public | BindingFlags.DeclaredOnly 
            | BindingFlags.Instance | BindingFlags.Static;

        internal ArchitectType(Architecture architecture, Type cls)
        {
            Architecture = architecture;
            Class = cls;
            Type = cls.GetArchType();
            Members = GetMembers();
            Interfaces = GetInterfaces();
        }

        /// <summary>
        /// Get the DeclaringType of each member in DependentMembers
        /// </summary>
        /// <returns>A list of Type (Class of ArchitectObject)</returns>
        internal List<ArchitectType> DependentClasses()
        {
            List<ArchitectType> list = new List<ArchitectType>();
            Members.ForEach(M => M.DependentMembers.ForEach(m => 
            { 
                if (!list.Contains(m.ArchitectType)) list.Add(m.ArchitectType); 
            }));
            return list;
        }

        /// <summary>
        /// Get the DeclaringType of each member in ReferencedMembers
        /// </summary>
        /// <returns>A list of Type (Class of ArchitectObject)</returns>
        internal List<ArchitectType> ReferencedClasses()
        {
            List<ArchitectType> list = new List<ArchitectType>();
            Members.ForEach(M => M.ReferencedMembers.ForEach(m =>
            {
                if (!list.Contains(m.ArchitectType)) list.Add(m.ArchitectType);
            }));
            return list;
        }

        /// <summary>
        /// Store properties and methods of Type(obj) into a string
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        internal string GetInfo()
        {
            /*  Name [ArchType] : BaseType, Interface_1, ...
             *  Classes:
             *      NestedType_1_Name [NestedType_1_Type] ...
             *  Fields: ...
             *  Properties: ...
             *  Methods: ...
             *  Events: ...
             *  References To:
             *      Reference_1_Class.Reference_1_Member ...
             *  References From: ...
             */

            string headIndent = "\n";
            string itemIndent = "\n\t";

            // Name
            string info = $"{Class.Name}";

            // [Type] : Base
            info += Type != Architect.ArchType.Unknown ? " [" + Type + "]" : "";
            info += Class.BaseType == null ? "" : " : " + Class.BaseType.Name + (Interfaces.Count > 0 ? "," : "");

            // Interfaces
            Interfaces.ForEach(I => info += " " + I.Name + (I == Interfaces[Interfaces.Count - 1] ? "" : ", "));

            info += InfoComponent(MemberTypes.NestedType);
            info += InfoComponent(MemberTypes.Field);
            info += InfoComponent(MemberTypes.Property);
            info += InfoComponent(MemberTypes.Method);
            info += InfoComponent(MemberTypes.Event);

            // References to
            List<ArchitectType> refTypes = ReferencedClasses();
            if (refTypes.Count > 0) info += $"{headIndent}References to:";
            refTypes.ForEach(t => info = $"{itemIndent}{t.Class.Name}");

            // References from
            refTypes = DependentClasses();
            if (refTypes.Count > 0) info += $"{headIndent}References from:";
            refTypes.ForEach(t => info = $"{itemIndent}{t.Class.Name}");

            return info;
        }

        private string InfoComponent(MemberTypes type)
        {
            string headIndent = "\n";
            string itemIndent = "\n\t";
            string info = string.Empty;
            List<MemberInfo> members = new List<MemberInfo>();
            Members.FindAll(m => m.Member.MemberType == type).ForEach(m => members.Add(m.Member));
            if (members.Count == 0) return string.Empty;

            info += $"{headIndent}{type}:";
            switch (type)
            {
                case MemberTypes.Field:
                    members.ForEach(m =>
                    {
                        string typeName = m.Name.Contains("List") ? $"List<{((FieldInfo)m).FieldType.GetGenericArguments()[0].Name}>" : ((FieldInfo)m).FieldType.Name;
                        info += $"{itemIndent}{m.Name} [{typeName}]";
                    });
                    break;
                case MemberTypes.Property:
                    members.ForEach(m =>
                    {
                        string typeName = m.Name.Contains("List") ? $"List<{((PropertyInfo)m).PropertyType.GetGenericArguments()[0].Name}>" : ((PropertyInfo)m).PropertyType.Name;
                        info += $"{itemIndent}{m.Name} [{typeName}]";
                    });
                    break;
                case MemberTypes.Method:
                    members.ForEach(m =>
                    {
                        bool includeComma = false;
                        string paramsStr = string.Empty;
                        ((MethodInfo)m).GetParameters().ToList().ForEach(p =>
                        {
                            if (includeComma) paramsStr += ", ";
                            includeComma = true;
                            paramsStr += p.ParameterType.Name.Contains("List") ? $"List<{p.ParameterType.GetGenericArguments()[0].Name}>" : p.ParameterType.Name;
                        });
                        info += $"{itemIndent}{m.Name}( {paramsStr} )";
                    });
                    break;
                case MemberTypes.NestedType:
                case MemberTypes.Event:
                    members.ForEach(m => info += $"{itemIndent}{m.Name}");
                    break;
                default:
                    break;
            }
            return info;
        }

        private List<Type> GetInterfaces() => Class.GetInterfaces().ToList().FindAll(I => I.Namespace == Class.Namespace);

        private List<ArchitectMember> GetMembers()
        {
            List<ArchitectMember> members = new List<ArchitectMember>();
            Class.GetMembers(standard).ToList().FindAll(m => m.DeclaringType == Class).ForEach(m =>
            {
                members.Add(new ArchitectMember(this, m));
            });
            return members;
        }
    }
}
