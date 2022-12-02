using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DependencyAnalyzer
{
    /// <summary>
    /// Couples reference information to a type
    /// </summary>
    internal sealed class TypeReferenceInfo : ReferenceInfo
    {
        public Architecture Architecture { get; }
        /// <summary>
        /// All MemberReferenceInfo objects that are within this object (not including TypeReferenceInfo objects)
        /// </summary>
        internal List<MemberReferenceInfo> FlattenedMembers { get; }
        /// <summary>
        /// All TypeReferenceInfo objects that are within this object (not including this object)
        /// </summary>
        internal List<TypeReferenceInfo> FlattenedTypes { get; }
        internal List<MemberReferenceInfo> Members { get; } = new();
        public string Namespace => ((Type)Host).Namespace;
        internal List<TypeReferenceInfo> NestedClasses { get; } = new();


        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="info">TypeReferenceInfo to copy</param>
        internal TypeReferenceInfo(TypeReferenceInfo info) : base(info)
        {
            Architecture = info.Architecture;

            // replace all members with cloned members
            foreach (MemberReferenceInfo m in info.Members) Members.Add(new(m));
            foreach (TypeReferenceInfo n in info.NestedClasses) NestedClasses.Add(new(n));

            FlattenedMembers = new(Members);
            FlattenedTypes = new() { this };
            foreach (TypeReferenceInfo n in NestedClasses)
            {
                FlattenedMembers.AddRange(n.FlattenedMembers);
                FlattenedTypes.AddRange(n.FlattenedTypes);
            }
        }
        /// <summary>
        /// Non-nested types' wrapper constructor
        /// </summary>
        /// <param name="type"></param>
        public TypeReferenceInfo(Architecture architecture, Type type) : this(architecture, type, null) { }
        /// <summary>
        /// Nested types' wrapper constructor
        /// </summary>
        /// <param name="type"></param>
        /// <param name="parent"></param>
        public TypeReferenceInfo(Architecture architecture, Type type, TypeReferenceInfo parent) : base(type, parent)
        {
            Architecture = architecture;

            foreach (MemberInfo m in type.GetMembers(Architecture.Filter))
            {
                if (m is Type t) NestedClasses.Add(new(architecture, t, this));
                else Members.Add(new(m, this));
            }

            FlattenedMembers = new(Members);
            FlattenedTypes = new() { this };
            foreach (TypeReferenceInfo n in NestedClasses)
            {
                FlattenedMembers.AddRange(n.FlattenedMembers);
                FlattenedTypes.AddRange(n.FlattenedTypes);
            }
        }


        /// <summary>
        /// Determine if the Members collection contains a wrapper of the given member
        /// </summary>
        /// <param name="member"></param>
        /// <returns>True if the member has the same metadata definition as any Host in the Members collection</returns>
        internal bool Contains(MemberInfo member) => Members.Exists(m => m.Host.HasSameMetadataDefinitionAs(member));
        internal override void FindReferencedMembers() => Members.ForEach(m => m.FindReferencedMembers());
        internal override void FindReferencingMembers() => Members.ForEach(m => m.FindReferencingMembers());
        /// <summary>
        /// Return the desired ReferenceInfo wrappper
        /// </summary>
        /// <param name="member"></param>
        /// <returns>The wrapper of a matched Member based on metadata definition</returns>
        internal MemberReferenceInfo GetMemberBy(MemberInfo member) => Members.Find(m => m.Host.HasSameMetadataDefinitionAs(member));
        /// <summary>
        /// Return the desired ReferenceInfo wrapper
        /// </summary>
        /// <param name="name">the exact name of a member</param>
        /// <returns>The wrapper of a matched Member based on name</returns>
        internal MemberReferenceInfo GetMemberBy(string name) => Members.Find(m => m.Host.Name.Equals(name));
        public override string ToFormattedString(string spacing)
        {
            Filter filter = Architecture.ReportFilter;

            if (filter.SimplifyCompilerReferences && IsCompilerGenerated) return string.Empty;

            ReportFormat format = Architecture.ReportFormat;
            StringBuilder builder = new();
            builder.Append($"{spacing}{ToString(format)}");
            spacing += '\t';

            int memberCount = 0;
            foreach (var member in Members)
            {
                string formattedString = member.ToFormattedString(spacing);
                if (!formattedString.Equals(string.Empty))
                {
                    builder.Append(formattedString);
                    memberCount++;
                }
            }
            foreach (var nested in NestedClasses)
            {
                string formattedString = nested.ToFormattedString(spacing);
                if (!formattedString.Equals(string.Empty))
                {
                    builder.Append(formattedString);
                    memberCount++;
                }
            }

            if (filter.ExistingReferenceCondition == Condition.With &&
                memberCount == 0) return string.Empty;
            else if (filter.ExistingReferenceCondition == Condition.Without &&
                memberCount != 0) return string.Empty;

            string info = builder.ToString();
            if (filter.ExcludeNamespace ?? false) info = info.Replace($"{Namespace}.", string.Empty);

            return info;
        }
    }
}
