using System.Reflection;

namespace DependencyAnalyzer
{
    /// <summary>
    /// Simple object to implement IReference
    /// </summary>
    internal class Reference : IReference
    {
        public uint Count { get; set; }
        public MemberInfo ReferencedMember { get; }
        public MemberInfo ReferencingMember { get; }

        public Reference(MemberInfo referencingMember, MemberInfo referencedMember, uint count)
        {
            ReferencingMember = referencingMember;
            ReferencedMember = referencedMember;
            Count = count;
        }

        internal string GetReferencedMemberFullName()
        {
            string fullname = ReferencedMember is TypeInfo T ? (T.FullName ?? T.Name)
                : $"{ReferencedMember.DeclaringType?.FullName}.{ReferencedMember.Name}";
            return fullname;
        }
        internal string GetReferencingMemberFullName()
        {
            string fullname = ReferencingMember is TypeInfo T ? (T.FullName ?? T.Name)
                : $"{ReferencingMember.DeclaringType?.FullName}.{ReferencingMember.Name}";
            return fullname;
        }
    }
}
