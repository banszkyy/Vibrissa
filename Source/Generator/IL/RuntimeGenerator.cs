using System.Reflection.Emit;
using LanguageCore.IL.Reflection;
using LanguageCore.Runtime;

namespace LanguageCore.IL.Generator;

public partial class CodeGeneratorForIL
{
    public delegate void GeneratedDelegate(ref ProcessorState processor);

    public static GeneratedDelegate Generate(in ProcessorState processor, ReadOnlySpan<Instruction> instructions, ModuleBuilder module)
    {
        DynamicMethod method = new(
            "exec",
            typeof(void),
            new Type[] { typeof(ProcessorState).MakeByRefType() },
            module
        );
        ILProxy il = new(method.GetILGenerator(), false);

        for (int i = 0; i < instructions.Length; i++)
        {
            Generate(processor, instructions[i], il);
        }

        il.Emit(OpCodes.Ret);

        return method.CreateDelegate<GeneratedDelegate>();
    }

    static void Generate(in ProcessorState processor, in Instruction instruction, ILProxy il)
    {
        switch (instruction.Opcode)
        {
            case Opcode.NOP:
                break;
            case Opcode.Push:
                il.Emit(OpCodes.Ldarg_0);
                EmitGetData(instruction.Operand1, il);
                EmitValue((int)instruction.Operand1.BitWidth, il);
                il.Emit(OpCodes.Call, typeof(ProcessorState).GetMethod("Push", new Type[] { typeof(int), typeof(BitWidth) })!);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldflda, typeof(ProcessorState).GetField(nameof(ProcessorState.Registers))!);
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldfld, typeof(Registers).GetField(nameof(Registers.CodePointer))!);
                EmitValue(1, il);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Stfld, typeof(Registers).GetField(nameof(Registers.CodePointer))!);
                break;
            default:
                throw new NotImplementedException();
        }
    }

    /*
    static void EmitPush(in ProcessorState processor, BitWidth size, ILProxy il)
    {
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Dup);
        il.Emit(OpCodes.Ldflda, typeof(ProcessorState).GetField(nameof(ProcessorState.Registers))!);
        il.Emit(OpCodes.Ldfld, typeof(Registers).GetField(nameof(Registers.StackPointer))!);
        EmitValue((int)size * ProcessorState.StackDirection, il);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ldflda, typeof(ProcessorState).GetField(nameof(ProcessorState.Registers))!);
        il.Emit(OpCodes.Stfld, typeof(Registers).GetField(nameof(Registers.StackPointer))!);

        Label l = il.DefineLabel();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldflda, typeof(ProcessorState).GetField(nameof(ProcessorState.Registers))!);
        il.Emit(OpCodes.Ldfld, typeof(Registers).GetField(nameof(Registers.StackPointer))!);
        EmitValue((int)size, il);
        il.Emit(OpCodes.Add);
        EmitValue(processor.Memory.Length, il);
        il.Emit(OpCodes.Bgt, l);

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldflda, typeof(ProcessorState).GetField(nameof(ProcessorState.Registers))!);
        il.Emit(OpCodes.Ldfld, typeof(Registers).GetField(nameof(Registers.StackPointer))!);
        il.Emit(OpCodes.Ldflda, typeof(ProcessorState).GetField(nameof(ProcessorState.Registers))!);
        il.Emit(OpCodes.Ldfld, typeof(Registers).GetField(nameof(Registers.MEow))!);
        il.Emit(OpCodes.Bge, l);

        il.MarkLabel(l);
        il.Emit(OpCodes.Ldarg_0);
        EmitValue((int)Signal.StackOverflow, il);
        il.Emit(OpCodes.Stfld, typeof(ProcessorState).GetField(nameof(ProcessorState.Signal))!);
        il.Emit(OpCodes.Ret);

        EmitSetData(size);
    }
    */

    static void EmitGetData(InstructionOperand operand, ILProxy il)
    {
        switch (operand.Type)
        {
            case InstructionOperandType.Immediate8:
            {
                EmitValue(operand.Value, il);
                break;
            }
            case InstructionOperandType.Immediate16:
            {
                EmitValue(operand.Value, il);
                break;
            }
            case InstructionOperandType.Immediate32:
            {
                EmitValue(operand.Value, il);
                break;
            }
            case InstructionOperandType.Immediate64:
            {
                EmitValue(operand.Value, il);
                break;
            }
            case InstructionOperandType.Pointer8:
            {
                throw new NotImplementedException();
                //GetData<byte>(operand.Int).I32();
                //break;
            }
            case InstructionOperandType.Pointer16:
            {
                throw new NotImplementedException();
                //GetData<short>(operand.Int).I32();
                //break;
            }
            case InstructionOperandType.Pointer32:
            {
                throw new NotImplementedException();
                //GetData<int>(operand.Int).I32();
                //break;
            }
            case InstructionOperandType.PointerBP8:
            {
                throw new NotImplementedException();
                //GetData<byte>(Registers.BasePointer + operand.Int).I32();
                //break;
            }
            case InstructionOperandType.PointerBP16:
            {
                throw new NotImplementedException();
                //GetData<short>(Registers.BasePointer + operand.Int).I32();
                //break;
            }
            case InstructionOperandType.PointerBP32:
            {
                throw new NotImplementedException();
                //GetData<int>(Registers.BasePointer + operand.Int).I32();
                //break;
            }
            case InstructionOperandType.PointerBP64:
            {
                throw new NotImplementedException();
            }
            case InstructionOperandType.PointerSP8:
            {
                throw new NotImplementedException();
                //GetData<byte>(Registers.StackPointer + operand.Int).I32();
                //break;
            }
            case InstructionOperandType.PointerSP16:
            {
                throw new NotImplementedException();
                //GetData<short>(Registers.StackPointer + operand.Int).I32();
                //break;
            }
            case InstructionOperandType.PointerSP32:
            {
                throw new NotImplementedException();
                //GetData<int>(Registers.StackPointer + operand.Int).I32();
                //break;
            }

            case InstructionOperandType.PointerEAX8:
            {
                throw new NotImplementedException();
                //GetData<byte>(Registers.EAX + operand.Int).I32();
                //break;
            }
            case InstructionOperandType.PointerEAX16:
            {
                throw new NotImplementedException();
                //GetData<short>(Registers.EAX + operand.Int).I32();
                //break;
            }
            case InstructionOperandType.PointerEAX32:
            {
                throw new NotImplementedException();
                //GetData<int>(Registers.EAX + operand.Int).I32();
                //break;
            }
            case InstructionOperandType.PointerEAX64:
            {
                throw new NotImplementedException();
            }
            case InstructionOperandType.PointerEBX8:
            {
                throw new NotImplementedException();
                //GetData<byte>(Registers.EBX + operand.Int).I32();
                //break;
            }
            case InstructionOperandType.PointerEBX16:
            {
                throw new NotImplementedException();
                //GetData<short>(Registers.EBX + operand.Int).I32();
                //break;
            }
            case InstructionOperandType.PointerEBX32:
            {
                throw new NotImplementedException();
                //GetData<int>(Registers.EBX + operand.Int).I32();
                //break;
            }
            case InstructionOperandType.PointerEBX64:
            {
                throw new NotImplementedException();
            }
            case InstructionOperandType.PointerECX8:
            {
                throw new NotImplementedException();
                //GetData<byte>(Registers.ECX + operand.Int).I32();
                //break;
            }
            case InstructionOperandType.PointerECX16:
            {
                throw new NotImplementedException();
                //GetData<short>(Registers.ECX + operand.Int).I32();
                //break;
            }
            case InstructionOperandType.PointerECX32:
            {
                throw new NotImplementedException();
                //GetData<int>(Registers.ECX + operand.Int).I32();
                //break;
            }
            case InstructionOperandType.PointerECX64:
            {
                throw new NotImplementedException();
            }
            case InstructionOperandType.PointerEDX8:
            {
                throw new NotImplementedException();
                //GetData<byte>(Registers.EDX + operand.Int).I32();
                //break;
            }
            case InstructionOperandType.PointerEDX16:
            {
                throw new NotImplementedException();
                //GetData<short>(Registers.EDX + operand.Int).I32();
                //break;
            }
            case InstructionOperandType.PointerEDX32:
            {
                throw new NotImplementedException();
                //GetData<int>(Registers.EDX + operand.Int).I32();
                //break;
            }
            case InstructionOperandType.PointerEDX64:
            {
                throw new NotImplementedException();
            }

            // fixme: Same
            case InstructionOperandType.PointerRAX8:
            {
                throw new NotImplementedException();
                //GetData<byte>(Registers.EAX + operand.Int).I32();
                //break;
            }
            case InstructionOperandType.PointerRAX16:
            {
                throw new NotImplementedException();
                //GetData<short>(Registers.EAX + operand.Int).I32();
                //break;
            }
            case InstructionOperandType.PointerRAX32:
            {
                throw new NotImplementedException();
                //GetData<int>(Registers.EAX + operand.Int).I32();
                //break;
            }
            case InstructionOperandType.PointerRAX64:
            {
                throw new NotImplementedException();
            }
            case InstructionOperandType.PointerRBX8:
            {
                throw new NotImplementedException();
                //GetData<byte>(Registers.EBX + operand.Int).I32();
                //break;
            }
            case InstructionOperandType.PointerRBX16:
            {
                throw new NotImplementedException();
                //GetData<short>(Registers.EBX + operand.Int).I32();
                //break;
            }
            case InstructionOperandType.PointerRBX32:
            {
                throw new NotImplementedException();
                //GetData<int>(Registers.EBX + operand.Int).I32();
                //break;
            }
            case InstructionOperandType.PointerRBX64:
            {
                throw new NotImplementedException();
            }
            case InstructionOperandType.PointerRCX8:
            {
                throw new NotImplementedException();
                //GetData<byte>(Registers.ECX + operand.Int).I32();
                //break;
            }
            case InstructionOperandType.PointerRCX16:
            {
                throw new NotImplementedException();
                //GetData<short>(Registers.ECX + operand.Int).I32();
                //break;
            }
            case InstructionOperandType.PointerRCX32:
            {
                throw new NotImplementedException();
                //GetData<int>(Registers.ECX + operand.Int).I32();
                //break;
            }
            case InstructionOperandType.PointerRCX64:
            {
                throw new NotImplementedException();
            }
            case InstructionOperandType.PointerRDX8:
            {
                throw new NotImplementedException();
                //GetData<byte>(Registers.EDX + operand.Int).I32();
                //break;
            }
            case InstructionOperandType.PointerRDX16:
            {
                throw new NotImplementedException();
                //GetData<short>(Registers.EDX + operand.Int).I32();
                //break;
            }
            case InstructionOperandType.PointerRDX32:
            {
                throw new NotImplementedException();
                //GetData<int>(Registers.EDX + operand.Int).I32();
                //break;
            }
            case InstructionOperandType.PointerRDX64:
            {
                throw new NotImplementedException();
            }
            case InstructionOperandType.Register:
            {
                il.Emit(OpCodes.Ldarga, typeof(ProcessorState).GetField(nameof(ProcessorState.Registers))!);
                il.Emit(OpCodes.Ldarg, typeof(Registers).GetField((Register)operand.Int switch
                {
                    Register.CodePointer => nameof(Registers.CodePointer),
                    Register.StackPointer => nameof(Registers.StackPointer),
                    Register.BasePointer => nameof(Registers.BasePointer),
                    Register.EAX => nameof(Registers.EAX),
                    Register.AX => nameof(Registers.AX),
                    Register.AH => nameof(Registers.AH),
                    Register.AL => nameof(Registers.AL),
                    Register.EBX => nameof(Registers.EBX),
                    Register.BX => nameof(Registers.BX),
                    Register.BH => nameof(Registers.BH),
                    Register.BL => nameof(Registers.BL),
                    Register.ECX => nameof(Registers.ECX),
                    Register.CX => nameof(Registers.CX),
                    Register.CH => nameof(Registers.CH),
                    Register.CL => nameof(Registers.CL),
                    Register.EDX => nameof(Registers.EDX),
                    Register.DX => nameof(Registers.DX),
                    Register.DH => nameof(Registers.DH),
                    Register.DL => nameof(Registers.DL),
                    Register.MEow => nameof(Registers.MEow),
                    _ => throw new UnreachableException(),
                })!);
                break;
            }
            default: throw new UnreachableException();
        }
    }
}
