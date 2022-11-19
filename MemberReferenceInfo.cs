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
            else if (!(member is TypeInfo type && (Host.DeclaringType?.HasSameMetadataDefinitionAs(type) ?? false)) &&
                (!member.DeclaringType?.HasSameMetadataDefinitionAs(Host.DeclaringType) ?? true)) _referencedMembers.Add(member, count);
        }
        internal void AddReferencedMember(KeyValuePair<MemberInfo, int> refMember) => AddReferencedMember(refMember.Key, refMember.Value);
        internal void AddReferencingMember(MemberInfo member, int count = 1)
        {
            MemberInfo existingRef = _referencingMembers
                                        .ToList()
                                        .Find(m => m.Key.HasSameMetadataDefinitionAs(member)).Key;

            if (existingRef != null) _referencingMembers[existingRef] += count;
            else if (!(member is TypeInfo type && (Host.DeclaringType?.HasSameMetadataDefinitionAs(type) ?? false)) &&
                (!member.DeclaringType?.HasSameMetadataDefinitionAs(Host.DeclaringType) ?? true)) _referencingMembers.Add(member, count);
        }
        internal void AddReferencingMember(KeyValuePair<MemberInfo, int> refMember) => AddReferencingMember(refMember.Key, refMember.Value);
        internal override void FindReferencedMembers(IEnumerable<ClassReferenceInfo> referenceTypes)
        {
            MemberInterpreter interpreter = new(referenceTypes.ToList().ConvertAll(c => c.Host as Type));
            List<MemberInfo> referencedMembers = interpreter.GetReferencedMembers(Host);
            
            List<ClassReferenceInfo> closures = referenceTypes.ToList().FindAll(c => c.IsClosure);
            foreach (var member in referencedMembers)
            {
                if (member.DeclaringType is Type t && closures.Exists(c => c.Host.HasSameMetadataDefinitionAs(t)))
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
                cri.FlattenedReferenceMembers
                    .FindAll(m => m.Host.HasSameMetadataDefinitionAs(Host))
                    .ForEach(m => AddReferencingMember(m.Host));
            }
        }
        /// <summary>
        /// Check if member is a compiler generated method.
        /// </summary>
        /// <param name="methodName"></param>
        /// <returns>True if name contains ">b__", otherwise false.</returns>
        internal bool IsAnonomous(out string methodName)
        {
            if (Host.Name.Contains(">b__"))
            {
                methodName = Host.Name.Split('>')[0].TrimStart('<');
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
            if (Host.Name.Contains(">k__BackingField"))
            {
                propertyName = Host.Name.Split('>')[0].TrimStart('<');
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
            if (!(Host is MethodInfo)) return false;

            if (Host.Name.Contains("get_"))
            {
                propertyName = Host.Name.Replace("get_", string.Empty);
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
            if (!(Host is MethodInfo)) return false;

            if (Host.Name.Contains("set_"))
            {
                propertyName = Host.Name.Replace("set_", string.Empty);
                return true;
            }

            propertyName = string.Empty;
            return false;
        }
        public override string ToString()
        {
            string info = $"[{Host.MemberType}] ";
            info += Host.DeclaringType is null ? string.Empty : Host.DeclaringType.FullName + '.';
            info += Host is Type type ? type.FullName : Host.Name;
            return info;
        }
    }
}
