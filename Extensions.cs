using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace DependencyAnalyzer
{
    internal static partial class Extensions
    {
        /// <summary>
        /// DeclaringType::MemberName
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public static string ArchName(this MemberInfo member)
        {
            if (member is null) return string.Empty;

            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    FieldInfo field = member as FieldInfo;
                    return $"{field.DeclaringType?.FullName}.{field.Name}";
                case MemberTypes.Method:
                    MethodInfo method = member as MethodInfo;
                    return $"{method.DeclaringType?.FullName}.{method.Name}";
                case MemberTypes.Event:
                    EventInfo eventI = member as EventInfo;
                    return $"{eventI.DeclaringType?.FullName}.{eventI.Name}";
                case MemberTypes.Constructor:
                    ConstructorInfo constructor = member as ConstructorInfo;
                    return $"{constructor.DeclaringType?.FullName}.{constructor.Name}";
                case MemberTypes.Property:
                    PropertyInfo property = member as PropertyInfo;
                    return $"{property.DeclaringType?.FullName}.{property.Name}";
                case MemberTypes.NestedType:
                case MemberTypes.TypeInfo:
                    TypeInfo typeI = member as TypeInfo;
                    return $"{typeI.FullName}";
                default: return string.Empty;
            }
        }
        /// <summary>
        /// Return a multibyte OpCode matching the value
        /// </summary>
        /// <param name="val">The second byte of the intended two-byte OpCode value</param>
        /// <returns></returns>
        public static OpCode ConvToMultiOpCode(this byte val)
        {
            switch (val)
            {
                case 0x00: return OpCodes.Arglist;
                case 0x01: return OpCodes.Ceq;
                case 0x02: return OpCodes.Cgt;
                case 0x03: return OpCodes.Cgt_Un;
                case 0x04: return OpCodes.Clt;
                case 0x05: return OpCodes.Clt_Un;
                case 0x06: return OpCodes.Ldftn;
                case 0x07: return OpCodes.Ldvirtftn;
                case 0x09: return OpCodes.Ldarg;
                case 0x0A: return OpCodes.Ldarga;
                case 0x0B: return OpCodes.Starg;
                case 0x0C: return OpCodes.Ldloc;
                case 0x0D: return OpCodes.Ldloca;
                case 0x0E: return OpCodes.Stloc;
                case 0x0F: return OpCodes.Localloc;
                case 0x11: return OpCodes.Endfilter;
                case 0x12: return OpCodes.Unaligned;
                case 0x13: return OpCodes.Volatile;
                case 0x14: return OpCodes.Tailcall;
                case 0x15: return OpCodes.Initobj;
                case 0x16: return OpCodes.Constrained;
                case 0x17: return OpCodes.Cpblk;
                case 0x18: return OpCodes.Initblk;
                case 0x1A: return OpCodes.Rethrow;
                case 0x1C: return OpCodes.Sizeof;
                case 0x1D: return OpCodes.Refanytype;
                case 0x1E: return OpCodes.Readonly;
                default: return OpCodes.Nop;
            };
        }
        /// <summary>
        /// Return a single-byte OpCode matching the value
        /// </summary>
        /// <param name="val">The intended OpCode value</param>
        /// <returns></returns>
        public static OpCode ConvToSingleOpCode(this byte val)
        {
            switch (val)
            {
                case 0x00: return OpCodes.Nop;
                case 0x01: return OpCodes.Break;
                case 0x02: return OpCodes.Ldarg_0;
                case 0x03: return OpCodes.Ldarg_1;
                case 0x04: return OpCodes.Ldarg_2;
                case 0x05: return OpCodes.Ldarg_3;
                case 0x06: return OpCodes.Ldloc_0;
                case 0x07: return OpCodes.Ldloc_1;
                case 0x08: return OpCodes.Ldloc_2;
                case 0x09: return OpCodes.Ldloc_3;
                case 0x0A: return OpCodes.Stloc_0;
                case 0x0B: return OpCodes.Stloc_1;
                case 0x0C: return OpCodes.Stloc_2;
                case 0x0D: return OpCodes.Stloc_3;
                case 0x0E: return OpCodes.Ldarg_S;
                case 0x0F: return OpCodes.Ldarga_S;
                case 0x10: return OpCodes.Starg_S;
                case 0x11: return OpCodes.Ldloc_S;
                case 0x12: return OpCodes.Ldloca_S;
                case 0x13: return OpCodes.Stloc_S;
                case 0x14: return OpCodes.Ldnull;
                case 0x15: return OpCodes.Ldc_I4_M1;
                case 0x16: return OpCodes.Ldc_I4_0;
                case 0x17: return OpCodes.Ldc_I4_1;
                case 0x18: return OpCodes.Ldc_I4_2;
                case 0x19: return OpCodes.Ldc_I4_3;
                case 0x1A: return OpCodes.Ldc_I4_4;
                case 0x1B: return OpCodes.Ldc_I4_5;
                case 0x1C: return OpCodes.Ldc_I4_6;
                case 0x1D: return OpCodes.Ldc_I4_7;
                case 0x1E: return OpCodes.Ldc_I4_8;
                case 0x1F: return OpCodes.Ldc_I4_S;
                case 0x20: return OpCodes.Ldc_I4;
                case 0x21: return OpCodes.Ldc_I8;
                case 0x22: return OpCodes.Ldc_R4;
                case 0x23: return OpCodes.Ldc_R8;
                case 0x25: return OpCodes.Dup;
                case 0x26: return OpCodes.Pop;
                case 0x27: return OpCodes.Jmp;
                case 0x28: return OpCodes.Call;
                case 0x29: return OpCodes.Calli;
                case 0x2A: return OpCodes.Ret;
                case 0x2B: return OpCodes.Br_S;
                case 0x2C: return OpCodes.Brfalse_S;
                case 0x2D: return OpCodes.Brtrue_S;
                case 0x2E: return OpCodes.Beq_S;
                case 0x2F: return OpCodes.Bge_S;
                case 0x30: return OpCodes.Bgt_S;
                case 0x31: return OpCodes.Ble_S;
                case 0x32: return OpCodes.Blt_S;
                case 0x33: return OpCodes.Bne_Un_S;
                case 0x34: return OpCodes.Bge_Un_S;
                case 0x35: return OpCodes.Bgt_Un_S;
                case 0x36: return OpCodes.Ble_Un_S;
                case 0x37: return OpCodes.Blt_Un_S;
                case 0x38: return OpCodes.Br;
                case 0x39: return OpCodes.Brfalse;
                case 0x3A: return OpCodes.Brtrue;
                case 0x3B: return OpCodes.Beq;
                case 0x3C: return OpCodes.Bge;
                case 0x3D: return OpCodes.Bgt;
                case 0x3E: return OpCodes.Ble;
                case 0x3F: return OpCodes.Blt;
                case 0x40: return OpCodes.Bne_Un;
                case 0x41: return OpCodes.Bge_Un;
                case 0x42: return OpCodes.Bgt_Un;
                case 0x43: return OpCodes.Ble_Un;
                case 0x44: return OpCodes.Blt_Un;
                case 0x45: return OpCodes.Switch;
                case 0x46: return OpCodes.Ldind_I1;
                case 0x47: return OpCodes.Ldind_U1;
                case 0x48: return OpCodes.Ldind_I2;
                case 0x49: return OpCodes.Ldind_U2;
                case 0x4A: return OpCodes.Ldind_I4;
                case 0x4B: return OpCodes.Ldind_U4;
                case 0x4C: return OpCodes.Ldind_I8;
                case 0x4D: return OpCodes.Ldind_I;
                case 0x4E: return OpCodes.Ldind_R4;
                case 0x4F: return OpCodes.Ldind_R8;
                case 0x50: return OpCodes.Ldind_Ref;
                case 0x51: return OpCodes.Stind_Ref;
                case 0x52: return OpCodes.Stind_I1;
                case 0x53: return OpCodes.Stind_I2;
                case 0x54: return OpCodes.Stind_I4;
                case 0x55: return OpCodes.Stind_I8;
                case 0x56: return OpCodes.Stind_R4;
                case 0x57: return OpCodes.Stind_R8;
                case 0x58: return OpCodes.Add;
                case 0x59: return OpCodes.Sub;
                case 0x5A: return OpCodes.Mul;
                case 0x5B: return OpCodes.Div;
                case 0x5C: return OpCodes.Div_Un;
                case 0x5D: return OpCodes.Rem;
                case 0x5E: return OpCodes.Rem_Un;
                case 0x5F: return OpCodes.And;
                case 0x60: return OpCodes.Or;
                case 0x61: return OpCodes.Xor;
                case 0x62: return OpCodes.Shl;
                case 0x63: return OpCodes.Shr;
                case 0x64: return OpCodes.Shr_Un;
                case 0x65: return OpCodes.Neg;
                case 0x66: return OpCodes.Not;
                case 0x67: return OpCodes.Conv_I1;
                case 0x68: return OpCodes.Conv_I2;
                case 0x69: return OpCodes.Conv_I4;
                case 0x6A: return OpCodes.Conv_I8;
                case 0x6B: return OpCodes.Conv_R4;
                case 0x6C: return OpCodes.Conv_R8;
                case 0x6D: return OpCodes.Conv_U4;
                case 0x6E: return OpCodes.Conv_U8;
                case 0x6F: return OpCodes.Callvirt;
                case 0x70: return OpCodes.Cpobj;
                case 0x71: return OpCodes.Ldobj;
                case 0x72: return OpCodes.Ldstr;
                case 0x73: return OpCodes.Newobj;
                case 0x74: return OpCodes.Castclass;
                case 0x75: return OpCodes.Isinst;
                case 0x76: return OpCodes.Conv_R_Un;
                case 0x79: return OpCodes.Unbox;
                case 0x7A: return OpCodes.Throw;
                case 0x7B: return OpCodes.Ldfld;
                case 0x7C: return OpCodes.Ldflda;
                case 0x7D: return OpCodes.Stfld;
                case 0x7E: return OpCodes.Ldsfld;
                case 0x7F: return OpCodes.Ldsflda;
                case 0x80: return OpCodes.Stsfld;
                case 0x81: return OpCodes.Stobj;
                case 0x82: return OpCodes.Conv_Ovf_I1_Un;
                case 0x83: return OpCodes.Conv_Ovf_I2_Un;
                case 0x84: return OpCodes.Conv_Ovf_I4_Un;
                case 0x85: return OpCodes.Conv_Ovf_I8_Un;
                case 0x86: return OpCodes.Conv_Ovf_U1_Un;
                case 0x87: return OpCodes.Conv_Ovf_U2_Un;
                case 0x88: return OpCodes.Conv_Ovf_U4_Un;
                case 0x89: return OpCodes.Conv_Ovf_U8_Un;
                case 0x8A: return OpCodes.Conv_Ovf_I_Un;
                case 0x8B: return OpCodes.Conv_Ovf_U_Un;
                case 0x8C: return OpCodes.Box;
                case 0x8D: return OpCodes.Newarr;
                case 0x8E: return OpCodes.Ldlen;
                case 0x8F: return OpCodes.Ldelema;
                case 0x90: return OpCodes.Ldelem_I1;
                case 0x91: return OpCodes.Ldelem_U1;
                case 0x92: return OpCodes.Ldelem_I2;
                case 0x93: return OpCodes.Ldelem_U2;
                case 0x94: return OpCodes.Ldelem_I4;
                case 0x95: return OpCodes.Ldelem_U4;
                case 0x96: return OpCodes.Ldelem_I8;
                case 0x97: return OpCodes.Ldelem_I;
                case 0x98: return OpCodes.Ldelem_R4;
                case 0x99: return OpCodes.Ldelem_R8;
                case 0x9A: return OpCodes.Ldelem_Ref;
                case 0x9B: return OpCodes.Stelem_I;
                case 0x9C: return OpCodes.Stelem_I1;
                case 0x9D: return OpCodes.Stelem_I2;
                case 0x9E: return OpCodes.Stelem_I4;
                case 0x9F: return OpCodes.Stelem_I8;
                case 0xA0: return OpCodes.Stelem_R4;
                case 0xA1: return OpCodes.Stelem_R8;
                case 0xA2: return OpCodes.Stelem_Ref;
                case 0xA3: return OpCodes.Ldelem;
                case 0xA4: return OpCodes.Stelem;
                case 0xA5: return OpCodes.Unbox_Any;
                case 0xB3: return OpCodes.Conv_Ovf_I1;
                case 0xB4: return OpCodes.Conv_Ovf_U1;
                case 0xB5: return OpCodes.Conv_Ovf_I2;
                case 0xB6: return OpCodes.Conv_Ovf_U2;
                case 0xB7: return OpCodes.Conv_Ovf_I4;
                case 0xB8: return OpCodes.Conv_Ovf_U4;
                case 0xB9: return OpCodes.Conv_Ovf_I8;
                case 0xBA: return OpCodes.Conv_Ovf_U8;
                case 0xC2: return OpCodes.Refanyval;
                case 0xC3: return OpCodes.Ckfinite;
                case 0xC6: return OpCodes.Mkrefany;
                case 0xD0: return OpCodes.Ldtoken;
                case 0xD1: return OpCodes.Conv_U2;
                case 0xD2: return OpCodes.Conv_U1;
                case 0xD3: return OpCodes.Conv_I;
                case 0xD4: return OpCodes.Conv_Ovf_I;
                case 0xD5: return OpCodes.Conv_Ovf_U;
                case 0xD6: return OpCodes.Add_Ovf;
                case 0xD7: return OpCodes.Add_Ovf_Un;
                case 0xD8: return OpCodes.Mul_Ovf;
                case 0xD9: return OpCodes.Mul_Ovf_Un;
                case 0xDA: return OpCodes.Sub_Ovf;
                case 0xDB: return OpCodes.Sub_Ovf_Un;
                case 0xDC: return OpCodes.Endfinally;
                case 0xDD: return OpCodes.Leave;
                case 0xDE: return OpCodes.Leave_S;
                case 0xDF: return OpCodes.Stind_I;
                case 0xE0: return OpCodes.Conv_U;
                default: return OpCodes.Nop;
            };
        }
        public static List<T> CopyToList<T>(this T[] collection)
        {
            List<T> list = new();
            foreach (T item in collection)
            {
                list.Add(item);
            }
            return list;
        }
        public static Predicate<Type> FilterPredicate(this Architecture.TypeFilter val)
        {
            switch (val)
            {
                case Architecture.TypeFilter.DeveloperClasses: // 1
                    return t => !t.Name.Contains("<>");
                case Architecture.TypeFilter.NonValueField: // 2
                    return t => !t.Name.Contains("value__");
                case Architecture.TypeFilter.NonValueField | Architecture.TypeFilter.DeveloperClasses: // 3
                    return t => !t.Name.Contains("<>") && !t.Name.Contains("value__");
                default:
                    return t => true;
            };
        }
        public static List<MemberInfo> GetMyMembers(this Type T) => T.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly).ToList();
        /// <summary>
        /// Shows a string of 'size' representing this number. Will return the full number if 'size' < val's size.
        /// </summary>
        /// <param name="val"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static string SetSizeStr(this int val, int size)
        {
            if (val.ToString().Length >= size) return val.ToString();

            string zeros = string.Empty;
            for (int i = val.ToString().Length; i < size; i++) zeros += "0";
            return zeros + val.ToString();
        }
    }
}
