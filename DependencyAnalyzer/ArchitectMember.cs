using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DependencyAnalyzer
{
    internal class ArchitectMember
    {
        internal readonly ArchitectType ArchitectType;
        internal MemberInfo Member;
        internal List<ArchitectMember> ReferencedMembers = new List<ArchitectMember>();
        internal List<ArchitectMember> DependentMembers = new List<ArchitectMember>();

        internal ArchitectMember(ArchitectType type, MemberInfo member)
        {
            ArchitectType = type;
            Member = member;
        }
    }
}
