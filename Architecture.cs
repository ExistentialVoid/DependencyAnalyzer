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
            ReferenceCollection references = new(References);

            if (!filter.IncludeSiblingReferences)
            {
                foreach (Reference r in new List<Reference>(references))
                {
                    // compare parents
                    Type? p1 = r.ReferencedMember is TypeInfo T1 ? T1 : r.ReferencedMember.DeclaringType;
                    Type? p2 = r.ReferencingMember is TypeInfo T2 ? T2 : r.ReferencingMember.DeclaringType;
                    if (p1 is Type && p2 is Type && p1.HasSameMetadataDefinitionAs(p2)) 
                        references.Exclude(r);
                }
            }

            if (!filter.IncludeTypeReferences)
            {
                foreach (Reference r in new List<Reference>(references))
                    if (r.ReferencedMember is TypeInfo || r.ReferencingMember is TypeInfo) 
                        references.Exclude(r);
            }

            if (filter.SimplifyCompilerReferences)
            {
                foreach (Reference r in new List<Reference>(references))
                {
                    string referencedFullname = r.GetReferencedMemberFullName();
                    string referencingFullname = r.GetReferencingMemberFullName();
                    if (referencedFullname.Contains('<') && !referencingFullname.Contains('<'))
                    {
                        references.Exclude(r);

                        bool matchedReferenced(Reference reference) =>
                            reference.ReferencingMember.HasSameMetadataDefinitionAs(r.ReferencedMember);
                        List<Reference> matchedRefernces = References.FindAll(matchedReferenced);
                        ReferenceCollection relays = RelayReferencedCompilerReferences(matchedRefernces);

                        foreach (var relay in relays) references.Include(relay);
                    }
                }
                // Remove compiler references
                foreach (Reference r in new List<Reference>(references))
                {
                    string referencedFullname = r.GetReferencedMemberFullName();
                    string referencingFullname = r.GetReferencingMemberFullName();
                    if (referencedFullname.Contains('<') || referencingFullname.Contains('<'))
                        references.Exclude(r.ReferencedMember, r.ReferencingMember, 0);
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
                            references.Include(property, r.ReferencingMember, r.Count);
                            references.Exclude(r);
                        }
                    }
                    if (r.ReferencingMember.Name.Length > 4 && r.ReferencingMember.Name[1..4].Equals("et_"))
                    {
                        PropertyInfo? property = (PropertyInfo)FlattenedMembers.Find(m =>
                            m.Name.Equals(r.ReferencingMember.Name[4..]) &&
                            (m.DeclaringType?.HasSameMetadataDefinitionAs(r.ReferencingMember.DeclaringType) ?? false));
                        if (property is not null)
                        {
                            references.Include(r.ReferencedMember, property, r.Count);
                            references.Exclude(r);
                        }
                    }
                }
            }

            return new List<IReference>(references);
        }
        private ReferenceCollection RelayReferencedCompilerReferences(List<Reference> collection)
        {
            ReferenceCollection newCollection = new();
            foreach (var r in collection)
            {
                if (r.GetReferencedMemberFullName().Contains('<'))
                {
                    bool matchedReferenced(Reference reference) =>
                        reference.ReferencingMember.HasSameMetadataDefinitionAs(r.ReferencedMember);
                    List<Reference> matchedRefernces = References.FindAll(matchedReferenced);
                    ReferenceCollection relays = RelayReferencedCompilerReferences(matchedRefernces);

                    foreach (var relay in relays) newCollection.Include(relay);
                }
                else newCollection.Include(r);
            }
            return newCollection;
        }
    }
}
