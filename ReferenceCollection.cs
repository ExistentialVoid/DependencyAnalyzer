using System.Collections.Generic;
using System.Reflection;

namespace DependencyAnalyzer
{
    internal sealed class ReferenceCollection : List<Reference>
    {
        public ReferenceCollection() { }
        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="collection"></param>
        public ReferenceCollection(ReferenceCollection collection)
        {
            foreach (var item in collection) Add(new(item.ReferencingMember, item.ReferencedMember, item.Count));
        }


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
            Reference? R = base.Find(referenceMatch);

            if (R is not null) R.Count += count;
            else base.Add(new Reference(referencingMember, referencedMember, count));
        }
        public void Include(Reference item) => Include(item.ReferencedMember, item.ReferencingMember, item.Count);
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
            Reference? R = base.Find(referenceMatch);

            if (R is not null)
            {
                R.Count -= count;
                if (count == 0 || R.Count == 0) base.Remove(R);
            }
        }
        public void Exclude(Reference item) => Exclude(item.ReferencedMember, item.ReferencingMember, item.Count);
    }
}
