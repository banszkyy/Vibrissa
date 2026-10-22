using LanguageCore.Compiler;
using LanguageCore.Runtime;

namespace LanguageCore.Bytecode.Generator;

public struct GeneratorStatistics
{
    public int Optimizations;
    public int Precomputations;
    public int FunctionEvaluations;
    public int InstructionLevelOptimizations;
    public int InlinedFunctions;
}

public struct BytecodeGeneratorResult
{
    public ImmutableArray<Instruction> Code;
    public BytecodeEmitter CodeEmitter;
    public DebugInformation? DebugInfo;
    public GeneratorStatistics Statistics;
    public ImmutableArray<CompiledFunctionDefinition> CompiledFunctions;
    public ImmutableArray<CompiledOperatorDefinition> CompiledOperators;
    public ImmutableArray<CompiledGeneralFunctionDefinition> CompiledGeneralFunctions;
    public FrozenDictionary<string, ExposedFunction> ExposedFunctions;
    public ImmutableArray<ExternalFunctionScopedSync> GeneratedUnmanagedFunctions;
    public ImmutableArray<ExternalFunctionScopedSyncCallback> GeneratedUnmanagedFunctionReferences;
    public ImmutableArray<string> ILGeneratorBuilders;
}
