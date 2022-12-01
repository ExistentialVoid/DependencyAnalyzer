using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DependencyAnalyzer
{
    public class Architecture
    {
        public readonly static BindingFlags Filter = BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        internal List<MemberReferenceInfo> FlattenedReferenceMembers { get; }
        internal List<TypeReferenceInfo> FlattenedReferenceTypes { get; }
        public TextWriter InstructionLog { get; set; }
        /// <summary>
        /// Modifiable object for alterning report results
        /// </summary>
        public ReferenceFilter ReportFilter { get; } = new();
        public ReportFormat ReportFormat { get; set; } = ReportFormat.Default;

        private readonly List<TypeReferenceInfo> ReferenceTypes;


        public Architecture(Type[] types)
        {
            ReferenceTypes = new();
            FlattenedReferenceTypes = new();
            FlattenedReferenceMembers = new();
            foreach (Type t in types)
            {
                if (t.IsEnum || t.IsNested) continue;
                TypeReferenceInfo cri = new(this, t);
                ReferenceTypes.Add(cri);
                FlattenedReferenceTypes.AddRange(cri.FlattenedTypes);
                FlattenedReferenceMembers.AddRange(cri.FlattenedMembers);
            }
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
            FlattenedReferenceTypes.ForEach(t => t.FindReferencedMembers());
            FlattenedReferenceTypes.ForEach(t => t.FindReferencingMembers());
        }
        public void RecordFormattedResults(TextWriter writer)
        {
            if (ReportFilter.ExcludeNamespace is null)
            {
                int namespaceCount = ReferenceTypes.ConvertAll(c => c.Namespace).Distinct().Count();
                ReportFilter.ExcludeNamespace = namespaceCount == 1;
            }

            ReferenceTypes.ForEach(t => writer.WriteLine(t.ToFormattedString("\n")));
        }

    }
    public enum ReportFormat { Default, Basic, Detailed, Signature, Short }
}
