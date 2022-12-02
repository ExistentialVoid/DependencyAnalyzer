using System.Collections.Generic;
using System.Reflection;

namespace DependencyAnalyzer
{
    internal sealed class ReferenceCollection : List<Reference>
    {
        /// <summary>
        /// Increase the occurance of a reference, or add if a matching one doesn't exist.
        /// </summary>
        /// <param name="referencedMember">The member being referenced</param>
        /// <param name="referencingMember">The member doing the referencing</param>
        /// <param name="count">Number of occurances (Default is 1)</param>
        public void Include(MemberInfo referencedMember, MemberInfo referencingMember, uint count = 1)
        {
            bool referenceMatch(Reference r) =>
                r.ReferencedMember.HasSameMetadataDefinitionAs(referencedMember) &&
                r.ReferencingMember.HasSameMetadataDefinitionAs(referencingMember);
            if (base.Exists(referenceMatch))
            {
                Reference R = base.Find(referenceMatch);
                R.Count += count;
            }
            else
            {
                base.Add(new Reference(referencingMember, referencedMember, count));
            }
        }
        /// <summary>
        /// Decrease the occurance of a reference, or remove if no remaining occurances.
        /// </summary>
        /// <param name="referencedMember">The member being referenced</param>
        /// <param name="referencingMember">The member doing the referencing</param>
        /// <param name="count">Number of occurances (Default is 0, force removal)</param>
        public void Exclude(MemberInfo referencedMember, MemberInfo referencingMember, uint count = 0)
        {
            bool referenceMatch(Reference r) =>
                r.ReferencedMember.HasSameMetadataDefinitionAs(referencedMember) &&
                r.ReferencingMember.HasSameMetadataDefinitionAs(referencingMember);
            if (base.Exists(referenceMatch))
            {
                Reference R = base.Find(referenceMatch);
                R.Count -= count;
                if (count == 0 || R.Count == 0) base.Remove(R);
            }
        }
    }
}
