using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DependencyAnalyzer
{
    /// <summary>
    /// Couples reference information to a type
    /// </summary>
    public sealed class ClassReferenceInfo : ReferenceInfo
    {
        public ClassReferenceInfo? CompilerClass => IsCompilerClass ? null : NestedMembers.Find(n => n.IsCompilerClass);
        internal List<MemberReferenceInfo> FlattenedMembers
        {
            get
            {
                List<MemberReferenceInfo> flattenedMembers = new(NonNestedMembers);
                NestedMembers.ForEach(n => flattenedMembers.AddRange(n.FlattenedMembers));
                return flattenedMembers;
            }
        }
        public IReadOnlyList<ReferenceInfo> Members => _members;
        internal List<ClassReferenceInfo> NestedMembers 
            => _members.FindAll(m => m is ClassReferenceInfo).ConvertAll<ClassReferenceInfo>(r => r as ClassReferenceInfo);
        internal List<MemberReferenceInfo> NonNestedMembers
            => _members.FindAll(m => m is MemberReferenceInfo).ConvertAll<MemberReferenceInfo>(r => r as MemberReferenceInfo);

        private readonly List<ReferenceInfo> _members = new();


        public ClassReferenceInfo(Type type) : base(type as TypeInfo)
        {
            foreach (MemberInfo m in type.GetMembers(Architecture.Filter))
                _members.Add(m is TypeInfo t ? new ClassReferenceInfo(t) : new MemberReferenceInfo(m));
        }


        internal override void FindReferencedMembers(IEnumerable<ClassReferenceInfo> referenceTypes)
            => _members.ForEach(m => m.FindReferencedMembers(referenceTypes));
        internal override void FindReferencingMembers(IEnumerable<ClassReferenceInfo> referenceTypes)
            => _members.ForEach(m => m.FindReferencingMembers(referenceTypes));
        internal void ImportMembers(ClassReferenceInfo nested)
        {
            foreach (var member in nested.Members)
            {
                //if (!_members.Exists(m => m.Member.HasSameMetadataDefinitionAs(member.Member)) && !member.IsCompilerGenerated)
                    _members.Add(member);
            }
        }
        public override string ToString() => (Member as Type).FullName ?? Member.Name;
    }
}
