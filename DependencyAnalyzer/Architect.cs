using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace DependencyAnalyzer
{
    // Original SDILReader: https://www.codeproject.com/Articles/14058/Parsing-the-IL-of-a-Method-Body


    /// <summary>
    /// A class to provide dynamic compiling functions
    /// </summary>
    public static class Architect
    {
        #region Enums
        internal enum ArchType { Unknown, Concrete, Abstract, Interface, Structure }
        public enum TypeFilter { None, DeveloperClasses, NoGetSet, NoValueField, Public }
        #endregion
        //internal static Dictionary<int, object> Cache = new Dictionary<int, object>();
        //internal static Module[] modules = null;
        internal static OpCode[] multiByteOpCodes;
        internal static OpCode[] singleByteOpCodes;

        /// <summary>
        /// Arrange OpCodes by their Value from their enum listing into single-/multi-ByteOpCodes globals.
        /// </summary>
        public static void LoadOpCodes()
        {
            singleByteOpCodes = new OpCode[256];
            multiByteOpCodes = new OpCode[256];
            typeof(OpCodes).GetFields().ToList().FindAll(f => f.FieldType == typeof(OpCode)).ForEach(c =>
            {
                OpCode code = (OpCode)c.GetValue(null);
                ushort val = (ushort)code.Value;
                if (val <= 0xff)
                    singleByteOpCodes[val] = code;
                else if ((val & 0xff00) != 0xfe00)
                    throw new Exception("Invalid OpCode.");
                else
                    multiByteOpCodes[val & 0xff] = code;
            });
        }

        /// <summary>
        /// Retrieve the friendly name of a type
        /// </summary>
        /// <param name="typeName">The complete name to the type</param>
        /// <returns>The simplified name of the type (i.e. "int" instead f System.Int32)</returns>
        public static string ProcessSpecialTypes(string typeName)
        {
            switch (typeName)
            {
                case "System.string":
                case "System.String":
                case "String":
                    return "string";
                case "System.Int32":
                case "Int":
                case "Int32":
                    return "int";
            }
            return typeName;
        }

        #region Extension Methods
        internal static ArchType GetArchType(this Type T)
        {
            if (T.IsAbstract) return ArchType.Abstract;
            if (T.IsInterface) return ArchType.Interface;
            if (T.IsClass) return ArchType.Concrete;
            if (T.IsValueType) return ArchType.Structure;
            else return ArchType.Unknown;
        }

        /// <summary>
        /// Determine if this object references a member within any of its members
        /// </summary>
        /// <param name="method"></param>
        /// <param name="member">member whose name will be searched</param>
        /// <returns>true if the member name can be shown to exist in the source code of this type</returns>
        internal static bool ReferencesMember(this MethodInfo method, ArchitectMember member)
        {
            try
            {
                MethodBodyReader reader = new MethodBodyReader(method);
                string methodbody = reader.GetBodyCode();
                if (methodbody.Contains(member.Member.Name)) return true;
            }
            catch
            {
                Console.WriteLine("Error reading method body");
            }

            //// Check if any fields or properties are local variables of method
            //List<LocalVariableInfo> locals = method.GetMethodBody().LocalVariables.ToList();
            //if (locals.FindAll(l => l.LocalType.) return true;
            //if (locals.FindAll(m => m.MemberType == MemberTypes.Property && ((PropertyInfo)m).PropertyType == member).Count > 0) return true;

            //// Check if methods include member
            //const string memberChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_";
            //List<MethodInfo> Methods = method.GetMethods().ToList();
            //Methods.FindAll(m => m.GetMethodBody()).ForEach(m =>
            //{
            //    //member is a comment or not?


            //    //memberName vs anotherMemberName?


            //    //Class.member vs anotherClass.member?

            //});


            return false;
        }

        public static Predicate<Type> GetAssociatedPredicate(this TypeFilter filter)
        {
            switch (filter)
            {
                case TypeFilter.None:
                    return t => true;
                case TypeFilter.DeveloperClasses:
                    return t => !t.Name.Contains("<>");
                case TypeFilter.NoGetSet:
                    return t => !t.Name.Contains("get_") && !t.Name.Contains("set_");
                case TypeFilter.NoValueField:
                    return t => !t.Name.Contains("value__");
                case TypeFilter.Public:
                    return t => t.IsPublic;
                default: throw new NotImplementedException($"'{filter}' has not been implemented");
            }
        }

        #region ToList extensions
        public static List<EventInfo> ToList(this EventInfo[] array)
        {
            List<EventInfo> list = new List<EventInfo>();
            foreach (EventInfo i in array) list.Add(i);
            return list;
        }
        public static List<FieldInfo> ToList(this FieldInfo[] array)
        {
            List<FieldInfo> list = new List<FieldInfo>();
            foreach (FieldInfo i in array) list.Add(i);
            return list;
        }
        public static List<LocalVariableInfo> ToList(this IList<LocalVariableInfo> locals)
        {
            List<LocalVariableInfo> list = new List<LocalVariableInfo>();
            foreach (LocalVariableInfo i in locals) list.Add(i);
            return list;
        }
        public static List<MemberInfo> ToList(this MemberInfo[] array)
        {
            List<MemberInfo> list = new List<MemberInfo>();
            foreach (MemberInfo i in array) list.Add(i);
            return list;
        }
        public static List<MethodInfo> ToList(this MethodInfo[] array)
        {
            List<MethodInfo> list = new List<MethodInfo>();
            foreach (MethodInfo i in array) list.Add(i);
            return list;
        }
        public static List<ParameterInfo> ToList(this ParameterInfo[] array)
        {
            List<ParameterInfo> list = new List<ParameterInfo>();
            foreach (ParameterInfo i in array) list.Add(i);
            return list;
        }
        public static List<PropertyInfo> ToList(this PropertyInfo[] array)
        {
            List<PropertyInfo> list = new List<PropertyInfo>();
            foreach (PropertyInfo i in array) list.Add(i);
            return list;
        }
        public static List<Type> ToList(this Type[] array)
        {
            List<Type> list = new List<Type>();
            foreach (Type i in array) list.Add(i);
            return list;
        }
        public static List<AssemblyName> ToList(this AssemblyName[] array)
        {
            List<AssemblyName> list = new List<AssemblyName>();
            foreach (AssemblyName i in array) list.Add(i);
            return list;
        }
        public static List<Module> ToList(this Module[] array)
        {
            List<Module> list = new List<Module>();
            foreach (Module i in array) list.Add(i);
            return list;
        }
        #endregion
        #endregion
    }
}