using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace DependencyAnalyzer
{
    /// <summary>
    /// Build readable MSIL instructions
    /// </summary>
    internal class MetadataObject
    {
        private byte[] IL { get; } = null;
        /// <summary>
        /// The full instruction set of MethodBody interpreted from ILByteArray
        /// </summary>
        private List<Instruction> InstructionStream { get; } = new List<Instruction>();
        private MethodBase Method { get; } = null; //Neccessary to get module and handle ConstructorInfo method body
        private List<Instruction> StrayInstructions { get; } = new List<Instruction>();

        #region grab il bytes
        private ushort U2(ref int p) => (ushort)I4(ref p);
        private int I4(ref int p) => IL[p++] | IL[p++] << 8 | IL[p++] << 16 | IL[p++] << 24;
        private uint U4(ref int p) => (uint)I4(ref p);
        private long I8(ref int p) => IL[p++] | IL[p++] << 8 | IL[p++] << 16 | IL[p++] << 24 | IL[p++] << 32 | IL[p++] << 40 | IL[p++] << 48 | IL[p++] << 56;
        private float R4(ref int p) => I4(ref p);
        private double R8(ref int p) => I8(ref p);
        private int MetadataToken(ref int p) => I4(ref p);
        #endregion


        /// <summary>
        /// Interpret method body for either a MethodInfo
        /// </summary>
        /// <param name="method"></param>
        public MetadataObject(MethodBase method)
        {
            Method = method;
            MethodBody body = method?.GetMethodBody();

            if (body != null)
            {
                IL = body.GetILAsByteArray();
                BuildInstructions();
            }
        }


        /// <summary>
        /// Constructs the list of ILInstructions according to the IL byte code for only 'call' type operands.
        /// </summary>
        /// <param name="module"></param>
        private void BuildInstructions()
        {
            int index = 0;
            while (index < IL.Length)
            {
                int pos = index; // for Instruction.Position
                byte ilByte = IL[index++];
                OpCode code;
                if (ilByte != 0xFE) code = ConvToSingleOpCode(ilByte);
                else
                {
                    ilByte = IL[index++];
                    code = ConvToMultiOpCode(ilByte);
                }

                string operand = GetOperand(code.OperandType, ref index); // advance index into next instruction
                Instruction instruction = new(code, operand, pos);
                InstructionStream.Add(instruction);
                Architecture.InstructionLog.AppendLine(
                    $"{Method.ReflectedType.FullName}.{Method.Name}()".PadRight(80)
                    + instruction.ToString());
            }
        }
        /// <summary>
        /// Return a multibyte OpCode matching the value
        /// </summary>
        /// <param name="val">The second byte of the intended two-byte OpCode value</param>
        /// <returns></returns>
        private static OpCode ConvToMultiOpCode(byte val)
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
        private static OpCode ConvToSingleOpCode(byte val)
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
        /// <summary>
        /// Retrieve only call, calli, and callvirt instructions from the stream
        /// </summary>
        /// <returns></returns>
        internal List<Instruction> GetInstructions()
        {
            List<Instruction> list = new();
            list.AddRange(InstructionStream.FindAll(I => I.Code.Name.Contains("call")));
            list.AddRange(InstructionStream.FindAll(I => I.Code.Name.Contains("fld")));
            list.AddRange(InstructionStream.FindAll(I => I.Code.Name.Equals("newobj")));;
            return list;
        }
        /// <summary>
        /// Get an operand as readable string from ilBytes[codeIndex]
        /// </summary>
        /// <param name="type">determine what object will be returned</param>
        /// <param name="pos">the index of il[]</param>
        /// <returns></returns>
        private string GetOperand(OperandType type, ref int pos)
        {
            // purposely advance the indexer through the bytes
            Module module = Method.Module;
            switch (type)
            {
                case OperandType.InlineBrTarget:
                    return (I4(ref pos) + pos).ToString();
                case OperandType.InlineField:
                    FieldInfo field = module.ResolveField(
                        MetadataToken(ref pos),
                        Method.ReflectedType.GetGenericArguments(),
                        Method is ConstructorInfo ? Array.Empty<Type>() : Method.GetGenericArguments()
                    );

                    //if (field.FieldType.Name.Contains("Predicate"))
                    //{
                    //    field.FieldType.GetMembers(Architecture.Filter).ToList()
                    //        .FindAll(m => m.MemberType == MemberTypes.Method)
                    //        .ForEach(m =>
                    //        {
                    //            MetadataObject mdobj = new((MethodInfo)m);
                    //            StrayInstructions.AddRange(mdobj.GetCallInstructions());
                    //        });
                    //}

                    return $"[{field.FieldType.Name}] {field.ReflectedType.Name}::{field.Name}";
                case OperandType.InlineI:
                    return I4(ref pos).ToString();
                case OperandType.InlineI8:
                    return I8(ref pos).ToString();
                case OperandType.InlineMethod:
                    MethodBase methodbase = module.ResolveMethod(
                        MetadataToken(ref pos),
                        Method.ReflectedType.GetGenericArguments(),
                        Method is ConstructorInfo ? Array.Empty<Type>() : Method.GetGenericArguments()
                    );

                    return (methodbase.IsStatic ? " static " : " ") + 
                        (methodbase is ConstructorInfo ? "[Void]" : ((MethodInfo)methodbase).ReturnType.Name)
                        + $" {methodbase.ReflectedType.Name}::{methodbase.Name}()";
                case OperandType.InlineR:
                    return R8(ref pos).ToString();
                case OperandType.InlineSig:
                    return module.ResolveSignature(MetadataToken(ref pos)).ToString();
                case OperandType.InlineString:
                    return $"\" {module.ResolveString(MetadataToken(ref pos))} \"";
                case OperandType.InlineSwitch:
                    string str = string.Empty;
                    uint cases = U4(ref pos);
                    for (uint i = 0; i < cases; i++)
                    {
                        str += $"{i}->{I4(ref pos)}";
                        if (i < cases - 1) str += ";";
                    }
                    return str;
                case OperandType.InlineTok:
                    // class [mscorlib]System.Console
                    // method int32 X::Fn()
                    // method bool GlobalFn(int32 &)
                    // field class X.Y Class::Field
                    //try { return module.ResolveType(MetadataToken(ref pos)).FullName; }
                    //catch { return "InlineTok not functional"; }

                    // Wont matter, just need to advance pos
                    MetadataToken(ref pos);
                    return string.Empty;
                case OperandType.InlineType:
                    // Use the generic attributes type in case of generic methods and classes
                    //Type[] genericArgs = Member.MemberType == MemberTypes.Method ? 
                    //    ((MethodInfo)Member).GetGenericArguments() : 
                    //    ((ConstructorInfo)Member).GetGenericArguments();
                    //return module.ResolveType(MetadataToken(ref pos), Member.ReflectedType.GetGenericArguments(), Member.GetType().GetGenericArguments()).FullName;
                    //return module.ResolveType(MetaTokenI4(ref pos), Member.ReflectedType.GetGenericArguments(), genericArgs).FullName;

                    // Wont matter, just need to advance pos
                    MetadataToken(ref pos);
                    return string.Empty;
                case OperandType.InlineVar:
                    return U2(ref pos).ToString();
                case OperandType.ShortInlineBrTarget:
                    return ((sbyte)(IL[pos++] + pos)).ToString();
                case OperandType.ShortInlineI:
                    return ((sbyte)IL[pos++]).ToString();
                case OperandType.ShortInlineR:
                    return R4(ref pos).ToString();
                case OperandType.ShortInlineVar:
                    return IL[pos++].ToString();
                default:
                    return string.Empty;
            }
        }
        public override string ToString() => $"{Method.ReflectedType?.Name}::{Method.Name}   {InstructionStream.Count} instructions";
    }
}
