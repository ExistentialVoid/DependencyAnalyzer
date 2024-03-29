﻿namespace DependencyAnalyzer
{
    /// <summary>
    /// Provides a set of simple logical properties that can be applied to references
    /// </summary>
    public interface IReferenceFilter
    {
        /// <summary>
        /// Do not include namespaces when displaying names.
        /// </summary>
        bool? ExcludeNamespace { get; set; }
        /// <summary>
        /// Specify conditon of members' reference count.
        /// </summary>
        Condition ExistingReferenceCondition { get; set; }
        /// <summary>
        /// Include references to members of the same declaring type.
        /// </summary>
        bool IncludeSiblingReferences { get; set; }
        /// <summary>
        /// Include references to types (and nested types) as well as members
        /// </summary>
        bool IncludeTypeReferences { get; set; }
        /// <summary>
        /// Remove getters and setters while also relaying their references to their property.
        /// </summary>
        bool SimplifyAccessors { get; set; }
        /// <summary>
        /// Compiler-generated member references will be cut from the reference chain.
        /// </summary>
        bool SimplifyCompilerReferences { get; set; }
    }
}
