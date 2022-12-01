﻿using System.Reflection;

namespace DependencyAnalyzer
{
    /// <summary>
    /// A wrapper class to a MemberInfo type
    /// </summary>
    internal abstract class ReferenceInfo
    {
        public string FullName => Host is TypeInfo t ? t.FullName : $"{Host.DeclaringType?.FullName}.{Host.Name}";
        public MemberInfo Host { get; }
        /// <summary>
        /// This member is auto-generated by the compiler
        /// </summary>
        internal bool IsCompilerGenerated => FullName.Contains(">");
        public bool IsNested => Host is TypeInfo t && t.IsNested;
        public TypeReferenceInfo Parent { get; } = null;
        protected internal ReferenceCollection ReferencedMembers { get; protected set; }
        protected internal ReferenceCollection ReferencingMembers { get; protected set; }
        /// <summary>
        /// The member presented similar to how it appears in source code
        /// </summary>
        public string Signature
        {
            get
            {
                if (_cachedSignature.Equals(string.Empty)) _cachedSignature = SignatureBuilder.GetSignature(Host);
                return _cachedSignature;
            }
        }

        private string _cachedSignature = string.Empty;


        public ReferenceInfo(MemberInfo host)
        {
            Host = host;
            ReferencedMembers = new(this);
            ReferencingMembers = new(this);
        }
        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="info">The info to be copied</param>
        internal ReferenceInfo(ReferenceInfo info)
        {
            Host = info.Host;
            Parent = info.Parent;
            ReferencedMembers = new(info.ReferencedMembers);
            ReferencingMembers = new(info.ReferencingMembers);
        }
        protected ReferenceInfo(MemberInfo host, TypeReferenceInfo parent) : this(host)
        {
            Parent = parent;
        }


        /// <summary>
        /// Evaluates the hosts' metadata definition
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>True if the compared object is a ReferenceInfo and its host has the same metadata definition</returns>
        public override bool Equals(object obj) => obj is ReferenceInfo ri && ri.Host.HasSameMetadataDefinitionAs(Host);
        internal abstract void FindReferencedMembers();
        internal abstract void FindReferencingMembers();
        /// <summary>
        /// Tests if this wrapper wraps the specified member
        /// </summary>
        /// <param name="member"></param>
        /// <returns>True if the Host has the same metadata definition as member</returns>
        internal bool Hosts(MemberInfo member) => Host.HasSameMetadataDefinitionAs(member);
        public override string ToString() => $"[{Host.MemberType}] {FullName}";
        public string ToString(ReportFormat format)
        {
            return format switch
            {
                ReportFormat.Basic => Host.Name,
                ReportFormat.Detailed => ToString(),
                ReportFormat.Signature => Signature,
                _ => Host.ToString()
            };
        }
        /// <summary>
        /// Present this type and its members formatted for reporting
        /// </summary>
        /// <param name="format"></param>
        /// <param name="spacing"></param>
        /// <returns></returns>
        public abstract string ToFormattedString(string spacing);
    }
}
