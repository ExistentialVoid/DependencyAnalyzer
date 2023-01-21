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
        public new void Add(Reference reference)
        {
            Reference? R = base.Find(r => r.Equals(reference));

            if (R is not null) R.Count += reference.Count;
            else base.Add(reference);
        }
        /// <summary>
        /// Decrease the occurance of a reference, or remove if no remaining occurances.
        /// </summary>
        /// <param name="referencedMember">The member being referenced</param>
        /// <param name="referencingMember">The member doing the referencing</param>
        /// <param name="count">Number of occurances (Default is 0, force removal)</param>
        public new void Remove(Reference reference)
        {
            Reference? R = base.Find(r => r.Equals(reference));

            if (R is not null)
            {
                R.Count -= reference.Count;
                if (R.Count <= 0) base.Remove(R);
            }
        }
    }
}
