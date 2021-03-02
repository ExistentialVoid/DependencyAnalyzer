using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace DependencyAnalyzer
{
    public class MethodBodyReader
    {
        public List<DependencyAnalyzer.ILInstruction> Instructions = null;
        protected byte[] il = null;
        private MethodInfo method = null;

        #region il read methods
        //private int ReadInt16(byte[] _il, ref int position) => _il[position++] | (_il[position++] << 8);
        private ushort ReadUInt16(byte[] _il, ref int position) => (ushort)(_il[position++] | (_il[position++] << 8));
        private int ReadInt32(byte[] _il, ref int position) => _il[position++] | (_il[position++] << 8) | (_il[position++] << 16) | (_il[position++] << 24);
        private ulong ReadInt64(byte[] _il, ref int position) => (ulong)(_il[position++] | (_il[position++] << 8) | (_il[position++] << 16) | (_il[position++] << 24) | (_il[position++] << 32) | (_il[position++] << 40) | (_il[position++] << 48) | (_il[position++] << 56));
        private double ReadDouble(byte[] _il, ref int position) => _il[position++] | (_il[position++] << 8) | (_il[position++] << 16) | (_il[position++] << 24) | (_il[position++] << 32) | (_il[position++] << 40) | (_il[position++] << 48) | (_il[position++] << 56);
        private sbyte ReadSByte(byte[] _il, ref int position) => (sbyte)_il[position++];
        private byte ReadByte(byte[] _il, ref int position) => _il[position++];
        private float ReadFloat(byte[] _il, ref int position) => _il[position++] | (_il[position++] << 8) | (_il[position++] << 16) | (_il[position++] << 24);
        #endregion

        /// <summary>
        /// MethodBodyReader constructor
        /// </summary>
        /// <param name="method">The System.Reflection defined MethodInfo</param>
        public MethodBodyReader(MethodInfo method)
        {
            if (method.GetMethodBody() == null) return;

            if (Architect.singleByteOpCodes == null) Architect.LoadOpCodes();
            this.method = method;
            il = method.GetMethodBody().GetILAsByteArray();
            Instructions = ConstructInstructions(method.Module);
        }
        
        /// <summary>
        /// Constructs the array of ILInstructions according to the IL byte code.
        /// </summary>
        /// <param name="module"></param>
        private List<ILInstruction> ConstructInstructions(Module module)
        {
            byte[] il = this.il;
            int position = 0;
            Instructions = new List<ILInstruction>();
            ILInstruction instruction;
            OpCode code;
            ushort value;
            while (position < il.Length)
            {
                // get the operation code of the current instruction
                value = il[position++];
                if (value != 254) code = Architect.singleByteOpCodes[value];
                else
                {
                    value = il[position++];
                    code = Architect.multiByteOpCodes[value];
                    value = (ushort)(value | 65024);
                }
                instruction = new ILInstruction() {Code = code, Offset = position-1 };

                // get the operand of the current operation
                switch (code.OperandType)
                {
                    case OperandType.InlineBrTarget: 
                        instruction.Operand = ReadInt32(il, ref position) + position; 
                        break;
                    case OperandType.InlineField: 
                        instruction.Operand = module.ResolveField(ReadInt32(il, ref position)); 
                        break;
                    case OperandType.InlineMethod:
                        try { instruction.Operand = module.ResolveMethod(ReadInt32(il, ref position)); }
                        catch { instruction.Operand = module.ResolveMember(ReadInt32(il, ref position)); }
                        break;
                    case OperandType.InlineSig: 
                        instruction.Operand = module.ResolveSignature(ReadInt32(il, ref position)); 
                        break;
                    case OperandType.InlineTok:
                        try { instruction.Operand = module.ResolveType(ReadInt32(il, ref position)); }
                        catch { }
                        // SSS : see what to do here
                        break;
                    case OperandType.InlineType:
                        // now we call the ResolveType always using the generic attributes type 
                        // in order to support decompilation of generic methods and classes                        
                        instruction.Operand = module.ResolveType(
                            ReadInt32(il, ref position), 
                            method.DeclaringType.GetGenericArguments(), 
                            method.GetGenericArguments());
                        break;
                    case OperandType.InlineI: 
                        instruction.Operand = ReadInt32(il, ref position); 
                        break;
                    case OperandType.InlineI8: 
                        instruction.Operand = ReadInt64(il, ref position); 
                        break;
                    case OperandType.InlineNone: 
                        instruction.Operand = null; 
                        break;
                    case OperandType.InlineR: 
                        instruction.Operand = ReadDouble(il, ref position); 
                        break;
                    case OperandType.InlineString: 
                        instruction.Operand = module.ResolveString(ReadInt32(il, ref position)); 
                        break;
                    case OperandType.InlineSwitch:
                        int count = ReadInt32(il, ref position);
                        int[] casesAddresses = new int[count];
                        int[] cases = new int[count];
                        for (int i = 0; i < count; i++) casesAddresses[i] = ReadInt32(il, ref position);
                        for (int i = 0; i < count; i++) cases[i] = position + casesAddresses[i];
                        break;
                    case OperandType.InlineVar: 
                        instruction.Operand = ReadUInt16(il, ref position); 
                        break;
                    case OperandType.ShortInlineBrTarget: 
                        instruction.Operand = ReadSByte(il, ref position) + position; 
                        break;
                    case OperandType.ShortInlineI: 
                        instruction.Operand = ReadSByte(il, ref position); 
                        break;
                    case OperandType.ShortInlineR: 
                        instruction.Operand = ReadFloat(il, ref position); 
                        break;
                    case OperandType.ShortInlineVar: 
                        instruction.Operand = ReadByte(il, ref position); 
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
