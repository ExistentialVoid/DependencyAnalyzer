using System.Reflection.Emit;

namespace SelfReferencing
{
    /// <summary>
    /// Individual section of IL bytes representing the IL instruction
    /// </summary>
    internal struct Instruction
    {
        /// <summary>
        /// The first byte of code of the instruction
        /// </summary>
        public OpCode Code { get; }
        /// <summary>
        /// The readable latter-section of the instruction
        /// </summary>
        public string Operand { get; }
        /// <summary>
        /// The position of the OpCode inside the ILByteArray
        /// </summary>
        public int Position { get; }

        public Instruction(OpCode code, string operand, int position)
        {
            Code = code;
            Operand = operand;
            Position = position;
        }

        /// <summary>
        /// Returns a friendly string representation of this instruction
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"{Position,4} : {Code} {Operand}";
    }
}
