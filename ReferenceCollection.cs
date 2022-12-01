using System;
using System.Collections.Generic;

namespace DependencyAnalyzer
{
    internal sealed class ReferenceCollection : Dictionary<ReferenceInfo, int>
    {
        /// <summary>
        /// Holder of the reference collection
        /// </summary>
        internal ReferenceInfo Member { get; }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="member">The holder of references</param>
        public ReferenceCollection(ReferenceInfo member)
        {
            Member = member;
        }
        /// <summary>
        /// Initialize with an existing collection of elements
        /// </summary>
        /// <param name="collection">An existing collection</param>
        /// <param name="member">The holder of references</param>
        internal ReferenceCollection(Dictionary<ReferenceInfo, int> collection, ReferenceInfo member) : this(member)
        {
            foreach (var item in collection) base.Add(item.Key, item.Value);
        }
        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="collection">The collection that will be copied</param>
        internal ReferenceCollection(ReferenceCollection collection)
        {
            Member = collection.Member;
            foreach (var item in collection) base.Add(item.Key, item.Value);
        }


        public void Add(KeyValuePair<ReferenceInfo, int> reference) => Add(reference.Key, reference.Value);
        public new void Add(ReferenceInfo member, int count = 1)
        {
            if (base.ContainsKey(member)) base[member] += count;
            else base.Add(member, count);
        }
        public void Remove(KeyValuePair<ReferenceInfo, int> reference) => Remove(reference.Key, reference.Value);
        public void Remove(ReferenceInfo member, int count)
        {
            if (base.ContainsKey(member))
            {
                base[member] -= count;
                if (base[member] <= 0) base.Remove(member);
            }
        }
        public int RemoveAll(Predicate<KeyValuePair<ReferenceInfo, int>> predicate)
        {
            int count = 0;
            foreach (var item in this)
            {
                if (predicate(item))
                {
                    base.Remove(item.Key);
                    count++;
                }
            }
            return count;
        }
        public void Replace(ReferenceInfo oldMember, ReferenceInfo newMember, int count)
        {
            Add(newMember, count);
            base.Remove(oldMember);
        }
    }
}
