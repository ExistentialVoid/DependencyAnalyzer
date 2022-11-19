using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DependencyAnalyzer
{
    /// <summary>
    /// Couples a member with other members the it references and that reference it
    /// </summary>
    public sealed class MemberReferenceInfo : ReferenceInfo
    {
        public IReadOnlyDictionary<MemberInfo, int> ReferencedMembers => _referencedMembers;
        public IReadOnlyDictionary<MemberInfo, int> ReferencingMembers => _referencingMembers;

        private readonly Dictionary<MemberInfo, int> _referencedMembers = new();
        private Dictionary<MemberInfo, int> _referencingMembers = new();


        public MemberReferenceInfo(MemberInfo member) : base(member) { }


        internal void AddReferencedMember(MemberInfo member, int count = 1)
        {
            MemberInfo existingRef = _referencedMembers
                                        .ToList()
                                        .Find(m => m.Key.HasSameMetadataDefinitionAs(member))
                                        .Key;

            if (existingRef != null) _referencedMembers[existingRef] += count;
            else if (!(member is TypeInfo type && (Member.DeclaringType?.HasSameMetadataDefinitionAs(type) ?? false)) &&
                (!member.DeclaringType?.HasSameMetadataDefinitionAs(Member.DeclaringType) ?? true)) _referencedMembers.Add(member, count);
        }
        internal void AddReferencedMember(KeyValuePair<MemberInfo, int> refMember) => AddReferencedMember(refMember.Key, refMember.Value);
        internal void AddReferencingMember(MemberInfo member, int count = 1)
        {
            MemberInfo existingRef = _referencingMembers
                                        .ToList()
                                        .Find(m => m.Key.HasSameMetadataDefinitionAs(member)).Key;

            if (existingRef != null) _referencingMembers[existingRef] += count;
            else if (!(member is TypeInfo type && (Member.DeclaringType?.HasSameMetadataDefinitionAs(type) ?? false)) &&
                (!member.DeclaringType?.HasSameMetadataDefinitionAs(Member.DeclaringType) ?? true)) _referencingMembers.Add(member, count);
        }
        internal void AddReferencingMember(KeyValuePair<MemberInfo, int> refMember) => AddReferencingMember(refMember.Key, refMember.Value);
        internal override void FindReferencedMembers(IEnumerable<ClassReferenceInfo> referenceTypes)
        {
            MemberInterpreter interpreter = new(referenceTypes.ToList().ConvertAll(c => c.Member as Type));
            List<MemberInfo> referencedMembers = interpreter.GetReferencedMembers(Member);
            
            List<ClassReferenceInfo> closures = referenceTypes.ToList().FindAll(c => c.IsClosure);
            foreach (var member in referencedMembers)
            {
                if (member.DeclaringType is Type t && closures.Exists(c => c.Member.HasSameMetadataDefinitionAs(t)))
                {
                    List<MemberInfo> closureMethodReferencedMembers = interpreter.GetReferencedMembers(member);
                    foreach (var closureMember in closureMethodReferencedMembers)
                        AddReferencedMember(closureMember);
                }
                else AddReferencedMember(member);
            }
        }
        internal override void FindReferencingMembers(IEnumerable<ClassReferenceInfo> referenceTypes)
        {
            _referencingMembers = new();
            foreach (ClassReferenceInfo cri in referenceTypes)
            {
                cri.FlattenedMembers
                    .FindAll(m => m.Member.HasSameMetadataDefinitionAs(Member))
                    .ForEach(m => AddReferencingMember(m.Member));
            }
        }
        /// <summary>
        /// Check if member is a compiler generated method.
        /// </summary>
        /// <param name="methodName"></param>
        /// <returns>True if name contains ">b__", otherwise false.</returns>
        internal bool IsAnonomous(out string methodName)
        {
            if (Member.Name.Contains(">b__"))
            {
                methodName = Member.Name.Split('>')[0].TrimStart('<');
                return true;
            }

            methodName = string.Empty;
            return false;
        }
        /// <summary>
        /// Check if member is a compiler generated underlying field of an auto-property.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns>True if name contains ">k__BackingField", otherwise false.</returns>
        internal bool IsBackingField(out string propertyName)
        {
            if (Member.Name.Contains(">k__BackingField"))
            {
                propertyName = Member.Name.Split('>')[0].TrimStart('<');
                return true;
            }

            propertyName = string.Empty;
            return false;
        }
        /// <summary>
        /// Check if member is the get method of a property.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns>True if name contains "get_", otherwise false.</returns>
        internal bool IsGetter(out string propertyName)
        {
            propertyName = string.Empty;
            if (!(Member is MethodInfo)) return false;

            if (Member.Name.Contains("get_"))
            {
                propertyName = Member.Name.Replace("get_", string.Empty);
                return true;
            }

            propertyName = string.Empty;
            return false;
        }
        /// <summary>
        /// Check if member is the set method of a property.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns>True if name contains "set_", otherwise false.</returns>
        internal bool IsSetter(out string propertyName)
        {
            propertyName = string.Empty;
            if (!(Member is MethodInfo)) return false;

            if (Member.Name.Contains("set_"))
            {
                propertyName = Member.Name.Replace("set_", string.Empty);
                return true;
            }

            propertyName = string.Empty;
            return false;
        }
        public override string ToString()
        {
            string info = $"[{Member.MemberType}] ";
            info += Member.DeclaringType is null ? string.Empty : Member.DeclaringType.FullName + '.';
            info += Member is Type type ? type.FullName : Member.Name;
            return info;
        }
    }
}
