using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SelfReferencing
{
    /// <summary>
    /// Couples reference information to a type
    /// </summary>
    public sealed class ClassReferenceInfo
    {
        public Type Class => _class;
        internal bool IsCompilerGenerated => Class.Name.Contains(">");
        public IReadOnlyList<MemberReferenceInfo> Members => _members;
        public string Signature
        {
            get
            {
                if (_cachedSignature == null)
                {
                    SignatureBuilder sig = new();
                    _cachedSignature = sig.GetSignature(_class);
                }
                return _cachedSignature;
            }
        }

        private string _cachedSignature = null;
        private readonly Type _class;
        private readonly List<MemberReferenceInfo> _members = new();


        public ClassReferenceInfo(Type type)
        {
            _class = type;
            foreach (MemberInfo m in type.GetMembers(Architecture.Filter)) _members.Add(new(m));
        }


        internal void FindReferencedMembers(IEnumerable<ClassReferenceInfo> referenceTypes)
        {
            _members.ForEach(m => m.FindReferencedMembers(referenceTypes));

            // Flatten underlying members' references
            foreach (var member in _members)
            {
                if (member.IsAnonomous(out string declaredMethodName))
                {

                    ClassReferenceInfo cri = this;
                    MemberReferenceInfo declaredMethod = null;
                    while (declaredMethod == null && cri != null)
                    {
                        declaredMethod = cri.Members
                            .ToList()
                            .Find(m => m.Member.Name.Equals(declaredMethodName));
                        cri = referenceTypes
                            .ToList()
                            .Find(c => cri.Class.DeclaringType != null && c.Class.HasSameMetadataDefinitionAs(cri.Class.DeclaringType));
                    }

                    foreach (var referencedMember in member.ReferencedMembers)
                    {
                        if (referencedMember.Key is not MethodInfo) continue;
                        declaredMethod.AddReferencedMember(referencedMember.Key);
                    }
                }
                else if (member.IsGetter(out string propertyName))
                {
                    MemberReferenceInfo property = _members.Find(m => m.Member.Name.Equals(propertyName));
                    foreach (var referencedMember in member.ReferencedMembers)
                        property.AddReferencedMember(referencedMember.Key);
                }
                else if (member.IsSetter(out propertyName))
                {
                    MemberReferenceInfo property = _members.Find(m => m.Member.Name.Equals(propertyName));
                    foreach (var referencedMember in member.ReferencedMembers)
                        property.AddReferencedMember(referencedMember.Key);
                }
            }

            // Flatten closures
        }
        internal void FindReferencingMembers(IEnumerable<ClassReferenceInfo> referenceTypes)
            => _members.ForEach(m => m.FindReferencingMembers(referenceTypes));
        internal void ImportMembers(ClassReferenceInfo nested)
        {
            foreach (var member in nested.Members)
            {
                //if (!_members.Exists(m => m.Member.HasSameMetadataDefinitionAs(member.Member)) && !member.IsCompilerGenerated)
                    _members.Add(member);
            }
        }
        public override string ToString() => _class.FullName;
    }
}
