# DependencyAnalyzer

An easy-to-use, adjustable, dependency viewer. Internally, all dependencies are mapped between members of a set of filtered classes.
The list of dependencies are further reduced to scope to user-controlled variables. The final collection of dependencies are available in the read only list of immutable record
ReferenceInfo(MemberInfo ReferencingMember, MemberInfo ReferencedMember). The ToString method on this record reads Memb1ClassName.Memb1Name -> Memb2ClassName.Memb2Name.\
\
To use, use Architecture class:
1) Instantiate with the parameter Type[] classes
2) Modify exposed members to adjust viewed results\
    a) Modify properties to narrow/widen dependency scope (ex: IncludeSelfReferences)\
    b) Use Flag attribute enum TypeFilter to analyze a subset of the included classes (In case of supplying entire assemplies, etc.  *Performance boost)
3) Call AnalyzeDependencies method
4) View the resulting dependencies with readonly References
