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

            List<MemberReferenceInfo> filtered = new List<MemberReferenceInfo>();

            foreach (var member in members)
            {
                if (Filter.HasFlag(ReferenceBindingFlags.NonCompiler) && member.IsCompilerGenerated) continue;

                if (Filter.HasFlag(ReferenceBindingFlags.NoPropertyMethods) && (member.IsSetter(out _) || member.IsSetter(out _))) continue; 

                if (member.Member.DeclaringType != null)
                {
                    Predicate<MemberInfo> siblingRefPredicate = m => m.DeclaringType != null && m.DeclaringType == member.Member.DeclaringType;
                    if (Filter.HasFlag(ReferenceBindingFlags.NoSiblingReferences) &&
                        (member.ReferencedMembers.Keys.ToList().Exists(siblingRefPredicate) ||
                        member.ReferencingMembers.Keys.ToList().Exists(siblingRefPredicate))) continue;
                }

                if (Filter.HasFlag(ReferenceBindingFlags.WithReferences) && 
                    member.ReferencedMembers.Count + member.ReferencingMembers.Count == 0) continue;

                if (Filter.HasFlag(ReferenceBindingFlags.WithReferences) &&
                    (member.ReferencedMembers.ToList().FindAll(m => !members.ToList().Find(mri => mri.Member.HasSameMetadataDefinitionAs(m.Key))?.IsCompilerGenerated ?? true).Count
                    + member.ReferencingMembers.ToList().FindAll(m => !members.ToList().Find(mri => mri.Member.HasSameMetadataDefinitionAs(m.Key))?.IsCompilerGenerated ?? true).Count == 0)) continue;

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
        NoPropertyMethods = 5
    }
}
