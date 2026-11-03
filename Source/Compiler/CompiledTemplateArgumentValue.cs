using LanguageCore.Parser.Statements;

namespace LanguageCore.Compiler;

public class CompiledTemplateArgumentValue : CompiledTemplateArgument, IReferenceable<IdentifierExpression>
{
    public required CompiledValue Value { get; init; }
    public List<Reference<IdentifierExpression>> References { get; } = new();

    public override bool SameAs(CompiledTemplateArgument other) => other is CompiledTemplateArgumentValue v && Value.Equals(v.Value);
    public override string ToString() => Value.ToString();
}
