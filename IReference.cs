using System.Reflection;

namespace DependencyAnalyzer
{
    /// <summary>
    /// The core information to define a reference relation
    /// </summary>
    public interface IReference
    {
        /// <summary>
        /// The number of occurances
        /// </summary>
        uint Count { get; set; }
        /// <summary>
        /// The member being referenced
        /// </summary>
        MemberInfo ReferencedMember { get; set; }
        /// <summary>
        /// The member doing the referencing
        /// </summary>
        MemberInfo ReferencingMember { get; set; }
    }
}
