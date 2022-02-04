global using System.Reflection.Emit;

namespace DependencyAnalyzer;

/// <summary>
/// Build readable MSIL instructions
/// </summary>
internal class MetadataObject
{
    private byte[] IL { get; } = null;
    /// <summary>
    /// The full instruction set of MethodBody interpreted from ILByteArray
    /// </summary>
    public List<Instruction> InstructionStream { get; } = new List<Instruction>();
    private MemberInfo Member { get; } = null; //Neccessary to get module and handle ConstructorInfo method body
    private MethodInfo Method { get; } = null;
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
    /// Interpret method body for any MethodInfo
    /// </summary>
    /// <param name="member"></param>
    /// <param name="method">Must leave null for constructor ONLY</param>
    public MetadataObject(MemberInfo member, MethodInfo method = null)
    {
        Member = member;
        Method = method;

        if (method?.GetMethodBody() != null)
        {
            IL = method.GetMethodBody()?.GetILAsByteArray();
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
            if (ilByte != 0xFE) code = ilByte.ConvToSingleOpCode();
            else
            {
                ilByte = IL[index++];
                code = ilByte.ConvToMultiOpCode();
            }

            string operand = GetOperand(code.OperandType, ref index); // advance index into next instruction
            InstructionStream.Add(new(code, operand, pos));
        }
    }
    /// <summary>
    /// Retrieve only call, calli, and callvirt instructions from the stream
    /// </summary>
    /// <returns></returns>
    internal List<Instruction> GetCallInstructions()
    {
        List<Instruction> list = new();
        list.AddRange(InstructionStream.FindAll(I => I.Code.Name.Contains("call")));
        list.AddRange(StrayInstructions.FindAll(I => I.Code.Name.Contains("call")));
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
        Module module = Member.Module;
        switch (type)
        {
            case OperandType.InlineBrTarget:
                return (I4(ref pos) + pos).ToString();
            case OperandType.InlineField:
                FieldInfo field = module.ResolveField(MetadataToken(ref pos),
                    Method.DeclaringType.GetGenericArguments(), Method.GetGenericArguments());

                if (field.FieldType.Name.Contains("Predicate"))
                {
                    field.FieldType.GetMembers().ToList().FindAll(m => m.MemberType == MemberTypes.Method).ForEach(m =>
                    {
                        MetadataObject mdobj = new MetadataObject(m, (MethodInfo)m);
                        StrayInstructions.AddRange(mdobj.GetCallInstructions());
                    });
                }

                return $"[{field.FieldType.Name}] {field.DeclaringType.Name}::{field.Name}";
            case OperandType.InlineI:
                return I4(ref pos).ToString();
            case OperandType.InlineI8:
                return I8(ref pos).ToString();
            case OperandType.InlineMethod:
                MethodBase methodbase;

                if (Method == null)
                {
                    ConstructorInfo C = (ConstructorInfo)Member;
                    methodbase = module.ResolveMethod(MetadataToken(ref pos),
                        C.DeclaringType.GetGenericArguments(), C.GetGenericArguments());
                }
                else
                {
                    methodbase = module.ResolveMethod(MetadataToken(ref pos),
                        Method.DeclaringType.GetGenericArguments(), Method.GetGenericArguments());
                }

                return (Method.IsStatic ? " static " : " ") + (Member.MemberType == MemberTypes.Constructor ? "[Void]" :
                    $"[{Method.ReturnType.Name}]") + $" {methodbase.DeclaringType.Name}::{methodbase.Name}()";
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
                //return module.ResolveType(MetadataToken(ref pos), Member.DeclaringType.GetGenericArguments(), Member.GetType().GetGenericArguments()).FullName;
                //return module.ResolveType(MetaTokenI4(ref pos), Member.DeclaringType.GetGenericArguments(), genericArgs).FullName;

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
    /// <summary>
    /// Concat all il line codes as readable text
    /// </summary>
    /// <returns>A single string with \n literals included</returns>
    public override string ToString()
    {
        string result = string.Empty;
        InstructionStream.ForEach(I => result += I.ToString() + "\n");
        return result;
    }
}