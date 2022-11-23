using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DependencyAnalyzer
{
    /// <summary>
    /// Couples reference information to a type
    /// </summary>
    internal sealed class ClassReferenceInfo : ReferenceInfo
    {
        /// <summary>
        /// All MemberReferenceInfo objects that are within this object (not including ClassReferenceInfo objects)
        /// </summary>
        internal List<MemberReferenceInfo> FlattenedMembers { get; }
        /// <summary>
        /// All ClassReferenceInfo objects that are within this object (not including this object)
        /// </summary>
        internal List<ClassReferenceInfo> FlattenedTypes { get; }
        internal List<MemberReferenceInfo> Members { get; }
        public string? Namespace => ((Type)Host).Namespace;
        internal List<ClassReferenceInfo> NestedClasses { get; }


        public ClassReferenceInfo(Type type): base(type)
        {
            Members = new();
            NestedClasses = new();
            foreach (MemberInfo m in type.GetMembers(Architecture.Filter))
            {
                if (m is Type t) NestedClasses.Add(new(t));
                else Members.Add(new(m));
            }
            // properties will be handled on the MemberInterpreter
            Members.RemoveAll(mri => mri.IsGetter(out _) || mri.IsSetter(out _));

            FlattenedMembers = new(Members);
            FlattenedTypes = new();
            FlattenedTypes.Add(this);
            foreach (ClassReferenceInfo cri in NestedClasses)
            {
                FlattenedMembers.AddRange(cri.FlattenedMembers);
                FlattenedTypes.AddRange(cri.FlattenedTypes);
            }
        }


        internal override void FindReferencedMembers(List<ClassReferenceInfo> referenceTypes)
            => Members.ForEach(m => m.FindReferencedMembers(referenceTypes));
        internal override void FindReferencingMembers(List<ClassReferenceInfo> referenceTypes)
            => Members.ForEach(m => m.FindReferencingMembers(referenceTypes));
        internal string GetSimpleFormat(string tabs)
        {
            System.Text.StringBuilder builder = new();
            builder.AppendLine($"{tabs}{FullName}");
            tabs += '\t';
            foreach (ReferenceInfo m in Members)
            {
                if (m is ClassReferenceInfo c) builder.Append(c.GetSimpleFormat(tabs));
                else
                {
                    MemberReferenceInfo M = m as MemberReferenceInfo;
                    if (M.IsGetter(out _) || M.IsSetter(out _)) continue;
                    else if (M.IsBackingField(out _)) continue;

                    string info = $"{tabs}{m.FullName}";
                    if (m.Host is MethodInfo) info += "()";
                    else if (m.Host is PropertyInfo property)
                    {
                        info += "{ ";
                        if (property.GetMethod is not null) info += "get; ";
                        if (property.SetMethod is not null) info += "set; ";
                        info += '}';
                    } 
                    builder.AppendLine(info);
                }
            }
            return builder.ToString();
        }
        internal void ImportMembers(ClassReferenceInfo nested)
        {
            foreach (var member in nested.Members)
            {
                //if (!_members.Exists(m => m.Member.HasSameMetadataDefinitionAs(member.Member)) && !member.IsCompilerGenerated)
                Members.Add(member);
            }
        }
        public override string ToString() => (Host as Type).FullName ?? Host.Name;
    }
}
