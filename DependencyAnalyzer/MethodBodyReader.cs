using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace DependencyAnalyzer
{
    /// <summary>
    /// 
    /// </summary>
    public class MethodBodyReader
    {
        public List<DependencyAnalyzer.ILInstruction> Instructions = null;
        private readonly MethodInfo method = null;

        #region il read methods
        //private int ReadInt16(byte[] _il, ref int position) => _il[position++] | (_il[position++] << 0x8);
        private ushort ReadUInt16(byte[] _il, ref int i) => (ushort)(_il[i++] | (_il[i++] << 0x8));
        private int ReadInt32(byte[] _il, ref int i) => _il[i++] | (_il[i++] << 0x8) | (_il[i++] << 0x10) | (_il[i++] << 0x18);
        private ulong ReadInt64(byte[] _il, ref int i) => (ulong)(_il[i++] | (_il[i++] << 0x8) | (_il[i++] << 0x10) | (_il[i++] << 0x18) | (_il[i++] << 0x20) | (_il[i++] << 0x28) | (_il[i++] << 0x30) | (_il[i++] << 0x38));
        private double ReadDouble(byte[] _il, ref int i) => _il[i++] | (_il[i++] << 0x8) | (_il[i++] << 0x10) | (_il[i++] << 0x18) | (_il[i++] << 0x20) | (_il[i++] << 0x28) | (_il[i++] << 0x30) | (_il[i++] << 0x38);
        private sbyte ReadSByte(byte[] _il, ref int i) => (sbyte)_il[i++];
        private byte ReadByte(byte[] _il, ref int i) => _il[i++];
        private float ReadFloat(byte[] _il, ref int i) => _il[i++] | (_il[i++] << 0x8) | (_il[i++] << 0x10) | (_il[i++] << 0x18);
        #endregion

        /// <summary>
        /// MethodBodyReader constructor
        /// </summary>
        /// <param name="method">The System.Reflection defined MethodInfo</param>
        public MethodBodyReader(MethodInfo method)
        {
            if (method.GetMethodBody() == null) return;

            this.method = method;
            Instructions = ConstructInstructions(method.Module);
        }
        
        /// <summary>
        /// Constructs the array of ILInstructions according to the IL byte code.
        /// </summary>
        /// <param name="module"></param>
        private List<DependencyAnalyzer.ILInstruction> ConstructInstructions(Module module)
        {
            byte[] il = method.GetMethodBody().GetILAsByteArray();
            int index = 0;
            Instructions = new List<DependencyAnalyzer.ILInstruction>();
            while (index < il.Length)
            {
                // get the operation code of the current instruction
                int ilArrPos = index;
                byte ilByte = il[index++];
                OpCode code;
                if (ilByte != 0xfe) code = Architect.singleByteOpCodes[ilByte];
                else
                {
                    ilArrPos--;
                    ilByte = il[index++];
                    code = Architect.multiByteOpCodes[ilByte];
                }
                DependencyAnalyzer.ILInstruction instruction = 
                    new DependencyAnalyzer.ILInstruction() { Code = code, ILArrayPos = ilArrPos };

                // get the operand of the current operation
                switch (code.OperandType)
                {
                    case OperandType.InlineBrTarget: 
                        instruction.Operand = ReadInt32(il, ref index) + index; 
                        break;
                    case OperandType.InlineField: 
                        instruction.Operand = module.ResolveField(ReadInt32(il, ref index)); 
                        break;
                    case OperandType.InlineMethod:
                        try { instruction.Operand = module.ResolveMethod(ReadInt32(il, ref index)); }
                        catch { instruction.Operand = module.ResolveMember(ReadInt32(il, ref index)); }
                        break;
                    case OperandType.InlineSig: 
                        instruction.Operand = module.ResolveSignature(ReadInt32(il, ref index)); 
                        break;
                    case OperandType.InlineTok:
                        try { instruction.Operand = module.ResolveType(ReadInt32(il, ref index)); }
                        catch { }
                        // SSS : see what to do here
                        break;
                    case OperandType.InlineType:
                        // now we call the ResolveType always using the generic attributes type 
                        // in order to support decompilation of generic methods and classes                        
                        instruction.Operand = module.ResolveType(
                            ReadInt32(il, ref index), 
                            method.DeclaringType.GetGenericArguments(), 
                            method.GetGenericArguments());
                        break;
                    case OperandType.InlineI: 
                        instruction.Operand = ReadInt32(il, ref index); 
                        break;
                    case OperandType.InlineI8: 
                        instruction.Operand = ReadInt64(il, ref index); 
                        break;
                    case OperandType.InlineNone: 
                        instruction.Operand = null; 
                        break;
                    case OperandType.InlineR: 
                        instruction.Operand = ReadDouble(il, ref index); 
                        break;
                    case OperandType.InlineString: 
                        instruction.Operand = module.ResolveString(ReadInt32(il, ref index)); 
                        break;
                    case OperandType.InlineSwitch:
                        int count = ReadInt32(il, ref index);
                        int[] casesAddresses = new int[count];
                        int[] cases = new int[count];
                        for (int i = 0; i < count; i++) casesAddresses[i] = ReadInt32(il, ref i);
                        for (int i = 0; i < count; i++) cases[i] = i + casesAddresses[i];
                        break;
                    case OperandType.InlineVar: 
                        instruction.Operand = ReadUInt16(il, ref index); 
                        break;
                    case OperandType.ShortInlineBrTarget: 
                        instruction.Operand = ReadSByte(il, ref index) + index; 
                        break;
                    case OperandType.ShortInlineI: 
                        instruction.Operand = ReadSByte(il, ref index); 
                        break;
                    case OperandType.ShortInlineR: 
                        instruction.Operand = ReadFloat(il, ref index); 
                        break;
                    case OperandType.ShortInlineVar: 
                        instruction.Operand = ReadByte(il, ref index); 
                        break;
                    default: 
                        throw new Exception("Unknown operand type.");
                }
                System.Threading.Tasks.Task.WaitAll();
                Instructions.Add(instruction);
            }
            return Instructions;
        }

        //public object GetRefferencedOperand(Module module, int metadataToken)
        //{
        //    object o = null;
        //    module.Assembly.GetReferencedAssemblies().ToList().ForEach(a =>
        //    {
        //        Assembly.Load(a).GetModules().ToList().ForEach(m =>
        //        {
        //            try { o = m.ResolveType(metadataToken); }
        //            catch { }
        //        });
        //    });
        //    return o;
        ////Assembly.Load(module.Assembly.GetReferencedAssemblies()[3]).GetModules()[0].ResolveType(metadataToken)
        //}

        /// <summary>
        /// Concat the latter, string, parts of opCode
        /// </summary>
        /// <returns>A single string with \n literals included</returns>
        public string GetBodyCode()
        {
            string result = "";
            if (Instructions != null) Instructions.ForEach(i => result += i.GetCode(false) + "\n");
            return result;
        }
    }
}
