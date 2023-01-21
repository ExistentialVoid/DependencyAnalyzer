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
        internal string ReferencedMemberFullName { get; }
        internal bool ReferencedMemberIsCompilerGenerated { get; }
        public MemberInfo ReferencingMember { get; }
        internal string ReferencingMemberFullName { get; }
        internal bool ReferencingMemberIsCompilerGenerated { get; }

        public Reference(MemberInfo referencingMember, MemberInfo referencedMember, uint count)
        {
            ReferencingMember = referencingMember;
            ReferencedMember = referencedMember;
            Count = count;

            string fullname = referencingMember is TypeInfo T1 ? (T1.FullName ?? T1.Name)
                : $"{referencingMember.DeclaringType?.FullName}.{referencingMember.Name}";
            ReferencingMemberFullName = fullname;
            ReferencingMemberIsCompilerGenerated = fullname.Contains('>');

            fullname = referencedMember is TypeInfo T2 ? (T2.FullName ?? T2.Name)
                : $"{referencedMember.DeclaringType?.FullName}.{referencedMember.Name}";
            ReferencedMemberFullName = fullname;
            ReferencedMemberIsCompilerGenerated = fullname.Contains('>');
        }

        public override bool Equals(object? obj)
        {
            if (obj is not Reference reference) return false;
            return reference.ReferencedMember.HasSameMetadataDefinitionAs(ReferencedMember) &&
                reference.ReferencingMember.HasSameMetadataDefinitionAs(ReferencingMember);
        }
        public override string ToString() => $"{ReferencingMemberFullName} -> {ReferencedMemberFullName}";
    }
}
