﻿using System;
using System.Collections.Generic;
using System.Reflection;

namespace DependencyAnalyzer
{
    internal abstract class ReferenceInfo
    {
        public string FullName => Host is TypeInfo t ? t.FullName : $"{Host.DeclaringType?.FullName}.{Host.Name}";
        /// <summary>
        /// This member is auto-generated by the compiler
        /// </summary>
        internal bool IsCompilerGenerated => Host.Name.Contains("<");
        //internal bool IsCompilerGenerated => Attribute.GetCustomAttribute(Host, typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute)) != null;
        public bool IsNested => Host is TypeInfo t && t.IsNested; 
        public MemberInfo Host { get; }
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


        public ReferenceInfo(MemberInfo member)
        {
            Host = member;
        }


        /// <summary>
        /// Evaluates the hosts' metadata definition
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>True if the compared object is a ReferenceInfo and its host has the same metadata definition</returns>
        public override bool Equals(object? obj)
        {
            return base.Equals(obj) || (obj is ReferenceInfo ri && ri.Host.HasSameMetadataDefinitionAs(Host));
        }
        internal abstract void FindReferencedMembers(List<ClassReferenceInfo> referenceTypes);
        internal abstract void FindReferencingMembers(List<ClassReferenceInfo> referenceTypes);
    }
}
