using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DependencyAnalyzer
{
    /// <summary>
    /// View all dependencies
    /// </summary>
    public class Architecture
    {
        /// <summary>
        /// Apply filters to which types will be analyzed
        /// </summary>
        public TypeFilter Filter { get; set; }
        public bool IncludeBaseReferences { get; set; } = false;
        public bool IncludeSelfReferences { get; set; } = false;
        public ReferenceCollection References => _references;

        private readonly ReferenceCollection _references = new();
        private List<Type> filteredTypes = new();
        private readonly List<Type> SuppliedTypes;


        public Architecture(Type[] classes)
        {
            SuppliedTypes = classes.CopyToList().Distinct().ToList();
        }


        public void AnalyzeDependencies()
        {
            /*  Apply filter to scope classes
             *
             *  Analyze methods of each class by
             *  reading the body of instructions
             *  
             *  When an instruction references another
             *  member in the scoped classes, append
             *  a ReferenceInfo object to the collection.
             */

            filteredTypes = SuppliedTypes.FindAll(Filter.FilterPredicate());

            filteredTypes.ForEach(t =>
            {
                List<MemberInfo> members = t.GetMyMembers();
                members.ForEach(m => DetermineReferences(m));
            });

            if (!IncludeSelfReferences)
            {
                List<ReferenceInfo> list = new(_references.ToList().FindAll(r =>
                    r.ReferencedMember.Name.Equals(r.ReferencingMember.Name)));
                list.ForEach(i => _references.Delete(i));

                list = new(_references.ToList().FindAll(r => 
                    r.ReferencedMember.DeclaringType?.FullName?.Equals(r.ReferencingMember.DeclaringType?.FullName) ?? false));
                list.ForEach(i => _references.Delete(i));
            }
            if (!IncludeBaseReferences) RemoveBaseReferences();
        }
        /// <summary>
        /// Store MetaDataObjects for each method part of a member
        /// </summary>
        private List<MetadataObject> CreateMetadataObjects(MemberInfo member)
        {
            if (member == null) return new();

            List<MetadataObject> bodies = new();
            List<MethodInfo> methods;

            switch (member.MemberType)
            {
                case MemberTypes.Constructor:
                    bodies.Add(new MetadataObject(member, null));
                    break;

                case MemberTypes.Event:
                    EventInfo E = (EventInfo)member;
                    methods = new()
                    {
                        E.GetAddMethod(),
                        E.GetRemoveMethod(),
                        E.GetRaiseMethod()
                    };
                    methods.AddRange(E.GetOtherMethods());
                    methods
                        .FindAll(m => m?.GetMethodBody() != null)
                        .ForEach(m => bodies.Add(new MetadataObject(member, m)));
                    break;

                case MemberTypes.Method:
                    try
                    {
                        bodies.Add(new MetadataObject(member, (MethodInfo)member));
                    }
                    catch (FileNotFoundException ex) { }
                    break;

                case MemberTypes.Property:
                    PropertyInfo P = (PropertyInfo)member;
                    methods = new()
                    {
                        P.GetGetMethod(),
                        P.GetSetMethod()
                    };
                    methods
                        .FindAll(m => m?.GetMethodBody() != null)
                        .ForEach(m => bodies.Add(new MetadataObject(member, m)));
                    break;

                default: break;
            }

            return bodies;
        }
        /// <summary>
        /// Find referenced members from method instructions
        /// </summary>
        private void DetermineReferences(MemberInfo member)
        {
            List<MethodInfo> methods;

            switch (member.MemberType)
            {
                case MemberTypes.Constructor:
                    // parameter types, local types, method
                    ConstructorInfo ctor = (ConstructorInfo)member;

                    ctor.GetParameters()
                        .ToList()
                        .ForEach(param =>
                        {
                            try
                            {
                                if (filteredTypes.Contains(param.ParameterType)) _references.Add(new(member, param.ParameterType)); 
                            } catch { }
                        });
                    ctor.GetMethodBody()?.LocalVariables
                        .ToList()
                        .ForEach(local =>
                        { 
                            if (filteredTypes.Contains(local.LocalType)) _references.Add(new(member, local.LocalType)); 
                        });
                    break;

                case MemberTypes.Method:
                    // return type, parameter types, local types, method
                    MethodInfo meth = (MethodInfo)member;
                    try
                    {
                        if (filteredTypes.Contains(meth.ReturnType)) _references.Add(new(member, meth.ReturnType));
                    }
                    catch (TypeLoadException ex) { }

                    try
                    {
                        meth.GetParameters()
                            .ToList()
                            .ForEach(param =>
                            { 
                                if (filteredTypes.Contains(param.ParameterType)) _references.Add(new(member, param.ParameterType)); 
                            });
                    }
                    catch (TypeLoadException ex) { /*Must lose all params info if a single one throws exception*/ }

                    try
                    {
                        meth.GetMethodBody()?.LocalVariables
                            .ToList()
                            .ForEach(local =>
                            {
                                if (filteredTypes.Contains(local.LocalType)) _references.Add(new(member, local.LocalType));
                            });
                    }
                    catch (TypeLoadException typeEx) { }
                    catch (FileNotFoundException missingEx) { }

                    break;

                case MemberTypes.Property: // type, get method, set method
                    PropertyInfo prop = (PropertyInfo)member;
                    if (filteredTypes.Contains(prop.PropertyType)) _references.Add(new(member, prop.PropertyType));

                    methods = new() 
                    { 
                        prop.GetGetMethod(), 
                        prop.GetSetMethod() 
                    };
                    methods.ForEach(m =>
                    {
                        m?.GetMethodBody()?.LocalVariables
                            .ToList()
                            .ForEach(local =>
                            { 
                                if (filteredTypes.Contains(local.LocalType)) _references.Add(new(member, local.LocalType)); 
                            });
                    });

                    break;
                case MemberTypes.Event:
                    // add method, remove method, raise method, 'other' methods
                    EventInfo eventi = (EventInfo)member;

                    methods = new() 
                    { 
                        eventi.GetAddMethod(), 
                        eventi.GetRemoveMethod(), 
                        eventi.GetRaiseMethod() 
                    };
                    methods.AddRange(eventi.GetOtherMethods());
                    methods.ForEach(m =>
                    {
                        m?.GetMethodBody()?.LocalVariables
                            .ToList()
                            .ForEach(local =>
                            { 
                                if (filteredTypes.Contains(local.LocalType)) _references.Add(new(member, local.LocalType)); 
                            });
                    });

                    break;
                case MemberTypes.Field:
                    // type
                    FieldInfo field = ((FieldInfo)member);
                    if (filteredTypes.Contains(field.FieldType)) _references.Add(new(member, field.FieldType));
                    break;
                default: // Nested, TypeInfo, Custom, All
                    break;
            }

            List<MetadataObject> methodBodies = CreateMetadataObjects(member);
            InterpretBodies(member, methodBodies);
        }
        /// <summary>
        /// Interpret IL instructions to find references to other type members
        /// </summary>
        private void InterpretBodies(MemberInfo member, List<MetadataObject> bodies)
        {
            bodies?.ForEach(B =>
            {
                List<Instruction> calls = B.GetCallInstructions();
                calls.ForEach(Ic => filteredTypes.ForEach(t =>
                {
                    List<MemberInfo> refMembers = t.GetMyMembers().FindAll(m => Ic.Operand.Contains(m.ArchName()));
                    refMembers.ForEach(m => _references.Add(new(member, m)));
                }));
            });
        }
        private void RemoveBaseReferences()
        {
            List<ReferenceInfo> refs = new(_references);
            foreach (ReferenceInfo r in refs)
            {
                var derivedType = r.ReferencingMember.DeclaringType;
                if (derivedType != null)
                {
                    //List<Type> baseTypes = SuppliedTypes.Where(t => t != derivedType && derivedType.IsAssignableFrom(t)).ToList();
                    List<Type> baseTypes = new();
                    SuppliedTypes.ForEach(t =>
                    {
                        if (t != derivedType && t.IsAssignableFrom(derivedType))
                            baseTypes.Add(t);
                    });
                    if (r.ReferencedMember.DeclaringType is not null && !baseTypes.Contains(r.ReferencedMember.DeclaringType))
                        _references.Delete(r);
                }
            }
        }

        [Flags]
        public enum TypeFilter 
        {
            None = 0b_0000,
            DeveloperClasses = 0b_0001,
            NonValueField = 0b_0010,
        }
    }
}
