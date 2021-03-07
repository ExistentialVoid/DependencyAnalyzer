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
        private object operand;
        private int ilArrPos;
        /// <summary>
        /// The OpCode associated with MethodBodyILByte[n]
        /// </summary>
        public OpCode Code { get => code; set => code = value; }
        /// <summary>
        /// The readable IL instruction
        /// </summary>
        public object Operand { get => operand; set => operand = value; }
        /// <summary>
        /// The n of MethodBodyILByte[n]; used for reporting on UI
        /// </summary>
        public int ILArrayPos { get => ilArrPos; set => ilArrPos = value; }

        public ILInstruction() { }

        /// <summary>
        /// Add enough zeros to a number as to be represented on 4 characters
        /// </summary>
        /// <param name="num">The number that must be represented on 4 characters</param>
        /// <returns></returns>
        private string Get4DigitNum(long num)
        {
            string numStr = num.ToString();
            for (int i = 0; numStr.Length < 4; i++) numStr = $"0{numStr}";
            return numStr;
        }

        /// <summary>
        /// Returns a friendly string representation of this instruction
        /// </summary>
        /// <returns></returns>
        public string GetILCode()
        {
            string result = "";
            result += $"{Get4DigitNum(ilArrPos)} : {code}";
            if (operand != null)
            {
                switch (code.OperandType)
                {
                    case OperandType.InlineField:
                        FieldInfo fOperand = ((FieldInfo)operand);
                        return $"{result} {Architect.ProcessSpecialTypes(fOperand.FieldType.ToString())} " +
                            $"{Architect.ProcessSpecialTypes(fOperand.ReflectedType.ToString())}::{fOperand.Name}";
                    case OperandType.InlineMethod:
                        try
                        {
                            MethodInfo mOperand = (MethodInfo)operand;
                            return result + (!mOperand.IsStatic ? " instance" : "") + 
                                $" {Architect.ProcessSpecialTypes(mOperand.ReturnType.ToString())} " +
                                $"{Architect.ProcessSpecialTypes(mOperand.ReflectedType.ToString())}::{mOperand.Name}()";
                        }
                        catch
                        {
                            try
                            {
                                ConstructorInfo mOperand = (ConstructorInfo)operand;
                                return result + (!mOperand.IsStatic ? " instance" : "") + 
                                    $" void {Architect.ProcessSpecialTypes(mOperand.ReflectedType.ToString())}::{mOperand.Name}()";
                            }
                            catch { return $"{result} Error!"; }
                        }
                    case OperandType.ShortInlineBrTarget:
                    case OperandType.InlineBrTarget: return $"{result} {Get4DigitNum((int)operand)}";
                    case OperandType.InlineType: return $"{result} {Architect.ProcessSpecialTypes(operand.ToString())}";
                    case OperandType.InlineString:
                        if (operand.ToString() == "\r\n") return result + " \"\\r\\n\"";
                        else return result + " \"" + operand.ToString() + "\"";
                    case OperandType.ShortInlineVar:
                    case OperandType.InlineI:
                    case OperandType.InlineI8:
                    case OperandType.InlineR:
                    case OperandType.ShortInlineI:
                    case OperandType.ShortInlineR: return $"{result} {operand}";
                    case OperandType.InlineTok:
                        if (operand is Type) return $"{result} {((Type)operand).FullName}";
                        else return $"{result} not supported";
                    default: return $"{result} not supported";
                } 
            }
            return result;
        }
    }
}
