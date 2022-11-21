using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DependencyAnalyzer
{
    /// <summary>
    /// Couples reference information to a type
    /// </summary>
    public sealed class ClassReferenceInfo : ReferenceInfo
    {
        public ClassReferenceInfo? CompilerClass => IsCompilerClass ? null : NestedMembers.Find(n => n.IsCompilerClass);
        /// <summary>
        /// All MemberReferenceInfo objects that are within this object (not including ClassReferenceInfo objects)
        /// </summary>
        internal List<MemberReferenceInfo> FlattenedReferenceMembers
        {
            get
            {
                List<MemberReferenceInfo> flattenedMembers = new(NonNestedMembers);
                NestedMembers.ForEach(n => flattenedMembers.AddRange(n.FlattenedReferenceMembers));
                return flattenedMembers;
            }
        }
        /// <summary>
        /// All ClassReferenceInfo objects that are within this object (not including this object)
        /// </summary>
        internal List<ClassReferenceInfo> FlattenedReferenceTypes
        {
            get
            {
                List<ClassReferenceInfo> flattenedTypes = new(NestedMembers);
                NestedMembers.ForEach(n => flattenedTypes.AddRange(n.FlattenedReferenceTypes));
                return flattenedTypes;
            }
        }
        public IReadOnlyList<ReferenceInfo> Members => _members;
        public string? Namespace => ((Type)Host).Namespace;
        internal List<ClassReferenceInfo> NestedMembers 
            => _members.FindAll(m => m is ClassReferenceInfo).ConvertAll<ClassReferenceInfo>(r => r as ClassReferenceInfo);
        internal List<MemberReferenceInfo> NonNestedMembers
            => _members.FindAll(m => m is MemberReferenceInfo).ConvertAll<MemberReferenceInfo>(r => r as MemberReferenceInfo);

        private readonly List<ReferenceInfo> _members = new();


        public ClassReferenceInfo(Type type) : base(type as TypeInfo)
        {
            foreach (MemberInfo m in type.GetMembers(Architecture.Filter))
                _members.Add(m is TypeInfo t ? new ClassReferenceInfo(t) : new MemberReferenceInfo(m));
        }


        internal override void FindReferencedMembers(IEnumerable<ClassReferenceInfo> referenceTypes)
            => _members.ForEach(m => m.FindReferencedMembers(referenceTypes));
        internal override void FindReferencingMembers(IEnumerable<ClassReferenceInfo> referenceTypes)
            => _members.ForEach(m => m.FindReferencingMembers(referenceTypes));
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
                    _members.Add(member);
            }
        }
        public override string ToString() => (Host as Type).FullName ?? Host.Name;
    }
}
