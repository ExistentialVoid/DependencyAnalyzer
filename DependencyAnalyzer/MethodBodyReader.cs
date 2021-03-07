using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace DependencyAnalyzer
{
    /// <summary>
    /// Build readable MSIL instructions
    /// </summary>
    public class MethodBodyReader
    {
        public List<DependencyAnalyzer.ILInstruction> Instructions = null;
        private readonly MethodInfo method = null;
        private readonly byte[] il;

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
            Instructions = BuildInstructions();
            il = method.GetMethodBody().GetILAsByteArray();
        }
        
        /// <summary>
        /// Get an operand from il[]
        /// </summary>
        /// <param name="type">determine what object will be returned</param>
        /// <param name="index">the index of il[]</param>
        /// <returns></returns>
        private object GetOperand(OperandType type, int index)
        {
            Module module = method.Module;
            switch (type)
            {
                case OperandType.InlineBrTarget: 
                    return ReadInt32(il, ref index) + index;
                case OperandType.InlineField: 
                    return module.ResolveField(ReadInt32(il, ref index));
                case OperandType.InlineMethod:
                    try { return module.ResolveMethod(ReadInt32(il, ref index)); }
                    catch { return module.ResolveMember(ReadInt32(il, ref index)); }
                case OperandType.InlineSig: return module.ResolveSignature(ReadInt32(il, ref index));
                case OperandType.InlineTok:
                    try { return module.ResolveType(ReadInt32(il, ref index)); }
                    catch { return null; }
                    // SSS : see what to do here
                case OperandType.InlineType:
                    // now we call the ResolveType always using the generic attributes type 
                    // in order to support decompilation of generic methods and classes                        
                    return module.ResolveType(
                        ReadInt32(il, ref index),
                        method.DeclaringType.GetGenericArguments(),
                        method.GetGenericArguments());
                case OperandType.InlineI: 
                    return ReadInt32(il, ref index);
                case OperandType.InlineI8: 
                    return ReadInt64(il, ref index);
                case OperandType.InlineNone: 
                    return null;
                case OperandType.InlineR: 
                    return ReadDouble(il, ref index);
                case OperandType.InlineString: 
                    return module.ResolveString(ReadInt32(il, ref index));
                case OperandType.InlineSwitch:
                    int count = ReadInt32(il, ref index);
                    int[] casesAddresses = new int[count];
                    int[] cases = new int[count];
                    for (int i = 0; i < count; i++) casesAddresses[i] = ReadInt32(il, ref i);
                    for (int i = 0; i < count; i++) cases[i] = i + casesAddresses[i];
                    return null;
                case OperandType.InlineVar: 
                    return ReadUInt16(il, ref index);
                case OperandType.ShortInlineBrTarget: 
                    return ReadSByte(il, ref index) + index;
                case OperandType.ShortInlineI: 
                    return ReadSByte(il, ref index);
                case OperandType.ShortInlineR: 
                    return ReadFloat(il, ref index);
                case OperandType.ShortInlineVar: 
                    return ReadByte(il, ref index);
                default: throw new Exception("Unknown operand type.");
            }
        }

        /// <summary>
        /// Constructs the array of ILInstructions according to the IL byte code.
        /// </summary>
        /// <param name="module"></param>
        private List<DependencyAnalyzer.ILInstruction> BuildInstructions()
        {
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
                
                Instructions.Add(new DependencyAnalyzer.ILInstruction() 
                { Code = code, ILArrayPos = ilArrPos, Operand = GetOperand(code.OperandType, index) });
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
        /// Concat all il line codes as readable text
        /// </summary>
        /// <returns>A single string with \n literals included</returns>
        public override string ToString()
        {
            string result = "";
            if (Instructions != null) Instructions.ForEach(i => result += i.GetILCode() + "\n");
            return result;
        }
    }
}
