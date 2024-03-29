﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DependencyAnalyzer
{
    /// <summary>
    /// Apply reference binding flags to a set of MemberReferenceInfos
    /// </summary>
    internal sealed class Filter : IReferenceFilter
    {
        public bool? ExcludeNamespace { get; set; } = null;
        public Condition ExistingReferenceCondition { get; set; } = Condition.With | Condition.Without;
        public bool IncludeSiblingReferences { get; set; } = true;
        public bool IncludeTypeReferences { get; set; } = true;
        public bool SimplifyAccessors { get; set; } = true;
        public bool SimplifyCompilerReferences { get; set; } = true;


        /// <summary>
        /// Transfer all referenced members of a property's accessors to itself
        /// </summary>
        /// <param name="property"></param>
        /// <param name="collection">The existing collection to be modify</param>
        internal void RelayAccessorsReferencedMembers(MemberReferenceInfo property, ReferenceCollection collection)
        {
            if (property.Host is not PropertyInfo) return;

            MemberReferenceInfo getter =
                property.Parent.Architecture.FlattenedReferenceMembers.Find(m => m.Host.Name.Equals($"get_{property.Host.Name}"));
            MemberReferenceInfo setter =
                property.Parent.Architecture.FlattenedReferenceMembers.Find(m => m.Host.Name.Equals($"set_{property.Host.Name}"));

            if (getter is not null) getter.ReferencedMembers.ToList().ForEach(r => collection.Add(r.Key, r.Value));
            if (setter is not null) setter.ReferencedMembers.ToList().ForEach(r => collection.Add(r.Key, r.Value));
        }
        /// <summary>
        /// Transfer all referencing members of a property's accessors to itself
        /// </summary>
        /// <param name="property"></param>
        /// <param name="collection">The existing collection to be modify</param>
        internal void RelayAccessorsReferencingMembers(MemberReferenceInfo property, ReferenceCollection collection)
        {
            if (property.Host is not PropertyInfo) return;

            MemberReferenceInfo getter =
                property.Parent.Architecture.FlattenedReferenceMembers.Find(m => m.Host.Name.Equals($"get_{property.Host.Name}"));
            MemberReferenceInfo setter =
                property.Parent.Architecture.FlattenedReferenceMembers.Find(m => m.Host.Name.Equals($"set_{property.Host.Name}"));

            if (getter is not null) getter.ReferencingMembers.ToList().ForEach(r => collection.Add(r.Key, r.Value));
            if (setter is not null) setter.ReferencingMembers.ToList().ForEach(r => collection.Add(r.Key, r.Value));
        }
        /// <summary>
        /// Cut compiler member references from the downstream reference chain
        /// </summary>
        /// <param name="collection">Referenced member whose ReferencedMembers' compiler references will be relayed</param>
        /// <returns>A new collection of flattened, non-compiler members</returns>
        internal ReferenceCollection RelayReferencedCompilerReferences(ReferenceCollection collection)
        {
            ReferenceCollection newCollection = new(member: null);
            foreach (var reference in collection)
            {
                if (reference.Key.IsCompilerGenerated)
                {
                    Dictionary<ReferenceInfo, int> relays = RelayReferencedCompilerReferences(reference.Key.ReferencedMembers);
                    foreach (var r in relays)
                    {
                        newCollection.Add(r.Key, r.Value);
                    }
                }
                else newCollection.Add(reference.Key, reference.Value);
            }
            return newCollection;
        }
        /// <summary>
        /// Cut compiler member references from the upstream reference chain
        /// </summary>
        /// <param name="collection">Referencing members whose ReferencingMembers' compiler references will be relayed</param>
        /// <returns>A new collection of flattened, non-compiler members</returns>
        internal ReferenceCollection RelayReferencingCompilerReferences(ReferenceCollection collection)
        {
            ReferenceCollection newCollection = new(member: null);
            foreach (var reference in collection)
            {
                if (reference.Key.IsCompilerGenerated)
                {
                    Dictionary<ReferenceInfo, int> relays = RelayReferencingCompilerReferences(reference.Key.ReferencingMembers);
                    relays.ToList().ForEach(r => newCollection.Add(r.Key, r.Value));
                }
                else newCollection.Add(reference.Key, reference.Value);
            }
            return newCollection;
        }
        /// <summary>
        /// Remove all references to other members of the same class
        /// </summary>
        /// <param name="parent">The class that defines siblings</param>
        /// <param name="collection">The collection of references</param>
        internal void RemoveSiblingReferences(TypeReferenceInfo parent, ReferenceCollection collection)
            => collection.RemoveAll(r => r.Key.Parent?.Equals(parent) ?? r.Key.Equals(parent));
        /// <summary>
        /// Remove all references that are a type (TypeReferenceInfo)
        /// </summary>
        /// <param name="collection">The collection of references</param>
        /// <returns>A new key-value collection of filtered members</returns>
        internal void RemoveTypeReferences(ReferenceCollection collection)
            => collection.RemoveAll(r => r.Key is TypeReferenceInfo);
        /// <summary>
        /// Replace all instances of accessor methods with their respective property
        /// </summary>
        /// <param name="collection">The collection of references</param>
        internal void ReplaceAccessors(ReferenceCollection collection)
        {
            ReferenceCollection dupCollection = new(collection);
            foreach (var reference in dupCollection)
            {
                if (reference.Key is MemberReferenceInfo mri && mri.IsAccessor(out MemberReferenceInfo property))
                    collection.Replace(reference.Key, property, reference.Value);
            }
        }
    }
}
