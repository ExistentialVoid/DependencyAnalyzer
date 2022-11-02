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
        private byte[] IL { get; }
        /// <summary>
        /// The full instruction set of MethodBody interpreted from ILByteArray
        /// </summary>
        private List<Instruction> InstructionStream { get; } = new List<Instruction>();
        private MethodBase Method { get; } //Neccessary to get module and handle ConstructorInfo method body
        // private List<Instruction> StrayInstructions { get; } = new List<Instruction>();

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
            if (method is null) throw new ArgumentNullException(nameof(method));

            Method = method;
            if (method.GetMethodBody()?.GetILAsByteArray() is byte[] il)
            {
                IL = il;
                BuildInstructions();
            }
            else IL = Array.Empty<byte>();
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
                    $"{Method.ReflectedType?.FullName}.{Method.Name}()".PadRight(80)
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
            return val switch
            {
                0x00 => OpCodes.Arglist,
                0x01 => OpCodes.Ceq,
                0x02 => OpCodes.Cgt,
                0x03 => OpCodes.Cgt_Un,
                0x04 => OpCodes.Clt,
                0x05 => OpCodes.Clt_Un,
                0x06 => OpCodes.Ldftn,
                0x07 => OpCodes.Ldvirtftn,
                0x09 => OpCodes.Ldarg,
                0x0A => OpCodes.Ldarga,
                0x0B => OpCodes.Starg,
                0x0C => OpCodes.Ldloc,
                0x0D => OpCodes.Ldloca,
                0x0E => OpCodes.Stloc,
                0x0F => OpCodes.Localloc,
                0x11 => OpCodes.Endfilter,
                0x12 => OpCodes.Unaligned,
                0x13 => OpCodes.Volatile,
                0x14 => OpCodes.Tailcall,
                0x15 => OpCodes.Initobj,
                0x16 => OpCodes.Constrained,
                0x17 => OpCodes.Cpblk,
                0x18 => OpCodes.Initblk,
                0x1A => OpCodes.Rethrow,
                0x1C => OpCodes.Sizeof,
                0x1D => OpCodes.Refanytype,
                0x1E => OpCodes.Readonly,
                _ => OpCodes.Nop
            };
        }
        /// <summary>
        /// Return a single-byte OpCode matching the value
        /// </summary>
        /// <param name="val">The intended OpCode value</param>
        /// <returns></returns>
        private static OpCode ConvToSingleOpCode(byte val)
        {
            return val switch
            {
                0x00 => OpCodes.Nop,
                0x01 => OpCodes.Break,
                0x02 => OpCodes.Ldarg_0,
                0x03 => OpCodes.Ldarg_1,
                0x04 => OpCodes.Ldarg_2,
                0x05 => OpCodes.Ldarg_3,
                0x06 => OpCodes.Ldloc_0,
                0x07 => OpCodes.Ldloc_1,
                0x08 => OpCodes.Ldloc_2,
                0x09 => OpCodes.Ldloc_3,
                0x0A => OpCodes.Stloc_0,
                0x0B => OpCodes.Stloc_1,
                0x0C => OpCodes.Stloc_2,
                0x0D => OpCodes.Stloc_3,
                0x0E => OpCodes.Ldarg_S,
                0x0F => OpCodes.Ldarga_S,
                0x10 => OpCodes.Starg_S,
                0x11 => OpCodes.Ldloc_S,
                0x12 => OpCodes.Ldloca_S,
                0x13 => OpCodes.Stloc_S,
                0x14 => OpCodes.Ldnull,
                0x15 => OpCodes.Ldc_I4_M1,
                0x16 => OpCodes.Ldc_I4_0,
                0x17 => OpCodes.Ldc_I4_1,
                0x18 => OpCodes.Ldc_I4_2,
                0x19 => OpCodes.Ldc_I4_3,
                0x1A => OpCodes.Ldc_I4_4,
                0x1B => OpCodes.Ldc_I4_5,
                0x1C => OpCodes.Ldc_I4_6,
                0x1D => OpCodes.Ldc_I4_7,
                0x1E => OpCodes.Ldc_I4_8,
                0x1F => OpCodes.Ldc_I4_S,
                0x20 => OpCodes.Ldc_I4,
                0x21 => OpCodes.Ldc_I8,
                0x22 => OpCodes.Ldc_R4,
                0x23 => OpCodes.Ldc_R8,
                0x25 => OpCodes.Dup,
                0x26 => OpCodes.Pop,
                0x27 => OpCodes.Jmp,
                0x28 => OpCodes.Call,
                0x29 => OpCodes.Calli,
                0x2A => OpCodes.Ret,
                0x2B => OpCodes.Br_S,
                0x2C => OpCodes.Brfalse_S,
                0x2D => OpCodes.Brtrue_S,
                0x2E => OpCodes.Beq_S,
                0x2F => OpCodes.Bge_S,
                0x30 => OpCodes.Bgt_S,
                0x31 => OpCodes.Ble_S,
                0x32 => OpCodes.Blt_S,
                0x33 => OpCodes.Bne_Un_S,
                0x34 => OpCodes.Bge_Un_S,
                0x35 => OpCodes.Bgt_Un_S,
                0x36 => OpCodes.Ble_Un_S,
                0x37 => OpCodes.Blt_Un_S,
                0x38 => OpCodes.Br,
                0x39 => OpCodes.Brfalse,
                0x3A => OpCodes.Brtrue,
                0x3B => OpCodes.Beq,
                0x3C => OpCodes.Bge,
                0x3D => OpCodes.Bgt,
                0x3E => OpCodes.Ble,
                0x3F => OpCodes.Blt,
                0x40 => OpCodes.Bne_Un,
                0x41 => OpCodes.Bge_Un,
                0x42 => OpCodes.Bgt_Un,
                0x43 => OpCodes.Ble_Un,
                0x44 => OpCodes.Blt_Un,
                0x45 => OpCodes.Switch,
                0x46 => OpCodes.Ldind_I1,
                0x47 => OpCodes.Ldind_U1,
                0x48 => OpCodes.Ldind_I2,
                0x49 => OpCodes.Ldind_U2,
                0x4A => OpCodes.Ldind_I4,
                0x4B => OpCodes.Ldind_U4,
                0x4C => OpCodes.Ldind_I8,
                0x4D => OpCodes.Ldind_I,
                0x4E => OpCodes.Ldind_R4,
                0x4F => OpCodes.Ldind_R8,
                0x50 => OpCodes.Ldind_Ref,
                0x51 => OpCodes.Stind_Ref,
                0x52 => OpCodes.Stind_I1,
                0x53 => OpCodes.Stind_I2,
                0x54 => OpCodes.Stind_I4,
                0x55 => OpCodes.Stind_I8,
                0x56 => OpCodes.Stind_R4,
                0x57 => OpCodes.Stind_R8,
                0x58 => OpCodes.Add,
                0x59 => OpCodes.Sub,
                0x5A => OpCodes.Mul,
                0x5B => OpCodes.Div,
                0x5C => OpCodes.Div_Un,
                0x5D => OpCodes.Rem,
                0x5E => OpCodes.Rem_Un,
                0x5F => OpCodes.And,
                0x60 => OpCodes.Or,
                0x61 => OpCodes.Xor,
                0x62 => OpCodes.Shl,
                0x63 => OpCodes.Shr,
                0x64 => OpCodes.Shr_Un,
                0x65 => OpCodes.Neg,
                0x66 => OpCodes.Not,
                0x67 => OpCodes.Conv_I1,
                0x68 => OpCodes.Conv_I2,
                0x69 => OpCodes.Conv_I4,
                0x6A => OpCodes.Conv_I8,
                0x6B => OpCodes.Conv_R4,
                0x6C => OpCodes.Conv_R8,
                0x6D => OpCodes.Conv_U4,
                0x6E => OpCodes.Conv_U8,
                0x6F => OpCodes.Callvirt,
                0x70 => OpCodes.Cpobj,
                0x71 => OpCodes.Ldobj,
                0x72 => OpCodes.Ldstr,
                0x73 => OpCodes.Newobj,
                0x74 => OpCodes.Castclass,
                0x75 => OpCodes.Isinst,
                0x76 => OpCodes.Conv_R_Un,
                0x79 => OpCodes.Unbox,
                0x7A => OpCodes.Throw,
                0x7B => OpCodes.Ldfld,
                0x7C => OpCodes.Ldflda,
                0x7D => OpCodes.Stfld,
                0x7E => OpCodes.Ldsfld,
                0x7F => OpCodes.Ldsflda,
                0x80 => OpCodes.Stsfld,
                0x81 => OpCodes.Stobj,
                0x82 => OpCodes.Conv_Ovf_I1_Un,
                0x83 => OpCodes.Conv_Ovf_I2_Un,
                0x84 => OpCodes.Conv_Ovf_I4_Un,
                0x85 => OpCodes.Conv_Ovf_I8_Un,
                0x86 => OpCodes.Conv_Ovf_U1_Un,
                0x87 => OpCodes.Conv_Ovf_U2_Un,
                0x88 => OpCodes.Conv_Ovf_U4_Un,
                0x89 => OpCodes.Conv_Ovf_U8_Un,
                0x8A => OpCodes.Conv_Ovf_I_Un,
                0x8B => OpCodes.Conv_Ovf_U_Un,
                0x8C => OpCodes.Box,
                0x8D => OpCodes.Newarr,
                0x8E => OpCodes.Ldlen,
                0x8F => OpCodes.Ldelema,
                0x90 => OpCodes.Ldelem_I1,
                0x91 => OpCodes.Ldelem_U1,
                0x92 => OpCodes.Ldelem_I2,
                0x93 => OpCodes.Ldelem_U2,
                0x94 => OpCodes.Ldelem_I4,
                0x95 => OpCodes.Ldelem_U4,
                0x96 => OpCodes.Ldelem_I8,
                0x97 => OpCodes.Ldelem_I,
                0x98 => OpCodes.Ldelem_R4,
                0x99 => OpCodes.Ldelem_R8,
                0x9A => OpCodes.Ldelem_Ref,
                0x9B => OpCodes.Stelem_I,
                0x9C => OpCodes.Stelem_I1,
                0x9D => OpCodes.Stelem_I2,
                0x9E => OpCodes.Stelem_I4,
                0x9F => OpCodes.Stelem_I8,
                0xA0 => OpCodes.Stelem_R4,
                0xA1 => OpCodes.Stelem_R8,
                0xA2 => OpCodes.Stelem_Ref,
                0xA3 => OpCodes.Ldelem,
                0xA4 => OpCodes.Stelem,
                0xA5 => OpCodes.Unbox_Any,
                0xB3 => OpCodes.Conv_Ovf_I1,
                0xB4 => OpCodes.Conv_Ovf_U1,
                0xB5 => OpCodes.Conv_Ovf_I2,
                0xB6 => OpCodes.Conv_Ovf_U2,
                0xB7 => OpCodes.Conv_Ovf_I4,
                0xB8 => OpCodes.Conv_Ovf_U4,
                0xB9 => OpCodes.Conv_Ovf_I8,
                0xBA => OpCodes.Conv_Ovf_U8,
                0xC2 => OpCodes.Refanyval,
                0xC3 => OpCodes.Ckfinite,
                0xC6 => OpCodes.Mkrefany,
                0xD0 => OpCodes.Ldtoken,
                0xD1 => OpCodes.Conv_U2,
                0xD2 => OpCodes.Conv_U1,
                0xD3 => OpCodes.Conv_I,
                0xD4 => OpCodes.Conv_Ovf_I,
                0xD5 => OpCodes.Conv_Ovf_U,
                0xD6 => OpCodes.Add_Ovf,
                0xD7 => OpCodes.Add_Ovf_Un,
                0xD8 => OpCodes.Mul_Ovf,
                0xD9 => OpCodes.Mul_Ovf_Un,
                0xDA => OpCodes.Sub_Ovf,
                0xDB => OpCodes.Sub_Ovf_Un,
                0xDC => OpCodes.Endfinally,
                0xDD => OpCodes.Leave,
                0xDE => OpCodes.Leave_S,
                0xDF => OpCodes.Stind_I,
                0xE0 => OpCodes.Conv_U,
                _ => OpCodes.Nop
            };
        }
        /// <summary>
        /// Retrieve only call, calli, and callvirt instructions from the stream
        /// </summary>
        /// <returns></returns>
        internal List<Instruction> GetInstructions()
        {
            List<Instruction> list = new();
            list.AddRange(InstructionStream.FindAll(I => I.Code.Name?.Contains("call") ?? false));
            list.AddRange(InstructionStream.FindAll(I => I.Code.Name?.Contains("fld") ?? false));
            list.AddRange(InstructionStream.FindAll(I => I.Code.Name?.Equals("newobj") ?? false));;
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
                    FieldInfo? field = module.ResolveField(
                        MetadataToken(ref pos),
                        Method.ReflectedType?.GetGenericArguments(),
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

                    return field is null ? string.Empty : $"[{field.FieldType.Name}] {field.ReflectedType?.Name}::{field.Name}";
                case OperandType.InlineI:
                    return I4(ref pos).ToString();
                case OperandType.InlineI8:
                    return I8(ref pos).ToString();
                case OperandType.InlineMethod:
                    MethodBase? methodbase = module.ResolveMethod(
                        MetadataToken(ref pos),
                        Method.ReflectedType?.GetGenericArguments(),
                        Method is ConstructorInfo ? Array.Empty<Type>() : Method.GetGenericArguments()
                    );

                    if (methodbase is null) return string.Empty;

                    return (methodbase.IsStatic ? " static " : " ") + 
                        (methodbase is ConstructorInfo ? "[Void]" : ((MethodInfo)methodbase).ReturnType.Name)
                        + $" {methodbase.ReflectedType?.Name}::{methodbase.Name}()";
                case OperandType.InlineR:
                    return R8(ref pos).ToString();
                case OperandType.InlineSig:
                    return module.ResolveSignature(MetadataToken(ref pos)).ToString() ?? string.Empty;
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
