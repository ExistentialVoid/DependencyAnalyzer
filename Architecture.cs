﻿global using System.Reflection;

namespace DependencyAnalyzer;

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
    public IReadOnlyList<ReferenceInfo> References => _references;

    private List<ReferenceInfo> _references = new();
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
            _references.RemoveAll(r => r.ReferencedMemeber.Name.Equals(r.ReferencingMember.Name));
            _references.RemoveAll(r => r.ReferencedMemeber.DeclaringType?.FullName?.Equals(r.ReferencingMember.DeclaringType?.FullName) ?? false);
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
                bodies.Add(new MetadataObject(member, (MethodInfo)member));
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
                        if (filteredTypes.Contains(param.ParameterType)) _references.Add(new(member, param.ParameterType)); 
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
                if (filteredTypes.Contains(meth.ReturnType)) _references.Add(new(member, meth.ReturnType));

                meth.GetParameters()
                    .ToList()
                    .ForEach(param =>
                    { 
                        if (filteredTypes.Contains(param.ParameterType)) _references.Add(new(member, param.ParameterType)); 
                    });
                meth.GetMethodBody()?.LocalVariables
                    .ToList()
                    .ForEach(local =>
                    { 
                        if (filteredTypes.Contains(local.LocalType)) _references.Add(new(member, local.LocalType)); 
                    });
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
        List<ReferenceInfo> newRefs = new();

        foreach (ReferenceInfo r in _references)
        {
            var derivedType = r.ReferencingMember.DeclaringType;
            if (derivedType != null)
            {
                //List<Type> baseTypes = SuppliedTypes.Where(t => t != derivedType && derivedType.IsAssignableFrom(t)).ToList();
                List<Type> baseTypes = new();
                SuppliedTypes.ForEach(t =>
                {
                    if (t != derivedType && t.IsAssignableFrom(derivedType))
                    {
                        baseTypes.Add(t);
                    }
                });
                if (r.ReferencedMemeber.DeclaringType == null || !baseTypes.Contains(r.ReferencedMemeber.DeclaringType))
                {
                    newRefs.Add(r);
                }
            }
        }
        _references = newRefs;
    }

    [Flags]
    public enum TypeFilter 
    {
        None = 0b_0000,
        DeveloperClasses = 0b_0001,
        NonValueField = 0b_0010,
    }
}