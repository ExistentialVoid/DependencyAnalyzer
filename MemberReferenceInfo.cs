using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DependencyAnalyzer
{
    /// <summary>
    /// Couples a member with other members the it references and that reference it
    /// </summary>
    internal sealed class MemberReferenceInfo : ReferenceInfo
    {
        internal Dictionary<MemberInfo, int> ReferencedMembers { get; private set; } = new();
        internal Dictionary<MemberInfo, int> ReferencingMembers { get; private set; } = new();


        public MemberReferenceInfo(MemberInfo member) : base(member) { }


        internal void AddReferencedMember(MemberInfo member, int count = 1)
        {
            MemberInfo existingRef = ReferencedMembers
                                        .ToList()
                                        .Find(m => m.Key.HasSameMetadataDefinitionAs(member))
                                        .Key;

            if (existingRef != null) ReferencedMembers[existingRef] += count;
            else if (!(member is TypeInfo type && (Host.DeclaringType?.HasSameMetadataDefinitionAs(type) ?? false)) &&
                (!member.DeclaringType?.HasSameMetadataDefinitionAs(Host.DeclaringType) ?? true)) ReferencedMembers.Add(member, count);
        }
        internal void AddReferencedMember(KeyValuePair<MemberInfo, int> refMember) => AddReferencedMember(refMember.Key, refMember.Value);
        internal void AddReferencingMember(MemberInfo member, int count = 1)
        {
            MemberInfo existingRef = ReferencingMembers
                                        .ToList()
                                        .Find(m => m.Key.HasSameMetadataDefinitionAs(member)).Key;

            if (existingRef != null) ReferencingMembers[existingRef] += count;
            else if (!(member is TypeInfo type && (Host.DeclaringType?.HasSameMetadataDefinitionAs(type) ?? false)) &&
                (!member.DeclaringType?.HasSameMetadataDefinitionAs(Host.DeclaringType) ?? true)) ReferencingMembers.Add(member, count);
        }
        internal void AddReferencingMember(KeyValuePair<MemberInfo, int> refMember) => AddReferencingMember(refMember.Key, refMember.Value);
        internal override void FindReferencedMembers(List<ClassReferenceInfo> referenceTypes)
        {
            ReferencedMembers = new();
            MemberInterpreter interpreter = new(referenceTypes.ConvertAll(c => c.Host as Type));
            List<MemberInfo> refMembers = interpreter.GetReferencedMembers(Host);
            
            foreach (MemberInfo member in refMembers) AddReferencedMember(member);
        }
        internal override void FindReferencingMembers(List<ClassReferenceInfo> referenceTypes)
        {
            ReferencingMembers = new();
            foreach (ClassReferenceInfo cri in referenceTypes)
            {
                cri.FlattenedMembers
                    .FindAll(m => m.ReferencedMembers.Keys.ToList().Exists(rm => rm.HasSameMetadataDefinitionAs(Host)))
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
        /// <summary>
        /// Relay all members in ReferencedMembers who reference a compiled generated type's method or a complied generated method
        /// </summary>
        /// <param name="referenceMembers">All relevant method information</param>
        internal void TransferClosureReferences(List<MemberReferenceInfo> referenceMembers)
        {
            List<MemberReferenceInfo> generated = new();
            foreach (MemberInfo mi in ReferencedMembers.Keys)
            {
                MemberReferenceInfo mri = new(mi);
                if (mri.IsCompilerGenerated)
                {
                    mri = referenceMembers.Find(rm => rm.Equals(mri));
                    generated.Add(mri);
                }
            }

            if (generated.Count > 0)
                generated.ForEach(mri => mri.TransferClosureReferences(referenceMembers));

            foreach (MemberReferenceInfo mri in generated)
                mri.ReferencedMembers.ToList().ForEach(rm => AddReferencedMember(rm));
        }
    }
}
