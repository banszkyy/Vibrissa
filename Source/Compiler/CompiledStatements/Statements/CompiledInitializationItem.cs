namespace LanguageCore.Compiler;

public class CompiledInitializationItem : CompiledStatement
{
    public required CompiledField Field { get; init; }
    public required CompiledExpression Value { get; init; }
    public required bool IsCompoundAssignment { get; init; }

    public override string ToString() => $"{Field} = {Value}";
}
