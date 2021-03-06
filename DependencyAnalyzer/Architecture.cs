using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DependencyAnalyzer
{
    public class Architecture
    {
        private readonly List<ArchitectType> ObjectSystem;

        /// <summary>
        /// Create an Architecture with determinent properties
        /// </summary>
        /// <param name="classes">A list of types that will be the scope of the Architecture</param>
        public Architecture(List<Type> classes, Architect.TypeFilter[] filters)
        {
            ObjectSystem = new List<ArchitectType>();
            foreach (Architect.TypeFilter of in filters)
            {
                classes = classes.FindAll(of.GetAssociatedPredicate());
            }
            classes.ForEach(c => ObjectSystem.Add(new ArchitectType(this, c)));
            Architect.LoadOpCodes();
            AssignAllDependencies();
        }

        /// <summary>
        /// Populate all ArchitectObject.ReferencedMembers and .DependentMembers
        /// </summary>
        /// <param name="classes"></param>
        private void AssignAllDependencies()
        {
            List<ArchitectType> group2;
            foreach (ArchitectType o1 in ObjectSystem)
            {
                if (o1.Class.IsEnum || o1.Class.IsNestedPrivate)
                {
                    continue;
                }
                else if (o1.Class.IsClass)
                {

                }
                else if (o1.Class.IsInterface)
                {

                }
            }

            //ObjectSystem.ForEach(o1 =>
            //{
            //    group2 = ObjectSystem.FindAll(o2 => !o2.Equals(o1));
            //    o1.Members.ForEach(m1 =>
            //    {
            //        group2.ForEach(o2 =>
            //        {
            //            o2.Members.ForEach(m2 =>
            //            {
            //                if (m1.MemberType == MemberTypes.Method && ((MethodInfo)m1).ReferencesMember(m2))
            //                {
            //                    o1.ReferencedMembers.Add(m2);
            //                    o2.DependentMembers.Add(m1);
            //                }
            //            });
            //        });
            //    });
            //});
        }

        /// <summary>
        /// List each class and its members of this system
        /// </summary>
        /// <returns>A formatted string report</returns>
        public string GetInfo()
        {
            string info = string.Empty;
            ObjectSystem.ForEach(t => info += t.GetInfo() + "\n");
            return info;
        }
    }
}
