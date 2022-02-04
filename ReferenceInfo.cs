namespace DependencyAnalyzer;

public record ReferenceInfo(MemberInfo ReferencingMember, MemberInfo ReferencedMemeber)
{
    public override string ToString()
    {
        System.Text.StringBuilder stringBuilder = new();
        stringBuilder.Append($"{ReferencingMember.DeclaringType?.Name}.{ReferencingMember.Name}");
        stringBuilder.Append($" -> ");
        stringBuilder.Append($"{ReferencedMemeber.DeclaringType?.Name}.{ReferencedMemeber.Name}");
        return stringBuilder.ToString();
    }
}
