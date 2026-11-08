using LanguageCore.Tokenizing;

namespace LanguageCore.Compiler;

class CompiledFrame
{
    public required ImmutableDictionary<string, CompiledTemplateArgument> TemplateArguments { get; init; }
    public required ImmutableArray<Token> TemplateParameters { get; init; }
    public required bool IsTemplateInstance { get; init; }
    public required bool IsTemplate { get; init; }
    public required ImmutableArray<CompiledParameter> CompiledParameters { get; init; }
    public required ImmutableArray<CompiledLabelDeclaration> InstructionLabels { get; init; }
    public required Stack<Scope> Scopes { get; init; }
    public required GeneralType? CurrentReturnType { get; set; }
    public required bool IsTopLevel { get; set; }
    public HashSet<CompiledVariableDefinition> CapturedVariables { get; } = new();
    public HashSet<CompiledParameter> CapturedParameters { get; } = new();
    public bool? CapturesGlobalVariables { get; set; } = false;
    public bool IsMsilCompatible { get; set; } = true;

    public static CompiledFrame Empty => new()
    {
        TemplateArguments = ImmutableDictionary<string, CompiledTemplateArgument>.Empty,
        TemplateParameters = ImmutableArray<Token>.Empty,
        IsTemplateInstance = false,
        IsTemplate = false,
        CompiledParameters = ImmutableArray<CompiledParameter>.Empty,
        InstructionLabels = ImmutableArray<CompiledLabelDeclaration>.Empty,
        Scopes = new(),
        CurrentReturnType = BuiltinType.Void,
        IsTopLevel = false,
    };
}
