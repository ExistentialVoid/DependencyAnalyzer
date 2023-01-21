using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;

namespace DependencyAnalyzer
{
    public class Architecture
    {
        public readonly static BindingFlags Filter = BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        public TextWriter? InstructionLog { get; set; } = null;

        private readonly ImmutableList<MemberInfo> FlattenedMembers;
        internal readonly ReferenceCollection References = new();
        private readonly List<Type> Types;


        public Architecture(Type[] types)
        {
            Types = new(types);

            List<MemberInfo> members = new();
            foreach (Type t in types) members.AddRange(t.GetMembers(Filter));
            FlattenedMembers = members.ToImmutableList();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="types"></param>
        /// <param name="instructionlog">Record every instruction streams of each method in types collection</param>
        public Architecture(Type[] types, TextWriter instructionlog) : this(types)
        {
            InstructionLog = instructionlog;
        }


        /// <summary>
        /// Perform analysis
        /// </summary>
        /// <returns>Returns the results of the analysis</returns>
        public void Analyze()
        {
            MemberInterpreter interpreter = new(Types, InstructionLog);
            References.Clear();
            foreach (Type t in Types)
            {
                foreach (MemberInfo m in t.GetMembers(Filter))
                {
                    List<MemberInfo> referencedMembers = interpreter.GetReferencedMembers(m);
                    referencedMembers.ForEach(r => References.Add(new Reference(m, r, 1)));
                }
            }
        }
        public IList<IReference> Results(IReferenceFilter filter)
        {
            ReferenceCollection references = new(References);

            if (filter.SimplifyCompilerReferences)
            {
                foreach (Reference r in new List<Reference>(references))
                {
                    if (r.ReferencedMemberIsCompilerGenerated && !r.ReferencingMemberIsCompilerGenerated)
                    {
                        references.Remove(r);
                        ReferenceCollection relays = RelayReferencedCompilerReferences(r.ReferencedMember);
                        foreach (Reference relay in relays)
                        {
                            Reference quasiReference = new(r.ReferencingMember, relay.ReferencedMember, relay.Count);
                            references.Add(quasiReference);
                        }
                    }
                    else if (r.ReferencedMemberIsCompilerGenerated || r.ReferencingMemberIsCompilerGenerated) references.Remove(r);
                }
            }

            if (filter.SimplifyAccessors)
            {
                foreach (Reference r in new List<Reference>(references))
                {
                    if (r.ReferencedMember.Name.Length > 4 && r.ReferencedMember.Name[1..4].Equals("et_"))
                    {
                        PropertyInfo? property = (PropertyInfo)FlattenedMembers.Find(m =>
                            m.Name.Equals(r.ReferencedMember.Name[4..]) &&
                            (m.DeclaringType?.HasSameMetadataDefinitionAs(r.ReferencedMember.DeclaringType) ?? false));
                        if (property is not null)
                        {
                            Reference quasiReference = new(r.ReferencingMember, property, r.Count);
                            references.Add(quasiReference);
                            references.Remove(r);
                        }
                    }
                    if (r.ReferencingMember.Name.Length > 4 && r.ReferencingMember.Name[1..4].Equals("et_"))
                    {
                        PropertyInfo? property = (PropertyInfo)FlattenedMembers.Find(m =>
                            m.Name.Equals(r.ReferencingMember.Name[4..]) &&
                            (m.DeclaringType?.HasSameMetadataDefinitionAs(r.ReferencingMember.DeclaringType) ?? false));
                        if (property is not null)
                        {
                            Reference quasiReference = new(property, r.ReferencedMember, r.Count);
                            references.Add(quasiReference);
                            references.Remove(r);
                        }
                    }
                }
            }

            if (!filter.IncludeSiblingReferences)
            {
                foreach (Reference r in new List<Reference>(references))
                {
                    // compare parents
                    Type? p1 = r.ReferencedMember is TypeInfo T1 ? T1 : r.ReferencedMember.DeclaringType;
                    Type? p2 = r.ReferencingMember is TypeInfo T2 ? T2 : r.ReferencingMember.DeclaringType;
                    if (p1 is Type && p2 is Type && p1.HasSameMetadataDefinitionAs(p2)) 
                        references.Remove(r);
                }
            }

            if (!filter.IncludeTypeReferences)
            {
                foreach (Reference r in new List<Reference>(references))
                    if (r.ReferencedMember is TypeInfo || r.ReferencingMember is TypeInfo) references.Remove(r);
            }

            return new List<IReference>(references);
        }
        private ReferenceCollection RelayReferencedCompilerReferences(MemberInfo compilerReferencedMember)
        {
            if (compilerReferencedMember.MemberType == MemberTypes.Constructor) return new();

            ReferenceCollection newCollection = new();
            bool matchedReferenced(Reference r) => r.ReferencingMember.HasSameMetadataDefinitionAs(compilerReferencedMember);
            List<Reference> matchedReferences = References.FindAll(matchedReferenced);

            foreach (Reference r in matchedReferences)
            {
                if (r.ReferencedMemberIsCompilerGenerated)
                {
                    ReferenceCollection relays = RelayReferencedCompilerReferences(r.ReferencedMember);
                    foreach (var relay in relays) newCollection.Add(relay);
                }
                else newCollection.Add(r);
            }
            return newCollection;
        }
    }
}
