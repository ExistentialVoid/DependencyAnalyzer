using System;
using System.Reflection;
using System.Text;

namespace DependencyAnalyzer
{
    public class ReferenceInfo
    {
        public int Occurances { get; set; }
        public MemberInfo ReferencingMember { get; }
        public MemberInfo ReferencedMember { get; }


        public ReferenceInfo(MemberInfo referencingMember, MemberInfo referencedMember)
        {
            ReferencingMember = referencingMember;
            ReferencedMember = referencedMember;
        }


        public override string ToString()
        {
            System.Text.StringBuilder stringBuilder = new();
            stringBuilder.Append(("(" + Occurances.ToString() + ")").PadRight(6));
            stringBuilder.Append(ReferencingMember.ArchName());
            stringBuilder.Append($"\n\t-> {ReferencedMember.ArchName()}");

            return stringBuilder.ToString();
        }
    }
}
