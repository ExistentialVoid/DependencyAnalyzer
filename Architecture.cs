using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DependencyAnalyzer
{
    public class Architecture
    {
        public readonly static BindingFlags Filter = BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        internal List<MemberReferenceInfo> FlattenedReferenceMembers { get; }
        internal List<ClassReferenceInfo> FlattenedReferenceTypes { get; }
        public static StringBuilder InstructionLog { get; } = new();
        public ReferenceBindingFlags ReferenceFilters { get; set; } = ReferenceBindingFlags.NonCompiler;
        private List<ClassReferenceInfo> ReferenceTypes { get; }


        public Architecture(Type[] types)
        {
            ReferenceTypes = new();
            FlattenedReferenceTypes = new();
            FlattenedReferenceMembers = new();
            foreach (Type t in types)
            {
                if (t.IsEnum || t.IsNested) continue;
                ClassReferenceInfo cri = new(t);
                ReferenceTypes.Add(cri);
                FlattenedReferenceTypes.AddRange(cri.FlattenedTypes);
                FlattenedReferenceMembers.AddRange(cri.FlattenedMembers);
            }
        }


        /// <summary>
        /// Perform analysis
        /// </summary>
        /// <returns>Returns the results of the analysis</returns>
        public void Analyze()
        {
            // Collect all references by reading instruction set of each member
            FlattenedReferenceTypes.ForEach(t => t.FindReferencedMembers(FlattenedReferenceTypes));


            // It is critical to adjust referencing relays between FindReferencedTypes and FindReferencingTypes
            List<MemberReferenceInfo> accessorReferencers = FlattenedReferenceMembers
                .FindAll(mri => mri.ReferencedMembers.Keys.ToList().Exists(rm => rm.Name.Contains("et_")));
            foreach (var mri in accessorReferencers)
            {
                // replace MethodInfo (get_/set_) with the PropertyInfo
                List<KeyValuePair<MemberInfo, int>> accessorReferences = 
                    mri.ReferencedMembers.ToList().FindAll(r => r.Key.Name.Contains("et_"));
                foreach (var r in accessorReferences)
                {
                    MemberReferenceInfo dummyMri = new(r.Key);
                    _ = dummyMri.IsGetter(out string propertyName) || dummyMri.IsSetter(out propertyName);
                    ClassReferenceInfo propertyClass = FlattenedReferenceTypes.Find(c => c.Host.HasSameMetadataDefinitionAs(r.Key.DeclaringType));
                    MemberReferenceInfo property = propertyClass.Members.Find(m => m.Host.Name.Equals(propertyName));
                    mri.ReferencedMembers.Remove(r.Key);
                    mri.AddReferencedMember(property.Host, r.Value);
                }
            }

            //FlattenedReferenceMembers.ForEach(mri => mri.ReplaceAccessorReferences(FlattenedReferenceTypes, FlattenedReferenceMembers));

            List<MemberReferenceInfo> nonCompilerMembers = FlattenedReferenceMembers.FindAll(mri => !mri.IsCompilerGenerated);
            nonCompilerMembers.ForEach(mri => mri.ReplaceClosureReferences(FlattenedReferenceMembers));

            FlattenedReferenceMembers.ForEach(mri => mri.PendingReferenceRemovals.ForEach(rem => mri.ReferencedMembers.Remove(rem)));


            // Coordinate reversal of found references to record referencing (no need to re-read metadata)
            FlattenedReferenceTypes.ForEach(t => t.FindReferencingMembers(FlattenedReferenceTypes));
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
            List<MemberReferenceInfo> filteredMembers = filter.ApplyFilterTo(cri.FlattenedMembers.ToList()).ToList();
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

            if (format == ReportFormat.Short)
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

        public enum ReportFormat { Default, Basic, Detailed, Signature, Short }
    }
}
