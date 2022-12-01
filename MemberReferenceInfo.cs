using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DependencyAnalyzer
{
    /// <summary>
    /// Couples a member with other members the it references and that reference it
    /// </summary>
    internal sealed class MemberReferenceInfo : ReferenceInfo
    {
        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="info">Member to be copied</param>
        internal MemberReferenceInfo(MemberReferenceInfo info) : base(info) { }
        public MemberReferenceInfo(MemberInfo member, TypeReferenceInfo parent) : base(member, parent) { }


        internal override void FindReferencedMembers()
        {
            ReferencedMembers.Clear();

            List<TypeReferenceInfo> referenceTypes = Parent.Architecture.FlattenedReferenceTypes;
            MemberInterpreter interpreter = new(referenceTypes);

            List<ReferenceInfo> refMembers = interpreter.GetReferencedMembers(Host);
            refMembers.ForEach(rm => ReferencedMembers.Add(rm));
        }
        internal override void FindReferencingMembers()
        {
            ReferencingMembers.Clear();
            foreach (MemberReferenceInfo mri in Parent.Architecture.FlattenedReferenceMembers)
            {
                foreach (var r in mri.ReferencedMembers)
                {
                    if (r.Key.Equals(this)) ReferencingMembers.Add(mri, r.Value);
                }
            }
        }
        /// <summary>
        /// Check if member is the get or set method of a property.
        /// </summary>
        /// <param name="property"></param>
        /// <returns>True if name contains "et_" and is not compiler generated, otherwise false.</returns>
        internal bool IsAccessor(out MemberReferenceInfo property)
        {
            property = null;
            if (!(Host is MethodInfo)) return false;

            string name = Host.Name;
            if (name.Length > 4 && name[1..4].Equals("et_"))
            {
                string propertyName = name[4..];
                property = Parent.GetMemberBy(propertyName);
                return true;
            }

            return false;
        }
        public override string ToFormattedString(string spacing)
        {
            ReferenceFilter filter = Parent.Architecture.ReportFilter;

            if (filter.SimplifyCompilerReferences && IsCompilerGenerated) return string.Empty;

            ReferenceCollection filteredReferencedMembers = new(ReferencedMembers);
            ReferenceCollection filteredReferencingMembers = new(ReferencingMembers);

            if (filter.SimplifyCompilerReferences)
            {
                filteredReferencedMembers = filter.RelayReferencedCompilerReferences(filteredReferencedMembers);
                filteredReferencingMembers = filter.RelayReferencingCompilerReferences(filteredReferencingMembers);
            }
            if (filter.SimplifyAccessors)
            {
                if (IsAccessor(out _)) return string.Empty;
                if (Host is PropertyInfo)
                {
                    filter.RelayAccessorsReferencedMembers(this, filteredReferencedMembers);
                    filter.RelayAccessorsReferencingMembers(this, filteredReferencingMembers);
                }
                filter.ReplaceAccessors(filteredReferencedMembers);
                filter.ReplaceAccessors(filteredReferencingMembers);
            }
            if (!filter.IncludeSiblingReferences)
            {
                filter.RemoveSiblingReferences(Parent, filteredReferencedMembers);
                filter.RemoveSiblingReferences(Parent, filteredReferencingMembers);
            }
            if (!filter.IncludeTypeReferences)
            {
                filter.RemoveTypeReferences(filteredReferencedMembers);
                filter.RemoveTypeReferences(filteredReferencingMembers);
            }

            if (filter.ExistingReferencesCondition == Condition.With &&
                !filteredReferencedMembers.Any() && !filteredReferencingMembers.Any()) return string.Empty;
            else if (filter.ExistingReferencesCondition == Condition.Without &&
                (filteredReferencedMembers.Any() || filteredReferencingMembers.Any())) return string.Empty;


            ReportFormat format = Parent.Architecture.ReportFormat;
            StringBuilder builder = new();

            builder.Append($"{spacing}{ToString(format)}");
            spacing += '\t';
            if (filteredReferencedMembers.Any())
            {
                builder.Append($"{spacing}References:");
                filteredReferencedMembers.ToList().ForEach(r =>
                    builder.Append($"{spacing}{$"({r.Value})",-5}{r.Key.ToString(format)}"));
            }
            if (filteredReferencingMembers.Any())
            {
                builder.Append($"{spacing}Referenced by:");
                filteredReferencingMembers.ToList().ForEach(r =>
                    builder.Append($"{spacing}{$"({r.Value})",-5}{r.Key.ToString(format)}"));
            }

            string info = builder.ToString();
            if (filter.ExcludeNamespace ?? false) info = info.Replace($"{Parent.Namespace}.", string.Empty);

            return info;
        }
    }
}
