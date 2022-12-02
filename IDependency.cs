using System.Reflection;

namespace DependencyAnalyzer
{
    public interface IDependency
    {
        MemberInfo ReferencedMember { get; }
        MemberInfo ReferencingMember { get; }
    }
}
