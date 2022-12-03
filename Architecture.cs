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
                    referencedMembers.ForEach(r => References.Include(r, m));
                }
            }
        }
        public IList<IReference> Results() => new List<IReference>(References);
        public IList<IReference> Results(IReferenceFilter filter)
        {
            ReferenceCollection references = new();
            foreach (Reference r in References)
            {
                if (!filter.IncludeSiblingReferences)
                {
                    Type? parent1 = r.ReferencedMember is TypeInfo T1 ? T1 : r.ReferencedMember.DeclaringType;
                    Type? parent2 = r.ReferencingMember is TypeInfo T2 ? T2 : r.ReferencingMember.DeclaringType;
                    if (parent1 is Type && parent2 is Type && parent1.HasSameMetadataDefinitionAs(parent2))
                        continue;
                }
                
                if (!filter.IncludeTypeReferences && (r.ReferencedMember is TypeInfo || r.ReferencingMember is TypeInfo))
                {
                    continue;
                }

                if (filter.SimplifyAccessors)
                {
                    if (r.ReferencedMember.Name[1..4].Equals("et_"))
                    {
                        PropertyInfo? property = (PropertyInfo)FlattenedMembers.Find(m =>
                            m.Name.Equals(r.ReferencedMember.Name[4..]) && 
                            (m.DeclaringType?.HasSameMetadataDefinitionAs(r.ReferencedMember.DeclaringType) ?? false));
                        if (property is null) continue;
                        r.ReferencedMember = property;
                    }
                    if (r.ReferencingMember.Name[1..4].Equals("et_"))
                    {
                        PropertyInfo? property = (PropertyInfo)FlattenedMembers.Find(m =>
                            m.Name.Equals(r.ReferencingMember.Name[4..]) &&
                            (m.DeclaringType?.HasSameMetadataDefinitionAs(r.ReferencingMember.DeclaringType) ?? false));
                        if (property is null) continue;
                        r.ReferencingMember = property;
                    }
                }

                if (filter.SimplifyCompilerReferences)
                {
                    MemberInfo referenced = r.ReferencedMember;
                    string referencedFullname = referenced is TypeInfo T1 ? (T1.FullName ?? T1.Name)
                        : $"{referenced.DeclaringType?.FullName}.{referenced.Name}";
                    MemberInfo referencing = r.ReferencingMember;
                    string referencingFullname = referencing is TypeInfo T2 ? (T2.FullName ?? T2.Name)
                        : $"{referencing.DeclaringType?.FullName}.{referencing.Name}";

                    if (referencedFullname.Contains("<") && !referencingFullname.Contains("<"))
                    {
                        bool matchedReferenced(Reference reference) => reference.ReferencingMember.HasSameMetadataDefinitionAs(r.ReferencingMember);
                        ReferenceCollection relays = RelayReferencedCompilerReferences(References.FindAll(matchedReferenced));
                        foreach (var relay in relays)
                            references.Include(relay.ReferencedMember, relay.ReferencingMember, relay.Count);
                        continue;
                    }
                    else if (referencingFullname.Contains("<") && !referencedFullname.Contains("<"))
                    {
                        bool matchedReferenced(Reference reference) => reference.ReferencedMember.HasSameMetadataDefinitionAs(r.ReferencedMember);
                        ReferenceCollection relays = RelayReferencedCompilerReferences(References.FindAll(matchedReferenced));
                        foreach (var relay in relays)
                            references.Include(relay.ReferencedMember, relay.ReferencingMember, relay.Count);
                        continue;
                    }
                }

                references.Include(r.ReferencedMember, r.ReferencingMember, r.Count);
            }
            return new List<IReference>(references);
        }
        private ReferenceCollection RelayReferencedCompilerReferences(List<Reference> collection)
        {
            ReferenceCollection newCollection = new();
            foreach (var reference in collection)
            {
                MemberInfo referenced = reference.ReferencedMember;
                string fullname = referenced is TypeInfo T ? (T.FullName ?? T.Name)
                    : $"{referenced.DeclaringType?.FullName}.{referenced.Name}";
                if (fullname.Contains("<"))
                {
                    bool matchedReferenced(Reference r) => r.ReferencingMember.HasSameMetadataDefinitionAs(reference.ReferencingMember);
                    ReferenceCollection relays = RelayReferencedCompilerReferences(References.FindAll(matchedReferenced));
                    foreach (var r in relays)
                        newCollection.Include(r.ReferencedMember, r.ReferencingMember, r.Count);
                }
            }
            return newCollection;
        }
        private ReferenceCollection RelayReferencingCompilerReferences(List<Reference> collection)
        {
            ReferenceCollection newCollection = new();
            foreach (var reference in collection)
            {
                MemberInfo referencing = reference.ReferencingMember;
                string fullname = referencing is TypeInfo T ? (T.FullName ?? T.Name)
                    : $"{referencing.DeclaringType?.FullName}.{referencing.Name}";
                if (fullname.Contains("<"))
                {
                    bool matchedReferencing(Reference r) => r.ReferencedMember.HasSameMetadataDefinitionAs(reference.ReferencedMember);
                    ReferenceCollection relays = RelayReferencingCompilerReferences(References.FindAll(matchedReferencing));
                    foreach (var r in relays)
                        newCollection.Include(r.ReferencedMember, r.ReferencingMember, r.Count);
                }
            }
            return newCollection;
        }
    }
}
