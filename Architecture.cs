using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DependencyAnalyzer
{
    public class Architecture
    {
        public static BindingFlags Filter { get; } = BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        internal List<MemberReferenceInfo> FlattenedReferenceMembers
        {
            get
            {
                List<MemberReferenceInfo> members = new();
                ReferenceTypes.ForEach(cri => members.AddRange(cri.FlattenedReferenceMembers));
                return members;
            }
        }
        internal List<ClassReferenceInfo> FlattenedReferenceTypes
        {
            get
            {
                List<ClassReferenceInfo> classes = new(ReferenceTypes);
                ReferenceTypes.ForEach(c => classes.AddRange(c.FlattenedReferenceTypes));
                return classes;
            }
        }
        public static StringBuilder InstructionLog { get; } = new();
        public ReferenceBindingFlags ReferenceFilters { get; set; } = ReferenceBindingFlags.NonCompiler;
        private List<ClassReferenceInfo> ReferenceTypes { get; } = new();


        public Architecture(Type[] types)
        {
            foreach (Type t in types)
            {
                if (t.IsEnum) continue;
                ClassReferenceInfo cri = new(t as TypeInfo);
                ReferenceTypes.Add(cri);
            }
        }


        /// <summary>
        /// Perform analysis
        /// </summary>
        /// <returns>Returns the results of the analysis</returns>
        public void Analyze()
        {
            FlattenedReferenceTypes.ForEach(t => t.FindReferencedMembers(FlattenedReferenceTypes));
            FlattenedReferenceTypes.ForEach(t => t.FindReferencingMembers(FlattenedReferenceTypes));

            ApplyReferenceModifications();
        }
        /// <summary>
        /// Handle unique manipulations to references
        /// </summary>
        private void ApplyReferenceModifications()
        {
            foreach (var type in FlattenedReferenceTypes)
            {
                // Flatten underlying members' references
                foreach (var member in type.FlattenedReferenceMembers)
                {
                    if (member.IsAnonomous(out string declaredMethodName))
                    {
                        ClassReferenceInfo? cri = type;
                        MemberReferenceInfo? declaredMethod = null;
                        // when type is nested
                        while (declaredMethod is null && cri is not null)
                        {
                            declaredMethod = cri.FlattenedReferenceMembers
                                .ToList()
                                .Find(m => m.Host.Name.Equals(declaredMethodName));
                            cri = FlattenedReferenceTypes
                                .ToList()
                                .Find(c => cri.Host.DeclaringType != null && c.Host.HasSameMetadataDefinitionAs(cri.Host.DeclaringType));
                        }

                        foreach (var referencedMember in member.ReferencedMembers)
                        {
                            if (referencedMember.Key is not MethodInfo) continue;
                            declaredMethod?.AddReferencedMember(referencedMember);
                        }
                    }
                    else if (member.IsGetter(out string propertyName) || member.IsSetter(out propertyName))
                    {
                        MemberReferenceInfo? property = type.Members.ToList().Find(m => m.Host.Name.Equals(propertyName)) as MemberReferenceInfo;
                        if (property is not null)
                        {
                            foreach (var referencedMember in member.ReferencedMembers)
                                property.AddReferencedMember(referencedMember);
                            foreach (var referencingMember in member.ReferencingMembers)
                                property.AddReferencingMember(referencingMember);
                        }
                    }
                }
            }
        }
        private string GetFormattedClass(ClassReferenceInfo? cri, ReportFormat format)
        {
            if (cri is null) return string.Empty;

            StringBuilder builder = new();
            string indents = cri.IsNested ? "\t" : string.Empty;
            Type? t = cri.Host.DeclaringType;
            while (t is not null && t.IsNested)
            {
                indents += '\t';
                t = t.DeclaringType;
            }

            string header = format switch {
                ReportFormat.Basic => cri.Host.Name,
                ReportFormat.Detailed => cri.ToString(),
                ReportFormat.Signature => cri.Signature,
                _ => cri.Host.ToString() };
            builder.Append($"\n{indents}{header}");
            indents += '\t';

            ReferenceFilter filter = new(ReferenceFilters);
            List<MemberReferenceInfo> filteredMembers = filter.ApplyFilterTo(cri.FlattenedReferenceMembers.ToList()).ToList();
            foreach (MemberReferenceInfo mri in filteredMembers)
            {
                if (mri.Host is TypeInfo ti)
                {
                    builder.Append(GetFormattedClass(FlattenedReferenceTypes.Find(rt => rt.Host.HasSameMetadataDefinitionAs(ti)), format));
                    continue;
                }

                header = format switch {
                    ReportFormat.Basic => mri.Host.Name,
                    ReportFormat.Detailed => mri.ToString() ?? string.Empty,
                    ReportFormat.Signature => mri.Signature,
                    _ => mri.Host.ToString() ?? string.Empty };
                // Hold off posting this member until filtering can be fully considered on reference members
                string pendingMemberInfo = $"\n{indents}{header}";

                // Member's references
                Dictionary<MemberInfo, int> referencedMembers = new(mri.ReferencedMembers);
                List<MemberReferenceInfo> referencedReferenceMembers = FlattenedReferenceMembers.FindAll(fm => mri.ReferencedMembers.Keys.Contains(fm.Host));
                List<MemberReferenceInfo> filteredReferencedReferenceMembers = filter.ApplyFilterTo(referencedReferenceMembers).ToList();
                var filteredReferencedMembers = referencedMembers.ToList().FindAll(rm => filteredReferencedReferenceMembers.ConvertAll(rrmi => rrmi.Host).Contains(rm.Key));
                if (!ReferenceFilters.HasFlag(ReferenceBindingFlags.WithReferences) || filteredReferencedMembers.Count > 0)
                {
                    builder.Append(pendingMemberInfo);
                    pendingMemberInfo = string.Empty;
                    builder.Append(GetFormattedMembers(new(filteredReferencedMembers), true, indents, format));
                }

                referencedMembers = new(mri.ReferencingMembers);
                referencedReferenceMembers = FlattenedReferenceMembers.FindAll(fm => mri.ReferencingMembers.Keys.Contains(fm.Host));
                filteredReferencedReferenceMembers = filter.ApplyFilterTo(referencedReferenceMembers).ToList();
                filteredReferencedMembers = referencedMembers.ToList().FindAll(rm => filteredReferencedReferenceMembers.ConvertAll(rrmi => rrmi.Host).Contains(rm.Key));
                if (!ReferenceFilters.HasFlag(ReferenceBindingFlags.WithReferences) || filteredReferencedMembers.Count > 0)
                {
                    if (!pendingMemberInfo.Equals(string.Empty)) builder.Append(pendingMemberInfo);
                    builder.Append(GetFormattedMembers(new(filteredReferencedMembers), false, indents, format));
                }
            }
            return builder.ToString();
        }
        private string GetFormattedMembers(Dictionary<MemberInfo, int> members, bool isReferencing, string indents, ReportFormat format)
        {
            if (members.Count == 0) return string.Empty;

            StringBuilder builder = new();
            string section = isReferencing ? "References" : "Referenced by";
            builder.Append($"\n{indents}    {section}: ");
            foreach (MemberInfo m in members.Keys)
            {
                string info = format switch
                {
                    ReportFormat.Basic => $"[{m.MemberType}] {m.DeclaringType?.FullName ?? string.Empty}::{(m is TypeInfo rt ? rt.Name : m.Name)}",
                    ReportFormat.Detailed => $"[{m.MemberType}] {m.DeclaringType?.FullName ?? string.Empty}::{(m is TypeInfo rt ? rt.FullName : m.Name)}",
                    ReportFormat.Signature => FlattenedReferenceMembers.Find(frmi => frmi.Host.HasSameMetadataDefinitionAs(m))?.Signature ?? string.Empty,
                    _ => $"[{m.MemberType}] {m}"
                };
                if (string.IsNullOrEmpty(info)) info = m.ToString() ?? string.Empty;
                builder.Append($"\n{indents}    {$"({members[m]})",-5}{info}");
            }
            return builder.ToString();
        }
        public string GetFormattedResults(ReportFormat format)
        {
            StringBuilder builder = new();

            bool shortFormat = true;
            if (shortFormat)
            {
                ReferenceTypes.ForEach(c => builder.Append(c.GetSimpleFormat(string.Empty)));
                return builder.ToString();
            }

            foreach (ClassReferenceInfo cri in ReferenceTypes) builder.Append(GetFormattedClass(cri, format));
            string results = builder.ToString();

            int namespaceCount = ReferenceTypes.ConvertAll(c => c.Namespace).Distinct().Count();
            if (namespaceCount == 1) results = results.Replace($"{ReferenceTypes[0].Namespace}.", string.Empty);

            return results;
        }

        public enum ReportFormat { Default, Basic, Detailed, Signature }
    }
}
