using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

namespace DependencyAnalyzer
{
    public class ILInstruction
    {
        private OpCode code;
        public OpCode Code { get => code; set => code = value; }
        private object operand;
        public object Operand { get => operand; set => operand = value; }
        private int offset;
        public int Offset { get => offset; set => offset = value; }

        public ILInstruction() { }

        /// <summary>
        /// Returns a friendly string representation of this instruction
        /// </summary>
        /// <returns></returns>
        public string GetCode(bool includeILByte)
        {
            string result = "";
            result += includeILByte ? $"{GetExpandedOffset(Offset)} : " : "" + Code.ToString();
            if (Operand != null)
            {
                switch (Code.OperandType)
                {
                    case OperandType.InlineField:
                        FieldInfo fOperand = ((FieldInfo)Operand);
                        result += $" {Architect.ProcessSpecialTypes(fOperand.FieldType.ToString())} " +
                            $"{Architect.ProcessSpecialTypes(fOperand.ReflectedType.ToString())}::{fOperand.Name}";
                        break;
                    case OperandType.InlineMethod:
                        try
                        {
                            MethodInfo mOperand = (MethodInfo)Operand;
                            if (!mOperand.IsStatic) result += " instance";
                            result += $" {Architect.ProcessSpecialTypes(mOperand.ReturnType.ToString())} " +
                                $"{Architect.ProcessSpecialTypes(mOperand.ReflectedType.ToString())}::{mOperand.Name}()";
                        }
                        catch
                        {
                            try
                            {
                                ConstructorInfo mOperand = (ConstructorInfo)Operand;
                                if (!mOperand.IsStatic) result += " instance";
                                result += $" void {Architect.ProcessSpecialTypes(mOperand.ReflectedType.ToString())}::{mOperand.Name}()";
                            }
                            catch { }
                        }
                        break;
                    case OperandType.ShortInlineBrTarget:
                    case OperandType.InlineBrTarget:
                        result += $" {GetExpandedOffset((int)Operand)}";
                        break;
                    case OperandType.InlineType:
                        result += $" {Architect.ProcessSpecialTypes(Operand.ToString())}";
                        break;
                    case OperandType.InlineString:
                        if (Operand.ToString() == "\r\n") result += " \"\\r\\n\"";
                        else result += " \"" + Operand.ToString() + "\"";
                        break;
                    case OperandType.ShortInlineVar:
                    case OperandType.InlineI:
                    case OperandType.InlineI8:
                    case OperandType.InlineR:
                    case OperandType.ShortInlineI:
                    case OperandType.ShortInlineR:
                        result += Operand.ToString();
                        break;
                    case OperandType.InlineTok:
                        if (Operand is Type) result += ((Type)Operand).FullName;
                        else result += "not supported";
                        break;
                    default: 
                        result += "not supported"; 
                        break;
                }
            }
            return result;
        }

        /// <summary>
        /// Add enough zeros to a number as to be represented on 4 characters
        /// </summary>
        /// <param name="offset">The number that must be represented on 4 characters</param>
        /// <returns></returns>
        private string GetExpandedOffset(long offset)
        {
            string result = offset.ToString();
            for (int i = 0; result.Length < 4; i++) result = $"0{result}";
            return result;
        }
    }
}
