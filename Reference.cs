using System.Reflection;

namespace DependencyAnalyzer
{
    /// <summary>
    /// Simple object to implement IReference
    /// </summary>
    internal class Reference : IReference
    {
        public uint Count { get; set; }
        public MemberInfo ReferencedMember { get; set; }
        public MemberInfo ReferencingMember { get; set; }

        public Reference(MemberInfo referencingMember, MemberInfo referencedMember, uint count)
        {
            ReferencingMember = referencingMember;
            ReferencedMember = referencedMember;
            Count = count;
        }
    }
}
