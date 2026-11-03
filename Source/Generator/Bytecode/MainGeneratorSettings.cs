using System.Threading;
using LanguageCore.IL.Generator;
using LanguageCore.Runtime;

namespace LanguageCore.Bytecode.Generator;

[Flags]
public enum GeneratorOptimizationSettings : uint
{
    None = 0,
    All = uint.MaxValue,
    BytecodeLevel = 1,
    BinaryOperatorFetchSkip = 2,
    IndexerFetchSkip = 4,
    TrimReturnBreak = 8,
    CrashStringOnStack = 16,
    ConditionTrimming = 32,
    TailCall = 64,
}

[ExcludeFromCodeCoverage]
public struct MainGeneratorSettings
{
    public bool GenerateComments;
    public GeneratorOptimizationSettings Optimizations;
    public bool CheckNullPointers;
    public int PointerSize;
    public readonly BitWidth PointerBitWidth => (BitWidth)PointerSize;
    public bool CleanupGlobalVaraibles;
    public ILGeneratorSettings? ILGeneratorSettings;
    public bool IsExpression;
    public CancellationToken CancellationToken;

    public MainGeneratorSettings(MainGeneratorSettings other)
    {
        GenerateComments = other.GenerateComments;
        Optimizations = other.Optimizations;
        CheckNullPointers = other.CheckNullPointers;
        PointerSize = other.PointerSize;
        CleanupGlobalVaraibles = other.CleanupGlobalVaraibles;
        ILGeneratorSettings = other.ILGeneratorSettings;
        IsExpression = other.IsExpression;
        CancellationToken = other.CancellationToken;
    }

    public static MainGeneratorSettings Default => new()
    {
        GenerateComments = true,
        Optimizations = GeneratorOptimizationSettings.All,
        CheckNullPointers = true,
        PointerSize = 4,
        CleanupGlobalVaraibles = true,
        ILGeneratorSettings = null,
        CancellationToken = CancellationToken.None,
    };
}
