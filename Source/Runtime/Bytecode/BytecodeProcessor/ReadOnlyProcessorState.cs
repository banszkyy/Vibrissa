namespace LanguageCore.Runtime;

public readonly ref struct ReadOnlyProcessorState
{
    public const int StackDirection = -1;

    public bool IsDone => Registers.CodePointer == Code.Length;
    public int StackStart => ProcessorState.StackDirection > 0 ? Settings.HeapSize : Settings.HeapSize + Settings.StackSize - 1;

    readonly BytecodeInterpreterSettings Settings;

    public readonly ReadOnlySpan<byte> Memory;
    public readonly ReadOnlySpan<Instruction> Code;
    public readonly ReadOnlySpan<ExternalFunctionScopedSync> ScopedExternalFunctions;

    public readonly int Crash;
    public readonly Signal Signal;
    public readonly Registers Registers;

    public bool IsValid => !Code.IsEmpty;

#if !UNITY_BURST
    public readonly ReadOnlySpan<IExternalFunction> ExternalFunctions;
#endif

    public ReadOnlyProcessorState(
        BytecodeInterpreterSettings settings,
        Registers registers,
        ReadOnlySpan<byte> memory,
        ReadOnlySpan<Instruction> code,
#if !UNITY_BURST
        ReadOnlySpan<IExternalFunction> externalFunctions,
#endif
        ReadOnlySpan<ExternalFunctionScopedSync> scopedExternalFunctions,
        int crash = 0,
        Signal signal = Signal.None
    )
    {
        Settings = settings;
        Registers = registers;
        Memory = memory;
        Code = code;
#if !UNITY_BURST
        ExternalFunctions = externalFunctions;
#endif
        ScopedExternalFunctions = scopedExternalFunctions;
        Crash = crash;
        Signal = signal;
    }
}
