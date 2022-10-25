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
        private List<MemberReferenceInfo> FlattenedReferenceMembers
        {
            get
            {
                List<MemberReferenceInfo> members = new List<MemberReferenceInfo>();
                ReferenceTypes.ForEach(cri => members.AddRange(cri.Members.ToList()));
                return members;
            }
        }
        public static StringBuilder InstructionLog { get; } = new();
        internal int NamespaceCount => _referenceTypes.ConvertAll(c => c.Class.Namespace).Distinct().Count();
        public ReferenceBindingFlags ReferenceFilters { get; set; } = ReferenceBindingFlags.NonCompiler;
        internal List<ClassReferenceInfo> ReferenceTypes => _referenceTypes;

        private readonly List<ClassReferenceInfo> _referenceTypes = new();


        public Architecture(Type[] types)
        {
            // Flatten nested types for discoverability. Invisible to results
            List<Type> flattenedTypes = new List<Type>(types);
            types.ToList().ForEach(t => flattenedTypes.AddRange(t.GetNestedTypes()));
            foreach (Type t in flattenedTypes)
            {
                if (t.IsEnum) continue;

                ClassReferenceInfo cri = new(t);
                _referenceTypes.Add(cri);
            }
        }


        /// <summary>
        /// Perform analysis
        /// </summary>
        /// <returns>Returns the results of the analysis</returns>
        public void Analyze()
        {
            _referenceTypes.ForEach(t => t.FindReferencedMembers(_referenceTypes));
            _referenceTypes.ForEach(t => t.FindReferencingMembers(_referenceTypes));
        } 
        private string GetFormattedClass(ClassReferenceInfo cri, ReportFormat format)
        {
            if (cri == null) return string.Empty;

            StringBuilder builder = new();
            string indents = cri.Class.IsNested ? "\t" : string.Empty;
            Type t = cri.Class.DeclaringType;
            while (t is not null && t.IsNested)
            {
                indents += '\t';
                t = t.DeclaringType;
            }

            string header = format switch {
                ReportFormat.Basic => cri.Class.Name,
                ReportFormat.Detailed => cri.ToString(),
                ReportFormat.Signature => cri.Signature,
                _ => cri.Class.ToString() };
            builder.Append($"\n{indents}{header}");
            indents += '\t';

            ReferenceFilter filter = new(ReferenceFilters);
            List<MemberReferenceInfo> filteredMembers = filter.ApplyFilterTo(cri.Members.ToList()).ToList();
            foreach (MemberReferenceInfo mri in filteredMembers)
            {
                if (mri.Member is TypeInfo ti)
                {
                    builder.Append(GetFormattedClass(_referenceTypes.Find(rt => rt.Class.HasSameMetadataDefinitionAs(ti)), format));
                    continue;
                }

                header = format switch {
                    ReportFormat.Basic => mri.Member.Name,
                    ReportFormat.Detailed => mri.ToString(),
                    ReportFormat.Signature => mri.Signature,
                    _ => mri.Member.ToString() };
                builder.Append($"\n{indents}{header}");

                // Member's references
                Dictionary<MemberInfo, int> referencedMembers = new(mri.ReferencedMembers);
                List<MemberReferenceInfo> referencedReferenceMembers = FlattenedReferenceMembers.FindAll(fm => mri.ReferencedMembers.Keys.Contains(fm.Member));
                List<MemberReferenceInfo> filteredReferencedReferenceMembers = filter.ApplyFilterTo(referencedReferenceMembers).ToList();
                var filteredReferencedMembers = referencedMembers.ToList().FindAll(rm => filteredReferencedReferenceMembers.ConvertAll(rrmi => rrmi.Member).Contains(rm.Key));
                builder.Append(GetFormattedMembers(new(filteredReferencedMembers), true, indents, format));

                referencedMembers = new(mri.ReferencingMembers);
                referencedReferenceMembers = FlattenedReferenceMembers.FindAll(fm => mri.ReferencingMembers.Keys.Contains(fm.Member));
                filteredReferencedReferenceMembers = filter.ApplyFilterTo(referencedReferenceMembers).ToList();
                filteredReferencedMembers = referencedMembers.ToList().FindAll(rm => filteredReferencedReferenceMembers.ConvertAll(rrmi => rrmi.Member).Contains(rm.Key));
                builder.Append(GetFormattedMembers(new(filteredReferencedMembers), false, indents, format));
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
                    ReportFormat.Signature => FlattenedReferenceMembers.Find(frmi => frmi.Member.HasSameMetadataDefinitionAs(m)).Signature,
                    _ => $"[{m.MemberType}] {m}"
                };
                if (string.IsNullOrEmpty(info)) info = m.ToString();
                builder.Append($"\n{indents}    {$"({members[m]})",-5}{info}");
            }
            return builder.ToString();
        }
        public string GetFormattedResults(ReportFormat format)
        {
            StringBuilder builder = new();
            // 1 : newobj  [Void] StringBuilder::.ctor()
            // 6 : stloc.0
            // 8 : ldarg.0

            foreach (ClassReferenceInfo cri in _referenceTypes.FindAll(rt => !rt.Class.IsNested))
                // 9 : call  List`1 Architecture::get_ReferenceTypes()
                // 14 : ldsfld [Predicate`1] <>c::<>9__10_0
                // 19 : dup
                // {
                //     20 : brtrue.s 45
                //     22 : pop
                //     45 : callvirt  List`1 List`1::FindAll()
                //     50 : callvirt  Enumerator List`1::GetEnumerator()
                //     55 : stloc.2

                builder.Append(GetFormattedClass(cri, format));
            // 28 : ldftn  Boolean <>c::<GetFormattedResults>b__10_0()
            // 34 : newobj  [Void] Predicate`1::.ctor()
            // 39 : dup
            // 40 : stsfld [Predicate`1] <>c::<>9__10_0

            // 56 : br.s 81
            // 58 : ldloca.s 2
            // 60 : call  ClassReferenceInfo Enumerator::get_Current()
            // 65 : stloc.3
            // 66 : ldloc.0
            // 67 : ldarg.0
            // 68 : ldloc.3
            // 69 : ldarg.1
            // 70 : call  String Architecture::GetFormattedClass()
            // 75 : callvirt  StringBuilder StringBuilder::Append()
            // 80 : pop
            // 81 : ldloca.s 2
            // 83 : call  Boolean Enumerator::MoveNext()
            // 88 : brtrue.s 58
            // 90 : leave.s 107
            // 92 : ldloca.s 2
            // 94 : constrained.
            // 100 : callvirt  Void IDisposable::Dispose()
            // 105 : nop
            // 106 : endfinally
            // 107 : ldloc.0
            // 108 : callvirt  String Object::ToString()
            // 113 : stloc.1
            // 114 : ldarg.0
            // 115 : call  Int32 Architecture::get_NamespaceCount()
            // 120 : ldc.i4.1
            // 121 : ceq
            // 123 : stloc.s 4
            // 125 : ldloc.s 4
            // 127 : brfalse.s -83
            // 129 : ldloc.1
            // 130 : ldarg.0
            // 131 : call  List`1 Architecture::get_ReferenceTypes()
            // 136 : ldc.i4.0
            // 137 : callvirt  ClassReferenceInfo List`1::get_Item()
            // 142 : callvirt  Type ClassReferenceInfo::get_Class()
            // 147 : callvirt  String Type::get_Namespace()
            // 152 : ldstr " . "
            // 157 : call  static String String::Concat()
            // 162 : ldsfld [String] String::Empty
            // 167 : callvirt  String String::Replace()
            // 172 : stloc.1
            // 173 : ldloc.1
            // 174 : stloc.s 5
            // 176 : br.s -78
            string results = builder.ToString();
            if (NamespaceCount == 1) results = results.Replace($"{_referenceTypes[0].Class.Namespace}.", string.Empty);
            return results;
            // 178 : ldloc.s 5
            // 180 : ret
        }

        public enum ReportFormat { Default, Basic, Detailed, Signature }
    }
}
