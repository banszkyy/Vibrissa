namespace LanguageCore.Runtime;

public struct BytecodeInterpreterSettings
{
    public int MemorySize;

    public readonly int StackStart => ProcessorState.StackDirection > 0 ? throw new NotImplementedException() : MemorySize - 1;

    public static BytecodeInterpreterSettings Default => new()
    {
        MemorySize = 4096,
    };

    public BytecodeInterpreterSettings(BytecodeInterpreterSettings other)
    {
        MemorySize = other.MemorySize;
    }
}
