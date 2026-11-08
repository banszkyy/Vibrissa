namespace LanguageCore.Compiler;

public class CompiledConstructorCall : CompiledExpression
{
    public required TemplateInstance<CompiledGeneralFunctionDefinition> Function { get; init; }
    public required CompiledExpression Object { get; init; }
    public required ImmutableArray<CompiledArgument> Arguments { get; init; }
    public required ImmutableArray<CompiledInitializationItem> Initialization { get; init; }

    public override string ToString() => $"new {GeneralType.TryInsertTemplateParameters(Function.Template.Type, Function.TemplateArguments)}({string.Join(", ", Arguments.Select(v => v.ToString()))}){(Initialization.IsDefault ? null : $"{{{string.Join(", ", Initialization.Select(v => v.ToString()))}}}")}";
}
