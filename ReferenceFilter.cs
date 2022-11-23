using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DependencyAnalyzer
{
    /// <summary>
    /// Apply reference binding flags to a set of MemberReferenceInfos
    /// </summary>
    internal sealed class ReferenceFilter
    {
        public ReferenceBindingFlags Filter { get; }


        public ReferenceFilter(ReferenceBindingFlags filter)
        {
            Filter = filter;
        }


        public IReadOnlyList<MemberReferenceInfo> ApplyFilterTo(IList<MemberReferenceInfo> members)
        {
            if (members is null) return new List<MemberReferenceInfo>();
            if (Filter == ReferenceBindingFlags.Default) return new List<MemberReferenceInfo>(members);

            List<MemberReferenceInfo> filtered = new();

            foreach (var member in members)
            {
                if (Filter.HasFlag(ReferenceBindingFlags.NonCompiler) && member.IsCompilerGenerated) continue;

                if (Filter.HasFlag(ReferenceBindingFlags.NoPropertyMethods) && (member.IsGetter(out _) || member.IsSetter(out _))) continue; 

                if (member.Host.DeclaringType != null)
                {
                    bool isSiblingReference(MemberInfo m) => m.DeclaringType != null && m.DeclaringType == member.Host.DeclaringType;
                    if (Filter.HasFlag(ReferenceBindingFlags.NoSiblingReferences) &&
                        (member.ReferencedMembers.Keys.ToList().Exists(isSiblingReference)
                        || member.ReferencingMembers.Keys.ToList().Exists(isSiblingReference))) continue;
                }

                if (Filter.HasFlag(ReferenceBindingFlags.WithReferences) && 
                    member.ReferencedMembers.Count + member.ReferencingMembers.Count == 0) continue;

                if (Filter.HasFlag(ReferenceBindingFlags.WithReferences))
                {
                    bool hasMember(MemberInfo m) => !members.ToList().Find(mri => mri.Host.HasSameMetadataDefinitionAs(m))?.IsCompilerGenerated ?? true;
                    if (member.ReferencedMembers.Keys.ToList().FindAll(hasMember).Count
                        + member.ReferencingMembers.Keys.ToList().FindAll(hasMember).Count == 0) continue;
                }

                filtered.Add(member);
            }

            return filtered;
        }
    }


    /// <summary>
    /// Specifies filters for reference info members when reporting
    /// </summary>
    [Flags]
    public enum ReferenceBindingFlags
    {
        /// <summary>
        /// Specifies that no binding flags are defined.
        /// </summary>
        Default = 0,
        /// <summary>
        /// Specifies that members must have some reference components to appear in report.
        /// </summary>
        WithReferences = 1,
        /// <summary>
        /// Specifies that references to other members in own defining class will not appear in report.
        /// </summary>
        NoSiblingReferences = 2,
        /// <summary>
        /// Specifies that compiler-generated members will not appear in report
        /// </summary>
        NonCompiler = 4,
        /// <summary>
        /// Specifies that get and set methods will not appear in report
        /// </summary>
        NoPropertyMethods = 8
    }
}
